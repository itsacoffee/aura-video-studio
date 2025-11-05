using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service for caching visual candidates with stable request IDs
/// </summary>
public class CandidateCacheService
{
    private readonly ILogger<CandidateCacheService> _logger;
    private readonly ConcurrentDictionary<string, CachedCandidateEntry> _cache = new();
    private readonly TimeSpan _defaultExpiration = TimeSpan.FromHours(2);

    public CandidateCacheService(ILogger<CandidateCacheService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate a stable request ID based on visual prompt parameters
    /// </summary>
    public string GenerateRequestId(VisualPrompt prompt, ImageSelectionConfig? config = null)
    {
        var stableParams = new
        {
            prompt.SceneIndex,
            prompt.DetailedDescription,
            prompt.Subject,
            prompt.Framing,
            NarrativeKeywords = string.Join(",", prompt.NarrativeKeywords ?? Array.Empty<string>()),
            prompt.Style,
            prompt.QualityTier,
            config?.MinimumAestheticThreshold,
            config?.CandidatesPerScene,
            config?.PreferGeneratedImages
        };

        var json = JsonSerializer.Serialize(stableParams);
        var bytes = Encoding.UTF8.GetBytes(json);
        var hash = SHA256.HashData(bytes);
        
        return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    /// <summary>
    /// Try to get cached candidates
    /// </summary>
    public Task<CachedCandidateEntry?> GetCachedCandidatesAsync(string requestId, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(requestId, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Cache hit for request {RequestId}", requestId);
                return Task.FromResult<CachedCandidateEntry?>(entry);
            }

            _cache.TryRemove(requestId, out _);
            _logger.LogDebug("Cache expired for request {RequestId}", requestId);
        }

        return Task.FromResult<CachedCandidateEntry?>(null);
    }

    /// <summary>
    /// Cache candidates with expiration
    /// </summary>
    public Task CacheCandidatesAsync(
        string requestId,
        ImageSelectionResult result,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        var entry = new CachedCandidateEntry
        {
            RequestId = requestId,
            Result = result,
            CachedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.Add(expiration ?? _defaultExpiration)
        };

        _cache[requestId] = entry;

        _logger.LogInformation(
            "Cached {CandidateCount} candidates for request {RequestId}, expires at {ExpiresAt}",
            result.Candidates.Count,
            requestId,
            entry.ExpiresAt);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Invalidate cache entry
    /// </summary>
    public Task InvalidateCacheAsync(string requestId, CancellationToken ct = default)
    {
        if (_cache.TryRemove(requestId, out _))
        {
            _logger.LogInformation("Invalidated cache for request {RequestId}", requestId);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Clear all expired entries
    /// </summary>
    public Task ClearExpiredEntriesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.ExpiresAt <= now)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleared {Count} expired cache entries", expiredKeys.Count);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        var now = DateTime.UtcNow;
        var entries = _cache.Values.ToList();

        return new CacheStatistics
        {
            TotalEntries = entries.Count,
            ExpiredEntries = entries.Count(e => e.ExpiresAt <= now),
            OldestEntryAge = entries.Any() ? now - entries.Min(e => e.CachedAt) : TimeSpan.Zero,
            TotalCandidatesCached = entries.Sum(e => e.Result.Candidates.Count)
        };
    }
}

/// <summary>
/// Cached candidate entry with metadata
/// </summary>
public record CachedCandidateEntry
{
    public string RequestId { get; init; } = string.Empty;
    public ImageSelectionResult Result { get; init; } = null!;
    public DateTime CachedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
}

/// <summary>
/// Cache statistics
/// </summary>
public record CacheStatistics
{
    public int TotalEntries { get; init; }
    public int ExpiredEntries { get; init; }
    public TimeSpan OldestEntryAge { get; init; }
    public int TotalCandidatesCached { get; init; }
}
