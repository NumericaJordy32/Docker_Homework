using System.Collections.Concurrent;
using System.Text.Json;

namespace HistorialClinico.Api.Messaging;

public sealed record PacienteEventReceived(
    Guid EventId,
    string EventType,
    DateTimeOffset OccurredAt,
    string RoutingKey,
    JsonElement Data,
    DateTimeOffset ReceivedAt);

public interface IPacienteEventInbox
{
    void Add(PacienteEventReceived patientEvent);
    IReadOnlyList<PacienteEventReceived> GetAll();
}

internal sealed class PacienteEventInbox : IPacienteEventInbox
{
    private const int MaximumEvents = 100;
    private readonly ConcurrentQueue<PacienteEventReceived> _events = new();

    public void Add(PacienteEventReceived patientEvent)
    {
        _events.Enqueue(patientEvent);
        while (_events.Count > MaximumEvents)
            _events.TryDequeue(out _);
    }

    public IReadOnlyList<PacienteEventReceived> GetAll() =>
        _events.Reverse().ToArray();
}
