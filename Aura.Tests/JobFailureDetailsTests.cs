using System;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Artifacts;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class JobFailureDetailsTests
{
    [Fact]
    public void JobFailure_ContainsExpectedProperties()
    {
        // Arrange & Act
        var failure = new JobFailure
        {
            Stage = "Render",
            Message = "FFmpeg render failed",
            CorrelationId = "test-correlation-id",
            StderrSnippet = "Error: codec not found",
            LogPath = "/path/to/log.log",
            SuggestedActions = new[] { "Try software encoder", "Check FFmpeg installation" },
            ErrorCode = "E304-FFMPEG_RUNTIME",
            FailedAt = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("Render", failure.Stage);
        Assert.Equal("FFmpeg render failed", failure.Message);
        Assert.Equal("test-correlation-id", failure.CorrelationId);
        Assert.NotNull(failure.StderrSnippet);
        Assert.Contains("codec not found", failure.StderrSnippet);
        Assert.Equal(2, failure.SuggestedActions.Length);
        Assert.Equal("E304-FFMPEG_RUNTIME", failure.ErrorCode);
    }

    [Fact]
    public void Job_CanStoreFailureDetails()
    {
        // Arrange
        var failure = new JobFailure
        {
            Stage = "Voice",
            Message = "TTS service unavailable",
            CorrelationId = "test-123",
            SuggestedActions = new[] { "Check internet connection", "Retry later" }
        };

        // Act
        var job = new Job
        {
            Id = "job-123",
            Status = JobStatus.Failed,
            ErrorMessage = "Voice generation failed",
            FailureDetails = failure
        };

        // Assert
        Assert.NotNull(job.FailureDetails);
        Assert.Equal("Voice", job.FailureDetails.Stage);
        Assert.Equal("TTS service unavailable", job.FailureDetails.Message);
        Assert.Equal(2, job.FailureDetails.SuggestedActions.Length);
    }

    [Fact]
    public void JobFailure_HandlesNullOptionalFields()
    {
        // Arrange & Act
        var failure = new JobFailure
        {
            Stage = "Script",
            Message = "Script generation failed",
            CorrelationId = "test-456"
        };

        // Assert
        Assert.Null(failure.StderrSnippet);
        Assert.Null(failure.InstallLogSnippet);
        Assert.Null(failure.LogPath);
        Assert.Null(failure.ErrorCode);
        Assert.Empty(failure.SuggestedActions);
    }
}
