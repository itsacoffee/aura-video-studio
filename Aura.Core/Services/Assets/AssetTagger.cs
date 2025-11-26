using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Assets;

/// <summary>
/// Service for automatic asset tagging using rule-based analysis.
/// This provides fallback tagging when LLM providers are unavailable.
/// </summary>
public class AssetTagger
{
    private readonly ILogger<AssetTagger> _logger;

    public AssetTagger(ILogger<AssetTagger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate tags for an asset using rule-based analysis
    /// </summary>
    public async Task<List<AssetTag>> GenerateTagsAsync(Asset asset)
    {
        _logger.LogInformation("Generating tags for asset {AssetId} of type {Type}", asset.Id, asset.Type);

        var tags = new List<AssetTag>();

        try
        {
            tags = asset.Type switch
            {
                Models.Assets.AssetType.Image => await GenerateImageTagsAsync(asset).ConfigureAwait(false),
                Models.Assets.AssetType.Video => await GenerateVideoTagsAsync(asset).ConfigureAwait(false),
                Models.Assets.AssetType.Audio => await GenerateAudioTagsAsync(asset).ConfigureAwait(false),
                _ => tags
            };

            _logger.LogInformation("Generated {Count} tags for asset {AssetId}", tags.Count, asset.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate tags for asset {AssetId}", asset.Id);
        }

        return tags;
    }

    private async Task<List<AssetTag>> GenerateImageTagsAsync(Asset asset)
    {
        var tags = new List<AssetTag>();

        // Add type tag
        tags.Add(new AssetTag("image", 1.0f, TagCategory.Object));

        // Analyze filename for keywords
        var filename = Path.GetFileNameWithoutExtension(asset.FilePath).ToLowerInvariant();
        var keywords = filename.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var keyword in keywords.Take(5))
        {
            if (keyword.Length > 2)
                tags.Add(new AssetTag(keyword, 0.8f, InferCategoryFromKeyword(keyword)));
        }

        // Add resolution-based tags
        if (asset.Metadata.Width.HasValue && asset.Metadata.Height.HasValue)
        {
            var width = asset.Metadata.Width.Value;
            var height = asset.Metadata.Height.Value;
            
            if (width >= 3840 && height >= 2160)
                tags.Add(new AssetTag("4k", 1.0f, TagCategory.Style));
            else if (width >= 1920 && height >= 1080)
                tags.Add(new AssetTag("hd", 1.0f, TagCategory.Style));

            if (width > height)
                tags.Add(new AssetTag("landscape", 0.9f, TagCategory.Style));
            else if (height > width)
                tags.Add(new AssetTag("portrait", 0.9f, TagCategory.Style));
            else
                tags.Add(new AssetTag("square", 0.9f, TagCategory.Style));
        }

        return await Task.FromResult(tags).ConfigureAwait(false);
    }

    private async Task<List<AssetTag>> GenerateVideoTagsAsync(Asset asset)
    {
        var tags = new List<AssetTag>
        {
            new AssetTag("video", 1.0f, TagCategory.Object)
        };

        // Analyze filename
        var filename = Path.GetFileNameWithoutExtension(asset.FilePath).ToLowerInvariant();
        var keywords = filename.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var keyword in keywords.Take(5))
        {
            if (keyword.Length > 2)
                tags.Add(new AssetTag(keyword, 0.8f, InferCategoryFromKeyword(keyword)));
        }

        // Add duration-based tags
        if (asset.Metadata.Duration.HasValue)
        {
            var duration = asset.Metadata.Duration.Value;
            if (duration.TotalSeconds < 10)
                tags.Add(new AssetTag("short", 0.9f, TagCategory.Style));
            else if (duration.TotalSeconds < 60)
                tags.Add(new AssetTag("medium", 0.9f, TagCategory.Style));
            else
                tags.Add(new AssetTag("long", 0.9f, TagCategory.Style));
        }

        // Add resolution tags
        if (asset.Metadata.Width.HasValue && asset.Metadata.Height.HasValue)
        {
            var width = asset.Metadata.Width.Value;
            var height = asset.Metadata.Height.Value;
            
            if (width >= 3840 && height >= 2160)
                tags.Add(new AssetTag("4k", 1.0f, TagCategory.Style));
            else if (width >= 1920 && height >= 1080)
                tags.Add(new AssetTag("hd", 1.0f, TagCategory.Style));
        }

        return await Task.FromResult(tags).ConfigureAwait(false);
    }

    private async Task<List<AssetTag>> GenerateAudioTagsAsync(Asset asset)
    {
        var tags = new List<AssetTag>
        {
            new AssetTag("audio", 1.0f, TagCategory.Object)
        };

        // Analyze filename for mood/style keywords
        var filename = Path.GetFileNameWithoutExtension(asset.FilePath).ToLowerInvariant();
        var keywords = filename.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var keyword in keywords.Take(5))
        {
            if (keyword.Length > 2)
                tags.Add(new AssetTag(keyword, 0.8f, InferCategoryFromKeyword(keyword)));
        }

        // Add duration-based tags
        if (asset.Metadata.Duration.HasValue)
        {
            var duration = asset.Metadata.Duration.Value;
            if (duration.TotalSeconds < 30)
                tags.Add(new AssetTag("short", 0.9f, TagCategory.Style));
            else if (duration.TotalSeconds < 180)
                tags.Add(new AssetTag("medium", 0.9f, TagCategory.Style));
            else
                tags.Add(new AssetTag("long", 0.9f, TagCategory.Style));
        }

        // Common audio mood tags based on filename patterns
        var moodKeywords = new Dictionary<string, string>
        {
            { "calm", "calm" },
            { "energetic", "energetic" },
            { "dramatic", "dramatic" },
            { "corporate", "corporate" },
            { "upbeat", "upbeat" },
            { "dark", "dark" },
            { "ambient", "ambient" },
            { "epic", "epic" }
        };

        foreach (var (pattern, tag) in moodKeywords)
        {
            if (filename.Contains(pattern))
                tags.Add(new AssetTag(tag, 0.85f, TagCategory.Mood));
        }

        return await Task.FromResult(tags).ConfigureAwait(false);
    }

    /// <summary>
    /// Infer tag category from keyword content
    /// </summary>
    private static TagCategory InferCategoryFromKeyword(string keyword)
    {
        var lowerKeyword = keyword.ToLowerInvariant();

        var colorTerms = new[] { "red", "blue", "green", "yellow", "orange", "purple", "pink", "black", "white", "gray", "brown" };
        var moodTerms = new[] { "happy", "sad", "dramatic", "calm", "energetic", "peaceful", "intense", "dark", "bright", "cheerful" };
        var styleTerms = new[] { "modern", "vintage", "minimal", "rustic", "elegant", "casual", "professional", "artistic" };
        var settingTerms = new[] { "indoor", "outdoor", "urban", "nature", "office", "home", "studio", "landscape" };
        var actionTerms = new[] { "walking", "running", "sitting", "talking", "working", "playing", "dancing" };

        if (colorTerms.Any(c => lowerKeyword.Contains(c))) return TagCategory.Color;
        if (moodTerms.Any(m => lowerKeyword.Contains(m))) return TagCategory.Mood;
        if (styleTerms.Any(s => lowerKeyword.Contains(s))) return TagCategory.Style;
        if (settingTerms.Any(s => lowerKeyword.Contains(s))) return TagCategory.Setting;
        if (actionTerms.Any(a => lowerKeyword.Contains(a))) return TagCategory.Action;

        return TagCategory.Subject;
    }
}
