using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Narrative;

/// <summary>
/// Result of comprehensive narrative flow analysis for a video
/// </summary>
public record NarrativeAnalysisResult
{
    public IReadOnlyList<ScenePairCoherence> PairwiseCoherence { get; init; } = Array.Empty<ScenePairCoherence>();
    public NarrativeArcValidation ArcValidation { get; init; } = new();
    public IReadOnlyList<ContinuityIssue> ContinuityIssues { get; init; } = Array.Empty<ContinuityIssue>();
    public IReadOnlyList<BridgingSuggestion> BridgingSuggestions { get; init; } = Array.Empty<BridgingSuggestion>();
    public double OverallCoherenceScore { get; init; }
    public TimeSpan AnalysisDuration { get; init; }
}

/// <summary>
/// Coherence analysis between two consecutive scenes
/// </summary>
public record ScenePairCoherence
{
    public int FromSceneIndex { get; init; }
    public int ToSceneIndex { get; init; }
    public double CoherenceScore { get; init; }
    public string Reasoning { get; init; } = string.Empty;
    public IReadOnlyList<string> ConnectionTypes { get; init; } = Array.Empty<string>();
    public double ConfidenceScore { get; init; }
    public bool RequiresBridging { get; init; }
}

/// <summary>
/// Validation of narrative arc structure for the video
/// </summary>
public record NarrativeArcValidation
{
    public string VideoType { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public string DetectedStructure { get; init; } = string.Empty;
    public string ExpectedStructure { get; init; } = string.Empty;
    public IReadOnlyList<string> StructuralIssues { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Recommendations { get; init; } = Array.Empty<string>();
    public string Reasoning { get; init; } = string.Empty;
}

/// <summary>
/// Continuity issue detected in the narrative flow
/// </summary>
public record ContinuityIssue
{
    public int SceneIndex { get; init; }
    public string IssueType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>
/// Suggestion for bridging content between scenes
/// </summary>
public record BridgingSuggestion
{
    public int FromSceneIndex { get; init; }
    public int ToSceneIndex { get; init; }
    public string BridgingText { get; init; } = string.Empty;
    public string Rationale { get; init; } = string.Empty;
    public double CoherenceImprovement { get; init; }
}

/// <summary>
/// Scene reordering suggestion with optimization details
/// </summary>
public record SceneReorderingSuggestion
{
    public IReadOnlyList<int> OriginalOrder { get; init; } = Array.Empty<int>();
    public IReadOnlyList<int> SuggestedOrder { get; init; } = Array.Empty<int>();
    public double OriginalCoherence { get; init; }
    public double ImprovedCoherence { get; init; }
    public double CoherenceGain { get; init; }
    public TimeSpan OriginalDuration { get; init; }
    public TimeSpan AdjustedDuration { get; init; }
    public double DurationChangePercent { get; init; }
    public string Rationale { get; init; } = string.Empty;
    public bool MaintainsDurationConstraint { get; init; }
}

/// <summary>
/// Video type for narrative arc validation
/// </summary>
public enum VideoType
{
    Educational,
    Entertainment,
    Documentary,
    Tutorial,
    General
}

/// <summary>
/// Connection type between scenes
/// </summary>
public static class ConnectionType
{
    public const string Causal = "causal";
    public const string Thematic = "thematic";
    public const string Prerequisite = "prerequisite";
    public const string Callback = "callback";
    public const string Sequential = "sequential";
    public const string Contrast = "contrast";
}

/// <summary>
/// Severity levels for continuity issues
/// </summary>
public static class IssueSeverity
{
    public const string Critical = "critical";
    public const string Warning = "warning";
    public const string Info = "info";
}
