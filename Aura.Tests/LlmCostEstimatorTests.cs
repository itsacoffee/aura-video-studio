using Xunit;
using Aura.Providers.Llm;
using Aura.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace Aura.Tests;

/// <summary>
/// Tests for LLM cost estimation functionality with dynamic configuration
/// </summary>
public class LlmCostEstimatorTests
{
    private readonly LlmCostEstimator _estimator;

    public LlmCostEstimatorTests()
    {
        // Create estimator with default configuration loading
        // In tests, it will use default configuration if JSON file not found
        _estimator = new LlmCostEstimator(NullLogger<LlmCostEstimator>.Instance);
    }

    [Theory]
    [InlineData("gpt-4o", 1000, 500, 0.0075)]  // (1000/1M * 2.50) + (500/1M * 10.00)
    [InlineData("gpt-4o-mini", 1000, 500, 0.00045)]  // (1000/1M * 0.15) + (500/1M * 0.60)
    [InlineData("gpt-4-turbo", 1000, 500, 0.025)]  // (1000/1M * 10.00) + (500/1M * 30.00)
    [InlineData("claude-3-haiku-20240307", 1000, 500, 0.000875)]  // (1000/1M * 0.25) + (500/1M * 1.25)
    public void CalculateCost_WithKnownModels_ReturnsCorrectCost(
        string model, 
        int inputTokens, 
        int outputTokens, 
        double expectedCostDouble)
    {
        // Arrange
        var expectedCost = (decimal)expectedCostDouble;

        // Act
        var result = _estimator.CalculateCost(inputTokens, outputTokens, model);

        // Assert
        Assert.Equal(expectedCost, result.TotalCost, 6);
        Assert.Equal(inputTokens, result.InputTokens);
        Assert.Equal(outputTokens, result.OutputTokens);
        Assert.Equal(inputTokens + outputTokens, result.TotalTokens);
        Assert.Equal(model, result.Model);
    }

    [Theory]
    [InlineData("ollama", 10000, 10000, 0.0)]
    [InlineData("local", 5000, 5000, 0.0)]
    [InlineData("rulebased", 1000, 1000, 0.0)]
    public void CalculateCost_WithFreeProviders_ReturnsZeroCost(
        string model,
        int inputTokens,
        int outputTokens,
        double expectedCostDouble)
    {
        // Arrange
        var expectedCost = (decimal)expectedCostDouble;

        // Act
        var result = _estimator.CalculateCost(inputTokens, outputTokens, model);

        // Assert
        Assert.Equal(expectedCost, result.TotalCost);
    }

    [Fact]
    public void CalculateCost_WithUnknownModel_UsesFallbackPricing()
    {
        // Act
        var result = _estimator.CalculateCost(1000, 1000, "unknown-model-xyz");

        // Assert
        Assert.True(result.TotalCost > 0);
        Assert.Equal("unknown-model-xyz", result.Model);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("test", 1)]  // 4 chars = 1 token
    [InlineData("Hello World", 3)]  // 11 chars = 2.75 ≈ 3 tokens
    [InlineData("This is a longer test string", 7)]  // 28 chars = 7 tokens
    public void EstimateTokenCount_WithVariousTexts_ReturnsCorrectEstimate(
        string text,
        int expectedTokens)
    {
        // Act
        var result = _estimator.EstimateTokenCount(text);

        // Assert
        Assert.Equal(expectedTokens, result);
    }

    [Fact]
    public void EstimateTokenCount_WithNullOrWhitespace_ReturnsZero()
    {
        // Act & Assert
        Assert.Equal(0, _estimator.EstimateTokenCount(null!));
        Assert.Equal(0, _estimator.EstimateTokenCount(""));
        Assert.Equal(0, _estimator.EstimateTokenCount("   "));
    }

