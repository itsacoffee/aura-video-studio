using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Script analysis results with quality scores and suggestions
/// </summary>
public record ScriptAnalysis(
    double CoherenceScore,
    double PacingScore,
    double EngagementScore,
    double ReadabilityScore,
    double OverallQualityScore,
    List<string> Issues,
    List<string> Suggestions,
    ScriptStatistics Statistics);

/// <summary>
/// Statistical information about a script
/// </summary>
public record ScriptStatistics(
    int TotalWordCount,
    double AverageWordsPerScene,
    TimeSpan EstimatedReadingTime,
    double ComplexityScore);

/// <summary>
/// Enhanced script with improvements and diff information
/// </summary>
public record EnhancedScript(
    string NewScript,
    List<DiffChange> Changes,
    string ImprovementSummary);

/// <summary>
/// Represents a change in the script diff
/// </summary>
public record DiffChange(
    string Type, // "added", "removed", "modified"
    int LineNumber,
    string OriginalText,
    string NewText);

/// <summary>
/// Options for script enhancement
/// </summary>
public record EnhancementOptions(
    bool FixCoherence = false,
    bool IncreaseEngagement = false,
    bool ImproveClarity = false,
    bool AddDetails = false);

/// <summary>
/// Suggestion for visual assets for a scene
/// </summary>
public record AssetSuggestion(
    string Keyword,
    string Description,
    List<AssetMatch> Matches);

/// <summary>
/// A matching asset with relevance information
/// </summary>
public record AssetMatch(
    string FilePath,
    string Url,
    double RelevanceScore,
    string ThumbnailUrl);

/// <summary>
/// Pacing optimization results for a timeline
/// </summary>
public record PacingOptimization(
    List<ScenePacingSuggestion> Suggestions,
    string OverallAssessment);

/// <summary>
/// Pacing suggestion for a specific scene
/// </summary>
public record ScenePacingSuggestion(
    int SceneIndex,
    TimeSpan CurrentDuration,
    TimeSpan SuggestedDuration,
    string Reasoning,
    PacingPriority Priority);

/// <summary>
/// Priority level for pacing adjustments
/// </summary>
public enum PacingPriority
{
    Optional,
    Recommended,
    Critical
}
