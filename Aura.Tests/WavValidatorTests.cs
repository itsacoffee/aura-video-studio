using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class WavValidatorTests : IDisposable
{
    private readonly WavValidator _validator;
    private readonly string _testDataDir;

    public WavValidatorTests()
    {
        var logger = NullLogger<WavValidator>.Instance;
        _validator = new WavValidator(logger);
        _testDataDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDataDir);
    }

    [Fact]
    public async Task QuickValidateAsync_Should_ReturnFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataDir, "nonexistent.wav");

        // Act
        var result = await _validator.QuickValidateAsync(nonExistentPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task QuickValidateAsync_Should_ReturnFalse_WhenFileTooSmall()
    {
        // Arrange
        var tinyFilePath = Path.Combine(_testDataDir, "tiny.wav");
        await File.WriteAllTextAsync(tinyFilePath, "tiny"); // Only 4 bytes

        // Act
        var result = await _validator.QuickValidateAsync(tinyFilePath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task QuickValidateAsync_Should_ReturnFalse_WhenMissingRiffHeader()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDataDir, "invalid.wav");
        var invalidData = new byte[50];
        // Fill with non-RIFF data
        Array.Fill<byte>(invalidData, 0xFF);
        await File.WriteAllBytesAsync(invalidPath, invalidData);

        // Act
        var result = await _validator.QuickValidateAsync(invalidPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task QuickValidateAsync_Should_ReturnTrue_WithValidWavHeader()
    {
        // Arrange
        var validPath = Path.Combine(_testDataDir, "valid.wav");
        var validWav = CreateMinimalValidWavFile();
        await File.WriteAllBytesAsync(validPath, validWav);

        // Act
        var result = await _validator.QuickValidateAsync(validPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnInvalid_WhenFileDoesNotExist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDataDir, "nonexistent.wav");

        // Act
        var result = await _validator.ValidateAsync(nonExistentPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnInvalid_WhenFileTooSmall()
    {
        // Arrange
        var tinyFilePath = Path.Combine(_testDataDir, "tiny.wav");
        await File.WriteAllTextAsync(tinyFilePath, "tiny");

        // Act
        var result = await _validator.ValidateAsync(tinyFilePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("too small", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnInvalid_WhenMissingRiffHeader()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDataDir, "no_riff.wav");
        var invalidData = new byte[50];
        Array.Fill<byte>(invalidData, 0xFF);
        await File.WriteAllBytesAsync(invalidPath, invalidData);

        // Act
        var result = await _validator.ValidateAsync(invalidPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.HasValidHeader);
        Assert.Contains("RIFF", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnInvalid_WhenMissingWaveFormat()
    {
        // Arrange
        var invalidPath = Path.Combine(_testDataDir, "no_wave.wav");
        var invalidData = new byte[50];
        // Add RIFF but not WAVE
        invalidData[0] = (byte)'R';
        invalidData[1] = (byte)'I';
        invalidData[2] = (byte)'F';
        invalidData[3] = (byte)'F';
        // Fill size (4 bytes)
        BitConverter.GetBytes(42).CopyTo(invalidData, 4);
        // Wrong format identifier
        invalidData[8] = (byte)'X';
        invalidData[9] = (byte)'X';
        invalidData[10] = (byte)'X';
        invalidData[11] = (byte)'X';
        await File.WriteAllBytesAsync(invalidPath, invalidData);

        // Act
        var result = await _validator.ValidateAsync(invalidPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.False(result.HasValidHeader);
        Assert.Contains("WAVE", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateAsync_Should_ParseValidWavFile()
    {
        // Arrange
        var validPath = Path.Combine(_testDataDir, "valid_parse.wav");
        var validWav = CreateValidWavFile(
            sampleRate: 48000,
            channels: 2,
            bitsPerSample: 16,
            durationSeconds: 1.0);
        await File.WriteAllBytesAsync(validPath, validWav);

        // Act
        var result = await _validator.ValidateAsync(validPath);

        // Assert
        Assert.True(result.IsValid, $"Expected valid, got: {result.ErrorMessage}");
        Assert.True(result.HasValidHeader);
        Assert.Equal(48000, result.SampleRate.GetValueOrDefault());
        Assert.Equal((short)2, result.Channels.GetValueOrDefault());
        Assert.Equal((short)16, result.BitsPerSample.GetValueOrDefault());
        Assert.NotNull(result.Format);
        Assert.Contains("PCM", result.Format);
        Assert.NotNull(result.Duration);
        Assert.True(result.Duration > 0.9 && result.Duration < 1.1); // Roughly 1 second
    }

    [Fact]
    public async Task ValidateAsync_Should_RejectInvalidSampleRate()
    {
        // Arrange - create WAV with invalid sample rate
        var invalidPath = Path.Combine(_testDataDir, "invalid_rate.wav");
        var invalidWav = CreateValidWavFile(
            sampleRate: 5000, // Too low (< 8000)
            channels: 2,
            bitsPerSample: 16,
            durationSeconds: 1.0);
        await File.WriteAllBytesAsync(invalidPath, invalidWav);

        // Act
        var result = await _validator.ValidateAsync(invalidPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("sample rate", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_Should_RejectInvalidChannelCount()
    {
        // Arrange - create WAV with invalid channel count
        var invalidPath = Path.Combine(_testDataDir, "invalid_channels.wav");
        var invalidWav = CreateValidWavFile(
            sampleRate: 48000,
            channels: 0, // Invalid
            bitsPerSample: 16,
            durationSeconds: 1.0);
        await File.WriteAllBytesAsync(invalidPath, invalidWav);

        // Act
        var result = await _validator.ValidateAsync(invalidPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("channel", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_Should_RejectInvalidBitsPerSample()
    {
        // Arrange - create WAV with invalid bits per sample
        var invalidPath = Path.Combine(_testDataDir, "invalid_bits.wav");
        var invalidWav = CreateValidWavFile(
            sampleRate: 48000,
            channels: 2,
            bitsPerSample: 13, // Invalid (not 8, 16, 24, or 32)
            durationSeconds: 1.0);
        await File.WriteAllBytesAsync(invalidPath, invalidWav);

        // Act
        var result = await _validator.ValidateAsync(invalidPath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("bits per sample", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_Should_AcceptValidMonoWav()
    {
        // Arrange
        var monoPath = Path.Combine(_testDataDir, "mono.wav");
        var monoWav = CreateValidWavFile(
            sampleRate: 44100,
            channels: 1, // Mono
            bitsPerSample: 16,
            durationSeconds: 0.5);
        await File.WriteAllBytesAsync(monoPath, monoWav);

        // Act
        var result = await _validator.ValidateAsync(monoPath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal((short)1, result.Channels.GetValueOrDefault());
        Assert.Contains("Mono", result.Format);
    }

    [Fact]
    public async Task ValidateAsync_Should_AcceptValidStereoWav()
    {
        // Arrange
        var stereoPath = Path.Combine(_testDataDir, "stereo.wav");
        var stereoWav = CreateValidWavFile(
            sampleRate: 44100,
            channels: 2, // Stereo
            bitsPerSample: 16,
            durationSeconds: 0.5);
        await File.WriteAllBytesAsync(stereoPath, stereoWav);

        // Act
        var result = await _validator.ValidateAsync(stereoPath);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal((short)2, result.Channels.GetValueOrDefault());
        Assert.Contains("Stereo", result.Format);
    }

    // Helper method to create a minimal valid WAV file (just header, no data)
    private byte[] CreateMinimalValidWavFile()
    {
        var wav = new byte[44];
        
        // RIFF header
        wav[0] = (byte)'R';
        wav[1] = (byte)'I';
        wav[2] = (byte)'F';
        wav[3] = (byte)'F';
        
        // File size - 8 (placeholder)
        BitConverter.GetBytes(36).CopyTo(wav, 4);
        
        // WAVE format
        wav[8] = (byte)'W';
        wav[9] = (byte)'A';
        wav[10] = (byte)'V';
        wav[11] = (byte)'E';
        
        // fmt chunk
        wav[12] = (byte)'f';
        wav[13] = (byte)'m';
        wav[14] = (byte)'t';
        wav[15] = (byte)' ';
        
        // fmt chunk size (16 for PCM)
        BitConverter.GetBytes(16).CopyTo(wav, 16);
        
        // Audio format (1 = PCM)
        BitConverter.GetBytes((short)1).CopyTo(wav, 20);
        
        // Channels (2 = stereo)
        BitConverter.GetBytes((short)2).CopyTo(wav, 22);
        
        // Sample rate (48000)
        BitConverter.GetBytes(48000).CopyTo(wav, 24);
        
        // Byte rate (sample rate * channels * bits per sample / 8)
        BitConverter.GetBytes(48000 * 2 * 16 / 8).CopyTo(wav, 28);
        
        // Block align (channels * bits per sample / 8)
        BitConverter.GetBytes((short)(2 * 16 / 8)).CopyTo(wav, 32);
        
        // Bits per sample (16)
        BitConverter.GetBytes((short)16).CopyTo(wav, 34);
        
        // data chunk header
        wav[36] = (byte)'d';
        wav[37] = (byte)'a';
        wav[38] = (byte)'t';
        wav[39] = (byte)'a';
        
        // data chunk size (0)
        BitConverter.GetBytes(0).CopyTo(wav, 40);
        
        return wav;
    }

    // Helper method to create a valid WAV file with specified parameters
    private byte[] CreateValidWavFile(int sampleRate, short channels, short bitsPerSample, double durationSeconds)
    {
        var samplesPerChannel = (int)(sampleRate * durationSeconds);
        var bytesPerSample = channels * (bitsPerSample / 8);
        var dataSize = samplesPerChannel * bytesPerSample;
        var totalSize = 44 + dataSize;
        
        var wav = new byte[totalSize];
        
        // RIFF header
        wav[0] = (byte)'R';
        wav[1] = (byte)'I';
        wav[2] = (byte)'F';
        wav[3] = (byte)'F';
        
        // File size - 8
        BitConverter.GetBytes(totalSize - 8).CopyTo(wav, 4);
        
        // WAVE format
        wav[8] = (byte)'W';
        wav[9] = (byte)'A';
        wav[10] = (byte)'V';
        wav[11] = (byte)'E';
        
        // fmt chunk
        wav[12] = (byte)'f';
        wav[13] = (byte)'m';
        wav[14] = (byte)'t';
        wav[15] = (byte)' ';
        
        // fmt chunk size (16 for PCM)
        BitConverter.GetBytes(16).CopyTo(wav, 16);
        
        // Audio format (1 = PCM)
        BitConverter.GetBytes((short)1).CopyTo(wav, 20);
        
        // Channels
        BitConverter.GetBytes(channels).CopyTo(wav, 22);
        
        // Sample rate
        BitConverter.GetBytes(sampleRate).CopyTo(wav, 24);
        
        // Byte rate
        BitConverter.GetBytes(sampleRate * channels * (bitsPerSample / 8)).CopyTo(wav, 28);
        
        // Block align
        BitConverter.GetBytes((short)(channels * (bitsPerSample / 8))).CopyTo(wav, 32);
        
        // Bits per sample
        BitConverter.GetBytes(bitsPerSample).CopyTo(wav, 34);
        
        // data chunk header
        wav[36] = (byte)'d';
        wav[37] = (byte)'a';
        wav[38] = (byte)'t';
        wav[39] = (byte)'a';
        
        // data chunk size
        BitConverter.GetBytes(dataSize).CopyTo(wav, 40);
        
        // Fill data with silence (zeros)
        // Already zeros by default
        
        return wav;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDataDir))
            {
                Directory.Delete(_testDataDir, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
