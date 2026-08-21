using System.Text;
using System.Text.Json;
using HistorialClinico.Api.Data;
using HistorialClinico.Api.Dtos;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HistorialClinico.Api.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "admin";
    public string Password { get; set; } = "root12345";
    public string VirtualHost { get; set; } = "/";
    public string Exchange { get; set; } = "clinica.events";
    public string Queue { get; set; } = "clinica.historial-clinico";
    public string RoutingKey { get; set; } = "paciente.*";
    public string ServiceName { get; set; } = "historial-clinico-api";
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

internal sealed class RabbitMqConsumer(
    IOptions<RabbitMqOptions> options,
    IPacienteEventInbox eventInbox,
    IServiceScopeFactory scopeFactory,
    ILogger<RabbitMqConsumer> logger) : BackgroundService
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
                var message = JsonSerializer.Deserialize<DomainEvent<JsonElement>>(eventArgs.Body.Span)
                    ?? throw new JsonException("El evento de paciente está vacío.");

                await SynchronizePatientAsync(message, cancellationToken);

                eventInbox.Add(new PacienteEventReceived(
                    message.EventId,
                    message.EventType,
                    message.OccurredAt,
                    eventArgs.RoutingKey,
                    message.Data,
                    DateTimeOffset.UtcNow));

                logger.LogInformation(
                    "Evento de Pacientes recibido: {EventType} ({RoutingKey}, {EventId})",
                    message.EventType,
                    eventArgs.RoutingKey,
                    message.EventId);
                await channel.BasicAckAsync(eventArgs.DeliveryTag, false, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error procesando el evento {DeliveryTag}", eventArgs.DeliveryTag);
                await channel.BasicNackAsync(
                    eventArgs.DeliveryTag,
                    multiple: false,
                    requeue: ex is not JsonException,
                    cancellationToken);
            }
        };

        await channel.BasicConsumeAsync(_options.Queue, false, consumer, cancellationToken: cancellationToken);
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task SynchronizePatientAsync(DomainEvent<JsonElement> message, CancellationToken cancellationToken)
    {
        if (message.EventType is not ("PacienteCreado" or "PacienteActualizado")) return;

        if (!message.Data.TryGetProperty("IdPaciente", out var idProperty) || !idProperty.TryGetInt32(out var patientId))
            throw new JsonException($"El evento {message.EventType} no contiene un IdPaciente válido.");

        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IHistorialRepository>();

        if (message.EventType == "PacienteActualizado")
        {
            var currentHistory = await repository.GetAutomaticByPatientIdAsync(patientId, cancellationToken);
            if (currentHistory is null)
            {
                logger.LogWarning("No existe historial automático para el paciente {PatientId}; se creará uno", patientId);
            }
            else
            {
                var diagnosis = currentHistory.Diagnostico.StartsWith("Pendiente de valoración médica", StringComparison.Ordinal)
                    ? "Pendiente de valoración médica (datos del paciente actualizados)"
                    : currentHistory.Diagnostico;

                await repository.UpdateAsync(currentHistory.IdHistorialClinico, new HistorialRequest
                {
                    IdPaciente = patientId,
                    NumHistoria = currentHistory.NumHistoria,
                    Diagnostico = diagnosis,
                    Tratamiento = currentHistory.Tratamiento,
                    Fecha = message.OccurredAt.UtcDateTime
                }, cancellationToken);

                logger.LogInformation(
                    "Historial básico {HistoryNumber} actualizado por cambios del paciente {PatientId}",
                    currentHistory.NumHistoria,
                    patientId);
                return;
            }
        }

        var request = new HistorialRequest
        {
            IdPaciente = patientId,
            NumHistoria = $"AUTO-{patientId:D8}",
            Diagnostico = "Pendiente de valoración médica",
            Tratamiento = null,
            Fecha = message.OccurredAt.UtcDateTime
        };

        try
        {
            var history = await repository.CreateAsync(request, cancellationToken);
            logger.LogInformation(
                "Historial básico {HistoryNumber} creado para el paciente {PatientId}",
                history.NumHistoria,
                patientId);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            logger.LogInformation("El historial básico del paciente {PatientId} ya había sido creado", patientId);
        }
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
        services.AddSingleton<IPacienteEventInbox, PacienteEventInbox>();
        services.AddHostedService<RabbitMqConsumer>();
        return services;
    }
}
