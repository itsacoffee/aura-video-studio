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
/// Configuration for script refinement process
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
