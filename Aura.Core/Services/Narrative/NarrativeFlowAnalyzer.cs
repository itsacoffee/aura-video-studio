using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Narrative;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Narrative;

/// <summary>
/// Service for analyzing narrative flow and coherence using LLM-based analysis
/// Evaluates scene-to-scene transitions and overall story arc integrity
/// </summary>
public class NarrativeFlowAnalyzer
{
    private readonly ILogger<NarrativeFlowAnalyzer> _logger;
    private readonly ILlmProvider _llmProvider;

    public NarrativeFlowAnalyzer(
        ILogger<NarrativeFlowAnalyzer> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Performs comprehensive narrative flow analysis on a sequence of scenes
    /// </summary>
    public async Task<NarrativeAnalysisResult> AnalyzeNarrativeFlowAsync(
        IReadOnlyList<Scene> scenes,
        string videoGoal,
        string videoType,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Starting narrative flow analysis for {SceneCount} scenes", scenes.Count);

        try
        {
            var pairwiseCoherence = await AnalyzePairwiseCoherenceAsync(scenes, videoGoal, ct).ConfigureAwait(false);
            var arcValidation = await AnalyzeNarrativeArcAsync(scenes, videoGoal, videoType, ct).ConfigureAwait(false);
            var continuityIssues = DetectContinuityIssues(pairwiseCoherence, arcValidation);
            var bridgingSuggestions = await GenerateBridgingSuggestionsAsync(
                scenes, pairwiseCoherence, videoGoal, ct).ConfigureAwait(false);

            var overallScore = CalculateOverallCoherence(pairwiseCoherence);

            sw.Stop();
            _logger.LogInformation(
                "Narrative flow analysis completed in {Duration}ms. Overall coherence: {Score:F2}",
                sw.ElapsedMilliseconds, overallScore);

            return new NarrativeAnalysisResult
            {
                PairwiseCoherence = pairwiseCoherence,
                ArcValidation = arcValidation,
                ContinuityIssues = continuityIssues,
                BridgingSuggestions = bridgingSuggestions,
                OverallCoherenceScore = overallScore,
                AnalysisDuration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during narrative flow analysis");
            throw;
        }
    }

    /// <summary>
    /// Analyzes coherence between consecutive scene pairs
    /// </summary>
    private async Task<List<ScenePairCoherence>> AnalyzePairwiseCoherenceAsync(
        IReadOnlyList<Scene> scenes,
        string videoGoal,
        CancellationToken ct)
    {
        var results = new List<ScenePairCoherence>();

        for (int i = 0; i < scenes.Count - 1; i++)
        {
            ct.ThrowIfCancellationRequested();

            var fromScene = scenes[i];
            var toScene = scenes[i + 1];

            _logger.LogDebug("Analyzing coherence between scene {From} and {To}", i, i + 1);

            try
            {
                var llmResult = await _llmProvider.AnalyzeSceneCoherenceAsync(
                    fromScene.Script,
                    toScene.Script,
                    videoGoal,
                    ct).ConfigureAwait(false);

                if (llmResult != null)
                {
                    results.Add(new ScenePairCoherence
                    {
                        FromSceneIndex = i,
                        ToSceneIndex = i + 1,
                        CoherenceScore = llmResult.CoherenceScore,
                        Reasoning = llmResult.Reasoning,
                        ConnectionTypes = llmResult.ConnectionTypes,
                        ConfidenceScore = llmResult.ConfidenceScore,
                        RequiresBridging = llmResult.CoherenceScore < 70
                    });
                }
                else
                {
                    _logger.LogWarning("LLM returned null for scene pair {From}-{To}, using fallback", i, i + 1);
                    results.Add(CreateFallbackCoherence(i, i + 1, fromScene, toScene));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error analyzing scene pair {From}-{To}, using fallback", i, i + 1);
                results.Add(CreateFallbackCoherence(i, i + 1, fromScene, toScene));
            }
        }

        return results;
    }

    /// <summary>
    /// Analyzes overall narrative arc structure
    /// </summary>
    private async Task<NarrativeArcValidation> AnalyzeNarrativeArcAsync(
        IReadOnlyList<Scene> scenes,
        string videoGoal,
        string videoType,
        CancellationToken ct)
    {
        _logger.LogDebug("Analyzing narrative arc structure for {VideoType} video", videoType);

        try
        {
            var sceneTexts = scenes.Select(s => s.Script).ToList();
            var llmResult = await _llmProvider.ValidateNarrativeArcAsync(
                sceneTexts,
                videoGoal,
                videoType,
                ct).ConfigureAwait(false);

            if (llmResult != null)
            {
                return new NarrativeArcValidation
                {
                    VideoType = videoType,
                    IsValid = llmResult.IsValid,
                    DetectedStructure = llmResult.DetectedStructure,
                    ExpectedStructure = llmResult.ExpectedStructure,
                    StructuralIssues = llmResult.StructuralIssues,
                    Recommendations = llmResult.Recommendations,
                    Reasoning = llmResult.Reasoning
                };
            }
            else
            {
                _logger.LogWarning("LLM returned null for narrative arc validation, using fallback");
                return CreateFallbackArcValidation(videoType, scenes.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error analyzing narrative arc, using fallback");
            return CreateFallbackArcValidation(videoType, scenes.Count);
        }
    }

    /// <summary>
    /// Generates bridging suggestions for scenes with low coherence
    /// </summary>
    private async Task<List<BridgingSuggestion>> GenerateBridgingSuggestionsAsync(
        IReadOnlyList<Scene> scenes,
        IReadOnlyList<ScenePairCoherence> coherenceResults,
        string videoGoal,
        CancellationToken ct)
    {
        var suggestions = new List<BridgingSuggestion>();

        foreach (var coherence in coherenceResults.Where(c => c.RequiresBridging))
        {
            ct.ThrowIfCancellationRequested();

            var fromScene = scenes[coherence.FromSceneIndex];
            var toScene = scenes[coherence.ToSceneIndex];

            _logger.LogDebug(
                "Generating bridging text for low-coherence pair {From}-{To} (score: {Score:F2})",
                coherence.FromSceneIndex, coherence.ToSceneIndex, coherence.CoherenceScore);

            try
            {
                var bridgingText = await _llmProvider.GenerateTransitionTextAsync(
                    fromScene.Script,
                    toScene.Script,
                    videoGoal,
                    ct).ConfigureAwait(false);

                if (!string.IsNullOrWhiteSpace(bridgingText))
                {
                    suggestions.Add(new BridgingSuggestion
                    {
                        FromSceneIndex = coherence.FromSceneIndex,
                        ToSceneIndex = coherence.ToSceneIndex,
                        BridgingText = bridgingText,
                        Rationale = $"Bridging low coherence transition (score: {coherence.CoherenceScore:F2})",
                        CoherenceImprovement = 85 - coherence.CoherenceScore
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Error generating bridging text for pair {From}-{To}",
                    coherence.FromSceneIndex,
                    coherence.ToSceneIndex);
            }
        }

        return suggestions;
    }

    /// <summary>
    /// Detects continuity issues based on coherence and arc analysis
    /// </summary>
    private List<ContinuityIssue> DetectContinuityIssues(
        IReadOnlyList<ScenePairCoherence> coherence,
        NarrativeArcValidation arcValidation)
    {
        var issues = new List<ContinuityIssue>();

        foreach (var pair in coherence)
        {
            if (pair.CoherenceScore < 40)
            {
                issues.Add(new ContinuityIssue
                {
                    SceneIndex = pair.FromSceneIndex,
                    IssueType = "abrupt_transition",
                    Severity = IssueSeverity.Critical,
                    Description = $"Critical: Abrupt transition from scene {pair.FromSceneIndex} to {pair.ToSceneIndex} (coherence: {pair.CoherenceScore:F2})",
                    Recommendation = "Add bridging content or reorder scenes"
                });
            }
            else if (pair.CoherenceScore < 70)
            {
                issues.Add(new ContinuityIssue
                {
                    SceneIndex = pair.FromSceneIndex,
                    IssueType = "weak_transition",
                    Severity = IssueSeverity.Warning,
                    Description = $"Warning: Weak transition from scene {pair.FromSceneIndex} to {pair.ToSceneIndex} (coherence: {pair.CoherenceScore:F2})",
                    Recommendation = "Consider adding transition text"
                });
            }
        }

        if (!arcValidation.IsValid)
        {
            foreach (var structuralIssue in arcValidation.StructuralIssues)
            {
                issues.Add(new ContinuityIssue
                {
                    SceneIndex = -1,
                    IssueType = "structural_issue",
                    Severity = IssueSeverity.Warning,
                    Description = structuralIssue,
                    Recommendation = string.Join("; ", arcValidation.Recommendations)
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Calculates overall coherence score from pairwise results
    /// </summary>
    private double CalculateOverallCoherence(IReadOnlyList<ScenePairCoherence> coherence)
    {
        if (coherence.Count == 0)
        {
            return 100.0;
        }

        return coherence.Average(c => c.CoherenceScore);
    }

    /// <summary>
    /// Creates fallback coherence result when LLM fails
    /// </summary>
    private ScenePairCoherence CreateFallbackCoherence(
        int fromIndex,
        int toIndex,
        Scene fromScene,
        Scene toScene)
    {
        var fromWords = GetSignificantWords(fromScene.Script);
        var toWords = GetSignificantWords(toScene.Script);
        var commonWords = fromWords.Intersect(toWords, StringComparer.OrdinalIgnoreCase).ToList();
        var overlapRatio = commonWords.Count / (double)Math.Max(fromWords.Count, 1);
        var score = Math.Clamp(overlapRatio * 100, 0, 100);

        return new ScenePairCoherence
        {
            FromSceneIndex = fromIndex,
            ToSceneIndex = toIndex,
            CoherenceScore = score,
            Reasoning = "Fallback: Word overlap-based analysis",
            ConnectionTypes = new[] { ConnectionType.Sequential },
            ConfidenceScore = 0.5,
            RequiresBridging = score < 70
        };
    }

    /// <summary>
    /// Creates fallback arc validation when LLM fails
    /// </summary>
    private NarrativeArcValidation CreateFallbackArcValidation(string videoType, int sceneCount)
    {
        var expectedStructure = GetExpectedStructure(videoType);
        return new NarrativeArcValidation
        {
            VideoType = videoType,
            IsValid = true,
            DetectedStructure = "Unknown (fallback mode)",
            ExpectedStructure = expectedStructure,
            StructuralIssues = Array.Empty<string>(),
            Recommendations = new[] { "LLM analysis unavailable, structural validation skipped" },
            Reasoning = "Fallback: LLM provider unavailable"
        };
    }

    /// <summary>
    /// Gets expected narrative structure for video type
    /// </summary>
    private string GetExpectedStructure(string videoType)
    {
        return videoType.ToLowerInvariant() switch
        {
            "educational" => "problem → explanation → solution",
            "entertainment" => "setup → conflict → resolution",
            "documentary" => "introduction → evidence → conclusion",
            "tutorial" => "overview → steps → summary",
            _ => "introduction → body → conclusion"
        };
    }

    /// <summary>
    /// Extracts significant words from text for fallback analysis
    /// </summary>
    private List<string> GetSignificantWords(string text)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "is", "are", "was", "were", "be", "been",
            "this", "that", "these", "those", "we", "you", "they", "it"
        };

        return text
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !stopWords.Contains(w))
            .ToList();
    }
}
