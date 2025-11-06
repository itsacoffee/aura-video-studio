using System.Collections.Generic;
using Aura.Core.Models.CostTracking;
using Aura.Core.Telemetry.Costing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for enhanced budget checking with soft/hard thresholds
/// </summary>
public class BudgetCheckerTests
{
    private readonly BudgetChecker _checker;
    private readonly CostEstimatorService _estimator;
    
    public BudgetCheckerTests()
    {
        _estimator = new CostEstimatorService(NullLogger<CostEstimatorService>.Instance);
        _checker = new BudgetChecker(
            NullLogger<BudgetChecker>.Instance,
            _estimator);
    }
    
    [Fact]
    public void CheckBudget_WithinBudget_NoWarnings()
    {
        // Arrange
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 100m,
            HardBudgetLimit = false,
            AlertThresholds = new List<int> { 50, 75, 90 }
        };
        
        // Act
        var result = _checker.CheckBudget(config, 10m, 5m, "OpenAI");
        
        // Assert
        Assert.True(result.IsWithinBudget);
        Assert.False(result.ShouldBlock);
        Assert.False(result.ShouldWarn);
        Assert.Equal(BudgetThresholdType.None, result.ThresholdType);
        Assert.Empty(result.Warnings);
        Assert.Equal(85m, result.BudgetRemaining);
    }
    
    [Fact]
    public void CheckBudget_SoftThreshold_Warning()
    {
        // Arrange
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 100m,
            HardBudgetLimit = false,
            AlertThresholds = new List<int> { 50, 75, 90 }
        };
        
        // Act - 92% of budget used
        var result = _checker.CheckBudget(config, 90m, 2m, "OpenAI");
        
        // Assert
        Assert.True(result.IsWithinBudget);
        Assert.False(result.ShouldBlock);
        Assert.True(result.ShouldWarn);
        Assert.Equal(BudgetThresholdType.Soft, result.ThresholdType);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("WARNING"));
    }
    
    [Fact]
    public void CheckBudget_HardLimit_Blocks()
    {
        // Arrange
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 100m,
            HardBudgetLimit = true,
            AlertThresholds = new List<int> { 50, 75, 90 }
        };
        
        // Act - Would exceed budget
        var result = _checker.CheckBudget(config, 95m, 10m, "OpenAI");
        
        // Assert
        Assert.False(result.IsWithinBudget);
        Assert.True(result.ShouldBlock);
        Assert.Equal(BudgetThresholdType.Hard, result.ThresholdType);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("BLOCKED"));
    }
    
    [Fact]
    public void CheckBudget_SoftLimit_AllowsOverage()
    {
        // Arrange
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 100m,
            HardBudgetLimit = false,
            AlertThresholds = new List<int> { 50, 75, 90 }
        };
        
        // Act - Would exceed budget but soft limit
        var result = _checker.CheckBudget(config, 95m, 10m, "OpenAI");
        
        // Assert
        Assert.False(result.IsWithinBudget);
        Assert.False(result.ShouldBlock);
        Assert.True(result.ShouldWarn);
        Assert.Equal(BudgetThresholdType.Soft, result.ThresholdType);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("NOTICE"));
    }
    
    [Fact]
    public void CheckBudget_NoBudgetSet_AlwaysAllows()
    {
        // Arrange
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = null,
            HardBudgetLimit = false
        };
        
        // Act
        var result = _checker.CheckBudget(config, 1000m, 500m, "OpenAI");
        
        // Assert
        Assert.True(result.IsWithinBudget);
        Assert.False(result.ShouldBlock);
        Assert.False(result.ShouldWarn);
        Assert.Equal(BudgetThresholdType.None, result.ThresholdType);
    }
    
    [Fact]
    public void CheckBudget_ProviderBudget_HardLimit()
    {
        // Arrange
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 1000m,
            HardBudgetLimit = true,
            ProviderBudgets = new Dictionary<string, decimal>
            {
                ["OpenAI"] = 50m
            }
        };
        
        // Act - Overall budget OK, but provider budget exceeded
        var result = _checker.CheckBudget(config, 45m, 10m, "OpenAI");
        
        // Assert
        Assert.False(result.IsWithinBudget);
        Assert.True(result.ShouldBlock);
        Assert.Equal(BudgetThresholdType.Hard, result.ThresholdType);
        Assert.Contains(result.Warnings, w => w.Contains("OpenAI") && w.Contains("BLOCKED"));
    }
    
    [Fact]
    public void CheckBudget_ProviderBudget_SoftWarning()
    {
        // Arrange
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 1000m,
            HardBudgetLimit = false,
            ProviderBudgets = new Dictionary<string, decimal>
            {
                ["ElevenLabs"] = 50m
            }
        };
        
        // Act - 95% of provider budget used
        var result = _checker.CheckBudget(config, 45m, 2.5m, "ElevenLabs");
        
        // Assert
        Assert.True(result.IsWithinBudget);
        Assert.False(result.ShouldBlock);
        Assert.True(result.ShouldWarn);
        Assert.Contains(result.Warnings, w => w.Contains("ElevenLabs") && w.Contains("WARNING"));
    }
    
    [Fact]
    public void CheckBudget_MultipleThresholds_ShowsHighest()
    {
        // Arrange
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 100m,
            HardBudgetLimit = false,
            AlertThresholds = new List<int> { 50, 75, 90 }
        };
        
        // Act - 80% used, should trigger 75% threshold
        var result = _checker.CheckBudget(config, 70m, 10m, "OpenAI");
        
        // Assert
        Assert.True(result.IsWithinBudget);
        Assert.True(result.ShouldWarn);
        Assert.NotEmpty(result.Warnings);
        // Should mention 75% threshold
        Assert.Contains(result.Warnings, w => w.Contains("75"));
    }
    
    [Fact]
    public void CheckBudget_ApproachingBudget_SpecificWarning()
    {
        // Arrange
        var config = new CostTrackingConfiguration
        {
            OverallMonthlyBudget = 100m,
            HardBudgetLimit = false,
            AlertThresholds = new List<int> { 50, 75, 90 },
            Currency = "USD"
        };
        
        // Act - 95% used
        var result = _checker.CheckBudget(config, 90m, 5m, "OpenAI");
        
        // Assert
        Assert.True(result.IsWithinBudget);
        Assert.True(result.ShouldWarn);
        Assert.Contains(result.Warnings, w => w.Contains("WARNING") && w.Contains("Approaching"));
    }
    
    [Fact]
    public void CalculateAccumulatedCost_SkipsFailedRetries()
    {
        // Arrange
        var records = new[]
        {
            new Aura.Core.Telemetry.RunTelemetryRecord
            {
                JobId = "job1",
                CorrelationId = "corr1",
                Stage = Aura.Core.Telemetry.RunStage.Script,
                Provider = "OpenAI",
                TokensIn = 1000,
                TokensOut = 2000,
                Retries = 1,
                ResultStatus = Aura.Core.Telemetry.ResultStatus.Error,
                LatencyMs = 500,
                StartedAt = System.DateTime.UtcNow,
                EndedAt = System.DateTime.UtcNow.AddMilliseconds(500)
            },
            new Aura.Core.Telemetry.RunTelemetryRecord
            {
                JobId = "job1",
                CorrelationId = "corr1",
                Stage = Aura.Core.Telemetry.RunStage.Script,
                Provider = "OpenAI",
                TokensIn = 1000,
                TokensOut = 2000,
                Retries = 1,
                ResultStatus = Aura.Core.Telemetry.ResultStatus.Ok,
                LatencyMs = 500,
                StartedAt = System.DateTime.UtcNow,
                EndedAt = System.DateTime.UtcNow.AddMilliseconds(500)
            }
        };
        
        // Act
        var total = _checker.CalculateAccumulatedCost(records);
        
        // Assert
        // Should only count the successful retry, not the failed one
        var expectedCost = (1000m / 1000m * 0.03m) + (2000m / 1000m * 0.06m);
        Assert.Equal(expectedCost, total);
    }
    
    [Fact]
    public void CalculateAccumulatedCost_HandlesCacheHits()
    {
        // Arrange
        var records = new[]
        {
            new Aura.Core.Telemetry.RunTelemetryRecord
            {
                JobId = "job1",
                CorrelationId = "corr1",
                Stage = Aura.Core.Telemetry.RunStage.Script,
                Provider = "OpenAI",
                TokensIn = 1000,
                TokensOut = 2000,
                CacheHit = true,
                ResultStatus = Aura.Core.Telemetry.ResultStatus.Ok,
                LatencyMs = 100,
                StartedAt = System.DateTime.UtcNow,
                EndedAt = System.DateTime.UtcNow.AddMilliseconds(100)
            }
        };
        
        // Act
        var total = _checker.CalculateAccumulatedCost(records);
        
        // Assert
        // Cache hit should result in reduced or zero cost
        var fullCost = (1000m / 1000m * 0.03m) + (2000m / 1000m * 0.06m);
        Assert.True(total < fullCost, "Cache hit should reduce cost");
    }
}
