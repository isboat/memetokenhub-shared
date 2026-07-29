using System.Text.Json;
using System.Text.Json.Serialization;

namespace MemeTokenHub.Shared.Messaging;

public interface IIntegrationEvent
{
    string EventType { get; }
}

public sealed record EventEnvelope<T>(
    Guid EventId,
    string EventType,
    int SchemaVersion,
    DateTimeOffset OccurredAt,
    string CorrelationId,
    string? CausationId,
    string Producer,
    string SubjectId,
    T Payload) where T : IIntegrationEvent
{
    public static EventEnvelope<T> Create(T payload, string producer, string subjectId, string correlationId,
        string? causationId = null, int schemaVersion = 1, TimeProvider? timeProvider = null) => new(
            Guid.NewGuid(), payload.EventType, schemaVersion, (timeProvider ?? TimeProvider.System).GetUtcNow(),
            correlationId, causationId, producer, subjectId, payload);
}

public static class EventSerializer
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static string Serialize<T>(EventEnvelope<T> envelope) where T : IIntegrationEvent =>
        JsonSerializer.Serialize(envelope, Options);

    public static EventEnvelope<T> Deserialize<T>(string json) where T : IIntegrationEvent =>
        JsonSerializer.Deserialize<EventEnvelope<T>>(json, Options) ?? throw new JsonException("The event envelope was empty.");
}
