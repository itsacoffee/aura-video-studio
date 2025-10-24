using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aura.Core.ML;
using Aura.Core.ML.Models;
using Aura.Core.Models;
using Aura.Core.Models.Settings;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.ML;

public class ContentOptimizationTests
{
    [Fact]
    public void ContentSuccessPredictionModel_PredictSuccess_ReturnsValidPrediction()
    {
        // Arrange
        var model = new ContentSuccessPredictionModel();
        var features = new ContentFeatures
        {
            Topic = "How to Build a Website",
            DurationMinutes = 3.5,
            Pacing = "Conversational",
            Density = "Balanced",
            Tone = "informative",
            HistoricalAverageScore = 0
        };

        // Act
        var result = model.PredictSuccess(features);

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.PredictedScore, 0, 100);
        Assert.InRange(result.Confidence, 0, 1);
        Assert.NotEmpty(result.ContributingFactors);
    }

    [Fact]
    public void ContentSuccessPredictionModel_OptimalDuration_HigherScore()
    {
        // Arrange
        var model = new ContentSuccessPredictionModel();
        
        var optimalFeatures = new ContentFeatures
        {
            Topic = "Quick Tutorial",
            DurationMinutes = 3.0, // Optimal duration
            Pacing = "Fast",
            Density = "Balanced",
            Tone = "entertaining"
        };

        var longFeatures = new ContentFeatures
        {
            Topic = "Quick Tutorial",
            DurationMinutes = 15.0, // Too long
            Pacing = "Fast",
            Density = "Balanced",
            Tone = "entertaining"
        };

        // Act
        var optimalResult = model.PredictSuccess(optimalFeatures);
        var longResult = model.PredictSuccess(longFeatures);

        // Assert
        Assert.True(optimalResult.PredictedScore > longResult.PredictedScore);
    }

    [Fact]
    public async Task ProviderPerformanceTracker_RecordAndRetrieveStats()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ProviderPerformanceTracker>>();
        var tracker = new ProviderPerformanceTracker(mockLogger.Object);

        // Act
        await tracker.RecordGenerationAsync(
            "Ollama",
            "script",
            85.0,
            TimeSpan.FromSeconds(5),
            true);

        var stats = await tracker.GetProviderStatsAsync("Ollama", "script");

        // Assert
        Assert.NotNull(stats);
        Assert.Equal("Ollama", stats.ProviderName);
        Assert.Equal(1, stats.TotalGenerations);
        Assert.Equal(1.0, stats.SuccessRate);
        Assert.Equal(85.0, stats.AverageQualityScore);
    }

    [Fact]
    public async Task ProviderPerformanceTracker_GetBestProvider_SelectsHighestScore()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ProviderPerformanceTracker>>();
        var tracker = new ProviderPerformanceTracker(mockLogger.Object);

        // Record different performance for providers
        await tracker.RecordGenerationAsync("Ollama", "script", 75.0, TimeSpan.FromSeconds(5), true);
        await tracker.RecordGenerationAsync("Ollama", "script", 76.0, TimeSpan.FromSeconds(6), true);
        await tracker.RecordGenerationAsync("Ollama", "script", 77.0, TimeSpan.FromSeconds(5), true);

        await tracker.RecordGenerationAsync("OpenAI", "script", 85.0, TimeSpan.FromSeconds(3), true);
        await tracker.RecordGenerationAsync("OpenAI", "script", 86.0, TimeSpan.FromSeconds(3), true);
        await tracker.RecordGenerationAsync("OpenAI", "script", 87.0, TimeSpan.FromSeconds(3), true);

        var availableProviders = new List<string> { "Ollama", "OpenAI" };

        // Act
        var bestProvider = await tracker.GetBestProviderAsync("script", availableProviders);

        // Assert
        Assert.Equal("OpenAI", bestProvider);
    }

    [Fact]
    public async Task DynamicPromptEnhancer_DisabledSettings_ReturnsOriginal()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DynamicPromptEnhancer>>();
        var enhancer = new DynamicPromptEnhancer(mockLogger.Object);
        
        var settings = new AIOptimizationSettings { Enabled = false };
        var originalPrompt = "Create a video about testing";
        var brief = new Brief("Testing", null, null, "informative", "en", Aspect.Widescreen16x9);
        var spec = new PlanSpec(TimeSpan.FromMinutes(3), Pacing.Conversational, Density.Balanced, null);

        // Act
        var result = await enhancer.EnhancePromptAsync(originalPrompt, brief, spec, settings);

        // Assert
        Assert.False(result.Applied);
        Assert.Equal(originalPrompt, result.Prompt);
    }

    [Fact]
    public async Task DynamicPromptEnhancer_EnabledSettings_AppliesEnhancements()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<DynamicPromptEnhancer>>();
        var enhancer = new DynamicPromptEnhancer(mockLogger.Object);
        
        var settings = new AIOptimizationSettings 
        { 
            Enabled = true,
            Level = OptimizationLevel.Balanced,
            OptimizationMetrics = new List<OptimizationMetric> 
            { 
                OptimizationMetric.Engagement,
                OptimizationMetric.Quality 
            }
        };
        
        var originalPrompt = "Create a video about testing";
        var brief = new Brief("Testing", null, null, "informative", "en", Aspect.Widescreen16x9);
        var spec = new PlanSpec(TimeSpan.FromMinutes(3), Pacing.Conversational, Density.Balanced, null);

        // Act
        var result = await enhancer.EnhancePromptAsync(originalPrompt, brief, spec, settings);

        // Assert
        Assert.True(result.Applied);
        Assert.NotEqual(originalPrompt, result.Prompt);
        Assert.NotEmpty(result.Enhancements);
        Assert.True(result.Prompt.Length > originalPrompt.Length);
    }

    [Fact]
    public async Task ContentOptimizationEngine_OptimizationDisabled_ReturnsOriginalRequest()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ContentOptimizationEngine>>();
        var mockPromptEnhancer = new Mock<DynamicPromptEnhancer>(Mock.Of<ILogger<DynamicPromptEnhancer>>());
        var predictionModel = new ContentSuccessPredictionModel();
        var mockTracker = new Mock<ProviderPerformanceTracker>(Mock.Of<ILogger<ProviderPerformanceTracker>>());

        var engine = new ContentOptimizationEngine(
            mockLogger.Object,
            mockPromptEnhancer.Object,
            predictionModel,
            mockTracker.Object);

        var brief = new Brief("Test Topic", null, null, "informative", "en", Aspect.Widescreen16x9);
        var spec = new PlanSpec(TimeSpan.FromMinutes(3), Pacing.Conversational, Density.Balanced, null);
        var settings = new AIOptimizationSettings { Enabled = false };

        // Act
        var result = await engine.OptimizeContentRequestAsync(brief, spec, settings);

        // Assert
        Assert.False(result.Applied);
        Assert.Equal(brief, result.OriginalBrief);
        Assert.Equal(spec, result.OriginalSpec);
    }

    [Fact]
    public async Task ContentOptimizationEngine_OptimizationEnabled_GeneratesPrediction()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ContentOptimizationEngine>>();
        var mockPromptEnhancer = new Mock<DynamicPromptEnhancer>(Mock.Of<ILogger<DynamicPromptEnhancer>>());
        var predictionModel = new ContentSuccessPredictionModel();
        var mockTracker = new Mock<ProviderPerformanceTracker>(Mock.Of<ILogger<ProviderPerformanceTracker>>());

        var engine = new ContentOptimizationEngine(
            mockLogger.Object,
            mockPromptEnhancer.Object,
            predictionModel,
            mockTracker.Object);

        var brief = new Brief("Test Topic", null, null, "informative", "en", Aspect.Widescreen16x9);
        var spec = new PlanSpec(TimeSpan.FromMinutes(3), Pacing.Conversational, Density.Balanced, null);
        var settings = new AIOptimizationSettings { Enabled = true };

        // Act
        var result = await engine.OptimizeContentRequestAsync(brief, spec, settings);

        // Assert
        Assert.NotNull(result.Prediction);
        Assert.InRange(result.Prediction.PredictedScore, 0, 100);
        Assert.InRange(result.Prediction.Confidence, 0, 1);
    }
}
