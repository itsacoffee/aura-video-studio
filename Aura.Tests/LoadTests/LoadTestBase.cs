using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.LoadTests;

/// <summary>
/// Base class for load testing scenarios
/// </summary>
public abstract class LoadTestBase
{
    protected ITestOutputHelper Output { get; }

    protected LoadTestBase(ITestOutputHelper output)
    {
        Output = output;
    }

    /// <summary>
    /// Runs a load test scenario
    /// </summary>
    protected async Task<LoadTestResult> RunLoadTestAsync(
        Func<int, Task> action,
        LoadTestConfiguration config)
    {
        Output.WriteLine($"Starting load test: {config.TestName}");
        Output.WriteLine($"Concurrency: {config.ConcurrentUsers}");
        Output.WriteLine($"Duration: {config.Duration}");
        Output.WriteLine($"Ramp-up: {config.RampUpDuration}");
        Output.WriteLine("");

        var sw = Stopwatch.StartNew();
        var results = new List<OperationResult>();
        var cts = new CancellationTokenSource(config.Duration + config.RampUpDuration);
        
        var tasks = new List<Task>();
        var userStartDelay = config.RampUpDuration.TotalMilliseconds / config.ConcurrentUsers;

        for (int i = 0; i < config.ConcurrentUsers; i++)
        {
            var userId = i;
            var task = Task.Run(async () =>
            {
                // Ramp-up: stagger user starts
                if (config.RampUpDuration > TimeSpan.Zero)
                {
                    await Task.Delay((int)(userId * userStartDelay), cts.Token);
                }

                // Run operations until test duration expires
                var userStopwatch = Stopwatch.StartNew();
                int operationCount = 0;

                while (!cts.Token.IsCancellationRequested && 
                       userStopwatch.Elapsed < config.Duration)
                {
                    var opResult = await ExecuteOperationAsync(action, userId, operationCount, cts.Token);
                    lock (results)
                    {
                        results.Add(opResult);
                    }
                    operationCount++;

                    // Optional: Add think time between operations
                    if (config.ThinkTime > TimeSpan.Zero)
                    {
                        await Task.Delay(config.ThinkTime, cts.Token);
                    }
                }
            }, cts.Token);

            tasks.Add(task);
        }

        // Wait for all users to complete
        await Task.WhenAll(tasks);
        sw.Stop();

        var result = new LoadTestResult
        {
            TestName = config.TestName,
            TotalDuration = sw.Elapsed,
            ConcurrentUsers = config.ConcurrentUsers,
            TotalOperations = results.Count,
            SuccessfulOperations = results.Count(r => r.Success),
            FailedOperations = results.Count(r => !r.Success),
            AverageResponseTime = TimeSpan.FromMilliseconds(results.Average(r => r.Duration.TotalMilliseconds)),
            MinResponseTime = results.Min(r => r.Duration),
            MaxResponseTime = results.Max(r => r.Duration),
            P50ResponseTime = GetPercentile(results, 0.50),
            P95ResponseTime = GetPercentile(results, 0.95),
            P99ResponseTime = GetPercentile(results, 0.99),
            ThroughputPerSecond = results.Count / sw.Elapsed.TotalSeconds,
            Errors = results.Where(r => !r.Success).Select(r => r.Error).ToList()
        };

        PrintResults(result);
        return result;
    }

    private async Task<OperationResult> ExecuteOperationAsync(
        Func<int, Task> action,
        int userId,
        int operationId,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await action(userId);
            sw.Stop();
            return new OperationResult
            {
                UserId = userId,
                OperationId = operationId,
                Success = true,
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            return new OperationResult
            {
                UserId = userId,
                OperationId = operationId,
                Success = false,
                Duration = sw.Elapsed,
                Error = ex.Message
            };
        }
    }

    private TimeSpan GetPercentile(List<OperationResult> results, double percentile)
    {
        var sorted = results.OrderBy(r => r.Duration.TotalMilliseconds).ToList();
        var index = (int)(sorted.Count * percentile);
        return sorted[Math.Min(index, sorted.Count - 1)].Duration;
    }

