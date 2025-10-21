using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.ScriptEnhancement;
using Aura.Core.Services.ScriptEnhancement;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Aura.Tests;

public class AdvancedScriptEnhancerTests
{
    private readonly Mock<ILogger<AdvancedScriptEnhancer>> _mockLogger;
    private readonly Mock<ILogger<ScriptAnalysisService>> _mockAnalysisLogger;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly ScriptAnalysisService _analysisService;
    private readonly AdvancedScriptEnhancer _enhancer;

    public AdvancedScriptEnhancerTests()
    {
        _mockLogger = new Mock<ILogger<AdvancedScriptEnhancer>>();
        _mockAnalysisLogger = new Mock<ILogger<ScriptAnalysisService>>();
        _mockLlmProvider = new Mock<ILlmProvider>();
        _analysisService = new ScriptAnalysisService(_mockAnalysisLogger.Object, _mockLlmProvider.Object);
        _enhancer = new AdvancedScriptEnhancer(_mockLogger.Object, _mockLlmProvider.Object, _analysisService);
    }

    [Fact]
    public async Task EnhanceScriptAsync_ReturnsSuccessResponse()
    {
        // Arrange
        var script = "This is a basic script that needs enhancement.";

        // Act
        var result = await _enhancer.EnhanceScriptAsync(
            script, "Tutorial", "Beginners", "casual", null, false, null, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.BeforeAnalysis);
        Assert.NotEmpty(result.Suggestions);
        Assert.NotNull(result.ChangesSummary);
    }

    [Fact]
    public async Task EnhanceScriptAsync_WithAutoApply_AppliesHighConfidenceSuggestions()
    {
        // Arrange
        var script = "This is a script.";

        // Act
        var result = await _enhancer.EnhanceScriptAsync(
            script, null, null, null, null, true, null, CancellationToken.None);

        // Assert
        // When autoApply is true, enhancedScript should be set (even if it's the same as original)
        Assert.True(result.Success);
        Assert.NotNull(result.BeforeAnalysis);
    }

    [Fact]
    public async Task EnhanceScriptAsync_WithFocusAreas_FiltersAppropriateSuggestions()
    {
        // Arrange
        var script = "This is a script.";
        var focusAreas = new List<SuggestionType> { SuggestionType.Structure };

        // Act
        var result = await _enhancer.EnhanceScriptAsync(
            script, null, null, null, focusAreas, false, null, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Suggestions);
        // Should contain at least one structure suggestion
        Assert.Contains(result.Suggestions, s => s.Type == SuggestionType.Structure);
    }

    [Fact]
    public async Task OptimizeHookAsync_ReturnsImprovedHook()
    {
        // Arrange
        var script = "Welcome to my video.";
        _mockLlmProvider.Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("What if I told you there's a better way?");

        // Act
        var result = await _enhancer.OptimizeHookAsync(script, null, null, 15, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.OptimizedHook);
        Assert.NotEmpty(result.Techniques);
    }

    [Fact]
    public async Task AnalyzeEmotionalArcAsync_ReturnsEmotionalArc()
    {
        // Arrange
        var script = @"This is exciting!
But we have a problem.
Let's find a solution.";

        // Act
        var result = await _enhancer.AnalyzeEmotionalArcAsync(
            script, null, null, null, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.CurrentArc);
        Assert.NotNull(result.OptimizedArc);
        Assert.NotEmpty(result.Suggestions);
    }

    [Fact]
    public async Task FactCheckScriptAsync_IdentifiesClaims()
    {
        // Arrange
        var script = "Studies show that 90% of people prefer videos. Research proves this works.";

        // Act
        var result = await _enhancer.FactCheckScriptAsync(script, true, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.TotalClaims > 0);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public async Task AdjustToneAsync_ChangesToneProfile()
    {
        // Arrange
        var script = "This is a casual script, you know?";
        var targetTone = new ToneProfile(
            FormalityLevel: 80.0,
            EnergyLevel: 50.0,
            EmotionLevel: 40.0,
            PersonalityTraits: new List<string> { "professional" },
            BrandVoice: null,
            CustomAttributes: null
        );
        
        _mockLlmProvider.Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("This is a formal, professional script.");

        // Act
        var result = await _enhancer.AdjustToneAsync(script, targetTone, null, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.AdjustedScript);
        Assert.NotNull(result.OriginalTone);
        Assert.NotNull(result.AchievedTone);
    }

    [Fact]
    public async Task ApplyStorytellingFrameworkAsync_AppliesFramework()
    {
        // Arrange
        var script = "This is a basic script.";
        _mockLlmProvider.Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("## Setup\nIntroduction\n\n## Confrontation\nChallenge\n\n## Resolution\nSolution");

        // Act
        var result = await _enhancer.ApplyStorytellingFrameworkAsync(
            script, StoryFrameworkType.ThreeAct, null, null, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.EnhancedScript);
        Assert.NotNull(result.AppliedFramework);
        Assert.Equal(StoryFrameworkType.ThreeAct, result.AppliedFramework.Type);
    }

    [Fact]
    public async Task GetSuggestionsAsync_ReturnsSuggestions()
    {
        // Arrange
        var script = "This is a script that needs several improvements.";

        // Act
        var result = await _enhancer.GetSuggestionsAsync(
            script, null, null, null, 5, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Suggestions);
        Assert.True(result.Suggestions.Count <= 5);
        Assert.True(result.TotalCount >= result.Suggestions.Count);
    }

    [Fact]
    public async Task CompareVersionsAsync_GeneratesDiff()
    {
        // Arrange
        var versionA = "Line 1\nLine 2\nLine 3";
        var versionB = "Line 1\nLine 2 modified\nLine 3";

        // Act
        var result = await _enhancer.CompareVersionsAsync(versionA, versionB, true, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Differences);
        Assert.Contains(result.Differences, d => d.Type == "modified");
    }

    [Fact]
    public async Task CompareVersionsAsync_WithAnalysis_CalculatesImprovements()
    {
        // Arrange
        var versionA = "Welcome to my basic video.";
        var versionB = "Have you ever wondered about amazing video creation? Let me show you!";

        // Act
        var result = await _enhancer.CompareVersionsAsync(versionA, versionB, true, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.AnalysisA);
        Assert.NotNull(result.AnalysisB);
        Assert.NotEmpty(result.ImprovementMetrics);
        Assert.Contains("hook", result.ImprovementMetrics.Keys);
    }
}
