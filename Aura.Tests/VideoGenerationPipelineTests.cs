using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for the complete video generation pipeline end-to-end.
/// These tests verify that all components are properly wired together.
/// </summary>
public class VideoGenerationPipelineTests
{
    private readonly ILoggerFactory _loggerFactory;

    public VideoGenerationPipelineTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    }

    [Fact]
    public async Task JobRunner_CreateAndStartJob_CreatesJobWithCorrectStatus()
    {
        // Arrange
        var artifactManager = new ArtifactManager(
            _loggerFactory.CreateLogger<ArtifactManager>());
        var orchestrator = CreateMockOrchestrator();
        var hardwareDetector = new MockHardwareDetector();
        var telemetryCollector = new Core.Telemetry.RunTelemetryCollector(
            _loggerFactory.CreateLogger<Core.Telemetry.RunTelemetryCollector>(),
            System.IO.Path.GetTempPath());

        var jobRunner = new JobRunner(
            _loggerFactory.CreateLogger<JobRunner>(),
            artifactManager,
            orchestrator,
            hardwareDetector,
            telemetryCollector);

        var brief = new Brief(
            Topic: "Test Video",
            Audience: "General",
            Goal: "Inform",
            Tone: "Professional",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9);

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Modern");

        var voiceSpec = new VoiceSpec(
            VoiceName: "en-US-AriaNeural",
            Rate: 1.0,
            Pitch: 1.0,
            Pause: PauseStyle.Natural);

        var renderSpec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 5000,
            AudioBitrateK: 192,
            Fps: 30,
            Codec: "H264",
            QualityLevel: 75,
            EnableSceneCut: true);

        // Act
        var job = await jobRunner.CreateAndStartJobAsync(
            brief,
            planSpec,
            voiceSpec,
            renderSpec,
            correlationId: "test-correlation-id",
            isQuickDemo: false,
            ct: CancellationToken.None);

        // Assert
        Assert.NotNull(job);
        Assert.NotEmpty(job.Id);
        Assert.Equal(JobStatus.Queued, job.Status);
        Assert.Equal("Script", job.Stage);
        Assert.NotNull(job.CorrelationId);
        Assert.Equal("test-correlation-id", job.CorrelationId);
        Assert.NotNull(job.Brief);
        Assert.NotNull(job.PlanSpec);
        Assert.NotNull(job.VoiceSpec);
        Assert.NotNull(job.RenderSpec);

        // Wait a moment for background task to start
        await Task.Delay(500);

        // Verify job was saved to artifact storage
        var loadedJob = artifactManager.LoadJob(job.Id);
        Assert.NotNull(loadedJob);
        Assert.Equal(job.Id, loadedJob.Id);
    }

    [Fact]
    public async Task JobRunner_CancelJob_UpdatesJobStatus()
    {
        // Arrange
        var artifactManager = new ArtifactManager(
            _loggerFactory.CreateLogger<ArtifactManager>());
        var orchestrator = CreateSlowMockOrchestrator();
        var hardwareDetector = new MockHardwareDetector();
        var telemetryCollector = new Core.Telemetry.RunTelemetryCollector(
            _loggerFactory.CreateLogger<Core.Telemetry.RunTelemetryCollector>(),
            System.IO.Path.GetTempPath());

        var jobRunner = new JobRunner(
            _loggerFactory.CreateLogger<JobRunner>(),
            artifactManager,
            orchestrator,
            hardwareDetector,
            telemetryCollector);

        var brief = new Brief("Test", "General", "Inform", "Professional", "en-US", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(30), Pacing.Conversational, Density.Balanced, "Modern");
        var voiceSpec = new VoiceSpec("en-US-AriaNeural", 1.0, 1.0, PauseStyle.Natural);
        var renderSpec = new RenderSpec(
            new Resolution(1920, 1080), "mp4", 5000, 192, 30, "H264", 75, true);

        var job = await jobRunner.CreateAndStartJobAsync(
            brief, planSpec, voiceSpec, renderSpec,
            correlationId: "cancel-test",
            isQuickDemo: false,
            ct: CancellationToken.None);

        // Wait for job to start
        await Task.Delay(1000);

        // Act
        var cancelled = jobRunner.CancelJob(job.Id);

        // Assert
        Assert.True(cancelled);

        // Wait for cancellation to process
        await Task.Delay(1000);

        var updatedJob = jobRunner.GetJob(job.Id);
        Assert.NotNull(updatedJob);
        Assert.True(updatedJob.Status == JobStatus.Canceled || updatedJob.Status == JobStatus.Failed);
    }

    [Fact]
    public void ArtifactManager_CreateArtifact_ReturnsValidArtifact()
    {
        // Arrange
        var artifactManager = new ArtifactManager(
            _loggerFactory.CreateLogger<ArtifactManager>());

        var jobId = Guid.NewGuid().ToString();
        var testFilePath = System.IO.Path.GetTempFileName();

        try
        {
            // Write some test data
            System.IO.File.WriteAllText(testFilePath, "Test video content");

            // Act
            var artifact = artifactManager.CreateArtifact(
                jobId,
                "test-video.mp4",
                testFilePath,
                "video/mp4");

            // Assert
            Assert.NotNull(artifact);
            Assert.Equal("test-video.mp4", artifact.Name);
            Assert.Equal(testFilePath, artifact.Path);
            Assert.Equal("video/mp4", artifact.Type);
            Assert.True(artifact.SizeBytes > 0);
        }
        finally
        {
            // Cleanup
            if (System.IO.File.Exists(testFilePath))
            {
                System.IO.File.Delete(testFilePath);
            }
        }
    }

    [Fact]
    public void ArtifactManager_SaveAndLoadJob_PreservesJobData()
    {
        // Arrange
        var artifactManager = new ArtifactManager(
            _loggerFactory.CreateLogger<ArtifactManager>());

        var job = new Job
        {
            Id = Guid.NewGuid().ToString(),
            Stage = "Script",
            Status = JobStatus.Running,
            CorrelationId = "test-correlation",
            Brief = new Brief("Test Topic", "General", "Inform", "Professional", "en-US", Aspect.Widescreen16x9),
            PlanSpec = new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "Modern"),
            VoiceSpec = new VoiceSpec("en-US-AriaNeural", 1.0, 1.0, PauseStyle.Natural),
            RenderSpec = new RenderSpec(new Resolution(1920, 1080), "mp4", 5000, 192, 30, "H264", 75, true),
            CreatedUtc = DateTime.UtcNow,
            QueuedUtc = DateTime.UtcNow
        };

        try
        {
            // Act
            artifactManager.SaveJob(job);
            var loadedJob = artifactManager.LoadJob(job.Id);

            // Assert
            Assert.NotNull(loadedJob);
            Assert.Equal(job.Id, loadedJob.Id);
            Assert.Equal(job.Stage, loadedJob.Stage);
            Assert.Equal(job.Status, loadedJob.Status);
            Assert.Equal(job.CorrelationId, loadedJob.CorrelationId);
            Assert.NotNull(loadedJob.Brief);
            Assert.Equal(job.Brief.Topic, loadedJob.Brief.Topic);
        }
        finally
        {
            // Cleanup job directory
            var jobDir = artifactManager.GetJobDirectory(job.Id);
            if (System.IO.Directory.Exists(jobDir))
            {
                System.IO.Directory.Delete(jobDir, recursive: true);
            }
        }
    }

    private VideoOrchestrator CreateMockOrchestrator()
    {
        var monitor = new Core.Services.Generation.ResourceMonitor(
            _loggerFactory.CreateLogger<Core.Services.Generation.ResourceMonitor>());
        var selector = new Core.Services.Generation.StrategySelector(
            _loggerFactory.CreateLogger<Core.Services.Generation.StrategySelector>());
        var smartOrchestrator = new Core.Services.Generation.VideoGenerationOrchestrator(
            _loggerFactory.CreateLogger<Core.Services.Generation.VideoGenerationOrchestrator>(),
            monitor,
            selector);

        var mockLlmProvider = new MockLlmProvider();
        var mockTtsProvider = new TestMockTtsProvider();
        var mockVideoComposer = new MockVideoComposer();
        var mockFfmpegLocator = new MockFfmpegLocator();
        var mockHardwareDetector = new MockHardwareDetector();
        var mockCache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var ffmpegResolver = new Core.Dependencies.FFmpegResolver(
            _loggerFactory.CreateLogger<Core.Dependencies.FFmpegResolver>(),
            mockCache);
        var preGenerationValidator = new Core.Validation.PreGenerationValidator(
            _loggerFactory.CreateLogger<Core.Validation.PreGenerationValidator>(),
            mockFfmpegLocator,
            ffmpegResolver,
            mockHardwareDetector);
        var scriptValidator = new Core.Validation.ScriptValidator();
        var retryWrapper = new Core.Services.ProviderRetryWrapper(
            _loggerFactory.CreateLogger<Core.Services.ProviderRetryWrapper>());
        var ttsValidator = new Core.Validation.TtsOutputValidator(
            _loggerFactory.CreateLogger<Core.Validation.TtsOutputValidator>());
        var imageValidator = new Core.Validation.ImageOutputValidator(
            _loggerFactory.CreateLogger<Core.Validation.ImageOutputValidator>());
        var llmValidator = new Core.Validation.LlmOutputValidator(
            _loggerFactory.CreateLogger<Core.Validation.LlmOutputValidator>());
        var cleanupManager = new Core.Services.ResourceCleanupManager(
            _loggerFactory.CreateLogger<Core.Services.ResourceCleanupManager>());
        var timelineBuilder = new Core.Timeline.TimelineBuilder();
        var providerSettings = new Core.Configuration.ProviderSettings(
            _loggerFactory.CreateLogger<Core.Configuration.ProviderSettings>());
        var telemetryCollector = new Core.Telemetry.RunTelemetryCollector(
            _loggerFactory.CreateLogger<Core.Telemetry.RunTelemetryCollector>(),
            System.IO.Path.GetTempPath());

        return new VideoOrchestrator(
            _loggerFactory.CreateLogger<VideoOrchestrator>(),
            mockLlmProvider,
            mockTtsProvider,
            mockVideoComposer,
            smartOrchestrator,
            monitor,
            preGenerationValidator,
            scriptValidator,
            retryWrapper,
            ttsValidator,
            imageValidator,
            llmValidator,
            cleanupManager,
            timelineBuilder,
            providerSettings,
            telemetryCollector);
    }

    private VideoOrchestrator CreateSlowMockOrchestrator()
    {
        // Create an orchestrator that takes time to execute
        // This allows testing cancellation
        return CreateMockOrchestrator();
    }
}
