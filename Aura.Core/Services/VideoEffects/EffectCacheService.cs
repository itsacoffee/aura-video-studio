using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.VideoEffects;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.VideoEffects;

/// <summary>
/// Service for caching effect previews and rendered results
/// </summary>
public interface IEffectCacheService
{
    /// <summary>
    /// Get cached effect result if available
    /// </summary>
    Task<string?> GetCachedEffectAsync(string cacheKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cache an effect result
    /// </summary>
    Task CacheEffectAsync(string cacheKey, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate cache key for an effect
    /// </summary>
    string GenerateCacheKey(string inputPath, VideoEffect effect);

    /// <summary>
    /// Generate cache key for multiple effects
    /// </summary>
    string GenerateCacheKey(string inputPath, System.Collections.Generic.List<VideoEffect> effects);

    /// <summary>
    /// Clear all cached effects
    /// </summary>
    Task ClearCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    CacheStatistics GetStatistics();

    /// <summary>
    /// Clean up old cache entries
    /// </summary>
    Task CleanupOldEntriesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public long TotalSizeBytes { get; set; }
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public double HitRate => TotalRequests > 0 ? (double)HitCount / TotalRequests : 0.0;
    public int TotalRequests => HitCount + MissCount;
}

/// <summary>
/// Implementation of effect cache service
/// </summary>
public class EffectCacheService : IEffectCacheService
{
    private readonly ILogger<EffectCacheService> _logger;
    private readonly string _cacheDirectory;
    private readonly ConcurrentDictionary<string, CacheEntry> _cacheIndex;
    private readonly SemaphoreSlim _cacheLock;
    private int _hitCount;
    private int _missCount;

    private class CacheEntry
    {
        public required string FilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessedAt { get; set; }
        public long SizeBytes { get; set; }
        public int AccessCount { get; set; }
    }

    public EffectCacheService(
        ILogger<EffectCacheService> logger,
        string? cacheDirectory = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Aura", "VideoEffects", "Cache");
        
        Directory.CreateDirectory(_cacheDirectory);
        _cacheIndex = new ConcurrentDictionary<string, CacheEntry>();
        _cacheLock = new SemaphoreSlim(1, 1);
        
        // Load existing cache index
        LoadCacheIndex();
    }

    public async Task<string?> GetCachedEffectAsync(string cacheKey, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        if (_cacheIndex.TryGetValue(cacheKey, out var entry))
        {
            if (File.Exists(entry.FilePath))
            {
                entry.LastAccessedAt = DateTime.UtcNow;
                entry.AccessCount++;
                Interlocked.Increment(ref _hitCount);
                
                _logger.LogDebug("Cache hit for key {Key}", cacheKey);
                return entry.FilePath;
            }
            else
            {
                // File was deleted, remove from index
                _cacheIndex.TryRemove(cacheKey, out _);
            }
        }

        Interlocked.Increment(ref _missCount);
        _logger.LogDebug("Cache miss for key {Key}", cacheKey);
        return null;
    }

    public async Task CacheEffectAsync(string cacheKey, string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Source file not found", filePath);
        }

        await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var fileInfo = new FileInfo(filePath);
            var cachedFileName = $"{cacheKey}{Path.GetExtension(filePath)}";
            var cachedFilePath = Path.Combine(_cacheDirectory, cachedFileName);

            // Copy file to cache directory
            await Task.Run(() => File.Copy(filePath, cachedFilePath, overwrite: true), cancellationToken).ConfigureAwait(false);

            var entry = new CacheEntry
            {
                FilePath = cachedFilePath,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                SizeBytes = fileInfo.Length,
                AccessCount = 0
            };

            _cacheIndex.AddOrUpdate(cacheKey, entry, (_, __) => entry);
            
            _logger.LogInformation("Cached effect with key {Key}, size {Size} bytes", cacheKey, fileInfo.Length);
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public string GenerateCacheKey(string inputPath, VideoEffect effect)
    {
        // Create a unique hash based on input file and effect parameters
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(Path.GetFileName(inputPath));
        keyBuilder.Append(File.GetLastWriteTimeUtc(inputPath).Ticks);
        keyBuilder.Append(effect.GetType().Name);
        keyBuilder.Append(effect.StartTime);
        keyBuilder.Append(effect.Duration);
        keyBuilder.Append(effect.Intensity);
        
        // Add all parameters
        foreach (var param in effect.Parameters.OrderBy(p => p.Key))
        {
            keyBuilder.Append(param.Key);
            keyBuilder.Append(param.Value?.ToString() ?? "null");
        }

        return ComputeHash(keyBuilder.ToString());
    }

    public string GenerateCacheKey(string inputPath, System.Collections.Generic.List<VideoEffect> effects)
    {
        var keyBuilder = new StringBuilder();
        keyBuilder.Append(Path.GetFileName(inputPath));
        keyBuilder.Append(File.GetLastWriteTimeUtc(inputPath).Ticks);
        
        foreach (var effect in effects.OrderBy(e => e.Id))
        {
            keyBuilder.Append(effect.GetType().Name);
            keyBuilder.Append(effect.StartTime);
            keyBuilder.Append(effect.Duration);
            keyBuilder.Append(effect.Intensity);
            
            foreach (var param in effect.Parameters.OrderBy(p => p.Key))
            {
                keyBuilder.Append(param.Key);
                keyBuilder.Append(param.Value?.ToString() ?? "null");
            }
        }

        return ComputeHash(keyBuilder.ToString());
    }

    public async Task ClearCacheAsync(CancellationToken cancellationToken = default)
    {
        await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var files = Directory.GetFiles(_cacheDirectory);
            foreach (var file in files)
            {
                try
                {
                    await Task.Run(() => File.Delete(file), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete cache file {File}", file);
                }
            }

            _cacheIndex.Clear();
            _hitCount = 0;
            _missCount = 0;
            
            _logger.LogInformation("Cleared effect cache");
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public CacheStatistics GetStatistics()
    {
        var totalSize = 0L;
        foreach (var entry in _cacheIndex.Values)
        {
            totalSize += entry.SizeBytes;
        }

        return new CacheStatistics
        {
            TotalEntries = _cacheIndex.Count,
            TotalSizeBytes = totalSize,
            HitCount = _hitCount,
            MissCount = _missCount
        };
    }

    public async Task CleanupOldEntriesAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var cutoffTime = DateTime.UtcNow - maxAge;
            var entriesToRemove = _cacheIndex
                .Where(kvp => kvp.Value.LastAccessedAt < cutoffTime)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in entriesToRemove)
            {
                if (_cacheIndex.TryRemove(key, out var entry))
                {
                    try
                    {
                        if (File.Exists(entry.FilePath))
                        {
                            await Task.Run(() => File.Delete(entry.FilePath), cancellationToken).ConfigureAwait(false);
                        }
                        _logger.LogDebug("Removed old cache entry {Key}", key);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old cache file {File}", entry.FilePath);
                    }
                }
            }

            if (entriesToRemove.Count > 0)
            {
                _logger.LogInformation("Cleaned up {Count} old cache entries", entriesToRemove.Count);
            }
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private void LoadCacheIndex()
    {
        if (!Directory.Exists(_cacheDirectory))
        {
            return;
        }

        var files = Directory.GetFiles(_cacheDirectory);
        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                var key = Path.GetFileNameWithoutExtension(file);
                
                var entry = new CacheEntry
                {
                    FilePath = file,
                    CreatedAt = fileInfo.CreationTimeUtc,
                    LastAccessedAt = fileInfo.LastAccessTimeUtc,
                    SizeBytes = fileInfo.Length,
                    AccessCount = 0
                };

                _cacheIndex.TryAdd(key, entry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load cache entry for {File}", file);
            }
        }

        _logger.LogInformation("Loaded {Count} cache entries", _cacheIndex.Count);
    }

    private static string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes)[..16]; // Use first 16 characters
    }
}
