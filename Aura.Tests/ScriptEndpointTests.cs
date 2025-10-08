using Xunit;
using Aura.Core.Models;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Tests;

/// <summary>
/// Tests for script generation endpoint behavior and validation
/// </summary>
public class ScriptEndpointTests
{
    [Fact]
    public async Task ScriptGeneration_Should_WorkWithValidInput()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var brief = new Brief(
            Topic: "How to brew pour-over coffee",
            Audience: "Beginners",
            Goal: "Educational",
            Tone: "Conversational",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2.5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "How-to"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.Contains("brew", script, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("##", script); // Should have scene markers
    }

    [Fact]
    public async Task ScriptGeneration_Should_HandleDifferentDensities()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: null,
            Goal: null,
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        // Test Sparse
        var sparseSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Conversational,
            Density: Density.Sparse,
            Style: "Educational"
        );
        var sparseScript = await provider.DraftScriptAsync(brief, sparseSpec, CancellationToken.None);
        
        // Test Balanced
        var balancedSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational"
        );
        var balancedScript = await provider.DraftScriptAsync(brief, balancedSpec, CancellationToken.None);
        
        // Test Dense
        var denseSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(3),
            Pacing: Pacing.Conversational,
            Density: Density.Dense,
            Style: "Educational"
        );
        var denseScript = await provider.DraftScriptAsync(brief, denseSpec, CancellationToken.None);

        // Assert - Denser scripts should have more words
        int sparseWords = CountWords(sparseScript);
        int balancedWords = CountWords(balancedScript);
        int denseWords = CountWords(denseScript);
        
        Assert.True(sparseWords < balancedWords, $"Sparse ({sparseWords}) should be less than Balanced ({balancedWords})");
        Assert.True(balancedWords < denseWords, $"Balanced ({balancedWords}) should be less than Dense ({denseWords})");
    }

    [Fact]
    public async Task ScriptGeneration_Should_HandleAllAspectRatios()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Standard"
        );

        // Test each aspect ratio
        var aspectRatios = new[] { Aspect.Widescreen16x9, Aspect.Vertical9x16, Aspect.Square1x1 };
        
        foreach (var aspect in aspectRatios)
        {
            var brief = new Brief(
                Topic: "Test Topic",
                Audience: null,
                Goal: null,
                Tone: "Informative",
                Language: "en-US",
                Aspect: aspect
            );

            // Act
            var script = await provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

            // Assert
            Assert.NotNull(script);
            Assert.NotEmpty(script);
        }
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(5.0)]
    [InlineData(10.0)]
    public async Task ScriptGeneration_Should_HandleVariousDurations(double minutes)
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: null,
            Goal: null,
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(minutes),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        
        // Longer videos should have more content
        int wordCount = CountWords(script);
        int expectedMinWords = (int)(minutes * 100); // Rough estimate
        Assert.True(wordCount >= expectedMinWords, $"Expected at least {expectedMinWords} words for {minutes} minutes, got {wordCount}");
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
