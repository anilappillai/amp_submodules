using Amp.Core.Services.Resilience;
using Amp.Core.Services.Secrets;

namespace Amp.Core.Services.Extensions;

public sealed class AmpCoreServicesOptions
{
    public bool UseRedisCache { get; set; } = true;
    public bool RegisterSecretsService { get; set; } = true;
    public string DefaultHttpClientName { get; set; } = "default";
    public TimeSpan DefaultHttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
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
