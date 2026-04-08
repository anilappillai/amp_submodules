using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Amp.Core.Services.Resilience;

/// <summary>
/// Extension methods that attach the AMP default resilience policy to HTTP clients
/// and register the general-purpose <see cref="ResiliencePipeline"/> for DI injection.
/// </summary>
public static class AmpResilienceExtensions
{
    private const string HandlerKey = "amp-default";

    /// <summary>
    /// Attaches the AMP resilience handler to an <see cref="IHttpClientBuilder"/>:
    /// <list type="bullet">
    ///   <item>3 retries with exponential backoff + jitter on transient HTTP faults
    ///         (5xx, 408 Request Timeout, 429 Too Many Requests, network errors)</item>
    ///   <item>Circuit breaker — opens at ≥ 50 % failure ratio over 30 s (min 5 requests),
    ///         stays open 30 s (configurable via <paramref name="options"/>)</item>
    /// </list>
    ///
    /// Retry and circuit-breaker telemetry is emitted automatically via .NET's
    /// <c>ILogger</c> and <c>IMeterFactory</c> integrations in Polly 8.
    ///
    /// Usage:
    /// <code>
    ///   services.AddHttpClient("payments").AddAmpResilienceHandler();
    /// </code>
    /// </summary>
    public static IHttpClientBuilder AddAmpResilienceHandler(
        this IHttpClientBuilder builder,
        AmpHttpResilienceOptions? options = null)
    {
        options ??= new AmpHttpResilienceOptions();

        builder.AddResilienceHandler(HandlerKey, pipeline =>
        {
            // ── Retry ─────────────────────────────────────────────────────────
            // HttpRetryStrategyOptions pre-configures ShouldHandle for:
            //   • HttpRequestException (network errors, DNS failures)
            //   • HTTP 408, 429, 500, 502, 503, 504
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,          // randomises backoff to avoid thundering herd
                Delay = options.BaseDelay,
                MaxDelay = options.MaxDelay,
            });

            // ── Circuit breaker ────────────────────────────────────────────────
            // Sits after retry so transient faults are retried before contributing
            // to the circuit failure count.
            if (options.EnableCircuitBreaker)
            {
                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = AmpResilienceDefaults.CircuitBreakerSamplingDuration,
                    FailureRatio = AmpResilienceDefaults.CircuitBreakerFailureRatio,
                    MinimumThroughput = AmpResilienceDefaults.CircuitBreakerMinThroughput,
                    BreakDuration = AmpResilienceDefaults.CircuitBreakerBreakDuration,
                });
            }
        });

        return builder;
    }

    /// <summary>
    /// Registers <see cref="AmpResilienceDefaults.GeneralPipeline"/> as a
    /// <see cref="ResiliencePipeline"/> singleton so it can be injected into services
    /// that need resilience for non-HTTP operations (database, AWS SDK, message bus).
    ///
    /// Usage:
    /// <code>
    ///   // Registration (already called by AddAmpCoreServices)
    ///   builder.Services.AddAmpResiliencePipeline();
    ///
    ///   // Injection
    ///   public class MyRepo(ResiliencePipeline resilience) { ... }
    ///
    ///   // Execution
    ///   await resilience.ExecuteAsync(async ct => await dbCall(ct), cancellationToken);
    /// </code>
    /// </summary>
    public static IServiceCollection AddAmpResiliencePipeline(this IServiceCollection services) =>
        services.AddSingleton(AmpResilienceDefaults.GeneralPipeline);
}
