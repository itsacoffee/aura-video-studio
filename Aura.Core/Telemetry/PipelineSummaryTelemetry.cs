using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aura.Core.Telemetry;

/// <summary>
/// Aggregated telemetry for a complete pipeline execution.
/// Captures all metrics for a single video generation run in one place.
/// </summary>
public record PipelineSummaryTelemetry
{
    /// <summary>
    /// Schema version for forward compatibility
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";

    /// <summary>
    /// Unique identifier for this pipeline run
    /// </summary>
    [JsonPropertyName("pipeline_id")]
    public required string PipelineId { get; init; }

    /// <summary>
    /// Correlation ID for request tracing
    /// </summary>
    [JsonPropertyName("correlation_id")]
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Project ID if applicable
    /// </summary>
    [JsonPropertyName("project_id")]
    public string? ProjectId { get; init; }

    /// <summary>
    /// Video topic/title
    /// </summary>
    [JsonPropertyName("topic")]
    public required string Topic { get; init; }

    /// <summary>
    /// When pipeline started
    /// </summary>
    [JsonPropertyName("started_at")]
    public required DateTime StartedAt { get; init; }

    /// <summary>
    /// When pipeline completed
    /// </summary>
    [JsonPropertyName("completed_at")]
    public required DateTime CompletedAt { get; init; }

    /// <summary>
    /// Total execution duration
    /// </summary>
    [JsonPropertyName("total_duration_ms")]
    public long TotalDurationMs => (long)(CompletedAt - StartedAt).TotalMilliseconds;

    /// <summary>
    /// Whether pipeline completed successfully
    /// </summary>
    [JsonPropertyName("success")]
    public required bool Success { get; init; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    [JsonPropertyName("error_message")]
    public string? ErrorMessage { get; init; }

    // Token Usage
    /// <summary>
    /// Total input tokens used across all LLM operations
    /// </summary>
    [JsonPropertyName("total_input_tokens")]
    public int TotalInputTokens { get; init; }

    /// <summary>
    /// Total output tokens used across all LLM operations
    /// </summary>
    [JsonPropertyName("total_output_tokens")]
    public int TotalOutputTokens { get; init; }

    /// <summary>
    /// Combined total tokens (input + output)
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens => TotalInputTokens + TotalOutputTokens;

    // Cost Tracking
    /// <summary>
    /// Total estimated cost for this pipeline run
    /// </summary>
    [JsonPropertyName("total_cost")]
    public decimal TotalCost { get; init; }

    /// <summary>
    /// Currency code for cost values
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Cost breakdown by pipeline stage
    /// </summary>
    [JsonPropertyName("cost_by_stage")]
    public Dictionary<string, decimal> CostByStage { get; init; } = new();

    /// <summary>
    /// Cost breakdown by provider
    /// </summary>
    [JsonPropertyName("cost_by_provider")]
    public Dictionary<string, decimal> CostByProvider { get; init; } = new();

    // Stage Timings
    /// <summary>
    /// Execution time for each pipeline stage
    /// </summary>
    [JsonPropertyName("stage_timings_ms")]
    public Dictionary<string, long> StageTimingsMs { get; init; } = new();

    // Cache Metrics
    /// <summary>
    /// Number of cache hits during pipeline execution
    /// </summary>
    [JsonPropertyName("cache_hits")]
    public int CacheHits { get; init; }

    /// <summary>
    /// Number of cache misses during pipeline execution
    /// </summary>
    [JsonPropertyName("cache_misses")]
    public int CacheMisses { get; init; }

    /// <summary>
    /// Estimated cost saved by cache hits
    /// </summary>
    [JsonPropertyName("cost_saved_by_cache")]
    public decimal CostSavedByCache { get; init; }

    // Provider Usage
    /// <summary>
    /// Number of operations per provider
    /// </summary>
    [JsonPropertyName("operations_by_provider")]
    public Dictionary<string, int> OperationsByProvider { get; init; } = new();

    /// <summary>
    /// Number of retries per provider
    /// </summary>
    [JsonPropertyName("retry_count_by_provider")]
    public Dictionary<string, int> RetryCountByProvider { get; init; } = new();

    // Output Metrics
    /// <summary>
    /// Number of scenes in the generated video
    /// </summary>
    [JsonPropertyName("scene_count")]
    public int SceneCount { get; init; }

    /// <summary>
    /// Duration of generated video in seconds
    /// </summary>
    [JsonPropertyName("video_duration_seconds")]
    public double VideoDurationSeconds { get; init; }

    /// <summary>
    /// Total characters processed by TTS
    /// </summary>
    [JsonPropertyName("tts_characters")]
    public int TtsCharacters { get; init; }

    /// <summary>
    /// Number of images generated
    /// </summary>
    [JsonPropertyName("images_generated")]
    public int ImagesGenerated { get; init; }

    // Strategy Used
    /// <summary>
    /// Strategy type used for generation (e.g., "HighQuality", "Fast")
    /// </summary>
    [JsonPropertyName("strategy_type")]
    public string? StrategyType { get; init; }

    /// <summary>
    /// Visual approach used (e.g., "AI-Generated", "Stock")
    /// </summary>
    [JsonPropertyName("visual_approach")]
    public string? VisualApproach { get; init; }

    /// <summary>
    /// Maximum concurrency level used
    /// </summary>
    [JsonPropertyName("max_concurrency")]
    public int MaxConcurrency { get; init; }

    // Quality Metrics (if available)
    /// <summary>
    /// Overall quality score for the generated video (0-100)
    /// </summary>
    [JsonPropertyName("quality_score")]
    public double? QualityScore { get; init; }
}
