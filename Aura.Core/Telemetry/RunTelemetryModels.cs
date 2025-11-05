using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aura.Core.Telemetry;

/// <summary>
/// Unified telemetry record for a single pipeline stage operation
/// Conforms to RunTelemetry v1 schema
/// </summary>
public record RunTelemetryRecord
{
    /// <summary>
    /// Schema version (always "1.0")
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";
    
    /// <summary>
    /// Unique job identifier
    /// </summary>
    [JsonPropertyName("job_id")]
    public required string JobId { get; init; }
    
    /// <summary>
    /// Request correlation identifier for tracing
    /// </summary>
    [JsonPropertyName("correlation_id")]
    public required string CorrelationId { get; init; }
    
    /// <summary>
    /// Optional project identifier
    /// </summary>
    [JsonPropertyName("project_id")]
    public string? ProjectId { get; init; }
    
    /// <summary>
    /// Pipeline stage name
    /// </summary>
    [JsonPropertyName("stage")]
    public required RunStage Stage { get; init; }
    
    /// <summary>
    /// Optional scene index for per-scene operations
    /// </summary>
    [JsonPropertyName("scene_index")]
    public int? SceneIndex { get; init; }
    
    /// <summary>
    /// Model identifier used (e.g., 'gpt-4', 'claude-3-sonnet')
    /// </summary>
    [JsonPropertyName("model_id")]
    public string? ModelId { get; init; }
    
    /// <summary>
    /// Provider name (e.g., 'OpenAI', 'ElevenLabs', 'Piper')
    /// </summary>
    [JsonPropertyName("provider")]
    public string? Provider { get; init; }
    
    /// <summary>
    /// How the provider was selected
    /// </summary>
    [JsonPropertyName("selection_source")]
    public SelectionSource? SelectionSource { get; init; }
    
    /// <summary>
    /// Reason for fallback if selection_source is 'fallback'
    /// </summary>
    [JsonPropertyName("fallback_reason")]
    public string? FallbackReason { get; init; }
    
    /// <summary>
    /// Input tokens for LLM operations
    /// </summary>
    [JsonPropertyName("tokens_in")]
    public int? TokensIn { get; init; }
    
    /// <summary>
    /// Output tokens for LLM operations
    /// </summary>
    [JsonPropertyName("tokens_out")]
    public int? TokensOut { get; init; }
    
    /// <summary>
    /// Whether operation hit cache
    /// </summary>
    [JsonPropertyName("cache_hit")]
    public bool? CacheHit { get; init; }
    
    /// <summary>
    /// Number of retry attempts
    /// </summary>
    [JsonPropertyName("retries")]
    public int Retries { get; init; } = 0;
    
    /// <summary>
    /// Operation latency in milliseconds
    /// </summary>
    [JsonPropertyName("latency_ms")]
    public required long LatencyMs { get; init; }
    
    /// <summary>
    /// Estimated cost for this operation
    /// </summary>
    [JsonPropertyName("cost_estimate")]
    public decimal? CostEstimate { get; init; }
    
    /// <summary>
    /// Currency code for cost_estimate
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "USD";
    
    /// <summary>
    /// Pricing data version used for cost estimation
    /// </summary>
    [JsonPropertyName("pricing_version")]
    public string? PricingVersion { get; init; }
    
    /// <summary>
    /// Operation result status
    /// </summary>
    [JsonPropertyName("result_status")]
    public required ResultStatus ResultStatus { get; init; }
    
    /// <summary>
    /// Error code if result_status is 'error' or 'warn'
    /// </summary>
    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; init; }
    
    /// <summary>
    /// Human-readable status message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }
    
    /// <summary>
    /// ISO 8601 timestamp when operation started
    /// </summary>
    [JsonPropertyName("started_at")]
    public required DateTime StartedAt { get; init; }
    
    /// <summary>
    /// ISO 8601 timestamp when operation ended
    /// </summary>
    [JsonPropertyName("ended_at")]
    public required DateTime EndedAt { get; init; }
    
    /// <summary>
    /// Additional stage-specific metadata (no PII)
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Pipeline stage names
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RunStage
{
    Brief,
    Plan,
    Script,
    Ssml,
    Tts,
    Visuals,
    Render,
    Post
}

/// <summary>
/// How a provider was selected
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SelectionSource
{
    Default,
    Pinned,
    Cli,
    Fallback
}

/// <summary>
/// Operation result status
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResultStatus
{
    Ok,
    Warn,
    Error
}

/// <summary>
/// Collection of telemetry records for a complete run
/// </summary>
public record RunTelemetryCollection
{
    /// <summary>
    /// Schema version
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";
    
    /// <summary>
    /// Job identifier
    /// </summary>
    [JsonPropertyName("job_id")]
    public required string JobId { get; init; }
    
    /// <summary>
    /// Correlation identifier
    /// </summary>
    [JsonPropertyName("correlation_id")]
    public required string CorrelationId { get; init; }
    
    /// <summary>
    /// When telemetry collection started
    /// </summary>
    [JsonPropertyName("collection_started_at")]
    public required DateTime CollectionStartedAt { get; init; }
    
    /// <summary>
    /// When telemetry collection ended
    /// </summary>
    [JsonPropertyName("collection_ended_at")]
    public DateTime? CollectionEndedAt { get; init; }
    
    /// <summary>
    /// All telemetry records for this run
    /// </summary>
    [JsonPropertyName("records")]
    public required List<RunTelemetryRecord> Records { get; init; }
    
    /// <summary>
    /// Summary statistics
    /// </summary>
    [JsonPropertyName("summary")]
    public RunTelemetrySummary? Summary { get; init; }
}

/// <summary>
/// Summary statistics for a run
/// </summary>
public record RunTelemetrySummary
{
    /// <summary>
    /// Total number of operations
    /// </summary>
    [JsonPropertyName("total_operations")]
    public int TotalOperations { get; init; }
    
    /// <summary>
    /// Number of successful operations
    /// </summary>
    [JsonPropertyName("successful_operations")]
    public int SuccessfulOperations { get; init; }
    
    /// <summary>
    /// Number of failed operations
    /// </summary>
    [JsonPropertyName("failed_operations")]
    public int FailedOperations { get; init; }
    
    /// <summary>
    /// Total estimated cost
    /// </summary>
    [JsonPropertyName("total_cost")]
    public decimal TotalCost { get; init; }
    
    /// <summary>
    /// Currency for total cost
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "USD";
    
    /// <summary>
    /// Total latency in milliseconds
    /// </summary>
    [JsonPropertyName("total_latency_ms")]
    public long TotalLatencyMs { get; init; }
    
    /// <summary>
    /// Total input tokens
    /// </summary>
    [JsonPropertyName("total_tokens_in")]
    public int TotalTokensIn { get; init; }
    
    /// <summary>
    /// Total output tokens
    /// </summary>
    [JsonPropertyName("total_tokens_out")]
    public int TotalTokensOut { get; init; }
    
    /// <summary>
    /// Cache hit count
    /// </summary>
    [JsonPropertyName("cache_hits")]
    public int CacheHits { get; init; }
    
    /// <summary>
    /// Total retry count
    /// </summary>
    [JsonPropertyName("total_retries")]
    public int TotalRetries { get; init; }
    
    /// <summary>
    /// Cost breakdown by stage
    /// </summary>
    [JsonPropertyName("cost_by_stage")]
    public Dictionary<string, decimal>? CostByStage { get; init; }
    
    /// <summary>
    /// Operations breakdown by provider
    /// </summary>
    [JsonPropertyName("operations_by_provider")]
    public Dictionary<string, int>? OperationsByProvider { get; init; }
}
