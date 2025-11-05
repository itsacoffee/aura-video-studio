using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Telemetry;

namespace Aura.Core.Services.Diagnostics;

/// <summary>
/// Detects anomalies in telemetry data (cost spikes, latency issues, etc.)
/// </summary>
public class TelemetryAnomalyDetector
{
    /// <summary>
    /// Analyze telemetry collection for anomalies
    /// </summary>
    public static TelemetryAnomalies DetectAnomalies(RunTelemetryCollection? telemetry)
    {
        if (telemetry == null || telemetry.Records.Count == 0)
        {
            return new TelemetryAnomalies();
        }

        var anomalies = new TelemetryAnomalies
        {
            CostAnomalies = DetectCostAnomalies(telemetry.Records),
            LatencyAnomalies = DetectLatencyAnomalies(telemetry.Records),
            ProviderIssues = DetectProviderIssues(telemetry.Records),
            RetryPatterns = DetectRetryPatterns(telemetry.Records)
        };

        return anomalies;
    }

    /// <summary>
    /// Detect cost anomalies (unusually high costs per stage)
    /// </summary>
    private static List<CostAnomaly> DetectCostAnomalies(List<RunTelemetryRecord> records)
    {
        var anomalies = new List<CostAnomaly>();

        var costByStage = records
            .Where(r => r.CostEstimate.HasValue && r.CostEstimate.Value > 0)
            .GroupBy(r => r.Stage)
            .Select(g => new
            {
                Stage = g.Key,
                TotalCost = g.Sum(r => r.CostEstimate ?? 0),
                AvgCost = g.Average(r => r.CostEstimate ?? 0),
                MaxCost = g.Max(r => r.CostEstimate ?? 0),
                Count = g.Count()
            })
            .ToList();

        var totalCost = costByStage.Sum(s => s.TotalCost);
        
        foreach (var stage in costByStage)
        {
            // Flag stages that cost more than 50% of total or have very high single operation costs
            if (stage.TotalCost > totalCost * 0.5m || stage.MaxCost > 1.0m)
            {
                anomalies.Add(new CostAnomaly
                {
                    Stage = stage.Stage.ToString(),
                    TotalCost = stage.TotalCost,
                    AvgCostPerOperation = stage.AvgCost,
                    MaxCostSingleOperation = stage.MaxCost,
                    OperationCount = stage.Count,
                    Severity = stage.MaxCost > 2.0m ? AnomalySeverity.High : 
                              stage.TotalCost > totalCost * 0.7m ? AnomalySeverity.High : 
                              AnomalySeverity.Medium,
                    Description = stage.MaxCost > 1.0m 
                        ? $"Single operation in {stage.Stage} stage cost ${stage.MaxCost:F4}, which is unusually high"
                        : $"{stage.Stage} stage accounts for {(stage.TotalCost / totalCost * 100):F1}% of total cost"
                });
            }
        }

        return anomalies;
    }

    /// <summary>
    /// Detect latency anomalies (unusually slow operations)
    /// </summary>
    private static List<LatencyAnomaly> DetectLatencyAnomalies(List<RunTelemetryRecord> records)
    {
        var anomalies = new List<LatencyAnomaly>();

        var latencyByStage = records
            .GroupBy(r => r.Stage)
            .Select(g => new
            {
                Stage = g.Key,
                AvgLatency = g.Average(r => r.LatencyMs),
                MaxLatency = g.Max(r => r.LatencyMs),
                P95Latency = CalculatePercentile(g.Select(r => r.LatencyMs).ToList(), 0.95),
                Count = g.Count()
            })
            .ToList();

        foreach (var stage in latencyByStage)
        {
            // Flag stages with very high max latency or high P95
            if (stage.MaxLatency > 60000) // > 60 seconds
            {
                anomalies.Add(new LatencyAnomaly
                {
                    Stage = stage.Stage.ToString(),
                    AvgLatencyMs = (long)stage.AvgLatency,
                    MaxLatencyMs = stage.MaxLatency,
                    P95LatencyMs = (long)stage.P95Latency,
                    OperationCount = stage.Count,
                    Severity = stage.MaxLatency > 300000 ? AnomalySeverity.High : AnomalySeverity.Medium,
                    Description = $"{stage.Stage} stage had operations taking up to {stage.MaxLatency / 1000:F1} seconds, which is unusually slow"
                });
            }
            else if (stage.P95Latency > 30000) // P95 > 30 seconds
            {
                anomalies.Add(new LatencyAnomaly
                {
                    Stage = stage.Stage.ToString(),
                    AvgLatencyMs = (long)stage.AvgLatency,
                    MaxLatencyMs = stage.MaxLatency,
                    P95LatencyMs = (long)stage.P95Latency,
                    OperationCount = stage.Count,
                    Severity = AnomalySeverity.Medium,
                    Description = $"{stage.Stage} stage P95 latency is {stage.P95Latency / 1000:F1} seconds, indicating slow performance"
                });
            }
        }

        return anomalies;
    }

