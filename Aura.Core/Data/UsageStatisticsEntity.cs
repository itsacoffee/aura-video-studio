using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Tracks video generation usage statistics for local analytics
/// All data stays on the user's machine - no telemetry sent externally
/// </summary>
[Table("UsageStatistics")]
public class UsageStatisticsEntity : IAuditableEntity
{
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// ID of the project/job this usage relates to
    /// </summary>
    [MaxLength(100)]
    public string? ProjectId { get; set; }
    
    /// <summary>
    /// Job ID if this usage is from a queued job
    /// </summary>
    [MaxLength(100)]
    public string? JobId { get; set; }
    
    /// <summary>
    /// Type of generation (video, audio, caption, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string GenerationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Provider used (openai, anthropic, google, local, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Specific model used
    /// </summary>
    [MaxLength(100)]
    public string? Model { get; set; }
    
    /// <summary>
    /// Number of input tokens consumed
    /// </summary>
    public long InputTokens { get; set; }
    
    /// <summary>
    /// Number of output tokens generated
    /// </summary>
    public long OutputTokens { get; set; }
    
    /// <summary>
    /// Total duration of generation in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
    
    /// <summary>
    /// Whether the generation was successful
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if generation failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Feature that triggered this usage (wizard, advanced-mode, manual, etc.)
    /// </summary>
    [MaxLength(50)]
    public string? FeatureUsed { get; set; }
    
    /// <summary>
    /// Output video duration in seconds (if applicable)
    /// </summary>
    public double? OutputDurationSeconds { get; set; }
    
    /// <summary>
    /// Number of scenes generated
    /// </summary>
    public int? SceneCount { get; set; }
    
    /// <summary>
    /// Whether retry was involved
    /// </summary>
    public bool IsRetry { get; set; }
    
    /// <summary>
    /// Retry attempt number (if retry)
    /// </summary>
    public int? RetryAttempt { get; set; }
    
    /// <summary>
    /// Timestamp when usage occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    // IAuditableEntity implementation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
