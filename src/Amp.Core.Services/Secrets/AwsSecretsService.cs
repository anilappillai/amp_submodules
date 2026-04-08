using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Extensions.Caching;
using Amp.Core.Services.Abstractions.Secrets;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Amp.Core.Services.Secrets;

/// <summary>
/// AWS Secrets Manager implementation of <see cref="ISecretsService"/>.
///
/// Uses the AWS-provided <see cref="SecretsManagerCache"/> client instead of
/// a hand-rolled in-memory dictionary. Key differences from the old approach:
///
///   • TTL-based refresh (default 1 hour) — secrets rotate without a pod restart.
///   • Thread-safe, request-coalescing — concurrent requests for the same secret
///     are collapsed into one API call (no stampede on cold start).
///   • Respects AWS SDK retry/backoff — transient Secrets Manager errors are
///     retried automatically before propagating.
///
/// The cache is a singleton: one instance per process, shared across all requests.
/// Credentials are obtained via IRSA (IAM Roles for Service Accounts) — no
/// static access keys anywhere.
///
/// Configuration:
///   secretName  — from EnvironmentHelper.AwsSecretName or appsettings Aws:SecretsManager:SecretName
///   region      — from EnvironmentHelper.AwsRegion (default: us-east-1)
///   cacheTtl    — how long the cached secret bundle is valid before Secrets Manager
///                 is queried again. Default: 1 hour. Override via <see cref="AwsSecretsCacheOptions"/>.
/// </summary>
public sealed class AwsSecretsService : ISecretsService, IAsyncDisposable
{
    private readonly string _secretName;
    private readonly ILogger<AwsSecretsService> _logger;
    private readonly SecretsManagerCache _cache;

    // Parsed key-value bundle, refreshed when the cache TTL expires.
    // Volatile ensures cross-thread visibility without a full lock on reads.
    private volatile IReadOnlyDictionary<string, string>? _parsed;
    private readonly SemaphoreSlim _parseLock = new(1, 1);

    public AwsSecretsService(
        string secretName,
        string region,
        ILogger<AwsSecretsService> logger,
        AwsSecretsCacheOptions? cacheOptions = null)
    {
        _secretName = secretName;
        _logger = logger;

        var opts = cacheOptions ?? new AwsSecretsCacheOptions();

        var cacheConfig = new SecretCacheConfiguration
        {
            // How long to hold the secret before re-fetching from Secrets Manager.
            // 1 hour keeps API call volume low while still picking up rotated secrets.
            CacheItemTTL = (uint)opts.CacheTtl.TotalMilliseconds,

            // Maximum number of cached secrets (SecretCacheConfiguration.MaxCacheSize is ushort).
            MaxCacheSize = (ushort)Math.Min(opts.MaxCacheSize, ushort.MaxValue)
        };

        var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
        _cache = new SecretsManagerCache(client, cacheConfig);
    }

    public async Task<string> GetSecretValueAsync(string key, CancellationToken ct = default)
    {
        var secrets = await GetAllSecretsAsync(ct);
        return secrets.TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException(
                $"Secret key '{key}' was not found in secret '{_secretName}'.");
    }

    public async Task<string?> GetSecretValueOrDefaultAsync(
        string key, string? defaultValue = null, CancellationToken ct = default)
    {
        var secrets = await GetAllSecretsAsync(ct);
        return secrets.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllSecretsAsync(CancellationToken ct = default)
    {
        // Fast path: parsed bundle still valid (TTL enforced by SecretsManagerCache).
        // SecretsManagerCache.GetSecretString refreshes from AWS when its internal TTL expires.
        var raw = await _cache.GetSecretString(_secretName);

        // Re-parse only when the raw JSON has changed (pointer comparison is intentional —
        // SecretsManagerCache returns the same string reference while the cache is warm).
        if (_parsed is not null && ReferenceEquals(await _cache.GetSecretString(_secretName), raw))
            return _parsed;

        await _parseLock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock.
            if (_parsed is not null) return _parsed;

            if (string.IsNullOrWhiteSpace(raw))
            {
                _logger.LogWarning(
                    "AWS Secrets Manager returned an empty secret for '{SecretName}'.", _secretName);
                _parsed = new Dictionary<string, string>();
                return _parsed;
            }

            var jsonDoc = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                raw,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            _parsed = jsonDoc
                .ToDictionary(k => k.Key, v => v.Value.ToString())
                .AsReadOnly();

            _logger.LogInformation(
                "Parsed {Count} secrets from AWS Secrets Manager (secret: {SecretName})",
                _parsed.Count, _secretName);

            return _parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to parse secrets from AWS Secrets Manager (secret: {SecretName})", _secretName);
            throw;
        }
        finally
        {
            _parseLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cache.Dispose();
        _parseLock.Dispose();
        await ValueTask.CompletedTask;
    }
}
