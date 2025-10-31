using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Performance;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class LatencyManagementServiceTests
{
    private readonly ILogger<LatencyManagementService> _logger;
    private readonly ILogger<LatencyTelemetry> _telemetryLogger;
    private readonly LatencyTelemetry _telemetry;
    private readonly LlmTimeoutPolicy _defaultPolicy;

    public LatencyManagementServiceTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        _logger = loggerFactory.CreateLogger<LatencyManagementService>();
        _telemetryLogger = loggerFactory.CreateLogger<LatencyTelemetry>();
        _telemetry = new LatencyTelemetry(_telemetryLogger);
        _defaultPolicy = new LlmTimeoutPolicy();
    }

    [Fact]
    public void RecordMetrics_AddsMetricsToHistoricalData()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);
        var metrics = new LatencyMetrics
        {
            ProviderName = "OpenAI",
            OperationType = "ScriptGeneration",
            PromptTokenCount = 500,
            ResponseTimeMs = 15000,
            Success = true,
            RetryCount = 0
        };

        // Act
        service.RecordMetrics(metrics);
        var summary = service.GetPerformanceSummary("OpenAI", "ScriptGeneration");

        // Assert
        Assert.Equal(1, summary.DataPointCount);
        Assert.Equal(15000, summary.AverageResponseTimeMs);
        Assert.Equal(1.0, summary.SuccessRate);
    }

    [Fact]
    public void PredictDuration_WithNoHistoricalData_ReturnsDefaultEstimate()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);

        // Act
        var estimate = service.PredictDuration("OpenAI", "ScriptGeneration", 500);

        // Assert
        Assert.True(estimate.EstimatedSeconds > 0);
        Assert.True(estimate.Confidence < 0.5);
        Assert.Contains("estimated", estimate.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PredictDuration_WithHistoricalData_ReturnsAccurateEstimate()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);
        
        for (int i = 0; i < 15; i++)
        {
            service.RecordMetrics(new LatencyMetrics
            {
                ProviderName = "OpenAI",
                OperationType = "ScriptGeneration",
                PromptTokenCount = 500,
                ResponseTimeMs = 15000 + (i * 100),
                Success = true,
                RetryCount = 0
            });
        }

        // Act
        var estimate = service.PredictDuration("OpenAI", "ScriptGeneration", 500);

        // Assert
        Assert.True(estimate.EstimatedSeconds >= 15);
        Assert.True(estimate.EstimatedSeconds <= 20);
        Assert.True(estimate.Confidence >= 0.6);
        Assert.Contains("typically", estimate.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PredictDuration_AdjustsForTokenCount()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);
        
        for (int i = 0; i < 15; i++)
        {
            service.RecordMetrics(new LatencyMetrics
            {
                ProviderName = "OpenAI",
                OperationType = "ScriptGeneration",
                PromptTokenCount = 500,
                ResponseTimeMs = 10000,
                Success = true,
                RetryCount = 0
            });
        }

        // Act - Request prediction for 2x token count
        var estimate = service.PredictDuration("OpenAI", "ScriptGeneration", 1000);

        // Assert - Should estimate roughly 2x the time
        Assert.True(estimate.EstimatedSeconds >= 18);
        Assert.True(estimate.EstimatedSeconds <= 25);
    }

    [Fact]
    public void GetTimeoutSeconds_ReturnsCorrectTimeout_ForScriptGeneration()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);

        // Act
        var timeout = service.GetTimeoutSeconds("ScriptGeneration");

        // Assert
        Assert.Equal(120, timeout);
    }

    [Fact]
    public void GetTimeoutSeconds_ReturnsCorrectTimeout_ForVisualPrompt()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);

        // Act
        var timeout = service.GetTimeoutSeconds("VisualPrompt");

        // Assert
        Assert.Equal(45, timeout);
    }

    [Fact]
    public void ShouldWarnTimeout_ReturnsFalse_WhenBelowThreshold()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);

        // Act - 30 seconds elapsed out of 120 second timeout (25%)
        var shouldWarn = service.ShouldWarnTimeout("ScriptGeneration", 30);

        // Assert
        Assert.False(shouldWarn);
    }

    [Fact]
    public void ShouldWarnTimeout_ReturnsTrue_WhenAboveThreshold()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);

        // Act - 70 seconds elapsed out of 120 second timeout (58%)
        var shouldWarn = service.ShouldWarnTimeout("ScriptGeneration", 70);

        // Assert
        Assert.True(shouldWarn);
    }

    [Fact]
    public void GetPerformanceSummary_CalculatesSuccessRate()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);
        
        for (int i = 0; i < 10; i++)
        {
            service.RecordMetrics(new LatencyMetrics
            {
                ProviderName = "OpenAI",
                OperationType = "ScriptGeneration",
                PromptTokenCount = 500,
                ResponseTimeMs = 15000,
                Success = i < 8,
                RetryCount = i >= 8 ? 3 : 0
            });
        }

        // Act
        var summary = service.GetPerformanceSummary("OpenAI", "ScriptGeneration");

        // Assert
        Assert.Equal(10, summary.DataPointCount);
        Assert.Equal(0.8, summary.SuccessRate);
        Assert.True(summary.AverageRetryCount > 0);
    }

    [Fact]
    public void RecordMetrics_MaintainsMaximumHistorySize()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);
        
        for (int i = 0; i < 150; i++)
        {
            service.RecordMetrics(new LatencyMetrics
            {
                ProviderName = "OpenAI",
                OperationType = "ScriptGeneration",
                PromptTokenCount = 500,
                ResponseTimeMs = 15000,
                Success = true,
                RetryCount = 0
            });
        }

        // Act
        var summary = service.GetPerformanceSummary("OpenAI", "ScriptGeneration");

        // Assert - Should maintain max 100 records
        Assert.Equal(100, summary.DataPointCount);
    }

    [Fact]
    public void PredictDuration_WithMixedProviders_ReturnsProviderSpecificEstimate()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);
        
        for (int i = 0; i < 15; i++)
        {
            service.RecordMetrics(new LatencyMetrics
            {
                ProviderName = "OpenAI",
                OperationType = "ScriptGeneration",
                PromptTokenCount = 500,
                ResponseTimeMs = 10000,
                Success = true,
                RetryCount = 0
            });
            
            service.RecordMetrics(new LatencyMetrics
            {
                ProviderName = "Ollama",
                OperationType = "ScriptGeneration",
                PromptTokenCount = 500,
                ResponseTimeMs = 30000,
                Success = true,
                RetryCount = 0
            });
        }

        // Act
        var openAiEstimate = service.PredictDuration("OpenAI", "ScriptGeneration", 500);
        var ollamaEstimate = service.PredictDuration("Ollama", "ScriptGeneration", 500);

        // Assert - Ollama should have significantly higher estimate
        Assert.True(ollamaEstimate.EstimatedSeconds > openAiEstimate.EstimatedSeconds * 2);
    }

    [Fact]
    public void GetTimeoutSeconds_ReturnsDefaultForUnknownOperation()
    {
        // Arrange
        var service = new LatencyManagementService(_logger, _telemetry, _defaultPolicy);

        // Act
        var timeout = service.GetTimeoutSeconds("UnknownOperation");

        // Assert
        Assert.Equal(120, timeout);
    }

    [Fact]
    public void PredictDuration_WithCustomPolicy_UsesCustomTimeouts()
    {
        // Arrange
        var customPolicy = new LlmTimeoutPolicy
        {
            ScriptGenerationTimeoutSeconds = 300
        };
        var service = new LatencyManagementService(_logger, _telemetry, customPolicy);

        // Act
        var timeout = service.GetTimeoutSeconds("ScriptGeneration");
        var estimate = service.PredictDuration("OpenAI", "ScriptGeneration", 500);

        // Assert
        Assert.Equal(300, timeout);
        Assert.True(estimate.MaxSeconds <= 300);
    }
}
