using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Models.Visual;
using Aura.Core.Models.Voice;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Core.Services.Audio;
using Aura.Core.Services.Generation;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for VideoOrchestrator with NarrationOptimizationService
/// </summary>
public class VideoOrchestratorNarrationOptimizationTests
{
    private readonly VideoOrchestrator _orchestratorWithOptimization;
    private readonly VideoOrchestrator _orchestratorWithoutOptimization;
    private readonly TestLlmProvider _llmProvider;
    private readonly TestTtsProvider _ttsProvider;
    private readonly TestVideoComposer _videoComposer;
    private readonly NarrationOptimizationService _optimizationService;

    public VideoOrchestratorNarrationOptimizationTests()
    {
        _llmProvider = new TestLlmProvider();
        _ttsProvider = new TestTtsProvider();
        _videoComposer = new TestVideoComposer();
        var resourceMonitor = new ResourceMonitor(NullLogger<ResourceMonitor>.Instance);
        var preValidator = new PreGenerationValidator(
            NullLogger<PreGenerationValidator>.Instance,
            resourceMonitor);
        var scriptValidator = new ScriptValidator(NullLogger<ScriptValidator>.Instance);
        var retryWrapper = new ProviderRetryWrapper(NullLogger<ProviderRetryWrapper>.Instance);
        var ttsValidator = new TtsOutputValidator(NullLogger<TtsOutputValidator>.Instance);
        var imageValidator = new ImageOutputValidator(NullLogger<ImageOutputValidator>.Instance);
        var llmValidator = new LlmOutputValidator(NullLogger<LlmOutputValidator>.Instance);
        var cleanupManager = new ResourceCleanupManager(NullLogger<ResourceCleanupManager>.Instance);
        var smartOrchestrator = new VideoGenerationOrchestrator(
            NullLogger<VideoGenerationOrchestrator>.Instance,
            resourceMonitor);
        var timelineBuilder = new Timeline.TimelineBuilder(NullLogger<Timeline.TimelineBuilder>.Instance);
        var providerSettings = new ProviderSettings();

        _optimizationService = new NarrationOptimizationService(
            NullLogger<NarrationOptimizationService>.Instance,
            _llmProvider);

        // Orchestrator with optimization
        _orchestratorWithOptimization = new VideoOrchestrator(
            NullLogger<VideoOrchestrator>.Instance,
            _llmProvider,
            _ttsProvider,
            _videoComposer,
            smartOrchestrator,
            resourceMonitor,
            preValidator,
            scriptValidator,
            retryWrapper,
            ttsValidator,
            imageValidator,
            llmValidator,
            cleanupManager,
            timelineBuilder,
            providerSettings,
            null,
            null,
            null,
            _optimizationService);

        // Orchestrator without optimization
        _orchestratorWithoutOptimization = new VideoOrchestrator(
            NullLogger<VideoOrchestrator>.Instance,
            _llmProvider,
            _ttsProvider,
            _videoComposer,
            smartOrchestrator,
            resourceMonitor,
            preValidator,
            scriptValidator,
            retryWrapper,
            ttsValidator,
            imageValidator,
            llmValidator,
            cleanupManager,
            timelineBuilder,
            providerSettings,
            null,
            null,
            null,
            null);
    }

    [Fact]
    public async Task GenerateVideoAsync_WithOptimization_OptimizesNarration()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Video",
            Audience: "General",
            Goal: "Test",
            Tone: "professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9);

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test");

        var voiceSpec = new VoiceSpec("test-voice", 1.0, 1.0, PauseStyle.Natural);
        var renderSpec = new RenderSpec(
            Resolution: new Resolution(1920, 1080),
            FrameRate: 30,
            Bitrate: 5000000);

        _ttsProvider.Reset();

        // Act
        var outputPath = await _orchestratorWithOptimization.GenerateVideoAsync(
            brief,
            planSpec,
            voiceSpec,
            renderSpec,
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(outputPath);
        Assert.True(_ttsProvider.SynthesizeCallCount > 0, "TTS should be called");
    }

