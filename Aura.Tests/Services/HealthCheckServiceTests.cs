using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services;

/// <summary>
/// Unit tests for HealthCheckService
/// </summary>
public class HealthCheckServiceTests
{
    [Fact]
    public void CheckLiveness_ReturnsHealthyStatus()
    {
        // Arrange - create minimal mocks
        var loggerMock = new Mock<ILogger<HealthCheckService>>();
        var ffmpegLocatorMock = new Mock<IFfmpegLocator>();
        var providerSettingsMock = CreateProviderSettingsMock();
        var ttsProviderFactoryMock = new Mock<TtsProviderFactory>();

        var service = new HealthCheckService(
            loggerMock.Object,
            ffmpegLocatorMock.Object,
            providerSettingsMock,
            ttsProviderFactoryMock.Object);

        // Act
        var result = service.CheckLiveness();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("healthy", result.Status);
        Assert.NotNull(result.Checks);
        Assert.NotEmpty(result.Checks);
    }

    [Fact]
    public async Task GetSystemHealthAsync_ReturnsHealthResponse()
    {
        // Arrange
        var correlationId = "test-correlation-id";
        var loggerMock = new Mock<ILogger<HealthCheckService>>();
        var ffmpegLocatorMock = new Mock<IFfmpegLocator>();
        var providerSettingsMock = CreateProviderSettingsMock();
        var ttsProviderFactoryMock = new Mock<TtsProviderFactory>();
        
        ffmpegLocatorMock
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = true,
                FfmpegPath = "/usr/bin/ffmpeg",
                VersionString = "6.0"
            });

        var service = new HealthCheckService(
            loggerMock.Object,
            ffmpegLocatorMock.Object,
            providerSettingsMock,
            ttsProviderFactoryMock.Object);

        // Act
        var result = await service.GetSystemHealthAsync(correlationId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.BackendOnline);
        Assert.NotNull(result.Version);
        Assert.Equal(correlationId, result.CorrelationId);
        Assert.NotNull(result.Database);
        Assert.NotNull(result.Ffmpeg);
        Assert.True(result.Ffmpeg.Installed);
        Assert.True(result.Ffmpeg.Valid);
        Assert.Equal("6.0", result.Ffmpeg.Version);
        Assert.NotNull(result.ProvidersSummary);
    }

    [Fact]
    public async Task GetSystemHealthAsync_WithMissingFfmpeg_ReturnsDegradedStatus()
    {
        // Arrange
        var correlationId = "test-correlation-id";
        var loggerMock = new Mock<ILogger<HealthCheckService>>();
        var ffmpegLocatorMock = new Mock<IFfmpegLocator>();
        var providerSettingsMock = CreateProviderSettingsMock();
        var ttsProviderFactoryMock = new Mock<TtsProviderFactory>();
        
        ffmpegLocatorMock
            .Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = false,
                Reason = "FFmpeg not found in PATH or standard locations"
            });

        var service = new HealthCheckService(
            loggerMock.Object,
            ffmpegLocatorMock.Object,
            providerSettingsMock,
            ttsProviderFactoryMock.Object);

        // Act
        var result = await service.GetSystemHealthAsync(correlationId, CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.BackendOnline);
        Assert.Equal("degraded", result.OverallStatus);
        Assert.NotNull(result.Ffmpeg);
        Assert.False(result.Ffmpeg.Installed);
        Assert.False(result.Ffmpeg.Valid);
    }

    private static ProviderSettings CreateProviderSettingsMock()
    {
        // Create a minimal instance that returns safe defaults
        var loggerMock = new Mock<ILogger<ProviderSettings>>();
        var ffmpegConfigMock = new Mock<IFfmpegConfigurationService>();
        return new ProviderSettings(loggerMock.Object, ffmpegConfigMock.Object);
    }
}
