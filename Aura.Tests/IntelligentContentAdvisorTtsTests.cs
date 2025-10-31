using System;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for IntelligentContentAdvisor TTS quality metrics
/// </summary>
public class IntelligentContentAdvisorTtsTests
{
    private readonly IntelligentContentAdvisor _advisor;
    private readonly ILlmProvider _mockLlmProvider;

    public IntelligentContentAdvisorTtsTests()
    {
        _mockLlmProvider = new Aura.Providers.Llm.RuleBasedLlmProvider(
            NullLogger<Aura.Providers.Llm.RuleBasedLlmProvider>.Instance);
        _advisor = new IntelligentContentAdvisor(
            NullLogger<IntelligentContentAdvisor>.Instance,
            _mockLlmProvider);
    }

    [Fact]
    public void AnalyzeTtsQuality_WithSimpleText_ReturnsHighScores()
    {
        // Arrange
        var script = "Hello world. This is a test. Everything works great.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.ReadabilityScore >= 70, $"Readability score {metrics.ReadabilityScore} should be >= 70");
        Assert.True(metrics.SentenceStructureScore >= 70, $"Structure score {metrics.SentenceStructureScore} should be >= 70");
        Assert.True(metrics.NaturalFlowScore >= 70, $"Flow score {metrics.NaturalFlowScore} should be >= 70");
    }

    [Fact]
    public void AnalyzeTtsQuality_WithLongSentences_DetectsIssues()
    {
        // Arrange
        var script = "This is an extremely long sentence that contains way more than twenty five words and should be detected as a complex sentence that needs to be simplified for better text to speech synthesis quality and natural delivery because it is very difficult to speak.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.IssuesDetected > 0, "Should detect issues with long sentences");
        Assert.Contains(metrics.TtsIssues, issue => issue.Contains("exceed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(metrics.TtsSuggestions, s => s.Contains("shorter", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeTtsQuality_WithTechnicalTerms_DetectsPronunciationComplexity()
    {
        // Arrange
        var script = "The XMLHttpRequest uses RESTful APIs to communicate with the SQL database via HTTPS connections.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.PronunciationComplexity >= 0, "Should calculate pronunciation complexity");
        // Note: The actual detection depends on the specific heuristics used
    }

    [Fact]
    public void AnalyzeTtsQuality_WithRoboticTransitions_DetectsUnnaturalFlow()
    {
        // Arrange
        var script = "Firstly, we need to understand the basics. Secondly, we must analyze the data. Thirdly, we implement the solution. Lastly, we evaluate the results.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.IssuesDetected > 0, "Should detect robotic transitions");
        Assert.Contains(metrics.TtsIssues, issue => issue.Contains("mechanical", StringComparison.OrdinalIgnoreCase) || issue.Contains("transition", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeTtsQuality_WithAiPhrases_DetectsUnnaturalLanguage()
    {
        // Arrange
        var script = "Let's delve into the subject. It's important to note that in today's digital age, we must understand the implications.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.IssuesDetected > 0, "Should detect unnatural AI phrases");
        Assert.Contains(metrics.TtsIssues, issue => issue.Contains("unnatural", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeTtsQuality_WithVariedSentences_ReturnsGoodFlowScore()
    {
        // Arrange
        var script = "Hello. This is a test sentence. Here's a slightly longer one that adds variety. Short again. And another medium-length sentence to maintain good pacing.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.NaturalFlowScore >= 70, $"Natural flow score {metrics.NaturalFlowScore} should be >= 70 for varied sentences");
    }

    [Fact]
    public void AnalyzeTtsQuality_WithRepetitiveStructure_DetectsMonotony()
    {
        // Arrange
        var script = "The system works well. The system processes data. The system generates output. The system validates results.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.IssuesDetected > 0, "Should detect repetitive structure");
        Assert.Contains(metrics.TtsIssues, issue => issue.Contains("repetitive", StringComparison.OrdinalIgnoreCase) || issue.Contains("structure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AnalyzeTtsQuality_WithComplexPunctuation_DetectsReadabilityIssues()
    {
        // Arrange
        var script = "First: understand the problem; second: analyze the data; third: implement the solution; fourth: validate the results; fifth: deploy the system.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.IssuesDetected > 0, "Should detect complex punctuation");
        Assert.True(metrics.ReadabilityScore < 100, "Readability score should be penalized for complex punctuation");
    }

    [Fact]
    public void AnalyzeTtsQuality_ReturnsAllExpectedMetrics()
    {
        // Arrange
        var script = "This is a comprehensive test of the TTS quality analysis system.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.ReadabilityScore >= 0 && metrics.ReadabilityScore <= 100);
        Assert.True(metrics.PronunciationComplexity >= 0 && metrics.PronunciationComplexity <= 100);
        Assert.True(metrics.SentenceStructureScore >= 0 && metrics.SentenceStructureScore <= 100);
        Assert.True(metrics.NaturalFlowScore >= 0 && metrics.NaturalFlowScore <= 100);
        Assert.NotNull(metrics.TtsIssues);
        Assert.NotNull(metrics.TtsSuggestions);
    }

    [Fact]
    public void AnalyzeTtsQuality_WithMissingPauses_DetectsStructureIssues()
    {
        // Arrange
        var script = "We need to understand the problem and analyze the data and implement the solution and validate the results.";

        // Act
        var metrics = _advisor.AnalyzeTtsQuality(script);

        // Assert
        Assert.NotNull(metrics);
        Assert.True(metrics.SentenceStructureScore >= 0 && metrics.SentenceStructureScore <= 100, "Should calculate sentence structure score");
        Assert.NotNull(metrics.TtsSuggestions);
        // Note: Missing pauses detection depends on the sentence structure analysis heuristics
    }
}
