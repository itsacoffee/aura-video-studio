using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Audio;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class AudioValidatorTests
{
    private readonly AudioValidator _validator;
    private readonly string _testDataDir;

    public AudioValidatorTests()
    {
        var logger = NullLogger<AudioValidator>.Instance;
        _validator = new AudioValidator(logger);
        _testDataDir = Path.Combine(Path.GetTempPath(), "AuraTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDataDir);
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
        Assert.False(result.IsCorrupted);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnInvalid_WhenFileTooSmall()
    {
        // Arrange
        var tinyFilePath = Path.Combine(_testDataDir, "tiny.wav");
        await File.WriteAllTextAsync(tinyFilePath, "tiny"); // Only 4 bytes

        // Act
        var result = await _validator.ValidateAsync(tinyFilePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("too small", result.ErrorMessage);
        Assert.True(result.IsCorrupted);

        // Cleanup
        File.Delete(tinyFilePath);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnInvalid_WhenFileIsZeroLength()
    {
        // Arrange
        var emptyFilePath = Path.Combine(_testDataDir, "empty.wav");
        File.WriteAllText(emptyFilePath, string.Empty);

        // Act
        var result = await _validator.ValidateAsync(emptyFilePath);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("too small", result.ErrorMessage);
        Assert.True(result.IsCorrupted);

        // Cleanup
        File.Delete(emptyFilePath);
    }

    [Fact]
    public async Task ValidateAsync_Should_ReturnValid_WhenNoValidationToolsAvailable()
    {
        // Arrange - create a file > 128 bytes
        var filePath = Path.Combine(_testDataDir, "basic.wav");
        await File.WriteAllTextAsync(filePath, new string('x', 200));

        // Act - validator with no ffmpeg/ffprobe
        var result = await _validator.ValidateAsync(filePath);

        // Assert - should pass basic check
        Assert.True(result.IsValid);
        Assert.Contains("No ffprobe/ffmpeg", result.ErrorMessage ?? "");

        // Cleanup
        File.Delete(filePath);
    }

    [Fact]
    public async Task ReencodeAsync_Should_ReturnFalse_WhenFfmpegNotAvailable()
    {
        // Arrange
        var inputPath = Path.Combine(_testDataDir, "input.wav");
        var outputPath = Path.Combine(_testDataDir, "output.wav");
        await File.WriteAllTextAsync(inputPath, new string('x', 200));

        // Act
        var (success, errorMessage) = await _validator.ReencodeAsync(inputPath, outputPath);

        // Assert
        Assert.False(success);
        Assert.Contains("not available", errorMessage);

        // Cleanup
        if (File.Exists(inputPath)) File.Delete(inputPath);
        if (File.Exists(outputPath)) File.Delete(outputPath);
    }

    [Fact]
    public async Task GenerateSilentWavAsync_Should_ReturnFalse_WhenFfmpegNotAvailable()
    {
        // Arrange
        var outputPath = Path.Combine(_testDataDir, "silent.wav");

        // Act
        var (success, errorMessage) = await _validator.GenerateSilentWavAsync(outputPath, 1.0);

        // Assert
        Assert.False(success);
        Assert.Contains("not available", errorMessage);

        // Cleanup
        if (File.Exists(outputPath)) File.Delete(outputPath);
    }

    [Fact]
    public void AudioValidationResult_Should_HavePropertiesSet()
    {
        // Arrange & Act
        var result = new AudioValidationResult
        {
            IsValid = true,
            Duration = 10.5,
            BitRate = 128000,
            Format = "wav",
            CodecName = "pcm_s16le"
        };

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(10.5, result.Duration);
        Assert.Equal(128000, result.BitRate);
        Assert.Equal("wav", result.Format);
        Assert.Equal("pcm_s16le", result.CodecName);
    }

    // Integration tests with actual ffmpeg would go here if ffmpeg is available in test environment
    // These would test ValidateWithFfprobeAsync, ValidateWithFfmpegAsync, ReencodeAsync, etc.
    // For now, we test the API surface and error handling without dependencies
}