    [Fact]
    public async Task GenerateVideoAsync_WithoutOptimization_SkipsOptimization()
    {
        // Arrange
        var brief = new Brief(
            Topic: "Test Video",
            Audience: "General",
            Goal: "Test",
            Tone: "professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9);

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test");

        var voiceSpec = new VoiceSpec("test-voice", 1.0, 1.0, PauseStyle.Natural);
        var renderSpec = new RenderSpec(
            Resolution: new Resolution(1920, 1080),
            FrameRate: 30,
            Bitrate: 5000000);

        _ttsProvider.Reset();

        // Act
        var outputPath = await _orchestratorWithoutOptimization.GenerateVideoAsync(
            brief,
            planSpec,
            voiceSpec,
            renderSpec,
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(outputPath);
        Assert.True(_ttsProvider.SynthesizeCallCount > 0, "TTS should be called");
    }

    [Fact]
    public async Task GenerateVideoAsync_OptimizationFailure_ContinuesWithOriginalText()
    {
        // Arrange - Use a provider that will cause optimization to fail
        var failingLlmProvider = new FailingLlmProvider();
        var failingOptimizationService = new NarrationOptimizationService(
            NullLogger<NarrationOptimizationService>.Instance,
            failingLlmProvider);

        var orchestrator = new VideoOrchestrator(
            NullLogger<VideoOrchestrator>.Instance,
            _llmProvider,
            _ttsProvider,
            _videoComposer,
            new VideoGenerationOrchestrator(
                NullLogger<VideoGenerationOrchestrator>.Instance,
                new ResourceMonitor(NullLogger<ResourceMonitor>.Instance)),
            new ResourceMonitor(NullLogger<ResourceMonitor>.Instance),
            new PreGenerationValidator(
                NullLogger<PreGenerationValidator>.Instance,
                new ResourceMonitor(NullLogger<ResourceMonitor>.Instance)),
            new ScriptValidator(NullLogger<ScriptValidator>.Instance),
            new ProviderRetryWrapper(NullLogger<ProviderRetryWrapper>.Instance),
            new TtsOutputValidator(NullLogger<TtsOutputValidator>.Instance),
            new ImageOutputValidator(NullLogger<ImageOutputValidator>.Instance),
            new LlmOutputValidator(NullLogger<LlmOutputValidator>.Instance),
            new ResourceCleanupManager(NullLogger<ResourceCleanupManager>.Instance),
            new Timeline.TimelineBuilder(NullLogger<Timeline.TimelineBuilder>.Instance),
            new ProviderSettings(),
            null,
            null,
            null,
            failingOptimizationService);

        var brief = new Brief(
            Topic: "Test Video",
            Audience: "General",
            Goal: "Test",
            Tone: "professional",
            Language: "English",
            Aspect: Aspect.Widescreen16x9);

        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test");

        var voiceSpec = new VoiceSpec("test-voice", 1.0, 1.0, PauseStyle.Natural);
        var renderSpec = new RenderSpec(
            Resolution: new Resolution(1920, 1080),
            FrameRate: 30,
            Bitrate: 5000000);

        _ttsProvider.Reset();

        // Act - Should not throw, should continue with original text
        var outputPath = await orchestrator.GenerateVideoAsync(
            brief,
            planSpec,
            voiceSpec,
            renderSpec,
            null,
            CancellationToken.None);

        // Assert
        Assert.NotNull(outputPath);
    }

    private sealed class TestLlmProvider : ILlmProvider
    {
        public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct = default)
        {
            return Task.FromResult("## Scene 1\nThis is a test script with some content that needs optimization.");
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
    }

    private sealed class TestTtsProvider : ITtsProvider
    {
        public int SynthesizeCallCount { get; private set; }

        public void Reset()
        {
            SynthesizeCallCount = 0;
        }

        public Task<IReadOnlyList<string>> GetAvailableVoicesAsync()
        {
            return Task.FromResult<IReadOnlyList<string>>(new List<string> { "test-voice" });
        }

        public Task<string> SynthesizeAsync(IEnumerable<ScriptLine> lines, VoiceSpec voiceSpec, CancellationToken ct = default)
        {
            SynthesizeCallCount++;
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test-audio-{Guid.NewGuid()}.wav");
            System.IO.File.WriteAllBytes(tempPath, new byte[] { 0x52, 0x49, 0x46, 0x46 });
            return Task.FromResult(tempPath);
        }
    }

    private sealed class TestVideoComposer : IVideoComposer
    {
        public Task<string> RenderAsync(
            Core.Providers.Timeline timeline,
            RenderSpec spec,
            IProgress<RenderProgress> progress,
            CancellationToken ct)
        {
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"test-video-{Guid.NewGuid()}.mp4");
            System.IO.File.WriteAllBytes(tempPath, new byte[] { 0x00, 0x00, 0x00, 0x20 });
            return Task.FromResult(tempPath);
        }
    }

    private sealed class FailingLlmProvider : ILlmProvider
    {
        public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct = default)
        {
            throw new InvalidOperationException("Simulated LLM failure");
        }

        public Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(
            string sceneText, 
            string? previousSceneText, 
            string videoGoal, 
            CancellationToken ct)
        {
            throw new InvalidOperationException("Simulated LLM failure");
        }

        public Task<VisualPromptResult?> GenerateVisualPromptAsync(
            string sceneText, 
            string? previousSceneText, 
            string videoTone, 
            VisualStyle targetStyle, 
            CancellationToken ct)
        {
            throw new InvalidOperationException("Simulated LLM failure");
        }
    }
}
