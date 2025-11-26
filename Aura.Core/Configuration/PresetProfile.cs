using System;

namespace Aura.Core.Configuration;

/// <summary>
/// Represents a preset profile for video generation settings.
/// These presets provide prosumer-friendly defaults for common use cases.
/// </summary>
public class PresetProfile
{
    /// <summary>
    /// Unique identifier for the preset (e.g., "quick-demo", "youtube-short")
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// User-friendly display name for the preset
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of what this preset is optimized for
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Icon or emoji to display in the UI
    /// </summary>
    public string Icon { get; init; } = "📹";

    /// <summary>
    /// Target duration for videos using this preset
    /// </summary>
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// TTS provider to use (Windows, Piper, ElevenLabs, etc.)
    /// </summary>
    public string TtsProvider { get; init; } = "Windows";

    /// <summary>
    /// LLM provider to use (RuleBased, Ollama, OpenAI, etc.)
    /// </summary>
    public string LlmProvider { get; init; } = "RuleBased";

    /// <summary>
    /// Image provider to use (Placeholder, Stock, LocalSD, etc.)
    /// </summary>
    public string ImageProvider { get; init; } = "Placeholder";

    /// <summary>
    /// Output resolution (720p, 1080p, 4k)
    /// </summary>
    public string Resolution { get; init; } = "720p";

    /// <summary>
    /// Aspect ratio for the output video (16:9, 9:16, 1:1)
    /// </summary>
    public string AspectRatio { get; init; } = "16:9";

    /// <summary>
    /// Visual style preset (modern, minimal, cinematic, etc.)
    /// </summary>
    public string VisualStyle { get; init; } = "modern";

    /// <summary>
    /// Music genre for background music (ambient, upbeat, dramatic, none)
    /// </summary>
    public string MusicGenre { get; init; } = "ambient";

    /// <summary>
    /// Whether this preset requires any API keys or external services
    /// </summary>
    public bool RequiresApiKey { get; init; } = false;

    /// <summary>
    /// Whether this preset works completely offline
    /// </summary>
    public bool WorksOffline { get; init; } = true;

    /// <summary>
    /// Estimated cost per video (0 for free presets)
    /// </summary>
    public decimal EstimatedCost { get; init; } = 0m;

    /// <summary>
    /// Target platform this preset is optimized for
    /// </summary>
    public string TargetPlatform { get; init; } = "general";

    /// <summary>
    /// Video output format
    /// </summary>
    public string OutputFormat { get; init; } = "mp4";

    /// <summary>
    /// Whether to include captions by default
    /// </summary>
    public bool IncludeCaptions { get; init; } = true;
}
