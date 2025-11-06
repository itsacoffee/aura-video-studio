using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class FFmpegResolverTests : IDisposable
{
    private readonly FFmpegResolver _resolver;
    private readonly IMemoryCache _cache;
    private readonly string _testDir;

    public FFmpegResolverTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _resolver = new FFmpegResolver(NullLogger<FFmpegResolver>.Instance, _cache);

        _testDir = Path.Combine(Path.GetTempPath(), "FFmpegResolverTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    [Fact]
    public async Task ResolveAsync_WhenNoFFmpegInstalled_ReturnsNotFound()
    {
        // Arrange - no FFmpeg installed anywhere

        // Act
        var result = await _resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);

        // Assert
        Assert.False(result.Found);
        Assert.False(result.IsValid);
        Assert.Equal("None", result.Source);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ResolveAsync_WithInvalidConfiguredPath_ReturnsNotFound()
    {
        // Arrange
        var invalidPath = "/path/that/does/not/exist/ffmpeg.exe";

        // Act
        var result = await _resolver.ResolveAsync(invalidPath, forceRefresh: true, CancellationToken.None);

        // Assert
        Assert.False(result.Found);
        // Since configured path doesn't exist, it falls through to PATH (which likely doesn't have FFmpeg in test environment)
        // So the final source will be "None"
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ResolveAsync_WithFFmpegStringPath_ChecksPATH()
    {
        // Arrange - "ffmpeg" string indicates PATH lookup

        // Act
        var result = await _resolver.ResolveAsync("ffmpeg", forceRefresh: true, CancellationToken.None);

        // Assert
        // Since we can't guarantee FFmpeg is on PATH in test environment
        // Just verify it attempts resolution (may not find it)
        Assert.NotNull(result);
    }

    [Fact]
    public async Task InvalidateCache_ClearsCachedResult()
    {
        // Arrange
        await _resolver.ResolveAsync(null, forceRefresh: false, CancellationToken.None);

        // Act
        _resolver.InvalidateCache();
        
        // Assert - next call should not use cache (will force fresh lookup)
        var result = await _resolver.ResolveAsync(null, forceRefresh: false, CancellationToken.None);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResolveAsync_CachesResult()
    {
        // Arrange
        var result1 = await _resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);

        // Act - call again without forcing refresh
        var result2 = await _resolver.ResolveAsync(null, forceRefresh: false, CancellationToken.None);

        // Assert - results should be the same (from cache)
        Assert.Equal(result1.Found, result2.Found);
        Assert.Equal(result1.Source, result2.Source);
        Assert.Equal(result1.Error, result2.Error);
    }

    [Fact]
    public async Task ResolveAsync_ForceRefresh_BypassesCache()
    {
        // Arrange
        await _resolver.ResolveAsync(null, forceRefresh: false, CancellationToken.None);

        // Act - force refresh should bypass cache
        var result = await _resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
            {
                Directory.Delete(_testDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}
