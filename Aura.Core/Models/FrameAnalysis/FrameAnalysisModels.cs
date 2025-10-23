using System;
using System.Collections.Generic;

namespace Aura.Core.Models.FrameAnalysis;

/// <summary>
/// Options for frame analysis
/// </summary>
public record FrameAnalysisOptions(
    int? MaxFramesToAnalyze = null,
    double MinimumImportanceThreshold = 0.5,
    bool ExtractKeyFramesOnly = false,
    FrameQuality Quality = FrameQuality.Medium
);

/// <summary>
/// Information about a single frame
/// </summary>
public record FrameInfo(
    int Index,
    TimeSpan Timestamp,
    bool IsKeyFrame,
    int Width,
    int Height
);

/// <summary>
/// Result of frame analysis
/// </summary>
public record FrameAnalysisResult(
    int TotalFrames,
    int AnalyzedFrames,
    List<FrameInfo> KeyFrames,
    Dictionary<int, double> ImportanceScores,
    List<FrameRecommendation> Recommendations,
    TimeSpan ProcessingTime
);

/// <summary>
/// Recommendation for a specific frame
/// </summary>
public record FrameRecommendation(
    int FrameIndex,
    TimeSpan Timestamp,
    double ImportanceScore,
    RecommendationType RecommendationType,
    string Reasoning
);

/// <summary>
/// Quality settings for frame extraction
/// </summary>
public enum FrameQuality
{
    Low,
    Medium,
    High,
    Maximum
}

/// <summary>
/// Type of recommendation for frame usage
/// </summary>
public enum RecommendationType
{
    HighlightMoment,
    VisualInterest,
    TransitionPoint,
    ThumbnailCandidate,
    SkipRecommended
}
