using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.StockMedia;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Alias to disambiguate Asset types
using CoreAsset = Aura.Core.Models.Asset;

namespace Aura.Providers.Images;

/// <summary>
/// Configuration for the unified stock media service fallback chain.
/// </summary>
public class UnifiedStockMediaOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "StockMedia";

    /// <summary>
    /// The order of providers to try. Defaults to: Pexels, Pixabay, Unsplash.
    /// Placeholder colors are always tried last as the ultimate fallback.
    /// </summary>
    public List<string> FallbackOrder { get; set; } = new() { "Pexels", "Pixabay", "Unsplash" };

    /// <summary>
    /// Whether to enable placeholder color fallback when all providers fail.
    /// </summary>
    public bool EnablePlaceholderFallback { get; set; } = true;

    /// <summary>
    /// Configuration for placeholder generation.
    /// </summary>
    public PlaceholderColorConfig PlaceholderConfig { get; set; } = new();
}

/// <summary>
/// Unified stock media service that implements fallback chain logic.
/// Tries providers in configured order and falls back to placeholder colors as last resort.
/// </summary>
public class UnifiedStockProviderService : IUnifiedStockProvider
{
    private readonly ILogger<UnifiedStockProviderService> _logger;
    private readonly IEnumerable<IEnhancedStockProvider> _providers;
    private readonly IEnumerable<IStockProvider> _basicProviders;
    private readonly PlaceholderColorGenerator _placeholderGenerator;
    private readonly UnifiedStockMediaOptions _options;

    public UnifiedStockProviderService(
        ILogger<UnifiedStockProviderService> logger,
        IEnumerable<IEnhancedStockProvider> providers,
        IEnumerable<IStockProvider> basicProviders,
        PlaceholderColorGenerator placeholderGenerator,
        IOptions<UnifiedStockMediaOptions>? options = null)
    {
        _logger = logger;
        _providers = providers;
        _basicProviders = basicProviders;
        _placeholderGenerator = placeholderGenerator;
        _options = options?.Value ?? new UnifiedStockMediaOptions();
    }

    /// <inheritdoc />
    public string Name => "UnifiedStockProvider";

    /// <inheritdoc />
    public async Task<UnifiedStockSearchResult> SearchWithFallbackAsync(
        string query,
        int count,
        StockMediaType mediaType = StockMediaType.Image,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Starting unified stock search for '{Query}' (count: {Count}, type: {Type})",
            query, count, mediaType);

        var providersTried = new List<string>();
        var providerErrors = new Dictionary<string, string>();

