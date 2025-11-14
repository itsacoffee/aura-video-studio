using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.ML;

/// <summary>
/// Tracks AI provider performance for intelligent provider selection
/// Records generation quality, speed, and reliability metrics
/// </summary>
public class ProviderPerformanceTracker
{
    private readonly ILogger<ProviderPerformanceTracker> _logger;
    private readonly Dictionary<string, List<ProviderPerformanceRecord>> _performanceHistory = new();
    private readonly SemaphoreSlim _lock = new(1, 1);

    public ProviderPerformanceTracker(ILogger<ProviderPerformanceTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Record a provider generation result
    /// </summary>
    public async Task RecordGenerationAsync(
        string providerName,
        string contentType,
        double qualityScore,
        TimeSpan duration,
        bool success,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var record = new ProviderPerformanceRecord
            {
                ProviderName = providerName,
                ContentType = contentType,
                QualityScore = qualityScore,
                DurationMs = (long)duration.TotalMilliseconds,
                Success = success,
                Timestamp = DateTime.UtcNow
            };

            if (!_performanceHistory.TryGetValue(providerName, out var value))
            {
                value = new List<ProviderPerformanceRecord>();
                _performanceHistory[providerName] = value;
            }

            value.Add(record);

            // Keep only recent history (last 100 records per provider)
            if (value.Count > 100)
            {
                value.RemoveAt(0);
            }

            _logger.LogDebug(
                "Recorded performance for {Provider}: Quality={Quality:F1}, Duration={Duration}ms, Success={Success}",
                providerName, qualityScore, duration.TotalMilliseconds, success);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Get provider statistics for a specific content type
    /// </summary>
    public async Task<ProviderStatistics?> GetProviderStatsAsync(
        string providerName,
        string? contentType = null,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!_performanceHistory.TryGetValue(providerName, out var records))
            {
                return null;
            }

            // Filter by content type if specified
            if (!string.IsNullOrEmpty(contentType))
            {
                records = records.Where(r => r.ContentType.Equals(contentType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (records.Count == 0)
            {
                return null;
            }

            var successfulRecords = records.Where(r => r.Success).ToList();
            var recentRecords = records.Where(r => (DateTime.UtcNow - r.Timestamp).TotalDays <= 7).ToList();

            return new ProviderStatistics
            {
                ProviderName = providerName,
                ContentType = contentType,
                TotalGenerations = records.Count,
                SuccessRate = records.Count > 0 ? (double)successfulRecords.Count / records.Count : 0,
                AverageQualityScore = successfulRecords.Count != 0 ? successfulRecords.Average(r => r.QualityScore) : 0,
                AverageDurationMs = successfulRecords.Count != 0 ? (long)successfulRecords.Average(r => r.DurationMs) : 0,
                RecentPerformanceTrend = CalculateTrend(recentRecords),
                LastUpdated = records.Max(r => r.Timestamp)
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Get best provider for a content type based on historical performance
    /// </summary>
    public async Task<string?> GetBestProviderAsync(
        string contentType,
        List<string> availableProviders,
        CancellationToken ct = default)
    {
        var providerScores = new Dictionary<string, double>();

        foreach (var provider in availableProviders)
        {
            var stats = await GetProviderStatsAsync(provider, contentType, ct);
            
            if (stats == null || stats.TotalGenerations < 3)
            {
                // Not enough data, use neutral score
                providerScores[provider] = 50.0;
                continue;
            }

            // Calculate composite score based on quality, success rate, and speed
            var qualityScore = stats.AverageQualityScore;
            var successPenalty = (1 - stats.SuccessRate) * 20; // Up to -20 for low success rate
            var speedBonus = stats.AverageDurationMs < 10000 ? 5 : 0; // Bonus for fast generation
            var trendBonus = stats.RecentPerformanceTrend == PerformanceTrend.Improving ? 5 : 
                           stats.RecentPerformanceTrend == PerformanceTrend.Declining ? -5 : 0;

            providerScores[provider] = qualityScore - successPenalty + speedBonus + trendBonus;
        }

        if (providerScores.Count == 0)
        {
            return null;
        }

        var bestProvider = providerScores.OrderByDescending(kv => kv.Value).First().Key;
        
        _logger.LogInformation(
            "Best provider for {ContentType}: {Provider} (score: {Score:F1})",
            contentType, bestProvider, providerScores[bestProvider]);

        return bestProvider;
    }

    /// <summary>
    /// Calculate performance trend from recent records
    /// </summary>
    private PerformanceTrend CalculateTrend(List<ProviderPerformanceRecord> recentRecords)
    {
        if (recentRecords.Count < 5)
        {
            return PerformanceTrend.Stable;
        }

        // Compare first half vs second half
        var halfPoint = recentRecords.Count / 2;
        var firstHalfAvg = recentRecords.Take(halfPoint).Average(r => r.QualityScore);
        var secondHalfAvg = recentRecords.Skip(halfPoint).Average(r => r.QualityScore);

        var difference = secondHalfAvg - firstHalfAvg;

        if (difference > 5)
        {
            return PerformanceTrend.Improving;
        }
        else if (difference < -5)
        {
            return PerformanceTrend.Declining;
        }
        else
        {
            return PerformanceTrend.Stable;
        }
    }

    /// <summary>
    /// Clear all performance history
    /// </summary>
    public async Task ClearHistoryAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _performanceHistory.Clear();
            _logger.LogInformation("Cleared provider performance history");
        }
        finally
        {
            _lock.Release();
        }
    }
}

/// <summary>
/// Performance record for a single generation
/// </summary>
public record ProviderPerformanceRecord
{
    public string ProviderName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public double QualityScore { get; init; }
    public long DurationMs { get; init; }
    public bool Success { get; init; }
    public DateTime Timestamp { get; init; }
}

/// <summary>
/// Aggregated statistics for a provider
/// </summary>
public record ProviderStatistics
{
    public string ProviderName { get; init; } = string.Empty;
    public string? ContentType { get; init; }
    public int TotalGenerations { get; init; }
    public double SuccessRate { get; init; }
    public double AverageQualityScore { get; init; }
    public long AverageDurationMs { get; init; }
    public PerformanceTrend RecentPerformanceTrend { get; init; }
    public DateTime LastUpdated { get; init; }
}

/// <summary>
/// Performance trend indicator
/// </summary>
public enum PerformanceTrend
{
    Declining,
    Stable,
    Improving
}
