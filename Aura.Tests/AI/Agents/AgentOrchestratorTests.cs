using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Agents;
using Aura.Core.AI.Validation;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.AI.Agents;

public class AgentOrchestratorTests
{
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly Mock<ILogger<ScreenwriterAgent>> _mockScreenwriterLogger;
    private readonly Mock<ILogger<VisualDirectorAgent>> _mockVisualDirectorLogger;
    private readonly Mock<ILogger<CriticAgent>> _mockCriticLogger;
    private readonly Mock<ILogger<AgentOrchestrator>> _mockOrchestratorLogger;
    private readonly ScriptSchemaValidator _validator;

    public AgentOrchestratorTests()
    {
        _mockLlmProvider = new Mock<ILlmProvider>();
        _mockScreenwriterLogger = new Mock<ILogger<ScreenwriterAgent>>();
        _mockVisualDirectorLogger = new Mock<ILogger<VisualDirectorAgent>>();
        _mockCriticLogger = new Mock<ILogger<CriticAgent>>();
        _mockOrchestratorLogger = new Mock<ILogger<AgentOrchestrator>>();
        _validator = new ScriptSchemaValidator();
    }

    [Fact]
    public async Task GenerateAsync_SingleIterationApproval_ReturnsApprovedScript()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Introduction to Machine Learning",
            Audience: "Beginners",
            Goal: "Understand ML basics",
            Tone: "Educational",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational"
        );

        var scriptText = @"# Introduction to Machine Learning

## What is Machine Learning?
Machine learning is a subset of artificial intelligence that enables computers to learn from data.

## Applications
Machine learning is used in many applications like recommendation systems and image recognition.";

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptText);

        _mockLlmProvider
            .Setup(x => x.GenerateVisualPromptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VisualStyle>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisualPromptResult(
                DetailedDescription: "Professional educational visual",
                CompositionGuidelines: "Clean, modern",
                LightingMood: "Natural",
                LightingDirection: "Front",
                LightingQuality: "High",
                TimeOfDay: "Day",
                ColorPalette: new[] { "Blue", "White" },
                ShotType: "Medium",
                CameraAngle: "Eye level",
                DepthOfField: "Medium",
                StyleKeywords: new[] { "Professional", "Educational" },
                NegativeElements: Array.Empty<string>(),
                ContinuityElements: Array.Empty<string>(),
                Reasoning: "Educational content"
            ));

        var screenwriter = new ScreenwriterAgent(_mockLlmProvider.Object, _mockScreenwriterLogger.Object);
        var visualDirector = new VisualDirectorAgent(_mockLlmProvider.Object, _mockVisualDirectorLogger.Object);
        var critic = new CriticAgent(_mockLlmProvider.Object, _validator, _mockCriticLogger.Object);
        var orchestrator = new AgentOrchestrator(screenwriter, visualDirector, critic, _mockOrchestratorLogger.Object);

        // Act
        var result = await orchestrator.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Script);
        Assert.NotEmpty(result.Script.RawText);
        Assert.True(result.ApprovedByCritic);
        Assert.Single(result.Iterations);
        Assert.Null(result.Iterations[0].CriticFeedback);
    }

    [Fact]
    public async Task GenerateAsync_MultipleIterationsWithFeedback_ReturnsAfterApproval()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Test",
            Tone: "Neutral",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        var poorScript = @"# Test Script
## Scene 1
Short content.";

        var goodScript = @"# Test Script

## Introduction
This is a comprehensive introduction to the topic.

## Main Content
This section contains detailed information about the subject.

## Conclusion
This wraps up the content nicely.";

        var callCount = 0;
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? poorScript : goodScript;
            });

        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Please add more content and structure to the script.");

        _mockLlmProvider
            .Setup(x => x.GenerateVisualPromptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VisualStyle>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisualPromptResult(
                DetailedDescription: "Test visual",
                CompositionGuidelines: "Test",
                LightingMood: "Natural",
                LightingDirection: "Front",
                LightingQuality: "High",
                TimeOfDay: "Day",
                ColorPalette: Array.Empty<string>(),
                ShotType: "Medium",
                CameraAngle: "Eye level",
                DepthOfField: "Medium",
                StyleKeywords: Array.Empty<string>(),
                NegativeElements: Array.Empty<string>(),
                ContinuityElements: Array.Empty<string>(),
                Reasoning: "Test"
            ));

        var screenwriter = new ScreenwriterAgent(_mockLlmProvider.Object, _mockScreenwriterLogger.Object);
        var visualDirector = new VisualDirectorAgent(_mockLlmProvider.Object, _mockVisualDirectorLogger.Object);
        var critic = new CriticAgent(_mockLlmProvider.Object, _validator, _mockCriticLogger.Object);
        var orchestrator = new AgentOrchestrator(screenwriter, visualDirector, critic, _mockOrchestratorLogger.Object);

        // Act
        var result = await orchestrator.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Iterations.Count >= 1);
        // First iteration should have feedback
        Assert.NotNull(result.Iterations[0].CriticFeedback);
    }

    [Fact]
    public async Task GenerateAsync_MaxIterationsReached_ReturnsBestResult()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Test",
            Tone: "Neutral",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        var poorScript = @"# Test Script
