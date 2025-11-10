using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Assets;

/// <summary>
/// Manages video generation assets including caching, CDN upload, and watermarking
/// </summary>
public class AssetManager
{
    private readonly ILogger<AssetManager> _logger;
    private readonly string _cacheDirectory;
    private readonly TimeSpan _cacheExpiration;
    private readonly Dictionary<string, CachedAsset> _assetCache;

    public AssetManager(ILogger<AssetManager> logger, string? cacheDirectory = null, TimeSpan? cacheExpiration = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
        
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Path.GetTempPath(), 
            "AuraVideoStudio", 
            "AssetCache");
        
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
            _logger.LogInformation("Created asset cache directory: {Directory}", _cacheDirectory);
        }
        
        _assetCache = new Dictionary<string, CachedAsset>();
        
        // Clean expired cache on startup
        Task.Run(() => CleanExpiredCacheAsync());
    }

    /// <summary>
    /// Cache an asset with a unique key
    /// </summary>
    public async Task<string> CacheAssetAsync(string key, Stream assetStream, string extension, CancellationToken ct = default)
    {
        var cacheKey = GenerateCacheKey(key);
        var cachePath = Path.Combine(_cacheDirectory, $"{cacheKey}{extension}");
        
        try
        {
            await using var fileStream = new FileStream(cachePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
            await assetStream.CopyToAsync(fileStream, 81920, ct);
            
            _assetCache[cacheKey] = new CachedAsset
            {
                Key = cacheKey,
                FilePath = cachePath,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(_cacheExpiration),
                Size = fileStream.Length
            };
            
            _logger.LogInformation("Cached asset: {Key} -> {Path} ({Size} bytes)", key, cachePath, fileStream.Length);
            return cachePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache asset: {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Retrieve a cached asset by key
    /// </summary>
    public string? GetCachedAsset(string key)
    {
        var cacheKey = GenerateCacheKey(key);
        
        if (_assetCache.TryGetValue(cacheKey, out var asset))
        {
            if (asset.ExpiresAt > DateTime.UtcNow && File.Exists(asset.FilePath))
            {
                _logger.LogDebug("Cache hit: {Key}", key);
                return asset.FilePath;
            }
            
            // Expired or missing, remove from cache
            _assetCache.Remove(cacheKey);
            if (File.Exists(asset.FilePath))
            {
                TryDeleteFile(asset.FilePath);
            }
        }
        
        _logger.LogDebug("Cache miss: {Key}", key);
        return null;
    }

    /// <summary>
    /// Clean expired cache entries
    /// </summary>
    public Task CleanExpiredCacheAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _assetCache
                .Where(kv => kv.Value.ExpiresAt <= now)
                .Select(kv => kv.Key)
                .ToList();
            
            foreach (var key in expiredKeys)
            {
                if (_assetCache.TryGetValue(key, out var asset))
                {
                    TryDeleteFile(asset.FilePath);
                    _assetCache.Remove(key);
                    _logger.LogDebug("Removed expired cache entry: {Key}", key);
                }
            }
            
            if (expiredKeys.Count > 0)
            {
                _logger.LogInformation("Cleaned {Count} expired cache entries", expiredKeys.Count);
            }
        }, ct);
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStatistics GetCacheStatistics()
    {
        var totalSize = _assetCache.Values.Sum(a => a.Size);
        var entryCount = _assetCache.Count;
        
        return new CacheStatistics
        {
            EntryCount = entryCount,
            TotalSizeBytes = totalSize,
            TotalSizeMB = totalSize / (1024.0 * 1024.0),
            OldestEntry = _assetCache.Values.Any() ? _assetCache.Values.Min(a => a.CreatedAt) : (DateTime?)null,
            NewestEntry = _assetCache.Values.Any() ? _assetCache.Values.Max(a => a.CreatedAt) : (DateTime?)null
        };
    }

    /// <summary>
    /// Clear all cached assets
    /// </summary>
    public void ClearCache()
    {
        _logger.LogInformation("Clearing asset cache");
        
        foreach (var asset in _assetCache.Values)
        {
            TryDeleteFile(asset.FilePath);
        }
        
        _assetCache.Clear();
    }

    private string GenerateCacheKey(string key)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete cached file: {Path}", filePath);
        }
    }
}

/// <summary>
/// Represents a cached asset
/// </summary>
internal class CachedAsset
{
    public required string Key { get; init; }
    public required string FilePath { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public required long Size { get; init; }
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public int EntryCount { get; init; }
    public long TotalSizeBytes { get; init; }
    public double TotalSizeMB { get; init; }
    public DateTime? OldestEntry { get; init; }
    public DateTime? NewestEntry { get; init; }
}
