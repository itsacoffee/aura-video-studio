using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// User configurable settings for analytics data retention
/// Provides user control over data collection and storage
/// </summary>
[Table("AnalyticsRetentionSettings")]
public class AnalyticsRetentionSettingsEntity : IAuditableEntity
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Whether analytics collection is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Number of days to retain usage statistics (0 = unlimited)
    /// </summary>
    public int UsageStatisticsRetentionDays { get; set; } = 90;
    
    /// <summary>
    /// Number of days to retain cost tracking (0 = unlimited)
    /// </summary>
    public int CostTrackingRetentionDays { get; set; } = 365;
    
    /// <summary>
    /// Number of days to retain performance metrics (0 = unlimited)
    /// </summary>
    public int PerformanceMetricsRetentionDays { get; set; } = 30;
    
    /// <summary>
    /// Whether to automatically cleanup old data
    /// </summary>
    public bool AutoCleanupEnabled { get; set; } = true;
    
    /// <summary>
    /// Hour of day (0-23) to run automatic cleanup
    /// </summary>
    public int CleanupHourUtc { get; set; } = 3;

    /// <summary>
    /// Whether to track successful operations only (exclude failures)
    /// </summary>
    public bool TrackSuccessOnly { get; set; }

    /// <summary>
    /// Whether to collect hardware utilization metrics
    /// </summary>
    public bool CollectHardwareMetrics { get; set; } = true;
    
    /// <summary>
    /// Whether to aggregate old data (vs delete entirely)
    /// </summary>
    public bool AggregateOldData { get; set; } = true;
    
    /// <summary>
    /// Days after which to aggregate detailed data into summaries
    /// </summary>
    public int AggregationThresholdDays { get; set; } = 30;
    
    /// <summary>
    /// Maximum database size for analytics in MB (0 = unlimited)
    /// </summary>
    public int MaxDatabaseSizeMB { get; set; } = 500;
    
    // IAuditableEntity implementation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
