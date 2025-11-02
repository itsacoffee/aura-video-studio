using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models.Platform;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Platform;

/// <summary>
/// Service for managing platform profiles and their requirements
/// </summary>
public class PlatformProfileService
{
    private readonly ILogger<PlatformProfileService> _logger;
    private readonly Dictionary<string, PlatformProfile> _platformProfiles;

    public PlatformProfileService(ILogger<PlatformProfileService> logger)
    {
        _logger = logger;
        _platformProfiles = InitializePlatformProfiles();
    }

    /// <summary>
    /// Get platform profile by ID
    /// </summary>
    public PlatformProfile? GetPlatformProfile(string platformId)
    {
        _platformProfiles.TryGetValue(platformId.ToLowerInvariant(), out var profile);
        return profile;
    }

    /// <summary>
    /// Get all available platforms
    /// </summary>
    public List<PlatformProfile> GetAllPlatforms()
    {
        return _platformProfiles.Values.ToList();
    }

    /// <summary>
    /// Get platforms by aspect ratio support
    /// </summary>
    public List<PlatformProfile> GetPlatformsByAspectRatio(string aspectRatio)
    {
        return _platformProfiles.Values
            .Where(p => p.Requirements.SupportedAspectRatios.Any(ar => ar.Ratio == aspectRatio))
            .ToList();
    }

