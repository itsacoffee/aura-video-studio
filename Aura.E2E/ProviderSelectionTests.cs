using System;
using System.Collections.Generic;
using System.Linq;
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
/// Tests for provider selection and validation logic
/// </summary>
public class ProviderSelectionTests
{
    /// <summary>
    /// Test that ProviderMixer selects appropriate providers for each tier
    /// </summary>
    [Theory]
    [InlineData("Free", "RuleBased", false)]
    [InlineData("Pro", "OpenAI", false)]
    public void LlmProviderSelection_Should_SelectCorrectProviderForTier(
        string tier, 
        string expectedProvider, 
        bool expectedFallback)
    {
        // Arrange
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = false,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new System.Collections.Generic.Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
            ["OpenAI"] = new FailingLlmProvider("OpenAI") // Simulates Pro provider availability
        };

        // Act
        var selection = mixer.SelectLlmProvider(providers, tier);

        // Assert
        Assert.Equal(expectedProvider, selection.SelectedProvider);
        Assert.Equal(expectedFallback, selection.IsFallback);
        Assert.Equal("Script", selection.Stage);
    }

    /// <summary>
    /// Test ProIfAvailable falls back when Pro not available
    /// </summary>
    [Fact]
    public void LlmProviderSelection_ProIfAvailable_Should_FallbackWhenProUnavailable()
    {
        // Arrange - Only Free providers available
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = false,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new System.Collections.Generic.Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
            // OpenAI not included - simulates unavailability
        };

        // Act
        var selection = mixer.SelectLlmProvider(providers, "ProIfAvailable");

        // Assert
        Assert.Equal("RuleBased", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.Equal("Script", selection.Stage);
    }

    /// <summary>
    /// Test TTS provider selection for different tiers
    /// </summary>
    [Theory]
    [InlineData("Free", "Windows")]
    [InlineData("Pro", "ElevenLabs")]
    public void TtsProviderSelection_Should_SelectCorrectProviderForTier(
        string tier,
        string expectedProvider)
    {
        // Arrange
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = false,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new System.Collections.Generic.Dictionary<string, ITtsProvider>
        {
            ["Windows"] = new MockTtsProvider("Windows"),
            ["ElevenLabs"] = new MockTtsProvider("ElevenLabs")
        };

        // Act
        var selection = mixer.SelectTtsProvider(providers, tier);

        // Assert
        Assert.Equal(expectedProvider, selection.SelectedProvider);
        Assert.Equal("TTS", selection.Stage);
    }

    /// <summary>
    /// Test TTS ProIfAvailable falls back when Pro not available
    /// </summary>
    [Fact]
    public void TtsProviderSelection_ProIfAvailable_Should_FallbackWhenProUnavailable()
    {
        // Arrange - Only Free providers available
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = false,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new System.Collections.Generic.Dictionary<string, ITtsProvider>
        {
            ["Windows"] = new MockTtsProvider("Windows")
            // ElevenLabs not included - simulates unavailability
        };

        // Act
        var selection = mixer.SelectTtsProvider(providers, "ProIfAvailable");

        // Assert
        Assert.Equal("Windows", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.Equal("TTS", selection.Stage);
    }

    /// <summary>
    /// Test Visual provider selection for different tiers
    /// </summary>
    [Theory]
    [InlineData("Free", "Stock", false, 0)]
    [InlineData("Pro", "Stability", false, 0)]
    [InlineData("StockOrLocal", "Stock", false, 0)]
    [InlineData("StockOrLocal", "StableDiffusion", true, 8)] // When SD available and GPU present
    public void VisualProviderSelection_Should_SelectCorrectProviderForTier(
        string tier,
        string expectedProvider,
        bool hasNvidiaGpu,
        int vramGB)
    {
        // Arrange
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = false,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new System.Collections.Generic.Dictionary<string, object>
        {
            ["Stock"] = new object(),
            ["Stability"] = new object(),
            ["StableDiffusion"] = new object()
        };

        // Act
        var selection = mixer.SelectVisualProvider(providers, tier, hasNvidiaGpu, vramGB);

        // Assert
        Assert.Equal(expectedProvider, selection.SelectedProvider);
        Assert.Equal("Visuals", selection.Stage);
    }

    /// <summary>
    /// Test that fallback chain works correctly
    /// </summary>
    [Fact]
    public void ProviderMixer_Should_IndicateFallbackWhenProNotAvailable()
    {
        // Arrange
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = false,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        // Only Free providers available
        var providers = new System.Collections.Generic.Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance)
        };

        // Act - Request Pro tier
        var selection = mixer.SelectLlmProvider(providers, "Pro");

        // Assert - Should fallback to RuleBased
        Assert.Equal("RuleBased", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.NotNull(selection.FallbackFrom);
        Assert.Contains("Pro", selection.FallbackFrom);
    }

    /// <summary>
    /// Test specific provider name selection
    /// </summary>
    [Fact]
    public void ProviderMixer_Should_SelectSpecificProviderByName()
    {
        // Arrange
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = false,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new System.Collections.Generic.Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
            ["Ollama"] = new FailingLlmProvider("Ollama")
        };

        // Act - Request specific provider by name
        var selection = mixer.SelectLlmProvider(providers, "Ollama");

        // Assert
        Assert.Equal("Ollama", selection.SelectedProvider);
        Assert.False(selection.IsFallback);
    }

    /// <summary>
    /// Test logging of provider selection
    /// </summary>
    [Fact]
    public void ProviderMixer_LogSelection_Should_NotThrow()
    {
        // Arrange
        var config = new ProviderMixingConfig
        {
            LogProviderSelection = true,
            AutoFallback = true
        };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var selection = new ProviderSelection
        {
            Stage = "Script",
            SelectedProvider = "RuleBased",
            Reason = "Test",
            IsFallback = false
        };

        // Act & Assert - Should not throw
        mixer.LogSelection(selection);
    }
}
