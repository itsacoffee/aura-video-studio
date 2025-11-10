using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Tracks performance metrics for rendering and generation
/// Helps users optimize their workflow and identify bottlenecks
/// </summary>
[Table("PerformanceMetrics")]
public class PerformanceMetricsEntity : IAuditableEntity
{
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// Project ID
    /// </summary>
    [MaxLength(100)]
    public string? ProjectId { get; set; }
    
    /// <summary>
    /// Job ID
    /// </summary>
    [MaxLength(100)]
    public string? JobId { get; set; }
    
    /// <summary>
    /// Operation type (script-generation, image-generation, audio-generation, rendering, etc.)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string OperationType { get; set; } = string.Empty;
    
    /// <summary>
    /// Specific stage within operation
    /// </summary>
    [MaxLength(100)]
    public string? Stage { get; set; }
    
    /// <summary>
    /// Total duration in milliseconds
    /// </summary>
    public long DurationMs { get; set; }
    
    /// <summary>
    /// CPU usage percentage during operation (0-100)
    /// </summary>
    public double? CpuUsagePercent { get; set; }
    
    /// <summary>
    /// Memory used in megabytes
    /// </summary>
    public double? MemoryUsedMB { get; set; }
    
    /// <summary>
    /// Peak memory used in megabytes
    /// </summary>
    public double? PeakMemoryMB { get; set; }
    
    /// <summary>
    /// GPU usage if applicable (0-100)
    /// </summary>
    public double? GpuUsagePercent { get; set; }
    
    /// <summary>
    /// Disk I/O operations count
    /// </summary>
    public long? DiskIOOperations { get; set; }
    
    /// <summary>
    /// Network bytes transferred (for API calls)
    /// </summary>
    public long? NetworkBytesTransferred { get; set; }
    
    /// <summary>
    /// Output file size in bytes
    /// </summary>
    public long? OutputFileSizeBytes { get; set; }
    
    /// <summary>
    /// Whether operation completed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if failed
    /// </summary>
    [MaxLength(500)]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Number of parallel workers used
    /// </summary>
    public int? WorkerCount { get; set; }
    
    /// <summary>
    /// Queue wait time before processing started (ms)
    /// </summary>
    public long? QueueWaitMs { get; set; }
    
    /// <summary>
    /// Throughput metric (e.g., frames per second)
    /// </summary>
    public double? Throughput { get; set; }
    
    /// <summary>
    /// Throughput unit (fps, tokens/sec, etc.)
    /// </summary>
    [MaxLength(20)]
    public string? ThroughputUnit { get; set; }
    
    /// <summary>
    /// Timestamp when operation occurred
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// System information snapshot (OS, CPU model, etc.) as JSON
    /// </summary>
    [Column(TypeName = "text")]
    public string? SystemInfo { get; set; }
    
    // IAuditableEntity implementation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
