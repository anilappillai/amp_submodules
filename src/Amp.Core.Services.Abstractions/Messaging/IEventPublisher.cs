namespace Amp.Core.Services.Abstractions.Messaging;

/// <summary>
/// Abstraction for publishing domain events to a message bus
/// (AWS SNS/SQS, EventBridge, etc.).
/// Callers depend on this interface; the concrete transport is wired in DI.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single event to the configured topic/queue.
    /// </summary>
    /// <typeparam name="TEvent">The event payload type. Must be JSON-serialisable.</typeparam>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : class;

    /// <summary>
    /// Publishes a batch of events. Implementations should respect the underlying
    /// service's batch size limits (e.g. SQS max 10 messages per batch).
    /// </summary>
    Task PublishBatchAsync<TEvent>(IEnumerable<TEvent> events, CancellationToken ct = default) where TEvent : class;
}

/// <summary>
/// Marker interface for domain event payloads.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}

/// <summary>
/// Base record for domain events — provides <see cref="IDomainEvent"/> defaults.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}
