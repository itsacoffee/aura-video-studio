using Xunit;
using Aura.Core.Configuration;
using Aura.Core.Models.CostTracking;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aura.Tests;

/// <summary>
/// Tests for LLM pricing configuration generation cost estimation
/// </summary>
public class GenerationCostEstimateTests
{
    private readonly LlmPricingConfiguration _config;

    public GenerationCostEstimateTests()
    {
        // Use default configuration for testing
        _config = LlmPricingConfiguration.LoadDefault(NullLogger.Instance);
    }

    [Fact]
    public void EstimateGenerationCost_WithFreeProviders_ReturnsZeroCost()
    {
        // Arrange
        var scriptLength = 1000;
        var sceneCount = 5;
        var llmProvider = "Ollama";
        var llmModel = "llama3";
        var ttsProvider = "Piper";
        var imageProvider = "Placeholder";

        // Act
        var estimate = _config.EstimateGenerationCost(
            scriptLength,
            sceneCount,
            llmProvider,
            llmModel,
            ttsProvider,
            imageProvider);

        // Assert
        Assert.Equal(0m, estimate.TotalCost);
        Assert.True(estimate.IsFreeGeneration);
        Assert.Equal("USD", estimate.Currency);
        Assert.NotEmpty(estimate.Breakdown);
        Assert.All(estimate.Breakdown, item => Assert.True(item.IsFree));
    }

    [Fact]
    public void EstimateGenerationCost_WithPaidLlmProvider_IncludesLlmCost()
    {
        // Arrange
        var scriptLength = 1000;  // ~250 tokens input, ~500 output
        var sceneCount = 5;
        var llmProvider = "OpenAI";
        var llmModel = "gpt-4o-mini";
        var ttsProvider = "Piper";
        var imageProvider = "Placeholder";

        // Act
        var estimate = _config.EstimateGenerationCost(
            scriptLength,
            sceneCount,
            llmProvider,
            llmModel,
            ttsProvider,
            imageProvider);

        // Assert
        Assert.True(estimate.LlmCost >= 0);  // Should have some cost
        Assert.Equal(0m, estimate.TtsCost);  // Piper is free
        Assert.Equal(0m, estimate.ImageCost);  // Placeholder is free
        Assert.Equal(estimate.LlmCost, estimate.TotalCost);
        Assert.False(estimate.IsFreeGeneration);
    }

    [Fact]
    public void EstimateGenerationCost_WithPaidTtsProvider_IncludesTtsCost()
    {
        // Arrange
        var scriptLength = 1000;
        var sceneCount = 5;
        var llmProvider = "Ollama";
        var llmModel = "llama3";
        var ttsProvider = "ElevenLabs";
        var imageProvider = "Placeholder";

        // Act
        var estimate = _config.EstimateGenerationCost(
            scriptLength,
            sceneCount,
            llmProvider,
            llmModel,
            ttsProvider,
            imageProvider);

        // Assert
        Assert.Equal(0m, estimate.LlmCost);  // Ollama is free
        Assert.True(estimate.TtsCost > 0);  // ElevenLabs costs money
        Assert.Equal(0m, estimate.ImageCost);  // Placeholder is free
        Assert.Equal(estimate.TtsCost, estimate.TotalCost);
        Assert.False(estimate.IsFreeGeneration);
    }

    [Fact]
    public void EstimateGenerationCost_WithPaidImageProvider_IncludesImageCost()
    {
        // Arrange
        var scriptLength = 1000;
        var sceneCount = 5;
        var llmProvider = "Ollama";
        var llmModel = "llama3";
        var ttsProvider = "Windows";
        var imageProvider = "DALL-E";

        // Act
        var estimate = _config.EstimateGenerationCost(
            scriptLength,
            sceneCount,
            llmProvider,
            llmModel,
            ttsProvider,
            imageProvider);

        // Assert
        Assert.Equal(0m, estimate.LlmCost);  // Ollama is free
        Assert.Equal(0m, estimate.TtsCost);  // Windows is free
        Assert.True(estimate.ImageCost > 0);  // DALL-E costs money
        Assert.Equal(estimate.ImageCost, estimate.TotalCost);
        Assert.False(estimate.IsFreeGeneration);
    }