        // Try each provider in the configured fallback order
        foreach (var providerName in _options.FallbackOrder)
        {
            ct.ThrowIfCancellationRequested();

            var provider = FindProvider(providerName);
            if (provider == null)
            {
                _logger.LogDebug("Provider {Provider} not found or not configured", providerName);
                continue;
            }

            providersTried.Add(providerName);

            try
            {
                _logger.LogDebug("Trying provider: {Provider}", providerName);

                var results = await TrySearchProviderAsync(provider, query, count, mediaType, ct)
                    .ConfigureAwait(false);

                if (results.Count > 0)
                {
                    _logger.LogInformation(
                        "Provider {Provider} returned {Count} results",
                        providerName, results.Count);

                    return new UnifiedStockSearchResult
                    {
                        Assets = results,
                        SourceProvider = providerName,
                        IsPlaceholder = false,
                        ProvidersTried = providersTried,
                        ProviderErrors = providerErrors
                    };
                }

                _logger.LogDebug("Provider {Provider} returned no results", providerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed", providerName);
                providerErrors[providerName] = ex.Message;
            }
        }

        // All providers failed or returned no results, try placeholder fallback
        if (_options.EnablePlaceholderFallback)
        {
            _logger.LogInformation(
                "All providers exhausted, generating placeholder color frames");

            var placeholders = await _placeholderGenerator.GeneratePlaceholdersAsync(query, count, ct)
                .ConfigureAwait(false);

            return new UnifiedStockSearchResult
            {
                Assets = placeholders,
                SourceProvider = "Placeholder",
                IsPlaceholder = true,
                ProvidersTried = providersTried,
                ProviderErrors = providerErrors
            };
        }

        _logger.LogWarning("All providers failed and placeholder fallback is disabled");

        return new UnifiedStockSearchResult
        {
            Assets = Array.Empty<Asset>(),
            SourceProvider = "None",
            IsPlaceholder = false,
            ProvidersTried = providersTried,
            ProviderErrors = providerErrors
        };
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        // Check if any provider in the fallback chain is available
        foreach (var providerName in _options.FallbackOrder)
        {
            var provider = FindEnhancedProvider(providerName);
            if (provider != null)
            {
                try
                {
                    var isValid = await provider.ValidateAsync(ct).ConfigureAwait(false);
                    if (isValid)
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Provider {Provider} validation failed", providerName);
                }
            }
        }

        // Placeholder generator is always available
        return _options.EnablePlaceholderFallback;
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, ProviderStatus>> GetProviderStatusAsync(CancellationToken ct = default)
    {
        var status = new Dictionary<string, ProviderStatus>();

        foreach (var providerName in _options.FallbackOrder)
        {
            var provider = FindEnhancedProvider(providerName);
            if (provider == null)
            {
                status[providerName] = new ProviderStatus
                {
                    IsAvailable = false,
                    IsConfigured = false,
                    StatusMessage = "Provider not configured"
                };
                continue;
            }

            try
            {
                var isValid = await provider.ValidateAsync(ct).ConfigureAwait(false);
                var rateLimit = provider.GetRateLimitStatus();

                status[providerName] = new ProviderStatus
                {
                    IsAvailable = isValid && !rateLimit.IsLimited,
                    IsConfigured = true,
                    StatusMessage = isValid
                        ? (rateLimit.IsLimited ? "Rate limited" : "Available")
                        : "API key invalid or expired",
                    RateLimit = rateLimit
                };
            }
            catch (Exception ex)
            {
                status[providerName] = new ProviderStatus
                {
                    IsAvailable = false,
                    IsConfigured = true,
                    StatusMessage = $"Error: {ex.Message}"
                };
            }
        }

        // Add placeholder status
        status["Placeholder"] = new ProviderStatus
        {
            IsAvailable = _options.EnablePlaceholderFallback,
            IsConfigured = true,
            StatusMessage = _options.EnablePlaceholderFallback
                ? "Available (generates solid color frames)"
                : "Disabled in configuration"
        };

        return status;
    }

    /// <inheritdoc />
    public IReadOnlyList<string> GetFallbackOrder()
    {
        var order = new List<string>(_options.FallbackOrder);
        if (_options.EnablePlaceholderFallback)
        {
            order.Add("Placeholder");
        }
        return order;
    }

    private IEnhancedStockProvider? FindEnhancedProvider(string name)
    {
        return _providers.FirstOrDefault(p =>
            p.ProviderName.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private object? FindProvider(string name)
    {
        // First try enhanced providers
        var enhanced = FindEnhancedProvider(name);
        if (enhanced != null)
        {
            return enhanced;
        }

        // Then try basic providers by checking their type name
        return _basicProviders.FirstOrDefault(p =>
            p.GetType().Name.StartsWith(name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<CoreAsset>> TrySearchProviderAsync(
        object provider,
        string query,
        int count,
        StockMediaType mediaType,
        CancellationToken ct)
    {
        if (provider is IEnhancedStockProvider enhanced)
        {
            // Skip if video is requested but provider doesn't support it
            if (mediaType == StockMediaType.Video && !enhanced.SupportsVideo)
            {
                _logger.LogDebug(
                    "Provider {Provider} does not support video, skipping",
                    enhanced.ProviderName);
                return Array.Empty<CoreAsset>();
            }

            var request = new StockMediaSearchRequest
            {
                Query = query,
                Count = count,
                Type = mediaType
            };

            var results = await enhanced.SearchAsync(request, ct).ConfigureAwait(false);

            // Convert StockMediaResult to Asset
            return results.Select(r => new CoreAsset(
                Kind: r.Type == StockMediaType.Video ? "video" : "image",
                PathOrUrl: r.FullSizeUrl,
                License: r.Licensing.LicenseType,
                Attribution: r.Licensing.Attribution
            )).ToList();
        }

        if (provider is IStockProvider basic)
        {
            // Basic providers only support images
            if (mediaType == StockMediaType.Video)
            {
                return Array.Empty<CoreAsset>();
            }

            return await basic.SearchAsync(query, count, ct).ConfigureAwait(false);
        }

        return Array.Empty<CoreAsset>();
    }
}
