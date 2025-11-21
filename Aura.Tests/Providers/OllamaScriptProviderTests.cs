using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Interfaces;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests.Providers;

/// <summary>
/// Tests for OllamaScriptProvider with streaming support
/// </summary>
public class OllamaScriptProviderTests : IDisposable
{
    private readonly Mock<ILogger<OllamaScriptProvider>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly OllamaScriptProvider _provider;
    private const string BaseUrl = "http://127.0.0.1:11434";
    private const string Model = "llama3.1:8b-q4_k_m";

    public OllamaScriptProviderTests()
    {
        _loggerMock = new Mock<ILogger<OllamaScriptProvider>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        _provider = new OllamaScriptProvider(
            _loggerMock.Object,
            _httpClient,
            BaseUrl,
            Model,
            maxRetries: 2,
            timeoutSeconds: 30);
    }

    [Fact]
    public async Task GenerateScriptAsync_WithValidResponse_ReturnsScript()
    {
        // Arrange
        var request = CreateTestRequest();

        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
        };

        var generateResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    response = "Scene 1: Introduction to the topic.\nScene 2: Key points explained.\nScene 3: Conclusion.",
                    done = true
                }),
                Encoding.UTF8,
                "application/json")
        };

        SetupHttpResponseSequence(versionResponse, generateResponse);

        // Act
        var result = await _provider.GenerateScriptAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Topic", result.Title);
        Assert.NotEmpty(result.Scenes);
        Assert.Equal("Ollama", result.Metadata.ProviderName);
        Assert.Equal(Model, result.Metadata.ModelUsed);
        Assert.Equal(ProviderTier.Free, result.Metadata.Tier);
    }

    [Fact]
    public async Task StreamGenerateAsync_WithValidResponse_YieldsProgressUpdates()
    {
        // Arrange
        var request = CreateTestRequest();

        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
        };

        var streamResponse = CreateStreamingResponse();
        SetupHttpResponseSequence(versionResponse, streamResponse);

        // Act
        var progressUpdates = new List<ScriptGenerationProgress>();
        await foreach (var update in _provider.StreamGenerateAsync(request, CancellationToken.None))
        {
            progressUpdates.Add(update);
        }

        // Assert
        Assert.NotEmpty(progressUpdates);
        Assert.True(progressUpdates.Count >= 3, "Should have at least 3 progress updates");
        
        var lastUpdate = progressUpdates.Last();
        Assert.Equal(100, lastUpdate.PercentComplete);
        Assert.NotEmpty(lastUpdate.PartialScript);
        Assert.Contains("complete", lastUpdate.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StreamGenerateAsync_AccumulatesContentCorrectly()
    {
        // Arrange
        var request = CreateTestRequest();

        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
        };

        var streamResponse = CreateStreamingResponse();
        SetupHttpResponseSequence(versionResponse, streamResponse);

        // Act
        string? finalScript = null;
        await foreach (var update in _provider.StreamGenerateAsync(request, CancellationToken.None))
        {
            finalScript = update.PartialScript;
        }

        // Assert
        Assert.NotNull(finalScript);
        Assert.Contains("Hello", finalScript);
        Assert.Contains("world", finalScript);
        Assert.Contains("test", finalScript);
    }

    [Fact]
    public async Task GenerateScriptAsync_WhenServiceUnavailable_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CreateTestRequest();

        var versionResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        SetupHttpResponse(versionResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _provider.GenerateScriptAsync(request, CancellationToken.None));

        Assert.Contains("Cannot connect to Ollama", exception.Message);
    }

    [Fact]
    public async Task GenerateScriptAsync_WithModelNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = CreateTestRequest();

        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
        };

        var generateResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent(
                "{\"error\":\"model not found\"}",
                Encoding.UTF8,
                "application/json")
        };

        SetupHttpResponseSequence(versionResponse, generateResponse);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _provider.GenerateScriptAsync(request, CancellationToken.None));

        Assert.Contains("Model", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WithValidResponse_ReturnsModels()
    {
        // Arrange
        var tagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    models = new[]
                    {
                        new { name = "llama3.1:8b-q4_k_m" },
                        new { name = "mistral:7b" },
                        new { name = "codellama:13b" }
                    }
                }),
                Encoding.UTF8,
                "application/json")
        };

        SetupHttpResponse(tagsResponse);

        // Act
        var result = await _provider.GetAvailableModelsAsync(CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("llama3.1:8b-q4_k_m", result);
        Assert.Contains("mistral:7b", result);
        Assert.Contains("codellama:13b", result);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WhenServiceAvailable_ReturnsValid()
    {
        // Arrange
        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
        };

        var tagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    models = new[]
                    {
                        new { name = Model }
                    }
                }),
                Encoding.UTF8,
                "application/json")
        };

        SetupHttpResponseSequence(versionResponse, tagsResponse);

        // Act
        var result = await _provider.ValidateConfigurationAsync(CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task ValidateConfigurationAsync_WhenServiceUnavailable_ReturnsInvalid()
    {
        // Arrange
        var versionResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        SetupHttpResponse(versionResponse);

        // Act
        var result = await _provider.ValidateConfigurationAsync(CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("Cannot connect"));
    }

    [Fact]
    public void GetProviderMetadata_ReturnsCorrectMetadata()
    {
        // Act
        var metadata = _provider.GetProviderMetadata();

        // Assert
        Assert.Equal("Ollama", metadata.Name);
        Assert.Equal(ProviderTier.Free, metadata.Tier);
        Assert.False(metadata.RequiresInternet);
        Assert.False(metadata.RequiresApiKey);
        Assert.Contains("streaming", metadata.Capabilities);
        Assert.Contains("local-execution", metadata.Capabilities);
        Assert.Equal(0m, metadata.EstimatedCostPer1KTokens);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceRunning_ReturnsTrue()
    {
        // Arrange
        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
        };

        SetupHttpResponse(versionResponse);

        // Act
        var result = await _provider.IsAvailableAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsAvailableAsync_WhenServiceDown_ReturnsFalse()
    {
        // Arrange
        var versionResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        SetupHttpResponse(versionResponse);

        // Act
        var result = await _provider.IsAvailableAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    private ScriptGenerationRequest CreateTestRequest()
    {
        return new ScriptGenerationRequest
        {
            Brief = new Brief
            {
                Topic = "Test Topic",
                Description = "Test Description",
                Audience = "General",
                Goal = "Educate",
                Tone = "Professional"
            },
            PlanSpec = new PlanSpec
            {
                TargetDuration = TimeSpan.FromSeconds(30),
                Style = "Educational",
                Pacing = "Medium"
            },
            CorrelationId = Guid.NewGuid().ToString()
        };
    }

    private HttpResponseMessage CreateStreamingResponse()
    {
        var chunks = new[]
        {
            "{\"model\":\"llama3.1\",\"created_at\":\"2024-01-01T00:00:00Z\",\"response\":\"Hello \",\"done\":false}",
            "{\"model\":\"llama3.1\",\"created_at\":\"2024-01-01T00:00:01Z\",\"response\":\"world \",\"done\":false}",
            "{\"model\":\"llama3.1\",\"created_at\":\"2024-01-01T00:00:02Z\",\"response\":\"test\",\"done\":false}",
            "{\"model\":\"llama3.1\",\"created_at\":\"2024-01-01T00:00:03Z\",\"response\":\"\",\"done\":true,\"total_duration\":3000000000,\"eval_count\":3}"
        };

        var streamContent = string.Join("\n", chunks);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(streamContent, Encoding.UTF8, "application/json")
        };
    }

    private void SetupHttpResponse(HttpResponseMessage response)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    private void SetupHttpResponseSequence(params HttpResponseMessage[] responses)
    {
        var setup = _httpMessageHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        foreach (var response in responses)
        {
            setup = setup.ReturnsAsync(response);
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
