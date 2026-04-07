namespace Amp.Core.Services.Abstractions.Messaging;

/// <summary>
/// Marker interface for domain event payloads.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
}
