using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aura.Core.Configuration;
using Aura.Core.Models.CostTracking;
using Aura.Core.Models.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.CostTracking;

/// <summary>
/// Enhanced cost tracking service with configuration, alerts, and detailed reporting
/// </summary>
public class EnhancedCostTrackingService
{
    private readonly ILogger<EnhancedCostTrackingService> _logger;
    private readonly ProviderSettings _settings;
    private readonly string _dataDirectory;
    private readonly string _configPath;
    private readonly string _logsPath;
    private readonly string _pricingPath;
    private readonly object _lock = new();
    
    private CostTrackingConfiguration _configuration;
    private List<CostLog> _costLogs = new();
    private Dictionary<string, Models.CostTracking.ProviderPricing> _providerPricing = new();

    public EnhancedCostTrackingService(
        ILogger<EnhancedCostTrackingService> logger,
        ProviderSettings settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        
        _dataDirectory = Path.Combine(_settings.GetAuraDataDirectory(), "cost-tracking");
        Directory.CreateDirectory(_dataDirectory);
        
        _configPath = Path.Combine(_dataDirectory, "configuration.json");
        _logsPath = Path.Combine(_dataDirectory, "cost-logs.json");
        _pricingPath = Path.Combine(_dataDirectory, "provider-pricing.json");
        
        _configuration = LoadConfiguration();
        LoadCostLogs();
        LoadProviderPricing();
        InitializeDefaultPricing();
    }

    /// <summary>
    /// Get current cost tracking configuration
    /// </summary>
    public CostTrackingConfiguration GetConfiguration()
    {
        lock (_lock)
        {
            return _configuration;
        }
    }

    /// <summary>
    /// Update cost tracking configuration
    /// </summary>
    public void UpdateConfiguration(CostTrackingConfiguration configuration)
    {
        lock (_lock)
        {
            _configuration = configuration with { UpdatedAt = DateTime.UtcNow };
            SaveConfiguration();
            _logger.LogInformation("Cost tracking configuration updated");
        }
    }

    /// <summary>
    /// Log a cost entry
    /// </summary>
    public void LogCost(CostLog costLog)
    {
        lock (_lock)
        {
            _costLogs.Add(costLog);
            SaveCostLogs();
            
            _logger.LogInformation(
                "Logged cost: {Provider}/{Feature} - ${Cost:F4}",
                costLog.ProviderName, costLog.Feature, costLog.Cost);
            
            CheckBudgetAlerts(costLog.ProviderName);
        }
    }

    /// <summary>
    /// Get current spending for the current budget period
    /// </summary>
    public decimal GetCurrentPeriodSpending(string? providerId = null)
    {
        lock (_lock)
        {
            var (start, end) = GetCurrentPeriodDates();
            return GetSpending(start, end, providerId);
        }
    }

