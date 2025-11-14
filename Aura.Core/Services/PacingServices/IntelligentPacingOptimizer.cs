using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.ML.Models;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PacingServices;

/// <summary>
/// Orchestrates ML-powered pacing optimization for video scenes
/// Combines LLM analysis, ML predictions, and heuristics for optimal timing
/// </summary>
public class IntelligentPacingOptimizer
{
    private readonly ILogger<IntelligentPacingOptimizer> _logger;
    private readonly SceneImportanceAnalyzer _sceneAnalyzer;
    private readonly AttentionCurvePredictor _attentionPredictor;
    private readonly TransitionRecommender _transitionRecommender;
    private readonly EmotionalBeatAnalyzer _emotionalBeatAnalyzer;
    private readonly SceneRelationshipMapper _relationshipMapper;
    private readonly ContentComplexityAnalyzer _complexityAnalyzer;
    private readonly FrameImportanceModel? _frameModel;

    // Pacing calculation constants
    private const double BaseWordsPerMinute = 150.0;
    private const double ComplexityFactor = 0.3;
    private const double ImportanceFactor = 0.2;
    private const double MinDurationMultiplier = 0.7;
    private const double MaxDurationMultiplier = 1.3;

    public IntelligentPacingOptimizer(
        ILogger<IntelligentPacingOptimizer> logger,
        SceneImportanceAnalyzer sceneAnalyzer,
        AttentionCurvePredictor attentionPredictor,
        TransitionRecommender transitionRecommender,
        EmotionalBeatAnalyzer emotionalBeatAnalyzer,
        SceneRelationshipMapper relationshipMapper,
        ContentComplexityAnalyzer complexityAnalyzer,
        FrameImportanceModel? frameModel = null)
    {
        _logger = logger;
        _sceneAnalyzer = sceneAnalyzer;
        _attentionPredictor = attentionPredictor;
        _transitionRecommender = transitionRecommender;
        _emotionalBeatAnalyzer = emotionalBeatAnalyzer;
        _relationshipMapper = relationshipMapper;
        _complexityAnalyzer = complexityAnalyzer;
        _frameModel = frameModel;
    }

