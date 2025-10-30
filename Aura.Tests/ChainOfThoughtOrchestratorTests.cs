using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.AI;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Tests;

/// <summary>
/// Unit tests for ChainOfThoughtOrchestrator
/// </summary>
public class ChainOfThoughtOrchestratorTests
{
    private readonly Mock<ILogger<ChainOfThoughtOrchestrator>> _mockLogger;
    private readonly Mock<ILogger<PromptCustomizationService>> _mockPromptLogger;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly PromptCustomizationService _promptService;
    private readonly ChainOfThoughtOrchestrator _orchestrator;

    public ChainOfThoughtOrchestratorTests()
    {
        _mockLogger = new Mock<ILogger<ChainOfThoughtOrchestrator>>();
        _mockPromptLogger = new Mock<ILogger<PromptCustomizationService>>();
        _mockLlmProvider = new Mock<ILlmProvider>();
        _promptService = new PromptCustomizationService(_mockPromptLogger.Object);
        _orchestrator = new ChainOfThoughtOrchestrator(
            _mockLogger.Object,
            _mockLlmProvider.Object,
            _promptService);
    }

    [Fact]
    public async Task ExecuteStageAsync_TopicAnalysis_ReturnsResult()
    {
        // Arrange
        var brief = CreateSampleBrief();
        var spec = CreateSampleSpec();
        var expectedContent = "Topic analysis content with insights and strategies.";

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContent);

        // Act
        var result = await _orchestrator.ExecuteStageAsync(
            ChainOfThoughtStage.TopicAnalysis,
            brief,
            spec,
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChainOfThoughtStage.TopicAnalysis, result.Stage);
        Assert.Equal(expectedContent, result.Content);
        Assert.False(result.RequiresUserReview);
    }

    [Fact]
    public async Task ExecuteStageAsync_Outline_WithPreviousContent_ReturnsResult()
    {
        // Arrange
        var brief = CreateSampleBrief();
        var spec = CreateSampleSpec();
        var previousContent = "Previous topic analysis content";
        var expectedContent = "Detailed outline based on analysis";

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContent);

        // Act
        var result = await _orchestrator.ExecuteStageAsync(
            ChainOfThoughtStage.Outline,
            brief,
            spec,
            previousContent,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChainOfThoughtStage.Outline, result.Stage);
        Assert.Equal(expectedContent, result.Content);
        Assert.True(result.RequiresUserReview);
    }

    [Fact]
    public async Task ExecuteStageAsync_FullScript_WithPreviousContent_ReturnsResult()
    {
        // Arrange
        var brief = CreateSampleBrief();
        var spec = CreateSampleSpec();
        var previousContent = "Detailed outline content";
        var expectedContent = "Complete script with all scenes";

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContent);

        // Act
        var result = await _orchestrator.ExecuteStageAsync(
            ChainOfThoughtStage.FullScript,
            brief,
            spec,
            previousContent,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ChainOfThoughtStage.FullScript, result.Stage);
        Assert.Equal(expectedContent, result.Content);
        Assert.True(result.RequiresUserReview);
    }

    [Fact]
    public async Task ExecuteStageAsync_CallsLlmProvider()
    {
        // Arrange
        var brief = CreateSampleBrief();
        var spec = CreateSampleSpec();

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Generated content");

        // Act
        await _orchestrator.ExecuteStageAsync(
            ChainOfThoughtStage.TopicAnalysis,
            brief,
            spec,
            null,
            CancellationToken.None);

        // Assert
        _mockLlmProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteStageAsync_LlmProviderFails_ThrowsException()
    {
        // Arrange
        var brief = CreateSampleBrief();
        var spec = CreateSampleSpec();

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("LLM provider error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _orchestrator.ExecuteStageAsync(
                ChainOfThoughtStage.TopicAnalysis,
                brief,
                spec,
                null,
                CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteStageAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var brief = CreateSampleBrief();
        var spec = CreateSampleSpec();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            _orchestrator.ExecuteStageAsync(
                ChainOfThoughtStage.TopicAnalysis,
                brief,
                spec,
                null,
                cts.Token));
    }

    [Fact]
    public async Task ExecuteStageAsync_TopicAnalysis_DoesNotRequireReview()
    {
        // Arrange
        var brief = CreateSampleBrief();
        var spec = CreateSampleSpec();

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Content");

        // Act
        var result = await _orchestrator.ExecuteStageAsync(
            ChainOfThoughtStage.TopicAnalysis,
            brief,
            spec,
            null,
            CancellationToken.None);

        // Assert
        Assert.False(result.RequiresUserReview);
    }

    [Fact]
    public async Task ExecuteStageAsync_OutlineAndFullScript_RequireReview()
    {
        // Arrange
        var brief = CreateSampleBrief();
        var spec = CreateSampleSpec();

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Content");

        // Act
        var outlineResult = await _orchestrator.ExecuteStageAsync(
            ChainOfThoughtStage.Outline,
            brief,
            spec,
            "previous",
            CancellationToken.None);

        var scriptResult = await _orchestrator.ExecuteStageAsync(
            ChainOfThoughtStage.FullScript,
            brief,
            spec,
            "previous",
            CancellationToken.None);

        // Assert
        Assert.True(outlineResult.RequiresUserReview);
        Assert.True(scriptResult.RequiresUserReview);
    }

    [Fact]
    public async Task ExecuteStageAsync_IncludesSuggestedEdits()
    {
        // Arrange
        var brief = CreateSampleBrief();
        var spec = CreateSampleSpec();

        _mockLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Content");

        // Act
        var result = await _orchestrator.ExecuteStageAsync(
            ChainOfThoughtStage.Outline,
            brief,
            spec,
            "previous",
            CancellationToken.None);

        // Assert
        Assert.NotNull(result.SuggestedEdits);
    }

    private static Brief CreateSampleBrief()
    {
        return new Brief(
            Topic: "Machine Learning Basics",
            Audience: "Beginners",
            Goal: "Education",
            Tone: "informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );
    }

    private static PlanSpec CreateSampleSpec()
    {
        return new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: PacingEnum.Conversational,
            Density: DensityEnum.Balanced,
            Style: "educational"
        );
    }
}
