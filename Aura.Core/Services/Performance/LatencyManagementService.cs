using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Performance;

/// <summary>
/// Manages LLM latency tracking, prediction, and historical data analysis
/// </summary>
public class LatencyManagementService
{
    private readonly ILogger<LatencyManagementService> _logger;
    private readonly LatencyTelemetry _telemetry;
    private readonly LlmTimeoutPolicy _timeoutPolicy;
    
    private readonly ConcurrentDictionary<string, List<LatencyMetrics>> _historicalData;
    private const int MaxHistoricalRecords = 100;
    private const int MinDataPointsForAccuratePrediction = 10;

    public LatencyManagementService(
        ILogger<LatencyManagementService> logger,
        LatencyTelemetry telemetry,
        LlmTimeoutPolicy timeoutPolicy)
    {
        _logger = logger;
        _telemetry = telemetry;
        _timeoutPolicy = timeoutPolicy;
        _historicalData = new ConcurrentDictionary<string, List<LatencyMetrics>>();
    }

    /// <summary>
    /// Record metrics for a completed LLM operation
    /// </summary>
    public void RecordMetrics(LatencyMetrics metrics)
    {
        var key = GetMetricsKey(metrics.ProviderName, metrics.OperationType);
        
        _historicalData.AddOrUpdate(
            key,
            _ => new List<LatencyMetrics> { metrics },
            (_, existing) =>
            {
                lock (existing)
                {
                    existing.Add(metrics);
                    
                    // Keep only the most recent records
                    if (existing.Count > MaxHistoricalRecords)
                    {
                        existing.RemoveAt(0);
                    }
                    
                    return existing;
                }
            });

        _telemetry.LogLatencyMetrics(metrics);
    }

    /// <summary>
    /// Predict time estimate for an LLM operation based on historical data
    /// </summary>
    public TimeEstimate PredictDuration(string providerName, string operationType, int promptTokenCount)
    {
        var key = GetMetricsKey(providerName, operationType);
        
        if (!_historicalData.TryGetValue(key, out var history) || history.Count == 0)
        {
            return GetDefaultEstimate(operationType);
        }

        List<LatencyMetrics> relevantMetrics;
        lock (history)
        {
            relevantMetrics = history
                .Where(m => m.Success)
                .OrderByDescending(m => m.Timestamp)
                .Take(50)
                .ToList();
        }

        if (relevantMetrics.Count == 0)
        {
            return GetDefaultEstimate(operationType);
        }

        var responseTimes = relevantMetrics.Select(m => (double)m.ResponseTimeMs).ToList();
        var avgMs = responseTimes.Average();
        var minMs = responseTimes.Min();
        var maxMs = responseTimes.Max();

        // Calculate standard deviation for confidence
        var variance = responseTimes.Average(t => Math.Pow(t - avgMs, 2));
        var stdDev = Math.Sqrt(variance);

        // Adjust estimate based on token count if we have varied data
        var avgTokens = relevantMetrics.Average(m => (double)m.PromptTokenCount);
        if (avgTokens > 0 && promptTokenCount > 0)
        {
            var tokenRatio = promptTokenCount / avgTokens;
            avgMs *= tokenRatio;
            minMs *= tokenRatio;
            maxMs *= tokenRatio;
        }

        // Confidence based on data points and consistency
        var confidence = CalculateConfidence(relevantMetrics.Count, stdDev, avgMs);

        var estimatedSeconds = (int)Math.Ceiling(avgMs / 1000.0);
        var minSeconds = Math.Max(1, (int)Math.Floor(minMs / 1000.0));
        var maxSeconds = (int)Math.Ceiling(maxMs / 1000.0);

        var description = GenerateEstimateDescription(minSeconds, maxSeconds, confidence);

        var estimate = new TimeEstimate
        {
            EstimatedSeconds = estimatedSeconds,
            MinSeconds = minSeconds,
            MaxSeconds = maxSeconds,
            Confidence = confidence,
            Description = description
        };

        _telemetry.LogTimeEstimate(providerName, operationType, estimate);

        return estimate;
    }

