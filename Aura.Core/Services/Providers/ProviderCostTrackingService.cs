using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aura.Core.Configuration;
using Aura.Core.Models.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Tracks cost of LLM provider usage per operation and per month.
/// Monitors budget limits and provides warnings when approaching limits.
/// </summary>
public class ProviderCostTrackingService
{
    private readonly ILogger<ProviderCostTrackingService> _logger;
    private readonly ProviderSettings _settings;
    private readonly string _costTrackingPath;
    private readonly object _lock = new();
    private Dictionary<string, ProviderCostTracking> _monthlyTracking = new();

    public ProviderCostTrackingService(
        ILogger<ProviderCostTrackingService> logger,
        ProviderSettings settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
        var dataDir = _settings.GetAuraDataDirectory();
        _costTrackingPath = Path.Combine(dataDir, "cost-tracking.json");
        
        LoadCostTracking();
    }

    /// <summary>
    /// Record cost for a completed LLM operation
    /// </summary>
    public void RecordCost(
        string providerName,
        LlmOperationType operationType,
        decimal costUsd,
        int totalTokens)
    {
        lock (_lock)
        {
            var month = GetCurrentMonth();
            var key = $"{providerName}_{month}";

            if (!_monthlyTracking.TryGetValue(key, out var tracking))
            {
                tracking = new ProviderCostTracking
                {
                    ProviderName = providerName,
                    Month = month,
                    TotalCostUsd = 0,
                    CostByOperation = new Dictionary<LlmOperationType, decimal>()
                };
                _monthlyTracking[key] = tracking;
            }

            tracking = tracking with
            {
                TotalCostUsd = tracking.TotalCostUsd + costUsd,
                OperationCount = tracking.OperationCount + 1,
                LastUpdated = DateTime.UtcNow
            };

            if (!tracking.CostByOperation.ContainsKey(operationType))
            {
                tracking.CostByOperation[operationType] = 0;
            }
            tracking.CostByOperation[operationType] += costUsd;

            _monthlyTracking[key] = tracking;

            _logger.LogInformation(
                "Recorded cost for {ProviderName}/{OperationType}: ${Cost:F4} ({Tokens} tokens). Monthly total: ${MonthlyTotal:F2}",
                providerName, operationType, costUsd, totalTokens, tracking.TotalCostUsd);

            SaveCostTracking();
        }
    }

    /// <summary>
    /// Get cost tracking for current month for a specific provider
    /// </summary>
    public ProviderCostTracking? GetMonthlyTracking(string providerName, string? month = null)
    {
        lock (_lock)
        {
            var targetMonth = month ?? GetCurrentMonth();
            var key = $"{providerName}_{targetMonth}";
            return _monthlyTracking.TryGetValue(key, out var tracking) ? tracking : null;
        }
    }

    /// <summary>
    /// Get total cost for current month across all providers
    /// </summary>
    public decimal GetTotalMonthlyCost(string? month = null)
    {
        lock (_lock)
        {
            var targetMonth = month ?? GetCurrentMonth();
            return _monthlyTracking.Values
                .Where(t => t.Month == targetMonth)
                .Sum(t => t.TotalCostUsd);
        }
    }

    /// <summary>
    /// Get cost breakdown by operation type for current month
    /// </summary>
    public Dictionary<LlmOperationType, decimal> GetCostByOperation(string? month = null)
    {
        lock (_lock)
        {
            var targetMonth = month ?? GetCurrentMonth();
            var breakdown = new Dictionary<LlmOperationType, decimal>();

            foreach (var tracking in _monthlyTracking.Values.Where(t => t.Month == targetMonth))
            {
                foreach (var (operationType, cost) in tracking.CostByOperation)
                {
                    if (!breakdown.ContainsKey(operationType))
                    {
                        breakdown[operationType] = 0;
                    }
                    breakdown[operationType] += cost;
                }
            }

            return breakdown;
        }
    }

