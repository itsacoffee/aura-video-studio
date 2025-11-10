using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Performance;

/// <summary>
/// Base class for performance tests with timing and metrics
/// </summary>
public abstract class PerformanceTestBase
{
    protected readonly ITestOutputHelper Output;
    private readonly List<PerformanceMetric> _metrics = new();

    protected PerformanceTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    /// <summary>
    /// Measure execution time of an action
    /// </summary>
    protected async Task<TimeSpan> MeasureAsync(Func<Task> action, string operationName = "Operation")
    {
        var sw = Stopwatch.StartNew();
        await action();
        sw.Stop();

        var metric = new PerformanceMetric
        {
            OperationName = operationName,
            Duration = sw.Elapsed,
            Timestamp = DateTime.UtcNow
        };

        _metrics.Add(metric);
        Output.WriteLine($"[{operationName}] Completed in {sw.ElapsedMilliseconds}ms");

        return sw.Elapsed;
    }

    /// <summary>
    /// Measure execution time of a synchronous action
    /// </summary>
    protected TimeSpan Measure(Action action, string operationName = "Operation")
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();

        var metric = new PerformanceMetric
        {
            OperationName = operationName,
            Duration = sw.Elapsed,
            Timestamp = DateTime.UtcNow
        };

        _metrics.Add(metric);
        Output.WriteLine($"[{operationName}] Completed in {sw.ElapsedMilliseconds}ms");

        return sw.Elapsed;
    }

    /// <summary>
    /// Assert that operation completes within threshold
    /// </summary>
    protected void AssertPerformance(TimeSpan actual, TimeSpan threshold, string operationName = "Operation")
    {
        Assert.True(actual <= threshold,
            $"{operationName} took {actual.TotalMilliseconds}ms, exceeding threshold of {threshold.TotalMilliseconds}ms");
    }

    /// <summary>
    /// Run an operation multiple times and get average duration
    /// </summary>
    protected async Task<TimeSpan> AverageDurationAsync(Func<Task> action, int iterations, string operationName = "Operation")
    {
        var durations = new List<TimeSpan>();

        for (int i = 0; i < iterations; i++)
        {
            var duration = await MeasureAsync(action, $"{operationName} #{i + 1}");
            durations.Add(duration);
        }

        var average = TimeSpan.FromMilliseconds(durations.Average(d => d.TotalMilliseconds));
        Output.WriteLine($"[{operationName}] Average: {average.TotalMilliseconds}ms over {iterations} iterations");

        return average;
    }

    /// <summary>
    /// Get performance summary
    /// </summary>
    protected void PrintPerformanceSummary()
    {
        if (_metrics.Count == 0)
        {
            Output.WriteLine("No performance metrics recorded");
            return;
        }

        Output.WriteLine("\n=== Performance Summary ===");
        Output.WriteLine($"Total operations: {_metrics.Count}");
        Output.WriteLine($"Total time: {_metrics.Sum(m => m.Duration.TotalMilliseconds)}ms");
        Output.WriteLine($"Average time: {_metrics.Average(m => m.Duration.TotalMilliseconds):F2}ms");
        Output.WriteLine($"Min time: {_metrics.Min(m => m.Duration.TotalMilliseconds):F2}ms");
        Output.WriteLine($"Max time: {_metrics.Max(m => m.Duration.TotalMilliseconds):F2}ms");

        Output.WriteLine("\nOperations by duration:");
        foreach (var metric in _metrics.OrderByDescending(m => m.Duration))
        {
            Output.WriteLine($"  {metric.OperationName}: {metric.Duration.TotalMilliseconds:F2}ms");
        }
    }

    protected class PerformanceMetric
    {
        public string OperationName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
