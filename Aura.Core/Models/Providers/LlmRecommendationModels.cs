using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Providers;

/// <summary>
/// Type of LLM operation requiring a provider
/// </summary>
public enum LlmOperationType
{
    /// <summary>
    /// Generating initial video script from brief
    /// </summary>
    ScriptGeneration,
    
    /// <summary>
    /// Refining and improving existing script
    /// </summary>
    ScriptRefinement,
    
    /// <summary>
    /// Generating detailed visual prompts for image generation
    /// </summary>
    VisualPrompts,
    
    /// <summary>
    /// Optimizing narration text for TTS synthesis
    /// </summary>
    NarrationOptimization,
    
    /// <summary>
    /// Quick, low-complexity operations (transitions, simple analysis)
    /// </summary>
    QuickOperations,
    
    /// <summary>
    /// Scene analysis for pacing optimization
    /// </summary>
    SceneAnalysis,
    
    /// <summary>
    /// Content complexity analysis
    /// </summary>
    ContentComplexity,
    
    /// <summary>
    /// Narrative arc validation
    /// </summary>
    NarrativeValidation
}

/// <summary>
/// Recommendation for which LLM provider to use for an operation
/// </summary>
public record ProviderRecommendation
{
    /// <summary>
    /// Name of the recommended provider (OpenAI, Claude, Gemini, Ollama, RuleBased)
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Human-readable reasoning for this recommendation
    /// </summary>
    public required string Reasoning { get; init; }
    
    /// <summary>
    /// Quality score estimate (0-100)
    /// </summary>
    public required int QualityScore { get; init; }
    
    /// <summary>
    /// Estimated cost in USD for this operation
    /// </summary>
    public required decimal EstimatedCost { get; init; }
    
    /// <summary>
    /// Expected latency in seconds
    /// </summary>
    public required double ExpectedLatencySeconds { get; init; }
    
    /// <summary>
    /// Whether this provider is currently available (API key configured, service healthy)
    /// </summary>
    public required bool IsAvailable { get; init; }
    
    /// <summary>
    /// Current health status of this provider
    /// </summary>
    public ProviderHealthStatus HealthStatus { get; init; }
    
    /// <summary>
    /// Confidence in this recommendation (0-100)
    /// </summary>
    public int Confidence { get; init; } = 100;
}

/// <summary>
/// Health status of a provider
/// </summary>
public enum ProviderHealthStatus
{
    /// <summary>
    /// Provider is healthy (>90% success rate)
    /// </summary>
    Healthy,
    
    /// <summary>
    /// Provider is degraded (70-90% success rate)
    /// </summary>
    Degraded,
    
    /// <summary>
    /// Provider is unhealthy (<70% success rate)
    /// </summary>
    Unhealthy,
    
    /// <summary>
    /// Provider status is unknown (not enough data)
    /// </summary>
    Unknown
}

/// <summary>
/// Health metrics for a provider
/// </summary>
public record ProviderHealthMetrics
{
    /// <summary>
    /// Provider name
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Success rate as percentage (0-100)
    /// </summary>
    public required double SuccessRatePercent { get; init; }
    
    /// <summary>
    /// Average latency in seconds
    /// </summary>
    public required double AverageLatencySeconds { get; init; }
    
    /// <summary>
    /// Total number of requests tracked
    /// </summary>
    public required int TotalRequests { get; init; }
    
    /// <summary>
    /// Number of consecutive failures
    /// </summary>
    public required int ConsecutiveFailures { get; init; }
    
    /// <summary>
    /// Overall health status
    /// </summary>
    public required ProviderHealthStatus Status { get; init; }
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Cost estimate for an LLM operation
/// </summary>
public record CostEstimate
{
    /// <summary>
    /// Provider name
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Operation type
    /// </summary>
    public required LlmOperationType OperationType { get; init; }
    
    /// <summary>
    /// Estimated input tokens
    /// </summary>
    public required int EstimatedInputTokens { get; init; }
    
    /// <summary>
    /// Estimated output tokens
    /// </summary>
    public required int EstimatedOutputTokens { get; init; }
    
    /// <summary>
    /// Total estimated tokens
    /// </summary>
    public int TotalTokens => EstimatedInputTokens + EstimatedOutputTokens;
    
    /// <summary>
    /// Estimated cost in USD
    /// </summary>
    public required decimal EstimatedCostUsd { get; init; }
    
