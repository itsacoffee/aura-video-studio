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

/// <summary>
/// Tests for provider validation, fallback, and downgrade logic
/// </summary>
public class ProviderDowngradeTests
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
    public async Task ProLlmFails_ShouldFallbackToFreeProvider()
    {
        // Arrange - Pro LLM provider (OpenAI) fails
        var mockProLlmProvider = new Mock<ILlmProvider>();
        mockProLlmProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Pro LLM provider unavailable"));

        // Free provider (RuleBased) succeeds
        var mockFreeProvider = new Mock<ILlmProvider>();
        mockFreeProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Test Script\n## Introduction\nGenerated with free provider");

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProLlmProvider.Object,
            ["RuleBased"] = mockFreeProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false, AutoFallback = true };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Pro",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success, "Should succeed with fallback provider");
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.True(result.IsFallback, "Should be marked as fallback");
        Assert.Equal("OpenAI", result.RequestedProvider);
        Assert.NotNull(result.DowngradeReason);
        Assert.Contains("failed", result.DowngradeReason, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(result.Script);
        
        // Verify Pro provider was tried first
        mockProLlmProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        // Verify fallback provider was used
        mockFreeProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task LocalSdDisabled_ShouldFallbackToStockVisuals()
    {
        // This test simulates what would happen in a visual generation scenario
        // Since we don't have a VisualOrchestrator yet, we'll test the pattern with ScriptOrchestrator
        
        // Arrange - Simulate SD provider not available
        var mockSdProvider = new Mock<ILlmProvider>();
        // Provider doesn't exist in the dictionary, simulating it being disabled
        
        var mockStockProvider = new Mock<ILlmProvider>();
        mockStockProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Test Script\n## Scene 1\nUsing stock visuals");

        var providers = new Dictionary<string, ILlmProvider>
        {
            // StableDiffusion not in dictionary (disabled/unreachable)
            ["RuleBased"] = mockStockProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        // Act - Request local provider tier which would prefer SD
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Free",
            offlineOnly: true,
            CancellationToken.None
        );

        // Assert
        Assert.True(result.Success);
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.NotNull(result.Script);
    }

    [Fact]
    public async Task DowngradeEnvelope_ShouldContainMetadata()
    {
        // Arrange
        var mockPrimaryProvider = new Mock<ILlmProvider>();
        mockPrimaryProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Primary failed"));

        var mockFallbackProvider = new Mock<ILlmProvider>();
        mockFallbackProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Fallback Script\n## Scene 1\nGenerated by fallback");

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockPrimaryProvider.Object,
            ["RuleBased"] = mockFallbackProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Pro",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert - Verify downgrade metadata is captured
        Assert.True(result.Success);
        Assert.NotNull(result.RequestedProvider);
        Assert.NotNull(result.ProviderUsed);
        Assert.NotEqual(result.RequestedProvider, result.ProviderUsed);
        Assert.True(result.IsFallback);
        Assert.NotNull(result.DowngradeReason);
    }

    [Fact]
    public async Task AllProvidersFail_ShouldReturnFailure()
    {
        // Arrange - All providers fail
        var mockProvider1 = new Mock<ILlmProvider>();
        mockProvider1
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider 1 failed"));

        var mockProvider2 = new Mock<ILlmProvider>();
        mockProvider2
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Provider 2 failed"));

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProvider1.Object,
            ["RuleBased"] = mockProvider2.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Pro",
            offlineOnly: false,
            CancellationToken.None
        );

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorCode);
        Assert.NotNull(result.ErrorMessage);
        
        // Both providers should have been tried
        mockProvider1.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        mockProvider2.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task PrimarySucceeds_ShouldNotFallback()
    {
        // Arrange - Primary provider succeeds
        var mockPrimaryProvider = new Mock<ILlmProvider>();
        mockPrimaryProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Primary Script\n## Scene 1\nGenerated by primary");

        var mockFallbackProvider = new Mock<ILlmProvider>();

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockPrimaryProvider.Object,
            ["RuleBased"] = mockFallbackProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

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
        Assert.Null(result.DowngradeReason);
        
        // Primary was used
        mockPrimaryProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        // Fallback was not used
        mockFallbackProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task SecondProviderSucceeds_ShouldSkipFurtherFallbacks()
    {
        // Arrange
        var mockPrimaryProvider = new Mock<ILlmProvider>();
        mockPrimaryProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Primary failed"));

        var mockOllamaProvider = new Mock<ILlmProvider>();
        mockOllamaProvider
            .Setup(p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("# Ollama Script\n## Scene 1\nGenerated by Ollama");

        var mockRuleBasedProvider = new Mock<ILlmProvider>();

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockPrimaryProvider.Object,
            ["Ollama"] = mockOllamaProvider.Object,
            ["RuleBased"] = mockRuleBasedProvider.Object
        };

        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            mixer,
            providers
        );

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
        Assert.Equal("Ollama", result.ProviderUsed);
        Assert.True(result.IsFallback);
        Assert.Equal("OpenAI", result.RequestedProvider);
        
        // Primary was tried
        mockPrimaryProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        // Ollama succeeded
        mockOllamaProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), 
            Times.Once);
        
        // RuleBased was not needed
        mockRuleBasedProvider.Verify(
            p => p.DraftScriptAsync(It.IsAny<Brief>(), It.IsAny<PlanSpec>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }
}
