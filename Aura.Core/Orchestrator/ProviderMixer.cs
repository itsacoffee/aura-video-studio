using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Manages provider selection and automatic fallback based on profiles and availability
/// </summary>
public class ProviderMixer
{
    private readonly ILogger<ProviderMixer> _logger;
    private readonly ProviderMixingConfig _config;

    public ProviderMixer(ILogger<ProviderMixer> logger, ProviderMixingConfig config)
    {
        _logger = logger;
        _config = config;
    }

    /// <summary>
    /// Selects the best available LLM provider based on profile and availability
    /// </summary>
    public ProviderSelection SelectLlmProvider(
        Dictionary<string, ILlmProvider> availableProviders,
        string preferredTier)
    {
        var stage = "Script";
        _logger.LogInformation("Selecting LLM provider for {Stage} stage (preferred: {Tier})", stage, preferredTier);

        // If specific provider is requested (not a tier), try to use it directly
        if (!string.IsNullOrWhiteSpace(preferredTier) && 
            preferredTier != "Pro" && 
            preferredTier != "ProIfAvailable" && 
            preferredTier != "Free")
        {
            // Normalize provider name
            var normalizedName = NormalizeProviderName(preferredTier);
            if (availableProviders.ContainsKey(normalizedName))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = normalizedName,
                    Reason = $"User-selected provider: {normalizedName}",
                    IsFallback = false
                };
            }
            
