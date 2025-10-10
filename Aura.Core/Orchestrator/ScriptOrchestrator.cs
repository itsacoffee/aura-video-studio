using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Orchestrates script generation with provider routing and fallback logic
/// </summary>
public class ScriptOrchestrator
{
    private readonly ILogger<ScriptOrchestrator> _logger;
    private readonly ProviderMixer _providerMixer;
    private readonly Dictionary<string, ILlmProvider> _providers;

    public ScriptOrchestrator(
        ILogger<ScriptOrchestrator> logger,
        ProviderMixer providerMixer,
        Dictionary<string, ILlmProvider> providers)
    {
        _logger = logger;
        _providerMixer = providerMixer;
        _providers = providers;
    }

    /// <summary>
    /// Generate a script using the appropriate provider based on tier and availability
    /// </summary>
    public async Task<ScriptResult> GenerateScriptAsync(
        Brief brief, 
        PlanSpec spec, 
        string preferredTier,
        bool offlineOnly,
        CancellationToken ct)
    {
        _logger.LogInformation("Starting script generation for topic: {Topic}, preferredTier: {Tier}, offlineOnly: {OfflineOnly}", 
            brief.Topic, preferredTier, offlineOnly);

        // Block Pro providers if offline-only mode
        if (offlineOnly && (preferredTier == "Pro" || preferredTier == "ProIfAvailable"))
        {
            _logger.LogWarning("Pro providers requested but system is in OfflineOnly mode (E307)");
            
            if (preferredTier == "Pro")
            {
                return new ScriptResult
                {
                    Success = false,
                    ErrorCode = "E307",
                    ErrorMessage = "Pro LLM providers require internet connection but system is in OfflineOnly mode. Please disable OfflineOnly mode or use Free providers.",
                    Script = null,
                    ProviderUsed = null,
                    IsFallback = false
                };
            }
            
            // ProIfAvailable downgrades gracefully
            _logger.LogInformation("ProIfAvailable downgrading to Free providers due to OfflineOnly mode");
            preferredTier = "Free";
        }

        // Select provider
        var selection = _providerMixer.SelectLlmProvider(_providers, preferredTier);
        _providerMixer.LogSelection(selection);

        // Try primary provider
        var result = await TryGenerateWithProviderAsync(
            brief, 
            spec, 
            selection.SelectedProvider, 
            selection.IsFallback,
            ct).ConfigureAwait(false);

        if (result.Success)
        {
            return result;
        }

        // If primary provider failed, try fallback chain (always enabled for now)
        {
            _logger.LogWarning("Primary provider {Provider} failed, attempting fallback", selection.SelectedProvider);
            
            // Try Ollama if not already tried
            if (selection.SelectedProvider != "Ollama" && _providers.ContainsKey("Ollama"))
            {
                _logger.LogInformation("Falling back to Ollama");
                result = await TryGenerateWithProviderAsync(brief, spec, "Ollama", true, ct).ConfigureAwait(false);
                if (result.Success)
                {
                    return result;
                }
            }

            // Finally, fall back to RuleBased (always available)
            if (selection.SelectedProvider != "RuleBased" && _providers.ContainsKey("RuleBased"))
            {
                _logger.LogInformation("Falling back to RuleBased provider (final fallback)");
                result = await TryGenerateWithProviderAsync(brief, spec, "RuleBased", true, ct).ConfigureAwait(false);
                if (result.Success)
                {
                    return result;
                }
            }
        }

        // All providers failed
        return new ScriptResult
        {
            Success = false,
            ErrorCode = "E300",
            ErrorMessage = "All LLM providers failed to generate script",
            Script = null,
            ProviderUsed = null,
            IsFallback = false
        };
    }

    private async Task<ScriptResult> TryGenerateWithProviderAsync(
        Brief brief,
        PlanSpec spec,
        string providerName,
        bool isFallback,
        CancellationToken ct)
    {
        if (!_providers.TryGetValue(providerName, out var provider))
        {
            _logger.LogWarning("Provider {Provider} not available", providerName);
            return new ScriptResult
            {
                Success = false,
                ErrorCode = "E305",
                ErrorMessage = $"Provider {providerName} not available",
                Script = null,
                ProviderUsed = providerName,
                IsFallback = isFallback
            };
        }

        try
        {
            _logger.LogInformation("Attempting script generation with {Provider}", providerName);
            var script = await provider.DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(script))
            {
                _logger.LogWarning("Provider {Provider} returned empty script", providerName);
                return new ScriptResult
                {
                    Success = false,
                    ErrorCode = "E302",
                    ErrorMessage = $"Provider {providerName} returned empty script",
                    Script = null,
                    ProviderUsed = providerName,
                    IsFallback = isFallback
                };
            }

            _logger.LogInformation("Successfully generated script with {Provider} ({Length} chars)", 
                providerName, script.Length);

            return new ScriptResult
            {
                Success = true,
                ErrorCode = null,
                ErrorMessage = null,
                Script = script,
                ProviderUsed = providerName,
                IsFallback = isFallback
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider {Provider} failed: {Message}", providerName, ex.Message);
            return new ScriptResult
            {
                Success = false,
                ErrorCode = "E300",
                ErrorMessage = $"Provider {providerName} failed: {ex.Message}",
                Script = null,
                ProviderUsed = providerName,
                IsFallback = isFallback
            };
        }
    }
}

/// <summary>
/// Result of script generation attempt
/// 
/// Error Codes:
/// - E300: General script provider failure
/// - E301: Request timeout or cancellation
/// - E302: Provider returned empty/invalid script
/// - E303: Invalid enum value or input validation failure
/// - E304: Invalid plan parameters (duration, etc.)
/// - E305: Provider not available/not registered
/// - E307: Offline mode restriction (Pro providers blocked)
/// </summary>
public record ScriptResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Script { get; init; }
    public string? ProviderUsed { get; init; }
    public bool IsFallback { get; init; }
}