    /// <summary>
    /// Get spending for a specific date range
    /// </summary>
    public decimal GetSpending(DateTime startDate, DateTime endDate, string? providerId = null)
    {
        lock (_lock)
        {
            var query = _costLogs.Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate);
            
            if (!string.IsNullOrEmpty(providerId))
            {
                query = query.Where(l => l.ProviderName == providerId);
            }
            
            return query.Sum(l => l.Cost);
        }
    }

    /// <summary>
    /// Get spending breakdown by provider
    /// </summary>
    public Dictionary<string, decimal> GetSpendingByProvider(DateTime startDate, DateTime endDate)
    {
        lock (_lock)
        {
            return _costLogs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .GroupBy(l => l.ProviderName)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Cost));
        }
    }

    /// <summary>
    /// Get spending breakdown by feature
    /// </summary>
    public Dictionary<CostFeatureType, decimal> GetSpendingByFeature(DateTime startDate, DateTime endDate)
    {
        lock (_lock)
        {
            return _costLogs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .GroupBy(l => l.Feature)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Cost));
        }
    }

    /// <summary>
    /// Get spending breakdown by project
    /// </summary>
    public Dictionary<string, decimal> GetSpendingByProject(DateTime startDate, DateTime endDate)
    {
        lock (_lock)
        {
            return _costLogs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate && l.ProjectId != null)
                .GroupBy(l => l.ProjectId!)
                .ToDictionary(g => g.Key, g => g.Sum(l => l.Cost));
        }
    }

    /// <summary>
    /// Check if a planned operation would exceed budget
    /// </summary>
    public BudgetCheckResult CheckBudget(string providerName, decimal estimatedCost)
    {
        lock (_lock)
        {
            var currentTotal = GetCurrentPeriodSpending();
            var currentProviderCost = GetCurrentPeriodSpending(providerName);
            var warnings = new List<string>();
            var wouldExceed = false;

            if (_configuration.OverallMonthlyBudget.HasValue)
            {
                var newTotal = currentTotal + estimatedCost;
                var limit = _configuration.OverallMonthlyBudget.Value;

                if (newTotal > limit)
                {
                    wouldExceed = true;
                    warnings.Add($"Would exceed overall budget of {_configuration.Currency} {limit:F2} (current: {currentTotal:F2}, new: {newTotal:F2})");
                }
                else if (newTotal > limit * 0.9m)
                {
                    warnings.Add($"Approaching overall budget: {newTotal:F2} / {limit:F2} ({newTotal / limit * 100:F0}%)");
                }
            }

            if (_configuration.ProviderBudgets.TryGetValue(providerName, out var providerLimit))
            {
                var newProviderTotal = currentProviderCost + estimatedCost;

                if (newProviderTotal > providerLimit)
                {
                    wouldExceed = true;
                    warnings.Add($"Would exceed {providerName} budget of {_configuration.Currency} {providerLimit:F2} (current: {currentProviderCost:F2}, new: {newProviderTotal:F2})");
                }
                else if (newProviderTotal > providerLimit * 0.9m)
                {
                    warnings.Add($"Approaching {providerName} budget: {newProviderTotal:F2} / {providerLimit:F2} ({newProviderTotal / providerLimit * 100:F0}%)");
                }
            }

            var shouldBlock = wouldExceed && _configuration.HardBudgetLimit;

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
    /// Get provider pricing information
    /// </summary>
    public Models.CostTracking.ProviderPricing? GetProviderPricing(string providerName)
    {
        lock (_lock)
        {
            return _providerPricing.TryGetValue(providerName, out var pricing) ? pricing : null;
        }
    }

    /// <summary>
    /// Update provider pricing
    /// </summary>
    public void UpdateProviderPricing(Models.CostTracking.ProviderPricing pricing)
    {
        lock (_lock)
        {
            _providerPricing[pricing.ProviderName] = pricing with { LastUpdated = DateTime.UtcNow };
            SaveProviderPricing();
            _logger.LogInformation("Updated pricing for provider: {Provider}", pricing.ProviderName);
        }
    }

    /// <summary>
    /// Get all provider pricing
    /// </summary>
    public List<Models.CostTracking.ProviderPricing> GetAllProviderPricing()
    {
        lock (_lock)
        {
            return _providerPricing.Values.ToList();
        }
    }

    /// <summary>
    /// Estimate cost for an LLM operation
    /// </summary>
    public decimal EstimateLlmCost(string providerName, int inputTokens, int outputTokens)
    {
        var pricing = GetProviderPricing(providerName);
        if (pricing == null || pricing.IsFree)
            return 0;

        if (pricing.CostPer1KInputTokens.HasValue && pricing.CostPer1KOutputTokens.HasValue)
        {
            return (inputTokens / 1000m * pricing.CostPer1KInputTokens.Value) +
                   (outputTokens / 1000m * pricing.CostPer1KOutputTokens.Value);
        }
        else if (pricing.CostPer1KTokens.HasValue)
        {
            return (inputTokens + outputTokens) / 1000m * pricing.CostPer1KTokens.Value;
        }

        return 0;
    }

    /// <summary>
    /// Estimate cost for a TTS operation
    /// </summary>
    public decimal EstimateTtsCost(string providerName, int characters)
    {
        var pricing = GetProviderPricing(providerName);
        if (pricing == null || pricing.IsFree)
            return 0;

        if (pricing.CostPer1KCharacters.HasValue)
        {
            return characters / 1000m * pricing.CostPer1KCharacters.Value;
        }
        else if (pricing.CostPerCharacter.HasValue)
        {
            return characters * pricing.CostPerCharacter.Value;
        }

        return 0;
    }

    /// <summary>
    /// Reset budget for current period (for testing or manual reset)
    /// </summary>
    public void ResetPeriodBudget()
    {
        lock (_lock)
        {
            var (start, _) = GetCurrentPeriodDates();
            _costLogs.RemoveAll(l => l.Timestamp >= start);
            SaveCostLogs();
            
            _configuration = _configuration with 
            { 
                TriggeredAlerts = new Dictionary<string, DateTime>() 
            };
            SaveConfiguration();
            
            _logger.LogInformation("Budget reset for current period");
        }
    }

    private void CheckBudgetAlerts(string providerName)
    {
        var currentTotal = GetCurrentPeriodSpending();
        
        if (_configuration.OverallMonthlyBudget.HasValue)
        {
            var budget = _configuration.OverallMonthlyBudget.Value;
            var percentage = (currentTotal / budget) * 100;
            
            foreach (var threshold in _configuration.AlertThresholds.OrderBy(t => t))
            {
                if (percentage >= threshold)
                {
                    TriggerAlert($"overall_{threshold}", 
                        $"Overall budget {threshold}% threshold reached: {_configuration.Currency} {currentTotal:F2} / {budget:F2}");
                }
            }
        }
        
        if (_configuration.ProviderBudgets.TryGetValue(providerName, out var providerBudget))
        {
            var providerSpend = GetCurrentPeriodSpending(providerName);
            var percentage = (providerSpend / providerBudget) * 100;
            
            foreach (var threshold in _configuration.AlertThresholds.OrderBy(t => t))
            {
                if (percentage >= threshold)
                {
                    TriggerAlert($"{providerName}_{threshold}", 
                        $"{providerName} budget {threshold}% threshold reached: {_configuration.Currency} {providerSpend:F2} / {providerBudget:F2}");
                }
            }
        }
    }

    private void TriggerAlert(string alertKey, string message)
    {
        var now = DateTime.UtcNow;
        
        if (_configuration.TriggeredAlerts.TryGetValue(alertKey, out var lastTriggered))
        {
            var shouldTrigger = _configuration.AlertFrequency switch
            {
                AlertFrequency.Once => false,
                AlertFrequency.Daily => (now - lastTriggered).TotalDays >= 1,
                AlertFrequency.EveryTime => true,
                _ => false
            };
            
            if (!shouldTrigger)
                return;
        }
        
        _logger.LogWarning("Budget Alert: {Message}", message);
        
        _configuration.TriggeredAlerts[alertKey] = now;
        SaveConfiguration();
    }

    private (DateTime start, DateTime end) GetCurrentPeriodDates()
    {
        var now = DateTime.UtcNow;
        
        return _configuration.PeriodType switch
        {
            BudgetPeriodType.Monthly => (new DateTime(now.Year, now.Month, 1), 
                                        new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1)),
            BudgetPeriodType.Weekly => GetWeekDates(now),
            BudgetPeriodType.Custom => (_configuration.BudgetPeriodStart ?? now, 
                                       _configuration.BudgetPeriodEnd ?? now),
            _ => (new DateTime(now.Year, now.Month, 1), 
                 new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1))
        };
    }

    private static (DateTime start, DateTime end) GetWeekDates(DateTime date)
    {
        var diff = date.DayOfWeek - DayOfWeek.Sunday;
        if (diff < 0) diff += 7;
        var start = date.AddDays(-diff).Date;
        var end = start.AddDays(6);
        return (start, end);
    }

    private void InitializeDefaultPricing()
    {
        var defaults = new[]
        {
            new ProviderPricing
            {
                ProviderName = "OpenAI",
                ProviderType = ProviderType.LLM,
                CostPer1KInputTokens = 0.03m,
                CostPer1KOutputTokens = 0.06m,
                Notes = "GPT-4 pricing (2024)"
            },
            new ProviderPricing
            {
                ProviderName = "Anthropic",
                ProviderType = ProviderType.LLM,
                CostPer1KInputTokens = 0.015m,
                CostPer1KOutputTokens = 0.075m,
                Notes = "Claude 3 Sonnet pricing (2024)"
            },
            new ProviderPricing
            {
                ProviderName = "Gemini",
                ProviderType = ProviderType.LLM,
                CostPer1KTokens = 0.00025m,
                Notes = "Gemini Pro pricing (2024)"
            },
            new ProviderPricing
            {
                ProviderName = "Ollama",
                ProviderType = ProviderType.LLM,
                IsFree = true,
                Notes = "Local/offline - no API costs"
            },
            new ProviderPricing
            {
                ProviderName = "RuleBased",
                ProviderType = ProviderType.LLM,
                IsFree = true,
                Notes = "Local/offline - no API costs"
            },
            new ProviderPricing
            {
                ProviderName = "ElevenLabs",
                ProviderType = ProviderType.TTS,
                CostPer1KCharacters = 0.30m,
                Notes = "ElevenLabs standard pricing (2024)"
            },
            new ProviderPricing
            {
                ProviderName = "PlayHT",
                ProviderType = ProviderType.TTS,
                CostPer1KCharacters = 0.20m,
                Notes = "PlayHT standard pricing (2024)"
            },
            new ProviderPricing
            {
                ProviderName = "Windows",
                ProviderType = ProviderType.TTS,
                IsFree = true,
                Notes = "Windows SAPI - no API costs"
            },
            new ProviderPricing
            {
                ProviderName = "Piper",
                ProviderType = ProviderType.TTS,
                IsFree = true,
                Notes = "Local/offline - no API costs"
            },
            new ProviderPricing
            {
                ProviderName = "Mimic3",
                ProviderType = ProviderType.TTS,
                IsFree = true,
                Notes = "Local/offline - no API costs"
            }
        };

        foreach (var pricing in defaults)
        {
            if (!_providerPricing.ContainsKey(pricing.ProviderName))
            {
                _providerPricing[pricing.ProviderName] = pricing;
            }
        }
        
        SaveProviderPricing();
    }

    private CostTrackingConfiguration LoadConfiguration()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<CostTrackingConfiguration>(json) 
                       ?? new CostTrackingConfiguration();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cost tracking configuration");
        }
        
        return new CostTrackingConfiguration();
    }

    private void SaveConfiguration()
    {
        try
        {
            var json = JsonSerializer.Serialize(_configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cost tracking configuration");
        }
    }

    private void LoadCostLogs()
    {
        try
        {
            if (File.Exists(_logsPath))
            {
                var json = File.ReadAllText(_logsPath);
                _costLogs = JsonSerializer.Deserialize<List<CostLog>>(json) ?? new List<CostLog>();
                
                const int retentionMonths = 3;
                var (start, _) = GetCurrentPeriodDates();
                _costLogs = _costLogs.Where(l => l.Timestamp >= start.AddMonths(-retentionMonths)).ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cost logs");
        }
    }

    private void SaveCostLogs()
    {
        try
        {
            var json = JsonSerializer.Serialize(_costLogs, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_logsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save cost logs");
        }
    }

    private void LoadProviderPricing()
    {
        try
        {
            if (File.Exists(_pricingPath))
            {
                var json = File.ReadAllText(_pricingPath);
                var pricingList = JsonSerializer.Deserialize<List<ProviderPricing>>(json);
                if (pricingList != null)
                {
                    _providerPricing = pricingList.ToDictionary(p => p.ProviderName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load provider pricing");
        }
    }

    private void SaveProviderPricing()
    {
        try
        {
            var json = JsonSerializer.Serialize(_providerPricing.Values.ToList(), new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_pricingPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save provider pricing");
        }
    }
}
