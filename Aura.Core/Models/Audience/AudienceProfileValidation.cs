using System.Collections.Generic;

namespace Aura.Core.Models.Audience;

/// <summary>
/// Validation result for audience profile
/// </summary>
public class ValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<ValidationIssue> Errors { get; set; } = new();
    public List<ValidationIssue> Warnings { get; set; } = new();
    public List<ValidationIssue> Infos { get; set; } = new();
}

/// <summary>
/// Individual validation issue
/// </summary>
public class ValidationIssue
{
    public ValidationSeverity Severity { get; set; }
    public string Field { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? SuggestedFix { get; set; }
}

/// <summary>
/// Validation severity levels
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// Blocks profile from being used - must be fixed
    /// </summary>
    Error,
    
    /// <summary>
    /// Suggests review - profile can still be used but may have issues
    /// </summary>
    Warning,
    
    /// <summary>
    /// Optional improvement suggestions
    /// </summary>
    Info
}
