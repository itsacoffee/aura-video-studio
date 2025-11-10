using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Aura.Core.Data;

/// <summary>
/// Tracks estimated API costs for local budget monitoring
/// All data stays local - helps users understand their spending
/// </summary>
[Table("CostTracking")]
public class CostTrackingEntity : IAuditableEntity
{
    [Key]
    public long Id { get; set; }
    
    /// <summary>
    /// Reference to usage statistics entry
    /// </summary>
    public long? UsageStatisticsId { get; set; }
    
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
    /// Provider name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;
    
    /// <summary>
    /// Model name
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// Input tokens used
    /// </summary>
    public long InputTokens { get; set; }
    
    /// <summary>
    /// Output tokens generated
    /// </summary>
    public long OutputTokens { get; set; }
    
    /// <summary>
    /// Cost per 1M input tokens (USD)
    /// </summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal InputPricePer1M { get; set; }
    
    /// <summary>
    /// Cost per 1M output tokens (USD)
    /// </summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal OutputPricePer1M { get; set; }
    
    /// <summary>
    /// Calculated input cost (USD)
    /// </summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal InputCost { get; set; }
    
    /// <summary>
    /// Calculated output cost (USD)
    /// </summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal OutputCost { get; set; }
    
    /// <summary>
    /// Total cost (USD)
    /// </summary>
    [Column(TypeName = "decimal(18,6)")]
    public decimal TotalCost { get; set; }
    
    /// <summary>
    /// Currency code (default USD)
    /// </summary>
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    /// <summary>
    /// Year-month for aggregation (e.g., "2024-11")
    /// </summary>
    [Required]
    [MaxLength(7)]
    public string YearMonth { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when cost was recorded
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Whether this was an estimated cost (vs actual from provider)
    /// </summary>
    public bool IsEstimated { get; set; } = true;
    
    // IAuditableEntity implementation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    
    // Navigation property
    [ForeignKey(nameof(UsageStatisticsId))]
    public UsageStatisticsEntity? UsageStatistics { get; set; }
}
