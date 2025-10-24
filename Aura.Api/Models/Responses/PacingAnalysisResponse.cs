using System;
using System.Collections.Generic;
using Aura.Core.Models.PacingModels;

namespace Aura.Api.Models.Responses;

/// <summary>
/// Response from pacing analysis with suggestions and metrics.
/// </summary>
public class PacingAnalysisResponse
{
    /// <summary>
    /// Overall pacing score (0-100)
    /// </summary>
    public double OverallScore { get; set; }

    /// <summary>
    /// Scene timing suggestions
    /// </summary>
    public List<SceneTimingSuggestion> Suggestions { get; set; } = new();

    /// <summary>
    /// Attention curve data points
    /// </summary>
    public AttentionCurveData? AttentionCurve { get; set; }

    /// <summary>
    /// Estimated viewer retention rate (0-100)
    /// </summary>
    public double EstimatedRetention { get; set; }

    /// <summary>
    /// Average engagement score (0-100)
    /// </summary>
    public double AverageEngagement { get; set; }

    /// <summary>
    /// Unique analysis ID for retrieval and reanalysis
    /// </summary>
    public string AnalysisId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the analysis
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for troubleshooting
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score for the analysis (0-100)
    /// </summary>
    public double ConfidenceScore { get; set; }

    /// <summary>
    /// Warnings or recommendations
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Platform preset information.
/// </summary>
public class PlatformPreset
{
    /// <summary>
    /// Platform name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Recommended pacing style
    /// </summary>
    public string RecommendedPacing { get; set; } = string.Empty;

    /// <summary>
    /// Average scene duration recommendation
    /// </summary>
    public string AvgSceneDuration { get; set; } = string.Empty;

    /// <summary>
    /// Optimal video length in seconds
    /// </summary>
    public double OptimalVideoLength { get; set; }

    /// <summary>
    /// Pacing multiplier for calculations
    /// </summary>
    public double PacingMultiplier { get; set; }
}

/// <summary>
/// Response containing platform presets.
/// </summary>
public class PlatformPresetsResponse
{
    /// <summary>
    /// Available platform presets
    /// </summary>
    public List<PlatformPreset> Platforms { get; set; } = new();
}

/// <summary>
/// Response for delete operations.
/// </summary>
public class DeleteAnalysisResponse
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
