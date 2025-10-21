using System;
using System.Collections.Generic;

namespace Aura.Core.Models.EditingIntelligence;

/// <summary>
/// Type of cut point detected
/// </summary>
public enum CutPointType
{
    NaturalPause,
    SentenceBoundary,
    BreathPoint,
    FillerRemoval,
    SceneTransition,
    EmphasisPoint
}

/// <summary>
/// Suggested cut point in the timeline
/// </summary>
public record CutPoint(
    TimeSpan Timestamp,
    CutPointType Type,
    double Confidence,
    string Reasoning,
    TimeSpan? DurationToRemove = null);

/// <summary>
/// Issue type affecting pacing
/// </summary>
public enum PacingIssueType
{
    TooSlow,
    TooFast,
    Monotonous,
    PoorRhythm,
    InformationOverload,
    AttentionSpanExceeded
}

/// <summary>
/// Recommendation for scene pacing
/// </summary>
public record ScenePacingRecommendation(
    int SceneIndex,
    TimeSpan CurrentDuration,
    TimeSpan RecommendedDuration,
    double EngagementScore,
    PacingIssueType? IssueType,
    string Reasoning);

/// <summary>
/// Complete pacing analysis result
/// </summary>
public record PacingAnalysis(
    IReadOnlyList<ScenePacingRecommendation> SceneRecommendations,
    double OverallEngagementScore,
    IReadOnlyList<TimeSpan> SlowSegments,
    IReadOnlyList<TimeSpan> FastSegments,
    double ContentDensity,
    string Summary);

/// <summary>
/// Transition type for scene changes
/// </summary>
public enum TransitionType
{
    Cut,
    Fade,
    Dissolve,
    Wipe,
    Zoom,
    Slide,
    None
}

/// <summary>
/// Suggested transition between scenes
/// </summary>
public record TransitionSuggestion(
    int FromSceneIndex,
    int ToSceneIndex,
    TimeSpan Location,
    TransitionType Type,
    TimeSpan Duration,
    string Reasoning,
    double Confidence);

/// <summary>
/// Effect type that can be applied
/// </summary>
public enum EffectType
{
    SlowMotion,
    SpeedUp,
    Zoom,
    Pan,
    ColorGrade,
    Blur,
    Vignette,
    TextOverlay,
    SplitScreen
}

/// <summary>
/// Purpose of applying an effect
/// </summary>
public enum EffectPurpose
{
    Emphasis,
    Transition,
    Style,
    Correction,
    Engagement
}

/// <summary>
/// Suggested effect application
/// </summary>
public record EffectSuggestion(
    TimeSpan StartTime,
    TimeSpan Duration,
    EffectType EffectType,
    EffectPurpose Purpose,
    Dictionary<string, object> Parameters,
    string Reasoning,
    double Confidence);

/// <summary>
/// Engagement level at a point in time
/// </summary>
public record EngagementPoint(
    TimeSpan Timestamp,
    double PredictedEngagement,
    string Context);

/// <summary>
/// Complete engagement analysis
/// </summary>
public record EngagementCurve(
    IReadOnlyList<EngagementPoint> Points,
    double AverageEngagement,
    IReadOnlyList<TimeSpan> RetentionRisks,
    double HookStrength,
    double EndingImpact,
    IReadOnlyList<string> BoosterSuggestions);

/// <summary>
/// Quality issue severity level
/// </summary>
public enum QualityIssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Type of quality issue detected
/// </summary>
public enum QualityIssueType
{
    LowResolution,
    AudioClipping,
    ColorInconsistency,
    BlackFrame,
    AudioDesync,
    ContinuityError,
    RenderingArtifact,
    MissingAsset
}

/// <summary>
/// Detected quality issue
/// </summary>
public record QualityIssue(
    QualityIssueType Type,
    QualityIssueSeverity Severity,
    TimeSpan? Location,
    string Description,
    string? FixSuggestion);

/// <summary>
/// User decision on an AI suggestion
/// </summary>
public enum EditingDecisionAction
{
    Accepted,
    Rejected,
    Modified,
    Ignored
}

/// <summary>
/// Record of user decision on editing suggestion
/// </summary>
public record EditingDecision(
    string SuggestionId,
    string SuggestionType,
    EditingDecisionAction Action,
    DateTime Timestamp,
    string? Notes);

/// <summary>
/// Request to analyze timeline
/// </summary>
public record AnalyzeTimelineRequest(
    string JobId,
    bool IncludeCutPoints = true,
    bool IncludePacing = true,
    bool IncludeEngagement = true,
    bool IncludeQuality = true);

/// <summary>
/// Complete timeline analysis result
/// </summary>
public record TimelineAnalysisResult(
    IReadOnlyList<CutPoint>? CutPoints,
    PacingAnalysis? PacingAnalysis,
    EngagementCurve? EngagementAnalysis,
    IReadOnlyList<QualityIssue>? QualityIssues,
    IReadOnlyList<string> GeneralRecommendations);

/// <summary>
/// Request to optimize pacing
/// </summary>
public record EditingPacingRequest(
    string JobId,
    TimeSpan? TargetDuration = null,
    string? PacingStyle = null);

/// <summary>
/// Request to suggest scene sequencing
/// </summary>
public record SequenceScenesRequest(
    string JobId,
    string NarrativeStyle);

/// <summary>
/// Scene sequencing result
/// </summary>
public record SceneSequencingResult(
    IReadOnlyList<int> RecommendedOrder,
    string Reasoning,
    IReadOnlyList<string> AlternativeApproaches);

/// <summary>
/// Request for auto-assembly
/// </summary>
public record AutoAssembleRequest(
    string JobId,
    TimeSpan? TargetDuration = null,
    string? EditingStyle = null);

/// <summary>
/// Request to optimize for target duration
/// </summary>
public record OptimizeDurationRequest(
    string JobId,
    TimeSpan TargetDuration,
    string Strategy);
