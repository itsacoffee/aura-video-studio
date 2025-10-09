using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Audio;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class CaptionsIntegrationTests
{
    private readonly AudioProcessor _audioProcessor;

    public CaptionsIntegrationTests()
    {
        _audioProcessor = new AudioProcessor(NullLogger<AudioProcessor>.Instance);
    }

    [Fact]
    public void Timeline_Should_SupportSubtitlesPath()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new Scene(1, "Scene 2", "This is a test", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };

        var sceneAssets = new Dictionary<int, IReadOnlyList<Asset>>();
        var narrationPath = "/path/to/narration.wav";
        var musicPath = "/path/to/music.mp3";
        var subtitlesPath = "/path/to/subtitles.srt";

        // Act
        var timeline = new Timeline(scenes, sceneAssets, narrationPath, musicPath, subtitlesPath);

        // Assert
        Assert.Equal(subtitlesPath, timeline.SubtitlesPath);
    }

    [Fact]
    public void GenerateCaptions_Should_MatchScriptLineTimings()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "This is a test", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)),
            new ScriptLine(2, "Final line", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2))
        };

        // Act
        string srtCaptions = _audioProcessor.GenerateSrtSubtitles(lines);
        string vttCaptions = _audioProcessor.GenerateVttSubtitles(lines);

        // Assert - SRT
        Assert.Contains("1", srtCaptions);
        Assert.Contains("00:00:00,000 --> 00:00:02,000", srtCaptions);
        Assert.Contains("Hello world", srtCaptions);
        
        Assert.Contains("2", srtCaptions);
        Assert.Contains("00:00:02,000 --> 00:00:05,000", srtCaptions);
        Assert.Contains("This is a test", srtCaptions);
        
        Assert.Contains("3", srtCaptions);
        Assert.Contains("00:00:05,000 --> 00:00:07,000", srtCaptions);
        Assert.Contains("Final line", srtCaptions);

        // Assert - VTT
        Assert.StartsWith("WEBVTT", vttCaptions);
        Assert.Contains("00:00:00.000 --> 00:00:02.000", vttCaptions);
        Assert.Contains("Hello world", vttCaptions);
        Assert.Contains("00:00:02.000 --> 00:00:05.000", vttCaptions);
        Assert.Contains("This is a test", vttCaptions);
    }

    [Fact]
    public void GenerateCaptions_Should_HandleOverlappingTimings()
    {
        // Arrange - Lines with overlapping timings
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3)),
            new ScriptLine(1, "Line 2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };

        // Act
        string srtCaptions = _audioProcessor.GenerateSrtSubtitles(lines);

        // Assert - Should handle both lines even with overlap
        Assert.Contains("Line 1", srtCaptions);
        Assert.Contains("Line 2", srtCaptions);
        Assert.Contains("00:00:00,000 --> 00:00:03,000", srtCaptions);
        Assert.Contains("00:00:02,000 --> 00:00:05,000", srtCaptions);
    }

    [Fact]
    public void GenerateCaptions_Should_PreserveSpecialCharacters()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line with \"quotes\" and 'apostrophes'", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "Line with émojis 🎬 and ümlaüts", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2))
        };

        // Act
        string srtCaptions = _audioProcessor.GenerateSrtSubtitles(lines);
        string vttCaptions = _audioProcessor.GenerateVttSubtitles(lines);

        // Assert
        Assert.Contains("\"quotes\"", srtCaptions);
        Assert.Contains("'apostrophes'", srtCaptions);
        Assert.Contains("🎬", srtCaptions);
        Assert.Contains("ümlaüts", srtCaptions);
        
        Assert.Contains("\"quotes\"", vttCaptions);
        Assert.Contains("🎬", vttCaptions);
    }

    [Fact]
    public void BuildSubtitleFilter_Should_CreateValidFFmpegFilter()
    {
        // Arrange
        var subtitlePath = "/path/to/subtitles.srt";

        // Act
        var filter = _audioProcessor.BuildSubtitleFilter(
            subtitlePath,
            fontName: "Arial",
            fontSize: 24,
            primaryColor: "FFFFFF",
            outlineColor: "000000",
            outlineWidth: 2
        );

        // Assert
        Assert.Contains("subtitles=", filter);
        Assert.Contains("FontName=Arial", filter);
        Assert.Contains("FontSize=24", filter);
        Assert.Contains("PrimaryColour=&HFFFFFF&", filter);
        Assert.Contains("OutlineColour=&H000000&", filter);
    }

    [Fact]
    public void CaptionsGeneration_Should_SupportBothBurnInAndSidecar()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test line", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
        };

        // Act - Generate sidecar captions
        string srtCaptions = _audioProcessor.GenerateSrtSubtitles(lines);
        
        // Act - Generate burn-in filter
        var burnInFilter = _audioProcessor.BuildSubtitleFilter("/tmp/test.srt");

        // Assert - Both should be valid
        Assert.NotNull(srtCaptions);
        Assert.NotEmpty(srtCaptions);
        Assert.NotNull(burnInFilter);
        Assert.NotEmpty(burnInFilter);
        Assert.Contains("subtitles=", burnInFilter);
    }

    [Fact]
    public void CaptionsGeneration_Should_HandleLongVideoDurations()
    {
        // Arrange - Test with hours
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line at 1h30m", TimeSpan.FromHours(1.5), TimeSpan.FromSeconds(3)),
            new ScriptLine(1, "Line at 2h", TimeSpan.FromHours(2), TimeSpan.FromSeconds(5))
        };

        // Act
        string srtCaptions = _audioProcessor.GenerateSrtSubtitles(lines);

        // Assert
        Assert.Contains("01:30:00,000", srtCaptions);
        Assert.Contains("01:30:03,000", srtCaptions);
        Assert.Contains("02:00:00,000", srtCaptions);
        Assert.Contains("02:00:05,000", srtCaptions);
    }

    [Fact]
    public void CaptionsGeneration_Should_HandleMilliseconds()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1500)),
            new ScriptLine(1, "Line 2", TimeSpan.FromMilliseconds(2250), TimeSpan.FromMilliseconds(750))
        };

        // Act
        string srtCaptions = _audioProcessor.GenerateSrtSubtitles(lines);
        string vttCaptions = _audioProcessor.GenerateVttSubtitles(lines);

        // Assert - SRT format uses comma for milliseconds
        Assert.Contains("00:00:00,500", srtCaptions);
        Assert.Contains("00:00:02,000", srtCaptions);
        Assert.Contains("00:00:02,250", srtCaptions);
        Assert.Contains("00:00:03,000", srtCaptions);

        // Assert - VTT format uses dot for milliseconds
        Assert.Contains("00:00:00.500", vttCaptions);
        Assert.Contains("00:00:02.000", vttCaptions);
        Assert.Contains("00:00:02.250", vttCaptions);
        Assert.Contains("00:00:03.000", vttCaptions);
    }
}
