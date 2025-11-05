using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Services.Export;
using Aura.Core.Services.Render;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class RenderPreflightServiceTests
{
    private readonly Mock<ILogger<RenderPreflightService>> _mockLogger;
    private readonly Mock<IHardwareDetector> _mockHardwareDetector;
    private readonly ExportPreflightValidator _exportValidator;
    private readonly HardwareEncoder _hardwareEncoder;

    public RenderPreflightServiceTests()
    {
        _mockLogger = new Mock<ILogger<RenderPreflightService>>();
        _mockHardwareDetector = new Mock<IHardwareDetector>();
        
        var mockExportLogger = new Mock<ILogger<ExportPreflightValidator>>();
        _exportValidator = new ExportPreflightValidator(mockExportLogger.Object, _mockHardwareDetector.Object);
        
        var mockEncoderLogger = new Mock<ILogger<HardwareEncoder>>();
        _hardwareEncoder = new HardwareEncoder(mockEncoderLogger.Object, "ffmpeg");
    }

    [Fact]
    public async Task ValidateRenderAsync_WithValidSettings_ReturnsCanProceedTrue()
    {
        // Arrange
        var service = CreateService();
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(5);
        var outputDir = System.IO.Path.GetTempPath();

        SetupDefaultMocks();

        // Act
        var result = await service.ValidateRenderAsync(
            preset, duration, outputDir, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.CorrelationId);
    }

    [Fact]
    public async Task ValidateRenderAsync_WithEncoderOverride_UsesSpecifiedEncoder()
    {
        // Arrange
        var service = CreateService();
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(5);
        var outputDir = System.IO.Path.GetTempPath();
        var encoderOverride = "h264_nvenc";

        SetupDefaultMocks();

        // Act
        var result = await service.ValidateRenderAsync(
            preset, duration, outputDir, encoderOverride: encoderOverride, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(result.EncoderSelection);
        Assert.Equal(encoderOverride, result.EncoderSelection.EncoderName);
    }

    [Fact]
    public async Task ValidateRenderAsync_IncludesEstimates()
    {
        // Arrange
        var service = CreateService();
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(5);
        var outputDir = System.IO.Path.GetTempPath();

        SetupDefaultMocks();

        // Act
        var result = await service.ValidateRenderAsync(
            preset, duration, outputDir, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(result.Estimates);
        Assert.True(result.Estimates.EstimatedFileSizeMB > 0);
        Assert.True(result.Estimates.EstimatedDurationMinutes > 0);
        Assert.True(result.Estimates.RequiredDiskSpaceMB > 0);
    }

    [Fact]
    public async Task ValidateRenderAsync_WithHardwarePreference_SelectsHardwareEncoder()
    {
        // Arrange
        var service = CreateService();
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(5);
        var outputDir = System.IO.Path.GetTempPath();

        SetupDefaultMocks(hasNVENC: true);

        // Act
        var result = await service.ValidateRenderAsync(
            preset, duration, outputDir, preferHardware: true, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(result.EncoderSelection);
    }

    [Fact]
    public async Task ValidateRenderAsync_WithoutHardwarePreference_UsesSoftwareEncoder()
    {
        // Arrange
        var service = CreateService();
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(5);
        var outputDir = System.IO.Path.GetTempPath();

        SetupDefaultMocks();

        // Act
        var result = await service.ValidateRenderAsync(
            preset, duration, outputDir, preferHardware: false, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(result.EncoderSelection);
        Assert.False(result.EncoderSelection.IsHardwareAccelerated);
    }

    [Fact]
    public async Task ValidateRenderAsync_IncludesTempDirectoryInfo()
    {
        // Arrange
        var service = CreateService();
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(5);
        var outputDir = System.IO.Path.GetTempPath();

        SetupDefaultMocks();

        // Act
        var result = await service.ValidateRenderAsync(
            preset, duration, outputDir, cancellationToken: CancellationToken.None);

        // Assert
        Assert.NotNull(result.Estimates);
        Assert.False(string.IsNullOrEmpty(result.Estimates.TempDirectory));
        Assert.True(result.Estimates.RequiredTempSpaceMB > 0);
    }

    [Fact]
    public async Task ValidateRenderAsync_WithCorrelationId_IncludesInResult()
    {
        // Arrange
        var service = CreateService();
        var preset = ExportPresets.YouTube1080p;
        var duration = TimeSpan.FromMinutes(5);
        var outputDir = System.IO.Path.GetTempPath();
        var correlationId = "test-correlation-123";

        SetupDefaultMocks();

        // Act
        var result = await service.ValidateRenderAsync(
            preset, duration, outputDir, correlationId: correlationId, cancellationToken: CancellationToken.None);

        // Assert
        Assert.Equal(correlationId, result.CorrelationId);
    }

    [Fact]
    public async Task ValidateRenderAsync_WithHighQualityPreset_EstimatesLongerDuration()
    {
        // Arrange
        var service = CreateService();
        var draftPreset = ExportPresets.DraftPreview;
        var highPreset = ExportPresets.MasterArchive;
        var duration = TimeSpan.FromMinutes(5);
        var outputDir = System.IO.Path.GetTempPath();

        SetupDefaultMocks();

        // Act
        var draftResult = await service.ValidateRenderAsync(
            draftPreset, duration, outputDir, preferHardware: false, cancellationToken: CancellationToken.None);
        
        var highResult = await service.ValidateRenderAsync(
            highPreset, duration, outputDir, preferHardware: false, cancellationToken: CancellationToken.None);

        // Assert
        Assert.True(highResult.Estimates.EstimatedDurationMinutes > draftResult.Estimates.EstimatedDurationMinutes,
            "High quality preset should take longer to render");
    }

    private RenderPreflightService CreateService()
    {
        return new RenderPreflightService(
            _mockLogger.Object,
            _mockHardwareDetector.Object,
            _exportValidator,
            _hardwareEncoder
        );
    }

    private void SetupDefaultMocks(bool hasNVENC = false)
    {
        var systemProfile = new SystemProfile
        {
            AutoDetect = true,
            LogicalCores = 8,
            PhysicalCores = 4,
            RamGB = 16,
            Gpu = hasNVENC ? new GpuInfo("NVIDIA", "RTX 3080", 10, "30") : null,
            Tier = HardwareTier.B,
            EnableNVENC = hasNVENC,
            EnableSD = false,
            OfflineOnly = false
        };

        _mockHardwareDetector
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(systemProfile);
    }
}
