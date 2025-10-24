using System.Collections.Generic;

namespace Aura.Core.Models.Settings;

/// <summary>
/// User-configurable AI optimization settings
/// Controls ML-driven content optimization features (opt-in)
/// </summary>
public record AIOptimizationSettings
{
    /// <summary>
    /// Master toggle for AI content optimization
    /// </summary>
    public bool Enabled { get; init; } = false;

    /// <summary>
    /// Optimization aggressiveness level
    /// </summary>
    public OptimizationLevel Level { get; init; } = OptimizationLevel.Balanced;

    /// <summary>
    /// Automatically regenerate content below quality threshold
    /// </summary>
    public bool AutoRegenerateIfLowQuality { get; init; } = false;

    /// <summary>
    /// Minimum quality score (0-100) for auto-regeneration
    /// </summary>
    public int MinimumQualityThreshold { get; init; } = 75;

    /// <summary>
    /// Track performance data for learning
    /// </summary>
    public bool TrackPerformanceData { get; init; } = true;

    /// <summary>
    /// Share anonymous analytics for improvement
    /// </summary>
    public bool ShareAnonymousAnalytics { get; init; } = false;

    /// <summary>
    /// Metrics to optimize for
    /// </summary>
    public List<OptimizationMetric> OptimizationMetrics { get; init; } = new()
    {
        OptimizationMetric.Engagement,
        OptimizationMetric.Quality,
        OptimizationMetric.Authenticity
    };

    /// <summary>
    /// Enabled AI providers for optimization
    /// </summary>
    public List<string> EnabledProviders { get; init; } = new()
    {
        "Ollama",
        "OpenAI",
        "Gemini",
        "Azure"
    };

    /// <summary>
    /// Provider selection mode
    /// </summary>
    public ProviderSelectionMode SelectionMode { get; init; } = ProviderSelectionMode.Automatic;

    /// <summary>
    /// Learning aggressiveness
    /// </summary>
    public LearningMode LearningMode { get; init; } = LearningMode.Normal;

    /// <summary>
    /// Create default settings
    /// </summary>
    public static AIOptimizationSettings Default => new();
}

/// <summary>
/// Optimization aggressiveness levels
/// </summary>
public enum OptimizationLevel
{
    /// <summary>
    /// Minimal optimization, preserve user input
    /// </summary>
    Conservative,

    /// <summary>
    /// Moderate optimization balancing quality and user intent
    /// </summary>
    Balanced,

    /// <summary>
    /// Maximum optimization for best quality
    /// </summary>
    Aggressive
}

/// <summary>
/// Optimization metrics to prioritize
/// </summary>
public enum OptimizationMetric
{
    /// <summary>
    /// Optimize for viewer engagement
    /// </summary>
    Engagement,

    /// <summary>
    /// Optimize for content quality
    /// </summary>
    Quality,

    /// <summary>
    /// Optimize for authenticity and human feel
    /// </summary>
    Authenticity,

    /// <summary>
    /// Optimize for generation speed
    /// </summary>
    Speed
}

/// <summary>
/// Provider selection modes
/// </summary>
public enum ProviderSelectionMode
{
    /// <summary>
    /// Automatically select best provider based on content type
    /// </summary>
    Automatic,

    /// <summary>
    /// User manually selects provider
    /// </summary>
    Manual
}

/// <summary>
/// Learning mode settings
/// </summary>
public enum LearningMode
{
    /// <summary>
    /// Minimal learning, only track essential data
    /// </summary>
    Passive,

    /// <summary>
    /// Standard learning and optimization
    /// </summary>
    Normal,

    /// <summary>
    /// Aggressive learning with frequent model updates
    /// </summary>
    Aggressive
}
