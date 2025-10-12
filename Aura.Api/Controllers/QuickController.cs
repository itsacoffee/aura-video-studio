using Aura.Core.Orchestrator;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for one-click safe video generation.
/// Provides guaranteed-success path for new users without configuration.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class QuickController : ControllerBase
{
    private readonly QuickService _quickService;

    public QuickController(QuickService quickService)
    {
        _quickService = quickService;
    }

    /// <summary>
    /// Create a quick demo video with safe defaults.
    /// Forces Free-only providers: RuleBased LLM + Windows TTS + Stock visuals.
    /// Locks render to 1080p30 H.264 for maximum compatibility.
    /// Generates a 10-15 second video with captions.
    /// </summary>
    /// <param name="request">Optional topic override</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Job ID and status</returns>
    [HttpPost("demo")]
    public async Task<IActionResult> CreateQuickDemo(
        [FromBody] QuickDemoRequest? request = null,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Quick Demo requested with topic: {Topic}", correlationId, request?.Topic ?? "(default)");

            var result = await _quickService.CreateQuickDemoAsync(request?.Topic, ct);

            if (result.Success)
            {
                Log.Information("[{CorrelationId}] Quick Demo job created successfully: {JobId}", correlationId, result.JobId);
                return Ok(new
                {
                    jobId = result.JobId,
                    status = "queued",
                    message = result.Message,
                    correlationId
                });
            }
            else
            {
                Log.Warning("[{CorrelationId}] Quick Demo creation failed: {Message}", correlationId, result.Message);
                return StatusCode(500, new
                {
                    type = "https://docs.aura.studio/errors/E200",
                    title = "Quick Demo Failed",
                    status = 500,
                    detail = result.Message,
                    correlationId,
                    guidance = "Quick Demo uses safe defaults and should always work. This error indicates a system issue. Please check logs or contact support."
                });
            }
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error creating quick demo", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/E200",
                title = "Quick Demo Error",
                status = 500,
                detail = $"Unexpected error creating quick demo: {ex.Message}",
                correlationId,
                guidance = "Quick Demo should never fail. This indicates a critical system issue. Please check logs and verify all dependencies are installed."
            });
        }
    }
}

/// <summary>
/// Request model for quick demo creation
/// </summary>
public record QuickDemoRequest(string? Topic);