    [Fact]
    public void EstimateInputTokensForScriptGeneration_WithBriefAndSpec_ReturnsReasonableEstimate()
    {
        // Arrange
        var brief = new Brief
        {
            Topic = "Introduction to Machine Learning",
            Audience = "Beginners",
            Goal = "Educate",
            Tone = "Conversational",
            Context = "This is a comprehensive guide covering basics of ML"
        };
        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Modern"
        );

        // Act
        var result = _estimator.EstimateInputTokensForScriptGeneration(brief, spec);

        // Assert - Should include system prompt (~600) + brief/spec/context
        Assert.True(result > 600);
        Assert.True(result < 1000); // Sanity check
    }

    [Fact]
    public void EstimateOutputTokensForScriptGeneration_WithDuration_ReturnsProportionalEstimate()
    {
        // Arrange
        var shortSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Modern"
        );
        var longSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(5),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Modern"
        );

        // Act
        var shortResult = _estimator.EstimateOutputTokensForScriptGeneration(shortSpec);
        var longResult = _estimator.EstimateOutputTokensForScriptGeneration(longSpec);

        // Assert
        Assert.True(shortResult > 0);
        Assert.True(longResult > shortResult * 4); // 5x duration should be >4x tokens
        Assert.True(longResult < shortResult * 6); // but less than 6x (some overhead)
    }

    [Theory]
    [InlineData(QualityTier.Budget, 0.01, "gpt-4o-mini")]
    [InlineData(QualityTier.Budget, 0.001, "claude-3-haiku-20240307")]
    [InlineData(QualityTier.Balanced, 0.05, "gpt-4o")]
    [InlineData(QualityTier.Balanced, 0.01, "gpt-4o-mini")]
    [InlineData(QualityTier.Premium, 0.50, "gpt-4-turbo")]
    [InlineData(QualityTier.Premium, 0.20, "claude-3-5-sonnet-20241022")]
    [InlineData(QualityTier.Premium, 0.10, "gpt-4o")]
    [InlineData(QualityTier.Maximum, 0.10, "gpt-4-turbo")]
    public void RecommendModel_WithBudgetAndQuality_ReturnsAppropriateModel(
        QualityTier quality,
        double budgetDouble,
        string expectedModel)
    {
        // Arrange
        var budget = (decimal)budgetDouble;

        // Act
        var result = _estimator.RecommendModel(budget, quality);

        // Assert
        Assert.Equal(expectedModel, result);
    }

    [Fact]
    public void CompareModels_WithDifferentModels_ReturnsAccurateSavings()
    {
        // Arrange
        int inputTokens = 1000;
        int outputTokens = 1000;

        // Act
        var comparison = _estimator.CompareModels(
            inputTokens,
            outputTokens,
            "gpt-4-turbo",
            "gpt-4o-mini"
        );

        // Assert
        Assert.Equal("gpt-4-turbo", comparison.CurrentModel);
        Assert.Equal("gpt-4o-mini", comparison.AlternativeModel);
        Assert.True(comparison.CurrentCost > comparison.AlternativeCost);
        Assert.True(comparison.Savings > 0);
        Assert.True(comparison.SavingsPercentage > 0);
        Assert.True(comparison.SavingsPercentage < 100);
    }

    [Fact]
    public void CompareModels_WithSameModel_ReturnsZeroSavings()
    {
        // Arrange
        int inputTokens = 1000;
        int outputTokens = 1000;

        // Act
        var comparison = _estimator.CompareModels(
            inputTokens,
            outputTokens,
            "gpt-4o",
            "gpt-4o"
        );

        // Assert
        Assert.Equal(0, comparison.Savings);
        Assert.Equal(0, comparison.SavingsPercentage);
    }

    [Fact]
    public void EstimateCost_WithPromptString_ReturnsValidEstimate()
    {
        // Arrange
        var prompt = "Generate a video script about artificial intelligence";
        var estimatedCompletionTokens = 500;

        // Act
        var result = _estimator.EstimateCost(prompt, estimatedCompletionTokens, "gpt-4o-mini");

        // Assert
        Assert.True(result.InputTokens > 0);
        Assert.Equal(estimatedCompletionTokens, result.OutputTokens);
        Assert.True(result.TotalCost > 0);
        Assert.Equal("gpt-4o-mini", result.Model);
    }

    [Theory]
    [InlineData("GPT-4O-MINI", "gpt-4o-mini")]
    [InlineData("gpt-4o", "gpt-4o")]
    [InlineData("GPT-4-TURBO", "gpt-4-turbo")]
    [InlineData("claude-3-haiku", "claude-3-haiku-20240307")]
    [InlineData("gemini-flash", "gemini-1.5-flash")]
    [InlineData("ollama", "ollama")]
    public void CalculateCost_WithVariousModelNameFormats_NormalizesCorrectly(
        string inputModelName,
        string expectedNormalizedModel)
    {
        // Act
        var result = _estimator.CalculateCost(100, 100, inputModelName);

        // Assert
        // Verify the cost matches the expected normalized model
        var expectedResult = _estimator.CalculateCost(100, 100, expectedNormalizedModel);
        Assert.Equal(expectedResult.TotalCost, result.TotalCost);
    }

    [Fact]
    public void CostEstimate_IncludesTimestamp()
    {
        // Arrange
        var beforeEstimate = DateTime.UtcNow;

        // Act
        var result = _estimator.CalculateCost(100, 100, "gpt-4o-mini");
        var afterEstimate = DateTime.UtcNow;

        // Assert
        Assert.True(result.EstimatedAt >= beforeEstimate);
        Assert.True(result.EstimatedAt <= afterEstimate);
    }

    [Fact]
    public void CostEstimate_IncludesConfigVersion()
    {
        // Act
        var result = _estimator.CalculateCost(100, 100, "gpt-4o-mini");

        // Assert
        Assert.NotNull(result.ConfigVersion);
        Assert.NotEmpty(result.ConfigVersion);
    }

    [Fact]
    public void GetConfigVersion_ReturnsVersion()
    {
        // Act
        var version = _estimator.GetConfigVersion();

        // Assert
        Assert.NotNull(version);
        Assert.NotEmpty(version);
        Assert.Matches(@"^\d{4}\.\d{2}$", version); // Format: YYYY.MM
    }

    [Fact]
    public void GetConfigLastUpdated_ReturnsDate()
    {
        // Act
        var lastUpdated = _estimator.GetConfigLastUpdated();

        // Assert
        Assert.NotNull(lastUpdated);
        Assert.NotEmpty(lastUpdated);
    }

    [Fact]
    public void GetAvailableModels_ReturnsModelList()
    {
        // Act
        var models = _estimator.GetAvailableModels();

        // Assert
        Assert.NotNull(models);
        Assert.NotEmpty(models);
        Assert.Contains(models, m => m.Contains("gpt"));
    }

    [Fact]
    public void CalculateCost_WithLargeTokenCounts_HandlesCorrectly()
    {
        // Arrange - Simulate a very large request
        int inputTokens = 100_000;
        int outputTokens = 50_000;

        // Act
        var result = _estimator.CalculateCost(inputTokens, outputTokens, "gpt-4o");

        // Assert
        Assert.Equal(150_000, result.TotalTokens);
        Assert.True(result.TotalCost > 0.50m); // Should be relatively expensive
        Assert.True(result.InputCost < result.OutputCost); // Output usually costs more
    }

    [Fact]
    public void CostEstimate_BreaksDownInputAndOutputCosts()
    {
        // Act
        var result = _estimator.CalculateCost(1000, 1000, "gpt-4o");

        // Assert
        Assert.True(result.InputCost > 0);
        Assert.True(result.OutputCost > 0);
        Assert.Equal(result.InputCost + result.OutputCost, result.TotalCost);
        // For most models, output costs more than input
        Assert.True(result.OutputCost > result.InputCost);
    }
}
