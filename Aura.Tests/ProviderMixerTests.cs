using System.Collections.Generic;
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
        Assert.Equal("Pro TTS", selection.FallbackFrom);
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

        // Act - Should NOT throw, should return Windows as guaranteed fallback
        var selection = mixer.SelectTtsProvider(emptyProviders, "Pro");

        // Assert
        Assert.NotNull(selection);
        Assert.Equal("Windows", selection.SelectedProvider);
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
}
