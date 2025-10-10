using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

public record Brief(string Topic, string? Audience, string? Goal, string Tone, string Language, Aspect Aspect);

public record PlanSpec(TimeSpan TargetDuration, Pacing Pacing, Density Density, string Style);

public record VoiceSpec(string VoiceName, double Rate, double Pitch, PauseStyle Pause);

public record Scene(int Index, string Heading, string Script, TimeSpan Start, TimeSpan Duration);

public record ScriptLine(int SceneIndex, string Text, TimeSpan Start, TimeSpan Duration);

public record Asset(string Kind, string PathOrUrl, string? License, string? Attribution);

public record Resolution(int Width, int Height);

public record RenderSpec(
    Resolution Res, 
    string Container, 
    int VideoBitrateK, 
    int AudioBitrateK,
    int Fps = 30,
    string Codec = "H264",
    int QualityLevel = 75,
    bool EnableSceneCut = true);

public record RenderProgress(float Percentage, TimeSpan Elapsed, TimeSpan Remaining, string CurrentStage);

public record SystemProfile
{
    public bool AutoDetect { get; init; } = true;
    public int LogicalCores { get; init; }
    public int PhysicalCores { get; init; }
    public int RamGB { get; init; }
    public GpuInfo? Gpu { get; init; }
    public HardwareTier Tier { get; init; }
    public bool EnableNVENC { get; init; }
    public bool EnableSD { get; init; }
    public bool OfflineOnly { get; init; }
    
    // Manual overrides (per spec: RAM 8-256 GB, cores 2-32+, GPU presets)
    public HardwareOverrides? Overrides { get; init; }
}

/// <summary>
/// Manual hardware overrides for users who want to customize detection results
/// Spec: RAM (8-256 GB), cores (2-32+), GPU presets (NVIDIA 50/40/30/20/16/10 series, AMD RX, Intel Arc)
/// </summary>
public record HardwareOverrides
{
    public int? ManualRamGB { get; init; }  // 8-256 GB
    public int? ManualLogicalCores { get; init; }  // 2-32+
    public int? ManualPhysicalCores { get; init; }  // 2-32+
    public string? ManualGpuPreset { get; init; }  // e.g., "NVIDIA RTX 3080", "AMD RX 6800", "Intel Arc A770"
    public bool? ForceEnableNVENC { get; init; }
    public bool? ForceEnableSD { get; init; }
    public bool? ForceOfflineMode { get; init; }
}

public record GpuInfo(string Vendor, string Model, int VramGB, string? Series);

/// <summary>
/// Request for planner recommendations
/// </summary>
public record RecommendationRequest(
    Brief Brief,
    PlanSpec PlanSpec,
    string? AudiencePersona,
    RecommendationConstraints? Constraints);

/// <summary>
/// Constraints for recommendations
/// </summary>
public record RecommendationConstraints(
    int? MaxSceneCount,
    int? MinSceneCount,
    double? MaxBRollPercentage,
    int? MaxReadingLevel);

/// <summary>
/// Comprehensive recommendations for video production
/// </summary>
public record PlannerRecommendations(
    string Outline,
    int SceneCount,
    int ShotsPerScene,
    double BRollPercentage,
    int OverlayDensity,
    int ReadingLevel,
    VoiceRecommendations Voice,
    MusicRecommendations Music,
    CaptionStyle Captions,
    string ThumbnailPrompt,
    SeoRecommendations Seo);

/// <summary>
/// Voice recommendations
/// </summary>
public record VoiceRecommendations(
    double Rate,
    double Pitch,
    string Style);

/// <summary>
/// Music recommendations
/// </summary>
public record MusicRecommendations(
    string Tempo,
    string IntensityCurve,
    string Genre);

/// <summary>
/// Caption style recommendations
/// </summary>
public record CaptionStyle(
    string Position,
    string FontSize,
    bool HighlightKeywords);

/// <summary>
/// SEO recommendations
/// </summary>
public record SeoRecommendations(
    string Title,
    string Description,
    string[] Tags);

/// <summary>
/// Brand kit settings for visual customization (watermark, colors, etc.)
/// </summary>
public record BrandKit(
    string? WatermarkPath,
    string? WatermarkPosition,
    float WatermarkOpacity,
    string? BrandColor,
    string? AccentColor);