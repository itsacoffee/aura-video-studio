using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;

namespace Aura.Core.Providers;

/// <summary>
/// Provider interface for script critique with structured rubric-based scoring
/// </summary>
public interface ICriticProvider
{
    /// <summary>
    /// Generate structured critique of a script based on rubrics
    /// </summary>
    /// <param name="script">Script to critique</param>
    /// <param name="brief">Original brief context</param>
    /// <param name="spec">Plan specification</param>
    /// <param name="rubrics">Evaluation rubrics to apply</param>
    /// <param name="currentMetrics">Current quality metrics for context</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Structured critique with scores and feedback</returns>
    Task<CritiqueResult> CritiqueScriptAsync(
        string script,
        Brief brief,
        PlanSpec spec,
        IReadOnlyList<RefinementRubric> rubrics,
        ScriptQualityMetrics? currentMetrics,
        CancellationToken ct);

    /// <summary>
    /// Assess timing fit between script word count and target duration
    /// </summary>
    /// <param name="script">Script to analyze</param>
    /// <param name="targetDuration">Target duration</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Timing analysis result</returns>
    Task<TimingAnalysis> AnalyzeTimingFitAsync(
        string script,
        System.TimeSpan targetDuration,
        CancellationToken ct);
}

/// <summary>
/// Result of script critique
/// </summary>
public record CritiqueResult
{
    /// <summary>
    /// Overall quality score (0-100)
    /// </summary>
    public double OverallScore { get; init; }

    /// <summary>
    /// Scores per rubric
    /// </summary>
    public Dictionary<string, double> RubricScores { get; init; } = new();

    /// <summary>
    /// Structured issues identified
    /// </summary>
    public List<CritiqueIssue> Issues { get; init; } = new();

    /// <summary>
    /// Specific strengths identified
    /// </summary>
    public List<string> Strengths { get; init; } = new();

    /// <summary>
    /// Targeted improvement suggestions
    /// </summary>
    public List<CritiqueSuggestion> Suggestions { get; init; } = new();

    /// <summary>
    /// Timing analysis
    /// </summary>
    public TimingAnalysis? TimingAnalysis { get; init; }

    /// <summary>
    /// Raw critique text
    /// </summary>
    public string RawCritique { get; init; } = string.Empty;
}

/// <summary>
/// Specific issue identified by critic
/// </summary>
public record CritiqueIssue
{
    /// <summary>
    /// Issue category (clarity, coherence, timing, etc.)
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Issue description
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Severity (High, Medium, Low)
    /// </summary>
    public string Severity { get; init; } = "Medium";

    /// <summary>
    /// Location in script (if applicable)
    /// </summary>
    public string? Location { get; init; }
}

/// <summary>
/// Targeted suggestion for improvement
/// </summary>
public record CritiqueSuggestion
{
    /// <summary>
    /// Type of change (rewrite, expand, condense, reorder, etc.)
    /// </summary>
    public string ChangeType { get; init; } = string.Empty;

    /// <summary>
    /// Target location or section
    /// </summary>
    public string Target { get; init; } = string.Empty;

    /// <summary>
    /// Specific suggestion text
    /// </summary>
    public string Suggestion { get; init; } = string.Empty;

    /// <summary>
    /// Expected impact on quality
    /// </summary>
    public string ExpectedImpact { get; init; } = string.Empty;
}

/// <summary>
/// Analysis of script timing fit
/// </summary>
public record TimingAnalysis
{
    /// <summary>
    /// Estimated word count
    /// </summary>
    public int WordCount { get; init; }

    /// <summary>
    /// Target word count based on duration
    /// </summary>
    public int TargetWordCount { get; init; }

    /// <summary>
    /// Variance from target (percentage)
    /// </summary>
    public double Variance { get; init; }

    /// <summary>
    /// Whether timing fits within acceptable range
    /// </summary>
    public bool WithinAcceptableRange { get; init; }

    /// <summary>
    /// Recommended adjustments
    /// </summary>
    public string? Recommendation { get; init; }
}
