using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Audio;

/// <summary>
/// WAV-specific validation result with detailed diagnostics
/// </summary>
public class WavValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public bool HasValidHeader { get; set; }
    public int? SampleRate { get; set; }
    public short? Channels { get; set; }
    public short? BitsPerSample { get; set; }
    public long? DataSize { get; set; }
    public double? Duration { get; set; }
    public string? Format { get; set; }
}

/// <summary>
/// Specialized validator for WAV audio files with RIFF/WAVE header validation
/// </summary>
public class WavValidator
{
    private readonly ILogger<WavValidator> _logger;

    public WavValidator(ILogger<WavValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Quick validation - checks if file has valid WAV header structure
    /// </summary>
    public async Task<bool> QuickValidateAsync(string wavPath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(wavPath))
                return false;

            var fileInfo = new FileInfo(wavPath);
            if (fileInfo.Length < 44) // Minimum WAV file size (header is 44 bytes)
                return false;

            await using var stream = File.OpenRead(wavPath);
            var header = new byte[12];
            await stream.ReadAsync(header, 0, 12, ct).ConfigureAwait(false);

            // Check RIFF header
            if (header[0] != 'R' || header[1] != 'I' || header[2] != 'F' || header[3] != 'F')
                return false;

            // Check WAVE format
            if (header[8] != 'W' || header[9] != 'A' || header[10] != 'V' || header[11] != 'E')
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Quick validation failed for {Path}", wavPath);
            return false;
        }
    }

    /// <summary>
    /// Detailed validation - performs complete WAV header parsing and validation
    /// </summary>
    public async Task<WavValidationResult> ValidateAsync(string wavPath, CancellationToken ct = default)
    {
        _logger.LogInformation("Performing detailed WAV validation: {Path}", wavPath);

        var result = new WavValidationResult();

        // Check file exists
        if (!File.Exists(wavPath))
        {
            result.IsValid = false;
            result.ErrorMessage = "File does not exist";
            return result;
        }

        var fileInfo = new FileInfo(wavPath);
        if (fileInfo.Length < 44)
        {
            result.IsValid = false;
            result.ErrorMessage = $"File too small ({fileInfo.Length} bytes). WAV header requires minimum 44 bytes.";
            return result;
        }

        try
        {
            await using var stream = File.OpenRead(wavPath);
            var header = new byte[44];
            await stream.ReadAsync(header, 0, 44, ct).ConfigureAwait(false);

            // Validate RIFF header
            if (!ValidateRiffHeader(header, out var riffError))
            {
                result.IsValid = false;
                result.HasValidHeader = false;
                result.ErrorMessage = riffError;
                return result;
            }

            result.HasValidHeader = true;

            // Parse WAV format chunk
            if (!ParseWavFormat(header, out var format))
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid WAV format chunk";
                return result;
            }

            result.SampleRate = format.SampleRate;
            result.Channels = format.Channels;
            result.BitsPerSample = format.BitsPerSample;
            result.Format = $"PCM {format.BitsPerSample}-bit {(format.Channels == 1 ? "Mono" : "Stereo")}";

            // Find and validate data chunk
            var dataSize = FindDataChunkSize(stream, ct);
            if (dataSize.HasValue)
            {
                result.DataSize = dataSize.Value;
                
                // Calculate duration
                if (format.SampleRate > 0 && format.Channels > 0 && format.BitsPerSample > 0)
                {
                    var bytesPerSample = format.Channels * (format.BitsPerSample / 8);
                    var samples = dataSize.Value / bytesPerSample;
                    result.Duration = (double)samples / format.SampleRate;
                }
            }

            // Validate sample rate
            if (result.SampleRate < 8000 || result.SampleRate > 192000)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Invalid sample rate: {result.SampleRate} Hz (expected 8000-192000 Hz)";
                return result;
            }

            // Validate channels
            if (result.Channels < 1 || result.Channels > 8)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Invalid channel count: {result.Channels} (expected 1-8)";
                return result;
            }

            // Validate bits per sample
            if (result.BitsPerSample != 8 && result.BitsPerSample != 16 && 
                result.BitsPerSample != 24 && result.BitsPerSample != 32)
            {
                result.IsValid = false;
                result.ErrorMessage = $"Invalid bits per sample: {result.BitsPerSample} (expected 8, 16, 24, or 32)";
                return result;
            }

            // Validate duration if available
            if (result.Duration.HasValue && result.Duration.Value <= 0)
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid duration: audio file has no data";
                return result;
            }

            result.IsValid = true;
            _logger.LogInformation("WAV validation successful: {Format}, {SampleRate}Hz, {Duration}s",
                result.Format, result.SampleRate, result.Duration);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during WAV validation");
            result.IsValid = false;
            result.ErrorMessage = $"Validation exception: {ex.Message}";
            return result;
        }
    }

    private bool ValidateRiffHeader(byte[] header, out string? error)
    {
        error = null;

        // Check RIFF signature
        if (header[0] != 'R' || header[1] != 'I' || header[2] != 'F' || header[3] != 'F')
        {
            error = "Missing RIFF header signature";
            return false;
        }

        // Check WAVE format
        if (header[8] != 'W' || header[9] != 'A' || header[10] != 'V' || header[11] != 'E')
        {
            error = "Missing WAVE format identifier";
            return false;
        }

        // Check fmt chunk signature
        if (header[12] != 'f' || header[13] != 'm' || header[14] != 't' || header[15] != ' ')
        {
            error = "Missing fmt chunk signature";
            return false;
        }

        return true;
    }

    private bool ParseWavFormat(byte[] header, out WavFormat format)
    {
        format = new WavFormat();

        try
        {
            // Audio format (offset 20-21): 1 = PCM
            var audioFormat = BitConverter.ToInt16(header, 20);
            if (audioFormat != 1) // Only support PCM for now
            {
                return false;
            }

            // Number of channels (offset 22-23)
            format.Channels = BitConverter.ToInt16(header, 22);

            // Sample rate (offset 24-27)
            format.SampleRate = BitConverter.ToInt32(header, 24);

            // Bits per sample (offset 34-35)
            format.BitsPerSample = BitConverter.ToInt16(header, 34);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private long? FindDataChunkSize(FileStream stream, CancellationToken ct)
    {
        try
        {
            // Start after the format chunk
            stream.Seek(36, SeekOrigin.Begin);

            var chunkHeader = new byte[8];
            while (stream.Position < stream.Length - 8)
            {
                var read = stream.Read(chunkHeader, 0, 8);
                if (read < 8)
                    break;

                // Check if this is the data chunk
                if (chunkHeader[0] == 'd' && chunkHeader[1] == 'a' && 
                    chunkHeader[2] == 't' && chunkHeader[3] == 'a')
                {
                    // Read chunk size (4 bytes, little-endian)
                    var size = BitConverter.ToInt32(chunkHeader, 4);
                    return size;
                }

                // Skip to next chunk
                var chunkSize = BitConverter.ToInt32(chunkHeader, 4);
                stream.Seek(chunkSize, SeekOrigin.Current);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private struct WavFormat
    {
        public short Channels { get; set; }
        public int SampleRate { get; set; }
        public short BitsPerSample { get; set; }
    }
}