    /// <summary>
    /// Get timeout for a specific operation type
    /// </summary>
    public int GetTimeoutSeconds(string operationType)
    {
        return operationType switch
        {
            "ScriptGeneration" => _timeoutPolicy.ScriptGenerationTimeoutSeconds,
            "ScriptRefinement" => _timeoutPolicy.ScriptRefinementTimeoutSeconds,
            "VisualPrompt" => _timeoutPolicy.VisualPromptTimeoutSeconds,
            "NarrationOptimization" => _timeoutPolicy.NarrationOptimizationTimeoutSeconds,
            "PacingAnalysis" => _timeoutPolicy.PacingAnalysisTimeoutSeconds,
            "SceneImportance" => _timeoutPolicy.SceneImportanceTimeoutSeconds,
            "ContentComplexity" => _timeoutPolicy.ContentComplexityTimeoutSeconds,
            "NarrativeArc" => _timeoutPolicy.NarrativeArcTimeoutSeconds,
            _ => 120 // Default 2 minutes
        };
    }

    /// <summary>
    /// Check if operation has exceeded warning threshold
    /// </summary>
    public bool ShouldWarnTimeout(string operationType, int elapsedSeconds)
    {
        var timeout = GetTimeoutSeconds(operationType);
        var warningThreshold = timeout * _timeoutPolicy.WarningThresholdPercentage;
        return elapsedSeconds >= warningThreshold;
    }

    /// <summary>
    /// Get historical performance summary for a provider and operation type
    /// </summary>
    public PerformanceSummary GetPerformanceSummary(string providerName, string operationType)
    {
        var key = GetMetricsKey(providerName, operationType);
        
        if (!_historicalData.TryGetValue(key, out var history) || history.Count == 0)
        {
            return new PerformanceSummary
            {
                ProviderName = providerName,
                OperationType = operationType,
                DataPointCount = 0,
                AverageResponseTimeMs = 0,
                SuccessRate = 0,
                AverageRetryCount = 0
            };
        }

        List<LatencyMetrics> snapshot;
        lock (history)
        {
            snapshot = history.ToList();
        }

        var successfulOps = snapshot.Where(m => m.Success).ToList();
        
        return new PerformanceSummary
        {
            ProviderName = providerName,
            OperationType = operationType,
            DataPointCount = snapshot.Count,
            AverageResponseTimeMs = snapshot.Average(m => m.ResponseTimeMs),
            SuccessRate = snapshot.Count > 0 ? (double)successfulOps.Count / snapshot.Count : 0,
            AverageRetryCount = snapshot.Average(m => m.RetryCount)
        };
    }

    private static string GetMetricsKey(string providerName, string operationType)
    {
        return $"{providerName}:{operationType}";
    }

    private TimeEstimate GetDefaultEstimate(string operationType)
    {
        var timeoutSeconds = GetTimeoutSeconds(operationType);
        var estimatedSeconds = timeoutSeconds / 2;

        return new TimeEstimate
        {
            EstimatedSeconds = estimatedSeconds,
            MinSeconds = estimatedSeconds / 2,
            MaxSeconds = timeoutSeconds,
            Confidence = 0.3,
            Description = $"estimated {estimatedSeconds}-{timeoutSeconds} seconds (limited historical data)"
        };
    }

    private static double CalculateConfidence(int dataPoints, double stdDev, double avgMs)
    {
        if (dataPoints < MinDataPointsForAccuratePrediction)
        {
            return 0.3 + (dataPoints / (double)MinDataPointsForAccuratePrediction * 0.3);
        }

        var coefficientOfVariation = avgMs > 0 ? stdDev / avgMs : 1.0;
        var consistencyScore = Math.Max(0, 1.0 - coefficientOfVariation);
        
        var dataScore = Math.Min(1.0, dataPoints / 50.0);
        
        return 0.6 + (consistencyScore * 0.2) + (dataScore * 0.2);
    }

    private static string GenerateEstimateDescription(int minSeconds, int maxSeconds, double confidence)
    {
        if (confidence >= 0.7)
        {
            return $"typically takes {minSeconds}-{maxSeconds} seconds";
        }
        else if (confidence >= 0.5)
        {
            return $"usually takes {minSeconds}-{maxSeconds} seconds";
        }
        else
        {
            return $"estimated {minSeconds}-{maxSeconds} seconds";
        }
    }
}

/// <summary>
/// Summary of historical performance for a provider and operation type
/// </summary>
public record PerformanceSummary
{
    public string ProviderName { get; init; } = string.Empty;
    public string OperationType { get; init; } = string.Empty;
    public int DataPointCount { get; init; }
    public double AverageResponseTimeMs { get; init; }
    public double SuccessRate { get; init; }
    public double AverageRetryCount { get; init; }
}
