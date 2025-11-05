using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Diagnostics;

/// <summary>
/// AI-powered analysis of job failure with root causes and remediation
/// </summary>
public record FailureAnalysis
{
    /// <summary>
    /// Job ID that failed
    /// </summary>
    public required string JobId { get; init; }
    
    /// <summary>
    /// When the analysis was performed
    /// </summary>
    public DateTime AnalyzedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Primary root cause identified
    /// </summary>
    public required RootCause PrimaryRootCause { get; init; }
    
    /// <summary>
    /// Additional possible root causes
    /// </summary>
    public List<RootCause> SecondaryRootCauses { get; init; } = new();
    
    /// <summary>
    /// Recommended next steps ordered by priority
    /// </summary>
    public required List<RecommendedAction> RecommendedActions { get; init; }
    
    /// <summary>
    /// Summary of the failure
    /// </summary>
    public string Summary { get; init; } = string.Empty;
    
    /// <summary>
    /// Links to relevant documentation
    /// </summary>
    public List<DocumentationLink> DocumentationLinks { get; init; } = new();
    
    /// <summary>
    /// Confidence score (0-100) of the analysis
    /// </summary>
    public int ConfidenceScore { get; init; }
}

/// <summary>
/// Identified root cause of failure
/// </summary>
public record RootCause
{
    /// <summary>
    /// Type of root cause
    /// </summary>
    public required RootCauseType Type { get; init; }
    
    /// <summary>
    /// Human-readable description
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// Evidence supporting this root cause
    /// </summary>
    public List<string> Evidence { get; init; } = new();
    
    /// <summary>
    /// Confidence level (0-100)
    /// </summary>
    public int Confidence { get; init; }
    
    /// <summary>
    /// Stage where this issue manifested
    /// </summary>
    public string? Stage { get; init; }
    
    /// <summary>
    /// Provider involved if applicable
    /// </summary>
    public string? Provider { get; init; }
}

/// <summary>
/// Types of root causes
/// </summary>
public enum RootCauseType
{
    /// <summary>
    /// API rate limit exceeded
    /// </summary>
    RateLimit,
    
    /// <summary>
    /// Invalid or missing API key
    /// </summary>
    InvalidApiKey,
    
    /// <summary>
    /// Missing API key
    /// </summary>
    MissingApiKey,
    
    /// <summary>
    /// Network connectivity issue
    /// </summary>
    NetworkError,
    
    /// <summary>
    /// Missing codec or encoder
    /// </summary>
    MissingCodec,
    
    /// <summary>
    /// FFmpeg not found or invalid
    /// </summary>
    FFmpegNotFound,
    
    /// <summary>
    /// Insufficient system resources
    /// </summary>
    InsufficientResources,
    
    /// <summary>
    /// Invalid input or configuration
    /// </summary>
    InvalidInput,
    
    /// <summary>
    /// Provider service unavailable
    /// </summary>
    ProviderUnavailable,
    
    /// <summary>
    /// Timeout during operation
    /// </summary>
    Timeout,
    
    /// <summary>
    /// Budget or quota exceeded
    /// </summary>
    BudgetExceeded,
    
    /// <summary>
    /// File system error (permissions, disk space)
    /// </summary>
    FileSystemError,
    
    /// <summary>
    /// Unknown or unclassified error
    /// </summary>
    Unknown
}

/// <summary>
/// Recommended action to resolve the issue
/// </summary>
public record RecommendedAction
{
    /// <summary>
    /// Priority of this action (1 = highest)
    /// </summary>
    public int Priority { get; init; }
    
    /// <summary>
    /// Title of the action
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// Detailed description
    /// </summary>
    public required string Description { get; init; }
    
    /// <summary>
    /// Steps to perform this action
    /// </summary>
    public List<string> Steps { get; init; } = new();
    
    /// <summary>
    /// Whether this action can be automated
    /// </summary>
    public bool CanAutomate { get; init; }
    
    /// <summary>
    /// Estimated time to complete (in minutes)
    /// </summary>
    public int? EstimatedMinutes { get; init; }
    
    /// <summary>
    /// Type of action
    /// </summary>
    public ActionType Type { get; init; }
}

/// <summary>
/// Types of recommended actions
/// </summary>
public enum ActionType
{
    /// <summary>
    /// Update configuration or settings
    /// </summary>
    Configuration,
    
    /// <summary>
    /// Add or update API key
    /// </summary>
    ApiKey,
    
    /// <summary>
    /// Install missing dependency
    /// </summary>
    Installation,
    
    /// <summary>
    /// Retry the operation
    /// </summary>
    Retry,
    
    /// <summary>
    /// Wait and retry later
    /// </summary>
    WaitAndRetry,
    
    /// <summary>
    /// Switch to alternative provider
    /// </summary>
    ProviderSwitch,
    
    /// <summary>
    /// Check network or firewall
    /// </summary>
    Network,
    
    /// <summary>
    /// Free up system resources
    /// </summary>
    Resources,
    
    /// <summary>
    /// Contact support
    /// </summary>
    Support
}

/// <summary>
/// Link to relevant documentation
/// </summary>
public record DocumentationLink
{
    /// <summary>
    /// Title of the documentation
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// URL to the documentation
    /// </summary>
    public required string Url { get; init; }
    
    /// <summary>
    /// Brief description
    /// </summary>
    public string Description { get; init; } = string.Empty;
}
