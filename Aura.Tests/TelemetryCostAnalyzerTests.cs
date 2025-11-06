using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Telemetry;
using Aura.Core.Telemetry.Costing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for TelemetryCostAnalyzer - post-run cost breakdown from RunTelemetry v1
/// </summary>
public class TelemetryCostAnalyzerTests
{
    private readonly TelemetryCostAnalyzer _analyzer;
    private readonly CostEstimatorService _estimator;
    
    public TelemetryCostAnalyzerTests()
    {
        _estimator = new CostEstimatorService(NullLogger<CostEstimatorService>.Instance);
        _analyzer = new TelemetryCostAnalyzer(
            NullLogger<TelemetryCostAnalyzer>.Instance,
            _estimator);
    }
    
    [Fact]
    public void AnalyzeCosts_EmptyTelemetry_ReturnsZeroCost()
    {
        // Arrange
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = DateTime.UtcNow,
            CollectionEndedAt = DateTime.UtcNow.AddSeconds(10),
            Records = new List<RunTelemetryRecord>()
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        Assert.Equal(0m, breakdown.TotalCost);
        Assert.Equal("test-job", breakdown.JobId);
        Assert.Empty(breakdown.ByStage);
        Assert.Empty(breakdown.ByProvider);
        Assert.Empty(breakdown.OperationDetails);
    }
    
