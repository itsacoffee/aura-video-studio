using System;
using System.Collections.Generic;

namespace Aura.Core.Models.ContentPlanning;

/// <summary>
/// Represents scheduled content in the calendar
/// </summary>
public class ScheduledContent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ContentPlanId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public DateTime ScheduledDateTime { get; set; }
    public TimeSpan OptimalTimeWindow { get; set; }
    public double PredictedReach { get; set; }
    public SchedulingStatus Status { get; set; } = SchedulingStatus.Pending;
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Status of scheduled content
/// </summary>
public enum SchedulingStatus
{
    Pending,
    Ready,
    Published,
    Failed,
    Cancelled
}

/// <summary>
/// Request for content scheduling recommendations
/// </summary>
public class ContentSchedulingRequest
{
    public string Platform { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? PreferredDate { get; set; }
    public List<string> TargetAudience { get; set; } = new();
}

/// <summary>
/// Response with scheduling recommendations
/// </summary>
public class ContentSchedulingResponse
{
    public List<SchedulingRecommendation> Recommendations { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Recommendation for when to schedule content
/// </summary>
public class SchedulingRecommendation
{
    public DateTime RecommendedDateTime { get; set; }
    public double ConfidenceScore { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public double PredictedEngagement { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}
