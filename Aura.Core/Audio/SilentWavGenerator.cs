using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Audio;

/// <summary>
/// Generates silent WAV audio files with specified duration and format.
/// Used as fallback when TTS providers fail or are unavailable.
/// </summary>
public class SilentWavGenerator
{
    private readonly ILogger<SilentWavGenerator> _logger;

    public SilentWavGenerator(ILogger<SilentWavGenerator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generates a silent WAV file with specified parameters.
    /// </summary>
    /// <param name="outputPath">Path where the WAV file will be written</param>
    /// <param name="duration">Duration of the silent audio</param>
    /// <param name="sampleRate">Sample rate in Hz (default: 44100)</param>
    /// <param name="channels">Number of channels (default: 1 for mono)</param>
    /// <param name="bitsPerSample">Bits per sample (default: 16)</param>
    /// <param name="ct">Cancellation token</param>
    public async Task GenerateAsync(
        string outputPath,
        TimeSpan duration,
        int sampleRate = 44100,
        short channels = 1,
        short bitsPerSample = 16,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {Duration}s of silent audio at {SampleRate}Hz to {Path}", 
            duration.TotalSeconds, sampleRate, outputPath);

        // Validate parameters
        if (duration.TotalSeconds <= 0)
        {
            throw new ArgumentException("Duration must be positive", nameof(duration));
        }

        if (sampleRate < 8000 || sampleRate > 192000)
        {
            throw new ArgumentException("Sample rate must be between 8000 and 192000 Hz", nameof(sampleRate));
        }

        if (channels < 1 || channels > 8)
        {
            throw new ArgumentException("Channels must be between 1 and 8", nameof(channels));
        }

        if (bitsPerSample != 8 && bitsPerSample != 16 && bitsPerSample != 24 && bitsPerSample != 32)
        {
            throw new ArgumentException("Bits per sample must be 8, 16, 24, or 32", nameof(bitsPerSample));
        }

        // Calculate sizes
        int numSamples = (int)(duration.TotalSeconds * sampleRate);
        int bytesPerSample = channels * (bitsPerSample / 8);
        int dataSize = numSamples * bytesPerSample;

        // Create output directory if needed
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to temp file first for atomic operation
        string tempPath = outputPath + ".tmp";

        try
        {
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await using var writer = new BinaryWriter(fileStream);

            // Write WAV header
            WriteWavHeader(writer, dataSize, sampleRate, bitsPerSample, channels);

            // Write silence (zeros) in chunks for efficiency
            byte[] buffer = new byte[8192];
            int remaining = dataSize;
            while (remaining > 0)
            {
                ct.ThrowIfCancellationRequested();

                int toWrite = Math.Min(remaining, buffer.Length);
                writer.Write(buffer, 0, toWrite);
                remaining -= toWrite;
            }

            await fileStream.FlushAsync(ct);

            // Close streams before rename
            await writer.DisposeAsync();
            await fileStream.DisposeAsync();

            // Atomic rename
            File.Move(tempPath, outputPath, overwrite: true);

            _logger.LogInformation("Generated silent audio: {Path}, Duration: {Duration}s, Size: {Size} bytes",
                outputPath, duration.TotalSeconds, dataSize + 44);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate silent audio");

            // Clean up temp file on error
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }

            throw;
        }
    }

    /// <summary>
    /// Generates a silent WAV file with 1 second duration (factory method).
    /// </summary>
    public Task GenerateOneSecondAsync(string outputPath, CancellationToken ct = default)
    {
        return GenerateAsync(outputPath, TimeSpan.FromSeconds(1), ct: ct);
    }

    /// <summary>
    /// Generates a silent WAV file with 5 seconds duration (factory method).
    /// </summary>
    public Task GenerateFiveSecondsAsync(string outputPath, CancellationToken ct = default)
    {
        return GenerateAsync(outputPath, TimeSpan.FromSeconds(5), ct: ct);
    }

    /// <summary>
    /// Generates a silent WAV file with 10 seconds duration (factory method).
    /// </summary>
    public Task GenerateTenSecondsAsync(string outputPath, CancellationToken ct = default)
    {
        return GenerateAsync(outputPath, TimeSpan.FromSeconds(10), ct: ct);
    }

    /// <summary>
    /// Writes a standard WAV file header.
    /// </summary>
    private static void WriteWavHeader(BinaryWriter writer, int dataSize, int sampleRate, short bitsPerSample, short numChannels)
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
