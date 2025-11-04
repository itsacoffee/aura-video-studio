using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Quantifiable quality metrics for script assessment across refinement iterations
/// </summary>
public class ScriptQualityMetrics
{
    /// <summary>
    /// Narrative coherence score (0-100): Measures logical flow and story structure
    /// </summary>
    public double NarrativeCoherence { get; set; }

    /// <summary>
    /// Pacing appropriateness score (0-100): Measures rhythm and timing suitability
    /// </summary>
    public double PacingAppropriateness { get; set; }

    /// <summary>
    /// Audience alignment score (0-100): Measures how well content matches target audience
    /// </summary>
    public double AudienceAlignment { get; set; }

    /// <summary>
    /// Visual clarity score (0-100): Measures how well script supports visual storytelling
    /// </summary>
    public double VisualClarity { get; set; }

    /// <summary>
    /// Engagement potential score (0-100): Measures predicted viewer engagement
    /// </summary>
    public double EngagementPotential { get; set; }

    /// <summary>
    /// Overall quality score (0-100): Weighted average of all metrics
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// When this assessment was performed
    /// </summary>
    public DateTime AssessedAt { get; set; }

    /// <summary>
    /// Iteration number in refinement pipeline (0 = initial draft)
    /// </summary>
    public int Iteration { get; set; }

    /// <summary>
    /// Specific issues identified in this iteration
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// Actionable suggestions for improvement
    /// </summary>
    public List<string> Suggestions { get; set; } = new();

    /// <summary>
    /// Strengths identified in the content
    /// </summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>
    /// Calculate overall score from component metrics using weighted average
    /// </summary>
    public void CalculateOverallScore()
    {
        OverallScore = (
            NarrativeCoherence * 0.25 +
            PacingAppropriateness * 0.20 +
            AudienceAlignment * 0.20 +
            VisualClarity * 0.15 +
            EngagementPotential * 0.20
        );
    }

    /// <summary>
    /// Calculate improvement delta compared to another metrics instance
    /// </summary>
    public ScriptQualityImprovement CalculateImprovement(ScriptQualityMetrics? baseline)
    {
        if (baseline == null)
        {
            return new ScriptQualityImprovement
            {
                OverallDelta = OverallScore,
                NarrativeCoherenceDelta = NarrativeCoherence,
                PacingDelta = PacingAppropriateness,
                AudienceDelta = AudienceAlignment,
                VisualClarityDelta = VisualClarity,
                EngagementDelta = EngagementPotential,
                IterationImproved = Iteration
            };
        }

        return new ScriptQualityImprovement
        {
            OverallDelta = OverallScore - baseline.OverallScore,
            NarrativeCoherenceDelta = NarrativeCoherence - baseline.NarrativeCoherence,
            PacingDelta = PacingAppropriateness - baseline.PacingAppropriateness,
            AudienceDelta = AudienceAlignment - baseline.AudienceAlignment,
            VisualClarityDelta = VisualClarity - baseline.VisualClarity,
            EngagementDelta = EngagementPotential - baseline.EngagementPotential,
            IterationImproved = Iteration
        };
    }

    /// <summary>
    /// Check if quality meets or exceeds threshold
    /// </summary>
    public bool MeetsThreshold(double threshold)
    {
        return OverallScore >= threshold;
    }
}

/// <summary>
/// Tracks improvement between iterations
/// </summary>
public class ScriptQualityImprovement
{
    public double OverallDelta { get; set; }
    public double NarrativeCoherenceDelta { get; set; }
    public double PacingDelta { get; set; }
    public double AudienceDelta { get; set; }
    public double VisualClarityDelta { get; set; }
    public double EngagementDelta { get; set; }
    public int IterationImproved { get; set; }

    /// <summary>
    /// Check if any meaningful improvement occurred (threshold: 5 points)
    /// </summary>
    public bool HasMeaningfulImprovement()
    {
        return OverallDelta >= 5.0;
    }
}

/// <summary>
/// Configuration for script refinement process with generator-critic-editor pattern
/// </summary>
public class ScriptRefinementConfig
{
    /// <summary>
    /// Number of refinement passes to perform (1-3)
    /// </summary>
    public int MaxRefinementPasses { get; set; } = 2;

