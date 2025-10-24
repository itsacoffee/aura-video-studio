using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.ML.Models;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Services.PacingServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for the AttentionCurvePredictor
/// </summary>
public class AttentionCurvePredictorTests
{
    private readonly AttentionCurvePredictor _predictor;
    private readonly AttentionRetentionModel _model;

    public AttentionCurvePredictorTests()
    {
        var modelLogger = NullLogger<AttentionRetentionModel>.Instance;
        var predictorLogger = NullLogger<AttentionCurvePredictor>.Instance;
        
        _model = new AttentionRetentionModel(modelLogger);
        _predictor = new AttentionCurvePredictor(predictorLogger, _model);
    }

    [Fact]
    public async Task GenerateAttentionCurveAsync_Should_GenerateDataPoints()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene1", "Test content.", TimeSpan.Zero, TimeSpan.FromSeconds(10)),
            new Scene(1, "Scene2", "More content.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10))
        };

        var suggestions = new List<SceneTimingSuggestion>
        {
            new SceneTimingSuggestion
            {
                SceneIndex = 0,
                OptimalDuration = TimeSpan.FromSeconds(10),
                ImportanceScore = 80,
                ComplexityScore = 60,
                EmotionalIntensity = 70
            },
            new SceneTimingSuggestion
            {
                SceneIndex = 1,
                OptimalDuration = TimeSpan.FromSeconds(10),
                ImportanceScore = 60,
                ComplexityScore = 50,
                EmotionalIntensity = 50
            }
        };

        // Act
        var curve = await _predictor.GenerateAttentionCurveAsync(scenes, suggestions, CancellationToken.None);

