using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Core.Services.Generation;
using Aura.Core.Services.Providers;
using Aura.Core.Telemetry;
using Aura.Core.Timeline;
using Aura.Core.Validation;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for VideoOrchestrator pipeline validation functionality
/// </summary>
public class VideoOrchestratorValidationTests
{
    private readonly ILoggerFactory _loggerFactory;

    public VideoOrchestratorValidationTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
    }

    [Fact]
    public async Task ValidatePipelineAsync_WithAllServicesAvailable_ReturnsValid()
    {
        // Arrange
        var orchestrator = CreateOrchestratorWithValidServices();

        // Act
        var (isValid, errors) = await orchestrator.ValidatePipelineAsync(CancellationToken.None);

        // Assert
        Assert.True(isValid, "Pipeline should be valid when all services are available");
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidatePipelineAsync_WithMissingTtsVoices_ReturnsInvalid()
    {
        // Arrange
        var mockTtsProvider = new MockTtsProviderNoVoices();
        var orchestrator = CreateOrchestrator(ttsProvider: mockTtsProvider);

        // Act
        var (isValid, errors) = await orchestrator.ValidatePipelineAsync(CancellationToken.None);

        // Assert
        Assert.False(isValid, "Pipeline should be invalid when TTS has no voices");
        Assert.Contains(errors, e => e.Contains("no available voices"));
    }

    [Fact]
    public async Task ValidatePipelineAsync_WithoutImageProvider_StillValid()
    {
        // Arrange
        var orchestrator = CreateOrchestrator(imageProvider: null);

        // Act
        var (isValid, errors) = await orchestrator.ValidatePipelineAsync(CancellationToken.None);

        // Assert
        Assert.True(isValid, "Pipeline should be valid without image provider (optional)");
        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidatePipelineAsync_WithFFmpegNotFound_ReturnsInvalid()
    {
        // Arrange
        var mockResolver = new MockFFmpegResolverNotFound();
        var orchestrator = CreateOrchestrator(ffmpegResolver: mockResolver);

        // Act
        var (isValid, errors) = await orchestrator.ValidatePipelineAsync(CancellationToken.None);

        // Assert
        Assert.False(isValid, "Pipeline should be invalid when FFmpeg is not found");
        Assert.Contains(errors, e => e.Contains("FFmpeg not found"));
    }

    private VideoOrchestrator CreateOrchestratorWithValidServices()
    {
        return CreateOrchestrator();
    }

    private VideoOrchestrator CreateOrchestrator(
        ILlmProvider? llmProvider = null,
        ITtsProvider? ttsProvider = null,
        IVideoComposer? videoComposer = null,
        IImageProvider? imageProvider = null,
        FFmpegResolver? ffmpegResolver = null)
    {
        var monitor = new ResourceMonitor(_loggerFactory.CreateLogger<ResourceMonitor>());
        var selector = new StrategySelector(_loggerFactory.CreateLogger<StrategySelector>());
        var smartOrchestrator = new VideoGenerationOrchestrator(
            _loggerFactory.CreateLogger<VideoGenerationOrchestrator>(), 
            monitor, 
            selector);

        var mockCache = new MemoryCache(new MemoryCacheOptions());
        var defaultFFmpegResolver = new FFmpegResolver(
            _loggerFactory.CreateLogger<FFmpegResolver>(),
            mockCache);

        var mockFfmpegLocator = new MockFfmpegLocator();
        var mockHardwareDetector = new MockHardwareDetector();
        var preGenerationValidator = new PreGenerationValidator(
            _loggerFactory.CreateLogger<PreGenerationValidator>(),
            mockFfmpegLocator,
            defaultFFmpegResolver,
            mockHardwareDetector,
            CreateReadyProviderReadinessService());

        return new VideoOrchestrator(
            _loggerFactory.CreateLogger<VideoOrchestrator>(),
            llmProvider ?? new MockLlmProvider(),
            ttsProvider ?? new TestMockTtsProvider(),
            videoComposer ?? new MockVideoComposer(),
            smartOrchestrator,
            monitor,
            preGenerationValidator,
            new ScriptValidator(),
            new ProviderRetryWrapper(_loggerFactory.CreateLogger<ProviderRetryWrapper>()),
            new TtsOutputValidator(_loggerFactory.CreateLogger<TtsOutputValidator>()),
            new ImageOutputValidator(_loggerFactory.CreateLogger<ImageOutputValidator>()),
            new LlmOutputValidator(_loggerFactory.CreateLogger<LlmOutputValidator>()),
            new ResourceCleanupManager(_loggerFactory.CreateLogger<ResourceCleanupManager>()),
            new TimelineBuilder(),
            new ProviderSettings(_loggerFactory.CreateLogger<ProviderSettings>()),
            new RunTelemetryCollector(_loggerFactory.CreateLogger<RunTelemetryCollector>(), System.IO.Path.GetTempPath()),
            imageProvider: imageProvider ?? new MockImageProvider(),
            ffmpegResolver: ffmpegResolver ?? defaultFFmpegResolver
        );
    }

    private static ProviderReadinessService CreateReadyProviderReadinessService()
    {
        return new ProviderReadinessService(new Dictionary<string, ProviderStatus>
        {
            { "llm", new ProviderStatus { Available = true, Message = "Ready" } },
            { "tts", new ProviderStatus { Available = true, Message = "Ready" } }
        });
    }

    private class MockTtsProviderNoVoices : ITtsProvider
    {
        public Task<List<VoiceInfo>> GetAvailableVoicesAsync() => Task.FromResult(new List<VoiceInfo>());
        public Task<string> SynthesizeAsync(IReadOnlyList<ScriptLine> lines, VoiceSpec spec, CancellationToken ct) => 
            throw new NotImplementedException();
    }

    private class MockFFmpegResolverNotFound : FFmpegResolver
    {
        public MockFFmpegResolverNotFound() 
            : base(LoggerFactory.Create(b => b.AddConsole()).CreateLogger<FFmpegResolver>(), 
                   new MemoryCache(new MemoryCacheOptions()))
        {
        }

        public new async Task<FfmpegResolutionResult> ResolveAsync(
            string? configuredPath = null, 
            bool forceRefresh = false, 
            CancellationToken ct = default)
        {
            await Task.CompletedTask;
            return new FfmpegResolutionResult
            {
                Found = false,
                IsValid = false,
                Error = "FFmpeg not found in any location",
                AttemptedPaths = new List<string> { "/usr/bin/ffmpeg", "/usr/local/bin/ffmpeg" }
            };
        }
    }
}
