using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Persistent storage for background job queue entries
/// Ensures jobs survive application restarts and crashes
/// </summary>
[Table("JobQueue")]
public class JobQueueEntity : IAuditableEntity
{
    [Key]
    public string JobId { get; set; } = string.Empty;
    
    /// <summary>
    /// Priority of the job (lower number = higher priority, 1-10)
    /// </summary>
    public int Priority { get; set; } = 5;
    
    /// <summary>
    /// Current status of the job in queue
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";  // Pending, Processing, Completed, Failed, Cancelled
    
    /// <summary>
    /// Serialized job data (Brief, PlanSpec, VoiceSpec, RenderSpec)
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string JobDataJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Correlation ID for tracing
    /// </summary>
    [MaxLength(100)]
    public string? CorrelationId { get; set; }
    
    /// <summary>
    /// Number of retry attempts
    /// </summary>
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Maximum allowed retries
    /// </summary>
    public int MaxRetries { get; set; } = 3;
    
    /// <summary>
    /// Last error message if failed
    /// </summary>
    [Column(TypeName = "TEXT")]
    public string? LastError { get; set; }
    
    /// <summary>
    /// When the job was enqueued
    /// </summary>
    public DateTime EnqueuedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the job started processing
    /// </summary>
    public DateTime? StartedAt { get; set; }
    
    /// <summary>
    /// When the job completed (success or failure)
    /// </summary>
    public DateTime? CompletedAt { get; set; }
    
    /// <summary>
    /// Next retry time (for exponential backoff)
    /// </summary>
    public DateTime? NextRetryAt { get; set; }
    
    /// <summary>
    /// Current progress percentage (0-100)
    /// </summary>
    public int ProgressPercent { get; set; } = 0;
    
    /// <summary>
    /// Current processing stage
    /// </summary>
    [MaxLength(100)]
    public string? CurrentStage { get; set; }
    
    /// <summary>
    /// Output file path when completed
    /// </summary>
    [MaxLength(500)]
    public string? OutputPath { get; set; }
    
    /// <summary>
    /// Worker ID that's processing this job (for distributed systems)
    /// </summary>
    [MaxLength(100)]
    public string? WorkerId { get; set; }
    
    /// <summary>
    /// Whether this is a Quick Demo job
    /// </summary>
    public bool IsQuickDemo { get; set; } = false;
    
    // IAuditableEntity implementation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
