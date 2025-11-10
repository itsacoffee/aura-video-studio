using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Performance;

/// <summary>
/// Utilities for performance and load testing
/// </summary>
public class LoadTestUtilities
{
    private readonly ITestOutputHelper _output;

    public LoadTestUtilities(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Executes a load test with specified parameters
    /// </summary>
    public async Task<LoadTestResult> ExecuteLoadTestAsync(
        Func<int, Task<TimeSpan>> operation,
        int concurrentUsers,
        int requestsPerUser,
        TimeSpan? timeout = null)
    {
        timeout ??= TimeSpan.FromMinutes(5);
        var cts = new CancellationTokenSource(timeout.Value);

        var results = new List<RequestResult>();
        var startTime = DateTime.UtcNow;

        try
        {
            var userTasks = Enumerable.Range(0, concurrentUsers)
                .Select(userId => SimulateUserAsync(userId, requestsPerUser, operation, results, cts.Token))
                .ToArray();

            await Task.WhenAll(userTasks);
        }
        catch (OperationCanceledException)
        {
            _output.WriteLine("Load test timed out");
        }

        var endTime = DateTime.UtcNow;
        var totalDuration = endTime - startTime;

        return AnalyzeResults(results, totalDuration, concurrentUsers, requestsPerUser);
    }

    private async Task SimulateUserAsync(
        int userId,
        int requestCount,
        Func<int, Task<TimeSpan>> operation,
        List<RequestResult> results,
        CancellationToken cancellationToken)
    {
        for (int i = 0; i < requestCount; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var requestStart = DateTime.UtcNow;
            try
            {
                var duration = await operation(userId);
                
                lock (results)
                {
                    results.Add(new RequestResult
                    {
                        UserId = userId,
                        RequestNumber = i,
                        Duration = duration,
                        Success = true,
                        Timestamp = requestStart
                    });
                }
            }
            catch (Exception ex)
            {
                lock (results)
                {
                    results.Add(new RequestResult
                    {
                        UserId = userId,
                        RequestNumber = i,
                        Duration = DateTime.UtcNow - requestStart,
                        Success = false,
                        Error = ex.Message,
                        Timestamp = requestStart
                    });
                }
            }

            // Small delay between requests from same user
            await Task.Delay(10, cancellationToken);
        }
    }

    private LoadTestResult AnalyzeResults(
        List<RequestResult> results,
        TimeSpan totalDuration,
        int concurrentUsers,
        int requestsPerUser)
    {
        var successfulRequests = results.Where(r => r.Success).ToList();
        var failedRequests = results.Where(r => !r.Success).ToList();

        var durations = successfulRequests.Select(r => r.Duration.TotalMilliseconds).OrderBy(d => d).ToList();
        
        var result = new LoadTestResult
        {
            TotalRequests = results.Count,
            SuccessfulRequests = successfulRequests.Count,
            FailedRequests = failedRequests.Count,
            SuccessRate = results.Count > 0 ? (double)successfulRequests.Count / results.Count : 0,
            TotalDuration = totalDuration,
            ConcurrentUsers = concurrentUsers,
            RequestsPerUser = requestsPerUser,
            RequestsPerSecond = results.Count / totalDuration.TotalSeconds
        };

        if (durations.Any())
        {
            result.MinResponseTime = TimeSpan.FromMilliseconds(durations.First());
            result.MaxResponseTime = TimeSpan.FromMilliseconds(durations.Last());
            result.AvgResponseTime = TimeSpan.FromMilliseconds(durations.Average());
            result.MedianResponseTime = TimeSpan.FromMilliseconds(durations[durations.Count / 2]);
            
            // Calculate percentiles
            result.P50ResponseTime = TimeSpan.FromMilliseconds(Percentile(durations, 0.50));
            result.P95ResponseTime = TimeSpan.FromMilliseconds(Percentile(durations, 0.95));
            result.P99ResponseTime = TimeSpan.FromMilliseconds(Percentile(durations, 0.99));
        }

        return result;
    }

    private static double Percentile(List<double> sortedValues, double percentile)
    {
        if (!sortedValues.Any())
            return 0;

        var index = (int)Math.Ceiling(percentile * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));
        return sortedValues[index];
    }

    public void PrintResults(LoadTestResult result)
    {
        _output.WriteLine("=== Load Test Results ===");
        _output.WriteLine($"Total Requests: {result.TotalRequests}");
        _output.WriteLine($"Successful: {result.SuccessfulRequests} ({result.SuccessRate:P2})");
        _output.WriteLine($"Failed: {result.FailedRequests}");
        _output.WriteLine($"Duration: {result.TotalDuration.TotalSeconds:F2}s");
        _output.WriteLine($"Requests/sec: {result.RequestsPerSecond:F2}");
        _output.WriteLine($"Concurrent Users: {result.ConcurrentUsers}");
        _output.WriteLine("");
        _output.WriteLine("Response Times:");
        _output.WriteLine($"  Min: {result.MinResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"  Avg: {result.AvgResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"  Median: {result.MedianResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"  P95: {result.P95ResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"  P99: {result.P99ResponseTime.TotalMilliseconds:F2}ms");
        _output.WriteLine($"  Max: {result.MaxResponseTime.TotalMilliseconds:F2}ms");
    }
}

public class RequestResult
{
    public int UserId { get; set; }
    public int RequestNumber { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public DateTime Timestamp { get; set; }
}

public class LoadTestResult
{
    public int TotalRequests { get; set; }
    public int SuccessfulRequests { get; set; }
    public int FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public int ConcurrentUsers { get; set; }
    public int RequestsPerUser { get; set; }
    public double RequestsPerSecond { get; set; }
    public TimeSpan MinResponseTime { get; set; }
    public TimeSpan MaxResponseTime { get; set; }
    public TimeSpan AvgResponseTime { get; set; }
    public TimeSpan MedianResponseTime { get; set; }
    public TimeSpan P50ResponseTime { get; set; }
    public TimeSpan P95ResponseTime { get; set; }
    public TimeSpan P99ResponseTime { get; set; }

    public bool MeetsPerformanceCriteria(TimeSpan maxP95ResponseTime, double minSuccessRate = 0.99)
    {
        return P95ResponseTime <= maxP95ResponseTime && SuccessRate >= minSuccessRate;
    }
}
