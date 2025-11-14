using Aura.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for preflight readiness checks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PreflightController : ControllerBase
{
    private readonly PreflightService _preflightService;

    public PreflightController(PreflightService preflightService)
    {
        _preflightService = preflightService;
    }

    /// <summary>
    /// Run preflight checks for a specific profile
    /// </summary>
    /// <param name="profile">Profile name (Free-Only, Balanced Mix, Pro-Max). Defaults to Free-Only.</param>
    /// <param name="correlationId">Optional correlation ID for request tracking</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Preflight check report</returns>
    [HttpGet]
    public async Task<IActionResult> GetPreflightReport(
        [FromQuery] string profile = "Free-Only",
        [FromQuery] string? correlationId = null,
        CancellationToken ct = default)
    {
        try
        {
            var corrId = correlationId ?? Guid.NewGuid().ToString();
            Log.Information("Preflight check requested for profile: {Profile}, CorrelationId: {CorrelationId}", profile, corrId);
            
            var report = await _preflightService.RunPreflightAsync(profile, ct).ConfigureAwait(false);
            
            // Add correlation ID to response headers for tracing
            Response.Headers["X-Correlation-Id"] = corrId;
            
            return Ok(report);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error running preflight checks");
            return StatusCode(500, new { error = "Error running preflight checks", details = ex.Message });
        }
    }

    /// <summary>
    /// Get safe defaults profile configuration
    /// </summary>
    /// <returns>Safe defaults profile</returns>
    [HttpGet("safe-defaults")]
    public IActionResult GetSafeDefaults()
    {
        try
        {
            Log.Information("Safe defaults profile requested");
            
            var profile = _preflightService.GetSafeDefaultsProfile();
            
            return Ok(profile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error getting safe defaults");
            return StatusCode(500, new { error = "Error getting safe defaults", details = ex.Message });
        }
    }
}
