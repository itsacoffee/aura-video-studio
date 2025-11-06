using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models.CostTracking;
using Aura.Core.Telemetry;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Telemetry.Costing;

/// <summary>
/// Enhanced budget checker that properly handles partial scenes, retries, and cache hits
/// Provides clear soft vs hard threshold behavior
/// </summary>
public class BudgetChecker
{
    private readonly ILogger<BudgetChecker> _logger;
    private readonly CostEstimatorService _estimator;
    
    public BudgetChecker(
        ILogger<BudgetChecker> logger,
        CostEstimatorService estimator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
    }
    
    /// <summary>
    /// Check if an operation would exceed budget thresholds
    /// </summary>
    /// <param name="configuration">Budget configuration</param>
    /// <param name="currentSpending">Current period spending</param>
    /// <param name="estimatedCost">Estimated cost of proposed operation</param>
    /// <param name="providerName">Provider for the operation</param>
    /// <returns>Result indicating budget status and recommended actions</returns>
    public EnhancedBudgetCheckResult CheckBudget(
        CostTrackingConfiguration configuration,
        decimal currentSpending,
        decimal estimatedCost,
        string providerName)
    {
        var result = new EnhancedBudgetCheckResult
        {
            IsWithinBudget = true,
            ShouldBlock = false,
            ShouldWarn = false,
            ThresholdType = BudgetThresholdType.None,
            Warnings = new List<string>(),
            CurrentSpending = currentSpending,
            EstimatedNewTotal = currentSpending + estimatedCost,
            EstimatedCost = estimatedCost,
            BudgetRemaining = 0m
        };
        
        if (!configuration.OverallMonthlyBudget.HasValue)
        {
            // No budget limit set, operation allowed
            return result;
        }
        
        var budget = configuration.OverallMonthlyBudget.Value;
        var newTotal = currentSpending + estimatedCost;
        var percentUsed = (newTotal / budget) * 100m;
        
        result.BudgetRemaining = Math.Max(0m, budget - newTotal);
        
        // Check hard budget limit
        if (configuration.HardBudgetLimit && newTotal > budget)
        {
            result.IsWithinBudget = false;
            result.ShouldBlock = true;
            result.ThresholdType = BudgetThresholdType.Hard;
            result.Warnings.Add($"BLOCKED: Operation would exceed hard budget limit of {CurrencyFormatter.Format(budget, configuration.Currency, 2)}");
            result.Warnings.Add($"Current spending: {CurrencyFormatter.Format(currentSpending, configuration.Currency, 2)}");
            result.Warnings.Add($"Estimated cost: {CurrencyFormatter.Format(estimatedCost, configuration.Currency, 4)}");
            result.Warnings.Add($"Would result in: {CurrencyFormatter.Format(newTotal, configuration.Currency, 2)} ({percentUsed:F1}% of budget)");
            
            _logger.LogWarning(
                "Budget hard limit check FAILED for {Provider}: {NewTotal} > {Budget} ({Percent:F1}%)",
                providerName, newTotal, budget, percentUsed);
                
            return result;
        }
        
        // Check soft thresholds (warnings)
        var triggeredThreshold = configuration.AlertThresholds
            .OrderByDescending(t => t)
            .FirstOrDefault(threshold => percentUsed >= threshold);
        
        if (triggeredThreshold > 0)
        {
            result.ShouldWarn = true;
            result.ThresholdType = BudgetThresholdType.Soft;
            
            if (percentUsed >= 90m)
            {
                result.Warnings.Add($"WARNING: Approaching budget limit ({percentUsed:F1}% of {CurrencyFormatter.Format(budget, configuration.Currency, 2)})");
            }
            else
            {
                result.Warnings.Add($"INFO: Budget threshold {triggeredThreshold}% reached ({percentUsed:F1}% of {CurrencyFormatter.Format(budget, configuration.Currency, 2)})");
            }
            
            result.Warnings.Add($"Budget remaining: {CurrencyFormatter.Format(result.BudgetRemaining, configuration.Currency, 2)}");
            
            _logger.LogInformation(
                "Budget soft threshold {Threshold}% triggered for {Provider}: {Percent:F1}%",
                triggeredThreshold, providerName, percentUsed);
        }
        
        // Check if operation would exceed budget (but not blocking if soft limit)
        if (!configuration.HardBudgetLimit && newTotal > budget)
        {
            result.IsWithinBudget = false;
            result.ShouldWarn = true;
            result.ThresholdType = BudgetThresholdType.Soft;
            result.Warnings.Add($"NOTICE: Operation would exceed soft budget limit of {CurrencyFormatter.Format(budget, configuration.Currency, 2)}");
            result.Warnings.Add($"Overage: {CurrencyFormatter.Format(newTotal - budget, configuration.Currency, 2)}");
            result.Warnings.Add("Operation will be allowed (soft limit), but consider adjusting budget or reducing costs");
        }
        
        // Check provider-specific budgets
        if (configuration.ProviderBudgets.TryGetValue(providerName, out var providerBudget))
        {
            var providerResult = CheckProviderBudget(
                providerName,
                providerBudget,
                currentSpending,
                estimatedCost,
                configuration);
                
            if (providerResult.ShouldBlock)
            {
                result.IsWithinBudget = false;
                result.ShouldBlock = true;
                result.ThresholdType = BudgetThresholdType.Hard;
            }
            else if (providerResult.ShouldWarn)
            {
                result.ShouldWarn = true;
                if (result.ThresholdType == BudgetThresholdType.None)
                {
                    result.ThresholdType = BudgetThresholdType.Soft;
                }
            }
            
            result.Warnings.AddRange(providerResult.Warnings);
        }
        
        return result;
    }
    
