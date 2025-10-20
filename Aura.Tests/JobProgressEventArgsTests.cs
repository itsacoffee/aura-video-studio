using System;
using System.Text.Json;
using Aura.Core.Models;
using Aura.Core.Models.Events;
using Xunit;

namespace Aura.Tests;

public class JobProgressEventArgsTests
{
    [Fact]
    public void Constructor_WithJob_ShouldPopulateProperties()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job-123",
            Stage = "Rendering",
            Status = JobStatus.Running,
            Percent = 50,
            CorrelationId = "corr-456",
            Eta = TimeSpan.FromMinutes(5)
        };

        // Act
        var eventArgs = new JobProgressEventArgs(job);

        // Assert
        Assert.Equal("test-job-123", eventArgs.JobId);
        Assert.Equal(50, eventArgs.Progress);
        Assert.Equal(JobStatus.Running, eventArgs.Status);
        Assert.Equal("Rendering", eventArgs.Stage);
        Assert.Equal("corr-456", eventArgs.CorrelationId);
        Assert.Equal(TimeSpan.FromMinutes(5), eventArgs.Eta);
        Assert.NotEqual(default(DateTime), eventArgs.Timestamp);
    }

    [Fact]
    public void Constructor_WithParameters_ShouldSetAllProperties()
    {
        // Arrange
        var jobId = "test-job-789";
        var progress = 75;
        var status = JobStatus.Running;
        var stage = "Postprocessing";
        var message = "Finalizing video";
        var correlationId = "corr-999";
        var eta = TimeSpan.FromMinutes(2);

        // Act
        var eventArgs = new JobProgressEventArgs(
            jobId, progress, status, stage, message, correlationId, eta);

        // Assert
        Assert.Equal(jobId, eventArgs.JobId);
        Assert.Equal(progress, eventArgs.Progress);
        Assert.Equal(status, eventArgs.Status);
        Assert.Equal(stage, eventArgs.Stage);
        Assert.Equal(message, eventArgs.Message);
        Assert.Equal(correlationId, eventArgs.CorrelationId);
        Assert.Equal(eta, eventArgs.Eta);
        Assert.NotEqual(default(DateTime), eventArgs.Timestamp);
    }

    [Fact]
    public void Serialization_ShouldProduceValidJson()
    {
        // Arrange
        var eventArgs = new JobProgressEventArgs(
            jobId: "test-123",
            progress: 50,
            status: JobStatus.Running,
            stage: "Rendering",
            message: "Processing video",
            correlationId: "corr-123",
            eta: TimeSpan.FromMinutes(5));

        // Act
        var json = JsonSerializer.Serialize(eventArgs);
        var deserialized = JsonSerializer.Deserialize<JobProgressEventArgs>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(eventArgs.JobId, deserialized!.JobId);
        Assert.Equal(eventArgs.Progress, deserialized.Progress);
        Assert.Equal(eventArgs.Status, deserialized.Status);
        Assert.Equal(eventArgs.Stage, deserialized.Stage);
        Assert.Equal(eventArgs.Message, deserialized.Message);
        Assert.Equal(eventArgs.CorrelationId, deserialized.CorrelationId);
    }

    [Fact]
    public void GetJobStatusMessage_ForFailedJob_ShouldReturnErrorMessage()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job",
            Status = JobStatus.Failed,
            ErrorMessage = "FFmpeg failed"
        };

        // Act
        var eventArgs = new JobProgressEventArgs(job);

        // Assert
        Assert.Equal("FFmpeg failed", eventArgs.Message);
    }

    [Fact]
    public void GetJobStatusMessage_ForRunningJob_ShouldIncludeStage()
    {
        // Arrange
        var job = new Job
        {
            Id = "test-job",
            Status = JobStatus.Running,
            Stage = "Voice Generation"
        };

        // Act
        var eventArgs = new JobProgressEventArgs(job);

        // Assert
        Assert.Contains("Voice Generation", eventArgs.Message);
    }

    [Fact]
    public void JsonPropertyNames_ShouldUseCamelCase()
    {
        // Arrange
        var eventArgs = new JobProgressEventArgs(
            jobId: "test-123",
            progress: 50,
            status: JobStatus.Running,
            stage: "Rendering",
            message: "Processing",
            correlationId: "corr-123");

        // Act
        var json = JsonSerializer.Serialize(eventArgs);

        // Assert
        Assert.Contains("\"jobId\":", json);
        Assert.Contains("\"progress\":", json);
        Assert.Contains("\"status\":", json);
        Assert.Contains("\"stage\":", json);
        Assert.Contains("\"message\":", json);
        Assert.Contains("\"correlationId\":", json);
        Assert.Contains("\"timestamp\":", json);
    }

    [Fact]
    public void StatusSerialization_ShouldUseStringEnum()
    {
        // Arrange
        var eventArgs = new JobProgressEventArgs(
            jobId: "test-123",
            progress: 100,
            status: JobStatus.Done,
            stage: "Complete",
            message: "Job completed",
            correlationId: "corr-123");

        // Act
        var json = JsonSerializer.Serialize(eventArgs);

        // Assert
        Assert.Contains("\"status\":\"Done\"", json);
    }
}
