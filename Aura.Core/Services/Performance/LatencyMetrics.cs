using System;

namespace Aura.Core.Services.Performance;

/// <summary>
/// Metrics for tracking LLM operation latency and performance
/// </summary>
public record LatencyMetrics
{
    /// <summary>
    /// Provider name (e.g., "OpenAI", "Ollama", "Gemini")
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// Operation type (e.g., "ScriptGeneration", "VisualPrompt", "PacingAnalysis")
    /// </summary>
    public string OperationType { get; init; } = string.Empty;

    /// <summary>
    /// Approximate token count in the prompt
    /// </summary>
    public int PromptTokenCount { get; init; }

    /// <summary>
    /// Actual response time in milliseconds
    /// </summary>
    public long ResponseTimeMs { get; init; }

    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Number of retry attempts made (0 if succeeded on first try)
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Timestamp when the operation was recorded
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional context or error information
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Configuration for LLM operation timeouts
/// </summary>
public record LlmTimeoutPolicy
{
    /// <summary>
    /// Timeout for script generation operations (default: 120 seconds)
    /// </summary>
    public int ScriptGenerationTimeoutSeconds { get; init; } = 120;

    /// <summary>
    /// Timeout for script refinement operations (default: 180 seconds for multi-pass)
    /// </summary>
    public int ScriptRefinementTimeoutSeconds { get; init; } = 180;

    /// <summary>
    /// Timeout for visual prompt generation per scene (default: 45 seconds)
    /// </summary>
    public int VisualPromptTimeoutSeconds { get; init; } = 45;

    /// <summary>
    /// Timeout for narration optimization per scene (default: 30 seconds)
    /// </summary>
    public int NarrationOptimizationTimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Timeout for pacing analysis for full video (default: 60 seconds)
    /// </summary>
    public int PacingAnalysisTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Timeout for scene importance analysis (default: 45 seconds)
    /// </summary>
    public int SceneImportanceTimeoutSeconds { get; init; } = 45;

    /// <summary>
    /// Timeout for content complexity analysis (default: 45 seconds)
    /// </summary>
    public int ContentComplexityTimeoutSeconds { get; init; } = 45;

    /// <summary>
    /// Timeout for narrative arc validation (default: 60 seconds)
    /// </summary>
    public int NarrativeArcTimeoutSeconds { get; init; } = 60;

    /// <summary>
    /// Warning threshold as percentage of timeout (default: 50%)
    /// </summary>
    public double WarningThresholdPercentage { get; init; } = 0.5;
}

/// <summary>
/// Time estimation for an LLM operation based on historical data
/// </summary>
public record TimeEstimate
{
    /// <summary>
    /// Estimated duration in seconds
    /// </summary>
    public int EstimatedSeconds { get; init; }

    /// <summary>
    /// Lower bound of estimate range
    /// </summary>
    public int MinSeconds { get; init; }

    /// <summary>
    /// Upper bound of estimate range
    /// </summary>
    public int MaxSeconds { get; init; }

    /// <summary>
    /// Confidence level of the estimate (0.0 to 1.0)
    /// Based on number of historical data points
    /// </summary>
    public double Confidence { get; init; }

    /// <summary>
    /// Human-readable description (e.g., "typically takes 15-30 seconds")
    /// </summary>
    public string Description { get; init; } = string.Empty;
}
