using Aura.Core.Analytics.Retention;
using Aura.Core.Analytics.Platforms;
using Aura.Core.Analytics.Content;
using Aura.Core.Analytics.Recommendations;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for audience retention analytics and content optimization
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnalyticsController : ControllerBase
{
    private readonly RetentionPredictor _retentionPredictor;
    private readonly PlatformOptimizer _platformOptimizer;
    private readonly ContentAnalyzer _contentAnalyzer;
    private readonly ImprovementEngine _improvementEngine;

    public AnalyticsController(
        RetentionPredictor retentionPredictor,
        PlatformOptimizer platformOptimizer,
        ContentAnalyzer contentAnalyzer,
        ImprovementEngine improvementEngine)
    {
        _retentionPredictor = retentionPredictor;
        _platformOptimizer = platformOptimizer;
        _contentAnalyzer = contentAnalyzer;
        _improvementEngine = improvementEngine;
    }

    /// <summary>
    /// Predicts audience retention for content
    /// </summary>
    [HttpPost("predict-retention")]
    public async Task<IActionResult> PredictRetention(
        [FromBody] PredictRetentionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Predicting retention for {ContentType}", correlationId, request.ContentType);

            var prediction = await _retentionPredictor.PredictRetentionAsync(
                request.Content,
                request.ContentType,
                TimeSpan.Parse(request.VideoDuration),
                request.TargetDemographic,
                ct
            );

            return Ok(new
            {
                retentionCurve = prediction.RetentionCurve.Select(p => new
                {
                    timePoint = p.TimePoint.ToString(),
                    retention = p.Retention
                }),
                predictedAverageRetention = prediction.PredictedAverageRetention,
                engagementDips = prediction.EngagementDips.Select(d => new
                {
                    timePoint = d.TimePoint.ToString(),
                    retentionDrop = d.RetentionDrop,
                    severity = d.Severity,
                    reason = d.Reason
                }),
                optimalLength = prediction.OptimalLength.ToString(),
                recommendations = prediction.Recommendations,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error predicting retention", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/retention-prediction",
                title = "Retention Prediction Failed",
                status = 500,
                detail = $"Failed to predict retention: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Analyzes attention span patterns
    /// </summary>
    [HttpPost("analyze-attention")]
    public async Task<IActionResult> AnalyzeAttention(
        [FromBody] AnalyzeAttentionRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Analyzing attention span", correlationId);

            var analysis = await _retentionPredictor.AnalyzeAttentionSpanAsync(
                request.Content,
                TimeSpan.Parse(request.VideoDuration),
                ct
            );

            return Ok(new
            {
                segmentScores = analysis.SegmentScores.Select(s => new
                {
                    segmentIndex = s.SegmentIndex,
                    startTime = s.StartTime.ToString(),
                    duration = s.Duration.ToString(),
                    engagementScore = s.EngagementScore,
                    reasoning = s.Reasoning
                }),
                criticalDropPoints = analysis.CriticalDropPoints.Select(s => new
                {
                    segmentIndex = s.SegmentIndex,
                    startTime = s.StartTime.ToString(),
                    engagementScore = s.EngagementScore
                }),
                averageEngagement = analysis.AverageEngagement,
                suggestions = analysis.Suggestions,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error analyzing attention", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/attention-analysis",
                title = "Attention Analysis Failed",
                status = 500,
                detail = $"Failed to analyze attention: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Gets platform-specific optimization recommendations
    /// </summary>
    [HttpPost("optimize-platform")]
    public async Task<IActionResult> OptimizePlatform(
        [FromBody] OptimizePlatformRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Optimizing for platform: {Platform}", correlationId, request.Platform);

            var optimization = await _platformOptimizer.GetPlatformOptimizationAsync(
                request.Platform,
                request.Content,
                TimeSpan.Parse(request.VideoDuration),
                ct
            );

            return Ok(new
            {
                platform = optimization.Platform,
                optimalDuration = optimization.OptimalDuration.ToString(),
                recommendedAspectRatio = optimization.RecommendedAspectRatio,
                optimalThumbnailSize = optimization.OptimalThumbnailSize,
                recommendations = optimization.Recommendations,
                metadataGuidelines = optimization.MetadataGuidelines,
                hashtagSuggestions = optimization.HashtagSuggestions,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error optimizing platform", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/platform-optimization",
                title = "Platform Optimization Failed",
                status = 500,
                detail = $"Failed to optimize platform: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Suggests aspect ratios for cross-platform publishing
    /// </summary>
    [HttpPost("suggest-aspect-ratios")]
    public async Task<IActionResult> SuggestAspectRatios(
        [FromBody] SuggestAspectRatiosRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Suggesting aspect ratios for {Count} platforms", 
                correlationId, request.TargetPlatforms.Count);

            var suggestions = await _platformOptimizer.SuggestAspectRatioAdaptationsAsync(
                request.TargetPlatforms,
                ct
            );

            return Ok(new
            {
                suggestions = suggestions.Suggestions.Select(s => new
                {
                    platform = s.Platform,
                    aspectRatio = s.AspectRatio,
                    resolution = s.Resolution,
                    reasoning = s.Reasoning
                }),
                recommendedPrimaryFormat = suggestions.RecommendedPrimaryFormat,
                adaptationStrategy = suggestions.AdaptationStrategy,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error suggesting aspect ratios", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/aspect-ratio-suggestion",
                title = "Aspect Ratio Suggestion Failed",
                status = 500,
                detail = $"Failed to suggest aspect ratios: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Analyzes content structure
    /// </summary>
    [HttpPost("analyze-structure")]
    public async Task<IActionResult> AnalyzeStructure(
        [FromBody] AnalyzeStructureRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Analyzing content structure", correlationId);

            var analysis = await _contentAnalyzer.AnalyzeContentStructureAsync(
                request.Content,
                request.ContentType,
                ct
            );

            return Ok(new
            {
                hookQuality = analysis.HookQuality,
                hookSuggestions = analysis.HookSuggestions,
                pacingScore = analysis.PacingScore,
                pacingIssues = analysis.PacingIssues,
                structuralStrength = analysis.StructuralStrength,
                improvementAreas = analysis.ImprovementAreas,
                overallScore = analysis.OverallScore,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error analyzing structure", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/structure-analysis",
                title = "Structure Analysis Failed",
                status = 500,
                detail = $"Failed to analyze structure: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Gets content improvement recommendations
    /// </summary>
    [HttpPost("get-recommendations")]
    public async Task<IActionResult> GetRecommendations(
        [FromBody] GetRecommendationsRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Getting recommendations", correlationId);

            var recommendations = await _contentAnalyzer.GetContentRecommendationsAsync(
                request.Content,
                request.TargetAudience,
                ct
            );

            return Ok(new
            {
                targetAudience = recommendations.TargetAudience,
                recommendations = recommendations.Recommendations.Select(r => new
                {
                    area = r.Area,
                    priority = r.Priority,
                    currentState = r.CurrentState,
                    suggestion = r.Suggestion,
                    expectedImpact = r.ExpectedImpact
                }),
                estimatedImprovementScore = recommendations.EstimatedImprovementScore,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting recommendations", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/recommendations",
                title = "Recommendations Failed",
                status = 500,
                detail = $"Failed to get recommendations: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Generates comprehensive improvement roadmap
    /// </summary>
    [HttpPost("improvement-roadmap")]
    public async Task<IActionResult> GetImprovementRoadmap(
        [FromBody] ImprovementRoadmapRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Generating improvement roadmap", correlationId);

            var roadmap = await _improvementEngine.GenerateImprovementRoadmapAsync(
                request.Content,
                request.ContentType,
                TimeSpan.Parse(request.VideoDuration),
                request.TargetPlatforms,
                ct
            );

            return Ok(new
            {
                currentScore = roadmap.CurrentScore,
                potentialScore = roadmap.PotentialScore,
                prioritizedActions = roadmap.PrioritizedActions.Select(a => new
                {
                    title = a.Title,
                    description = a.Description,
                    impact = a.Impact,
                    difficulty = a.Difficulty,
                    category = a.Category,
                    estimatedTime = a.EstimatedTime.ToString()
                }),
                quickWins = roadmap.QuickWins.Select(a => new { a.Title, a.Description }),
                estimatedTimeToImprove = roadmap.EstimatedTimeToImprove.ToString(),
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error generating roadmap", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/improvement-roadmap",
                title = "Improvement Roadmap Failed",
                status = 500,
                detail = $"Failed to generate roadmap: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Provides real-time feedback for content being created
    /// </summary>
    [HttpPost("real-time-feedback")]
    public async Task<IActionResult> GetRealTimeFeedback(
        [FromBody] RealTimeFeedbackRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Providing real-time feedback", correlationId);

            var feedback = await _improvementEngine.GetRealTimeFeedbackAsync(
                request.CurrentContent,
                request.CurrentWordCount,
                TimeSpan.Parse(request.CurrentDuration),
                ct
            );

            return Ok(new
            {
                issues = feedback.Issues.Select(i => new
                {
                    type = i.Type,
                    severity = i.Severity,
                    message = i.Message,
                    suggestion = i.Suggestion
                }),
                strengths = feedback.Strengths,
                currentQualityScore = feedback.CurrentQualityScore,
                suggestions = feedback.Suggestions,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error providing feedback", correlationId);

            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/real-time-feedback",
                title = "Real-time Feedback Failed",
                status = 500,
                detail = $"Failed to provide feedback: {ex.Message}",
                correlationId
            });
        }
    }
}

// Request models
public record PredictRetentionRequest(
    string Content,
    string ContentType,
    string VideoDuration,
    string? TargetDemographic = null
);

public record AnalyzeAttentionRequest(
    string Content,
    string VideoDuration
);

public record OptimizePlatformRequest(
    string Platform,
    string Content,
    string VideoDuration
);

public record SuggestAspectRatiosRequest(
    List<string> TargetPlatforms
);

public record AnalyzeStructureRequest(
    string Content,
    string ContentType
);

public record GetRecommendationsRequest(
    string Content,
    string TargetAudience
);

public record ImprovementRoadmapRequest(
    string Content,
    string ContentType,
    string VideoDuration,
    List<string> TargetPlatforms
);

public record RealTimeFeedbackRequest(
    string CurrentContent,
    int CurrentWordCount,
    string CurrentDuration
);
