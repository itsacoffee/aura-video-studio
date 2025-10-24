using System;
using System.Collections.Generic;

namespace Aura.Core.Models.PacingModels;

/// <summary>
/// Attention curve prediction data showing viewer engagement over time
/// </summary>
public record AttentionCurveData
{
    /// <summary>
    /// Individual attention data points over the video timeline
    /// </summary>
    public IReadOnlyList<AttentionDataPoint> DataPoints { get; init; } = Array.Empty<AttentionDataPoint>();

    /// <summary>
    /// Average attention/engagement level (0-100)
    /// </summary>
    public double AverageEngagement { get; init; }

    /// <summary>
    /// Timestamps where attention is predicted to peak
    /// </summary>
    public IReadOnlyList<TimeSpan> EngagementPeaks { get; init; } = Array.Empty<TimeSpan>();

    /// <summary>
    /// Timestamps where attention is predicted to drop
    /// </summary>
    public IReadOnlyList<TimeSpan> EngagementValleys { get; init; } = Array.Empty<TimeSpan>();

    /// <summary>
    /// Overall predicted retention score (0-100)
    /// </summary>
    public double OverallRetentionScore { get; init; }
}

/// <summary>
/// Single point on the attention curve
/// </summary>
public record AttentionDataPoint
{
    /// <summary>
    /// Timestamp in the video
    /// </summary>
    public TimeSpan Timestamp { get; init; }

    /// <summary>
    /// Predicted attention level at this point (0-100)
    /// </summary>
    public double AttentionLevel { get; init; }

    /// <summary>
    /// Predicted retention rate at this point (0-100)
    /// </summary>
    public double RetentionRate { get; init; }

    /// <summary>
    /// Engagement score at this point (0-100)
    /// </summary>
    public double EngagementScore { get; init; }
}
