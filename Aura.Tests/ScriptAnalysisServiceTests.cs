using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ScriptEnhancement;
using Aura.Core.Services.ScriptEnhancement;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Xunit;
using Moq;

namespace Aura.Tests;

public class ScriptAnalysisServiceTests
{
    private readonly Mock<ILogger<ScriptAnalysisService>> _mockLogger;
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly ScriptAnalysisService _service;

    public ScriptAnalysisServiceTests()
    {
        _mockLogger = new Mock<ILogger<ScriptAnalysisService>>();
        _mockLlmProvider = new Mock<ILlmProvider>();
        _service = new ScriptAnalysisService(_mockLogger.Object, _mockLlmProvider.Object);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_WithValidScript_ReturnsAnalysis()
    {
        // Arrange
        var script = @"# Introduction
Welcome to this amazing tutorial!

## Setup
Let's get started with the basics.

## Main Content
Here's what you need to know.

## Conclusion
Thanks for watching!";

        // Act
        var result = await _service.AnalyzeScriptAsync(script, "Tutorial", "Beginners", null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.StructureScore > 0);
        Assert.True(result.EngagementScore > 0);
        Assert.True(result.ClarityScore > 0);
        Assert.True(result.HookStrength >= 0);
        Assert.NotEmpty(result.EmotionalCurve);
        Assert.NotNull(result.ReadabilityMetrics);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_WithQuestionInHook_HigherHookStrength()
    {
        // Arrange
        var scriptWithQuestion = "Have you ever wondered how to create amazing videos?";
        var scriptWithoutQuestion = "Welcome to my video tutorial.";

        // Act
        var resultWith = await _service.AnalyzeScriptAsync(scriptWithQuestion, null, null, null, CancellationToken.None);
        var resultWithout = await _service.AnalyzeScriptAsync(scriptWithoutQuestion, null, null, null, CancellationToken.None);

        // Assert
        Assert.True(resultWith.HookStrength > resultWithout.HookStrength);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_CalculatesReadabilityMetrics()
    {
        // Arrange
        var script = "This is a simple sentence. This is another simple sentence.";

        // Act
        var result = await _service.AnalyzeScriptAsync(script, null, null, null, CancellationToken.None);

        // Assert
        Assert.Contains("fleschReadingEase", result.ReadabilityMetrics.Keys);
        Assert.Contains("fleschKincaidGrade", result.ReadabilityMetrics.Keys);
        Assert.Contains("totalWords", result.ReadabilityMetrics.Keys);
        Assert.Contains("totalSentences", result.ReadabilityMetrics.Keys);
        Assert.True(result.ReadabilityMetrics["fleschReadingEase"] >= 0);
        Assert.True(result.ReadabilityMetrics["fleschReadingEase"] <= 100);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_DetectsStoryFramework()
    {
        // Arrange
        var problemSolutionScript = @"Here's the problem: people struggle with video creation.
The solution is simple: use our tool.";

        // Act
        var result = await _service.AnalyzeScriptAsync(problemSolutionScript, null, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result.DetectedFramework);
        Assert.Equal(StoryFrameworkType.ProblemSolution, result.DetectedFramework);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_IdentifiesIssuesAndStrengths()
    {
        // Arrange
        var poorScript = "This is a very long sentence with way too many words that goes on and on and on without any clear structure or purpose and makes it very difficult to read and understand what the author is trying to say which is a common problem in writing.";
        
        var goodScript = @"What's the secret to great videos?

It's simple: clear structure and engaging content.

Let me show you how.";

        // Act
        var poorResult = await _service.AnalyzeScriptAsync(poorScript, null, null, null, CancellationToken.None);
        var goodResult = await _service.AnalyzeScriptAsync(goodScript, null, null, null, CancellationToken.None);

        // Assert
        Assert.NotEmpty(poorResult.Issues);
        Assert.Contains(poorResult.Issues, i => i.Contains("long"));
        
        Assert.NotEmpty(goodResult.Strengths);
        Assert.True(goodResult.HookStrength > poorResult.HookStrength);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_EmptyScript_ReturnsLowScores()
    {
        // Arrange
        var emptyScript = "No content";

        // Act
        var result = await _service.AnalyzeScriptAsync(emptyScript, null, null, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.StructureScore >= 0);
    }

    [Fact]
    public async Task AnalyzeScriptAsync_DetectsEmotionalTones()
    {
        // Arrange
        var script = @"This is exciting news!
But we have a problem to solve.
Hopefully, we can find a solution.";

        // Act
        var result = await _service.AnalyzeScriptAsync(script, null, null, null, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.EmotionalCurve);
        Assert.Contains(result.EmotionalCurve, p => p.Tone == EmotionalTone.Excited || p.Tone == EmotionalTone.Concerned || p.Tone == EmotionalTone.Hopeful);
    }
}
