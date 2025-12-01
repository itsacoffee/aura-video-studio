using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Visuals;

/// <summary>
/// Meta-provider that aggregates stock image providers (Pexels, Unsplash).
/// This provider delegates to the first available stock provider, providing a unified
/// "Stock" option for users who have configured any stock image API key.
/// </summary>
public class StockMetaProvider : BaseVisualProvider
{
    private readonly List<BaseVisualProvider> _stockProviders;

    public StockMetaProvider(
        ILogger<StockMetaProvider> logger,
        List<BaseVisualProvider> stockProviders) : base(logger)
    {
        _stockProviders = stockProviders ?? new List<BaseVisualProvider>();
    }

    public override string ProviderName => "Stock";

    public override bool RequiresApiKey => false;

    public override async Task<string?> GenerateImageAsync(
        string prompt,
        VisualGenerationOptions options,
        CancellationToken ct = default)
    {
        foreach (var provider in _stockProviders)
        {
            try
            {
                var isAvailable = await provider.IsAvailableAsync(ct).ConfigureAwait(false);
                if (!isAvailable)
                {
                    Logger.LogDebug("Stock provider {Provider} not available, trying next", provider.ProviderName);
                    continue;
                }

                Logger.LogInformation("Generating image with stock provider: {Provider}", provider.ProviderName);
                var result = await provider.GenerateImageAsync(prompt, options, ct).ConfigureAwait(false);
                
                if (result != null)
                {
                    Logger.LogInformation("Image generated successfully with {Provider}", provider.ProviderName);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Stock provider {Provider} failed, trying next", provider.ProviderName);
            }
        }

        Logger.LogWarning("All stock providers failed for prompt: {Prompt}", prompt);
        return null;
    }

    public override async Task<List<string>> BatchGenerateAsync(
        List<string> prompts,
        VisualGenerationOptions options,
        IProgress<BatchGenerationProgress>? progress = null,
        CancellationToken ct = default)
    {
        foreach (var provider in _stockProviders)
        {
            try
            {
                var isAvailable = await provider.IsAvailableAsync(ct).ConfigureAwait(false);
                if (!isAvailable)
                {
                    continue;
                }

                Logger.LogInformation("Batch generating with stock provider: {Provider}", provider.ProviderName);
                var results = await provider.BatchGenerateAsync(prompts, options, progress, ct).ConfigureAwait(false);
                
                if (results != null && results.Count > 0)
                {
                    return results;
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Stock provider {Provider} batch generation failed", provider.ProviderName);
            }
        }

        return new List<string>();
    }

    public override VisualProviderCapabilities GetProviderCapabilities()
    {
        return new VisualProviderCapabilities
        {
            ProviderName = ProviderName,
            SupportsNegativePrompts = false,
            SupportsBatchGeneration = true,
            SupportsStylePresets = false,
            SupportedAspectRatios = new List<string> { "16:9", "9:16", "1:1", "4:3" },
            SupportedStyles = new List<string> { "photorealistic", "natural", "professional", "vibrant" },
            MaxWidth = 6000,
            MaxHeight = 4000,
            IsLocal = false,
            IsFree = true,
            CostPerImage = 0m,
            Tier = "Free"
        };
    }

    public override async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        foreach (var provider in _stockProviders)
        {
            try
            {
                var isAvailable = await provider.IsAvailableAsync(ct).ConfigureAwait(false);
                if (isAvailable)
                {
                    Logger.LogDebug("Stock meta-provider available via {Provider}", provider.ProviderName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Failed to check availability of {Provider}", provider.ProviderName);
            }
        }

        return false;
    }
}
