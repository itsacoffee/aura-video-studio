using System;
using System.Collections.Generic;
using Aura.Core.Models.Voice;

namespace Aura.Core.Models.Audio;

/// <summary>
/// SSML planning request with target durations and constraints
/// </summary>
public record SSMLPlanningRequest
{
    /// <summary>
    /// Script lines to convert to SSML
    /// </summary>
    public required IReadOnlyList<ScriptLine> ScriptLines { get; init; }

    /// <summary>
    /// Target TTS provider for SSML generation
    /// </summary>
    public required VoiceProvider TargetProvider { get; init; }

    /// <summary>
    /// Voice specification for synthesis
    /// </summary>
    public required VoiceSpec VoiceSpec { get; init; }

    /// <summary>
    /// Target scene durations in seconds
    /// </summary>
    public required IReadOnlyDictionary<int, double> TargetDurations { get; init; }

    /// <summary>
    /// Duration tolerance (default ±2%)
    /// </summary>
    public double DurationTolerance { get; init; } = 0.02;

    /// <summary>
    /// Maximum fitting iterations (default 10)
    /// </summary>
    public int MaxFittingIterations { get; init; } = 10;

    /// <summary>
    /// Enable aggressive prosody adjustments
    /// </summary>
    public bool EnableAggressiveAdjustments { get; init; } = false;
}

/// <summary>
/// SSML planning result with provider-specific markup
/// </summary>
public record SSMLPlanningResult
{
    /// <summary>
    /// Generated SSML segments per scene
    /// </summary>
    public required IReadOnlyList<SSMLSegmentResult> Segments { get; init; }

    /// <summary>
    /// Duration fitting statistics
    /// </summary>
    public required DurationFittingStats Stats { get; init; }

    /// <summary>
    /// Any validation warnings
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Planning duration in milliseconds
    /// </summary>
    public long PlanningDurationMs { get; init; }
}

/// <summary>
/// SSML segment result for a single scene
/// </summary>
public record SSMLSegmentResult
{
    /// <summary>
    /// Scene index
    /// </summary>
    public required int SceneIndex { get; init; }

    /// <summary>
    /// Original text
    /// </summary>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Provider-specific SSML markup
    /// </summary>
    public required string SsmlMarkup { get; init; }

    /// <summary>
    /// Estimated duration in milliseconds
    /// </summary>
    public required int EstimatedDurationMs { get; init; }

    /// <summary>
    /// Target duration in milliseconds
    /// </summary>
    public required int TargetDurationMs { get; init; }

    /// <summary>
    /// Actual duration deviation percentage
    /// </summary>
    public double DeviationPercent { get; init; }

    /// <summary>
    /// Prosody adjustments applied
    /// </summary>
    public required ProsodyAdjustments Adjustments { get; init; }

    /// <summary>
    /// Timing markers for synchronization
    /// </summary>
    public IReadOnlyList<TimingMarker> TimingMarkers { get; init; } = Array.Empty<TimingMarker>();
}

/// <summary>
/// Prosody adjustments applied to a segment
/// </summary>
public record ProsodyAdjustments
{
    /// <summary>
    /// Speech rate multiplier (0.5 - 2.0)
    /// </summary>
    public double Rate { get; init; } = 1.0;

    /// <summary>
    /// Pitch adjustment in semitones
    /// </summary>
    public double Pitch { get; init; } = 0.0;

    /// <summary>
    /// Volume adjustment (0.0 - 2.0)
    /// </summary>
    public double Volume { get; init; } = 1.0;

    /// <summary>
    /// Pause insertions (position -> duration in ms)
    /// </summary>
    public IReadOnlyDictionary<int, int> Pauses { get; init; } = new Dictionary<int, int>();

    /// <summary>
    /// Emphasis markers (text spans)
    /// </summary>
    public IReadOnlyList<EmphasisSpan> Emphasis { get; init; } = Array.Empty<EmphasisSpan>();

    /// <summary>
    /// Number of adjustment iterations
    /// </summary>
    public int Iterations { get; init; }
}

/// <summary>
/// Emphasis span with strength
/// </summary>
public record EmphasisSpan(
    int StartPosition,
    int Length,
    Voice.EmphasisLevel Level
);

/// <summary>
/// Timing marker for synchronization
/// </summary>
public record TimingMarker(
    int OffsetMs,
    string Name,
    string? Metadata
);

/// <summary>
/// Duration fitting statistics
/// </summary>
public record DurationFittingStats
{
    /// <summary>
    /// Number of segments that required fitting
    /// </summary>
    public int SegmentsAdjusted { get; init; }

    /// <summary>
    /// Average fitting iterations per segment
    /// </summary>
    public double AverageFitIterations { get; init; }

    /// <summary>
    /// Maximum fitting iterations used
    /// </summary>
    public int MaxFitIterations { get; init; }

    /// <summary>
    /// Percentage of segments within tolerance
    /// </summary>
    public double WithinTolerancePercent { get; init; }

    /// <summary>
    /// Average deviation from target (percent)
    /// </summary>
    public double AverageDeviation { get; init; }

    /// <summary>
    /// Maximum deviation from target (percent)
    /// </summary>
    public double MaxDeviation { get; init; }

    /// <summary>
    /// Total target duration in seconds
    /// </summary>
    public double TargetDurationSeconds { get; init; }

    /// <summary>
    /// Total actual duration in seconds
    /// </summary>
    public double ActualDurationSeconds { get; init; }
}

/// <summary>
/// SSML validation result
/// </summary>
public record SSMLValidationResult
{
    /// <summary>
    /// Is the SSML valid for the target provider?
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Validation errors
    /// </summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Validation warnings
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Auto-repair suggestions
    /// </summary>
    public IReadOnlyList<SSMLRepairSuggestion> RepairSuggestions { get; init; } = Array.Empty<SSMLRepairSuggestion>();
}

/// <summary>
/// SSML auto-repair suggestion
/// </summary>
public record SSMLRepairSuggestion(
    string Issue,
    string Suggestion,
    bool CanAutoFix
);

/// <summary>
/// Provider-specific SSML constraints
/// </summary>
public record ProviderSSMLConstraints
{
    /// <summary>
    /// Supported SSML tags
    /// </summary>
    public required IReadOnlySet<string> SupportedTags { get; init; }

    /// <summary>
    /// Supported prosody attributes
    /// </summary>
    public required IReadOnlySet<string> SupportedProsodyAttributes { get; init; }

    /// <summary>
    /// Rate range (min, max)
    /// </summary>
    public (double Min, double Max) RateRange { get; init; } = (0.5, 2.0);

    /// <summary>
    /// Pitch range in semitones (min, max)
    /// </summary>
    public (double Min, double Max) PitchRange { get; init; } = (-12.0, 12.0);

    /// <summary>
    /// Volume range (min, max)
    /// </summary>
    public (double Min, double Max) VolumeRange { get; init; } = (0.0, 2.0);

    /// <summary>
    /// Maximum pause duration in milliseconds
    /// </summary>
    public int MaxPauseDurationMs { get; init; } = 10000;

    /// <summary>
    /// Supports timing markers
    /// </summary>
    public bool SupportsTimingMarkers { get; init; }

    /// <summary>
    /// Maximum text length per segment
    /// </summary>
    public int? MaxTextLength { get; init; }
}
