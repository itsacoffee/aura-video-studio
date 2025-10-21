using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Pacing;
using Aura.Core.Models;
using Aura.Core.Services.Analytics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoints for AI-driven pacing and rhythm optimization.
/// </summary>
[ApiController]
[Route("api/pacing")]
[Produces("application/json")]
public class PacingController : ControllerBase
{
    private readonly ILogger<PacingController> _logger;
    private readonly PacingAnalyzer _pacingAnalyzer;
    private readonly RetentionOptimizer _retentionOptimizer;
    private readonly ViewerRetentionPredictor _retentionPredictor;

    public PacingController(
        ILogger<PacingController> logger,
        PacingAnalyzer pacingAnalyzer,
        RetentionOptimizer retentionOptimizer,
        ViewerRetentionPredictor retentionPredictor)
    {
        _logger = logger;
        _pacingAnalyzer = pacingAnalyzer;
        _retentionOptimizer = retentionOptimizer;
        _retentionPredictor = retentionPredictor;
    }

    /// <summary>
    /// Analyzes pacing for a set of scenes.
    /// </summary>
    /// <param name="request">Pacing analysis request with scenes and options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Pacing analysis results with recommendations</returns>
    [HttpPost("analyze")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PacingAnalysisResult>> AnalyzePacing(
        [FromBody] PacingAnalysisRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Analyzing pacing for {SceneCount} scenes", request.Scenes.Count);

            if (request.Scenes == null || request.Scenes.Count == 0)
            {
                return BadRequest(new { error = "Scenes collection cannot be empty" });
            }

            var result = await _pacingAnalyzer.AnalyzePacingAsync(
                request.Scenes,
                request.AudioPath,
                request.Format,
                ct
            ).ConfigureAwait(false);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing pacing");
            return StatusCode(500, new { error = "Internal server error during pacing analysis" });
        }
    }

    /// <summary>
    /// Predicts viewer retention for video content.
    /// </summary>
    /// <param name="request">Retention analysis request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Comprehensive retention analysis</returns>
    [HttpPost("retention")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VideoRetentionAnalysis>> PredictRetention(
        [FromBody] RetentionAnalysisRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Predicting retention for video format: {Format}", request.Format);

            if (request.Scenes == null || request.Scenes.Count == 0)
            {
                return BadRequest(new { error = "Scenes collection cannot be empty" });
            }

            var analysis = await _retentionPredictor.AnalyzeRetentionAsync(
                request.Scenes,
                request.AudioPath,
                request.Format,
                ct
            ).ConfigureAwait(false);

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error predicting retention");
            return StatusCode(500, new { error = "Internal server error during retention prediction" });
        }
    }

