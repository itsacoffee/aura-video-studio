using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Telemetry.Costing;

/// <summary>
/// Service for generating post-run cost breakdowns from RunTelemetry v1 data
/// Provides comprehensive analysis of costs by stage, provider, and operation type
/// </summary>
public class TelemetryCostAnalyzer
{
    private readonly ILogger<TelemetryCostAnalyzer> _logger;
    private readonly CostEstimatorService _estimator;
    
    public TelemetryCostAnalyzer(
        ILogger<TelemetryCostAnalyzer> logger,
        CostEstimatorService estimator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
    }
    
    /// <summary>
    /// Analyze costs from a complete telemetry collection
    /// </summary>
    public CostBreakdown AnalyzeCosts(RunTelemetryCollection telemetry)
    {
        var breakdown = new CostBreakdown
        {
            JobId = telemetry.JobId,
            CorrelationId = telemetry.CorrelationId,
            StartedAt = telemetry.CollectionStartedAt,
            CompletedAt = telemetry.CollectionEndedAt,
            TotalCost = 0m,
            Currency = "USD",
            ByStage = new Dictionary<string, StageCostDetail>(),
            ByProvider = new Dictionary<string, decimal>(),
            OperationDetails = new List<OperationCostDetail>(),
            CacheSavings = 0m,
            RetryOverhead = 0m
        };
        
        var currencyFromRecords = telemetry.Records.FirstOrDefault()?.Currency ?? "USD";
        breakdown.Currency = currencyFromRecords;
        
        // Process each record
        foreach (var record in telemetry.Records)
        {
            ProcessRecord(record, breakdown);
        }
        
        // Calculate summary from telemetry summary if available
        if (telemetry.Summary != null)
        {
            breakdown.TotalOperations = telemetry.Summary.TotalOperations;
            breakdown.SuccessfulOperations = telemetry.Summary.SuccessfulOperations;
            breakdown.FailedOperations = telemetry.Summary.FailedOperations;
            breakdown.CacheHitCount = telemetry.Summary.CacheHits;
            breakdown.TotalRetries = telemetry.Summary.TotalRetries;
            breakdown.TotalTokensIn = telemetry.Summary.TotalTokensIn;
            breakdown.TotalTokensOut = telemetry.Summary.TotalTokensOut;
        }
        
        // Validate total matches summary if available
        if (telemetry.Summary?.TotalCost > 0)
        {
            var difference = Math.Abs(breakdown.TotalCost - telemetry.Summary.TotalCost);
            var percentDiff = (difference / telemetry.Summary.TotalCost) * 100m;
            
            if (percentDiff > 5m)
            {
                _logger.LogWarning(
                    "Cost breakdown total ({Calculated}) differs from telemetry summary ({Summary}) by {Percent:F2}%",
                    breakdown.TotalCost, telemetry.Summary.TotalCost, percentDiff);
            }
        }
        
        return breakdown;
    }
    
    /// <summary>
    /// Process a single telemetry record and update breakdown
    /// </summary>
    private void ProcessRecord(RunTelemetryRecord record, CostBreakdown breakdown)
    {
        var estimate = _estimator.EstimateCost(record);
        
        // Update total
        breakdown.TotalCost += estimate.Amount;
        
        // Track by stage
        var stageName = record.Stage.ToString();
        if (!breakdown.ByStage.TryGetValue(stageName, out var stageDetail))
        {
            stageDetail = new StageCostDetail
            {
                StageName = stageName,
                Cost = 0m,
                OperationCount = 0,
                AverageLatencyMs = 0
            };
            breakdown.ByStage[stageName] = stageDetail;
        }

        stageDetail.Cost += estimate.Amount;
        stageDetail.OperationCount++;
        stageDetail.AverageLatencyMs = ((stageDetail.AverageLatencyMs * (stageDetail.OperationCount - 1)) + record.LatencyMs) / stageDetail.OperationCount;
        
        // Track by provider
        if (!string.IsNullOrEmpty(record.Provider))
        {
            if (!breakdown.ByProvider.ContainsKey(record.Provider))
            {
                breakdown.ByProvider[record.Provider] = 0m;
            }
            breakdown.ByProvider[record.Provider] += estimate.Amount;
        }
        
        // Track operation detail
        breakdown.OperationDetails.Add(new OperationCostDetail
        {
            Timestamp = record.StartedAt,
            Stage = stageName,
            Provider = record.Provider ?? "Unknown",
            ModelId = record.ModelId,
            Cost = estimate.Amount,
            Currency = estimate.Currency,
            PricingVersion = estimate.PricingVersion,
            LatencyMs = record.LatencyMs,
            TokensIn = record.TokensIn,
            TokensOut = record.TokensOut,
            CacheHit = record.CacheHit ?? false,
            Retries = record.Retries,
            ResultStatus = record.ResultStatus.ToString(),
            SceneIndex = record.SceneIndex
        });
        
        // Track cache savings
        if (record.CacheHit == true)
        {
            // Estimate what it would have cost without cache
            var withoutCacheCost = EstimateCostWithoutCache(record);
            var savings = withoutCacheCost - estimate.Amount;
            breakdown.CacheSavings += savings;
        }
        
        // Track retry overhead
        if (record.Retries > 0)
        {
            // Retry overhead is the cost of failed attempts
            // We estimate this as (retries * average_cost_per_attempt)
            var avgCostPerAttempt = estimate.Amount / (record.Retries + 1);
            breakdown.RetryOverhead += avgCostPerAttempt * record.Retries;
        }
    }
    
