using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.Platform;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Platform;

/// <summary>
/// Service for generating and optimizing metadata for different platforms
/// </summary>
public class MetadataOptimizationService
{
    private readonly ILogger<MetadataOptimizationService> _logger;
    private readonly PlatformProfileService _platformProfile;

    public MetadataOptimizationService(
        ILogger<MetadataOptimizationService> logger,
        PlatformProfileService platformProfile)
    {
        _logger = logger;
        _platformProfile = platformProfile;
    }

    /// <summary>
    /// Generate optimized metadata for a platform
    /// </summary>
    public async Task<OptimizedMetadata> GenerateMetadata(MetadataGenerationRequest request)
    {
        _logger.LogInformation("Generating metadata for platform: {Platform}", request.Platform);

        var profile = _platformProfile.GetPlatformProfile(request.Platform);
        if (profile == null)
        {
            throw new ArgumentException($"Unknown platform: {request.Platform}");
        }

        var metadata = new OptimizedMetadata();

        // Generate title
        metadata.Title = GenerateTitle(request, profile);

        // Generate description
        metadata.Description = GenerateDescription(request, profile);

        // Generate tags
        metadata.Tags = GenerateTags(request, profile);

        // Generate hashtags
        metadata.Hashtags = GenerateHashtags(request, profile);

        // Generate call to action
        metadata.CallToAction = GenerateCallToAction(request.Platform, profile);

        // Add custom fields based on platform
        AddPlatformSpecificFields(metadata, profile);

        await Task.Delay(50); // Simulate async processing

        _logger.LogInformation("Metadata generated with title length: {Length}", metadata.Title.Length);
        return metadata;
    }

    /// <summary>
    /// Generate optimized title
    /// </summary>
    private string GenerateTitle(MetadataGenerationRequest request, PlatformProfile profile)
    {
        var title = request.VideoTitle;
        var maxLength = profile.Requirements.Metadata.TitleMaxLength;

        if (maxLength > 0 && title.Length > maxLength)
        {
            // Truncate intelligently, preserving key information
            title = title.Substring(0, maxLength - 3) + "...";
        }

        // Add platform-specific optimizations
        if (profile.PlatformId == "youtube")
        {
            // YouTube favors keyword-rich titles
            if (request.Keywords.Any() && !title.Contains(request.Keywords[0], StringComparison.OrdinalIgnoreCase))
            {
                title = $"{request.Keywords[0]} - {title}";
                if (maxLength > 0 && title.Length > maxLength)
                {
                    title = title.Substring(0, maxLength - 3) + "...";
                }
            }
        }

        return title;
    }

    /// <summary>
    /// Generate optimized description
    /// </summary>
    private string GenerateDescription(MetadataGenerationRequest request, PlatformProfile profile)
    {
        var description = request.VideoDescription;
        var maxLength = profile.Requirements.Metadata.DescriptionMaxLength;

        if (string.IsNullOrEmpty(description))
        {
            description = $"Content optimized for {profile.Name}. ";
        }

        // Add keywords naturally
        if (request.Keywords.Any())
        {
            var keywordSection = "\n\nRelated topics: " + string.Join(", ", request.Keywords.Take(5));
            description += keywordSection;
        }

        // Add platform-specific best practices
        if (profile.PlatformId == "linkedin")
        {
            description = AddProfessionalContext(description, request);
        }

        if (maxLength > 0 && description.Length > maxLength)
        {
            description = description.Substring(0, maxLength - 3) + "...";
        }

        return description;
    }

    /// <summary>
    /// Generate optimized tags
    /// </summary>
    private List<string> GenerateTags(MetadataGenerationRequest request, PlatformProfile profile)
    {
        var tags = new List<string>(request.Keywords);
        var maxTags = profile.Requirements.Metadata.MaxTags;

        // Add content type variations
        if (!string.IsNullOrEmpty(request.ContentType))
        {
            tags.Add(request.ContentType);
            tags.Add($"{request.ContentType} content");
        }

        // Add target audience variations
        if (!string.IsNullOrEmpty(request.TargetAudience))
        {
            tags.Add(request.TargetAudience);
        }

        // Platform-specific tag strategies
        if (profile.PlatformId == "youtube")
        {
            // YouTube allows many tags, add variations
            var extraTags = new List<string>();
            foreach (var keyword in request.Keywords.Take(3))
            {
                extraTags.Add($"{keyword} tutorial");
                extraTags.Add($"how to {keyword}");
            }
            tags.AddRange(extraTags);
        }

        // Limit to max tags and remove duplicates
        tags = tags.Distinct().ToList();
        if (maxTags > 0 && tags.Count > maxTags)
        {
            tags = tags.Take(maxTags).ToList();
        }

        return tags;
    }

