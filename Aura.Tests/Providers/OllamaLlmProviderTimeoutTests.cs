using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.AI;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests.Providers;

/// <summary>
/// Unit tests for OllamaLlmProvider timeout and empty response handling.
/// These tests verify that the provider correctly handles:
/// - Empty responses from Ollama API
/// - Timeout scenarios with proper cancellation token usage
/// - Response reading with consistent cancellation tokens
/// </summary>
public class OllamaLlmProviderTimeoutTests
{
    private readonly Brief _testBrief = new Brief(
        Topic: "Test Topic",
        Audience: "General",
        Goal: "Inform",
        Tone: "Professional",
        Language: "en-US",
        Aspect: Aspect.Widescreen16x9
    );

    private readonly PlanSpec _testSpec = new PlanSpec(
        TargetDuration: TimeSpan.FromMinutes(2),
        Pacing: Pacing.Conversational,
        Density: Density.Balanced,
        Style: "Standard"
    );

    [Fact]
    public async Task DraftScriptAsync_EmptyResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"response\": \"\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 0,
            timeoutSeconds: 30
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DraftScriptAsync_WhitespaceOnlyResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"response\": \"   \\n\\t  \"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 0,
            timeoutSeconds: 30
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DraftScriptAsync_ValidResponse_ReturnsScript()
    {
        // Arrange
        var expectedScript = "## Introduction\nThis is a test script about Test Topic.\n## Conclusion\nThank you for watching.";
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent($"{{\"response\": \"{expectedScript.Replace("\n", "\\n")}\"}}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 0,
            timeoutSeconds: 30
        );

        // Act
        var result = await provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("Introduction", result);
    }

    [Fact]
    public async Task DraftScriptAsync_MissingResponseField_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"done\": true}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 0,
            timeoutSeconds: 30
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None)
        );
    }

    [Fact]
    public async Task DraftScriptAsync_ApiError_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"error\": \"Model not found\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 0,
            timeoutSeconds: 30
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None)
        );
    }

    [Fact]
    public async Task CompleteAsync_EmptyResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"response\": \"\"}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 0,
            timeoutSeconds: 30
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.CompleteAsync("Generate a response", CancellationToken.None)
        );
    }

    [Fact]
    public async Task CompleteAsync_ValidResponse_ReturnsCompletion()
    {
        // Arrange
        var expectedResponse = "This is a valid completion response.";
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent($"{{\"response\": \"{expectedResponse}\"}}", Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 0,
            timeoutSeconds: 30
        );

        // Act
        var result = await provider.CompleteAsync("Generate a response", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task DraftScriptAsync_Timeout_ThrowsInvalidOperationExceptionWithTimeoutMessage()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 0,
            timeoutSeconds: 1 // Short timeout for test
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None)
        );
        
        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DraftScriptAsync_ExternalCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns<HttpRequestMessage, CancellationToken>(async (request, ct) =>
            {
                // Simulate slow response
                await Task.Delay(5000, ct);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"response\": \"test\"}")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 0,
            timeoutSeconds: 300
        );

        // Create a cancellation token that cancels quickly
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => provider.DraftScriptAsync(_testBrief, _testSpec, cts.Token)
        );
    }

    [Fact]
    public async Task DraftScriptAsync_RetryOnFailure_RetriesCorrectNumberOfTimes()
    {
        // Arrange
        var attemptCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Callback(() => attemptCount++)
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(20)
        };

        var logger = NullLogger<OllamaLlmProvider>.Instance;
        var provider = new OllamaLlmProvider(
            logger,
            httpClient,
            "http://127.0.0.1:11434",
            "test-model",
            maxRetries: 2,
            timeoutSeconds: 30
        );

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.DraftScriptAsync(_testBrief, _testSpec, CancellationToken.None)
        );

        // Should have attempted 3 times (initial + 2 retries)
        Assert.Equal(3, attemptCount);
    }
}
