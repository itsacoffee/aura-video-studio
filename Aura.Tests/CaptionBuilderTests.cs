using System;
using System.Collections.Generic;
using Aura.Core.Captions;
using Aura.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class CaptionBuilderTests
{
    private readonly CaptionBuilder _captionBuilder;

    public CaptionBuilderTests()
    {
        _captionBuilder = new CaptionBuilder(NullLogger<CaptionBuilder>.Instance);
    }

    [Fact]
    public void GenerateSrt_Should_CreateValidSrtFormat()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "This is a test", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };

        // Act
        string srt = _captionBuilder.GenerateSrt(lines);

        // Assert
        Assert.Contains("1", srt);  // First subtitle index
        Assert.Contains("00:00:00,000 --> 00:00:02,000", srt);  // First timing (SRT uses comma)
        Assert.Contains("Hello world", srt);
        Assert.Contains("2", srt);  // Second subtitle index
        Assert.Contains("00:00:02,000 --> 00:00:05,000", srt);  // Second timing
        Assert.Contains("This is a test", srt);
    }

    [Fact]
    public void GenerateVtt_Should_CreateValidVttFormat()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "This is a test", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };

        // Act
        string vtt = _captionBuilder.GenerateVtt(lines);

        // Assert
        Assert.StartsWith("WEBVTT", vtt);  // VTT header
        Assert.Contains("00:00:00.000 --> 00:00:02.000", vtt);  // First timing (VTT uses dot)
        Assert.Contains("Hello world", vtt);
        Assert.Contains("00:00:02.000 --> 00:00:05.000", vtt);  // Second timing
        Assert.Contains("This is a test", vtt);
    }

    [Fact]
    public void GenerateSrt_Should_HandleLongDurations()
    {
        // Arrange - Test with hours
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Long video", TimeSpan.FromHours(1.5), TimeSpan.FromSeconds(3))
        };

        // Act
        string srt = _captionBuilder.GenerateSrt(lines);

        // Assert
        Assert.Contains("01:30:00,000", srt);  // 1 hour 30 minutes
        Assert.Contains("01:30:03,000", srt);  // Plus 3 seconds
    }

    [Fact]
    public void GenerateVtt_Should_HandleLongDurations()
    {
        // Arrange - Test with hours
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Long video", TimeSpan.FromHours(2), TimeSpan.FromSeconds(5))
        };

        // Act
        string vtt = _captionBuilder.GenerateVtt(lines);

        // Assert
        Assert.Contains("02:00:00.000", vtt);  // 2 hours
        Assert.Contains("02:00:05.000", vtt);  // Plus 5 seconds
    }

    [Fact]
    public void GenerateSrt_Should_HandleMilliseconds()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1500)),
            new ScriptLine(1, "Line 2", TimeSpan.FromMilliseconds(2250), TimeSpan.FromMilliseconds(750))
        };

        // Act
        string srt = _captionBuilder.GenerateSrt(lines);

        // Assert
        Assert.Contains("00:00:00,500", srt);
        Assert.Contains("00:00:02,000", srt);
        Assert.Contains("00:00:02,250", srt);
        Assert.Contains("00:00:03,000", srt);
    }

    [Fact]
    public void GenerateVtt_Should_HandleMilliseconds()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(1500))
        };

        // Act
        string vtt = _captionBuilder.GenerateVtt(lines);

        // Assert
        Assert.Contains("00:00:00.500", vtt);
        Assert.Contains("00:00:02.000", vtt);
    }

    [Fact]
    public void BuildBurnInFilter_Should_CreateValidFFmpegFilter()
    {
        // Arrange
        var style = new CaptionRenderStyle(
            FontName: "Arial",
            FontSize: 24,
            PrimaryColor: "FFFFFF",
            OutlineColor: "000000",
            OutlineWidth: 2,
            BorderStyle: 3,
            Alignment: 2
        );

        // Act
        string filter = _captionBuilder.BuildBurnInFilter("subtitles.srt", style);

        // Assert
        Assert.Contains("subtitles=", filter);
        Assert.Contains("subtitles.srt", filter);
        Assert.Contains("FontName=Arial", filter);
        Assert.Contains("FontSize=24", filter);
        Assert.Contains("PrimaryColour=&HFFFFFF&", filter);
        Assert.Contains("OutlineColour=&H000000&", filter);
        Assert.Contains("Outline=2", filter);
        Assert.Contains("BorderStyle=3", filter);
        Assert.Contains("Alignment=2", filter);
    }

    [Fact]
    public void BuildBurnInFilter_Should_EscapePathCharacters()
    {
        // Arrange
        var style = new CaptionRenderStyle();
        var pathWithSpecialChars = "C:\\Users\\test\\subtitles.srt";

        // Act
        string filter = _captionBuilder.BuildBurnInFilter(pathWithSpecialChars, style);

        // Assert
        // FFmpeg requires escaping backslashes and colons
        Assert.Contains("C\\:\\\\Users\\\\test\\\\subtitles.srt", filter);
    }

    [Fact]
    public void ValidateTimecodes_Should_PassForValidTimecodes()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "Line 2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3)),
            new ScriptLine(2, "Line 3", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(2))
        };

        // Act
        bool isValid = _captionBuilder.ValidateTimecodes(lines, out string? message);

        // Assert
        Assert.True(isValid);
        Assert.Null(message);
    }

    [Fact]
    public void ValidateTimecodes_Should_DetectOverlaps()
    {
        // Arrange - Lines with overlapping timings
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(3)),
            new ScriptLine(1, "Line 2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };

        // Act
        bool isValid = _captionBuilder.ValidateTimecodes(lines, out string? message);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(message);
        Assert.Contains("overlap", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTimecodes_Should_DetectNegativeDurations()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(-1))
        };

        // Act
        bool isValid = _captionBuilder.ValidateTimecodes(lines, out string? message);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(message);
        Assert.Contains("non-positive duration", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTimecodes_Should_DetectZeroDurations()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromSeconds(0), TimeSpan.Zero),
            new ScriptLine(1, "Line 2", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2))
        };

        // Act
        bool isValid = _captionBuilder.ValidateTimecodes(lines, out string? message);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(message);
        Assert.Contains("non-positive duration", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateSrt_Should_PreserveSpecialCharacters()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line with \"quotes\" and 'apostrophes'", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "Line with émojis 🎬 and ümlaüts", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2))
        };

        // Act
        string srt = _captionBuilder.GenerateSrt(lines);

        // Assert
        Assert.Contains("\"quotes\"", srt);
        Assert.Contains("'apostrophes'", srt);
        Assert.Contains("🎬", srt);
        Assert.Contains("ümlaüts", srt);
    }

    [Fact]
    public void GenerateVtt_Should_PreserveSpecialCharacters()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line with émojis 🎬", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
        };

        // Act
        string vtt = _captionBuilder.GenerateVtt(lines);

        // Assert
        Assert.Contains("🎬", vtt);
    }

    [Fact]
    public void CaptionRenderStyle_Should_UseDefaultValues()
    {
        // Act
        var style = new CaptionRenderStyle();

        // Assert
        Assert.Equal("Arial", style.FontName);
        Assert.Equal(24, style.FontSize);
        Assert.Equal("FFFFFF", style.PrimaryColor);
        Assert.Equal("000000", style.OutlineColor);
        Assert.Equal(2, style.OutlineWidth);
        Assert.Equal(3, style.BorderStyle);
        Assert.Equal(2, style.Alignment);
    }
}
