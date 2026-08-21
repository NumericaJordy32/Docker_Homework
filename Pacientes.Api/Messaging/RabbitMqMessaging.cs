using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Pacientes.Api.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "root12345";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "clinica.events";
    public string Queue { get; set; } = "clinica.pacientes";
    public string RoutingKey { get; set; } = "historial.*";
    public string ServiceName { get; set; } = "pacientes-api";
}

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string routingKey, string eventType, T data, CancellationToken cancellationToken = default);
}

internal sealed record DomainEvent<T>(Guid EventId, string EventType, DateTimeOffset OccurredAt, T Data);

internal sealed class RabbitMqPublisher(IOptions<RabbitMqOptions> options) : IRabbitMqPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync<T>(string routingKey, string eventType, T data, CancellationToken cancellationToken = default)
    {
        var message = new DomainEvent<T>(Guid.NewGuid(), eventType, DateTimeOffset.UtcNow, data);
        var body = JsonSerializer.SerializeToUtf8Bytes(message);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await EnsureConnectedAsync(cancellationToken);
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = message.EventId.ToString(),
                Type = eventType
            };

            await _channel!.BasicPublishAsync(_options.Exchange, routingKey, false, properties, body, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true } && _channel is { IsOpen: true }) return;

        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();

        var factory = RabbitMqFactory.Create(_options, $"{_options.ServiceName}-publisher");
        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await _channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, true, false, cancellationToken: cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null) await _channel.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        _gate.Dispose();
    }
}

internal sealed class RabbitMqConsumer(IOptions<RabbitMqOptions> options, ILogger<RabbitMqConsumer> logger) : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ConsumeAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No fue posible conectar con RabbitMQ. Nuevo intento en 5 segundos.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        var factory = RabbitMqFactory.Create(_options, $"{_options.ServiceName}-consumer");
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(_options.Exchange, ExchangeType.Topic, true, false, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(_options.Queue, true, false, false, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(_options.Queue, _options.Exchange, _options.RoutingKey, cancellationToken: cancellationToken);
        await channel.BasicQosAsync(0, 10, false, cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            try
            {
                logger.LogInformation("Evento RabbitMQ recibido ({RoutingKey}): {Message}", eventArgs.RoutingKey, Encoding.UTF8.GetString(eventArgs.Body.Span));
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error procesando el evento {DeliveryTag}", eventArgs.DeliveryTag);
                await channel.BasicNackAsync(eventArgs.DeliveryTag, false, false, cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(_options.Queue, false, consumer, cancellationToken: cancellationToken);
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}

internal static class RabbitMqFactory
{
    public static ConnectionFactory Create(RabbitMqOptions options, string clientName) => new()
    {
        HostName = options.HostName,
        Port = options.Port,
        UserName = options.UserName,
        Password = options.Password,
        VirtualHost = options.VirtualHost,
        ClientProvidedName = clientName,
        AutomaticRecoveryEnabled = true,
        TopologyRecoveryEnabled = true
    };
}

public static class RabbitMqServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
            .Validate(x => !string.IsNullOrWhiteSpace(x.HostName), "RabbitMq:HostName es obligatorio")
            .Validate(x => x.Port is > 0 and <= 65535, "RabbitMq:Port no es válido")
            .Validate(x => !string.IsNullOrWhiteSpace(x.UserName), "RabbitMq:UserName es obligatorio")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Password), "RabbitMq:Password es obligatorio")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Exchange), "RabbitMq:Exchange es obligatorio")
            .Validate(x => !string.IsNullOrWhiteSpace(x.Queue), "RabbitMq:Queue es obligatoria")
            .ValidateOnStart();
        services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();
        services.AddHostedService<RabbitMqConsumer>();
        return services;
    }
}