    /// <summary>
    /// Cost per 1K tokens for this provider
    /// </summary>
    public required decimal CostPer1KTokens { get; init; }
}

/// <summary>
/// User preference profile for provider selection
/// </summary>
public enum ProviderProfile
{
    /// <summary>
    /// Always use highest quality provider regardless of cost
    /// </summary>
    MaximumQuality,
    
    /// <summary>
    /// Balance between quality, cost, and speed
    /// </summary>
    Balanced,
    
    /// <summary>
    /// Prefer cheaper providers that meet minimum quality threshold
    /// </summary>
    BudgetConscious,
    
    /// <summary>
    /// Optimize for fastest response times
    /// </summary>
    SpeedOptimized,
    
    /// <summary>
    /// Only use local/offline providers (Ollama, RuleBased)
    /// </summary>
    LocalOnly,
    
    /// <summary>
    /// User-defined custom rules
    /// </summary>
    Custom
}

/// <summary>
/// User's provider preferences
/// </summary>
public record ProviderPreferences
{
    /// <summary>
    /// Global default provider for all operations
    /// </summary>
    public string? GlobalDefault { get; init; }
    
    /// <summary>
    /// Whether to always use the global default (bypass recommendations)
    /// </summary>
    public bool AlwaysUseDefault { get; init; }
    
    /// <summary>
    /// Per-operation provider overrides
    /// </summary>
    public Dictionary<LlmOperationType, string> PerOperationOverrides { get; init; } = new();
    
    /// <summary>
    /// Active provider profile
    /// </summary>
    public ProviderProfile ActiveProfile { get; init; } = ProviderProfile.Balanced;
    
    /// <summary>
    /// Providers to exclude from recommendations (soft exclusion - still manually selectable)
    /// </summary>
    public HashSet<string> ExcludedProviders { get; init; } = new();
    
    /// <summary>
    /// Pinned provider (overrides all recommendations unless user changes)
    /// </summary>
    public string? PinnedProvider { get; init; }
    
    /// <summary>
    /// Whether to enable automatic failover to fallback provider
    /// </summary>
    public bool AutoFailover { get; init; } = false;
    
    /// <summary>
    /// Fallback chains per operation type
    /// </summary>
    public Dictionary<LlmOperationType, List<string>> FallbackChains { get; init; } = new();
    
    /// <summary>
    /// Whether to enable preference learning (opt-in)
    /// </summary>
    public bool EnableLearning { get; init; } = false;
    
    /// <summary>
    /// Monthly budget limit in USD (null = no limit)
    /// </summary>
    public decimal? MonthlyBudgetLimit { get; init; }
    
    /// <summary>
    /// Per-provider monthly budget limits
    /// </summary>
    public Dictionary<string, decimal> PerProviderBudgetLimits { get; init; } = new();
    
    /// <summary>
    /// Whether budget warnings are hard limits (block generation) or soft warnings
    /// </summary>
    public bool HardBudgetLimit { get; init; } = false;
}

/// <summary>
/// Learned user preference based on override history
/// </summary>
public record LearnedPreference
{
    /// <summary>
    /// Operation type this preference applies to
    /// </summary>
    public required LlmOperationType OperationType { get; init; }
    
    /// <summary>
    /// Provider the user consistently prefers
    /// </summary>
    public required string PreferredProvider { get; init; }
    
    /// <summary>
    /// Number of times user selected this provider for this operation
    /// </summary>
    public required int SelectionCount { get; init; }
    
    /// <summary>
    /// Confidence in this learned preference (0-100)
    /// </summary>
    public required int Confidence { get; init; }
    
    /// <summary>
    /// When this preference was learned
    /// </summary>
    public DateTime LearnedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Monthly cost tracking for a provider
/// </summary>
public record ProviderCostTracking
{
    /// <summary>
    /// Provider name
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Month (YYYY-MM format)
    /// </summary>
    public required string Month { get; init; }
    
    /// <summary>
    /// Total cost in USD for this month
    /// </summary>
    public required decimal TotalCostUsd { get; init; }
    
    /// <summary>
    /// Cost breakdown by operation type
    /// </summary>
    public Dictionary<LlmOperationType, decimal> CostByOperation { get; init; } = new();
    
    /// <summary>
    /// Number of operations performed
    /// </summary>
    public int OperationCount { get; init; }
    
    /// <summary>
    /// Last updated timestamp
    /// </summary>
    public DateTime LastUpdated { get; init; } = DateTime.UtcNow;
}