    [Fact]
    public void AnalyzeCosts_SingleOperation_CalculatesCostCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = startTime,
            CollectionEndedAt = startTime.AddSeconds(10),
            Records = new List<RunTelemetryRecord>
            {
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Script,
                    Provider = "OpenAI",
                    TokensIn = 1000,
                    TokensOut = 2000,
                    LatencyMs = 500,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime,
                    EndedAt = startTime.AddMilliseconds(500)
                }
            }
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        Assert.True(breakdown.TotalCost > 0);
        
        // OpenAI: $0.03 per 1K input, $0.06 per 1K output
        var expectedCost = (1000m / 1000m * 0.03m) + (2000m / 1000m * 0.06m);
        Assert.Equal(expectedCost, breakdown.TotalCost);
        
        // Check by-stage breakdown
        Assert.Single(breakdown.ByStage);
        Assert.True(breakdown.ByStage.ContainsKey("Script"));
        Assert.Equal(expectedCost, breakdown.ByStage["Script"].Cost);
        Assert.Equal(1, breakdown.ByStage["Script"].OperationCount);
        
        // Check by-provider breakdown
        Assert.Single(breakdown.ByProvider);
        Assert.Equal(expectedCost, breakdown.ByProvider["OpenAI"]);
        
        // Check operation details
        Assert.Single(breakdown.OperationDetails);
        var detail = breakdown.OperationDetails.First();
        Assert.Equal("Script", detail.Stage);
        Assert.Equal("OpenAI", detail.Provider);
        Assert.Equal(expectedCost, detail.Cost);
        Assert.Equal(1000, detail.TokensIn);
        Assert.Equal(2000, detail.TokensOut);
        Assert.False(detail.CacheHit);
        Assert.Equal(0, detail.Retries);
    }
    
    [Fact]
    public void AnalyzeCosts_MultipleStages_GroupsCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = startTime,
            CollectionEndedAt = startTime.AddSeconds(30),
            Records = new List<RunTelemetryRecord>
            {
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Script,
                    Provider = "OpenAI",
                    TokensIn = 1000,
                    TokensOut = 2000,
                    LatencyMs = 500,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime,
                    EndedAt = startTime.AddMilliseconds(500)
                },
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Tts,
                    Provider = "ElevenLabs",
                    LatencyMs = 1000,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime.AddSeconds(1),
                    EndedAt = startTime.AddSeconds(2),
                    Metadata = new Dictionary<string, object>
                    {
                        ["characters"] = 500
                    }
                },
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Visuals,
                    Provider = "StableDiffusion",
                    LatencyMs = 2000,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime.AddSeconds(3),
                    EndedAt = startTime.AddSeconds(5)
                }
            }
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        Assert.True(breakdown.TotalCost > 0);
        
        // Should have 3 stages
        Assert.Equal(3, breakdown.ByStage.Count);
        Assert.True(breakdown.ByStage.ContainsKey("Script"));
        Assert.True(breakdown.ByStage.ContainsKey("Tts"));
        Assert.True(breakdown.ByStage.ContainsKey("Visuals"));
        
        // Should have 3 providers
        Assert.Equal(3, breakdown.ByProvider.Count);
        
        // Should have 3 operation details
        Assert.Equal(3, breakdown.OperationDetails.Count);
    }
    
    [Fact]
    public void AnalyzeCosts_WithCacheHits_CalculatesSavings()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = startTime,
            CollectionEndedAt = startTime.AddSeconds(10),
            Records = new List<RunTelemetryRecord>
            {
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Script,
                    Provider = "OpenAI",
                    TokensIn = 1000,
                    TokensOut = 2000,
                    CacheHit = false,
                    LatencyMs = 500,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime,
                    EndedAt = startTime.AddMilliseconds(500)
                },
                new RunTelemetryRecord
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
                    StartedAt = startTime.AddSeconds(1),
                    EndedAt = startTime.AddSeconds(1).AddMilliseconds(100)
                }
            }
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        
        // Cache savings should be greater than zero
        Assert.True(breakdown.CacheSavings > 0, $"Expected positive cache savings, got {breakdown.CacheSavings}");
        
        // Total cost should be less than if both were non-cached
        var fullCostBoth = 2 * ((1000m / 1000m * 0.03m) + (2000m / 1000m * 0.06m));
        Assert.True(breakdown.TotalCost < fullCostBoth);
    }
    
    [Fact]
    public void AnalyzeCosts_WithRetries_TracksOverhead()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = startTime,
            CollectionEndedAt = startTime.AddSeconds(10),
            Records = new List<RunTelemetryRecord>
            {
                new RunTelemetryRecord
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
                    StartedAt = startTime,
                    EndedAt = startTime.AddMilliseconds(1500)
                }
            }
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        
        // Retry overhead should be tracked
        Assert.True(breakdown.RetryOverhead > 0);
        
        // Operation should show retry count
        var detail = breakdown.OperationDetails.First();
        Assert.Equal(2, detail.Retries);
    }
    
    [Fact]
    public void AnalyzeCosts_WithSceneIndex_TracksPartialScenes()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = startTime,
            CollectionEndedAt = startTime.AddSeconds(10),
            Records = new List<RunTelemetryRecord>
            {
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Tts,
                    SceneIndex = 0,
                    Provider = "ElevenLabs",
                    LatencyMs = 500,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime,
                    EndedAt = startTime.AddMilliseconds(500),
                    Metadata = new Dictionary<string, object>
                    {
                        ["characters"] = 500
                    }
                },
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Tts,
                    SceneIndex = 1,
                    Provider = "ElevenLabs",
                    LatencyMs = 500,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime.AddSeconds(1),
                    EndedAt = startTime.AddSeconds(1).AddMilliseconds(500),
                    Metadata = new Dictionary<string, object>
                    {
                        ["characters"] = 300
                    }
                }
            }
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        
        // Should have 2 operations with different scene indices
        Assert.Equal(2, breakdown.OperationDetails.Count);
        Assert.Equal(0, breakdown.OperationDetails[0].SceneIndex);
        Assert.Equal(1, breakdown.OperationDetails[1].SceneIndex);
        
        // Total cost should be sum of both scenes
        Assert.True(breakdown.TotalCost > 0);
        Assert.Equal(2, breakdown.ByStage["Tts"].OperationCount);
    }
    
    [Fact]
    public void AnalyzeCosts_FreeProvider_ZeroCost()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = startTime,
            CollectionEndedAt = startTime.AddSeconds(10),
            Records = new List<RunTelemetryRecord>
            {
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Script,
                    Provider = "Ollama",
                    TokensIn = 1000,
                    TokensOut = 2000,
                    LatencyMs = 500,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime,
                    EndedAt = startTime.AddMilliseconds(500)
                }
            }
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        Assert.Equal(0m, breakdown.TotalCost);
        Assert.Equal(0m, breakdown.ByProvider["Ollama"]);
    }
    
    [Fact]
    public void AnalyzeCosts_WithSummary_ValidatesTotal()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var expectedCost = (1000m / 1000m * 0.03m) + (2000m / 1000m * 0.06m);
        
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = startTime,
            CollectionEndedAt = startTime.AddSeconds(10),
            Records = new List<RunTelemetryRecord>
            {
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Script,
                    Provider = "OpenAI",
                    TokensIn = 1000,
                    TokensOut = 2000,
                    LatencyMs = 500,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime,
                    EndedAt = startTime.AddMilliseconds(500)
                }
            },
            Summary = new RunTelemetrySummary
            {
                TotalOperations = 1,
                SuccessfulOperations = 1,
                FailedOperations = 0,
                TotalCost = expectedCost,
                TotalTokensIn = 1000,
                TotalTokensOut = 2000,
                CacheHits = 0,
                TotalRetries = 0
            }
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        Assert.Equal(expectedCost, breakdown.TotalCost);
        Assert.Equal(1, breakdown.TotalOperations);
        Assert.Equal(1, breakdown.SuccessfulOperations);
        Assert.Equal(0, breakdown.FailedOperations);
        Assert.Equal(1000, breakdown.TotalTokensIn);
        Assert.Equal(2000, breakdown.TotalTokensOut);
    }
    
    [Fact]
    public void AnalyzeCosts_MultipleProviders_BreaksDownCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = startTime,
            CollectionEndedAt = startTime.AddSeconds(10),
            Records = new List<RunTelemetryRecord>
            {
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Script,
                    Provider = "OpenAI",
                    TokensIn = 1000,
                    TokensOut = 2000,
                    LatencyMs = 500,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime,
                    EndedAt = startTime.AddMilliseconds(500)
                },
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Script,
                    Provider = "Anthropic",
                    TokensIn = 1000,
                    TokensOut = 2000,
                    LatencyMs = 600,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime.AddSeconds(1),
                    EndedAt = startTime.AddSeconds(1).AddMilliseconds(600)
                }
            }
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        
        // Should have costs for both providers
        Assert.Equal(2, breakdown.ByProvider.Count);
        Assert.True(breakdown.ByProvider.ContainsKey("OpenAI"));
        Assert.True(breakdown.ByProvider.ContainsKey("Anthropic"));
        
        // OpenAI cost
        var openAiCost = (1000m / 1000m * 0.03m) + (2000m / 1000m * 0.06m);
        Assert.Equal(openAiCost, breakdown.ByProvider["OpenAI"]);
        
        // Anthropic cost
        var anthropicCost = (1000m / 1000m * 0.025m) + (2000m / 1000m * 0.075m);
        Assert.Equal(anthropicCost, breakdown.ByProvider["Anthropic"]);
        
        // Total should be sum
        Assert.Equal(openAiCost + anthropicCost, breakdown.TotalCost);
    }
    
    [Fact]
    public void AnalyzeCosts_AverageLatency_CalculatesCorrectly()
    {
        // Arrange
        var startTime = DateTime.UtcNow;
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-corr",
            CollectionStartedAt = startTime,
            CollectionEndedAt = startTime.AddSeconds(10),
            Records = new List<RunTelemetryRecord>
            {
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Script,
                    Provider = "OpenAI",
                    TokensIn = 1000,
                    TokensOut = 2000,
                    LatencyMs = 500,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime,
                    EndedAt = startTime.AddMilliseconds(500)
                },
                new RunTelemetryRecord
                {
                    JobId = "test-job",
                    CorrelationId = "test-corr",
                    Stage = RunStage.Script,
                    Provider = "OpenAI",
                    TokensIn = 1000,
                    TokensOut = 2000,
                    LatencyMs = 700,
                    ResultStatus = ResultStatus.Ok,
                    StartedAt = startTime.AddSeconds(1),
                    EndedAt = startTime.AddSeconds(1).AddMilliseconds(700)
                }
            }
        };
        
        // Act
        var breakdown = _analyzer.AnalyzeCosts(telemetry);
        
        // Assert
        Assert.NotNull(breakdown);
        
        // Average latency should be (500 + 700) / 2 = 600
        Assert.Equal(600, breakdown.ByStage["Script"].AverageLatencyMs);
        Assert.Equal(2, breakdown.ByStage["Script"].OperationCount);
    }
}
