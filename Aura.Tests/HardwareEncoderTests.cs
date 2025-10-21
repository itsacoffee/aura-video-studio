using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Services.Render;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class HardwareEncoderTests
{
    private readonly Mock<ILogger<HardwareEncoder>> _mockLogger;

    public HardwareEncoderTests()
    {
        _mockLogger = new Mock<ILogger<HardwareEncoder>>();
    }

    [Fact]
    public async Task DetectHardwareCapabilitiesAsync_ReturnsCapabilities()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");

        // Act
        var capabilities = await encoder.DetectHardwareCapabilitiesAsync();

        // Assert
        Assert.NotNull(capabilities);
        Assert.NotNull(capabilities.AvailableEncoders);
    }

    [Fact]
    public async Task DetectHardwareCapabilitiesAsync_CachesResult()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");

        // Act
        var capabilities1 = await encoder.DetectHardwareCapabilitiesAsync();
        var capabilities2 = await encoder.DetectHardwareCapabilitiesAsync();

        // Assert
        // Should return equivalent cached result (values may be different instances but same data)
        Assert.Equal(capabilities1.HasNVENC, capabilities2.HasNVENC);
        Assert.Equal(capabilities1.HasAMF, capabilities2.HasAMF);
        Assert.Equal(capabilities1.HasQSV, capabilities2.HasQSV);
        Assert.Equal(capabilities1.HasVideoToolbox, capabilities2.HasVideoToolbox);
    }

    [Fact]
    public async Task SelectBestEncoderAsync_WithNoHardware_ReturnsSoftwareEncoder()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");
        var preset = ExportPresets.YouTube1080p;

        // Act
        var config = await encoder.SelectBestEncoderAsync(preset, preferHardware: false);

        // Assert
        Assert.NotNull(config);
        Assert.False(config.IsHardwareAccelerated);
        Assert.Contains("libx264", config.EncoderName);
    }

    [Fact]
    public async Task SelectBestEncoderAsync_ReturnsValidParameters()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");
        var preset = ExportPresets.YouTube1080p;

        // Act
        var config = await encoder.SelectBestEncoderAsync(preset, preferHardware: false);

        // Assert
        Assert.NotNull(config.Parameters);
        Assert.Contains("-c:v", config.Parameters.Keys);
        Assert.Contains("-preset", config.Parameters.Keys);
        Assert.Contains("-b:v", config.Parameters.Keys);
    }

    [Fact]
    public async Task SelectBestEncoderAsync_Draft_UsesFastPreset()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");
        var preset = ExportPresets.DraftPreview;

        // Act
        var config = await encoder.SelectBestEncoderAsync(preset, preferHardware: false);

        // Assert
        Assert.Contains("-preset", config.Parameters.Keys);
        Assert.Equal("ultrafast", config.Parameters["-preset"]);
    }

    [Fact]
    public async Task SelectBestEncoderAsync_Maximum_UsesSlowPreset()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");
        var preset = ExportPresets.MasterArchive;

        // Act
        var config = await encoder.SelectBestEncoderAsync(preset, preferHardware: false);

        // Assert
        Assert.Contains("-preset", config.Parameters.Keys);
        Assert.Equal("veryslow", config.Parameters["-preset"]);
    }

    [Fact]
    public async Task SelectBestEncoderAsync_H265_UsesLibx265()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");
        var preset = ExportPresets.YouTube4K; // Uses H.265

        // Act
        var config = await encoder.SelectBestEncoderAsync(preset, preferHardware: false);

        // Assert
        Assert.Contains("libx265", config.EncoderName);
    }

    [Fact]
    public void GetEncoderArguments_FormatsCorrectly()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");
        var config = new EncoderConfig(
            EncoderName: "libx264",
            Description: "Test",
            IsHardwareAccelerated: false,
            Parameters: new System.Collections.Generic.Dictionary<string, string>
            {
                ["-c:v"] = "libx264",
                ["-preset"] = "medium",
                ["-crf"] = "23"
            }
        );

        // Act
        var args = encoder.GetEncoderArguments(config);

        // Assert
        Assert.Contains("-c:v libx264", args);
        Assert.Contains("-preset medium", args);
        Assert.Contains("-crf 23", args);
    }

    [Theory]
    [InlineData(QualityLevel.Draft, "28")]
    [InlineData(QualityLevel.Good, "23")]
    [InlineData(QualityLevel.High, "20")]
    [InlineData(QualityLevel.Maximum, "18")]
    public async Task SelectBestEncoderAsync_UsesCorrectCRF(QualityLevel quality, string expectedCrf)
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");
        var preset = new ExportPreset(
            Name: "Test",
            Description: "Test",
            Platform: Platform.Generic,
            Container: "mp4",
            VideoCodec: "libx264",
            AudioCodec: "aac",
            Resolution: new Resolution(1920, 1080),
            FrameRate: 30,
            VideoBitrate: 8000,
            AudioBitrate: 192,
            PixelFormat: "yuv420p",
            ColorSpace: "bt709",
            AspectRatio: AspectRatio.SixteenByNine,
            Quality: quality
        );

        // Act
        var config = await encoder.SelectBestEncoderAsync(preset, preferHardware: false);

        // Assert
        Assert.Contains("-crf", config.Parameters.Keys);
        Assert.Equal(expectedCrf, config.Parameters["-crf"]);
    }

    [Fact]
    public async Task SelectBestEncoderAsync_IncludesBitrate()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");
        var preset = ExportPresets.YouTube1080p;

        // Act
        var config = await encoder.SelectBestEncoderAsync(preset, preferHardware: false);

        // Assert
        Assert.Contains("-b:v", config.Parameters.Keys);
        Assert.Equal("8000k", config.Parameters["-b:v"]);
    }

    [Fact]
    public async Task SelectBestEncoderAsync_IncludesPixelFormat()
    {
        // Arrange
        var encoder = new HardwareEncoder(_mockLogger.Object, "ffmpeg");
        var preset = ExportPresets.YouTube1080p;

        // Act
        var config = await encoder.SelectBestEncoderAsync(preset, preferHardware: false);

        // Assert
        Assert.Contains("-pix_fmt", config.Parameters.Keys);
        Assert.Equal("yuv420p", config.Parameters["-pix_fmt"]);
    }
}
