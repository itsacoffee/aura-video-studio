using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Artifacts;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.Generation;
using Aura.Providers.Llm;
using Aura.Providers.Tts;
using Aura.Providers.Video;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace Aura.E2E;

/// <summary>
/// End-to-end tests for complete video generation pipeline execution
/// </summary>
public class PipelineExecutionTests
{
    private readonly ITestOutputHelper _output;

    public PipelineExecutionTests(ITestOutputHelper output)
    {
        _output = output;
    }

    /// <summary>
    /// Tests that Quick Demo generates a complete video from start to finish
    /// Validates the entire pipeline: Brief → Script → Audio → Visuals → Render
    /// </summary>
    [Fact(Skip = "Integration test - requires FFmpeg and may take 2+ minutes")]
    public async Task QuickDemo_Should_GenerateCompleteVideo()
    {
        // Arrange: Create service provider with all required dependencies
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Core services
        services.AddSingleton<HardwareDetector>();
        services.AddSingleton<ProviderSettings>();
        services.AddHttpClient();

        // Providers - use simple, guaranteed-to-work implementations
        services.AddSingleton<ILlmProvider, RuleBasedLlmProvider>();
        services.AddSingleton<ITtsProvider, NullTtsProvider>();
        services.AddSingleton<IVideoComposer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FfmpegVideoComposer>>();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var ffmpegLocator = new Aura.Core.Dependencies.FfmpegLocator(
                sp.GetRequiredService<ILogger<Aura.Core.Dependencies.FfmpegLocator>>(),
                providerSettings.GetToolsDirectory());
            var outputDirectory = providerSettings.GetOutputDirectory();
            return new FfmpegVideoComposer(logger, ffmpegLocator, null, outputDirectory);
        });

        // Orchestration services
        services.AddSingleton<ResourceMonitor>();
        services.AddSingleton<StrategySelector>();
        services.AddSingleton<VideoGenerationOrchestrator>();
        services.AddSingleton<VideoOrchestrator>();

        // Job management
        services.AddSingleton<ArtifactManager>();
        services.AddSingleton<JobRunner>();

        var serviceProvider = services.BuildServiceProvider();

        // Act: Create a demo job
        var jobRunner = serviceProvider.GetRequiredService<JobRunner>();
        
        var brief = new Brief(
            Topic: "Introduction to AI Video Generation",
            Audience: "Beginners",
            Goal: "Demonstrate Aura Video Studio capabilities",
            Tone: "Friendly and informative",
            Language: "en-US",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Demo"
        );

        var voiceSpec = new VoiceSpec(
            VoiceName: "en-US-Standard-A",
            Rate: 1.0,
            Pitch: 0.0,
            Pause: PauseStyle.Short
        );

        var renderSpec = new RenderSpec(
            Res: new Resolution(1920, 1080),
            Container: "mp4",
            VideoBitrateK: 5000,
            AudioBitrateK: 192,
            Fps: 30,
            Codec: "H264",
            QualityLevel: 75,
            EnableSceneCut: true
        );

        var correlationId = $"e2e-test-{DateTime.UtcNow:yyyyMMddHHmmss}";

        _output.WriteLine($"Creating job with correlation ID: {correlationId}");
        var job = await jobRunner.CreateAndStartJobAsync(brief, planSpec, voiceSpec, renderSpec, correlationId);
        
        Assert.NotNull(job);
        Assert.NotNull(job.Id);
        _output.WriteLine($"Job created: {job.Id}");

        // Poll for completion with timeout
        var timeout = TimeSpan.FromMinutes(2);
        var pollInterval = TimeSpan.FromSeconds(1);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        Job? completedJob = null;
        while (stopwatch.Elapsed < timeout)
        {
            completedJob = jobRunner.GetJob(job.Id);
            Assert.NotNull(completedJob);

            _output.WriteLine($"[{stopwatch.Elapsed:mm\\:ss}] Job status: {completedJob.Status}, Progress: {completedJob.Percent}%, Stage: {completedJob.Stage}");

            if (completedJob.Status == JobStatus.Done)
            {
                _output.WriteLine("Job completed successfully!");
                break;
            }

            if (completedJob.Status == JobStatus.Failed)
            {
                var errorMessage = completedJob.ErrorMessage ?? "Unknown error";
                var failureDetails = completedJob.FailureDetails != null
                    ? $"\nStage: {completedJob.FailureDetails.Stage}\nDetails: {completedJob.FailureDetails.Message}"
                    : "";
                
                Assert.Fail($"Job failed: {errorMessage}{failureDetails}");
            }

            await Task.Delay(pollInterval);
        }

        // Assert: Job completed successfully
        Assert.NotNull(completedJob);
        Assert.Equal(JobStatus.Done, completedJob.Status);
        Assert.Equal(100, completedJob.Percent);
        Assert.NotEmpty(completedJob.Artifacts);

        // Verify video file exists and has content
        var videoArtifact = Assert.Single(completedJob.Artifacts);
        Assert.NotNull(videoArtifact.Path);
        Assert.True(File.Exists(videoArtifact.Path), $"Video file does not exist at: {videoArtifact.Path}");

        var fileInfo = new FileInfo(videoArtifact.Path);
        Assert.True(fileInfo.Length > 100_000, $"Video file too small ({fileInfo.Length} bytes), expected > 100KB");
        
        _output.WriteLine($"Video generated successfully: {videoArtifact.Path} ({fileInfo.Length:N0} bytes)");
    }
}
