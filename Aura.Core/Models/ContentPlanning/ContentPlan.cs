using System;
using System.Collections.Generic;

namespace Aura.Core.Models.ContentPlanning;

/// <summary>
/// Represents a content plan for video creation
/// </summary>
public class ContentPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TargetPlatform { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ScheduledDate { get; set; }
    public ContentPlanStatus Status { get; set; } = ContentPlanStatus.Draft;
    public List<string> Tags { get; set; } = new();
    public string? TargetAudience { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Status of a content plan
/// </summary>
public enum ContentPlanStatus
{
    Draft,
    Scheduled,
    InProduction,
    Published,
    Archived
}
