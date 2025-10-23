using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Services.VideoOptimization;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.VideoOptimization;

public class AttentionPredictionServiceTests
{
    private readonly Mock<ILogger<AttentionPredictionService>> _mockLogger;
    private readonly AttentionPredictionService _service;

    public AttentionPredictionServiceTests()
    {
        _mockLogger = new Mock<ILogger<AttentionPredictionService>>();
        _service = new AttentionPredictionService(_mockLogger.Object);
    }

    [Fact]
    public async Task PredictAttentionAsync_WithValidScenes_ReturnsAttentionPrediction()
    {
        // Arrange
        var scenes = CreateTestScenes(5);
        var options = new PredictionOptions();

        // Act
        var result = await _service.PredictAttentionAsync(scenes, options);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.DataPoints);
        Assert.True(result.OverallEngagement >= 0 && result.OverallEngagement <= 1);
        Assert.True(result.PredictedRetentionRate >= 0 && result.PredictedRetentionRate <= 1);
    }

    [Fact]
    public async Task PredictAttentionAsync_WithEmptyScenes_ThrowsArgumentException()
    {
        // Arrange
        var scenes = new List<Scene>();
        var options = new PredictionOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.PredictAttentionAsync(scenes, options));
    }

    [Fact]
    public async Task PredictAttentionAsync_WithNullScenes_ThrowsArgumentNullException()
    {
        // Arrange
        List<Scene>? scenes = null;
        var options = new PredictionOptions();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.PredictAttentionAsync(scenes!, options));
    }

    [Fact]
    public async Task PredictAttentionAsync_DetectsEngagementDrops()
    {
        // Arrange
        var scenes = CreateTestScenes(10);
        var options = new PredictionOptions(EngagementDropThreshold: 0.7);

        // Act
        var result = await _service.PredictAttentionAsync(scenes, options);

        // Assert
        Assert.NotNull(result.EngagementDrops);
        // Some scenes should have drops below threshold
        Assert.All(result.EngagementDrops, drop =>
        {
            Assert.True(drop.PredictedEngagement < 0.7);
            Assert.NotEmpty(drop.Recommendation);
        });
    }

    [Fact]
    public async Task PredictAttentionAsync_OpeningScenesHaveHigherEngagement()
    {
        // Arrange
        var scenes = CreateTestScenes(5);
        var options = new PredictionOptions();

        // Act
        var result = await _service.PredictAttentionAsync(scenes, options);

        // Assert
        var openingDataPoints = result.DataPoints.Where(dp => dp.SceneIndex == 0).ToList();
        var middleDataPoints = result.DataPoints.Where(dp => dp.SceneIndex == 2).ToList();
        
        var avgOpeningEngagement = openingDataPoints.Average(dp => dp.EngagementLevel);
        var avgMiddleEngagement = middleDataPoints.Average(dp => dp.EngagementLevel);
        
        // Opening should generally have higher engagement
        Assert.True(avgOpeningEngagement >= avgMiddleEngagement - 0.1);
    }

    [Fact]
    public async Task PredictAttentionAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var scenes = CreateTestScenes(5);
        var options = new PredictionOptions();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.PredictAttentionAsync(scenes, options, cts.Token));
    }

    [Fact]
    public async Task PredictAttentionAsync_IdentifiesHighRiskSegments()
    {
        // Arrange
        var scenes = CreateTestScenes(10);
        var options = new PredictionOptions(EngagementDropThreshold: 0.5);

        // Act
        var result = await _service.PredictAttentionAsync(scenes, options);

        // Assert
        Assert.NotNull(result.HighRiskSegments);
        Assert.All(result.HighRiskSegments, segment =>
        {
            Assert.True(segment.Severity >= DropSeverity.High);
        });
    }

    private List<Scene> CreateTestScenes(int count)
    {
        var scenes = new List<Scene>();
        var cumulativeStart = TimeSpan.Zero;

        for (int i = 0; i < count; i++)
        {
            var duration = TimeSpan.FromSeconds(10 + (i * 2));
            var script = GenerateScript(duration);
            
            scenes.Add(new Scene(
                Index: i,
                Heading: $"Scene {i + 1}",
                Script: script,
                Start: cumulativeStart,
                Duration: duration
            ));

            cumulativeStart += duration;
        }

        return scenes;
    }

    private string GenerateScript(TimeSpan duration)
    {
        // Generate ~2.5 words per second
        var wordCount = (int)(duration.TotalSeconds * 2.5);
        var words = Enumerable.Range(0, wordCount).Select(i => $"word{i}");
        return string.Join(" ", words);
    }
}