        // Assert
        Assert.NotNull(curve);
        Assert.NotEmpty(curve.DataPoints);
        Assert.InRange(curve.AverageEngagement, 0, 100);
        Assert.InRange(curve.OverallRetentionScore, 0, 100);
    }

    [Fact]
    public async Task GenerateAttentionCurveAsync_Should_HaveOrderedTimestamps()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "S1", "Test.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "S2", "Test.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)),
            new Scene(2, "S3", "Test.", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(5))
        };

        var suggestions = scenes.Select(s => new SceneTimingSuggestion
        {
            SceneIndex = s.Index,
            OptimalDuration = s.Duration,
            ImportanceScore = 50,
            ComplexityScore = 50,
            EmotionalIntensity = 50
        }).ToList();

        // Act
        var curve = await _predictor.GenerateAttentionCurveAsync(scenes, suggestions, CancellationToken.None);

        // Assert
        for (int i = 1; i < curve.DataPoints.Count; i++)
        {
            Assert.True(curve.DataPoints[i].Timestamp >= curve.DataPoints[i - 1].Timestamp,
                $"Data point {i} timestamp should be >= previous point");
        }
    }

    [Fact]
    public async Task GenerateAttentionCurveAsync_Should_GenerateMultiplePointsPerScene()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "LongScene", "Content.", TimeSpan.Zero, TimeSpan.FromSeconds(30))
        };

        var suggestions = new List<SceneTimingSuggestion>
        {
            new SceneTimingSuggestion
            {
                SceneIndex = 0,
                OptimalDuration = TimeSpan.FromSeconds(30),
                ImportanceScore = 70,
                ComplexityScore = 60,
                EmotionalIntensity = 60
            }
        };

        // Act
        var curve = await _predictor.GenerateAttentionCurveAsync(scenes, suggestions, CancellationToken.None);

        // Assert
        // Should have multiple points for a 30-second scene (at least 5-6 points)
        Assert.True(curve.DataPoints.Count >= 5, $"Expected at least 5 points, got {curve.DataPoints.Count}");
    }

    [Fact]
    public void IdentifyEngagementPeaks_Should_FindHighEngagementPoints()
    {
        // Arrange
        var dataPoints = new List<AttentionDataPoint>
        {
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(0), EngagementScore = 60 },
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(5), EngagementScore = 85 }, // Peak
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(10), EngagementScore = 65 },
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(15), EngagementScore = 75 }, // Peak
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(20), EngagementScore = 60 }
        };

        var curve = new AttentionCurveData
        {
            DataPoints = dataPoints,
            AverageEngagement = 70,
            OverallRetentionScore = 75
        };

        // Act
        var peaks = _predictor.IdentifyEngagementPeaks(curve);

        // Assert
        Assert.NotEmpty(peaks);
        // Should identify the high engagement points
        Assert.Contains(peaks, p => p == TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void IdentifyEngagementValleys_Should_FindLowEngagementPoints()
    {
        // Arrange
        var dataPoints = new List<AttentionDataPoint>
        {
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(0), EngagementScore = 60 },
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(5), EngagementScore = 40 }, // Valley
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(10), EngagementScore = 65 },
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(15), EngagementScore = 35 }, // Valley
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(20), EngagementScore = 60 }
        };

        var curve = new AttentionCurveData
        {
            DataPoints = dataPoints,
            AverageEngagement = 50,
            OverallRetentionScore = 50
        };

        // Act
        var valleys = _predictor.IdentifyEngagementValleys(curve);

        // Assert
        Assert.NotEmpty(valleys);
        // Should identify the low engagement points
        Assert.Contains(valleys, v => v == TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CalculateOverallRetentionScore_Should_WeightLaterPoints()
    {
        // Arrange
        var dataPoints = new List<AttentionDataPoint>
        {
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(0), RetentionRate = 90 },
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(10), RetentionRate = 80 },
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(20), RetentionRate = 70 },
            new AttentionDataPoint { Timestamp = TimeSpan.FromSeconds(30), RetentionRate = 60 }
        };

        var curve = new AttentionCurveData
        {
            DataPoints = dataPoints,
            AverageEngagement = 70,
            OverallRetentionScore = 0 // Will be calculated
        };

        // Act
        var score = _predictor.CalculateOverallRetentionScore(curve);

        // Assert
        Assert.InRange(score, 0, 100);
        // Score should be lower than simple average (75) since later points are weighted more
        // and they have lower retention
        Assert.True(score < 75, $"Expected weighted score < 75, got {score:F2}");
    }

    [Fact]
    public async Task GenerateAttentionCurveAsync_WithHighImportanceScene_Should_HaveHigherEngagement()
    {
        // Arrange - Two identical scenes, one with high importance, one with low
        var scene = new Scene(0, "Test", "Same content.", TimeSpan.Zero, TimeSpan.FromSeconds(10));
        
        var highImportanceSuggestion = new SceneTimingSuggestion
        {
            SceneIndex = 0,
            OptimalDuration = TimeSpan.FromSeconds(10),
            ImportanceScore = 90,
            ComplexityScore = 50,
            EmotionalIntensity = 50
        };

        var lowImportanceSuggestion = new SceneTimingSuggestion
        {
            SceneIndex = 0,
            OptimalDuration = TimeSpan.FromSeconds(10),
            ImportanceScore = 30,
            ComplexityScore = 50,
            EmotionalIntensity = 50
        };

        // Act
        var highCurve = await _predictor.GenerateAttentionCurveAsync(
            new[] { scene }, new[] { highImportanceSuggestion }, CancellationToken.None);
        
        var lowCurve = await _predictor.GenerateAttentionCurveAsync(
            new[] { scene }, new[] { lowImportanceSuggestion }, CancellationToken.None);

        // Assert
        Assert.True(highCurve.AverageEngagement > lowCurve.AverageEngagement,
            $"High importance ({highCurve.AverageEngagement:F1}) should have higher engagement than low importance ({lowCurve.AverageEngagement:F1})");
    }

    [Fact]
    public async Task GenerateAttentionCurveAsync_EmptyScenes_Should_ReturnValidCurve()
    {
        // Arrange
        var scenes = new List<Scene>();
        var suggestions = new List<SceneTimingSuggestion>();

        // Act
        var curve = await _predictor.GenerateAttentionCurveAsync(scenes, suggestions, CancellationToken.None);

        // Assert
        Assert.NotNull(curve);
        Assert.Empty(curve.DataPoints);
        Assert.Equal(0, curve.AverageEngagement);
    }

    [Fact]
    public async Task GenerateAttentionCurveAsync_Should_IncludeAllScenes()
    {
        // Arrange
        var scenes = Enumerable.Range(0, 5).Select(i => new Scene(
            i,
            $"Scene{i}",
            "Content.",
            TimeSpan.FromSeconds(i * 10),
            TimeSpan.FromSeconds(10)
        )).ToList();

        var suggestions = scenes.Select(s => new SceneTimingSuggestion
        {
            SceneIndex = s.Index,
            OptimalDuration = s.Duration,
            ImportanceScore = 50,
            ComplexityScore = 50,
            EmotionalIntensity = 50
        }).ToList();

        // Act
        var curve = await _predictor.GenerateAttentionCurveAsync(scenes, suggestions, CancellationToken.None);

        // Assert
        // Should have data points covering all scenes (total ~50 seconds)
        var maxTimestamp = curve.DataPoints.Max(p => p.Timestamp);
        Assert.True(maxTimestamp >= TimeSpan.FromSeconds(40), 
            $"Max timestamp {maxTimestamp} should cover most of the video duration");
    }
}
