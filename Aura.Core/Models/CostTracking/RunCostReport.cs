using System;
using System.Collections.Generic;

namespace Aura.Core.Models.CostTracking;

/// <summary>
/// Comprehensive cost report for a video generation run
/// </summary>
public record RunCostReport
{
    /// <summary>
    /// Job ID this report is for
    /// </summary>
    public required string JobId { get; init; }
    
    /// <summary>
    /// Project ID
    /// </summary>
    public string? ProjectId { get; init; }
    
    /// <summary>
    /// Project name
    /// </summary>
    public string? ProjectName { get; init; }
    
    /// <summary>
    /// When the run started
    /// </summary>
    public required DateTime StartedAt { get; init; }
    
    /// <summary>
    /// When the run completed
    /// </summary>
    public DateTime? CompletedAt { get; init; }
    
    /// <summary>
    /// Total duration in seconds
    /// </summary>
    public double DurationSeconds => CompletedAt.HasValue 
        ? (CompletedAt.Value - StartedAt).TotalSeconds 
        : 0;
    
    /// <summary>
    /// Total cost across all stages and providers
    /// </summary>
    public required decimal TotalCost { get; init; }
    
    /// <summary>
    /// Currency (USD, EUR, etc.)
    /// </summary>
    public string Currency { get; init; } = "USD";
    
    /// <summary>
    /// Cost breakdown by stage
    /// </summary>
    public required Dictionary<string, StageCostBreakdown> CostByStage { get; init; }
    
    /// <summary>
    /// Cost breakdown by provider
    /// </summary>
    public required Dictionary<string, decimal> CostByProvider { get; init; }
    
    /// <summary>
    /// Token usage statistics (LLM operations only)
    /// </summary>
    public TokenUsageStatistics? TokenStats { get; init; }
    
    /// <summary>
    /// List of all operations performed
    /// </summary>
    public List<OperationCostDetail> Operations { get; init; } = new();
    
    /// <summary>
    /// Optimization suggestions if available
    /// </summary>
    public List<CostOptimizationSuggestion> OptimizationSuggestions { get; init; } = new();
    
    /// <summary>
    /// Whether the run was within budget
    /// </summary>
    public bool WithinBudget { get; init; } = true;
    
    /// <summary>
    /// Budget limit if applicable
    /// </summary>
    public decimal? BudgetLimit { get; init; }
}

/// <summary>
/// Cost breakdown for a specific pipeline stage
/// </summary>
public record StageCostBreakdown
{
    /// <summary>
    /// Stage name (ScriptGeneration, TTS, Visuals, Rendering)
    /// </summary>
    public required string StageName { get; init; }
    
    /// <summary>
    /// Total cost for this stage
    /// </summary>
    public required decimal Cost { get; init; }
    
    /// <summary>
    /// Percentage of total cost
    /// </summary>
    public double PercentageOfTotal { get; init; }
    
    /// <summary>
    /// Duration of this stage in seconds
    /// </summary>
    public double DurationSeconds { get; init; }
    
    /// <summary>
    /// Number of operations in this stage
    /// </summary>
    public int OperationCount { get; init; }
    
    /// <summary>
    /// Primary provider used for this stage
    /// </summary>
    public string? ProviderName { get; init; }
}

/// <summary>
/// Detailed cost information for a single operation
/// </summary>
public record OperationCostDetail
{
    /// <summary>
    /// Operation timestamp
    /// </summary>
    public required DateTime Timestamp { get; init; }
    
    /// <summary>
    /// Operation type/description
    /// </summary>
    public required string OperationType { get; init; }
    
    /// <summary>
    /// Provider used
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Cost of this operation
    /// </summary>
    public required decimal Cost { get; init; }
    
    /// <summary>
    /// Duration in milliseconds
    /// </summary>
    public long DurationMs { get; init; }
    
    /// <summary>
    /// Tokens used (if LLM operation)
    /// </summary>
    public int? TokensUsed { get; init; }
    
    /// <summary>
    /// Characters processed (if TTS operation)
    /// </summary>
    public int? CharactersProcessed { get; init; }
    
    /// <summary>
    /// Whether operation was served from cache
    /// </summary>
    public bool CacheHit { get; init; }
}

/// <summary>
/// Cost optimization suggestion
/// </summary>
public record CostOptimizationSuggestion
{
    /// <summary>
    /// Category of optimization
    /// </summary>
    public required OptimizationCategory Category { get; init; }
    
    /// <summary>
    /// Description of the suggestion
    /// </summary>
    public required string Suggestion { get; init; }
    
    /// <summary>
    /// Estimated cost savings
    /// </summary>
    public decimal EstimatedSavings { get; init; }
    
    /// <summary>
    /// Impact on quality or features
    /// </summary>
    public string? QualityImpact { get; init; }
}

/// <summary>
/// Categories of cost optimization
/// </summary>
public enum OptimizationCategory
{
    /// <summary>
    /// Use lower-cost model alternatives
    /// </summary>
    ModelSelection,
    
    /// <summary>
    /// Reduce token usage through prompt optimization
    /// </summary>
    PromptOptimization,
    
    /// <summary>
    /// Enable or increase caching
    /// </summary>
    Caching,
    
    /// <summary>
    /// Use different provider with better pricing
    /// </summary>
    ProviderSwitch,
    
    /// <summary>
    /// Reduce output length or quality
    /// </summary>
    OutputReduction,
    
    /// <summary>
    /// Batch operations more efficiently
    /// </summary>
    Batching
}
