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
    
    // JSON serialized configurations
    public string ScriptStructureJson { get; set; } = string.Empty;
    public string VideoStructureJson { get; set; } = string.Empty;
    public string LLMPipelineJson { get; set; } = string.Empty;
    public string VisualPreferencesJson { get; set; } = string.Empty;
}