    [Fact]
    public void EstimateGenerationCost_BreakdownContainsAllStages()
    {
        // Arrange
        var scriptLength = 1000;
        var sceneCount = 5;
        var llmProvider = "OpenAI";
        var llmModel = "gpt-4o-mini";
        var ttsProvider = "ElevenLabs";
        var imageProvider = "DALL-E";

        // Act
        var estimate = _config.EstimateGenerationCost(
            scriptLength,
            sceneCount,
            llmProvider,
            llmModel,
            ttsProvider,
            imageProvider);

        // Assert
        Assert.Equal(3, estimate.Breakdown.Count);
        Assert.Contains(estimate.Breakdown, b => b.Name == "Script Generation");
        Assert.Contains(estimate.Breakdown, b => b.Name == "Text-to-Speech");
        Assert.Contains(estimate.Breakdown, b => b.Name == "Image Generation");
    }

    [Fact]
    public void EstimateGenerationCost_WithNoImageProvider_ExcludesImageFromBreakdown()
    {
        // Arrange
        var scriptLength = 1000;
        var sceneCount = 5;
        var llmProvider = "Ollama";
        var llmModel = "llama3";
        var ttsProvider = "Windows";
        string? imageProvider = null;

        // Act
        var estimate = _config.EstimateGenerationCost(
            scriptLength,
            sceneCount,
            llmProvider,
            llmModel,
            ttsProvider,
            imageProvider);

        // Assert
        Assert.DoesNotContain(estimate.Breakdown, b => b.Name == "Image Generation");
    }

    [Fact]
    public void EstimateGenerationCost_BreakdownContainsCorrectUnitTypes()
    {
        // Arrange
        var scriptLength = 1000;
        var sceneCount = 5;
        var llmProvider = "OpenAI";
        var llmModel = "gpt-4o-mini";
        var ttsProvider = "ElevenLabs";
        var imageProvider = "DALL-E";

        // Act
        var estimate = _config.EstimateGenerationCost(
            scriptLength,
            sceneCount,
            llmProvider,
            llmModel,
            ttsProvider,
            imageProvider);

        // Assert
        var scriptItem = estimate.Breakdown.First(b => b.Name == "Script Generation");
        var ttsItem = estimate.Breakdown.First(b => b.Name == "Text-to-Speech");
        var imageItem = estimate.Breakdown.First(b => b.Name == "Image Generation");

        Assert.Equal("tokens", scriptItem.UnitType);
        Assert.Equal("characters", ttsItem.UnitType);
        Assert.Equal("images", imageItem.UnitType);
    }

    [Theory]
    [InlineData("Ollama", "llama3", "Piper", "Placeholder", CostEstimateConfidence.High)]
    [InlineData("RuleBased", "default", "Windows", "Stock", CostEstimateConfidence.High)]
    public void EstimateGenerationCost_WithKnownProviders_HasHighConfidence(
        string llmProvider,
        string llmModel,
        string ttsProvider,
        string imageProvider,
        CostEstimateConfidence expectedConfidence)
    {
        // Act
        var estimate = _config.EstimateGenerationCost(
            1000,
            5,
            llmProvider,
            llmModel,
            ttsProvider,
            imageProvider);

        // Assert
        Assert.Equal(expectedConfidence, estimate.Confidence);
    }

    [Fact]
    public void EstimateGenerationCost_TotalCostMatchesSumOfParts()
    {
        // Arrange
        var scriptLength = 2000;
        var sceneCount = 10;
        var llmProvider = "OpenAI";
        var llmModel = "gpt-4o-mini";
        var ttsProvider = "ElevenLabs";
        var imageProvider = "DALL-E";

        // Act
        var estimate = _config.EstimateGenerationCost(
            scriptLength,
            sceneCount,
            llmProvider,
            llmModel,
            ttsProvider,
            imageProvider);

        // Assert
        Assert.Equal(
            estimate.LlmCost + estimate.TtsCost + estimate.ImageCost,
            estimate.TotalCost);
    }
}
