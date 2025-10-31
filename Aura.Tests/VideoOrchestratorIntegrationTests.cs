using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Visual;
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
        var mockTtsProvider = new TestMockTtsProvider();
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

        var timelineBuilder = new Aura.Core.Timeline.TimelineBuilder();
        var providerSettings = new Aura.Core.Configuration.ProviderSettings(
            _loggerFactory.CreateLogger<Aura.Core.Configuration.ProviderSettings>());

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
            timelineBuilder,
            providerSettings,
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
        var mockTtsProvider = new TestMockTtsProvider();
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

        var timelineBuilder = new Aura.Core.Timeline.TimelineBuilder();
        var providerSettings = new Aura.Core.Configuration.ProviderSettings(
            _loggerFactory.CreateLogger<Aura.Core.Configuration.ProviderSettings>());

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
            timelineBuilder,
            providerSettings);

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

        public Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(
            string sceneText,
            string? previousSceneText,
            string videoGoal,
            CancellationToken ct)
        {
            return Task.FromResult<SceneAnalysisResult?>(null);
        }

        public Task<VisualPromptResult?> GenerateVisualPromptAsync(
            string sceneText,
            string? previousSceneText,
            string videoTone,
            VisualStyle targetStyle,
            CancellationToken ct)
        {
            return Task.FromResult<VisualPromptResult?>(null);
        }

        public Task<ContentComplexityAnalysisResult?> AnalyzeContentComplexityAsync(
            string sceneText,
            string? previousSceneText,
            string videoGoal,
            CancellationToken ct)
        {
            return Task.FromResult<ContentComplexityAnalysisResult?>(null);
        }
    }

    private class TestMockTtsProvider : ITtsProvider
    {
        public bool SynthesizeCalled { get; private set; }

        public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(new List<string> { "en-US-AriaNeural" });
        }

        public async Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec spec, CancellationToken ct)
        {
            SynthesizeCalled = true;
            
            // Create a temporary valid WAV file for testing
            var outputPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test-audio-{Guid.NewGuid()}.wav");
            
            // Generate a simple valid WAV file (silent audio, 1 second)
            // RIFF header for a valid WAV file
            var sampleRate = 44100;
            var channels = 1;
            var bitsPerSample = 16;
            var duration = 1.0; // 1 second of audio
            var numSamples = (int)(sampleRate * duration);
            var dataSize = numSamples * channels * (bitsPerSample / 8);
            
            using (var fs = new System.IO.FileStream(outputPath, System.IO.FileMode.Create))
            using (var writer = new System.IO.BinaryWriter(fs))
            {
                // RIFF chunk
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataSize); // File size - 8
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
                
                // fmt chunk
                writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16); // Chunk size
                writer.Write((short)1); // Audio format (PCM)
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * (bitsPerSample / 8)); // Byte rate
                writer.Write((short)(channels * (bitsPerSample / 8))); // Block align
                writer.Write((short)bitsPerSample);
                
                // data chunk
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);
                
                // Write silence (zeros)
                for (int i = 0; i < numSamples; i++)
                {
                    writer.Write((short)0);
                }
            }
            
            return await Task.FromResult(outputPath);
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
        public async Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct)
        {
            // Create a minimal valid JPEG file (1x1 pixel)
            var imagePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test-image-{Guid.NewGuid()}.jpg");
            
            // Minimal JPEG file header (1x1 red pixel)
            byte[] minimalJpeg = new byte[]
            {
                0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0x01, 0x01, 0x00, 0x48,
                0x00, 0x48, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43, 0x00, 0x03, 0x02, 0x02, 0x03, 0x02, 0x02, 0x03,
                0x03, 0x03, 0x03, 0x04, 0x03, 0x03, 0x04, 0x05, 0x08, 0x05, 0x05, 0x04, 0x04, 0x05, 0x0A, 0x07,
                0x07, 0x06, 0x08, 0x0C, 0x0A, 0x0C, 0x0C, 0x0B, 0x0A, 0x0B, 0x0B, 0x0D, 0x0E, 0x12, 0x10, 0x0D,
                0x0E, 0x11, 0x0E, 0x0B, 0x0B, 0x10, 0x16, 0x10, 0x11, 0x13, 0x14, 0x15, 0x15, 0x15, 0x0C, 0x0F,
                0x17, 0x18, 0x16, 0x14, 0x18, 0x12, 0x14, 0x15, 0x14, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
                0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x14, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09, 0xFF, 0xC4, 0x00, 0x14,
                0x10, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0xD2, 0xCF, 0x20, 0xFF,
                0xD9
            };
            
            await System.IO.File.WriteAllBytesAsync(imagePath, minimalJpeg, ct);
            
            var assets = new List<Asset>
            {
                new Asset("image", imagePath, "CC0", null)
            };
            return await Task.FromResult<IReadOnlyList<Asset>>(assets);
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
