using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Interfaces;
using Aura.Core.Models;
using Aura.Core.Models.RAG;
using Aura.Core.Services.RAG;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests.Providers;

/// <summary>
/// Tests for OllamaScriptProvider RAG integration
/// </summary>
public class OllamaScriptProviderRagTests : IDisposable
{
    private readonly Mock<ILogger<OllamaScriptProvider>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "http://127.0.0.1:11434";
    private const string Model = "llama3.1:8b-q4_k_m";

    public OllamaScriptProviderRagTests()
    {
        _loggerMock = new Mock<ILogger<OllamaScriptProvider>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
    }

    [Fact]
    public void Constructor_WithRagContextBuilder_LogsRagEnabled()
    {
        // Arrange
        var ragContextBuilder = new Mock<RagContextBuilder>(
            Mock.Of<ILogger<RagContextBuilder>>(),
            Mock.Of<VectorIndex>(),
            Mock.Of<EmbeddingService>());

        // Act
        var provider = new OllamaScriptProvider(
            _loggerMock.Object,
            _httpClient,
            ragContextBuilder.Object,
            BaseUrl,
            Model);

        // Assert - verify logging indicates RAG is enabled
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ragEnabled=True")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithoutRagContextBuilder_LogsRagDisabled()
    {
        // Act
        var provider = new OllamaScriptProvider(
            _loggerMock.Object,
            _httpClient,
            ragContextBuilder: null,
            BaseUrl,
            Model);

        // Assert - verify logging indicates RAG is disabled
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ragEnabled=False")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateScriptAsync_WithRagEnabled_RetrievesContext()
    {
        // Arrange
        var ragContextBuilderMock = new Mock<RagContextBuilder>(
            Mock.Of<ILogger<RagContextBuilder>>(),
            Mock.Of<VectorIndex>(),
            Mock.Of<EmbeddingService>());

        var testContext = new RagContext
        {
            Query = "Test Topic",
            Chunks = new List<ContextChunk>
            {
                new ContextChunk
                {
                    Content = "Test context content",
                    Source = "test.pdf",
                    RelevanceScore = 0.9f,
                    CitationNumber = 1
                }
            },
            FormattedContext = "# Reference Material\n\nTest context content [Citation 1]",
            TotalTokens = 50
        };

        ragContextBuilderMock
            .Setup(r => r.BuildContextAsync(
                It.IsAny<string>(),
                It.IsAny<RagConfig>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(testContext);

        var provider = new OllamaScriptProvider(
            _loggerMock.Object,
            _httpClient,
            ragContextBuilderMock.Object,
            BaseUrl,
            Model,
            maxRetries: 0,
            timeoutSeconds: 30);

        var request = CreateTestRequestWithRag();

        // Setup HTTP responses
        SetupSuccessfulOllamaResponse();

        // Act
        try
        {
            await provider.GenerateScriptAsync(request, CancellationToken.None);
        }
        catch
        {
            // Expected to fail due to mocked HTTP, but RAG should have been called
        }

        // Assert - verify RAG context builder was called
        ragContextBuilderMock.Verify(
            r => r.BuildContextAsync(
                It.Is<string>(s => s == "Test Topic"),
                It.Is<RagConfig>(c => c.Enabled),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateScriptAsync_WithRagDisabled_SkipsContextRetrieval()
    {
        // Arrange
        var ragContextBuilderMock = new Mock<RagContextBuilder>(
            Mock.Of<ILogger<RagContextBuilder>>(),
            Mock.Of<VectorIndex>(),
            Mock.Of<EmbeddingService>());

        var provider = new OllamaScriptProvider(
            _loggerMock.Object,
            _httpClient,
            ragContextBuilderMock.Object,
            BaseUrl,
            Model,
            maxRetries: 0,
            timeoutSeconds: 30);

        var request = CreateTestRequestWithoutRag();

        // Setup HTTP responses
        SetupSuccessfulOllamaResponse();

        // Act
        try
        {
            await provider.GenerateScriptAsync(request, CancellationToken.None);
        }
        catch
        {
            // Expected to fail due to mocked HTTP, but RAG should NOT have been called
        }

        // Assert - verify RAG context builder was NOT called
        ragContextBuilderMock.Verify(
            r => r.BuildContextAsync(
                It.IsAny<string>(),
                It.IsAny<RagConfig>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GenerateScriptAsync_WhenRagFails_ContinuesWithoutContext()
    {
        // Arrange
        var ragContextBuilderMock = new Mock<RagContextBuilder>(
            Mock.Of<ILogger<RagContextBuilder>>(),
            Mock.Of<VectorIndex>(),
            Mock.Of<EmbeddingService>());

        ragContextBuilderMock
            .Setup(r => r.BuildContextAsync(
                It.IsAny<string>(),
                It.IsAny<RagConfig>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("RAG service unavailable"));

        var provider = new OllamaScriptProvider(
            _loggerMock.Object,
            _httpClient,
            ragContextBuilderMock.Object,
            BaseUrl,
            Model,
            maxRetries: 0,
            timeoutSeconds: 30);

        var request = CreateTestRequestWithRag();

        // Setup HTTP responses
        SetupSuccessfulOllamaResponse();

        // Act
        try
        {
            await provider.GenerateScriptAsync(request, CancellationToken.None);
        }
        catch
        {
            // Expected to fail due to mocked HTTP
        }

        // Assert - verify warning was logged about RAG failure
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to retrieve RAG context")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private ScriptGenerationRequest CreateTestRequestWithRag()
    {
        return new ScriptGenerationRequest
        {
            Brief = new Brief(
                Topic: "Test Topic",
                Audience: "General",
                Goal: "Educate",
                Tone: "Professional",
                Language: "en",
                Aspect: Aspect.Widescreen16x9,
                RagConfiguration: new RagConfiguration(
                    Enabled: true,
                    TopK: 5,
                    MinimumScore: 0.7f,
                    MaxContextTokens: 2000,
                    IncludeCitations: true,
                    TightenClaims: false)),
            PlanSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(30),
                Pacing: Pacing.Medium,
                Density: Density.Medium,
                Style: "Educational"),
            CorrelationId = Guid.NewGuid().ToString()
        };
    }

    private ScriptGenerationRequest CreateTestRequestWithoutRag()
    {
        return new ScriptGenerationRequest
        {
            Brief = new Brief(
                Topic: "Test Topic",
                Audience: "General",
                Goal: "Educate",
                Tone: "Professional",
                Language: "en",
                Aspect: Aspect.Widescreen16x9,
                RagConfiguration: null),
            PlanSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(30),
                Pacing: Pacing.Medium,
                Density: Density.Medium,
                Style: "Educational"),
            CorrelationId = Guid.NewGuid().ToString()
        };
    }

    private void SetupSuccessfulOllamaResponse()
    {
        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"0.1.0\"}", Encoding.UTF8, "application/json")
        };

        var generateResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    response = "Scene 1: Introduction.\nScene 2: Main content.\nScene 3: Conclusion.",
                    done = true
                }),
                Encoding.UTF8,
                "application/json")
        };

        _httpMessageHandlerMock
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(versionResponse)
            .ReturnsAsync(generateResponse);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
