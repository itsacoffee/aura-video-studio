using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Models.Timeline;
using Aura.Core.Services.Export;
using Aura.Core.Services.Timeline;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services;

/// <summary>
/// Tests for export pipeline output path propagation to job artifacts and SSE responses.
/// Verifies the fix for the issue where output paths were only logged but not returned to the frontend.
/// </summary>
public class ExportPathPropagationTests
{
    private readonly Mock<ILogger<ArtifactManager>> _artifactLoggerMock;
    private readonly Mock<ILogger<ExportJobService>> _exportJobLoggerMock;
    private readonly Mock<TimelineSerializationService> _timelineSerializerMock;
    private readonly ArtifactManager _artifactManager;
    private readonly ExportJobService _exportJobService;

    public ExportPathPropagationTests()
    {
        _artifactLoggerMock = new Mock<ILogger<ArtifactManager>>();
        _exportJobLoggerMock = new Mock<ILogger<ExportJobService>>();
        _timelineSerializerMock = new Mock<TimelineSerializationService>(MockBehavior.Loose);
        
        _artifactManager = new ArtifactManager(_artifactLoggerMock.Object, _timelineSerializerMock.Object);
        _exportJobService = new ExportJobService(_exportJobLoggerMock.Object);
    }

    [Fact]
    public async Task RecordArtifactAsync_WithValidPath_ReturnsArtifactWithPath()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test content");

        try
        {
            // Act
            var artifact = await _artifactManager.RecordArtifactAsync(
                jobId,
                "test-video.mp4",
                tempFile,
                "video/mp4",
                CancellationToken.None);

            // Assert
            artifact.Should().NotBeNull();
            artifact.Path.Should().Be(tempFile);
            artifact.Name.Should().Be("test-video.mp4");
            artifact.Type.Should().Be("video/mp4");
            artifact.SizeBytes.Should().BeGreaterThan(0);
            artifact.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task RecordArtifactAsync_WithNonExistentPath_LogsWarningButReturnsArtifact()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.mp4");

        // Act
        var artifact = await _artifactManager.RecordArtifactAsync(
            jobId,
            "missing-video.mp4",
            nonExistentPath,
            "video/mp4",
            CancellationToken.None);

        // Assert
        artifact.Should().NotBeNull();
        artifact.Path.Should().Be(nonExistentPath);
        artifact.SizeBytes.Should().Be(0); // File doesn't exist, size is 0
    }

