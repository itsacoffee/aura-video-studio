using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Aura.Core.Configuration;
using Aura.Core.Models.CostTracking;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.CostTracking;

/// <summary>
/// Service for tracking detailed token usage across LLM operations
/// </summary>
public class TokenTrackingService
{
    private readonly ILogger<TokenTrackingService> _logger;
    private readonly ProviderSettings _settings;
    private readonly EnhancedCostTrackingService _costTrackingService;
    private readonly string _dataDirectory;
    private readonly string _metricsPath;
    private readonly object _lock = new();
    
    private List<TokenUsageMetrics> _tokenMetrics = new();
    private Dictionary<string, List<TokenUsageMetrics>> _runMetrics = new();

    public TokenTrackingService(
        ILogger<TokenTrackingService> logger,
        ProviderSettings settings,
        EnhancedCostTrackingService costTrackingService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _costTrackingService = costTrackingService ?? throw new ArgumentNullException(nameof(costTrackingService));
        
        _dataDirectory = Path.Combine(_settings.GetAuraDataDirectory(), "cost-tracking");
        Directory.CreateDirectory(_dataDirectory);
        
        _metricsPath = Path.Combine(_dataDirectory, "token-metrics.json");
        
        LoadTokenMetrics();
    }

    /// <summary>
    /// Record token usage for an LLM operation
    /// </summary>
    public void RecordTokenUsage(TokenUsageMetrics metrics)
    {
        lock (_lock)
        {
            _tokenMetrics.Add(metrics);
            
            if (!string.IsNullOrEmpty(metrics.JobId))
            {
                if (!_runMetrics.TryGetValue(metrics.JobId, out var value))
                {
                    value = new List<TokenUsageMetrics>();
                    _runMetrics[metrics.JobId] = value;
                }

                value.Add(metrics);
            }
            
            SaveTokenMetrics();
            
            _logger.LogInformation(
                "Recorded token usage: {Provider}/{Model} - {InputTokens} in / {OutputTokens} out (${Cost:F4})",
                metrics.ProviderName, metrics.ModelName, metrics.InputTokens, 
                metrics.OutputTokens, metrics.EstimatedCost);
            
            if (!metrics.Success)
            {
                _logger.LogWarning("Token usage recording for failed operation: {Error}", 
                    metrics.ErrorMessage);
            }
        }
    }

    /// <summary>
    /// Get token usage statistics for a specific job
    /// </summary>
    public TokenUsageStatistics GetJobStatistics(string jobId)
    {
        lock (_lock)
        {
            if (!_runMetrics.TryGetValue(jobId, out var metrics) || metrics.Count == 0)
            {
                return new TokenUsageStatistics
                {
                    TotalInputTokens = 0,
                    TotalOutputTokens = 0,
                    OperationCount = 0,
                    CacheHits = 0,
                    AverageResponseTimeMs = 0,
                    TotalCost = 0,
                    CostSavedByCache = 0
                };
            }

            var successfulOps = metrics.Where(m => m.Success).ToList();
            var cacheHits = successfulOps.Count(m => m.CacheHit);
            var totalCost = successfulOps.Sum(m => m.EstimatedCost);
            
            var cachedOps = successfulOps.Where(m => m.CacheHit).ToList();
            var costSavedByCache = cachedOps.Sum(m => m.EstimatedCost);

            return new TokenUsageStatistics
            {
                TotalInputTokens = successfulOps.Sum(m => (long)m.InputTokens),
                TotalOutputTokens = successfulOps.Sum(m => (long)m.OutputTokens),
                OperationCount = successfulOps.Count,
                CacheHits = cacheHits,
                AverageResponseTimeMs = successfulOps.Count != 0
                    ? (long)successfulOps.Average(m => m.ResponseTimeMs) 
                    : 0,
                TotalCost = totalCost,
                CostSavedByCache = costSavedByCache
            };
        }
    }

    /// <summary>
    /// Get all token metrics for a specific job
    /// </summary>
    public List<TokenUsageMetrics> GetJobMetrics(string jobId)
    {
        lock (_lock)
        {
            return _runMetrics.TryGetValue(jobId, out var metrics) 
                ? metrics.ToList() 
                : new List<TokenUsageMetrics>();
        }
    }

    /// <summary>
    /// Get token usage statistics for a date range
    /// </summary>
    public TokenUsageStatistics GetStatistics(DateTime startDate, DateTime endDate, string? providerId = null)
    {
        lock (_lock)
        {
            var query = _tokenMetrics.Where(m => 
                m.Timestamp >= startDate && 
                m.Timestamp <= endDate && 
                m.Success);
            
            if (!string.IsNullOrEmpty(providerId))
            {
                query = query.Where(m => m.ProviderName == providerId);
            }
            
            var metrics = query.ToList();
            
            if (metrics.Count == 0)
            {
                return new TokenUsageStatistics
                {
                    TotalInputTokens = 0,
                    TotalOutputTokens = 0,
                    OperationCount = 0,
                    CacheHits = 0,
                    AverageResponseTimeMs = 0,
                    TotalCost = 0,
                    CostSavedByCache = 0
                };
            }

            var cacheHits = metrics.Count(m => m.CacheHit);
            var cachedOps = metrics.Where(m => m.CacheHit).ToList();

            return new TokenUsageStatistics
            {
                TotalInputTokens = metrics.Sum(m => (long)m.InputTokens),
                TotalOutputTokens = metrics.Sum(m => (long)m.OutputTokens),
                OperationCount = metrics.Count,
                CacheHits = cacheHits,
                AverageResponseTimeMs = (long)metrics.Average(m => m.ResponseTimeMs),
                TotalCost = metrics.Sum(m => m.EstimatedCost),
                CostSavedByCache = cachedOps.Sum(m => m.EstimatedCost)
            };
        }
    }

