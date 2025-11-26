using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.StockMedia;

namespace Aura.Core.Providers;

/// <summary>
/// Unified interface for stock media providers with fallback chain support.
/// Implementations should try configured providers in order and fall back to
/// placeholder color frames as a last resort.
/// </summary>
public interface IUnifiedStockProvider
{
    /// <summary>
    /// Gets the name of this unified provider instance.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Searches for stock media using the configured fallback chain.
    /// Falls back through Pexels → Pixabay → Unsplash → Placeholder Colors.
    /// </summary>
    /// <param name="query">Search query for stock media</param>
    /// <param name="count">Number of results to return</param>
    /// <param name="mediaType">Type of media to search for (image or video)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of assets from the first successful provider</returns>
    Task<UnifiedStockSearchResult> SearchWithFallbackAsync(
        string query,
        int count,
        StockMediaType mediaType = StockMediaType.Image,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if any provider in the fallback chain is available.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if at least one provider is available</returns>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the status of all providers in the fallback chain.
    /// </summary>
    /// <returns>Dictionary of provider names to their availability status</returns>
    Task<Dictionary<string, ProviderStatus>> GetProviderStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the currently configured fallback order.
    /// </summary>
    /// <returns>List of provider names in fallback order</returns>
    IReadOnlyList<string> GetFallbackOrder();
}

/// <summary>
/// Result from unified stock search including source provider information.
/// </summary>
public record UnifiedStockSearchResult
{
    /// <summary>
    /// The assets found by the search.
    /// </summary>
    public IReadOnlyList<Asset> Assets { get; init; } = Array.Empty<Asset>();

    /// <summary>
    /// The provider that returned these results.
    /// </summary>
    public string SourceProvider { get; init; } = string.Empty;

    /// <summary>
    /// Whether the results came from the placeholder fallback.
    /// </summary>
    public bool IsPlaceholder { get; init; }

    /// <summary>
    /// Providers that were tried before getting results.
    /// </summary>
    public IReadOnlyList<string> ProvidersTried { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Error messages from failed providers (if any).
    /// </summary>
    public IReadOnlyDictionary<string, string> ProviderErrors { get; init; } = 
        new Dictionary<string, string>();
}

/// <summary>
/// Status of a stock media provider.
/// </summary>
public record ProviderStatus
{
    /// <summary>
    /// Whether the provider is currently available.
    /// </summary>
    public bool IsAvailable { get; init; }

    /// <summary>
    /// Whether the provider has an API key configured.
    /// </summary>
    public bool IsConfigured { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string StatusMessage { get; init; } = string.Empty;

    /// <summary>
    /// Rate limit information if available.
    /// </summary>
    public RateLimitStatus? RateLimit { get; init; }
}
