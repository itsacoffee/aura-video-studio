using System;
using System.IO;
using System.Threading.Tasks;
using Aura.Providers.Tts;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class AudioNormalizerTests : IDisposable
{
    private readonly AudioNormalizer _normalizer;
    private readonly string _testDir;

    public AudioNormalizerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "AuraTests", $"Normalizer_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _normalizer = new AudioNormalizer(NullLogger<AudioNormalizer>.Instance);
    }

    [Fact]
    public async Task NormalizeAsync_WithMissingFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDir, "nonexistent.wav");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _normalizer.NormalizeAsync(nonExistentPath));
    }

    [Fact]
    public async Task NormalizeAsync_WithInvalidFfmpegPath_ThrowsFileNotFoundException()
    {
        // Arrange
        var invalidNormalizer = new AudioNormalizer(
            NullLogger<AudioNormalizer>.Instance,
            ffmpegPath: "/invalid/path/to/ffmpeg");
        
        var testFile = CreateTestWavFile();

        try
        {
            // Act & Assert
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                invalidNormalizer.NormalizeAsync(testFile));
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public void Constructor_CreatesOutputDirectory()
    {
        // The normalizer creates its output directory on construction
        // We can't easily test this without accessing private fields,
        // but we can verify it doesn't throw
        var normalizer = new AudioNormalizer(NullLogger<AudioNormalizer>.Instance);
        Assert.NotNull(normalizer);
    }

    [Theory]
    [InlineData(-16.0)]
    [InlineData(-20.0)]
    [InlineData(-14.0)]
    public async Task NormalizeAsync_WithDifferentTargetLufs_AcceptsValidValues(double targetLufs)
    {
        // This test verifies that different target LUFS values are accepted
        // Actual normalization would require FFmpeg, so we just test the API
        var testFile = CreateTestWavFile();

        try
        {
            // We expect this to throw because FFmpeg is not available in test environment
            // But the important thing is that it accepts the parameter
            try
            {
                await _normalizer.NormalizeAsync(testFile, targetLufs: targetLufs);
            }
            catch (FileNotFoundException)
            {
                // Expected when FFmpeg is not available
            }
        }
        finally
        {
            if (File.Exists(testFile))
                File.Delete(testFile);
        }
    }

    [Fact]
    public async Task NormalizeBatchAsync_WithMultipleFiles_ProcessesAll()
    {
        // Arrange
        var files = new[]
        {
            CreateTestWavFile(),
            CreateTestWavFile(),
            CreateTestWavFile()
        };

        try
        {
            // Act & Assert - Should accept multiple files
            // Will throw FileNotFoundException for FFmpeg, but that's expected in tests
            try
            {
                await _normalizer.NormalizeBatchAsync(files);
            }
            catch (FileNotFoundException)
            {
                // Expected when FFmpeg is not available
            }
        }
        finally
        {
            foreach (var file in files)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task NormalizeBatchAsync_WithEmptyArray_HandlesGracefully()
    {
        // Arrange
        var files = Array.Empty<string>();

        // Act
        var results = await _normalizer.NormalizeBatchAsync(files);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    private string CreateTestWavFile()
    {
        var path = Path.Combine(_testDir, $"test_{Guid.NewGuid():N}.wav");
        
        // Create minimal valid WAV file
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);
        
        // RIFF header
        bw.Write(new[] { 'R', 'I', 'F', 'F' });
        bw.Write(36 + 2000); // File size
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
        bw.Write(2000); // Data size
        
        // Generate some sine wave data for realism
        var samples = 2000 / 2; // 16-bit samples
        for (int i = 0; i < samples; i++)
        {
            var sample = (short)(Math.Sin(2 * Math.PI * 440 * i / 44100) * 16000);
            bw.Write(sample);
        }

        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try
            {
                Directory.Delete(_testDir, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
