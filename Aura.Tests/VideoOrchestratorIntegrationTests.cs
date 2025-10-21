using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.Generation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class VideoOrchestratorIntegrationTests
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<VideoOrchestrator> _orchestratorLogger;
    private readonly ILogger<VideoGenerationOrchestrator> _smartOrchestratorLogger;
    private readonly ILogger<ResourceMonitor> _monitorLogger;
    private readonly ILogger<StrategySelector> _selectorLogger;

    public VideoOrchestratorIntegrationTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _orchestratorLogger = _loggerFactory.CreateLogger<VideoOrchestrator>();
        _smartOrchestratorLogger = _loggerFactory.CreateLogger<VideoGenerationOrchestrator>();
        _monitorLogger = _loggerFactory.CreateLogger<ResourceMonitor>();
        _selectorLogger = _loggerFactory.CreateLogger<StrategySelector>();
    }

    [Fact]
    public async Task GenerateVideoAsync_WithSystemProfile_UsesSmartOrchestration()
    {
        // Arrange
        var monitor = new ResourceMonitor(_monitorLogger);
        var selector = new StrategySelector(_selectorLogger);
        var smartOrchestrator = new VideoGenerationOrchestrator(_smartOrchestratorLogger, monitor, selector);

        var mockLlmProvider = new MockLlmProvider();
        var mockTtsProvider = new MockTtsProvider();
        var mockVideoComposer = new MockVideoComposer();
        var mockImageProvider = new MockImageProvider();
        var mockFfmpegLocator = new MockFfmpegLocator();
        var mockHardwareDetector = new MockHardwareDetector();
        var preGenerationValidator = new Aura.Core.Validation.PreGenerationValidator(
            _loggerFactory.CreateLogger<Aura.Core.Validation.PreGenerationValidator>(),
            mockFfmpegLocator,
            mockHardwareDetector);
        var scriptValidator = new Aura.Core.Validation.ScriptValidator();
        var retryWrapper = new Aura.Core.Services.ProviderRetryWrapper(
            _loggerFactory.CreateLogger<Aura.Core.Services.ProviderRetryWrapper>());
        var ttsValidator = new Aura.Core.Validation.TtsOutputValidator(
            _loggerFactory.CreateLogger<Aura.Core.Validation.TtsOutputValidator>());
        var imageValidator = new Aura.Core.Validation.ImageOutputValidator(
            _loggerFactory.CreateLogger<Aura.Core.Validation.ImageOutputValidator>());
        var llmValidator = new Aura.Core.Validation.LlmOutputValidator(
            _loggerFactory.CreateLogger<Aura.Core.Validation.LlmOutputValidator>());
        var cleanupManager = new Aura.Core.Services.ResourceCleanupManager(
            _loggerFactory.CreateLogger<Aura.Core.Services.ResourceCleanupManager>());

        var orchestrator = new VideoOrchestrator(
            _orchestratorLogger,
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
            mockImageProvider);

        var brief = new Brief("AI Revolution", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(30), Pacing.Conversational, Density.Balanced, "Modern");
        var voiceSpec = new VoiceSpec("en-US-AriaNeural", 1.0, 1.0, PauseStyle.Natural);
        var renderSpec = new RenderSpec(
            new Resolution(1920, 1080),
            "mp4",
            5000,
            192,
            30,
            "H264",
            75,
            true);
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = 4,
            PhysicalCores = 2,
            RamGB = 8,
            OfflineOnly = false
        };

        int progressReports = 0;
        var progress = new Progress<string>(msg =>
        {
            progressReports++;
        });

        // Act
        var result = await orchestrator.GenerateVideoAsync(
            brief,
            planSpec,
            voiceSpec,
            renderSpec,
            systemProfile,
            progress,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(progressReports > 0, "Progress should be reported");
        Assert.True(mockLlmProvider.DraftScriptCalled, "Script generation should be called");
        Assert.True(mockTtsProvider.SynthesizeCalled, "TTS should be called");
        Assert.True(mockVideoComposer.RenderCalled, "Video composition should be called");
    }

    [Fact]
    public async Task GenerateVideoAsync_WithoutSystemProfile_UsesFallbackOrchestration()
    {
        // Arrange
        var monitor = new ResourceMonitor(_monitorLogger);
        var selector = new StrategySelector(_selectorLogger);
        var smartOrchestrator = new VideoGenerationOrchestrator(_smartOrchestratorLogger, monitor, selector);

        var mockLlmProvider = new MockLlmProvider();
        var mockTtsProvider = new MockTtsProvider();
        var mockVideoComposer = new MockVideoComposer();
        var mockFfmpegLocator = new MockFfmpegLocator();
        var mockHardwareDetector = new MockHardwareDetector();
        var preGenerationValidator = new Aura.Core.Validation.PreGenerationValidator(
            _loggerFactory.CreateLogger<Aura.Core.Validation.PreGenerationValidator>(),
            mockFfmpegLocator,
            mockHardwareDetector);
        var scriptValidator = new Aura.Core.Validation.ScriptValidator();
        var retryWrapper = new Aura.Core.Services.ProviderRetryWrapper(
            _loggerFactory.CreateLogger<Aura.Core.Services.ProviderRetryWrapper>());
        var ttsValidator = new Aura.Core.Validation.TtsOutputValidator(
            _loggerFactory.CreateLogger<Aura.Core.Validation.TtsOutputValidator>());
        var imageValidator = new Aura.Core.Validation.ImageOutputValidator(
            _loggerFactory.CreateLogger<Aura.Core.Validation.ImageOutputValidator>());
        var llmValidator = new Aura.Core.Validation.LlmOutputValidator(
            _loggerFactory.CreateLogger<Aura.Core.Validation.LlmOutputValidator>());
        var cleanupManager = new Aura.Core.Services.ResourceCleanupManager(
            _loggerFactory.CreateLogger<Aura.Core.Services.ResourceCleanupManager>());

        var orchestrator = new VideoOrchestrator(
            _orchestratorLogger,
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
            cleanupManager);

        var brief = new Brief("Test Video", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(30), Pacing.Conversational, Density.Balanced, "Modern");
        var voiceSpec = new VoiceSpec("en-US-AriaNeural", 1.0, 1.0, PauseStyle.Natural);
        var renderSpec = new RenderSpec(
            new Resolution(1920, 1080),
            "mp4",
            5000,
            192,
            30,
            "H264",
            75,
            true);

        // Act
        var result = await orchestrator.GenerateVideoAsync(
            brief,
            planSpec,
            voiceSpec,
            renderSpec,
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.True(mockLlmProvider.DraftScriptCalled);
        Assert.True(mockTtsProvider.SynthesizeCalled);
        Assert.True(mockVideoComposer.RenderCalled);
    }

    // Mock implementations
    private class MockLlmProvider : ILlmProvider
    {
        public bool DraftScriptCalled { get; private set; }

        public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
        {
            DraftScriptCalled = true;
            // Return a properly formatted script with title, at least 2 scenes, and appropriate word count for the duration
            // For 30 seconds, we need ~75 words (2.5 words per second)
            return Task.FromResult(@"# AI Revolution

## Scene 1
Artificial intelligence is transforming our world. From self-driving cars to smart assistants, AI is everywhere. This technology enables machines to learn and perform tasks that require human intelligence.

## Scene 2
Today AI is used in healthcare finance education and entertainment. Machine learning analyzes data to make predictions recognize patterns and automate processes. The future is exciting.");
        }
    }

    private class MockTtsProvider : ITtsProvider
    {
        public bool SynthesizeCalled { get; private set; }

        public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(new List<string> { "en-US-AriaNeural" });
        }

        public Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
        {
            SynthesizeCalled = true;
            return Task.FromResult("/tmp/test-audio.wav");
        }
    }

    private class MockVideoComposer : IVideoComposer
    {
        public bool RenderCalled { get; private set; }

        public Task<string> RenderAsync(Aura.Core.Providers.Timeline timeline, RenderSpec spec, IProgress<RenderProgress> progress, CancellationToken ct)
        {
            RenderCalled = true;
            progress?.Report(new RenderProgress(100, TimeSpan.FromSeconds(1), TimeSpan.Zero, "Complete"));
            return Task.FromResult("/tmp/test-video.mp4");
        }
    }

    private class MockImageProvider : IImageProvider
    {
        public Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct)
        {
            var assets = new List<Asset>
            {
                new Asset("image", "/tmp/test-image.jpg", "CC0", null)
            };
            return Task.FromResult<IReadOnlyList<Asset>>(assets);
        }
    }

    private class MockFfmpegLocator : Aura.Core.Dependencies.IFfmpegLocator
    {
        private readonly string _mockPath;

        public MockFfmpegLocator()
        {
            // Create a temporary file to simulate FFmpeg
            _mockPath = System.IO.Path.GetTempFileName();
        }

        public Task<string> GetEffectiveFfmpegPathAsync(string? configuredPath = null, CancellationToken ct = default)
        {
            return Task.FromResult(_mockPath);
        }

        public Task<Aura.Core.Dependencies.FfmpegValidationResult> CheckAllCandidatesAsync(string? configuredPath = null, CancellationToken ct = default)
        {
            return Task.FromResult(new Aura.Core.Dependencies.FfmpegValidationResult
            {
                Found = true,
                FfmpegPath = _mockPath,
                VersionString = "4.4.0",
                ValidationOutput = "ffmpeg version 4.4.0",
                Reason = "Mock FFmpeg",
                HasX264 = true,
                Source = "Mock"
            });
        }

        public Task<Aura.Core.Dependencies.FfmpegValidationResult> ValidatePathAsync(string ffmpegPath, CancellationToken ct = default)
        {
            return Task.FromResult(new Aura.Core.Dependencies.FfmpegValidationResult
            {
                Found = true,
                FfmpegPath = ffmpegPath,
                VersionString = "4.4.0",
                ValidationOutput = "ffmpeg version 4.4.0",
                Reason = "Mock FFmpeg",
                HasX264 = true,
                Source = "Mock"
            });
        }
    }

    private class MockHardwareDetector : Aura.Core.Hardware.IHardwareDetector
    {
        public Task<SystemProfile> DetectSystemAsync()
        {
            return Task.FromResult(new SystemProfile
            {
                AutoDetect = true,
                LogicalCores = 8,
                PhysicalCores = 4,
                RamGB = 16,
                Gpu = new GpuInfo("NVIDIA", "GTX 1080", 8, "10"),
                Tier = HardwareTier.B,
                EnableNVENC = true,
                EnableSD = true,
                OfflineOnly = false
            });
        }

        public SystemProfile ApplyManualOverrides(SystemProfile detected, HardwareOverrides overrides)
        {
            return detected;
        }

        public Task RunHardwareProbeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
