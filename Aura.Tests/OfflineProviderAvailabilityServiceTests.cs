using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class OfflineProviderAvailabilityServiceTests
{
    private readonly ILogger<OfflineProviderAvailabilityService> _logger;
    private readonly ILogger<ProviderSettings> _providerSettingsLogger;
    private readonly Mock<IHardwareDetector> _mockHardwareDetector;
    private readonly ProviderSettings _providerSettings;

    public OfflineProviderAvailabilityServiceTests()
    {
        _logger = NullLogger<OfflineProviderAvailabilityService>.Instance;
        _providerSettingsLogger = NullLogger<ProviderSettings>.Instance;
        _mockHardwareDetector = new Mock<IHardwareDetector>();
        _providerSettings = new ProviderSettings(_providerSettingsLogger);
    }

    [Fact]
    public async Task CheckAllProvidersAsync_Should_ReturnStatus()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OfflineProviderAvailabilityService(
            _logger,
            httpClient,
            _providerSettings,
            _mockHardwareDetector.Object
        );

        // Act
        var result = await service.CheckAllProvidersAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Piper);
        Assert.NotNull(result.Mimic3);
        Assert.NotNull(result.Ollama);
        Assert.NotNull(result.StableDiffusion);
        Assert.NotNull(result.WindowsTts);
    }

    [Fact]
    public async Task CheckPiperAsync_Should_ReturnNotAvailable_WhenPathNotConfigured()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new OfflineProviderAvailabilityService(
            _logger,
            httpClient,
            _providerSettings,
            _mockHardwareDetector.Object
        );

        // Act
        var result = await service.CheckPiperAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Piper TTS", result.Name);
        Assert.False(result.IsAvailable);
        Assert.Contains("not configured", result.Message);
        Assert.NotNull(result.InstallationGuideUrl);
        Assert.NotEmpty(result.Recommendations);
    }

    [Fact]
    public async Task CheckMimic3Async_Should_ReturnNotAvailable_WhenServerNotRunning()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OfflineProviderAvailabilityService(
            _logger,
            httpClient,
            _providerSettings,
            _mockHardwareDetector.Object
        );

        // Act
        var result = await service.CheckMimic3Async();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Mimic3 TTS", result.Name);
        Assert.False(result.IsAvailable);
        Assert.Contains("not running", result.Message);
        Assert.NotNull(result.InstallationGuideUrl);
        Assert.NotEmpty(result.Recommendations);
    }

    [Fact]
    public async Task CheckOllamaAsync_Should_ReturnNotAvailable_WhenServerNotRunning()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OfflineProviderAvailabilityService(
            _logger,
            httpClient,
            _providerSettings,
            _mockHardwareDetector.Object
        );

        // Act
        var result = await service.CheckOllamaAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Ollama", result.Name);
        Assert.False(result.IsAvailable);
        Assert.Contains("not running", result.Message);
        Assert.NotNull(result.InstallationGuideUrl);
        Assert.NotEmpty(result.Recommendations);
    }

    [Fact]
    public async Task CheckWindowsTtsAsync_Should_ReturnAvailable_OnWindows()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new OfflineProviderAvailabilityService(
            _logger,
            httpClient,
            _providerSettings,
            _mockHardwareDetector.Object
        );

        // Act
        var result = await service.CheckWindowsTtsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Windows TTS", result.Name);
        
        if (OperatingSystem.IsWindows())
        {
            Assert.True(result.IsAvailable);
            Assert.Contains("available", result.Message);
        }
        else
        {
            Assert.False(result.IsAvailable);
            Assert.Contains("only available on Windows", result.Message);
        }
    }

    [Fact]
    public async Task CheckStableDiffusionAsync_Should_ReturnNotAvailable_WhenServerNotRunning()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OfflineProviderAvailabilityService(
            _logger,
            httpClient,
            _providerSettings,
            _mockHardwareDetector.Object
        );

        // Act
        var result = await service.CheckStableDiffusionAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Stable Diffusion WebUI", result.Name);
        Assert.False(result.IsAvailable);
        Assert.Contains("not running", result.Message);
        Assert.NotNull(result.InstallationGuideUrl);
        Assert.NotEmpty(result.Recommendations);
    }

    [Fact]
    public async Task CheckAllProvidersAsync_Should_SetCorrectOperationalFlags()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var service = new OfflineProviderAvailabilityService(
            _logger,
            httpClient,
            _providerSettings,
            _mockHardwareDetector.Object
        );

        // Act
        var result = await service.CheckAllProvidersAsync();

        // Assert
        Assert.False(result.IsFullyOperational);
        
        if (OperatingSystem.IsWindows())
        {
            Assert.True(result.HasTtsProvider);
        }
    }
}
