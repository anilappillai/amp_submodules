using Amp.Core.Common.Helpers;
using Amp.Core.Services.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Amp.Core.Services.Caching;

/// <summary>
/// Redis-backed implementation of <see cref="ICachingService"/> via ElastiCache.
///
/// <b>RemoveByPrefixAsync</b> — what it is and why it needs special handling:
///
///   A cache prefix is a naming convention where related keys share a common leading
///   string, e.g. "orders:" for all order-related cache entries:
///     orders:123, orders:456, orders:list:page1
///
///   Removing by prefix lets you invalidate an entire logical group in one call —
///   for example, after a bulk update you can evict all "orders:" keys at once
///   rather than tracking every individual key.
///
///   <see cref="IDistributedCache"/> has no built-in prefix removal because it is
///   provider-agnostic (it works over in-memory, Redis, SQL Server, etc.).
///   Redis itself has no atomic "delete by prefix" command either.
///
///   The correct Redis approach is:
///     1. Use <c>SCAN</c> to find all keys matching the prefix pattern.
///        (Never use <c>KEYS *</c> in production — it blocks the Redis server.)
///     2. Delete the matched keys in batches.
///
///   This requires <see cref="IConnectionMultiplexer"/> (StackExchange.Redis) to
///   access <c>IServer.KeysAsync()</c>. <see cref="IDistributedCache"/> alone
///   cannot do it — hence the dedicated constructor overload.
///
/// Registered in DI by <c>AddAmpCoreServices</c> when a Redis connection string is present.
/// </summary>
public sealed class DistributedCachingService : ICachingService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedCachingService> _logger;

    // Optional — only available when IConnectionMultiplexer is registered in DI.
    // Required for RemoveByPrefixAsync. When null, prefix removal logs a warning and no-ops.
    private readonly IConnectionMultiplexer? _multiplexer;

    public DistributedCachingService(
        IDistributedCache cache,
        ILogger<DistributedCachingService> logger,
        IConnectionMultiplexer? multiplexer = null)
    {
        _cache = cache;
        _logger = logger;
        _multiplexer = multiplexer;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var bytes = await _cache.GetAsync(key, ct);
            if (bytes is null) return null;
            return JsonSerializer.Deserialize<T>(bytes, JsonHelper.DefaultOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache GET failed for key '{Key}'", key);
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
            await _cache.SetAsync(key, bytes, entryOptions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache SET failed for key '{Key}'", key);
        }
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory,
        TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        var cached = await GetAsync<T>(key, ct);
        if (cached is not null) return cached;

        var value = await factory(ct);
        await SetAsync(key, value, ttl, ct);
        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try { await _cache.RemoveAsync(key, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Cache REMOVE failed for key '{Key}'", key); }
    }

    /// <summary>
    /// Removes all Redis keys that begin with <paramref name="prefix"/> using
    /// <c>SCAN</c> + <c>DEL</c> (never <c>KEYS</c> — that blocks the server).
    ///
    /// Requires <see cref="IConnectionMultiplexer"/> to be registered in DI.
    /// If it is not registered, a warning is logged and the method is a no-op.
    ///
    /// Example:
    /// <code>
    ///   // Evict all cached order pages after a bulk update
    ///   await cache.RemoveByPrefixAsync("orders:");
    ///   // Removes: orders:123, orders:list:page1, orders:list:page2, ...
    /// </code>
    /// </summary>
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        if (_multiplexer is null)
        {
            _logger.LogWarning(
                "RemoveByPrefixAsync for prefix '{Prefix}' requires IConnectionMultiplexer. " +
                "Register it via AddAmpCoreServices or services.AddSingleton<IConnectionMultiplexer>(...). " +
                "No keys were removed.", prefix);
            return;
        }

        var pattern = $"{prefix}*";
        var deleted = 0;

        try
        {
            // SCAN across all Redis server nodes (handles cluster and single-node).
            // IServer.KeysAsync uses SCAN internally — it is non-blocking and safe in production.
            foreach (var server in _multiplexer.GetServers())
            {
                // Only target primary nodes to avoid scanning replicas.
                if (server.IsReplica) continue;

                var db = _multiplexer.GetDatabase();
                var keys = new List<RedisKey>();

                await foreach (var key in server.KeysAsync(pattern: pattern).WithCancellation(ct))
                    keys.Add(key);

                if (keys.Count == 0) continue;

                // DEL accepts multiple keys — batch in one round-trip per server.
                await db.KeyDeleteAsync([.. keys]);
                deleted += keys.Count;
            }

            _logger.LogDebug(
                "Removed {Count} cache entries with prefix '{Prefix}'", deleted, prefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Cache REMOVE BY PREFIX failed for prefix '{Prefix}'", prefix);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        var value = await _cache.GetAsync(key, ct);
        return value is not null;
    }

    public async Task RefreshAsync(string key, CancellationToken ct = default)
    {
        try { await _cache.RefreshAsync(key, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Cache REFRESH failed for key '{Key}'", key); }
    }
}
