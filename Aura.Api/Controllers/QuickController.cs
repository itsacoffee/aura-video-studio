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
            Log.Information("Quick Demo requested with topic: {Topic}", request?.Topic ?? "(default)");

            var result = await _quickService.CreateQuickDemoAsync(request?.Topic, ct);

            if (result.Success)
            {
                return Ok(new
                {
                    jobId = result.JobId,
                    status = "queued",
                    message = result.Message
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    error = "Quick demo creation failed",
                    details = result.Message
                });
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating quick demo");
            return StatusCode(500, new
            {
                error = "Error creating quick demo",
                details = ex.Message
            });
        }
    }
}

/// <summary>
/// Request model for quick demo creation
/// </summary>
public record QuickDemoRequest(string? Topic);
