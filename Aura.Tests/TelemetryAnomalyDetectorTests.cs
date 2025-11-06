using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Services.Diagnostics;
using Aura.Core.Telemetry;
using Xunit;

namespace Aura.Tests;

public class TelemetryAnomalyDetectorTests
{
    [Fact]
    public void DetectAnomalies_WithNullTelemetry_ReturnsEmptyAnomalies()
    {
        // Act
        var anomalies = TelemetryAnomalyDetector.DetectAnomalies(null);

        // Assert
        Assert.NotNull(anomalies);
        Assert.Empty(anomalies.CostAnomalies);
        Assert.Empty(anomalies.LatencyAnomalies);
        Assert.Empty(anomalies.ProviderIssues);
        Assert.Empty(anomalies.RetryPatterns);
        Assert.False(anomalies.HasAnyAnomalies);
    }

    [Fact]
    public void DetectAnomalies_WithEmptyRecords_ReturnsEmptyAnomalies()
    {
        // Arrange
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-correlation",
            CollectionStartedAt = DateTime.UtcNow,
            Records = new List<RunTelemetryRecord>()
        };

        // Act
        var anomalies = TelemetryAnomalyDetector.DetectAnomalies(telemetry);

        // Assert
        Assert.NotNull(anomalies);
        Assert.Empty(anomalies.CostAnomalies);
        Assert.False(anomalies.HasAnyAnomalies);
    }

    [Fact]
    public void DetectAnomalies_DetectsHighCostStage()
    {
        // Arrange
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-correlation",
            CollectionStartedAt = DateTime.UtcNow,
            Records = new List<RunTelemetryRecord>
            {
                // Normal cost stage
                CreateRecord(RunStage.Script, costEstimate: 0.01m),
                CreateRecord(RunStage.Script, costEstimate: 0.01m),
                
                // Very high cost stage
                CreateRecord(RunStage.Tts, costEstimate: 1.5m),
                CreateRecord(RunStage.Tts, costEstimate: 0.05m),
                
                // Normal cost stage
                CreateRecord(RunStage.Visuals, costEstimate: 0.02m)
            }
        };

        // Act
        var anomalies = TelemetryAnomalyDetector.DetectAnomalies(telemetry);

        // Assert
        Assert.NotEmpty(anomalies.CostAnomalies);
        var costAnomaly = anomalies.CostAnomalies.First();
        Assert.Equal("Tts", costAnomaly.Stage);
        Assert.Equal(AnomalySeverity.High, costAnomaly.Severity);
        Assert.Contains("$1.5", costAnomaly.Description);
    }

    [Fact]
    public void DetectAnomalies_DetectsHighLatency()
    {
        // Arrange
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-correlation",
            CollectionStartedAt = DateTime.UtcNow,
            Records = new List<RunTelemetryRecord>
            {
                // Normal latency
                CreateRecord(RunStage.Script, latencyMs: 5000),
                CreateRecord(RunStage.Script, latencyMs: 4000),
                
                // Very high latency (> 60 seconds)
                CreateRecord(RunStage.Tts, latencyMs: 95000),
                CreateRecord(RunStage.Tts, latencyMs: 3000)
            }
        };

        // Act
        var anomalies = TelemetryAnomalyDetector.DetectAnomalies(telemetry);

        // Assert
        Assert.NotEmpty(anomalies.LatencyAnomalies);
        var latencyAnomaly = anomalies.LatencyAnomalies.First();
        Assert.Equal("Tts", latencyAnomaly.Stage);
        Assert.Contains("95.0 seconds", latencyAnomaly.Description);
    }

    [Fact]
    public void DetectAnomalies_DetectsHighErrorRate()
    {
        // Arrange
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-correlation",
            CollectionStartedAt = DateTime.UtcNow,
            Records = new List<RunTelemetryRecord>
            {
                // Provider with high error rate
                CreateRecord(RunStage.Tts, provider: "ElevenLabs", resultStatus: ResultStatus.Error, errorCode: "E429"),
                CreateRecord(RunStage.Tts, provider: "ElevenLabs", resultStatus: ResultStatus.Error, errorCode: "E429"),
                CreateRecord(RunStage.Tts, provider: "ElevenLabs", resultStatus: ResultStatus.Error, errorCode: "E500"),
                CreateRecord(RunStage.Tts, provider: "ElevenLabs", resultStatus: ResultStatus.Ok),
                CreateRecord(RunStage.Tts, provider: "ElevenLabs", resultStatus: ResultStatus.Error, errorCode: "E429")
            }
        };

        // Act
        var anomalies = TelemetryAnomalyDetector.DetectAnomalies(telemetry);

        // Assert
        Assert.NotEmpty(anomalies.ProviderIssues);
        var providerIssue = anomalies.ProviderIssues.First();
        Assert.Equal("ElevenLabs", providerIssue.Provider);
        Assert.Equal("HighErrorRate", providerIssue.IssueType);
        Assert.Equal(4, providerIssue.ErrorCount);
        Assert.Equal(5, providerIssue.TotalOperations);
        Assert.Contains("E429", providerIssue.ErrorCodes);
        Assert.Contains("E500", providerIssue.ErrorCodes);
    }

    [Fact]
    public void DetectAnomalies_DetectsExcessiveRetries()
    {
        // Arrange
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-correlation",
            CollectionStartedAt = DateTime.UtcNow,
            Records = new List<RunTelemetryRecord>
            {
                CreateRecord(RunStage.Tts, provider: "PlayHT", retries: 5),
                CreateRecord(RunStage.Tts, provider: "PlayHT", retries: 3),
                CreateRecord(RunStage.Tts, provider: "PlayHT", retries: 4)
            }
        };

        // Act
        var anomalies = TelemetryAnomalyDetector.DetectAnomalies(telemetry);

        // Assert
        Assert.NotEmpty(anomalies.ProviderIssues);
        var retryIssue = anomalies.ProviderIssues.First(i => i.IssueType == "ExcessiveRetries");
        Assert.Equal("PlayHT", retryIssue.Provider);
        Assert.Equal(12, retryIssue.TotalRetries); // 5 + 3 + 4
        Assert.Equal(3, retryIssue.TotalOperations);
    }

    [Fact]
    public void DetectAnomalies_DetectsRetryPatterns()
    {
        // Arrange
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-correlation",
            CollectionStartedAt = DateTime.UtcNow,
            Records = new List<RunTelemetryRecord>
            {
                CreateRecord(RunStage.Script, retries: 2),
                CreateRecord(RunStage.Script, retries: 1),
                CreateRecord(RunStage.Tts, retries: 5),
                CreateRecord(RunStage.Tts, retries: 4),
                CreateRecord(RunStage.Tts, retries: 3)
            }
        };

        // Act
        var anomalies = TelemetryAnomalyDetector.DetectAnomalies(telemetry);

        // Assert
        Assert.NotEmpty(anomalies.RetryPatterns);
        var ttsPattern = anomalies.RetryPatterns.First(p => p.Stage == "Tts");
        Assert.Equal(12, ttsPattern.TotalRetries); // 5 + 4 + 3
        Assert.Equal(3, ttsPattern.OperationsWithRetries);
        Assert.Equal(4.0, ttsPattern.AvgRetriesPerOperation);
        Assert.Equal(AnomalySeverity.High, ttsPattern.Severity);
    }

    [Fact]
    public void DetectAnomalies_DetectsMultipleAnomaliesSimultaneously()
    {
        // Arrange
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-correlation",
            CollectionStartedAt = DateTime.UtcNow,
            Records = new List<RunTelemetryRecord>
            {
                // High cost
                CreateRecord(RunStage.Tts, costEstimate: 2.0m, latencyMs: 150000, provider: "ElevenLabs", resultStatus: ResultStatus.Error),
                CreateRecord(RunStage.Tts, costEstimate: 0.5m, latencyMs: 5000, provider: "ElevenLabs", resultStatus: ResultStatus.Ok),
                
                // Normal operations
                CreateRecord(RunStage.Script, costEstimate: 0.01m, latencyMs: 3000)
            }
        };

        // Act
        var anomalies = TelemetryAnomalyDetector.DetectAnomalies(telemetry);

        // Assert
        Assert.True(anomalies.HasAnyAnomalies);
        Assert.NotEmpty(anomalies.CostAnomalies); // High cost
        Assert.NotEmpty(anomalies.LatencyAnomalies); // High latency
        Assert.NotEmpty(anomalies.ProviderIssues); // Error rate
    }

    [Fact]
    public void DetectAnomalies_IgnoresLowSeverityIssues()
    {
        // Arrange
        var telemetry = new RunTelemetryCollection
        {
            JobId = "test-job",
            CorrelationId = "test-correlation",
            CollectionStartedAt = DateTime.UtcNow,
            Records = new List<RunTelemetryRecord>
            {
                // Low cost, low latency, no errors
                CreateRecord(RunStage.Script, costEstimate: 0.001m, latencyMs: 1000),
                CreateRecord(RunStage.Tts, costEstimate: 0.002m, latencyMs: 2000),
                CreateRecord(RunStage.Visuals, costEstimate: 0.001m, latencyMs: 1500)
            }
        };

        // Act
        var anomalies = TelemetryAnomalyDetector.DetectAnomalies(telemetry);

        // Assert
        Assert.False(anomalies.HasAnyAnomalies);
        Assert.Empty(anomalies.CostAnomalies);
        Assert.Empty(anomalies.LatencyAnomalies);
        Assert.Empty(anomalies.ProviderIssues);
    }

    private static RunTelemetryRecord CreateRecord(
        RunStage stage,
        decimal costEstimate = 0.01m,
        long latencyMs = 1000,
        string? provider = null,
        ResultStatus resultStatus = ResultStatus.Ok,
        string? errorCode = null,
        int retries = 0)
    {
        return new RunTelemetryRecord
        {
            JobId = "test-job",
            CorrelationId = "test-correlation",
            Stage = stage,
            Provider = provider,
            CostEstimate = costEstimate,
            LatencyMs = latencyMs,
            ResultStatus = resultStatus,
            ErrorCode = errorCode,
            Retries = retries,
            StartedAt = DateTime.UtcNow.AddSeconds(-10),
            EndedAt = DateTime.UtcNow
        };
    }
}
