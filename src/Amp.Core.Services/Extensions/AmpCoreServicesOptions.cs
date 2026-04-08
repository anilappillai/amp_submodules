using Amp.Core.Services.Resilience;
using Amp.Core.Services.Secrets;

namespace Amp.Core.Services.Extensions;

/// <summary>
/// Options for AMP core infrastructure services (caching, secrets, HTTP clients, resilience).
/// Pass a configure delegate to <c>AddAmpCoreServices</c> to override defaults.
/// </summary>
public sealed class AmpCoreServicesOptions
{
    /// <summary>
    /// When <c>true</c>, registers <c>DistributedCachingService</c> backed by Redis.
    /// When <c>false</c>, registers <c>InMemoryCachingService</c> instead. Default: <c>true</c>.
    /// </summary>
    public bool UseRedisCache { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, registers <c>ISecretsService</c> backed by AWS Secrets Manager.
    /// Default: <c>true</c>.
    /// </summary>
    public bool RegisterSecretsService { get; set; } = true;

    /// <summary>
    /// Name of the default named <see cref="System.Net.Http.HttpClient"/> registered in the factory.
    /// Default: <c>"default"</c>.
    /// </summary>
    public string DefaultHttpClientName { get; set; } = "default";

    /// <summary>
    /// Default request timeout applied to all registered HTTP clients. Default: 30 seconds.
    /// </summary>
    public TimeSpan DefaultHttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Named HTTP clients to register. Key = client name, Value = base URL (empty string uses no base address).
    /// </summary>
    public Dictionary<string, string> HttpClients { get; set; } = new()
    {
        ["default"] = string.Empty
    };

    /// <summary>
    /// When <c>true</c>, all HTTP clients registered by <c>AddAmpCoreServices</c> receive
    /// the AMP resilience handler (retry + circuit breaker). Default: <c>true</c>.
    /// </summary>
    public bool EnableResilience { get; set; } = true;

    /// <summary>
    /// Overrides for the retry and circuit-breaker defaults applied to HTTP clients.
    /// Only used when <see cref="EnableResilience"/> is <c>true</c>.
    /// </summary>
    public AmpHttpResilienceOptions ResilienceOptions { get; set; } = new();

    /// <summary>
    /// Tuning options for the AWS Secrets Manager in-process cache.
    /// Only used when <see cref="RegisterSecretsService"/> is <c>true</c>.
    /// </summary>
    public AwsSecretsCacheOptions SecretsCacheOptions { get; set; } = new();
}
