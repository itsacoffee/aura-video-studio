using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for system dependency scanning and validation
/// </summary>
[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly ILogger<SystemController> _logger;
    private readonly DependencyScanner _scanner;
    private readonly DependencyScanCache _scanCache;
    
    public SystemController(
        ILogger<SystemController> logger,
        DependencyScanner scanner,
        DependencyScanCache scanCache)
    {
        _logger = logger;
        _scanner = scanner;
        _scanCache = scanCache;
    }
    
    /// <summary>
    /// Scan system dependencies and return immediate results
    /// </summary>
    /// <param name="forceRefresh">Force a new scan even if cached result exists</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Dependency scan result</returns>
    [HttpPost("scan")]
    public async Task<IActionResult> ScanDependencies(
        [FromQuery] bool forceRefresh = false,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        try
        {
            _logger.LogInformation("Dependency scan requested, ForceRefresh: {ForceRefresh}, CorrelationId: {CorrelationId}",
                forceRefresh, correlationId);
            
            // Check cache unless force refresh
            if (!forceRefresh)
            {
                var cached = _scanCache.GetCached();
                if (cached != null)
                {
                    _logger.LogInformation("Returning cached scan result from {ScanTime}", cached.ScanTime);
                    Response.Headers["X-Correlation-Id"] = correlationId;
                    Response.Headers["X-Cache-Hit"] = "true";
                    return Ok(cached);
                }
            }
            
            // Perform new scan
            var result = await _scanner.ScanAsync(null, ct);
            result.CorrelationId = correlationId;
            
            // Cache the result
            _scanCache.SetCached(result);
            
            Response.Headers["X-Correlation-Id"] = correlationId;
            Response.Headers["X-Cache-Hit"] = "false";
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during dependency scan, CorrelationId: {CorrelationId}", correlationId);
            
            return StatusCode(500, new ProblemDetails
            {
                Title = "Dependency Scan Failed",
                Detail = "An error occurred while scanning system dependencies",
                Status = 500,
                Instance = HttpContext.Request.Path,
                Extensions =
                {
                    ["correlationId"] = correlationId,
                    ["timestamp"] = DateTime.UtcNow,
                    ["errorMessage"] = ex.Message,
                    ["errorType"] = ex.GetType().Name
                }
            });
        }
    }
    
    /// <summary>
    /// Stream dependency scan progress via Server-Sent Events
    /// </summary>
    /// <param name="forceRefresh">Force a new scan even if cached result exists</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>SSE stream</returns>
    [HttpGet("scan/stream")]
    public async Task ScanDependenciesStream(
        [FromQuery] bool forceRefresh = false,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.TraceIdentifier;
        
        _logger.LogInformation("SSE dependency scan requested, ForceRefresh: {ForceRefresh}, CorrelationId: {CorrelationId}",
            forceRefresh, correlationId);
        
        // Set up SSE response
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["X-Accel-Buffering"] = "no";
        Response.Headers["X-Correlation-Id"] = correlationId;
        
        try
        {
            // Check cache unless force refresh
            if (!forceRefresh)
            {
                var cached = _scanCache.GetCached();
                if (cached != null)
                {
                    _logger.LogInformation("Streaming cached scan result from {ScanTime}", cached.ScanTime);
                    
                    // Send cached result as completed event
                    await SendSseEventAsync("started", new { message = "Using cached scan result" }, ct);
                    
                    foreach (var issue in cached.Issues)
                    {
                        await SendSseEventAsync("issue", issue, ct);
                    }
                    
                    await SendSseEventAsync("completed", new
                    {
                        scanTime = cached.ScanTime,
                        duration = cached.Duration,
                        issueCount = cached.Issues.Count,
                        hasErrors = cached.HasErrors,
                        hasWarnings = cached.HasWarnings,
                        cached = true
                    }, ct);
                    
                    return;
                }
            }
            
            // Perform new scan with progress reporting
            var progress = new Progress<ScanProgress>(async p =>
            {
                try
                {
                    await SendSseEventAsync(p.Event, new
                    {
                        message = p.Message,
                        percentComplete = p.PercentComplete,
                        issue = p.Issue
                    }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error sending SSE event");
                }
            });
            
            var result = await _scanner.ScanAsync(progress, ct);
            result.CorrelationId = correlationId;
            
            // Cache the result
            _scanCache.SetCached(result);
            
            // Send final completion event
            await SendSseEventAsync("completed", new
            {
                scanTime = result.ScanTime,
                duration = result.Duration,
                issueCount = result.Issues.Count,
                hasErrors = result.HasErrors,
                hasWarnings = result.HasWarnings,
                cached = false
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SSE dependency scan, CorrelationId: {CorrelationId}", correlationId);
            
            await SendSseEventAsync("error", new
            {
                message = "An error occurred during the dependency scan",
                error = ex.Message,
                correlationId
            }, ct);
        }
    }
    
    /// <summary>
    /// Get cached scan result if available
    /// </summary>
    /// <returns>Cached scan result or 404 if not cached</returns>
    [HttpGet("scan/cached")]
    public IActionResult GetCachedScan()
    {
        var cached = _scanCache.GetCached();
        
        if (cached == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "No Cached Scan",
                Detail = "No cached dependency scan result is available",
                Status = 404,
                Instance = HttpContext.Request.Path
            });
        }
        
        Response.Headers["X-Cache-Hit"] = "true";
        return Ok(cached);
    }
    
    /// <summary>
    /// Clear cached scan result
    /// </summary>
    /// <returns>Success response</returns>
    [HttpDelete("scan/cache")]
    public IActionResult ClearScanCache()
    {
        _scanCache.ClearCache();
        _logger.LogInformation("Scan cache cleared");
        
        return Ok(new { success = true, message = "Scan cache cleared" });
    }
    
    private async Task SendSseEventAsync(string eventType, object data, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await Response.WriteAsync($"event: {eventType}\n", ct);
        await Response.WriteAsync($"data: {json}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}