    /// <summary>
    /// Optimizes scene durations for better viewer retention.
    /// </summary>
    /// <param name="request">Optimization request</param>
    /// <returns>Optimized scenes</returns>
    [HttpPost("optimize")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<Scene>> OptimizeScenes([FromBody] OptimizationRequest request)
    {
        try
        {
            _logger.LogInformation("Optimizing {SceneCount} scenes for format: {Format}",
                request.Scenes.Count, request.Format);

            if (request.Scenes == null || request.Scenes.Count == 0)
            {
                return BadRequest(new { error = "Scenes collection cannot be empty" });
            }

            var optimizedScenes = _retentionOptimizer.OptimizeForRetention(
                request.Scenes,
                request.Format
            );

            return Ok(optimizedScenes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing scenes");
            return StatusCode(500, new { error = "Internal server error during optimization" });
        }
    }

    /// <summary>
    /// Generates attention curve for a video.
    /// </summary>
    /// <param name="request">Attention curve request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Predicted attention curve</returns>
    [HttpPost("attention-curve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AttentionCurve>> GetAttentionCurve(
        [FromBody] AttentionCurveRequest request,
        CancellationToken ct)
    {
        try
        {
            _logger.LogDebug("Generating attention curve for duration: {Duration}", request.VideoDuration);

            if (request.Scenes == null || request.Scenes.Count == 0)
            {
                return BadRequest(new { error = "Scenes collection cannot be empty" });
            }

            var curve = await _retentionOptimizer.GenerateAttentionCurveAsync(
                request.Scenes,
                request.VideoDuration,
                ct
            ).ConfigureAwait(false);

            return Ok(curve);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating attention curve");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Compares original vs optimized versions.
    /// </summary>
    /// <param name="request">Comparison request</param>
    /// <returns>Comparison metrics</returns>
    [HttpPost("compare")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<VideoComparisonMetrics> CompareVersions([FromBody] ComparisonRequest request)
    {
        try
        {
            _logger.LogInformation("Comparing original vs optimized versions");

            if (request.OriginalScenes == null || request.OriginalScenes.Count == 0)
            {
                return BadRequest(new { error = "Original scenes collection cannot be empty" });
            }

            if (request.OptimizedScenes == null || request.OptimizedScenes.Count == 0)
            {
                return BadRequest(new { error = "Optimized scenes collection cannot be empty" });
            }

            var metrics = _retentionPredictor.CompareVersions(
                request.OriginalScenes,
                request.OptimizedScenes,
                request.Format
            );

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing versions");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Gets available content templates.
    /// </summary>
    /// <returns>List of content templates with their parameters</returns>
    [HttpGet("templates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<ContentTemplateDto>> GetTemplates()
    {
        var templates = new List<ContentTemplateDto>
        {
            new ContentTemplateDto(
                "Explainer Video",
                "Clear, concise explanations with visual support",
                VideoFormat.Explainer,
                new PacingParametersDto(8, 25, 15, 0.6, 10, true)
            ),
            new ContentTemplateDto(
                "Tutorial Video",
                "Step-by-step instructional content",
                VideoFormat.Tutorial,
                new PacingParametersDto(15, 40, 25, 0.4, 12, false)
            ),
            new ContentTemplateDto(
                "Vlog",
                "Personal, narrative-driven content",
                VideoFormat.Vlog,
                new PacingParametersDto(5, 20, 12, 0.8, 8, true)
            ),
            new ContentTemplateDto(
                "Review Video",
                "Product or service evaluation",
                VideoFormat.Review,
                new PacingParametersDto(10, 30, 18, 0.5, 10, true)
            ),
            new ContentTemplateDto(
                "Educational Content",
                "In-depth learning material",
                VideoFormat.Educational,
                new PacingParametersDto(20, 60, 35, 0.3, 15, false)
            ),
            new ContentTemplateDto(
                "Entertainment",
                "Engaging, fast-paced content",
                VideoFormat.Entertainment,
                new PacingParametersDto(3, 15, 8, 0.9, 5, true)
            )
        };

        return Ok(templates);
    }
}

// Request/Response DTOs

/// <summary>
/// Request for pacing analysis.
/// </summary>
public record PacingAnalysisRequest(
    List<Scene> Scenes,
    string? AudioPath,
    VideoFormat Format
);

/// <summary>
/// Request for retention analysis.
/// </summary>
public record RetentionAnalysisRequest(
    List<Scene> Scenes,
    string? AudioPath,
    VideoFormat Format
);

/// <summary>
/// Request for scene optimization.
/// </summary>
public record OptimizationRequest(
    List<Scene> Scenes,
    VideoFormat Format
);

/// <summary>
/// Request for attention curve generation.
/// </summary>
public record AttentionCurveRequest(
    List<Scene> Scenes,
    TimeSpan VideoDuration
);

/// <summary>
/// Request for version comparison.
/// </summary>
public record ComparisonRequest(
    List<Scene> OriginalScenes,
    List<Scene> OptimizedScenes,
    VideoFormat Format
);

/// <summary>
/// Content template DTO for API responses.
/// </summary>
public record ContentTemplateDto(
    string Name,
    string Description,
    VideoFormat Format,
    PacingParametersDto Parameters
);

/// <summary>
/// Pacing parameters DTO.
/// </summary>
public record PacingParametersDto(
    double MinSceneDuration,
    double MaxSceneDuration,
    double AverageSceneDuration,
    double TransitionDensity,
    double HookDuration,
    bool MusicSyncEnabled
);
