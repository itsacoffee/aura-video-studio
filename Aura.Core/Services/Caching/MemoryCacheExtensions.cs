using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Caching;

/// <summary>
/// Extension methods for IMemoryCache with common patterns
/// </summary>
public static class MemoryCacheExtensions
{
    /// <summary>
    /// Get or create a cache entry with sliding expiration
    /// </summary>
    public static async Task<T> GetOrCreateAsync<T>(
        this IMemoryCache cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan slidingExpiration,
        ILogger? logger = null)
    {
        if (cache.TryGetValue(key, out T? cached))
        {
            logger?.LogDebug("Cache hit for key: {Key}", key);
            return cached!;
        }

        logger?.LogDebug("Cache miss for key: {Key}, creating...", key);
        var value = await factory().ConfigureAwait(false);
        
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = slidingExpiration,
            Size = 1 // For size-based eviction
        };

        cache.Set(key, value, options);
        return value;
    }

    /// <summary>
    /// Get or create a cache entry with absolute expiration
    /// </summary>
    public static async Task<T> GetOrCreateAbsoluteAsync<T>(
        this IMemoryCache cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan absoluteExpiration,
        ILogger? logger = null)
    {
        if (cache.TryGetValue(key, out T? cached))
        {
            logger?.LogDebug("Cache hit for key: {Key}", key);
            return cached!;
        }

        logger?.LogDebug("Cache miss for key: {Key}, creating...", key);
        var value = await factory().ConfigureAwait(false);
        
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration,
            Size = 1
        };

        cache.Set(key, value, options);
        return value;
    }

    /// <summary>
    /// Remove entries matching a pattern
    /// </summary>
    public static void RemoveByPattern(this IMemoryCache cache, string pattern)
    {
        // Note: IMemoryCache doesn't support pattern-based removal out of the box
        // This is a placeholder for custom implementation if needed
        // Consider using IDistributedCache for pattern-based operations
    }

    /// <summary>
    /// Set with high priority to prevent eviction
    /// </summary>
    public static void SetHighPriority<T>(
        this IMemoryCache cache,
        string key,
        T value,
        TimeSpan expiration)
    {
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
            Priority = CacheItemPriority.High,
            Size = 1
        };

        cache.Set(key, value, options);
    }
}