## Scene 1
Short.";

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(poorScript);

        _mockLlmProvider
            .Setup(x => x.CompleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Script needs improvement.");

        _mockLlmProvider
            .Setup(x => x.GenerateVisualPromptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VisualStyle>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisualPromptResult(
                DetailedDescription: "Test visual",
                CompositionGuidelines: "Test",
                LightingMood: "Natural",
                LightingDirection: "Front",
                LightingQuality: "High",
                TimeOfDay: "Day",
                ColorPalette: Array.Empty<string>(),
                ShotType: "Medium",
                CameraAngle: "Eye level",
                DepthOfField: "Medium",
                StyleKeywords: Array.Empty<string>(),
                NegativeElements: Array.Empty<string>(),
                ContinuityElements: Array.Empty<string>(),
                Reasoning: "Test"
            ));

        var screenwriter = new ScreenwriterAgent(_mockLlmProvider.Object, _mockScreenwriterLogger.Object);
        var visualDirector = new VisualDirectorAgent(_mockLlmProvider.Object, _mockVisualDirectorLogger.Object);
        var critic = new CriticAgent(_mockLlmProvider.Object, _validator, _mockCriticLogger.Object);
        var orchestrator = new AgentOrchestrator(screenwriter, visualDirector, critic, _mockOrchestratorLogger.Object);

        // Act
        var result = await orchestrator.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Iterations.Count <= 3); // Max iterations
        Assert.NotNull(result.Script);
    }

    [Fact]
    public async Task GenerateAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test",
            Audience: null,
            Goal: null,
            Tone: "Neutral",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        var screenwriter = new ScreenwriterAgent(_mockLlmProvider.Object, _mockScreenwriterLogger.Object);
        var visualDirector = new VisualDirectorAgent(_mockLlmProvider.Object, _mockVisualDirectorLogger.Object);
        var critic = new CriticAgent(_mockLlmProvider.Object, _validator, _mockCriticLogger.Object);
        var orchestrator = new AgentOrchestrator(screenwriter, visualDirector, critic, _mockOrchestratorLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => orchestrator.GenerateAsync(brief, spec, cts.Token));
    }

    [Fact]
    public async Task GenerateAsync_AgentCommunication_LogsInteractions()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Test",
            Tone: "Neutral",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        var scriptText = @"# Test Script

## Scene 1
Content for scene 1.

## Scene 2
Content for scene 2.";

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(scriptText);

        _mockLlmProvider
            .Setup(x => x.GenerateVisualPromptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VisualStyle>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VisualPromptResult(
                DetailedDescription: "Test visual",
                CompositionGuidelines: "Test",
                LightingMood: "Natural",
                LightingDirection: "Front",
                LightingQuality: "High",
                TimeOfDay: "Day",
                ColorPalette: Array.Empty<string>(),
                ShotType: "Medium",
                CameraAngle: "Eye level",
                DepthOfField: "Medium",
                StyleKeywords: Array.Empty<string>(),
                NegativeElements: Array.Empty<string>(),
                ContinuityElements: Array.Empty<string>(),
                Reasoning: "Test"
            ));

        var screenwriter = new ScreenwriterAgent(_mockLlmProvider.Object, _mockScreenwriterLogger.Object);
        var visualDirector = new VisualDirectorAgent(_mockLlmProvider.Object, _mockVisualDirectorLogger.Object);
        var critic = new CriticAgent(_mockLlmProvider.Object, _validator, _mockCriticLogger.Object);
        var orchestrator = new AgentOrchestrator(screenwriter, visualDirector, critic, _mockOrchestratorLogger.Object);

        // Act
        var result = await orchestrator.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Iterations.Count > 0);
        // Verify that iterations contain script and visual prompts
        Assert.All(result.Iterations, iteration =>
        {
            Assert.NotNull(iteration.Script);
            Assert.NotNull(iteration.VisualPrompts);
        });
    }
}

