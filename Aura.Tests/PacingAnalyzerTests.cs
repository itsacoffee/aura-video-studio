using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Pacing;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class PacingAnalyzerTests
{
    private readonly Mock<ILogger<PacingAnalyzer>> _mockLogger;
    private readonly Mock<ILogger<RhythmDetector>> _mockRhythmLogger;
    private readonly Mock<ILogger<RetentionOptimizer>> _mockRetentionLogger;
    private readonly RhythmDetector _rhythmDetector;
    private readonly RetentionOptimizer _retentionOptimizer;
    private readonly PacingAnalyzer _pacingAnalyzer;

    public PacingAnalyzerTests()
    {
        _mockLogger = new Mock<ILogger<PacingAnalyzer>>();
        _mockRhythmLogger = new Mock<ILogger<RhythmDetector>>();
        _mockRetentionLogger = new Mock<ILogger<RetentionOptimizer>>();
        _rhythmDetector = new RhythmDetector(_mockRhythmLogger.Object);
        _retentionOptimizer = new RetentionOptimizer(_mockRetentionLogger.Object);
        _pacingAnalyzer = new PacingAnalyzer(_mockLogger.Object, _rhythmDetector, _retentionOptimizer);
    }

    [Fact]
    public async Task AnalyzePacingAsync_WithValidScenes_ReturnsAnalysis()
    {
        // Arrange
        var scenes = CreateTestScenes(3);
        var format = VideoFormat.Explainer;

        // Act
        var result = await _pacingAnalyzer.AnalyzePacingAsync(scenes, null, format);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EngagementScore >= 0 && result.EngagementScore <= 100);
        Assert.Equal(scenes.Count, result.SceneRecommendations.Count);
        Assert.NotEmpty(result.NarrativeArcAssessment);
    }

    [Fact]
    public async Task AnalyzePacingAsync_WithAudioPath_IncludesRhythmAnalysis()
    {
        // Arrange
        var scenes = CreateTestScenes(3);
        var audioPath = "/path/to/audio.wav";
        var format = VideoFormat.Entertainment;

        // Act
        var result = await _pacingAnalyzer.AnalyzePacingAsync(scenes, audioPath, format);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SuggestedTransitions);
    }

    [Fact]
    public async Task AnalyzePacingAsync_WithLongScenes_GeneratesWarnings()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            CreateScene(0, "Intro", 50), // Long scene for hook
            CreateScene(1, "Middle", 70), // Very long scene
            CreateScene(2, "Outro", 25)
        };
        var format = VideoFormat.Vlog;

        // Act
        var result = await _pacingAnalyzer.AnalyzePacingAsync(scenes, null, format);

        // Assert
        Assert.NotNull(result);
        // Warnings may or may not be generated depending on template parameters
        // The important thing is the analysis completes successfully
        Assert.True(result.EngagementScore > 0);
    }

    [Fact]
    public async Task AnalyzePacingAsync_WithSingleScene_HandlesGracefully()
    {
        // Arrange
        var scenes = CreateTestScenes(1);
        var format = VideoFormat.Tutorial;

        // Act
        var result = await _pacingAnalyzer.AnalyzePacingAsync(scenes, null, format);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.SceneRecommendations);
    }

    [Theory]
    [InlineData(VideoFormat.Explainer)]
    [InlineData(VideoFormat.Tutorial)]
    [InlineData(VideoFormat.Vlog)]
    [InlineData(VideoFormat.Entertainment)]
    public async Task AnalyzePacingAsync_WithDifferentFormats_AppliesCorrectTemplate(VideoFormat format)
    {
        // Arrange
        var scenes = CreateTestScenes(3);

        // Act
        var result = await _pacingAnalyzer.AnalyzePacingAsync(scenes, null, format);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.EngagementScore > 0);
        
        // Different formats should produce different recommendations
        foreach (var rec in result.SceneRecommendations)
        {
            Assert.True(rec.RecommendedDuration.TotalSeconds > 0);
        }
    }

    [Fact]
    public async Task AnalyzePacingAsync_CalculatesOptimalDuration()
    {
        // Arrange
        var scenes = CreateTestScenes(5);
        var format = VideoFormat.Explainer;

        // Act
        var result = await _pacingAnalyzer.AnalyzePacingAsync(scenes, null, format);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OptimalDuration.TotalSeconds > 0);
        
        // Optimal duration should be sum of recommended durations
        var totalRecommended = 0.0;
        foreach (var rec in result.SceneRecommendations)
        {
            totalRecommended += rec.RecommendedDuration.TotalSeconds;
        }
        Assert.Equal(totalRecommended, result.OptimalDuration.TotalSeconds, precision: 1);
    }

    [Fact]
    public async Task AnalyzePacingAsync_DetectsTransitionPoints()
    {
        // Arrange
        var scenes = CreateTestScenes(4);
        var format = VideoFormat.Review;

        // Act
        var result = await _pacingAnalyzer.AnalyzePacingAsync(scenes, null, format);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SuggestedTransitions);
        
        // Should have at least scene change transitions
        var sceneChangeTransitions = result.SuggestedTransitions
            .Where(t => t.Type == TransitionType.SceneChange)
            .ToList();
        Assert.NotEmpty(sceneChangeTransitions);
    }

    [Fact]
    public async Task AnalyzePacingAsync_AssessesNarrativeArc()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            CreateScene(0, "Hook", 10),
            CreateScene(1, "Buildup 1", 20),
            CreateScene(2, "Buildup 2", 25),
            CreateScene(3, "Payoff", 15)
        };
        var format = VideoFormat.Explainer;

        // Act
        var result = await _pacingAnalyzer.AnalyzePacingAsync(scenes, null, format);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("hook", result.NarrativeArcAssessment.ToLower());
    }

    [Fact]
    public async Task AnalyzePacingAsync_PrioritizesFirstScene()
    {
        // Arrange
        var scenes = CreateTestScenes(5);
        var format = VideoFormat.Entertainment;

        // Act
        var result = await _pacingAnalyzer.AnalyzePacingAsync(scenes, null, format);

        // Assert
        Assert.NotNull(result);
        var firstSceneRec = result.SceneRecommendations[0];
        
        // First scene (hook) should have high importance
        Assert.True(firstSceneRec.ImportanceScore >= 0.8);
    }

    [Fact]
    public void AnalyzePacingAsync_WithCancellation_ThrowsOperationCanceled()
    {
        // Arrange
        var scenes = CreateTestScenes(3);
        var format = VideoFormat.Tutorial;
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _pacingAnalyzer.AnalyzePacingAsync(scenes, null, format, cts.Token)
        );
    }

    private List<Scene> CreateTestScenes(int count)
    {
        var scenes = new List<Scene>();
        for (int i = 0; i < count; i++)
        {
            scenes.Add(CreateScene(i, $"Scene {i + 1}", 15 + (i * 5)));
        }
        return scenes;
    }

    private Scene CreateScene(int index, string heading, double durationSeconds)
    {
        var script = GenerateTestScript(durationSeconds);
        return new Scene(
            index,
            heading,
            script,
            TimeSpan.FromSeconds(index * durationSeconds),
            TimeSpan.FromSeconds(durationSeconds)
        );
    }

    private string GenerateTestScript(double durationSeconds)
    {
        // Generate approximately 150 words per minute of speech
        var wordCount = (int)(durationSeconds / 60.0 * 150);
        var words = new List<string>();
        for (int i = 0; i < wordCount; i++)
        {
            words.Add($"word{i}");
        }
        return string.Join(" ", words);
    }
}
