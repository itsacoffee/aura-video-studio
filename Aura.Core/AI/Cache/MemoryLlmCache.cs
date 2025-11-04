using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Core.AI.Cache;

/// <summary>
/// In-memory LLM cache implementation with LRU eviction and TTL support
/// </summary>
public class MemoryLlmCache : ILlmCache
{
    private readonly ILogger<MemoryLlmCache> _logger;
    private readonly LlmCacheOptions _options;
    private readonly ConcurrentDictionary<string, CachedEntry> _cache;
    private readonly object _statsLock = new();
    
    private long _totalHits;
    private long _totalMisses;
    private long _totalEvictions;
    private long _totalExpirations;
    
    public MemoryLlmCache(
        ILogger<MemoryLlmCache> logger,
        IOptions<LlmCacheOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _cache = new ConcurrentDictionary<string, CachedEntry>();
        
        _logger.LogInformation(
            "MemoryLlmCache initialized: Enabled={Enabled}, MaxEntries={MaxEntries}, DefaultTTL={TtlSeconds}s",
            _options.Enabled,
            _options.MaxEntries,
            _options.DefaultTtlSeconds);
    }
    
    public Task<CachedEntry?> GetAsync(string key, CancellationToken ct = default)
    {
        if (!_options.Enabled || IsMemoryThresholdExceeded())
        {
            return Task.FromResult<CachedEntry?>(null);
        }
        
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        }
        
        if (_cache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                _cache.TryRemove(key, out _);
                
                lock (_statsLock)
                {
                    _totalMisses++;
                    _totalExpirations++;
                }
                
                _logger.LogDebug(
                    "Cache MISS (expired) for key {KeyHash} (provider={Provider}, model={Model}, op={Operation})",
                    GetKeyHash(key),
                    entry.Metadata.ProviderName,
                    entry.Metadata.ModelName,
                    entry.Metadata.OperationType);
                
                return Task.FromResult<CachedEntry?>(null);
            }
            
            entry.AccessCount++;
            entry.LastAccessedAt = DateTime.UtcNow;
            
            lock (_statsLock)
            {
                _totalHits++;
            }
            
            _logger.LogDebug(
                "Cache HIT for key {KeyHash} (provider={Provider}, model={Model}, op={Operation}, accessCount={AccessCount})",
                GetKeyHash(key),
                entry.Metadata.ProviderName,
                entry.Metadata.ModelName,
                entry.Metadata.OperationType,
                entry.AccessCount);
            
            return Task.FromResult<CachedEntry?>(entry);
        }
        
        lock (_statsLock)
        {
            _totalMisses++;
        }
        
        _logger.LogDebug("Cache MISS for key {KeyHash}", GetKeyHash(key));
        
        return Task.FromResult<CachedEntry?>(null);
    }
    
    public Task SetAsync(string key, string response, CacheMetadata metadata, CancellationToken ct = default)
    {
        if (!_options.Enabled || IsMemoryThresholdExceeded())
        {
            if (IsMemoryThresholdExceeded())
            {
                _logger.LogWarning(
                    "Cache disabled due to memory threshold exceeded: {Threshold}%",
                    _options.MemoryThresholdPercent);
            }
            return Task.CompletedTask;
        }
        
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        }
        
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new ArgumentException("Response cannot be null or empty", nameof(response));
        }
        
        if (_cache.Count >= _options.MaxEntries)
        {
            EvictLruEntry();
        }
        
        var now = DateTime.UtcNow;
        var ttlSeconds = metadata.TtlSeconds > 0 ? metadata.TtlSeconds : _options.DefaultTtlSeconds;
        
        var entry = new CachedEntry
        {
            Response = response,
            Metadata = metadata with 
            { 
                ResponseSizeBytes = Encoding.UTF8.GetByteCount(response),
                TtlSeconds = ttlSeconds
            },
            CachedAt = now,
            ExpiresAt = now.AddSeconds(ttlSeconds),
            AccessCount = 0,
            LastAccessedAt = now
        };
        
        _cache[key] = entry;
        
        _logger.LogInformation(
            "Cached response for key {KeyHash} (provider={Provider}, model={Model}, op={Operation}, size={Size}B, ttl={Ttl}s)",
            GetKeyHash(key),
            metadata.ProviderName,
            metadata.ModelName,
            metadata.OperationType,
            entry.Metadata.ResponseSizeBytes,
            ttlSeconds);
        
        return Task.CompletedTask;
    }
    
    public Task<bool> RemoveAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        }
        
        var removed = _cache.TryRemove(key, out var entry);
        
        if (removed && entry != null)
        {
            _logger.LogInformation(
                "Removed cache entry for key {KeyHash} (provider={Provider}, model={Model})",
                GetKeyHash(key),
                entry.Metadata.ProviderName,
                entry.Metadata.ModelName);
        }
        
        return Task.FromResult(removed);
    }
    
    public Task ClearAsync(CancellationToken ct = default)
    {
        var count = _cache.Count;
        _cache.Clear();
        
        _logger.LogInformation("Cleared {Count} entries from cache", count);
        
        return Task.CompletedTask;
    }
    
    public Task<int> EvictExpiredAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => now > kvp.Value.ExpiresAt)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expiredKeys)
        {
            if (_cache.TryRemove(key, out _))
            {
                lock (_statsLock)
                {
                    _totalExpirations++;
                }
            }
        }
        
        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Evicted {Count} expired entries from cache", expiredKeys.Count);
        }
        
        return Task.FromResult(expiredKeys.Count);
    }
    
    public Task<CacheStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        long totalSize;
        
        lock (_statsLock)
        {
            totalSize = _cache.Values.Sum(e => e.Metadata.ResponseSizeBytes);
        }
        
        var stats = new CacheStatistics
        {
            TotalEntries = _cache.Count,
            TotalHits = _totalHits,
            TotalMisses = _totalMisses,
            TotalSizeBytes = totalSize,
            TotalEvictions = _totalEvictions,
            TotalExpirations = _totalExpirations
        };
        
        return Task.FromResult(stats);
    }
    
    private void EvictLruEntry()
    {
        var lruEntry = _cache
            .OrderBy(kvp => kvp.Value.LastAccessedAt)
            .FirstOrDefault();
        
        if (lruEntry.Key != null && _cache.TryRemove(lruEntry.Key, out var entry))
        {
            lock (_statsLock)
            {
                _totalEvictions++;
            }
            
            _logger.LogDebug(
                "Evicted LRU entry for key {KeyHash} (lastAccessed={LastAccessed})",
                GetKeyHash(lruEntry.Key),
                entry.LastAccessedAt);
        }
    }
    
    private bool IsMemoryThresholdExceeded()
    {
        if (_options.MemoryThresholdPercent <= 0 || _options.MemoryThresholdPercent >= 100)
        {
            return false;
        }
        
        try
        {
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemory = gcInfo.TotalAvailableMemoryBytes;
            var usedMemory = GC.GetTotalMemory(false);
            
            if (totalMemory > 0)
            {
                var usagePercent = (usedMemory * 100.0) / totalMemory;
                return usagePercent >= _options.MemoryThresholdPercent;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check memory threshold");
            return false;
        }
    }
    
    private static string GetKeyHash(string key)
    {
        return key.Length > 16 ? key.Substring(0, 16) + "..." : key;
    }
}
