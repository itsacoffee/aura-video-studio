using System;
using System.Collections.Generic;

namespace Aura.Core.Models.ContentPlanning;

/// <summary>
/// Represents an AI-generated topic suggestion
/// </summary>
public class TopicSuggestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Topic { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public double RelevanceScore { get; set; }
    public double TrendScore { get; set; }
    public double PredictedEngagement { get; set; }
    public List<string> Keywords { get; set; } = new();
    public List<string> RecommendedPlatforms { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Request for generating topic suggestions
/// </summary>
public class TopicSuggestionRequest
{
    public string? Category { get; set; }
    public string? TargetAudience { get; set; }
    public List<string> Interests { get; set; } = new();
    public List<string> PreferredPlatforms { get; set; } = new();
    public int Count { get; set; } = 10;
}

/// <summary>
/// Response containing generated topic suggestions
/// </summary>
public class TopicSuggestionResponse
{
    public List<TopicSuggestion> Suggestions { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public int TotalCount { get; set; }
}
