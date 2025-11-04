using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing LLM cache
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CacheController : ControllerBase
{
    private readonly ILogger<CacheController> _logger;
    private readonly ILlmCache _cache;
    
    public CacheController(
        ILogger<CacheController> logger,
        ILlmCache cache)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }
    
    /// <summary>
    /// Gets cache statistics including memory usage
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cache statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(CacheStatisticsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CacheStatisticsResponse>> GetStatistics(CancellationToken ct)
    {
        try
        {
            var stats = await _cache.GetStatisticsAsync(ct);
            
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            var workingSetMB = currentProcess.WorkingSet64 / (1024.0 * 1024.0);
            var gcMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
            
            var response = new CacheStatisticsResponse
            {
                TotalEntries = stats.TotalEntries,
                TotalHits = stats.TotalHits,
                TotalMisses = stats.TotalMisses,
                HitRate = stats.HitRate,
                TotalSizeBytes = stats.TotalSizeBytes,
                TotalEvictions = stats.TotalEvictions,
                TotalExpirations = stats.TotalExpirations,
                MemoryUsageMB = workingSetMB,
                GcMemoryMB = gcMemoryMB
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache statistics");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Failed to retrieve cache statistics",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
    
    /// <summary>
    /// Clears all entries from the cache
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPost("clear")]
    [ProducesResponseType(typeof(CacheClearResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CacheClearResponse>> Clear(CancellationToken ct)
    {
        try
        {
            var statsBefore = await _cache.GetStatisticsAsync(ct);
            
            await _cache.ClearAsync(ct);
            
            _logger.LogInformation(
                "Cache cleared: {Entries} entries removed (CorrelationId: {CorrelationId})",
                statsBefore.TotalEntries,
                HttpContext.TraceIdentifier);
            
            var response = new CacheClearResponse
            {
                Success = true,
                Message = $"Cache cleared successfully. Removed {statsBefore.TotalEntries} entries.",
                EntriesRemoved = statsBefore.TotalEntries
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Failed to clear cache",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
    
    /// <summary>
    /// Evicts expired entries from the cache
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPost("evict-expired")]
    [ProducesResponseType(typeof(CacheEvictResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CacheEvictResponse>> EvictExpired(CancellationToken ct)
    {
        try
        {
            var statsBefore = await _cache.GetStatisticsAsync(ct);
            var entriesBefore = statsBefore.TotalEntries;
            
            await _cache.EvictExpiredAsync(ct);
            
            var statsAfter = await _cache.GetStatisticsAsync(ct);
            var entriesRemoved = entriesBefore - statsAfter.TotalEntries;
            
            _logger.LogInformation(
                "Evicted {Count} expired entries (CorrelationId: {CorrelationId})",
                entriesRemoved,
                HttpContext.TraceIdentifier);
            
            var response = new CacheEvictResponse
            {
                Success = true,
                Message = $"Evicted {entriesRemoved} expired entries.",
                EntriesRemoved = entriesRemoved,
                EntriesRemaining = statsAfter.TotalEntries
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to evict expired entries");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Failed to evict expired entries",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
    
    /// <summary>
    /// Removes a specific cache entry by key
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpDelete("{key}")]
    [ProducesResponseType(typeof(CacheRemoveResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CacheRemoveResponse>> RemoveEntry(string key, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid cache key",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = "Cache key cannot be null or empty",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }
            
            var removed = await _cache.RemoveAsync(key, ct);
            
            if (!removed)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Cache entry not found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = $"No cache entry found with key: {key}",
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }
            
            _logger.LogInformation(
                "Cache entry removed: key={Key} (CorrelationId: {CorrelationId})",
                key,
                HttpContext.TraceIdentifier);
            
            var response = new CacheRemoveResponse
            {
                Success = true,
                Message = $"Cache entry with key '{key}' removed successfully.",
                Key = key
            };
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cache entry");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Failed to remove cache entry",
                Status = StatusCodes.Status500InternalServerError,
                Detail = ex.Message,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
    
    /// <summary>
    /// Forces a refresh by clearing the cache (alias for backwards compatibility)
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Success message</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(CacheClearResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CacheClearResponse>> ForceRefresh(CancellationToken ct)
    {
        return await Clear(ct);
    }
}

/// <summary>
/// Response model for cache statistics
/// </summary>
public record CacheStatisticsResponse
{
    public int TotalEntries { get; init; }
    public long TotalHits { get; init; }
    public long TotalMisses { get; init; }
    public double HitRate { get; init; }
    public long TotalSizeBytes { get; init; }
    public long TotalEvictions { get; init; }
    public long TotalExpirations { get; init; }
    public double MemoryUsageMB { get; init; }
    public double GcMemoryMB { get; init; }
}

/// <summary>
/// Response model for cache clear operation
/// </summary>
public record CacheClearResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int EntriesRemoved { get; init; }
}

/// <summary>
/// Response model for cache eviction operation
/// </summary>
public record CacheEvictResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int EntriesRemoved { get; init; }
    public int EntriesRemaining { get; init; }
}

/// <summary>
/// Response model for cache remove operation
/// </summary>
public record CacheRemoveResponse
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
}
