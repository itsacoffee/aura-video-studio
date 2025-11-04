using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.StockMedia;

namespace Aura.Core.Providers;

/// <summary>
/// Enhanced stock media provider interface with video support and licensing
/// </summary>
public interface IEnhancedStockProvider
{
    /// <summary>
    /// Gets the provider name
    /// </summary>
    StockMediaProvider ProviderName { get; }

    /// <summary>
    /// Checks if the provider supports video content
    /// </summary>
    bool SupportsVideo { get; }

    /// <summary>
    /// Searches for stock media (images or videos)
    /// </summary>
    Task<List<StockMediaResult>> SearchAsync(
        StockMediaSearchRequest request,
        CancellationToken ct);

    /// <summary>
    /// Gets rate limit status for the provider
    /// </summary>
    RateLimitStatus GetRateLimitStatus();

    /// <summary>
    /// Validates API key and connectivity
    /// </summary>
    Task<bool> ValidateAsync(CancellationToken ct);

    /// <summary>
    /// Downloads media from the provider
    /// </summary>
    Task<byte[]> DownloadMediaAsync(
        string url,
        CancellationToken ct);

    /// <summary>
    /// Tracks download for providers that require it (e.g., Unsplash)
    /// </summary>
    Task TrackDownloadAsync(
        string mediaId,
        CancellationToken ct);
}
