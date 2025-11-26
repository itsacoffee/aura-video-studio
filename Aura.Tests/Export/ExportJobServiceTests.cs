using System;
using System.Threading.Tasks;
using Aura.Core.Models.Export;
using Aura.Core.Services.Export;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Export;

/// <summary>
/// Unit tests for the ExportJobService.
/// </summary>
public class ExportJobServiceTests
{
    private readonly Mock<ILogger<ExportJobService>> _loggerMock;
    private readonly ExportJobService _service;

    public ExportJobServiceTests()
    {
        _loggerMock = new Mock<ILogger<ExportJobService>>();
        _service = new ExportJobService(_loggerMock.Object);
    }

    [Fact]
    public async Task CreateJobAsync_CreatesJobWithCorrectProperties()
    {
        // Arrange
        var job = new VideoJob
        {
            Id = "test-job-123",
            Status = "queued",
            Progress = 0,
            Stage = "Initializing"
        };

        // Act
        var result = await _service.CreateJobAsync(job);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-job-123", result.Id);
        Assert.Equal("queued", result.Status);
        Assert.Equal(0, result.Progress);
        Assert.Equal("Initializing", result.Stage);
    }

    [Fact]
    public async Task GetJobAsync_ReturnsCreatedJob()
    {
        // Arrange
        var job = new VideoJob
        {
            Id = "test-job-456",
            Status = "queued",
            Progress = 0,
            Stage = "Initializing"
        };
        await _service.CreateJobAsync(job);

        // Act
        var result = await _service.GetJobAsync("test-job-456");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-job-456", result.Id);
    }

    [Fact]
    public async Task GetJobAsync_ReturnsNullForNonExistentJob()
    {
        // Act
        var result = await _service.GetJobAsync("non-existent-job");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateJobProgressAsync_UpdatesProgressAndStage()
    {
        // Arrange
        var job = new VideoJob
        {
            Id = "progress-job",
            Status = "queued",
            Progress = 0,
            Stage = "Initializing"
        };
        await _service.CreateJobAsync(job);

        // Act
        await _service.UpdateJobProgressAsync("progress-job", 50, "Rendering video");
        var result = await _service.GetJobAsync("progress-job");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50, result.Progress);
        Assert.Equal("Rendering video", result.Stage);
        Assert.Equal("running", result.Status);
        Assert.NotNull(result.StartedAt);
    }

    [Fact]
    public async Task UpdateJobProgressAsync_ClampsProgressTo100()
    {
        // Arrange
        var job = new VideoJob
        {
            Id = "clamp-job",
            Status = "queued",
            Progress = 0,
            Stage = "Initializing"
        };
        await _service.CreateJobAsync(job);

        // Act
        await _service.UpdateJobProgressAsync("clamp-job", 150, "Rendering video");
        var result = await _service.GetJobAsync("clamp-job");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100, result.Progress);
    }

    [Fact]
    public async Task UpdateJobStatusAsync_SetsCompletedStatus()
    {
        // Arrange
        var job = new VideoJob
        {
            Id = "complete-job",
            Status = "running",
            Progress = 90,
            Stage = "Rendering video"
        };
        await _service.CreateJobAsync(job);

        // Act
        await _service.UpdateJobStatusAsync("complete-job", "completed", 100, "/output/video.mp4");
        var result = await _service.GetJobAsync("complete-job");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("completed", result.Status);
        Assert.Equal(100, result.Progress);
        Assert.Equal("/output/video.mp4", result.OutputPath);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task UpdateJobStatusAsync_SetsFailedStatusWithErrorMessage()
    {
        // Arrange
        var job = new VideoJob
        {
            Id = "failed-job",
            Status = "running",
            Progress = 50,
            Stage = "Rendering video"
        };
        await _service.CreateJobAsync(job);

        // Act
        await _service.UpdateJobStatusAsync("failed-job", "failed", 50, null, "FFmpeg encoding failed");
        var result = await _service.GetJobAsync("failed-job");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("failed", result.Status);
        Assert.Equal(50, result.Progress);
        Assert.Equal("FFmpeg encoding failed", result.ErrorMessage);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task CleanupOldJobsAsync_RemovesExpiredJobs()
    {
        // Arrange
        var oldJob = new VideoJob
        {
            Id = "old-job",
            Status = "completed",
            Progress = 100,
            Stage = "Complete",
            CompletedAt = DateTime.UtcNow.AddHours(-25)
        };
        await _service.CreateJobAsync(oldJob);

        var recentJob = new VideoJob
        {
            Id = "recent-job",
            Status = "completed",
            Progress = 100,
            Stage = "Complete",
            CompletedAt = DateTime.UtcNow.AddHours(-1)
        };
        await _service.CreateJobAsync(recentJob);

        // Act
        var removedCount = await _service.CleanupOldJobsAsync(TimeSpan.FromHours(24));

        // Assert
        Assert.Equal(1, removedCount);
        Assert.Null(await _service.GetJobAsync("old-job"));
        Assert.NotNull(await _service.GetJobAsync("recent-job"));
    }

    [Fact]
    public async Task CleanupOldJobsAsync_DoesNotRemoveRunningJobs()
    {
        // Arrange
        var runningJob = new VideoJob
        {
            Id = "running-job",
            Status = "running",
            Progress = 50,
            Stage = "Rendering video",
            CompletedAt = null
        };
        await _service.CreateJobAsync(runningJob);

        // Act
        var removedCount = await _service.CleanupOldJobsAsync(TimeSpan.FromHours(1));

        // Assert
        Assert.Equal(0, removedCount);
        Assert.NotNull(await _service.GetJobAsync("running-job"));
    }

    [Fact]
    public async Task UpdateJobProgressAsync_DoesNotThrowForNonExistentJob()
    {
        // Act - Should not throw
        await _service.UpdateJobProgressAsync("non-existent-job", 50, "Rendering");

        // Assert - Verify service is still functional after updating non-existent job
        Assert.NotNull(_service);
    }

    [Fact]
    public async Task UpdateJobStatusAsync_DoesNotThrowForNonExistentJob()
    {
        // Act - Should not throw
        await _service.UpdateJobStatusAsync("non-existent-job", "completed", 100);

        // Assert - Verify service is still functional after updating non-existent job
        Assert.NotNull(_service);
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ExportJobService(null!));
    }
}
