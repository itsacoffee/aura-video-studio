using Aura.Api.Services.QualityValidation;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for video quality validation endpoints
/// </summary>
[ApiController]
[Route("api/quality")]
public class QualityValidationController : ControllerBase
{
    private readonly ResolutionValidationService _resolutionService;
    private readonly AudioQualityService _audioService;
    private readonly FrameRateService _frameRateService;
    private readonly ConsistencyAnalysisService _consistencyService;
    private readonly PlatformRequirementsService _platformService;

    public QualityValidationController(
        ResolutionValidationService resolutionService,
        AudioQualityService audioService,
        FrameRateService frameRateService,
        ConsistencyAnalysisService consistencyService,
        PlatformRequirementsService platformService)
    {
        _resolutionService = resolutionService;
        _audioService = audioService;
        _frameRateService = frameRateService;
        _consistencyService = consistencyService;
        _platformService = platformService;
    }

    /// <summary>
    /// Validates video resolution against minimum requirements
    /// </summary>
    /// <param name="width">Video width in pixels</param>
    /// <param name="height">Video height in pixels</param>
    /// <param name="min_resolution">Minimum resolution specification (e.g., "1280x720")</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Resolution validation result</returns>
    [HttpGet("validate/resolution")]
    public async Task<IActionResult> ValidateResolution(
        [FromQuery] int width,
        [FromQuery] int height,
        [FromQuery] string? min_resolution = "1280x720",
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/quality/validate/resolution - width={Width}, height={Height}, min={MinRes}",
                correlationId, width, height, min_resolution);

            if (width <= 0 || height <= 0)
            {
                return BadRequest(new { success = false, error = "Width and height must be positive integers" });
            }

            // Parse minimum resolution
            var minParts = min_resolution?.Split('x') ?? new[] { "1280", "720" };
            if (minParts.Length != 2 || !int.TryParse(minParts[0], out var minWidth) || !int.TryParse(minParts[1], out var minHeight))
            {
                return BadRequest(new { success = false, error = "Invalid min_resolution format. Use format: WIDTHxHEIGHT (e.g., 1280x720)" });
            }

            var result = await _resolutionService.ValidateResolutionAsync(width, height, minWidth, minHeight, ct).ConfigureAwait(false);

            Log.Information("[{CorrelationId}] Resolution validation complete: Valid={IsValid}, Score={Score}",
                correlationId, result.IsValid, result.Score);

