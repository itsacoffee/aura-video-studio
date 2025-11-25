using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;

namespace Aura.Core.Providers;

/// <summary>
/// Interface for asset tagging providers that generate semantic tags using AI/LLM.
/// Implementations can use different backends (Ollama, OpenAI, local models, etc.)
/// </summary>
public interface IAssetTagProvider
{
    /// <summary>
    /// Provider name for identification and logging
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Check if the provider is currently available and ready to process requests
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Generate semantic tags for an asset based on its file content
    /// </summary>
    /// <param name="assetPath">Path to the asset file</param>
    /// <param name="assetType">Type of the asset (Image, Video, Audio)</param>
    /// <param name="existingMetadata">Optional existing metadata to enhance</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Generated semantic metadata including tags, mood, subject, etc.</returns>
    Task<SemanticAssetMetadata> GenerateTagsAsync(
        string assetPath,
        AssetType assetType,
        AssetMetadata? existingMetadata = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generate an embedding vector for semantic similarity search
    /// </summary>
    /// <param name="text">Text to embed (typically a description or concatenated tags)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Embedding vector for similarity calculations</returns>
    Task<float[]?> GenerateEmbeddingAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Match assets to a scene description using semantic similarity
    /// </summary>
    /// <param name="sceneDescription">Description of the scene to match</param>
    /// <param name="availableAssets">List of assets with their semantic metadata</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Ranked list of asset matches with similarity scores</returns>
    Task<List<SemanticSearchResult>> MatchAssetsToSceneAsync(
        string sceneDescription,
        IEnumerable<(Guid AssetId, SemanticAssetMetadata Metadata)> availableAssets,
        int maxResults = 5,
        CancellationToken ct = default);
}
