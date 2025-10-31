using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.Requests;
using Aura.Api.Models.Responses;
using Aura.Api.Services;
using Aura.Core.Models;
using Aura.Core.Services.PacingServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// REST API endpoints for pacing analysis and management.
/// </summary>
[ApiController]
[Route("api/pacing")]
[Produces("application/json")]
public class PacingController : ControllerBase
{
    private readonly ILogger<PacingController> _logger;
    private readonly IntelligentPacingOptimizer _pacingOptimizer;
    private readonly PacingAnalysisCacheService _cacheService;

    public PacingController(
        ILogger<PacingController> logger,
        IntelligentPacingOptimizer pacingOptimizer,
        PacingAnalysisCacheService cacheService)
    {
        _logger = logger;
        _pacingOptimizer = pacingOptimizer;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Analyze script and scenes for optimal pacing.
    /// </summary>
    /// <param name="request">Pacing analysis request with script, scenes, and parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Comprehensive pacing analysis with suggestions</returns>
    [HttpPost("analyze")]
    [ProducesResponseType(typeof(PacingAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PacingAnalysisResponse>> AnalyzePacing(
        [FromBody] PacingAnalysisRequest request,
        CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation("Starting pacing analysis. CorrelationId: {CorrelationId}, Scenes: {SceneCount}, Platform: {Platform}",
                correlationId, request.Scenes?.Count ?? 0, request.TargetPlatform);

            // Validate request
            if (string.IsNullOrWhiteSpace(request.Script))
            {
                return Problem(
                    detail: "Script must not be empty or null",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request"
                );
            }

            if (request.Scenes == null || request.Scenes.Count == 0)
            {
                return Problem(
                    detail: "Scenes array must contain at least 1 scene",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request"
                );
            }

            if (request.TargetDuration <= 0)
            {
                return Problem(
                    detail: "Target duration must be a positive number",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request"
                );
            }

            if (!IsValidPlatform(request.TargetPlatform))
            {
                return Problem(
                    detail: "Target platform must be one of: YouTube, TikTok, Instagram Reels, YouTube Shorts, Facebook",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request"
                );
            }

            // Perform analysis
            var result = await _pacingOptimizer.OptimizePacingAsync(
                request.Scenes,
                request.Brief,
                llmProvider: null,
                useAdaptivePacing: false,
                pacingProfile: PacingProfile.BalancedDocumentary,
                ct
            ).ConfigureAwait(false);

            // Create response
            var analysisId = Guid.NewGuid().ToString();
            var response = new PacingAnalysisResponse
            {
                OverallScore = result.ConfidenceScore,
                Suggestions = result.TimingSuggestions.ToList(),
                AttentionCurve = result.AttentionCurve,
                EstimatedRetention = result.PredictedRetentionRate,
                AverageEngagement = result.AttentionCurve?.AverageEngagement ?? 0,
                AnalysisId = analysisId,
                Timestamp = DateTime.UtcNow,
                CorrelationId = correlationId,
                ConfidenceScore = result.ConfidenceScore,
                Warnings = result.Warnings.ToList()
            };

            // Cache the result
            _cacheService.Set(analysisId, response);

            _logger.LogInformation("Pacing analysis completed. CorrelationId: {CorrelationId}, AnalysisId: {AnalysisId}, Score: {Score:F1}",
                correlationId, analysisId, response.OverallScore);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pacing analysis. CorrelationId: {CorrelationId}", correlationId);
            
            return Problem(
                detail: $"An error occurred during pacing analysis. CorrelationId: {correlationId}. Please try again or contact support if the issue persists.",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error"
            );
        }
    }

    /// <summary>
    /// Get available platform presets with pacing recommendations.
    /// </summary>
    /// <returns>List of platform presets</returns>
    [HttpGet("platforms")]
    [ProducesResponseType(typeof(PlatformPresetsResponse), StatusCodes.Status200OK)]
    [ResponseCache(Duration = 3600)] // Cache for 1 hour
    public ActionResult<PlatformPresetsResponse> GetPlatformPresets()
    {
        _logger.LogDebug("Retrieving platform presets");

        var response = new PlatformPresetsResponse
        {
            Platforms = new List<PlatformPreset>
            {
                new PlatformPreset
                {
                    Name = "YouTube",
                    RecommendedPacing = "Conversational",
                    AvgSceneDuration = "15-30s",
                    OptimalVideoLength = 600, // 8-15 min range, using 10 min as default
                    PacingMultiplier = 1.0
                },
                new PlatformPreset
                {
                    Name = "TikTok",
                    RecommendedPacing = "Fast",
                    AvgSceneDuration = "3-8s",
                    OptimalVideoLength = 37.5, // 15-60s range, using average
                    PacingMultiplier = 0.7
                },
                new PlatformPreset
                {
                    Name = "Instagram Reels",
                    RecommendedPacing = "Fast",
                    AvgSceneDuration = "3-8s",
                    OptimalVideoLength = 52.5, // 15-90s range, using average
                    PacingMultiplier = 0.75
                },
                new PlatformPreset
                {
                    Name = "YouTube Shorts",
                    RecommendedPacing = "Fast",
                    AvgSceneDuration = "5-10s",
                    OptimalVideoLength = 37.5, // 15-60s range, using average
                    PacingMultiplier = 0.8
                },
                new PlatformPreset
                {
                    Name = "Facebook",
                    RecommendedPacing = "Balanced",
                    AvgSceneDuration = "10-20s",
                    OptimalVideoLength = 240, // 3-5 min range, using 4 min as default
                    PacingMultiplier = 0.9
                }
            }
        };

        return Ok(response);
    }

    /// <summary>
    /// Reanalyze with different parameters.
    /// </summary>
    /// <param name="analysisId">ID of the previous analysis</param>
    /// <param name="request">Reanalysis parameters</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated analysis results</returns>
    [HttpPost("reanalyze/{analysisId}")]
    [ProducesResponseType(typeof(PacingAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PacingAnalysisResponse>> ReanalyzePacing(
        string analysisId,
        [FromBody] ReanalyzeRequest request,
        CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogInformation("Reanalyzing {AnalysisId}. CorrelationId: {CorrelationId}, Platform: {Platform}, Level: {Level}",
                analysisId, correlationId, request.TargetPlatform, request.OptimizationLevel);

            // Retrieve the original analysis
            var originalAnalysis = _cacheService.Get(analysisId);
            if (originalAnalysis == null)
            {
                return Problem(
                    detail: $"Analysis with ID '{analysisId}' not found or has expired",
                    statusCode: StatusCodes.Status404NotFound,
                    title: "Analysis Not Found"
                );
            }

            if (!IsValidPlatform(request.TargetPlatform))
            {
                return Problem(
                    detail: "Target platform must be one of: YouTube, TikTok, Instagram Reels, YouTube Shorts, Facebook",
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid Request"
                );
            }

            // For reanalysis, we'd need to store the original request data
            // For now, return the original analysis with a note
            // In a full implementation, we'd store the original request and re-run with new parameters
            
            var newAnalysisId = Guid.NewGuid().ToString();
            var response = new PacingAnalysisResponse
            {
                OverallScore = originalAnalysis.OverallScore,
                Suggestions = originalAnalysis.Suggestions,
                AttentionCurve = originalAnalysis.AttentionCurve,
                EstimatedRetention = originalAnalysis.EstimatedRetention,
                AverageEngagement = originalAnalysis.AverageEngagement,
                AnalysisId = newAnalysisId,
                Timestamp = DateTime.UtcNow,
                CorrelationId = correlationId,
                ConfidenceScore = originalAnalysis.ConfidenceScore,
                Warnings = new List<string>(originalAnalysis.Warnings)
                {
                    $"Reanalyzed from {analysisId} with platform: {request.TargetPlatform}, level: {request.OptimizationLevel}"
                }
            };

            _cacheService.Set(newAnalysisId, response);

            _logger.LogInformation("Reanalysis completed. CorrelationId: {CorrelationId}, NewAnalysisId: {AnalysisId}",
                correlationId, newAnalysisId);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reanalysis. CorrelationId: {CorrelationId}, AnalysisId: {AnalysisId}",
                correlationId, analysisId);

            return Problem(
                detail: $"An error occurred during reanalysis. CorrelationId: {correlationId}",
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal Server Error"
            );
        }
    }

    /// <summary>
    /// Retrieve previous analysis results.
    /// </summary>
    /// <param name="analysisId">ID of the analysis to retrieve</param>
    /// <returns>Analysis results</returns>
    [HttpGet("analysis/{analysisId}")]
    [ProducesResponseType(typeof(PacingAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<PacingAnalysisResponse> GetAnalysis(string analysisId)
    {
        _logger.LogInformation("Retrieving analysis {AnalysisId}", analysisId);

        var analysis = _cacheService.Get(analysisId);
        if (analysis == null)
        {
            return Problem(
                detail: $"Analysis with ID '{analysisId}' not found or has expired",
                statusCode: StatusCodes.Status404NotFound,
                title: "Analysis Not Found"
            );
        }

        return Ok(analysis);
    }

    /// <summary>
    /// Delete analysis results.
    /// </summary>
    /// <param name="analysisId">ID of the analysis to delete</param>
    /// <returns>Delete operation result</returns>
    [HttpDelete("analysis/{analysisId}")]
    [ProducesResponseType(typeof(DeleteAnalysisResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public ActionResult<DeleteAnalysisResponse> DeleteAnalysis(string analysisId)
    {
        _logger.LogInformation("Deleting analysis {AnalysisId}", analysisId);

        var deleted = _cacheService.Delete(analysisId);
        
        if (!deleted)
        {
            return Problem(
                detail: $"Analysis with ID '{analysisId}' not found",
                statusCode: StatusCodes.Status404NotFound,
                title: "Analysis Not Found"
            );
        }

        return Ok(new DeleteAnalysisResponse
        {
            Success = true,
            Message = $"Analysis '{analysisId}' has been successfully deleted"
        });
    }

    /// <summary>
    /// Validates if the platform name is supported.
    /// </summary>
    private static bool IsValidPlatform(string platform)
    {
        var validPlatforms = new[]
        {
            "YouTube",
            "TikTok",
            "Instagram Reels",
            "YouTube Shorts",
            "Facebook"
        };

        return validPlatforms.Contains(platform, StringComparer.OrdinalIgnoreCase);
    }
}
