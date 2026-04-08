namespace Amp.Core.Services.Secrets;

/// <summary>
/// Tuning options for the AWS Secrets Manager cache inside <see cref="AwsSecretsService"/>.
///
/// Register via <c>AddAmpCoreServices</c>:
/// <code>
///   builder.Services.AddAmpCoreServices(builder.Configuration, o =>
///       o.SecretsCacheOptions = new AwsSecretsCacheOptions { CacheTtl = TimeSpan.FromMinutes(30) });
/// </code>
/// </summary>
public sealed class AwsSecretsCacheOptions
{
    /// <summary>
    /// How long the secret bundle is held in memory before Secrets Manager is queried again.
    /// Default: 1 hour — keeps API call volume low while still picking up rotated secrets
    /// within a reasonable window.
    ///
    /// Trade-off:
    ///   Shorter TTL  → faster rotation propagation, more Secrets Manager API calls
    ///   Longer TTL   → fewer API calls, slower propagation of rotated credentials
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Maximum number of distinct secret names the cache can hold.
    /// Only relevant if you call multiple secret names. Default: 1000.
    /// Capped at 65535 (ushort.MaxValue) by the underlying SDK.
    /// </summary>
    public int MaxCacheSize { get; set; } = 1000;
}
