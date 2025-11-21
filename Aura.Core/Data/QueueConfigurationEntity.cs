using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Configuration settings for the background job queue
/// </summary>
[Table("QueueConfiguration")]
public class QueueConfigurationEntity
{
    [Key]
    public string Id { get; set; } = "default";  // Singleton config
    
    /// <summary>
    /// Maximum number of concurrent jobs
    /// </summary>
    public int MaxConcurrentJobs { get; set; } = 2;
    
    /// <summary>
    /// Whether to pause queue on battery power (laptop power mode detection)
    /// </summary>
    public bool PauseOnBattery { get; set; } = true;
    
    /// <summary>
    /// Maximum CPU usage threshold (0-100) before throttling
    /// </summary>
    public int CpuThrottleThreshold { get; set; } = 85;
    
    /// <summary>
    /// Maximum memory usage threshold (0-100) before throttling
    /// </summary>
    public int MemoryThrottleThreshold { get; set; } = 85;
    
    /// <summary>
    /// Whether queue processing is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Polling interval for checking new jobs (in seconds)
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 5;
    
    /// <summary>
    /// How long to keep completed jobs in history (in days)
    /// </summary>
    public int JobHistoryRetentionDays { get; set; } = 7;
    
    /// <summary>
    /// How long to keep failed jobs in history (in days)
    /// </summary>
    public int FailedJobRetentionDays { get; set; } = 30;
    
    /// <summary>
    /// Base delay for exponential backoff (in seconds)
    /// </summary>
    public int RetryBaseDelaySeconds { get; set; } = 5;
    
    /// <summary>
    /// Maximum delay for exponential backoff (in seconds)
    /// </summary>
    public int RetryMaxDelaySeconds { get; set; } = 300;
    
    /// <summary>
    /// Whether to send desktop notifications on job completion
    /// </summary>
    public bool EnableNotifications { get; set; } = true;
    
    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