    /// <summary>
    /// Detect provider-specific issues
    /// </summary>
    private static List<ProviderIssue> DetectProviderIssues(List<RunTelemetryRecord> records)
    {
        var issues = new List<ProviderIssue>();

        var providerStats = records
            .Where(r => !string.IsNullOrEmpty(r.Provider))
            .GroupBy(r => r.Provider!)
            .Select(g => new
            {
                Provider = g.Key,
                ErrorCount = g.Count(r => r.ResultStatus == ResultStatus.Error),
                TotalCount = g.Count(),
                AvgLatency = g.Average(r => r.LatencyMs),
                TotalRetries = g.Sum(r => r.Retries),
                ErrorCodes = g.Where(r => !string.IsNullOrEmpty(r.ErrorCode))
                             .Select(r => r.ErrorCode!)
                             .Distinct()
                             .ToList()
            })
            .ToList();

        foreach (var provider in providerStats)
        {
            var errorRate = (double)provider.ErrorCount / provider.TotalCount;

            // High error rate
            if (errorRate > 0.5)
            {
                issues.Add(new ProviderIssue
                {
                    Provider = provider.Provider,
                    IssueType = "HighErrorRate",
                    ErrorCount = provider.ErrorCount,
                    TotalOperations = provider.TotalCount,
                    ErrorRate = errorRate,
                    Severity = errorRate > 0.8 ? AnomalySeverity.High : AnomalySeverity.Medium,
                    Description = $"{provider.Provider} has {errorRate * 100:F1}% error rate ({provider.ErrorCount}/{provider.TotalCount} operations failed)",
                    ErrorCodes = provider.ErrorCodes
                });
            }

            // Many retries
            if (provider.TotalRetries > provider.TotalCount * 2)
            {
                issues.Add(new ProviderIssue
                {
                    Provider = provider.Provider,
                    IssueType = "ExcessiveRetries",
                    TotalRetries = provider.TotalRetries,
                    TotalOperations = provider.TotalCount,
                    Severity = AnomalySeverity.Medium,
                    Description = $"{provider.Provider} required {provider.TotalRetries} retries across {provider.TotalCount} operations, indicating instability"
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Detect retry patterns that might indicate specific issues
    /// </summary>
    private static List<RetryPattern> DetectRetryPatterns(List<RunTelemetryRecord> records)
    {
        var patterns = new List<RetryPattern>();

        var retriedOps = records.Where(r => r.Retries > 0).ToList();
        if (retriedOps.Count == 0)
        {
            return patterns;
        }

        var byStage = retriedOps
            .GroupBy(r => r.Stage)
            .Select(g => new
            {
                Stage = g.Key,
                TotalRetries = g.Sum(r => r.Retries),
                AvgRetries = g.Average(r => r.Retries),
                Count = g.Count()
            })
            .OrderByDescending(s => s.TotalRetries)
            .ToList();

        foreach (var stage in byStage.Take(3)) // Top 3 stages with retries
        {
            patterns.Add(new RetryPattern
            {
                Stage = stage.Stage.ToString(),
                TotalRetries = stage.TotalRetries,
                AvgRetriesPerOperation = stage.AvgRetries,
                OperationsWithRetries = stage.Count,
                Severity = stage.AvgRetries > 3 ? AnomalySeverity.High : AnomalySeverity.Low,
                Description = $"{stage.Stage} stage required {stage.TotalRetries} total retries (avg {stage.AvgRetries:F1} per operation)"
            });
        }

        return patterns;
    }

    /// <summary>
    /// Calculate percentile of a list of values
    /// </summary>
    private static double CalculatePercentile(List<long> values, double percentile)
    {
        if (values.Count == 0)
            return 0;

        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
        return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
    }
}

/// <summary>
/// Collection of detected anomalies
/// </summary>
public record TelemetryAnomalies
{
    public List<CostAnomaly> CostAnomalies { get; init; } = new();
    public List<LatencyAnomaly> LatencyAnomalies { get; init; } = new();
    public List<ProviderIssue> ProviderIssues { get; init; } = new();
    public List<RetryPattern> RetryPatterns { get; init; } = new();

    public bool HasAnyAnomalies => 
        CostAnomalies.Count > 0 || 
        LatencyAnomalies.Count > 0 || 
        ProviderIssues.Count > 0 || 
        RetryPatterns.Count > 0;
}

/// <summary>
/// Cost anomaly detected
/// </summary>
public record CostAnomaly
{
    public required string Stage { get; init; }
    public decimal TotalCost { get; init; }
    public decimal AvgCostPerOperation { get; init; }
    public decimal MaxCostSingleOperation { get; init; }
    public int OperationCount { get; init; }
    public AnomalySeverity Severity { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Latency anomaly detected
/// </summary>
public record LatencyAnomaly
{
    public required string Stage { get; init; }
    public long AvgLatencyMs { get; init; }
    public long MaxLatencyMs { get; init; }
    public long P95LatencyMs { get; init; }
    public int OperationCount { get; init; }
    public AnomalySeverity Severity { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Provider-specific issue
/// </summary>
public record ProviderIssue
{
    public required string Provider { get; init; }
    public required string IssueType { get; init; }
    public int ErrorCount { get; init; }
    public int TotalOperations { get; init; }
    public double ErrorRate { get; init; }
    public int TotalRetries { get; init; }
    public AnomalySeverity Severity { get; init; }
    public required string Description { get; init; }
    public List<string> ErrorCodes { get; init; } = new();
}

/// <summary>
/// Retry pattern detected
/// </summary>
public record RetryPattern
{
    public required string Stage { get; init; }
    public int TotalRetries { get; init; }
    public double AvgRetriesPerOperation { get; init; }
    public int OperationsWithRetries { get; init; }
    public AnomalySeverity Severity { get; init; }
    public required string Description { get; init; }
}

/// <summary>
/// Severity level for anomalies
/// </summary>
public enum AnomalySeverity
{
    Low,
    Medium,
    High
}
