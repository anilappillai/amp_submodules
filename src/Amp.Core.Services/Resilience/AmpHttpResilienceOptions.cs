namespace Amp.Core.Services.Resilience;

/// <summary>
/// Per-client overrides for the AMP HTTP resilience handler.
/// All values default to <see cref="AmpResilienceDefaults"/> constants so most
/// consuming applications will not need to set anything here.
/// </summary>
public sealed class AmpHttpResilienceOptions
{
    /// <summary>Number of retry attempts. Default: <see cref="AmpResilienceDefaults.MaxRetryAttempts"/> (3).</summary>
    public int MaxRetryAttempts { get; set; } = AmpResilienceDefaults.MaxRetryAttempts;

    /// <summary>Base delay for exponential backoff. Default: 1 s.</summary>
    public TimeSpan BaseDelay { get; set; } = AmpResilienceDefaults.BaseDelay;

    /// <summary>Maximum delay cap. Default: 30 s.</summary>
    public TimeSpan MaxDelay { get; set; } = AmpResilienceDefaults.MaxDelay;

    /// <summary>
    /// When <c>true</c>, a circuit breaker is added after the retry strategy.
    /// The circuit opens when ≥ 50 % of requests fail over a 30 s window (min 5 requests)
    /// and stays open for 30 s. Default: <c>true</c>.
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;
}
