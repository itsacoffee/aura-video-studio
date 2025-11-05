using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Captions;
using Aura.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class SubtitleServiceTests : IDisposable
{
    private readonly SubtitleService _subtitleService;
    private readonly CaptionBuilder _captionBuilder;
    private readonly string _tempDirectory;

    public SubtitleServiceTests()
    {
        _captionBuilder = new CaptionBuilder(NullLogger<CaptionBuilder>.Instance);
        _subtitleService = new SubtitleService(
            NullLogger<SubtitleService>.Instance,
            _captionBuilder);
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"SubtitleServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Fact]
    public async Task GenerateSubtitlesAsync_Should_GenerateSrtContent()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "This is a test", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };

        var request = new SubtitleGenerationRequest
        {
            ScriptLines = lines,
            TargetLanguage = "en",
            Format = SubtitleExportFormat.SRT,
            IsRightToLeft = false,
            ExportToFile = false
        };

        // Act
        var result = await _subtitleService.GenerateSubtitlesAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(SubtitleExportFormat.SRT, result.Format);
        Assert.Equal(2, result.LineCount);
        Assert.Contains("Hello world", result.Content);
        Assert.Contains("This is a test", result.Content);
        Assert.Contains("00:00:00,000 --> 00:00:02,000", result.Content);
    }

    [Fact]
    public async Task GenerateSubtitlesAsync_Should_ExportToFile_WhenRequested()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test line", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
        };

        var request = new SubtitleGenerationRequest
        {
            ScriptLines = lines,
            TargetLanguage = "en",
            Format = SubtitleExportFormat.SRT,
            IsRightToLeft = false,
            ExportToFile = true,
            OutputDirectory = _tempDirectory,
            BaseFileName = "test_subtitles"
        };

        // Act
        var result = await _subtitleService.GenerateSubtitlesAsync(request);

        // Assert
        Assert.NotNull(result.ExportedFilePath);
        Assert.True(File.Exists(result.ExportedFilePath));
        var content = await File.ReadAllTextAsync(result.ExportedFilePath);
        Assert.Contains("Test line", content);
    }

    [Fact]
    public async Task GenerateSubtitlesAsync_Should_HandleRTL_Language()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "مرحبا بالعالم", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2))
        };

        var request = new SubtitleGenerationRequest
        {
            ScriptLines = lines,
            TargetLanguage = "ar",
            Format = SubtitleExportFormat.SRT,
            IsRightToLeft = true,
            ExportToFile = false
        };

        // Act
        var result = await _subtitleService.GenerateSubtitlesAsync(request);

        // Assert
        Assert.True(result.IsRightToLeft);
        Assert.Equal("ar", result.TargetLanguage);
        Assert.Contains("مرحبا بالعالم", result.Content);
    }

    [Fact]
    public async Task GenerateSubtitlesAsync_Should_GenerateVttFormat()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "VTT test", TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2))
        };

        var request = new SubtitleGenerationRequest
        {
            ScriptLines = lines,
            TargetLanguage = "en",
            Format = SubtitleExportFormat.VTT,
            IsRightToLeft = false,
            ExportToFile = false
        };

        // Act
        var result = await _subtitleService.GenerateSubtitlesAsync(request);

        // Assert
        Assert.Equal(SubtitleExportFormat.VTT, result.Format);
        Assert.StartsWith("WEBVTT", result.Content);
        Assert.Contains("00:00:01.000 --> 00:00:03.000", result.Content);
    }

    [Fact]
    public void GenerateBurnInFilter_Should_CreateFilterForLTR()
    {
        // Arrange
        var options = new BurnInOptions
        {
            FontName = "Arial",
            FontSize = 24,
            IsRightToLeft = false
        };

        // Act
        var filter = _subtitleService.GenerateBurnInFilter("test.srt", options);

        // Assert
        Assert.Contains("subtitles=", filter);
        Assert.Contains("test.srt", filter);
        Assert.Contains("FontName=Arial", filter);
        Assert.Contains("FontSize=24", filter);
    }

    [Fact]
    public void GenerateBurnInFilter_Should_UseRTLFont_ForRTLLanguage()
    {
        // Arrange
        var options = new BurnInOptions
        {
            FontName = "Arial",
            FontSize = 24,
            IsRightToLeft = true,
            RtlFontFallback = "Arial Unicode MS"
        };

        // Act
        var filter = _subtitleService.GenerateBurnInFilter("arabic.srt", options);

        // Assert
        Assert.Contains("subtitles=", filter);
        Assert.Contains("FontName=Arial Unicode MS", filter);
    }

    [Fact]
    public void GetRecommendedStyle_Should_ReturnRTLStyle_ForArabic()
    {
        // Act
        var style = _subtitleService.GetRecommendedStyle("ar");

        // Assert
        Assert.True(style.IsRightToLeft);
        Assert.NotNull(style.RtlFontFallback);
        Assert.Equal("Arial Unicode MS", style.RtlFontFallback);
    }

    [Fact]
    public void GetRecommendedStyle_Should_ReturnLTRStyle_ForEnglish()
    {
        // Act
        var style = _subtitleService.GetRecommendedStyle("en");

        // Assert
        Assert.False(style.IsRightToLeft);
        Assert.Null(style.RtlFontFallback);
    }

    [Fact]
    public void ValidateTimingAlignment_Should_Pass_WithinTolerance()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "Line 2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };
        var targetDuration = 5.0; // Total is 5 seconds
        var tolerance = 0.02; // 2%

        // Act
        var result = _subtitleService.ValidateTimingAlignment(lines, targetDuration, tolerance);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(5.0, result.ActualDuration);
        Assert.Equal(0.0, result.DeviationPercent);
    }

    [Fact]
    public void ValidateTimingAlignment_Should_Fail_OutsideTolerance()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "Line 2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4))
        };
        var targetDuration = 5.0;
        var tolerance = 0.02; // 2%

        // Act
        var result = _subtitleService.ValidateTimingAlignment(lines, targetDuration, tolerance);

        // Assert
        Assert.False(result.IsValid); // Actual is 6s, target is 5s, deviation is 20%
        Assert.Equal(6.0, result.ActualDuration);
        Assert.True(result.DeviationPercent > 2.0);
    }

    [Fact]
    public void ValidateTimingAlignment_Should_HandleSmallDeviation()
    {
        // Arrange - 1% deviation
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(5.05))
        };
        var targetDuration = 5.0;
        var tolerance = 0.02; // 2%

        // Act
        var result = _subtitleService.ValidateTimingAlignment(lines, targetDuration, tolerance);

        // Assert
        Assert.True(result.IsValid); // 1% is within 2% tolerance
        Assert.Equal(5.05, result.ActualDuration);
        Assert.True(result.DeviationPercent <= 2.0);
    }

    [Fact]
    public void GetRecommendedStyle_Should_HandleUnknownLanguage()
    {
        // Act
        var style = _subtitleService.GetRecommendedStyle("unknown");

        // Assert
        Assert.NotNull(style);
        Assert.False(style.IsRightToLeft);
    }
}