    [Fact]
    public void CreateArtifact_WithValidPath_ReturnsArtifactWithPath()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, "test content for size calculation");

        try
        {
            // Act
            var artifact = _artifactManager.CreateArtifact(
                jobId,
                "output.mp4",
                tempFile,
                "video/mp4");

            // Assert
            artifact.Should().NotBeNull();
            artifact.Path.Should().Be(tempFile);
            artifact.Name.Should().Be("output.mp4");
            artifact.Type.Should().Be("video/mp4");
            artifact.SizeBytes.Should().BeGreaterThan(0);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public async Task ExportJobService_UpdateStatusToCompleted_RequiresOutputPath()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var job = new VideoJob
        {
            Id = jobId,
            Status = "queued",
            Progress = 0
        };

        await _exportJobService.CreateJobAsync(job);

        // Act - Try to mark as completed WITHOUT outputPath
        await _exportJobService.UpdateJobStatusAsync(jobId, "completed", 100, outputPath: null);

        // Assert - Job should NOT transition to completed status
        var updatedJob = await _exportJobService.GetJobAsync(jobId);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be("queued", "job should reject transition to completed without outputPath");
        updatedJob.OutputPath.Should().BeNull();
    }

    [Fact]
    public async Task ExportJobService_UpdateStatusToCompleted_WithOutputPath_Succeeds()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var outputPath = Path.Combine(Path.GetTempPath(), "export-output.mp4");
        
        var job = new VideoJob
        {
            Id = jobId,
            Status = "running",
            Progress = 50
        };

        await _exportJobService.CreateJobAsync(job);

        // Act - Mark as completed WITH outputPath
        await _exportJobService.UpdateJobStatusAsync(jobId, "completed", 100, outputPath: outputPath);

        // Assert - Job should transition to completed and have outputPath set
        var updatedJob = await _exportJobService.GetJobAsync(jobId);
        updatedJob.Should().NotBeNull();
        updatedJob!.Status.Should().Be("completed");
        updatedJob.Progress.Should().Be(100);
        updatedJob.OutputPath.Should().Be(outputPath);
        updatedJob.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExportJobService_SubscribeToJobUpdates_IncludesOutputPathInFinalEvent()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var outputPath = Path.Combine(Path.GetTempPath(), "subscribed-export.mp4");
        
        var job = new VideoJob
        {
            Id = jobId,
            Status = "queued",
            Progress = 0
        };

        await _exportJobService.CreateJobAsync(job);

        var receivedUpdates = new System.Collections.Generic.List<Aura.Core.Models.Export.VideoJob>();
        using var cts = new CancellationTokenSource();

        // Act - Subscribe to updates
        var subscriptionTask = Task.Run(async () =>
        {
            await foreach (var update in _exportJobService.SubscribeToJobUpdatesAsync(jobId, cts.Token))
            {
                receivedUpdates.Add(update);
                
                // Break when we get the completed event
                if (update.Status == "completed")
                {
                    break;
                }
            }
        });

        // Simulate job progress
        await Task.Delay(100);
        await _exportJobService.UpdateJobProgressAsync(jobId, 50, "Processing");
        await Task.Delay(100);
        await _exportJobService.UpdateJobStatusAsync(jobId, "completed", 100, outputPath: outputPath);
        
        // Wait for subscription to process final event
        await Task.Delay(200);
        cts.Cancel();
        
        try
        {
            await subscriptionTask;
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation token fires
        }

        // Assert
        receivedUpdates.Should().NotBeEmpty();
        
        var completedUpdate = receivedUpdates.FirstOrDefault(u => u.Status == "completed");
        completedUpdate.Should().NotBeNull("the completed event should have been sent");
        completedUpdate!.OutputPath.Should().Be(outputPath, "the completed event must include the output path");
        completedUpdate.Progress.Should().Be(100);
    }

    [Fact]
    public async Task ExportJobService_SubscribeToJobUpdates_ClosesStreamAfterTerminalState()
    {
        // Arrange
        var jobId = Guid.NewGuid().ToString();
        var outputPath = Path.Combine(Path.GetTempPath(), "terminal-test.mp4");
        
        var job = new VideoJob
        {
            Id = jobId,
            Status = "completed",
            Progress = 100,
            OutputPath = outputPath
        };

        await _exportJobService.CreateJobAsync(job);

        var receivedUpdates = new System.Collections.Generic.List<Aura.Core.Models.Export.VideoJob>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        // Act - Subscribe to a job that's already completed
        await foreach (var update in _exportJobService.SubscribeToJobUpdatesAsync(jobId, cts.Token))
        {
            receivedUpdates.Add(update);
        }

        // Assert
        receivedUpdates.Should().HaveCount(1, "should send initial state and then close");
        receivedUpdates[0].Status.Should().Be("completed");
        receivedUpdates[0].OutputPath.Should().Be(outputPath);
    }

    [Fact]
    public async Task ExportPipeline_Integration_PropagatesOutputPathThroughFullFlow()
    {
        // This test validates the full integration between ExportOrchestrationService and ExportJobService
        // Simulating the scenario where:
        // 1. ExportController creates a VideoJob
        // 2. ExportController queues an export with the videoJobId
        // 3. ExportOrchestrationService processes the export
        // 4. Upon completion, ExportOrchestrationService updates the VideoJob with the outputPath
        // 5. SSE subscribers receive the outputPath in the completion event
        
        // Arrange
        var videoJobId = Guid.NewGuid().ToString();
        var outputFile = Path.Combine(Path.GetTempPath(), $"integration-test-{Guid.NewGuid()}.mp4");
        
        // Create the VideoJob (simulating ExportController creating it)
        var videoJob = new VideoJob
        {
            Id = videoJobId,
            Status = "queued",
            Progress = 0,
            Stage = "Preparing export"
        };
        await _exportJobService.CreateJobAsync(videoJob);
        
        // Verify initial state has no outputPath
        var initialJob = await _exportJobService.GetJobAsync(videoJobId);
        initialJob.Should().NotBeNull();
        initialJob!.OutputPath.Should().BeNullOrEmpty("outputPath should not be set initially");
        
        // Act - Simulate the export completing
        // In the real flow, ExportOrchestrationService.ProcessJobAsync would call this after FFmpeg finishes
        await _exportJobService.UpdateJobStatusAsync(
            videoJobId,
            "completed",
            100,
            outputPath: outputFile);
        
        // Assert - Verify outputPath was propagated
        var completedJob = await _exportJobService.GetJobAsync(videoJobId);
        completedJob.Should().NotBeNull();
        completedJob!.Status.Should().Be("completed");
        completedJob.Progress.Should().Be(100);
        completedJob.OutputPath.Should().Be(outputFile, "outputPath must be set when job completes successfully");
        completedJob.CompletedAt.Should().NotBeNull();
        
        // Verify SSE stream would include outputPath
        // (In real usage, frontend subscribes before job completes and receives updates)
        var receivedUpdates = new System.Collections.Generic.List<Aura.Core.Models.Export.VideoJob>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        await foreach (var update in _exportJobService.SubscribeToJobUpdatesAsync(videoJobId, cts.Token))
        {
            receivedUpdates.Add(update);
        }
        
        receivedUpdates.Should().HaveCount(1, "should receive the completed job state");
        receivedUpdates[0].OutputPath.Should().Be(outputFile, "SSE stream must include outputPath");
    }
}
