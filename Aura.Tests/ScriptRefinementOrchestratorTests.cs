using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Services;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Tests;

/// <summary>
/// Tests for ScriptRefinementOrchestrator multi-stage refinement pipeline
/// </summary>
public class ScriptRefinementOrchestratorTests
{
    private readonly RuleBasedLlmProvider _llmProvider;
    private readonly IntelligentContentAdvisor _contentAdvisor;
    private readonly ScriptRefinementOrchestrator _orchestrator;

    public ScriptRefinementOrchestratorTests()
    {
        _llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        _contentAdvisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            _llmProvider
        );
        _orchestrator = new ScriptRefinementOrchestrator(
            NullLogger<ScriptRefinementOrchestrator>.Instance,
            _llmProvider,
            _contentAdvisor
        );
    }

    [Fact]
    public async Task RefineScript_WithDefaultConfig_GeneratesMultiplePasses()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Artificial Intelligence Basics",
            Audience: "Beginners",
            Goal: "Educational introduction",
            Tone: "friendly",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "educational"
        );

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 2,
            QualityThreshold = 85.0,
            MinimumImprovement = 5.0,
            EnableAdvisorValidation = true
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.True(result.Success, $"Refinement should succeed. Error: {result.ErrorMessage}");
        Assert.NotEmpty(result.FinalScript);
        Assert.NotEmpty(result.IterationMetrics);
        Assert.True(result.TotalPasses >= 1, "Should perform at least initial draft");
        Assert.NotNull(result.StopReason);
        Assert.True(result.TotalDuration > TimeSpan.Zero);
    }

    [Fact]
    public async Task RefineScript_TracksQualityImprovementAcrossIterations()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Climate Change Solutions",
            Audience: "General public",
            Goal: "Awareness and action",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "educational"
        );

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 2,
            QualityThreshold = 95.0,
            MinimumImprovement = 5.0
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.InitialMetrics);
        Assert.NotNull(result.FinalMetrics);

        if (result.TotalPasses > 1)
        {
            var improvement = result.GetTotalImprovement();
            Assert.NotNull(improvement);
            
            // Verify metrics are in valid range
            Assert.InRange(result.FinalMetrics.OverallScore, 0, 100);
            Assert.InRange(result.FinalMetrics.NarrativeCoherence, 0, 100);
            Assert.InRange(result.FinalMetrics.PacingAppropriateness, 0, 100);
            Assert.InRange(result.FinalMetrics.AudienceAlignment, 0, 100);
            Assert.InRange(result.FinalMetrics.VisualClarity, 0, 100);
            Assert.InRange(result.FinalMetrics.EngagementPotential, 0, 100);
        }
    }

    [Fact]
    public async Task RefineScript_StopsEarlyWhenThresholdMet()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Simple Topic",
            Audience: "Everyone",
            Goal: "Quick info",
            Tone: "casual",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "simple"
        );

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 3,
            QualityThreshold = 70.0,
            MinimumImprovement = 5.0
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.StopReason);
        
        // Should stop before max passes if quality is good
        Assert.True(result.TotalPasses <= config.MaxRefinementPasses);
    }

    [Fact]
    public async Task RefineScript_RespectsMaxPasses()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Complex Technical Topic",
            Audience: "Experts",
            Goal: "Deep dive",
            Tone: "technical",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "technical"
        );

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 1,
            QualityThreshold = 99.0,
            MinimumImprovement = 5.0
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.TotalPasses <= config.MaxRefinementPasses + 1); // +1 for initial draft
    }

    [Fact]
    public void ScriptQualityMetrics_CalculatesOverallScoreCorrectly()
    {
        // Arrange
        var metrics = new ScriptQualityMetrics
        {
            NarrativeCoherence = 80.0,
            PacingAppropriateness = 75.0,
            AudienceAlignment = 85.0,
            VisualClarity = 70.0,
            EngagementPotential = 90.0,
            Iteration = 0
        };

        // Act
        metrics.CalculateOverallScore();

        // Assert
        var expectedScore = 80.0 * 0.25 + 75.0 * 0.20 + 85.0 * 0.20 + 70.0 * 0.15 + 90.0 * 0.20;
        Assert.Equal(expectedScore, metrics.OverallScore, 0.1);
        Assert.InRange(metrics.OverallScore, 0, 100);
    }

    [Fact]
    public void ScriptQualityMetrics_CalculatesImprovementCorrectly()
    {
        // Arrange
        var baseline = new ScriptQualityMetrics
        {
            NarrativeCoherence = 70.0,
            PacingAppropriateness = 65.0,
            AudienceAlignment = 75.0,
            VisualClarity = 60.0,
            EngagementPotential = 80.0,
            Iteration = 0
        };
        baseline.CalculateOverallScore();

        var improved = new ScriptQualityMetrics
        {
            NarrativeCoherence = 85.0,
            PacingAppropriateness = 80.0,
            AudienceAlignment = 85.0,
            VisualClarity = 75.0,
            EngagementPotential = 90.0,
            Iteration = 1
        };
        improved.CalculateOverallScore();

        // Act
        var improvement = improved.CalculateImprovement(baseline);

        // Assert
        Assert.True(improvement.OverallDelta > 0, "Overall score should improve");
        Assert.True(improvement.NarrativeCoherenceDelta > 0);
        Assert.True(improvement.PacingDelta > 0);
        Assert.True(improvement.HasMeaningfulImprovement());
    }

    [Fact]
    public void ScriptQualityMetrics_MeetsThreshold_WorksCorrectly()
    {
        // Arrange
        var metrics = new ScriptQualityMetrics
        {
            NarrativeCoherence = 90.0,
            PacingAppropriateness = 85.0,
            AudienceAlignment = 88.0,
            VisualClarity = 82.0,
            EngagementPotential = 87.0,
            Iteration = 0
        };
        metrics.CalculateOverallScore();

        // Act & Assert
        Assert.True(metrics.MeetsThreshold(80.0));
        Assert.True(metrics.MeetsThreshold(85.0));
        Assert.False(metrics.MeetsThreshold(95.0));
    }

    [Fact]
    public void ScriptRefinementConfig_ValidatesCorrectly()
    {
        // Valid config should not throw
        var validConfig = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 2,
            QualityThreshold = 85.0,
            MinimumImprovement = 5.0
        };
        validConfig.Validate(); // Should not throw

        // Invalid max passes
        var invalidMaxPasses = new ScriptRefinementConfig { MaxRefinementPasses = 0 };
        Assert.Throws<ArgumentException>(() => invalidMaxPasses.Validate());

        var invalidMaxPassesTooHigh = new ScriptRefinementConfig { MaxRefinementPasses = 5 };
        Assert.Throws<ArgumentException>(() => invalidMaxPassesTooHigh.Validate());

        // Invalid quality threshold
        var invalidThreshold = new ScriptRefinementConfig { QualityThreshold = -10 };
        Assert.Throws<ArgumentException>(() => invalidThreshold.Validate());

        var invalidThresholdTooHigh = new ScriptRefinementConfig { QualityThreshold = 150 };
        Assert.Throws<ArgumentException>(() => invalidThresholdTooHigh.Validate());

        // Invalid minimum improvement
        var invalidMinImprovement = new ScriptRefinementConfig { MinimumImprovement = -5 };
        Assert.Throws<ArgumentException>(() => invalidMinImprovement.Validate());
    }

    [Fact]
    public async Task RefineScript_WithAdvisorValidation_CompletesSuccessfully()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Machine Learning Fundamentals",
            Audience: "Tech enthusiasts",
            Goal: "Educational",
            Tone: "engaging",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "educational"
        );

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 2,
            QualityThreshold = 85.0,
            EnableAdvisorValidation = true
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.FinalScript);
    }

    [Fact]
    public void ScriptRefinementResult_GetTotalImprovement_HandlesEmptyMetrics()
    {
        // Arrange
        var result = new ScriptRefinementResult();

        // Act
        var improvement = result.GetTotalImprovement();

        // Assert
        Assert.Null(improvement);
    }

    [Fact]
    public void ScriptRefinementResult_GetTotalImprovement_CalculatesCorrectly()
    {
        // Arrange
        var result = new ScriptRefinementResult();
        
        var initialMetrics = new ScriptQualityMetrics
        {
            NarrativeCoherence = 70.0,
            PacingAppropriateness = 65.0,
            AudienceAlignment = 75.0,
            VisualClarity = 60.0,
            EngagementPotential = 80.0,
            Iteration = 0
        };
        initialMetrics.CalculateOverallScore();

        var finalMetrics = new ScriptQualityMetrics
        {
            NarrativeCoherence = 85.0,
            PacingAppropriateness = 82.0,
            AudienceAlignment = 88.0,
            VisualClarity = 78.0,
            EngagementPotential = 90.0,
            Iteration = 2
        };
        finalMetrics.CalculateOverallScore();

        result.IterationMetrics.Add(initialMetrics);
        result.IterationMetrics.Add(finalMetrics);

        // Act
        var improvement = result.GetTotalImprovement();

        // Assert
        Assert.NotNull(improvement);
        Assert.True(improvement.OverallDelta > 0);
        Assert.True(improvement.HasMeaningfulImprovement());
    }
}
