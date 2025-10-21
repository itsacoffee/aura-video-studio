using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Pacing;

/// <summary>
/// Analyzes video content and provides pacing optimization recommendations.
/// Uses ML-based techniques to determine optimal scene durations and transitions.
/// </summary>
public class PacingAnalyzer
{
    private readonly ILogger<PacingAnalyzer> _logger;
    private readonly RhythmDetector _rhythmDetector;
    private readonly RetentionOptimizer _retentionOptimizer;

    public PacingAnalyzer(
        ILogger<PacingAnalyzer> logger,
        RhythmDetector rhythmDetector,
        RetentionOptimizer retentionOptimizer)
    {
        _logger = logger;
        _rhythmDetector = rhythmDetector;
        _retentionOptimizer = retentionOptimizer;
    }

    /// <summary>
    /// Analyzes scenes and provides comprehensive pacing recommendations.
    /// </summary>
    public async Task<PacingAnalysisResult> AnalyzePacingAsync(
        IReadOnlyList<Scene> scenes,
        string? audioPath,
        VideoFormat format,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting pacing analysis for {SceneCount} scenes with format {Format}", 
            scenes.Count, format);

        try
        {
            // Get content template for this format
            var template = GetContentTemplate(format);
            _logger.LogDebug("Using template: {TemplateName}", template.Name);

            // Analyze rhythm if audio is available
            RhythmAnalysis? rhythmAnalysis = null;
            if (!string.IsNullOrEmpty(audioPath))
            {
                rhythmAnalysis = await _rhythmDetector.DetectRhythmAsync(audioPath, ct).ConfigureAwait(false);
                _logger.LogDebug("Rhythm analysis complete. Score: {Score}", rhythmAnalysis.OverallRhythmScore);
            }

            // Analyze scene complexity and importance
            var sceneRecommendations = AnalyzeScenes(scenes, template.Parameters, rhythmAnalysis);
            
            // Calculate optimal total duration
            var optimalDuration = CalculateOptimalDuration(sceneRecommendations, template.Parameters);
            
            // Detect natural transition points
            var transitions = DetectTransitionPoints(scenes, rhythmAnalysis);
            
            // Assess narrative arc
            var narrativeAssessment = AssessNarrativeArc(scenes);
            
            // Calculate engagement score
            var engagementScore = CalculateEngagementScore(sceneRecommendations, transitions, narrativeAssessment);
            
            // Generate warnings
            var warnings = GenerateWarnings(sceneRecommendations, template.Parameters);

            var result = new PacingAnalysisResult(
                optimalDuration,
                engagementScore,
                sceneRecommendations,
                transitions,
                narrativeAssessment,
                warnings
            );

            _logger.LogInformation("Pacing analysis complete. Engagement score: {Score:F2}, Optimal duration: {Duration}", 
                engagementScore, optimalDuration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pacing analysis");
            throw;
        }
    }

    /// <summary>
    /// Analyzes individual scenes for pacing optimization.
    /// </summary>
    private List<ScenePacingRecommendation> AnalyzeScenes(
        IReadOnlyList<Scene> scenes,
        PacingParameters parameters,
        RhythmAnalysis? rhythmAnalysis)
    {
        var recommendations = new List<ScenePacingRecommendation>();

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            
            // Calculate complexity based on word count and content
            var complexity = CalculateComplexity(scene.Script);
            
            // Calculate importance (first and last scenes are more important)
            var importance = CalculateImportance(i, scenes.Count);
            
            // Determine recommended duration based on complexity and importance
            var recommendedDuration = CalculateRecommendedDuration(
                complexity,
                importance,
                parameters,
                i == 0 // First scene (hook)
            );
            
            var reasoning = GenerateReasoning(complexity, importance, recommendedDuration, scene.Duration);

            recommendations.Add(new ScenePacingRecommendation(
                i,
                scene.Duration,
                recommendedDuration,
                importance,
                complexity,
                reasoning
            ));
        }

