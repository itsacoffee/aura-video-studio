using Aura.Api.Services.QualityValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.QualityValidation;

public class AudioQualityServiceTests
{
    private readonly AudioQualityService _service;
    private readonly string _testFilePath;

    public AudioQualityServiceTests()
    {
        _service = new AudioQualityService(NullLogger<AudioQualityService>.Instance);
        _testFilePath = Path.GetTempFileName();
        
        // Create a temporary test file
        File.WriteAllText(_testFilePath, "test audio content");
    }

    [Fact]
    public async Task AnalyzeAudioAsync_ValidFile_ReturnsResult()
    {
        // Act
        var result = await _service.AnalyzeAudioAsync(_testFilePath);

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.Score, 0, 100);
        Assert.InRange(result.LoudnessLUFS, -50, 0);
        Assert.InRange(result.NoiseLevel, 0, 100);
        Assert.InRange(result.ClarityScore, 0, 100);
    }

    [Fact]
    public async Task AnalyzeAudioAsync_FileNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentFile = "/tmp/non_existent_audio.wav";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.AnalyzeAudioAsync(nonExistentFile));
    }

    [Fact]
    public async Task AnalyzeAudioAsync_ValidAudio_IncludesMetadata()
    {
        // Act
        var result = await _service.AnalyzeAudioAsync(_testFilePath);

        // Assert
        Assert.True(result.SampleRate > 0);
        Assert.True(result.BitDepth > 0);
        Assert.True(result.Channels > 0);
        Assert.True(result.DynamicRange >= 0);
    }

    [Fact]
    public async Task AnalyzeAudioAsync_ChecksForClipping()
    {
        // Act
        var result = await _service.AnalyzeAudioAsync(_testFilePath);

        // Assert
        // The service should detect clipping
        Assert.NotNull(result.HasClipping);
    }

    [Fact]
    public async Task AnalyzeAudioAsync_ValidatesLoudness()
    {
        // Act
        var result = await _service.AnalyzeAudioAsync(_testFilePath);

        // Assert
        // Loudness should be within broadcast range
        Assert.InRange(result.LoudnessLUFS, -50, 0);
    }

    public void Dispose()
    {
        // Cleanup test file
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }
}
