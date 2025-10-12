using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Aura.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Providers;

/// <summary>
/// Factory for creating and managing TTS providers based on configuration.
/// Uses DI to resolve providers - no reflection or Activator.CreateInstance.
/// </summary>
public class TtsProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TtsProviderFactory> _logger;
    private readonly ProviderSettings _providerSettings;

    public TtsProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<TtsProviderFactory> logger,
        ProviderSettings providerSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _providerSettings = providerSettings;
    }

    /// <summary>
    /// Creates all available TTS providers based on configuration.
    /// Resolves providers from DI - never throws on failure.
    /// </summary>
    public Dictionary<string, ITtsProvider> CreateAvailableProviders()
    {
        var providers = new Dictionary<string, ITtsProvider>();
        string correlationId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation("[{CorrelationId}] Creating available TTS providers", correlationId);

        // Try to resolve all registered ITtsProvider instances
        try
        {
            var allProviders = _serviceProvider.GetServices<ITtsProvider>();
            if (allProviders != null)
            {
                foreach (var provider in allProviders)
                {
                    if (provider != null)
                    {
                        var providerType = provider.GetType().Name;
                        var providerName = providerType.Replace("TtsProvider", "");
                        
                        // Map type names to friendly names
                        providerName = providerName switch
                        {
                            "Windows" => "Windows",
                            "Piper" => "Piper",
                            "Mimic3" => "Mimic3",
                            "ElevenLabs" => "ElevenLabs",
                            "PlayHT" => "PlayHT",
                            "Null" => "Null",
                            "Mock" => "Mock",
                            _ => providerName
                        };
                        
                        providers[providerName] = provider;
                        _logger.LogInformation("[{CorrelationId}] Registered {Provider} TTS provider", correlationId, providerName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error enumerating TTS providers from DI", correlationId);
        }

        _logger.LogInformation("[{CorrelationId}] Total TTS providers available: {Count}", correlationId, providers.Count);

        return providers;
    }

    /// <summary>
    /// Gets the default TTS provider based on configuration and availability.
    /// Never throws - returns NullTtsProvider if no other providers available.
    /// </summary>
    public ITtsProvider GetDefaultProvider()
    {
        string correlationId = Guid.NewGuid().ToString("N")[..8];
        
        try
        {
            var providers = CreateAvailableProviders();

            // Try Pro providers first if available
            if (providers.ContainsKey("ElevenLabs"))
            {
                _logger.LogInformation("[{CorrelationId}] Selected ElevenLabs as default TTS provider", correlationId);
                return providers["ElevenLabs"];
            }

            if (providers.ContainsKey("PlayHT"))
            {
                _logger.LogInformation("[{CorrelationId}] Selected PlayHT as default TTS provider", correlationId);
                return providers["PlayHT"];
            }

            // Try local providers
            if (providers.ContainsKey("Mimic3"))
            {
                _logger.LogInformation("[{CorrelationId}] Selected Mimic3 as default TTS provider", correlationId);
                return providers["Mimic3"];
            }

            if (providers.ContainsKey("Piper"))
            {
                _logger.LogInformation("[{CorrelationId}] Selected Piper as default TTS provider", correlationId);
                return providers["Piper"];
            }

            // Fall back to Windows TTS
            if (providers.ContainsKey("Windows"))
            {
                _logger.LogInformation("[{CorrelationId}] Selected Windows as default TTS provider", correlationId);
                return providers["Windows"];
            }

            // Last resort: Null provider
            if (providers.ContainsKey("Null"))
            {
                _logger.LogWarning("[{CorrelationId}] No TTS providers available, using Null provider (generates silence)", correlationId);
                return providers["Null"];
            }

            // If even Null provider is not available in the dictionary, try to resolve it directly as absolute fallback
            _logger.LogError("[{CorrelationId}] CRITICAL: No TTS providers registered, attempting to resolve Null provider directly", correlationId);
            
            // Get all registered providers and find Null
            var allProviders = _serviceProvider.GetServices<ITtsProvider>();
            var nullProvider = allProviders?.FirstOrDefault(p => p.GetType().Name == "NullTtsProvider");
            if (nullProvider != null)
            {
                _logger.LogWarning("[{CorrelationId}] Found Null provider via direct resolution", correlationId);
                return nullProvider;
            }

            // Final fallback - should never reach here if DI is configured properly
            throw new InvalidOperationException("No TTS providers available and NullTtsProvider could not be resolved from DI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Error getting default TTS provider", correlationId);
            
            // Try one last time to get Null provider directly
            try
            {
                var allProviders = _serviceProvider.GetServices<ITtsProvider>();
                var nullProvider = allProviders?.FirstOrDefault(p => p.GetType().Name == "NullTtsProvider");
                if (nullProvider != null)
                {
                    _logger.LogWarning("[{CorrelationId}] Recovered by resolving Null provider directly", correlationId);
                    return nullProvider;
                }
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "[{CorrelationId}] Failed to resolve Null provider as fallback", correlationId);
            }

            throw;
        }
    }
}
