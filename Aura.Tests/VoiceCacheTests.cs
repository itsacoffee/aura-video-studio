using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Providers.Tts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class VoiceCacheTests : IDisposable
{
    private readonly VoiceCache _cache;
    private readonly string _testCacheDir;

    public VoiceCacheTests()
    {
        _testCacheDir = Path.Combine(Path.GetTempPath(), "AuraTests", $"Cache_{Guid.NewGuid():N}");
        _cache = new VoiceCache(NullLogger<VoiceCache>.Instance, _testCacheDir, maxCacheSizeMb: 10);
    }

    [Fact]
    public void Constructor_CreatesCacheDirectory()
    {
        Assert.True(Directory.Exists(_testCacheDir));
    }

    [Fact]
    public void TryGetCached_WithNoCachedContent_ReturnsNull()
    {
        var result = _cache.TryGetCached("ElevenLabs", "TestVoice", "Hello World");
        Assert.Null(result);
    }

    [Fact]
    public async Task StoreAsync_And_TryGetCached_ReturnsCachedFile()
    {
        // Arrange
        var testText = "Test audio content";
        var testAudioPath = CreateTestAudioFile();

        try
        {
            // Act - Store
            var storedPath = await _cache.StoreAsync(
                "ElevenLabs",
                "TestVoice",
                testText,
                testAudioPath);

            Assert.True(File.Exists(storedPath));

            // Act - Retrieve
            var cachedPath = _cache.TryGetCached("ElevenLabs", "TestVoice", testText);

            // Assert
            Assert.NotNull(cachedPath);
            Assert.Equal(storedPath, cachedPath);
            Assert.True(File.Exists(cachedPath));
        }
        finally
        {
            if (File.Exists(testAudioPath))
                File.Delete(testAudioPath);
        }
    }

    [Fact]
    public async Task StoreAsync_WithDifferentParameters_CreatesSeparateCacheEntries()
    {
        // Arrange
        var testText = "Same text";
        var audio1 = CreateTestAudioFile();
        var audio2 = CreateTestAudioFile();

        try
        {
            // Act - Store with different rates
            var path1 = await _cache.StoreAsync("ElevenLabs", "Voice1", testText, audio1, rate: 1.0);
            var path2 = await _cache.StoreAsync("ElevenLabs", "Voice1", testText, audio2, rate: 1.5);

            // Assert - Different cache keys
            Assert.NotEqual(path1, path2);

            var cached1 = _cache.TryGetCached("ElevenLabs", "Voice1", testText, rate: 1.0);
            var cached2 = _cache.TryGetCached("ElevenLabs", "Voice1", testText, rate: 1.5);

            Assert.NotNull(cached1);
            Assert.NotNull(cached2);
            Assert.NotEqual(cached1, cached2);
        }
        finally
        {
            if (File.Exists(audio1)) File.Delete(audio1);
            if (File.Exists(audio2)) File.Delete(audio2);
        }
    }

    [Fact]
    public void GetStatistics_ReturnsCorrectStats()
    {
        // Act
        var stats = _cache.GetStatistics();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(_testCacheDir, stats.CacheDirectory);
        Assert.Equal(10.0, stats.MaxSizeMb);
        Assert.True(stats.TotalFiles >= 0);
    }

    [Fact]
    public async Task ClearAsync_RemovesAllCachedFiles()
    {
        // Arrange
        var testAudioPath = CreateTestAudioFile();
        
        try
        {
            await _cache.StoreAsync("ElevenLabs", "TestVoice", "Test 1", testAudioPath);
            await _cache.StoreAsync("ElevenLabs", "TestVoice", "Test 2", testAudioPath);

            var statsBefore = _cache.GetStatistics();
            Assert.True(statsBefore.TotalFiles >= 2);

            // Act
            await _cache.ClearAsync();

            // Assert
            var statsAfter = _cache.GetStatistics();
            Assert.Equal(0, statsAfter.TotalFiles);
        }
        finally
        {
            if (File.Exists(testAudioPath))
                File.Delete(testAudioPath);
        }
    }

    [Fact]
    public async Task StoreAsync_WithMissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testCacheDir, "nonexistent.wav");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _cache.StoreAsync("ElevenLabs", "TestVoice", "Test", nonExistentPath));
    }

    [Fact]
    public async Task Cache_HandlesMultipleConcurrentRequests()
    {
        // Arrange
        var testAudioPath = CreateTestAudioFile();
        var tasks = new Task[10];

        try
        {
            // Act - Store same content multiple times concurrently
            for (int i = 0; i < 10; i++)
            {
                var index = i;
                tasks[i] = Task.Run(async () =>
                {
                    await _cache.StoreAsync("ElevenLabs", "Voice", $"Text {index}", testAudioPath);
                });
            }

            await Task.WhenAll(tasks);

            // Assert
            var stats = _cache.GetStatistics();
            Assert.True(stats.TotalFiles >= 10);
        }
        finally
        {
            if (File.Exists(testAudioPath))
                File.Delete(testAudioPath);
        }
    }

    [Fact]
    public async Task TryGetCached_UpdatesAccessStatistics()
    {
        // Arrange
        var testAudioPath = CreateTestAudioFile();
        
        try
        {
            await _cache.StoreAsync("ElevenLabs", "Voice", "Test", testAudioPath);
            var statsBefore = _cache.GetStatistics();

            // Act - Access cache multiple times
            for (int i = 0; i < 5; i++)
            {
                _cache.TryGetCached("ElevenLabs", "Voice", "Test");
            }

            // Assert
            var statsAfter = _cache.GetStatistics();
            Assert.True(statsAfter.TotalAccessCount > statsBefore.TotalAccessCount);
        }
        finally
        {
            if (File.Exists(testAudioPath))
                File.Delete(testAudioPath);
        }
    }

    private string CreateTestAudioFile()
    {
        var path = Path.Combine(_testCacheDir, $"test_{Guid.NewGuid():N}.wav");
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Create minimal WAV file (44 bytes header + some data)
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);
        
        // RIFF header
        bw.Write(new[] { 'R', 'I', 'F', 'F' });
        bw.Write(36 + 1000); // File size
        bw.Write(new[] { 'W', 'A', 'V', 'E' });
        
        // fmt subchunk
        bw.Write(new[] { 'f', 'm', 't', ' ' });
        bw.Write(16); // Subchunk size
        bw.Write((short)1); // PCM
        bw.Write((short)2); // Stereo
        bw.Write(44100); // Sample rate
        bw.Write(44100 * 2 * 2); // Byte rate
        bw.Write((short)4); // Block align
        bw.Write((short)16); // Bits per sample
        
        // data subchunk
        bw.Write(new[] { 'd', 'a', 't', 'a' });
        bw.Write(1000); // Data size
        bw.Write(new byte[1000]); // Dummy data

        return path;
    }

    public void Dispose()
    {
        _cache?.Dispose();
        
        if (Directory.Exists(_testCacheDir))
        {
            try
            {
                Directory.Delete(_testCacheDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
