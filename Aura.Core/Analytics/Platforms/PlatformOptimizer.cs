using Microsoft.Extensions.Logging;

namespace Aura.Core.Analytics.Platforms;

/// <summary>
/// Optimizes content for specific platforms (YouTube, TikTok, Instagram, etc.)
/// </summary>
public class PlatformOptimizer
{
    private readonly ILogger<PlatformOptimizer> _logger;

    public PlatformOptimizer(ILogger<PlatformOptimizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets optimization recommendations for a specific platform
    /// </summary>
    public Task<PlatformOptimization> GetPlatformOptimizationAsync(
        string platform,
        string content,
        TimeSpan videoDuration,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting optimization for platform: {Platform}", platform);

        var specs = GetPlatformSpecs(platform);
        var recommendations = GeneratePlatformRecommendations(platform, content, videoDuration, specs);
        
        return Task.FromResult(new PlatformOptimization(
            Platform: platform,
            OptimalDuration: specs.OptimalDuration,
            RecommendedAspectRatio: specs.AspectRatio,
            OptimalThumbnailSize: specs.ThumbnailSize,
            Recommendations: recommendations,
            MetadataGuidelines: GetMetadataGuidelines(platform),
            HashtagSuggestions: GenerateHashtags(content, platform)
        ));
    }

    /// <summary>
    /// Suggests aspect ratio adaptations for cross-platform publishing
    /// </summary>
    public Task<AspectRatioSuggestions> SuggestAspectRatioAdaptationsAsync(
        List<string> targetPlatforms,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Suggesting aspect ratios for {Count} platforms", targetPlatforms.Count);

        var suggestions = targetPlatforms.Select(platform =>
        {
            var specs = GetPlatformSpecs(platform);
            return new PlatformAspectRatio(
                Platform: platform,
                AspectRatio: specs.AspectRatio,
                Resolution: specs.RecommendedResolution,
                Reasoning: specs.AspectRatioReasoning
            );
        }).ToList();

        return Task.FromResult(new AspectRatioSuggestions(
            Suggestions: suggestions,
            RecommendedPrimaryFormat: DeterminePrimaryFormat(suggestions),
            AdaptationStrategy: "Create primary in 16:9, crop to 9:16 for shorts platforms"
        ));
    }

    /// <summary>
    /// Generates hashtag recommendations for the platform
    /// </summary>
    public Task<HashtagRecommendations> GenerateHashtagsAsync(
        string content,
        string platform,
        string? niche = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating hashtags for {Platform}", platform);

        var hashtags = GenerateHashtags(content, platform);
        var specs = GetPlatformSpecs(platform);

        return Task.FromResult(new HashtagRecommendations(
            Platform: platform,
            PrimaryHashtags: hashtags.Take(5).ToList(),
            SecondaryHashtags: hashtags.Skip(5).Take(10).ToList(),
            MaxRecommended: specs.MaxHashtags,
            Guidelines: GetHashtagGuidelines(platform)
        ));
    }

    private PlatformSpecs GetPlatformSpecs(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "youtube" => new PlatformSpecs(
                OptimalDuration: TimeSpan.FromMinutes(8),
                AspectRatio: "16:9",
                RecommendedResolution: "1920x1080",
                AspectRatioReasoning: "Standard widescreen for desktop and mobile viewing",
                ThumbnailSize: "1280x720",
                MaxHashtags: 15,
                IntroOptimalLength: TimeSpan.FromSeconds(5),
                OutroOptimalLength: TimeSpan.FromSeconds(10)
            ),
            "tiktok" => new PlatformSpecs(
                OptimalDuration: TimeSpan.FromSeconds(45),
                AspectRatio: "9:16",
                RecommendedResolution: "1080x1920",
                AspectRatioReasoning: "Vertical format optimized for mobile viewing",
                ThumbnailSize: "1080x1920",
                MaxHashtags: 5,
                IntroOptimalLength: TimeSpan.FromSeconds(1),
                OutroOptimalLength: TimeSpan.FromSeconds(2)
            ),
            "instagram" => new PlatformSpecs(
                OptimalDuration: TimeSpan.FromSeconds(60),
                AspectRatio: "9:16",
                RecommendedResolution: "1080x1920",
                AspectRatioReasoning: "Vertical format for Reels",
                ThumbnailSize: "1080x1920",
                MaxHashtags: 30,
                IntroOptimalLength: TimeSpan.FromSeconds(2),
                OutroOptimalLength: TimeSpan.FromSeconds(3)
            ),
            "youtube shorts" => new PlatformSpecs(
                OptimalDuration: TimeSpan.FromSeconds(45),
                AspectRatio: "9:16",
                RecommendedResolution: "1080x1920",
                AspectRatioReasoning: "Vertical format for shorts",
                ThumbnailSize: "1080x1920",
                MaxHashtags: 10,
                IntroOptimalLength: TimeSpan.FromSeconds(1),
                OutroOptimalLength: TimeSpan.FromSeconds(2)
            ),
            _ => new PlatformSpecs(
                OptimalDuration: TimeSpan.FromMinutes(5),
                AspectRatio: "16:9",
                RecommendedResolution: "1920x1080",
                AspectRatioReasoning: "Standard widescreen format",
                ThumbnailSize: "1280x720",
                MaxHashtags: 10,
                IntroOptimalLength: TimeSpan.FromSeconds(3),
                OutroOptimalLength: TimeSpan.FromSeconds(5)
            )
        };
    }

