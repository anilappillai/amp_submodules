using Amp.Core.Common.Helpers;
using Amp.Core.Services.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Amp.Core.Services.Caching;

/// <summary>
/// Redis-backed implementation of <see cref="ICachingService"/>.
/// Redis connection string must come from AWS Secrets Manager
/// (key: ConnectionStrings__RedisConnection).
///
/// Registered in DI as:
///   services.AddStackExchangeRedisCache(opts => opts.Configuration = ...)
///   services.AddScoped&lt;ICachingService, DistributedCachingService&gt;();
/// </summary>
public sealed class DistributedCachingService(
    IDistributedCache cache,
    ILogger<DistributedCachingService> logger) : ICachingService
{
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var bytes = await cache.GetAsync(key, ct);
            if (bytes is null) return null;
            return JsonSerializer.Deserialize<T>(bytes, JsonHelper.DefaultOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache GET failed for key '{Key}'", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonHelper.DefaultOptions);
            var entryOptions = new DistributedCacheEntryOptions();
            if (ttl.HasValue) entryOptions.AbsoluteExpirationRelativeToNow = ttl;
            await cache.SetAsync(key, bytes, entryOptions, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache SET failed for key '{Key}'", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory(ct);
        await SetAsync(key, value, ttl, ct);
        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await cache.RemoveAsync(key, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Cache REMOVE failed for key '{Key}'", key); }
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Redis prefix-based removal requires SCAN — log a warning;
        // full implementation depends on StackExchange.Redis IServer.
        logger.LogWarning("RemoveByPrefixAsync for prefix '{Prefix}' is not implemented in DistributedCachingService. " +
                          "Inject IConnectionMultiplexer and use IServer.Keys() for prefix scans.", prefix);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var value = await cache.GetAsync(key, ct);
        return value is not null;
    }

    public async Task RefreshAsync(string key, CancellationToken ct = default)
    {
        try { await cache.RefreshAsync(key, ct); }
        catch (Exception ex) { logger.LogWarning(ex, "Cache REFRESH failed for key '{Key}'", key); }
    }
}
