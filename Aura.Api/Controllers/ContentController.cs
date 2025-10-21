using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Content;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI-powered content analysis and enhancement
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly ContentAnalyzer _contentAnalyzer;
    private readonly ScriptEnhancer _scriptEnhancer;
    private readonly VisualAssetSuggester _visualAssetSuggester;
    private readonly PacingOptimizer _pacingOptimizer;

    public ContentController(
        ContentAnalyzer contentAnalyzer,
        ScriptEnhancer scriptEnhancer,
        VisualAssetSuggester visualAssetSuggester,
        PacingOptimizer pacingOptimizer)
    {
        _contentAnalyzer = contentAnalyzer;
        _scriptEnhancer = scriptEnhancer;
        _visualAssetSuggester = visualAssetSuggester;
        _pacingOptimizer = pacingOptimizer;
    }

    /// <summary>
    /// Analyzes a script for quality metrics
    /// </summary>
    [HttpPost("analyze-script")]
    public async Task<IActionResult> AnalyzeScript(
        [FromBody] AnalyzeScriptRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Analyzing script", correlationId);

            var analysis = await _contentAnalyzer.AnalyzeScriptAsync(request.Script, ct);

            return Ok(new
            {
                coherenceScore = analysis.CoherenceScore,
                pacingScore = analysis.PacingScore,
                engagementScore = analysis.EngagementScore,
                readabilityScore = analysis.ReadabilityScore,
                overallQualityScore = analysis.OverallQualityScore,
                issues = analysis.Issues,
                suggestions = analysis.Suggestions,
                statistics = new
                {
                    totalWordCount = analysis.Statistics.TotalWordCount,
                    averageWordsPerScene = analysis.Statistics.AverageWordsPerScene,
                    estimatedReadingTime = analysis.Statistics.EstimatedReadingTime.ToString(),
                    complexityScore = analysis.Statistics.ComplexityScore
                },
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error analyzing script", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/content-analysis",
                title = "Script Analysis Failed",
                status = 500,
                detail = $"Failed to analyze script: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Enhances a script based on provided options
    /// </summary>
    [HttpPost("enhance-script")]
    public async Task<IActionResult> EnhanceScript(
        [FromBody] EnhanceScriptRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Enhancing script", correlationId);

            var options = new EnhancementOptions(
                FixCoherence: request.FixCoherence,
                IncreaseEngagement: request.IncreaseEngagement,
                ImproveClarity: request.ImproveClarity,
                AddDetails: request.AddDetails
            );

            var enhanced = await _scriptEnhancer.EnhanceScriptAsync(request.Script, options, ct);

            return Ok(new
            {
                newScript = enhanced.NewScript,
                changes = enhanced.Changes.Select(c => new
                {
                    type = c.Type,
                    lineNumber = c.LineNumber,
                    originalText = c.OriginalText,
                    newText = c.NewText
                }),
                improvementSummary = enhanced.ImprovementSummary,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error enhancing script", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/content-enhancement",
                title = "Script Enhancement Failed",
                status = 500,
                detail = $"Failed to enhance script: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Suggests visual assets for a scene
    /// </summary>
    [HttpPost("suggest-assets")]
    public async Task<IActionResult> SuggestAssets(
        [FromBody] SuggestAssetsRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Suggesting assets for scene: {Heading}", correlationId, request.SceneHeading);

            var suggestions = await _visualAssetSuggester.SuggestAssetsForSceneAsync(
                request.SceneHeading, 
                request.SceneScript, 
                ct
            );

            return Ok(new
            {
                suggestions = suggestions.Select(s => new
                {
                    keyword = s.Keyword,
                    description = s.Description,
                    matches = s.Matches.Select(m => new
                    {
                        filePath = m.FilePath,
                        url = m.Url,
                        relevanceScore = m.RelevanceScore,
                        thumbnailUrl = m.ThumbnailUrl
                    })
                }),
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error suggesting assets", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/asset-suggestion",
                title = "Asset Suggestion Failed",
                status = 500,
                detail = $"Failed to suggest assets: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Optimizes pacing for a timeline
    /// </summary>
    [HttpPost("optimize-pacing")]
    public async Task<IActionResult> OptimizePacing(
        [FromBody] OptimizePacingRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Optimizing pacing for timeline", correlationId);

            // Convert request to Timeline
            var scenes = request.Scenes.Select(s => new Scene(
                Index: s.Index,
                Heading: s.Heading,
                Script: s.Script,
                Start: TimeSpan.Parse(s.Start),
                Duration: TimeSpan.Parse(s.Duration)
            )).ToList();

            var timeline = new Aura.Core.Providers.Timeline(
                Scenes: scenes,
                SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
                NarrationPath: request.NarrationPath ?? "",
                MusicPath: request.MusicPath ?? "",
                SubtitlesPath: null
            );

            var optimization = await _pacingOptimizer.OptimizeTimingAsync(timeline, ct);

            return Ok(new
            {
                suggestions = optimization.Suggestions.Select(s => new
                {
                    sceneIndex = s.SceneIndex,
                    currentDuration = s.CurrentDuration.ToString(),
                    suggestedDuration = s.SuggestedDuration.ToString(),
                    reasoning = s.Reasoning,
                    priority = s.Priority.ToString()
                }),
                overallAssessment = optimization.OverallAssessment,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error optimizing pacing", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://docs.aura.studio/errors/pacing-optimization",
                title = "Pacing Optimization Failed",
                status = 500,
                detail = $"Failed to optimize pacing: {ex.Message}",
                correlationId
            });
        }
    }
}

// Request models
public record AnalyzeScriptRequest(string Script);

public record EnhanceScriptRequest(
    string Script,
    bool FixCoherence = false,
    bool IncreaseEngagement = false,
    bool ImproveClarity = false,
    bool AddDetails = false);

public record SuggestAssetsRequest(string SceneHeading, string SceneScript);

public record OptimizePacingRequest(
    List<SceneDto> Scenes,
    string? NarrationPath = null,
    string? MusicPath = null);

public record SceneDto(
    int Index,
    string Heading,
    string Script,
    string Start,
    string Duration);
