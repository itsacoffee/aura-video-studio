using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for LLM provider API key validation and error handling
/// </summary>
public class LlmProviderValidationTests
{
    // Test data constants
    private const string ValidOpenAiResponse = "{\"choices\":[{\"message\":{\"content\":\"Test response\"}}]}";
    private const string ValidOpenAiApiKey = "sk-1234567890abcdefghijklmnopqrstuvwxyz1234567890";
    private const string ValidAzureApiKey = "12345678901234567890123456789012";
    private const string ValidAzureEndpoint = "https://myresource.openai.azure.com";
    private const string ValidGeminiApiKey = "AIzaSyABCDEFGH1234567890IJKLMNOPQRSTUVWXYZ";

    private readonly Brief _testBrief = new Brief(
        Topic: "Test Topic",
        Audience: "General",
        Goal: "Educational",
        Tone: "Neutral",
        Language: "en-US",
        Aspect: Aspect.Widescreen16x9
    );

    private readonly PlanSpec _testSpec = new PlanSpec(
        TargetDuration: TimeSpan.FromMinutes(1),
        Pacing: Pacing.Conversational,
        Density: Density.Balanced,
        Style: "Educational"
    );

    #region OpenAI Provider Tests

    [Fact]
    public void OpenAiProvider_Should_ThrowOnNullApiKey()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new OpenAiLlmProvider(
                NullLogger<OpenAiLlmProvider>.Instance,
                new HttpClient(),
                null!));

        Assert.Contains("not configured", exception.Message);
        Assert.Contains("OpenAI", exception.Message);
    }

    [Fact]
    public void OpenAiProvider_Should_ThrowOnEmptyApiKey()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new OpenAiLlmProvider(
                NullLogger<OpenAiLlmProvider>.Instance,
                new HttpClient(),
                ""));

        Assert.Contains("not configured", exception.Message);
    }

    [Fact]
    public void OpenAiProvider_Should_ThrowOnInvalidApiKeyFormat()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new OpenAiLlmProvider(
                NullLogger<OpenAiLlmProvider>.Instance,
                new HttpClient(),
                "invalid-key-format"));

        Assert.Contains("invalid", exception.Message);
        Assert.Contains("OpenAI", exception.Message);
    }

    [Fact]
    public void OpenAiProvider_Should_AcceptValidApiKeyFormat()
    {
        // Arrange & Act
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            new HttpClient(),
            ValidOpenAiApiKey);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task OpenAiProvider_Should_RetryOnNetworkError()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        int callCount = 0;
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    throw new HttpRequestException("Network error");
                }
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(ValidOpenAiResponse)
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidOpenAiApiKey,
            maxRetries: 2);

        // Act
        var result = await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(2, callCount); // Should have retried once
    }

    [Fact]
    public async Task OpenAiProvider_Should_ProvideHelpfulErrorOnUnauthorized()
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
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidOpenAiApiKey);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None));

        Assert.Contains("invalid", exception.Message.ToLower());
        Assert.Contains("Settings", exception.Message);
    }

    [Fact]
    public async Task OpenAiProvider_Should_ProvideHelpfulErrorOnRateLimit()
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
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("{\"error\":{\"message\":\"Rate limit exceeded\"}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidOpenAiApiKey,
            maxRetries: 0); // No retries for this test

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None));

        Assert.Contains("rate limit", exception.Message.ToLower());
    }

    #endregion

    #region Azure OpenAI Provider Tests

    [Fact]
    public void AzureProvider_Should_ThrowOnNullApiKey()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new AzureOpenAiLlmProvider(
                NullLogger<AzureOpenAiLlmProvider>.Instance,
                new HttpClient(),
                null!,
                ValidAzureEndpoint));

        Assert.Contains("not configured", exception.Message);
        Assert.Contains("Azure", exception.Message);
    }

    [Fact]
    public void AzureProvider_Should_ThrowOnNullEndpoint()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new AzureOpenAiLlmProvider(
                NullLogger<AzureOpenAiLlmProvider>.Instance,
                new HttpClient(),
                ValidAzureApiKey,
                null!));

        Assert.Contains("not configured", exception.Message);
    }

    [Fact]
    public void AzureProvider_Should_ThrowOnNonHttpsEndpoint()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new AzureOpenAiLlmProvider(
                NullLogger<AzureOpenAiLlmProvider>.Instance,
                new HttpClient(),
                ValidAzureApiKey,
                "http://test.openai.azure.com"));

        Assert.Contains("HTTPS", exception.Message);
    }

    [Fact]
    public void AzureProvider_Should_ThrowOnInvalidEndpointFormat()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new AzureOpenAiLlmProvider(
                NullLogger<AzureOpenAiLlmProvider>.Instance,
                new HttpClient(),
                ValidAzureApiKey,
                "https://invalid-endpoint.com"));

        Assert.Contains("format", exception.Message.ToLower());
        Assert.Contains("openai.azure.com", exception.Message);
    }

    [Fact]
    public void AzureProvider_Should_AcceptValidConfiguration()
    {
        // Arrange & Act
        var provider = new AzureOpenAiLlmProvider(
            NullLogger<AzureOpenAiLlmProvider>.Instance,
            new HttpClient(),
            ValidAzureApiKey,
            ValidAzureEndpoint);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task AzureProvider_Should_ProvideHelpfulErrorOnDeploymentNotFound()
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
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("{\"error\":{\"message\":\"Deployment not found\"}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new AzureOpenAiLlmProvider(
            NullLogger<AzureOpenAiLlmProvider>.Instance,
            httpClient,
            ValidAzureApiKey,
            ValidAzureEndpoint,
            deploymentName: "nonexistent");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None));

        Assert.Contains("deployment", exception.Message.ToLower());
        Assert.Contains("nonexistent", exception.Message);
    }

    #endregion

    #region Gemini Provider Tests

    [Fact]
    public void GeminiProvider_Should_ThrowOnNullApiKey()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new GeminiLlmProvider(
                NullLogger<GeminiLlmProvider>.Instance,
                new HttpClient(),
                null!));

        Assert.Contains("not configured", exception.Message);
        Assert.Contains("Gemini", exception.Message);
    }

    [Fact]
    public void GeminiProvider_Should_ThrowOnEmptyApiKey()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new GeminiLlmProvider(
                NullLogger<GeminiLlmProvider>.Instance,
                new HttpClient(),
                ""));

        Assert.Contains("not configured", exception.Message);
    }

    [Fact]
    public void GeminiProvider_Should_ThrowOnTooShortApiKey()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            new GeminiLlmProvider(
                NullLogger<GeminiLlmProvider>.Instance,
                new HttpClient(),
                "shortkey"));

        Assert.Contains("invalid", exception.Message.ToLower());
    }

    [Fact]
    public void GeminiProvider_Should_AcceptValidApiKey()
    {
        // Arrange & Act
        var provider = new GeminiLlmProvider(
            NullLogger<GeminiLlmProvider>.Instance,
            new HttpClient(),
            ValidGeminiApiKey);

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task GeminiProvider_Should_ProvideHelpfulErrorOnQuotaExceeded()
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
                StatusCode = HttpStatusCode.TooManyRequests,
                Content = new StringContent("{\"error\":{\"message\":\"Quota exceeded\"}}")
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new GeminiLlmProvider(
            NullLogger<GeminiLlmProvider>.Instance,
            httpClient,
            ValidGeminiApiKey,
            maxRetries: 0);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None));

        Assert.Contains("quota", exception.Message.ToLower());
        Assert.Contains("makersuite", exception.Message.ToLower());
    }

    #endregion

    #region Ollama Provider Tests

    [Fact]
    public void OllamaProvider_Should_NotThrowOnInitialization()
    {
        // Arrange & Act
        var provider = new OllamaLlmProvider(
            NullLogger<OllamaLlmProvider>.Instance,
            new HttpClient(),
            baseUrl: "http://127.0.0.1:11434");

        // Assert
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task OllamaProvider_Should_ProvideHelpfulErrorWhenNotRunning()
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
        var provider = new OllamaLlmProvider(
            NullLogger<OllamaLlmProvider>.Instance,
            httpClient,
            baseUrl: "http://127.0.0.1:11434",
            maxRetries: 0);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None));

        Assert.Contains("Cannot connect", exception.Message);
        Assert.Contains("ollama serve", exception.Message.ToLower());
    }

    #endregion

    #region Timeout Tests

    [Fact]
    public async Task OpenAiProvider_Should_RespectTimeout()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage req, CancellationToken ct) =>
            {
                await Task.Delay(10000, ct); // Simulate slow response
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(ValidOpenAiResponse)
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var provider = new OpenAiLlmProvider(
            NullLogger<OpenAiLlmProvider>.Instance,
            httpClient,
            ValidOpenAiApiKey,
            maxRetries: 0,
            timeoutSeconds: 1); // 1 second timeout

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None));

        Assert.Contains("timed out", exception.Message.ToLower());
    }

    #endregion
}
