using System;
using System.Collections.Generic;
using Aura.Core.Models.Audience;
using Aura.Core.Models.RAG;

namespace Aura.Core.Models;

/// <summary>
/// LLM generation parameters for fine-tuning model behavior.
/// </summary>
/// <param name="Temperature">Controls randomness in output generation (0.0-2.0)</param>
/// <param name="TopP">Controls nucleus sampling probability (0.0-1.0)</param>
/// <param name="TopK">Limits tokens to consider at each step</param>
/// <param name="MaxTokens">Maximum tokens to generate</param>
/// <param name="FrequencyPenalty">Penalty for token frequency</param>
/// <param name="PresencePenalty">Penalty for token presence</param>
/// <param name="StopSequences">Sequences that stop generation</param>
/// <param name="ModelOverride">Override the default model</param>
/// <param name="ResponseFormat">Desired response format (e.g., "json" for structured output, null for plain text).
/// When set to "json", instructs compatible providers to return JSON-formatted responses.
/// Leave null for plain text responses like translations.
/// Currently supported by: Ollama (with "json" format). Other providers may ignore this parameter.
/// If an unsupported value is passed, providers may log a warning and proceed without format constraint.</param>
public record LlmParameters(
    double? Temperature = null,
    double? TopP = null,
    int? TopK = null,
    int? MaxTokens = null,
    double? FrequencyPenalty = null,
    double? PresencePenalty = null,
    List<string>? StopSequences = null,
    string? ModelOverride = null,
    string? ResponseFormat = null);

/// <summary>
/// Brief configuration for video generation with optional prompt customization
/// Supports both simple string-based audience and rich structured AudienceProfile
/// </summary>
public record Brief(
    string Topic, 
    string? Audience, 
    string? Goal, 
    string Tone, 
    string Language, 
    Aspect Aspect,
    PromptModifiers? PromptModifiers = null,
    AudienceProfile? AudienceProfile = null,
    RagConfiguration? RagConfiguration = null,
    LlmParameters? LlmParameters = null);

/// <summary>
/// User customization options for prompt engineering
/// </summary>
public record PromptModifiers(
    string? AdditionalInstructions = null,
    string? ExampleStyle = null,
    bool EnableChainOfThought = false,
    string? PromptVersion = null);

public record PlanSpec(
    TimeSpan TargetDuration, 
    Pacing Pacing, 
    Density Density, 
    string Style,
    ScriptRefinementConfig? RefinementConfig = null,
    int? MinSceneCount = null,
    int? MaxSceneCount = null,
    int? TargetSceneCount = null)
{
    // Scene duration constants based on density (in seconds)
    private const int SparseSecondsPerScene = 20;
    private const int BalancedSecondsPerScene = 12;
    private const int DenseSecondsPerScene = 8;
    private const int DefaultMinScenes = 3;
    private const int DefaultMaxScenes = 20;

    /// <summary>
    /// Calculates the target scene count based on duration and density.
    /// Uses density to determine seconds per scene:
    /// - Sparse: 20 seconds per scene
    /// - Balanced: 12 seconds per scene  
    /// - Dense: 8 seconds per scene
    /// </summary>
    public int GetCalculatedSceneCount()
    {
        var secondsPerScene = Density switch
        {
            Density.Sparse => SparseSecondsPerScene,
            Density.Balanced => BalancedSecondsPerScene,
            Density.Dense => DenseSecondsPerScene,
            _ => BalancedSecondsPerScene
        };
        
        var calculated = (int)Math.Ceiling(TargetDuration.TotalSeconds / secondsPerScene);
        
        // Apply min/max bounds
        var minScenes = MinSceneCount ?? DefaultMinScenes;
        var maxScenes = MaxSceneCount ?? DefaultMaxScenes;
        
        // If target is explicitly set, use it within bounds
        if (TargetSceneCount.HasValue)
        {
            return Math.Clamp(TargetSceneCount.Value, minScenes, maxScenes);
        }
        
        return Math.Clamp(calculated, minScenes, maxScenes);
    }
}

public record VoiceSpec(string VoiceName, double Rate, double Pitch, PauseStyle Pause);

public record Scene(
    int Index, 
    string Heading, 
    string Script, 
    TimeSpan Start, 
    TimeSpan Duration,
    List<Citation>? Citations = null);

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
    bool EnableSceneCut = true,
    ScriptRefinementConfig? RefinementConfig = null);

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
    SeoRecommendations Seo,
    double QualityScore = 0.75,
    string? ProviderUsed = null,
    string? ExplainabilityNotes = null);

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

/// <summary>
/// RAG (Retrieval-Augmented Generation) configuration for script grounding
/// </summary>
public record RagConfiguration(
    bool Enabled,
    int TopK = 5,
    float MinimumScore = 0.6f,
    int MaxContextTokens = 2000,
    bool IncludeCitations = true,
    bool TightenClaims = false);