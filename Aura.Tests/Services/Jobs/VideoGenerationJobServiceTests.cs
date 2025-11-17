using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Models.Jobs;
using Aura.Core.Orchestrator;
using Aura.Core.Services.Jobs;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using JobStatus = Aura.Core.Models.Jobs.JobStatus;

namespace Aura.Tests.Services.Jobs;

public class VideoGenerationJobServiceTests
{
    private readonly Mock<VideoOrchestrator> _mockOrchestrator;
    private readonly VideoGenerationJobService _jobService;

    public VideoGenerationJobServiceTests()
    {
        _mockOrchestrator = new Mock<VideoOrchestrator>(
            MockBehavior.Loose,
            new object[]
            {
                null!, null!, null!, null!, null!, null!, null!, null!,
                null!, null!, null!, null!, null!, null!, null!, null!,
                null!, null!, null!, null!, null!
            }
        );
        
        _jobService = new VideoGenerationJobService(
            NullLogger<VideoGenerationJobService>.Instance,
            _mockOrchestrator.Object
        );
    }

    [Fact]
    public void CreateJob_ShouldGenerateJobWithCorrectProperties()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Video",
            Audience: "General",
            Goal: "Test",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Documentary"
        );
        var voiceSpec = new VoiceSpec(
            VoiceName: "default",
            Rate: 1.0,
            Pitch: 1.0,
            Pause: PauseStyle.Natural
        );
        var renderSpec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 2500,
            AudioBitrateK: 128,
            Fps: 30,
            Codec: "libx264"
        );
        var systemProfile = new SystemProfile { Tier = HardwareTier.B };

        // Act
        var jobId = _jobService.CreateJob(brief, planSpec, voiceSpec, renderSpec, systemProfile);

        // Assert
        Assert.NotNull(jobId);
        Assert.NotEmpty(jobId);
        
        var job = _jobService.GetJobStatus(jobId);
        Assert.NotNull(job);
        Assert.Equal(JobStatus.Pending, job.Status);
        Assert.Equal(brief.Topic, job.Brief.Topic);
    }

    [Fact]
    public async Task ExecuteJobAsync_Success_ShouldUpdateStatusToCompleted()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Video",
            Audience: "General",
            Goal: "Test",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Documentary"
        );
        var voiceSpec = new VoiceSpec(
            VoiceName: "default",
            Rate: 1.0,
            Pitch: 1.0,
            Pause: PauseStyle.Natural
        );
        var renderSpec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 2500,
            AudioBitrateK: 128,
            Fps: 30,
            Codec: "libx264"
        );
        var systemProfile = new SystemProfile { Tier = HardwareTier.B };

        var jobId = _jobService.CreateJob(brief, planSpec, voiceSpec, renderSpec, systemProfile);
        
        _mockOrchestrator
            .Setup(o => o.GenerateVideoAsync(
                It.IsAny<Brief>(),
                It.IsAny<PlanSpec>(),
                It.IsAny<VoiceSpec>(),
                It.IsAny<RenderSpec>(),
                It.IsAny<SystemProfile>(),
                It.IsAny<IProgress<string>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IProgress<GenerationProgress>>()
            ))
            .ReturnsAsync("/path/to/output.mp4");

        // Act
        await _jobService.ExecuteJobAsync(jobId);

        // Assert
        var job = _jobService.GetJobStatus(jobId);
        Assert.NotNull(job);
        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.NotNull(job.OutputPath);
        Assert.Equal("/path/to/output.mp4", job.OutputPath);
        Assert.NotNull(job.StartedAt);
        Assert.NotNull(job.CompletedAt);
    }

    [Fact]
    public async Task ExecuteJobAsync_Cancelled_ShouldUpdateStatusToCancelled()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Video",
            Audience: "General",
            Goal: "Test",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Documentary"
        );
        var voiceSpec = new VoiceSpec(
            VoiceName: "default",
            Rate: 1.0,
            Pitch: 1.0,
            Pause: PauseStyle.Natural
        );
        var renderSpec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 2500,
            AudioBitrateK: 128,
            Fps: 30,
            Codec: "libx264"
        );
        var systemProfile = new SystemProfile { Tier = HardwareTier.B };

        var jobId = _jobService.CreateJob(brief, planSpec, voiceSpec, renderSpec, systemProfile);
        
        _mockOrchestrator
            .Setup(o => o.GenerateVideoAsync(
                It.IsAny<Brief>(),
                It.IsAny<PlanSpec>(),
                It.IsAny<VoiceSpec>(),
                It.IsAny<RenderSpec>(),
                It.IsAny<SystemProfile>(),
                It.IsAny<IProgress<string>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IProgress<GenerationProgress>>()
            ))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            _jobService.ExecuteJobAsync(jobId, cts.Token));
        
        var job = _jobService.GetJobStatus(jobId);
        Assert.NotNull(job);
        Assert.Equal(JobStatus.Cancelled, job.Status);
    }

    [Fact]
    public void GetJobs_WithStatusFilter_ShouldReturnFilteredJobs()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Documentary"
        );
        var voiceSpec = new VoiceSpec(
            VoiceName: "default",
            Rate: 1.0,
            Pitch: 1.0,
            Pause: PauseStyle.Natural
        );
        var renderSpec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 2500,
            AudioBitrateK: 128,
            Fps: 30,
            Codec: "libx264"
        );
        var systemProfile = new SystemProfile { Tier = HardwareTier.B };

        var jobId1 = _jobService.CreateJob(brief, planSpec, voiceSpec, renderSpec, systemProfile);
        var jobId2 = _jobService.CreateJob(brief, planSpec, voiceSpec, renderSpec, systemProfile);

        // Act
        var allJobs = _jobService.GetJobs();
        var pendingJobs = _jobService.GetJobs(JobStatus.Pending);

        // Assert
        Assert.Equal(2, allJobs.Count);
        Assert.Equal(2, pendingJobs.Count);
        Assert.All(pendingJobs, job => Assert.Equal(JobStatus.Pending, job.Status));
    }

    [Fact]
    public void CancelJob_RunningJob_ShouldCancelSuccessfully()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Documentary"
        );
        var voiceSpec = new VoiceSpec(
            VoiceName: "default",
            Rate: 1.0,
            Pitch: 1.0,
            Pause: PauseStyle.Natural
        );
        var renderSpec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 2500,
            AudioBitrateK: 128,
            Fps: 30,
            Codec: "libx264"
        );
        var systemProfile = new SystemProfile { Tier = HardwareTier.B };

        var jobId = _jobService.CreateJob(brief, planSpec, voiceSpec, renderSpec, systemProfile);
        
        // Manually set to running (simulating ExecuteJobAsync start)
        var job = _jobService.GetJobStatus(jobId);
        job!.Status = JobStatus.Running;

        // Act
        var cancelled = _jobService.CancelJob(jobId);

        // Assert
        Assert.True(cancelled);
        Assert.Equal(JobStatus.Cancelled, job.Status);
    }

    [Fact]
    public void CleanupOldJobs_ShouldRemoveExpiredJobs()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test",
            Audience: "General",
            Goal: "Test",
            Tone: "Professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9
        );
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Documentary"
        );
        var voiceSpec = new VoiceSpec(
            VoiceName: "default",
            Rate: 1.0,
            Pitch: 1.0,
            Pause: PauseStyle.Natural
        );
        var renderSpec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 2500,
            AudioBitrateK: 128,
            Fps: 30,
            Codec: "libx264"
        );
        var systemProfile = new SystemProfile { Tier = HardwareTier.B };

        var jobId = _jobService.CreateJob(brief, planSpec, voiceSpec, renderSpec, systemProfile);
        
        // Mark as completed in the past
        var job = _jobService.GetJobStatus(jobId);
        job!.Status = JobStatus.Completed;
        job!.CompletedAt = DateTime.UtcNow.AddDays(-2);

        // Act
        var cleanedCount = _jobService.CleanupOldJobs(TimeSpan.FromDays(1));

        // Assert
        Assert.Equal(1, cleanedCount);
        Assert.Null(_jobService.GetJobStatus(jobId));
    }
}
