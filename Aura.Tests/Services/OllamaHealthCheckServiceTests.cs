using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests.Services;

public class OllamaHealthCheckServiceTests
{
    private readonly Mock<ILogger<OllamaHealthCheckService>> _mockLogger;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;

    public OllamaHealthCheckServiceTests()
    {
        _mockLogger = new Mock<ILogger<OllamaHealthCheckService>>();
        _mockCache = new Mock<IMemoryCache>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOllamaRunning_ReturnsHealthyStatus()
    {
        // Arrange
        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { version = "0.1.17" }))
        };

        var tagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                models = new[]
                {
                    new { name = "llama3.1:8b-q4_k_m", size = 4661224736L, modified_at = "2024-01-01T00:00:00Z" }
                }
            }))
        };

        var psResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { models = Array.Empty<object>() }))
        };

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(versionResponse)
            .ReturnsAsync(tagsResponse)
            .ReturnsAsync(psResponse);

        object? cacheValue = null;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);
        
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(Mock.Of<ICacheEntry>());

        var service = new OllamaHealthCheckService(_mockLogger.Object, _httpClient, _mockCache.Object);

        // Act
        var result = await service.PerformHealthCheckAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("0.1.17", result.Version);
        Assert.Single(result.AvailableModels);
        Assert.Contains("llama3.1:8b-q4_k_m", result.AvailableModels);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOllamaNotRunning_ReturnsUnhealthyStatus()
    {
        // Arrange
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        object? cacheValue = null;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(false);

        var service = new OllamaHealthCheckService(_mockLogger.Object, _httpClient, _mockCache.Object);

        // Act
        var result = await service.PerformHealthCheckAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsHealthy);
        Assert.Null(result.Version);
        Assert.Empty(result.AvailableModels);
        Assert.Empty(result.RunningModels);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Connection refused", result.ErrorMessage);
    }

    [Fact]
    public async Task CheckHealthAsync_UsesCachedResult_WhenAvailable()
    {
        // Arrange
        var cachedHealth = new OllamaHealthStatus(
            IsHealthy: true,
            Version: "0.1.17",
            AvailableModels: new List<string> { "llama3.1:8b-q4_k_m" },
            RunningModels: new List<string>(),
            BaseUrl: "http://localhost:11434",
            ResponseTimeMs: 100,
            ErrorMessage: null,
            LastChecked: DateTime.UtcNow
        );

        object? cacheValue = cachedHealth;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
            .Returns(true);

        var service = new OllamaHealthCheckService(_mockLogger.Object, _httpClient, _mockCache.Object);

        // Act
        var result = await service.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsHealthy);
        Assert.Equal("0.1.17", result.Version);
        Assert.Single(result.AvailableModels);
        
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public void ClearCache_RemovesCachedHealth()
    {
        // Arrange
        var service = new OllamaHealthCheckService(_mockLogger.Object, _httpClient, _mockCache.Object);

        // Act
        service.ClearCache();

        // Assert
        _mockCache.Verify(x => x.Remove(It.IsAny<object>()), Times.Once);
    }
}
