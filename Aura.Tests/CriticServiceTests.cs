using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for CriticService rubric-based script critique
/// </summary>
public class CriticServiceTests
{
    private readonly CriticService _criticService;
    private readonly RuleBasedLlmProvider _llmProvider;

    public CriticServiceTests()
    {
        _llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        _criticService = new CriticService(
            NullLogger<CriticService>.Instance,
            _llmProvider
        );
    }

    [Fact]
    public async Task CritiqueScript_WithRubrics_ReturnsStructuredCritique()
    {
        // Arrange
        var script = "Welcome to our video about AI. AI is transforming the world. Let's explore how.";
        
        var brief = new Brief(
            Topic: "AI Basics",
            Audience: "Beginners",
            Goal: "Educational",
            Tone: "friendly",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "educational"
        );

        var rubrics = RefinementRubricBuilder.GetDefaultRubrics();

        // Act
        var result = await _criticService.CritiqueScriptAsync(
            script, brief, spec, rubrics, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.OverallScore, 0, 100);
        Assert.NotEmpty(result.RubricScores);
        Assert.NotNull(result.TimingAnalysis);
        Assert.True(result.TimingAnalysis.WordCount > 0);
    }

    [Fact]
    public async Task CritiqueScript_ComputesRubricScores_Deterministically()
    {
        // Arrange
        var script = "Test script content for evaluation.";
        
        var brief = new Brief(
            Topic: "Test",
            Audience: null,
            Goal: null,
            Tone: "neutral",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );

        var rubrics = new List<RefinementRubric>
        {
            RefinementRubricBuilder.BuildClarityRubric(),
            RefinementRubricBuilder.BuildCoherenceRubric()
        };

        // Act - Run twice to test determinism
        var result1 = await _criticService.CritiqueScriptAsync(
            script, brief, spec, rubrics, null, CancellationToken.None);
        
        var result2 = await _criticService.CritiqueScriptAsync(
            script, brief, spec, rubrics, null, CancellationToken.None);

        // Assert - Scores should be identical (deterministic)
        Assert.Equal(result1.OverallScore, result2.OverallScore);
        Assert.Equal(result1.RubricScores.Count, result2.RubricScores.Count);
        
        foreach (var kvp in result1.RubricScores)
        {
            Assert.True(result2.RubricScores.ContainsKey(kvp.Key));
            Assert.Equal(kvp.Value, result2.RubricScores[kvp.Key]);
        }
    }

    [Fact]
    public async Task AnalyzeTimingFit_CalculatesWordCount_Accurately()
    {
        // Arrange
        var script = "This is a test script. It has exactly fifteen words in total to test counting.";
        var targetDuration = TimeSpan.FromMinutes(1);

        // Act
        var result = await _criticService.AnalyzeTimingFitAsync(script, targetDuration, CancellationToken.None);

        // Assert
        Assert.Equal(15, result.WordCount);
        Assert.Equal(150, result.TargetWordCount); // 1 minute * 150 words/minute
        Assert.True(result.Variance < 0); // 15 is less than 150, so negative variance
        Assert.False(result.WithinAcceptableRange); // -90% variance is not within ±15%
        Assert.NotNull(result.Recommendation);
        Assert.Contains("too short", result.Recommendation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AnalyzeTimingFit_IdentifiesAcceptableRange()
    {
        // Arrange - Create script with approximately correct word count
        var words = string.Join(" ", Enumerable.Repeat("word", 145)); // 145 words
        var targetDuration = TimeSpan.FromMinutes(1); // Target: 150 words

        // Act
        var result = await _criticService.AnalyzeTimingFitAsync(words, targetDuration, CancellationToken.None);

        // Assert
        Assert.Equal(145, result.WordCount);
        Assert.Equal(150, result.TargetWordCount);
        Assert.InRange(result.Variance, -15, 15); // Within ±15%
        Assert.True(result.WithinAcceptableRange);
        Assert.Null(result.Recommendation); // No recommendation needed when in range
    }

    [Fact]
    public async Task AnalyzeTimingFit_DetectsTooLongScript()
    {
        // Arrange - Create script that's too long
        var words = string.Join(" ", Enumerable.Repeat("word", 200)); // 200 words
        var targetDuration = TimeSpan.FromMinutes(1); // Target: 150 words

        // Act
        var result = await _criticService.AnalyzeTimingFitAsync(words, targetDuration, CancellationToken.None);

        // Assert
        Assert.Equal(200, result.WordCount);
        Assert.True(result.Variance > 15); // More than 15% over
        Assert.False(result.WithinAcceptableRange);
        Assert.NotNull(result.Recommendation);
        Assert.Contains("too long", result.Recommendation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("50", result.Recommendation); // Should suggest removing ~50 words
    }

    [Fact]
    public async Task CritiqueScript_WithPreviousMetrics_IncludesContext()
    {
        // Arrange
        var script = "Updated script with improvements.";
        
        var brief = new Brief(
            Topic: "Test",
            Audience: null,
            Goal: null,
            Tone: "neutral",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );

        var previousMetrics = new ScriptQualityMetrics
        {
            Iteration = 0,
            OverallScore = 70.0,
            NarrativeCoherence = 65.0,
            PacingAppropriateness = 70.0,
            AudienceAlignment = 75.0,
            VisualClarity = 70.0,
            EngagementPotential = 70.0
        };

        var rubrics = RefinementRubricBuilder.GetDefaultRubrics();

        // Act
        var result = await _criticService.CritiqueScriptAsync(
            script, brief, spec, rubrics, previousMetrics, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.OverallScore, 0, 100);
    }

    [Fact]
    public void RubricScores_SumWithWeights_ToOverallScore()
    {
        // Arrange
        var rubrics = RefinementRubricBuilder.GetDefaultRubrics();
        var totalWeight = rubrics.Sum(r => r.Weight);

        // Assert
        Assert.Equal(1.0, totalWeight, precision: 2);
    }

    [Fact]
    public void DefaultRubrics_HaveValidThresholds()
    {
        // Arrange
        var rubrics = RefinementRubricBuilder.GetDefaultRubrics();

        // Assert
        foreach (var rubric in rubrics)
        {
            Assert.InRange(rubric.TargetThreshold, 0, 100);
            Assert.InRange(rubric.Weight, 0, 1);
            Assert.NotEmpty(rubric.Criteria);
            
            foreach (var criterion in rubric.Criteria)
            {
                Assert.NotEmpty(criterion.Name);
                Assert.NotEmpty(criterion.Description);
                Assert.NotEmpty(criterion.ScoringGuideline);
            }
        }
    }
}
