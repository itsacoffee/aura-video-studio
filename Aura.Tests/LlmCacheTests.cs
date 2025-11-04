using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Moq;

namespace Aura.Tests;

public class LlmCacheTests
{
    private readonly Mock<ILogger<MemoryLlmCache>> _loggerMock;
    private readonly LlmCacheOptions _options;
    private readonly MemoryLlmCache _cache;

    public LlmCacheTests()
    {
        _loggerMock = new Mock<ILogger<MemoryLlmCache>>();
        _options = new LlmCacheOptions
        {
            Enabled = true,
            MaxEntries = 10,
            DefaultTtlSeconds = 3600
        };
        var optionsMock = new Mock<IOptions<LlmCacheOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);
        
        _cache = new MemoryLlmCache(_loggerMock.Object, optionsMock.Object);
    }

    [Fact]
    public async Task GetAsync_WhenEmpty_ReturnsNull()
    {
        var result = await _cache.GetAsync("test-key");
        
        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValue()
    {
        var key = "test-key";
        var response = "test response";
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        await _cache.SetAsync(key, response, metadata);
        
        var result = await _cache.GetAsync(key);
        
        Assert.NotNull(result);
        Assert.Equal(response, result.Response);
        Assert.Equal("OpenAI", result.Metadata.ProviderName);
        Assert.Equal("gpt-4", result.Metadata.ModelName);
        Assert.Equal(1, result.AccessCount);
    }

    [Fact]
    public async Task GetAsync_MultipleAccesses_IncrementsAccessCount()
    {
        var key = "test-key";
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        await _cache.SetAsync(key, "response", metadata);
        
        await _cache.GetAsync(key);
        await _cache.GetAsync(key);
        var result = await _cache.GetAsync(key);
        
        Assert.NotNull(result);
        Assert.Equal(3, result.AccessCount);
    }

    [Fact]
    public async Task GetAsync_ExpiredEntry_ReturnsNull()
    {
        var key = "test-key";
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 1
        };
        
        await _cache.SetAsync(key, "response", metadata);
        
        await Task.Delay(1500);
        
        var result = await _cache.GetAsync(key);
        
        Assert.Null(result);
    }

    [Fact]
    public async Task RemoveAsync_RemovesEntry()
    {
        var key = "test-key";
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold"
        };
        
        await _cache.SetAsync(key, "response", metadata);
        await _cache.RemoveAsync(key);
        
        var result = await _cache.GetAsync(key);
        
        Assert.Null(result);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold"
        };
        
        await _cache.SetAsync("key1", "response1", metadata);
        await _cache.SetAsync("key2", "response2", metadata);
        await _cache.SetAsync("key3", "response3", metadata);
        
        await _cache.ClearAsync();
        
        var stats = await _cache.GetStatisticsAsync();
        Assert.Equal(0, stats.TotalEntries);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCorrectStats()
    {
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold"
        };
        
        await _cache.SetAsync("key1", "response1", metadata);
        await _cache.SetAsync("key2", "response2", metadata);
        
        await _cache.GetAsync("key1");
        await _cache.GetAsync("key1");
        await _cache.GetAsync("key3");
        
        var stats = await _cache.GetStatisticsAsync();
        
        Assert.Equal(2, stats.TotalEntries);
        Assert.Equal(2, stats.TotalHits);
        Assert.Equal(1, stats.TotalMisses);
        Assert.Equal(2.0 / 3.0, stats.HitRate, 2);
    }

    [Fact]
    public async Task SetAsync_WhenFull_EvictsLRU()
    {
        var options = new LlmCacheOptions
        {
            Enabled = true,
            MaxEntries = 3,
            DefaultTtlSeconds = 3600
        };
        var optionsMock = new Mock<IOptions<LlmCacheOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);
        var cache = new MemoryLlmCache(_loggerMock.Object, optionsMock.Object);
        
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold"
        };
        
        await cache.SetAsync("key1", "response1", metadata);
        await cache.SetAsync("key2", "response2", metadata);
        await cache.SetAsync("key3", "response3", metadata);
        
        await Task.Delay(100);
        
        await cache.SetAsync("key4", "response4", metadata);
        
        var result1 = await cache.GetAsync("key1");
        var result4 = await cache.GetAsync("key4");
        
        Assert.Null(result1);
        Assert.NotNull(result4);
    }

    [Fact]
    public async Task EvictExpiredAsync_RemovesExpiredEntries()
    {
        var metadata1 = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 1
        };
        
        var metadata2 = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        await _cache.SetAsync("key1", "response1", metadata1);
        await _cache.SetAsync("key2", "response2", metadata2);
        
        await Task.Delay(1500);
        
        await _cache.EvictExpiredAsync();
        
        var stats = await _cache.GetStatisticsAsync();
        
        Assert.Equal(1, stats.TotalEntries);
    }

    [Fact]
    public async Task GetAsync_WhenDisabled_ReturnsNull()
    {
        var options = new LlmCacheOptions
        {
            Enabled = false,
            MaxEntries = 10
        };
        var optionsMock = new Mock<IOptions<LlmCacheOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);
        var cache = new MemoryLlmCache(_loggerMock.Object, optionsMock.Object);
        
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold"
        };
        
        await cache.SetAsync("key", "response", metadata);
        
        var result = await cache.GetAsync("key");
        
        Assert.Null(result);
    }
}
