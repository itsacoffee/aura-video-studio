using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Planner;

/// <summary>
/// Orchestrates planner recommendations with LLM routing and fallback logic
/// Similar to ScriptOrchestrator but for planning/recommendations
/// </summary>
public class PlannerService : IRecommendationService
{
    private readonly ILogger<PlannerService> _logger;
    private readonly Func<Dictionary<string, ILlmPlannerProvider>>? _providerFactory;
    private Dictionary<string, ILlmPlannerProvider>? _providers;
    private readonly string _preferredTier;
    private readonly object _lock = new object();

    /// <summary>
    /// Constructor with pre-created providers (for backward compatibility)
    /// </summary>
    public PlannerService(
        ILogger<PlannerService> logger,
        Dictionary<string, ILlmPlannerProvider> providers,
        string preferredTier = "ProIfAvailable")
    {
        _logger = logger;
        _providers = providers;
        _providerFactory = null;
        _preferredTier = preferredTier;
    }

    /// <summary>
    /// Constructor with factory delegate for lazy provider initialization (recommended)
    /// </summary>
    public PlannerService(
        ILogger<PlannerService> logger,
        Func<Dictionary<string, ILlmPlannerProvider>> providerFactory,
        string preferredTier = "ProIfAvailable")
    {
        _logger = logger;
        _providerFactory = providerFactory;
        _providers = null;
        _preferredTier = preferredTier;
    }

    private Dictionary<string, ILlmPlannerProvider> GetProviders()
    {
        if (_providers != null)
        {
            return _providers;
        }

        if (_providerFactory == null)
        {
            throw new InvalidOperationException("No providers or provider factory configured");
        }

        lock (_lock)
        {
            if (_providers != null)
            {
                return _providers;
            }

            _logger.LogDebug("Lazily initializing Planner providers on first use");
            _providers = _providerFactory();
            return _providers;
        }
    }

    public async Task<PlannerRecommendations> GenerateRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting planner recommendations for topic: {Topic}, preferredTier: {Tier}",
            request.Brief.Topic, _preferredTier);

        // Select provider based on availability and tier preference
        var selectedProvider = SelectProvider();

        if (!selectedProvider.HasValue)
        {
            _logger.LogError("No planner providers available");
            throw new InvalidOperationException("No planner providers available");
        }

        _logger.LogInformation("Selected planner provider: {Provider}", selectedProvider.Value.Key);

        // Try primary provider
        try
        {
            var recommendations = await selectedProvider.Value.Value.GenerateRecommendationsAsync(request, ct)
                .ConfigureAwait(false);

            _logger.LogInformation("Successfully generated recommendations using {Provider}, Quality: {Quality}",
                selectedProvider.Value.Key, recommendations.QualityScore);

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Provider {Provider} failed: {Message}",
                selectedProvider.Value.Key, ex.Message);

            // Try fallback to RuleBased if it wasn't the primary
            if (selectedProvider.Value.Key != "RuleBased" && GetProviders().ContainsKey("RuleBased"))
            {
                _logger.LogInformation("Falling back to RuleBased provider");

                try
                {
                    var recommendations = await GetProviders()["RuleBased"]
                        .GenerateRecommendationsAsync(request, ct)
                        .ConfigureAwait(false);

                    _logger.LogWarning("Successfully fell back from {Primary} to RuleBased",
                        selectedProvider.Value.Key);

                    return recommendations;
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogError(fallbackEx, "Fallback to RuleBased also failed");
                    throw;
                }
            }

            throw;
        }
    }

    private KeyValuePair<string, ILlmPlannerProvider>? SelectProvider()
    {
        var providers = GetProviders();
        
        // Priority order based on tier preference
        var providerPriority = _preferredTier switch
        {
            "Pro" => new[] { "OpenAI", "Azure", "Gemini", "Ollama", "RuleBased" },
            "ProIfAvailable" => new[] { "OpenAI", "Azure", "Gemini", "Ollama", "RuleBased" },
            "Free" => new[] { "Ollama", "RuleBased" },
            _ => new[] { "RuleBased" }
        };

        foreach (var providerName in providerPriority)
        {
            if (providers.TryGetValue(providerName, out var provider))
            {
                return new KeyValuePair<string, ILlmPlannerProvider>(providerName, provider);
            }
        }

        return providers.FirstOrDefault();
    }
}