    private void PrintResults(LoadTestResult result)
    {
        Output.WriteLine("");
        Output.WriteLine("=== Load Test Results ===");
        Output.WriteLine($"Test Name: {result.TestName}");
        Output.WriteLine($"Total Duration: {result.TotalDuration.TotalSeconds:F2}s");
        Output.WriteLine($"Concurrent Users: {result.ConcurrentUsers}");
        Output.WriteLine("");
        Output.WriteLine($"Total Operations: {result.TotalOperations}");
        Output.WriteLine($"Successful: {result.SuccessfulOperations}");
        Output.WriteLine($"Failed: {result.FailedOperations}");
        Output.WriteLine($"Success Rate: {result.SuccessRate:P2}");
        Output.WriteLine("");
        Output.WriteLine($"Throughput: {result.ThroughputPerSecond:F2} ops/sec");
        Output.WriteLine("");
        Output.WriteLine("Response Times:");
        Output.WriteLine($"  Average: {result.AverageResponseTime.TotalMilliseconds:F2}ms");
        Output.WriteLine($"  Min: {result.MinResponseTime.TotalMilliseconds:F2}ms");
        Output.WriteLine($"  Max: {result.MaxResponseTime.TotalMilliseconds:F2}ms");
        Output.WriteLine($"  P50: {result.P50ResponseTime.TotalMilliseconds:F2}ms");
        Output.WriteLine($"  P95: {result.P95ResponseTime.TotalMilliseconds:F2}ms");
        Output.WriteLine($"  P99: {result.P99ResponseTime.TotalMilliseconds:F2}ms");

        if (result.Errors.Any())
        {
            Output.WriteLine("");
            Output.WriteLine("Errors:");
            foreach (var error in result.Errors.Take(10))
            {
                Output.WriteLine($"  - {error}");
            }
            if (result.Errors.Count > 10)
            {
                Output.WriteLine($"  ... and {result.Errors.Count - 10} more");
            }
        }

        Output.WriteLine("");
    }
}

public class LoadTestConfiguration
{
    public string TestName { get; set; } = "Load Test";
    public int ConcurrentUsers { get; set; } = 10;
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan RampUpDuration { get; set; } = TimeSpan.FromSeconds(5);
    public TimeSpan ThinkTime { get; set; } = TimeSpan.Zero;
}

public class LoadTestResult
{
    public string TestName { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public int ConcurrentUsers { get; set; }
    public int TotalOperations { get; set; }
    public int SuccessfulOperations { get; set; }
    public int FailedOperations { get; set; }
    public double SuccessRate => TotalOperations > 0 ? (double)SuccessfulOperations / TotalOperations : 0;
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan P50ResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }
    public double ThroughputPerSecond { get; set; }
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Asserts that the load test meets specified thresholds
    /// </summary>
    public void AssertMeetsThresholds(LoadTestThresholds thresholds)
    {
        Assert.True(SuccessRate >= thresholds.MinSuccessRate, 
            $"Success rate {SuccessRate:P2} below threshold {thresholds.MinSuccessRate:P2}");

        Assert.True(AverageResponseTime <= thresholds.MaxAverageResponseTime,
            $"Average response time {AverageResponseTime.TotalMilliseconds}ms exceeds threshold {thresholds.MaxAverageResponseTime.TotalMilliseconds}ms");

        Assert.True(P95ResponseTime <= thresholds.MaxP95ResponseTime,
            $"P95 response time {P95ResponseTime.TotalMilliseconds}ms exceeds threshold {thresholds.MaxP95ResponseTime.TotalMilliseconds}ms");

        Assert.True(ThroughputPerSecond >= thresholds.MinThroughput,
            $"Throughput {ThroughputPerSecond:F2} ops/sec below threshold {thresholds.MinThroughput:F2} ops/sec");
    }
}

public class LoadTestThresholds
{
    public double MinSuccessRate { get; set; } = 0.99; // 99%
    public TimeSpan MaxAverageResponseTime { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxP95ResponseTime { get; set; } = TimeSpan.FromSeconds(2);
    public double MinThroughput { get; set; } = 10.0; // ops/sec
}

internal class OperationResult
{
    public int UserId { get; set; }
    public int OperationId { get; set; }
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Error { get; set; }
}
