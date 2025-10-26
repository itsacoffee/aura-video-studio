using System;
using System.Threading.Tasks;
using Aura.Core.Services.AIEditing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI-powered auto-editing features
/// Provides endpoints for scene detection, highlight detection, beat sync, auto-framing, and auto-captions
/// </summary>
[ApiController]
[Route("api/ai-editing")]
public class AIEditingController : ControllerBase
{
    private readonly ILogger<AIEditingController> _logger;
    private readonly SceneDetectionService _sceneDetectionService;
    private readonly HighlightDetectionService _highlightDetectionService;
    private readonly BeatDetectionService _beatDetectionService;
    private readonly AutoFramingService _autoFramingService;
    private readonly SpeechRecognitionService _speechRecognitionService;

    public AIEditingController(
        ILogger<AIEditingController> logger,
        SceneDetectionService sceneDetectionService,
        HighlightDetectionService highlightDetectionService,
        BeatDetectionService beatDetectionService,
        AutoFramingService autoFramingService,
        SpeechRecognitionService speechRecognitionService)
    {
        _logger = logger;
        _sceneDetectionService = sceneDetectionService;
        _highlightDetectionService = highlightDetectionService;
        _beatDetectionService = beatDetectionService;
        _autoFramingService = autoFramingService;
        _speechRecognitionService = speechRecognitionService;
    }

