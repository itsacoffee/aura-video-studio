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

    [Fact]
    public void VisualComposition_IntegrationTest_WithStillsAndBrandKit()
    {
        // Arrange - Create a simple storyboard with scenes
        var builder = new TimelineBuilder();
        var scenes = new List<Scene>
        {
            new Scene(0, "Opening", "Welcome to our presentation.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Main Content", "Here is the main content.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10)),
            new Scene(2, "Closing", "Thank you for watching.", TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(5))
        };

        // Create timeline with narration
        var timeline = builder.BuildTimeline(scenes, "/tmp/narration.wav", musicPath: "/tmp/music.mp3");

        // Add stock assets to each scene (simulating stock provider results)
        var sceneAssets = new List<Asset>
        {
            new Asset("image", "/tmp/stock1.jpg", "Pexels License", "Pexels"),
            new Asset("image", "/tmp/stock2.jpg", "Pixabay License", "Pixabay"),
            new Asset("image", "/tmp/stock3.jpg", "Unsplash License", "Unsplash")
        };

        // Add assets to each scene
        timeline = builder.AddSceneAssets(timeline, 0, new[] { sceneAssets[0] });
        timeline = builder.AddSceneAssets(timeline, 1, new[] { sceneAssets[1] });
        timeline = builder.AddSceneAssets(timeline, 2, new[] { sceneAssets[2] });

        // Create FFmpeg plan with brand kit and Ken Burns
        var ffmpegBuilder = new Aura.Core.Rendering.FFmpegPlanBuilder();
        var brandKit = new BrandKit(
            WatermarkPath: "/tmp/logo.png",
            WatermarkPosition: "bottom-right",
            WatermarkOpacity: 0.8f,
            BrandColor: "#FF6B35",
            AccentColor: "#00D9FF");

        var resolution = new Resolution(1920, 1080);
        var filterGraph = ffmpegBuilder.BuildFilterGraph(
            resolution,
            addSubtitles: true,
            subtitlePath: "/tmp/subtitles.srt",
            brandKit: brandKit,
            enableKenBurns: true);

        // Act & Assert - Verify timeline was composed correctly
        Assert.NotNull(timeline);
        Assert.Equal(3, timeline.Scenes.Count);
        Assert.Equal(3, timeline.SceneAssets.Count);
        Assert.Single(timeline.SceneAssets[0]);
        Assert.Single(timeline.SceneAssets[1]);
        Assert.Single(timeline.SceneAssets[2]);

        // Verify each scene has appropriate asset
        Assert.Equal("Pexels", timeline.SceneAssets[0][0].Attribution);
        Assert.Equal("Pixabay", timeline.SceneAssets[1][0].Attribution);
        Assert.Equal("Unsplash", timeline.SceneAssets[2][0].Attribution);

        // Verify filter graph includes all features
        Assert.Contains("scale=1920:1080", filterGraph);
        Assert.Contains("zoompan", filterGraph); // Ken Burns
        Assert.Contains("drawbox", filterGraph); // Brand color
        Assert.Contains("movie='/tmp/logo.png'", filterGraph); // Watermark
        Assert.Contains("overlay=", filterGraph); // Watermark position
        Assert.Contains("subtitles", filterGraph); // Subtitles

        // Verify narration and music paths
        Assert.Equal("/tmp/narration.wav", timeline.NarrationPath);
        Assert.Equal("/tmp/music.mp3", timeline.MusicPath);
    }
}
