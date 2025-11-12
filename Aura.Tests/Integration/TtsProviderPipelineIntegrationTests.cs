using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Providers;
using Aura.Providers.Tts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for TTS provider pipeline including factory, registration, and fallback behavior.
/// These tests validate the complete PR-PROVIDER-002 implementation.
/// </summary>
public class TtsProviderPipelineIntegrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TtsProviderFactory _factory;

    public TtsProviderPipelineIntegrationTests()
    {
        var services = new ServiceCollection();
        
        // Register logging
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        
        // Register HTTP client factory
        services.AddHttpClient();
        
        // Register configuration and provider settings
        services.AddSingleton<ProviderSettings>();
        
        // Register audio services required by TTS providers
        services.AddSingleton<Aura.Core.Audio.WavValidator>();
        services.AddSingleton<Aura.Core.Audio.SilentWavGenerator>();
        
        // Register all TTS providers using the actual registration method
        services.AddTtsProviders();
        
        // Register the factory
        services.AddSingleton<TtsProviderFactory>();
        
        _serviceProvider = services.BuildServiceProvider();
        _factory = _serviceProvider.GetRequiredService<TtsProviderFactory>();
    }

    [Fact]
    public void TtsProviderFactory_ShouldBeRegistered()
    {
        Assert.NotNull(_factory);
    }

    [Fact]
    public void CreateAvailableProviders_ShouldReturnAtLeastNullProvider()
    {
        var providers = _factory.CreateAvailableProviders();
        
        Assert.NotNull(providers);
        Assert.NotEmpty(providers);
        Assert.Contains("Null", providers.Keys);
    }

    [Fact]
    public void CreateAvailableProviders_ShouldIncludeWindowsProviderOnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var providers = _factory.CreateAvailableProviders();
        
        Assert.Contains("Windows", providers.Keys);
        Assert.IsAssignableFrom<ITtsProvider>(providers["Windows"]);
    }

    [Fact]
    public void GetDefaultProvider_ShouldNeverReturnNull()
    {
        var defaultProvider = _factory.GetDefaultProvider();
        
        Assert.NotNull(defaultProvider);
        Assert.IsAssignableFrom<ITtsProvider>(defaultProvider);
    }

    [Fact]
    public void GetDefaultProvider_ShouldPreferBetterProvidersOverNull()
    {
        var defaultProvider = _factory.GetDefaultProvider();
        
        // On Windows, should prefer Windows TTS over Null
        if (OperatingSystem.IsWindows())
        {
            Assert.IsNotType<NullTtsProvider>(defaultProvider);
        }
        else
        {
            // On non-Windows with no API keys, should fall back to Null
            Assert.IsType<NullTtsProvider>(defaultProvider);
        }
    }

    [Fact]
    public void TryCreateProvider_WithValidName_ShouldReturnProvider()
    {
        var provider = _factory.TryCreateProvider("Null");
        
        Assert.NotNull(provider);
        Assert.IsType<NullTtsProvider>(provider);
    }

    [Fact]
    public void TryCreateProvider_WithInvalidName_ShouldReturnNull()
    {
        var provider = _factory.TryCreateProvider("NonExistentProvider");
        
        Assert.Null(provider);
    }

    [Fact]
    public async Task NullProvider_ShouldAlwaysBeAvailable()
    {
        var provider = _factory.TryCreateProvider("Null");
        Assert.NotNull(provider);
        
        var voices = await provider.GetAvailableVoicesAsync();
        Assert.NotNull(voices);
    }

    [Fact]
    public async Task NullProvider_ShouldGenerateSilentAudio()
    {
        var provider = _factory.TryCreateProvider("Null");
        Assert.NotNull(provider);
        
        var scriptLine = new ScriptLine(
            SceneIndex: 0,
            Text: "Test narration",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(2)
        );
        
        var voiceSpec = new VoiceSpec(
            VoiceName: "default",
            Rate: 1.0,
            Pitch: 0.0,
            Pause: PauseStyle.Natural
        );
        
        var audioPath = await provider.SynthesizeAsync(new[] { scriptLine }, voiceSpec, CancellationToken.None);
        
        Assert.NotNull(audioPath);
        Assert.NotEmpty(audioPath);
    }

    [Fact]
    public async Task WindowsProvider_OnWindows_ShouldHaveVoices()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var provider = _factory.TryCreateProvider("Windows");
        Assert.NotNull(provider);
        
        var voices = await provider.GetAvailableVoicesAsync();
        
        Assert.NotNull(voices);
        Assert.NotEmpty(voices);
    }

    [Fact]
    public void AllRegisteredProviders_ShouldBeResolvable()
    {
        var providers = _factory.CreateAvailableProviders();
        
        foreach (var (name, provider) in providers)
        {
            Assert.NotNull(provider);
            Assert.IsAssignableFrom<ITtsProvider>(provider);
            
            // Verify provider name mapping works correctly
            Assert.DoesNotContain("TtsProvider", name);
        }
    }

    [Fact]
    public void ProviderNaming_ShouldRemoveTtsProviderSuffix()
    {
        var providers = _factory.CreateAvailableProviders();
        
        foreach (var name in providers.Keys)
        {
            Assert.DoesNotContain("TtsProvider", name);
            Assert.DoesNotContain("Provider", name);
        }
    }

    [Fact]
    public void ProviderRegistration_ShouldNotIncludeMockProviders()
    {
        var providers = _factory.CreateAvailableProviders();
        
        // MockTtsProvider should be excluded from production
        Assert.DoesNotContain("Mock", providers.Keys);
        Assert.DoesNotContain("MockTts", providers.Keys);
    }

    [Fact]
    public void FallbackChain_ShouldFollowCorrectPriority()
    {
        var providers = _factory.CreateAvailableProviders();
        var defaultProvider = _factory.GetDefaultProvider();
        
        Assert.NotNull(defaultProvider);
        
        // Verify fallback priority: 
        // ElevenLabs > PlayHT > Azure > Mimic3 > Piper > Windows > Null
        if (providers.ContainsKey("ElevenLabs"))
        {
            Assert.Equal("ElevenLabsTtsProvider", defaultProvider.GetType().Name);
        }
        else if (providers.ContainsKey("PlayHT"))
        {
            Assert.Equal("PlayHTTtsProvider", defaultProvider.GetType().Name);
        }
        else if (providers.ContainsKey("Azure"))
        {
            Assert.Equal("AzureTtsProvider", defaultProvider.GetType().Name);
        }
        else if (providers.ContainsKey("Mimic3"))
        {
            Assert.Equal("Mimic3TtsProvider", defaultProvider.GetType().Name);
        }
        else if (providers.ContainsKey("Piper"))
        {
            Assert.Equal("PiperTtsProvider", defaultProvider.GetType().Name);
        }
        else if (providers.ContainsKey("Windows"))
        {
            Assert.Equal("WindowsTtsProvider", defaultProvider.GetType().Name);
        }
        else
        {
            Assert.Equal("NullTtsProvider", defaultProvider.GetType().Name);
        }
    }

    [Fact]
    public async Task MultipleProviders_ShouldAllHaveGetAvailableVoicesAsync()
    {
        var providers = _factory.CreateAvailableProviders();
        
        foreach (var (name, provider) in providers)
        {
            var exception = await Record.ExceptionAsync(async () =>
            {
                var voices = await provider.GetAvailableVoicesAsync();
                Assert.NotNull(voices);
            });
            
            Assert.Null(exception);
        }
    }

    [Fact]
    public void ServiceCollection_ShouldRegisterAllRequiredServices()
    {
        // Verify all required services are registered
        Assert.NotNull(_serviceProvider.GetService<ProviderSettings>());
        Assert.NotNull(_serviceProvider.GetService<Aura.Core.Audio.WavValidator>());
        Assert.NotNull(_serviceProvider.GetService<Aura.Core.Audio.SilentWavGenerator>());
        Assert.NotNull(_serviceProvider.GetService<TtsProviderFactory>());
        
        // Verify at least one ITtsProvider is registered
        var ttsProviders = _serviceProvider.GetServices<ITtsProvider>();
        Assert.NotEmpty(ttsProviders);
    }

    [Fact]
    public void ProviderSettings_ShouldBeConfigurable()
    {
        var settings = _serviceProvider.GetRequiredService<ProviderSettings>();
        Assert.NotNull(settings);
        
        // Verify settings can be accessed without exceptions
        var exception = Record.Exception(() =>
        {
            var _ = settings.IsOfflineOnly();
            var __ = settings.GetFfmpegPath();
        });
        
        Assert.Null(exception);
    }

    [Fact]
    public async Task SynthesizeAsync_ShouldAcceptCancellationToken()
    {
        var provider = _factory.GetDefaultProvider();
        var cts = new CancellationTokenSource();
        
        var scriptLine = new ScriptLine(
            SceneIndex: 0,
            Text: "Test",
            Start: TimeSpan.Zero,
            Duration: TimeSpan.FromSeconds(1)
        );
        
        var voiceSpec = new VoiceSpec(
            VoiceName: "default",
            Rate: 1.0,
            Pitch: 0.0,
            Pause: PauseStyle.Natural
        );
        
        cts.Cancel();
        
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await provider.SynthesizeAsync(new[] { scriptLine }, voiceSpec, cts.Token);
        });
    }
}
