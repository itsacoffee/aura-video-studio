using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Services.FFmpeg;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.FFmpeg;

public class HardwareAccelerationDetectorTests
{
    private readonly Mock<IFFmpegService> _mockFFmpegService;
    private readonly Mock<IHardwareDetector> _mockHardwareDetector;
    private readonly Mock<ILogger<HardwareAccelerationDetector>> _mockLogger;
    private readonly HardwareAccelerationDetector _detector;

    public HardwareAccelerationDetectorTests()
    {
        _mockFFmpegService = new Mock<IFFmpegService>();
        _mockHardwareDetector = new Mock<IHardwareDetector>();
        _mockLogger = new Mock<ILogger<HardwareAccelerationDetector>>();
        _detector = new HardwareAccelerationDetector(
            _mockFFmpegService.Object,
            _mockHardwareDetector.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task DetectAsync_WithNvidiaGpu_ShouldDetectNvenc()
    {
        // Arrange
        var systemProfile = new SystemProfile
        {
            Gpu = new GpuInfo("NVIDIA", "GeForce RTX 3080", 10, "30")
        };

        _mockHardwareDetector
            .Setup(h => h.DetectSystemAsync())
            .ReturnsAsync(systemProfile);

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync("-hide_banner -encoders", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult
            {
                Success = true,
                StandardOutput = " V..... h264_nvenc           NVIDIA NVENC H.264 encoder\n V..... hevc_nvenc           NVIDIA NVENC hevc encoder"
            });

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync("-hide_banner -decoders", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult
            {
                Success = true,
                StandardOutput = " V..... h264_cuvid           Nvidia CUVID H264 decoder"
            });

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync("-hide_banner -hwaccels", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult
            {
                Success = true,
                StandardOutput = "Hardware acceleration methods:\ncuda\ndxva2"
            });

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal("nvenc", result.AccelerationType);
        Assert.Equal("h264_nvenc", result.VideoCodec);
        Assert.Equal("cuda", result.HwaccelDevice);
        Assert.Contains("h264_nvenc", result.SupportedEncoders);
        Assert.Contains("hevc_nvenc", result.SupportedEncoders);
    }

    [Fact]
    public async Task DetectAsync_WithIntelGpu_ShouldDetectQsv()
    {
        // Arrange
        var systemProfile = new SystemProfile
        {
            Gpu = new GpuInfo("Intel", "Arc A770", 16, "Arc")
        };

        _mockHardwareDetector
            .Setup(h => h.DetectSystemAsync())
            .ReturnsAsync(systemProfile);

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync("-hide_banner -encoders", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult
            {
                Success = true,
                StandardOutput = " V..... h264_qsv             H.264 (Intel Quick Sync Video acceleration)"
            });

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync("-hide_banner -decoders", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult
            {
                Success = true,
                StandardOutput = " V..... h264_qsv             H264 video (Intel Quick Sync Video acceleration)"
            });

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync("-hide_banner -hwaccels", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult
            {
                Success = true,
                StandardOutput = "Hardware acceleration methods:\nqsv\ndxva2"
            });

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal("qsv", result.AccelerationType);
        Assert.Equal("h264_qsv", result.VideoCodec);
        Assert.Equal("qsv", result.HwaccelDevice);
    }

    [Fact]
    public async Task DetectAsync_WithNoHardwareAcceleration_ShouldReturnSoftwareEncoding()
    {
        // Arrange
        var systemProfile = new SystemProfile
        {
            Gpu = null
        };

        _mockHardwareDetector
            .Setup(h => h.DetectSystemAsync())
            .ReturnsAsync(systemProfile);

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult
            {
                Success = true,
                StandardOutput = " V..... libx264             libx264 H.264 / AVC / MPEG-4 AVC / MPEG-4 part 10"
            });

        // Act
        var result = await _detector.DetectAsync();

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Equal("software", result.AccelerationType);
        Assert.Equal("libx264", result.VideoCodec);
    }

    [Fact]
    public async Task GetOptimalEncoderSettingsAsync_WithHardwareAcceleration_ShouldReturnHardwareSettings()
    {
        // Arrange
        var hwInfo = new HardwareAccelerationInfo
        {
            IsAvailable = true,
            AccelerationType = "nvenc",
            VideoCodec = "h264_nvenc",
            HwaccelDevice = "cuda",
            RecommendedPreset = "p4"
        };

        var resolution = new Resolution(1920, 1080);
        var targetBitrate = 5000;

        // Act
        var result = await _detector.GetOptimalEncoderSettingsAsync(hwInfo, resolution, targetBitrate);

        // Assert
        Assert.Equal("h264_nvenc", result.Encoder);
        Assert.Equal("p4", result.Preset);
        Assert.Equal("cuda", result.HwaccelFlag);
        Assert.Equal(targetBitrate, result.Bitrate);
        Assert.Contains("rc", result.EncoderOptions.Keys);
    }

    [Fact]
    public async Task GetOptimalEncoderSettingsAsync_WithoutHardwareAcceleration_ShouldReturnSoftwareSettings()
    {
        // Arrange
        var hwInfo = new HardwareAccelerationInfo
        {
            IsAvailable = false
        };

        var resolution = new Resolution(1920, 1080);
        var targetBitrate = 5000;

        // Act
        var result = await _detector.GetOptimalEncoderSettingsAsync(hwInfo, resolution, targetBitrate);

        // Assert
        Assert.Equal("libx264", result.Encoder);
        Assert.NotNull(result.Crf);
        Assert.Contains("profile", result.EncoderOptions.Keys);
    }

    [Fact]
    public async Task IsCodecSupportedAsync_WithSupportedCodec_ShouldReturnTrue()
    {
        // Arrange
        var systemProfile = new SystemProfile
        {
            Gpu = new GpuInfo("NVIDIA", "GeForce RTX 3080", 10, "30")
        };

        _mockHardwareDetector
            .Setup(h => h.DetectSystemAsync())
            .ReturnsAsync(systemProfile);

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync("-hide_banner -encoders", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult
            {
                Success = true,
                StandardOutput = " V..... h264_nvenc           NVIDIA NVENC H.264 encoder"
            });

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync("-hide_banner -decoders", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardOutput = "" });

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync("-hide_banner -hwaccels", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardOutput = "cuda" });

        // Act
        var result = await _detector.IsCodecSupportedAsync("h264_nvenc");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DetectAsync_ShouldCacheResult()
    {
        // Arrange
        var systemProfile = new SystemProfile
        {
            Gpu = new GpuInfo("NVIDIA", "GeForce RTX 3080", 10, "30")
        };

        _mockHardwareDetector
            .Setup(h => h.DetectSystemAsync())
            .ReturnsAsync(systemProfile);

        _mockFFmpegService
            .Setup(s => s.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FFmpegResult { Success = true, StandardOutput = "cuda\nh264_nvenc" });

        // Act
        var result1 = await _detector.DetectAsync();
        var result2 = await _detector.DetectAsync();

        // Assert
        Assert.Equal(result1, result2);
        // Should only call FFmpeg once due to caching
        _mockFFmpegService.Verify(
            s => s.ExecuteAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>()),
            Times.Exactly(3) // encoders, decoders, hwaccels
        );
    }
}
