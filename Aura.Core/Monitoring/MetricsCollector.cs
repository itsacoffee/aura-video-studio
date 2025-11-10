using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Monitoring;

/// <summary>
/// Centralized metrics collection service for business and technical KPIs
/// </summary>
public class MetricsCollector
{
    private readonly ILogger<MetricsCollector> _logger;
    private readonly ConcurrentDictionary<string, MetricValue> _metrics = new();
    private readonly ConcurrentDictionary<string, Counter> _counters = new();
    private readonly ConcurrentDictionary<string, Histogram> _histograms = new();
    private readonly object _lock = new();

    public MetricsCollector(ILogger<MetricsCollector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Record a gauge metric (current value)
    /// </summary>
    public void RecordGauge(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        _metrics[key] = new MetricValue
        {
            Name = name,
            Value = value,
            Tags = tags ?? new Dictionary<string, string>(),
            Timestamp = DateTimeOffset.UtcNow,
            Type = MetricType.Gauge
        };

        _logger.LogDebug("Gauge metric recorded: {Name} = {Value} {@Tags}", name, value, tags);
    }

    /// <summary>
    /// Increment a counter metric
    /// </summary>
    public void IncrementCounter(string name, long increment = 1, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        var counter = _counters.GetOrAdd(key, _ => new Counter
        {
            Name = name,
            Tags = tags ?? new Dictionary<string, string>(),
            Value = 0
        });

        lock (counter)
        {
            counter.Value += increment;
            counter.LastUpdate = DateTimeOffset.UtcNow;
        }

        _logger.LogDebug("Counter incremented: {Name} += {Increment} {@Tags}", name, increment, tags);
    }

    /// <summary>
    /// Record a histogram value (for percentiles and distributions)
    /// </summary>
    public void RecordHistogram(string name, double value, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        var histogram = _histograms.GetOrAdd(key, _ => new Histogram
        {
            Name = name,
            Tags = tags ?? new Dictionary<string, string>(),
            Values = new List<double>()
        });

        lock (histogram)
        {
            histogram.Values.Add(value);
            histogram.LastUpdate = DateTimeOffset.UtcNow;
            
            // Keep only last 1000 values to prevent memory growth
            if (histogram.Values.Count > 1000)
            {
                histogram.Values.RemoveRange(0, histogram.Values.Count - 1000);
            }
        }

        _logger.LogDebug("Histogram value recorded: {Name} = {Value} {@Tags}", name, value, tags);
    }

    /// <summary>
    /// Record a duration using a stopwatch
    /// </summary>
    public IDisposable MeasureDuration(string name, Dictionary<string, string>? tags = null)
    {
        return new DurationMeasurement(this, name, tags);
    }

    /// <summary>
    /// Get current value of a metric
    /// </summary>
    public double? GetGaugeValue(string name, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        return _metrics.TryGetValue(key, out var metric) ? metric.Value : null;
    }

    /// <summary>
    /// Get current counter value
    /// </summary>
    public long? GetCounterValue(string name, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        return _counters.TryGetValue(key, out var counter) ? counter.Value : null;
    }

    /// <summary>
    /// Get histogram statistics
    /// </summary>
    public HistogramStats? GetHistogramStats(string name, Dictionary<string, string>? tags = null)
    {
        var key = BuildKey(name, tags);
        if (!_histograms.TryGetValue(key, out var histogram))
            return null;

        lock (histogram)
        {
            if (histogram.Values.Count == 0)
                return null;

            var sorted = histogram.Values.OrderBy(v => v).ToList();
            return new HistogramStats
            {
                Count = sorted.Count,
                Min = sorted.First(),
                Max = sorted.Last(),
                Mean = sorted.Average(),
                P50 = GetPercentile(sorted, 0.50),
                P90 = GetPercentile(sorted, 0.90),
                P95 = GetPercentile(sorted, 0.95),
                P99 = GetPercentile(sorted, 0.99)
            };
        }
    }

    /// <summary>
    /// Get all metrics snapshot
    /// </summary>
    public MetricsSnapshot GetSnapshot()
    {
        var gauges = _metrics.Values.ToList();
        var counters = _counters.Values.Select(c =>
        {
            lock (c)
            {
                return new CounterSnapshot
                {
                    Name = c.Name,
                    Value = c.Value,
                    Tags = c.Tags,
                    LastUpdate = c.LastUpdate
                };
            }
        }).ToList();

        var histograms = _histograms.Select(kvp =>
        {
            var stats = GetHistogramStats(kvp.Value.Name, kvp.Value.Tags);
            return new HistogramSnapshot
            {
                Name = kvp.Value.Name,
                Tags = kvp.Value.Tags,
                Stats = stats
            };
        }).ToList();

        return new MetricsSnapshot
        {
            Timestamp = DateTimeOffset.UtcNow,
            Gauges = gauges,
            Counters = counters,
            Histograms = histograms
        };
    }

    /// <summary>
    /// Reset all counters (typically called at beginning of new time window)
    /// </summary>
    public void ResetCounters()
    {
        foreach (var counter in _counters.Values)
        {
            lock (counter)
            {
                counter.Value = 0;
            }
        }
        _logger.LogInformation("All counters reset");
    }

    private string BuildKey(string name, Dictionary<string, string>? tags)
    {
        if (tags == null || tags.Count == 0)
            return name;

        var tagString = string.Join(",", tags.OrderBy(t => t.Key).Select(t => $"{t.Key}={t.Value}"));
        return $"{name}[{tagString}]";
    }

    private double GetPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
            return 0;

        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));
        return sortedValues[index];
    }

    private class DurationMeasurement : IDisposable
    {
        private readonly MetricsCollector _collector;
        private readonly string _name;
        private readonly Dictionary<string, string>? _tags;
        private readonly Stopwatch _stopwatch;

        public DurationMeasurement(MetricsCollector collector, string name, Dictionary<string, string>? tags)
        {
            _collector = collector;
            _name = name;
            _tags = tags;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _collector.RecordHistogram($"{_name}.duration_ms", _stopwatch.Elapsed.TotalMilliseconds, _tags);
        }
    }
}

public enum MetricType
{
    Gauge,
    Counter,
    Histogram
}

public class MetricValue
{
    public string Name { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTimeOffset Timestamp { get; set; }
    public MetricType Type { get; set; }
}

public class Counter
{
    public string Name { get; set; } = string.Empty;
    public long Value { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTimeOffset LastUpdate { get; set; }
}

public class Histogram
{
    public string Name { get; set; } = string.Empty;
    public List<double> Values { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTimeOffset LastUpdate { get; set; }
}

public class HistogramStats
{
    public int Count { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public double Mean { get; set; }
    public double P50 { get; set; }
    public double P90 { get; set; }
    public double P95 { get; set; }
    public double P99 { get; set; }
}

public class MetricsSnapshot
{
    public DateTimeOffset Timestamp { get; set; }
    public List<MetricValue> Gauges { get; set; } = new();
    public List<CounterSnapshot> Counters { get; set; } = new();
    public List<HistogramSnapshot> Histograms { get; set; } = new();
}

public class CounterSnapshot
{
    public string Name { get; set; } = string.Empty;
    public long Value { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public DateTimeOffset LastUpdate { get; set; }
}

public class HistogramSnapshot
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string> Tags { get; set; } = new();
    public HistogramStats? Stats { get; set; }
}
