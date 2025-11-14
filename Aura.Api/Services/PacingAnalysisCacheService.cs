using System;
using System.Collections.Concurrent;
using Aura.Api.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// In-memory cache service for pacing analysis results with 1-hour TTL.
/// </summary>
public class PacingAnalysisCacheService
{
    private readonly ILogger<PacingAnalysisCacheService> _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    public PacingAnalysisCacheService(ILogger<PacingAnalysisCacheService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Store an analysis result in the cache.
    /// </summary>
    public void Set(string analysisId, PacingAnalysisResponse analysis)
    {
        var entry = new CacheEntry
        {
            Analysis = analysis,
            ExpiresAt = DateTime.UtcNow.Add(DefaultTtl)
        };

        _cache.AddOrUpdate(analysisId, entry, (_, _) => entry);
        _logger.LogDebug("Cached analysis {AnalysisId}, expires at {ExpiresAt}", analysisId, entry.ExpiresAt);
    }

    /// <summary>
    /// Retrieve an analysis result from the cache.
    /// </summary>
    public PacingAnalysisResponse? Get(string analysisId)
    {
        if (_cache.TryGetValue(analysisId, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                _logger.LogDebug("Cache hit for analysis {AnalysisId}", analysisId);
                return entry.Analysis;
            }

            // Expired, remove it
            _cache.TryRemove(analysisId, out _);
            _logger.LogDebug("Cache entry expired for analysis {AnalysisId}", analysisId);
        }

        _logger.LogDebug("Cache miss for analysis {AnalysisId}", analysisId);
        return null;
    }

    /// <summary>
    /// Delete an analysis result from the cache.
    /// </summary>
    public bool Delete(string analysisId)
    {
        var removed = _cache.TryRemove(analysisId, out _);
        if (removed)
        {
            _logger.LogInformation("Deleted analysis {AnalysisId} from cache", analysisId);
        }
        return removed;
    }

    /// <summary>
    /// Clear expired entries from the cache.
    /// </summary>
    public void ClearExpired()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = new System.Collections.Generic.List<string>();

        foreach (var kvp in _cache)
        {
            if (kvp.Value.ExpiresAt <= now)
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleared {Count} expired cache entries", expiredKeys.Count);
        }
    }

    private sealed class CacheEntry
    {
        public PacingAnalysisResponse Analysis { get; init; } = null!;
        public DateTime ExpiresAt { get; init; }
    }
}
