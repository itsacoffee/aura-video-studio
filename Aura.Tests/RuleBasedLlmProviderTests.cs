using Xunit;
using Aura.Core.Models;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Tests;

public class RuleBasedLlmProviderTests
{
    [Fact]
    public async Task DraftScriptAsync_Should_GenerateScript()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var brief = new Brief(
            Topic: "Machine Learning Basics",
            Audience: "Students",
            Goal: "Educational",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.Contains("Machine Learning Basics", script);
        Assert.Contains("##", script); // Should contain section headers
    }

    [Fact]
    public async Task DraftScriptAsync_Should_GenerateMultipleScenes()
    {
        // Arrange
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var brief = new Brief(
            Topic: "Introduction to Programming",
            Audience: null,
            Goal: null,
            Tone: "Educational",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(8),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script);
        // Should have multiple scenes (Introduction + Body sections + Conclusion)
        var sectionCount = script.Split("##", StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.True(sectionCount >= 3, $"Expected at least 3 sections, got {sectionCount}");
    }

    [Theory]
    [InlineData(Pacing.Chill, 2, 180)]  // Slower pacing = fewer words
    [InlineData(Pacing.Fast, 2, 280)]   // Faster pacing = more words
    public async Task DraftScriptAsync_Should_AdjustLengthByPacing(Pacing pacing, int minutes, int minWords)
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
            Pacing: pacing,
            Density: Density.Balanced,
            Style: "Educational"
        );

        // Act
        var script = await provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

        // Assert
        var wordCount = script.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.True(wordCount >= minWords, $"Expected at least {minWords} words, got {wordCount}");
    }
}
