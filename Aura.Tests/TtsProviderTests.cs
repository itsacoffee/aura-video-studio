using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Aura.Core.Models;
using Aura.Providers.Tts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class TtsProviderTests
{
    [Fact]
    public async Task MockTtsProvider_Should_GenerateValidWav()
    {
        // Arrange
        var wavValidator = new WavValidator(NullLogger<WavValidator>.Instance);
        var provider = new MockTtsProvider(NullLogger<MockTtsProvider>.Instance, wavValidator);
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Hello world", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2)),
            new ScriptLine(1, "This is a test", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3))
        };
        var voiceSpec = new VoiceSpec("Mock Voice 1", 1.0, 0.0, PauseStyle.Natural);

        // Act
        var result = await provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        
        // Verify it's a valid WAV file
        var fileInfo = new FileInfo(result);
        Assert.True(fileInfo.Length > 44); // At least header size
        
        // Read and verify WAV header
        using var stream = File.OpenRead(result);
        using var reader = new BinaryReader(stream);
        
        var riff = new string(reader.ReadChars(4));
        Assert.Equal("RIFF", riff);
        
        reader.ReadInt32(); // File size
        
        var wave = new string(reader.ReadChars(4));
        Assert.Equal("WAVE", wave);
        
        // Cleanup
        File.Delete(result);
    }

    [Fact]
    public async Task MockTtsProvider_Should_GenerateCorrectDuration()
    {
        // Arrange
        var wavValidator = new WavValidator(NullLogger<WavValidator>.Instance);
        var provider = new MockTtsProvider(NullLogger<MockTtsProvider>.Instance, wavValidator);
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Line 1", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1)),
            new ScriptLine(1, "Line 2", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)),
            new ScriptLine(2, "Line 3", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("Mock Voice 1", 1.0, 0.0, PauseStyle.Natural);

        // Act
        var result = await provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        
        // Read WAV file to check duration
        using var stream = File.OpenRead(result);
        using var reader = new BinaryReader(stream);
        
        // Read RIFF header
        reader.BaseStream.Seek(0, SeekOrigin.Begin);
        var riff = new string(reader.ReadChars(4)); // "RIFF"
        reader.ReadInt32(); // File size
        var wave = new string(reader.ReadChars(4)); // "WAVE"
        
        // Read fmt chunk
        var fmt = new string(reader.ReadChars(4)); // "fmt "
        int fmtSize = reader.ReadInt32();
        reader.ReadInt16(); // Audio format
        short numChannels = reader.ReadInt16(); // Num channels
        int sampleRate = reader.ReadInt32();
        reader.ReadInt32(); // Byte rate
        reader.ReadInt16(); // Block align
        short bitsPerSample = reader.ReadInt16();
        
        // Read data chunk header
        var data = new string(reader.ReadChars(4)); // "data"
        int dataSize = reader.ReadInt32();
        
        // Calculate duration from data size, sample rate, channels, and bits per sample
        int bytesPerSample = numChannels * (bitsPerSample / 8);
        double durationSeconds = (double)dataSize / (sampleRate * bytesPerSample);
        
        // Expected duration is 6 seconds (last line ends at 5 + 1)
        Assert.InRange(durationSeconds, 5.5, 6.5);
        
        // Cleanup
        File.Delete(result);
    }

    [Fact]
    public async Task MockTtsProvider_Should_ReturnMockVoices()
    {
        // Arrange
        var wavValidator = new WavValidator(NullLogger<WavValidator>.Instance);
        var provider = new MockTtsProvider(NullLogger<MockTtsProvider>.Instance, wavValidator);

        // Act
        var voices = await provider.GetAvailableVoicesAsync();

        // Assert
        Assert.NotNull(voices);
        Assert.NotEmpty(voices);
        Assert.Contains("Mock Voice 1", voices);
    }

    [Fact]
    public async Task MockTtsProvider_Should_HandleEmptyLines()
    {
        // Arrange
        var wavValidator = new WavValidator(NullLogger<WavValidator>.Instance);
        var provider = new MockTtsProvider(NullLogger<MockTtsProvider>.Instance, wavValidator);
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec("Mock Voice 1", 1.0, 0.0, PauseStyle.Natural);

        // Act
        var result = await provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        
        // Cleanup
        File.Delete(result);
    }

#if WINDOWS10_0_19041_0_OR_GREATER
    [Fact]
    public async Task WindowsTtsProvider_Should_ReturnVoices()
    {
        // Arrange
        var provider = new WindowsTtsProvider(NullLogger<WindowsTtsProvider>.Instance);

        // Act
        var voices = await provider.GetAvailableVoicesAsync();

        // Assert
        Assert.NotNull(voices);
        Assert.NotEmpty(voices);
    }

    [Fact]
    public async Task WindowsTtsProvider_Should_GenerateValidWav()
    {
        // Arrange
        var provider = new WindowsTtsProvider(NullLogger<WindowsTtsProvider>.Instance);
        var voices = await provider.GetAvailableVoicesAsync();
        
        var lines = new List<ScriptLine>
        {
            new ScriptLine(0, "Test", TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(1))
        };
        var voiceSpec = new VoiceSpec(voices[0], 1.0, 0.0, PauseStyle.Natural);

        // Act
        var result = await provider.SynthesizeAsync(lines, voiceSpec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(File.Exists(result));
        
        // Cleanup
        File.Delete(result);
    }
#endif
}
