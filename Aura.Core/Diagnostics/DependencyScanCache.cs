using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Diagnostics;

/// <summary>
/// Service for caching dependency scan results with TTL
/// </summary>
public class DependencyScanCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DependencyScanCache> _logger;
    private const string CacheKey = "dependency-scan-result";
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(5);
    
    public DependencyScanCache(IMemoryCache cache, ILogger<DependencyScanCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }
    
    /// <summary>
    /// Get cached scan result if available and not expired
    /// </summary>
    public DependencyScanResult? GetCached()
    {
        if (_cache.TryGetValue<DependencyScanResult>(CacheKey, out var result))
        {
            _logger.LogDebug("Returning cached dependency scan result from {ScanTime}", result?.ScanTime);
            return result;
        }
        
        return null;
    }
    
    /// <summary>
    /// Store scan result in cache
    /// </summary>
    public void SetCached(DependencyScanResult result, TimeSpan? ttl = null)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl ?? DefaultTtl
        };
        
        _cache.Set(CacheKey, result, cacheOptions);
        _logger.LogDebug("Cached dependency scan result with TTL {Ttl}", cacheOptions.AbsoluteExpirationRelativeToNow);
    }
    
    /// <summary>
    /// Clear cached scan result
    /// </summary>
    public void ClearCache()
    {
        _cache.Remove(CacheKey);
        _logger.LogDebug("Cleared cached dependency scan result");
    }
    
    /// <summary>
    /// Check if cached result exists and is not expired
    /// </summary>
    public bool IsCached()
    {
        return _cache.TryGetValue(CacheKey, out DependencyScanResult? _);
    }
}
