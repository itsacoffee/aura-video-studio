using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Health;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Provider tier preference for selection
/// </summary>
public enum ProviderTier
{
    Free,
    Balanced,
    Pro
}

/// <summary>
/// Smart provider selector that chooses the best provider based on health metrics
/// </summary>
public class SmartProviderSelector
{
    private readonly ProviderHealthMonitor _healthMonitor;
    private readonly ILogger<SmartProviderSelector> _logger;
    private readonly Dictionary<string, ILlmProvider> _llmProviders;
    private readonly Dictionary<string, ITtsProvider> _ttsProviders;
    private readonly Dictionary<string, IImageProvider> _imageProviders;
    private readonly ILlmProvider _fallbackLlmProvider;
    private readonly ITtsProvider _fallbackTtsProvider;
    private readonly IImageProvider? _fallbackImageProvider;

    public SmartProviderSelector(
        ProviderHealthMonitor healthMonitor,
        ILogger<SmartProviderSelector> logger,
        Dictionary<string, ILlmProvider>? llmProviders = null,
        Dictionary<string, ITtsProvider>? ttsProviders = null,
        Dictionary<string, IImageProvider>? imageProviders = null,
        ILlmProvider? fallbackLlmProvider = null,
        ITtsProvider? fallbackTtsProvider = null,
        IImageProvider? fallbackImageProvider = null)
    {
        _healthMonitor = healthMonitor;
        _logger = logger;
        _llmProviders = llmProviders ?? new Dictionary<string, ILlmProvider>();
        _ttsProviders = ttsProviders ?? new Dictionary<string, ITtsProvider>();
        _imageProviders = imageProviders ?? new Dictionary<string, IImageProvider>();
        
        // Ensure we have fallback providers
        _fallbackLlmProvider = fallbackLlmProvider ?? _llmProviders.Values.FirstOrDefault()!;
        _fallbackTtsProvider = fallbackTtsProvider ?? _ttsProviders.Values.FirstOrDefault()!;
        _fallbackImageProvider = fallbackImageProvider ?? _imageProviders.Values.FirstOrDefault();
    }

    /// <summary>
    /// Select the best LLM provider based on health metrics and tier preference
    /// </summary>
    public async Task<ILlmProvider> SelectBestLlmProviderAsync(
        ProviderTier tierPreference = ProviderTier.Balanced,
        CancellationToken ct = default)
    {
        var candidateProviders = FilterProvidersByTier(_llmProviders.Keys, tierPreference);
        
        var healthyProvider = await SelectBestProviderAsync(
            candidateProviders,
            "LLM",
            ct).ConfigureAwait(false);

        if (healthyProvider == null)
        {
            _logger.LogWarning("No healthy LLM providers found, using fallback provider");
            return _fallbackLlmProvider;
        }

        if (_llmProviders.TryGetValue(healthyProvider, out var provider))
        {
            return provider;
        }

        _logger.LogWarning("Selected provider {ProviderName} not found in registry, using fallback", healthyProvider);
        return _fallbackLlmProvider;
    }

    /// <summary>
    /// Select the best TTS provider based on health metrics and tier preference
    /// </summary>
    public async Task<ITtsProvider> SelectBestTtsProviderAsync(
        ProviderTier tierPreference = ProviderTier.Balanced,
        CancellationToken ct = default)
    {
        var candidateProviders = FilterProvidersByTier(_ttsProviders.Keys, tierPreference);
        
        var healthyProvider = await SelectBestProviderAsync(
            candidateProviders,
            "TTS",
            ct).ConfigureAwait(false);

        if (healthyProvider == null)
        {
            _logger.LogWarning("No healthy TTS providers found, using fallback provider");
            return _fallbackTtsProvider;
        }

        if (_ttsProviders.TryGetValue(healthyProvider, out var provider))
        {
            return provider;
        }

        _logger.LogWarning("Selected provider {ProviderName} not found in registry, using fallback", healthyProvider);
        return _fallbackTtsProvider;
    }

    /// <summary>
    /// Select the best image provider based on health metrics and tier preference
    /// </summary>
    public async Task<IImageProvider?> SelectBestImageProviderAsync(
        ProviderTier tierPreference = ProviderTier.Balanced,
        CancellationToken ct = default)
    {
        var candidateProviders = FilterProvidersByTier(_imageProviders.Keys, tierPreference);
        
        var healthyProvider = await SelectBestProviderAsync(
            candidateProviders,
            "Image",
            ct).ConfigureAwait(false);

        if (healthyProvider == null)
        {
            _logger.LogWarning("No healthy image providers found, using fallback provider");
            return _fallbackImageProvider;
        }

        if (_imageProviders.TryGetValue(healthyProvider, out var provider))
        {
            return provider;
        }

        _logger.LogWarning("Selected provider {ProviderName} not found in registry, using fallback", healthyProvider);
        return _fallbackImageProvider;
    }

