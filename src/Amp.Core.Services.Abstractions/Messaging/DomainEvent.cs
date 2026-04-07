namespace Amp.Core.Services.Abstractions.Messaging;

/// <summary>
/// Base record for domain events — provides <see cref="IDomainEvent"/> defaults.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public abstract string EventType { get; }
}
