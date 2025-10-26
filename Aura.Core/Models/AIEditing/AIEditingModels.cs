using System;
using System.Collections.Generic;

namespace Aura.Core.Models.AIEditing;

/// <summary>
/// Scene change detected in video
/// </summary>
public record SceneChange(
    TimeSpan Timestamp,
    int FrameIndex,
    double Confidence,
    string ChangeType,
    string Description);

/// <summary>
/// Result of scene detection analysis
/// </summary>
public record SceneDetectionResult(
    IReadOnlyList<SceneChange> Scenes,
    TimeSpan TotalDuration,
    int TotalFramesAnalyzed,
    string Summary);

/// <summary>
/// Highlight moment detected in footage
/// </summary>
public record HighlightMoment(
    TimeSpan StartTime,
    TimeSpan EndTime,
    double Score,
    string Type,
    string Reasoning,
    IReadOnlyList<string> Features);

/// <summary>
/// Result of highlight detection
/// </summary>
public record HighlightDetectionResult(
    IReadOnlyList<HighlightMoment> Highlights,
    TimeSpan TotalDuration,
    double AverageEngagement,
    string Summary);

/// <summary>
/// Beat detected in audio
/// </summary>
public record BeatPoint(
    TimeSpan Timestamp,
    double Strength,
    double Tempo,
    bool IsDownbeat);

/// <summary>
/// Result of beat detection
/// </summary>
public record BeatDetectionResult(
    IReadOnlyList<BeatPoint> Beats,
    double AverageTempo,
    TimeSpan Duration,
    int TotalBeats,
    string Summary);

/// <summary>
/// Auto-framing suggestion for footage
/// </summary>
public record FramingSuggestion(
    TimeSpan StartTime,
    TimeSpan Duration,
    int TargetWidth,
    int TargetHeight,
    int CropX,
    int CropY,
    int CropWidth,
    int CropHeight,
    double Confidence,
    string Reasoning);

/// <summary>
/// Result of auto-framing analysis
/// </summary>
public record AutoFramingResult(
    IReadOnlyList<FramingSuggestion> Suggestions,
    int SourceWidth,
    int SourceHeight,
    string Summary);

/// <summary>
/// B-roll placement suggestion
/// </summary>
public record BRollPlacement(
    TimeSpan InsertAt,
    TimeSpan Duration,
    string SearchQuery,
    string Context,
    double Relevance);

/// <summary>
/// Caption/subtitle with timing
/// </summary>
public record Caption(
    TimeSpan StartTime,
    TimeSpan EndTime,
    string Text,
    double Confidence);

/// <summary>
/// Result of speech recognition
/// </summary>
public record SpeechRecognitionResult(
    IReadOnlyList<Caption> Captions,
    TimeSpan Duration,
    string Language,
    double AverageConfidence,
    string Summary);

/// <summary>
/// Video stabilization result
/// </summary>
public record StabilizationResult(
    string StabilizedVideoPath,
    double SmoothingFactor,
    TimeSpan ProcessingTime,
    string Summary);

/// <summary>
/// Content-aware fill result
/// </summary>
public record ContentFillResult(
    string OutputVideoPath,
    TimeSpan ProcessingTime,
    IReadOnlyList<TimeSpan> AffectedFrames,
    string Summary);

/// <summary>
/// Smart trim suggestion
/// </summary>
public record TrimSuggestion(
    TimeSpan StartTime,
    TimeSpan EndTime,
    string Type,
    string Reasoning);

/// <summary>
/// Result of smart trim analysis
/// </summary>
public record SmartTrimResult(
    IReadOnlyList<TrimSuggestion> Suggestions,
    TimeSpan OriginalDuration,
    TimeSpan EstimatedNewDuration,
    string Summary);

/// <summary>
/// One-click video creation request
/// </summary>
public record VideoCreationRequest(
    string Script,
    string VoiceStyle,
    string VideoStyle,
    TimeSpan? TargetDuration);

/// <summary>
/// One-click video creation result
/// </summary>
public record VideoCreationResult(
    string VideoPath,
    TimeSpan Duration,
    IReadOnlyList<string> AssetsUsed,
    string Summary);
