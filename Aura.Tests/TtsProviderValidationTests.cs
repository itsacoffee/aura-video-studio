using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Aura.Core.Models;
using Aura.Providers.Tts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for TTS provider validation, API key handling, and offline mode enforcement
/// </summary>
public class TtsProviderValidationTests
{
    #region ElevenLabs Tests

    [Fact]
    public void ElevenLabsProvider_Constructor_Should_LogWarning_WhenApiKeyMissing()
    {
        // Arrange & Act
        var provider = new ElevenLabsTtsProvider(
            NullLogger<ElevenLabsTtsProvider>.Instance,
            new HttpClient(),
            apiKey: null,
            offlineOnly: false);

        // Assert - No exception should be thrown
        Assert.NotNull(provider);
    }

    [Fact]
    public void ElevenLabsProvider_Constructor_Should_LogInfo_WhenOfflineMode()
    {
        // Arrange & Act
        var provider = new ElevenLabsTtsProvider(
            NullLogger<ElevenLabsTtsProvider>.Instance,
            new HttpClient(),
            apiKey: "test-key",
            offlineOnly: true);

        // Assert - No exception should be thrown
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task ElevenLabsProvider_SynthesizeAsync_Should_Throw_WhenOfflineMode()
    {
        // Arrange
        var provider = new ElevenLabsTtsProvider(
            NullLogger<ElevenLabsTtsProvider>.Instance,
            new HttpClient(),
            apiKey: "test-key",
            offlineOnly: true);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
        
        Assert.Contains("offline mode", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Piper", ex.Message);
    }

    [Fact]
    public async Task ElevenLabsProvider_SynthesizeAsync_Should_Throw_WhenApiKeyMissing()
    {
        // Arrange
        var provider = new ElevenLabsTtsProvider(
            NullLogger<ElevenLabsTtsProvider>.Instance,
            new HttpClient(),
            apiKey: null,
            offlineOnly: false);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
        
        Assert.Contains("API key", ex.Message);
        Assert.Contains("settings", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region PlayHT Tests

    [Fact]
    public void PlayHTProvider_Constructor_Should_LogWarning_WhenCredentialsMissing()
    {
        // Arrange & Act - Missing both
        var provider1 = new PlayHTTtsProvider(
            NullLogger<PlayHTTtsProvider>.Instance,
            new HttpClient(),
            apiKey: null,
            userId: null,
            offlineOnly: false);

        // Missing API key
        var provider2 = new PlayHTTtsProvider(
            NullLogger<PlayHTTtsProvider>.Instance,
            new HttpClient(),
            apiKey: null,
            userId: "test-user",
            offlineOnly: false);

        // Missing User ID
        var provider3 = new PlayHTTtsProvider(
            NullLogger<PlayHTTtsProvider>.Instance,
            new HttpClient(),
            apiKey: "test-key",
            userId: null,
            offlineOnly: false);

        // Assert - No exception should be thrown
        Assert.NotNull(provider1);
        Assert.NotNull(provider2);
        Assert.NotNull(provider3);
    }

    [Fact]
    public async Task PlayHTProvider_SynthesizeAsync_Should_Throw_WhenOfflineMode()
    {
        // Arrange
        var provider = new PlayHTTtsProvider(
            NullLogger<PlayHTTtsProvider>.Instance,
            new HttpClient(),
            apiKey: "test-key",
            userId: "test-user",
            offlineOnly: true);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
        
        Assert.Contains("offline mode", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PlayHTProvider_SynthesizeAsync_Should_Throw_WhenCredentialsMissing()
    {
        // Arrange
        var provider = new PlayHTTtsProvider(
            NullLogger<PlayHTTtsProvider>.Instance,
            new HttpClient(),
            apiKey: null,
            userId: "test-user",
            offlineOnly: false);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
        
        Assert.Contains("API key", ex.Message);
        Assert.Contains("User ID", ex.Message);
    }

    #endregion

    #region Azure Tests

    [Fact]
    public void AzureProvider_Constructor_Should_LogWarning_WhenApiKeyMissing()
    {
        // Arrange & Act
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            apiKey: null,
            region: "eastus",
            offlineOnly: false);

        // Assert - No exception should be thrown
        Assert.NotNull(provider);
    }

    [Fact]
    public void AzureProvider_Constructor_Should_UseDefaultRegion_WhenRegionEmpty()
    {
        // Arrange & Act
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            apiKey: "test-key",
            region: "",
            offlineOnly: false);

        // Assert - Should not throw, will use default region
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task AzureProvider_SynthesizeAsync_Should_Throw_WhenOfflineMode()
    {
        // Arrange
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            apiKey: "test-key",
            region: "eastus",
            offlineOnly: true);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
        
        Assert.Contains("offline mode", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AzureProvider_SynthesizeAsync_Should_Throw_WhenApiKeyMissing()
    {
        // Arrange
        var provider = new AzureTtsProvider(
            NullLogger<AzureTtsProvider>.Instance,
            apiKey: null,
            region: "eastus",
            offlineOnly: false);

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
        
        Assert.Contains("API key", ex.Message);
    }

    #endregion

    #region Piper Tests

    [Fact]
    public void PiperProvider_Constructor_Should_LogWarning_WhenExecutableMissing()
    {
        // Arrange & Act
        var provider = new PiperTtsProvider(
            NullLogger<PiperTtsProvider>.Instance,
            new SilentWavGenerator(NullLogger<SilentWavGenerator>.Instance),
            new WavValidator(NullLogger<WavValidator>.Instance),
            piperExecutable: "/nonexistent/piper",
            voiceModelPath: "/nonexistent/model.onnx");

        // Assert - No exception should be thrown in constructor
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task PiperProvider_SynthesizeAsync_Should_Throw_WhenExecutableMissing()
    {
        // Arrange
        var provider = new PiperTtsProvider(
            NullLogger<PiperTtsProvider>.Instance,
            new SilentWavGenerator(NullLogger<SilentWavGenerator>.Instance),
            new WavValidator(NullLogger<WavValidator>.Instance),
            piperExecutable: "/nonexistent/piper",
            voiceModelPath: "/nonexistent/model.onnx");

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FileNotFoundException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
        
        Assert.Contains("Piper executable", ex.Message);
        Assert.Contains("github.com/rhasspy/piper", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Mimic3 Tests

    [Fact]
    public void Mimic3Provider_Constructor_Should_LogWarning_WhenServerUnreachable()
    {
        // Arrange & Act - Use an unreachable URL
        var provider = new Mimic3TtsProvider(
            NullLogger<Mimic3TtsProvider>.Instance,
            new HttpClient(),
            new SilentWavGenerator(NullLogger<SilentWavGenerator>.Instance),
            new WavValidator(NullLogger<WavValidator>.Instance),
            baseUrl: "http://localhost:99999");

        // Assert - No exception should be thrown in constructor
        Assert.NotNull(provider);
    }

    [Fact]
    public async Task Mimic3Provider_SynthesizeAsync_Should_Throw_WhenServerUnreachable()
    {
        // Arrange
        var provider = new Mimic3TtsProvider(
            NullLogger<Mimic3TtsProvider>.Instance,
            new HttpClient(),
            new SilentWavGenerator(NullLogger<SilentWavGenerator>.Instance),
            new WavValidator(NullLogger<WavValidator>.Instance),
            baseUrl: "http://localhost:99999");

        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("TestVoice", 1.0, 0.0, PauseStyle.Natural);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None));
        
        Assert.Contains("not reachable", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docker run", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region TtsProviderFactory Tests

    [Fact]
    public void TtsProviderFactory_TryCreateProvider_Should_ReturnNull_WhenProviderNotFound()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<Aura.Core.Providers.TtsProviderFactory>>(
            NullLogger<Aura.Core.Providers.TtsProviderFactory>.Instance);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<Aura.Core.Configuration.ProviderSettings>>(
            NullLogger<Aura.Core.Configuration.ProviderSettings>.Instance);
        services.AddSingleton<Aura.Core.Configuration.ProviderSettings>();
        services.AddSingleton<Aura.Core.Providers.TtsProviderFactory>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<Aura.Core.Providers.TtsProviderFactory>();

        // Act
        var provider = factory.TryCreateProvider("NonExistentProvider");

        // Assert
        Assert.Null(provider);
    }

    [Fact]
    public void TtsProviderFactory_GetDefaultProvider_Should_PrioritizeCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<Aura.Core.Providers.TtsProviderFactory>>(
            NullLogger<Aura.Core.Providers.TtsProviderFactory>.Instance);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<NullTtsProvider>>(
            NullLogger<NullTtsProvider>.Instance);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<SilentWavGenerator>>(
            NullLogger<SilentWavGenerator>.Instance);
        services.AddSingleton<Microsoft.Extensions.Logging.ILogger<Aura.Core.Configuration.ProviderSettings>>(
            NullLogger<Aura.Core.Configuration.ProviderSettings>.Instance);
        services.AddSingleton<Aura.Core.Configuration.ProviderSettings>();
        services.AddSingleton<SilentWavGenerator>();
        services.AddSingleton<Aura.Core.Providers.TtsProviderFactory>();
        
        // Register only NullTtsProvider
        services.AddSingleton<Aura.Core.Providers.ITtsProvider, NullTtsProvider>();
        
        var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<Aura.Core.Providers.TtsProviderFactory>();

        // Act
        var provider = factory.GetDefaultProvider();

        // Assert
        Assert.NotNull(provider);
        Assert.IsType<NullTtsProvider>(provider);
    }

    #endregion
}
