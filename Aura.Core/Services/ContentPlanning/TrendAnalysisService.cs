using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentPlanning;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentPlanning;

/// <summary>
/// Service for analyzing trends across platforms and categories
/// </summary>
public class TrendAnalysisService
{
    private readonly ILogger<TrendAnalysisService> _logger;

    public TrendAnalysisService(ILogger<TrendAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes trends based on keywords and platform
    /// </summary>
    public async Task<TrendAnalysisResponse> AnalyzeTrendsAsync(
        TrendAnalysisRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing trends for category: {Category}, platform: {Platform}",
            request.Category, request.Platform);

        await Task.Delay(100, ct); // Simulate API call

        var trends = new List<TrendData>();
        var platforms = string.IsNullOrEmpty(request.Platform)
            ? new[] { "YouTube", "TikTok", "Instagram" }
            : new[] { request.Platform };

        foreach (var platform in platforms)
        {
            foreach (var keyword in request.Keywords.Take(5))
            {
                trends.Add(GenerateTrendData(keyword, request.Category ?? "General", platform));
            }
        }

        return new TrendAnalysisResponse
        {
            Trends = trends,
            AnalyzedAt = DateTime.UtcNow,
            Summary = GenerateSummary(trends)
        };
    }

    /// <summary>
    /// Gets trending topics for a specific platform
    /// </summary>
    public async Task<List<TrendData>> GetPlatformTrendsAsync(
        string platform,
        string? category = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting platform trends for {Platform}, category: {Category}",
            platform, category);

        await Task.Delay(50, ct);

        var topics = new[] { "AI Technology", "Productivity", "Fitness", "Gaming", "Cooking" };
        return topics.Select(topic => GenerateTrendData(topic, category ?? "General", platform)).ToList();
    }

    private TrendData GenerateTrendData(string topic, string category, string platform)
    {
        var direction = Random.Shared.Next(0, 3) switch
        {
            0 => TrendDirection.Rising,
            1 => TrendDirection.Stable,
            _ => TrendDirection.Declining
        };

        var dataPoints = new List<TrendDataPoint>();
        var baseValue = 50 + Random.Shared.NextDouble() * 30;

        for (int i = 0; i < 7; i++)
        {
            var value = direction == TrendDirection.Rising
                ? baseValue + (i * 5)
                : direction == TrendDirection.Declining
                    ? baseValue - (i * 3)
                    : baseValue + (Random.Shared.NextDouble() - 0.5) * 5;

            dataPoints.Add(new TrendDataPoint
            {
                Timestamp = DateTime.UtcNow.AddDays(-6 + i),
                Value = Math.Max(0, value)
            });
        }

        return new TrendData
        {
            Topic = topic,
            Category = category,
            Platform = platform,
            TrendScore = 60 + Random.Shared.NextDouble() * 40,
            Direction = direction,
            DataPoints = dataPoints,
            Metrics = new Dictionary<string, object>
            {
                ["searchVolume"] = Random.Shared.Next(1000, 10000),
                ["engagement"] = Random.Shared.Next(50, 100),
                ["competition"] = Random.Shared.Next(20, 80)
            }
        };
    }

    private string GenerateSummary(List<TrendData> trends)
    {
        var risingCount = trends.Count(t => t.Direction == TrendDirection.Rising);
        var decliningCount = trends.Count(t => t.Direction == TrendDirection.Declining);

        if (risingCount > trends.Count / 2)
        {
            return "Strong upward trends detected across multiple topics. Great time to create content.";
        }
        else if (decliningCount > trends.Count / 2)
        {
            return "Most trends are declining. Consider exploring emerging topics.";
        }
        else
        {
            return "Mixed trends observed. Focus on rising topics for best results.";
        }
    }
}
