using System;

namespace Aura.Core.Models;

/// <summary>
/// Detailed failure information for a failed job, including diagnostics and suggested actions.
/// </summary>
public record JobFailure
{
    /// <summary>
    /// The stage at which the job failed (e.g., "Script", "Voice", "Render")
    /// </summary>
    public string Stage { get; init; } = string.Empty;
    
    /// <summary>
    /// High-level error message
    /// </summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>
    /// Correlation ID for tracking this error
    /// </summary>
    public string CorrelationId { get; init; } = string.Empty;
    
    /// <summary>
    /// Last 16KB of FFmpeg stderr output (if applicable)
    /// </summary>
    public string? StderrSnippet { get; init; }
    
    /// <summary>
    /// Install log snippet (if failure was during dependency install)
    /// </summary>
    public string? InstallLogSnippet { get; init; }
    
    /// <summary>
    /// Path to full log file
    /// </summary>
    public string? LogPath { get; init; }
    
    /// <summary>
    /// Suggested actions for the user to take
    /// </summary>
    public string[] SuggestedActions { get; init; } = Array.Empty<string>();
    
    /// <summary>
    /// Error code (e.g., "E304-FFMPEG_RUNTIME")
    /// </summary>
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// When the failure occurred
    /// </summary>
    public DateTime FailedAt { get; init; } = DateTime.UtcNow;
}
