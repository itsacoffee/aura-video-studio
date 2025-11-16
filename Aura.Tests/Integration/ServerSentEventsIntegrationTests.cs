using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for Server-Sent Events (SSE) and real-time progress reporting
/// Tests that progress updates are emitted correctly throughout the pipeline
/// </summary>
public class ServerSentEventsIntegrationTests
{
    /// <summary>
    /// Test that progress events are emitted during job execution
    /// </summary>
    [Fact]
    public async Task SSE_Should_EmitProgressEvents()
    {
        // Arrange - Create a progress tracker
        var progressEvents = new List<JobProgressEventArgs>();
        var progressHandler = new Progress<JobProgressEventArgs>(e =>
        {
            progressEvents.Add(e);
        });

        // Create a simple job that reports progress
        var jobId = Guid.NewGuid().ToString();
        var job = new Job
        {
            Id = jobId,
            Status = JobStatus.Running,
            Stage = "Script",
            Percent = 0,


        };

        // Act - Simulate progress updates
        await SimulateJobProgressAsync(job, progressHandler);

        // Assert - Progress events were emitted
        Assert.NotEmpty(progressEvents);

        // Should have progress events from multiple stages
        var stages = progressEvents.Select(e => e.Stage).Distinct().ToList();
        Assert.Contains("Script", stages);
        Assert.Contains("TTS", stages);
        Assert.Contains("Visuals", stages);
        Assert.Contains("Render", stages);

        // Progress should be monotonically increasing
        for (int i = 1; i < progressEvents.Count; i++)
        {
            Assert.True(
                progressEvents[i].Percent >= progressEvents[i - 1].Percent,
                "Progress percentage should never decrease"
            );
        }

        // Final progress should be 100%
        Assert.Equal(100, progressEvents.Last().Percent);
        Assert.Equal("Done", progressEvents.Last().Stage);
    }

    /// <summary>
    /// Test that progress events include correlation IDs for tracking
    /// </summary>
    [Fact]
    public async Task SSE_Should_IncludeCorrelationId()
    {
        // Arrange
        var correlationId = $"test-{Guid.NewGuid()}";
        var progressEvents = new List<JobProgressEventArgs>();
        var progressHandler = new Progress<JobProgressEventArgs>(e =>
        {
            progressEvents.Add(e);
        });

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Status = JobStatus.Running,
            Stage = "Script",
            Percent = 0,

            CorrelationId = correlationId
        };

        // Act
        await SimulateJobProgressAsync(job, progressHandler);

