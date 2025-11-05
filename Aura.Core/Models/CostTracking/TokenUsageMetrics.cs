using System;

namespace Aura.Core.Models.CostTracking;

/// <summary>
/// Detailed token usage metrics for LLM operations
/// </summary>
public record TokenUsageMetrics
{
    /// <summary>
    /// Unique identifier for this usage record
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Timestamp when tokens were used
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Provider name (OpenAI, Anthropic, etc.)
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Model name (gpt-4, claude-3-sonnet, etc.)
    /// </summary>
    public required string ModelName { get; init; }
    
    /// <summary>
    /// Operation type (Planning, Scripting, etc.)
    /// </summary>
    public required string OperationType { get; init; }
    
    /// <summary>
    /// Number of input tokens (prompt)
    /// </summary>
    public required int InputTokens { get; init; }
    
    /// <summary>
    /// Number of output tokens (completion)
    /// </summary>
    public required int OutputTokens { get; init; }
    
    /// <summary>
    /// Total tokens used (input + output)
    /// </summary>
    public int TotalTokens => InputTokens + OutputTokens;
    
    /// <summary>
    /// Response time in milliseconds
    /// </summary>
    public required long ResponseTimeMs { get; init; }
    
    /// <summary>
    /// Number of retry attempts before success
    /// </summary>
    public int RetryCount { get; init; }
    
    /// <summary>
    /// Whether this response was served from cache
    /// </summary>
    public bool CacheHit { get; init; }
    
    /// <summary>
    /// Estimated cost in USD for this operation
    /// </summary>
    public required decimal EstimatedCost { get; init; }
    
    /// <summary>
    /// Job ID this operation belongs to
    /// </summary>
    public string? JobId { get; init; }
    
    /// <summary>
    /// Project ID this operation belongs to
    /// </summary>
    public string? ProjectId { get; init; }
    
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; init; } = true;
    
    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Additional metadata (JSON serialized)
    /// </summary>
    public string? Metadata { get; init; }
}

/// <summary>
/// Aggregated token usage statistics
/// </summary>
public record TokenUsageStatistics
{
    /// <summary>
    /// Total input tokens across all operations
    /// </summary>
    public long TotalInputTokens { get; init; }
    
    /// <summary>
    /// Total output tokens across all operations
    /// </summary>
    public long TotalOutputTokens { get; init; }
    
    /// <summary>
    /// Total tokens (input + output)
    /// </summary>
    public long TotalTokens => TotalInputTokens + TotalOutputTokens;
    
    /// <summary>
    /// Total number of operations
    /// </summary>
    public int OperationCount { get; init; }
    
    /// <summary>
    /// Number of cache hits
    /// </summary>
    public int CacheHits { get; init; }
    
    /// <summary>
    /// Cache hit percentage
    /// </summary>
    public double CacheHitRate => OperationCount > 0 
        ? (double)CacheHits / OperationCount * 100 
        : 0;
    
    /// <summary>
    /// Average tokens per operation
    /// </summary>
    public double AverageTokensPerOperation => OperationCount > 0 
        ? (double)TotalTokens / OperationCount 
        : 0;
    
    /// <summary>
    /// Average response time in milliseconds
    /// </summary>
    public long AverageResponseTimeMs { get; init; }
    
    /// <summary>
    /// Total estimated cost
    /// </summary>
    public decimal TotalCost { get; init; }
    
    /// <summary>
    /// Cost saved by cache hits
    /// </summary>
    public decimal CostSavedByCache { get; init; }
}