    /// <summary>
    /// Initialize platform profiles with current specifications
    /// </summary>
    private Dictionary<string, PlatformProfile> InitializePlatformProfiles()
    {
        var profiles = new Dictionary<string, PlatformProfile>();

        // YouTube
        profiles["youtube"] = new PlatformProfile
        {
            PlatformId = "youtube",
            Name = "YouTube",
            Description = "World's largest video sharing platform, SEO-focused with emphasis on watch time",
            Requirements = new PlatformRequirements
            {
                SupportedAspectRatios = new List<AspectRatioSpec>
                {
                    new() { Ratio = "16:9", Width = 1920, Height = 1080, IsPreferred = true, UseCase = "standard" },
                    new() { Ratio = "16:9", Width = 3840, Height = 2160, IsPreferred = false, UseCase = "4K" },
                    new() { Ratio = "4:3", Width = 1440, Height = 1080, IsPreferred = false, UseCase = "legacy" }
                },
                Video = new VideoSpecs
                {
                    MinDurationSeconds = 1,
                    MaxDurationSeconds = 43200, // 12 hours
                    OptimalMinDurationSeconds = 480, // 8 minutes
                    OptimalMaxDurationSeconds = 900, // 15 minutes
                    MaxFileSizeBytes = 256L * 1024L * 1024L * 1024L, // 256GB
                    RecommendedCodecs = new List<string> { "h264", "h265" },
                    MaxBitrate = 85000,
                    RecommendedBitrate = 8000,
                    RequiredFrameRates = new List<string> { "24", "25", "30", "60" }
                },
                Thumbnail = new ThumbnailSpecs
                {
                    Width = 1280,
                    Height = 720,
                    MinWidth = 640,
                    MinHeight = 360,
                    MaxFileSizeBytes = 2 * 1024 * 1024, // 2MB
                    SupportedFormats = new List<string> { "jpg", "png" },
                    TextOverlayRecommended = true,
                    SafeAreaDescription = "Keep text and important elements within central 90% of frame"
                },
                Metadata = new MetadataLimits
                {
                    TitleMaxLength = 100,
                    DescriptionMaxLength = 5000,
                    MaxTags = 500,
                    MaxHashtags = 15,
                    HashtagMaxLength = 30
                },
                SupportedFormats = new List<string> { "mp4", "mov", "avi", "wmv", "flv", "webm" }
            },
            BestPractices = new PlatformBestPractices
            {
                HookDurationSeconds = 15,
                HookStrategy = "Preview value, tease content, establish authority",
                ContentPacing = "Moderate - maintain engagement throughout",
                CaptionsRequired = false,
                MusicImportant = false,
                TextOverlayEffective = true,
                ToneAndStyle = "Educational, entertaining, or documentary style",
                ContentStrategies = new List<string>
                {
                    "Optimize for watch time and retention",
                    "Use chapters for longer videos",
                    "Create compelling thumbnails with faces and text",
                    "Front-load value in first 15 seconds",
                    "Include clear CTAs for likes, comments, subscriptions"
                },
                OptimalPostingTimes = "2-4 PM weekdays, 9-11 AM weekends (viewer timezone)"
            },
            AlgorithmFactors = new PlatformAlgorithmFactors
            {
                AlgorithmType = "watch_time",
                FavorsNewContent = true,
                TypicalViralTimeframeHours = 72,
                Factors = new List<RankingFactor>
                {
                    new() { Name = "Watch Time", Description = "Total time viewers spend watching", Weight = 10 },
                    new() { Name = "Click-Through Rate", Description = "CTR on thumbnails in search/browse", Weight = 9 },
                    new() { Name = "Engagement", Description = "Likes, comments, shares", Weight = 8 },
                    new() { Name = "Session Time", Description = "How long users stay on YouTube after", Weight = 7 },
                    new() { Name = "Upload Frequency", Description = "Consistent upload schedule", Weight = 6 }
                }
            }
        };

        // TikTok
        profiles["tiktok"] = new PlatformProfile
        {
            PlatformId = "tiktok",
            Name = "TikTok",
            Description = "Short-form vertical video platform emphasizing trends and engagement",
            Requirements = new PlatformRequirements
            {
                SupportedAspectRatios = new List<AspectRatioSpec>
                {
                    new() { Ratio = "9:16", Width = 1080, Height = 1920, IsPreferred = true, UseCase = "standard" },
                    new() { Ratio = "1:1", Width = 1080, Height = 1080, IsPreferred = false, UseCase = "square" }
                },
                Video = new VideoSpecs
                {
                    MinDurationSeconds = 1,
                    MaxDurationSeconds = 600, // 10 minutes
                    OptimalMinDurationSeconds = 15,
                    OptimalMaxDurationSeconds = 60,
                    MaxFileSizeBytes = 287 * 1024 * 1024, // 287MB
                    RecommendedCodecs = new List<string> { "h264" },
                    MaxBitrate = 12000,
                    RecommendedBitrate = 8000,
                    RequiredFrameRates = new List<string> { "30", "60" }
                },
                Thumbnail = new ThumbnailSpecs
                {
                    Width = 1080,
                    Height = 1920,
                    MinWidth = 640,
                    MinHeight = 640,
                    MaxFileSizeBytes = 500 * 1024, // 500KB
                    SupportedFormats = new List<string> { "jpg", "png" },
                    TextOverlayRecommended = true,
                    SafeAreaDescription = "Avoid bottom 15% (UI elements) and top 20% (text overlays)"
                },
                Metadata = new MetadataLimits
                {
                    TitleMaxLength = 150,
                    DescriptionMaxLength = 2200,
                    MaxTags = 0,
                    MaxHashtags = 30,
                    HashtagMaxLength = 24
                },
                SupportedFormats = new List<string> { "mp4", "mov", "webm" }
            },
            BestPractices = new PlatformBestPractices
            {
                HookDurationSeconds = 3,
                HookStrategy = "Immediate visual or audio hook, use trending sounds",
                ContentPacing = "Fast - quick cuts, high energy",
                CaptionsRequired = true,
                MusicImportant = true,
                TextOverlayEffective = true,
                ToneAndStyle = "Casual, authentic, trend-focused",
                ContentStrategies = new List<string>
                {
                    "Hook viewers in first 3 seconds",
                    "Use trending sounds and effects",
                    "Leverage relevant hashtags and challenges",
                    "Post multiple times per day for best results",
                    "Engage with comments quickly",
                    "Use on-screen text for accessibility"
                },
                OptimalPostingTimes = "6-10 AM, 7-11 PM local time"
            },
            AlgorithmFactors = new PlatformAlgorithmFactors
            {
                AlgorithmType = "engagement",
                FavorsNewContent = true,
                TypicalViralTimeframeHours = 24,
                Factors = new List<RankingFactor>
                {
                    new() { Name = "Completion Rate", Description = "% who watch to the end", Weight = 10 },
                    new() { Name = "Shares", Description = "Video shares to others", Weight = 9 },
                    new() { Name = "Comments", Description = "User comments and replies", Weight = 8 },
                    new() { Name = "Likes", Description = "User likes on video", Weight = 7 },
                    new() { Name = "Rewatches", Description = "Users watching multiple times", Weight = 9 }
                }
            }
        };

        // Instagram Reels
        profiles["instagram-reels"] = new PlatformProfile
        {
            PlatformId = "instagram-reels",
            Name = "Instagram Reels",
            Description = "Instagram's short-form vertical video feature",
            Requirements = new PlatformRequirements
            {
                SupportedAspectRatios = new List<AspectRatioSpec>
                {
                    new() { Ratio = "9:16", Width = 1080, Height = 1920, IsPreferred = true, UseCase = "reels" }
                },
                Video = new VideoSpecs
                {
                    MinDurationSeconds = 3,
                    MaxDurationSeconds = 90,
                    OptimalMinDurationSeconds = 15,
                    OptimalMaxDurationSeconds = 60,
                    MaxFileSizeBytes = 4L * 1024L * 1024L * 1024L, // 4GB
                    RecommendedCodecs = new List<string> { "h264" },
                    MaxBitrate = 10000,
                    RecommendedBitrate = 8000,
                    RequiredFrameRates = new List<string> { "30", "60" }
                },
                Thumbnail = new ThumbnailSpecs
                {
                    Width = 1080,
                    Height = 1920,
                    MinWidth = 540,
                    MinHeight = 960,
                    MaxFileSizeBytes = 1 * 1024 * 1024, // 1MB
                    SupportedFormats = new List<string> { "jpg", "png" },
                    TextOverlayRecommended = true,
                    SafeAreaDescription = "Avoid bottom 20% and top 15% for UI elements"
                },
                Metadata = new MetadataLimits
                {
                    TitleMaxLength = 0,
                    DescriptionMaxLength = 2200,
                    MaxTags = 0,
                    MaxHashtags = 30,
                    HashtagMaxLength = 30
                },
                SupportedFormats = new List<string> { "mp4", "mov" }
            },
            BestPractices = new PlatformBestPractices
            {
                HookDurationSeconds = 3,
                HookStrategy = "Visual impact first, leverage Instagram's audio library",
                ContentPacing = "Fast - maintain visual interest throughout",
                CaptionsRequired = true,
                MusicImportant = true,
                TextOverlayEffective = true,
                ToneAndStyle = "Visual-first, aesthetic, lifestyle-oriented",
                ContentStrategies = new List<string>
                {
                    "Use trending audio from Instagram library",
                    "Create visually striking content",
                    "Leverage hashtags strategically",
                    "Engage with comments and shares",
                    "Post during peak activity times",
                    "Use captions for accessibility"
                },
                OptimalPostingTimes = "9 AM - 12 PM, 5 PM - 9 PM local time"
            },
            AlgorithmFactors = new PlatformAlgorithmFactors
            {
                AlgorithmType = "engagement",
                FavorsNewContent = true,
                TypicalViralTimeframeHours = 48,
                Factors = new List<RankingFactor>
                {
                    new() { Name = "Engagement Rate", Description = "Likes, comments, shares, saves", Weight = 10 },
                    new() { Name = "Completion Rate", Description = "% who watch to end", Weight = 9 },
                    new() { Name = "Shares", Description = "Shares via DM or Stories", Weight = 8 },
                    new() { Name = "Audio Usage", Description = "Using trending audio", Weight = 7 },
                    new() { Name = "Saves", Description = "Users saving for later", Weight = 8 }
                }
            }
        };

        // Instagram Feed
        profiles["instagram-feed"] = new PlatformProfile
        {
            PlatformId = "instagram-feed",
            Name = "Instagram Feed",
            Description = "Traditional Instagram feed posts with focus on aesthetics",
            Requirements = new PlatformRequirements
            {
                SupportedAspectRatios = new List<AspectRatioSpec>
                {
                    new() { Ratio = "1:1", Width = 1080, Height = 1080, IsPreferred = true, UseCase = "square" },
                    new() { Ratio = "4:5", Width = 1080, Height = 1350, IsPreferred = true, UseCase = "portrait" },
                    new() { Ratio = "16:9", Width = 1080, Height = 608, IsPreferred = false, UseCase = "landscape" }
                },
                Video = new VideoSpecs
                {
                    MinDurationSeconds = 3,
                    MaxDurationSeconds = 60,
                    OptimalMinDurationSeconds = 15,
                    OptimalMaxDurationSeconds = 30,
                    MaxFileSizeBytes = 4L * 1024L * 1024L * 1024L, // 4GB
                    RecommendedCodecs = new List<string> { "h264" },
                    MaxBitrate = 8000,
                    RecommendedBitrate = 5000,
                    RequiredFrameRates = new List<string> { "30" }
                },
                Thumbnail = new ThumbnailSpecs
                {
                    Width = 1080,
                    Height = 1080,
                    MinWidth = 320,
                    MinHeight = 320,
                    MaxFileSizeBytes = 8 * 1024 * 1024, // 8MB
                    SupportedFormats = new List<string> { "jpg", "png" },
                    TextOverlayRecommended = false,
                    SafeAreaDescription = "Full frame visible"
                },
                Metadata = new MetadataLimits
                {
                    TitleMaxLength = 0,
                    DescriptionMaxLength = 2200,
                    MaxTags = 0,
                    MaxHashtags = 30,
                    HashtagMaxLength = 30
                },
                SupportedFormats = new List<string> { "mp4", "mov" }
            },
            BestPractices = new PlatformBestPractices
            {
                HookDurationSeconds = 5,
                HookStrategy = "Visual aesthetics, brand consistency",
                ContentPacing = "Moderate - aesthetic over speed",
                CaptionsRequired = false,
                MusicImportant = false,
                TextOverlayEffective = false,
                ToneAndStyle = "Curated, aesthetic, brand-focused",
                ContentStrategies = new List<string>
                {
                    "Maintain consistent visual aesthetic",
                    "Write engaging captions with call-to-actions",
                    "Use relevant hashtags (5-10 most effective)",
                    "Post 1-2 times daily",
                    "Engage with audience in comments",
                    "Use location tags when relevant"
                },
                OptimalPostingTimes = "11 AM - 1 PM, 7 PM - 9 PM local time"
            },
            AlgorithmFactors = new PlatformAlgorithmFactors
            {
                AlgorithmType = "engagement",
                FavorsNewContent = true,
                TypicalViralTimeframeHours = 72,
                Factors = new List<RankingFactor>
                {
                    new() { Name = "Engagement Rate", Description = "Likes, comments, shares per follower", Weight = 10 },
                    new() { Name = "Saves", Description = "Users saving posts", Weight = 9 },
                    new() { Name = "Shares", Description = "DMs and story shares", Weight = 8 },
                    new() { Name = "Time Spent", Description = "Time viewing post", Weight = 7 },
                    new() { Name = "Profile Visits", Description = "Visits to profile from post", Weight = 6 }
                }
            }
        };

        // YouTube Shorts
        profiles["youtube-shorts"] = new PlatformProfile
        {
            PlatformId = "youtube-shorts",
            Name = "YouTube Shorts",
            Description = "YouTube's short-form vertical video feature",
            Requirements = new PlatformRequirements
            {
                SupportedAspectRatios = new List<AspectRatioSpec>
                {
                    new() { Ratio = "9:16", Width = 1080, Height = 1920, IsPreferred = true, UseCase = "shorts" }
                },
                Video = new VideoSpecs
                {
                    MinDurationSeconds = 1,
                    MaxDurationSeconds = 60,
                    OptimalMinDurationSeconds = 15,
                    OptimalMaxDurationSeconds = 59,
                    MaxFileSizeBytes = 2L * 1024L * 1024L * 1024L, // 2GB
                    RecommendedCodecs = new List<string> { "h264" },
                    MaxBitrate = 10000,
                    RecommendedBitrate = 8000,
                    RequiredFrameRates = new List<string> { "30", "60" }
                },
                Thumbnail = new ThumbnailSpecs
                {
                    Width = 1080,
                    Height = 1920,
                    MinWidth = 640,
                    MinHeight = 640,
                    MaxFileSizeBytes = 2 * 1024 * 1024, // 2MB
                    SupportedFormats = new List<string> { "jpg", "png" },
                    TextOverlayRecommended = true,
                    SafeAreaDescription = "Avoid bottom and top UI areas"
                },
                Metadata = new MetadataLimits
                {
                    TitleMaxLength = 100,
                    DescriptionMaxLength = 5000,
                    MaxTags = 500,
                    MaxHashtags = 15,
                    HashtagMaxLength = 30
                },
                SupportedFormats = new List<string> { "mp4", "mov" }
            },
            BestPractices = new PlatformBestPractices
            {
                HookDurationSeconds = 3,
                HookStrategy = "Immediate value or entertainment, loop-friendly content",
                ContentPacing = "Fast - keep it punchy and engaging",
                CaptionsRequired = false,
                MusicImportant = true,
                TextOverlayEffective = true,
                ToneAndStyle = "Entertaining, educational snippets, or viral content",
                ContentStrategies = new List<string>
                {
                    "Create content that loops naturally",
                    "Use #Shorts in title or description",
                    "Front-load the hook",
                    "Keep text readable on mobile",
                    "Use trending topics",
                    "Optimize for discovery, not just subscribers"
                },
                OptimalPostingTimes = "3-6 PM, 9-11 PM viewer timezone"
            },
            AlgorithmFactors = new PlatformAlgorithmFactors
            {
                AlgorithmType = "engagement",
                FavorsNewContent = true,
                TypicalViralTimeframeHours = 48,
                Factors = new List<RankingFactor>
                {
                    new() { Name = "Swipe-Through Rate", Description = "% who watch without swiping", Weight = 10 },
                    new() { Name = "Likes", Description = "User likes on short", Weight = 8 },
                    new() { Name = "Comments", Description = "Engagement in comments", Weight = 7 },
                    new() { Name = "Shares", Description = "Shares of the short", Weight = 8 },
                    new() { Name = "Channel Subscriptions", Description = "New subs from short", Weight = 6 }
                }
            }
        };

        // LinkedIn
        profiles["linkedin"] = new PlatformProfile
        {
            PlatformId = "linkedin",
            Name = "LinkedIn",
            Description = "Professional networking platform with focus on business content",
            Requirements = new PlatformRequirements
            {
                SupportedAspectRatios = new List<AspectRatioSpec>
                {
                    new() { Ratio = "1:1", Width = 1080, Height = 1080, IsPreferred = true, UseCase = "square" },
                    new() { Ratio = "16:9", Width = 1920, Height = 1080, IsPreferred = true, UseCase = "landscape" },
                    new() { Ratio = "9:16", Width = 1080, Height = 1920, IsPreferred = false, UseCase = "vertical" }
                },
                Video = new VideoSpecs
                {
                    MinDurationSeconds = 3,
                    MaxDurationSeconds = 600, // 10 minutes
                    OptimalMinDurationSeconds = 30,
                    OptimalMaxDurationSeconds = 180, // 3 minutes
                    MaxFileSizeBytes = 5L * 1024L * 1024L * 1024L, // 5GB
                    RecommendedCodecs = new List<string> { "h264" },
                    MaxBitrate = 10000,
                    RecommendedBitrate = 5000,
                    RequiredFrameRates = new List<string> { "30" }
                },
                Thumbnail = new ThumbnailSpecs
                {
                    Width = 1200,
                    Height = 627,
                    MinWidth = 600,
                    MinHeight = 314,
                    MaxFileSizeBytes = 5 * 1024 * 1024, // 5MB
                    SupportedFormats = new List<string> { "jpg", "png" },
                    TextOverlayRecommended = true,
                    SafeAreaDescription = "Professional, clear text overlay"
                },
                Metadata = new MetadataLimits
                {
                    TitleMaxLength = 0,
                    DescriptionMaxLength = 3000,
                    MaxTags = 0,
                    MaxHashtags = 5,
                    HashtagMaxLength = 30
                },
                SupportedFormats = new List<string> { "mp4", "mov", "avi" }
            },
            BestPractices = new PlatformBestPractices
            {
                HookDurationSeconds = 5,
                HookStrategy = "Professional value proposition, industry insights",
                ContentPacing = "Moderate - professional and informative",
                CaptionsRequired = true,
                MusicImportant = false,
                TextOverlayEffective = true,
                ToneAndStyle = "Professional, educational, thought leadership",
                ContentStrategies = new List<string>
                {
                    "Lead with professional value",
                    "Use captions (85% watch without sound)",
                    "Keep it concise and actionable",
                    "Include clear CTAs",
                    "Tag relevant companies and people",
                    "Use 3-5 relevant hashtags"
                },
                OptimalPostingTimes = "7-9 AM, 12 PM, 5-6 PM Tuesday-Thursday"
            },
            AlgorithmFactors = new PlatformAlgorithmFactors
            {
                AlgorithmType = "engagement",
                FavorsNewContent = true,
                TypicalViralTimeframeHours = 48,
                Factors = new List<RankingFactor>
                {
                    new() { Name = "Engagement Rate", Description = "Likes, comments, shares", Weight = 10 },
                    new() { Name = "Dwell Time", Description = "Time spent on post", Weight = 9 },
                    new() { Name = "Connection Relevance", Description = "Relevance to viewer's network", Weight = 8 },
                    new() { Name = "Professional Relevance", Description = "Industry and role relevance", Weight = 7 },
                    new() { Name = "Comments Quality", Description = "Meaningful discussion", Weight = 8 }
                }
            }
        };

        // Twitter/X
        profiles["twitter"] = new PlatformProfile
        {
            PlatformId = "twitter",
            Name = "Twitter/X",
            Description = "Microblogging platform focused on real-time conversations",
            Requirements = new PlatformRequirements
            {
                SupportedAspectRatios = new List<AspectRatioSpec>
                {
                    new() { Ratio = "16:9", Width = 1280, Height = 720, IsPreferred = true, UseCase = "landscape" },
                    new() { Ratio = "1:1", Width = 720, Height = 720, IsPreferred = true, UseCase = "square" }
                },
                Video = new VideoSpecs
                {
                    MinDurationSeconds = 1,
                    MaxDurationSeconds = 140,
                    OptimalMinDurationSeconds = 30,
                    OptimalMaxDurationSeconds = 90,
                    MaxFileSizeBytes = 512 * 1024 * 1024, // 512MB
                    RecommendedCodecs = new List<string> { "h264" },
                    MaxBitrate = 5000,
                    RecommendedBitrate = 3000,
                    RequiredFrameRates = new List<string> { "30", "60" }
                },
                Thumbnail = new ThumbnailSpecs
                {
                    Width = 1200,
                    Height = 675,
                    MinWidth = 600,
                    MinHeight = 335,
                    MaxFileSizeBytes = 5 * 1024 * 1024, // 5MB
                    SupportedFormats = new List<string> { "jpg", "png" },
                    TextOverlayRecommended = true,
                    SafeAreaDescription = "Clear, mobile-friendly"
                },
                Metadata = new MetadataLimits
                {
                    TitleMaxLength = 280,
                    DescriptionMaxLength = 280,
                    MaxTags = 0,
                    MaxHashtags = 2,
                    HashtagMaxLength = 20
                },
                SupportedFormats = new List<string> { "mp4", "mov" }
            },
            BestPractices = new PlatformBestPractices
            {
                HookDurationSeconds = 3,
                HookStrategy = "Text-driven, newsworthy, conversation-starting",
                ContentPacing = "Fast - get to the point quickly",
                CaptionsRequired = true,
                MusicImportant = false,
                TextOverlayEffective = true,
                ToneAndStyle = "Conversational, timely, opinionated or informative",
                ContentStrategies = new List<string>
                {
                    "Native video outperforms links",
                    "Keep videos short (30-90s)",
                    "Use captions (most watch muted)",
                    "Tie to trending topics",
                    "Engage in replies quickly",
                    "Use 1-2 hashtags max"
                },
                OptimalPostingTimes = "8-10 AM, 6-9 PM local time"
            },
            AlgorithmFactors = new PlatformAlgorithmFactors
            {
                AlgorithmType = "engagement",
                FavorsNewContent = true,
                TypicalViralTimeframeHours = 24,
                Factors = new List<RankingFactor>
                {
                    new() { Name = "Engagement Rate", Description = "Retweets, likes, replies", Weight = 10 },
                    new() { Name = "Recency", Description = "How recent the tweet is", Weight = 9 },
                    new() { Name = "Replies", Description = "Quality conversation", Weight = 8 },
                    new() { Name = "Retweets", Description = "Shareability", Weight = 9 },
                    new() { Name = "Follower Engagement", Description = "Engagement from followers", Weight = 7 }
                }
            }
        };

        // Facebook
        profiles["facebook"] = new PlatformProfile
        {
            PlatformId = "facebook",
            Name = "Facebook",
            Description = "Social networking platform with broad demographic reach",
            Requirements = new PlatformRequirements
            {
                SupportedAspectRatios = new List<AspectRatioSpec>
                {
                    new() { Ratio = "1:1", Width = 1080, Height = 1080, IsPreferred = true, UseCase = "square" },
                    new() { Ratio = "4:5", Width = 1080, Height = 1350, IsPreferred = true, UseCase = "portrait" },
                    new() { Ratio = "16:9", Width = 1920, Height = 1080, IsPreferred = false, UseCase = "landscape" }
                },
                Video = new VideoSpecs
                {
                    MinDurationSeconds = 1,
                    MaxDurationSeconds = 240 * 60, // 240 minutes
                    OptimalMinDurationSeconds = 60,
                    OptimalMaxDurationSeconds = 180,
                    MaxFileSizeBytes = 100L * 1024L * 1024L * 1024L, // 100GB - increased for professional video workflows
                    RecommendedCodecs = new List<string> { "h264" },
                    MaxBitrate = 8000,
                    RecommendedBitrate = 5000,
                    RequiredFrameRates = new List<string> { "30" }
                },
                Thumbnail = new ThumbnailSpecs
                {
                    Width = 1200,
                    Height = 630,
                    MinWidth = 600,
                    MinHeight = 315,
                    MaxFileSizeBytes = 8 * 1024 * 1024, // 8MB
                    SupportedFormats = new List<string> { "jpg", "png" },
                    TextOverlayRecommended = false,
                    SafeAreaDescription = "Clean, minimal text"
                },
                Metadata = new MetadataLimits
                {
                    TitleMaxLength = 0,
                    DescriptionMaxLength = 63206,
                    MaxTags = 0,
                    MaxHashtags = 5,
                    HashtagMaxLength = 30
                },
                SupportedFormats = new List<string> { "mp4", "mov" }
            },
            BestPractices = new PlatformBestPractices
            {
                HookDurationSeconds = 5,
                HookStrategy = "Emotional or relatable hook, native upload preferred",
                ContentPacing = "Moderate - varies by content type",
                CaptionsRequired = true,
                MusicImportant = false,
                TextOverlayEffective = true,
                ToneAndStyle = "Conversational, community-focused, authentic",
                ContentStrategies = new List<string>
                {
                    "Upload natively (don't share YouTube links)",
                    "Use captions (85% watch without sound)",
                    "Square or vertical performs better in feed",
                    "First 3 seconds crucial for stopping scroll",
                    "Encourage meaningful interactions",
                    "Post during peak engagement times"
                },
                OptimalPostingTimes = "1-4 PM Wednesday-Friday"
            },
            AlgorithmFactors = new PlatformAlgorithmFactors
            {
                AlgorithmType = "engagement",
                FavorsNewContent = true,
                TypicalViralTimeframeHours = 72,
                Factors = new List<RankingFactor>
                {
                    new() { Name = "Meaningful Interactions", Description = "Comments, shares, reactions", Weight = 10 },
                    new() { Name = "Watch Time", Description = "Video completion rate", Weight = 8 },
                    new() { Name = "Shares", Description = "Shares to friends/groups", Weight = 9 },
                    new() { Name = "Native Content", Description = "Uploaded directly to Facebook", Weight = 7 },
                    new() { Name = "Relationship", Description = "Connection strength with poster", Weight = 8 }
                }
            }
        };

        return profiles;
    }
}
