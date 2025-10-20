using System;
using Aura.Core.Models;
using Aura.Core.Services.Generation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class StrategySelectorTests
{
    private readonly ILogger<StrategySelector> _logger;

    public StrategySelectorTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _logger = loggerFactory.CreateLogger<StrategySelector>();
    }

    [Fact]
    public void SelectStrategy_WithHighEndSystem_ShouldSelectParallelStrategy()
    {
        // Arrange
        var selector = new StrategySelector(_logger);
        var brief = new Brief("Test Topic", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(5), Pacing.Conversational, Density.Balanced, "Modern");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.A,
            LogicalCores = 16,
            PhysicalCores = 8,
            RamGB = 32,
            OfflineOnly = false
        };

        // Act
        var strategy = selector.SelectStrategy(brief, systemProfile, planSpec);

        // Assert
        Assert.NotNull(strategy);
        Assert.True(strategy.MaxConcurrency > 2);
        Assert.True(strategy.EnableProgressiveCaching);
    }

    [Fact]
    public void SelectStrategy_WithLowEndSystem_ShouldSelectSequentialStrategy()
    {
        // Arrange
        var selector = new StrategySelector(_logger);
        var brief = new Brief("Test Topic", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(5), Pacing.Conversational, Density.Balanced, "Modern");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.D,
            LogicalCores = 2,
            PhysicalCores = 2,
            RamGB = 8,
            OfflineOnly = false
        };

        // Act
        var strategy = selector.SelectStrategy(brief, systemProfile, planSpec);

        // Assert
        Assert.NotNull(strategy);
        Assert.True(strategy.MaxConcurrency <= 2);
    }

    [Fact]
    public void SelectStrategy_WithOfflineMode_ShouldSelectSequentialStrategy()
    {
        // Arrange
        var selector = new StrategySelector(_logger);
        var brief = new Brief("Test Topic", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(5), Pacing.Conversational, Density.Balanced, "Modern");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.A,
            LogicalCores = 16,
            PhysicalCores = 8,
            RamGB = 32,
            OfflineOnly = true
        };

        // Act
        var strategy = selector.SelectStrategy(brief, systemProfile, planSpec);

        // Assert
        Assert.Equal(StrategyType.Sequential, strategy.StrategyType);
        Assert.Equal(VisualGenerationApproach.StockOnly, strategy.VisualApproach);
    }

    [Fact]
    public void SelectStrategy_WithComplexContent_ShouldEnableFallback()
    {
        // Arrange
        var selector = new StrategySelector(_logger);
        var brief = new Brief("Complex Technical Topic", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(15), Pacing.Fast, Density.Dense, "Technical");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = 8,
            PhysicalCores = 4,
            RamGB = 16,
            OfflineOnly = false
        };

        // Act
        var strategy = selector.SelectStrategy(brief, systemProfile, planSpec);

        // Assert
        Assert.True(strategy.EnableEarlyFallback);
        Assert.True(strategy.ContentComplexity > 0.5);
    }

    [Fact]
    public void SelectStrategy_WithTechnicalTopic_ShouldPreferAIVisuals()
    {
        // Arrange
        var selector = new StrategySelector(_logger);
        var brief = new Brief("Programming in Python", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(5), Pacing.Conversational, Density.Balanced, "Modern");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = 8,
            PhysicalCores = 4,
            RamGB = 16,
            OfflineOnly = false,
            EnableSD = true
        };

        // Act
        var strategy = selector.SelectStrategy(brief, systemProfile, planSpec);

        // Assert
        Assert.True(
            strategy.VisualApproach == VisualGenerationApproach.HybridAIFirst ||
            strategy.VisualApproach == VisualGenerationApproach.AIOnly);
    }

    [Fact]
    public void SelectStrategy_WithoutAICapability_ShouldUseStockOnly()
    {
        // Arrange
        var selector = new StrategySelector(_logger);
        var brief = new Brief("Test Topic", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(5), Pacing.Conversational, Density.Balanced, "Modern");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = 8,
            PhysicalCores = 4,
            RamGB = 16,
            OfflineOnly = false,
            EnableSD = false
        };

        // Act
        var strategy = selector.SelectStrategy(brief, systemProfile, planSpec);

        // Assert
        Assert.Equal(VisualGenerationApproach.StockOnly, strategy.VisualApproach);
    }

    [Fact]
    public void RecordStrategyPerformance_ShouldStoreMetrics()
    {
        // Arrange
        var selector = new StrategySelector(_logger);
        var strategy = new GenerationStrategy(
            StrategyType.Parallel,
            4,
            VisualGenerationApproach.HybridStockFirst,
            0.5,
            false,
            true);

        // Act
        selector.RecordStrategyPerformance(strategy, TimeSpan.FromSeconds(30), true, 0.85);

        // Assert
        var performance = selector.GetStrategyPerformance(StrategyType.Parallel);
        Assert.NotNull(performance);
        Assert.Equal(1, performance.TotalExecutions);
        Assert.Equal(1, performance.SuccessfulExecutions);
        Assert.Equal(1.0, performance.SuccessRate);
        Assert.True(performance.AverageExecutionTime > TimeSpan.Zero);
    }

    [Fact]
    public void RecordStrategyPerformance_WithMultipleExecutions_ShouldCalculateAverages()
    {
        // Arrange
        var selector = new StrategySelector(_logger);
        var strategy = new GenerationStrategy(
            StrategyType.Parallel,
            4,
            VisualGenerationApproach.HybridStockFirst,
            0.5,
            false,
            true);

        // Act
        selector.RecordStrategyPerformance(strategy, TimeSpan.FromSeconds(30), true, 0.8);
        selector.RecordStrategyPerformance(strategy, TimeSpan.FromSeconds(40), true, 0.9);
        selector.RecordStrategyPerformance(strategy, TimeSpan.FromSeconds(35), false, 0.5);

        // Assert
        var performance = selector.GetStrategyPerformance(StrategyType.Parallel);
        Assert.NotNull(performance);
        Assert.Equal(3, performance.TotalExecutions);
        Assert.Equal(2, performance.SuccessfulExecutions);
        Assert.Equal(2.0 / 3.0, performance.SuccessRate, 2);
        Assert.True(performance.AverageExecutionTime > TimeSpan.Zero);
        Assert.InRange(performance.AverageQualityScore, 0, 1);
    }

    [Fact]
    public void GetStrategyPerformance_WithNoHistory_ShouldReturnNull()
    {
        // Arrange
        var selector = new StrategySelector(_logger);

        // Act
        var performance = selector.GetStrategyPerformance(StrategyType.Adaptive);

        // Assert
        Assert.Null(performance);
    }

    [Fact]
    public void StrategyPerformance_ShouldLimitHistorySize()
    {
        // Arrange
        var selector = new StrategySelector(_logger);
        var strategy = new GenerationStrategy(
            StrategyType.Sequential,
            1,
            VisualGenerationApproach.StockOnly,
            0.3,
            false,
            true);

        // Act - Record more than 100 executions
        for (int i = 0; i < 150; i++)
        {
            selector.RecordStrategyPerformance(strategy, TimeSpan.FromSeconds(10), true, 0.8);
        }

        // Assert
        var performance = selector.GetStrategyPerformance(StrategyType.Sequential);
        Assert.NotNull(performance);
        Assert.Equal(100, performance.TotalExecutions); // Should be capped at 100
    }
}
