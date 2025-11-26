using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.AI.Templates;
using Aura.Core.AI.Validation;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.AI;

/// <summary>
/// Tests for ScriptGenerationPipeline with validation, retry, and fallback
/// </summary>
public class ScriptGenerationPipelineTests
{
    private readonly Mock<ILlmProvider> _mockLlmProvider;
    private readonly Mock<ILogger<ScriptGenerationPipeline>> _mockLogger;
    private readonly ScriptSchemaValidator _validator;
    private readonly FallbackScriptGenerator _fallbackGenerator;
    private readonly ScriptGenerationPipeline _pipeline;

    public ScriptGenerationPipelineTests()
    {
        _mockLlmProvider = new Mock<ILlmProvider>();
        _mockLogger = new Mock<ILogger<ScriptGenerationPipeline>>();
        _validator = new ScriptSchemaValidator();
        _fallbackGenerator = new FallbackScriptGenerator();
        _pipeline = new ScriptGenerationPipeline(
            _mockLlmProvider.Object,
            _validator,
            _fallbackGenerator,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GenerateAsync_ValidScriptOnFirstAttempt_ReturnsSuccess()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Introduction to AI",
            Audience: "General",
            Goal: "Educate",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational");

        var validScript = @"# Introduction to AI

## Introduction
Welcome to this video about Introduction to AI. Today we'll explore what artificial intelligence is and how it works.

## Main Concepts
Artificial intelligence is a fascinating field that combines computer science and cognitive science. It enables machines to learn, reason, and make decisions.

## Conclusion
We've covered the basics of Introduction to AI. Thank you for watching!";

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validScript);

        // Act
        var result = await _pipeline.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.UsedFallback);
        Assert.Equal(validScript, result.Script);
        Assert.True(result.QualityScore >= 0.6);
        Assert.Single(result.Attempts);
        Assert.Null(result.Attempts[0].Error);
        Assert.NotNull(result.Attempts[0].Validation);
        Assert.True(result.Attempts[0].Validation!.IsValid);
    }

    [Fact]
    public async Task GenerateAsync_InvalidScript_RetriesWithModifiedPrompt()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Machine Learning",
            Audience: "General",
            Goal: "Educate",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational");

        var invalidScript1 = "This is too short and has no structure.";
        var invalidScript2 = @"# Machine Learning

## Introduction
This script is about Machine Learning. Machine Learning is important.

## Conclusion
Thank you for watching!";

        var validScript = @"# Machine Learning

## Introduction
Welcome to this video about Machine Learning. Today we'll explore what machine learning is and how it works.

## Main Concepts
Machine learning is a subset of artificial intelligence that enables systems to learn from data. It's revolutionizing many industries.

## Conclusion
We've covered the basics of Machine Learning. Thank you for watching!";

        var callCount = 0;
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount switch
                {
                    1 => invalidScript1,
                    2 => invalidScript2,
                    _ => validScript
                };
            });

        // Act
        var result = await _pipeline.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.False(result.UsedFallback);
        Assert.Equal(validScript, result.Script);
        Assert.Equal(3, result.Attempts.Count);
        
        // First attempt should have validation errors
        Assert.NotNull(result.Attempts[0].Validation);
        Assert.False(result.Attempts[0].Validation!.IsValid);
        Assert.NotEmpty(result.Attempts[0].Validation.Errors);
        
        // Second attempt should also have validation errors
        Assert.NotNull(result.Attempts[1].Validation);
        Assert.False(result.Attempts[1].Validation!.IsValid);
        
        // Third attempt should succeed
        Assert.NotNull(result.Attempts[2].Validation);
        Assert.True(result.Attempts[2].Validation!.IsValid);
    }

    [Fact]
    public async Task GenerateAsync_AllRetriesFail_UsesFallback()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Quantum Computing",
            Audience: "General",
            Goal: "Educate",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational");

        var invalidScript = "I cannot generate this script.";

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidScript);

        // Act
        var result = await _pipeline.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.UsedFallback);
        Assert.NotNull(result.Script);
        Assert.Contains("Quantum Computing", result.Script);
        Assert.Equal(3, result.Attempts.Count);
        
        // All attempts should have failed validation
        foreach (var attempt in result.Attempts)
        {
            Assert.NotNull(attempt.Validation);
            Assert.False(attempt.Validation!.IsValid);
        }
    }

    [Fact]
    public async Task GenerateAsync_ProviderThrowsException_RetriesAndFallsBack()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Blockchain Technology",
            Audience: "General",
            Goal: "Educate",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational");

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Network error"));

        // Act
        var result = await _pipeline.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.UsedFallback);
        Assert.NotNull(result.Script);
        Assert.Equal(3, result.Attempts.Count);
        
        // All attempts should have errors
        foreach (var attempt in result.Attempts)
        {
            Assert.NotNull(attempt.Error);
            Assert.Contains("Network error", attempt.Error);
        }
    }

    [Fact]
    public async Task GenerateAsync_EmptyScript_RetriesAndFallsBack()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Cloud Computing",
            Audience: "General",
            Goal: "Educate",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational");

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        // Act
        var result = await _pipeline.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        Assert.True(result.UsedFallback);
        Assert.NotNull(result.Script);
        Assert.NotEmpty(result.Script);
    }

    [Fact]
    public async Task GenerateAsync_CancellationRequested_PropagatesCancellation()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Data Science",
            Audience: "General",
            Goal: "Educate",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _pipeline.GenerateAsync(brief, spec, cts.Token));
    }

    [Fact]
    public async Task GenerateAsync_CapturesMetrics()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Cybersecurity",
            Audience: "General",
            Goal: "Educate",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational");

        var validScript = @"# Cybersecurity

