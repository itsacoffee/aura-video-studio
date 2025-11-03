using System;

namespace Aura.Core.Data;

/// <summary>
/// Database entity for custom video templates
/// </summary>
public class CustomTemplateEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty; // Comma-separated
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Author { get; set; } = "User";
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Soft-delete support: indicates if this template has been deleted
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Soft-delete support: when the template was deleted (null if not deleted)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Soft-delete support: user who deleted the template
    /// </summary>
    public string? DeletedByUserId { get; set; }
    
    // JSON serialized configurations
    public string ScriptStructureJson { get; set; } = string.Empty;
    public string VideoStructureJson { get; set; } = string.Empty;
    public string LLMPipelineJson { get; set; } = string.Empty;
    public string VisualPreferencesJson { get; set; } = string.Empty;
}
