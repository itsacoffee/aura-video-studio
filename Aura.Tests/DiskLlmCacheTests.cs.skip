using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
using Moq;

namespace Aura.Tests;

public class DiskLlmCacheTests : IDisposable
{
    private readonly Mock<ILogger<DiskLlmCache>> _loggerMock;
    private readonly LlmCacheOptions _options;
    private readonly DiskLlmCache _cache;
    private readonly string _tempCachePath;

    public DiskLlmCacheTests()
    {
        _loggerMock = new Mock<ILogger<DiskLlmCache>>();
        _tempCachePath = Path.Combine(Path.GetTempPath(), $"test-cache-{Guid.NewGuid()}");
        
        _options = new LlmCacheOptions
        {
            Enabled = true,
            MaxEntries = 10,
            DefaultTtlSeconds = 3600,
            UseDiskStorage = true,
            DiskStoragePath = _tempCachePath,
            MaxDiskSizeMB = 10
        };
        
        var optionsMock = new Mock<IOptions<LlmCacheOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);
        
        _cache = new DiskLlmCache(_loggerMock.Object, optionsMock.Object);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempCachePath))
            {
                Directory.Delete(_tempCachePath, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public async Task SetAsync_CreatesCacheDirectory()
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
        
        Assert.True(Directory.Exists(_tempCachePath));
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsValueFromMemory()
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
    }

    [Fact]
    public async Task GetAsync_LoadsFromDisk_WhenNotInMemory()
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
        
        var cache2Options = new LlmCacheOptions
        {
            Enabled = true,
            MaxEntries = 10,
            DefaultTtlSeconds = 3600,
            UseDiskStorage = true,
            DiskStoragePath = _tempCachePath,
            MaxDiskSizeMB = 10
        };
        
        var optionsMock2 = new Mock<IOptions<LlmCacheOptions>>();
        optionsMock2.Setup(x => x.Value).Returns(cache2Options);
        var cache2 = new DiskLlmCache(_loggerMock.Object, optionsMock2.Object);
        
        var result = await cache2.GetAsync(key);
        
        Assert.NotNull(result);
        Assert.Equal(response, result.Response);
    }

    [Fact]
    public async Task SetAsync_CreatesDiskFile()
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
        
        var filePath = Path.Combine(_tempCachePath, $"{key}.cache");
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task RemoveAsync_DeletesDiskFile()
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
        
        var filePath = Path.Combine(_tempCachePath, $"{key}.cache");
        Assert.True(File.Exists(filePath));
        
        await _cache.RemoveAsync(key);
        
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task ClearAsync_RemovesAllDiskFiles()
    {
        for (int i = 0; i < 3; i++)
        {
            var key = $"test-key-{i}";
            var metadata = new CacheMetadata
            {
                ProviderName = "OpenAI",
                ModelName = "gpt-4",
                OperationType = "PlanScaffold",
                TtlSeconds = 3600
            };
            
            await _cache.SetAsync(key, $"response {i}", metadata);
        }
        
        await _cache.ClearAsync();
        
        var files = Directory.GetFiles(_tempCachePath, "*.cache");
        Assert.Empty(files);
    }

    [Fact]
    public async Task EvictExpiredAsync_RemovesExpiredDiskEntries()
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
        
        await Task.Delay(1100);
        
        var evicted = await _cache.EvictExpiredAsync();
        
        Assert.True(evicted >= 1);
        var filePath = Path.Combine(_tempCachePath, $"{key}.cache");
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task GetStatisticsAsync_IncludesDiskEntries()
    {
        for (int i = 0; i < 3; i++)
        {
            var key = $"test-key-{i}";
            var metadata = new CacheMetadata
            {
                ProviderName = "OpenAI",
                ModelName = "gpt-4",
                OperationType = "PlanScaffold",
                TtlSeconds = 3600
            };
            
            await _cache.SetAsync(key, $"response {i}", metadata);
        }
        
        var stats = await _cache.GetStatisticsAsync();
        
        Assert.True(stats.TotalEntries >= 3);
        Assert.True(stats.TotalSizeBytes > 0);
    }

    [Fact]
    public async Task DiskCache_WorksWithoutDiskStorage()
    {
        var options = new LlmCacheOptions
        {
            Enabled = true,
            MaxEntries = 10,
            DefaultTtlSeconds = 3600,
            UseDiskStorage = false
        };
        
        var optionsMock = new Mock<IOptions<LlmCacheOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);
        
        var cache = new DiskLlmCache(_loggerMock.Object, optionsMock.Object);
        
        var key = "test-key";
        var metadata = new CacheMetadata
        {
            ProviderName = "OpenAI",
            ModelName = "gpt-4",
            OperationType = "PlanScaffold",
            TtlSeconds = 3600
        };
        
        await cache.SetAsync(key, "response", metadata);
        var result = await cache.GetAsync(key);
        
        Assert.NotNull(result);
        Assert.Equal("response", result.Response);
    }

    [Fact]
    public async Task GetAsync_ExpiredDiskEntry_ReturnsNull()
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
        await _cache.ClearAsync();
        
        await Task.Delay(1100);
        
        var result = await _cache.GetAsync(key);
        
        Assert.Null(result);
    }
}
