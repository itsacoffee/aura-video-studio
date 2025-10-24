using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Services.PacingServices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for the EmotionalBeatAnalyzer service
/// </summary>
public class EmotionalBeatAnalyzerTests
{
    private readonly EmotionalBeatAnalyzer _analyzer;

    public EmotionalBeatAnalyzerTests()
    {
        var logger = NullLogger<EmotionalBeatAnalyzer>.Instance;
        _analyzer = new EmotionalBeatAnalyzer(logger);
    }

    [Fact]
    public async Task AnalyzeEmotionalBeatsAsync_WithMultipleScenes_ReturnsBeatsForAllScenes()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Intro", "Welcome to this amazing video!", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Main", "Let's explore this topic.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
            new Scene(2, "End", "Thanks for watching!", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15))
        };

        // Act
        var beats = await _analyzer.AnalyzeEmotionalBeatsAsync(scenes);

        // Assert
        Assert.Equal(3, beats.Count);
        Assert.All(beats, b => Assert.InRange(b.EmotionalIntensity, 0, 100));
    }

    [Fact]
    public async Task AnalyzeEmotionalBeatsAsync_WithHighIntensityWords_DetectsHighIntensity()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Exciting", "This is amazing! Incredible! Shocking news!", TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };

        // Act
        var beats = await _analyzer.AnalyzeEmotionalBeatsAsync(scenes);

        // Assert
        Assert.Single(beats);
        Assert.True(beats[0].EmotionalIntensity > 60, $"Expected high intensity, got {beats[0].EmotionalIntensity}");
    }

    [Fact]
    public async Task AnalyzeEmotionalBeatsAsync_WithCalmContent_DetectsLowIntensity()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Calm", "Everything is peaceful and calm.", TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };

        // Act
        var beats = await _analyzer.AnalyzeEmotionalBeatsAsync(scenes);

        // Assert
        Assert.Single(beats);
        Assert.Equal("calm", beats[0].PrimaryEmotion);
    }

    [Fact]
    public async Task AnalyzeEmotionalBeatsAsync_IdentifiesPeaksAndValleys()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Low", "Something simple.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Peak", "Amazing! Incredible! Shocking! Must see!", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
            new Scene(2, "Low", "Back to normal.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15))
        };

        // Act
        var beats = await _analyzer.AnalyzeEmotionalBeatsAsync(scenes);

        // Assert
        Assert.Equal(3, beats.Count);
        Assert.True(beats.Any(b => b.IsPeak), "Expected at least one peak");
    }

    [Fact]
    public async Task AnalyzeEmotionalBeatsAsync_CalculatesArcPositions()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "First", "Scene one.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Second", "Scene two.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
            new Scene(2, "Third", "Scene three.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15))
        };

        // Act
        var beats = await _analyzer.AnalyzeEmotionalBeatsAsync(scenes);

        // Assert
        Assert.Equal(3, beats.Count);
        Assert.Equal(0.0, beats[0].ArcPosition);
        Assert.Equal(0.5, beats[1].ArcPosition);
        Assert.Equal(1.0, beats[2].ArcPosition);
    }

    [Fact]
    public async Task AnalyzeEmotionalBeatsAsync_DetectsEmotionalChange()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Calm", "Everything is peaceful.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Rising", "This is getting exciting! Amazing!", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var beats = await _analyzer.AnalyzeEmotionalBeatsAsync(scenes);

        // Assert
        Assert.Equal(2, beats.Count);
        Assert.Equal(EmotionalChange.Rising, beats[1].EmotionalChange);
    }

    [Fact]
    public async Task AnalyzeEmotionalBeatsAsync_AssignsCorrectTimestamps()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "First", "Scene one.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Second", "Scene two.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var beats = await _analyzer.AnalyzeEmotionalBeatsAsync(scenes);

        // Assert
        Assert.Equal(TimeSpan.Zero, beats[0].Timestamp);
        Assert.Equal(TimeSpan.FromSeconds(5), beats[1].Timestamp);
    }

    [Fact]
    public async Task AnalyzeEmotionalBeatsAsync_RecommendsPacingEmphasis()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "High", "This is amazing! Incredible! Must see!", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Low", "Now things are calm.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var beats = await _analyzer.AnalyzeEmotionalBeatsAsync(scenes);

        // Assert
        Assert.Equal(2, beats.Count);
        Assert.Equal(PacingEmphasis.More, beats[0].RecommendedEmphasis);
        Assert.Equal(PacingEmphasis.Less, beats[1].RecommendedEmphasis);
    }
}
