using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ScriptOrchestratorTests
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

    [Fact]
    public async Task GenerateScript_Should_UseProProvider_WhenAvailable()
    {
        // Arrange
        var mockProProvider = new Mock<ILlmProvider>();
        mockProProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Test Script\n## Introduction\nTest content");

        var mockFreeProvider = new Mock<ILlmProvider>();

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProProvider.Object,
            ["RuleBased"] = mockFreeProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Pro",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal("OpenAI", result.ProviderUsed);
        Assert.False(result.IsFallback);
        Assert.NotNull(result.Script);
        mockProProvider.Verify(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        mockFreeProvider.Verify(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateScript_Should_FallbackToFree_WhenProFails()
    {
        // Arrange
        var mockProProvider = new Mock<ILlmProvider>();
        mockProProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Pro provider unavailable"));

        var mockFreeProvider = new Mock<ILlmProvider>();
        mockFreeProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Test Script\n## Introduction\nTest content");

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProProvider.Object,
            ["RuleBased"] = mockFreeProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false, AutoFallback = true };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Pro",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.True(result.IsFallback);
        Assert.NotNull(result.Script);
    }

    [Fact]
    public async Task GenerateScript_Should_BlockPro_WhenOfflineOnly()
    {
        // Arrange
        var mockProProvider = new Mock<ILlmProvider>();
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Pro",
            offlineOnly: true,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Equal("E307", result.ErrorCode);
        Assert.Contains("OfflineOnly", result.ErrorMessage);
        mockProProvider.Verify(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GenerateScript_Should_DowngradeGracefully_WhenProIfAvailableAndOffline()
    {
        // Arrange
        var mockFreeProvider = new Mock<ILlmProvider>();
        mockFreeProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Test Script\n## Introduction\nTest content");

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = mockFreeProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "ProIfAvailable",
            offlineOnly: true,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.NotNull(result.Script);
    }

    [Fact]
    public async Task GenerateScript_Should_FallbackChain_OllamaThenRuleBased()
    {
        // Arrange
        var mockProProvider = new Mock<ILlmProvider>();
        mockProProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Pro provider failed"));

        var mockOllamaProvider = new Mock<ILlmProvider>();
        mockOllamaProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Ollama not available"));

        var mockRuleBasedProvider = new Mock<ILlmProvider>();
        mockRuleBasedProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Test Script\n## Introduction\nFallback content");

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProProvider.Object,
            ["Ollama"] = mockOllamaProvider.Object,
            ["RuleBased"] = mockRuleBasedProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false, AutoFallback = true };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Pro",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.True(result.IsFallback);
        Assert.NotNull(result.Script);
        
        // Verify fallback chain was followed
        mockProProvider.Verify(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        mockOllamaProvider.Verify(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        mockRuleBasedProvider.Verify(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GenerateScript_Should_HandleEmptyScriptResponse()
    {
        // Arrange
        var mockProvider = new Mock<ILlmProvider>();
        mockProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(""); // Empty script

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = mockProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(NullLogger<ScriptOrchestrator>.Instance, NullLoggerFactory.Instance, mixer, providers);

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Free",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.Equal("E300", result.ErrorCode);
        Assert.Contains("failed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
