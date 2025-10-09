using Xunit;
using Aura.Providers.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;

namespace Aura.Tests;

public class ProviderValidationTests
{
    [Fact]
    public async Task OpenAiValidator_NoApiKey_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new OpenAiValidator(NullLogger<OpenAiValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync(null, null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("OpenAI", result.Name);
        Assert.Contains("not configured", result.Details);
    }

    [Fact]
    public async Task OpenAiValidator_InvalidApiKey_ReturnsFailure()
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
                Content = new StringContent("{\"error\":{\"message\":\"Invalid API key\"}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new OpenAiValidator(NullLogger<OpenAiValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync("invalid-key", null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("OpenAI", result.Name);
        Assert.Contains("Invalid API key", result.Details);
    }

    [Fact]
    public async Task OpenAiValidator_ValidApiKey_ReturnsSuccess()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"choices\":[{\"message\":{\"content\":\"Hi\"}}]}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new OpenAiValidator(NullLogger<OpenAiValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync("valid-key", null, CancellationToken.None);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal("OpenAI", result.Name);
        Assert.Contains("Connected successfully", result.Details);
    }

    [Fact]
    public async Task OpenAiValidator_Timeout_ReturnsFailure()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new OpenAiValidator(NullLogger<OpenAiValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync("valid-key", null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("OpenAI", result.Name);
        Assert.Contains("timed out", result.Details);
    }

    [Fact]
    public async Task OllamaValidator_NotRunning_ReturnsFailure()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new OllamaValidator(NullLogger<OllamaValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync(null, "http://127.0.0.1:11434", CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("Ollama", result.Name);
        Assert.Contains("Cannot connect", result.Details);
    }

    [Fact]
    public async Task OllamaValidator_NoModels_ReturnsFailure()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"models\":[]}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new OllamaValidator(NullLogger<OllamaValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync(null, "http://127.0.0.1:11434", CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("Ollama", result.Name);
        Assert.Contains("No models installed", result.Details);
    }

    [Fact]
    public async Task StableDiffusionValidator_NotRunning_ReturnsFailure()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new StableDiffusionValidator(NullLogger<StableDiffusionValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync(null, "http://127.0.0.1:7860", CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("StableDiffusion", result.Name);
        Assert.Contains("Cannot connect", result.Details);
    }

    [Fact]
    public async Task StableDiffusionValidator_NoModels_ReturnsFailure()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new StableDiffusionValidator(NullLogger<StableDiffusionValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync(null, "http://127.0.0.1:7860", CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("StableDiffusion", result.Name);
        Assert.Contains("No models installed", result.Details);
    }

    [Fact]
    public async Task ElevenLabsValidator_NoApiKey_ReturnsFailure()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new ElevenLabsValidator(NullLogger<ElevenLabsValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync(null, null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("not configured", result.Details);
    }

    [Fact]
    public async Task ElevenLabsValidator_InvalidApiKey_ReturnsFailure()
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
                StatusCode = HttpStatusCode.Unauthorized
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(NullLogger<ElevenLabsValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync("invalid-key", null, CancellationToken.None);

        // Assert
        Assert.False(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("Invalid API key", result.Details);
    }

    [Fact]
    public async Task ElevenLabsValidator_ValidApiKey_ReturnsSuccess()
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
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"voices\":[]}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new ElevenLabsValidator(NullLogger<ElevenLabsValidator>.Instance, httpClient);

        // Act
        var result = await validator.ValidateAsync("valid-key", null, CancellationToken.None);

        // Assert
        Assert.True(result.Ok);
        Assert.Equal("ElevenLabs", result.Name);
        Assert.Contains("Connected successfully", result.Details);
    }
}
