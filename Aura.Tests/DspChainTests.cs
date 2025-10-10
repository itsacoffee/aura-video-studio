using System;
using Aura.Core.Audio;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class DspChainTests
{
    private readonly DspChain _dspChain;

    public DspChainTests()
    {
        _dspChain = new DspChain(NullLogger<DspChain>.Instance);
    }

    [Fact]
    public void BuildDspFilterChain_Should_IncludeAllStages()
    {
        // Act
        string filterChain = _dspChain.BuildDspFilterChain(
            targetLufs: -14.0,
            peakCeiling: -1.0,
            enableHpf: true,
            enableDeEsser: true,
            enableCompressor: true,
            enableLimiter: true
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
    public void BuildDspFilterChain_Should_SetTargetLufs(double targetLufs)
    {
        // Act
        string filterChain = _dspChain.BuildDspFilterChain(targetLufs: targetLufs);

        // Assert
        Assert.Contains($"loudnorm=I={targetLufs}", filterChain);
    }

    [Fact]
    public void BuildDspFilterChain_Should_SetPeakCeiling()
    {
        // Act
        string filterChain = _dspChain.BuildDspFilterChain(
            targetLufs: -14.0,
            peakCeiling: -1.0
        );

        // Assert
        Assert.Contains("alimiter=limit=-1dB", filterChain);
        Assert.Contains("loudnorm=I=-14:TP=-1", filterChain);
    }

    [Fact]
    public void BuildDspFilterChain_Should_OmitOptionalStages()
    {
        // Act
        string filterChain = _dspChain.BuildDspFilterChain(
            enableHpf: false,
            enableDeEsser: false,
            enableCompressor: false,
            enableLimiter: false
        );

        // Assert
        Assert.DoesNotContain("highpass", filterChain);
        Assert.DoesNotContain("treble", filterChain);
        Assert.DoesNotContain("acompressor", filterChain);
        Assert.DoesNotContain("alimiter", filterChain);
        Assert.Contains("loudnorm", filterChain);    // Should still have normalization
    }

    [Theory]
    [InlineData(-14.0, -1.5, -14.0, -1.0, 1.0, true)]   // Within tolerance
    [InlineData(-14.5, -1.2, -14.0, -1.0, 1.0, true)]   // Within tolerance (0.5dB off)
    [InlineData(-15.0, -1.5, -14.0, -1.0, 1.0, true)]   // Within tolerance (1.0dB off)
    [InlineData(-16.0, -1.5, -14.0, -1.0, 1.0, false)]  // Outside tolerance (2.0dB off)
    [InlineData(-14.0, 0.5, -14.0, -1.0, 1.0, false)]   // Peak exceeds ceiling
    public void ValidateLoudness_Should_CheckBounds(
        double measuredLufs,
        double measuredPeak,
        double targetLufs,
        double peakCeiling,
        double tolerance,
        bool expectedValid)
    {
        // Act
        bool isValid = _dspChain.ValidateLoudness(
            measuredLufs, 
            measuredPeak, 
            targetLufs, 
            peakCeiling, 
            out string? message,
            tolerance);

        // Assert
        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
        {
            Assert.NotNull(message);
            Assert.NotEmpty(message);
        }
    }

    [Theory]
    [InlineData("youtube", -14.0)]
    [InlineData("web", -14.0)]
    [InlineData("default", -14.0)]
    [InlineData("voice", -16.0)]
    [InlineData("podcast", -16.0)]
    [InlineData("narration", -16.0)]
    [InlineData("music", -12.0)]
    [InlineData("music-forward", -12.0)]
    public void GetRecommendedLufs_Should_ReturnCorrectTarget(string contentType, double expectedLufs)
    {
        // Act
        double lufs = _dspChain.GetRecommendedLufs(contentType);

        // Assert
        Assert.Equal(expectedLufs, lufs);
    }

    [Fact]
    public void BuildDspFilterChain_Should_UseCommaSeparator()
    {
        // Act
        string filterChain = _dspChain.BuildDspFilterChain(
            targetLufs: -14.0,
            peakCeiling: -1.0,
            enableHpf: true,
            enableDeEsser: true,
            enableCompressor: true,
            enableLimiter: true
        );

        // Assert - Filters should be comma-separated
        Assert.Contains(",", filterChain);
        
        // Split and count stages (should have 5: HPF, De-esser, Compressor, Limiter, LUFS)
        var stages = filterChain.Split(',');
        Assert.Equal(5, stages.Length);
    }

    [Fact]
    public void ValidateLoudness_Should_AllowExactMatch()
    {
        // Act
        bool isValid = _dspChain.ValidateLoudness(
            measuredLufs: -14.0,
            measuredPeak: -1.5,
            targetLufs: -14.0,
            peakCeiling: -1.0,
            out string? message,
            tolerance: 1.0);

        // Assert
        Assert.True(isValid);
        Assert.Null(message);
    }

    [Fact]
    public void ValidateLoudness_Should_RejectWhenBothConditionsFail()
    {
        // Act
        bool isValid = _dspChain.ValidateLoudness(
            measuredLufs: -10.0,  // Too loud (more than 1dB off)
            measuredPeak: 0.5,    // Peak exceeds ceiling
            targetLufs: -14.0,
            peakCeiling: -1.0,
            out string? message,
            tolerance: 1.0);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(message);
        // Should report the first failure (LUFS)
        Assert.Contains("LUFS", message);
    }
}
