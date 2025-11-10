using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Database entity for custom video templates
/// </summary>
[Table("custom_templates")]
public class CustomTemplateEntity : IAuditableEntity, ISoftDeletable
{
    [Key]
    [Column("id")]
    [MaxLength(50)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("category")]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Column("tags")]
    [MaxLength(500)]
    public string Tags { get; set; } = string.Empty; // Comma-separated

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    [MaxLength(200)]
    public string? CreatedBy { get; set; }

    [Column("modified_by")]
    [MaxLength(200)]
    public string? ModifiedBy { get; set; }

    [Required]
    [Column("author")]
    [MaxLength(200)]
    public string Author { get; set; } = "User";

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Soft-delete support: indicates if this template has been deleted
    /// </summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Soft-delete support: when the template was deleted (null if not deleted)
    /// </summary>
    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Soft-delete support: user who deleted the template
    /// </summary>
    [Column("deleted_by")]
    [MaxLength(200)]
    public string? DeletedBy { get; set; }
    
    // JSON serialized configurations
    [Column("script_structure_json", TypeName = "TEXT")]
    public string ScriptStructureJson { get; set; } = string.Empty;

    [Column("video_structure_json", TypeName = "TEXT")]
    public string VideoStructureJson { get; set; } = string.Empty;

    [Column("llm_pipeline_json", TypeName = "TEXT")]
    public string LLMPipelineJson { get; set; } = string.Empty;

    [Column("visual_preferences_json", TypeName = "TEXT")]
    public string VisualPreferencesJson { get; set; } = string.Empty;
}
