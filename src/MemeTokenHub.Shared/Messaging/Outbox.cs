namespace MemeTokenHub.Shared.Messaging;

public sealed record OutboxMessage(Guid EventId, string EventType, int SchemaVersion, string Payload, DateTimeOffset OccurredAt,
    string CorrelationId, DateTimeOffset? PublishedAt = null, string? Error = null);

public interface IOutboxStore
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(int batchSize, CancellationToken cancellationToken = default);
    Task MarkPublishedAsync(Guid eventId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default);
    Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default);
}

public interface IProcessedEventStore
{
    /// <summary>Atomically reserves an event for processing, returning false when it is already in flight or complete.</summary>
    Task<bool> TryBeginAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Marks a reserved event complete after its handler effects have been committed durably.</summary>
    Task CompleteAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>Releases a reservation after handler failure so a later delivery can retry it.</summary>
    Task AbandonAsync(Guid eventId, CancellationToken cancellationToken = default);
}

public sealed class InMemoryProcessedEventStore : IProcessedEventStore
{
    private readonly HashSet<Guid> _inFlight = [];
    private readonly HashSet<Guid> _processed = [];
    private readonly object _lock = new();

    public Task<bool> TryBeginAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (_processed.Contains(eventId) || !_inFlight.Add(eventId)) return Task.FromResult(false);
            return Task.FromResult(true);
        }
    }

    public Task CompleteAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_inFlight.Remove(eventId)) throw new InvalidOperationException($"Event {eventId} is not being processed.");
            _processed.Add(eventId);
        }
        return Task.CompletedTask;
    }

    public Task AbandonAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock) _inFlight.Remove(eventId);
        return Task.CompletedTask;
    }
}

/// <summary>A thread-safe development and test implementation; production services should persist outbox records atomically with domain writes.</summary>
public sealed class InMemoryOutboxStore : IOutboxStore
{
    private readonly Dictionary<Guid, OutboxMessage> _messages = [];
    private readonly HashSet<Guid> _claimed = [];
    private readonly object _lock = new();

    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_messages.TryAdd(message.EventId, message)) throw new InvalidOperationException($"Outbox event {message.EventId} already exists.");
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<OutboxMessage>> ClaimPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var selected = _messages.Values.Where(x => x.PublishedAt is null && !_claimed.Contains(x.EventId))
                .OrderBy(x => x.OccurredAt).Take(batchSize).ToArray();
            foreach (var message in selected) _claimed.Add(message.EventId);
            return Task.FromResult<IReadOnlyList<OutboxMessage>>(selected);
        }
    }

    public Task MarkPublishedAsync(Guid eventId, DateTimeOffset publishedAt, CancellationToken cancellationToken = default) =>
        UpdateAsync(eventId, message => message with { PublishedAt = publishedAt, Error = null }, cancellationToken);

    public Task MarkFailedAsync(Guid eventId, string error, CancellationToken cancellationToken = default) =>
        UpdateAsync(eventId, message => message with { Error = error }, cancellationToken);

    private Task UpdateAsync(Guid eventId, Func<OutboxMessage, OutboxMessage> update, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!_messages.TryGetValue(eventId, out var message)) throw new KeyNotFoundException($"Outbox event {eventId} was not found.");
            _messages[eventId] = update(message);
            _claimed.Remove(eventId);
        }
        return Task.CompletedTask;
    }
}