## Introduction
Welcome to this video about Cybersecurity. Today we'll explore important security concepts.

## Main Concepts
Cybersecurity is crucial in our digital age. Understanding threats and defenses is essential for everyone.

## Conclusion
We've covered the basics of Cybersecurity. Stay safe online!";

        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(validScript);

        // Act
        var result = await _pipeline.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Metrics);
        Assert.True(result.Metrics.SceneCount >= 2);
        Assert.True(result.Metrics.TotalCharacters > 0);
        Assert.True(result.Metrics.AverageSceneLength > 0);
        Assert.True(result.Metrics.ReadabilityScore >= 0.0);
    }

    [Fact]
    public async Task GenerateAsync_PromptModificationIncludesFeedback()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Renewable Energy",
            Audience: "General",
            Goal: "Educate",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational");

        var invalidScript = "Too short";
        var validScript = @"# Renewable Energy

## Introduction
Welcome to this video about Renewable Energy. Today we'll explore sustainable energy sources.

## Main Concepts
Renewable energy comes from natural sources like sun, wind, and water. These sources are sustainable and environmentally friendly.

## Conclusion
We've covered the basics of Renewable Energy. Thank you for watching!";

        var callCount = 0;
        _mockLlmProvider
            .Setup(x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? invalidScript : validScript;
            });

        // Act
        var result = await _pipeline.GenerateAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Attempts.Count);
        
        // Verify that the provider was called multiple times (prompt modification should happen)
        _mockLlmProvider.Verify(
            x => x.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public void ScriptSchemaValidator_ParseScenes_HandlesEmptyLines()
    {
        // Arrange
        var script = @"# Test Script

## Scene One

This is the first scene.

It has multiple paragraphs.

## Scene Two

This is the second scene.

With more content.";

        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test");

        // Act
        var result = _validator.Validate(script, brief, spec);

        // Assert
        Assert.True(result.Metrics.SceneCount >= 2);
        Assert.True(result.IsValid || result.QualityScore >= 0.5);
    }

    [Fact]
    public void ScriptSchemaValidator_ParseScenes_HandlesSpecialCharacters()
    {
        // Arrange
        var script = @"# Test Script with Special Chars: ""quotes"", 'apostrophes', & symbols!

## Scene 1: Introduction
This scene has special characters: @#$%^&*()[]{}|\\:;""'<>?,./

## Scene 2: Main Content
More content with émojis 🎬 and unicode: 中文 العربية";

        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test");

        // Act
        var result = _validator.Validate(script, brief, spec);

        // Assert
        Assert.True(result.Metrics.SceneCount >= 2);
        // Should not throw exceptions with special characters
    }

    [Fact]
    public void ScriptSchemaValidator_ParseScenes_HandlesNumberedScenes()
    {
        // Arrange
        var script = @"# Test Script

Scene 1: Introduction
This is the introduction scene.

Scene 2: Main Content
This is the main content scene.

Scene 3: Conclusion
This is the conclusion.";

        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test");

        // Act
        var result = _validator.Validate(script, brief, spec);

        // Assert
        Assert.True(result.Metrics.SceneCount >= 2);
    }

    [Fact]
    public void ScriptSchemaValidator_ParseScenes_HandlesNoSceneMarkers()
    {
        // Arrange - Script without explicit scene markers
        var script = @"# Test Script Title

This is a script without explicit scene markers. It should still be parsed as a single scene or multiple scenes based on content structure.

The content continues here with more paragraphs and information.

And even more content to make it substantial.";

        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test");

        // Act
        var result = _validator.Validate(script, brief, spec);

        // Assert - Should parse as at least one scene
        Assert.True(result.Metrics.SceneCount >= 1);
    }

    [Fact]
    public void ScriptSchemaValidator_ParseScenes_HandlesSectionDividers()
    {
        // Arrange - Script with section dividers
        var script = @"# Test Script

## Scene One
This is scene one content.

---

## Scene Two
This is scene two content.

***

## Scene Three
This is scene three content.";

        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test");

        // Act
        var result = _validator.Validate(script, brief, spec);

        // Assert
        Assert.True(result.Metrics.SceneCount >= 2);
    }

    [Fact]
    public void ScriptSchemaValidator_ParseScenes_HandlesEmptyHeadings()
    {
        // Arrange - Script with empty scene headings
        var script = @"# Test Script

## 
This scene has an empty heading but has content.

## Scene Two
This scene has a proper heading.";

        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test");

        // Act
        var result = _validator.Validate(script, brief, spec);

        // Assert - Should handle empty headings gracefully
        Assert.True(result.Metrics.SceneCount >= 1);
    }

    [Fact]
    public void ScriptSchemaValidator_ParseScenes_HandlesWhitespaceOnlyLines()
    {
        // Arrange - Script with lots of whitespace
        var script = @"# Test Script
   
## Scene One
   
   This scene has whitespace.
   
   More content here.
   
## Scene Two
   
   Another scene with whitespace.";

        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Informative",
            Language: "en",
            Aspect: Aspect.Widescreen16x9);

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test");

        // Act
        var result = _validator.Validate(script, brief, spec);

        // Assert
        Assert.True(result.Metrics.SceneCount >= 2);
    }
}

