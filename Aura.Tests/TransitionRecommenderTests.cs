using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Services.PacingServices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for the TransitionRecommender service
/// </summary>
public class TransitionRecommenderTests
{
    private readonly TransitionRecommender _recommender;

    public TransitionRecommenderTests()
    {
        var logger = NullLogger<TransitionRecommender>.Instance;
        _recommender = new TransitionRecommender(logger);
    }

    [Fact]
    public async Task RecommendTransitionsAsync_WithTwoScenes_ReturnsOneRecommendation()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Intro", "Welcome to this video about AI.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Main", "Let's explore AI in depth.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var recommendations = await _recommender.RecommendTransitionsAsync(scenes);

        // Assert
        Assert.Single(recommendations);
        Assert.Equal(0, recommendations[0].FromSceneIndex);
        Assert.Equal(1, recommendations[0].ToSceneIndex);
    }

    [Fact]
    public async Task RecommendTransitionsAsync_WithDirectlyRelatedContent_RecommendsCut()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "First", "AI technology is amazing.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Second", "This AI technology works well.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var recommendations = await _recommender.RecommendTransitionsAsync(scenes);

        // Assert
        Assert.Single(recommendations);
        Assert.Equal(TransitionType.Cut, recommendations[0].RecommendedType);
        Assert.Contains("directly related", recommendations[0].ContentRelationship);
    }

    [Fact]
    public async Task RecommendTransitionsAsync_WithTimeChange_RecommendsFade()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Before", "We started this morning.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "After", "Later in the day we continued.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        // Act
        var recommendations = await _recommender.RecommendTransitionsAsync(scenes);

        // Assert
        Assert.Single(recommendations);
        Assert.Equal(TransitionType.Fade, recommendations[0].RecommendedType);
        Assert.True(recommendations[0].DurationSeconds > 0);
    }

    [Fact]
    public async Task RecommendTransitionsAsync_WithEmotionalIntensityIncrease_RecommendsCut()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Calm", "Things are peaceful.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Exciting", "Surprise! Something amazing happened!", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        var analyses = new List<SceneAnalysisData>
        {
            new SceneAnalysisData { SceneIndex = 0, EmotionalIntensity = 30 },
            new SceneAnalysisData { SceneIndex = 1, EmotionalIntensity = 80 }
        };

        // Act
        var recommendations = await _recommender.RecommendTransitionsAsync(scenes, analyses);

        // Assert
        Assert.Single(recommendations);
        Assert.Equal(TransitionType.Cut, recommendations[0].RecommendedType);
        Assert.Equal(0.0, recommendations[0].DurationSeconds);
    }

    [Fact]
    public async Task RecommendTransitionsAsync_WithEmotionalIntensityDecrease_RecommendsFade()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Intense", "Critical urgent action needed now!", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Calm", "Everything is peaceful and calm.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        var analyses = new List<SceneAnalysisData>
        {
            new SceneAnalysisData { SceneIndex = 0, EmotionalIntensity = 80 },
            new SceneAnalysisData { SceneIndex = 1, EmotionalIntensity = 30 }
        };

        // Act
        var recommendations = await _recommender.RecommendTransitionsAsync(scenes, analyses);

        // Assert
        Assert.Single(recommendations);
        Assert.Equal(TransitionType.Fade, recommendations[0].RecommendedType);
        Assert.True(recommendations[0].DurationSeconds > 0);
    }

    [Fact]
    public async Task RecommendTransitionsAsync_WithSingleScene_ReturnsEmpty()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "Only", "This is the only scene.", TimeSpan.Zero, TimeSpan.FromSeconds(5))
        };

        // Act
        var recommendations = await _recommender.RecommendTransitionsAsync(scenes);

        // Assert
        Assert.Empty(recommendations);
    }

    [Fact]
    public async Task RecommendTransitionsAsync_WithTikTokBrief_OptimizesForPlatform()
    {
        // Arrange
        var scenes = new List<Scene>
        {
            new Scene(0, "First", "Scene one.", TimeSpan.Zero, TimeSpan.FromSeconds(5)),
            new Scene(1, "Second", "Scene two.", TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10))
        };

        var brief = new Brief(
            "Test Topic",
            "Engage viewers",
            "General",
            "Casual",
            "English",
            Aspect.Vertical9x16
        );

        // Act
        var recommendations = await _recommender.RecommendTransitionsAsync(scenes, null, brief);

        // Assert
        Assert.Single(recommendations);
        Assert.NotNull(recommendations[0].PlatformOptimization);
        Assert.Contains("TikTok", recommendations[0].PlatformOptimization);
    }
}