    /// <summary>
    /// Get cost breakdown by provider for current month
    /// </summary>
    public Dictionary<string, decimal> GetCostByProvider(string? month = null)
    {
        lock (_lock)
        {
            var targetMonth = month ?? GetCurrentMonth();
            return _monthlyTracking.Values
                .Where(t => t.Month == targetMonth)
                .GroupBy(t => t.ProviderName)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.TotalCostUsd));
        }
    }

    /// <summary>
    /// Check if adding this cost would exceed budget limits
    /// </summary>
    public BudgetCheckResult CheckBudget(
        string providerName,
        decimal estimatedCost,
        ProviderPreferences preferences)
    {
        lock (_lock)
        {
            var currentTotal = GetTotalMonthlyCost();
            var providerTracking = GetMonthlyTracking(providerName);
            var currentProviderCost = providerTracking?.TotalCostUsd ?? 0;

            var warnings = new List<string>();
            var wouldExceed = false;

            if (preferences.MonthlyBudgetLimit.HasValue)
            {
                var newTotal = currentTotal + estimatedCost;
                var limit = preferences.MonthlyBudgetLimit.Value;

                if (newTotal > limit)
                {
                    wouldExceed = true;
                    warnings.Add($"Would exceed monthly budget limit of ${limit:F2} (current: ${currentTotal:F2}, new total: ${newTotal:F2})");
                }
                else if (newTotal > limit * 0.9m)
                {
                    warnings.Add($"Approaching monthly budget limit: ${newTotal:F2} / ${limit:F2} ({(newTotal / limit * 100):F0}%)");
                }
            }

            if (preferences.PerProviderBudgetLimits.TryGetValue(providerName, out var providerLimit))
            {
                var newProviderTotal = currentProviderCost + estimatedCost;

                if (newProviderTotal > providerLimit)
                {
                    wouldExceed = true;
                    warnings.Add($"Would exceed {providerName} budget limit of ${providerLimit:F2} (current: ${currentProviderCost:F2}, new total: ${newProviderTotal:F2})");
                }
                else if (newProviderTotal > providerLimit * 0.9m)
                {
                    warnings.Add($"Approaching {providerName} budget limit: ${newProviderTotal:F2} / ${providerLimit:F2} ({(newProviderTotal / providerLimit * 100):F0}%)");
                }
            }

            var shouldBlock = wouldExceed && preferences.HardBudgetLimit;

            return new BudgetCheckResult
            {
                IsWithinBudget = !wouldExceed,
                ShouldBlock = shouldBlock,
                Warnings = warnings,
                CurrentMonthlyCost = currentTotal,
                EstimatedNewTotal = currentTotal + estimatedCost
            };
        }
    }

    /// <summary>
    /// Reset cost tracking (useful for testing)
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _monthlyTracking.Clear();
            SaveCostTracking();
            _logger.LogInformation("Cost tracking reset");
        }
    }

    private void LoadCostTracking()
    {
        try
        {
            if (File.Exists(_costTrackingPath))
            {
                var json = File.ReadAllText(_costTrackingPath);
                var tracking = JsonSerializer.Deserialize<Dictionary<string, ProviderCostTracking>>(json);
                _monthlyTracking = tracking ?? new Dictionary<string, ProviderCostTracking>();
                
                _logger.LogInformation("Loaded cost tracking data from {Path}", _costTrackingPath);
            }
            else
            {
                _monthlyTracking = new Dictionary<string, ProviderCostTracking>();
                _logger.LogInformation("No existing cost tracking data found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cost tracking data, starting fresh");
            _monthlyTracking = new Dictionary<string, ProviderCostTracking>();
        }
    }

    private void SaveCostTracking()
    {
        try
        {
            var json = JsonSerializer.Serialize(_monthlyTracking, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_costTrackingPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cost tracking data");
        }
    }

    private static string GetCurrentMonth()
    {
        return DateTime.UtcNow.ToString("yyyy-MM");
    }
}

/// <summary>
/// Result of a budget check
/// </summary>
public record BudgetCheckResult
{
    /// <summary>
    /// Whether the estimated cost is within budget
    /// </summary>
    public required bool IsWithinBudget { get; init; }

    /// <summary>
    /// Whether generation should be blocked (hard limit exceeded)
    /// </summary>
    public required bool ShouldBlock { get; init; }

    /// <summary>
    /// Warning messages about budget status
    /// </summary>
    public required List<string> Warnings { get; init; }

    /// <summary>
    /// Current total monthly cost
    /// </summary>
    public required decimal CurrentMonthlyCost { get; init; }

    /// <summary>
    /// Estimated new total if this operation proceeds
    /// </summary>
    public required decimal EstimatedNewTotal { get; init; }
}
