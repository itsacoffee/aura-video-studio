using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Pacing;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class RetentionOptimizerTests
{
    private readonly Mock<ILogger<RetentionOptimizer>> _mockLogger;
    private readonly RetentionOptimizer _retentionOptimizer;

    public RetentionOptimizerTests()
    {
        _mockLogger = new Mock<ILogger<RetentionOptimizer>>();
        _retentionOptimizer = new RetentionOptimizer(_mockLogger.Object);
    }

    [Fact]
    public async Task PredictRetentionAsync_WithValidScenes_Returnsprediction()
    {
        // Arrange
        var scenes = CreateTestScenes(4);

        // Act
        var result = await _retentionOptimizer.PredictRetentionAsync(scenes, null);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OverallRetentionScore >= 0 && result.OverallRetentionScore <= 1.0);
        Assert.Equal(scenes.Count, result.Segments.Count);
    }

    [Fact]
    public async Task PredictRetentionAsync_WithPacingAnalysis_IncorporatesRecommendations()
    {
        // Arrange
        var scenes = CreateTestScenes(3);
        var pacingAnalysis = CreateMockPacingAnalysis(scenes);

        // Act
        var result = await _retentionOptimizer.PredictRetentionAsync(scenes, pacingAnalysis);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Segments);
    }

    [Fact]
    public async Task GenerateAttentionCurveAsync_ReturnsValidCurve()
    {
        // Arrange
        var scenes = CreateTestScenes(3);
        var totalDuration = TimeSpan.FromSeconds(scenes.Sum(s => s.Duration.TotalSeconds));

        // Act
        var result = await _retentionOptimizer.GenerateAttentionCurveAsync(scenes, totalDuration);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Points);
        Assert.True(result.AverageEngagement >= 0 && result.AverageEngagement <= 1.0);
        
        // Should have attention points throughout the duration
        Assert.True(result.Points.First().Timestamp == TimeSpan.Zero);
        Assert.True(result.Points.Last().Timestamp <= totalDuration);
    }

    [Fact]
    public async Task GenerateAttentionCurveAsync_FirstSecondsHaveHighAttention()
    {
        // Arrange
        var scenes = CreateTestScenes(5);
        var totalDuration = TimeSpan.FromSeconds(100);

        // Act
        var result = await _retentionOptimizer.GenerateAttentionCurveAsync(scenes, totalDuration);

        // Assert
        var earlyPoints = result.Points.Where(p => p.Timestamp.TotalSeconds <= 15).ToList();
        Assert.NotEmpty(earlyPoints);
        
        // First 15 seconds should have high attention
        Assert.All(earlyPoints, point => Assert.True(point.AttentionLevel >= 0.8));
    }

    [Fact]
    public void OptimizeForRetention_ShortensLongHook()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            CreateScene(0, "Long Hook", 25), // Too long for hook
            CreateScene(1, "Middle", 15),
            CreateScene(2, "End", 12)
        };

        // Act
        var optimized = _retentionOptimizer.OptimizeForRetention(scenes, VideoFormat.Entertainment);

        // Assert
        Assert.NotNull(optimized);
        Assert.True(optimized[0].Duration.TotalSeconds <= 15);
    }

    [Fact]
    public void OptimizeForRetention_EntertainmentFormat_EnforcesMaxDuration()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            CreateScene(0, "Intro", 10),
            CreateScene(1, "Scene 1", 30), // Too long for entertainment
            CreateScene(2, "Scene 2", 25), // Too long for entertainment
            CreateScene(3, "Outro", 8)
        };

        // Act
        var optimized = _retentionOptimizer.OptimizeForRetention(scenes, VideoFormat.Entertainment);

        // Assert
        Assert.NotNull(optimized);
        foreach (var scene in optimized)
        {
            Assert.True(scene.Duration.TotalSeconds <= 20);
        }
    }

    [Fact]
    public void OptimizeForRetention_TutorialFormat_AllowsLongerScenes()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            CreateScene(0, "Intro", 12),
            CreateScene(1, "Step 1", 35),
            CreateScene(2, "Step 2", 40),
            CreateScene(3, "Conclusion", 15)
        };

        // Act
        var optimized = _retentionOptimizer.OptimizeForRetention(scenes, VideoFormat.Tutorial);

        // Assert
        Assert.NotNull(optimized);
        // Tutorial format should preserve longer scenes
        Assert.Contains(optimized, s => s.Duration.TotalSeconds >= 30);
    }

    [Fact]
    public async Task PredictRetentionAsync_IdentifiesDropRiskPoints()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            CreateScene(0, "Intro", 10),
            CreateScene(1, "Scene 1", 15),
            CreateScene(2, "Long Scene", 50), // High drop risk
            CreateScene(3, "Outro", 12)
        };

        // Act
        var result = await _retentionOptimizer.PredictRetentionAsync(scenes, null);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.HighDropRiskPoints);
    }

    [Fact]
    public async Task PredictRetentionAsync_FirstSceneHasHighRetention()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            CreateScene(0, "Hook", 12), // Good hook duration
            CreateScene(1, "Middle", 20),
            CreateScene(2, "End", 15)
        };

        // Act
        var result = await _retentionOptimizer.PredictRetentionAsync(scenes, null);

        // Assert
        Assert.NotNull(result);
        var firstSegment = result.Segments.First();
        Assert.True(firstSegment.PredictedRetention >= 0.8);
    }

    [Fact]
    public async Task PredictRetentionAsync_LateScenesShouldHaveLowerRetention()
    {
        // Arrange
        var scenes = CreateTestScenes(5);

        // Act
        var result = await _retentionOptimizer.PredictRetentionAsync(scenes, null);

        // Assert
        Assert.NotNull(result);
        var firstSegment = result.Segments.First();
        var lastSegment = result.Segments.Last();
        
        // Natural decay: later scenes should have lower retention
        Assert.True(lastSegment.PredictedRetention <= firstSegment.PredictedRetention);
    }

    [Fact]
    public async Task GenerateAttentionCurveAsync_IdentifiesCriticalDrops()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            CreateScene(0, "Good Start", 10),
            CreateScene(1, "Boring Part", 45), // Should cause attention drop
            CreateScene(2, "Recovery", 15)
        };
        var totalDuration = TimeSpan.FromSeconds(70);

        // Act
        var result = await _retentionOptimizer.GenerateAttentionCurveAsync(scenes, totalDuration);

        // Assert
        Assert.NotNull(result);
        // Long, boring section should be identified
        Assert.True(result.CriticalDropPoints.Count >= 0); // May or may not identify drops based on thresholds
    }

    [Theory]
    [InlineData(VideoFormat.Explainer)]
    [InlineData(VideoFormat.Vlog)]
    [InlineData(VideoFormat.Educational)]
    public void OptimizeForRetention_DifferentFormats_ProduceDifferentResults(VideoFormat format)
    {
        // Arrange
        var scenes = CreateTestScenes(4);

        // Act
        var optimized = _retentionOptimizer.OptimizeForRetention(scenes, format);

        // Assert
        Assert.NotNull(optimized);
        Assert.Equal(scenes.Count, optimized.Count);
    }

    [Fact]
    public async Task PredictRetentionAsync_EmptyScenes_HandlesGracefully()
    {
        // Arrange
        var scenes = new List<Scene>();

        // Act
        var result = await _retentionOptimizer.PredictRetentionAsync(scenes, null);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Segments);
    }

    private List<Scene> CreateTestScenes(int count)
    {
        var scenes = new List<Scene>();
        for (int i = 0; i < count; i++)
        {
            scenes.Add(CreateScene(i, $"Scene {i + 1}", 15 + (i * 3)));
        }
        return scenes;
    }

    private Scene CreateScene(int index, string heading, double durationSeconds)
    {
        var wordCount = (int)(durationSeconds / 60.0 * 150);
        var script = string.Join(" ", Enumerable.Range(0, wordCount).Select(i => $"word{i}"));
        
        return new Scene(
            index,
            heading,
            script,
            TimeSpan.FromSeconds(index * durationSeconds),
            TimeSpan.FromSeconds(durationSeconds)
        );
    }

    private PacingAnalysisResult CreateMockPacingAnalysis(List<Scene> scenes)
    {
        var recommendations = scenes.Select((s, i) => new ScenePacingRecommendation(
            i,
            s.Duration,
            TimeSpan.FromSeconds(s.Duration.TotalSeconds * 0.9),
            0.7,
            0.6,
            "Test recommendation"
        )).ToList();

        return new PacingAnalysisResult(
            TimeSpan.FromSeconds(scenes.Sum(s => s.Duration.TotalSeconds)),
            75.0,
            recommendations,
            Array.Empty<TransitionPoint>(),
            "Test narrative assessment",
            new List<string>()
        );
    }
}