    /// <summary>
    /// Detect scene changes in video
    /// </summary>
    [HttpPost("detect-scenes")]
    public async Task<IActionResult> DetectScenes([FromBody] AIDetectScenesRequest request)
    {
        try
        {
            _logger.LogInformation("Detecting scenes in video: {VideoPath}", request.VideoPath);

            var result = await _sceneDetectionService.DetectScenesAsync(
                request.VideoPath, 
                request.Threshold ?? 0.3);

            return Ok(new
            {
                success = true,
                result,
                message = "Scene detection completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting scenes in video: {VideoPath}", request.VideoPath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate chapter markers from detected scenes
    /// </summary>
    [HttpPost("generate-chapters")]
    public async Task<IActionResult> GenerateChapters([FromBody] AIGenerateChaptersRequest request)
    {
        try
        {
            _logger.LogInformation("Generating chapter markers for video: {VideoPath}", request.VideoPath);

            var sceneResult = await _sceneDetectionService.DetectScenesAsync(request.VideoPath);
            var chapters = await _sceneDetectionService.GenerateChapterMarkersAsync(sceneResult);

            return Ok(new
            {
                success = true,
                chapters,
                message = $"Generated {chapters.Count} chapter markers"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating chapters: {VideoPath}", request.VideoPath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Detect highlight moments in video
    /// </summary>
    [HttpPost("detect-highlights")]
    public async Task<IActionResult> DetectHighlights([FromBody] AIDetectHighlightsRequest request)
    {
        try
        {
            _logger.LogInformation("Detecting highlights in video: {VideoPath}", request.VideoPath);

            var result = await _highlightDetectionService.DetectHighlightsAsync(
                request.VideoPath,
                request.MaxHighlights ?? 10);

            return Ok(new
            {
                success = true,
                result,
                message = "Highlight detection completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting highlights: {VideoPath}", request.VideoPath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Detect beats in audio for music synchronization
    /// </summary>
    [HttpPost("detect-beats")]
    public async Task<IActionResult> DetectBeats([FromBody] AIDetectBeatsRequest request)
    {
        try
        {
            _logger.LogInformation("Detecting beats in file: {FilePath}", request.FilePath);

            var result = await _beatDetectionService.DetectBeatsAsync(request.FilePath);

            return Ok(new
            {
                success = true,
                result,
                message = "Beat detection completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting beats: {FilePath}", request.FilePath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate beat-aligned cut points
    /// </summary>
    [HttpPost("beat-cuts")]
    public async Task<IActionResult> GenerateBeatCuts([FromBody] AIBeatCutsRequest request)
    {
        try
        {
            _logger.LogInformation("Generating beat-aligned cuts for: {FilePath}", request.FilePath);

            var beatResult = await _beatDetectionService.DetectBeatsAsync(request.FilePath);
            var cuts = await _beatDetectionService.GenerateBeatAlignedCutsAsync(
                beatResult, 
                request.CutEveryNBeats ?? 4);

            return Ok(new
            {
                success = true,
                cuts,
                beatCount = beatResult.TotalBeats,
                message = $"Generated {cuts.Count} beat-aligned cut points"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating beat cuts: {FilePath}", request.FilePath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Analyze video for auto-framing suggestions
    /// </summary>
    [HttpPost("auto-frame")]
    public async Task<IActionResult> AutoFrame([FromBody] AIAutoFrameRequest request)
    {
        try
        {
            _logger.LogInformation("Auto-framing video: {VideoPath}", request.VideoPath);

            var result = await _autoFramingService.AnalyzeFramingAsync(
                request.VideoPath,
                request.TargetWidth,
                request.TargetHeight);

            return Ok(new
            {
                success = true,
                result,
                message = "Auto-framing analysis completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing auto-framing: {VideoPath}", request.VideoPath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Convert video to vertical format (9:16)
    /// </summary>
    [HttpPost("convert-vertical")]
    public async Task<IActionResult> ConvertToVertical([FromBody] AIConvertFormatRequest request)
    {
        try
        {
            _logger.LogInformation("Converting to vertical format: {VideoPath}", request.VideoPath);

            var result = await _autoFramingService.ConvertToVerticalAsync(request.VideoPath);

            return Ok(new
            {
                success = true,
                result,
                message = "Vertical conversion analysis completed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to vertical: {VideoPath}", request.VideoPath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Convert video to square format (1:1)
    /// </summary>
    [HttpPost("convert-square")]
    public async Task<IActionResult> ConvertToSquare([FromBody] AIConvertFormatRequest request)
    {
        try
        {
            _logger.LogInformation("Converting to square format: {VideoPath}", request.VideoPath);

            var result = await _autoFramingService.ConvertToSquareAsync(request.VideoPath);

            return Ok(new
            {
                success = true,
                result,
                message = "Square conversion analysis completed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting to square: {VideoPath}", request.VideoPath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate captions from video/audio
    /// </summary>
    [HttpPost("generate-captions")]
    public async Task<IActionResult> GenerateCaptions([FromBody] AIGenerateCaptionsRequest request)
    {
        try
        {
            _logger.LogInformation("Generating captions for: {FilePath}", request.FilePath);

            var result = await _speechRecognitionService.GenerateCaptionsAsync(
                request.FilePath,
                request.Language ?? "en");

            return Ok(new
            {
                success = true,
                result,
                message = "Caption generation completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating captions: {FilePath}", request.FilePath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Export captions to SRT format
    /// </summary>
    [HttpPost("export-srt")]
    public async Task<IActionResult> ExportSrt([FromBody] AIExportCaptionsRequest request)
    {
        try
        {
            _logger.LogInformation("Exporting captions to SRT: {OutputPath}", request.OutputPath);

            var captionResult = await _speechRecognitionService.GenerateCaptionsAsync(request.FilePath);
            var outputPath = await _speechRecognitionService.ExportToSrtAsync(captionResult, request.OutputPath);

            return Ok(new
            {
                success = true,
                outputPath,
                captionCount = captionResult.Captions.Count,
                message = "SRT export completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to SRT: {FilePath}", request.FilePath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Export captions to VTT format
    /// </summary>
    [HttpPost("export-vtt")]
    public async Task<IActionResult> ExportVtt([FromBody] AIExportCaptionsRequest request)
    {
        try
        {
            _logger.LogInformation("Exporting captions to VTT: {OutputPath}", request.OutputPath);

            var captionResult = await _speechRecognitionService.GenerateCaptionsAsync(request.FilePath);
            var outputPath = await _speechRecognitionService.ExportToVttAsync(captionResult, request.OutputPath);

            return Ok(new
            {
                success = true,
                outputPath,
                captionCount = captionResult.Captions.Count,
                message = "VTT export completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to VTT: {FilePath}", request.FilePath);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }
}

// Request DTOs for AI Editing
public record AIDetectScenesRequest(string VideoPath, double? Threshold);
public record AIGenerateChaptersRequest(string VideoPath);
public record AIDetectHighlightsRequest(string VideoPath, int? MaxHighlights);
public record AIDetectBeatsRequest(string FilePath);
public record AIBeatCutsRequest(string FilePath, int? CutEveryNBeats);
public record AIAutoFrameRequest(string VideoPath, int TargetWidth, int TargetHeight);
public record AIConvertFormatRequest(string VideoPath);
public record AIGenerateCaptionsRequest(string FilePath, string? Language);
public record AIExportCaptionsRequest(string FilePath, string OutputPath);
