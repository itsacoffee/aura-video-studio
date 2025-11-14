using System;
using System.Threading.Tasks;
using Aura.Core.Models.Events;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time job queue updates
/// Provides live notifications for job status changes, progress updates, and completion
/// </summary>
public class JobQueueHub : Hub
{
    private readonly ILogger<JobQueueHub> _logger;

    public JobQueueHub(ILogger<JobQueueHub> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Client subscribes to updates for a specific job
    /// </summary>
    public async Task SubscribeToJob(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job-{jobId}").ConfigureAwait(false);
        _logger.LogInformation(
            "Connection {ConnectionId} subscribed to job {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Client unsubscribes from updates for a specific job
    /// </summary>
    public async Task UnsubscribeFromJob(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job-{jobId}").ConfigureAwait(false);
        _logger.LogInformation(
            "Connection {ConnectionId} unsubscribed from job {JobId}",
            Context.ConnectionId, jobId);
    }

    /// <summary>
    /// Client subscribes to all queue updates
    /// </summary>
    public async Task SubscribeToQueue()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "queue").ConfigureAwait(false);
        _logger.LogInformation(
            "Connection {ConnectionId} subscribed to queue updates",
            Context.ConnectionId);
    }

    /// <summary>
    /// Client unsubscribes from all queue updates
    /// </summary>
    public async Task UnsubscribeFromQueue()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "queue").ConfigureAwait(false);
        _logger.LogInformation(
            "Connection {ConnectionId} unsubscribed from queue updates",
            Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning(exception,
                "Client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}

/// <summary>
/// Helper service for sending SignalR notifications from background services
/// </summary>
public class JobQueueNotificationService
{
    private readonly IHubContext<JobQueueHub> _hubContext;
    private readonly ILogger<JobQueueNotificationService> _logger;

    public JobQueueNotificationService(
        IHubContext<JobQueueHub> hubContext,
        ILogger<JobQueueNotificationService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Notifies clients about job status change
    /// </summary>
    public async Task NotifyJobStatusChangedAsync(
        string jobId,
        string status,
        string? correlationId = null,
        string? outputPath = null,
        string? errorMessage = null)
    {
        try
        {
            var notification = new
            {
                jobId,
                status,
                correlationId,
                outputPath,
                errorMessage,
                timestamp = DateTime.UtcNow
            };

            // Send to job-specific group
            await _hubContext.Clients
                .Group($"job-{jobId}")
                .SendAsync("JobStatusChanged", notification).ConfigureAwait(false);

            // Send to queue group
            await _hubContext.Clients
                .Group("queue")
                .SendAsync("JobStatusChanged", notification).ConfigureAwait(false);

            _logger.LogDebug(
                "Sent JobStatusChanged notification for job {JobId}: {Status}",
                jobId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending JobStatusChanged notification for job {JobId}",
                jobId);
        }
    }

    /// <summary>
    /// Notifies clients about job progress update
    /// </summary>
    public async Task NotifyJobProgressAsync(JobProgressEventArgs progressArgs)
    {
        try
        {
            var notification = new
            {
                progressArgs.JobId,
                progressArgs.Stage,
                progress = progressArgs.Progress,
                status = progressArgs.Status.ToString(),
                progressArgs.Message,
                progressArgs.CorrelationId,
                timestamp = DateTime.UtcNow
            };

            // Send to job-specific group
            await _hubContext.Clients
                .Group($"job-{progressArgs.JobId}")
                .SendAsync("JobProgress", notification).ConfigureAwait(false);

            _logger.LogTrace(
                "Sent JobProgress notification for job {JobId}: {Progress}%",
                progressArgs.JobId, progressArgs.Progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending JobProgress notification for job {JobId}",
                progressArgs.JobId);
        }
    }

    /// <summary>
    /// Notifies clients about job completion
    /// </summary>
    public async Task NotifyJobCompletedAsync(
        string jobId,
        string outputPath,
        string? correlationId = null)
    {
        try
        {
            var notification = new
            {
                jobId,
                outputPath,
                correlationId,
                timestamp = DateTime.UtcNow
            };

            // Send to job-specific group
            await _hubContext.Clients
                .Group($"job-{jobId}")
                .SendAsync("JobCompleted", notification).ConfigureAwait(false);

            // Send to queue group
            await _hubContext.Clients
                .Group("queue")
                .SendAsync("JobCompleted", notification).ConfigureAwait(false);

            _logger.LogInformation(
                "Sent JobCompleted notification for job {JobId}",
                jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending JobCompleted notification for job {JobId}",
                jobId);
        }
    }

    /// <summary>
    /// Notifies clients about job failure
    /// </summary>
    public async Task NotifyJobFailedAsync(
        string jobId,
        string errorMessage,
        string? correlationId = null)
    {
        try
        {
            var notification = new
            {
                jobId,
                errorMessage,
                correlationId,
                timestamp = DateTime.UtcNow
            };

            // Send to job-specific group
            await _hubContext.Clients
                .Group($"job-{jobId}")
                .SendAsync("JobFailed", notification).ConfigureAwait(false);

            // Send to queue group
            await _hubContext.Clients
                .Group("queue")
                .SendAsync("JobFailed", notification).ConfigureAwait(false);

            _logger.LogWarning(
                "Sent JobFailed notification for job {JobId}: {Error}",
                jobId, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending JobFailed notification for job {JobId}",
                jobId);
        }
    }

    /// <summary>
    /// Notifies clients about queue statistics update
    /// </summary>
    public async Task NotifyQueueStatisticsAsync(object statistics)
    {
        try
        {
            await _hubContext.Clients
                .Group("queue")
                .SendAsync("QueueStatistics", statistics).ConfigureAwait(false);

            _logger.LogTrace("Sent QueueStatistics notification");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending QueueStatistics notification");
        }
    }
}
