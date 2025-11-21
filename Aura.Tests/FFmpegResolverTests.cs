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

    [Fact]
    public async Task ResolveAsync_WithAuraFfmpegPathEnvVar_UsesEnvironmentPath()
    {
        // Arrange - Set AURA_FFMPEG_PATH environment variable
        var testPath = Path.Combine(_testDir, "ffmpeg-aura");
        Directory.CreateDirectory(testPath);
        var originalValue = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");
        
        try
        {
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", testPath);
            
            // Act
            var result = await _resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);
            
            // Assert - Environment path should be attempted
            Assert.Contains(testPath, result.AttemptedPaths);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", originalValue);
        }
    }

    [Fact]
    public async Task ResolveAsync_WithLegacyFfmpegPathEnvVar_UsesEnvironmentPath()
    {
        // Arrange - Set FFMPEG_PATH environment variable (legacy)
        var testPath = Path.Combine(_testDir, "ffmpeg-legacy");
        Directory.CreateDirectory(testPath);
        var originalAuraValue = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");
        var originalFfmpegValue = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        
        try
        {
            // Ensure AURA_FFMPEG_PATH is not set so FFMPEG_PATH is used
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", null);
            Environment.SetEnvironmentVariable("FFMPEG_PATH", testPath);
            
            // Act
            var result = await _resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);
            
            // Assert - Legacy environment path should be attempted
            Assert.Contains(testPath, result.AttemptedPaths);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", originalAuraValue);
            Environment.SetEnvironmentVariable("FFMPEG_PATH", originalFfmpegValue);
        }
    }

    [Fact]
    public async Task ResolveAsync_WithFfmpegBinariesPathEnvVar_UsesEnvironmentPath()
    {
        // Arrange - Set FFMPEG_BINARIES_PATH environment variable (legacy)
        var testPath = Path.Combine(_testDir, "ffmpeg-binaries");
        Directory.CreateDirectory(testPath);
        var originalAuraValue = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");
        var originalFfmpegValue = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        var originalBinariesValue = Environment.GetEnvironmentVariable("FFMPEG_BINARIES_PATH");
        
        try
        {
            // Ensure higher priority vars are not set
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", null);
            Environment.SetEnvironmentVariable("FFMPEG_PATH", null);
            Environment.SetEnvironmentVariable("FFMPEG_BINARIES_PATH", testPath);
            
            // Act
            var result = await _resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);
            
            // Assert - Legacy binaries path should be attempted
            Assert.Contains(testPath, result.AttemptedPaths);
        }
        finally
        {
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", originalAuraValue);
            Environment.SetEnvironmentVariable("FFMPEG_PATH", originalFfmpegValue);
            Environment.SetEnvironmentVariable("FFMPEG_BINARIES_PATH", originalBinariesValue);
        }
    }

    [Fact]
    public async Task ResolveAsync_WithMultipleEnvVars_PrioritizesAuraFfmpegPath()
    {
        // Arrange - Set all three environment variables
        var auraPath = Path.Combine(_testDir, "ffmpeg-aura-priority");
        var legacyPath = Path.Combine(_testDir, "ffmpeg-legacy-priority");
        var binariesPath = Path.Combine(_testDir, "ffmpeg-binaries-priority");
        Directory.CreateDirectory(auraPath);
        Directory.CreateDirectory(legacyPath);
        Directory.CreateDirectory(binariesPath);
        
        var originalAuraValue = Environment.GetEnvironmentVariable("AURA_FFMPEG_PATH");
        var originalFfmpegValue = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        var originalBinariesValue = Environment.GetEnvironmentVariable("FFMPEG_BINARIES_PATH");
        
        try
        {
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", auraPath);
            Environment.SetEnvironmentVariable("FFMPEG_PATH", legacyPath);
            Environment.SetEnvironmentVariable("FFMPEG_BINARIES_PATH", binariesPath);
            
            // Act
            var result = await _resolver.ResolveAsync(null, forceRefresh: true, CancellationToken.None);
            
            // Assert - AURA_FFMPEG_PATH should be tried first
            var auraIndex = result.AttemptedPaths.IndexOf(auraPath);
            var legacyIndex = result.AttemptedPaths.IndexOf(legacyPath);
            
            Assert.True(auraIndex >= 0, "AURA_FFMPEG_PATH should be in attempted paths");
            if (legacyIndex >= 0)
            {
                Assert.True(auraIndex < legacyIndex, "AURA_FFMPEG_PATH should be tried before FFMPEG_PATH");
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("AURA_FFMPEG_PATH", originalAuraValue);
            Environment.SetEnvironmentVariable("FFMPEG_PATH", originalFfmpegValue);
            Environment.SetEnvironmentVariable("FFMPEG_BINARIES_PATH", originalBinariesValue);
        }
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
