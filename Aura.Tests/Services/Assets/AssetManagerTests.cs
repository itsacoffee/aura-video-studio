using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Aura.Core.Services.Assets;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Services.Assets;

public class AssetManagerTests : IDisposable
{
    private readonly AssetManager _assetManager;
    private readonly string _testCacheDir;

    public AssetManagerTests()
    {
        _testCacheDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString());
        _assetManager = new AssetManager(
            NullLogger<AssetManager>.Instance,
            _testCacheDir,
            TimeSpan.FromMinutes(5)
        );
    }

    [Fact]
    public async Task CacheAsset_ShouldStoreAndRetrieve()
    {
        // Arrange
        var key = "test-asset";
        var content = "Test content for asset caching";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var cachePath = await _assetManager.CacheAssetAsync(key, stream, ".txt");
        var retrievedPath = _assetManager.GetCachedAsset(key);

        // Assert
        Assert.NotNull(cachePath);
        Assert.NotNull(retrievedPath);
        Assert.Equal(cachePath, retrievedPath);
        Assert.True(File.Exists(cachePath));
        
        var retrievedContent = await File.ReadAllTextAsync(cachePath);
        Assert.Equal(content, retrievedContent);
    }

    [Fact]
    public void GetCachedAsset_NonExistent_ShouldReturnNull()
    {
        // Act
        var result = _assetManager.GetCachedAsset("non-existent-key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CleanExpiredCache_ShouldRemoveExpiredEntries()
    {
        // Arrange
        var shortLivedManager = new AssetManager(
            NullLogger<AssetManager>.Instance,
            _testCacheDir,
            TimeSpan.FromMilliseconds(100)
        );
        
        var key = "expiring-asset";
        var content = "This will expire";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        await shortLivedManager.CacheAssetAsync(key, stream, ".txt");
        await Task.Delay(200); // Wait for expiration
        await shortLivedManager.CleanExpiredCacheAsync();
        var retrievedPath = shortLivedManager.GetCachedAsset(key);

        // Assert
        Assert.Null(retrievedPath);
    }

    [Fact]
    public async Task GetCacheStatistics_ShouldReturnCorrectStats()
    {
        // Arrange
        var content1 = "Content 1";
        var content2 = "Content 2";
        using var stream1 = new MemoryStream(Encoding.UTF8.GetBytes(content1));
        using var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(content2));

        // Act
        await _assetManager.CacheAssetAsync("asset1", stream1, ".txt");
        await _assetManager.CacheAssetAsync("asset2", stream2, ".txt");
        var stats = _assetManager.GetCacheStatistics();

        // Assert
        Assert.Equal(2, stats.EntryCount);
        Assert.True(stats.TotalSizeBytes > 0);
        Assert.NotNull(stats.OldestEntry);
        Assert.NotNull(stats.NewestEntry);
    }

    [Fact]
    public async Task ClearCache_ShouldRemoveAllEntries()
    {
        // Arrange
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test"));
        await _assetManager.CacheAssetAsync("asset", stream, ".txt");

        // Act
        _assetManager.ClearCache();
        var stats = _assetManager.GetCacheStatistics();

        // Assert
        Assert.Equal(0, stats.EntryCount);
        Assert.Equal(0, stats.TotalSizeBytes);
    }

    public void Dispose()
    {
        _assetManager.ClearCache();
        if (Directory.Exists(_testCacheDir))
        {
            try
            {
                Directory.Delete(_testCacheDir, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
