using System;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class JobRunnerTests
{
    [Fact]
    public void ArtifactManager_Should_CreateJobDirectory()
    {
        // Arrange
        var manager = new ArtifactManager(NullLogger<ArtifactManager>.Instance);
        var jobId = Guid.NewGuid().ToString();

        // Act
        var jobDir = manager.GetJobDirectory(jobId);

        // Assert
        Assert.NotNull(jobDir);
        Assert.Contains(jobId, jobDir);
    }

    [Fact]
    public void ArtifactManager_Should_SaveAndLoadJob()
    {
        // Arrange
        var manager = new ArtifactManager(NullLogger<ArtifactManager>.Instance);
        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Stage = "Script",
            Status = JobStatus.Running,
            Percent = 50,
            StartedAt = DateTime.UtcNow
        };

        // Act
        manager.SaveJob(job);
        var loadedJob = manager.LoadJob(job.Id);

        // Assert
        Assert.NotNull(loadedJob);
        Assert.Equal(job.Id, loadedJob.Id);
        Assert.Equal(job.Stage, loadedJob.Stage);
        Assert.Equal(job.Status, loadedJob.Status);
    }

    [Fact]
    public void ArtifactManager_Should_ListJobs()
    {
        // Arrange
        var manager = new ArtifactManager(NullLogger<ArtifactManager>.Instance);
        var job1 = new Job { Id = Guid.NewGuid().ToString(), Stage = "Script" };
        var job2 = new Job { Id = Guid.NewGuid().ToString(), Stage = "Render" };

        manager.SaveJob(job1);
        manager.SaveJob(job2);

        // Act
        var jobs = manager.ListJobs();

        // Assert
        Assert.NotEmpty(jobs);
        Assert.Contains(jobs, j => j.Id == job1.Id);
        Assert.Contains(jobs, j => j.Id == job2.Id);
    }

    [Fact]
    public void ArtifactManager_Should_CreateArtifact()
    {
        // Arrange
        var manager = new ArtifactManager(NullLogger<ArtifactManager>.Instance);
        var jobId = Guid.NewGuid().ToString();
        var testPath = "/tmp/test.mp4";

        // Act
        var artifact = manager.CreateArtifact(jobId, "video.mp4", testPath, "video/mp4");

        // Assert
        Assert.NotNull(artifact);
        Assert.Equal("video.mp4", artifact.Name);
        Assert.Equal(testPath, artifact.Path);
        Assert.Equal("video/mp4", artifact.Type);
    }

    [Fact]
    public void ArtifactManager_Should_UseStandardPath()
    {
        // Arrange
        var manager = new ArtifactManager(NullLogger<ArtifactManager>.Instance);
        var jobId = Guid.NewGuid().ToString();

        // Act
        var jobDir = manager.GetJobDirectory(jobId);

        // Assert
        Assert.NotNull(jobDir);
        Assert.Contains("Aura", jobDir);
        Assert.Contains("jobs", jobDir);
        Assert.Contains(jobId, jobDir);
        Assert.True(System.IO.Directory.Exists(jobDir), "Job directory should be created");
    }

    [Fact]
    public void ArtifactManager_Should_CreateDirectoryIfMissing()
    {
        // Arrange
        var manager = new ArtifactManager(NullLogger<ArtifactManager>.Instance);
        var jobId = Guid.NewGuid().ToString();

        // Act
        var jobDir1 = manager.GetJobDirectory(jobId);
        var jobDir2 = manager.GetJobDirectory(jobId);

        // Assert
        Assert.Equal(jobDir1, jobDir2);
        Assert.True(System.IO.Directory.Exists(jobDir1));
    }
}