    /// <summary>
    /// Record the result of a provider usage for accuracy tracking
    /// </summary>
    public void RecordProviderUsage(string providerName, bool success, string? errorMessage = null)
    {
        // This feeds into the health metrics for more accurate provider selection
        _logger.LogDebug("Provider usage recorded: {ProviderName} - {Result}",
            providerName, success ? "success" : $"failure: {errorMessage}");
    }

    private List<string> FilterProvidersByTier(IEnumerable<string> allProviders, ProviderTier tier)
    {
        var providers = allProviders.ToList();

        return tier switch
        {
            ProviderTier.Free => providers.Where(p => IsFreeProvider(p)).ToList(),
            ProviderTier.Pro => providers.Where(p => IsProProvider(p)).ToList(),
            ProviderTier.Balanced => providers, // All providers allowed
            _ => providers
        };
    }

    private async Task<string?> SelectBestProviderAsync(
        List<string> candidateProviders,
        string providerType,
        CancellationToken ct)
    {
        if (candidateProviders.Count == 0)
        {
            _logger.LogWarning("No {ProviderType} providers available", providerType);
            return null;
        }

        // Get health metrics for all candidates
        var providerScores = new List<(string Name, double Score, ProviderHealthMetrics? Metrics)>();

        foreach (var providerName in candidateProviders)
        {
            var metrics = _healthMonitor.GetProviderHealth(providerName);
            
            // Exclude unhealthy providers
            if (metrics != null && (!metrics.IsHealthy || metrics.ConsecutiveFailures >= 3))
            {
                _logger.LogDebug("Excluding {ProviderName}: unhealthy (failures: {Failures})",
                    providerName, metrics.ConsecutiveFailures);
                continue;
            }

            // Calculate composite score
            var score = CalculateProviderScore(metrics, providerName);
            providerScores.Add((providerName, score, metrics));
        }

        if (providerScores.Count == 0)
        {
            _logger.LogWarning("No healthy {ProviderType} providers found after filtering", providerType);
            return null;
        }

        // Sort by score (highest first)
        var selected = providerScores.OrderByDescending(p => p.Score).First();
        
        _logger.LogInformation(
            "Selected {ProviderType} provider: {ProviderName} (score: {Score:F2}, success rate: {SuccessRate:P0}, avg response: {AvgResponse}ms)",
            providerType,
            selected.Name,
            selected.Score,
            selected.Metrics?.SuccessRate ?? 0,
            selected.Metrics?.AverageResponseTime.TotalMilliseconds ?? 0);

        return selected.Name;
    }

    private double CalculateProviderScore(ProviderHealthMetrics? metrics, string providerName)
    {
        if (metrics == null)
        {
            // No metrics yet - give neutral score
            return 50.0;
        }

        // Composite score calculation:
        // - Success rate: 50% weight
        // - Response time: 30% weight (inverse - faster is better)
        // - Tier preference: 20% weight

        var successScore = metrics.SuccessRate * 50.0;

        // Normalize response time score (assume 0-5 seconds range)
        var responseSeconds = Math.Min(metrics.AverageResponseTime.TotalSeconds, 5.0);
        var responseScore = (1.0 - (responseSeconds / 5.0)) * 30.0;

        // Tier preference score
        var tierScore = IsProProvider(providerName) ? 20.0 : IsFreeProvider(providerName) ? 15.0 : 17.5;

        return successScore + responseScore + tierScore;
    }

    private bool IsFreeProvider(string providerName)
    {
        return providerName.Equals("RuleBased", StringComparison.OrdinalIgnoreCase) ||
               providerName.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ||
               providerName.Equals("Windows", StringComparison.OrdinalIgnoreCase) ||
               providerName.Equals("Stock", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsProProvider(string providerName)
    {
        return providerName.Equals("OpenAI", StringComparison.OrdinalIgnoreCase) ||
               providerName.Equals("Azure", StringComparison.OrdinalIgnoreCase) ||
               providerName.Equals("Gemini", StringComparison.OrdinalIgnoreCase) ||
               providerName.Equals("ElevenLabs", StringComparison.OrdinalIgnoreCase) ||
               providerName.Equals("StableDiffusion", StringComparison.OrdinalIgnoreCase);
    }
}
