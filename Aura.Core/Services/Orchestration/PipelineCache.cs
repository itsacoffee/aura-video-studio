using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Orchestration;

/// <summary>
/// LRU cache for pipeline service results with automatic expiration
/// </summary>
public class PipelineCache
{
    private readonly ILogger<PipelineCache> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ConcurrentDictionary<string, DateTime> _accessTimes = new();
    private readonly int _maxEntries;
    private readonly TimeSpan _defaultTtl;
    private readonly ReaderWriterLockSlim _lock = new();

    public PipelineCache(ILogger<PipelineCache> logger, int maxEntries = 100, TimeSpan? defaultTtl = null)
    {
        _logger = logger;
        _maxEntries = maxEntries;
        _defaultTtl = defaultTtl ?? TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Generates a cache key from multiple parameters
    /// </summary>
    public string GenerateKey(params object?[] parameters)
    {
        var sb = new StringBuilder();
        foreach (var param in parameters)
        {
            if (param != null)
            {
                sb.Append(param.ToString());
                sb.Append('|');
            }
        }

        var keyString = sb.ToString();
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Tries to get a value from cache
    /// </summary>
    public bool TryGet<T>(string key, out T? value)
    {
        value = default;

        _lock.EnterReadLock();
        try
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (DateTime.UtcNow < entry.ExpiresAt)
                {
                    _accessTimes[key] = DateTime.UtcNow;
                    
                    if (entry.Value is T typedValue)
                    {
                        value = typedValue;
                        _logger.LogDebug("Cache hit for key: {Key}", key.Substring(0, Math.Min(16, key.Length)));
                        return true;
                    }
                }
                else
                {
                    _logger.LogDebug("Cache entry expired for key: {Key}", key.Substring(0, Math.Min(16, key.Length)));
                    _cache.TryRemove(key, out _);
                    _accessTimes.TryRemove(key, out _);
                }
            }

            _logger.LogDebug("Cache miss for key: {Key}", key.Substring(0, Math.Min(16, key.Length)));
            return false;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Sets a value in cache with optional TTL
    /// </summary>
    public void Set(string key, object value, TimeSpan? ttl = null)
    {
        var actualTtl = ttl ?? _defaultTtl;
        var entry = new CacheEntry
        {
            Value = value,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(actualTtl)
        };

        _lock.EnterWriteLock();
        try
        {
            if (_cache.Count >= _maxEntries)
            {
                EvictLru();
            }

            _cache[key] = entry;
            _accessTimes[key] = DateTime.UtcNow;
            
            _logger.LogDebug("Cached value for key: {Key}, TTL: {Ttl}s", 
                key.Substring(0, Math.Min(16, key.Length)), 
                actualTtl.TotalSeconds);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Invalidates cache entries that depend on a changed value
    /// </summary>
    public void Invalidate(params string[] keys)
    {
        _lock.EnterWriteLock();
        try
        {
            foreach (var key in keys)
            {
                if (_cache.TryRemove(key, out _))
                {
                    _accessTimes.TryRemove(key, out _);
                    _logger.LogDebug("Invalidated cache key: {Key}", key.Substring(0, Math.Min(16, key.Length)));
                }
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears all cache entries
    /// </summary>
    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            var count = _cache.Count;
            _cache.Clear();
            _accessTimes.Clear();
            _logger.LogInformation("Cleared {Count} cache entries", count);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public (int Count, int Capacity) GetStats()
    {
        _lock.EnterReadLock();
        try
        {
            return (_cache.Count, _maxEntries);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Evicts the least recently used entry
    /// </summary>
    private void EvictLru()
    {
        var oldestKey = _accessTimes
            .OrderBy(kvp => kvp.Value)
            .FirstOrDefault()
            .Key;

        if (oldestKey != null)
        {
            _cache.TryRemove(oldestKey, out _);
            _accessTimes.TryRemove(oldestKey, out _);
            _logger.LogDebug("Evicted LRU cache entry: {Key}", oldestKey.Substring(0, Math.Min(16, oldestKey.Length)));
        }
    }
}
