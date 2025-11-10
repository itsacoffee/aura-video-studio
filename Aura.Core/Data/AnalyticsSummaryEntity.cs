using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Aggregated analytics summaries for efficient reporting
/// Pre-computed daily/monthly summaries to reduce query overhead
/// </summary>
[Table("AnalyticsSummaries")]
public class AnalyticsSummaryEntity : IAuditableEntity
{
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// Period type (daily, weekly, monthly)
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PeriodType { get; set; } = string.Empty;
    
    /// <summary>
    /// Period identifier (e.g., "2024-11-10", "2024-11", "2024-W45")
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string PeriodId { get; set; } = string.Empty;
    
    /// <summary>
    /// Start of period
    /// </summary>
    public DateTime PeriodStart { get; set; }
    
    /// <summary>
    /// End of period
    /// </summary>
    public DateTime PeriodEnd { get; set; }
    
    /// <summary>
    /// Total number of generations
    /// </summary>
    public int TotalGenerations { get; set; }
    
    /// <summary>
    /// Successful generations
    /// </summary>
    public int SuccessfulGenerations { get; set; }
    
    /// <summary>
    /// Failed generations
    /// </summary>
    public int FailedGenerations { get; set; }
    
    /// <summary>
    /// Total tokens consumed (input + output)
    /// </summary>
    public long TotalTokens { get; set; }
    
    /// <summary>
    /// Total input tokens
    /// </summary>
    public long TotalInputTokens { get; set; }
    
    /// <summary>
    /// Total output tokens
    /// </summary>
    public long TotalOutputTokens { get; set; }
    
    /// <summary>
    /// Total estimated cost in USD
    /// </summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal TotalCostUSD { get; set; }
    
    /// <summary>
    /// Average generation duration in milliseconds
    /// </summary>
    public long AverageDurationMs { get; set; }
    
    /// <summary>
    /// Total rendering time in milliseconds
    /// </summary>
    public long TotalRenderingTimeMs { get; set; }
    
    /// <summary>
    /// Most used provider
    /// </summary>
    [MaxLength(50)]
    public string? MostUsedProvider { get; set; }
    
    /// <summary>
    /// Most used model
    /// </summary>
    [MaxLength(100)]
    public string? MostUsedModel { get; set; }
    
    /// <summary>
    /// Most used feature
    /// </summary>
    [MaxLength(50)]
    public string? MostUsedFeature { get; set; }
    
    /// <summary>
    /// Total video duration generated in seconds
    /// </summary>
    public double TotalVideoDurationSeconds { get; set; }
    
    /// <summary>
    /// Total number of scenes created
    /// </summary>
    public int TotalScenes { get; set; }
    
    /// <summary>
    /// Average CPU usage
    /// </summary>
    public double? AverageCpuUsage { get; set; }
    
    /// <summary>
    /// Average memory usage in MB
    /// </summary>
    public double? AverageMemoryUsageMB { get; set; }
    
    /// <summary>
    /// Provider breakdown as JSON (provider -> count/cost)
    /// </summary>
    [Column(TypeName = "text")]
    public string? ProviderBreakdown { get; set; }
    
    /// <summary>
    /// Feature usage breakdown as JSON (feature -> count)
    /// </summary>
    [Column(TypeName = "text")]
    public string? FeatureBreakdown { get; set; }
    
    // IAuditableEntity implementation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}