            _logger.LogWarning("Requested provider {Provider} not available, falling back to tier logic", preferredTier);
        }

        // Try Pro providers first if requested
        if (preferredTier == "Pro" || preferredTier == "ProIfAvailable")
        {
            if (availableProviders.ContainsKey("OpenAI"))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = "OpenAI",
                    Reason = "Pro provider available and preferred",
                    IsFallback = false
                };
            }

            if (availableProviders.ContainsKey("Azure"))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = "Azure",
                    Reason = "Pro provider available and preferred",
                    IsFallback = false
                };
            }

            if (availableProviders.ContainsKey("Gemini"))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = "Gemini",
                    Reason = "Pro provider available and preferred",
                    IsFallback = false
                };
            }

            // If ProIfAvailable and no Pro providers, fall back to free
            if (preferredTier == "ProIfAvailable")
            {
                _logger.LogInformation("No Pro LLM providers available, falling back to free");
            }
            else
            {
                _logger.LogWarning("Pro LLM provider requested but none available");
            }
        }

        // Try free providers
        if (availableProviders.ContainsKey("Ollama"))
        {
            bool isFallback = preferredTier == "Pro" || preferredTier == "ProIfAvailable";
            return new ProviderSelection
            {
                Stage = stage,
                SelectedProvider = "Ollama",
                Reason = "Local Ollama available",
                IsFallback = isFallback,
                FallbackFrom = isFallback ? "Pro LLM" : null
            };
        }

        if (availableProviders.ContainsKey("RuleBased"))
        {
            bool isFallback = preferredTier == "Pro" || preferredTier == "ProIfAvailable";
            return new ProviderSelection
            {
                Stage = stage,
                SelectedProvider = "RuleBased",
                Reason = "Free fallback - always available",
                IsFallback = isFallback,
                FallbackFrom = isFallback ? "Pro/Local LLM" : null
            };
        }

        // RuleBased provider is ALWAYS available as last resort - never throw
        _logger.LogWarning("No LLM providers in registry, returning RuleBased as guaranteed fallback");
        return new ProviderSelection
        {
            Stage = stage,
            SelectedProvider = "RuleBased",
            Reason = "RuleBased fallback - guaranteed always-available provider",
            IsFallback = true,
            FallbackFrom = "All providers"
        };
    }

    /// <summary>
    /// Selects the best available TTS provider based on profile and availability
    /// </summary>
    public ProviderSelection SelectTtsProvider(
        Dictionary<string, ITtsProvider> availableProviders,
        string preferredTier)
    {
        var stage = "TTS";
        _logger.LogInformation("Selecting TTS provider for {Stage} stage (preferred: {Tier})", stage, preferredTier);

        // If specific provider is requested (not a tier), try to use it directly
        if (!string.IsNullOrWhiteSpace(preferredTier) && 
            preferredTier != "Pro" && 
            preferredTier != "ProIfAvailable" && 
            preferredTier != "Free")
        {
            var normalizedName = NormalizeProviderName(preferredTier);
            if (availableProviders.ContainsKey(normalizedName))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = normalizedName,
                    Reason = $"User-selected provider: {normalizedName}",
                    IsFallback = false
                };
            }
            
            _logger.LogWarning("Requested TTS provider {Provider} not available, falling back to tier logic", preferredTier);
        }

        // Try Pro providers first if requested
        if (preferredTier == "Pro" || preferredTier == "ProIfAvailable")
        {
            if (availableProviders.ContainsKey("ElevenLabs"))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = "ElevenLabs",
                    Reason = "Pro provider available and preferred",
                    IsFallback = false
                };
            }

            if (availableProviders.ContainsKey("PlayHT"))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = "PlayHT",
                    Reason = "Pro provider available and preferred",
                    IsFallback = false
                };
            }

            // If ProIfAvailable and no Pro providers, fall back to local/free
            if (preferredTier == "ProIfAvailable")
            {
                _logger.LogInformation("No Pro TTS providers available, falling back to local/free TTS");
            }
            else
            {
                _logger.LogWarning("Pro TTS provider requested but none available");
            }
        }

        // Try local TTS providers (offline, high quality)
        if (availableProviders.ContainsKey("Mimic3"))
        {
            bool isFallback = preferredTier == "Pro";
            return new ProviderSelection
            {
                Stage = stage,
                SelectedProvider = "Mimic3",
                Reason = "Local Mimic3 TTS available (offline)",
                IsFallback = isFallback,
                FallbackFrom = isFallback ? "Pro TTS" : null
            };
        }

        if (availableProviders.ContainsKey("Piper"))
        {
            bool isFallback = preferredTier == "Pro";
            return new ProviderSelection
            {
                Stage = stage,
                SelectedProvider = "Piper",
                Reason = "Local Piper TTS available (offline, fast)",
                IsFallback = isFallback,
                FallbackFrom = isFallback ? "Pro TTS" : null
            };
        }

        // Fall back to Windows TTS (always available)
        if (availableProviders.ContainsKey("Windows"))
        {
            return new ProviderSelection
            {
                Stage = stage,
                SelectedProvider = "Windows",
                Reason = "Windows TTS - free and always available",
                IsFallback = preferredTier == "Pro" || preferredTier == "ProIfAvailable",
                FallbackFrom = (preferredTier == "Pro" || preferredTier == "ProIfAvailable") ? "Pro/Local TTS" : null
            };
        }

        // Windows TTS is ALWAYS available as last resort - never throw
        _logger.LogWarning("No TTS providers in registry, returning Windows as guaranteed fallback");
        return new ProviderSelection
        {
            Stage = stage,
            SelectedProvider = "Windows",
            Reason = "Windows TTS fallback - guaranteed always-available provider",
            IsFallback = true,
            FallbackFrom = "All TTS providers"
        };
    }

    /// <summary>
    /// Selects the best available image/visual provider based on profile and availability
    /// </summary>
    public ProviderSelection SelectVisualProvider(
        Dictionary<string, object> availableProviders,
        string preferredTier,
        bool isNvidiaGpu,
        int vramGB)
    {
        var stage = "Visuals";
        _logger.LogInformation("Selecting visual provider for {Stage} stage (preferred: {Tier})", stage, preferredTier);

        // If specific provider is requested (not a tier), try to use it directly
        if (!string.IsNullOrWhiteSpace(preferredTier) && 
            preferredTier != "Pro" && 
            preferredTier != "ProIfAvailable" && 
            preferredTier != "Free" &&
            preferredTier != "StockOrLocal")
        {
            var normalizedName = NormalizeProviderName(preferredTier);
            if (availableProviders.ContainsKey(normalizedName))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = normalizedName,
                    Reason = $"User-selected provider: {normalizedName}",
                    IsFallback = false
                };
            }
            
            _logger.LogWarning("Requested visual provider {Provider} not available, falling back to tier logic", preferredTier);
        }

        // Try Pro providers first if requested
        if (preferredTier == "Pro" || preferredTier == "ProIfAvailable" || preferredTier == "CloudPro")
        {
            if (availableProviders.ContainsKey("Stability"))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = "Stability",
                    Reason = "Pro provider available and preferred",
                    IsFallback = false
                };
            }

            if (availableProviders.ContainsKey("Runway"))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = "Runway",
                    Reason = "Pro provider available and preferred",
                    IsFallback = false
                };
            }

            if (preferredTier == "ProIfAvailable")
            {
                _logger.LogInformation("No Pro visual providers available, falling back to local/stock");
            }
        }

        // Try local SD if available (NVIDIA-only with sufficient VRAM)
        if (preferredTier == "StockOrLocal" || preferredTier == "ProIfAvailable")
        {
            if (isNvidiaGpu && vramGB >= 6 && availableProviders.ContainsKey("StableDiffusion"))
            {
                return new ProviderSelection
                {
                    Stage = stage,
                    SelectedProvider = "StableDiffusion",
                    Reason = $"Local SD available (NVIDIA GPU with {vramGB}GB VRAM)",
                    IsFallback = preferredTier == "Pro",
                    FallbackFrom = preferredTier == "Pro" ? "Pro Visual" : null
                };
            }
        }

        // Fall back to stock images
        if (availableProviders.ContainsKey("Stock"))
        {
            return new ProviderSelection
            {
                Stage = stage,
                SelectedProvider = "Stock",
                Reason = "Free stock images",
                IsFallback = preferredTier == "Pro" || preferredTier == "StockOrLocal",
                FallbackFrom = preferredTier != "Stock" ? "Pro/Local Visual" : null
            };
        }

        // Ultimate fallback: slideshow/solid colors
        return new ProviderSelection
        {
            Stage = stage,
            SelectedProvider = "Slideshow",
            Reason = "Fallback to slideshow (no other providers available)",
            IsFallback = true,
            FallbackFrom = "All visual providers"
        };
    }

    /// <summary>
    /// Logs a provider selection decision
    /// </summary>
    public void LogSelection(ProviderSelection selection)
    {
        if (!_config.LogProviderSelection)
            return;

        if (selection.IsFallback)
        {
            _logger.LogWarning(
                "[{Stage}] Provider: {Provider} (FALLBACK from {FallbackFrom}) - {Reason}",
                selection.Stage,
                selection.SelectedProvider,
                selection.FallbackFrom,
                selection.Reason
            );
        }
        else
        {
            _logger.LogInformation(
                "[{Stage}] Provider: {Provider} - {Reason}",
                selection.Stage,
                selection.SelectedProvider,
                selection.Reason
            );
        }
    }

    /// <summary>
    /// Normalizes provider names to match DI registration keys
    /// </summary>
    private static string NormalizeProviderName(string name)
    {
        return name switch
        {
            // LLM providers
            "RuleBased" or "rulebased" or "Rule-Based" or "rule-based" => "RuleBased",
            "Ollama" or "ollama" => "Ollama",
            "OpenAI" or "openai" or "OpenAi" => "OpenAI",
            "AzureOpenAI" or "Azure" or "azure" or "AzureOpenAi" or "azureopenai" => "Azure",
            "Gemini" or "gemini" => "Gemini",
            
            // TTS providers
            "Windows" or "windows" or "Windows SAPI" or "WindowsSAPI" or "SAPI" => "Windows",
            "ElevenLabs" or "elevenlabs" or "Eleven" or "eleven" => "ElevenLabs",
            "PlayHT" or "playht" or "Play.ht" or "PlayHt" => "PlayHT",
            "Piper" or "piper" => "Piper",
            "Mimic3" or "mimic3" or "Mimic" or "mimic" => "Mimic3",
            
            // Visual providers
            "Stock" or "stock" => "Stock",
            "LocalSD" or "localsd" or "StableDiffusion" or "stablediffusion" or "SD" => "StableDiffusion",
            "CloudPro" or "cloudpro" or "Cloud" or "cloud" => "CloudPro",
            "Stability" or "stability" or "StabilityAI" or "StabilityAi" => "Stability",
            "Runway" or "runway" or "RunwayML" or "RunwayMl" => "Runway",
            "Slideshow" or "slideshow" => "Slideshow",
            
            // Default: return as-is
            _ => name
        };
    }
}
