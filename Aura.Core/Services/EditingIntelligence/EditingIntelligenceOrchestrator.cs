using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Models;
using Aura.Core.Models.EditingIntelligence;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.EditingIntelligence;

/// <summary>
/// Orchestrates all editing intelligence services for comprehensive timeline analysis and optimization
/// </summary>
public class EditingIntelligenceOrchestrator
{
    private readonly ILogger<EditingIntelligenceOrchestrator> _logger;
    private readonly CutPointDetectionService _cutPointService;
    private readonly PacingOptimizationService _pacingService;
    private readonly TransitionRecommendationService _transitionService;
    private readonly EngagementOptimizationService _engagementService;
    private readonly QualityControlService _qualityService;
    private readonly ArtifactManager _artifactManager;

    public EditingIntelligenceOrchestrator(
        ILogger<EditingIntelligenceOrchestrator> logger,
        CutPointDetectionService cutPointService,
        PacingOptimizationService pacingService,
        TransitionRecommendationService transitionService,
        EngagementOptimizationService engagementService,
        QualityControlService qualityService,
        ArtifactManager artifactManager)
    {
        _logger = logger;
        _cutPointService = cutPointService;
        _pacingService = pacingService;
        _transitionService = transitionService;
        _engagementService = engagementService;
        _qualityService = qualityService;
        _artifactManager = artifactManager;
    }

    /// <summary>
    /// Perform comprehensive timeline analysis
    /// </summary>
    public async Task<TimelineAnalysisResult> AnalyzeTimelineAsync(
        string jobId,
        AnalyzeTimelineRequest request)
    {
        _logger.LogInformation("Starting comprehensive timeline analysis for job {JobId}", jobId);

        // Load timeline
        var timeline = await LoadTimelineAsync(jobId);
        if (timeline == null)
        {
            throw new InvalidOperationException($"Timeline not found for job {jobId}");
        }

        IReadOnlyList<CutPoint>? cutPoints = null;
        PacingAnalysis? pacingAnalysis = null;
        EngagementCurve? engagementAnalysis = null;
        IReadOnlyList<QualityIssue>? qualityIssues = null;

        // Run requested analyses
        if (request.IncludeCutPoints)
        {
            _logger.LogInformation("Analyzing cut points");
            cutPoints = await _cutPointService.DetectCutPointsAsync(timeline);
        }

        if (request.IncludePacing)
        {
            _logger.LogInformation("Analyzing pacing");
            pacingAnalysis = await _pacingService.AnalyzePacingAsync(timeline);
        }

        if (request.IncludeEngagement)
        {
            _logger.LogInformation("Analyzing engagement");
            engagementAnalysis = await _engagementService.GenerateEngagementCurveAsync(timeline);
        }

        if (request.IncludeQuality)
        {
            _logger.LogInformation("Running quality checks");
            qualityIssues = await _qualityService.RunQualityChecksAsync(timeline);
        }

        // Generate general recommendations
        var recommendations = GenerateGeneralRecommendations(
            cutPoints,
            pacingAnalysis,
            engagementAnalysis,
            qualityIssues
        );

        return new TimelineAnalysisResult(
            CutPoints: cutPoints,
            PacingAnalysis: pacingAnalysis,
            EngagementAnalysis: engagementAnalysis,
            QualityIssues: qualityIssues,
            GeneralRecommendations: recommendations
        );
    }

    /// <summary>
    /// Optimize timeline for target duration
    /// </summary>
    public async Task<EditableTimeline> OptimizeForDurationAsync(
        string jobId,
        OptimizeDurationRequest request)
    {
        _logger.LogInformation(
            "Optimizing timeline for target duration {Duration} using strategy {Strategy}",
            request.TargetDuration,
            request.Strategy);

        var timeline = await LoadTimelineAsync(jobId);
        if (timeline == null)
        {
            throw new InvalidOperationException($"Timeline not found for job {jobId}");
        }

        var optimized = await _pacingService.OptimizeForDurationAsync(
            timeline,
            request.TargetDuration,
            request.Strategy
        );

        // Save optimized timeline
        await SaveTimelineAsync(jobId, optimized);

        return optimized;
    }

