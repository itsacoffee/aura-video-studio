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
    private readonly ILogger<VideoOrchestrator> _orchestratorLogger;
    private readonly ILogger<VideoGenerationOrchestrator> _smartOrchestratorLogger;
    private readonly ILogger<ResourceMonitor> _monitorLogger;
    private readonly ILogger<StrategySelector> _selectorLogger;

    public VideoOrchestratorIntegrationTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _orchestratorLogger = loggerFactory.CreateLogger<VideoOrchestrator>();
        _smartOrchestratorLogger = loggerFactory.CreateLogger<VideoGenerationOrchestrator>();
        _monitorLogger = loggerFactory.CreateLogger<ResourceMonitor>();
        _selectorLogger = loggerFactory.CreateLogger<StrategySelector>();
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

        var orchestrator = new VideoOrchestrator(
            _orchestratorLogger,
            mockLlmProvider,
            mockTtsProvider,
            mockVideoComposer,
            smartOrchestrator,
            monitor,
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

        var orchestrator = new VideoOrchestrator(
            _orchestratorLogger,
            mockLlmProvider,
            mockTtsProvider,
            mockVideoComposer,
            smartOrchestrator,
            monitor);

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
            return Task.FromResult("## Scene 1\nThis is a test script about AI.\n\n## Scene 2\nIt covers the basics.");
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
}
