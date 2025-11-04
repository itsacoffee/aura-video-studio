using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Services.Export;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Export;

public class ExportPreflightValidatorTests
{
    private readonly Mock<ILogger<ExportPreflightValidator>> _loggerMock;
    private readonly Mock<IHardwareDetector> _hardwareDetectorMock;
    private readonly ExportPreflightValidator _validator;

    public ExportPreflightValidatorTests()
    {
        _loggerMock = new Mock<ILogger<ExportPreflightValidator>>();
        _hardwareDetectorMock = new Mock<IHardwareDetector>();
        _validator = new ExportPreflightValidator(_loggerMock.Object, _hardwareDetectorMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_WithSufficientDiskSpace_ReturnsSuccess()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(2);
        var outputDir = Path.GetTempPath();
        
        _hardwareDetectorMock
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(CreateSystemProfile(HardwareTier.B));

        // Act
        var result = await _validator.ValidateAsync(preset, duration, outputDir);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanProceed);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Estimates);
        Assert.True(result.Estimates.EstimatedFileSizeMB > 0);
    }

    [Fact]
    public async Task ValidateAsync_WithAspectRatioMismatch_ReturnsWarning()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(1);
        var outputDir = Path.GetTempPath();
        var sourceAspectRatio = AspectRatio.NineBySixteen;
        
        _hardwareDetectorMock
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(CreateSystemProfile(HardwareTier.B));

        // Act
        var result = await _validator.ValidateAsync(
            preset, 
            duration, 
            outputDir,
            sourceAspectRatio: sourceAspectRatio);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanProceed);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("aspect ratio"));
    }

    [Fact]
    public async Task ValidateAsync_WithUpscaling_ReturnsWarning()
    {
        // Arrange
        var preset = ExportPresets.YouTube4K;
        var duration = TimeSpan.FromMinutes(1);
        var outputDir = Path.GetTempPath();
        var sourceResolution = new Resolution(1920, 1080);
        
        _hardwareDetectorMock
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(CreateSystemProfile(HardwareTier.B));

        // Act
        var result = await _validator.ValidateAsync(
            preset, 
            duration, 
            outputDir,
            sourceResolution: sourceResolution);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanProceed);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("upscaling"));
    }

    [Fact]
    public async Task ValidateAsync_WithTikTokExceedingMaxDuration_ReturnsError()
    {
        // Arrange
        var preset = ExportPresets.TikTok;
        var duration = TimeSpan.FromSeconds(700);
        var outputDir = Path.GetTempPath();
        
        _hardwareDetectorMock
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(CreateSystemProfile(HardwareTier.B));

        // Act
        var result = await _validator.ValidateAsync(preset, duration, outputDir);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.CanProceed);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("duration") && e.Contains("600"));
    }

    [Fact]
    public async Task ValidateAsync_WithNVENCAvailable_ReturnsHardwareEncoder()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(1);
        var outputDir = Path.GetTempPath();
        
        var systemProfile = CreateSystemProfile(HardwareTier.A);
        systemProfile = systemProfile with 
        { 
            EnableNVENC = true,
            Gpu = new GpuInfo("NVIDIA", "RTX 3080", 10, "30")
        };
        
        _hardwareDetectorMock
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(systemProfile);

        // Act
        var result = await _validator.ValidateAsync(preset, duration, outputDir);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanProceed);
        Assert.NotNull(result.Estimates);
        Assert.True(result.Estimates.HardwareAccelerationAvailable);
        Assert.Equal("h264_nvenc", result.Estimates.RecommendedEncoder);
    }

    [Fact]
    public async Task ValidateAsync_WithTierD_UsesSoftwareEncoder()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(1);
        var outputDir = Path.GetTempPath();
        
        _hardwareDetectorMock
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(CreateSystemProfile(HardwareTier.D));

        // Act
        var result = await _validator.ValidateAsync(preset, duration, outputDir);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanProceed);
        Assert.NotNull(result.Estimates);
        Assert.False(result.Estimates.HardwareAccelerationAvailable);
        Assert.Equal("libx264", result.Estimates.RecommendedEncoder);
    }

    [Fact]
    public async Task ValidateAsync_EstimatesDurationBasedOnTier()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(2);
        var outputDir = Path.GetTempPath();

        // Act with Tier A
        _hardwareDetectorMock
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(CreateSystemProfile(HardwareTier.A));
        var resultA = await _validator.ValidateAsync(preset, duration, outputDir);

        // Act with Tier D
        _hardwareDetectorMock
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(CreateSystemProfile(HardwareTier.D));
        var resultD = await _validator.ValidateAsync(preset, duration, outputDir);

        // Assert
        Assert.True(resultA.Estimates.EstimatedDurationMinutes < resultD.Estimates.EstimatedDurationMinutes,
            "Tier A should encode faster than Tier D");
    }

    [Fact]
    public void EstimateFileSizeMB_CalculatesCorrectly()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(5);

        // Act
        var estimatedSize = ExportPresets.EstimateFileSizeMB(preset, duration);

        // Assert
        Assert.True(estimatedSize > 0);
        var expectedMinSize = 200.0;
        var expectedMaxSize = 500.0;
        Assert.True(estimatedSize >= expectedMinSize && estimatedSize <= expectedMaxSize,
            $"Expected size between {expectedMinSize} and {expectedMaxSize}MB, got {estimatedSize}MB");
    }

    [Fact]
    public async Task ValidateAsync_CreatesOutputDirectoryIfMissing()
    {
        // Arrange
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(1);
        var outputDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        
        _hardwareDetectorMock
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(CreateSystemProfile(HardwareTier.B));

        try
        {
            // Act
            var result = await _validator.ValidateAsync(preset, duration, outputDir);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.CanProceed);
            Assert.True(Directory.Exists(outputDir), "Output directory should be created");
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }

    private SystemProfile CreateSystemProfile(HardwareTier tier)
    {
        return new SystemProfile
        {
            Tier = tier,
            LogicalCores = 8,
            PhysicalCores = 4,
            RamGB = 16,
            EnableNVENC = false,
            EnableSD = false,
            OfflineOnly = false
        };
    }
}
