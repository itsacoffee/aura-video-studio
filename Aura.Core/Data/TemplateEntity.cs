using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Database entity for persisting project templates
/// </summary>
[Table("templates")]
public class TemplateEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = string.Empty;

    public TemplateEntity()
    {
        Id = Guid.NewGuid().ToString();
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    [Required]
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("description")]
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column("category")]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    [Column("sub_category")]
    [MaxLength(100)]
    public string SubCategory { get; set; } = string.Empty;

    [Column("preview_image")]
    [MaxLength(500)]
    public string PreviewImage { get; set; } = string.Empty;

    [Column("preview_video")]
    [MaxLength(500)]
    public string PreviewVideo { get; set; } = string.Empty;

    [Column("tags")]
    [MaxLength(500)]
    public string Tags { get; set; } = string.Empty; // Comma-separated

    [Required]
    [Column("template_data")]
    public string TemplateData { get; set; } = string.Empty; // JSON

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Required]
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Required]
    [Column("author")]
    [MaxLength(200)]
    public string Author { get; set; } = "System";

    [Required]
    [Column("is_system_template")]
    public bool IsSystemTemplate { get; set; } = true;

    [Required]
    [Column("is_community_template")]
    public bool IsCommunityTemplate { get; set; } = false;

    [Column("usage_count")]
    public int UsageCount { get; set; } = 0;

    [Column("rating")]
    public double Rating { get; set; } = 0.0;

    [Column("rating_count")]
    public int RatingCount { get; set; } = 0;
}