    /// <summary>
    /// Performs comprehensive pacing analysis and optimization
    /// </summary>
    public async Task<PacingAnalysisResult> OptimizePacingAsync(
        IReadOnlyList<Scene> scenes,
        Brief brief,
        ILlmProvider? llmProvider = null,
        bool useAdaptivePacing = true,
        PacingProfile pacingProfile = PacingProfile.BalancedDocumentary,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting intelligent pacing optimization for {SceneCount} scenes", scenes.Count);

        var startTime = DateTime.UtcNow;
        string? llmProviderUsed = null;
        bool llmAnalysisSucceeded = false;

        try
        {
            // Step 1: Analyze scenes with LLM (if available)
            IReadOnlyList<SceneAnalysisData> sceneAnalyses;
            IReadOnlyList<ContentComplexityResult> complexityResults;
            
            if (llmProvider != null)
            {
                try
                {
                    llmProviderUsed = llmProvider.GetType().Name;
                    _logger.LogInformation("Using LLM provider: {Provider}, AdaptivePacing: {Adaptive}, Profile: {Profile}", 
                        llmProviderUsed, useAdaptivePacing, pacingProfile);
                    
                    sceneAnalyses = await _sceneAnalyzer.AnalyzeScenesAsync(
                        llmProvider, scenes, brief.Goal ?? "general video", ct).ConfigureAwait(false);
                    
                    llmAnalysisSucceeded = sceneAnalyses.Any(a => a.AnalyzedWithLlm);
                    _logger.LogInformation("LLM analysis: {SuccessCount}/{Total} scenes analyzed",
                        sceneAnalyses.Count(a => a.AnalyzedWithLlm), scenes.Count);

                    // Step 1b: Deep complexity analysis (if adaptive pacing enabled)
                    if (useAdaptivePacing)
                    {
                        _logger.LogInformation("Performing deep content complexity analysis");
                        complexityResults = await _complexityAnalyzer.AnalyzeComplexityBatchAsync(
                            llmProvider, scenes, brief.Goal ?? "general video", ct).ConfigureAwait(false);
                        
                        var avgComplexity = complexityResults.Average(r => r.OverallComplexityScore);
                        _logger.LogInformation("Complexity analysis complete. Average complexity: {AvgComplexity:F1}, " +
                            "LLM-analyzed: {LlmCount}/{Total}",
                            avgComplexity, complexityResults.Count(r => r.AnalyzedWithLlm), scenes.Count);
                    }
                    else
                    {
                        _logger.LogInformation("Adaptive pacing disabled, using standard analysis");
                        complexityResults = new List<ContentComplexityResult>();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "LLM analysis failed, falling back to heuristics");
                    sceneAnalyses = CreateFallbackAnalyses(scenes);
                    complexityResults = new List<ContentComplexityResult>();
                }
            }
            else
            {
                _logger.LogInformation("No LLM provider available, using heuristic analysis");
                sceneAnalyses = CreateFallbackAnalyses(scenes);
                complexityResults = new List<ContentComplexityResult>();
            }

            // Step 2: Calculate optimal timings using ML, LLM data, and complexity analysis
            var timingSuggestions = await CalculateOptimalTimingsAsync(
                scenes, sceneAnalyses, complexityResults, brief, useAdaptivePacing, pacingProfile, ct).ConfigureAwait(false);

            // Step 3: Generate attention curve predictions
            var attentionCurve = await _attentionPredictor.GenerateAttentionCurveAsync(
                scenes, timingSuggestions, ct).ConfigureAwait(false);

            // Step 4: Analyze transitions between scenes
            var transitionRecommendations = await _transitionRecommender.RecommendTransitionsAsync(
                scenes, sceneAnalyses, brief, ct).ConfigureAwait(false);

            // Step 5: Analyze emotional beats
            var emotionalBeats = await _emotionalBeatAnalyzer.AnalyzeEmotionalBeatsAsync(
                scenes, llmProvider, ct).ConfigureAwait(false);

            // Step 6: Map scene relationships
            var sceneRelationships = await _relationshipMapper.MapRelationshipsAsync(scenes, ct).ConfigureAwait(false);

            // Step 7: Calculate confidence and metrics
            var confidenceScore = CalculateConfidenceScore(sceneAnalyses, llmAnalysisSucceeded);
            var optimalDuration = TimeSpan.FromSeconds(
                timingSuggestions.Sum(s => s.OptimalDuration.TotalSeconds));
            var warnings = GenerateWarnings(timingSuggestions, scenes, sceneRelationships);

            var result = new PacingAnalysisResult
            {
                TimingSuggestions = timingSuggestions,
                AttentionCurve = attentionCurve,
                ConfidenceScore = confidenceScore,
                PredictedRetentionRate = attentionCurve.OverallRetentionScore,
                OptimalDuration = optimalDuration,
                LlmProviderUsed = llmProviderUsed,
                LlmAnalysisSucceeded = llmAnalysisSucceeded,
                Warnings = warnings,
                TransitionRecommendations = transitionRecommendations,
                EmotionalBeats = emotionalBeats,
                SceneRelationships = sceneRelationships
            };

            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation("Pacing optimization complete in {Elapsed:F2}s. " +
                "Confidence: {Confidence:F1}%, Retention: {Retention:F1}%, Duration: {Duration}, " +
                "Transitions: {TransitionCount}, Emotional Peaks: {PeakCount}, Flow Issues: {IssueCount}",
                elapsed.TotalSeconds, confidenceScore, attentionCurve.OverallRetentionScore, optimalDuration,
                transitionRecommendations.Count, emotionalBeats.Count(b => b.IsPeak), sceneRelationships.FlowIssues.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pacing optimization");
            throw;
        }
    }

    /// <summary>
    /// Calculates optimal scene timings using pacing algorithm with adaptive complexity adjustments
    /// </summary>
    private async Task<IReadOnlyList<SceneTimingSuggestion>> CalculateOptimalTimingsAsync(
        IReadOnlyList<Scene> scenes,
        IReadOnlyList<SceneAnalysisData> analyses,
        IReadOnlyList<ContentComplexityResult> complexityResults,
        Brief brief,
        bool useAdaptivePacing,
        PacingProfile pacingProfile,
        CancellationToken ct)
    {
        _logger.LogDebug("Calculating optimal timings for {SceneCount} scenes", scenes.Count);

        var suggestions = new List<SceneTimingSuggestion>();

        // Get platform and audience multipliers
        var platformMultiplier = GetPlatformMultiplier(brief);
        var audienceMultiplier = GetAudienceMultiplier(brief);

        for (int i = 0; i < scenes.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var scene = scenes[i];
            var analysis = analyses.FirstOrDefault(a => a.SceneIndex == i);

            if (analysis == null)
            {
                _logger.LogWarning("No analysis found for scene {SceneIndex}, using defaults", i);
                analysis = CreateDefaultAnalysis(scene, scenes.Count);
            }

            // Get complexity analysis if available
            var complexityResult = complexityResults.FirstOrDefault(c => c.SceneIndex == i);
            
            // Calculate base optimal duration
            var optimal = await CalculateSceneOptimalDurationAsync(
                scene, analysis, platformMultiplier, audienceMultiplier, ct).ConfigureAwait(false);

            // Apply adaptive duration adjustment based on complexity
            double durationMultiplier = 1.0;
            string adjustmentReasoning = string.Empty;
            
            if (useAdaptivePacing && complexityResult != null)
            {
                durationMultiplier = _complexityAnalyzer.CalculateDurationMultiplier(complexityResult, pacingProfile);
                optimal = TimeSpan.FromSeconds(optimal.TotalSeconds * durationMultiplier);
                
                adjustmentReasoning = $"Adaptive pacing adjustment: {(durationMultiplier - 1.0) * 100:+0;-0}% " +
                    $"based on complexity score {complexityResult.OverallComplexityScore:F0}";
                
                _logger.LogDebug("Scene {SceneIndex}: Complexity={Complexity:F0}, Multiplier={Multiplier:F2}, " +
                    "Adjustment={Adjustment:+0;-0}%",
                    i, complexityResult.OverallComplexityScore, durationMultiplier, (durationMultiplier - 1.0) * 100);
            }

            var minDuration = TimeSpan.FromSeconds(optimal.TotalSeconds * MinDurationMultiplier);
            var maxDuration = TimeSpan.FromSeconds(optimal.TotalSeconds * MaxDurationMultiplier);

            var reasoning = analysis.Reasoning;
            if (!string.IsNullOrEmpty(adjustmentReasoning))
            {
                reasoning = $"{reasoning}. {adjustmentReasoning}";
            }

            suggestions.Add(new SceneTimingSuggestion
            {
                SceneIndex = i,
                CurrentDuration = scene.Duration,
                OptimalDuration = optimal,
                MinDuration = minDuration,
                MaxDuration = maxDuration,
                ImportanceScore = analysis.Importance,
                ComplexityScore = analysis.Complexity,
                EmotionalIntensity = analysis.EmotionalIntensity,
                InformationDensity = analysis.InformationDensity,
                TransitionType = analysis.TransitionType,
                Confidence = CalculateSuggestionConfidence(analysis, complexityResult),
                Reasoning = reasoning,
                UsedLlmAnalysis = analysis.AnalyzedWithLlm,
                ContentComplexityScore = complexityResult?.OverallComplexityScore ?? 0,
                CognitiveProcessingTime = complexityResult?.CognitiveProcessingTime ?? TimeSpan.Zero,
                DurationAdjustmentMultiplier = durationMultiplier,
                ComplexityBreakdown = complexityResult?.DetailedBreakdown ?? string.Empty
            });
        }

        _logger.LogDebug("Calculated {Count} timing suggestions", suggestions.Count);
        return suggestions;
    }

    /// <summary>
    /// Calculates optimal duration for a single scene using the spec's algorithm
    /// Base duration = word_count / words_per_minute
    /// Complexity factor = LLM_complexity_score * 0.3
    /// Importance factor = LLM_importance_score * 0.2
    /// Audience factor = audience_type_multiplier
    /// Platform factor = platform_multiplier
    /// Optimal_duration = base * (1 + complexity + importance + audience + platform)
    /// </summary>
    private async Task<TimeSpan> CalculateSceneOptimalDurationAsync(
        Scene scene,
        SceneAnalysisData analysis,
        double platformMultiplier,
        double audienceMultiplier,
        CancellationToken ct)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        // Calculate word count
        var wordCount = scene.Script.Split(new[] { ' ', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;

        // Base duration from word count and speaking rate
        var baseDurationSeconds = (wordCount / BaseWordsPerMinute) * 60.0;

        // Normalize scores to 0-1 range
        var complexityNorm = analysis.Complexity / 100.0;
        var importanceNorm = analysis.Importance / 100.0;

        // Apply the formula from spec
        var complexityAdjustment = complexityNorm * ComplexityFactor;
        var importanceAdjustment = importanceNorm * ImportanceFactor;
        var audienceAdjustment = audienceMultiplier;
        var platformAdjustment = platformMultiplier - 1.0; // Convert to adjustment factor

        var totalMultiplier = 1.0 + complexityAdjustment + importanceAdjustment + 
            audienceAdjustment + platformAdjustment;

        var optimalSeconds = baseDurationSeconds * totalMultiplier;

        // Ensure reasonable bounds (3-120 seconds per scene)
        optimalSeconds = Math.Clamp(optimalSeconds, 3.0, 120.0);

        return TimeSpan.FromSeconds(optimalSeconds);
    }

    private double GetPlatformMultiplier(Brief brief)
    {
        // Platform-specific multipliers from spec
        // YouTube: 1.0, TikTok: 0.7, Instagram: 0.8, etc.
        var aspect = brief.Aspect;
        
        return aspect switch
        {
            Aspect.Vertical9x16 => 0.7,  // TikTok/Shorts (faster pacing)
            Aspect.Square1x1 => 0.8,     // Instagram (moderate pacing)
            _ => 1.0                      // YouTube/Landscape (standard pacing)
        };
    }

    private double GetAudienceMultiplier(Brief brief)
    {
        // Audience-based adjustments
        var audience = brief.Audience?.ToLowerInvariant();
        
        if (audience == null)
            return 0.0;

        return audience switch
        {
            var a when a.Contains("expert") || a.Contains("professional") => 0.1,
            var a when a.Contains("beginner") || a.Contains("novice") => -0.1,
            var a when a.Contains("general") => 0.0,
            _ => 0.0
        };
    }

    private IReadOnlyList<SceneAnalysisData> CreateFallbackAnalyses(IReadOnlyList<Scene> scenes)
    {
        var analyses = new List<SceneAnalysisData>();
        
        for (int i = 0; i < scenes.Count; i++)
        {
            analyses.Add(CreateDefaultAnalysis(scenes[i], scenes.Count));
        }

        return analyses;
    }

    private SceneAnalysisData CreateDefaultAnalysis(Scene scene, int totalScenes)
    {
        var wordCount = scene.Script.Split(new[] { ' ', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;

        return new SceneAnalysisData
        {
            SceneIndex = scene.Index,
            Importance = scene.Index == 0 ? 85.0 : 50.0,
            Complexity = wordCount > 70 ? 70.0 : 50.0,
            EmotionalIntensity = 50.0,
            InformationDensity = wordCount > 100 ? InformationDensity.High : InformationDensity.Medium,
            OptimalDurationSeconds = (wordCount / 2.5),
            TransitionType = TransitionType.Fade,
            Reasoning = "Default heuristic analysis",
            AnalyzedWithLlm = false
        };
    }

    private double CalculateConfidenceScore(
        IReadOnlyList<SceneAnalysisData> analyses,
        bool llmAnalysisSucceeded)
    {
        var baseConfidence = 60.0;

        // Boost if LLM was used successfully
        if (llmAnalysisSucceeded)
        {
            var llmAnalysisCount = analyses.Count(a => a.AnalyzedWithLlm);
            var llmPercentage = llmAnalysisCount / (double)analyses.Count;
            baseConfidence += llmPercentage * 30.0; // Up to +30 for full LLM coverage
        }

        // Boost for consistent analysis
        if (analyses.Count > 0)
        {
            baseConfidence += 10.0;
        }

        return Math.Clamp(baseConfidence, 0, 100);
    }

    private double CalculateSuggestionConfidence(SceneAnalysisData analysis, ContentComplexityResult? complexity)
    {
        var confidence = 70.0;

        if (analysis.AnalyzedWithLlm)
        {
            confidence += 15.0;
        }

        if (complexity?.AnalyzedWithLlm == true)
        {
            confidence += 15.0; // Additional confidence from deep complexity analysis
        }

        return Math.Clamp(confidence, 0, 100);
    }

    private IReadOnlyList<string> GenerateWarnings(
        IReadOnlyList<SceneTimingSuggestion> suggestions,
        IReadOnlyList<Scene> scenes,
        SceneRelationshipGraph? sceneRelationships)
    {
        var warnings = new List<string>();

        // Check for scenes that need significant adjustment
        var needsAdjustment = suggestions.Where(s =>
        {
            var diff = Math.Abs((s.OptimalDuration - s.CurrentDuration).TotalSeconds);
            return diff > 3.0;
        }).ToList();

        if (needsAdjustment.Count > 0)
        {
            warnings.Add($"{needsAdjustment.Count} scene(s) need timing adjustments of 3+ seconds");
        }

        // Check total duration
        var totalOptimal = suggestions.Sum(s => s.OptimalDuration.TotalSeconds);
        var totalCurrent = scenes.Sum(s => s.Duration.TotalSeconds);
        var totalDiff = Math.Abs(totalOptimal - totalCurrent);

        if (totalDiff > 10.0)
        {
            warnings.Add($"Total video duration should change by {totalDiff:F0} seconds");
        }

        // Check for very long scenes
        var longScenes = suggestions.Where(s => s.OptimalDuration.TotalSeconds > 60).ToList();
        if (longScenes.Count > 0)
        {
            warnings.Add($"{longScenes.Count} scene(s) exceed 60 seconds - consider breaking into smaller segments");
        }

        // Add warnings for flow issues
        if (sceneRelationships != null)
        {
            var highSeverityIssues = sceneRelationships.FlowIssues.Count(i => i.Severity == "high");
            if (highSeverityIssues > 0)
            {
                warnings.Add($"{highSeverityIssues} high-severity flow issue(s) detected - scenes may confuse viewers");
            }

            if (sceneRelationships.ReorderingSuggestions.Count > 0)
            {
                warnings.Add($"{sceneRelationships.ReorderingSuggestions.Count} scene(s) might benefit from reordering");
            }
        }

        return warnings;
    }
}
