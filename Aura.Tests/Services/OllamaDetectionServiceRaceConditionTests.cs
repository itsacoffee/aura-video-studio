using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests.Services;

/// <summary>
/// Tests for OllamaDetectionService race condition fixes
/// Validates that detection completes properly and can be awaited
/// </summary>
public class OllamaDetectionServiceRaceConditionTests : IDisposable
{
    private readonly IMemoryCache _cache;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;

    public OllamaDetectionServiceRaceConditionTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _cache?.Dispose();
    }

    [Fact]
    public async Task IsDetectionComplete_Should_BeFalse_BeforeStart()
    {
        // Arrange
        var service = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        // Assert
        Assert.False(service.IsDetectionComplete);
    }

    [Fact]
    public async Task IsDetectionComplete_Should_BecomeTrue_AfterDetection()
    {
        // Arrange
        SetupSuccessfulOllamaResponse();

        var service = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        // Act
        await service.StartAsync(CancellationToken.None);
        await service.WaitForInitialDetectionAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(service.IsDetectionComplete);
    }

    [Fact]
    public async Task WaitForInitialDetectionAsync_Should_CompleteWithinTimeout_WhenOllamaAvailable()
    {
        // Arrange
        SetupSuccessfulOllamaResponse();

        var service = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        await service.StartAsync(CancellationToken.None);

        // Act
        var result = await service.WaitForInitialDetectionAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(result);
        Assert.True(service.IsDetectionComplete);
    }

    [Fact]
    public async Task WaitForInitialDetectionAsync_Should_ReturnFalse_WhenTimeout()
    {
        // Arrange
        SetupSlowOllamaResponse();

        var service = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        await service.StartAsync(CancellationToken.None);

        // Act
        var result = await service.WaitForInitialDetectionAsync(TimeSpan.FromMilliseconds(100));

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task WaitForInitialDetectionAsync_Should_ReturnTrue_WhenAlreadyComplete()
    {
        // Arrange
        SetupSuccessfulOllamaResponse();

        var service = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        await service.StartAsync(CancellationToken.None);
        await service.WaitForInitialDetectionAsync(TimeSpan.FromSeconds(5));

        // Act - Call again after detection is complete
        var result = await service.WaitForInitialDetectionAsync(TimeSpan.FromSeconds(1));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetStatusAsync_Should_UseCache_AfterDetection()
    {
        // Arrange
        SetupSuccessfulOllamaResponse();

        var service = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        await service.StartAsync(CancellationToken.None);
        await service.WaitForInitialDetectionAsync(TimeSpan.FromSeconds(5));

        // Act
        var status1 = await service.GetStatusAsync();
        var status2 = await service.GetStatusAsync();

        // Assert - Both should return the same cached result
        Assert.True(status1.IsRunning);
        Assert.True(status2.IsRunning);
        Assert.Equal(status1.Version, status2.Version);
    }

    [Fact]
    public async Task DetectOllamaAsync_Should_MarkDetectionComplete_EvenOnFailure()
    {
        // Arrange
        SetupFailedOllamaResponse();

        var service = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        // Act
        await service.StartAsync(CancellationToken.None);
        await service.WaitForInitialDetectionAsync(TimeSpan.FromSeconds(5));

        // Assert - Detection should be marked complete even if Ollama is not running
        Assert.True(service.IsDetectionComplete);
    }

    private void SetupSuccessfulOllamaResponse()
    {
        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"0.1.0\"}")
        };

        var tagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"models\":[]}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/version")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(versionResponse);

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/tags")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(tagsResponse);
    }

    private void SetupSlowOllamaResponse()
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"version\":\"0.1.0\"}")
                };
            });
    }

    private void SetupFailedOllamaResponse()
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection refused"));
    }
}