    /// <summary>
    /// Check provider-specific budget
    /// </summary>
    private EnhancedBudgetCheckResult CheckProviderBudget(
        string providerName,
        decimal providerBudget,
        decimal currentProviderSpending,
        decimal estimatedCost,
        CostTrackingConfiguration configuration)
    {
        var result = new EnhancedBudgetCheckResult
        {
            IsWithinBudget = true,
            ShouldBlock = false,
            ShouldWarn = false,
            ThresholdType = BudgetThresholdType.None,
            Warnings = new List<string>(),
            CurrentSpending = currentProviderSpending,
            EstimatedNewTotal = currentProviderSpending + estimatedCost,
            EstimatedCost = estimatedCost,
            BudgetRemaining = Math.Max(0m, providerBudget - (currentProviderSpending + estimatedCost))
        };
        
        var newTotal = currentProviderSpending + estimatedCost;
        var percentUsed = (newTotal / providerBudget) * 100m;
        
        if (configuration.HardBudgetLimit && newTotal > providerBudget)
        {
            result.IsWithinBudget = false;
            result.ShouldBlock = true;
            result.ThresholdType = BudgetThresholdType.Hard;
            result.Warnings.Add($"BLOCKED: {providerName} hard budget limit of {CurrencyFormatter.Format(providerBudget, configuration.Currency, 2)} would be exceeded");
        }
        else if (percentUsed >= 90m)
        {
            result.ShouldWarn = true;
            result.ThresholdType = BudgetThresholdType.Soft;
            result.Warnings.Add($"WARNING: {providerName} budget at {percentUsed:F1}% ({CurrencyFormatter.Format(newTotal, configuration.Currency, 2)} / {CurrencyFormatter.Format(providerBudget, configuration.Currency, 2)})");
        }
        
        return result;
    }
    
    /// <summary>
    /// Calculate accumulated cost for a set of telemetry records
    /// Properly handles cache hits, retries, and partial scenes
    /// </summary>
    public decimal CalculateAccumulatedCost(IEnumerable<RunTelemetryRecord> records)
    {
        decimal total = 0m;
        var seenOperations = new HashSet<string>();
        
        foreach (var record in records)
        {
            // Create unique key for this operation (avoid double-counting retries)
            var operationKey = $"{record.JobId}_{record.Stage}_{record.SceneIndex ?? -1}";
            
            // For retries, only count the final successful attempt
            if (record.Retries > 0 && record.ResultStatus != ResultStatus.Ok)
            {
                // This is a failed retry, skip it
                continue;
            }
            
            if (seenOperations.Contains(operationKey) && record.Retries > 0)
            {
                // Already counted this operation, skip retry
                continue;
            }
            
            var estimate = _estimator.EstimateCost(record);
            total += estimate.Amount;
            
            seenOperations.Add(operationKey);
        }
        
        return total;
    }
}

/// <summary>
/// Enhanced result of budget check with clear threshold information
/// </summary>
public class EnhancedBudgetCheckResult
{
    /// <summary>
    /// Whether the operation is within budget
    /// </summary>
    public required bool IsWithinBudget { get; set; }
    
    /// <summary>
    /// Whether the operation should be blocked (hard limit exceeded)
    /// </summary>
    public required bool ShouldBlock { get; set; }
    
    /// <summary>
    /// Whether a warning should be shown (soft threshold reached)
    /// </summary>
    public required bool ShouldWarn { get; set; }
    
    /// <summary>
    /// Type of threshold that was triggered
    /// </summary>
    public required BudgetThresholdType ThresholdType { get; set; }
    
    /// <summary>
    /// Warning and information messages
    /// </summary>
    public required List<string> Warnings { get; set; }
    
    /// <summary>
    /// Current spending amount
    /// </summary>
    public required decimal CurrentSpending { get; set; }
    
    /// <summary>
    /// Estimated cost of the operation
    /// </summary>
    public required decimal EstimatedCost { get; set; }
    
    /// <summary>
    /// Estimated new total if operation proceeds
    /// </summary>
    public required decimal EstimatedNewTotal { get; set; }
    
    /// <summary>
    /// Budget remaining after operation
    /// </summary>
    public required decimal BudgetRemaining { get; set; }
}

/// <summary>
/// Type of budget threshold
/// </summary>
public enum BudgetThresholdType
{
    /// <summary>
    /// No threshold reached
    /// </summary>
    None,
    
    /// <summary>
    /// Soft threshold (warning only, operation allowed)
    /// </summary>
    Soft,
    
    /// <summary>
    /// Hard threshold (operation blocked)
    /// </summary>
    Hard
}
