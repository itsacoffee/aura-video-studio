using Xunit;
using Aura.Core.Models;
using System;

namespace Aura.Tests;

public class PlanSpecSceneCountTests
{
    [Theory]
    [InlineData(60, Density.Sparse, 3)]      // 60s / 20s = 3 scenes
    [InlineData(60, Density.Balanced, 5)]    // 60s / 12s = 5 scenes
    [InlineData(60, Density.Dense, 8)]       // 60s / 8s = ~8 scenes (rounded up)
    [InlineData(120, Density.Sparse, 6)]     // 120s / 20s = 6 scenes
    [InlineData(120, Density.Balanced, 10)]  // 120s / 12s = 10 scenes
    [InlineData(120, Density.Dense, 15)]     // 120s / 8s = 15 scenes
    public void GetCalculatedSceneCount_CalculatesBasedOnDensity(int durationSeconds, Density density, int expectedSceneCount)
    {
        // Arrange
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(durationSeconds),
            Pacing: Pacing.Conversational,
            Density: density,
            Style: "Informative"
        );

        // Act
        var result = planSpec.GetCalculatedSceneCount();

        // Assert
        Assert.Equal(expectedSceneCount, result);
    }

    [Fact]
    public void GetCalculatedSceneCount_RespectsMinimum()
    {
        // Arrange - Very short duration that would calculate to < 3 scenes
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(10),
            Pacing: Pacing.Conversational,
            Density: Density.Sparse,
            Style: "Informative"
        );

        // Act
        var result = planSpec.GetCalculatedSceneCount();

        // Assert - Should be at least 3 (the default minimum)
        Assert.True(result >= 3);
    }

    [Fact]
    public void GetCalculatedSceneCount_RespectsMaximum()
    {
        // Arrange - Very long duration that would calculate to > 20 scenes
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(300), // 5 minutes
            Pacing: Pacing.Conversational,
            Density: Density.Dense, // 300s / 8s = 37.5 scenes
            Style: "Informative"
        );

        // Act
        var result = planSpec.GetCalculatedSceneCount();

        // Assert - Should be at most 20 (the default maximum)
        Assert.True(result <= 20);
    }

    [Fact]
    public void GetCalculatedSceneCount_UsesCustomMinimum()
    {
        // Arrange
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(10),
            Pacing: Pacing.Conversational,
            Density: Density.Sparse,
            Style: "Informative",
            MinSceneCount: 5
        );

        // Act
        var result = planSpec.GetCalculatedSceneCount();

        // Assert
        Assert.True(result >= 5);
    }

    [Fact]
    public void GetCalculatedSceneCount_UsesCustomMaximum()
    {
        // Arrange
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(300),
            Pacing: Pacing.Conversational,
            Density: Density.Dense,
            Style: "Informative",
            MaxSceneCount: 10
        );

        // Act
        var result = planSpec.GetCalculatedSceneCount();

        // Assert
        Assert.True(result <= 10);
    }

    [Fact]
    public void GetCalculatedSceneCount_UsesTargetWhenExplicitlySet()
    {
        // Arrange
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced, // Would calculate to 5 scenes
            Style: "Informative",
            TargetSceneCount: 8 // Explicitly set to 8
        );

        // Act
        var result = planSpec.GetCalculatedSceneCount();

        // Assert
        Assert.Equal(8, result);
    }

    [Fact]
    public void GetCalculatedSceneCount_TargetRespectsBounds()
    {
        // Arrange - Target set outside bounds
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Informative",
            MinSceneCount: 5,
            MaxSceneCount: 10,
            TargetSceneCount: 15 // Above max
        );

        // Act
        var result = planSpec.GetCalculatedSceneCount();

        // Assert - Should be clamped to max
        Assert.Equal(10, result);
    }

    [Fact]
    public void GetCalculatedSceneCount_TargetRespectsBounds_BelowMin()
    {
        // Arrange - Target set below minimum
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Informative",
            MinSceneCount: 5,
            MaxSceneCount: 10,
            TargetSceneCount: 2 // Below min
        );

        // Act
        var result = planSpec.GetCalculatedSceneCount();

        // Assert - Should be clamped to min
        Assert.Equal(5, result);
    }

    [Theory]
    [InlineData(30, Density.Balanced, 3)]    // 30s / 12s = 2.5, ceil = 3
    [InlineData(45, Density.Balanced, 4)]    // 45s / 12s = 3.75, ceil = 4
    [InlineData(90, Density.Balanced, 8)]    // 90s / 12s = 7.5, ceil = 8
    public void GetCalculatedSceneCount_RoundsUpCorrectly(int durationSeconds, Density density, int expectedSceneCount)
    {
        // Arrange
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(durationSeconds),
            Pacing: Pacing.Conversational,
            Density: density,
            Style: "Informative"
        );

        // Act
        var result = planSpec.GetCalculatedSceneCount();

        // Assert
        Assert.Equal(expectedSceneCount, result);
    }
}
