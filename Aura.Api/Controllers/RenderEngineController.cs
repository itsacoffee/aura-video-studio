using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Services.Render;
using Aura.Core.Rendering;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for render engine operations including preset recommendations and preflight checks
/// </summary>
[ApiController]
[Route("api/render-engine")]
public class RenderEngineController : ControllerBase
{
    private readonly ILogger<RenderEngineController> _logger;
    private readonly PresetRecommendationService? _presetRecommendationService;
    private readonly RenderPreflightService? _renderPreflightService;
    private readonly FFmpegCommandLogger? _commandLogger;

    public RenderEngineController(
        ILogger<RenderEngineController> logger,
        PresetRecommendationService? presetRecommendationService = null,
        RenderPreflightService? renderPreflightService = null,
        FFmpegCommandLogger? commandLogger = null)
    {
        _logger = logger;
        _presetRecommendationService = presetRecommendationService;
        _renderPreflightService = renderPreflightService;
        _commandLogger = commandLogger;
    }

    /// <summary>
    /// Get all available render presets grouped by platform
    /// </summary>
    [HttpGet("presets")]
    public IActionResult GetRenderPresets()
    {
        try
        {
            var presetsByPlatform = RenderPresets.GetPresetsByPlatform();
            var allPresets = RenderPresets.GetPresetNames();

            return Ok(new
            {
                byPlatform = presetsByPlatform,
                allNames = allPresets
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting render presets");
            return StatusCode(500, new { error = "Failed to get render presets" });
        }
    }

    /// <summary>
    /// Get a specific render preset by name
    /// </summary>
    [HttpGet("presets/{name}")]
    public IActionResult GetRenderPreset(string name)
    {
        try
        {
            var preset = RenderPresets.GetPresetByName(name);
            
            if (preset == null)
            {
                return NotFound(new { error = $"Preset '{name}' not found" });
            }

            return Ok(preset);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting render preset: {PresetName}", name);
            return StatusCode(500, new { error = "Failed to get render preset" });
        }
    }

    /// <summary>
    /// Recommend the best preset based on project requirements
    /// </summary>
    [HttpPost("presets/recommend")]
    public async Task<IActionResult> RecommendPreset(
        [FromBody] PresetRecommendationRequest request,
        CancellationToken ct)
    {
        try
        {
            if (_presetRecommendationService == null)
            {
                return StatusCode(503, new { error = "Preset recommendation service not available" });
            }

            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "Preset recommendation requested: Platform={Platform}, ContentType={ContentType}, CorrelationId={CorrelationId}",
                request.TargetPlatform, request.ContentType, correlationId);

            var recommendation = await _presetRecommendationService.RecommendPresetAsync(request, ct).ConfigureAwait(false);

            Response.Headers["X-Correlation-Id"] = correlationId;

            return Ok(recommendation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recommending preset");
            return StatusCode(500, new
            {
                error = "Failed to recommend preset",
                correlationId = HttpContext.TraceIdentifier,
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Run preflight validation for a render operation
    /// </summary>
    [HttpPost("preflight")]
    public async Task<IActionResult> RunPreflight(
        [FromBody] RenderPreflightRequest request,
        CancellationToken ct)
    {
        try
        {
            if (_renderPreflightService == null)
            {
                return StatusCode(503, new { error = "Render preflight service not available" });
            }

            var correlationId = HttpContext.TraceIdentifier;
            _logger.LogInformation(
                "Render preflight requested: PresetName={PresetName}, Duration={Duration}, CorrelationId={CorrelationId}",
                request.PresetName, request.VideoDuration, correlationId);

            var preset = ExportPresets.GetPresetByName(request.PresetName);
            if (preset == null)
            {
                return BadRequest(new
                {
                    error = $"Unknown preset: {request.PresetName}",
                    correlationId
                });
            }

            var result = await _renderPreflightService.ValidateRenderAsync(
                preset,
                request.VideoDuration,
                request.OutputDirectory,
                request.EncoderOverride,
                request.PreferHardware,
                request.SourceResolution,
                request.SourceAspectRatio,
                correlationId,
                ct).ConfigureAwait(false);

            Response.Headers["X-Correlation-Id"] = result.CorrelationId;

            if (!result.CanProceed)
            {
                return BadRequest(new
                {
                    canProceed = result.CanProceed,
                    errors = result.Errors,
                    warnings = result.Warnings,
                    recommendedActions = result.RecommendedActions,
                    correlationId = result.CorrelationId
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running preflight validation");
            return StatusCode(500, new
            {
                error = "Failed to run preflight validation",
                correlationId = HttpContext.TraceIdentifier,
                details = ex.Message
            });
        }
    }

    /// <summary>
    /// Get FFmpeg command logs for a specific job
    /// </summary>
    [HttpGet("ffmpeg-logs/{jobId}")]
    public async Task<IActionResult> GetFfmpegLogs(string jobId)
    {
        try
        {
            if (_commandLogger == null)
            {
                return StatusCode(503, new { error = "FFmpeg command logger not available" });
            }

            var logs = await _commandLogger.GetCommandsByJobIdAsync(jobId).ConfigureAwait(false);

            if (logs.Count == 0)
            {
                return NotFound(new { error = $"No FFmpeg logs found for job {jobId}" });
            }

            return Ok(new
            {
                jobId,
                commandCount = logs.Count,
                commands = logs
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting FFmpeg logs for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to get FFmpeg logs" });
        }
    }

    /// <summary>
    /// Generate a support diagnostic report for a job
    /// </summary>
    [HttpGet("ffmpeg-logs/{jobId}/support-report")]
    public async Task<IActionResult> GetSupportReport(string jobId)
    {
        try
        {
            if (_commandLogger == null)
            {
                return StatusCode(503, new { error = "FFmpeg command logger not available" });
            }

            var report = await _commandLogger.GenerateSupportReportAsync(jobId).ConfigureAwait(false);
            
            return Ok(new
            {
                jobId,
                report,
                generatedAt = DateTimeOffset.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating support report for job {JobId}", jobId);
            return StatusCode(500, new { error = "Failed to generate support report" });
        }
    }

    /// <summary>
    /// Get the most recent FFmpeg command execution
    /// </summary>
    [HttpGet("ffmpeg-logs/recent")]
    public async Task<IActionResult> GetRecentFfmpegCommand()
    {
        try
        {
            if (_commandLogger == null)
            {
                return StatusCode(503, new { error = "FFmpeg command logger not available" });
            }

            var command = await _commandLogger.GetMostRecentCommandAsync().ConfigureAwait(false);

            if (command == null)
            {
                return NotFound(new { error = "No FFmpeg commands found" });
            }

            return Ok(command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent FFmpeg command");
            return StatusCode(500, new { error = "Failed to get recent FFmpeg command" });
        }
    }
}

/// <summary>
/// Request for render preflight validation
/// </summary>
public record RenderPreflightRequest
{
    public string PresetName { get; init; } = string.Empty;
    public TimeSpan VideoDuration { get; init; }
    public string OutputDirectory { get; init; } = string.Empty;
    public string? EncoderOverride { get; init; }
    public bool PreferHardware { get; init; } = true;
    public Resolution? SourceResolution { get; init; }
    public AspectRatio? SourceAspectRatio { get; init; }
}
