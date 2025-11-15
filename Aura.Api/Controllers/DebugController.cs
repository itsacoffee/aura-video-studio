using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Services.FFmpeg;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Debug endpoints for explicit, detailed diagnostics
/// </summary>
[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly ILogger<DebugController> _logger;
    private readonly FFmpegDirectCheckService _directCheckService;

    public DebugController(
        ILogger<DebugController> logger,
        FFmpegDirectCheckService directCheckService)
    {
        _logger = logger;
        _directCheckService = directCheckService;
    }

    /// <summary>
    /// Explicit FFmpeg direct check with full diagnostic information
    /// </summary>
    /// <remarks>
    /// Performs a deterministic, non-cached check of all FFmpeg candidate locations:
    /// 1. AURA_FFMPEG_PATH environment variable
    /// 2. Managed install directory
    /// 3. System PATH
    /// 
    /// Returns detailed information about each candidate including:
    /// - Whether the path exists
    /// - Whether execution was attempted
    /// - Exit code and timeout status
    /// - Raw version output
    /// - Parsed version
    /// - Validation result
    /// 
    /// This endpoint is for debugging and providing technical details to users.
    /// </remarks>
    [HttpGet("ffmpeg/direct-check")]
    public async Task<IActionResult> FFmpegDirectCheck(CancellationToken cancellationToken)
    {
        var correlationId = HttpContext.TraceIdentifier;

        _logger.LogInformation("[{CorrelationId}] GET /api/debug/ffmpeg/direct-check", correlationId);

        try
        {
            var result = await _directCheckService.CheckAsync(cancellationToken).ConfigureAwait(false);

            var candidates = result.Candidates.ConvertAll(c => new FFmpegCheckCandidate(
                Label: c.Label,
                Path: c.Path,
                Exists: c.Exists,
                ExecutionAttempted: c.ExecutionAttempted,
                ExitCode: c.ExitCode,
                TimedOut: c.TimedOut,
                RawVersionOutput: c.RawVersionOutput,
                VersionParsed: c.VersionParsed,
                Valid: c.Valid,
                Error: c.Error));

            var overall = new FFmpegDirectCheckOverall(
                Installed: result.Installed,
                Valid: result.Valid,
                Source: result.Source,
                ChosenPath: result.ChosenPath,
                Version: result.Version);

            var response = new FFmpegDirectCheckResponse(
                Candidates: candidates,
                Overall: overall,
                CorrelationId: correlationId);

            return Ok(response);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error performing FFmpeg direct check", correlationId);

            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#E999",
                title = "Direct Check Error",
                status = 500,
                detail = $"Failed to perform direct check: {ex.Message}",
                correlationId
            });
        }
    }
}
