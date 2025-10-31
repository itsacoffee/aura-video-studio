using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Narrative;
using Aura.Core.Services.Narrative;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for SceneCoherenceOptimizer service
/// </summary>
public class SceneCoherenceOptimizerTests
{
    private readonly SceneCoherenceOptimizer _optimizer;

    public SceneCoherenceOptimizerTests()
    {
        var logger = NullLogger<SceneCoherenceOptimizer>.Instance;
        _optimizer = new SceneCoherenceOptimizer(logger);
    }

    [Fact]
    public async Task OptimizeSceneOrderAsync_WithHighCoherence_ReturnsNull()
    {
        var scenes = CreateTestScenes(3);
        var analysisResult = CreateAnalysisResult(
            coherenceScores: new[] { 90.0, 88.0 },
            overallCoherence: 89.0);

        var result = await _optimizer.OptimizeSceneOrderAsync(
            scenes, 
            analysisResult, 
            TimeSpan.FromSeconds(15), 
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task OptimizeSceneOrderAsync_WithLowCoherence_SuggestsReordering()
    {
        var scenes = CreateTestScenes(4);
        var analysisResult = CreateAnalysisResult(
            coherenceScores: new[] { 80.0, 30.0, 85.0 },
            overallCoherence: 65.0,
            criticalIssues: new[]
            {
                new ContinuityIssue
                {
                    SceneIndex = 1,
                    IssueType = "abrupt_transition",
                    Severity = IssueSeverity.Critical,
                    Description = "Abrupt transition"
                }
            });

        var result = await _optimizer.OptimizeSceneOrderAsync(
            scenes, 
            analysisResult, 
            TimeSpan.FromSeconds(20), 
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.CoherenceGain >= 15.0);
        Assert.True(result.ImprovedCoherence > result.OriginalCoherence);
    }

    [Fact]
    public async Task OptimizeSceneOrderAsync_MaintainsDurationConstraint()
    {
        var scenes = CreateTestScenes(5);
        var targetDuration = TimeSpan.FromSeconds(25);
        var analysisResult = CreateAnalysisResult(
            coherenceScores: new[] { 50.0, 45.0, 55.0, 50.0 },
            overallCoherence: 50.0);

        var result = await _optimizer.OptimizeSceneOrderAsync(
            scenes, 
            analysisResult, 
            targetDuration, 
            CancellationToken.None);

        if (result != null)
        {
            Assert.True(result.DurationChangePercent <= 5.0, 
                $"Duration change {result.DurationChangePercent:F2}% exceeds 5% limit");
            Assert.True(result.MaintainsDurationConstraint);
        }
    }

    [Fact]
    public async Task OptimizeSceneOrderAsync_WithMinimalCoherenceGain_ReturnsNull()
    {
        var scenes = CreateTestScenes(3);
        var analysisResult = CreateAnalysisResult(
            coherenceScores: new[] { 78.0, 79.0 },
            overallCoherence: 78.5);

        var result = await _optimizer.OptimizeSceneOrderAsync(
            scenes, 
            analysisResult, 
            TimeSpan.FromSeconds(15), 
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task OptimizeSceneOrderAsync_WithFewScenes_ReturnsNull()
    {
        var scenes = CreateTestScenes(2);
        var analysisResult = CreateAnalysisResult(
            coherenceScores: new[] { 50.0 },
            overallCoherence: 50.0);

        var result = await _optimizer.OptimizeSceneOrderAsync(
            scenes, 
            analysisResult, 
            TimeSpan.FromSeconds(10), 
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task OptimizeSceneOrderAsync_WithMultipleLowCoherencePairs_PrioritizesCritical()
    {
        var scenes = CreateTestScenes(6);
        var analysisResult = CreateAnalysisResult(
            coherenceScores: new[] { 65.0, 25.0, 60.0, 55.0, 70.0 },
            overallCoherence: 55.0,
            criticalIssues: new[]
            {
                new ContinuityIssue
                {
                    SceneIndex = 1,
                    IssueType = "abrupt_transition",
                    Severity = IssueSeverity.Critical,
                    Description = "Critical break in flow"
                }
            });

        var result = await _optimizer.OptimizeSceneOrderAsync(
            scenes, 
            analysisResult, 
            TimeSpan.FromSeconds(30), 
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEqual(analysisResult.PairwiseCoherence[1].ToSceneIndex, 
            result.SuggestedOrder[analysisResult.PairwiseCoherence[1].ToSceneIndex]);
    }

    [Fact]
    public async Task OptimizeSceneOrderAsync_ReturnsValidRationale()
    {
        var scenes = CreateTestScenes(4);
        var analysisResult = CreateAnalysisResult(
            coherenceScores: new[] { 80.0, 35.0, 85.0 },
            overallCoherence: 66.7,
            criticalIssues: new[]
            {
                new ContinuityIssue
                {
                    SceneIndex = 1,
                    IssueType = "abrupt_transition",
                    Severity = IssueSeverity.Critical,
                    Description = "Abrupt transition"
                }
            });

        var result = await _optimizer.OptimizeSceneOrderAsync(
            scenes, 
            analysisResult, 
            TimeSpan.FromSeconds(20), 
            CancellationToken.None);

        if (result != null)
        {
            Assert.NotNull(result.Rationale);
            Assert.NotEmpty(result.Rationale);
            Assert.Contains("coherence", result.Rationale.ToLowerInvariant());
        }
    }

    [Fact]
    public async Task OptimizeSceneOrderAsync_PreservesOriginalOrder()
    {
        var scenes = CreateTestScenes(4);
        var analysisResult = CreateAnalysisResult(
            coherenceScores: new[] { 80.0, 35.0, 85.0 },
            overallCoherence: 66.7);

        var result = await _optimizer.OptimizeSceneOrderAsync(
            scenes, 
            analysisResult, 
            TimeSpan.FromSeconds(20), 
            CancellationToken.None);

        if (result != null)
        {
            Assert.Equal(4, result.OriginalOrder.Count);
            Assert.Equal(Enumerable.Range(0, 4), result.OriginalOrder);
            Assert.Equal(4, result.SuggestedOrder.Count);
        }
    }

    [Fact]
    public async Task OptimizeSceneOrderAsync_CalculatesCorrectDurationChange()
    {
        var scenes = CreateTestScenes(3);
        var targetDuration = TimeSpan.FromSeconds(15);
        var analysisResult = CreateAnalysisResult(
            coherenceScores: new[] { 40.0, 45.0 },
            overallCoherence: 42.5);

        var result = await _optimizer.OptimizeSceneOrderAsync(
            scenes, 
            analysisResult, 
            targetDuration, 
            CancellationToken.None);

        if (result != null)
        {
            var expectedChange = Math.Abs(
                (result.AdjustedDuration.TotalSeconds - targetDuration.TotalSeconds) / 
                targetDuration.TotalSeconds * 100);

            Assert.Equal(expectedChange, result.DurationChangePercent, 0.1);
        }
    }

    private List<Scene> CreateTestScenes(int count)
    {
        var scenes = new List<Scene>();
        for (int i = 0; i < count; i++)
        {
            scenes.Add(new Scene(
                i, 
                $"Scene {i}", 
                $"Content for scene {i} with some words", 
                TimeSpan.FromSeconds(i * 5), 
                TimeSpan.FromSeconds(5)));
        }
        return scenes;
    }

    private NarrativeAnalysisResult CreateAnalysisResult(
        double[] coherenceScores,
        double overallCoherence,
        ContinuityIssue[]? criticalIssues = null)
    {
        var pairwiseCoherence = new List<ScenePairCoherence>();
        for (int i = 0; i < coherenceScores.Length; i++)
        {
            pairwiseCoherence.Add(new ScenePairCoherence
            {
                FromSceneIndex = i,
                ToSceneIndex = i + 1,
                CoherenceScore = coherenceScores[i],
                Reasoning = $"Test coherence for pair {i}-{i + 1}",
                ConnectionTypes = new[] { ConnectionType.Sequential },
                ConfidenceScore = 0.8,
                RequiresBridging = coherenceScores[i] < 70
            });
        }

        return new NarrativeAnalysisResult
        {
            PairwiseCoherence = pairwiseCoherence,
            ArcValidation = new NarrativeArcValidation
            {
                VideoType = "general",
                IsValid = true,
                DetectedStructure = "sequential",
                ExpectedStructure = "introduction → body → conclusion",
                StructuralIssues = Array.Empty<string>(),
                Recommendations = Array.Empty<string>(),
                Reasoning = "Test arc"
            },
            ContinuityIssues = criticalIssues?.ToList() ?? new List<ContinuityIssue>(),
            BridgingSuggestions = Array.Empty<BridgingSuggestion>(),
            OverallCoherenceScore = overallCoherence,
            AnalysisDuration = TimeSpan.FromSeconds(1)
        };
    }
}
