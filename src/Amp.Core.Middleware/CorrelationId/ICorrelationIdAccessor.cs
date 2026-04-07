namespace Amp.Core.Middleware.CorrelationId;

/// <summary>Accessor injected into downstream services to read the current correlation ID.</summary>
public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; }
}
