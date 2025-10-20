using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Tts;

/// <summary>
/// Mock TTS provider for CI/Linux environments.
/// Generates deterministic beep/silence WAV files with correct length for testing.
/// Uses atomic file operations and validation for reliability.
/// </summary>
public class MockTtsProvider : ITtsProvider
{
    private readonly ILogger<MockTtsProvider> _logger;
    private readonly WavValidator _wavValidator;
    private readonly string _outputDirectory;

    public MockTtsProvider(ILogger<MockTtsProvider> logger, WavValidator wavValidator)
    {
        _logger = logger;
        _wavValidator = wavValidator;
        _outputDirectory = Path.Combine(Path.GetTempPath(), "AuraVideoStudio", "TTS");
        
        // Ensure output directory exists
        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
    {
        _logger.LogInformation("MockTtsProvider: Returning mock voices");
        return Task.FromResult<IReadOnlyList<string>>(new List<string> 
        { 
            "Mock Voice 1", 
            "Mock Voice 2", 
            "Mock Voice 3" 
        });
    }

    public async Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
    {
        _logger.LogInformation("MockTtsProvider: Synthesizing speech with mock voice {Voice}", spec.VoiceName);

        var linesList = lines.ToList();
        
        // Calculate total duration
        TimeSpan totalDuration = TimeSpan.Zero;
        foreach (var line in linesList)
        {
            totalDuration = totalDuration > (line.Start + line.Duration) 
                ? totalDuration 
                : line.Start + line.Duration;
        }

        // Generate a deterministic WAV file with the correct length
        string outputFilePath = Path.Combine(_outputDirectory, $"narration_mock_{Guid.NewGuid():N}.wav");
        string tempPath = outputFilePath + ".tmp";
        
        _logger.LogInformation("MockTtsProvider: Generating {Duration}s of mock audio for {Count} lines", 
            totalDuration.TotalSeconds, linesList.Count);

        try
        {
            // Write to temp file
            await using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await GenerateWavContentAsync(fileStream, totalDuration, ct);
            }

            // Validate the generated file
            var validationResult = await _wavValidator.ValidateAsync(tempPath, ct);
            if (!validationResult.IsValid)
            {
                _logger.LogError("Generated WAV file failed validation: {Error}", validationResult.ErrorMessage);
                throw new InvalidDataException($"Generated WAV file failed validation: {validationResult.ErrorMessage}");
            }

            // Atomic rename
            File.Move(tempPath, outputFilePath, overwrite: true);

            _logger.LogInformation("Successfully generated mock audio: {Path}", outputFilePath);
            return outputFilePath;
        }
        catch
        {
            // Clean up temp file on error
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }
            throw;
        }
    }

    /// <summary>
    /// Generates deterministic WAV content with silence/beep pattern.
    /// WAV format: 44.1kHz, 16-bit, mono
    /// </summary>
    private async Task GenerateWavContentAsync(FileStream fileStream, TimeSpan duration, CancellationToken ct)
    {
        const int sampleRate = 44100;
        const short bitsPerSample = 16;
        const short numChannels = 1;

        int numSamples = (int)(duration.TotalSeconds * sampleRate);
        int dataSize = numSamples * numChannels * (bitsPerSample / 8);
        
        // Write directly to stream without BinaryWriter to avoid disposal issues
        // Write WAV header
        await WriteWavHeaderDirectAsync(fileStream, dataSize, sampleRate, bitsPerSample, numChannels);

        // Generate deterministic audio samples (silence with occasional beeps)
        // This creates a predictable pattern for testing
        byte[] buffer = new byte[2]; // 16-bit samples = 2 bytes
        for (int i = 0; i < numSamples; i++)
        {
            ct.ThrowIfCancellationRequested();

            short sample;
            
            // Generate a beep every second (simple sine wave at 440 Hz)
            // For the rest, generate silence
            double time = (double)i / sampleRate;
            int secondMark = (int)time;
            double timeInSecond = time - secondMark;
            
            if (timeInSecond < 0.1) // First 100ms of each second has a beep
            {
                // Generate 440 Hz sine wave (A4 note)
                double frequency = 440.0;
                double amplitude = 0.3; // 30% volume
                sample = (short)(amplitude * short.MaxValue * Math.Sin(2.0 * Math.PI * frequency * time));
            }
            else
            {
                // Silence
                sample = 0;
            }

            // Write sample as little-endian bytes
            buffer[0] = (byte)(sample & 0xFF);
            buffer[1] = (byte)((sample >> 8) & 0xFF);
            await fileStream.WriteAsync(buffer, 0, 2, ct);
        }
    }

    private async Task WriteWavHeaderDirectAsync(FileStream stream, int dataSize, int sampleRate, short bitsPerSample, short numChannels)
    {
        int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
        short blockAlign = (short)(numChannels * (bitsPerSample / 8));

        byte[] header = new byte[44];
        int pos = 0;

        // RIFF header
        header[pos++] = (byte)'R';
        header[pos++] = (byte)'I';
        header[pos++] = (byte)'F';
        header[pos++] = (byte)'F';
        
        // File size - 8
        int fileSize = 36 + dataSize;
        header[pos++] = (byte)(fileSize & 0xFF);
        header[pos++] = (byte)((fileSize >> 8) & 0xFF);
        header[pos++] = (byte)((fileSize >> 16) & 0xFF);
        header[pos++] = (byte)((fileSize >> 24) & 0xFF);
        
        // WAVE
        header[pos++] = (byte)'W';
        header[pos++] = (byte)'A';
        header[pos++] = (byte)'V';
        header[pos++] = (byte)'E';

        // fmt subchunk
        header[pos++] = (byte)'f';
        header[pos++] = (byte)'m';
        header[pos++] = (byte)'t';
        header[pos++] = (byte)' ';
        
        // Subchunk1Size (16 for PCM)
        header[pos++] = 16;
        header[pos++] = 0;
        header[pos++] = 0;
        header[pos++] = 0;
        
        // AudioFormat (1 for PCM)
        header[pos++] = 1;
        header[pos++] = 0;
        
        // NumChannels
        header[pos++] = (byte)(numChannels & 0xFF);
        header[pos++] = (byte)((numChannels >> 8) & 0xFF);
        
        // SampleRate
        header[pos++] = (byte)(sampleRate & 0xFF);
        header[pos++] = (byte)((sampleRate >> 8) & 0xFF);
        header[pos++] = (byte)((sampleRate >> 16) & 0xFF);
        header[pos++] = (byte)((sampleRate >> 24) & 0xFF);
        
        // ByteRate
        header[pos++] = (byte)(byteRate & 0xFF);
        header[pos++] = (byte)((byteRate >> 8) & 0xFF);
        header[pos++] = (byte)((byteRate >> 16) & 0xFF);
        header[pos++] = (byte)((byteRate >> 24) & 0xFF);
        
        // BlockAlign
        header[pos++] = (byte)(blockAlign & 0xFF);
        header[pos++] = (byte)((blockAlign >> 8) & 0xFF);
        
        // BitsPerSample
        header[pos++] = (byte)(bitsPerSample & 0xFF);
        header[pos++] = (byte)((bitsPerSample >> 8) & 0xFF);

        // data subchunk
        header[pos++] = (byte)'d';
        header[pos++] = (byte)'a';
        header[pos++] = (byte)'t';
        header[pos++] = (byte)'a';
        
        // DataSize
        header[pos++] = (byte)(dataSize & 0xFF);
        header[pos++] = (byte)((dataSize >> 8) & 0xFF);
        header[pos++] = (byte)((dataSize >> 16) & 0xFF);
        header[pos++] = (byte)((dataSize >> 24) & 0xFF);

        await stream.WriteAsync(header, 0, 44);
    }

    /// <summary>
    /// Writes a standard WAV file header.
    /// </summary>
    private void WriteWavHeader(BinaryWriter writer, int dataSize, int sampleRate, short bitsPerSample, short numChannels)
    {
        int byteRate = sampleRate * numChannels * (bitsPerSample / 8);
        short blockAlign = (short)(numChannels * (bitsPerSample / 8));

        // RIFF header
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataSize); // File size - 8
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        // fmt subchunk
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // Subchunk1Size (16 for PCM)
        writer.Write((short)1); // AudioFormat (1 for PCM)
        writer.Write(numChannels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        // data subchunk
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);
    }
}