    private List<string> GeneratePlatformRecommendations(
        string platform,
        string content,
        TimeSpan duration,
        PlatformSpecs specs)
    {
        var recommendations = new List<string>();

        // Duration check
        if (duration > specs.OptimalDuration * 1.5)
        {
            recommendations.Add($"Consider shortening to around {specs.OptimalDuration.TotalMinutes:F1} minutes for {platform}");
        }

        // Platform-specific recommendations
        switch (platform.ToLowerInvariant())
        {
            case "youtube":
                recommendations.Add("Create compelling thumbnail with text overlay");
                recommendations.Add("Include cards and end screens for engagement");
                recommendations.Add("Add chapters for longer videos");
                recommendations.Add("Optimize title for search with keywords");
                break;
            case "tiktok":
                recommendations.Add("Hook viewers in first 1-2 seconds");
                recommendations.Add("Use trending sounds when relevant");
                recommendations.Add("Include text overlays for accessibility");
                recommendations.Add("End with strong call-to-action");
                break;
            case "instagram":
                recommendations.Add("Use trending audio for Reels");
                recommendations.Add("Add captions for sound-off viewing");
                recommendations.Add("Include call-to-action in caption");
                recommendations.Add("Post during peak engagement hours");
                break;
            case "youtube shorts":
                recommendations.Add("Start with immediate hook");
                recommendations.Add("Use vertical format 9:16");
                recommendations.Add("Keep under 60 seconds");
                recommendations.Add("Include #Shorts in title");
                break;
        }

        return recommendations;
    }

    private Dictionary<string, string> GetMetadataGuidelines(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "youtube" => new Dictionary<string, string>
            {
                ["Title"] = "Keep under 60 characters, include main keyword",
                ["Description"] = "Front-load important info, include timestamps",
                ["Tags"] = "Use 10-15 relevant tags, mix broad and specific",
                ["Thumbnail"] = "1280x720, high contrast, readable text"
            },
            "tiktok" => new Dictionary<string, string>
            {
                ["Caption"] = "Engaging, use 3-5 hashtags max",
                ["Sounds"] = "Use trending or original sounds",
                ["Effects"] = "Use relevant effects sparingly",
                ["CTA"] = "End with clear call-to-action"
            },
            "instagram" => new Dictionary<string, string>
            {
                ["Caption"] = "First line is crucial, use line breaks",
                ["Hashtags"] = "Use up to 30, mix popular and niche",
                ["Location"] = "Add location for local discovery",
                ["Alt Text"] = "Add for accessibility"
            },
            _ => new Dictionary<string, string>
            {
                ["General"] = "Follow platform-specific best practices"
            }
        };
    }

    private List<string> GenerateHashtags(string content, string platform)
    {
        // Simple hashtag generation based on content analysis
        var hashtags = new List<string> { "#content", "#video" };

        // Extract keywords from content
        var words = content.ToLowerInvariant()
            .Split(new[] { ' ', '\n', '.', ',', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4)
            .Distinct()
            .Take(10);

        hashtags.AddRange(words.Select(w => $"#{w}"));

        // Add platform-specific tags
        switch (platform.ToLowerInvariant())
        {
            case "youtube":
                hashtags.AddRange(new[] { "#youtube", "#youtuber", "#tutorial" });
                break;
            case "tiktok":
                hashtags.AddRange(new[] { "#tiktok", "#fyp", "#viral" });
                break;
            case "instagram":
                hashtags.AddRange(new[] { "#instagram", "#reels", "#explore" });
                break;
        }

        return hashtags.Distinct().Take(20).ToList();
    }

    private string GetHashtagGuidelines(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "youtube" => "Use 3-5 hashtags in description, include in title sparingly",
            "tiktok" => "Use 3-5 strategic hashtags, avoid over-tagging",
            "instagram" => "Use up to 30 hashtags, mix sizes (popular and niche)",
            _ => "Use relevant hashtags according to platform guidelines"
        };
    }

    private string DeterminePrimaryFormat(List<PlatformAspectRatio> suggestions)
    {
        // Determine most common format
        var formatCounts = suggestions
            .GroupBy(s => s.AspectRatio)
            .OrderByDescending(g => g.Count())
            .First();

        return formatCounts.Key;
    }
}

// Models
public record PlatformSpecs(
    TimeSpan OptimalDuration,
    string AspectRatio,
    string RecommendedResolution,
    string AspectRatioReasoning,
    string ThumbnailSize,
    int MaxHashtags,
    TimeSpan IntroOptimalLength,
    TimeSpan OutroOptimalLength
);

public record PlatformOptimization(
    string Platform,
    TimeSpan OptimalDuration,
    string RecommendedAspectRatio,
    string OptimalThumbnailSize,
    List<string> Recommendations,
    Dictionary<string, string> MetadataGuidelines,
    List<string> HashtagSuggestions
);

public record AspectRatioSuggestions(
    List<PlatformAspectRatio> Suggestions,
    string RecommendedPrimaryFormat,
    string AdaptationStrategy
);

public record PlatformAspectRatio(
    string Platform,
    string AspectRatio,
    string Resolution,
    string Reasoning
);

public record HashtagRecommendations(
    string Platform,
    List<string> PrimaryHashtags,
    List<string> SecondaryHashtags,
    int MaxRecommended,
    string Guidelines
);
