using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PacingServices;

/// <summary>
/// Analyzes content complexity and cognitive load using LLM-driven analysis
/// Evaluates concept difficulty, terminology density, prerequisite knowledge, and multi-step reasoning
/// </summary>
public class ContentComplexityAnalyzer
{
    private readonly ILogger<ContentComplexityAnalyzer> _logger;
    private readonly TimeSpan _llmTimeout = TimeSpan.FromSeconds(30);
    private readonly int _maxRetries = 2;

    public ContentComplexityAnalyzer(ILogger<ContentComplexityAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes content complexity for a single scene using LLM
    /// Returns a comprehensive complexity score (0-100) with detailed breakdown
    /// </summary>
    public async Task<ContentComplexityResult?> AnalyzeComplexityAsync(
        ILlmProvider llmProvider,
        Scene scene,
        Scene? previousScene,
        string videoGoal,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Analyzing content complexity for scene {SceneIndex} with LLM", scene.Index);

        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    _logger.LogDebug("Retry attempt {Attempt} for scene {SceneIndex} complexity analysis", attempt, scene.Index);
                    await Task.Delay(TimeSpan.FromSeconds(1 * attempt), ct).ConfigureAwait(false);
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(_llmTimeout);

                var result = await llmProvider.AnalyzeContentComplexityAsync(
                    scene.Script,
                    previousScene?.Script,
                    videoGoal,
                    cts.Token).ConfigureAwait(false);

                if (result != null)
                {
                    _logger.LogDebug("Scene {SceneIndex} complexity analysis: Score={Score}, " +
                        "ConceptDifficulty={ConceptDifficulty}, TerminologyDensity={TerminologyDensity}, " +
                        "PrerequisiteKnowledge={PrerequisiteKnowledge}, MultiStepReasoning={MultiStepReasoning}",
                        scene.Index, result.OverallComplexityScore, result.ConceptDifficulty, 
                        result.TerminologyDensity, result.PrerequisiteKnowledgeLevel, result.MultiStepReasoningRequired);

                    return new ContentComplexityResult
                    {
                        SceneIndex = scene.Index,
                        OverallComplexityScore = Math.Clamp(result.OverallComplexityScore, 0, 100),
                        ConceptDifficulty = Math.Clamp(result.ConceptDifficulty, 0, 100),
                        TerminologyDensity = Math.Clamp(result.TerminologyDensity, 0, 100),
                        PrerequisiteKnowledgeLevel = Math.Clamp(result.PrerequisiteKnowledgeLevel, 0, 100),
                        MultiStepReasoningRequired = Math.Clamp(result.MultiStepReasoningRequired, 0, 100),
                        NewConceptsIntroduced = Math.Max(0, result.NewConceptsIntroduced),
                        CognitiveProcessingTime = TimeSpan.FromSeconds(Math.Max(0, result.CognitiveProcessingTimeSeconds)),
                        OptimalAttentionWindow = TimeSpan.FromSeconds(Math.Max(3, result.OptimalAttentionWindowSeconds)),
                        DetailedBreakdown = result.DetailedBreakdown ?? string.Empty,
                        AnalyzedWithLlm = true
                    };
                }

                _logger.LogWarning("LLM returned null result for scene {SceneIndex} complexity analysis", scene.Index);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Complexity analysis cancelled for scene {SceneIndex}", scene.Index);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Complexity analysis timed out for scene {SceneIndex} (attempt {Attempt})", 
                    scene.Index, attempt + 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing complexity for scene {SceneIndex} (attempt {Attempt})", 
                    scene.Index, attempt + 1);
            }
        }

        _logger.LogWarning("Failed to analyze complexity for scene {SceneIndex} with LLM after {Attempts} attempts", 
            scene.Index, _maxRetries);
        return null;
    }

    /// <summary>
    /// Analyzes multiple scenes in batch for complexity
    /// </summary>
    public async Task<IReadOnlyList<ContentComplexityResult>> AnalyzeComplexityBatchAsync(
        ILlmProvider llmProvider,
        IReadOnlyList<Scene> scenes,
        string videoGoal,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing content complexity for {SceneCount} scenes with LLM provider", scenes.Count);

        var results = new List<ContentComplexityResult>();

        for (int i = 0; i < scenes.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var scene = scenes[i];
            var previousScene = i > 0 ? scenes[i - 1] : null;

            var complexity = await AnalyzeComplexityAsync(llmProvider, scene, previousScene, videoGoal, ct).ConfigureAwait(false);
            
            if (complexity != null)
            {
                results.Add(complexity);
            }
            else
            {
                _logger.LogDebug("Using fallback heuristic complexity analysis for scene {SceneIndex}", scene.Index);
                results.Add(CreateFallbackComplexityResult(scene, scenes.Count));
            }
        }

        _logger.LogInformation("Complexity analysis complete. {SuccessCount}/{TotalCount} scenes analyzed with LLM",
            results.Count(r => r.AnalyzedWithLlm), scenes.Count);

        return results;
    }

    /// <summary>
    /// Calculates duration adjustment multiplier based on complexity score
    /// Complex content (70-100): 30-50% more time
    /// Medium content (40-70): 0-30% adjustment
    /// Simple content (0-40): 20-30% less time
    /// </summary>
    public double CalculateDurationMultiplier(ContentComplexityResult complexity, PacingProfile profile)
    {
        var score = complexity.OverallComplexityScore;

        double baseMultiplier;
        
        if (score >= 70) // Complex technical concepts
        {
            baseMultiplier = 1.3 + (score - 70) * 0.02 / 3.0; // 1.3 to 1.5 (30-50% more)
        }
        else if (score >= 40) // Medium complexity
        {
            baseMultiplier = 1.0 + (score - 40) * 0.3 / 30.0; // 1.0 to 1.3
        }
        else // Simple transitions
        {
            baseMultiplier = 0.7 + (score / 40.0) * 0.3; // 0.7 to 1.0 (20-30% less)
        }

        // Apply profile adjustments
        baseMultiplier *= GetProfileMultiplier(profile);

        _logger.LogDebug("Duration multiplier for complexity score {Score} with profile {Profile}: {Multiplier:F2}",
            score, profile, baseMultiplier);

        return Math.Clamp(baseMultiplier, 0.5, 2.0);
    }

    /// <summary>
    /// Determines optimal attention window based on complexity
    /// Higher complexity requires longer attention windows
    /// </summary>
    public TimeSpan CalculateOptimalAttentionWindow(ContentComplexityResult complexity)
    {
        var score = complexity.OverallComplexityScore;
        
        var baseSeconds = score switch
        {
            >= 80 => 15.0, // Very complex: 15 seconds
            >= 60 => 12.0, // Complex: 12 seconds
            >= 40 => 10.0, // Medium: 10 seconds
            >= 20 => 8.0,  // Simple: 8 seconds
            _ => 5.0       // Very simple: 5 seconds
        };

        return TimeSpan.FromSeconds(baseSeconds);
    }

    /// <summary>
    /// Calculates new concepts per second (information density metric)
    /// </summary>
    public double CalculateConceptsPerSecond(ContentComplexityResult complexity, TimeSpan duration)
    {
        var newConceptCount = complexity.NewConceptsIntroduced;
        var durationSeconds = duration.TotalSeconds;

        if (durationSeconds <= 0)
            return 0;

        var conceptsPerSecond = newConceptCount / durationSeconds;

        _logger.LogDebug("Scene introduces {ConceptCount} concepts over {Duration}s = {Rate:F2} concepts/sec",
            newConceptCount, durationSeconds, conceptsPerSecond);

        return conceptsPerSecond;
    }

    private ContentComplexityResult CreateFallbackComplexityResult(Scene scene, int totalScenes)
    {
        var wordCount = scene.Script.Split(new[] { ' ', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;

        var technicalTerms = new[] { "algorithm", "implementation", "architecture", "framework", 
            "methodology", "optimization", "integration", "configuration", "deployment" };
        var technicalCount = technicalTerms.Count(term => 
            scene.Script.Contains(term, StringComparison.OrdinalIgnoreCase));

        var conceptDifficulty = wordCount > 100 ? 60.0 : 40.0;
        if (technicalCount > 0)
            conceptDifficulty += Math.Min(technicalCount * 10, 30);

        var terminologyDensity = technicalCount > 3 ? 70.0 : 40.0;
        var prerequisiteKnowledge = technicalCount > 2 ? 60.0 : 30.0;
        var multiStepReasoning = wordCount > 80 ? 50.0 : 30.0;

        var overallScore = (conceptDifficulty + terminologyDensity + 
            prerequisiteKnowledge + multiStepReasoning) / 4.0;

        return new ContentComplexityResult
        {
            SceneIndex = scene.Index,
            OverallComplexityScore = Math.Clamp(overallScore, 0, 100),
            ConceptDifficulty = Math.Clamp(conceptDifficulty, 0, 100),
            TerminologyDensity = Math.Clamp(terminologyDensity, 0, 100),
            PrerequisiteKnowledgeLevel = Math.Clamp(prerequisiteKnowledge, 0, 100),
            MultiStepReasoningRequired = Math.Clamp(multiStepReasoning, 0, 100),
            NewConceptsIntroduced = Math.Max(1, technicalCount + (wordCount / 50)),
            CognitiveProcessingTime = TimeSpan.FromSeconds(wordCount / 2.0),
            OptimalAttentionWindow = TimeSpan.FromSeconds(Math.Min(12, Math.Max(5, wordCount / 15.0))),
            DetailedBreakdown = "Fallback heuristic complexity analysis (LLM unavailable)",
            AnalyzedWithLlm = false
        };
    }

    private double GetProfileMultiplier(PacingProfile profile)
    {
        return profile switch
        {
            PacingProfile.FastPacedSocial => 0.85,        // 15% faster
            PacingProfile.ContemplativeEducational => 1.2, // 20% slower
            PacingProfile.BalancedDocumentary => 1.0,      // Standard
            _ => 1.0
        };
    }
}

/// <summary>
/// Result of content complexity analysis
/// </summary>
public class ContentComplexityResult
{
    /// <summary>
    /// Scene index
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Overall complexity score (0-100) combining all factors
    /// </summary>
    public double OverallComplexityScore { get; init; }

    /// <summary>
    /// Concept difficulty score (0-100)
    /// How difficult are the concepts being explained?
    /// </summary>
    public double ConceptDifficulty { get; init; }

    /// <summary>
    /// Terminology density score (0-100)
    /// How many specialized/technical terms are used?
    /// </summary>
    public double TerminologyDensity { get; init; }

    /// <summary>
    /// Prerequisite knowledge level (0-100)
    /// How much prior knowledge is assumed?
    /// </summary>
    public double PrerequisiteKnowledgeLevel { get; init; }

    /// <summary>
    /// Multi-step reasoning required (0-100)
    /// Does understanding require following multiple logical steps?
    /// </summary>
    public double MultiStepReasoningRequired { get; init; }

    /// <summary>
    /// Number of new concepts introduced in this scene
    /// </summary>
    public int NewConceptsIntroduced { get; init; }

    /// <summary>
    /// Estimated cognitive processing time required
    /// </summary>
    public TimeSpan CognitiveProcessingTime { get; init; }

    /// <summary>
    /// Optimal attention window for this content
    /// </summary>
    public TimeSpan OptimalAttentionWindow { get; init; }

    /// <summary>
    /// Detailed breakdown of complexity factors
    /// </summary>
    public string DetailedBreakdown { get; init; } = string.Empty;

    /// <summary>
    /// Whether this was analyzed with LLM or fallback heuristics
    /// </summary>
    public bool AnalyzedWithLlm { get; init; }
}

/// <summary>
/// Pacing profile presets for different video types
/// </summary>
public enum PacingProfile
{
    /// <summary>
    /// Fast-paced for social media (TikTok, Instagram Reels, YouTube Shorts)
    /// Quick cuts, high energy, minimal complexity
    /// </summary>
    FastPacedSocial,

    /// <summary>
    /// Contemplative for educational content
    /// Slower pacing, time for reflection, complex concepts
    /// </summary>
    ContemplativeEducational,

    /// <summary>
    /// Balanced for documentary-style content
    /// Standard pacing, moderate complexity
    /// </summary>
    BalancedDocumentary
}
