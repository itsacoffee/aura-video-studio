using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Audio;
using Aura.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class AudioProcessorTests
{
    private readonly AudioProcessor _processor;

    public AudioProcessorTests()
    {
        _processor = new AudioProcessor(NullLogger<AudioProcessor>.Instance);
    }

    [Fact]
    public void BuildAudioFilterChain_Should_IncludeAllStages()
    {
        // Act
        string filterChain = _processor.BuildAudioFilterChain(
            targetLufs: -14.0,
            enableDeEsser: true,
            enableCompressor: true,
            enableLimiter: true,
            peakCeiling: -1.0
        );

        // Assert - DSP chain: HPF -> De-esser -> Compressor -> Limiter -> LUFS normalization
        Assert.Contains("highpass", filterChain);        // High-pass filter
        Assert.Contains("treble", filterChain);          // De-esser
        Assert.Contains("acompressor", filterChain);     // Compressor
        Assert.Contains("alimiter", filterChain);        // Limiter
        Assert.Contains("loudnorm", filterChain);        // LUFS normalization
    }

    [Theory]
    [InlineData(-14.0)]  // YouTube standard
    [InlineData(-16.0)]  // Voice-only
    [InlineData(-12.0)]  // Music-forward
    public void BuildAudioFilterChain_Should_SetTargetLufs(double targetLufs)
    {
        // Act
        string filterChain = _processor.BuildAudioFilterChain(targetLufs: targetLufs);

        // Assert
        Assert.Contains($"loudnorm=I={targetLufs}", filterChain);
    }

    [Fact]
    public void BuildAudioFilterChain_Should_OmitOptionalStages()
    {
        // Act
        string filterChain = _processor.BuildAudioFilterChain(
            enableDeEsser: false,
            enableCompressor: false,
            enableLimiter: false
        );

        // Assert
        Assert.DoesNotContain("treble", filterChain);
        Assert.DoesNotContain("acompressor", filterChain);
        Assert.DoesNotContain("alimiter", filterChain);
        Assert.Contains("highpass", filterChain);    // Should still have HPF
        Assert.Contains("loudnorm", filterChain);    // Should still have normalization
    }

    [Fact]
    public void BuildMusicDuckingFilter_Should_CreateSidechainCompress()
    {
        // Act
        string filter = _processor.BuildMusicDuckingFilter(
            narrationInput: "0:a",
            musicInput: "1:a",
            duckDepthDb: -12.0
        );

        // Assert
        Assert.Contains("sidechaincompress", filter);
        Assert.Contains("[1:a]", filter);  // Music input
        Assert.Contains("[0:a]", filter);  // Narration input (trigger)
        Assert.Contains("[duckedmusic]", filter);  // Output label
    }

    [Theory]
    [InlineData("voice", 1, 96)]
    [InlineData("voice", 2, 128)]
    [InlineData("music", 1, 192)]
    [InlineData("music", 2, 256)]
    [InlineData("mixed", 2, 256)]
    public void SuggestAudioBitrate_Should_ReturnCorrectValue(string contentType, int channels, int expectedBitrate)
    {
        // Act
        int bitrate = _processor.SuggestAudioBitrate(contentType, channels);

        // Assert
        Assert.Equal(expectedBitrate, bitrate);
    }

    [Theory]
    [InlineData(-14.0, -1.5, true)]   // Good: YouTube standard
    [InlineData(-16.0, -2.0, true)]   // Good: Voice-only
    [InlineData(-12.0, -1.0, true)]   // Good: Music-forward
    [InlineData(-20.0, -3.0, false)]  // Bad: Too quiet
    [InlineData(-8.0, -0.5, false)]   // Bad: Too loud
    [InlineData(-14.0, 0.5, false)]   // Bad: Peak exceeds ceiling
    public void ValidateAudioSettings_Should_CheckBounds(double lufs, double peakDb, bool expectedValid)
    {
        // Act
        bool isValid = _processor.ValidateAudioSettings(lufs, peakDb, out string? message);

        // Assert
        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
        {
            Assert.NotNull(message);
            Assert.NotEmpty(message);
        }
    }

    [Fact]
    public void BuildSubtitleFilter_Should_CreateValidFilter()
    {
        // Act
        string filter = _processor.BuildSubtitleFilter(
            "subtitles.srt",
            fontName: "Arial",
            fontSize: 24,
            primaryColor: "FFFFFF",
            outlineColor: "000000",
            outlineWidth: 2
        );

        // Assert
        Assert.Contains("subtitles=", filter);
        Assert.Contains("subtitles.srt", filter);
        Assert.Contains("FontName=Arial", filter);
        Assert.Contains("FontSize=24", filter);
        Assert.Contains("PrimaryColour=&HFFFFFF&", filter);
        Assert.Contains("OutlineColour=&H000000&", filter);
    }

    [Fact]
    public void GenerateSrtSubtitles_Should_CreateValidSrt()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "This is a test", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };

        // Act
        string srt = _processor.GenerateSrtSubtitles(lines);

        // Assert
        Assert.Contains("1", srt);  // First subtitle index
        Assert.Contains("00:00:00,000 --> 00:00:02,000", srt);  // First timing
        Assert.Contains("Hello world", srt);
        Assert.Contains("2", srt);  // Second subtitle index
        Assert.Contains("00:00:02,000 --> 00:00:05,000", srt);  // Second timing
        Assert.Contains("This is a test", srt);
    }

    [Fact]
    public void GenerateVttSubtitles_Should_CreateValidVtt()
    {
        // Arrange
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "This is a test", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };

        // Act
        string vtt = _processor.GenerateVttSubtitles(lines);

        // Assert
        Assert.StartsWith("WEBVTT", vtt);  // VTT header
        Assert.Contains("00:00:00.000 --> 00:00:02.000", vtt);  // First timing (VTT format with dots)
        Assert.Contains("Hello world", vtt);
        Assert.Contains("00:00:02.000 --> 00:00:05.000", vtt);  // Second timing
        Assert.Contains("This is a test", vtt);
    }

    [Fact]
    public void GenerateSrtSubtitles_Should_HandleLongDuration()
    {
        // Arrange - Test with hours
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Long video", TimeSpan.FromHours(1.5), TimeSpan.FromSeconds(3))
        };

        // Act
        string srt = _processor.GenerateSrtSubtitles(lines);

        // Assert
        Assert.Contains("01:30:00,000", srt);  // 1 hour 30 minutes
        Assert.Contains("01:30:03,000", srt);  // Plus 3 seconds
    }
}
