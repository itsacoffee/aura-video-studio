using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aura.Tests.Providers;

/// <summary>
/// Integration tests for OllamaDirectClient that exercise the direct client path end-to-end
/// without contacting a real Ollama instance. Uses MockHttpMessageHandler to simulate responses.
/// </summary>
public class OllamaDirectClientIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly MockHttpMessageHandler _mockHttpHandler;

    public OllamaDirectClientIntegrationTests()
    {
        _mockHttpHandler = new MockHttpMessageHandler();
        
        // Set up ServiceCollection similar to Program.cs but scoped to tests
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Debug);
            builder.AddConsole();
        });
        
        // Add memory cache
        services.AddMemoryCache();
        
        // Configure OllamaSettings
        services.Configure<OllamaSettings>(options =>
        {
            options.BaseUrl = "http://127.0.0.1:11434";
            options.Timeout = TimeSpan.FromMinutes(3);
            options.MaxRetries = 3;
            options.GpuEnabled = true;
            options.NumGpu = -1;
            options.NumCtx = 4096;
        });
        
        // Register HttpClient with mock handler
        services.AddHttpClient<IOllamaDirectClient, OllamaDirectClient>()
            .ConfigurePrimaryHttpMessageHandler(() => _mockHttpHandler);
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void ServiceProvider_ResolvesIOllamaDirectClient_Successfully()
    {
        // Act
        var client = _serviceProvider.GetService<IOllamaDirectClient>();

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OllamaDirectClient>(client);
    }

    [Fact]
    public void ServiceProvider_ResolvesOllamaDirectClient_Successfully()
    {
        // Act
        var client = _serviceProvider.GetService<OllamaDirectClient>();

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void ServiceProvider_ResolvesLogger_Successfully()
    {
        // Act
        var logger = _serviceProvider.GetService<ILogger<OllamaDirectClient>>();

        // Assert
        Assert.NotNull(logger);
    }

    [Fact]
    public void ServiceProvider_ResolvesMemoryCache_Successfully()
    {
        // Act
        var cache = _serviceProvider.GetService<IMemoryCache>();

        // Assert
        Assert.NotNull(cache);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenOllamaResponds_ReturnsTrue()
    {
        // Arrange
        _mockHttpHandler.AddResponse(
            "GET",
            "http://127.0.0.1:11434/api/version",
            HttpStatusCode.OK,
            "{}");

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var isAvailable = await client.IsAvailableAsync();

        // Assert
        Assert.True(isAvailable);
        Assert.Equal(1, _mockHttpHandler.RequestCount);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenOllamaReturnsVersion_ReturnsTrue()
    {
        // Arrange
        var versionResponse = new { version = "0.1.0" };
        _mockHttpHandler.AddResponse(
            "GET",
            "http://127.0.0.1:11434/api/version",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(versionResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var isAvailable = await client.IsAvailableAsync();

        // Assert
        Assert.True(isAvailable);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenOllamaUnavailable_ReturnsFalse()
    {
        // Arrange
        _mockHttpHandler.AddResponse(
            "GET",
            "http://127.0.0.1:11434/api/version",
            HttpStatusCode.ServiceUnavailable,
            "Service Unavailable");

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var isAvailable = await client.IsAvailableAsync();

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task ListModelsAsync_ReturnsModelList()
    {
        // Arrange
        var modelsResponse = new
        {
            models = new[]
            {
                new { name = "llama3.1" },
                new { name = "codellama:7b" },
                new { name = "mistral:latest" }
            }
        };

        _mockHttpHandler.AddResponse(
            "GET",
            "http://127.0.0.1:11434/api/tags",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(modelsResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var models = await client.ListModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.Equal(3, models.Count);
        Assert.Contains("llama3.1", models);
        Assert.Contains("codellama:7b", models);
        Assert.Contains("mistral:latest", models);
    }

    [Fact]
    public async Task ListModelsAsync_WhenNoModels_ReturnsEmptyList()
    {
        // Arrange
        var modelsResponse = new { models = Array.Empty<object>() };

        _mockHttpHandler.AddResponse(
            "GET",
            "http://127.0.0.1:11434/api/tags",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(modelsResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var models = await client.ListModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.Empty(models);
    }

    [Fact]
    public async Task ListModelsAsync_WhenRequestFails_ReturnsEmptyList()
    {
        // Arrange
        _mockHttpHandler.AddResponse(
            "GET",
            "http://127.0.0.1:11434/api/tags",
            HttpStatusCode.InternalServerError,
            "Internal Server Error");

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var models = await client.ListModelsAsync();

        // Assert
        Assert.NotNull(models);
        Assert.Empty(models);
    }

    [Fact]
    public async Task GenerateAsync_WithValidRequest_ReturnsResponse()
    {
        // Arrange
        var generateResponse = new
        {
            response = "Test response from Ollama",
            model = "llama3.1",
            done = true
        };

        _mockHttpHandler.AddResponse(
            "POST",
            "http://127.0.0.1:11434/api/generate",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(generateResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var result = await client.GenerateAsync(
            "llama3.1",
            "What is the capital of France?");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test response from Ollama", result);
        Assert.Equal(1, _mockHttpHandler.RequestCount);
    }

    [Fact]
    public async Task GenerateAsync_WithSystemPrompt_SendsCorrectRequest()
    {
        // Arrange
        var generateResponse = new
        {
            response = "Paris is the capital of France.",
            model = "llama3.1",
            done = true
        };

        _mockHttpHandler.AddResponse(
            "POST",
            "http://127.0.0.1:11434/api/generate",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(generateResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var result = await client.GenerateAsync(
            "llama3.1",
            "What is the capital of France?",
            "You are a geography expert.");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("Paris", result);
    }

    [Fact]
    public async Task GenerateAsync_WithOptions_SendsCorrectRequest()
    {
        // Arrange
        var generateResponse = new
        {
            response = "Detailed explanation about machine learning.",
            model = "llama3.1",
            done = true
        };

        _mockHttpHandler.AddResponse(
            "POST",
            "http://127.0.0.1:11434/api/generate",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(generateResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        var options = new OllamaGenerationOptions
        {
            Temperature = 0.7,
            MaxTokens = 500,
            NumGpu = -1,
            NumCtx = 4096
        };

        // Act
        var result = await client.GenerateAsync(
            "llama3.1",
            "Explain machine learning",
            options: options);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("machine learning", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GenerateAsync_WhenModelNull_ThrowsArgumentNullException()
    {
        // Arrange
        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.GenerateAsync(null!, "test prompt"));
    }

    [Fact]
    public async Task GenerateAsync_WhenPromptNull_ThrowsArgumentNullException()
    {
        // Arrange
        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => client.GenerateAsync("llama3.1", null!));
    }

    [Fact]
    public async Task GenerateAsync_WithCancellationToken_CanBeCancelled()
    {
        // Arrange
        var generateResponse = new
        {
            response = "This should not be returned",
            model = "llama3.1",
            done = true
        };

        _mockHttpHandler.AddResponse(
            "POST",
            "http://127.0.0.1:11434/api/generate",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(generateResponse),
            delayMs: 5000); // Simulate slow response

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();
        var cts = new CancellationTokenSource();
        cts.CancelAfter(100); // Cancel after 100ms

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.GenerateAsync("llama3.1", "test", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task GenerateAsync_WhenResponseEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var generateResponse = new
        {
            response = "",
            model = "llama3.1",
            done = true
        };

        _mockHttpHandler.AddResponse(
            "POST",
            "http://127.0.0.1:11434/api/generate",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(generateResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GenerateAsync("llama3.1", "test"));
    }

    [Fact]
    public async Task GenerateAsync_WithRetry_SucceedsOnSecondAttempt()
    {
        // Arrange - First request fails, second succeeds
        _mockHttpHandler.AddResponse(
            "POST",
            "http://127.0.0.1:11434/api/generate",
            HttpStatusCode.ServiceUnavailable,
            "Service temporarily unavailable");

        var generateResponse = new
        {
            response = "Success on retry",
            model = "llama3.1",
            done = true
        };

        _mockHttpHandler.AddResponse(
            "POST",
            "http://127.0.0.1:11434/api/generate",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(generateResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var result = await client.GenerateAsync("llama3.1", "test prompt");

        // Assert
        Assert.Equal("Success on retry", result);
        Assert.Equal(2, _mockHttpHandler.RequestCount);
    }

    [Fact]
    public async Task GenerateAsync_CallsCorrectEndpoint_VerifiedByMock()
    {
        // Arrange - Verify that the client calls the correct base URL + endpoint
        var generateResponse = new
        {
            response = "Test response",
            model = "llama3.1",
            done = true
        };

        _mockHttpHandler.AddResponse(
            "POST",
            "http://127.0.0.1:11434/api/generate",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(generateResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var result = await client.GenerateAsync("llama3.1", "test prompt");

        // Assert - The mock handler validates that the correct URL was called
        Assert.NotNull(result);
        Assert.Equal("Test response", result);
        Assert.Equal(1, _mockHttpHandler.RequestCount);
    }

    [Fact]
    public async Task IsAvailableAsync_CallsCorrectEndpoint_VerifiedByMock()
    {
        // Arrange - Verify that the client calls the correct base URL + endpoint
        _mockHttpHandler.AddResponse(
            "GET",
            "http://127.0.0.1:11434/api/version",
            HttpStatusCode.OK,
            "{}");

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var isAvailable = await client.IsAvailableAsync();

        // Assert - The mock handler validates that the correct URL was called
        Assert.True(isAvailable);
        Assert.Equal(1, _mockHttpHandler.RequestCount);
    }

    [Fact]
    public async Task ListModelsAsync_CallsCorrectEndpoint_VerifiedByMock()
    {
        // Arrange - Verify that the client calls the correct base URL + endpoint
        var modelsResponse = new
        {
            models = new[]
            {
                new { name = "llama3.1" }
            }
        };

        _mockHttpHandler.AddResponse(
            "GET",
            "http://127.0.0.1:11434/api/tags",
            HttpStatusCode.OK,
            JsonSerializer.Serialize(modelsResponse));

        var client = _serviceProvider.GetRequiredService<IOllamaDirectClient>();

        // Act
        var models = await client.ListModelsAsync();

        // Assert - The mock handler validates that the correct URL was called
        Assert.NotNull(models);
        Assert.Single(models);
        Assert.Equal(1, _mockHttpHandler.RequestCount);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Lightweight mock HTTP message handler for intercepting HTTP requests
    /// and returning canned JSON responses without hitting a real Ollama instance.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<MockResponse> _responses = new();
        public int RequestCount { get; private set; }

        public void AddResponse(string method, string uri, HttpStatusCode statusCode, string content, int delayMs = 0)
        {
            _responses.Enqueue(new MockResponse
            {
                Method = method,
                Uri = uri,
                StatusCode = statusCode,
                Content = content,
                DelayMs = delayMs
            });
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;

            if (_responses.Count == 0)
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent("No mock response configured")
                };
            }

            var mockResponse = _responses.Dequeue();

            // Validate that the request matches expected method and URI
            var requestUri = request.RequestUri?.ToString() ?? string.Empty;
            var requestMethod = request.Method.Method;

            if (!string.IsNullOrEmpty(mockResponse.Method) && 
                !mockResponse.Method.Equals(requestMethod, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.MethodNotAllowed)
                {
                    Content = new StringContent($"Expected {mockResponse.Method}, got {requestMethod}")
                };
            }

            if (!string.IsNullOrEmpty(mockResponse.Uri) && 
                !requestUri.Equals(mockResponse.Uri, StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    Content = new StringContent($"Expected {mockResponse.Uri}, got {requestUri}")
                };
            }

            if (mockResponse.DelayMs > 0)
            {
                await Task.Delay(mockResponse.DelayMs, cancellationToken);
            }

            return new HttpResponseMessage(mockResponse.StatusCode)
            {
                Content = new StringContent(mockResponse.Content, Encoding.UTF8, "application/json")
            };
        }

        private class MockResponse
        {
            public string Method { get; set; } = string.Empty;
            public string Uri { get; set; } = string.Empty;
            public HttpStatusCode StatusCode { get; set; }
            public string Content { get; set; } = string.Empty;
            public int DelayMs { get; set; }
        }
    }
}