    /// <summary>
    /// Quality threshold for early stopping (0-100)
    /// </summary>
    public double QualityThreshold { get; set; } = 85.0;

    /// <summary>
    /// Minimum improvement required to continue refinement (delta score)
    /// </summary>
    public double MinimumImprovement { get; set; } = 5.0;

    /// <summary>
    /// Enable integration with IntelligentContentAdvisor for validation
    /// </summary>
    public bool EnableAdvisorValidation { get; set; } = true;

    /// <summary>
    /// Timeout per refinement pass
    /// </summary>
    public TimeSpan PassTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Model to use for critic role (can be cheaper/faster than generator)
    /// Null uses same model as generator
    /// </summary>
    public string? CriticModel { get; set; }

    /// <summary>
    /// Model to use for editor role (can be cheaper/faster than generator)
    /// Null uses same model as generator
    /// </summary>
    public string? EditorModel { get; set; }

    /// <summary>
    /// Maximum cost budget for refinement in dollars (early stop if exceeded)
    /// Null means no cost limit
    /// </summary>
    public double? MaxCostBudget { get; set; }

    /// <summary>
    /// Enable schema validation after each edit
    /// </summary>
    public bool EnableSchemaValidation { get; set; } = true;

    /// <summary>
    /// Enable telemetry collection for convergence analysis
    /// </summary>
    public bool EnableTelemetry { get; set; } = true;

    /// <summary>
    /// Validate configuration parameters
    /// </summary>
    public void Validate()
    {
        if (MaxRefinementPasses < 1 || MaxRefinementPasses > 3)
        {
            throw new ArgumentException("MaxRefinementPasses must be between 1 and 3");
        }

        if (QualityThreshold < 0 || QualityThreshold > 100)
        {
            throw new ArgumentException("QualityThreshold must be between 0 and 100");
        }

        if (MinimumImprovement < 0)
        {
            throw new ArgumentException("MinimumImprovement must be non-negative");
        }

        if (MaxCostBudget.HasValue && MaxCostBudget.Value < 0)
        {
            throw new ArgumentException("MaxCostBudget must be non-negative");
        }
    }
}

/// <summary>
/// Result of script refinement process
/// </summary>
public class ScriptRefinementResult
{
    /// <summary>
    /// Final refined script
    /// </summary>
    public string FinalScript { get; set; } = string.Empty;

    /// <summary>
    /// All quality metrics from each iteration
    /// </summary>
    public List<ScriptQualityMetrics> IterationMetrics { get; set; } = new();

    /// <summary>
    /// Total number of passes performed
    /// </summary>
    public int TotalPasses { get; set; }

    /// <summary>
    /// Reason refinement stopped (threshold met, max passes, error, etc.)
    /// </summary>
    public string StopReason { get; set; } = string.Empty;

    /// <summary>
    /// Total time spent on refinement
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// Whether refinement was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Error message if refinement failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Telemetry data collected during refinement
    /// </summary>
    public RefinementTelemetry? Telemetry { get; set; }

    /// <summary>
    /// Summarized critique embedded in metadata
    /// </summary>
    public string? CritiqueSummary { get; set; }

    /// <summary>
    /// Total cost incurred during refinement
    /// </summary>
    public double TotalCost { get; set; }

    /// <summary>
    /// Get final quality metrics
    /// </summary>
    public ScriptQualityMetrics? FinalMetrics =>
        IterationMetrics.Count > 0 ? IterationMetrics[^1] : null;

    /// <summary>
    /// Get initial quality metrics
    /// </summary>
    public ScriptQualityMetrics? InitialMetrics =>
        IterationMetrics.Count > 0 ? IterationMetrics[0] : null;

    /// <summary>
    /// Calculate total improvement from first to last iteration
    /// </summary>
    public ScriptQualityImprovement? GetTotalImprovement()
    {
        if (InitialMetrics == null || FinalMetrics == null)
        {
            return null;
        }

        return FinalMetrics.CalculateImprovement(InitialMetrics);
    }
}

/// <summary>
/// Telemetry data for refinement convergence analysis
/// </summary>
public class RefinementTelemetry
{
    /// <summary>
    /// Scores recorded per refinement round
    /// </summary>
    public List<RoundTelemetry> RoundData { get; set; } = new();

