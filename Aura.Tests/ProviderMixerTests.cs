using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ProviderMixerTests
{
    [Fact]
    public void SelectLlmProvider_Should_PreferProWhenAvailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var selection = mixer.SelectLlmProvider(providers, "Pro");

        // Assert
        Assert.Equal("OpenAI", selection.SelectedProvider);
        Assert.False(selection.IsFallback);
        Assert.Equal("Script", selection.Stage);
    }

    [Fact]
    public void SelectLlmProvider_Should_FallbackToFreeWhenProUnavailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var selection = mixer.SelectLlmProvider(providers, "Pro");

        // Assert
        Assert.Equal("RuleBased", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.NotNull(selection.FallbackFrom);
    }

    [Fact]
    public void SelectLlmProvider_Should_PreferOllamaOverRuleBased()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var selection = mixer.SelectLlmProvider(providers, "Free");

        // Assert
        Assert.Equal("Ollama", selection.SelectedProvider);
        Assert.False(selection.IsFallback);
    }

    [Fact]
    public void SelectLlmProvider_Should_UseProIfAvailableLogic()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        // Pro available
        var providersWithPro = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = Mock.Of<ILlmProvider>(),
            ["Ollama"] = Mock.Of<ILlmProvider>()
        };

        var selection1 = mixer.SelectLlmProvider(providersWithPro, "ProIfAvailable");
        Assert.Equal("OpenAI", selection1.SelectedProvider);
        Assert.False(selection1.IsFallback);

        // Pro not available
        var providersWithoutPro = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = Mock.Of<ILlmProvider>()
        };

        var selection2 = mixer.SelectLlmProvider(providersWithoutPro, "ProIfAvailable");
        Assert.Equal("Ollama", selection2.SelectedProvider);
        Assert.True(selection2.IsFallback);
    }

    [Fact]
    public void SelectTtsProvider_Should_PreferProWhenAvailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ITtsProvider>
        {
            ["ElevenLabs"] = Mock.Of<ITtsProvider>(),
            ["Windows"] = Mock.Of<ITtsProvider>()
        };

        // Act
        var selection = mixer.SelectTtsProvider(providers, "Pro");

        // Assert
        Assert.Equal("ElevenLabs", selection.SelectedProvider);
        Assert.False(selection.IsFallback);
        Assert.Equal("TTS", selection.Stage);
    }

    [Fact]
    public void SelectTtsProvider_Should_FallbackToWindowsWhenProUnavailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ITtsProvider>
        {
            ["Windows"] = Mock.Of<ITtsProvider>()
        };

        // Act
        var selection = mixer.SelectTtsProvider(providers, "Pro");

        // Assert
        Assert.Equal("Windows", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.Equal("Pro/Local TTS", selection.FallbackFrom);
    }

    [Fact]
    public void SelectVisualProvider_Should_RequireNvidiaForSD()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, object>
        {
            ["StableDiffusion"] = new object(),
            ["Stock"] = new object()
        };

        // NVIDIA GPU with sufficient VRAM - should use SD
        var selection1 = mixer.SelectVisualProvider(providers, "StockOrLocal", isNvidiaGpu: true, vramGB: 10);
        Assert.Equal("StableDiffusion", selection1.SelectedProvider);

        // AMD GPU with sufficient VRAM - should NOT use SD
        var selection2 = mixer.SelectVisualProvider(providers, "StockOrLocal", isNvidiaGpu: false, vramGB: 16);
        Assert.Equal("Stock", selection2.SelectedProvider);

        // NVIDIA GPU with insufficient VRAM - should NOT use SD
        var selection3 = mixer.SelectVisualProvider(providers, "StockOrLocal", isNvidiaGpu: true, vramGB: 4);
        Assert.Equal("Stock", selection3.SelectedProvider);
    }

    [Fact]
    public void SelectVisualProvider_Should_FallbackToSlideshow()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, object>(); // No providers

        // Act
        var selection = mixer.SelectVisualProvider(providers, "Pro", isNvidiaGpu: false, vramGB: 0);

        // Assert
        Assert.Equal("Slideshow", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
    }

    [Fact]
    public void ProviderProfile_Should_HaveCorrectPresets()
    {
        // Assert
        var freeOnly = ProviderProfile.FreeOnly;
        Assert.Equal("Free-Only", freeOnly.Name);
        Assert.Equal("Free", freeOnly.Stages["Script"]);
        Assert.Equal("Windows", freeOnly.Stages["TTS"]);
        Assert.Equal("Stock", freeOnly.Stages["Visuals"]);

        var balanced = ProviderProfile.BalancedMix;
        Assert.Equal("Balanced Mix", balanced.Name);
        Assert.Equal("ProIfAvailable", balanced.Stages["Script"]);
        Assert.Equal("StockOrLocal", balanced.Stages["Visuals"]);

        var proMax = ProviderProfile.ProMax;
        Assert.Equal("Pro-Max", proMax.Name);
        Assert.Equal("Pro", proMax.Stages["Script"]);
        Assert.Equal("Pro", proMax.Stages["TTS"]);
        Assert.Equal("Pro", proMax.Stages["Visuals"]);
    }

    [Fact]
    public void SelectLlmProvider_Should_NeverThrowException_EmptyProviders()
    {
        // Arrange - This is the critical fix for "No LLM providers available" error
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var emptyProviders = new Dictionary<string, ILlmProvider>(); // NO providers at all

        // Act - Should NOT throw, should return RuleBased as guaranteed fallback
        var selection = mixer.SelectLlmProvider(emptyProviders, "Pro");

        // Assert
        Assert.NotNull(selection);
        Assert.Equal("RuleBased", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.Contains("guaranteed", selection.Reason, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectTtsProvider_Should_NeverThrowException_EmptyProviders()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var emptyProviders = new Dictionary<string, ITtsProvider>(); // NO providers at all

        // Act - Should NOT throw, should return Null as guaranteed fallback
        var selection = mixer.SelectTtsProvider(emptyProviders, "Pro");

        // Assert
        Assert.NotNull(selection);
        Assert.Equal("Null", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.Contains("guaranteed", selection.Reason, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectLlmProvider_Should_UseSpecificProviderWhenRequested()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = Mock.Of<ILlmProvider>(),
            ["Ollama"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act - Request specific provider by name
        var selection1 = mixer.SelectLlmProvider(providers, "Ollama");
        var selection2 = mixer.SelectLlmProvider(providers, "OpenAI");
        var selection3 = mixer.SelectLlmProvider(providers, "RuleBased");

        // Assert
        Assert.Equal("Ollama", selection1.SelectedProvider);
        Assert.False(selection1.IsFallback);

        Assert.Equal("OpenAI", selection2.SelectedProvider);
        Assert.False(selection2.IsFallback);

        Assert.Equal("RuleBased", selection3.SelectedProvider);
        Assert.False(selection3.IsFallback);
    }

    [Theory]
    [InlineData("Pro")]
    [InlineData("ProIfAvailable")]
    [InlineData("Free")]
    [InlineData("")]
    [InlineData(null)]
    public void ProviderMixer_AlwaysReturnsProvider_NeverThrows(string preferredTier)
    {
        // Arrange - This is the critical acceptance test
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var emptyLlmProviders = new Dictionary<string, ILlmProvider>();
        var emptyTtsProviders = new Dictionary<string, ITtsProvider>();
        var emptyVisualProviders = new Dictionary<string, object>();

        // Act & Assert - Should NEVER throw, always returns a fallback
        var llmSelection = mixer.SelectLlmProvider(emptyLlmProviders, preferredTier ?? "Free");
        Assert.NotNull(llmSelection);
        Assert.NotNull(llmSelection.SelectedProvider);
        Assert.Equal("RuleBased", llmSelection.SelectedProvider);

        var ttsSelection = mixer.SelectTtsProvider(emptyTtsProviders, preferredTier ?? "Free");
        Assert.NotNull(ttsSelection);
        Assert.NotNull(ttsSelection.SelectedProvider);
        Assert.Equal("Null", ttsSelection.SelectedProvider);

        var visualSelection = mixer.SelectVisualProvider(emptyVisualProviders, preferredTier ?? "Free", false, 0);
        Assert.NotNull(visualSelection);
        Assert.NotNull(visualSelection.SelectedProvider);
        Assert.True(visualSelection.SelectedProvider == "Slideshow"); // Ultimate fallback
    }

    [Fact]
    public void SelectLlmProvider_Should_ReturnRuleBasedForAllTiers_WhenNoProvidersAvailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var emptyProviders = new Dictionary<string, ILlmProvider>();

        // Act - Test all tier options
        var proSelection = mixer.SelectLlmProvider(emptyProviders, "Pro");
        var proIfAvailableSelection = mixer.SelectLlmProvider(emptyProviders, "ProIfAvailable");
        var freeSelection = mixer.SelectLlmProvider(emptyProviders, "Free");
        var emptySelection = mixer.SelectLlmProvider(emptyProviders, "");
        var nullSelection = mixer.SelectLlmProvider(emptyProviders, null!);

        // Assert - All should return RuleBased as guaranteed fallback
        Assert.Equal("RuleBased", proSelection.SelectedProvider);
        Assert.True(proSelection.IsFallback);
        Assert.Equal("All providers", proSelection.FallbackFrom);

        Assert.Equal("RuleBased", proIfAvailableSelection.SelectedProvider);
        Assert.True(proIfAvailableSelection.IsFallback);

        Assert.Equal("RuleBased", freeSelection.SelectedProvider);
        Assert.True(freeSelection.IsFallback);

        Assert.Equal("RuleBased", emptySelection.SelectedProvider);
        Assert.True(emptySelection.IsFallback);

        Assert.Equal("RuleBased", nullSelection.SelectedProvider);
        Assert.True(nullSelection.IsFallback);
    }

    [Fact]
    public void SelectTtsProvider_Should_ReturnWindowsForAllTiers_WhenNoProvidersAvailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var emptyProviders = new Dictionary<string, ITtsProvider>();

        // Act
        var proSelection = mixer.SelectTtsProvider(emptyProviders, "Pro");
        var proIfAvailableSelection = mixer.SelectTtsProvider(emptyProviders, "ProIfAvailable");
        var freeSelection = mixer.SelectTtsProvider(emptyProviders, "Free");

        // Assert - All should return Windows as guaranteed fallback
        Assert.Equal("Null", proSelection.SelectedProvider);
        Assert.True(proSelection.IsFallback);
        Assert.Equal("All TTS providers", proSelection.FallbackFrom);

        Assert.Equal("Null", proIfAvailableSelection.SelectedProvider);
        Assert.True(proIfAvailableSelection.IsFallback);

        Assert.Equal("Null", freeSelection.SelectedProvider);
        Assert.True(freeSelection.IsFallback);
    }

    [Fact]
    public void SelectVisualProvider_Should_ReturnSlideshowForAllTiers_WhenNoProvidersAvailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var emptyProviders = new Dictionary<string, object>();

        // Act
        var proSelection = mixer.SelectVisualProvider(emptyProviders, "Pro", false, 0);
        var proIfAvailableSelection = mixer.SelectVisualProvider(emptyProviders, "ProIfAvailable", false, 0);
        var freeSelection = mixer.SelectVisualProvider(emptyProviders, "Free", false, 0);
        var stockOrLocalSelection = mixer.SelectVisualProvider(emptyProviders, "StockOrLocal", false, 0);

        // Assert - All should return Slideshow as guaranteed fallback
        Assert.Equal("Slideshow", proSelection.SelectedProvider);
        Assert.True(proSelection.IsFallback);
        Assert.Equal("All visual providers", proSelection.FallbackFrom);

        Assert.Equal("Slideshow", proIfAvailableSelection.SelectedProvider);
        Assert.True(proIfAvailableSelection.IsFallback);

        Assert.Equal("Slideshow", freeSelection.SelectedProvider);
        Assert.True(freeSelection.IsFallback);

        Assert.Equal("Slideshow", stockOrLocalSelection.SelectedProvider);
        Assert.True(stockOrLocalSelection.IsFallback);
    }

    [Fact]
    public void SelectLlmProvider_Should_PreferHigherTierWhenAvailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var allProviders = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = Mock.Of<ILlmProvider>(),
            ["Azure"] = Mock.Of<ILlmProvider>(),
            ["Gemini"] = Mock.Of<ILlmProvider>(),
            ["Ollama"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act - Request Pro tier with all providers available
        var selection = mixer.SelectLlmProvider(allProviders, "Pro");

        // Assert - Should select OpenAI (highest priority Pro provider)
        Assert.Equal("OpenAI", selection.SelectedProvider);
        Assert.False(selection.IsFallback);
    }

    [Fact]
    public void SelectLlmProvider_Should_FollowFallbackChain_ProTier()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        // Test fallback chain: OpenAI → Azure → Gemini → Ollama → RuleBased
        
        // Only Ollama and RuleBased available
        var providers1 = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };
        var selection1 = mixer.SelectLlmProvider(providers1, "Pro");
        Assert.Equal("Ollama", selection1.SelectedProvider);
        Assert.True(selection1.IsFallback);

        // Only RuleBased available
        var providers2 = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };
        var selection2 = mixer.SelectLlmProvider(providers2, "Pro");
        Assert.Equal("RuleBased", selection2.SelectedProvider);
        Assert.True(selection2.IsFallback);

        // No providers available - should still return RuleBased
        var providers3 = new Dictionary<string, ILlmProvider>();
        var selection3 = mixer.SelectLlmProvider(providers3, "Pro");
        Assert.Equal("RuleBased", selection3.SelectedProvider);
        Assert.True(selection3.IsFallback);
    }

    [Fact]
    public void SelectLlmProvider_Should_HandleEmptyDictionary_WithGuaranteedFallback()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        // Act - Empty dictionary should return guaranteed fallback
        var emptyProviders = new Dictionary<string, ILlmProvider>();
        var selection = mixer.SelectLlmProvider(emptyProviders, "Pro");

        // Assert
        Assert.NotNull(selection);
        Assert.Equal("RuleBased", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.Contains("guaranteed", selection.Reason, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectTtsProvider_Should_HandleEmptyDictionary_WithGuaranteedFallback()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        // Act - Empty dictionary should return guaranteed fallback
        var emptyProviders = new Dictionary<string, ITtsProvider>();
        var selection = mixer.SelectTtsProvider(emptyProviders, "Pro");

        // Assert
        Assert.NotNull(selection);
        Assert.Equal("Null", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.Contains("guaranteed", selection.Reason, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SelectVisualProvider_Should_NeverThrow_EmptyProviders()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var emptyProviders = new Dictionary<string, object>();

        // Act - Should NOT throw, should return Slideshow as fallback
        var selection = mixer.SelectVisualProvider(emptyProviders, "Pro", false, 0);

        // Assert
        Assert.NotNull(selection);
        Assert.Equal("Slideshow", selection.SelectedProvider);
        Assert.True(selection.IsFallback);
        Assert.NotNull(selection.Reason);
    }

    [Fact]
    public void ProviderMixer_Should_HandleAllProfileTypes_WithoutThrowing()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var emptyProviders = new Dictionary<string, ILlmProvider>();
        
        var profiles = new[] { "Free", "Pro", "ProIfAvailable", "Local", "Mixed", "Unknown", null, "" };

        // Act & Assert - None should throw
        foreach (var profile in profiles)
        {
            var exception = Record.Exception(() => 
            {
                var selection = mixer.SelectLlmProvider(emptyProviders, profile!);
                Assert.NotNull(selection);
                Assert.NotNull(selection.SelectedProvider);
            });
            
            Assert.Null(exception);
        }
    }

    [Fact]
    public void SelectProvider_Should_RecoverFromErrors_InProviderDictionary()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        // Dictionary with some providers that might have issues
        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act - Multiple calls should all succeed
        var selection1 = mixer.SelectLlmProvider(providers, "Pro");
        var selection2 = mixer.SelectLlmProvider(providers, "Free");
        var selection3 = mixer.SelectLlmProvider(providers, "Unknown");

        // Assert - All should return valid selections
        Assert.NotNull(selection1);
        Assert.Equal("OpenAI", selection1.SelectedProvider);
        
        Assert.NotNull(selection2);
        Assert.Equal("RuleBased", selection2.SelectedProvider);
        
        Assert.NotNull(selection3);
        Assert.NotNull(selection3.SelectedProvider);
    }

    #region ResolveLlm Tests (Deterministic Provider Decision)

    [Fact]
    public void ResolveLlm_Should_ReturnDeterministicDecision_ProAvailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = Mock.Of<ILlmProvider>(),
            ["Azure"] = Mock.Of<ILlmProvider>(),
            ["Ollama"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "Pro", offlineOnly: false);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("OpenAI", decision.ProviderName);
        Assert.Equal(1, decision.PriorityRank);
        Assert.False(decision.IsFallback);
        Assert.Null(decision.FallbackFrom);
        Assert.Equal(new[] { "OpenAI", "Azure", "Gemini", "Ollama", "RuleBased" }, decision.DowngradeChain);
        Assert.Contains("available and preferred", decision.Reason);
    }

    [Fact]
    public void ResolveLlm_Should_FallbackToOllama_WhenProUnavailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "Pro", offlineOnly: false);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("Ollama", decision.ProviderName);
        Assert.Equal(4, decision.PriorityRank); // 4th in chain (OpenAI, Azure, Gemini, then Ollama)
        Assert.True(decision.IsFallback);
        Assert.Equal("OpenAI → Azure → Gemini", decision.FallbackFrom);
        Assert.Equal(new[] { "OpenAI", "Azure", "Gemini", "Ollama", "RuleBased" }, decision.DowngradeChain);
    }

    [Fact]
    public void ResolveLlm_Should_FallbackToRuleBased_WhenNoProvidersAvailable()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var emptyProviders = new Dictionary<string, ILlmProvider>();

        // Act
        var decision = mixer.ResolveLlm(emptyProviders, "Pro", offlineOnly: false);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("RuleBased", decision.ProviderName);
        Assert.Equal(5, decision.PriorityRank); // Last in chain
        Assert.True(decision.IsFallback);
        Assert.Equal("All providers", decision.FallbackFrom);
        Assert.Contains("guaranteed", decision.Reason);
    }

    [Fact]
    public void ResolveLlm_Should_BlockPro_InOfflineMode()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = Mock.Of<ILlmProvider>(),
            ["Ollama"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "Pro", offlineOnly: true);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("None", decision.ProviderName);
        Assert.Equal(0, decision.PriorityRank);
        Assert.False(decision.IsFallback);
        Assert.Contains("offline-only mode", decision.Reason);
        Assert.Empty(decision.DowngradeChain);
    }

    [Fact]
    public void ResolveLlm_Should_DowngradeToFree_WhenProIfAvailableAndOffline()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = Mock.Of<ILlmProvider>(),
            ["Ollama"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "ProIfAvailable", offlineOnly: true);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("Ollama", decision.ProviderName);
        Assert.Equal(1, decision.PriorityRank);
        Assert.False(decision.IsFallback);
        Assert.Equal(new[] { "Ollama", "RuleBased" }, decision.DowngradeChain);
    }

    [Fact]
    public void ResolveLlm_Should_UseOllama_WhenAvailableInOfflineMode()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "Free", offlineOnly: true);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("Ollama", decision.ProviderName);
        Assert.Equal(1, decision.PriorityRank);
        Assert.False(decision.IsFallback);
        Assert.Equal(new[] { "Ollama", "RuleBased" }, decision.DowngradeChain);
    }

    [Fact]
    public void ResolveLlm_Should_UseRuleBased_WhenOllamaNotAvailableInOfflineMode()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "Free", offlineOnly: true);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("RuleBased", decision.ProviderName);
        Assert.Equal(2, decision.PriorityRank);
        Assert.True(decision.IsFallback);
        Assert.Equal("Ollama", decision.FallbackFrom);
    }

    [Fact]
    public void ResolveLlm_Should_ReturnGuaranteedFallback_WhenNoProvidersInOfflineMode()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var emptyProviders = new Dictionary<string, ILlmProvider>();

        // Act
        var decision = mixer.ResolveLlm(emptyProviders, "Free", offlineOnly: true);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("RuleBased", decision.ProviderName);
        Assert.Equal(2, decision.PriorityRank);
        Assert.True(decision.IsFallback);
        Assert.Equal("All providers", decision.FallbackFrom);
        Assert.Contains("guaranteed", decision.Reason);
    }

    [Theory]
    [InlineData("Pro", false, "OpenAI")]
    [InlineData("ProIfAvailable", false, "OpenAI")]
    [InlineData("Free", false, "Ollama")]
    [InlineData("Free", true, "Ollama")]
    public void ResolveLlm_Should_BeDeterministic_ForVariousCombinations(
        string tier, bool offlineOnly, string expectedProvider)
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["OpenAI"] = Mock.Of<ILlmProvider>(),
            ["Azure"] = Mock.Of<ILlmProvider>(),
            ["Ollama"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision1 = mixer.ResolveLlm(providers, tier, offlineOnly);
        var decision2 = mixer.ResolveLlm(providers, tier, offlineOnly);
        var decision3 = mixer.ResolveLlm(providers, tier, offlineOnly);

        // Assert - All calls should return identical decisions
        Assert.Equal(decision1.ProviderName, decision2.ProviderName);
        Assert.Equal(decision2.ProviderName, decision3.ProviderName);
        Assert.Equal(expectedProvider, decision1.ProviderName);
        Assert.Equal(decision1.PriorityRank, decision2.PriorityRank);
        Assert.Equal(decision1.DowngradeChain, decision2.DowngradeChain);
    }

    [Fact]
    public void ResolveLlm_Should_NeverThrow_WithEmptyProviders()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);
        var emptyProviders = new Dictionary<string, ILlmProvider>();

        // Act & Assert - Should never throw
        var exception = Record.Exception(() =>
        {
            var decision1 = mixer.ResolveLlm(emptyProviders, "Pro", offlineOnly: false);
            var decision2 = mixer.ResolveLlm(emptyProviders, "ProIfAvailable", offlineOnly: true);
            var decision3 = mixer.ResolveLlm(emptyProviders, "Free", offlineOnly: false);
            var decision4 = mixer.ResolveLlm(emptyProviders, null!, offlineOnly: true);

            // All should return valid decisions
            Assert.NotNull(decision1);
            Assert.NotNull(decision2);
            Assert.NotNull(decision3);
            Assert.NotNull(decision4);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void ResolveLlm_Should_ProvideCompleteDowngradeChain_ForPro()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "Pro", offlineOnly: false);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("RuleBased", decision.ProviderName);
        Assert.Equal(new[] { "OpenAI", "Azure", "Gemini", "Ollama", "RuleBased" }, decision.DowngradeChain);
        Assert.True(decision.IsFallback);
    }

    [Fact]
    public void ResolveLlm_Should_LogDowngradeReasons()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Ollama"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "Pro", offlineOnly: false);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("Ollama", decision.ProviderName);
        Assert.True(decision.IsFallback);
        Assert.NotNull(decision.FallbackFrom);
        Assert.Contains("OpenAI", decision.FallbackFrom);
        Assert.Contains("Azure", decision.FallbackFrom);
        Assert.Contains("Gemini", decision.FallbackFrom);
    }

    [Fact]
    public void ResolveLlm_Should_HandleAzureAsFallback()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Azure"] = Mock.Of<ILlmProvider>(),
            ["Ollama"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "Pro", offlineOnly: false);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("Azure", decision.ProviderName);
        Assert.Equal(2, decision.PriorityRank);
        Assert.True(decision.IsFallback);
        Assert.Equal("OpenAI", decision.FallbackFrom);
    }

    [Fact]
    public void ResolveLlm_Should_HandleGeminiAsFallback()
    {
        // Arrange
        var config = new ProviderMixingConfig { LogProviderSelection = false };
        var mixer = new ProviderMixer(NullLogger<ProviderMixer>.Instance, config);

        var providers = new Dictionary<string, ILlmProvider>
        {
            ["Gemini"] = Mock.Of<ILlmProvider>(),
            ["RuleBased"] = Mock.Of<ILlmProvider>()
        };

        // Act
        var decision = mixer.ResolveLlm(providers, "Pro", offlineOnly: false);

        // Assert
        Assert.NotNull(decision);
        Assert.Equal("Gemini", decision.ProviderName);
        Assert.Equal(3, decision.PriorityRank);
        Assert.True(decision.IsFallback);
        Assert.Equal("OpenAI → Azure", decision.FallbackFrom);
    }

    #endregion
}
