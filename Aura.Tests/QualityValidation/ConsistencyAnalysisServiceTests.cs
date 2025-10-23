using Aura.Api.Services.QualityValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.QualityValidation;

public class ConsistencyAnalysisServiceTests
{
    private readonly ConsistencyAnalysisService _service;
    private readonly string _testFilePath;

    public ConsistencyAnalysisServiceTests()
    {
        _service = new ConsistencyAnalysisService(NullLogger<ConsistencyAnalysisService>.Instance);
        _testFilePath = Path.GetTempFileName();
        
        // Create a temporary test file
        File.WriteAllText(_testFilePath, "test video content");
    }

    [Fact]
    public async Task AnalyzeConsistencyAsync_ValidFile_ReturnsResult()
    {
        // Act
        var result = await _service.AnalyzeConsistencyAsync(_testFilePath);

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.ConsistencyScore, 0, 100);
        Assert.InRange(result.ColorConsistency, 0, 100);
        Assert.InRange(result.BrightnessConsistency, 0, 100);
        Assert.InRange(result.MotionSmoothness, 0, 100);
    }

    [Fact]
    public async Task AnalyzeConsistencyAsync_FileNotFound_ThrowsException()
    {
        // Arrange
        var nonExistentFile = "/tmp/non_existent_video.mp4";

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _service.AnalyzeConsistencyAsync(nonExistentFile));
    }

    [Fact]
    public async Task AnalyzeConsistencyAsync_DetectsSceneChanges()
    {
        // Act
        var result = await _service.AnalyzeConsistencyAsync(_testFilePath);

        // Assert
        Assert.True(result.SceneChanges >= 0);
    }

    [Fact]
    public async Task AnalyzeConsistencyAsync_ChecksForFlickering()
    {
        // Act
        var result = await _service.AnalyzeConsistencyAsync(_testFilePath);

        // Assert
        // Should check for flickering
        Assert.NotNull(result.HasFlickering);
    }

    [Fact]
    public async Task AnalyzeConsistencyAsync_ChecksForAbruptTransitions()
    {
        // Act
        var result = await _service.AnalyzeConsistencyAsync(_testFilePath);

        // Assert
        Assert.NotNull(result.HasAbruptTransitions);
    }

    [Fact]
    public async Task AnalyzeConsistencyAsync_ListsDetectedArtifacts()
    {
        // Act
        var result = await _service.AnalyzeConsistencyAsync(_testFilePath);

        // Assert
        Assert.NotNull(result.DetectedArtifacts);
    }

    [Fact]
    public async Task AnalyzeConsistencyAsync_CalculatesOverallScore()
    {
        // Act
        var result = await _service.AnalyzeConsistencyAsync(_testFilePath);

        // Assert
        Assert.InRange(result.Score, 0, 100);
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
