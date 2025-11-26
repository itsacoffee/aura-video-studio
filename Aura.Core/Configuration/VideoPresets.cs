using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Aura.Core.Configuration;

/// <summary>
/// Static class providing predefined video generation presets for prosumers.
/// Each preset is optimized for a specific use case with sensible defaults.
/// </summary>
public static class VideoPresets
{
    /// <summary>
    /// Quick Demo - 30-second preview with Windows TTS and placeholder visuals.
    /// Works with zero configuration, completely offline.
    /// </summary>
    public static readonly PresetProfile QuickDemo = new()
    {
        Id = "quick-demo",
        Name = "Quick Demo",
        Description = "30-second preview with Windows TTS. Works with zero configuration.",
        Icon = "⚡",
        Duration = TimeSpan.FromSeconds(30),
        TtsProvider = "Windows",
        LlmProvider = "RuleBased",
        ImageProvider = "Placeholder",
        Resolution = "720p",
        AspectRatio = "16:9",
        VisualStyle = "modern",
        MusicGenre = "none",
        RequiresApiKey = false,
        WorksOffline = true,
        EstimatedCost = 0m,
        TargetPlatform = "general",
        OutputFormat = "mp4",
        IncludeCaptions = true
    };

    /// <summary>
    /// YouTube Short - 60-second vertical video optimized for YouTube Shorts.
    /// Uses 9:16 aspect ratio at 1080p.
    /// </summary>
    public static readonly PresetProfile YouTubeShort = new()
    {
        Id = "youtube-short",
        Name = "YouTube Short",
        Description = "60-second vertical video optimized for YouTube Shorts.",
        Icon = "📱",
        Duration = TimeSpan.FromSeconds(60),
        TtsProvider = "Windows",
        LlmProvider = "RuleBased",
        ImageProvider = "Stock",
        Resolution = "1080p",
        AspectRatio = "9:16",
        VisualStyle = "playful",
        MusicGenre = "upbeat",
        RequiresApiKey = false,
        WorksOffline = false,
        EstimatedCost = 0m,
        TargetPlatform = "youtube",
        OutputFormat = "mp4",
        IncludeCaptions = true
    };

    /// <summary>
    /// Tutorial - Longer format educational content with clear visuals.
    /// Optimized for step-by-step instruction videos.
    /// </summary>
    public static readonly PresetProfile Tutorial = new()
    {
        Id = "tutorial",
        Name = "Tutorial",
        Description = "Educational content with clear visuals. Perfect for how-to videos.",
        Icon = "📚",
        Duration = TimeSpan.FromMinutes(3),
        TtsProvider = "Windows",
        LlmProvider = "RuleBased",
        ImageProvider = "Stock",
        Resolution = "1080p",
        AspectRatio = "16:9",
        VisualStyle = "professional",
        MusicGenre = "ambient",
        RequiresApiKey = false,
        WorksOffline = false,
        EstimatedCost = 0m,
        TargetPlatform = "youtube",
        OutputFormat = "mp4",
        IncludeCaptions = true
    };

    /// <summary>
    /// Social Media - Quick engaging content for multiple platforms.
    /// Square format works well on Instagram, Twitter, and LinkedIn.
    /// </summary>
    public static readonly PresetProfile SocialMedia = new()
    {
        Id = "social-media",
        Name = "Social Media",
        Description = "Quick engaging content for Instagram, Twitter, and LinkedIn.",
        Icon = "🎯",
        Duration = TimeSpan.FromSeconds(45),
        TtsProvider = "Windows",
        LlmProvider = "RuleBased",
        ImageProvider = "Stock",
        Resolution = "1080p",
        AspectRatio = "1:1",
        VisualStyle = "modern",
        MusicGenre = "upbeat",
        RequiresApiKey = false,
        WorksOffline = false,
        EstimatedCost = 0m,
        TargetPlatform = "social",
        OutputFormat = "mp4",
        IncludeCaptions = true
    };

    /// <summary>
    /// Gets all available presets as an immutable list.
    /// </summary>
    public static IReadOnlyList<PresetProfile> All { get; } = ImmutableList.Create(
        QuickDemo,
        YouTubeShort,
        Tutorial,
        SocialMedia
    );

    /// <summary>
    /// Gets a preset by its ID.
    /// </summary>
    /// <param name="id">The preset ID (e.g., "quick-demo")</param>
    /// <returns>The preset profile, or null if not found</returns>
    public static PresetProfile? GetById(string id)
    {
        foreach (var preset in All)
        {
            if (string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return preset;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets the default preset (Quick Demo) for first-run experience.
    /// </summary>
    public static PresetProfile Default => QuickDemo;
}
