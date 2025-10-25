using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models;
using Aura.Api.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class ApiKeyValidationServiceTests
{
    private readonly Mock<ILogger<ApiKeyValidationService>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

    public ApiKeyValidationServiceTests()
    {
        _mockLogger = new Mock<ILogger<ApiKeyValidationService>>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
    }

    [Fact]
    public async Task ValidateOpenAIKeyAsync_WithValidKey_ReturnsSuccess()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"data\": []}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidateOpenAIKeyAsync("sk-test123456789");

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.AccountInfo);
        Assert.Equal("OpenAI", result.AccountInfo["provider"]);
    }

    [Fact]
    public async Task ValidateOpenAIKeyAsync_WithEmptyKey_ReturnsFailure()
    {
        // Arrange
        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidateOpenAIKeyAsync("");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("API key is required", result.ErrorMessage);
        Assert.Equal("MISSING_KEY", result.ErrorCode);
        Assert.NotNull(result.Suggestions);
    }

    [Fact]
    public async Task ValidateOpenAIKeyAsync_WithUnauthorized_ReturnsInvalidKey()
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
                StatusCode = HttpStatusCode.Unauthorized
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidateOpenAIKeyAsync("sk-invalid");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Invalid API key", result.ErrorMessage);
        Assert.Equal("INVALID_KEY", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateAnthropicKeyAsync_WithValidKey_ReturnsSuccess()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\": \"msg_123\"}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidateAnthropicKeyAsync("sk-ant-test123");

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.AccountInfo);
        Assert.Equal("Anthropic", result.AccountInfo["provider"]);
    }

    [Fact]
    public async Task ValidateGeminiKeyAsync_WithValidKey_ReturnsSuccess()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"models\": []}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidateGeminiKeyAsync("AIzatest123");

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateElevenLabsKeyAsync_WithValidKey_ReturnsSuccess()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"subscription\": {\"tier\": \"free\"}}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidateElevenLabsKeyAsync("test_key_123");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.AccountInfo);
        Assert.Equal("free", result.AccountInfo["tier"]);
    }

    [Fact]
    public async Task ValidatePlayHTKeyAsync_WithValidCredentials_ReturnsSuccess()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidatePlayHTKeyAsync("user123", "secret_key_456");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.AccountInfo);
        Assert.Equal("user123", result.AccountInfo["userId"]);
    }

    [Fact]
    public async Task ValidatePlayHTKeyAsync_WithMissingCredentials_ReturnsFailure()
    {
        // Arrange
        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidatePlayHTKeyAsync("", "");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("User ID and Secret Key are required", result.ErrorMessage);
        Assert.Equal("MISSING_CREDENTIALS", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateReplicateKeyAsync_WithValidToken_ReturnsSuccess()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"username\": \"testuser\"}")
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidateReplicateKeyAsync("r8_test123");

        // Assert
        Assert.True(result.IsValid);
        Assert.NotNull(result.AccountInfo);
        Assert.Equal("testuser", result.AccountInfo["username"]);
    }

    [Fact]
    public async Task ValidateOpenAIKeyAsync_WithRateLimit_ReturnsRateLimitError()
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
                StatusCode = HttpStatusCode.TooManyRequests
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidateOpenAIKeyAsync("sk-test");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Rate limit exceeded", result.ErrorMessage);
        Assert.Equal("RATE_LIMIT", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateOpenAIKeyAsync_WithForbidden_ReturnsPermissionError()
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
                StatusCode = HttpStatusCode.Forbidden
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        var service = new ApiKeyValidationService(_mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await service.ValidateOpenAIKeyAsync("sk-test");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("lacks permissions", result.ErrorMessage);
        Assert.Equal("INSUFFICIENT_PERMISSIONS", result.ErrorCode);
    }
}
