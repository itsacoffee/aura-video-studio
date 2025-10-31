using System;
using System.Text.Json.Serialization;

namespace Aura.Core.Models.Events;

/// <summary>
/// Event args for job progress updates with detailed information for SSE transport.
/// </summary>
public class JobProgressEventArgs : EventArgs
{
    /// <summary>
    /// The job ID
    /// </summary>
    [JsonPropertyName("jobId")]
    public string JobId { get; init; } = string.Empty;

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    [JsonPropertyName("progress")]
    public int Progress { get; init; }

    /// <summary>
    /// Current job status
    /// </summary>
    [JsonPropertyName("status")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public JobStatus Status { get; init; }

    /// <summary>
    /// Current stage/step name
    /// </summary>
    [JsonPropertyName("stage")]
    public string Stage { get; init; } = string.Empty;

    /// <summary>
    /// Progress message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Timestamp of the progress update
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for request tracing
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Estimated time of arrival (optional)
    /// </summary>
    [JsonPropertyName("eta")]
    public TimeSpan? Eta { get; init; }

    /// <summary>
    /// Whether this is a warning message (e.g., timeout threshold exceeded)
    /// </summary>
    [JsonPropertyName("isWarning")]
    public bool IsWarning { get; init; }

    /// <summary>
    /// Estimated duration in seconds for current operation (optional)
    /// </summary>
    [JsonPropertyName("estimatedDurationSeconds")]
    public int? EstimatedDurationSeconds { get; init; }

    /// <summary>
    /// Elapsed time in seconds for current operation (optional)
    /// </summary>
    [JsonPropertyName("elapsedSeconds")]
    public int? ElapsedSeconds { get; init; }

    /// <summary>
    /// Creates a new JobProgressEventArgs
    /// </summary>
    public JobProgressEventArgs()
    {
    }

    /// <summary>
    /// Creates a JobProgressEventArgs from a Job object
    /// </summary>
    public JobProgressEventArgs(Job job)
    {
        JobId = job.Id;
        Progress = job.Percent;
        Status = job.Status;
        Stage = job.Stage;
        Message = GetJobStatusMessage(job);
        Timestamp = DateTime.UtcNow;
        CorrelationId = job.CorrelationId ?? string.Empty;
        Eta = job.Eta;
    }

    /// <summary>
    /// Creates a JobProgressEventArgs with specific values
    /// </summary>
    public JobProgressEventArgs(
        string jobId,
        int progress,
        JobStatus status,
        string stage,
        string message,
        string correlationId,
        TimeSpan? eta = null,
        bool isWarning = false,
        int? estimatedDurationSeconds = null,
        int? elapsedSeconds = null)
    {
        JobId = jobId;
        Progress = progress;
        Status = status;
        Stage = stage;
        Message = message;
        CorrelationId = correlationId;
        Eta = eta;
        IsWarning = isWarning;
        EstimatedDurationSeconds = estimatedDurationSeconds;
        ElapsedSeconds = elapsedSeconds;
        Timestamp = DateTime.UtcNow;
    }

    private static string GetJobStatusMessage(Job job)
    {
        if (!string.IsNullOrEmpty(job.ErrorMessage) && job.Status == JobStatus.Failed)
        {
            return job.ErrorMessage;
        }

        return job.Status switch
        {
            JobStatus.Queued => "Job is queued for execution",
            JobStatus.Running => $"Processing: {job.Stage}",
            JobStatus.Done => "Job completed successfully",
            JobStatus.Failed => "Job failed",
            JobStatus.Canceled => "Job was canceled",
            _ => $"Status: {job.Status}"
        };
    }
}
