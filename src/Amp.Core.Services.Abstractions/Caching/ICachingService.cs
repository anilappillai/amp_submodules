namespace Amp.Core.Services.Abstractions.Caching;

/// <summary>
/// Uniform caching contract that works over both in-memory and distributed (Redis) backends.
/// Implementations are selected at composition root via DI — callers stay provider-agnostic.
/// </summary>
public interface ICachingService
{
    /// <summary>Returns the cached value for <paramref name="key"/>, or null if not present.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/> with an optional TTL.</summary>
    Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;

    /// <summary>
    /// Returns the cached value if present; otherwise executes <paramref name="factory"/>,
    /// caches the result, and returns it.
    /// </summary>
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? ttl = null, CancellationToken ct = default) where T : class;

    /// <summary>Removes the entry for <paramref name="key"/>.</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>Removes all entries whose keys start with <paramref name="prefix"/>.</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

    /// <summary>True if an entry exists for <paramref name="key"/>.</summary>
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>Refreshes the sliding expiry of a distributed cache entry.</summary>
    Task RefreshAsync(string key, CancellationToken ct = default);
}

/// <summary>Default cache TTL constants used across AMP services.</summary>
public static class CacheTtl
{
    public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Medium = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Long = TimeSpan.FromHours(1);
    public static readonly TimeSpan Day = TimeSpan.FromHours(24);
}
