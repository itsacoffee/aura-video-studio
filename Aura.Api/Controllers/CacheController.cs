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
    /// Gets cache statistics
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
            
            var response = new CacheStatisticsResponse
            {
                TotalEntries = stats.TotalEntries,
                TotalHits = stats.TotalHits,
                TotalMisses = stats.TotalMisses,
                HitRate = stats.HitRate,
                TotalSizeBytes = stats.TotalSizeBytes,
                TotalEvictions = stats.TotalEvictions,
                TotalExpirations = stats.TotalExpirations
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
