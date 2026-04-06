using Amp.Core.Services.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Amp.Core.Services.Caching;

/// <summary>
/// In-process memory-backed implementation of <see cref="ICachingService"/>.
/// Suitable for single-instance deployments or development environments.
/// For multi-pod EKS deployments use <see cref="DistributedCachingService"/> (Redis).
/// </summary>
public sealed class InMemoryCachingService(
    IMemoryCache cache,
    ILogger<InMemoryCachingService> logger) : ICachingService
{
    private readonly HashSet<string> _keys = [];
    private readonly Lock _lock = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        cache.TryGetValue(key, out T? value);
        return Task.FromResult(value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        var entryOptions = new MemoryCacheEntryOptions();
        if (ttl.HasValue) entryOptions.AbsoluteExpirationRelativeToNow = ttl;

        cache.Set(key, value, entryOptions);
        lock (_lock) _keys.Add(key);
        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default) where T : class
    {
        if (cache.TryGetValue(key, out T? cached) && cached is not null)
            return cached;

        var value = await factory(ct);
        await SetAsync(key, value, ttl, ct);
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        lock (_lock) _keys.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        List<string> toRemove;
        lock (_lock) toRemove = _keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var key in toRemove)
        {
            cache.Remove(key);
            lock (_lock) _keys.Remove(key);
        }
        logger.LogDebug("Removed {Count} cache entries with prefix '{Prefix}'", toRemove.Count, prefix);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(cache.TryGetValue(key, out _));

    public Task RefreshAsync(string key, CancellationToken ct = default) =>
        Task.CompletedTask; // Memory cache slides automatically
}
