using System;
using System.Linq;
using Aura.Core.Telemetry;
using Aura.Core.Telemetry.Costing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for the CostEstimatorService with versioned pricing
/// </summary>
public class CostEstimatorServiceTests
{
    private readonly CostEstimatorService _service;
    
    public CostEstimatorServiceTests()
    {
        _service = new CostEstimatorService(NullLogger<CostEstimatorService>.Instance);
    }
    
    [Fact]
    public void EstimateCost_OpenAI_WithTokenCounts_CalculatesCorrectly()
    {
        // Arrange
        var record = new RunTelemetryRecord
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            Stage = RunStage.Script,
            Provider = "OpenAI",
            TokensIn = 1000,
            TokensOut = 2000,
            LatencyMs = 500,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow.AddMilliseconds(500)
        };
        
        // Act
        var estimate = _service.EstimateCost(record);
        
        // Assert
        Assert.NotNull(estimate);
        Assert.True(estimate.Amount > 0);
        Assert.Equal("USD", estimate.Currency);
        Assert.NotNull(estimate.PricingVersion);
        
        // OpenAI pricing: $0.03 per 1K input, $0.06 per 1K output
        var expectedCost = (1000m / 1000m * 0.03m) + (2000m / 1000m * 0.06m);
        Assert.Equal(expectedCost, estimate.Amount);
    }
    
    [Fact]
    public void EstimateCost_CacheHit_ReturnsZeroOrReducedCost()
    {
        // Arrange
        var record = new RunTelemetryRecord
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            Stage = RunStage.Script,
            Provider = "OpenAI",
            TokensIn = 1000,
            TokensOut = 2000,
            CacheHit = true,
            LatencyMs = 100,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow.AddMilliseconds(100)
        };
        
        // Act
        var estimate = _service.EstimateCost(record);
        
        // Assert
        Assert.NotNull(estimate);
        
        // Cache hit should result in reduced or zero cost
        var fullCost = (1000m / 1000m * 0.03m) + (2000m / 1000m * 0.06m);
        Assert.True(estimate.Amount < fullCost, "Cache hit should reduce cost");
    }
    
    [Fact]
    public void EstimateCost_FreeProvider_ReturnsZero()
    {
        // Arrange
        var record = new RunTelemetryRecord
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            Stage = RunStage.Script,
            Provider = "Ollama",
            TokensIn = 1000,
            TokensOut = 2000,
            LatencyMs = 500,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow.AddMilliseconds(500)
        };
        
        // Act
        var estimate = _service.EstimateCost(record);
        
        // Assert
        Assert.NotNull(estimate);
        Assert.Equal(0m, estimate.Amount);
        Assert.Equal(CostConfidence.Exact, estimate.Confidence);
        Assert.Contains("free", estimate.Notes?.ToLowerInvariant() ?? "");
    }
    
    [Fact]
    public void EstimateCost_WithRetries_CostIncludesAllAttempts()
    {
        // Arrange
        var record = new RunTelemetryRecord
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            Stage = RunStage.Script,
            Provider = "OpenAI",
            TokensIn = 1000,
            TokensOut = 2000,
            Retries = 2,
            LatencyMs = 1500,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow.AddMilliseconds(1500)
        };
        
        // Act
        var estimate = _service.EstimateCost(record);
        
        // Assert
        Assert.NotNull(estimate);
        Assert.True(estimate.Amount > 0);
        
        // Cost should be based on actual tokens used (which includes retries)
        Assert.Contains("retry", estimate.Notes?.ToLowerInvariant() ?? "");
    }
    
    [Fact]
    public void EstimateCost_Anthropic_UsesCorrectPricing()
    {
        // Arrange
        var record = new RunTelemetryRecord
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            Stage = RunStage.Script,
            Provider = "Anthropic",
            TokensIn = 1000,
            TokensOut = 2000,
            LatencyMs = 500,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow.AddMilliseconds(500)
        };
        
        // Act
        var estimate = _service.EstimateCost(record);
        
        // Assert
        Assert.NotNull(estimate);
        
        // Anthropic pricing: $0.025 per 1K input, $0.075 per 1K output
        var expectedCost = (1000m / 1000m * 0.025m) + (2000m / 1000m * 0.075m);
        Assert.Equal(expectedCost, estimate.Amount);
    }
    
    [Fact]
    public void EstimateCost_Gemini_UsesCorrectPricing()
    {
        // Arrange
        var record = new RunTelemetryRecord
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            Stage = RunStage.Script,
            Provider = "Gemini",
            TokensIn = 1000,
            TokensOut = 2000,
            LatencyMs = 500,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow.AddMilliseconds(500)
        };
        
        // Act
        var estimate = _service.EstimateCost(record);
        
        // Assert
        Assert.NotNull(estimate);
        
        // Gemini pricing: very low cost
        var expectedCost = (1000m / 1000m * 0.00025m) + (2000m / 1000m * 0.0005m);
        Assert.Equal(expectedCost, estimate.Amount);
        Assert.True(estimate.Amount < 0.01m, "Gemini should be very cheap");
    }
    
    [Fact]
    public void EstimateCost_NoProvider_ReturnsZeroWithLowConfidence()
    {
        // Arrange
        var record = new RunTelemetryRecord
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            Stage = RunStage.Script,
            LatencyMs = 500,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow.AddMilliseconds(500)
        };
        
        // Act
        var estimate = _service.EstimateCost(record);
        
        // Assert
        Assert.NotNull(estimate);
        Assert.Equal(0m, estimate.Amount);
        Assert.Equal(CostConfidence.None, estimate.Confidence);
    }
    
    [Fact]
    public void EstimateCost_PartialScene_SceneIndexIncluded()
    {
        // Arrange
        var record = new RunTelemetryRecord
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            Stage = RunStage.Tts,
            SceneIndex = 3,
            Provider = "ElevenLabs",
            LatencyMs = 500,
            ResultStatus = ResultStatus.Ok,
            StartedAt = DateTime.UtcNow,
            EndedAt = DateTime.UtcNow.AddMilliseconds(500),
            Metadata = new System.Collections.Generic.Dictionary<string, object>
            {
                ["characters"] = 500
            }
        };
        
        // Act
        var estimate = _service.EstimateCost(record);
        
        // Assert
        Assert.NotNull(estimate);
        Assert.Contains("Scene 3", estimate.Notes ?? "");
    }
    
    [Fact]
    public void PricingVersionTable_GetVersionFor_ReturnsCorrectVersion()
    {
        // Arrange
        var table = new PricingVersionTable();
        var version1 = new PricingVersion
        {
            Version = "2024.1",
            ValidFrom = new DateTime(2024, 1, 1),
            ValidUntil = new DateTime(2024, 6, 30),
            ProviderName = "OpenAI",
            Currency = "USD",
            CostPer1KInputTokens = 0.03m
        };
        var version2 = new PricingVersion
        {
            Version = "2024.2",
            ValidFrom = new DateTime(2024, 7, 1),
            ProviderName = "OpenAI",
            Currency = "USD",
            CostPer1KInputTokens = 0.025m
        };
        
        table.AddVersion(version1);
        table.AddVersion(version2);
        
        // Act
        var oldPricing = table.GetVersionFor("OpenAI", new DateTime(2024, 3, 15));
        var newPricing = table.GetVersionFor("OpenAI", new DateTime(2024, 8, 1));
        
        // Assert
        Assert.NotNull(oldPricing);
        Assert.Equal("2024.1", oldPricing.Version);
        Assert.Equal(0.03m, oldPricing.CostPer1KInputTokens);
        
        Assert.NotNull(newPricing);
        Assert.Equal("2024.2", newPricing.Version);
        Assert.Equal(0.025m, newPricing.CostPer1KInputTokens);
    }
    
    [Fact]
    public void PricingVersionTable_InvalidateVersion_SetsValidUntil()
    {
        // Arrange
        var table = new PricingVersionTable();
        var version = new PricingVersion
        {
            Version = "2024.1",
            ValidFrom = new DateTime(2024, 1, 1),
            ProviderName = "OpenAI",
            Currency = "USD",
            CostPer1KInputTokens = 0.03m
        };
        
        table.AddVersion(version);
        
        // Act
        table.InvalidateVersion("OpenAI", "2024.1", new DateTime(2024, 6, 30));
        var pricing = table.GetVersionFor("OpenAI", new DateTime(2024, 7, 1));
        
        // Assert
        Assert.Null(pricing);
    }
}
