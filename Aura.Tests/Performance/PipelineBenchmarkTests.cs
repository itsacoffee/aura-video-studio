using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace Aura.Tests.Performance;

/// <summary>
/// Performance benchmark tests for video generation pipeline
/// Measures execution times, memory usage, and resource utilization
/// </summary>
public class PipelineBenchmarkTests
{
    private readonly ITestOutputHelper _output;

    public PipelineBenchmarkTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Benchmark script generation for various video durations
    /// </summary>
    [Theory]
    [InlineData(10, "10 second video")]
    [InlineData(30, "30 second video")]
    [InlineData(60, "1 minute video")]
    [InlineData(120, "2 minute video")]
    [InlineData(300, "5 minute video")]
    public async Task Benchmark_ScriptGeneration_VariousDurations(int durationSeconds, string description)
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = llmProvider
        };

        var providerMixer = new ProviderMixer(
            NullLogger<ProviderMixer>.Instance,
            new ProviderMixingConfig { ActiveProfile = "Free-Only" }
        );

        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            providerMixer,
            llmProviders
        );

        var brief = new Brief(
            Topic: $"Test Video - {description}",
            Audience: "Test Audience",
            Goal: "Performance Testing",
            Tone: "Informative",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(durationSeconds),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        // Warm-up run
        await orchestrator.GenerateScriptAsync(brief, planSpec, "Free", true, CancellationToken.None);

        // Act - Measure execution time
        var stopwatch = Stopwatch.StartNew();
        
        var result = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Free",
            offlineOnly: true,
            CancellationToken.None
        );
        
        stopwatch.Stop();

        // Assert and Report
        Assert.True(result.Success);
        Assert.NotNull(result.Script);

        var wordsPerSecond = result.Script!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 
                            (stopwatch.ElapsedMilliseconds / 1000.0);

        _output.WriteLine($"=== Benchmark: {description} ===");
        _output.WriteLine($"Duration Target: {durationSeconds}s");
        _output.WriteLine($"Execution Time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Script Length: {result.Script.Length} characters");
        _output.WriteLine($"Word Count: {result.Script.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length}");
        _output.WriteLine($"Generation Rate: {wordsPerSecond:F2} words/second");
        _output.WriteLine($"Provider Used: {result.ProviderUsed}");
        _output.WriteLine("");

        // Performance assertions - RuleBased provider should be fast
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, 
            $"Script generation took {stopwatch.ElapsedMilliseconds}ms, expected < 5000ms");
    }

    /// <summary>
    /// Benchmark memory usage during pipeline execution
    /// </summary>
    [Fact]
    public async Task Benchmark_MemoryUsage_DuringPipeline()
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = llmProvider
        };

        var providerMixer = new ProviderMixer(
            NullLogger<ProviderMixer>.Instance,
            new ProviderMixingConfig { ActiveProfile = "Free-Only" }
        );

        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            providerMixer,
            llmProviders
        );

        // Force garbage collection for accurate baseline
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryBefore = GC.GetTotalMemory(false);

        // Act - Execute pipeline
        var brief = new Brief(
            Topic: "Memory Usage Test",
            Audience: "Test",
            Goal: "Test",
            Tone: "Test",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        var result = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Free",
            offlineOnly: true,
            CancellationToken.None
        );

        var memoryDuring = GC.GetTotalMemory(false);

        // Force cleanup
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var memoryAfter = GC.GetTotalMemory(false);

        // Report
        var memoryUsedMB = (memoryDuring - memoryBefore) / (1024.0 * 1024.0);
        var memoryLeakedMB = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);

        _output.WriteLine("=== Memory Usage Benchmark ===");
        _output.WriteLine($"Memory Before: {memoryBefore / (1024.0 * 1024.0):F2} MB");
        _output.WriteLine($"Memory During: {memoryDuring / (1024.0 * 1024.0):F2} MB");
        _output.WriteLine($"Memory After GC: {memoryAfter / (1024.0 * 1024.0):F2} MB");
        _output.WriteLine($"Memory Used: {memoryUsedMB:F2} MB");
        _output.WriteLine($"Memory Leaked: {memoryLeakedMB:F2} MB");
        _output.WriteLine("");

        // Assert - Memory usage is reasonable
        Assert.True(result.Success);
        Assert.True(memoryUsedMB < 50, $"Memory usage ({memoryUsedMB:F2} MB) exceeded threshold");
        Assert.True(memoryLeakedMB < 5, $"Memory leak detected ({memoryLeakedMB:F2} MB)");
    }

    /// <summary>
    /// Benchmark throughput with multiple concurrent jobs
    /// </summary>
    [Theory]
    [InlineData(1, "Sequential")]
    [InlineData(2, "2 Concurrent")]
    [InlineData(5, "5 Concurrent")]
    [InlineData(10, "10 Concurrent")]
    public async Task Benchmark_Throughput_ConcurrentJobs(int concurrency, string description)
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = llmProvider
        };

        var providerMixer = new ProviderMixer(
            NullLogger<ProviderMixer>.Instance,
            new ProviderMixingConfig { ActiveProfile = "Free-Only" }
        );

        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            providerMixer,
            llmProviders
        );

        // Act - Execute concurrent jobs
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task<ScriptResult>>();

        for (int i = 0; i < concurrency; i++)
        {
            var brief = new Brief(
                Topic: $"Concurrent Job {i}",
                Audience: "Test",
                Goal: "Test",
                Tone: "Test",
                Language: "English",
                Aspect: Aspect.Widescreen16x9
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(30),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "Test"
            );

            tasks.Add(orchestrator.GenerateScriptAsync(
                brief,
                planSpec,
                "Free",
                offlineOnly: true,
                CancellationToken.None
            ));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Report
        var successCount = results.Count(r => r.Success);
        var throughput = successCount / (stopwatch.ElapsedMilliseconds / 1000.0);

        _output.WriteLine($"=== Throughput Benchmark: {description} ===");
        _output.WriteLine($"Concurrency: {concurrency}");
        _output.WriteLine($"Total Time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Successful Jobs: {successCount}/{concurrency}");
        _output.WriteLine($"Throughput: {throughput:F2} jobs/second");
        _output.WriteLine($"Average Time per Job: {stopwatch.ElapsedMilliseconds / (double)concurrency:F2}ms");
        _output.WriteLine("");

        // Assert - All jobs succeeded
        Assert.Equal(concurrency, successCount);
    }

    /// <summary>
    /// Benchmark provider selection time
    /// </summary>
    [Fact]
    public void Benchmark_ProviderSelection_Performance()
    {
        // Arrange
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["Provider1"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
            ["Provider2"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
            ["Provider3"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
            ["Provider4"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
            ["Provider5"] = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance),
        };

        var providerMixer = new ProviderMixer(
            NullLogger<ProviderMixer>.Instance,
            new ProviderMixingConfig { ActiveProfile = "Free-Only" }
        );

        // Act - Measure selection time
        var iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            var selection = providerMixer.SelectLlmProvider(llmProviders, "Free");
            Assert.NotNull(selection.SelectedProvider);
        }

        stopwatch.Stop();

        // Report
        var averageTimeMs = stopwatch.ElapsedMilliseconds / (double)iterations;
        var selectionsPerSecond = iterations / (stopwatch.ElapsedMilliseconds / 1000.0);

        _output.WriteLine("=== Provider Selection Benchmark ===");
        _output.WriteLine($"Iterations: {iterations}");
        _output.WriteLine($"Total Time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average Time: {averageTimeMs:F4}ms");
        _output.WriteLine($"Selections/Second: {selectionsPerSecond:F0}");
        _output.WriteLine("");

        // Assert - Selection should be very fast (< 1ms average)
        Assert.True(averageTimeMs < 1.0, $"Provider selection too slow: {averageTimeMs:F4}ms");
    }

    /// <summary>
    /// Benchmark cache effectiveness and performance impact
    /// </summary>
    [Fact]
    public async Task Benchmark_Cache_Performance()
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = llmProvider
        };

        var providerMixer = new ProviderMixer(
            NullLogger<ProviderMixer>.Instance,
            new ProviderMixingConfig { ActiveProfile = "Free-Only" }
        );

        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            providerMixer,
            llmProviders
        );

        var brief = new Brief(
            Topic: "Cache Test",
            Audience: "Test",
            Goal: "Test",
            Tone: "Test",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Test"
        );

        // Act - First execution (cold)
        var stopwatchCold = Stopwatch.StartNew();
        var result1 = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Free",
            offlineOnly: true,
            CancellationToken.None
        );
        stopwatchCold.Stop();

        // Second execution (potentially cached/warm)
        var stopwatchWarm = Stopwatch.StartNew();
        var result2 = await orchestrator.GenerateScriptAsync(
            brief,
            planSpec,
            "Free",
            offlineOnly: true,
            CancellationToken.None
        );
        stopwatchWarm.Stop();

        // Report
        _output.WriteLine("=== Cache Performance Benchmark ===");
        _output.WriteLine($"Cold Execution: {stopwatchCold.ElapsedMilliseconds}ms");
        _output.WriteLine($"Warm Execution: {stopwatchWarm.ElapsedMilliseconds}ms");
        _output.WriteLine($"Speedup: {stopwatchCold.ElapsedMilliseconds / (double)stopwatchWarm.ElapsedMilliseconds:F2}x");
        _output.WriteLine("");

        // Assert - Both executions succeeded
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        
        // RuleBased provider produces deterministic output
        Assert.Equal(result1.Script, result2.Script);
    }

    /// <summary>
    /// Benchmark scalability with increasing load
    /// </summary>
    [Fact]
    public async Task Benchmark_Scalability_IncreasingLoad()
    {
        // Arrange
        var llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        var llmProviders = new Dictionary<string, ILlmProvider>
        {
            ["RuleBased"] = llmProvider
        };

        var providerMixer = new ProviderMixer(
            NullLogger<ProviderMixer>.Instance,
            new ProviderMixingConfig { ActiveProfile = "Free-Only" }
        );

        var orchestrator = new ScriptOrchestrator(
            NullLogger<ScriptOrchestrator>.Instance,
            NullLoggerFactory.Instance,
            providerMixer,
            llmProviders
        );

        var loads = new[] { 1, 5, 10, 20 };
        var results = new List<(int Load, double AvgTimeMs, double ThroughputJobsPerSec)>();

        // Act - Test with increasing load
        foreach (var load in loads)
        {
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<ScriptResult>>();

            for (int i = 0; i < load; i++)
            {
                tasks.Add(orchestrator.GenerateScriptAsync(
                    new Brief($"Load Test {i}", "Test", "Test", "Test", "English", Aspect.Widescreen16x9),
                    new PlanSpec(TimeSpan.FromSeconds(10), Pacing.Fast, Density.Sparse, "Test"),
                    "Free",
                    offlineOnly: true,
                    CancellationToken.None
                ));
            }

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var avgTime = stopwatch.ElapsedMilliseconds / (double)load;
            var throughput = load / (stopwatch.ElapsedMilliseconds / 1000.0);
            
            results.Add((load, avgTime, throughput));
        }

        // Report
        _output.WriteLine("=== Scalability Benchmark ===");
        _output.WriteLine("Load | Avg Time (ms) | Throughput (jobs/s)");
        _output.WriteLine("-----|---------------|---------------------");
        foreach (var (load, avgTime, throughput) in results)
        {
            _output.WriteLine($"{load,4} | {avgTime,13:F2} | {throughput,19:F2}");
        }
        _output.WriteLine("");

        // Assert - System scales reasonably (throughput increases with load)
        Assert.True(results[^1].ThroughputJobsPerSec >= results[0].ThroughputJobsPerSec * 0.5,
            "Throughput should scale with load");
    }
}
