using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Media;

/// <summary>
/// Service for managing proxy media generation and caching
/// </summary>
public interface IProxyMediaService
{
    /// <summary>
    /// Generate proxy media for a source file
    /// </summary>
    Task<ProxyMediaMetadata> GenerateProxyAsync(
        string sourcePath,
        ProxyGenerationOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get proxy metadata for a source file
    /// </summary>
    Task<ProxyMediaMetadata?> GetProxyMetadataAsync(string sourcePath, ProxyQuality quality);
    
    /// <summary>
    /// Check if proxy exists for source file
    /// </summary>
    Task<bool> ProxyExistsAsync(string sourcePath, ProxyQuality quality);
    
    /// <summary>
    /// Get all proxy metadata
    /// </summary>
    Task<IEnumerable<ProxyMediaMetadata>> GetAllProxiesAsync();
    
    /// <summary>
    /// Delete proxy for source file
    /// </summary>
    Task DeleteProxyAsync(string sourcePath, ProxyQuality quality);
    
    /// <summary>
    /// Clear all proxies
    /// </summary>
    Task ClearAllProxiesAsync();
    
    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatistics> GetCacheStatisticsAsync();
    
    /// <summary>
    /// Set maximum cache size in bytes
    /// </summary>
    void SetMaxCacheSizeBytes(long maxSizeBytes);
    
    /// <summary>
    /// Get maximum cache size in bytes
    /// </summary>
    long GetMaxCacheSizeBytes();
    
    /// <summary>
    /// Evict least recently used proxies if cache exceeds size limit
    /// </summary>
    Task EvictLeastRecentlyUsedAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache statistics
/// </summary>
public record CacheStatistics
{
    public int TotalProxies { get; init; }
    public long TotalCacheSizeBytes { get; init; }
    public long TotalSourceSizeBytes { get; init; }
    public double CompressionRatio { get; init; }
    public long MaxCacheSizeBytes { get; init; }
    public double CacheUsagePercent { get; init; }
    public bool IsOverLimit { get; init; }
}

/// <summary>
/// Implementation of proxy media service
/// </summary>
public class ProxyMediaService : IProxyMediaService
{
    private readonly IFFmpegService _ffmpegService;
    private readonly ILogger<ProxyMediaService> _logger;
    private readonly string _cacheDirectory;
    private readonly string _metadataDirectory;
    private readonly ConcurrentDictionary<string, ProxyMediaMetadata> _proxyCache = new();
    private readonly SemaphoreSlim _generateLock = new(1, 1);
    private long _maxCacheSizeBytes = 10L * 1024 * 1024 * 1024; // Default 10GB

    public ProxyMediaService(
        IFFmpegService ffmpegService,
        ILogger<ProxyMediaService> logger,
        string? cacheDirectory = null)
    {
        _ffmpegService = ffmpegService ?? throw new ArgumentNullException(nameof(ffmpegService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _cacheDirectory = cacheDirectory ?? Path.Combine(Path.GetTempPath(), "aura-proxy-cache");
        _metadataDirectory = Path.Combine(_cacheDirectory, "metadata");
        
        Directory.CreateDirectory(_cacheDirectory);
        Directory.CreateDirectory(_metadataDirectory);
        
        LoadProxyMetadataAsync().GetAwaiter().GetResult();
    }

    public async Task<ProxyMediaMetadata> GenerateProxyAsync(
        string sourcePath,
        ProxyGenerationOptions options,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source file not found: {sourcePath}");
        }

        var sourceInfo = new FileInfo(sourcePath);
        var proxyKey = GetProxyKey(sourcePath, options.Quality);
        
        await _generateLock.WaitAsync(cancellationToken);
        try
        {
            var existing = await GetProxyMetadataAsync(sourcePath, options.Quality);
            if (existing != null && existing.Status == ProxyStatus.Completed && !options.Overwrite)
            {
                _logger.LogInformation("Proxy already exists for {SourcePath} at quality {Quality}", 
                    sourcePath, options.Quality);
                return existing;
            }

            var metadata = new ProxyMediaMetadata
            {
                Id = Guid.NewGuid().ToString(),
                SourcePath = sourcePath,
                ProxyPath = GetProxyPath(sourcePath, options.Quality),
                Quality = options.Quality,
                Status = ProxyStatus.Processing,
                CreatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                SourceFileSizeBytes = sourceInfo.Length
            };

            _proxyCache[proxyKey] = metadata;
            await SaveMetadataAsync(metadata);

            _logger.LogInformation("Starting proxy generation for {SourcePath} at quality {Quality}", 
                sourcePath, options.Quality);

            try
            {
                var videoInfo = await _ffmpegService.GetVideoInfoAsync(sourcePath, cancellationToken);
                
                var (width, height, bitrate) = GetProxySettings(options.Quality, videoInfo);
                
                metadata.Width = width;
                metadata.Height = height;
                metadata.BitrateKbps = bitrate;

                var arguments = BuildFFmpegArguments(sourcePath, metadata.ProxyPath, width, height, bitrate);
                
                var result = await _ffmpegService.ExecuteAsync(
                    arguments,
                    ffmpegProgress =>
                    {
                        metadata.ProgressPercent = ffmpegProgress.PercentComplete;
                        progress?.Report(ffmpegProgress.PercentComplete);
                    },
                    cancellationToken);

                if (!result.Success)
                {
                    metadata.Status = ProxyStatus.Failed;
                    metadata.ErrorMessage = result.ErrorMessage ?? "FFmpeg execution failed";
                    _logger.LogError("Proxy generation failed: {Error}", metadata.ErrorMessage);
                }
                else
                {
                    var proxyInfo = new FileInfo(metadata.ProxyPath);
                    metadata.FileSizeBytes = proxyInfo.Exists ? proxyInfo.Length : 0;
                    metadata.Status = ProxyStatus.Completed;
                    metadata.ProgressPercent = 100;
                    
                    _logger.LogInformation("Proxy generation completed. Size: {Size:N0} bytes (compression: {Ratio:P1})", 
                        metadata.FileSizeBytes, 
                        1.0 - ((double)metadata.FileSizeBytes / metadata.SourceFileSizeBytes));
                }
            }
            catch (Exception ex)
            {
                metadata.Status = ProxyStatus.Failed;
                metadata.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error generating proxy for {SourcePath}", sourcePath);
            }

            await SaveMetadataAsync(metadata);
            return metadata;
        }
        finally
        {
            _generateLock.Release();
        }
    }

    public async Task<ProxyMediaMetadata?> GetProxyMetadataAsync(string sourcePath, ProxyQuality quality)
    {
        var key = GetProxyKey(sourcePath, quality);
        if (_proxyCache.TryGetValue(key, out var metadata))
        {
            metadata.LastAccessedAt = DateTime.UtcNow;
            await SaveMetadataAsync(metadata);
            return metadata;
        }
        return null;
    }

    public async Task<bool> ProxyExistsAsync(string sourcePath, ProxyQuality quality)
    {
        var metadata = await GetProxyMetadataAsync(sourcePath, quality);
        return metadata?.Status == ProxyStatus.Completed && File.Exists(metadata.ProxyPath);
    }

    public Task<IEnumerable<ProxyMediaMetadata>> GetAllProxiesAsync()
    {
        return Task.FromResult<IEnumerable<ProxyMediaMetadata>>(_proxyCache.Values.ToList());
    }

    public async Task DeleteProxyAsync(string sourcePath, ProxyQuality quality)
    {
        var key = GetProxyKey(sourcePath, quality);
        if (_proxyCache.TryRemove(key, out var metadata))
        {
            try
            {
                if (File.Exists(metadata.ProxyPath))
                {
                    File.Delete(metadata.ProxyPath);
                }
                
                var metadataPath = GetMetadataPath(metadata.Id);
                if (File.Exists(metadataPath))
                {
                    File.Delete(metadataPath);
                }
                
                _logger.LogInformation("Deleted proxy for {SourcePath} at quality {Quality}", sourcePath, quality);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting proxy for {SourcePath}", sourcePath);
            }
        }
        
        await Task.CompletedTask;
    }

    public async Task ClearAllProxiesAsync()
    {
        _logger.LogInformation("Clearing all proxies from cache");
        
        var keys = _proxyCache.Keys.ToList();
        foreach (var key in keys)
        {
            if (_proxyCache.TryRemove(key, out var metadata))
            {
                try
                {
                    if (File.Exists(metadata.ProxyPath))
                    {
                        File.Delete(metadata.ProxyPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting proxy file {Path}", metadata.ProxyPath);
                }
            }
        }

        try
        {
            if (Directory.Exists(_metadataDirectory))
            {
                Directory.Delete(_metadataDirectory, true);
                Directory.CreateDirectory(_metadataDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing metadata directory");
        }
        
        await Task.CompletedTask;
    }

    public async Task<CacheStatistics> GetCacheStatisticsAsync()
    {
        var proxies = await GetAllProxiesAsync();
        var completed = proxies.Where(p => p.Status == ProxyStatus.Completed).ToList();
        
        var totalCacheSize = completed.Sum(p => p.FileSizeBytes);
        var totalSourceSize = completed.Sum(p => p.SourceFileSizeBytes);
        var compressionRatio = totalSourceSize > 0 ? 1.0 - ((double)totalCacheSize / totalSourceSize) : 0;
        var cacheUsagePercent = _maxCacheSizeBytes > 0 ? ((double)totalCacheSize / _maxCacheSizeBytes) * 100 : 0;
        var isOverLimit = totalCacheSize > _maxCacheSizeBytes;
        
        return new CacheStatistics
        {
            TotalProxies = completed.Count,
            TotalCacheSizeBytes = totalCacheSize,
            TotalSourceSizeBytes = totalSourceSize,
            CompressionRatio = compressionRatio,
            MaxCacheSizeBytes = _maxCacheSizeBytes,
            CacheUsagePercent = cacheUsagePercent,
            IsOverLimit = isOverLimit
        };
    }

    public void SetMaxCacheSizeBytes(long maxSizeBytes)
    {
        if (maxSizeBytes <= 0)
        {
            throw new ArgumentException("Max cache size must be greater than zero", nameof(maxSizeBytes));
        }
        
        _maxCacheSizeBytes = maxSizeBytes;
        _logger.LogInformation("Max cache size set to {Size:N0} bytes ({SizeGB:N2} GB)", 
            maxSizeBytes, maxSizeBytes / (1024.0 * 1024.0 * 1024.0));
    }

    public long GetMaxCacheSizeBytes()
    {
        return _maxCacheSizeBytes;
    }

    public async Task EvictLeastRecentlyUsedAsync(CancellationToken cancellationToken = default)
    {
        var stats = await GetCacheStatisticsAsync();
        
        if (!stats.IsOverLimit)
        {
            _logger.LogDebug("Cache size {Size:N0} bytes is within limit {Limit:N0} bytes. No eviction needed.",
                stats.TotalCacheSizeBytes, _maxCacheSizeBytes);
            return;
        }
        
        _logger.LogInformation("Cache size {Size:N0} bytes exceeds limit {Limit:N0} bytes. Starting LRU eviction.",
            stats.TotalCacheSizeBytes, _maxCacheSizeBytes);
        
        var proxies = await GetAllProxiesAsync();
        var completed = proxies
            .Where(p => p.Status == ProxyStatus.Completed)
            .OrderBy(p => p.LastAccessedAt)
            .ToList();
        
        long currentSize = stats.TotalCacheSizeBytes;
        long targetSize = (long)(_maxCacheSizeBytes * 0.8); // Evict to 80% of limit to avoid frequent evictions
        int evictedCount = 0;
        
        foreach (var proxy in completed)
        {
            if (currentSize <= targetSize)
            {
                break;
            }
            
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            
            try
            {
                await DeleteProxyAsync(proxy.SourcePath, proxy.Quality);
                currentSize -= proxy.FileSizeBytes;
                evictedCount++;
                
                _logger.LogDebug("Evicted proxy {SourcePath} ({Quality}), freed {Size:N0} bytes",
                    proxy.SourcePath, proxy.Quality, proxy.FileSizeBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error evicting proxy {SourcePath}", proxy.SourcePath);
            }
        }
        
        _logger.LogInformation("LRU eviction completed. Evicted {Count} proxies, freed {Size:N0} bytes. " +
            "New cache size: {NewSize:N0} bytes",
            evictedCount, stats.TotalCacheSizeBytes - currentSize, currentSize);
    }

    private async Task LoadProxyMetadataAsync()
    {
        try
        {
            if (!Directory.Exists(_metadataDirectory))
            {
                return;
            }

            var metadataFiles = Directory.GetFiles(_metadataDirectory, "*.json");
            foreach (var file in metadataFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var metadata = JsonSerializer.Deserialize<ProxyMediaMetadata>(json);
                    if (metadata != null)
                    {
                        var key = GetProxyKey(metadata.SourcePath, metadata.Quality);
                        _proxyCache[key] = metadata;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error loading metadata from {File}", file);
                }
            }
            
            _logger.LogInformation("Loaded {Count} proxy metadata entries", _proxyCache.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading proxy metadata");
        }
    }

    private async Task SaveMetadataAsync(ProxyMediaMetadata metadata)
    {
        try
        {
            var path = GetMetadataPath(metadata.Id);
            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            await File.WriteAllTextAsync(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving metadata for {Id}", metadata.Id);
        }
    }

    private string GetProxyKey(string sourcePath, ProxyQuality quality)
    {
        return $"{Path.GetFileName(sourcePath)}_{quality}";
    }

    private string GetProxyPath(string sourcePath, ProxyQuality quality)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = Path.GetExtension(sourcePath);
        return Path.Combine(_cacheDirectory, $"{fileName}_proxy_{quality.ToString().ToLowerInvariant()}{extension}");
    }

    private string GetMetadataPath(string id)
    {
        return Path.Combine(_metadataDirectory, $"{id}.json");
    }

    private static (int width, int height, int bitrate) GetProxySettings(ProxyQuality quality, VideoInfo videoInfo)
    {
        return quality switch
        {
            ProxyQuality.Draft => (854, 480, 1500),
            ProxyQuality.Preview => (1280, 720, 3000),
            ProxyQuality.High => (1920, 1080, 5000),
            _ => (1280, 720, 3000)
        };
    }

    private static string BuildFFmpegArguments(string sourcePath, string outputPath, int width, int height, int bitrate)
    {
        return $"-i \"{sourcePath}\" " +
               $"-vf scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2 " +
               $"-c:v libx264 " +
               $"-preset fast " +
               $"-b:v {bitrate}k " +
               $"-c:a aac " +
               $"-b:a 128k " +
               $"-movflags +faststart " +
               $"-y \"{outputPath}\"";
    }
}
