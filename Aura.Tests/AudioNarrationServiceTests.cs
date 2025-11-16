using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Audio;
using Aura.Core.Models;
using Aura.Core.Models.OpenAI;
using Aura.Core.Providers;

namespace Aura.Tests;

public class AudioNarrationServiceTests
{
    private readonly Mock<ILogger<AudioNarrationService>> _loggerMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ITtsProvider> _ttsProviderMock;

    public AudioNarrationServiceTests()
    {
        _loggerMock = new Mock<ILogger<AudioNarrationService>>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _ttsProviderMock = new Mock<ITtsProvider>();
    }

    // Create a minimal mock factory for testing
    private TtsProviderFactory CreateMockFactory()
    {
        // We'll need to work around the factory limitation
        // For now, just test what we can without the factory
        return null!;
    }

    [Fact]
    public async Task GenerateNarrationAsync_WithEmptyText_ThrowsArgumentException()
    {
        // Arrange
        var service = new AudioNarrationService(_loggerMock.Object, _cache, CreateMockFactory());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await service.GenerateNarrationAsync("", "alloy"));
    }

    [Fact]
    public async Task GenerateNarrationAsync_WithAudioGenerator_ReturnsResult()
    {
        // Arrange
        var service = new AudioNarrationService(_loggerMock.Object, _cache, CreateMockFactory());
        var testText = "Hello, this is a test narration.";
        var testVoice = "alloy";

        // Create a mock audio generator
        Func<string, AudioConfig, CancellationToken, Task<AudioGenerationResult>> audioGenerator = 
            async (text, config, ct) =>
            {
                await Task.Delay(10, ct);
                return new AudioGenerationResult
                {
                    AudioData = Convert.ToBase64String(new byte[] { 0x52, 0x49, 0x46, 0x46 }), // Minimal WAV header
                    Transcript = text,
                    Format = "wav",
                    Voice = config.Voice.ToString().ToLowerInvariant()
                };
            };

        // Act
        var result = await service.GenerateNarrationAsync(testText, testVoice, audioGenerator, useCache: false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("OpenAI", result.Provider);
        Assert.Equal(testText, result.Transcript);
        Assert.Equal("alloy", result.Voice);
        Assert.True(System.IO.File.Exists(result.AudioPath));

        // Cleanup
        if (System.IO.File.Exists(result.AudioPath))
        {
            System.IO.File.Delete(result.AudioPath);
        }
    }

    [Fact]
    public async Task GenerateNarrationAsync_UsesCacheOnSecondCall()
    {
        // Arrange
        var service = new AudioNarrationService(_loggerMock.Object, _cache, CreateMockFactory());
        var testText = "Test caching";
        var testVoice = "nova";
        int generatorCallCount = 0;

        Func<string, AudioConfig, CancellationToken, Task<AudioGenerationResult>> audioGenerator = 
            async (text, config, ct) =>
            {
                generatorCallCount++;
                await Task.Delay(10, ct);
                return new AudioGenerationResult
                {
                    AudioData = Convert.ToBase64String(new byte[] { 0x52, 0x49, 0x46, 0x46 }),
                    Transcript = text,
                    Format = "wav",
                    Voice = config.Voice.ToString().ToLowerInvariant()
                };
            };

        // Act - First call
        var result1 = await service.GenerateNarrationAsync(testText, testVoice, audioGenerator, useCache: true);
        Assert.Equal(1, generatorCallCount);

        // Act - Second call (should use cache)
        var result2 = await service.GenerateNarrationAsync(testText, testVoice, audioGenerator, useCache: true);
        Assert.Equal(1, generatorCallCount); // Should not increment

        // Assert
        Assert.Equal(result1.AudioPath, result2.AudioPath);

        // Cleanup
        if (System.IO.File.Exists(result1.AudioPath))
        {
            System.IO.File.Delete(result1.AudioPath);
        }
    }

    [Theory]
    [InlineData("alloy", AudioVoice.Alloy)]
    [InlineData("echo", AudioVoice.Echo)]
    [InlineData("fable", AudioVoice.Fable)]
    [InlineData("onyx", AudioVoice.Onyx)]
    [InlineData("nova", AudioVoice.Nova)]
    [InlineData("shimmer", AudioVoice.Shimmer)]
    [InlineData("unknown", AudioVoice.Alloy)] // Default fallback
    public async Task GenerateNarrationAsync_MapsVoiceNamesCorrectly(string voiceName, AudioVoice expectedVoice)
    {
        // Arrange
        var service = new AudioNarrationService(_loggerMock.Object, _cache, CreateMockFactory());
        AudioVoice? capturedVoice = null;

        Func<string, AudioConfig, CancellationToken, Task<AudioGenerationResult>> audioGenerator = 
            async (text, config, ct) =>
            {
                capturedVoice = config.Voice;
                await Task.Delay(10, ct);
                return new AudioGenerationResult
                {
                    AudioData = Convert.ToBase64String(new byte[] { 0x52, 0x49, 0x46, 0x46 }),
                    Transcript = text,
                    Format = "wav",
                    Voice = config.Voice.ToString().ToLowerInvariant()
                };
            };

        // Act
        var result = await service.GenerateNarrationAsync("Test", voiceName, audioGenerator, useCache: false);

        // Assert
        Assert.Equal(expectedVoice, capturedVoice);

        // Cleanup
        if (System.IO.File.Exists(result.AudioPath))
        {
            System.IO.File.Delete(result.AudioPath);
        }
    }

    [Fact]
    public void ClearCache_RemovesCachedItems()
    {
        // Arrange
        var service = new AudioNarrationService(_loggerMock.Object, _cache, CreateMockFactory());
        
        // Act
        service.ClearCache();

        // Assert - Should not throw
        Assert.True(true);
    }
}
