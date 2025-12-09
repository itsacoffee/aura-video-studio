using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Interface for comprehensive voice enhancement service.
/// Analyzes voice audio and applies enhancements via FFmpeg filters.
/// </summary>
public interface IVoiceEnhancementService
{
    /// <summary>
    /// Analyzes voice audio for quality characteristics.
    /// </summary>
    /// <param name="audioPath">Path to the voice audio file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Analysis result with quality metrics</returns>
    Task<VoiceAnalysis> AnalyzeVoiceAsync(
        string audioPath,
        CancellationToken ct = default);

    /// <summary>
    /// Applies enhancements to voice audio based on analysis.
    /// </summary>
    /// <param name="audioPath">Path to the input audio file</param>
    /// <param name="outputPath">Path for the enhanced output</param>
    /// <param name="options">Enhancement options</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with output path and applied enhancements</returns>
    Task<VoiceEnhancementResult> EnhanceVoiceAsync(
        string audioPath,
        string outputPath,
        VoiceEnhancementOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a preset enhancement configuration.
    /// </summary>
    VoiceEnhancementOptions GetPreset(VoiceEnhancementPreset preset);

    /// <summary>
    /// Generates an FFmpeg filter chain for the given options.
    /// </summary>
    string BuildFilterChain(VoiceEnhancementOptions options);
}

/// <summary>
/// Voice enhancement preset types.
/// </summary>
public enum VoiceEnhancementPreset
{
    /// <summary>
    /// Light enhancement - minimal processing.
    /// </summary>
    Light,

    /// <summary>
    /// Standard enhancement - balanced processing.
    /// </summary>
    Standard,

    /// <summary>
    /// Broadcast quality - professional-grade processing.
    /// </summary>
    Broadcast,

    /// <summary>
    /// Podcast optimized - for spoken word content.
    /// </summary>
    Podcast,

    /// <summary>
    /// Video narration - optimized for mixing with music/sfx.
    /// </summary>
    VideoNarration
}

/// <summary>
/// Voice audio analysis result.
/// </summary>
public record VoiceAnalysis(
    TimeSpan Duration,
    double AverageLoudness,
    double IntegratedLoudness,
    double TruePeak,
    double LoudnessRange,
    double NoiseFloor,
    bool HasClipping,
    bool HasExcessiveNoise,
    bool NeedsNormalization,
    List<string> Issues,
    List<string> Recommendations
);

/// <summary>
/// Options for voice enhancement processing.
/// </summary>
public record VoiceEnhancementOptions(
    bool EnableNoiseReduction,
    double NoiseReductionStrength,
    bool EnableHighPassFilter,
    double HighPassFrequency,
    bool EnableLowPassFilter,
    double LowPassFrequency,
    bool EnableCompression,
    double CompressionThreshold,
    double CompressionRatio,
    double CompressionAttack,
    double CompressionRelease,
    double CompressionMakeup,
    bool EnableEQ,
    double PresenceBoost,
    double BassRolloff,
    double DeEsserReduction,
    bool EnableLoudnessNormalization,
    double TargetLUFS,
    double TargetTruePeak,
    bool EnableDeClip,
    bool EnableDeClick
);

/// <summary>
/// Result of voice enhancement processing.
/// </summary>
public record VoiceEnhancementResult(
    string OutputPath,
    List<string> AppliedEnhancements,
    VoiceAnalysis BeforeAnalysis,
    VoiceAnalysis? AfterAnalysis,
    TimeSpan ProcessingTime,
    bool Success,
    string? ErrorMessage
);
