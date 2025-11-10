using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Providers;
using Aura.Core.Services.AI;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests.Providers;

/// <summary>
/// Comprehensive tests for OllamaLlmProvider
/// </summary>
public class OllamaLlmProviderTests : IDisposable
{
    private readonly Mock<ILogger<OllamaLlmProvider>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly OllamaLlmProvider _provider;
    private const string BaseUrl = "http://127.0.0.1:11434";
    private const string Model = "llama3.1:8b-q4_k_m";

    public OllamaLlmProviderTests()
    {
        _loggerMock = new Mock<ILogger<OllamaLlmProvider>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        var promptService = new PromptCustomizationService(
            Mock.Of<ILogger<PromptCustomizationService>>());
        
        _provider = new OllamaLlmProvider(
            _loggerMock.Object,
            _httpClient,
            BaseUrl,
            Model,
            maxRetries: 2,
            timeoutSeconds: 30,
            promptService);
    }

    [Fact]
    public async Task DraftScriptAsync_WithValidResponse_ReturnsScript()
    {
        // Arrange
        var brief = new Brief
        {
            Topic = "Test Topic",
            Description = "Test Description",
            TargetAudience = "General",
            Tone = "Professional"
        };
        
        var spec = new PlanSpec
        {
            TargetDuration = TimeSpan.FromSeconds(30),
            Style = "Educational",
            Pacing = "Medium"
        };

        var ollamaResponse = new
        {
            response = "This is a test script about the topic.",
            done = true
        };

        SetupHttpResponse(
            $"{BaseUrl}/api/generate",
            JsonSerializer.Serialize(ollamaResponse));

        // Act
        var result = await _provider.DraftScriptAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains("test script", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DraftScriptAsync_WithConnectionError_RetriesAndThrows()
    {
        // Arrange
        var brief = new Brief
        {
            Topic = "Test Topic",
            Description = "Test Description"
        };
        
        var spec = new PlanSpec
        {
            TargetDuration = TimeSpan.FromSeconds(30)
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _provider.DraftScriptAsync(brief, spec, CancellationToken.None));
        
        Assert.Contains("Cannot connect to Ollama", exception.Message);
    }

    [Fact]
    public async Task CompleteAsync_WithValidPrompt_ReturnsCompletion()
    {
        // Arrange
        var prompt = "Complete this sentence: Hello";
        var ollamaResponse = new
        {
            response = "Hello world!",
            done = true
        };

        SetupHttpResponse(
            $"{BaseUrl}/api/generate",
            JsonSerializer.Serialize(ollamaResponse));

        // Act
        var result = await _provider.CompleteAsync(prompt, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task IsServiceAvailableAsync_WhenServiceRunning_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse($"{BaseUrl}/api/version", "{\"version\":\"0.1.0\"}");

        // Act
        var result = await _provider.IsServiceAvailableAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsServiceAvailableAsync_WhenServiceNotRunning_ReturnsFalse()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/version")),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var result = await _provider.IsServiceAvailableAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsModelList()
    {
        // Arrange
        var modelsResponse = new
        {
            models = new[]
            {
                new
                {
                    name = "llama3.1:8b-q4_k_m",
                    size = 4661224384L,
                    modified_at = "2024-11-10T10:00:00Z"
                },
                new
                {
                    name = "codellama:7b",
                    size = 3825819519L,
                    modified_at = "2024-11-09T15:30:00Z"
                }
            }
        };

        SetupHttpResponse(
            $"{BaseUrl}/api/tags",
            JsonSerializer.Serialize(modelsResponse));

        // Act
        var result = await _provider.GetAvailableModelsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.Name == "llama3.1:8b-q4_k_m");
        Assert.Contains(result, m => m.Name == "codellama:7b");
    }

    [Fact]
    public async Task AnalyzeSceneImportanceAsync_ReturnsAnalysis()
    {
        // Arrange
        var sceneText = "The sun rises over the mountains.";
        var videoGoal = "Create an inspiring nature documentary";

        var ollamaResponse = new
        {
            response = @"{
                ""importance"": 75,
                ""complexity"": 40,
                ""emotionalIntensity"": 60,
                ""informationDensity"": ""medium"",
                ""optimalDurationSeconds"": 8,
                ""transitionType"": ""fade"",
                ""reasoning"": ""Opening scene sets the mood""
            }",
            done = true
        };

        SetupHttpResponse(
            $"{BaseUrl}/api/generate",
            JsonSerializer.Serialize(ollamaResponse));

        // Act
        var result = await _provider.AnalyzeSceneImportanceAsync(
            sceneText,
            null,
            videoGoal,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(75, result.Importance);
        Assert.Equal(40, result.Complexity);
        Assert.Equal("medium", result.InformationDensity);
    }

    [Fact]
    public async Task GenerateVisualPromptAsync_ReturnsVisualPrompt()
    {
        // Arrange
        var sceneText = "A bustling city street at night";
        var videoTone = "cinematic";
        var targetStyle = VisualStyle.Photorealistic;

        var ollamaResponse = new
        {
            response = @"{
                ""detailedDescription"": ""Neon-lit city street with cars and pedestrians"",
                ""compositionGuidelines"": ""Rule of thirds, leading lines"",
                ""lightingMood"": ""dramatic"",
                ""lightingDirection"": ""side"",
                ""lightingQuality"": ""hard"",
                ""timeOfDay"": ""night"",
                ""colorPalette"": [""#FF0080"", ""#00FFFF"", ""#1A1A1A""],
                ""shotType"": ""wide shot"",
                ""cameraAngle"": ""eye level"",
                ""depthOfField"": ""medium"",
                ""styleKeywords"": [""cyberpunk"", ""neon"", ""urban"", ""cinematic"", ""moody""],
                ""negativeElements"": [""daytime"", ""bright"", ""washed out""],
                ""continuityElements"": [""urban setting"", ""night time""],
                ""reasoning"": ""Captures the vibrant energy of city nightlife""
            }",
            done = true
        };

        SetupHttpResponse(
            $"{BaseUrl}/api/generate",
            JsonSerializer.Serialize(ollamaResponse));

        // Act
        var result = await _provider.GenerateVisualPromptAsync(
            sceneText,
            null,
            videoTone,
            targetStyle,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("city street", result.DetailedDescription, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("dramatic", result.LightingMood);
        Assert.Contains("cyberpunk", result.StyleKeywords);
    }

    [Fact]
    public async Task AnalyzeSceneCoherenceAsync_ReturnsCoherenceAnalysis()
    {
        // Arrange
        var fromScene = "The detective enters the room";
        var toScene = "He notices the broken window";
        var videoGoal = "Mystery thriller";

        var ollamaResponse = new
        {
            response = @"{
                ""coherenceScore"": 85,
                ""connectionTypes"": [""causal"", ""sequential""],
                ""confidenceScore"": 0.9,
                ""reasoning"": ""Natural progression of investigation""
            }",
            done = true
        };

        SetupHttpResponse(
            $"{BaseUrl}/api/generate",
            JsonSerializer.Serialize(ollamaResponse));

        // Act
        var result = await _provider.AnalyzeSceneCoherenceAsync(
            fromScene,
            toScene,
            videoGoal,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(85, result.CoherenceScore);
        Assert.Contains("causal", result.ConnectionTypes);
        Assert.Equal(0.9, result.ConfidenceScore);
    }

    [Fact]
    public async Task GenerateScriptAsync_WithValidInput_ReturnsScript()
    {
        // Arrange
        var brief = new Brief
        {
            Topic = "Machine Learning Basics",
            Description = "Introduction to ML concepts",
            TargetAudience = "Beginners"
        };

        var spec = new PlanSpec
        {
            TargetDuration = TimeSpan.FromSeconds(60),
            Style = "Educational"
        };

        var ollamaResponse = new
        {
            response = @"{
                ""title"": ""Machine Learning Basics"",
                ""scenes"": [
                    {""narration"": ""Welcome to Machine Learning"", ""duration"": 5},
                    {""narration"": ""ML is a subset of AI"", ""duration"": 10}
                ]
            }",
            done = true
        };

        SetupHttpResponse(
            $"{BaseUrl}/api/generate",
            JsonSerializer.Serialize(ollamaResponse));

        // Act
        var result = await _provider.GenerateScriptAsync(brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Machine Learning Basics", result.Title);
        Assert.NotEmpty(result.Scenes);
        Assert.Equal("Ollama", result.Metadata.ProviderName);
        Assert.Equal(Model, result.Metadata.ModelUsed);
    }

    [Fact]
    public async Task DraftScriptAsync_WithTimeout_ThrowsOperationException()
    {
        // Arrange
        var brief = new Brief { Topic = "Test" };
        var spec = new PlanSpec { TargetDuration = TimeSpan.FromSeconds(30) };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Timeout"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _provider.DraftScriptAsync(brief, spec, CancellationToken.None));
        
        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_WithConnectionError_ReturnsEmptyList()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        var result = await _provider.GetAvailableModelsAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DraftScriptAsync_WithInvalidResponse_ThrowsException(string? response)
    {
        // Arrange
        var brief = new Brief { Topic = "Test" };
        var spec = new PlanSpec { TargetDuration = TimeSpan.FromSeconds(30) };

        var ollamaResponse = new { response = response, done = true };
        SetupHttpResponse(
            $"{BaseUrl}/api/generate",
            JsonSerializer.Serialize(ollamaResponse));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _provider.DraftScriptAsync(brief, spec, CancellationToken.None));
    }

    private void SetupHttpResponse(string requestUri, string responseContent)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == requestUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