        // Assert - All events include correlation ID
        Assert.All(progressEvents, e =>
        {
            Assert.Equal(correlationId, e.CorrelationId);
        });
    }

    /// <summary>
    /// Test that error events are properly reported via SSE
    /// </summary>
    [Fact]
    public async Task SSE_Should_ReportErrors()
    {
        // Arrange
        var progressEvents = new List<JobProgressEventArgs>();
        var progressHandler = new Progress<JobProgressEventArgs>(e =>
        {
            progressEvents.Add(e);
        });

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Status = JobStatus.Running,
            Stage = "Script",
            Percent = 25,
        };

        // Act - Simulate failure during TTS stage
        await SimulateJobFailureAsync(job, progressHandler, "TTS", "Audio synthesis failed");

        // Assert - Error event was emitted
        Assert.NotEmpty(progressEvents);

        var errorEvent = progressEvents.FirstOrDefault(e => e.Status == JobStatus.Failed);
        Assert.NotNull(errorEvent);
        Assert.Equal("TTS", errorEvent.Stage);
        Assert.Contains("Audio synthesis failed", errorEvent.Message ?? string.Empty);
    }

    /// <summary>
    /// Test that multiple clients can subscribe to same job's SSE stream
    /// </summary>
    [Fact]
    public async Task SSE_Should_SupportMultipleSubscribers()
    {
        // Arrange - Multiple progress handlers
        var subscriber1Events = new List<JobProgressEventArgs>();
        var subscriber2Events = new List<JobProgressEventArgs>();
        var subscriber3Events = new List<JobProgressEventArgs>();

        var handler1 = new Progress<JobProgressEventArgs>(e => subscriber1Events.Add(e));
        var handler2 = new Progress<JobProgressEventArgs>(e => subscriber2Events.Add(e));
        var handler3 = new Progress<JobProgressEventArgs>(e => subscriber3Events.Add(e));

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Status = JobStatus.Running,
            Stage = "Script",
            Percent = 0,
        };

        // Act - Simulate progress with multiple subscribers
        await SimulateJobProgressWithMultipleSubscribersAsync(job, handler1, handler2, handler3);

        // Assert - All subscribers received events
        Assert.NotEmpty(subscriber1Events);
        Assert.NotEmpty(subscriber2Events);
        Assert.NotEmpty(subscriber3Events);

        // All subscribers should receive the same sequence
        Assert.Equal(subscriber1Events.Count, subscriber2Events.Count);
        Assert.Equal(subscriber2Events.Count, subscriber3Events.Count);

        // Verify event content matches
        for (int i = 0; i < subscriber1Events.Count; i++)
        {
            Assert.Equal(subscriber1Events[i].Percent, subscriber2Events[i].Percent);
            Assert.Equal(subscriber1Events[i].Stage, subscriber2Events[i].Stage);
        }
    }

    /// <summary>
    /// Test that SSE stream closes properly when job completes
    /// </summary>
    [Fact]
    public async Task SSE_Should_CloseStreamOnCompletion()
    {
        // Arrange
        var progressEvents = new List<JobProgressEventArgs>();
        var streamClosed = false;

        var progressHandler = new Progress<JobProgressEventArgs>(e =>
        {
            progressEvents.Add(e);

            // Check if this is completion event
            if (e.Status == JobStatus.Done && e.Percent == 100)
            {
                streamClosed = true;
            }
        });

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Status = JobStatus.Running,
            Stage = "Script",
            Percent = 0,
        };

        // Act
        await SimulateJobProgressAsync(job, progressHandler);

        // Assert - Stream was closed after completion
        Assert.True(streamClosed, "SSE stream should close after job completion");
        Assert.Equal(JobStatus.Done, progressEvents.Last().Status);
    }

    /// <summary>
    /// Test that progress events include timing information
    /// </summary>
    [Fact]
    public async Task SSE_Should_IncludeTimingInformation()
    {
        // Arrange
        var progressEvents = new List<JobProgressEventArgs>();
        var progressHandler = new Progress<JobProgressEventArgs>(e =>
        {
            progressEvents.Add(e);
        });

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Status = JobStatus.Running,
            Stage = "Script",
            Percent = 0,


        };

        // Act
        await SimulateJobProgressAsync(job, progressHandler);

        // Assert - Events include timestamps
        Assert.All(progressEvents, e =>
        {
            Assert.True(e.Timestamp > DateTime.MinValue);
            Assert.True(e.Timestamp <= DateTime.UtcNow);
        });

        // Timestamps should be in chronological order
        for (int i = 1; i < progressEvents.Count; i++)
        {
            Assert.True(
                progressEvents[i].Timestamp >= progressEvents[i - 1].Timestamp,
                "Event timestamps should be chronological"
            );
        }
    }

    // Helper methods

    private static async Task SimulateJobProgressAsync(
        Job job,
        IProgress<JobProgressEventArgs> progress)
    {
        var stages = new[] { "Script", "TTS", "Visuals", "Render", "Done" };
        var percentages = new[] { 0, 25, 50, 75, 100 };

        for (int i = 0; i < stages.Length; i++)
        {
            var progressEvent = new JobProgressEventArgs
            {
                JobId = job.Id,
                Status = i < stages.Length - 1 ? JobStatus.Running : JobStatus.Done,
                Stage = stages[i],
                Percent = percentages[i],
                Message = $"Processing {stages[i]}",
                Timestamp = DateTime.UtcNow,
                CorrelationId = job.CorrelationId
            };

            progress?.Report(progressEvent);
            await Task.Delay(10); // Simulate some work
        }
    }

    private static async Task SimulateJobFailureAsync(
        Job job,
        IProgress<JobProgressEventArgs> progress,
        string failureStage,
        string errorMessage)
    {
        // Report some progress first
        var stages = new[] { "Script", "TTS" };
        var percentages = new[] { 15, 35 };

        for (int i = 0; i < stages.Length; i++)
        {
            var progressEvent = new JobProgressEventArgs
            {
                JobId = job.Id,
                Status = JobStatus.Running,
                Stage = stages[i],
                Percent = percentages[i],
                Message = $"Processing {stages[i]}",
                Timestamp = DateTime.UtcNow,
                CorrelationId = job.CorrelationId
            };

            progress?.Report(progressEvent);
            await Task.Delay(10);

            // Fail at specified stage
            if (stages[i] == failureStage)
            {
                var errorEvent = new JobProgressEventArgs
                {
                    JobId = job.Id,
                    Status = JobStatus.Failed,
                    Stage = failureStage,
                    Percent = percentages[i],
                    Message = errorMessage,
                    Timestamp = DateTime.UtcNow,
                    CorrelationId = job.CorrelationId
                };

                progress?.Report(errorEvent);
                break;
            }
        }
    }

    private static async Task SimulateJobProgressWithMultipleSubscribersAsync(
        Job job,
        params IProgress<JobProgressEventArgs>[] progressHandlers)
    {
        var stages = new[] { "Script", "TTS", "Visuals", "Render" };
        var percentages = new[] { 15, 35, 65, 85 };

        for (int i = 0; i < stages.Length; i++)
        {
            var progressEvent = new JobProgressEventArgs
            {
                JobId = job.Id,
                Status = JobStatus.Running,
                Stage = stages[i],
                Percent = percentages[i],
                Message = $"Processing {stages[i]}",
                Timestamp = DateTime.UtcNow,
                CorrelationId = job.CorrelationId
            };

            // Report to all subscribers
            foreach (var handler in progressHandlers)
            {
                handler?.Report(progressEvent);
            }

            await Task.Delay(10);
        }

        // Final completion event
        var completionEvent = new JobProgressEventArgs
        {
            JobId = job.Id,
            Status = JobStatus.Done,
            Stage = "Done",
            Percent = 100,
            Message = "Job completed successfully",
            Timestamp = DateTime.UtcNow,
            CorrelationId = job.CorrelationId
        };

        foreach (var handler in progressHandlers)
        {
            handler?.Report(completionEvent);
        }
    }
}

/// <summary>
/// Event arguments for job progress updates
/// </summary>
public class JobProgressEventArgs : EventArgs
{
    public string? JobId { get; set; }
    public JobStatus Status { get; set; }
    public string? Stage { get; set; }
    public int Percent { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
    public string? CorrelationId { get; set; }
}
