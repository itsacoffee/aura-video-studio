using System;
using System.Collections.Generic;

namespace Aura.Core.Models.CostTracking;

/// <summary>
/// User's cost tracking configuration including budgets, alerts, and preferences
/// </summary>
public record CostTrackingConfiguration
{
    /// <summary>
    /// Unique identifier for this configuration
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// User identifier (for multi-user support in future)
    /// </summary>
    public string UserId { get; init; } = "default";
    
    /// <summary>
    /// Overall monthly budget limit across all providers (null = no limit)
    /// </summary>
    public decimal? OverallMonthlyBudget { get; init; }
    
    /// <summary>
    /// Budget period start date (for custom periods)
    /// </summary>
    public DateTime? BudgetPeriodStart { get; init; }
    
    /// <summary>
    /// Budget period end date (for custom periods)
    /// </summary>
    public DateTime? BudgetPeriodEnd { get; init; }
    
    /// <summary>
    /// Budget period type (Monthly, Weekly, Custom)
    /// </summary>
    public BudgetPeriodType PeriodType { get; init; } = BudgetPeriodType.Monthly;
    
    /// <summary>
    /// Currency code (USD, EUR, GBP, etc.)
    /// </summary>
    public string Currency { get; init; } = "USD";
    
    /// <summary>
    /// Alert thresholds as percentages (e.g., 50, 75, 90, 100)
    /// </summary>
    public List<int> AlertThresholds { get; init; } = new() { 50, 75, 90, 100 };
    
    /// <summary>
    /// Whether to send email notifications when alerts trigger
    /// </summary>
    public bool EmailNotificationsEnabled { get; init; }
    
    /// <summary>
    /// Email address for notifications
    /// </summary>
    public string? NotificationEmail { get; init; }
    
    /// <summary>
    /// Alert frequency setting
    /// </summary>
    public AlertFrequency AlertFrequency { get; init; } = AlertFrequency.Once;
    
    /// <summary>
    /// Per-provider budget limits
    /// </summary>
    public Dictionary<string, decimal> ProviderBudgets { get; init; } = new();
    
    /// <summary>
    /// Whether budget limits are hard limits (block operations) or soft warnings
    /// </summary>
    public bool HardBudgetLimit { get; init; }
    
    /// <summary>
    /// When this configuration was created
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this configuration was last updated
    /// </summary>
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Custom alert messages per threshold
    /// </summary>
    public Dictionary<int, string> CustomAlertMessages { get; init; } = new();
    
    /// <summary>
    /// Whether to track costs by project
    /// </summary>
    public bool EnableProjectTracking { get; init; } = true;
    
    /// <summary>
    /// Triggered alert history (for preventing duplicate alerts)
    /// </summary>
    public Dictionary<string, DateTime> TriggeredAlerts { get; init; } = new();
}

/// <summary>
/// Budget period type
/// </summary>
public enum BudgetPeriodType
{
    /// <summary>
    /// Monthly budget period (calendar month)
    /// </summary>
    Monthly,
    
    /// <summary>
    /// Weekly budget period (Sunday to Saturday)
    /// </summary>
    Weekly,
    
    /// <summary>
    /// Custom date range
    /// </summary>
    Custom
}

/// <summary>
/// Alert frequency setting
/// </summary>
public enum AlertFrequency
{
    /// <summary>
    /// Send alert only once per threshold per period
    /// </summary>
    Once,
    
    /// <summary>
    /// Send alert daily when threshold exceeded
    /// </summary>
    Daily,
    
    /// <summary>
    /// Send alert every time threshold is checked
    /// </summary>
    EveryTime
}
