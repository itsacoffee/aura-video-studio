using System;
using System.Collections.Generic;

namespace Aura.Core.Models.ContentPlanning;

/// <summary>
/// Represents insights about target audience
/// </summary>
public class AudienceInsight
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Platform { get; set; } = string.Empty;
    public Demographics Demographics { get; set; } = new();
    public List<string> TopInterests { get; set; } = new();
    public List<string> PreferredContentTypes { get; set; } = new();
    public double EngagementRate { get; set; }
    public Dictionary<string, double> BestPostingTimes { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Demographic information about audience
/// </summary>
public class Demographics
{
    public Dictionary<string, double> AgeDistribution { get; set; } = new();
    public Dictionary<string, double> GenderDistribution { get; set; } = new();
    public Dictionary<string, double> LocationDistribution { get; set; } = new();
}

/// <summary>
/// Request for audience analysis
/// </summary>
public class AudienceAnalysisRequest
{
    public string? Platform { get; set; }
    public string? Category { get; set; }
    public List<string> ContentTags { get; set; } = new();
}

/// <summary>
/// Response containing audience insights
/// </summary>
public class AudienceAnalysisResponse
{
    public AudienceInsight Insights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}
