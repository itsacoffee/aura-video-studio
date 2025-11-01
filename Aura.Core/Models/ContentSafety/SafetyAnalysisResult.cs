using System;
using System.Collections.Generic;

namespace Aura.Core.Models.ContentSafety;

/// <summary>
/// Result of content safety analysis
/// </summary>
public class SafetyAnalysisResult
{
    public string ContentId { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public bool IsSafe { get; set; } = true;
    public int OverallSafetyScore { get; set; } = 100;
    
    public List<SafetyViolation> Violations { get; set; } = new();
    public List<SafetyWarning> Warnings { get; set; } = new();
    public Dictionary<SafetyCategoryType, int> CategoryScores { get; set; } = new();
    
    public bool RequiresReview { get; set; }
    public bool AllowWithDisclaimer { get; set; }
    public string? RecommendedDisclaimer { get; set; }
    public List<string> SuggestedFixes { get; set; } = new();
}

/// <summary>
/// A safety policy violation
/// </summary>
public class SafetyViolation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public SafetyCategoryType Category { get; set; }
    public int SeverityScore { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? MatchedContent { get; set; }
    public int? Position { get; set; }
    public SafetyAction RecommendedAction { get; set; }
    public string? SuggestedFix { get; set; }
    public bool CanOverride { get; set; } = true;
}

/// <summary>
/// A safety warning (less severe than violation)
/// </summary>
public class SafetyWarning
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public SafetyCategoryType Category { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Context { get; set; }
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// Audit log entry for safety decisions
/// </summary>
public class SafetyAuditLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string ContentId { get; set; } = string.Empty;
    public string PolicyId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    
    public SafetyAnalysisResult AnalysisResult { get; set; } = new();
    public SafetyDecision Decision { get; set; }
    public string? DecisionReason { get; set; }
    public List<string> OverriddenViolations { get; set; } = new();
    
    public string? ProjectId { get; set; }
    public string ContentType { get; set; } = string.Empty;
}

/// <summary>
/// User decision on flagged content
/// </summary>
public enum SafetyDecision
{
    Approved = 0,
    Rejected = 1,
    Modified = 2,
    Deferred = 3
}
