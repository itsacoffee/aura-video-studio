using System;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Configuration options for the VideoOrchestrator pipeline
/// </summary>
public class OrchestratorOptions
{
    /// <summary>
    /// Enable checkpoint/resume functionality
    /// </summary>
    public bool EnableCheckpoints { get; set; } = true;

    /// <summary>
    /// Checkpoint frequency (number of stages between checkpoints)
    /// </summary>
    public int CheckpointFrequency { get; set; } = 1;

    /// <summary>
    /// Enable performance metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Enable detailed logging for debugging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Maximum retry attempts per stage
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Enable circuit breaker pattern for providers
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Maximum concurrent operations for parallel stages
    /// </summary>
    public int MaxConcurrency { get; set; } = 3;

    /// <summary>
    /// Enable memory-efficient streaming for large assets
    /// </summary>
    public bool EnableStreaming { get; set; } = true;

    /// <summary>
    /// Buffer size for channel operations (in items)
    /// </summary>
    public int ChannelBufferSize { get; set; } = 10;

    /// <summary>
    /// Timeout for individual stage execution
    /// </summary>
    public TimeSpan StageTimeout { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Timeout for the entire pipeline
    /// </summary>
    public TimeSpan PipelineTimeout { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Enable automatic cleanup of temporary files
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;

    /// <summary>
    /// Retain intermediate artifacts after completion
    /// </summary>
    public bool RetainIntermediateArtifacts { get; set; } = false;

    /// <summary>
    /// Enable progress streaming via Server-Sent Events
    /// </summary>
    public bool EnableProgressStreaming { get; set; } = true;

    /// <summary>
    /// Interval for sending progress updates
    /// </summary>
    public TimeSpan ProgressUpdateInterval { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Enable fallback to safe defaults on provider failure
    /// </summary>
    public bool EnableFallbackMode { get; set; } = true;

    /// <summary>
    /// Enable RAG (Retrieval Augmented Generation) if configured
    /// </summary>
    public bool EnableRag { get; set; } = false;

    /// <summary>
    /// Enable pacing optimization
    /// </summary>
    public bool EnablePacingOptimization { get; set; } = false;

    /// <summary>
    /// Enable narration optimization for TTS
    /// </summary>
    public bool EnableNarrationOptimization { get; set; } = false;

    /// <summary>
    /// Validate these options for consistency
    /// </summary>
    public void Validate()
    {
        if (MaxRetryAttempts < 0)
            throw new ArgumentException("MaxRetryAttempts must be non-negative", nameof(MaxRetryAttempts));

        if (MaxConcurrency < 1)
            throw new ArgumentException("MaxConcurrency must be at least 1", nameof(MaxConcurrency));

        if (ChannelBufferSize < 1)
            throw new ArgumentException("ChannelBufferSize must be at least 1", nameof(ChannelBufferSize));

        if (StageTimeout <= TimeSpan.Zero)
            throw new ArgumentException("StageTimeout must be positive", nameof(StageTimeout));

        if (PipelineTimeout <= TimeSpan.Zero)
            throw new ArgumentException("PipelineTimeout must be positive", nameof(PipelineTimeout));

        if (StageTimeout > PipelineTimeout)
            throw new ArgumentException("StageTimeout cannot exceed PipelineTimeout");

        if (ProgressUpdateInterval <= TimeSpan.Zero)
            throw new ArgumentException("ProgressUpdateInterval must be positive", nameof(ProgressUpdateInterval));

        if (CheckpointFrequency < 1)
            throw new ArgumentException("CheckpointFrequency must be at least 1", nameof(CheckpointFrequency));
    }

    /// <summary>
    /// Creates default options for production use
    /// </summary>
    public static OrchestratorOptions CreateDefault()
    {
        return new OrchestratorOptions();
    }

    /// <summary>
    /// Creates options optimized for development/debugging
    /// </summary>
    public static OrchestratorOptions CreateDebug()
    {
        return new OrchestratorOptions
        {
            EnableDetailedLogging = true,
            RetainIntermediateArtifacts = true,
            StageTimeout = TimeSpan.FromMinutes(30),
            PipelineTimeout = TimeSpan.FromHours(2)
        };
    }

    /// <summary>
    /// Creates options optimized for quick demos (more lenient, faster)
    /// </summary>
    public static OrchestratorOptions CreateQuickDemo()
    {
        return new OrchestratorOptions
        {
            EnableCheckpoints = false,
            EnableMetrics = false,
            MaxRetryAttempts = 1,
            StageTimeout = TimeSpan.FromMinutes(2),
            PipelineTimeout = TimeSpan.FromMinutes(10),
            EnableAutoCleanup = true,
            RetainIntermediateArtifacts = false,
            EnableFallbackMode = true
        };
    }
}