    /// <summary>
    /// Get token usage breakdown by operation type
    /// </summary>
    public Dictionary<string, TokenUsageStatistics> GetStatisticsByOperation(
        DateTime startDate, 
        DateTime endDate)
    {
        lock (_lock)
        {
            return _tokenMetrics
                .Where(m => m.Timestamp >= startDate && m.Timestamp <= endDate && m.Success)
                .GroupBy(m => m.OperationType)
                .ToDictionary(
                    g => g.Key,
                    g => new TokenUsageStatistics
                    {
                        TotalInputTokens = g.Sum(m => (long)m.InputTokens),
                        TotalOutputTokens = g.Sum(m => (long)m.OutputTokens),
                        OperationCount = g.Count(),
                        CacheHits = g.Count(m => m.CacheHit),
                        AverageResponseTimeMs = (long)g.Average(m => m.ResponseTimeMs),
                        TotalCost = g.Sum(m => m.EstimatedCost),
                        CostSavedByCache = g.Where(m => m.CacheHit).Sum(m => m.EstimatedCost)
                    });
        }
    }

    /// <summary>
    /// Generate cost optimization suggestions for a job
    /// </summary>
    public List<CostOptimizationSuggestion> GenerateOptimizationSuggestions(string jobId)
    {
        var suggestions = new List<CostOptimizationSuggestion>();
        
        lock (_lock)
        {
            if (!_runMetrics.TryGetValue(jobId, out var metrics) || metrics.Count == 0)
            {
                return suggestions;
            }

            var successfulOps = metrics.Where(m => m.Success).ToList();
            var stats = GetJobStatistics(jobId);
            
            if (stats.CacheHitRate < 20 && stats.OperationCount > 5)
            {
                suggestions.Add(new CostOptimizationSuggestion
                {
                    Category = OptimizationCategory.Caching,
                    Suggestion = "Enable LLM caching to reduce costs. Current cache hit rate is low.",
                    EstimatedSavings = stats.TotalCost * 0.3m,
                    QualityImpact = "No impact - identical results from cache"
                });
            }
            
            var expensiveProviders = successfulOps
                .GroupBy(m => m.ProviderName)
                .Where(g => g.Sum(m => m.EstimatedCost) > stats.TotalCost * 0.5m)
                .Select(g => g.Key)
                .ToList();
            
            if (expensiveProviders.Count != 0)
            {
                suggestions.Add(new CostOptimizationSuggestion
                {
                    Category = OptimizationCategory.ProviderSwitch,
                    Suggestion = $"Consider switching from {string.Join(", ", expensiveProviders)} to lower-cost alternatives like Gemini or local models",
                    EstimatedSavings = stats.TotalCost * 0.4m,
                    QualityImpact = "Slight quality reduction possible with some alternatives"
                });
            }
            
            var avgTokensPerOp = stats.AverageTokensPerOperation;
            if (avgTokensPerOp > 3000)
            {
                suggestions.Add(new CostOptimizationSuggestion
                {
                    Category = OptimizationCategory.PromptOptimization,
                    Suggestion = "Optimize prompts to reduce token usage. Average operation uses over 3000 tokens.",
                    EstimatedSavings = stats.TotalCost * 0.2m,
                    QualityImpact = "Minimal impact with careful prompt engineering"
                });
            }
            
            var highRetryOps = successfulOps.Count(m => m.RetryCount > 0);
            if (highRetryOps > successfulOps.Count * 0.2)
            {
                suggestions.Add(new CostOptimizationSuggestion
                {
                    Category = OptimizationCategory.ModelSelection,
                    Suggestion = $"{highRetryOps} operations required retries. Consider more reliable models or adjusting parameters.",
                    EstimatedSavings = stats.TotalCost * 0.15m,
                    QualityImpact = "Improved reliability"
                });
            }
        }
        
        return suggestions;
    }

    /// <summary>
    /// Clear metrics for a specific job (after report export)
    /// </summary>
    public void ClearJobMetrics(string jobId)
    {
        lock (_lock)
        {
            _runMetrics.Remove(jobId);
            _tokenMetrics.RemoveAll(m => m.JobId == jobId);
            SaveTokenMetrics();
            _logger.LogInformation("Cleared token metrics for job {JobId}", jobId);
        }
    }

    private void LoadTokenMetrics()
    {
        try
        {
            if (File.Exists(_metricsPath))
            {
                var json = File.ReadAllText(_metricsPath);
                _tokenMetrics = JsonSerializer.Deserialize<List<TokenUsageMetrics>>(json) 
                    ?? new List<TokenUsageMetrics>();
                
                const int retentionDays = 30;
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                _tokenMetrics = _tokenMetrics.Where(m => m.Timestamp >= cutoffDate).ToList();
                
                _runMetrics = _tokenMetrics
                    .Where(m => !string.IsNullOrEmpty(m.JobId))
                    .GroupBy(m => m.JobId!)
                    .ToDictionary(g => g.Key, g => g.ToList());
                
                _logger.LogInformation("Loaded {Count} token metrics from storage", _tokenMetrics.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load token metrics");
        }
    }

    private void SaveTokenMetrics()
    {
        try
        {
            var json = JsonSerializer.Serialize(_tokenMetrics, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_metricsPath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save token metrics");
        }
    }
}
