using Xunit;
using Aura.Core.Timeline;
using Aura.Core.Models;
using System;
using System.Collections.Generic;

namespace Aura.Tests;

public class TimelineBuilderTests
{
    [Fact]
    public void BuildTimeline_Should_CreateValidTimeline()
    {
        // Arrange
        var builder = new TimelineBuilder();
        var scenes = new List<Scene>
        {
            new Scene(0, "Introduction", "Welcome to this video.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Content", "Let me explain the topic.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var timeline = builder.BuildTimeline(scenes, "/path/to/narration.wav");

        // Assert
        Assert.NotNull(timeline);
        Assert.Equal(2, timeline.Scenes.Count);
        Assert.Equal("/path/to/narration.wav", timeline.NarrationPath);
        Assert.Empty(timeline.MusicPath);
    }

    [Fact]
    public void CalculateSceneTimings_Should_DistributeTimeProportionally()
    {
        // Arrange
        var builder = new TimelineBuilder();
        var scenes = new List<Scene>
        {
            new Scene(0, "Short", "Short scene.", TimeSpan.Zero, TimeSpan.Zero),
            new Scene(1, "Long", "This is a much longer scene with many more words to say.", TimeSpan.Zero, TimeSpan.Zero)
        };

        // Act
        var timedScenes = builder.CalculateSceneTimings(scenes, TimeSpan.FromMinutes(1), Pacing.Conversational);

        // Assert
        Assert.Equal(2, timedScenes.Count);
        Assert.True(timedScenes[1].Duration > timedScenes[0].Duration, "Longer scene should have more duration");
        Assert.Equal(TimeSpan.Zero, timedScenes[0].Start);
        Assert.True(timedScenes[1].Start > TimeSpan.Zero, "Second scene should start after first");
    }

    [Fact]
    public void CalculateSceneTimings_Should_HandleEmptySceneList()
    {
        // Arrange
        var builder = new TimelineBuilder();
        var scenes = new List<Scene>();

        // Act
        var timedScenes = builder.CalculateSceneTimings(scenes, TimeSpan.FromMinutes(1), Pacing.Conversational);

        // Assert
        Assert.Empty(timedScenes);
    }

    [Fact]
    public void GenerateSubtitles_Should_CreateSRT()
    {
        // Arrange
        var builder = new TimelineBuilder();
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "First subtitle.", TimeSpan.Zero, TimeSpan.FromSeconds(3)),
            new Scene(1, "Scene 2", "Second subtitle.", TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3))
        };

        // Act
        var srt = builder.GenerateSubtitles(scenes, "SRT");

        // Assert
        Assert.NotNull(srt);
        Assert.Contains("1", srt);
        Assert.Contains("00:00:00,000", srt);
        Assert.Contains("First subtitle.", srt);
        Assert.Contains("Second subtitle.", srt);
    }

    [Fact]
    public void GenerateSubtitles_Should_CreateVTT()
    {
        // Arrange
        var builder = new TimelineBuilder();
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "First subtitle.", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };

        // Act
        var vtt = builder.GenerateSubtitles(scenes, "VTT");

        // Assert
        Assert.NotNull(vtt);
        Assert.StartsWith("WEBVTT", vtt);
        Assert.Contains("00:00:00.000", vtt);
        Assert.Contains("First subtitle.", vtt);
    }

    [Fact]
    public void GenerateSubtitles_Should_ThrowForUnsupportedFormat()
    {
        // Arrange
        var builder = new TimelineBuilder();
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test.", TimeSpan.Zero, TimeSpan.FromSeconds(3))
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.GenerateSubtitles(scenes, "UNKNOWN"));
    }

    [Fact]
    public void AddSceneAssets_Should_UpdateTimeline()
    {
        // Arrange
        var builder = new TimelineBuilder();
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Content", TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };
        var timeline = builder.BuildTimeline(scenes, "/path/to/narration.wav");
        var assets = new List<Asset>
        {
            new Asset("image", "/path/to/image.jpg", "CC0", "Pixabay")
        };

        // Act
        var updatedTimeline = builder.AddSceneAssets(timeline, 0, assets);

        // Assert
        Assert.Single(updatedTimeline.SceneAssets[0]);
        Assert.Equal("/path/to/image.jpg", updatedTimeline.SceneAssets[0][0].PathOrUrl);
    }
}
