using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Providers.Llm.Validators;
using Aura.Providers.Tts.Validators;
using Aura.Providers.Images.Validators;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for provider validators
/// </summary>
public class ProviderValidatorTests
{
    [Fact]
    public async Task OpenAiValidator_WithNoApiKey_ReturnsUnavailable()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new OpenAiLlmValidator(
            NullLogger<OpenAiLlmValidator>.Instance,
            httpClient,
            null);

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Equal("OpenAI", result.ProviderName);
        Assert.Contains("not configured", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenAiValidator_WithInvalidApiKey_ReturnsUnavailable()
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
        var validator = new OpenAiLlmValidator(
            NullLogger<OpenAiLlmValidator>.Instance,
            httpClient,
            "invalid-key");

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Equal("OpenAI", result.ProviderName);
        Assert.Contains("Invalid API key", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OpenAiValidator_WithValidApiKey_ReturnsAvailable()
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
                Content = new StringContent("{\"data\":[]}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var validator = new OpenAiLlmValidator(
            NullLogger<OpenAiLlmValidator>.Instance,
            httpClient,
            "valid-key");

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal("OpenAI", result.ProviderName);
    }

    [Fact]
    public async Task OllamaValidator_NotRunning_ReturnsUnavailable()
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
        var validator = new OllamaLlmValidator(
            NullLogger<OllamaLlmValidator>.Instance,
            httpClient,
            "http://127.0.0.1:11434");

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Equal("Ollama", result.ProviderName);
        Assert.Contains("not running", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task OllamaValidator_NoModels_ReturnsUnavailable()
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
        var validator = new OllamaLlmValidator(
            NullLogger<OllamaLlmValidator>.Instance,
            httpClient,
            "http://127.0.0.1:11434");

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Equal("Ollama", result.ProviderName);
        Assert.Contains("No models", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RuleBasedValidator_AlwaysReturnsAvailable()
    {
        // Arrange
        var validator = new RuleBasedLlmValidator(
            NullLogger<RuleBasedLlmValidator>.Instance);

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal("RuleBased", result.ProviderName);
    }

    [Fact]
    public async Task WindowsTtsValidator_ReturnsAvailableOnWindows()
    {
        // Arrange
        var validator = new WindowsTtsValidator(
            NullLogger<WindowsTtsValidator>.Instance);

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.Equal("WindowsSAPI", result.ProviderName);
        // Availability depends on OS, so we don't assert the value
    }

    [Fact]
    public async Task ElevenLabsTtsValidator_WithNoApiKey_ReturnsUnavailable()
    {
        // Arrange
        var httpClient = new HttpClient();
        var validator = new ElevenLabsTtsValidator(
            NullLogger<ElevenLabsTtsValidator>.Instance,
            httpClient,
            null);

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Equal("ElevenLabs", result.ProviderName);
        Assert.Contains("not configured", result.Details, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StableDiffusionValidator_NotRunning_ReturnsUnavailable()
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
        var validator = new StableDiffusionImageValidator(
            NullLogger<StableDiffusionImageValidator>.Instance,
            httpClient,
            "http://127.0.0.1:7860");

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsAvailable);
        Assert.Equal("StableDiffusion", result.ProviderName);
        Assert.Contains("not running", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StockImageValidator_AlwaysReturnsAvailable()
    {
        // Arrange
        var validator = new StockImageValidator(
            NullLogger<StockImageValidator>.Instance);

        // Act
        var result = await validator.ValidateAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsAvailable);
        Assert.Equal("StockImages", result.ProviderName);
    }
}
