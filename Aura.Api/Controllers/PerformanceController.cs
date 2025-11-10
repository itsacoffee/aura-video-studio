using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Aura.Core.Services.Caching;
using System.Diagnostics;

namespace Aura.Api.Controllers;

/// <summary>
/// Performance monitoring and metrics endpoint
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PerformanceController : ControllerBase
{
    private readonly ILogger<PerformanceController> _logger;
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCacheService? _distributedCache;

    public PerformanceController(
        ILogger<PerformanceController> logger,
        IMemoryCache memoryCache,
        IDistributedCacheService? distributedCache = null)
    {
        _logger = logger;
        _memoryCache = memoryCache;
        _distributedCache = distributedCache;
    }

    /// <summary>
    /// Get performance metrics
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<PerformanceMetricsResponse> GetMetrics()
    {
        var process = Process.GetCurrentProcess();
        
        var metrics = new PerformanceMetricsResponse
        {
            ProcessMetrics = new ProcessMetrics
            {
                WorkingSetBytes = process.WorkingSet64,
                PrivateMemoryBytes = process.PrivateMemorySize64,
                VirtualMemoryBytes = process.VirtualMemorySize64,
                CpuTimeSeconds = process.TotalProcessorTime.TotalSeconds,
                ThreadCount = process.Threads.Count,
                HandleCount = process.HandleCount
            },
            CacheMetrics = _distributedCache?.GetStatistics(),
            Timestamp = DateTime.UtcNow
        };

        return Ok(metrics);
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("cache/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<CacheStatistics> GetCacheStats()
    {
        if (_distributedCache == null)
        {
            return Ok(new CacheStatistics
            {
                BackendType = "None (Caching Disabled)",
                Hits = 0,
                Misses = 0,
                Errors = 0
            });
        }

        var stats = _distributedCache.GetStatistics();
        return Ok(stats);
    }

    /// <summary>
    /// Clear all cache entries (admin operation)
    /// </summary>
    [HttpPost("cache/clear")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult> ClearCache()
    {
        _logger.LogWarning("Cache clear requested");

        if (_distributedCache != null)
        {
            await _distributedCache.ClearAsync();
        }

        if (_memoryCache is MemoryCache memCache)
        {
            memCache.Compact(1.0);
        }

        return Ok(new { message = "Cache cleared successfully" });
    }

    /// <summary>
    /// Get GC statistics
    /// </summary>
    [HttpGet("gc/stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<GCStats> GetGCStats()
    {
        var stats = new GCStats
        {
            TotalMemoryBytes = GC.GetTotalMemory(forceFullCollection: false),
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            TotalAllocatedBytes = GC.GetTotalAllocatedBytes(precise: false)
        };

        return Ok(stats);
    }

    /// <summary>
    /// Force garbage collection (admin operation, use with caution)
    /// </summary>
    [HttpPost("gc/collect")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult ForceGC([FromQuery] int generation = 2)
    {
        _logger.LogWarning("Manual GC collection requested for generation {Generation}", generation);
        
        GC.Collect(generation, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        
        return Ok(new { message = $"GC collection completed for generation {generation}" });
    }
}

/// <summary>
/// Performance metrics response
/// </summary>
public class PerformanceMetricsResponse
{
    public ProcessMetrics? ProcessMetrics { get; set; }
    public CacheStatistics? CacheMetrics { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Process-level metrics
/// </summary>
public class ProcessMetrics
{
    public long WorkingSetBytes { get; set; }
    public long PrivateMemoryBytes { get; set; }
    public long VirtualMemoryBytes { get; set; }
    public double CpuTimeSeconds { get; set; }
    public int ThreadCount { get; set; }
    public int HandleCount { get; set; }
}

/// <summary>
/// GC statistics
/// </summary>
public class GCStats
{
    public long TotalMemoryBytes { get; set; }
    public int Gen0Collections { get; set; }
    public int Gen1Collections { get; set; }
    public int Gen2Collections { get; set; }
    public long TotalAllocatedBytes { get; set; }
}
