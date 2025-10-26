using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Database entity for persisting export job history
/// </summary>
[Table("export_history")]
public class ExportHistoryEntity
{
    [Key]
    [Column("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [Column("input_file")]
    [MaxLength(500)]
    public string InputFile { get; set; } = string.Empty;

    [Required]
    [Column("output_file")]
    [MaxLength(500)]
    public string OutputFile { get; set; } = string.Empty;

    [Required]
    [Column("preset_name")]
    [MaxLength(100)]
    public string PresetName { get; set; } = string.Empty;

    [Required]
    [Column("status")]
    [MaxLength(50)]
    public string Status { get; set; } = "Queued";

    [Column("progress")]
    public double Progress { get; set; } = 0;

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("error_message")]
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    [Column("file_size")]
    public long? FileSize { get; set; }

    [Column("duration_seconds")]
    public double? DurationSeconds { get; set; }

    [Column("platform")]
    [MaxLength(50)]
    public string? Platform { get; set; }

    [Column("resolution")]
    [MaxLength(20)]
    public string? Resolution { get; set; }

    [Column("codec")]
    [MaxLength(50)]
    public string? Codec { get; set; }
}
