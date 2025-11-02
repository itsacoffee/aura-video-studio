using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Providers.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for ElevenLabsValidator
/// </summary>
public class ElevenLabsValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WithNoApiKey_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync(null, null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("not configured", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_WithEmptyApiKey_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync("   ", null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("not configured", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_WithInvalidFormatApiKey_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act - key too short
        var result1 = await validator.ValidateAsync("short", null, CancellationToken.None);
        
        // Act - key has invalid characters
        var result2 = await validator.ValidateAsync("12345678901234567890123456789xyz", null, CancellationToken.None);
        
        // Act - key correct length but not hex
        var result3 = await validator.ValidateAsync("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", null, CancellationToken.None);

        // Assert
        Assert.False(result1.Ok);
        Assert.Contains("format invalid", result1.Details, StringComparison.OrdinalIgnoreCase);
        
        Assert.False(result2.Ok);
        Assert.Contains("format invalid", result2.Details, StringComparison.OrdinalIgnoreCase);
        
        Assert.False(result3.Ok);
        Assert.Contains("format invalid", result3.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_TrimsWhitespaceFromApiKey()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString() == "https://api.elevenlabs.io/v1/user" &&
                    req.Headers.Contains("xi-api-key")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"subscription\": {}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act - key with leading/trailing whitespace
        var apiKeyWithWhitespace = "  abcdef1234567890abcdef1234567890  ";
        var result = await validator.ValidateAsync(apiKeyWithWhitespace, null, CancellationToken.None);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("Connected successfully", result.Details);
    }

    [Fact]
    public async Task ValidateAsync_WithUnauthorized_ReturnsInvalidKey()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("{\"detail\": {\"message\": \"Invalid API key\"}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync("abcdef1234567890abcdef1234567890", null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("invalid", result.Details, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("verify you copied it correctly", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_WithForbidden_ReturnsNoAccess()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent("{\"detail\": {\"message\": \"Access denied\"}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync("abcdef1234567890abcdef1234567890", null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("no access", result.Details, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("subscription", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_WithRateLimit_ReturnsRateLimitError()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = (HttpStatusCode)429,
                Content = new StringContent("{\"detail\": {\"message\": \"Rate limit exceeded\"}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync("abcdef1234567890abcdef1234567890", null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("Rate limit exceeded", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_WithValidApiKey_ReturnsSuccess()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString() == "https://api.elevenlabs.io/v1/user" &&
                    req.Headers.Contains("xi-api-key")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"subscription\": {\"tier\": \"free\"}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync("abcdef1234567890abcdef1234567890", null, CancellationToken.None);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("Connected successfully", result.Details);
    }

    [Fact]
    public async Task ValidateAsync_WithNetworkError_ReturnsNetworkError()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act
        var result = await validator.ValidateAsync("abcdef1234567890abcdef1234567890", null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("Could not reach ElevenLabs API", result.Details, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("internet connection", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_CallsUserEndpoint_NotVoicesEndpoint()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString() == "https://api.elevenlabs.io/v1/user"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"subscription\": {}}")
            })
            .Verifiable();

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act
        await validator.ValidateAsync("abcdef1234567890abcdef1234567890", null, CancellationToken.None);

        // Assert - verify it called the user endpoint
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.RequestUri!.ToString() == "https://api.elevenlabs.io/v1/user"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_UsesCorrectHeader()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Headers.Contains("xi-api-key")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"subscription\": {}}")
            })
            .Verifiable();

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(
            NullLogger<ElevenLabsValidator>.Instance,
            httpClient);

        // Act
        await validator.ValidateAsync("abcdef1234567890abcdef1234567890", null, CancellationToken.None);

        // Assert - verify it used the xi-api-key header
        mockHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Headers.Contains("xi-api-key")),
            ItExpr.IsAny<CancellationToken>());
    }
}
