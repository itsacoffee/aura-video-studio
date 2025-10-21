using System;
using System.Collections.Generic;

namespace Aura.Core.AI.Pacing;

/// <summary>
/// Represents the analysis results for video pacing optimization.
/// </summary>
public record PacingAnalysisResult(
    TimeSpan OptimalDuration,
    double EngagementScore,
    IReadOnlyList<ScenePacingRecommendation> SceneRecommendations,
    IReadOnlyList<TransitionPoint> SuggestedTransitions,
    string NarrativeArcAssessment,
    IReadOnlyList<string> Warnings
);

/// <summary>
/// Pacing recommendation for a specific scene.
/// </summary>
public record ScenePacingRecommendation(
    int SceneIndex,
    TimeSpan CurrentDuration,
    TimeSpan RecommendedDuration,
    double ImportanceScore,
    double ComplexityScore,
    string Reasoning
);

/// <summary>
/// Suggested transition point in the video timeline.
/// </summary>
public record TransitionPoint(
    TimeSpan Timestamp,
    TransitionType Type,
    double Confidence,
    string Context
);

/// <summary>
/// Types of transitions detected or suggested.
/// </summary>
public enum TransitionType
{
    NaturalPause,
    SceneChange,
    MusicBeat,
    EmphasisPoint,
    BRollInsert
}

/// <summary>
/// Attention curve prediction for viewer engagement.
/// </summary>
public record AttentionCurve(
    IReadOnlyList<AttentionPoint> Points,
    double AverageEngagement,
    IReadOnlyList<TimeSpan> CriticalDropPoints
);

/// <summary>
/// Attention level at a specific point in time.
/// </summary>
public record AttentionPoint(
    TimeSpan Timestamp,
    double AttentionLevel
);

/// <summary>
/// Content template for different video formats.
/// </summary>
public record ContentTemplate(
    string Name,
    string Description,
    VideoFormat Format,
    PacingParameters Parameters
);

/// <summary>
/// Video format types.
/// </summary>
public enum VideoFormat
{
    Explainer,
    Tutorial,
    Vlog,
    Review,
    Educational,
    Entertainment
}

/// <summary>
/// Pacing parameters for optimization.
/// </summary>
public record PacingParameters(
    double MinSceneDuration,
    double MaxSceneDuration,
    double AverageSceneDuration,
    double TransitionDensity,
    double HookDuration,
    bool MusicSyncEnabled
);

/// <summary>
/// Viewer retention prediction results.
/// </summary>
public record RetentionPrediction(
    double OverallRetentionScore,
    IReadOnlyList<RetentionSegment> Segments,
    IReadOnlyList<TimeSpan> HighDropRiskPoints
);

/// <summary>
/// Retention data for a video segment.
/// </summary>
public record RetentionSegment(
    TimeSpan Start,
    TimeSpan End,
    double PredictedRetention,
    string RiskFactors
);

/// <summary>
/// Rhythm analysis results.
/// </summary>
public record RhythmAnalysis(
    double OverallRhythmScore,
    IReadOnlyList<BeatPoint> BeatPoints,
    IReadOnlyList<PhraseSegment> Phrases,
    bool IsMusicSyncRecommended
);

/// <summary>
/// Musical beat or rhythm point.
/// </summary>
public record BeatPoint(
    TimeSpan Timestamp,
    double Strength,
    int Tempo
);

/// <summary>
/// Musical or narrative phrase segment.
/// </summary>
public record PhraseSegment(
    TimeSpan Start,
    TimeSpan End,
    string Type
);