    /// <summary>
    /// Overall convergence statistics
    /// </summary>
    public ConvergenceStatistics? Convergence { get; set; }

    /// <summary>
    /// Cost breakdown per phase
    /// </summary>
    public Dictionary<string, double> CostByPhase { get; set; } = new();

    /// <summary>
    /// Model usage tracking
    /// </summary>
    public ModelUsageStats ModelUsage { get; set; } = new();
}

/// <summary>
/// Telemetry for a single refinement round
/// </summary>
public class RoundTelemetry
{
    /// <summary>
    /// Round number (0 = initial draft)
    /// </summary>
    public int RoundNumber { get; set; }

    /// <summary>
    /// Quality scores before this round
    /// </summary>
    public ScriptQualityMetrics? BeforeMetrics { get; set; }

    /// <summary>
    /// Quality scores after this round
    /// </summary>
    public ScriptQualityMetrics? AfterMetrics { get; set; }

    /// <summary>
    /// Time taken for this round
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Cost incurred for this round
    /// </summary>
    public double Cost { get; set; }

    /// <summary>
    /// Model used for generation in this round
    /// </summary>
    public string? GeneratorModel { get; set; }

    /// <summary>
    /// Model used for critique in this round
    /// </summary>
    public string? CriticModel { get; set; }

    /// <summary>
    /// Model used for editing in this round
    /// </summary>
    public string? EditorModel { get; set; }

    /// <summary>
    /// Whether schema validation passed
    /// </summary>
    public bool SchemaValid { get; set; }

    /// <summary>
    /// Whether duration constraints were met
    /// </summary>
    public bool WithinDurationConstraints { get; set; }
}

/// <summary>
/// Convergence statistics for refinement process
/// </summary>
public class ConvergenceStatistics
{
    /// <summary>
    /// Average improvement per round
    /// </summary>
    public double AverageImprovementPerRound { get; set; }

    /// <summary>
    /// Standard deviation of improvements
    /// </summary>
    public double ImprovementStdDev { get; set; }

    /// <summary>
    /// Whether refinement converged (improvements plateaued)
    /// </summary>
    public bool Converged { get; set; }

    /// <summary>
    /// Round at which convergence was detected
    /// </summary>
    public int? ConvergenceRound { get; set; }

    /// <summary>
    /// Rate of convergence (higher = faster convergence)
    /// </summary>
    public double ConvergenceRate { get; set; }

    /// <summary>
    /// Total improvement from start to finish
    /// </summary>
    public double TotalImprovement { get; set; }
}

/// <summary>
/// Model usage statistics
/// </summary>
public class ModelUsageStats
{
    /// <summary>
    /// Total tokens used by generator model
    /// </summary>
    public int GeneratorTokens { get; set; }

    /// <summary>
    /// Total tokens used by critic model
    /// </summary>
    public int CriticTokens { get; set; }

    /// <summary>
    /// Total tokens used by editor model
    /// </summary>
    public int EditorTokens { get; set; }

    /// <summary>
    /// Total API calls made
    /// </summary>
    public int TotalApiCalls { get; set; }

    /// <summary>
    /// Number of retries needed
    /// </summary>
    public int RetryCount { get; set; }
}

/// <summary>
/// Structured rubrics for script evaluation
/// </summary>
public class RefinementRubric
{
    /// <summary>
    /// Rubric name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of what this rubric measures
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Scoring criteria (0-100)
    /// </summary>
    public List<RubricCriterion> Criteria { get; set; } = new();

    /// <summary>
    /// Weight in overall score (0-1)
    /// </summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>
    /// Target threshold for this rubric
    /// </summary>
    public double TargetThreshold { get; set; } = 85.0;
}

/// <summary>
/// Individual criterion within a rubric
/// </summary>
public class RubricCriterion
{
    /// <summary>
    /// Criterion name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description and evaluation guidelines
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Examples of excellent performance
    /// </summary>
    public List<string> ExcellentExamples { get; set; } = new();

    /// <summary>
    /// Examples of poor performance
    /// </summary>
    public List<string> PoorExamples { get; set; } = new();

    /// <summary>
    /// Scoring scale for this criterion
    /// </summary>
    public string ScoringGuideline { get; set; } = string.Empty;
}
