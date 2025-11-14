using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.E2E;

/// <summary>
/// End-to-end orchestrator validation tests
/// Tests full provider selection, fallback chains, and hybrid generation scenarios
/// </summary>
public class OrchestratorValidationTests
{
    private readonly Brief _testBrief = new(
        Topic: "Introduction to AI",
        Audience: "Beginners",
        Goal: "Educational",
        Tone: "Friendly",
        Language: "English",
        Aspect: Aspect.Widescreen16x9
    );

    private readonly PlanSpec _testSpec = new(
        TargetDuration: TimeSpan.FromSeconds(15),
        Pacing: Pacing.Conversational,
        Density: Density.Balanced,
        Style: "Educational"
    );

    /// <summary>
    /// Test offline local-only generation (Free tier)
    /// Uses only local providers: RuleBased LLM
    /// </summary>
    [Fact]
    public async Task OfflineGeneration_Should_UseOnlyLocalProviders()
    {
        // Arrange
        var ruleBasedProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = ruleBasedProvider
        };

        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            providers
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Free",
            offlineOnly: true,
            CancellationToken.None
        ).ConfigureAwait(false);

        // Assert
        Assert.True(result.Success, "Script generation should succeed with local provider");
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.False(result.IsFallback, "RuleBased should be the primary provider for Free tier");
        Assert.NotNull(result.Script);
        Assert.InRange(result.Script!.Length, 50, 5000);
    }

    /// <summary>
    /// Test hybrid generation with local LLM + local TTS + local visuals
    /// Validates that all stages can work offline
    /// </summary>
    [Fact]
    public async Task HybridGeneration_Should_UseLocalForAllStages()
    {
        // Arrange - LLM
        var ruleBasedProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = ruleBasedProvider
        };

        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        // Act - Script generation
        var scriptOrchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            llmProviders
        );

        var scriptResult = await scriptOrchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Free",
            offlineOnly: true,
            CancellationToken.None
        ).ConfigureAwait(false);

        // Assert - Script
        Assert.True(scriptResult.Success);
        Assert.Equal("RuleBased", scriptResult.ProviderUsed);

        // Act - TTS provider selection
        var mockTtsProviders = new Dictionary<string, ITtsProvider>
        {
            ["Windows"] = new MockTtsProvider("Windows")
        };
        var ttsSelection = mixer.SelectTtsProvider(mockTtsProviders, "Free");

        // Assert - TTS
        Assert.Equal("Windows", ttsSelection.SelectedProvider);
        Assert.False(ttsSelection.IsFallback);

        // Act - Visual provider selection
        var mockVisualProviders = new Dictionary<string, object>
        {
            ["Stock"] = new object()
        };
        var visualSelection = mixer.SelectVisualProvider(mockVisualProviders, "Free", false, 0);

        // Assert - Visual
        Assert.Equal("Stock", visualSelection.SelectedProvider);
        Assert.False(visualSelection.IsFallback);
    }

    /// <summary>
    /// Test Pro tier with automatic fallback to local providers
    /// Simulates Pro providers being unavailable and validates fallback chain
    /// </summary>
    [Fact]
    public async Task ProTierFallback_Should_DowngradeToLocalProviders()
    {
        // Arrange - Only local providers available
        var ruleBasedProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = ruleBasedProvider
            // Pro providers (OpenAI, etc.) intentionally not included to simulate unavailability
        };

        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            providers
        );

        // Act - Request Pro tier but should fallback to Free
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "ProIfAvailable",
            offlineOnly: false,
            CancellationToken.None
        ).ConfigureAwait(false);

        // Assert
        Assert.True(result.Success, "Should succeed with fallback");
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.True(result.IsFallback, "Should be marked as fallback");
        Assert.NotNull(result.Script);
    }

    /// <summary>
    /// Test complete fallback chain: Pro -> Ollama -> RuleBased
    /// Validates that each fallback level is attempted in order
    /// </summary>
    [Fact]
    public async Task FallbackChain_Should_TryAllProvidersInOrder()
    {
        // Arrange - Mock Pro provider that fails
        var mockProProvider = new FailingLlmProvider("OpenAI");
        var ruleBasedProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProProvider,
            ["RuleBased"] = ruleBasedProvider
        };

        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
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
        ).ConfigureAwait(false);

        // Assert
        Assert.True(result.Success, "Should succeed after fallback");
        Assert.Equal("RuleBased", result.ProviderUsed);
        Assert.True(result.IsFallback);
        Assert.Equal("OpenAI", result.RequestedProvider);
        Assert.NotNull(result.DowngradeReason);
    }

    /// <summary>
    /// Test that offline mode blocks Pro providers
    /// </summary>
    [Fact]
    public async Task OfflineMode_Should_BlockProProviders()
    {
        // Arrange
        var mockProProvider = new FailingLlmProvider("OpenAI");
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = mockProProvider
        };

        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            providers
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "Pro",
            offlineOnly: true,
            CancellationToken.None
        ).ConfigureAwait(false);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("E307", result.ErrorCode);
        Assert.Contains("OfflineOnly mode", result.ErrorMessage);
    }

    /// <summary>
    /// Test ProIfAvailable downgrades gracefully in offline mode
    /// </summary>
    [Fact]
    public async Task OfflineMode_ProIfAvailable_Should_DowngradeToFree()
    {
        // Arrange
        var ruleBasedProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = ruleBasedProvider
        };

        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            mixer,
            providers
        );

        // Act
        var result = await orchestrator.GenerateScriptAsync(
            _testBrief,
            _testSpec,
            "ProIfAvailable",
            offlineOnly: true,
            CancellationToken.None
        ).ConfigureAwait(false);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("RuleBased", result.ProviderUsed);
    }

    /// <summary>
    /// Test provider mixer logs selection correctly
    /// </summary>
    [Fact]
    public void ProviderMixer_Should_LogSelectionDetails()
    {
        // Arrange
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
            ["OpenAI"] = new FailingLlmProvider("OpenAI")
        };

        // Act
        var selection = mixer.SelectLlmProvider(providers, "Pro");

        // Assert
        Assert.Equal("OpenAI", selection.SelectedProvider);
        Assert.False(selection.IsFallback);
        Assert.Equal("Script", selection.Stage);

        // Act - Should select fallback when Pro not available
        var freeProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };
        var freeSelection = mixer.SelectLlmProvider(freeProviders, "Pro");

        // Assert
        Assert.Equal("RuleBased", freeSelection.SelectedProvider);
        Assert.True(freeSelection.IsFallback);
        Assert.NotNull(freeSelection.FallbackFrom);
    }
}
