using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Models;
using Aura.Core.Services.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for proxy media generation and management
/// </summary>
[ApiController]
[Route("api/proxy")]
public class ProxyMediaController : ControllerBase
{
    private readonly IProxyMediaService _proxyMediaService;
    private readonly ILogger<ProxyMediaController> _logger;

    public ProxyMediaController(
        IProxyMediaService proxyMediaService,
        ILogger<ProxyMediaController> logger)
    {
        _proxyMediaService = proxyMediaService ?? throw new ArgumentNullException(nameof(proxyMediaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generate proxy media for a source file
    /// </summary>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(ProxyMediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateProxy(
        [FromBody] GenerateProxyRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating proxy for {SourcePath} at quality {Quality}",
            request.SourcePath, request.Quality);

        try
        {
            if (!Enum.TryParse<ProxyQuality>(request.Quality, true, out var quality))
            {
                return BadRequest($"Invalid quality: {request.Quality}. Valid values: Draft, Preview, High");
            }

            var options = new ProxyGenerationOptions
            {
                Quality = quality,
                BackgroundGeneration = request.BackgroundGeneration,
                Priority = request.Priority,
                Overwrite = request.Overwrite
            };

            var metadata = await _proxyMediaService.GenerateProxyAsync(
                request.SourcePath,
                options,
                null,
                cancellationToken);

            var response = MapToResponse(metadata);
            return Ok(response);
        }
        catch (System.IO.FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Source file not found: {SourcePath}", request.SourcePath);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating proxy for {SourcePath}", request.SourcePath);
            return StatusCode(500, new { error = "Failed to generate proxy", details = ex.Message });
        }
    }

    /// <summary>
    /// Get proxy metadata for a source file
    /// </summary>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(ProxyMediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProxyMetadata(
        [FromQuery] string sourcePath,
        [FromQuery] string quality = "Preview")
    {
        try
        {
            if (!Enum.TryParse<ProxyQuality>(quality, true, out var proxyQuality))
            {
                return BadRequest($"Invalid quality: {quality}");
            }

            var metadata = await _proxyMediaService.GetProxyMetadataAsync(sourcePath, proxyQuality);
            if (metadata == null)
            {
                return NotFound(new { error = "Proxy not found" });
            }

            var response = MapToResponse(metadata);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting proxy metadata for {SourcePath}", sourcePath);
            return StatusCode(500, new { error = "Failed to get proxy metadata" });
        }
    }

    /// <summary>
    /// Check if proxy exists for source file
    /// </summary>
    [HttpGet("exists")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> ProxyExists(
        [FromQuery] string sourcePath,
        [FromQuery] string quality = "Preview")
    {
        try
        {
            if (!Enum.TryParse<ProxyQuality>(quality, true, out var proxyQuality))
            {
                return BadRequest($"Invalid quality: {quality}");
            }

            var exists = await _proxyMediaService.ProxyExistsAsync(sourcePath, proxyQuality);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking proxy existence for {SourcePath}", sourcePath);
            return StatusCode(500, new { error = "Failed to check proxy existence" });
        }
    }

    /// <summary>
    /// Get all proxy media
    /// </summary>
    [HttpGet("all")]
    [ProducesResponseType(typeof(ProxyMediaResponse[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllProxies()
    {
        try
        {
            var proxies = await _proxyMediaService.GetAllProxiesAsync();
            var responses = proxies.Select(MapToResponse).ToArray();
            return Ok(responses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all proxies");
            return StatusCode(500, new { error = "Failed to get proxies" });
        }
    }

    /// <summary>
    /// Delete proxy for source file
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteProxy(
        [FromQuery] string sourcePath,
        [FromQuery] string quality = "Preview")
    {
        try
        {
            if (!Enum.TryParse<ProxyQuality>(quality, true, out var proxyQuality))
            {
                return BadRequest($"Invalid quality: {quality}");
            }

            await _proxyMediaService.DeleteProxyAsync(sourcePath, proxyQuality);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting proxy for {SourcePath}", sourcePath);
            return StatusCode(500, new { error = "Failed to delete proxy" });
        }
    }

    /// <summary>
    /// Clear all proxies from cache
    /// </summary>
    [HttpPost("clear")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ClearAllProxies()
    {
        try
        {
            _logger.LogInformation("Clearing all proxy cache");
            await _proxyMediaService.ClearAllProxiesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing proxy cache");
            return StatusCode(500, new { error = "Failed to clear cache" });
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ProxyCacheStatsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCacheStats()
    {
        try
        {
            var stats = await _proxyMediaService.GetCacheStatisticsAsync();
            var response = new ProxyCacheStatsResponse(
                stats.TotalProxies,
                stats.TotalCacheSizeBytes,
                stats.TotalSourceSizeBytes,
                stats.CompressionRatio,
                stats.MaxCacheSizeBytes,
                stats.CacheUsagePercent,
                stats.IsOverLimit);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache statistics");
            return StatusCode(500, new { error = "Failed to get statistics" });
        }
    }

    /// <summary>
    /// Set maximum cache size
    /// </summary>
    [HttpPost("cache-limit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult SetCacheLimit([FromBody] SetCacheLimitRequest request)
    {
        try
        {
            if (request.MaxSizeBytes <= 0)
            {
                return BadRequest(new { error = "Max size must be greater than zero" });
            }

            _proxyMediaService.SetMaxCacheSizeBytes(request.MaxSizeBytes);
            return Ok(new { maxSizeBytes = request.MaxSizeBytes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache limit");
            return StatusCode(500, new { error = "Failed to set cache limit" });
        }
    }

    /// <summary>
    /// Get maximum cache size
    /// </summary>
    [HttpGet("cache-limit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCacheLimit()
    {
        try
        {
            var maxSize = _proxyMediaService.GetMaxCacheSizeBytes();
            return Ok(new { maxSizeBytes = maxSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache limit");
            return StatusCode(500, new { error = "Failed to get cache limit" });
        }
    }

    /// <summary>
    /// Manually trigger LRU eviction
    /// </summary>
    [HttpPost("evict")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> TriggerEviction(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manual cache eviction triggered");
            await _proxyMediaService.EvictLeastRecentlyUsedAsync(cancellationToken);
            var stats = await _proxyMediaService.GetCacheStatisticsAsync();
            return Ok(new { message = "Eviction completed", stats });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual eviction");
            return StatusCode(500, new { error = "Failed to evict cache" });
        }
    }

    private static ProxyMediaResponse MapToResponse(ProxyMediaMetadata metadata)
    {
        return new ProxyMediaResponse(
            metadata.Id,
            metadata.SourcePath,
            metadata.ProxyPath,
            metadata.Quality.ToString(),
            metadata.Status.ToString(),
            metadata.CreatedAt,
            metadata.LastAccessedAt,
            metadata.FileSizeBytes,
            metadata.SourceFileSizeBytes,
            metadata.Width,
            metadata.Height,
            metadata.BitrateKbps,
            metadata.ErrorMessage,
            metadata.ProgressPercent);
    }
}
