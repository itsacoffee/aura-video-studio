using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// Cache for TTS audio files to avoid regenerating identical audio
/// Uses content-based hashing for cache keys
/// </summary>
public class VoiceCache : IDisposable
{
    private readonly ILogger<VoiceCache> _logger;
    private readonly string _cacheDirectory;
    private readonly long _maxCacheSizeBytes;
    private readonly TimeSpan _cacheExpirationTime;
    private readonly ConcurrentDictionary<string, CacheEntry> _cacheIndex;
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);
    private bool _disposed;

    private const long DefaultMaxCacheSizeMb = 500; // 500 MB
    private const int DefaultExpirationDays = 7;

    public VoiceCache(
        ILogger<VoiceCache> logger,
        string? cacheDirectory = null,
        long? maxCacheSizeMb = null,
        int? expirationDays = null)
    {
        _logger = logger;
        _cacheDirectory = cacheDirectory ?? Path.Combine(
            Path.GetTempPath(),
            "AuraVideoStudio",
            "TTS",
            "Cache");
        _maxCacheSizeBytes = (maxCacheSizeMb ?? DefaultMaxCacheSizeMb) * 1024 * 1024;
        _cacheExpirationTime = TimeSpan.FromDays(expirationDays ?? DefaultExpirationDays);
        _cacheIndex = new ConcurrentDictionary<string, CacheEntry>();

        // Ensure cache directory exists
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
            _logger.LogInformation("Created voice cache directory: {Directory}", _cacheDirectory);
        }

        // Load existing cache index
        LoadCacheIndex();
    }

    /// <summary>
    /// Gets cached audio if available
    /// </summary>
    /// <param name="provider">TTS provider name</param>
    /// <param name="voiceName">Voice name</param>
    /// <param name="text">Text content</param>
    /// <param name="rate">Speech rate</param>
    /// <param name="pitch">Speech pitch</param>
    /// <returns>Path to cached audio file, or null if not cached</returns>
    public string? TryGetCached(
        string provider,
        string voiceName,
        string text,
        double rate = 1.0,
        double pitch = 0.0)
    {
        var cacheKey = GenerateCacheKey(provider, voiceName, text, rate, pitch);

        if (_cacheIndex.TryGetValue(cacheKey, out var entry))
        {
            if (File.Exists(entry.FilePath))
            {
                // Update access time
                entry.LastAccessed = DateTime.UtcNow;
                entry.AccessCount++;

                _logger.LogDebug(
                    "Cache hit for {Provider}/{Voice} (key: {Key}, size: {Size} bytes, access count: {Count})",
                    provider, voiceName, cacheKey[..8], entry.SizeBytes, entry.AccessCount);

                return entry.FilePath;
            }
            else
            {
                // File was deleted externally, remove from index
                _cacheIndex.TryRemove(cacheKey, out _);
                _logger.LogWarning("Cached file missing, removed from index: {Path}", entry.FilePath);
            }
        }

        _logger.LogDebug("Cache miss for {Provider}/{Voice} (key: {Key})", provider, voiceName, cacheKey[..8]);
        return null;
    }

    /// <summary>
    /// Stores audio file in cache
    /// </summary>
    /// <param name="provider">TTS provider name</param>
    /// <param name="voiceName">Voice name</param>
    /// <param name="text">Text content</param>
    /// <param name="audioPath">Path to audio file to cache</param>
    /// <param name="rate">Speech rate</param>
    /// <param name="pitch">Speech pitch</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Path to cached file</returns>
    public async Task<string> StoreAsync(
        string provider,
        string voiceName,
        string text,
        string audioPath,
        double rate = 1.0,
        double pitch = 0.0,
        CancellationToken ct = default)
    {
        if (!File.Exists(audioPath))
        {
            throw new FileNotFoundException("Audio file not found", audioPath);
        }

        var cacheKey = GenerateCacheKey(provider, voiceName, text, rate, pitch);
        var fileInfo = new FileInfo(audioPath);
        var extension = Path.GetExtension(audioPath);
        var cachedFilePath = Path.Combine(_cacheDirectory, $"{cacheKey}{extension}");

        // Copy to cache directory
        await Task.Run(() => File.Copy(audioPath, cachedFilePath, overwrite: true), ct);

        var entry = new CacheEntry
        {
            CacheKey = cacheKey,
            FilePath = cachedFilePath,
            Provider = provider,
            VoiceName = voiceName,
            TextHash = ComputeTextHash(text),
            SizeBytes = fileInfo.Length,
            CreatedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            AccessCount = 1,
            Rate = rate,
            Pitch = pitch
        };

        _cacheIndex[cacheKey] = entry;

        _logger.LogInformation(
            "Stored audio in cache: {Provider}/{Voice} (key: {Key}, size: {Size} bytes)",
            provider, voiceName, cacheKey[..8], entry.SizeBytes);

        // Trigger cleanup if cache is getting large
        _ = Task.Run(() => CleanupIfNeededAsync(ct), ct);

        return cachedFilePath;
    }

    /// <summary>
    /// Clears all cached audio files
    /// </summary>
    public async Task ClearAsync()
    {
        await _cleanupLock.WaitAsync();
        try
        {
            _logger.LogInformation("Clearing voice cache directory: {Directory}", _cacheDirectory);

            foreach (var entry in _cacheIndex.Values)
            {
                try
                {
                    if (File.Exists(entry.FilePath))
                    {
                        File.Delete(entry.FilePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete cached file: {Path}", entry.FilePath);
                }
            }

            _cacheIndex.Clear();
            _logger.LogInformation("Voice cache cleared");
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        long totalSize = 0;
        int fileCount = 0;
        int totalAccessCount = 0;

        foreach (var entry in _cacheIndex.Values)
        {
            if (File.Exists(entry.FilePath))
            {
                totalSize += entry.SizeBytes;
                fileCount++;
                totalAccessCount += entry.AccessCount;
            }
        }

        return new CacheStatistics
        {
            TotalFiles = fileCount,
            TotalSizeBytes = totalSize,
            TotalSizeMb = totalSize / (1024.0 * 1024.0),
            MaxSizeMb = _maxCacheSizeBytes / (1024.0 * 1024.0),
            UsagePercent = (_maxCacheSizeBytes > 0) ? (totalSize * 100.0 / _maxCacheSizeBytes) : 0,
            TotalAccessCount = totalAccessCount,
            AverageAccessCount = fileCount > 0 ? totalAccessCount / (double)fileCount : 0,
            ExpirationDays = (int)_cacheExpirationTime.TotalDays,
            CacheDirectory = _cacheDirectory
        };
    }

    /// <summary>
    /// Removes expired and least-recently-used entries when cache exceeds limits
    /// </summary>
    private async Task CleanupIfNeededAsync(CancellationToken ct)
    {
        var stats = GetStatistics();

        // Only cleanup if we're over 80% capacity
        if (stats.UsagePercent < 80)
        {
            return;
        }

        if (!await _cleanupLock.WaitAsync(0, ct))
        {
            // Another cleanup is already running
            return;
        }

        try
        {
            _logger.LogInformation(
                "Starting cache cleanup (usage: {Usage:F1}%, {Count} files, {Size:F1} MB)",
                stats.UsagePercent, stats.TotalFiles, stats.TotalSizeMb);

            var now = DateTime.UtcNow;
            var entriesToRemove = new List<CacheEntry>();

            // Find expired entries
            foreach (var entry in _cacheIndex.Values)
            {
                if ((now - entry.LastAccessed) > _cacheExpirationTime)
                {
                    entriesToRemove.Add(entry);
                }
            }

            // If still over capacity, remove least-recently-used entries
            if (stats.TotalSizeBytes > _maxCacheSizeBytes)
            {
                var remainingEntries = _cacheIndex.Values
                    .Where(e => !entriesToRemove.Contains(e))
                    .OrderBy(e => e.LastAccessed)
                    .ThenBy(e => e.AccessCount)
                    .ToList();

                long sizeToRemove = stats.TotalSizeBytes - (_maxCacheSizeBytes / 2); // Target 50% capacity
                long removedSize = entriesToRemove.Sum(e => e.SizeBytes);

                foreach (var entry in remainingEntries)
                {
                    if (removedSize >= sizeToRemove)
                    {
                        break;
                    }

                    entriesToRemove.Add(entry);
                    removedSize += entry.SizeBytes;
                }
            }

            // Remove entries
            foreach (var entry in entriesToRemove)
            {
                try
                {
                    if (File.Exists(entry.FilePath))
                    {
                        File.Delete(entry.FilePath);
                    }

                    _cacheIndex.TryRemove(entry.CacheKey, out _);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove cached file: {Path}", entry.FilePath);
                }
            }

            _logger.LogInformation(
                "Cache cleanup completed: removed {Count} entries ({Size:F1} MB)",
                entriesToRemove.Count,
                entriesToRemove.Sum(e => e.SizeBytes) / (1024.0 * 1024.0));
        }
        finally
        {
            _cleanupLock.Release();
        }
    }

    /// <summary>
    /// Generates a cache key based on all parameters that affect audio output
    /// </summary>
    private string GenerateCacheKey(
        string provider,
        string voiceName,
        string text,
        double rate,
        double pitch)
    {
        var keyData = $"{provider}|{voiceName}|{text}|{rate:F2}|{pitch:F2}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(keyData));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes hash of text content for metadata
    /// </summary>
    private string ComputeTextHash(string text)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
    }

    /// <summary>
    /// Loads cache index from disk (for persistence across restarts)
    /// </summary>
    private void LoadCacheIndex()
    {
        try
        {
            if (!Directory.Exists(_cacheDirectory))
            {
                return;
            }

            var files = Directory.GetFiles(_cacheDirectory, "*.*", SearchOption.TopDirectoryOnly);
            _logger.LogInformation("Loading cache index from {Count} files", files.Length);

            foreach (var file in files)
            {
                try
                {
                    var fileInfo = new FileInfo(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    // Skip if not a valid hash
                    if (fileName.Length != 64)
                    {
                        continue;
                    }

                    var entry = new CacheEntry
                    {
                        CacheKey = fileName,
                        FilePath = file,
                        Provider = "Unknown",
                        VoiceName = "Unknown",
                        TextHash = string.Empty,
                        SizeBytes = fileInfo.Length,
                        CreatedAt = fileInfo.CreationTimeUtc,
                        LastAccessed = fileInfo.LastAccessTimeUtc,
                        AccessCount = 0,
                        Rate = 1.0,
                        Pitch = 0.0
                    };

                    _cacheIndex[fileName] = entry;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load cache entry for file: {File}", file);
                }
            }

            _logger.LogInformation("Loaded {Count} cache entries", _cacheIndex.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cache index");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cleanupLock.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Cache entry metadata
/// </summary>
internal class CacheEntry
{
    public required string CacheKey { get; init; }
    public required string FilePath { get; init; }
    public required string Provider { get; init; }
    public required string VoiceName { get; init; }
    public required string TextHash { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime LastAccessed { get; set; }
    public int AccessCount { get; set; }
    public required double Rate { get; init; }
    public required double Pitch { get; init; }
}

/// <summary>
/// Cache statistics
/// </summary>
public record CacheStatistics
{
    public required int TotalFiles { get; init; }
    public required long TotalSizeBytes { get; init; }
    public required double TotalSizeMb { get; init; }
    public required double MaxSizeMb { get; init; }
    public required double UsagePercent { get; init; }
    public required int TotalAccessCount { get; init; }
    public required double AverageAccessCount { get; init; }
    public required int ExpirationDays { get; init; }
    public required string CacheDirectory { get; init; }
}
