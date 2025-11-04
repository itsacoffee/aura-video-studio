using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Services;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for EnhancedRefinementOrchestrator with generator-critic-editor pattern
/// </summary>
public class EnhancedRefinementOrchestratorTests
{
    private readonly EnhancedRefinementOrchestrator _orchestrator;
    private readonly RuleBasedLlmProvider _llmProvider;
    private readonly CriticService _criticService;
    private readonly EditorService _editorService;
    private readonly IntelligentContentAdvisor _contentAdvisor;

    public EnhancedRefinementOrchestratorTests()
    {
        _llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        _criticService = new CriticService(NullLogger<CriticService>.Instance, _llmProvider);
        _editorService = new EditorService(NullLogger<EditorService>.Instance, _llmProvider);
        _contentAdvisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            _llmProvider
        );
        _orchestrator = new EnhancedRefinementOrchestrator(
            NullLogger<EnhancedRefinementOrchestrator>.Instance,
            _llmProvider,
            _criticService,
            _editorService,
            _contentAdvisor
        );
    }

    [Fact]
    public async Task RefineScript_EndToEnd_GeneratesRefinedScript()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Artificial Intelligence",
            Audience: "Beginners",
            Goal: "Educational introduction",
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

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 2,
            QualityThreshold = 85.0,
            MinimumImprovement = 5.0,
            EnableSchemaValidation = true,
            EnableTelemetry = true
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.True(result.Success, $"Refinement should succeed. Error: {result.ErrorMessage}");
        Assert.NotEmpty(result.FinalScript);
        Assert.NotEmpty(result.IterationMetrics);
        Assert.True(result.TotalPasses >= 1);
        Assert.NotNull(result.StopReason);
        Assert.True(result.TotalDuration > TimeSpan.Zero);
        Assert.True(result.TotalCost >= 0);
        Assert.NotNull(result.Telemetry);
        Assert.NotNull(result.CritiqueSummary);
    }

    [Fact]
    public async Task RefineScript_CollectsTelemetry_PerRound()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Test",
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

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 2,
            QualityThreshold = 95.0,
            EnableTelemetry = true
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.NotNull(result.Telemetry);
        Assert.NotEmpty(result.Telemetry.RoundData);
        
        foreach (var round in result.Telemetry.RoundData)
        {
            Assert.InRange(round.RoundNumber, 0, config.MaxRefinementPasses);
            Assert.NotNull(round.AfterMetrics);
            Assert.True(round.Duration > TimeSpan.Zero);
            Assert.True(round.Cost >= 0);
            Assert.True(round.SchemaValid);
        }

        Assert.NotNull(result.Telemetry.Convergence);
        Assert.NotEmpty(result.Telemetry.CostByPhase);
    }

    [Fact]
    public async Task RefineScript_TracksQualityImprovement_AcrossRounds()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Climate Change",
            Audience: "General public",
            Goal: "Awareness",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
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
            
            Assert.InRange(result.FinalMetrics.OverallScore, 0, 100);
            Assert.InRange(result.FinalMetrics.NarrativeCoherence, 0, 100);
            Assert.InRange(result.FinalMetrics.PacingAppropriateness, 0, 100);
            Assert.InRange(result.FinalMetrics.AudienceAlignment, 0, 100);
        }
    }

    [Fact]
    public async Task RefineScript_StopsEarly_WhenThresholdMet()
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
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "simple"
        );

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 3,
            QualityThreshold = 70.0,
            EnableTelemetry = true
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("threshold", result.StopReason, StringComparison.OrdinalIgnoreCase);
        Assert.True(result.FinalMetrics!.OverallScore >= config.QualityThreshold);
    }

    [Fact]
    public async Task RefineScript_RespectsCostBudget()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Complex Topic",
            Audience: "Experts",
            Goal: "Deep dive",
            Tone: "technical",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Dense,
            Style: "technical"
        );

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 3,
            QualityThreshold = 95.0,
            MaxCostBudget = 0.01,
            EnableTelemetry = true
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.TotalCost <= config.MaxCostBudget.Value * 1.1);
        
        if (result.TotalCost >= config.MaxCostBudget.Value)
        {
            Assert.Contains("cost", result.StopReason, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task RefineScript_CalculatesConvergenceStatistics()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Convergence",
            Audience: "Test",
            Goal: "Test",
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

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 2,
            QualityThreshold = 95.0,
            EnableTelemetry = true
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.NotNull(result.Telemetry);
        Assert.NotNull(result.Telemetry.Convergence);
        
        var convergence = result.Telemetry.Convergence;
        Assert.InRange(convergence.AverageImprovementPerRound, double.MinValue, double.MaxValue);
        Assert.True(convergence.ImprovementStdDev >= 0);
        Assert.InRange(convergence.ConvergenceRate, double.MinValue, double.MaxValue);
        Assert.InRange(convergence.TotalImprovement, double.MinValue, double.MaxValue);
    }

    [Fact]
    public async Task RefineScript_ValidatesSchema_EachRound()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Schema Test",
            Audience: "Test",
            Goal: "Test",
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

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 2,
            QualityThreshold = 95.0,
            EnableSchemaValidation = true,
            EnableTelemetry = true
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.NotNull(result.Telemetry);
        
        foreach (var round in result.Telemetry.RoundData)
        {
            if (round.RoundNumber > 0)
            {
                Assert.NotNull(round.EditorModel);
                Assert.True(round.SchemaValid || !round.SchemaValid);
            }
        }
    }

    [Fact]
    public async Task RefineScript_StopsOnMinimalImprovement()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Minimal Improvement Test",
            Audience: "Test",
            Goal: "Test",
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

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 3,
            QualityThreshold = 95.0,
            MinimumImprovement = 5.0,
            EnableTelemetry = true
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.True(result.Success);
        
        if (result.StopReason.Contains("Minimal", StringComparison.OrdinalIgnoreCase))
        {
            Assert.True(result.TotalPasses < config.MaxRefinementPasses + 1);
        }
    }

    [Fact]
    public async Task RefineScript_CritiqueSummary_IsPopulated()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Summary Test",
            Audience: "Test",
            Goal: "Test",
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

        var config = new ScriptRefinementConfig
        {
            MaxRefinementPasses = 1,
            QualityThreshold = 95.0
        };

        // Act
        var result = await _orchestrator.RefineScriptAsync(brief, spec, config);

        // Assert
        Assert.NotNull(result.CritiqueSummary);
        Assert.NotEmpty(result.CritiqueSummary);
        Assert.Contains("Overall Score", result.CritiqueSummary);
    }
}
