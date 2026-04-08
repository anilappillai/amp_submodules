using Polly;
using Polly.Retry;
using System.Net.Sockets;

namespace Amp.Core.Services.Resilience;

/// <summary>
/// Central resilience constants and pipeline factories shared across all AMP services.
///
/// HTTP clients registered via <c>AddAmpCoreServices</c> automatically receive the
/// retry + circuit-breaker handler. For non-HTTP code (database, AWS SDK, message bus)
/// inject <see cref="ResiliencePipeline"/> (registered by <c>AddAmpResiliencePipeline</c>)
/// or use <see cref="GeneralPipeline"/> directly.
/// </summary>
public static class AmpResilienceDefaults
{
    // ── Retry ─────────────────────────────────────────────────────────────────

    /// <summary>Number of retry attempts (not counting the initial attempt).</summary>
    public const int MaxRetryAttempts = 3;

    /// <summary>
    /// Base delay for exponential backoff. Actual delay ≈ BaseDelay * 2^attempt + jitter.
    /// Attempt 1 ≈ 1 s, attempt 2 ≈ 2 s, attempt 3 ≈ 4 s (before jitter and capping).
    /// </summary>
    public static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(1);

    /// <summary>Maximum delay cap regardless of exponential growth.</summary>
    public static readonly TimeSpan MaxDelay = TimeSpan.FromSeconds(30);

    // ── Circuit breaker ───────────────────────────────────────────────────────

    /// <summary>Sliding window over which failures are sampled.</summary>
    public static readonly TimeSpan CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Failure ratio threshold that trips the circuit.
    /// 0.5 = circuit opens when ≥ 50 % of requests in the sampling window fail.
    /// </summary>
    public const double CircuitBreakerFailureRatio = 0.5;

    /// <summary>Minimum number of requests in the sampling window before the circuit can trip.</summary>
    public const int CircuitBreakerMinThroughput = 5;

    /// <summary>Duration the circuit stays open before allowing a probe request through.</summary>
    public static readonly TimeSpan CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30);

    // ── General-purpose pipeline ──────────────────────────────────────────────

    /// <summary>
    /// Pre-built <see cref="ResiliencePipeline"/> for non-HTTP transient faults
    /// (database, AWS SDK, message bus, etc.).
    ///
    /// Retries on:
    ///   • <see cref="TimeoutException"/>
    ///   • <see cref="HttpRequestException"/>
    ///   • <see cref="SocketException"/>
    ///   • <see cref="TaskCanceledException"/> wrapping a timeout (not user cancellation)
    ///
    /// Use via DI (preferred):
    /// <code>
    ///   builder.Services.AddAmpResiliencePipeline();
    ///   // then inject ResiliencePipeline in your service
    /// </code>
    ///
    /// Or directly for simple scenarios:
    /// <code>
    ///   await AmpResilienceDefaults.GeneralPipeline.ExecuteAsync(async ct => await DoWorkAsync(ct), ct);
    /// </code>
    /// </summary>
    public static readonly ResiliencePipeline GeneralPipeline = BuildGeneralPipeline();

    private static ResiliencePipeline BuildGeneralPipeline() =>
        new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = BaseDelay,
                MaxDelay = MaxDelay,
                ShouldHandle = new PredicateBuilder()
                    .Handle<TimeoutException>()
                    .Handle<HttpRequestException>()
                    .Handle<SocketException>()
                    .Handle<TaskCanceledException>(ex =>
                        // Retry on timeouts but not on user-initiated cancellation
                        ex.InnerException is TimeoutException or SocketException)
            })
            .Build();
}
