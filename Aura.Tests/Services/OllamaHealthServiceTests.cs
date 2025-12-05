using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests.Services;

public class OllamaHealthServiceTests
{
    private readonly IMemoryCache _memoryCache;

    public OllamaHealthServiceTests()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOllamaRunning_ReturnsHealthy()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/api/version")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
            });

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/api/tags")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"models\":[{\"name\":\"llama3.1:8b\"}]}", Encoding.UTF8, "application/json")
            });

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/api/ps")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"models\":[]}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            httpClient,
            _memoryCache,
            "http://127.0.0.1:11434");

        // Act
        var status = await service.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.True(status.IsHealthy);
        Assert.Equal("0.1.0", status.Version);
        Assert.Single(status.AvailableModels);
        Assert.Contains("llama3.1:8b", status.AvailableModels);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenOllamaNotRunning_ReturnsUnhealthy()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            httpClient,
            _memoryCache,
            "http://127.0.0.1:11434");

        // Act
        var status = await service.CheckHealthAsync(CancellationToken.None);

        // Assert
        Assert.False(status.IsHealthy);
        Assert.Contains("Cannot connect to Ollama", status.ErrorMessage ?? "");
    }

    [Fact]
    public async Task WaitForOllamaAsync_WhenOllamaStartsLater_ReturnsTrue()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new HttpRequestException("Connection refused");
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            httpClient,
            _memoryCache,
            "http://127.0.0.1:11434");

        // Act
        var result = await service.WaitForOllamaAsync(
            maxRetries: 5,
            retryDelayMs: 100,
            ct: CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(callCount >= 3);
    }

    [Fact]
    public async Task WaitForOllamaAsync_WhenOllamaNeverStarts_ReturnsFalse()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            httpClient,
            _memoryCache,
            "http://127.0.0.1:11434");

        // Act
        var result = await service.WaitForOllamaAsync(
            maxRetries: 3,
            retryDelayMs: 100,
            ct: CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CheckHealthAsync_CachesResult_ForDuration()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/api/version")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
                };
            });

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/api/tags")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"models\":[]}", Encoding.UTF8, "application/json")
            });

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri!.PathAndQuery.Contains("/api/ps")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"models\":[]}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            httpClient,
            _memoryCache,
            "http://127.0.0.1:11434");

        // Act - make multiple calls
        await service.CheckHealthAsync(CancellationToken.None);
        await service.CheckHealthAsync(CancellationToken.None);
        await service.CheckHealthAsync(CancellationToken.None);

        // Assert - should only call once due to caching (within 30 second window)
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void ClearCache_RemovesCachedResult()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHandler.Object);
        var cache = new MemoryCache(new MemoryCacheOptions());

        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            httpClient,
            cache,
            "http://127.0.0.1:11434");

        // Put something in cache first
        cache.Set("ollama:health", new OllamaHealthStatus(
            IsHealthy: true,
            Version: "test",
            AvailableModels: new List<string>(),
            RunningModels: new List<string>(),
            BaseUrl: "http://127.0.0.1:11434",
            ResponseTimeMs: 10,
            ErrorMessage: null,
            LastChecked: DateTime.UtcNow
        ));

        // Act
        service.ClearCache();

        // Assert
        var cached = cache.Get("ollama:health");
        Assert.Null(cached);
    }

    [Fact]
    public async Task WaitForOllamaAsync_RespectsMaxRetries()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback(() => callCount++)
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            httpClient,
            _memoryCache,
            "http://127.0.0.1:11434");

        // Act
        var result = await service.WaitForOllamaAsync(
            maxRetries: 3,
            retryDelayMs: 50,
            ct: CancellationToken.None);

        // Assert
        Assert.False(result);
        // Should call exactly 3 times (once per retry)
        Assert.Equal(3, callCount);
    }

    [Fact]
    public async Task WaitForOllamaAsync_RespectsCancellationToken()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new OllamaHealthCheckService(
            NullLogger<OllamaHealthCheckService>.Instance,
            httpClient,
            _memoryCache,
            "http://127.0.0.1:11434");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            service.WaitForOllamaAsync(
                maxRetries: 10,
                retryDelayMs: 1000,
                ct: cts.Token));
    }
}
