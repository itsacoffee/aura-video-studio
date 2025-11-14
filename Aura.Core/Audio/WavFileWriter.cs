using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Audio;

/// <summary>
/// Thread-safe WAV file writer with atomic writes and silent WAV generation
/// Ensures no zero-byte or corrupted WAV files are created
/// </summary>
public class WavFileWriter
{
    private readonly ILogger<WavFileWriter> _logger;
    private const int DefaultSampleRate = 48000;
    private const short DefaultChannels = 2;
    private const short DefaultBitsPerSample = 16;

    public WavFileWriter(ILogger<WavFileWriter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Write audio data to WAV file atomically
    /// File is written to a .part file first, then renamed on success
    /// </summary>
    /// <param name="outputPath">Final output path</param>
    /// <param name="audioData">PCM audio samples (16-bit signed)</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="bitsPerSample">Bits per sample</param>
    /// <param name="ct">Cancellation token</param>
    public async Task WriteAsync(
        string outputPath,
        short[] audioData,
        int sampleRate = DefaultSampleRate,
        short channels = DefaultChannels,
        short bitsPerSample = DefaultBitsPerSample,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(outputPath))
            throw new ArgumentNullException(nameof(outputPath));
        
        if (audioData == null || audioData.Length == 0)
            throw new ArgumentException("Audio data cannot be null or empty", nameof(audioData));

        _logger.LogInformation("Writing WAV file: {Path} ({Samples} samples, {Rate}Hz, {Channels}ch)", 
            outputPath, audioData.Length, sampleRate, channels);

        var partPath = outputPath + ".part";
        
        try
        {
            // Ensure directory exists
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Write to .part file
            await using (var fileStream = new FileStream(partPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await using var writer = new BinaryWriter(fileStream);
                WriteWavHeader(writer, audioData.Length, sampleRate, channels, bitsPerSample);
                
                // Write audio data
                foreach (var sample in audioData)
                {
                    ct.ThrowIfCancellationRequested();
                    writer.Write(sample);
                }
                
                // Ensure all data is flushed
                await fileStream.FlushAsync(ct).ConfigureAwait(false);
            }

            // Atomic rename - delete existing file first if present
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
            File.Move(partPath, outputPath);

            _logger.LogInformation("Successfully wrote WAV file: {Path} ({Size} bytes)", 
                outputPath, new FileInfo(outputPath).Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write WAV file: {Path}", outputPath);
            
            // Clean up partial file
            if (File.Exists(partPath))
            {
                try
                {
                    File.Delete(partPath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up partial file: {Path}", partPath);
                }
            }
            
            throw;
        }
    }

    /// <summary>
    /// Generate a silent WAV file of specified duration
    /// Useful as fallback when TTS fails
    /// </summary>
    /// <param name="outputPath">Output file path</param>
    /// <param name="durationSeconds">Duration in seconds</param>
    /// <param name="sampleRate">Sample rate in Hz</param>
    /// <param name="channels">Number of channels</param>
    /// <param name="ct">Cancellation token</param>
    public async Task GenerateSilenceAsync(
        string outputPath,
        double durationSeconds,
        int sampleRate = DefaultSampleRate,
        short channels = DefaultChannels,
        CancellationToken ct = default)
    {
        if (durationSeconds <= 0)
            throw new ArgumentException("Duration must be positive", nameof(durationSeconds));

        _logger.LogInformation("Generating silent WAV: {Duration}s -> {Path}", durationSeconds, outputPath);

        int totalSamples = (int)(durationSeconds * sampleRate * channels);
        var silentData = new short[totalSamples]; // All zeros = silence

        await WriteAsync(outputPath, silentData, sampleRate, channels, DefaultBitsPerSample, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate a silent WAV file with minimum duration to ensure valid playback
    /// </summary>
    public async Task GenerateMinimalSilenceAsync(
        string outputPath,
        CancellationToken ct = default)
    {
        const double minDuration = 0.1; // 100ms minimum
        await GenerateSilenceAsync(outputPath, minDuration, ct: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Write WAV file header (RIFF + fmt + data chunks)
    /// </summary>
    private void WriteWavHeader(BinaryWriter writer, int sampleCount, int sampleRate, short channels, short bitsPerSample)
    {
        int dataSize = sampleCount * sizeof(short);
        int byteRate = sampleRate * channels * (bitsPerSample / 8);
        short blockAlign = (short)(channels * (bitsPerSample / 8));

        // RIFF header
        writer.Write(new[] { 'R', 'I', 'F', 'F' });
        writer.Write(36 + dataSize); // ChunkSize
        writer.Write(new[] { 'W', 'A', 'V', 'E' });

        // fmt subchunk
        writer.Write(new[] { 'f', 'm', 't', ' ' });
        writer.Write(16); // Subchunk1Size (16 for PCM)
        writer.Write((short)1); // AudioFormat (1 = PCM)
        writer.Write(channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write(bitsPerSample);

        // data subchunk
        writer.Write(new[] { 'd', 'a', 't', 'a' });
        writer.Write(dataSize);
    }

    /// <summary>
    /// Validate that a WAV file is properly formatted and not corrupted
    /// </summary>
    public bool ValidateWavFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("WAV file does not exist: {Path}", filePath);
                return false;
            }

            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length < 44) // Minimum WAV file size (header only)
            {
                _logger.LogWarning("WAV file too small ({Size} bytes): {Path}", fileInfo.Length, filePath);
                return false;
            }

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            // Check RIFF header
            var riff = new string(reader.ReadChars(4));
            if (riff != "RIFF")
            {
                _logger.LogWarning("Invalid RIFF header in WAV file: {Path}", filePath);
                return false;
            }

            reader.ReadInt32(); // File size

            var wave = new string(reader.ReadChars(4));
            if (wave != "WAVE")
            {
                _logger.LogWarning("Invalid WAVE header in WAV file: {Path}", filePath);
                return false;
            }

            _logger.LogDebug("WAV file validated successfully: {Path}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating WAV file: {Path}", filePath);
            return false;
        }
    }
}
