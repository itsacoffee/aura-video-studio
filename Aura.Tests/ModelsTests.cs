using Xunit;
using Aura.Core.Models;
using System;

namespace Aura.Tests;

public class ModelsTests
{
    [Fact]
    public void Brief_Should_CreateCorrectly()
    {
        // Arrange & Act
        var brief = new Brief(
            Topic: "Introduction to AI",
            Audience: "Beginners",
            Goal: "Educational",
            Tone: "Informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        // Assert
        Assert.Equal("Introduction to AI", brief.Topic);
        Assert.Equal("Beginners", brief.Audience);
        Assert.Equal("Educational", brief.Goal);
        Assert.Equal("Informative", brief.Tone);
        Assert.Equal("en-US", brief.Language);
        Assert.Equal(Aspect.Widescreen16x9, brief.Aspect);
    }

    [Fact]
    public void PlanSpec_Should_CreateCorrectly()
    {
        // Arrange & Act
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(6),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Educational"
        );

        // Assert
        Assert.Equal(TimeSpan.FromMinutes(6), planSpec.TargetDuration);
        Assert.Equal(Pacing.Conversational, planSpec.Pacing);
        Assert.Equal(Density.Balanced, planSpec.Density);
        Assert.Equal("Educational", planSpec.Style);
    }

    [Fact]
    public void Scene_Should_CreateWithTimings()
    {
        // Arrange & Act
        var scene = new Scene(
            Index: 0,
            Heading: "Introduction",
            Script: "Welcome to this video about AI.",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(30)
        );

        // Assert
        Assert.Equal(0, scene.Index);
        Assert.Equal("Introduction", scene.Heading);
        Assert.Equal("Welcome to this video about AI.", scene.Script);
        Assert.Equal(TimeSpan.Zero, scene.Start);
        Assert.Equal(TimeSpan.FromSeconds(30), scene.Duration);
    }

    [Fact]
    public void Resolution_Should_CreateCorrectly()
    {
        // Arrange & Act
        var resolution = new Resolution(Width: 1920, Height: 1080);

        // Assert
        Assert.Equal(1920, resolution.Width);
        Assert.Equal(1080, resolution.Height);
    }
}
