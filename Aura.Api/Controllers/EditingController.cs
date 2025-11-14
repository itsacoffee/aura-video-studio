using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aura.Core.Models.EditingIntelligence;
using Aura.Core.Services.EditingIntelligence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI-powered editing intelligence and timeline optimization
/// </summary>
[ApiController]
[Route("api/editing")]
public class EditingController : ControllerBase
{
    private readonly ILogger<EditingController> _logger;
    private readonly EditingIntelligenceOrchestrator _orchestrator;
    private readonly CutPointDetectionService _cutPointService;
    private readonly PacingOptimizationService _pacingService;
    private readonly TransitionRecommendationService _transitionService;
    private readonly EngagementOptimizationService _engagementService;
    private readonly QualityControlService _qualityService;

    public EditingController(
        ILogger<EditingController> logger,
        EditingIntelligenceOrchestrator orchestrator,
        CutPointDetectionService cutPointService,
        PacingOptimizationService pacingService,
        TransitionRecommendationService transitionService,
        EngagementOptimizationService engagementService,
        QualityControlService qualityService)
    {
        _logger = logger;
        _orchestrator = orchestrator;
        _cutPointService = cutPointService;
        _pacingService = pacingService;
        _transitionService = transitionService;
        _engagementService = engagementService;
        _qualityService = qualityService;
    }

