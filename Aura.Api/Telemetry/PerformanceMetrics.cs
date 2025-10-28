using System.Collections.Concurrent;

namespace Aura.Api.Telemetry;

/// <summary>
/// Endpoint performance metrics
/// </summary>
public class EndpointMetrics
{
    public string Endpoint { get; set; } = string.Empty;
    public long TotalRequests { get; set; }
    public long TotalDurationMs { get; set; }
    public double AverageDurationMs => TotalRequests > 0 ? (double)TotalDurationMs / TotalRequests : 0;
    public long MinDurationMs { get; set; } = long.MaxValue;
    public long MaxDurationMs { get; set; }
    public long P50DurationMs { get; set; }
    public long P95DurationMs { get; set; }
    public long P99DurationMs { get; set; }
    public DateTime FirstRequestAt { get; set; }
    public DateTime LastRequestAt { get; set; }
}

/// <summary>
/// Service to collect and calculate performance metrics
/// </summary>
public class PerformanceMetrics
{
    private readonly ConcurrentDictionary<string, EndpointData> _endpointData = new();
    private readonly ILogger<PerformanceMetrics> _logger;

    /// <summary>
    /// Internal class to store raw request data for percentile calculations
    /// </summary>
    private class EndpointData
    {
        public long TotalRequests { get; set; }
        public long TotalDurationMs { get; set; }
        public long MinDurationMs { get; set; } = long.MaxValue;
        public long MaxDurationMs { get; set; }
        public DateTime FirstRequestAt { get; set; }
        public DateTime LastRequestAt { get; set; }
        
        // Store recent durations for percentile calculations (circular buffer)
        public List<long> RecentDurations { get; } = new();
        private const int MaxRecentDurations = 1000; // Keep last 1000 requests
        
        public void AddDuration(long durationMs)
        {
            TotalRequests++;
            TotalDurationMs += durationMs;
            MinDurationMs = Math.Min(MinDurationMs, durationMs);
            MaxDurationMs = Math.Max(MaxDurationMs, durationMs);
            LastRequestAt = DateTime.UtcNow;
            
            if (TotalRequests == 1)
            {
                FirstRequestAt = DateTime.UtcNow;
            }
            
            // Add to recent durations for percentile calculation
            lock (RecentDurations)
            {
                RecentDurations.Add(durationMs);
                
                // Keep only recent durations to prevent memory bloat
                if (RecentDurations.Count > MaxRecentDurations)
                {
                    RecentDurations.RemoveAt(0);
                }
            }
        }
        
        public (long p50, long p95, long p99) CalculatePercentiles()
        {
            lock (RecentDurations)
            {
                if (RecentDurations.Count == 0)
                {
                    return (0, 0, 0);
                }
                
                var sorted = RecentDurations.OrderBy(x => x).ToList();
                var count = sorted.Count;
                
                var p50Index = (int)Math.Ceiling(count * 0.50) - 1;
                var p95Index = (int)Math.Ceiling(count * 0.95) - 1;
                var p99Index = (int)Math.Ceiling(count * 0.99) - 1;
                
                return (
                    sorted[Math.Max(0, p50Index)],
                    sorted[Math.Max(0, p95Index)],
                    sorted[Math.Max(0, p99Index)]
                );
            }
        }
    }

    public PerformanceMetrics(ILogger<PerformanceMetrics> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Record a request duration for an endpoint
    /// </summary>
    public void RecordRequest(string endpoint, long durationMs)
    {
        var data = _endpointData.GetOrAdd(endpoint, _ => new EndpointData());
        data.AddDuration(durationMs);
    }

    /// <summary>
    /// Get all collected metrics
    /// </summary>
    public Dictionary<string, EndpointMetrics> GetMetrics()
    {
        var result = new Dictionary<string, EndpointMetrics>();

        foreach (var kvp in _endpointData)
        {
            var data = kvp.Value;
            var (p50, p95, p99) = data.CalculatePercentiles();

            result[kvp.Key] = new EndpointMetrics
            {
                Endpoint = kvp.Key,
                TotalRequests = data.TotalRequests,
                TotalDurationMs = data.TotalDurationMs,
                MinDurationMs = data.MinDurationMs == long.MaxValue ? 0 : data.MinDurationMs,
                MaxDurationMs = data.MaxDurationMs,
                P50DurationMs = p50,
                P95DurationMs = p95,
                P99DurationMs = p99,
                FirstRequestAt = data.FirstRequestAt,
                LastRequestAt = data.LastRequestAt
            };
        }

        return result;
    }

    /// <summary>
    /// Get metrics for a specific endpoint
    /// </summary>
    public EndpointMetrics? GetEndpointMetrics(string endpoint)
    {
        if (!_endpointData.TryGetValue(endpoint, out var data))
        {
            return null;
        }

        var (p50, p95, p99) = data.CalculatePercentiles();

        return new EndpointMetrics
        {
            Endpoint = endpoint,
            TotalRequests = data.TotalRequests,
            TotalDurationMs = data.TotalDurationMs,
            MinDurationMs = data.MinDurationMs == long.MaxValue ? 0 : data.MinDurationMs,
            MaxDurationMs = data.MaxDurationMs,
            P50DurationMs = p50,
            P95DurationMs = p95,
            P99DurationMs = p99,
            FirstRequestAt = data.FirstRequestAt,
            LastRequestAt = data.LastRequestAt
        };
    }

    /// <summary>
    /// Reset all metrics
    /// </summary>
    public void Reset()
    {
        _endpointData.Clear();
        _logger.LogInformation("Performance metrics reset");
    }

    /// <summary>
    /// Reset metrics for a specific endpoint
    /// </summary>
    public void ResetEndpoint(string endpoint)
    {
        _endpointData.TryRemove(endpoint, out _);
        _logger.LogInformation("Performance metrics reset for endpoint: {Endpoint}", endpoint);
    }
}
