using System;
using System.Collections.Generic;

namespace Aura.Core.Validation;

/// <summary>
/// Comprehensive preflight validation report with detailed status for each component.
/// </summary>
public class PreflightReport
{
    /// <summary>
    /// Overall validation result - true only if all required checks pass.
    /// </summary>
    public bool Ok { get; set; } = true;
    
    /// <summary>
    /// Timestamp when the validation was performed.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Duration of the validation in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }
    
    /// <summary>
    /// FFmpeg validation result.
    /// </summary>
    public PreflightCheckResult FFmpeg { get; set; } = new PreflightCheckResult();
    
    /// <summary>
    /// Ollama LLM provider validation result.
    /// </summary>
    public PreflightCheckResult Ollama { get; set; } = new PreflightCheckResult();
    
    /// <summary>
    /// TTS provider validation result.
    /// </summary>
    public PreflightCheckResult TTS { get; set; } = new PreflightCheckResult();
    
    /// <summary>
    /// Disk space validation result.
    /// </summary>
    public PreflightCheckResult DiskSpace { get; set; } = new PreflightCheckResult();
    
    /// <summary>
    /// Image provider validation result (optional - missing is a warning, not error).
    /// </summary>
    public PreflightCheckResult ImageProvider { get; set; } = new PreflightCheckResult();
    
    /// <summary>
    /// List of critical errors that prevent video generation.
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
    
    /// <summary>
    /// List of non-critical warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();
    
    /// <summary>
    /// Add an error for a specific component and mark validation as failed.
    /// </summary>
    /// <param name="component">Component name (e.g., "FFmpeg", "Ollama")</param>
    /// <param name="message">Error message</param>
    public void AddError(string component, string message)
    {
        Errors.Add("[" + component + "] " + message);
        Ok = false;
    }
    
    /// <summary>
    /// Add a warning for a specific component without failing validation.
    /// </summary>
    /// <param name="component">Component name</param>
    /// <param name="message">Warning message</param>
    public void AddWarning(string component, string message)
    {
        Warnings.Add("[" + component + "] " + message);
    }
}

/// <summary>
/// Result of an individual preflight check.
/// </summary>
public class PreflightCheckResult
{
    /// <summary>
    /// Whether the check passed.
    /// </summary>
    public bool Passed { get; set; }
    
    /// <summary>
    /// Whether the check was skipped (e.g., not configured).
    /// </summary>
    public bool Skipped { get; set; }
    
    /// <summary>
    /// Short status string (e.g., "Available", "Not found", "Error").
    /// </summary>
    public string Status { get; set; } = "Not checked";
    
    /// <summary>
    /// Detailed description of the check result.
    /// </summary>
    public string? Details { get; set; }
    
    /// <summary>
    /// Actionable suggestion for how to fix the issue.
    /// </summary>
    public string? SuggestedAction { get; set; }
}
