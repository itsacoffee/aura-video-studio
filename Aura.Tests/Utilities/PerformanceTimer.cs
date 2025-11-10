using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Tests.Utilities;

/// <summary>
/// Utility for measuring performance in tests
/// </summary>
public class PerformanceTimer : IDisposable
{
    private readonly Stopwatch _stopwatch;
    private readonly string _operationName;
    private readonly ILogger? _logger;
    private readonly TimeSpan? _threshold;
    private bool _disposed;

    public PerformanceTimer(string operationName, TimeSpan? threshold = null, ILogger? logger = null)
    {
        _operationName = operationName;
        _threshold = threshold;
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();
    }

    public TimeSpan Elapsed => _stopwatch.Elapsed;

    public void Dispose()
    {
        if (_disposed) return;

        _stopwatch.Stop();
        
        var elapsed = _stopwatch.Elapsed;
        var message = $"{_operationName} completed in {elapsed.TotalMilliseconds:F2}ms";

        if (_threshold.HasValue && elapsed > _threshold.Value)
        {
            _logger?.LogWarning("{Message} (exceeded threshold of {Threshold}ms)", 
                message, _threshold.Value.TotalMilliseconds);
        }
        else
        {
            _logger?.LogInformation(message);
        }

        _disposed = true;
    }

    /// <summary>
    /// Measures the execution time of an action
    /// </summary>
    public static TimeSpan Measure(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.Elapsed;
    }

    /// <summary>
    /// Measures the execution time of an async action
    /// </summary>
    public static async Task<TimeSpan> MeasureAsync(Func<Task> action)
    {
        var sw = Stopwatch.StartNew();
        await action();
        sw.Stop();
        return sw.Elapsed;
    }

    /// <summary>
    /// Measures the execution time of a function
    /// </summary>
    public static (T Result, TimeSpan Duration) Measure<T>(Func<T> func)
    {
        var sw = Stopwatch.StartNew();
        var result = func();
        sw.Stop();
        return (result, sw.Elapsed);
    }

    /// <summary>
    /// Measures the execution time of an async function
    /// </summary>
    public static async Task<(T Result, TimeSpan Duration)> MeasureAsync<T>(Func<Task<T>> func)
    {
        var sw = Stopwatch.StartNew();
        var result = await func();
        sw.Stop();
        return (result, sw.Elapsed);
    }

    /// <summary>
    /// Runs a benchmark with multiple iterations
    /// </summary>
    public static BenchmarkResult Benchmark(Action action, int iterations, int warmupIterations = 1)
    {
        // Warmup
        for (int i = 0; i < warmupIterations; i++)
        {
            action();
        }

        // Measure
        var times = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            var duration = Measure(action);
            times.Add(duration);
        }

        return new BenchmarkResult(times);
    }

    /// <summary>
    /// Runs an async benchmark with multiple iterations
    /// </summary>
    public static async Task<BenchmarkResult> BenchmarkAsync(Func<Task> action, int iterations, int warmupIterations = 1)
    {
        // Warmup
        for (int i = 0; i < warmupIterations; i++)
        {
            await action();
        }

        // Measure
        var times = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            var duration = await MeasureAsync(action);
            times.Add(duration);
        }

        return new BenchmarkResult(times);
    }
}

/// <summary>
/// Results from a performance benchmark
/// </summary>
public class BenchmarkResult
{
    public BenchmarkResult(IEnumerable<TimeSpan> times)
    {
        var timesList = times.ToList();
        
        Iterations = timesList.Count;
        Min = timesList.Min();
        Max = timesList.Max();
        Average = TimeSpan.FromTicks((long)timesList.Average(t => t.Ticks));
        
        // Calculate median
        var sorted = timesList.OrderBy(t => t.Ticks).ToList();
        if (sorted.Count % 2 == 0)
        {
            var mid = sorted.Count / 2;
            Median = TimeSpan.FromTicks((sorted[mid - 1].Ticks + sorted[mid].Ticks) / 2);
        }
        else
        {
            Median = sorted[sorted.Count / 2];
        }

        // Calculate standard deviation
        var avgTicks = Average.Ticks;
        var variance = timesList.Average(t => Math.Pow(t.Ticks - avgTicks, 2));
        StandardDeviation = TimeSpan.FromTicks((long)Math.Sqrt(variance));

        // Calculate percentiles
        P95 = sorted[(int)(sorted.Count * 0.95)];
        P99 = sorted[(int)(sorted.Count * 0.99)];
    }

    public int Iterations { get; }
    public TimeSpan Min { get; }
    public TimeSpan Max { get; }
    public TimeSpan Average { get; }
    public TimeSpan Median { get; }
    public TimeSpan StandardDeviation { get; }
    public TimeSpan P95 { get; }
    public TimeSpan P99 { get; }

    public override string ToString()
    {
        return $"Iterations: {Iterations}, " +
               $"Avg: {Average.TotalMilliseconds:F2}ms, " +
               $"Min: {Min.TotalMilliseconds:F2}ms, " +
               $"Max: {Max.TotalMilliseconds:F2}ms, " +
               $"Median: {Median.TotalMilliseconds:F2}ms, " +
               $"StdDev: {StandardDeviation.TotalMilliseconds:F2}ms, " +
               $"P95: {P95.TotalMilliseconds:F2}ms, " +
               $"P99: {P99.TotalMilliseconds:F2}ms";
    }

    /// <summary>
    /// Generates a markdown table representation
    /// </summary>
    public string ToMarkdownTable()
    {
        return $"| Metric | Value |\n" +
               $"|--------|-------|\n" +
               $"| Iterations | {Iterations} |\n" +
               $"| Average | {Average.TotalMilliseconds:F2}ms |\n" +
               $"| Median | {Median.TotalMilliseconds:F2}ms |\n" +
               $"| Min | {Min.TotalMilliseconds:F2}ms |\n" +
               $"| Max | {Max.TotalMilliseconds:F2}ms |\n" +
               $"| Std Dev | {StandardDeviation.TotalMilliseconds:F2}ms |\n" +
               $"| P95 | {P95.TotalMilliseconds:F2}ms |\n" +
               $"| P99 | {P99.TotalMilliseconds:F2}ms |";
    }
}

/// <summary>
/// Compares two benchmark results
/// </summary>
public class BenchmarkComparison
{
    public BenchmarkComparison(BenchmarkResult baseline, BenchmarkResult current)
    {
        Baseline = baseline;
        Current = current;
        
        AverageChange = ((current.Average.TotalMilliseconds - baseline.Average.TotalMilliseconds) 
            / baseline.Average.TotalMilliseconds) * 100;
        
        MedianChange = ((current.Median.TotalMilliseconds - baseline.Median.TotalMilliseconds) 
            / baseline.Median.TotalMilliseconds) * 100;
        
        IsRegression = AverageChange > 10; // More than 10% slower
        IsImprovement = AverageChange < -10; // More than 10% faster
    }

    public BenchmarkResult Baseline { get; }
    public BenchmarkResult Current { get; }
    public double AverageChange { get; }
    public double MedianChange { get; }
    public bool IsRegression { get; }
    public bool IsImprovement { get; }

    public override string ToString()
    {
        var status = IsRegression ? "⚠️ REGRESSION" : IsImprovement ? "✅ IMPROVEMENT" : "➡️ NO CHANGE";
        return $"{status}: Average {AverageChange:+0.0;-0.0}%, Median {MedianChange:+0.0;-0.0}%";
    }
}
