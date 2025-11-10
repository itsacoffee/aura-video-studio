using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Services.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Performance;

public class CachePerformanceTests
{
    [Fact]
    public async Task GetAsync_FromMemoryCache_IsFasterThanDistributed()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new DistributedCacheService(distributedCache, memoryCache, NullLogger<DistributedCacheService>.Instance);
        
        var testData = new TestCacheData { Id = 1, Name = "Test" };
        await service.SetAsync("test-key", testData, TimeSpan.FromMinutes(5));

        // Act - First access (distributed cache)
        var sw1 = Stopwatch.StartNew();
        var result1 = await service.GetAsync<TestCacheData>("test-key");
        sw1.Stop();

        // Act - Second access (memory cache)
        var sw2 = Stopwatch.StartNew();
        var result2 = await service.GetAsync<TestCacheData>("test-key");
        sw2.Stop();

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Equal(testData.Name, result1.Name);
        Assert.Equal(testData.Name, result2.Name);
        
        // Memory cache should be faster
        Assert.True(sw2.ElapsedMilliseconds <= sw1.ElapsedMilliseconds, 
            $"Memory cache ({sw2.ElapsedMilliseconds}ms) should be faster than distributed cache ({sw1.ElapsedMilliseconds}ms)");
    }

    [Fact]
    public async Task GetOrCreateAsync_CacheMiss_CallsFactory()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new DistributedCacheService(distributedCache, memoryCache, NullLogger<DistributedCacheService>.Instance);
        
        var factoryCalled = false;
        async Task<TestCacheData> Factory()
        {
            factoryCalled = true;
            await Task.Delay(10); // Simulate work
            return new TestCacheData { Id = 1, Name = "Created" };
        }

        // Act
        var result = await service.GetOrCreateAsync("test-key", _ => Factory(), TimeSpan.FromMinutes(5));

        // Assert
        Assert.True(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal("Created", result.Name);
    }

    [Fact]
    public async Task GetOrCreateAsync_CacheHit_DoesNotCallFactory()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new DistributedCacheService(distributedCache, memoryCache, NullLogger<DistributedCacheService>.Instance);
        
        var testData = new TestCacheData { Id = 1, Name = "Cached" };
        await service.SetAsync("test-key", testData, TimeSpan.FromMinutes(5));

        var factoryCalled = false;
        Task<TestCacheData> Factory()
        {
            factoryCalled = true;
            return Task.FromResult(new TestCacheData { Id = 2, Name = "Created" });
        }

        // Act
        var result = await service.GetOrCreateAsync("test-key", _ => Factory(), TimeSpan.FromMinutes(5));

        // Assert
        Assert.False(factoryCalled);
        Assert.NotNull(result);
        Assert.Equal("Cached", result.Name);
        Assert.Equal(1, result.Id);
    }

    [Fact]
    public async Task CacheStatistics_TracksHitsAndMisses()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new DistributedCacheService(distributedCache, memoryCache, NullLogger<DistributedCacheService>.Instance);
        
        var testData = new TestCacheData { Id = 1, Name = "Test" };
        await service.SetAsync("key1", testData, TimeSpan.FromMinutes(5));

        // Act
        await service.GetAsync<TestCacheData>("key1"); // Hit
        await service.GetAsync<TestCacheData>("key1"); // Hit (memory)
        await service.GetAsync<TestCacheData>("key2"); // Miss
        await service.GetAsync<TestCacheData>("key3"); // Miss

        // Assert
        var stats = service.GetStatistics();
        Assert.Equal(2, stats.Hits);
        Assert.Equal(2, stats.Misses);
        Assert.Equal(0.5, stats.HitRate);
    }

    [Fact]
    public async Task CacheStatistics_CalculatesCorrectHitRate()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new DistributedCacheService(distributedCache, memoryCache, NullLogger<DistributedCacheService>.Instance);
        
        // Add 10 items
        for (int i = 0; i < 10; i++)
        {
            await service.SetAsync($"key{i}", new TestCacheData { Id = i, Name = $"Test{i}" }, TimeSpan.FromMinutes(5));
        }

        // Act - Generate 80 hits and 20 misses (80% hit rate)
        for (int i = 0; i < 8; i++)
        {
            foreach (var j in Enumerable.Range(0, 10))
            {
                await service.GetAsync<TestCacheData>($"key{j}");
            }
        }
        for (int i = 0; i < 20; i++)
        {
            await service.GetAsync<TestCacheData>($"missing-key-{i}");
        }

        // Assert
        var stats = service.GetStatistics();
        Assert.True(stats.HitRate >= 0.79 && stats.HitRate <= 0.81, $"Hit rate should be around 80%, got {stats.HitRate}");
    }

    [Fact]
    public async Task ParallelCacheAccess_HandlesStampedeProtection()
    {
        // Arrange
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var distributedCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var service = new DistributedCacheService(distributedCache, memoryCache, NullLogger<DistributedCacheService>.Instance);
        
        var factoryCallCount = 0;
        async Task<TestCacheData> Factory()
        {
            System.Threading.Interlocked.Increment(ref factoryCallCount);
            await Task.Delay(50); // Simulate expensive operation
            return new TestCacheData { Id = 1, Name = "Created" };
        }

        // Act - Multiple parallel requests for same key
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => service.GetOrCreateAsync("stampede-test", _ => Factory(), TimeSpan.FromMinutes(5)))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.NotNull(r));
        Assert.All(results, r => Assert.Equal("Created", r.Name));
        
        // Factory should only be called once due to stampede protection
        Assert.Equal(1, factoryCallCount);
    }

    private class TestCacheData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
