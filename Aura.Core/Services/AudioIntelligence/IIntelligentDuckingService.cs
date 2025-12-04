using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Interface for intelligent audio ducking service.
/// Analyzes narration to detect silence and applies appropriate ducking profiles.
/// </summary>
public interface IIntelligentDuckingService
{
    /// <summary>
    /// Analyzes narration audio to detect silence segments and speech patterns.
    /// </summary>
    /// <param name="narrationPath">Path to the narration audio file</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Analysis result with silence segments and speech patterns</returns>
    Task<NarrationAnalysis> AnalyzeNarrationAsync(
        string narrationPath,
        CancellationToken ct = default);

    /// <summary>
    /// Plans ducking profile based on narration analysis and content type.
    /// </summary>
    /// <param name="analysis">Narration analysis result</param>
    /// <param name="contentType">Type of content (educational, entertainment, etc.)</param>
    /// <returns>Ducking plan with profile and segment-specific settings</returns>
    DuckingPlan PlanDucking(NarrationAnalysis analysis, string? contentType = null);

    /// <summary>
    /// Applies intelligent ducking to mix narration and music.
    /// </summary>
    /// <param name="narrationPath">Path to narration audio</param>
    /// <param name="musicPath">Path to background music</param>
    /// <param name="plan">Ducking plan from PlanDucking</param>
    /// <param name="outputPath">Path for the mixed output</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Path to the mixed audio file</returns>
    Task<string> ApplyDuckingAsync(
        string narrationPath,
        string musicPath,
        DuckingPlan plan,
        string outputPath,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the default ducking profile for a content type.
    /// </summary>
    DuckingProfile GetDefaultProfile(DuckingProfileType profileType);
}

/// <summary>
/// Ducking profile type presets.
/// </summary>
public enum DuckingProfileType
{
    /// <summary>
    /// Aggressive ducking - music almost silent during speech.
    /// Good for educational content where voice clarity is critical.
    /// </summary>
    Aggressive,

    /// <summary>
    /// Balanced ducking - music reduced but still audible.
    /// Good for most video content.
    /// </summary>
    Balanced,

    /// <summary>
    /// Gentle ducking - music only slightly reduced.
    /// Good for ambient/atmospheric content.
    /// </summary>
    Gentle,

    /// <summary>
    /// Dynamic ducking - adapts based on speech intensity.
    /// Most sophisticated option, adjusts in real-time.
    /// </summary>
    Dynamic
}

/// <summary>
/// Result of narration analysis.
/// </summary>
public record NarrationAnalysis(
    TimeSpan TotalDuration,
    List<SilenceSegment> SilenceSegments,
    List<SpeechSegment> SpeechSegments,
    double AverageLoudness,
    double NoiseFloor,
    bool HasClipping,
    double SpeechToSilenceRatio
);

/// <summary>
/// A detected silence segment in the narration.
/// </summary>
public record SilenceSegment(
    TimeSpan Start,
    TimeSpan End,
    double NoiseLevel
)
{
    public TimeSpan Duration => End - Start;
}

/// <summary>
/// A detected speech segment in the narration.
/// </summary>
public record SpeechSegment(
    TimeSpan Start,
    TimeSpan End,
    double AverageLoudness,
    double PeakLoudness
)
{
    public TimeSpan Duration => End - Start;
}

/// <summary>
/// Ducking profile with specific parameters.
/// </summary>
public record DuckingProfile(
    DuckingProfileType Type,
    double DuckDepthDb,
    TimeSpan AttackTime,
    TimeSpan ReleaseTime,
    double Threshold,
    double Ratio,
    double MusicBaseVolume
);

/// <summary>
/// Complete ducking plan for a video.
/// </summary>
public record DuckingPlan(
    DuckingProfile Profile,
    List<DuckingSegment> Segments,
    string FFmpegFilter,
    string Reasoning
);

/// <summary>
/// A segment-specific ducking instruction.
/// </summary>
public record DuckingSegment(
    TimeSpan Start,
    TimeSpan End,
    double MusicVolume,
    string Reason
);
