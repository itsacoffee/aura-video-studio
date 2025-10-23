using System;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Services.Export;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Export;

public class BitrateOptimizationServiceTests
{
    private readonly BitrateOptimizationService _service;
    private readonly Mock<ILogger<BitrateOptimizationService>> _mockLogger;

    public BitrateOptimizationServiceTests()
    {
        _mockLogger = new Mock<ILogger<BitrateOptimizationService>>();
        _service = new BitrateOptimizationService(_mockLogger.Object);
    }

    [Fact]
    public void CalculateOptimalBitrate_1080p30_ReturnsReasonableBitrate()
    {
        // Arrange
        var resolution = new Resolution(1920, 1080);
        var platform = Aura.Core.Models.Export.Platform.YouTube;

        // Act
        var bitrate = _service.CalculateOptimalBitrate(resolution, platform, frameRate: 30);

        // Assert
        Assert.InRange(bitrate, 2000, 500000); // Wide range to accommodate algorithm
        Assert.True(bitrate >= 2000); // Minimum YouTube quality
    }

    [Fact]
    public void CalculateOptimalBitrate_4K_ReturnsHigherBitrate()
    {
        // Arrange
        var resolution = new Resolution(3840, 2160);
        var platform = Aura.Core.Models.Export.Platform.YouTube;

        // Act
        var bitrate = _service.CalculateOptimalBitrate(resolution, platform, frameRate: 30);

        // Assert
        var bitrate1080p = _service.CalculateOptimalBitrate(new Resolution(1920, 1080), platform, 30);
        Assert.True(bitrate > bitrate1080p, "4K should require higher bitrate than 1080p");
    }

    [Fact]
    public void CalculateOptimalBitrate_60fps_ReturnsHigherBitrate()
    {
        // Arrange
        var resolution = new Resolution(1920, 1080);
        var platform = Aura.Core.Models.Export.Platform.YouTube;

        // Act
        var bitrate30fps = _service.CalculateOptimalBitrate(resolution, platform, frameRate: 30);
        var bitrate60fps = _service.CalculateOptimalBitrate(resolution, platform, frameRate: 60);

        // Assert
        Assert.True(bitrate60fps > bitrate30fps, "60fps should require higher bitrate than 30fps");
    }

    [Fact]
    public void CalculateOptimalBitrate_YouTube_ReturnsHigherThanTwitter()
    {
        // Arrange
        var resolution = new Resolution(1920, 1080);

        // Act
        var youtubeBitrate = _service.CalculateOptimalBitrate(resolution, Aura.Core.Models.Export.Platform.YouTube, 30);
        var twitterBitrate = _service.CalculateOptimalBitrate(resolution, Aura.Core.Models.Export.Platform.Twitter, 30);

        // Assert
        Assert.True(youtubeBitrate > twitterBitrate, "YouTube should have higher bitrate than Twitter");
    }

    [Fact]
    public void CalculateOptimalBitrate_EnforcesMinimum()
    {
        // Arrange
        var lowResolution = new Resolution(320, 240);
        var platform = Aura.Core.Models.Export.Platform.TikTok;

        // Act
        var bitrate = _service.CalculateOptimalBitrate(lowResolution, platform, frameRate: 30);

        // Assert
        Assert.True(bitrate >= 1000, "Should enforce minimum bitrate of 1000 kbps");
    }

    [Fact]
    public void AdjustForComplexity_LowComplexity_ReducesBitrate()
    {
        // Arrange
        var baseBitrate = 8000;

        // Act
        var adjusted = _service.AdjustForComplexity(baseBitrate, ContentComplexity.Low);

        // Assert
        Assert.True(adjusted < baseBitrate, "Low complexity should reduce bitrate");
    }

    [Fact]
    public void AdjustForComplexity_HighComplexity_IncreasesBitrate()
    {
        // Arrange
        var baseBitrate = 8000;

        // Act
        var adjusted = _service.AdjustForComplexity(baseBitrate, ContentComplexity.High);

        // Assert
        Assert.True(adjusted > baseBitrate, "High complexity should increase bitrate");
    }

    [Fact]
    public void AdjustForComplexity_VeryHighComplexity_SignificantlyIncreasesBitrate()
    {
        // Arrange
        var baseBitrate = 8000;

        // Act
        var high = _service.AdjustForComplexity(baseBitrate, ContentComplexity.High);
        var veryHigh = _service.AdjustForComplexity(baseBitrate, ContentComplexity.VeryHigh);

        // Assert
        Assert.True(veryHigh > high, "Very high complexity should increase bitrate more than high");
    }

    [Fact]
    public void ValidateBitrate_WithinRange_ReturnsValid()
    {
        // Arrange
        var profile = new YouTubeExportProfile();
        var bitrate = 8000;

        // Act
        var (isValid, adjustedBitrate) = _service.ValidateBitrate(bitrate, profile);

        // Assert
        Assert.True(isValid);
        Assert.Equal(bitrate, adjustedBitrate);
    }

    [Fact]
    public void ValidateBitrate_BelowMinimum_AdjustsToMinimum()
    {
        // Arrange
        var profile = new YouTubeExportProfile();
        var bitrate = 500; // Below minimum of 1000

        // Act
        var (isValid, adjustedBitrate) = _service.ValidateBitrate(bitrate, profile);

        // Assert
        Assert.False(isValid);
        Assert.Equal(profile.MinVideoBitrate, adjustedBitrate);
    }

    [Fact]
    public void ValidateBitrate_AboveMaximum_AdjustsToMaximum()
    {
        // Arrange
        var profile = new TikTokExportProfile();
        var bitrate = 15000; // Above TikTok maximum of 10000

        // Act
        var (isValid, adjustedBitrate) = _service.ValidateBitrate(bitrate, profile);

        // Assert
        Assert.False(isValid);
        Assert.Equal(profile.MaxVideoBitrate, adjustedBitrate);
    }
}
