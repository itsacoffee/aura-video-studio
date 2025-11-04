using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;

namespace Aura.Core.Providers;

/// <summary>
/// Interface for sound effects providers
/// </summary>
public interface ISfxProvider
{
    /// <summary>
    /// Provider name for identification
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Whether this provider is available (API key configured, service accessible)
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Search for sound effects matching criteria
    /// </summary>
    Task<SearchResult<SfxAsset>> SearchAsync(
        SfxSearchCriteria criteria,
        CancellationToken ct = default);

    /// <summary>
    /// Get a specific sound effect by ID
    /// </summary>
    Task<SfxAsset?> GetByIdAsync(string assetId, CancellationToken ct = default);

    /// <summary>
    /// Download sound effect file to specified path
    /// </summary>
    Task<string> DownloadAsync(string assetId, string destinationPath, CancellationToken ct = default);

    /// <summary>
    /// Get preview URL for streaming without download
    /// </summary>
    Task<string?> GetPreviewUrlAsync(string assetId, CancellationToken ct = default);

    /// <summary>
    /// Find sound effects by tags (common operation for SFX)
    /// </summary>
    Task<SearchResult<SfxAsset>> FindByTagsAsync(
        List<string> tags,
        int maxResults = 20,
        CancellationToken ct = default);
}
