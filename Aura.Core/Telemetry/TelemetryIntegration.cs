using System;
using System.Diagnostics;
using Aura.Core.AI.Orchestration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Telemetry;

/// <summary>
/// Helper service for integrating telemetry collection into existing code
/// Provides extension methods and utilities for easy telemetry emission
/// </summary>
public class TelemetryIntegration
{
    private readonly ILogger<TelemetryIntegration> _logger;
    private readonly RunTelemetryCollector _collector;
    
    public TelemetryIntegration(
        ILogger<TelemetryIntegration> logger,
        RunTelemetryCollector collector)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _collector = collector ?? throw new ArgumentNullException(nameof(collector));
    }
    
    /// <summary>
    /// Record telemetry from LlmOperationTelemetry (existing telemetry)
    /// Maps existing LLM telemetry to new RunTelemetry format
    /// </summary>
    public void RecordLlmOperation(
        string jobId,
        string correlationId,
        RunStage stage,
        LlmOperationTelemetry llmTelemetry)
    {
        try
        {
            var record = new RunTelemetryRecord
            {
                JobId = jobId,
                CorrelationId = correlationId,
                Stage = stage,
                ModelId = llmTelemetry.ModelName,
                Provider = llmTelemetry.ProviderName,
                SelectionSource = SelectionSource.Default,
                TokensIn = llmTelemetry.TokensIn,
                TokensOut = llmTelemetry.TokensOut,
                CacheHit = llmTelemetry.CacheHit,
                Retries = llmTelemetry.RetryCount,
                LatencyMs = llmTelemetry.LatencyMs,
                CostEstimate = llmTelemetry.EstimatedCost,
                ResultStatus = llmTelemetry.Success ? ResultStatus.Ok : ResultStatus.Error,
                ErrorCode = llmTelemetry.Success ? null : "LLM_ERROR",
                Message = llmTelemetry.ErrorMessage,
                StartedAt = llmTelemetry.StartedAt,
                EndedAt = llmTelemetry.CompletedAt
            };
            
            _collector.Record(record);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record LLM telemetry for job {JobId}", jobId);
        }
    }
    
    /// <summary>
    /// Create a simple stage telemetry record for non-LLM operations
    /// </summary>
    public void RecordStage(
        string jobId,
        string correlationId,
        RunStage stage,
        long latencyMs,
        ResultStatus status = ResultStatus.Ok,
        string? message = null,
        string? errorCode = null)
    {
        try
        {
            var record = new RunTelemetryRecord
            {
                JobId = jobId,
                CorrelationId = correlationId,
                Stage = stage,
                LatencyMs = latencyMs,
                ResultStatus = status,
                Message = message,
                ErrorCode = errorCode,
                StartedAt = DateTime.UtcNow.AddMilliseconds(-latencyMs),
                EndedAt = DateTime.UtcNow
            };
            
            _collector.Record(record);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record stage telemetry for job {JobId}, stage {Stage}", 
                jobId, stage);
        }
    }
}

/// <summary>
/// Extension methods for Stopwatch to simplify telemetry recording
/// </summary>
public static class StopwatchTelemetryExtensions
{
    /// <summary>
    /// Record telemetry for a completed stage using a stopwatch
    /// </summary>
    public static void RecordTelemetry(
        this Stopwatch stopwatch,
        TelemetryIntegration telemetry,
        string jobId,
        string correlationId,
        RunStage stage,
        ResultStatus status = ResultStatus.Ok,
        string? message = null,
        string? errorCode = null)
    {
        telemetry.RecordStage(
            jobId,
            correlationId,
            stage,
            stopwatch.ElapsedMilliseconds,
            status,
            message,
            errorCode);
    }
}