    /// <summary>
    /// Auto-assemble rough cut from job context
    /// </summary>
    public async Task<EditableTimeline> AutoAssembleAsync(
        string jobId,
        AutoAssembleRequest request)
    {
        _logger.LogInformation("Auto-assembling timeline for job {JobId}", jobId);

        // Load existing timeline or create new one
        var timeline = await LoadTimelineAsync(jobId) ?? new EditableTimeline();

        // If timeline already has scenes, use them
        if (timeline.Scenes.Count == 0)
        {
            _logger.LogInformation("No existing timeline, would need to generate from script");
            // In a full implementation, this would:
            // 1. Load script from job
            // 2. Match assets to script segments
            // 3. Calculate optimal timing
            // 4. Assemble scenes
            throw new InvalidOperationException("Auto-assembly from scratch not yet implemented");
        }

        // Optimize existing timeline
        if (request.TargetDuration.HasValue)
        {
            timeline = await _pacingService.OptimizeForDurationAsync(
                timeline,
                request.TargetDuration.Value,
                request.EditingStyle ?? "balanced"
            );
        }

        // Apply transitions
        var transitions = await _transitionService.RecommendTransitionsAsync(timeline);
        timeline = ApplyTransitions(timeline, transitions);

        // Save assembled timeline
        await SaveTimelineAsync(jobId, timeline);

        return timeline;
    }

    private EditableTimeline ApplyTransitions(
        EditableTimeline timeline,
        IReadOnlyList<TransitionSuggestion> suggestions)
    {
        var updated = new EditableTimeline
        {
            BackgroundMusicPath = timeline.BackgroundMusicPath,
            Subtitles = timeline.Subtitles
        };

        foreach (var scene in timeline.Scenes)
        {
            var transitionSuggestion = suggestions
                .FirstOrDefault(s => s.FromSceneIndex == scene.Index);

            var transitionType = transitionSuggestion?.Type.ToString() ?? "None";
            var transitionDuration = transitionSuggestion?.Duration;

            updated.AddScene(new TimelineScene(
                Index: scene.Index,
                Heading: scene.Heading,
                Script: scene.Script,
                Start: scene.Start,
                Duration: scene.Duration,
                NarrationAudioPath: scene.NarrationAudioPath,
                VisualAssets: scene.VisualAssets,
                TransitionType: transitionType,
                TransitionDuration: transitionDuration
            ));
        }

        return updated;
    }

    private List<string> GenerateGeneralRecommendations(
        IReadOnlyList<CutPoint>? cutPoints,
        PacingAnalysis? pacing,
        EngagementCurve? engagement,
        IReadOnlyList<QualityIssue>? quality)
    {
        var recommendations = new List<string>();

        if (cutPoints?.Any() == true)
        {
            var highConfidenceCuts = cutPoints.Count(c => c.Confidence > 0.8);
            if (highConfidenceCuts > 0)
            {
                recommendations.Add($"Consider applying {highConfidenceCuts} high-confidence cut suggestions for tighter editing");
            }
        }

        if (pacing != null)
        {
            if (pacing.OverallEngagementScore < 0.6)
            {
                recommendations.Add("Overall pacing could be improved - review scene duration recommendations");
            }
            else if (pacing.OverallEngagementScore > 0.85)
            {
                recommendations.Add("Excellent pacing! Minor refinements only");
            }
        }

        if (engagement != null)
        {
            if (engagement.HookStrength < 0.6)
            {
                recommendations.Add("Opening hook needs strengthening - first 10 seconds are critical for retention");
            }

            if (engagement.RetentionRisks.Count > 3)
            {
                recommendations.Add($"Address {engagement.RetentionRisks.Count} retention risk points with visual variety or pacing changes");
            }
        }

        if (quality?.Any() == true)
        {
            var criticalIssues = quality.Count(q => q.Severity == QualityIssueSeverity.Critical);
            if (criticalIssues > 0)
            {
                recommendations.Add($"⚠️ {criticalIssues} critical quality issues must be resolved before rendering");
            }
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Timeline looks great! Ready for final review and rendering.");
        }

        return recommendations;
    }

    private async Task<EditableTimeline?> LoadTimelineAsync(string jobId)
    {
        try
        {
            var jobDir = _artifactManager.GetJobDirectory(jobId);
            var timelinePath = System.IO.Path.Combine(jobDir, "timeline.json");
            if (!System.IO.File.Exists(timelinePath))
            {
                _logger.LogWarning("Timeline file not found: {Path}", timelinePath);
                return null;
            }

            var json = await System.IO.File.ReadAllTextAsync(timelinePath);
            var timeline = System.Text.Json.JsonSerializer.Deserialize<EditableTimeline>(json);
            return timeline;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading timeline for job {JobId}", jobId);
            return null;
        }
    }

    private async Task SaveTimelineAsync(string jobId, EditableTimeline timeline)
    {
        try
        {
            var jobDir = _artifactManager.GetJobDirectory(jobId);
            var timelinePath = System.IO.Path.Combine(jobDir, "timeline.json");
            var json = System.Text.Json.JsonSerializer.Serialize(timeline, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            await System.IO.File.WriteAllTextAsync(timelinePath, json);
            _logger.LogInformation("Saved timeline for job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving timeline for job {JobId}", jobId);
            throw;
        }
    }
}
