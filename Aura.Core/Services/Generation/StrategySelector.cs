using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Generation;

/// <summary>
/// Selects optimal generation strategies based on content type, system resources, and historical performance.
/// Uses heuristic-based selection with learning capabilities.
/// </summary>
public class StrategySelector
{
    private readonly ILogger<StrategySelector> _logger;
    private readonly Dictionary<string, StrategyPerformance> _performanceHistory = new();
    private readonly object _historyLock = new();

    public StrategySelector(ILogger<StrategySelector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Selects the optimal generation strategy for the given brief and system profile
    /// </summary>
    public GenerationStrategy SelectStrategy(Brief brief, SystemProfile systemProfile, PlanSpec planSpec)
    {
        _logger.LogInformation(
            "Selecting generation strategy for topic '{Topic}', duration {Duration}",
            brief.Topic,
            planSpec.TargetDuration);

        // Analyze content characteristics
        var contentComplexity = AnalyzeContentComplexity(brief, planSpec);
        var resourceAvailability = AnalyzeResourceAvailability(systemProfile);

        // Choose strategy based on heuristics
        var strategyType = DetermineStrategyType(contentComplexity, resourceAvailability, systemProfile);

        // Determine concurrency level
        int maxConcurrency = DetermineMaxConcurrency(systemProfile, contentComplexity);

        // Choose generation approach for visuals
        var visualApproach = DetermineVisualApproach(brief, systemProfile, contentComplexity);

        var strategy = new GenerationStrategy(
            strategyType,
            maxConcurrency,
            visualApproach,
            contentComplexity,
            EnableEarlyFallback: contentComplexity > 0.7, // Enable fallback for complex content
            EnableProgressiveCaching: true);

        _logger.LogInformation(
            "Selected strategy: {Type}, Concurrency: {Concurrency}, Visual: {Visual}, Complexity: {Complexity:F2}",
            strategyType,
            maxConcurrency,
            visualApproach,
            contentComplexity);

        return strategy;
    }

    /// <summary>
    /// Records the performance of a completed strategy execution
    /// </summary>
    public void RecordStrategyPerformance(
        GenerationStrategy strategy,
        TimeSpan executionTime,
        bool succeeded,
        double qualityScore)
    {
        var key = strategy.StrategyType.ToString();

        lock (_historyLock)
        {
            if (!_performanceHistory.TryGetValue(key, out var perf))
            {
                perf = new StrategyPerformance(key);
                _performanceHistory[key] = perf;
            }

            perf.RecordExecution(executionTime, succeeded, qualityScore);

            _logger.LogInformation(
                "Recorded strategy performance: {Strategy}, Success: {Success}, Quality: {Quality:F2}, AvgTime: {AvgTime}",
                key,
                succeeded,
                qualityScore,
                perf.AverageExecutionTime);
        }
    }

    /// <summary>
    /// Gets performance statistics for a strategy type
    /// </summary>
    public StrategyPerformance? GetStrategyPerformance(StrategyType strategyType)
    {
        var key = strategyType.ToString();
        lock (_historyLock)
        {
            return _performanceHistory.TryGetValue(key, out var perf) ? perf : null;
        }
    }

    private double AnalyzeContentComplexity(Brief brief, PlanSpec planSpec)
    {
        double complexity = 0.5; // Base complexity

        // Adjust for duration (longer = more complex)
        if (planSpec.TargetDuration.TotalMinutes > 10)
        {
            complexity += 0.2;
        }
        else if (planSpec.TargetDuration.TotalMinutes > 5)
        {
            complexity += 0.1;
        }

        // Adjust for pacing (faster = more complex)
        if (planSpec.Pacing == Pacing.Fast)
        {
            complexity += 0.1;
        }

        // Adjust for density (higher = more complex)
        if (planSpec.Density == Density.Dense)
        {
            complexity += 0.15;
        }

        // Technical topics are typically more complex
        if (IsTechnicalTopic(brief.Topic))
        {
            complexity += 0.1;
        }

        return Math.Min(1.0, complexity);
    }

    private double AnalyzeResourceAvailability(SystemProfile profile)
    {
        double availability = 0.5; // Base availability

        // High-end hardware
        if (profile.Tier == HardwareTier.A)
        {
            availability = 0.9;
        }
        else if (profile.Tier == HardwareTier.B || profile.Tier == HardwareTier.C)
        {
            availability = 0.6;
        }
        else
        {
            availability = 0.3;
        }

        // GPU acceleration bonus
        if (profile.Gpu != null)
        {
            availability = Math.Min(1.0, availability + 0.1);
        }

        return availability;
    }

    private StrategyType DetermineStrategyType(
        double contentComplexity,
        double resourceAvailability,
        SystemProfile profile)
    {
        // Offline mode requires sequential strategy
        if (profile.OfflineOnly)
        {
            return StrategyType.Sequential;
        }

        // High resources and high complexity: use adaptive
        if (resourceAvailability > 0.7 && contentComplexity > 0.6)
        {
            return StrategyType.Adaptive;
        }

        // Good resources: use parallel
        if (resourceAvailability > 0.5)
        {
            return StrategyType.Parallel;
        }

        // Limited resources: use sequential
        return StrategyType.Sequential;
    }

    private int DetermineMaxConcurrency(SystemProfile profile, double contentComplexity)
    {
        int baseConcurrency = Math.Max(1, profile.LogicalCores / 2);

        // Reduce concurrency for complex content
        if (contentComplexity > 0.7)
        {
            baseConcurrency = Math.Max(1, baseConcurrency / 2);
        }

        // Limit based on hardware tier
        if (profile.Tier == HardwareTier.D)
        {
            baseConcurrency = Math.Min(baseConcurrency, 2);
        }
        else if (profile.Tier == HardwareTier.C)
        {
            baseConcurrency = Math.Min(baseConcurrency, 4);
        }

        return Math.Max(1, Math.Min(baseConcurrency, 8)); // Cap at 8
    }

    private VisualGenerationApproach DetermineVisualApproach(
        Brief brief,
        SystemProfile profile,
        double contentComplexity)
    {
        // Offline mode: stock only
        if (profile.OfflineOnly)
        {
            return VisualGenerationApproach.StockOnly;
        }

        // AI generation not available: stock only
        if (!profile.EnableSD)
        {
            return VisualGenerationApproach.StockOnly;
        }

        // Complex or technical content: prefer AI generation
        if (contentComplexity > 0.6 || IsTechnicalTopic(brief.Topic))
        {
            return VisualGenerationApproach.HybridAIFirst;
        }

        // Default: hybrid with stock preference
        return VisualGenerationApproach.HybridStockFirst;
    }

    private bool IsTechnicalTopic(string topic)
    {
        var technicalKeywords = new[]
        {
            "programming", "software", "code", "algorithm", "technology",
            "science", "engineering", "math", "physics", "chemistry",
            "computer", "ai", "machine learning", "data", "api"
        };

        return technicalKeywords.Any(keyword =>
            topic.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Defines the overall generation strategy
/// </summary>
public record GenerationStrategy(
    StrategyType StrategyType,
    int MaxConcurrency,
    VisualGenerationApproach VisualApproach,
    double ContentComplexity,
    bool EnableEarlyFallback,
    bool EnableProgressiveCaching);

/// <summary>
/// Types of generation strategies
/// </summary>
public enum StrategyType
{
    /// <summary>Sequential execution of tasks</summary>
    Sequential,

    /// <summary>Parallel execution where possible</summary>
    Parallel,

    /// <summary>Adaptive execution based on runtime conditions</summary>
    Adaptive
}

/// <summary>
/// Approaches for visual asset generation
/// </summary>
public enum VisualGenerationApproach
{
    /// <summary>Use only stock images</summary>
    StockOnly,

    /// <summary>Use only AI-generated images</summary>
    AIOnly,

    /// <summary>Hybrid approach, preferring stock images</summary>
    HybridStockFirst,

    /// <summary>Hybrid approach, preferring AI-generated images</summary>
    HybridAIFirst
}

/// <summary>
/// Tracks performance metrics for a generation strategy
/// </summary>
public class StrategyPerformance
{
    private readonly List<ExecutionRecord> _executions = new();

    public string StrategyName { get; }
    public int TotalExecutions => _executions.Count;
    public int SuccessfulExecutions => _executions.Count(e => e.Succeeded);
    public double SuccessRate => TotalExecutions > 0 ? (double)SuccessfulExecutions / TotalExecutions : 0;
    public TimeSpan AverageExecutionTime => TotalExecutions > 0
        ? TimeSpan.FromMilliseconds(_executions.Average(e => e.ExecutionTime.TotalMilliseconds))
        : TimeSpan.Zero;
    public double AverageQualityScore => TotalExecutions > 0 ? _executions.Average(e => e.QualityScore) : 0;

    public StrategyPerformance(string strategyName)
    {
        StrategyName = strategyName;
    }

    public void RecordExecution(TimeSpan executionTime, bool succeeded, double qualityScore)
    {
        _executions.Add(new ExecutionRecord(executionTime, succeeded, qualityScore));

        // Keep only last 100 executions to prevent unbounded growth
        if (_executions.Count > 100)
        {
            _executions.RemoveAt(0);
        }
    }

    private sealed record ExecutionRecord(TimeSpan ExecutionTime, bool Succeeded, double QualityScore);
}
