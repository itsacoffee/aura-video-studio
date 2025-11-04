using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Xunit.Abstractions;
using Moq;

namespace Aura.Tests;

public class LlmCachePerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<MemoryLlmCache>> _loggerMock;
    private readonly LlmCacheOptions _options;
    private readonly MemoryLlmCache _cache;

    public LlmCachePerformanceTests(ITestOutputHelper output)
    {
        _output = output;
        _loggerMock = new Mock<ILogger<MemoryLlmCache>>();
        _options = new LlmCacheOptions
        {
            Enabled = true,
            MaxEntries = 1000,
            DefaultTtlSeconds = 3600
        };
        
        var optionsMock = new Mock<IOptions<LlmCacheOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);
        
        _cache = new MemoryLlmCache(_loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public async Task CacheHit_ShouldBeFasterThan100ms()
    {
        var key = "perf-test-key";
        var response = GenerateLargeResponse(1000);
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        await _cache.SetAsync(key, response, metadata);
        
        var sw = Stopwatch.StartNew();
        var result = await _cache.GetAsync(key);
        sw.Stop();
        
        _output.WriteLine($"Cache hit latency: {sw.ElapsedMilliseconds}ms");
        
        Assert.NotNull(result);
        Assert.True(sw.ElapsedMilliseconds < 100, 
            $"Cache hit took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    [Fact]
    public async Task CacheMiss_ShouldBeFasterThan50ms()
    {
        var sw = Stopwatch.StartNew();
        var result = await _cache.GetAsync("non-existent-key");
        sw.Stop();
        
        _output.WriteLine($"Cache miss latency: {sw.ElapsedMilliseconds}ms");
        
        Assert.Null(result);
        Assert.True(sw.ElapsedMilliseconds < 50, 
            $"Cache miss took {sw.ElapsedMilliseconds}ms, expected < 50ms");
    }

    [Fact]
    public async Task CacheHit_P50_ShouldBeLessThan5ms()
    {
        var key = "perf-test-key";
        var response = "test response";
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        await _cache.SetAsync(key, response, metadata);
        
        var latencies = new List<long>();
        
        for (int i = 0; i < 100; i++)
        {
            var sw = Stopwatch.StartNew();
            await _cache.GetAsync(key);
            sw.Stop();
            latencies.Add(sw.ElapsedMilliseconds);
        }
        
        latencies.Sort();
        var p50 = latencies[50];
        var p95 = latencies[95];
        var p99 = latencies[99];
        
        _output.WriteLine($"Cache hit latency - P50: {p50}ms, P95: {p95}ms, P99: {p99}ms");
        
        Assert.True(p50 < 5, $"P50 latency was {p50}ms, expected < 5ms");
        Assert.True(p95 < 10, $"P95 latency was {p95}ms, expected < 10ms");
    }

    [Fact]
    public async Task ConcurrentReads_ShouldHandleLoad()
    {
        var keys = new List<string>();
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        for (int i = 0; i < 100; i++)
        {
            var key = $"key-{i}";
            keys.Add(key);
            await _cache.SetAsync(key, $"response {i}", metadata);
        }
        
        var sw = Stopwatch.StartNew();
        var tasks = keys.Select(async key =>
        {
            for (int i = 0; i < 10; i++)
            {
                await _cache.GetAsync(key);
            }
        });
        
        await Task.WhenAll(tasks);
        sw.Stop();
        
        var totalOps = 100 * 10;
        var opsPerSecond = totalOps / (sw.ElapsedMilliseconds / 1000.0);
        
        _output.WriteLine($"Concurrent reads: {totalOps} ops in {sw.ElapsedMilliseconds}ms ({opsPerSecond:F0} ops/sec)");
        
        Assert.True(opsPerSecond > 1000, 
            $"Throughput was {opsPerSecond:F0} ops/sec, expected > 1000 ops/sec");
    }

    [Fact]
    public async Task CacheImprovesLatency_By99Percent()
    {
        var key = "latency-test-key";
        var response = "test response";
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        var simulatedLlmLatencyMs = 2000;
        
        var swCold = Stopwatch.StartNew();
        await Task.Delay(simulatedLlmLatencyMs);
        await _cache.SetAsync(key, response, metadata);
        swCold.Stop();
        
        var swWarm = Stopwatch.StartNew();
        var result = await _cache.GetAsync(key);
        swWarm.Stop();
        
        var improvement = ((swCold.ElapsedMilliseconds - swWarm.ElapsedMilliseconds) * 100.0) / swCold.ElapsedMilliseconds;
        
        _output.WriteLine($"Cold: {swCold.ElapsedMilliseconds}ms, Warm: {swWarm.ElapsedMilliseconds}ms, Improvement: {improvement:F1}%");
        
        Assert.NotNull(result);
        Assert.True(improvement >= 99, 
            $"Cache improved latency by {improvement:F1}%, expected >= 99%");
    }

    [Fact]
    public async Task MixedWorkload_ShouldMaintainPerformance()
    {
        var random = new Random(42);
        var keys = Enumerable.Range(0, 50).Select(i => $"key-{i}").ToList();
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        foreach (var key in keys)
        {
            await _cache.SetAsync(key, $"response for {key}", metadata);
        }
        
        var operations = 1000;
        var latencies = new List<long>();
        
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < operations; i++)
        {
            var key = keys[random.Next(keys.Count)];
            
            var opSw = Stopwatch.StartNew();
            
            if (random.NextDouble() < 0.8)
            {
                await _cache.GetAsync(key);
            }
            else
            {
                await _cache.SetAsync(key, $"updated response {i}", metadata);
            }
            
            opSw.Stop();
            latencies.Add(opSw.ElapsedMilliseconds);
        }
        sw.Stop();
        
        latencies.Sort();
        var p50 = latencies[latencies.Count / 2];
        var p95 = latencies[(int)(latencies.Count * 0.95)];
        var opsPerSecond = operations / (sw.ElapsedMilliseconds / 1000.0);
        
        _output.WriteLine($"Mixed workload - P50: {p50}ms, P95: {p95}ms, Throughput: {opsPerSecond:F0} ops/sec");
        
        Assert.True(p50 < 5, $"P50 was {p50}ms, expected < 5ms");
        Assert.True(p95 < 20, $"P95 was {p95}ms, expected < 20ms");
    }

    [Fact]
    public async Task HitRate_ShouldBeAccurate()
    {
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        for (int i = 0; i < 10; i++)
        {
            await _cache.SetAsync($"key-{i}", $"response {i}", metadata);
        }
        
        for (int i = 0; i < 10; i++)
        {
            await _cache.GetAsync($"key-{i}");
        }
        
        for (int i = 10; i < 15; i++)
        {
            await _cache.GetAsync($"key-{i}");
        }
        
        var stats = await _cache.GetStatisticsAsync();
        
        _output.WriteLine($"Hits: {stats.TotalHits}, Misses: {stats.TotalMisses}, Hit Rate: {stats.HitRate:P1}");
        
        Assert.Equal(10, stats.TotalHits);
        Assert.Equal(5, stats.TotalMisses);
        Assert.Equal(0.666, stats.HitRate, 2);
    }

    private static string GenerateLargeResponse(int sizeBytes)
    {
        var chars = new char[sizeBytes];
        var random = new Random(42);
        
        for (int i = 0; i < sizeBytes; i++)
        {
            chars[i] = (char)('a' + random.Next(26));
        }
        
        return new string(chars);
    }
}
