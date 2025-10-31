using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Performance;

/// <summary>
/// Telemetry logging for LLM operation latency and performance metrics
/// </summary>
public class LatencyTelemetry
{
    private readonly ILogger<LatencyTelemetry> _logger;

    public LatencyTelemetry(ILogger<LatencyTelemetry> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Log detailed latency metrics for an LLM operation
    /// </summary>
    public void LogLatencyMetrics(LatencyMetrics metrics)
    {
        var logData = new
        {
            Provider = metrics.ProviderName,
            Operation = metrics.OperationType,
            PromptTokens = metrics.PromptTokenCount,
            ResponseTimeMs = metrics.ResponseTimeMs,
            Success = metrics.Success,
            RetryCount = metrics.RetryCount,
            Timestamp = metrics.Timestamp,
            Notes = metrics.Notes
        };

        if (metrics.Success)
        {
            _logger.LogInformation(
                "LLM Operation: {Provider} {Operation} completed in {ResponseTimeMs}ms (tokens: {PromptTokens}, retries: {RetryCount})",
                metrics.ProviderName,
                metrics.OperationType,
                metrics.ResponseTimeMs,
                metrics.PromptTokenCount,
                metrics.RetryCount);
        }
        else
        {
            _logger.LogWarning(
                "LLM Operation: {Provider} {Operation} failed after {ResponseTimeMs}ms (tokens: {PromptTokens}, retries: {RetryCount}). {Notes}",
                metrics.ProviderName,
                metrics.OperationType,
                metrics.ResponseTimeMs,
                metrics.PromptTokenCount,
                metrics.RetryCount,
                metrics.Notes ?? "No additional information");
        }

        // Log structured data for analytics
        _logger.LogDebug("LLM Telemetry: {TelemetryData}",
            JsonSerializer.Serialize(logData, new JsonSerializerOptions { WriteIndented = false }));
    }

    /// <summary>
    /// Log timeout warning when operation exceeds threshold
    /// </summary>
    public void LogTimeoutWarning(string providerName, string operationType, int elapsedSeconds, int timeoutSeconds)
    {
        var percentage = (double)elapsedSeconds / timeoutSeconds * 100;
        
        _logger.LogWarning(
            "LLM Operation: {Provider} {Operation} is taking longer than usual ({ElapsedSeconds}s / {TimeoutSeconds}s, {Percentage:F0}%)",
            providerName,
            operationType,
            elapsedSeconds,
            timeoutSeconds,
            percentage);
    }

    /// <summary>
    /// Log retry attempt with reason
    /// </summary>
    public void LogRetryAttempt(string providerName, string operationType, int attemptNumber, int maxAttempts, string reason, int delayMs)
    {
        _logger.LogInformation(
            "LLM Operation: {Provider} {Operation} retry {Attempt}/{MaxAttempts} due to {Reason}. Waiting {DelayMs}ms before retry",
            providerName,
            operationType,
            attemptNumber,
            maxAttempts,
            reason,
            delayMs);
    }

    /// <summary>
    /// Log successful retry after failures
    /// </summary>
    public void LogRetrySuccess(string providerName, string operationType, int totalAttempts)
    {
        _logger.LogInformation(
            "LLM Operation: {Provider} {Operation} succeeded after {TotalAttempts} attempts",
            providerName,
            operationType,
            totalAttempts);
    }

    /// <summary>
    /// Log final failure after all retries exhausted
    /// </summary>
    public void LogRetryExhausted(string providerName, string operationType, int totalAttempts, string lastError)
    {
        _logger.LogError(
            "LLM Operation: {Provider} {Operation} failed after {TotalAttempts} attempts. Last error: {LastError}",
            providerName,
            operationType,
            totalAttempts,
            lastError);
    }

    /// <summary>
    /// Log time estimate for an operation
    /// </summary>
    public void LogTimeEstimate(string providerName, string operationType, TimeEstimate estimate)
    {
        _logger.LogDebug(
            "LLM Time Estimate: {Provider} {Operation} - {Description} (confidence: {Confidence:F2})",
            providerName,
            operationType,
            estimate.Description,
            estimate.Confidence);
    }
}
