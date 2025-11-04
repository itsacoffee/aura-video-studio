using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.AI.Cache;

/// <summary>
/// Interface for LLM response caching
/// </summary>
public interface ILlmCache
{
    /// <summary>
    /// Gets a cached response by key
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cached entry if found, null otherwise</returns>
    Task<CachedEntry?> GetAsync(string key, CancellationToken ct = default);
    
    /// <summary>
    /// Sets a cache entry
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="response">LLM response to cache</param>
    /// <param name="metadata">Cache metadata</param>
    /// <param name="ct">Cancellation token</param>
    Task SetAsync(string key, string response, CacheMetadata metadata, CancellationToken ct = default);
    
    /// <summary>
    /// Removes a specific cache entry
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if entry was removed, false if not found</returns>
    Task<bool> RemoveAsync(string key, CancellationToken ct = default);
    
    /// <summary>
    /// Clears all cache entries
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task ClearAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Evicts expired entries based on TTL
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Number of entries evicted</returns>
    Task<int> EvictExpiredAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets cache statistics
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cache statistics</returns>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default);
}

/// <summary>
/// Represents a cached LLM response
/// </summary>
public class CachedEntry
{
    public required string Response { get; init; }
    public required CacheMetadata Metadata { get; init; }
    public required DateTime CachedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public int AccessCount { get; set; }
    public DateTime LastAccessedAt { get; set; }
}

/// <summary>
/// Metadata about a cached entry
/// </summary>
public record CacheMetadata
{
    public required string ProviderName { get; init; }
    public required string ModelName { get; init; }
    public required string OperationType { get; init; }
    public int TtlSeconds { get; init; }
    public long ResponseSizeBytes { get; init; }
}

/// <summary>
/// Statistics about cache usage
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; init; }
    public long TotalHits { get; init; }
    public long TotalMisses { get; init; }
    public double HitRate => TotalHits + TotalMisses > 0 
        ? (double)TotalHits / (TotalHits + TotalMisses) 
        : 0.0;
    public long TotalSizeBytes { get; init; }
    public long TotalEvictions { get; init; }
    public long TotalExpirations { get; init; }
}
