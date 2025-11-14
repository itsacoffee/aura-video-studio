using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.Assets;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Assets;

/// <summary>
/// Service for automatic asset tagging using AI
/// </summary>
public class AssetTagger
{
    private readonly ILogger<AssetTagger> _logger;

    public AssetTagger(ILogger<AssetTagger> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate tags for an asset
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
        // In a full implementation, this would:
        // 1. Use a vision API or multimodal LLM
        // 2. Analyze the image content
        // 3. Generate relevant tags with confidence scores
        
        // For now, generate basic tags based on filename and metadata
        var tags = new List<AssetTag>();

        // Add type tag
        tags.Add(new AssetTag("image", 100));

        // Analyze filename for keywords
        var filename = Path.GetFileNameWithoutExtension(asset.FilePath).ToLowerInvariant();
        var keywords = filename.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var keyword in keywords.Take(5))
        {
            if (keyword.Length > 2)
                tags.Add(new AssetTag(keyword, 80));
        }

        // Add resolution-based tags
        if (asset.Metadata.Width.HasValue && asset.Metadata.Height.HasValue)
        {
            var width = asset.Metadata.Width.Value;
            var height = asset.Metadata.Height.Value;
            
            if (width >= 3840 && height >= 2160)
                tags.Add(new AssetTag("4k", 100));
            else if (width >= 1920 && height >= 1080)
                tags.Add(new AssetTag("hd", 100));

            if (width > height)
                tags.Add(new AssetTag("landscape", 90));
            else if (height > width)
                tags.Add(new AssetTag("portrait", 90));
            else
                tags.Add(new AssetTag("square", 90));
        }

        return await Task.FromResult(tags).ConfigureAwait(false);
    }

    private async Task<List<AssetTag>> GenerateVideoTagsAsync(Asset asset)
    {
        var tags = new List<AssetTag>
        {
            new AssetTag("video", 100)
        };

        // Analyze filename
        var filename = Path.GetFileNameWithoutExtension(asset.FilePath).ToLowerInvariant();
        var keywords = filename.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var keyword in keywords.Take(5))
        {
            if (keyword.Length > 2)
                tags.Add(new AssetTag(keyword, 80));
        }

        // Add duration-based tags
        if (asset.Metadata.Duration.HasValue)
        {
            var duration = asset.Metadata.Duration.Value;
            if (duration.TotalSeconds < 10)
                tags.Add(new AssetTag("short", 90));
            else if (duration.TotalSeconds < 60)
                tags.Add(new AssetTag("medium", 90));
            else
                tags.Add(new AssetTag("long", 90));
        }

        // Add resolution tags
        if (asset.Metadata.Width.HasValue && asset.Metadata.Height.HasValue)
        {
            var width = asset.Metadata.Width.Value;
            var height = asset.Metadata.Height.Value;
            
            if (width >= 3840 && height >= 2160)
                tags.Add(new AssetTag("4k", 100));
            else if (width >= 1920 && height >= 1080)
                tags.Add(new AssetTag("hd", 100));
        }

        return await Task.FromResult(tags).ConfigureAwait(false);
    }

    private async Task<List<AssetTag>> GenerateAudioTagsAsync(Asset asset)
    {
        var tags = new List<AssetTag>
        {
            new AssetTag("audio", 100)
        };

        // Analyze filename for mood/style keywords
        var filename = Path.GetFileNameWithoutExtension(asset.FilePath).ToLowerInvariant();
        var keywords = filename.Split(new[] { '_', '-', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var keyword in keywords.Take(5))
        {
            if (keyword.Length > 2)
                tags.Add(new AssetTag(keyword, 80));
        }

        // Add duration-based tags
        if (asset.Metadata.Duration.HasValue)
        {
            var duration = asset.Metadata.Duration.Value;
            if (duration.TotalSeconds < 30)
                tags.Add(new AssetTag("short", 90));
            else if (duration.TotalSeconds < 180)
                tags.Add(new AssetTag("medium", 90));
            else
                tags.Add(new AssetTag("long", 90));
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
                tags.Add(new AssetTag(tag, 85));
        }

        return await Task.FromResult(tags).ConfigureAwait(false);
    }
}
