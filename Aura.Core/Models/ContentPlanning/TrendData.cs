using System;
using System.Collections.Generic;

namespace Aura.Core.Models.ContentPlanning;

/// <summary>
/// Represents trend data for a specific topic or category
/// </summary>
public class TrendData
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Topic { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public double TrendScore { get; set; }
    public TrendDirection Direction { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public List<TrendDataPoint> DataPoints { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Direction of a trend
/// </summary>
public enum TrendDirection
{
    Rising,
    Stable,
    Declining
}

/// <summary>
/// Data point in a trend analysis
/// </summary>
public class TrendDataPoint
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}

/// <summary>
/// Request for trend analysis
/// </summary>
public class TrendAnalysisRequest
{
    public string? Category { get; set; }
    public string? Platform { get; set; }
    public List<string> Keywords { get; set; } = new();
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Response containing trend analysis results
/// </summary>
public class TrendAnalysisResponse
{
    public List<TrendData> Trends { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string Summary { get; set; } = string.Empty;
}