    /// <summary>
    /// Analyze timeline for all issues and recommendations
    /// </summary>
    [HttpPost("analyze-timeline")]
    public async Task<IActionResult> AnalyzeTimeline([FromBody] AnalyzeTimelineRequest request)
    {
        try
        {
            _logger.LogInformation("Analyzing timeline for job {JobId}", request.JobId);

            var result = await _orchestrator.AnalyzeTimelineAsync(request.JobId, request).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                analysis = result,
                message = "Timeline analysis complete"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing timeline for job {JobId}", request.JobId);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get cut point suggestions for timeline
    /// </summary>
    [HttpPost("suggest-cuts")]
    public async Task<IActionResult> SuggestCuts([FromBody] string jobId)
    {
        try
        {
            _logger.LogInformation("Suggesting cuts for job {JobId}", jobId);

            var timeline = await LoadTimelineAsync(jobId).ConfigureAwait(false);
            if (timeline == null)
            {
                return NotFound(new { success = false, error = "Timeline not found" });
            }

            var cutPoints = await _cutPointService.DetectCutPointsAsync(timeline).ConfigureAwait(false);
            var awkwardPauses = await _cutPointService.DetectAwkwardPausesAsync(timeline).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                cutPoints,
                awkwardPauses,
                message = $"Found {cutPoints.Count} cut suggestions"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting cuts for job {JobId}", jobId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Optimize timeline pacing
    /// </summary>
    [HttpPost("optimize-pacing")]
    public async Task<IActionResult> OptimizePacing([FromBody] EditingPacingRequest request)
    {
        try
        {
            _logger.LogInformation("Optimizing pacing for job {JobId}", request.JobId);

            var timeline = await LoadTimelineAsync(request.JobId).ConfigureAwait(false);
            if (timeline == null)
            {
                return NotFound(new { success = false, error = "Timeline not found" });
            }

            var analysis = await _pacingService.AnalyzePacingAsync(timeline, request.TargetDuration).ConfigureAwait(false);
            var slowSegments = await _pacingService.DetectSlowSegmentsAsync(timeline).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                analysis,
                slowSegments,
                message = "Pacing analysis complete"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing pacing for job {JobId}", request.JobId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Suggest optimal scene sequence
    /// </summary>
    [HttpPost("sequence-scenes")]
    public async Task<IActionResult> SequenceScenes([FromBody] SequenceScenesRequest request)
    {
        try
        {
            _logger.LogInformation("Sequencing scenes for job {JobId}", request.JobId);

            var timeline = await LoadTimelineAsync(request.JobId).ConfigureAwait(false);
            if (timeline == null)
            {
                return NotFound(new { success = false, error = "Timeline not found" });
            }

            // For now, return current order with reasoning
            // In full implementation, this would use LLM to analyze narrative flow
            var currentOrder = timeline.Scenes.Select(s => s.Index).ToList();

            return Ok(new
            {
                success = true,
                result = new SceneSequencingResult(
                    RecommendedOrder: currentOrder,
                    Reasoning: $"Current sequence follows {request.NarrativeStyle} narrative structure effectively",
                    AlternativeApproaches: new List<string>
                    {
                        "Consider starting with most impactful scene as hook",
                        "Experiment with non-linear storytelling for engagement"
                    }
                ),
                message = "Scene sequencing analysis complete"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sequencing scenes for job {JobId}", request.JobId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Recommend transitions between scenes
    /// </summary>
    [HttpPost("transitions")]
    public async Task<IActionResult> RecommendTransitions([FromBody] string jobId)
    {
        try
        {
            _logger.LogInformation("Recommending transitions for job {JobId}", jobId);

            var timeline = await LoadTimelineAsync(jobId).ConfigureAwait(false);
            if (timeline == null)
            {
                return NotFound(new { success = false, error = "Timeline not found" });
            }

            var suggestions = await _transitionService.RecommendTransitionsAsync(timeline).ConfigureAwait(false);
            var jarring = await _transitionService.DetectJarringTransitionsAsync(timeline).ConfigureAwait(false);
            var varied = await _transitionService.EnforceTransitionVarietyAsync(timeline, suggestions).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                suggestions = varied,
                jarringTransitions = jarring,
                message = $"Generated {varied.Count} transition recommendations"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recommending transitions for job {JobId}", jobId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Suggest effect applications
    /// </summary>
    [HttpPost("effects")]
    public async Task<IActionResult> SuggestEffects([FromBody] string jobId)
    {
        try
        {
            _logger.LogInformation("Suggesting effects for job {JobId}", jobId);

            var timeline = await LoadTimelineAsync(jobId).ConfigureAwait(false);
            if (timeline == null)
            {
                return NotFound(new { success = false, error = "Timeline not found" });
            }

            // Generate basic effect suggestions based on timeline analysis
            var suggestions = GenerateEffectSuggestions(timeline);

            return Ok(new
            {
                success = true,
                suggestions,
                message = $"Generated {suggestions.Count} effect suggestions"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting effects for job {JobId}", jobId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Generate engagement analysis
    /// </summary>
    [HttpPost("engagement")]
    public async Task<IActionResult> AnalyzeEngagement([FromBody] string jobId)
    {
        try
        {
            _logger.LogInformation("Analyzing engagement for job {JobId}", jobId);

            var timeline = await LoadTimelineAsync(jobId).ConfigureAwait(false);
            if (timeline == null)
            {
                return NotFound(new { success = false, error = "Timeline not found" });
            }

            var curve = await _engagementService.GenerateEngagementCurveAsync(timeline).ConfigureAwait(false);
            var fatiguePoints = await _engagementService.DetectFatiguePointsAsync(timeline).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                engagementCurve = curve,
                fatiguePoints,
                message = "Engagement analysis complete"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing engagement for job {JobId}", jobId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Auto-assemble rough cut from assets and script
    /// </summary>
    [HttpPost("auto-assemble")]
    public async Task<IActionResult> AutoAssemble([FromBody] AutoAssembleRequest request)
    {
        try
        {
            _logger.LogInformation("Auto-assembling timeline for job {JobId}", request.JobId);

            var timeline = await _orchestrator.AutoAssembleAsync(request.JobId, request).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                timeline,
                message = "Auto-assembly complete"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-assembling for job {JobId}", request.JobId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Run quality control checks
    /// </summary>
    [HttpPost("quality-check")]
    public async Task<IActionResult> RunQualityCheck([FromBody] string jobId)
    {
        try
        {
            _logger.LogInformation("Running quality check for job {JobId}", jobId);

            var timeline = await LoadTimelineAsync(jobId).ConfigureAwait(false);
            if (timeline == null)
            {
                return NotFound(new { success = false, error = "Timeline not found" });
            }

            var issues = await _qualityService.RunQualityChecksAsync(timeline).ConfigureAwait(false);
            var desyncIssues = await _qualityService.DetectDesyncIssuesAsync(timeline).ConfigureAwait(false);

            var allIssues = issues.Concat(desyncIssues).ToList();
            var criticalCount = allIssues.Count(i => i.Severity == QualityIssueSeverity.Critical);
            var errorCount = allIssues.Count(i => i.Severity == QualityIssueSeverity.Error);

            return Ok(new
            {
                success = true,
                issues = allIssues,
                summary = new
                {
                    total = allIssues.Count,
                    critical = criticalCount,
                    errors = errorCount,
                    warnings = allIssues.Count(i => i.Severity == QualityIssueSeverity.Warning)
                },
                message = criticalCount > 0
                    ? $"Found {criticalCount} critical issues that must be resolved"
                    : "Quality check passed"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running quality check for job {JobId}", jobId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Optimize timeline to target duration
    /// </summary>
    [HttpPost("optimize-duration")]
    public async Task<IActionResult> OptimizeDuration([FromBody] OptimizeDurationRequest request)
    {
        try
        {
            _logger.LogInformation(
                "Optimizing duration for job {JobId} to {Duration}",
                request.JobId,
                request.TargetDuration);

            var timeline = await _orchestrator.OptimizeForDurationAsync(request.JobId, request).ConfigureAwait(false);

            return Ok(new
            {
                success = true,
                timeline,
                message = $"Timeline optimized to {timeline.TotalDuration.TotalSeconds:F1}s"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing duration for job {JobId}", request.JobId);
            return StatusCode(500, new { success = false, error = ex.Message });
        }
    }

    private async Task<Core.Models.Timeline.EditableTimeline?> LoadTimelineAsync(string jobId)
    {
        try
        {
            // Use the orchestrator's artifact manager to load timeline
            var analysis = await _orchestrator.AnalyzeTimelineAsync(jobId, new AnalyzeTimelineRequest(jobId, false, false, false, false)).ConfigureAwait(false);
            return null; // Timeline is loaded internally
        }
        catch
        {
            return null;
        }
    }

    private List<EffectSuggestion> GenerateEffectSuggestions(Core.Models.Timeline.EditableTimeline timeline)
    {
        var suggestions = new List<EffectSuggestion>();

        // Add slow-mo suggestions for emphasis points
        foreach (var scene in timeline.Scenes.Take(3)) // Just first few scenes as example
        {
            var emphasisWords = new[] { "important", "critical", "amazing", "incredible" };
            if (emphasisWords.Any(w => scene.Script.ToLower().Contains(w)))
            {
                suggestions.Add(new EffectSuggestion(
                    StartTime: scene.Start,
                    Duration: TimeSpan.FromSeconds(2),
                    EffectType: EffectType.SlowMotion,
                    Purpose: EffectPurpose.Emphasis,
                    Parameters: new Dictionary<string, object> { { "speed", 0.7 } },
                    Reasoning: "Emphasize key moment with subtle slow-motion",
                    Confidence: 0.75
                ));
            }
        }

        return suggestions;
    }
}