        return recommendations;
    }

    /// <summary>
    /// Calculates content complexity based on script analysis.
    /// </summary>
    private double CalculateComplexity(string script)
    {
        if (string.IsNullOrWhiteSpace(script))
            return 0.3;

        var wordCount = script.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        
        // Base complexity on word count
        var baseComplexity = Math.Min(wordCount / 100.0, 1.0);
        
        // Adjust for technical terms (simple heuristic)
        var technicalWords = new[] { "algorithm", "function", "process", "system", "method", "implementation" };
        var technicalCount = technicalWords.Count(word => script.Contains(word, StringComparison.OrdinalIgnoreCase));
        var technicalAdjustment = Math.Min(technicalCount * 0.1, 0.3);
        
        return Math.Min(baseComplexity + technicalAdjustment, 1.0);
    }

    /// <summary>
    /// Calculates scene importance based on position in narrative.
    /// </summary>
    private double CalculateImportance(int sceneIndex, int totalScenes)
    {
        if (totalScenes <= 1)
            return 1.0;

        // First scene (hook) is most important
        if (sceneIndex == 0)
            return 1.0;

        // Last scene (conclusion) is also important
        if (sceneIndex == totalScenes - 1)
            return 0.9;

        // Middle scenes have lower importance
        var normalizedPosition = (double)sceneIndex / (totalScenes - 1);
        
        // Create a slight curve favoring beginning and end
        return 0.5 + (0.3 * Math.Cos(Math.PI * normalizedPosition));
    }

    /// <summary>
    /// Calculates recommended duration based on complexity and importance.
    /// </summary>
    private TimeSpan CalculateRecommendedDuration(
        double complexity,
        double importance,
        PacingParameters parameters,
        bool isHook)
    {
        // Hook should be short and punchy
        if (isHook)
        {
            return TimeSpan.FromSeconds(parameters.HookDuration);
        }

        // Base duration from complexity
        var baseDuration = parameters.MinSceneDuration + 
            (complexity * (parameters.MaxSceneDuration - parameters.MinSceneDuration));

        // Adjust by importance
        var adjustedDuration = baseDuration * (0.8 + (importance * 0.4));

        return TimeSpan.FromSeconds(Math.Max(parameters.MinSceneDuration, 
            Math.Min(parameters.MaxSceneDuration, adjustedDuration)));
    }

    /// <summary>
    /// Generates human-readable reasoning for pacing recommendation.
    /// </summary>
    private string GenerateReasoning(double complexity, double importance, TimeSpan recommended, TimeSpan current)
    {
        var parts = new List<string>();

        if (complexity > 0.7)
            parts.Add("High complexity content needs more time");
        else if (complexity < 0.3)
            parts.Add("Simple content can be paced faster");

        if (importance > 0.8)
            parts.Add("Critical for viewer engagement");

        var difference = (recommended - current).TotalSeconds;
        if (Math.Abs(difference) > 2)
        {
            if (difference > 0)
                parts.Add($"Extend by {difference:F1}s for better comprehension");
            else
                parts.Add($"Shorten by {Math.Abs(difference):F1}s to maintain interest");
        }
        else
        {
            parts.Add("Duration is optimal");
        }

        return string.Join(". ", parts);
    }

    /// <summary>
    /// Detects natural transition points based on content and rhythm.
    /// </summary>
    private List<TransitionPoint> DetectTransitionPoints(
        IReadOnlyList<Scene> scenes,
        RhythmAnalysis? rhythmAnalysis)
    {
        var transitions = new List<TransitionPoint>();

        // Add scene boundaries as natural transitions
        var currentTime = TimeSpan.Zero;
        foreach (var scene in scenes)
        {
            transitions.Add(new TransitionPoint(
                currentTime,
                TransitionType.SceneChange,
                0.95,
                $"Scene boundary: {scene.Heading}"
            ));

            currentTime += scene.Duration;
        }

        // Add music-based transitions if rhythm analysis available
        if (rhythmAnalysis != null && rhythmAnalysis.BeatPoints.Count > 0)
        {
            // Add strong beat points as potential transitions
            foreach (var beat in rhythmAnalysis.BeatPoints.Where(b => b.Strength > 0.7))
            {
                transitions.Add(new TransitionPoint(
                    beat.Timestamp,
                    TransitionType.MusicBeat,
                    beat.Strength,
                    $"Strong musical beat (tempo: {beat.Tempo})"
                ));
            }
        }

        return transitions.OrderBy(t => t.Timestamp).ToList();
    }

    /// <summary>
    /// Assesses the narrative arc structure (hook, buildup, payoff).
    /// </summary>
    private string AssessNarrativeArc(IReadOnlyList<Scene> scenes)
    {
        if (scenes.Count == 0)
            return "No scenes to analyze";

        if (scenes.Count == 1)
            return "Single scene: Ensure hook and payoff are present";

        var assessments = new List<string>();

        // Check hook (first scene)
        if (scenes[0].Duration.TotalSeconds < 20)
            assessments.Add("✓ Strong hook: First scene is concise");
        else
            assessments.Add("⚠ Hook could be shorter for better engagement");

        // Check buildup (middle sections)
        var middleScenes = scenes.Skip(1).Take(Math.Max(0, scenes.Count - 2)).ToList();
        if (middleScenes.Any())
        {
            var avgMiddleDuration = middleScenes.Average(s => s.Duration.TotalSeconds);
            if (avgMiddleDuration >= 15 && avgMiddleDuration <= 30)
                assessments.Add("✓ Good buildup pacing");
            else
                assessments.Add("⚠ Buildup sections may benefit from adjustment");
        }

        // Check payoff (last scene)
        var lastScene = scenes[^1];
        if (lastScene.Duration.TotalSeconds >= 10)
            assessments.Add("✓ Adequate payoff duration");
        else
            assessments.Add("⚠ Payoff may feel rushed");

        return string.Join(". ", assessments);
    }

    /// <summary>
    /// Calculates overall engagement score.
    /// </summary>
    private double CalculateEngagementScore(
        IReadOnlyList<ScenePacingRecommendation> recommendations,
        IReadOnlyList<TransitionPoint> transitions,
        string narrativeAssessment)
    {
        var score = 0.0;

        // Scene pacing variety (avoid monotony)
        var durationVariance = CalculateVariance(recommendations.Select(r => r.RecommendedDuration.TotalSeconds));
        score += Math.Min(durationVariance / 10.0, 0.3) * 100;

        // Transition density (good pacing has regular transitions)
        var avgSceneDuration = recommendations.Average(r => r.RecommendedDuration.TotalSeconds);
        var transitionDensity = transitions.Count / Math.Max(avgSceneDuration, 1.0);
        score += Math.Min(transitionDensity * 5, 0.3) * 100;

        // Narrative structure quality
        var narrativeBonus = narrativeAssessment.Contains("✓") ? 0.4 : 0.2;
        score += narrativeBonus * 100;

        return Math.Min(score, 100.0);
    }

    /// <summary>
    /// Calculates statistical variance.
    /// </summary>
    private double CalculateVariance(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count < 2)
            return 0;

        var mean = valuesList.Average();
        var squaredDiffs = valuesList.Select(v => Math.Pow(v - mean, 2));
        return Math.Sqrt(squaredDiffs.Average());
    }

    /// <summary>
    /// Calculates optimal total duration based on recommendations.
    /// </summary>
    private TimeSpan CalculateOptimalDuration(
        IReadOnlyList<ScenePacingRecommendation> recommendations,
        PacingParameters parameters)
    {
        var totalSeconds = recommendations.Sum(r => r.RecommendedDuration.TotalSeconds);
        return TimeSpan.FromSeconds(totalSeconds);
    }

    /// <summary>
    /// Generates warnings about pacing issues.
    /// </summary>
    private List<string> GenerateWarnings(
        IReadOnlyList<ScenePacingRecommendation> recommendations,
        PacingParameters parameters)
    {
        var warnings = new List<string>();

        // Check for overly long scenes
        var longScenes = recommendations.Where(r => r.RecommendedDuration.TotalSeconds > parameters.MaxSceneDuration).ToList();
        if (longScenes.Any())
        {
            warnings.Add($"{longScenes.Count} scene(s) exceed maximum duration - consider breaking into smaller segments");
        }

        // Check for monotonous pacing
        var variance = CalculateVariance(recommendations.Select(r => r.RecommendedDuration.TotalSeconds));
        if (variance < 2)
        {
            warnings.Add("Low pacing variation detected - consider varying scene durations for better engagement");
        }

        // Check for weak hook
        if (recommendations.Count > 0 && recommendations[0].RecommendedDuration.TotalSeconds > 20)
        {
            warnings.Add("First scene (hook) is long - consider shortening to capture attention quickly");
        }

        return warnings;
    }

    /// <summary>
    /// Gets the content template for a specific video format.
    /// </summary>
    private ContentTemplate GetContentTemplate(VideoFormat format)
    {
        return format switch
        {
            VideoFormat.Explainer => new ContentTemplate(
                "Explainer Video",
                "Clear, concise explanations with visual support",
                VideoFormat.Explainer,
                new PacingParameters(8, 25, 15, 0.6, 10, true)
            ),
            VideoFormat.Tutorial => new ContentTemplate(
                "Tutorial Video",
                "Step-by-step instructional content",
                VideoFormat.Tutorial,
                new PacingParameters(15, 40, 25, 0.4, 12, false)
            ),
            VideoFormat.Vlog => new ContentTemplate(
                "Vlog",
                "Personal, narrative-driven content",
                VideoFormat.Vlog,
                new PacingParameters(5, 20, 12, 0.8, 8, true)
            ),
            VideoFormat.Review => new ContentTemplate(
                "Review Video",
                "Product or service evaluation",
                VideoFormat.Review,
                new PacingParameters(10, 30, 18, 0.5, 10, true)
            ),
            VideoFormat.Educational => new ContentTemplate(
                "Educational Content",
                "In-depth learning material",
                VideoFormat.Educational,
                new PacingParameters(20, 60, 35, 0.3, 15, false)
            ),
            VideoFormat.Entertainment => new ContentTemplate(
                "Entertainment",
                "Engaging, fast-paced content",
                VideoFormat.Entertainment,
                new PacingParameters(3, 15, 8, 0.9, 5, true)
            ),
            _ => new ContentTemplate(
                "General Content",
                "Default pacing template",
                format,
                new PacingParameters(10, 30, 18, 0.5, 10, true)
            )
        };
    }
}
