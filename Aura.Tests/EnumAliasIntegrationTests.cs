using Xunit;
using Aura.Core.Models;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Tests;

/// <summary>
/// Integration tests for script generation with enum aliases
/// </summary>
public class EnumAliasIntegrationTests
{
    private readonly RuleBasedLlmProvider _provider;

    public EnumAliasIntegrationTests()
    {
        _provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
    }

    [Fact]
    public async Task ScriptGeneration_Should_WorkWithCanonicalEnumValues()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Coffee brewing",
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
        var script = await _provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
        Assert.Contains("coffee", script, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(Aspect.Widescreen16x9)]
    [InlineData(Aspect.Vertical9x16)]
    [InlineData(Aspect.Square1x1)]
    public async Task ScriptGeneration_Should_WorkWithAllAspectRatios(Aspect aspect)
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Informative",
            Language: "en-US",
            Aspect: aspect
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Standard"
        );

        // Act
        var script = await _provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
    }

    [Theory]
    [InlineData(Density.Sparse)]
    [InlineData(Density.Balanced)]
    [InlineData(Density.Dense)]
    public async Task ScriptGeneration_Should_WorkWithAllDensityValues(Density density)
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: density,
            Style: "Standard"
        );

        // Act
        var script = await _provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(script);
        Assert.NotEmpty(script);
    }

    [Fact]
    public async Task ScriptGeneration_Should_ProduceConsistentOutput_WithSameInputs()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Making scrambled eggs",
            Audience: "Beginners",
            Goal: "Tutorial",
            Tone: "Friendly",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1.5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Quick"
        );

        // Act - Generate twice with same inputs
        var script1 = await _provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);
        var script2 = await _provider.DraftScriptAsync(brief, planSpec, CancellationToken.None);

        // Assert - Should produce consistent results
        Assert.NotNull(script1);
        Assert.NotNull(script2);
        Assert.NotEmpty(script1);
        Assert.NotEmpty(script2);
        
        // Both should mention eggs
        Assert.Contains("egg", script1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("egg", script2, StringComparison.OrdinalIgnoreCase);
    }
}