            return Ok(new { success = true, result });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating resolution");
            return StatusCode(500, new { success = false, error = "Error validating resolution", details = ex.Message });
        }
    }

    /// <summary>
    /// Analyzes audio file for quality issues (loudness, clarity, noise)
    /// </summary>
    /// <param name="request">Audio analysis request containing file path</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Audio quality analysis result</returns>
    [HttpPost("validate/audio")]
    public async Task<IActionResult> ValidateAudio(
        [FromBody] AudioValidationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/quality/validate/audio - file={FilePath}",
                correlationId, request.AudioFilePath);

            if (string.IsNullOrWhiteSpace(request.AudioFilePath))
            {
                return BadRequest(new { success = false, error = "AudioFilePath is required" });
            }

            var result = await _audioService.AnalyzeAudioAsync(request.AudioFilePath, ct).ConfigureAwait(false);

            Log.Information("[{CorrelationId}] Audio analysis complete: Valid={IsValid}, Score={Score}",
                correlationId, result.IsValid, result.Score);

            return Ok(new { success = true, result });
        }
        catch (FileNotFoundException ex)
        {
            Log.Warning(ex, "Audio file not found");
            return NotFound(new { success = false, error = "Audio file not found", details = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error analyzing audio");
            return StatusCode(500, new { success = false, error = "Error analyzing audio", details = ex.Message });
        }
    }

    /// <summary>
    /// Verifies frame rate consistency
    /// </summary>
    /// <param name="expected_fps">Expected frame rate</param>
    /// <param name="actual_fps">Actual detected frame rate</param>
    /// <param name="tolerance">Acceptable variance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Frame rate validation result</returns>
    [HttpGet("validate/framerate")]
    public async Task<IActionResult> ValidateFrameRate(
        [FromQuery] double expected_fps,
        [FromQuery] double actual_fps,
        [FromQuery] double tolerance = 0.5,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/quality/validate/framerate - expected={Expected}, actual={Actual}, tolerance={Tolerance}",
                correlationId, expected_fps, actual_fps, tolerance);

            if (expected_fps <= 0 || actual_fps <= 0)
            {
                return BadRequest(new { success = false, error = "Frame rates must be positive numbers" });
            }

            if (tolerance < 0)
            {
                return BadRequest(new { success = false, error = "Tolerance must be non-negative" });
            }

            var result = await _frameRateService.ValidateFrameRateAsync(actual_fps, expected_fps, tolerance, ct).ConfigureAwait(false);

            Log.Information("[{CorrelationId}] Frame rate validation complete: Valid={IsValid}, Score={Score}",
                correlationId, result.IsValid, result.Score);

            return Ok(new { success = true, result });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating frame rate");
            return StatusCode(500, new { success = false, error = "Error validating frame rate", details = ex.Message });
        }
    }

    /// <summary>
    /// Checks for content consistency across frames
    /// </summary>
    /// <param name="request">Consistency analysis request containing video file path</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Content consistency analysis result</returns>
    [HttpPost("validate/consistency")]
    public async Task<IActionResult> ValidateConsistency(
        [FromBody] ConsistencyValidationRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] POST /api/quality/validate/consistency - file={FilePath}",
                correlationId, request.VideoFilePath);

            if (string.IsNullOrWhiteSpace(request.VideoFilePath))
            {
                return BadRequest(new { success = false, error = "VideoFilePath is required" });
            }

            var result = await _consistencyService.AnalyzeConsistencyAsync(request.VideoFilePath, ct).ConfigureAwait(false);

            Log.Information("[{CorrelationId}] Consistency analysis complete: Valid={IsValid}, Score={Score}",
                correlationId, result.IsValid, result.Score);

            return Ok(new { success = true, result });
        }
        catch (FileNotFoundException ex)
        {
            Log.Warning(ex, "Video file not found");
            return NotFound(new { success = false, error = "Video file not found", details = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error analyzing consistency");
            return StatusCode(500, new { success = false, error = "Error analyzing consistency", details = ex.Message });
        }
    }

    /// <summary>
    /// Validates against platform-specific requirements
    /// </summary>
    /// <param name="platform">Target platform (youtube, tiktok, instagram, twitter)</param>
    /// <param name="width">Video width in pixels</param>
    /// <param name="height">Video height in pixels</param>
    /// <param name="file_size_bytes">File size in bytes</param>
    /// <param name="duration_seconds">Video duration in seconds</param>
    /// <param name="codec">Video codec (default: H.264)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Platform requirements validation result</returns>
    [HttpGet("validate/platform-requirements")]
    public async Task<IActionResult> ValidatePlatformRequirements(
        [FromQuery] string platform,
        [FromQuery] int width,
        [FromQuery] int height,
        [FromQuery] long file_size_bytes,
        [FromQuery] double duration_seconds,
        [FromQuery] string codec = "H.264",
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] GET /api/quality/validate/platform-requirements - platform={Platform}",
                correlationId, platform);

            if (string.IsNullOrWhiteSpace(platform))
            {
                return BadRequest(new { success = false, error = "Platform is required" });
            }

            if (width <= 0 || height <= 0)
            {
                return BadRequest(new { success = false, error = "Width and height must be positive integers" });
            }

            if (file_size_bytes <= 0)
            {
                return BadRequest(new { success = false, error = "File size must be positive" });
            }

            if (duration_seconds <= 0)
            {
                return BadRequest(new { success = false, error = "Duration must be positive" });
            }

            var result = await _platformService.ValidateAsync(
                platform, width, height, file_size_bytes, duration_seconds, codec, ct).ConfigureAwait(false);

            Log.Information("[{CorrelationId}] Platform validation complete: Valid={IsValid}, Score={Score}",
                correlationId, result.IsValid, result.Score);

            return Ok(new { success = true, result });
        }
        catch (ArgumentException ex)
        {
            Log.Warning(ex, "Invalid platform specified");
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error validating platform requirements");
            return StatusCode(500, new { success = false, error = "Error validating platform requirements", details = ex.Message });
        }
    }
}

/// <summary>
/// Request model for audio validation
/// </summary>
public record AudioValidationRequest(string AudioFilePath);

/// <summary>
/// Request model for consistency validation
/// </summary>
public record ConsistencyValidationRequest(string VideoFilePath);
