using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// Null TTS provider that returns silence - used as final fallback when no other TTS is available
/// </summary>
public class NullTtsProvider : ITtsProvider
{
    private readonly ILogger<NullTtsProvider> _logger;
    private readonly string _outputDir;

    public NullTtsProvider(ILogger<NullTtsProvider> logger)
    {
        _logger = logger;
        _outputDir = Path.Combine(Path.GetTempPath(), "aura-null-tts");
        Directory.CreateDirectory(_outputDir);
    }

    public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
        // Return a single "silent" voice
        var voices = new List<string> { "Null (Silent)" };
        return Task.FromResult<IReadOnlyList<string>>(voices);
    }

    public async Task<string> SynthesizeAsync(
        IEnumerable<ScriptLine> lines,
        VoiceSpec spec,
        CancellationToken ct = default)
    {
        _logger.LogWarning("NullTtsProvider: Generating silent audio placeholder");
        
        // Calculate total duration
        var totalDuration = TimeSpan.Zero;
        foreach (var line in lines)
        {
            totalDuration += line.Duration;
        }

        var outputPath = Path.Combine(_outputDir, $"silent-{Guid.NewGuid()}.wav");

        // Generate a simple WAV file with silence
        // This is a minimal 16-bit PCM WAV with 1 second of silence at 44.1kHz
        var sampleRate = 44100;
        var channels = 1;
        var bitsPerSample = 16;
        var durationSeconds = (int)Math.Ceiling(totalDuration.TotalSeconds);
        
        var dataSize = sampleRate * channels * (bitsPerSample / 8) * durationSeconds;
        var headerSize = 44;
        var fileSize = headerSize + dataSize - 8;

        await using var stream = File.Create(outputPath);
        await using var writer = new BinaryWriter(stream);

        // WAV header
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(fileSize);
        writer.Write(new[] { 'W', 'A', 'V', 'E' });
        
        // fmt chunk
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // Chunk size
        writer.Write((short)1); // Audio format (PCM)
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(sampleRate * channels * bitsPerSample / 8); // Byte rate
        writer.Write((short)(channels * bitsPerSample / 8)); // Block align
        writer.Write((short)bitsPerSample);
        
        // data chunk
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);
        
        // Write silence (zeros)
        var buffer = new byte[4096];
        var remaining = dataSize;
        while (remaining > 0)
        {
            var toWrite = Math.Min(buffer.Length, remaining);
            writer.Write(buffer, 0, toWrite);
            remaining -= toWrite;
        }

        _logger.LogInformation("Generated silent audio: {Path}, Duration: {Duration}s", 
            outputPath, durationSeconds);
        
        return outputPath;
    }
}
