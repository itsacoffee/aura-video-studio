using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models;
using Aura.Api.Services;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

public class HealthCheckServiceTests
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly ILogger<ProviderSettings> _providerSettingsLogger;

    public HealthCheckServiceTests()
    {
        _logger = NullLogger<HealthCheckService>.Instance;
        _providerSettingsLogger = NullLogger<ProviderSettings>.Instance;
    }

    private Mock<TtsProviderFactory> CreateMockTtsProviderFactory()
    {
        return new Mock<TtsProviderFactory>(null, null, null);
    }

    [Fact]
    public void CheckLiveness_Should_ReturnHealthy()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var mockTtsProviderFactory = new Mock<TtsProviderFactory>(null, null, null);
        var service = new HealthCheckService(_logger, mockFfmpegLocator.Object, providerSettings, mockTtsProviderFactory.Object);

        // Act
        var result = service.CheckLiveness();

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.Single(result.Checks);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task CheckReadinessAsync_Should_ReturnHealthy_WhenAllChecksPass()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = true,
                FfmpegPath = "/usr/bin/ffmpeg",
                VersionString = "6.0"
            });

        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var service = new HealthCheckService(_logger, mockFfmpegLocator.Object, providerSettings, CreateMockTtsProviderFactory().Object);

        // Act
        var result = await service.CheckReadinessAsync();

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotEmpty(result.Checks);
        Assert.Empty(result.Errors);
        
        // Verify FFmpeg check passed
        var ffmpegCheck = Assert.Single(result.Checks, c => c.Name == "FFmpeg");
        Assert.Equal(HealthStatus.Healthy, ffmpegCheck.Status);
    }

    [Fact]
    public async Task CheckReadinessAsync_Should_ReturnUnhealthy_WhenFfmpegNotFound()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = false,
                Reason = "FFmpeg not found in any search path",
                AttemptedPaths = new() { "/usr/bin/ffmpeg", "/usr/local/bin/ffmpeg" }
            });

        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var service = new HealthCheckService(_logger, mockFfmpegLocator.Object, providerSettings, CreateMockTtsProviderFactory().Object);

        // Act
        var result = await service.CheckReadinessAsync();

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotEmpty(result.Errors);
        
        // Verify FFmpeg check failed
        var ffmpegCheck = Assert.Single(result.Checks, c => c.Name == "FFmpeg");
        Assert.Equal(HealthStatus.Unhealthy, ffmpegCheck.Status);
        Assert.Contains("FFmpeg not found", ffmpegCheck.Message);
    }

    [Fact]
    public async Task CheckReadinessAsync_Should_IncludeTempDirectoryCheck()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult { Found = true, FfmpegPath = "/usr/bin/ffmpeg" });

        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var service = new HealthCheckService(_logger, mockFfmpegLocator.Object, providerSettings, CreateMockTtsProviderFactory().Object);

        // Act
        var result = await service.CheckReadinessAsync();

        // Assert
        var tempDirCheck = Assert.Single(result.Checks, c => c.Name == "TempDirectory");
        Assert.Equal(HealthStatus.Healthy, tempDirCheck.Status);
        Assert.Contains("writable", tempDirCheck.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckReadinessAsync_Should_IncludeProviderRegistryCheck()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult { Found = true, FfmpegPath = "/usr/bin/ffmpeg" });

        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var service = new HealthCheckService(_logger, mockFfmpegLocator.Object, providerSettings, CreateMockTtsProviderFactory().Object);

        // Act
        var result = await service.CheckReadinessAsync();

        // Assert
        var providerCheck = Assert.Single(result.Checks, c => c.Name == "ProviderRegistry");
        // Should be healthy or degraded (directories created by ProviderSettings)
        Assert.True(
            providerCheck.Status == HealthStatus.Healthy || providerCheck.Status == HealthStatus.Degraded,
            $"Expected healthy or degraded, got {providerCheck.Status}"
        );
    }

    [Fact]
    public async Task CheckReadinessAsync_Should_IncludePortAvailabilityCheck()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult { Found = true, FfmpegPath = "/usr/bin/ffmpeg" });

        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var service = new HealthCheckService(_logger, mockFfmpegLocator.Object, providerSettings, CreateMockTtsProviderFactory().Object);

        // Act
        var result = await service.CheckReadinessAsync();

        // Assert
        var portCheck = Assert.Single(result.Checks, c => c.Name == "PortAvailability");
        // Port check should pass with either healthy or degraded status
        Assert.True(
            portCheck.Status == HealthStatus.Healthy || portCheck.Status == HealthStatus.Degraded,
            $"Expected healthy or degraded, got {portCheck.Status}"
        );
    }

    [Fact]
    public async Task CheckReadinessAsync_Should_ReturnAllChecks()
    {
        // Arrange
        var mockFfmpegLocator = new Mock<IFfmpegLocator>();
        mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult { Found = true, FfmpegPath = "/usr/bin/ffmpeg" });

        var providerSettings = new ProviderSettings(_providerSettingsLogger);
        var service = new HealthCheckService(_logger, mockFfmpegLocator.Object, providerSettings, CreateMockTtsProviderFactory().Object);

        // Act
        var result = await service.CheckReadinessAsync();

        // Assert
        // Should have all 4 checks
        Assert.Equal(4, result.Checks.Count);
        Assert.Contains(result.Checks, c => c.Name == "FFmpeg");
        Assert.Contains(result.Checks, c => c.Name == "TempDirectory");
        Assert.Contains(result.Checks, c => c.Name == "ProviderRegistry");
        Assert.Contains(result.Checks, c => c.Name == "PortAvailability");
    }
}
