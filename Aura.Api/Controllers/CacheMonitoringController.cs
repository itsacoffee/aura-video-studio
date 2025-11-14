using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Services.Caching;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for monitoring cache performance and statistics
/// </summary>
[ApiController]
[Route("api/cache/monitoring")]
public class CacheMonitoringController : ControllerBase
{
    private readonly IDistributedCacheService? _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheMonitoringController> _logger;

    public CacheMonitoringController(
        IMemoryCache memoryCache,
        ILogger<CacheMonitoringController> logger,
        IDistributedCacheService? distributedCache = null)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// Get cache statistics including hit rate and performance metrics
    /// </summary>
    [HttpGet("stats")]
    public IActionResult GetCacheStatistics()
    {
        try
        {
            var stats = new Dictionary<string, object>();

            // Distributed cache stats
            if (_distributedCache != null)
            {
                var distributedStats = _distributedCache.GetStatistics();
                stats["distributed"] = new
                {
                    hits = distributedStats.Hits,
                    misses = distributedStats.Misses,
                    errors = distributedStats.Errors,
                    hitRate = Math.Round(distributedStats.HitRate * 100, 2),
                    backendType = distributedStats.BackendType,
                    totalRequests = distributedStats.Hits + distributedStats.Misses
                };
            }

            // Memory cache stats
            if (_memoryCache is MemoryCache memCache)
            {
                var memoryCacheStats = GetMemoryCacheStatistics(memCache);
                stats["memory"] = memoryCacheStats;
            }

            // Overall health assessment
            var overallHitRate = 0.0;
            if (_distributedCache != null)
            {
                var distributedStats = _distributedCache.GetStatistics();
                overallHitRate = distributedStats.HitRate;
            }

            stats["health"] = new
            {
                status = overallHitRate >= 0.80 ? "healthy" : 
                        overallHitRate >= 0.60 ? "warning" : "critical",
                targetHitRate = 0.80,
                currentHitRate = Math.Round(overallHitRate, 3),
                recommendation = overallHitRate < 0.80 
                    ? "Consider increasing cache TTL or implementing cache warming"
                    : "Cache performance is optimal"
            };

            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache statistics");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Cache Statistics",
                Status = 500,
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Get detailed cache performance metrics over time
    /// </summary>
    [HttpGet("metrics")]
    public IActionResult GetCacheMetrics()
    {
        try
        {
            if (_distributedCache == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Cache Not Configured",
                    Status = 404,
                    Detail = "Distributed cache is not configured"
                });
            }

            var stats = _distributedCache.GetStatistics();
            var totalRequests = stats.Hits + stats.Misses;

            var metrics = new
            {
                timestamp = DateTime.UtcNow,
                cache = new
                {
                    hits = stats.Hits,
                    misses = stats.Misses,
                    errors = stats.Errors,
                    totalRequests = totalRequests,
                    hitRate = Math.Round(stats.HitRate * 100, 2),
                    missRate = Math.Round((1 - stats.HitRate) * 100, 2),
                    errorRate = totalRequests > 0 ? Math.Round((double)stats.Errors / totalRequests * 100, 2) : 0
                },
                performance = new
                {
                    status = stats.HitRate >= 0.80 ? "optimal" : 
                            stats.HitRate >= 0.60 ? "acceptable" : "poor",
                    backendType = stats.BackendType,
                    recommendations = GenerateRecommendations(stats)
                }
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving cache metrics");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Retrieving Cache Metrics",
                Status = 500,
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Clear all cache entries (admin operation)
    /// </summary>
    [HttpPost("clear")]
    public async Task<IActionResult> ClearCache()
    {
        try
        {
            _logger.LogWarning("Cache clear requested by {User}", User?.Identity?.Name ?? "Unknown");

            if (_distributedCache != null)
            {
                await _distributedCache.ClearAsync().ConfigureAwait(false);
            }

            if (_memoryCache is MemoryCache memCache)
            {
                memCache.Compact(1.0);
            }

            return Ok(new
            {
                message = "Cache cleared successfully",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Error Clearing Cache",
                Status = 500,
                Detail = ex.Message
            });
        }
    }

    private static object GetMemoryCacheStatistics(MemoryCache memCache)
    {
        // Access memory cache stats using reflection since they're internal
        try
        {
            var stats = memCache.GetCurrentStatistics();
            return new
            {
                currentEntryCount = stats?.CurrentEntryCount ?? 0,
                currentEstimatedSize = stats?.CurrentEstimatedSize ?? 0,
                totalHits = stats?.TotalHits ?? 0,
                totalMisses = stats?.TotalMisses ?? 0,
                hitRate = stats != null && (stats.TotalHits + stats.TotalMisses) > 0
                    ? Math.Round((double)stats.TotalHits / (stats.TotalHits + stats.TotalMisses) * 100, 2)
                    : 0
            };
        }
        catch
        {
            return new { status = "unavailable" };
        }
    }

    private static List<string> GenerateRecommendations(CacheStatistics stats)
    {
        var recommendations = new List<string>();

        if (stats.HitRate < 0.60)
        {
            recommendations.Add("Cache hit rate is below 60%. Consider increasing TTL values.");
            recommendations.Add("Review cache key generation strategy for better reuse.");
        }
        else if (stats.HitRate < 0.80)
        {
            recommendations.Add("Cache hit rate is acceptable but can be improved.");
            recommendations.Add("Consider implementing cache warming for frequently accessed data.");
        }

        if (stats.Errors > 0)
        {
            var errorRate = (double)stats.Errors / (stats.Hits + stats.Misses);
            if (errorRate > 0.05)
            {
                recommendations.Add($"Cache error rate is {errorRate:P2}. Investigate connection issues.");
            }
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Cache performance is optimal. No action needed.");
        }

        return recommendations;
    }
}
