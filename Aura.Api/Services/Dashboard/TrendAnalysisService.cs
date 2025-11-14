using Microsoft.Extensions.Logging;

namespace Aura.Api.Services.Dashboard;

/// <summary>
/// Service for analyzing historical quality trends
/// </summary>
public class TrendAnalysisService
{
    private readonly ILogger<TrendAnalysisService> _logger;

    public TrendAnalysisService(ILogger<TrendAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets historical trend data for quality metrics
    /// </summary>
    public async Task<HistoricalTrends> GetHistoricalTrendsAsync(
        DateTime startDate,
        DateTime endDate,
        string granularity = "daily",
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting historical trends from {StartDate} to {EndDate} with {Granularity} granularity",
                startDate, endDate, granularity);

            await Task.Delay(50, ct).ConfigureAwait(false);

            var dataPoints = new List<TrendDataPoint>();
            var days = (endDate - startDate).Days;

            // Generate sample trend data
            for (int i = 0; i <= days; i++)
            {
                var date = startDate.AddDays(i);
                dataPoints.Add(new TrendDataPoint
                {
                    Timestamp = date,
                    QualityScore = 85 + Random.Shared.NextDouble() * 15,
                    ProcessedVideos = Random.Shared.Next(30, 60),
                    ErrorCount = Random.Shared.Next(0, 5),
                    AverageProcessingTime = TimeSpan.FromMinutes(10 + Random.Shared.NextDouble() * 5)
                });
            }

            return new HistoricalTrends
            {
                StartDate = startDate,
                EndDate = endDate,
                Granularity = granularity,
                DataPoints = dataPoints,
                TrendDirection = CalculateTrendDirection(dataPoints),
                AverageChange = CalculateAverageChange(dataPoints)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting historical trends");
            throw;
        }
    }

    private string CalculateTrendDirection(List<TrendDataPoint> dataPoints)
    {
        if (dataPoints.Count < 2) return "stable";

        var firstHalf = dataPoints.Take(dataPoints.Count / 2).Average(d => d.QualityScore);
        var secondHalf = dataPoints.Skip(dataPoints.Count / 2).Average(d => d.QualityScore);

        if (secondHalf > firstHalf + 2) return "improving";
        if (secondHalf < firstHalf - 2) return "declining";
        return "stable";
    }

    private double CalculateAverageChange(List<TrendDataPoint> dataPoints)
    {
        if (dataPoints.Count < 2) return 0;

        var firstScore = dataPoints.First().QualityScore;
        var lastScore = dataPoints.Last().QualityScore;

        return lastScore - firstScore;
    }
}

public class HistoricalTrends
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Granularity { get; set; } = "daily";
    public List<TrendDataPoint> DataPoints { get; set; } = new();
    public string TrendDirection { get; set; } = "stable";
    public double AverageChange { get; set; }
}

public class TrendDataPoint
{
    public DateTime Timestamp { get; set; }
    public double QualityScore { get; set; }
    public int ProcessedVideos { get; set; }
    public int ErrorCount { get; set; }
    public TimeSpan AverageProcessingTime { get; set; }
}
