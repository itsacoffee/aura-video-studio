using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Stores detailed progress history for jobs
/// Useful for debugging, analytics, and recovery
/// </summary>
[Table("JobProgressHistory")]
public class JobProgressHistoryEntity
{
    [Key]
    public long Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string JobId { get; set; } = string.Empty;
    
    /// <summary>
    /// Processing stage at this progress point
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Stage { get; set; } = string.Empty;
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int ProgressPercent { get; set; }
    
    /// <summary>
    /// Progress message
    /// </summary>
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Sub-stage detail for granular tracking
    /// </summary>
    [MaxLength(200)]
    public string? SubstageDetail { get; set; }
    
    /// <summary>
    /// Current item being processed
    /// </summary>
    public int? CurrentItem { get; set; }
    
    /// <summary>
    /// Total items to process
    /// </summary>
    public int? TotalItems { get; set; }
    
    /// <summary>
    /// Timestamp when this progress was recorded
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Elapsed time since job start (in milliseconds)
    /// </summary>
    public long? ElapsedMilliseconds { get; set; }
    
    /// <summary>
    /// Estimated time remaining (in milliseconds)
    /// </summary>
    public long? EstimatedRemainingMilliseconds { get; set; }
    
    // Foreign key relationship
    [ForeignKey(nameof(JobId))]
    public JobQueueEntity? Job { get; set; }
}
