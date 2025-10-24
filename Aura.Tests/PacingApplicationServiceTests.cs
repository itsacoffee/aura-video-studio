using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Models.Settings;
using Aura.Core.Services.PacingServices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for PacingApplicationService
/// </summary>
public class PacingApplicationServiceTests
{
    private readonly PacingApplicationService _service;

    public PacingApplicationServiceTests()
    {
        var logger = NullLogger<PacingApplicationService>.Instance;
        _service = new PacingApplicationService(logger);
    }

    [Fact]
    public void ValidateSuggestions_WithValidData_Should_ReturnValid()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test content", TimeSpan.Zero, TimeSpan.FromSeconds(10)),
            new Scene(1, "Scene 2", "More content", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 85.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 0,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(8),
                    MinDuration = TimeSpan.FromSeconds(6),
                    MaxDuration = TimeSpan.FromSeconds(12),
                    Confidence = 80.0
                },
                new SceneTimingSuggestion
                {
                    SceneIndex = 1,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(12),
                    MinDuration = TimeSpan.FromSeconds(9),
                    MaxDuration = TimeSpan.FromSeconds(15),
                    Confidence = 75.0
                }
            }
        };

        // Act
        var result = _service.ValidateSuggestions(analysisResult, scenes);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidateSuggestions_WithLowConfidence_Should_ReturnInvalid()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 50.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 0,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(8),
                    MinDuration = TimeSpan.FromSeconds(6),
                    MaxDuration = TimeSpan.FromSeconds(12),
                    Confidence = 80.0
                }
            }
        };

        // Act
        var result = _service.ValidateSuggestions(analysisResult, scenes, null, 70.0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("confidence score"));
    }

    [Fact]
    public void ValidateSuggestions_WithOutOfRangeIndex_Should_ReturnInvalid()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 85.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 5, // Out of range
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(8),
                    MinDuration = TimeSpan.FromSeconds(6),
                    MaxDuration = TimeSpan.FromSeconds(12),
                    Confidence = 80.0
                }
            }
        };

        // Act
        var result = _service.ValidateSuggestions(analysisResult, scenes);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("out of range"));
    }

    [Fact]
    public void ApplySuggestions_WithBalancedLevel_Should_BlendDurations()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10)),
            new Scene(1, "Scene 2", "Test", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 85.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 0,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(5),
                    MinDuration = TimeSpan.FromSeconds(3),
                    MaxDuration = TimeSpan.FromSeconds(15),
                    Confidence = 85.0
                },
                new SceneTimingSuggestion
                {
                    SceneIndex = 1,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(15),
                    MinDuration = TimeSpan.FromSeconds(10),
                    MaxDuration = TimeSpan.FromSeconds(20),
                    Confidence = 80.0
                }
            }
        };

        var options = new PacingApplicationOptions(
            OptimizationLevel: OptimizationLevel.Balanced,
            MinimumConfidenceThreshold: 70.0);

        // Act
        var result = _service.ApplySuggestions(analysisResult, scenes, options);

        // Assert
        Assert.Equal(2, result.Count);
        
        // Balanced = 40% current + 60% optimal
        // Scene 0: 10 * 0.4 + 5 * 0.6 = 4 + 3 = 7
        Assert.Equal(7, result[0].Duration.TotalSeconds, 0.1);
        
        // Scene 1: 10 * 0.4 + 15 * 0.6 = 4 + 9 = 13
        Assert.Equal(13, result[1].Duration.TotalSeconds, 0.1);

        // Start times should be recalculated
        Assert.Equal(TimeSpan.Zero, result[0].Start);
        Assert.Equal(TimeSpan.FromSeconds(7), result[1].Start);
    }

    [Fact]
    public void ApplySuggestions_WithConservativeLevel_Should_MinimallyAdjust()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 85.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 0,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(6),
                    MinDuration = TimeSpan.FromSeconds(4),
                    MaxDuration = TimeSpan.FromSeconds(12),
                    Confidence = 85.0
                }
            }
        };

        var options = new PacingApplicationOptions(
            OptimizationLevel: OptimizationLevel.Conservative,
            MinimumConfidenceThreshold: 70.0);

        // Act
        var result = _service.ApplySuggestions(analysisResult, scenes, options);

        // Assert
        Assert.Single(result);
        
        // Conservative = 70% current + 30% optimal
        // 10 * 0.7 + 6 * 0.3 = 7 + 1.8 = 8.8
        Assert.Equal(8.8, result[0].Duration.TotalSeconds, 0.1);
    }

    [Fact]
    public void ApplySuggestions_WithAggressiveLevel_Should_UseOptimal()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 85.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 0,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(7),
                    MinDuration = TimeSpan.FromSeconds(5),
                    MaxDuration = TimeSpan.FromSeconds(12),
                    Confidence = 85.0
                }
            }
        };

        var options = new PacingApplicationOptions(
            OptimizationLevel: OptimizationLevel.Aggressive,
            MinimumConfidenceThreshold: 70.0);

        // Act
        var result = _service.ApplySuggestions(analysisResult, scenes, options);

        // Assert
        Assert.Single(result);
        Assert.Equal(7, result[0].Duration.TotalSeconds, 0.1);
    }

    [Fact]
    public void ApplySuggestions_WithLowConfidence_Should_KeepOriginal()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 85.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 0,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(5),
                    MinDuration = TimeSpan.FromSeconds(3),
                    MaxDuration = TimeSpan.FromSeconds(12),
                    Confidence = 50.0 // Low confidence
                }
            }
        };

        var options = new PacingApplicationOptions(
            OptimizationLevel: OptimizationLevel.Aggressive,
            MinimumConfidenceThreshold: 70.0);

        // Act
        var result = _service.ApplySuggestions(analysisResult, scenes, options);

        // Assert
        Assert.Single(result);
        Assert.Equal(10, result[0].Duration.TotalSeconds); // Should keep original
    }

    [Fact]
    public void ApplySuggestions_Should_EnforceMinimumConstraint()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 85.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 0,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(1), // Below minimum
                    MinDuration = TimeSpan.FromSeconds(0.5),
                    MaxDuration = TimeSpan.FromSeconds(5),
                    Confidence = 85.0
                }
            }
        };

        var options = new PacingApplicationOptions(
            OptimizationLevel: OptimizationLevel.Aggressive,
            MinimumConfidenceThreshold: 70.0);

        // Act
        var result = _service.ApplySuggestions(analysisResult, scenes, options);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].Duration.TotalSeconds >= 3.0); // Minimum constraint
    }

    [Fact]
    public void ApplySuggestions_Should_EnforceMaximumConstraint()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 85.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 0,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(150), // Above maximum
                    MinDuration = TimeSpan.FromSeconds(100),
                    MaxDuration = TimeSpan.FromSeconds(200),
                    Confidence = 85.0
                }
            }
        };

        var options = new PacingApplicationOptions(
            OptimizationLevel: OptimizationLevel.Aggressive,
            MinimumConfidenceThreshold: 70.0);

        // Act
        var result = _service.ApplySuggestions(analysisResult, scenes, options);

        // Assert
        Assert.Single(result);
        Assert.True(result[0].Duration.TotalSeconds <= 120.0); // Maximum constraint
    }

    [Fact]
    public void ApplySuggestions_WithTargetDuration_Should_AdjustProportionally()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Scene 1", "Test", TimeSpan.Zero, TimeSpan.FromSeconds(10)),
            new Scene(1, "Scene 2", "Test", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10))
        };

        var analysisResult = new PacingAnalysisResult
        {
            ConfidenceScore = 85.0,
            TimingSuggestions = new List<SceneTimingSuggestion>
            {
                new SceneTimingSuggestion
                {
                    SceneIndex = 0,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(8),
                    MinDuration = TimeSpan.FromSeconds(6),
                    MaxDuration = TimeSpan.FromSeconds(12),
                    Confidence = 85.0
                },
                new SceneTimingSuggestion
                {
                    SceneIndex = 1,
                    CurrentDuration = TimeSpan.FromSeconds(10),
                    OptimalDuration = TimeSpan.FromSeconds(12),
                    MinDuration = TimeSpan.FromSeconds(9),
                    MaxDuration = TimeSpan.FromSeconds(15),
                    Confidence = 80.0
                }
            }
        };

        var options = new PacingApplicationOptions(
            OptimizationLevel: OptimizationLevel.Aggressive,
            MinimumConfidenceThreshold: 70.0,
            TargetDuration: TimeSpan.FromSeconds(30)); // Force total to 30 seconds

        // Act
        var result = _service.ApplySuggestions(analysisResult, scenes, options);

        // Assert
        Assert.Equal(2, result.Count);
        var totalDuration = result.Sum(s => s.Duration.TotalSeconds);
        Assert.InRange(totalDuration, 29.0, 31.0); // Should be close to 30
    }
}