    /// <summary>
    /// Generate optimized hashtags
    /// </summary>
    private List<string> GenerateHashtags(MetadataGenerationRequest request, PlatformProfile profile)
    {
        var hashtags = new List<string>();
        var maxHashtags = profile.Requirements.Metadata.MaxHashtags;
        var maxHashtagLength = profile.Requirements.Metadata.HashtagMaxLength;

        if (maxHashtags == 0)
        {
            return hashtags;
        }

        // Convert keywords to hashtags
        foreach (var keyword in request.Keywords.Take(maxHashtags))
        {
            var hashtag = keyword.Replace(" ", "");
            if (maxHashtagLength > 0 && hashtag.Length > maxHashtagLength)
            {
                hashtag = hashtag.Substring(0, maxHashtagLength);
            }
            hashtags.Add(hashtag);
        }

        // Add platform-specific trending hashtags
        if (profile.PlatformId == "tiktok" || profile.PlatformId == "instagram-reels")
        {
            // Add general discovery hashtags
            var discoveryTags = new[] { "fyp", "foryou", "viral", "trending" };
            hashtags.AddRange(discoveryTags.Take(Math.Max(0, maxHashtags - hashtags.Count)));
        }
        else if (profile.PlatformId == "linkedin")
        {
            // Add professional hashtags
            hashtags.Add("professional");
            hashtags.Add("business");
        }

        // Limit to max hashtags
        if (hashtags.Count > maxHashtags)
        {
            hashtags = hashtags.Take(maxHashtags).ToList();
        }

        return hashtags;
    }

    /// <summary>
    /// Generate platform-appropriate call to action
    /// </summary>
    private string GenerateCallToAction(string platformId, PlatformProfile profile)
    {
        return platformId.ToLowerInvariant() switch
        {
            "youtube" => "Like, Subscribe, and turn on notifications for more!",
            "tiktok" => "Follow for more! 💫",
            "instagram-reels" => "Save this and follow for more! ✨",
            "instagram-feed" => "Double tap if you agree! Follow for daily content.",
            "linkedin" => "Connect with me for more professional insights.",
            "twitter" => "Retweet if you found this helpful!",
            "facebook" => "Share this with someone who needs to see it!",
            "youtube-shorts" => "Subscribe for more shorts! 🚀",
            _ => "Follow for more content!"
        };
    }

    /// <summary>
    /// Add platform-specific custom fields
    /// </summary>
    private void AddPlatformSpecificFields(OptimizedMetadata metadata, PlatformProfile profile)
    {
        metadata.CustomFields["platform"] = profile.PlatformId;
        metadata.CustomFields["requires_captions"] = profile.BestPractices.CaptionsRequired;
        metadata.CustomFields["music_important"] = profile.BestPractices.MusicImportant;
        metadata.CustomFields["hook_duration"] = profile.BestPractices.HookDurationSeconds;

        if (profile.BestPractices.TextOverlayEffective)
        {
            metadata.CustomFields["text_overlay_recommended"] = true;
        }
    }

    /// <summary>
    /// Add professional context for LinkedIn
    /// </summary>
    private string AddProfessionalContext(string description, MetadataGenerationRequest request)
    {
        var professional = description;
        
        if (!string.IsNullOrEmpty(request.TargetAudience))
        {
            professional = $"For {request.TargetAudience}: {professional}";
        }

        return professional;
    }

    /// <summary>
    /// Validate metadata against platform limits
    /// </summary>
    public bool ValidateMetadata(OptimizedMetadata metadata, string platformId)
    {
        var profile = _platformProfile.GetPlatformProfile(platformId);
        if (profile == null)
        {
            return false;
        }

        var limits = profile.Requirements.Metadata;

        if (limits.TitleMaxLength > 0 && metadata.Title.Length > limits.TitleMaxLength)
        {
            _logger.LogWarning("Title exceeds maximum length for {Platform}", platformId);
            return false;
        }

        if (limits.DescriptionMaxLength > 0 && metadata.Description.Length > limits.DescriptionMaxLength)
        {
            _logger.LogWarning("Description exceeds maximum length for {Platform}", platformId);
            return false;
        }

        if (limits.MaxTags > 0 && metadata.Tags.Count > limits.MaxTags)
        {
            _logger.LogWarning("Too many tags for {Platform}", platformId);
            return false;
        }

        if (limits.MaxHashtags > 0 && metadata.Hashtags.Count > limits.MaxHashtags)
        {
            _logger.LogWarning("Too many hashtags for {Platform}", platformId);
            return false;
        }

        return true;
    }
}
