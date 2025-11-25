using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Assets;

/// <summary>
/// Service for managing asset tagging operations including auto-tagging,
/// tag storage, and semantic search functionality.
/// </summary>
public class AssetTaggingService
{
    private readonly ILogger<AssetTaggingService> _logger;
    private readonly IAssetTagProvider? _tagProvider;
    private readonly AssetTagger _fallbackTagger;
    
    // In-memory storage for semantic metadata (will be enhanced with database integration)
    private readonly Dictionary<Guid, SemanticAssetMetadata> _metadataStore = new();
    private readonly object _storeLock = new();

    public AssetTaggingService(
        ILogger<AssetTaggingService> logger,
        IAssetTagProvider? tagProvider = null,
        AssetTagger? fallbackTagger = null)
    {
        _logger = logger;
        _tagProvider = tagProvider;
        
        // Create fallback tagger if not provided
        if (fallbackTagger == null)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
            _fallbackTagger = new AssetTagger(loggerFactory.CreateLogger<AssetTagger>());
        }
        else
        {
            _fallbackTagger = fallbackTagger;
        }

        _logger.LogInformation(
            "AssetTaggingService initialized with provider: {Provider}",
            _tagProvider?.Name ?? "Fallback (rule-based)");
    }

    /// <summary>
    /// Auto-generate tags for an asset using the configured provider
    /// </summary>
    public async Task<AssetTaggingResult> TagAssetAsync(
        Asset asset,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(asset);

        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting tagging for asset {AssetId} of type {Type}", asset.Id, asset.Type);

        try
        {
            SemanticAssetMetadata metadata;

            // Try to use the LLM provider if available
            if (_tagProvider != null && await _tagProvider.IsAvailableAsync(ct).ConfigureAwait(false))
            {
                _logger.LogDebug("Using {Provider} for asset tagging", _tagProvider.Name);
                
                metadata = await _tagProvider.GenerateTagsAsync(
                    asset.FilePath,
                    asset.Type,
                    asset.Metadata,
                    ct
                ).ConfigureAwait(false);
            }
            else
            {
                // Fall back to rule-based tagging
                _logger.LogDebug("Using fallback rule-based tagging");
                metadata = await GenerateFallbackMetadataAsync(asset).ConfigureAwait(false);
            }

            // Store the metadata
            StoreMetadata(asset.Id, metadata);

            var processingTime = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Asset {AssetId} tagged with {TagCount} tags in {Duration}ms",
                asset.Id,
                metadata.Tags.Count,
                processingTime.TotalMilliseconds);

            return new AssetTaggingResult
            {
                Success = true,
                Metadata = metadata,
                ProcessingTime = processingTime
            };
        }
        catch (Exception ex)
        {
            var processingTime = DateTime.UtcNow - startTime;
            _logger.LogError(ex, "Failed to tag asset {AssetId}", asset.Id);

            return new AssetTaggingResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ProcessingTime = processingTime
            };
        }
    }

    /// <summary>
    /// Batch tag multiple assets efficiently
    /// </summary>
    public async Task<List<AssetTaggingResult>> TagAssetsAsync(
        IEnumerable<Asset> assets,
        CancellationToken ct = default)
    {
        var results = new List<AssetTaggingResult>();
        
        foreach (var asset in assets)
        {
            ct.ThrowIfCancellationRequested();
            var result = await TagAssetAsync(asset, ct).ConfigureAwait(false);
            results.Add(result);
        }

        return results;
    }

    /// <summary>
    /// Get stored semantic metadata for an asset
    /// </summary>
    public SemanticAssetMetadata? GetMetadata(Guid assetId)
    {
        lock (_storeLock)
        {
            return _metadataStore.TryGetValue(assetId, out var metadata) ? metadata : null;
        }
    }

    /// <summary>
    /// Update or add tags to an asset manually
    /// </summary>
    public void UpdateTags(Guid assetId, List<AssetTag> tags)
    {
        lock (_storeLock)
        {
            if (_metadataStore.TryGetValue(assetId, out var existing))
            {
                _metadataStore[assetId] = existing with
                {
                    Tags = tags,
                    TaggedAt = DateTime.UtcNow
                };
            }
            else
            {
                _metadataStore[assetId] = new SemanticAssetMetadata
                {
                    AssetId = assetId,
                    Tags = tags,
                    TaggedAt = DateTime.UtcNow,
                    TaggingProvider = "Manual"
                };
            }
        }

        _logger.LogInformation("Updated tags for asset {AssetId}: {TagCount} tags", assetId, tags.Count);
    }

    /// <summary>
    /// Search assets by tags
    /// </summary>
    public List<Guid> SearchByTags(
        List<string> tags,
        bool matchAll = false)
    {
        lock (_storeLock)
        {
            var normalizedTags = tags.Select(t => t.ToLowerInvariant()).ToHashSet();

            var matches = _metadataStore
                .Where(kvp =>
                {
                    var assetTags = kvp.Value.Tags.Select(t => t.Name).ToHashSet();
                    
                    if (matchAll)
                    {
                        return normalizedTags.All(t => assetTags.Contains(t));
                    }
                    else
                    {
                        return normalizedTags.Any(t => assetTags.Contains(t));
                    }
                })
                .Select(kvp => kvp.Key)
                .ToList();

            _logger.LogDebug("Tag search returned {Count} assets for tags: {Tags}", matches.Count, string.Join(", ", tags));
            return matches;
        }
    }

    /// <summary>
    /// Search assets by semantic similarity to a scene description
    /// </summary>
    public async Task<List<SemanticSearchResult>> SearchBySimilarityAsync(
        string sceneDescription,
        int maxResults = 10,
        CancellationToken ct = default)
    {
        List<(Guid AssetId, SemanticAssetMetadata Metadata)> assetsWithMetadata;
        
        lock (_storeLock)
        {
            assetsWithMetadata = _metadataStore
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        if (assetsWithMetadata.Count == 0)
        {
            _logger.LogDebug("No assets with metadata available for similarity search");
            return new List<SemanticSearchResult>();
        }

        // Use LLM provider if available for semantic matching
        if (_tagProvider != null && await _tagProvider.IsAvailableAsync(ct).ConfigureAwait(false))
        {
            try
            {
                return await _tagProvider.MatchAssetsToSceneAsync(
                    sceneDescription,
                    assetsWithMetadata,
                    maxResults,
                    ct
                ).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM-based similarity search failed, falling back to keyword matching");
            }
        }

        // Fall back to keyword-based matching
        return PerformKeywordSearch(sceneDescription, assetsWithMetadata, maxResults);
    }

    /// <summary>
    /// Match assets to a specific scene for video generation
    /// </summary>
    public async Task<List<(Guid AssetId, float Score)>> MatchAssetsToSceneAsync(
        string sceneHeading,
        string sceneScript,
        AssetType preferredType = AssetType.Image,
        int maxResults = 5,
        CancellationToken ct = default)
    {
        var sceneDescription = $"{sceneHeading}. {sceneScript}";
        
        var searchResults = await SearchBySimilarityAsync(sceneDescription, maxResults * 2, ct).ConfigureAwait(false);
        
        // Filter by preferred type if metadata indicates type
        List<(Guid AssetId, SemanticAssetMetadata Metadata)> assetsWithType;
        lock (_storeLock)
        {
            assetsWithType = _metadataStore
                .Where(kvp => searchResults.Any(sr => sr.AssetId == kvp.Key))
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();
        }

        return searchResults
            .Take(maxResults)
            .Select(sr => (sr.AssetId, sr.SimilarityScore))
            .ToList();
    }

    /// <summary>
    /// Store semantic metadata for an asset
    /// </summary>
    private void StoreMetadata(Guid assetId, SemanticAssetMetadata metadata)
    {
        lock (_storeLock)
        {
            _metadataStore[assetId] = metadata;
        }
    }

    /// <summary>
    /// Generate fallback metadata using rule-based tagging
    /// </summary>
    private async Task<SemanticAssetMetadata> GenerateFallbackMetadataAsync(Asset asset)
    {
        var tags = await _fallbackTagger.GenerateTagsAsync(asset).ConfigureAwait(false);

        return new SemanticAssetMetadata
        {
            AssetId = asset.Id,
            Tags = tags,
            Description = asset.Description ?? asset.Title,
            DominantColor = asset.DominantColor,
            TaggedAt = DateTime.UtcNow,
            TaggingProvider = "Fallback",
            ConfidenceScore = 0.6f
        };
    }

    /// <summary>
    /// Infer tag category from tag name
    /// </summary>
    private static TagCategory InferCategory(string tagName)
    {
        var colorTerms = new[] { "red", "blue", "green", "yellow", "orange", "purple", "pink", "black", "white", "gray", "brown" };
        var moodTerms = new[] { "happy", "sad", "dramatic", "calm", "energetic", "peaceful", "intense", "dark", "bright", "cheerful" };
        var styleTerms = new[] { "modern", "vintage", "minimal", "rustic", "elegant", "casual", "professional", "artistic" };
        var settingTerms = new[] { "indoor", "outdoor", "urban", "nature", "office", "home", "studio", "landscape" };
        var actionTerms = new[] { "walking", "running", "sitting", "talking", "working", "playing", "dancing" };

        var lowerName = tagName.ToLowerInvariant();

        if (colorTerms.Any(c => lowerName.Contains(c))) return TagCategory.Color;
        if (moodTerms.Any(m => lowerName.Contains(m))) return TagCategory.Mood;
        if (styleTerms.Any(s => lowerName.Contains(s))) return TagCategory.Style;
        if (settingTerms.Any(s => lowerName.Contains(s))) return TagCategory.Setting;
        if (actionTerms.Any(a => lowerName.Contains(a))) return TagCategory.Action;

        return TagCategory.Subject;
    }

    /// <summary>
    /// Perform keyword-based search as a fallback
    /// </summary>
    private List<SemanticSearchResult> PerformKeywordSearch(
        string query,
        List<(Guid AssetId, SemanticAssetMetadata Metadata)> assets,
        int maxResults)
    {
        var queryWords = query.ToLowerInvariant()
            .Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 2)
            .ToHashSet();

        var results = new List<SemanticSearchResult>();

        foreach (var (assetId, metadata) in assets)
        {
            var assetWords = new HashSet<string>();
            
            foreach (var tag in metadata.Tags)
            {
                assetWords.Add(tag.Name);
            }
            
            if (!string.IsNullOrEmpty(metadata.Description))
            {
                foreach (var word in metadata.Description.ToLowerInvariant().Split(' '))
                {
                    if (word.Length > 2) assetWords.Add(word);
                }
            }
            
            if (!string.IsNullOrEmpty(metadata.Subject))
            {
                assetWords.Add(metadata.Subject.ToLowerInvariant());
            }
            
            if (!string.IsNullOrEmpty(metadata.Mood))
            {
                assetWords.Add(metadata.Mood.ToLowerInvariant());
            }

            var matchedWords = queryWords.Intersect(assetWords).ToList();
            
            if (matchedWords.Count > 0)
            {
                var score = (float)matchedWords.Count / Math.Max(queryWords.Count, 1);
                results.Add(new SemanticSearchResult
                {
                    AssetId = assetId,
                    SimilarityScore = Math.Min(score, 1.0f),
                    MatchedTags = matchedWords,
                    MatchReason = $"Matched {matchedWords.Count} keywords: {string.Join(", ", matchedWords)}"
                });
            }
        }

        return results
            .OrderByDescending(r => r.SimilarityScore)
            .Take(maxResults)
            .ToList();
    }
}