    /// <summary>
    /// Estimate what a record would have cost without cache
    /// </summary>
    private decimal EstimateCostWithoutCache(RunTelemetryRecord record)
    {
        if (string.IsNullOrEmpty(record.Provider))
            return 0m;
        
        var pricing = _estimator.GetPricingTable().GetVersionFor(record.Provider, record.StartedAt);
        
        if (pricing == null || pricing.IsFree)
            return 0m;
        
        decimal cost = 0m;
        
        if (record.TokensIn.HasValue && record.TokensOut.HasValue)
        {
            if (pricing.CostPer1KInputTokens.HasValue && pricing.CostPer1KOutputTokens.HasValue)
            {
                cost = (record.TokensIn.Value / 1000m) * pricing.CostPer1KInputTokens.Value;
                cost += (record.TokensOut.Value / 1000m) * pricing.CostPer1KOutputTokens.Value;
            }
        }
        
        return cost;
    }
}

/// <summary>
/// Comprehensive cost breakdown for a run
/// </summary>
public class CostBreakdown
{
    /// <summary>
    /// Job identifier
    /// </summary>
    public required string JobId { get; init; }
    
    /// <summary>
    /// Correlation identifier
    /// </summary>
    public required string CorrelationId { get; init; }
    
    /// <summary>
    /// When the run started
    /// </summary>
    public required DateTime StartedAt { get; init; }
    
    /// <summary>
    /// When the run completed
    /// </summary>
    public DateTime? CompletedAt { get; init; }
    
    /// <summary>
    /// Total cost for the entire run
    /// </summary>
    public required decimal TotalCost { get; set; }
    
    /// <summary>
    /// Currency code
    /// </summary>
    public required string Currency { get; set; }
    
    /// <summary>
    /// Cost breakdown by stage
    /// </summary>
    public required Dictionary<string, StageCostDetail> ByStage { get; init; }
    
    /// <summary>
    /// Cost breakdown by provider
    /// </summary>
    public required Dictionary<string, decimal> ByProvider { get; init; }
    
    /// <summary>
    /// Detailed list of all operations
    /// </summary>
    public required List<OperationCostDetail> OperationDetails { get; init; }
    
    /// <summary>
    /// Total operations executed
    /// </summary>
    public int TotalOperations { get; set; }
    
    /// <summary>
    /// Successful operations
    /// </summary>
    public int SuccessfulOperations { get; set; }
    
    /// <summary>
    /// Failed operations
    /// </summary>
    public int FailedOperations { get; set; }
    
    /// <summary>
    /// Number of cache hits
    /// </summary>
    public int CacheHitCount { get; set; }
    
    /// <summary>
    /// Total retries across all operations
    /// </summary>
    public int TotalRetries { get; set; }
    
    /// <summary>
    /// Total input tokens used
    /// </summary>
    public int TotalTokensIn { get; set; }
    
    /// <summary>
    /// Total output tokens generated
    /// </summary>
    public int TotalTokensOut { get; set; }
    
    /// <summary>
    /// Amount saved by cache hits
    /// </summary>
    public required decimal CacheSavings { get; set; }
    
    /// <summary>
    /// Cost overhead from retries
    /// </summary>
    public required decimal RetryOverhead { get; set; }
}

/// <summary>
/// Cost detail for a single stage
/// </summary>
public class StageCostDetail
{
    /// <summary>
    /// Stage name
    /// </summary>
    public required string StageName { get; init; }
    
    /// <summary>
    /// Total cost for this stage
    /// </summary>
    public required decimal Cost { get; set; }
    
    /// <summary>
    /// Number of operations in this stage
    /// </summary>
    public required int OperationCount { get; set; }
    
    /// <summary>
    /// Average latency for operations in this stage
    /// </summary>
    public required long AverageLatencyMs { get; set; }
}

/// <summary>
/// Cost detail for a single operation
/// </summary>
public class OperationCostDetail
{
    public required DateTime Timestamp { get; init; }
    public required string Stage { get; init; }
    public required string Provider { get; init; }
    public string? ModelId { get; init; }
    public required decimal Cost { get; init; }
    public required string Currency { get; init; }
    public string? PricingVersion { get; init; }
    public required long LatencyMs { get; init; }
    public int? TokensIn { get; init; }
    public int? TokensOut { get; init; }
    public required bool CacheHit { get; init; }
    public required int Retries { get; init; }
    public required string ResultStatus { get; init; }
    public int? SceneIndex { get; init; }
}
