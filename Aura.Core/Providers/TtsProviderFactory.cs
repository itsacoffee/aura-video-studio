using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Aura.Core.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Providers;

/// <summary>
/// Factory for creating and managing TTS providers based on configuration.
/// Handles provider selection, API key validation, and fallback logic.
/// </summary>
public class TtsProviderFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ProviderSettings _providerSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    public TtsProviderFactory(
        ILoggerFactory loggerFactory,
        ProviderSettings providerSettings,
        IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _providerSettings = providerSettings;
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Creates all available TTS providers based on configuration.
    /// </summary>
    public Dictionary<string, ITtsProvider> CreateAvailableProviders()
    {
        var providers = new Dictionary<string, ITtsProvider>();
        bool offlineOnly = _providerSettings.IsOfflineOnly();

        // Always add Windows TTS if available (platform-dependent)
        try
        {
            var windowsProvider = CreateWindowsProvider();
            if (windowsProvider != null)
            {
                providers["Windows"] = windowsProvider;
            }
        }
        catch (Exception)
        {
            // Windows TTS not available on this platform
        }

        // Add Mock provider for testing/CI
        providers["Mock"] = CreateMockProvider();

        // Add Pro providers if not in offline mode
        if (!offlineOnly)
        {
            var elevenLabsKey = _providerSettings.GetElevenLabsApiKey();
            if (!string.IsNullOrEmpty(elevenLabsKey))
            {
                providers["ElevenLabs"] = CreateElevenLabsProvider(elevenLabsKey, offlineOnly);
            }

            var playHTKey = _providerSettings.GetPlayHTApiKey();
            var playHTUserId = _providerSettings.GetPlayHTUserId();
            if (!string.IsNullOrEmpty(playHTKey) && !string.IsNullOrEmpty(playHTUserId))
            {
                providers["PlayHT"] = CreatePlayHTProvider(playHTKey, playHTUserId, offlineOnly);
            }
        }

        return providers;
    }

    /// <summary>
    /// Gets the default TTS provider based on configuration and availability.
    /// </summary>
    public ITtsProvider GetDefaultProvider()
    {
        var providers = CreateAvailableProviders();

        // Try Pro providers first if available
        if (providers.ContainsKey("ElevenLabs"))
        {
            return providers["ElevenLabs"];
        }

        if (providers.ContainsKey("PlayHT"))
        {
            return providers["PlayHT"];
        }

        // Fall back to Windows TTS
        if (providers.ContainsKey("Windows"))
        {
            return providers["Windows"];
        }

        // Last resort: Mock provider
        return providers["Mock"];
    }

    private ITtsProvider? CreateWindowsProvider()
    {
        // Use reflection to check if the Windows TTS provider is available
        // This avoids compile-time dependency on Windows-specific APIs
        var assembly = typeof(ITtsProvider).Assembly;
        var providerType = assembly.GetType("Aura.Providers.Tts.WindowsTtsProvider");
        
        if (providerType == null)
        {
            // Try loading from Aura.Providers assembly
            var providersAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Aura.Providers");
            
            if (providersAssembly != null)
            {
                providerType = providersAssembly.GetType("Aura.Providers.Tts.WindowsTtsProvider");
            }
        }

        if (providerType != null)
        {
            var logger = _loggerFactory.CreateLogger(providerType);
            return Activator.CreateInstance(providerType, logger) as ITtsProvider;
        }

        return null;
    }

    private ITtsProvider CreateMockProvider()
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Aura.Providers");
        
        if (assembly != null)
        {
            var providerType = assembly.GetType("Aura.Providers.Tts.MockTtsProvider");
            if (providerType != null)
            {
                var logger = _loggerFactory.CreateLogger(providerType);
                return (ITtsProvider)Activator.CreateInstance(providerType, logger)!;
            }
        }

        throw new InvalidOperationException("MockTtsProvider not found");
    }

    private ITtsProvider CreateElevenLabsProvider(string apiKey, bool offlineOnly)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Aura.Providers");
        
        if (assembly != null)
        {
            var providerType = assembly.GetType("Aura.Providers.Tts.ElevenLabsTtsProvider");
            if (providerType != null)
            {
                var logger = _loggerFactory.CreateLogger(providerType);
                var httpClient = _httpClientFactory.CreateClient();
                return (ITtsProvider)Activator.CreateInstance(providerType, logger, httpClient, apiKey, offlineOnly)!;
            }
        }

        throw new InvalidOperationException("ElevenLabsTtsProvider not found");
    }

    private ITtsProvider CreatePlayHTProvider(string apiKey, string userId, bool offlineOnly)
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Aura.Providers");
        
        if (assembly != null)
        {
            var providerType = assembly.GetType("Aura.Providers.Tts.PlayHTTtsProvider");
            if (providerType != null)
            {
                var logger = _loggerFactory.CreateLogger(providerType);
                var httpClient = _httpClientFactory.CreateClient();
                return (ITtsProvider)Activator.CreateInstance(providerType, logger, httpClient, apiKey, userId, offlineOnly)!;
            }
        }

        throw new InvalidOperationException("PlayHTTtsProvider not found");
    }
}
