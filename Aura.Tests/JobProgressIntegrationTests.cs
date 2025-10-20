using System;
using System.Text.Json;
using Aura.Core.Models;
using Aura.Core.Models.Events;
using Xunit;

namespace Aura.Tests;

public class JobProgressIntegrationTests
{
    [Fact]
    public void JobProgressEventArgs_FromJob_ShouldMapAllFields()
    {
        // Arrange
        var job = new Job
        {
            Id = "job-123",
            Stage = "Rendering",
            Status = JobStatus.Running,
            Percent = 75,
            CorrelationId = "corr-456",
            Eta = TimeSpan.FromMinutes(5)
        };

        // Act
        var eventArgs = new JobProgressEventArgs(job);

        // Assert
        Assert.Equal("job-123", eventArgs.JobId);
        Assert.Equal(75, eventArgs.Progress);
        Assert.Equal(JobStatus.Running, eventArgs.Status);
        Assert.Equal("Rendering", eventArgs.Stage);
        Assert.Equal("corr-456", eventArgs.CorrelationId);
        Assert.Equal(TimeSpan.FromMinutes(5), eventArgs.Eta);
        Assert.NotEqual(default(DateTime), eventArgs.Timestamp);
        Assert.Contains("Rendering", eventArgs.Message);
    }

    [Fact]
    public void JobProgressEventArgs_WithParameters_ShouldSetAllFields()
    {
        // Arrange & Act
        var eventArgs = new JobProgressEventArgs(
            jobId: "job-789",
            progress: 50,
            status: JobStatus.Running,
            stage: "Voice",
            message: "Generating narration",
            correlationId: "corr-999",
            eta: TimeSpan.FromMinutes(3));

        // Assert
        Assert.Equal("job-789", eventArgs.JobId);
        Assert.Equal(50, eventArgs.Progress);
        Assert.Equal(JobStatus.Running, eventArgs.Status);
        Assert.Equal("Voice", eventArgs.Stage);
        Assert.Equal("Generating narration", eventArgs.Message);
        Assert.Equal("corr-999", eventArgs.CorrelationId);
        Assert.Equal(TimeSpan.FromMinutes(3), eventArgs.Eta);
    }

    [Fact]
    public void JobProgressEventArgs_ShouldSerializeToJson()
    {
        // Arrange
        var eventArgs = new JobProgressEventArgs(
            jobId: "job-123",
            progress: 50,
            status: JobStatus.Running,
            stage: "Rendering",
            message: "Processing video",
            correlationId: "corr-456",
            eta: TimeSpan.FromMinutes(5));

        // Act
        var json = JsonSerializer.Serialize(eventArgs);

        // Assert
        Assert.Contains("\"jobId\":\"job-123\"", json);
        Assert.Contains("\"progress\":50", json);
        Assert.Contains("\"status\":\"Running\"", json);
        Assert.Contains("\"stage\":\"Rendering\"", json);
        Assert.Contains("\"message\":\"Processing video\"", json);
        Assert.Contains("\"correlationId\":\"corr-456\"", json);
    }

    [Fact]
    public void JobProgressEventArgs_ShouldRoundTripThroughJson()
    {
        // Arrange
        var original = new JobProgressEventArgs(
            jobId: "job-123",
            progress: 75,
            status: JobStatus.Running,
            stage: "Postprocessing",
            message: "Finalizing video",
            correlationId: "corr-456");

        // Act
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<JobProgressEventArgs>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.JobId, deserialized!.JobId);
        Assert.Equal(original.Progress, deserialized.Progress);
        Assert.Equal(original.Status, deserialized.Status);
        Assert.Equal(original.Stage, deserialized.Stage);
        Assert.Equal(original.Message, deserialized.Message);
        Assert.Equal(original.CorrelationId, deserialized.CorrelationId);
    }

    [Fact]
    public void JobProgressEventArgs_ForFailedJob_ShouldIncludeErrorMessage()
    {
        // Arrange
        var job = new Job
        {
            Id = "job-failed",
            Status = JobStatus.Failed,
            ErrorMessage = "FFmpeg render failed",
            Stage = "Rendering"
        };

        // Act
        var eventArgs = new JobProgressEventArgs(job);

        // Assert
        Assert.Equal(JobStatus.Failed, eventArgs.Status);
        Assert.Equal("FFmpeg render failed", eventArgs.Message);
    }

    [Fact]
    public void JobProgressEventArgs_ForCompletedJob_ShouldHaveSuccessMessage()
    {
        // Arrange
        var job = new Job
        {
            Id = "job-done",
            Status = JobStatus.Done,
            Stage = "Complete",
            Percent = 100
        };

        // Act
        var eventArgs = new JobProgressEventArgs(job);

        // Assert
        Assert.Equal(JobStatus.Done, eventArgs.Status);
        Assert.Equal(100, eventArgs.Progress);
        Assert.Contains("completed successfully", eventArgs.Message);
    }
}
