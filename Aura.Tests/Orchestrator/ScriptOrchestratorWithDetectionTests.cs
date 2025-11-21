using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests.Orchestrator;

/// <summary>
/// Tests for ScriptOrchestrator integration with OllamaDetectionService
/// Validates that script generation waits for Ollama detection to complete
/// </summary>
public class ScriptOrchestratorWithDetectionTests : IDisposable
{
    private readonly Brief _testBrief = new Brief(
        Topic: "Test Topic",
        Audience: "General",
        Goal: "Educational",
        Tone: "Informative",
        Language: "en-US",
        Aspect: Aspect.Widescreen16x9
    );

    private readonly PlanSpec _testSpec = new PlanSpec(
        TargetDuration: TimeSpan.FromMinutes(3),
        Pacing: Pacing.Conversational,
        Density: Density.Balanced,
        Style: "Educational"
    );

    private readonly IMemoryCache _cache;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;

    public ScriptOrchestratorWithDetectionTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _cache?.Dispose();
    }

    [Fact]
    public async Task GenerateScriptAsync_Should_WaitForOllamaDetection_BeforeGenerating()
    {
        // Arrange
        var detectionStarted = false;
        var detectionCompleted = false;
        var scriptGenerationStarted = false;

        SetupSuccessfulOllamaResponse();

        var mockOllamaProvider = new Mock<ILlmProvider>();
        mockOllamaProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                scriptGenerationStarted = true;
                await Task.Delay(10);
                return "# Test Script\n## Introduction\nTest content";
            });

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = mockOllamaProvider.Object,
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        var detectionService = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        // Start detection service
        await detectionService.StartAsync(CancellationToken.None);
        detectionStarted = true;

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            providers,
            detectionService
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Free",
            offlineOnly: false,
            CancellationToken.None
        );

        detectionCompleted = detectionService.IsDetectionComplete;

        // Assert
        Assert.True(detectionStarted);
        Assert.True(detectionCompleted);
        Assert.True(scriptGenerationStarted);
        Assert.True(result.Success);
        Assert.Equal("Ollama", result.ProviderUsed);
    }

    [Fact]
    public async Task GenerateScriptAsync_Should_ProceedWithFallback_WhenDetectionTimesOut()
    {
        // Arrange
        SetupSlowOllamaResponse();

        var mockRuleBasedProvider = new Mock<ILlmProvider>();
        mockRuleBasedProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# RuleBased Script\n## Introduction\nGenerated content");

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = mockRuleBasedProvider.Object
        };

        var detectionService = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        await detectionService.StartAsync(CancellationToken.None);

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            providers,
            detectionService
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Free",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert - Should succeed with RuleBased fallback
        Assert.True(result.Success);
        Assert.Equal("RuleBased", result.ProviderUsed);
    }

    [Fact]
    public async Task GenerateScriptAsync_Should_WorkWithoutDetectionService()
    {
        // Arrange - No detection service provided
        var mockOllamaProvider = new Mock<ILlmProvider>();
        mockOllamaProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Test Script\n## Introduction\nTest content");

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = mockOllamaProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            providers,
            ollamaDetectionService: null // No detection service
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Free",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert - Should still work without detection service
        Assert.True(result.Success);
        Assert.Equal("Ollama", result.ProviderUsed);
    }

    [Fact]
    public async Task GenerateScriptDeterministicAsync_Should_WaitForOllamaDetection()
    {
        // Arrange
        SetupSuccessfulOllamaResponse();

        var mockOllamaProvider = new Mock<ILlmProvider>();
        mockOllamaProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Test Script\n## Introduction\nTest content");

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = mockOllamaProvider.Object,
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        var detectionService = new OllamaDetectionService(
            NullLogger<OllamaDetectionService>.Instance,
            _httpClient,
            _cache,
            "http://localhost:11434"
        );

        await detectionService.StartAsync(CancellationToken.None);

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            providers,
            detectionService
        );

        // Act
        var result = await orchestrator.GenerateScriptDeterministicAsync(
            _testBrief,
            _testSpec,
            "Free",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert
        Assert.True(detectionService.IsDetectionComplete);
        Assert.True(result.Success);
    }

    private void SetupSuccessfulOllamaResponse()
    {
        var versionResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"version\":\"0.1.0\"}")
        };

        var tagsResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"models\":[]}")
        };

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/version")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(versionResponse);

        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/api/tags")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(tagsResponse);
    }

    private void SetupSlowOllamaResponse()
    {
        _mockHttpHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(15));
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"version\":\"0.1.0\"}")
                };
            });
    }
}
