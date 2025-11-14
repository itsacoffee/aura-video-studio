using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for end-to-end video generation workflow
/// Tests the complete pipeline from brief to rendered video
/// </summary>
public class VideoGenerationIntegrationTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;

    public VideoGenerationIntegrationTests()
    {
        _fixture = new TestFixture();
    }

    public Task InitializeAsync() => _fixture.InitializeAsync();
    public Task DisposeAsync() => _fixture.DisposeAsync();

    [Fact]
    public async Task GenerateVideo_WithValidBrief_CompletesSuccessfully()
    {
        // Arrange
        var brief = new Brief
        {
            Topic = "Introduction to AI",
            Audience = "General public",
            Goal = "Educate viewers about artificial intelligence"
        };

        var planSpec = new PlanSpec
        {
            TargetDuration = TimeSpan.FromSeconds(30),
            Style = "educational"
        };

        var voiceSpec = new VoiceSpec
        {
            VoiceName = "default",
            Speed = 1.0
        };

        var renderSpec = new RenderSpec
        {
            Res = new VideoResolution(1280, 720),
            Fps = 30,
            Codec = "h264"
        };

        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = Environment.ProcessorCount,
            PhysicalCores = Math.Max(1, Environment.ProcessorCount / 2)
        };

        var progressEvents = new System.Collections.Generic.List<string>();
        var progress = new Progress<string>(msg => progressEvents.Add(msg));

        // Act
        var outputPath = await _fixture.VideoOrchestrator.GenerateVideoAsync(
            brief,
            planSpec,
            voiceSpec,
            renderSpec,
            systemProfile,
            progress,
            CancellationToken.None
        );

        // Assert
        Assert.NotNull(outputPath);
        Assert.True(System.IO.File.Exists(outputPath), "Output video file should exist");
        Assert.NotEmpty(progressEvents);
        Assert.Contains(progressEvents, p => p.Contains("complete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GenerateVideo_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var brief = new Brief
        {
            Topic = "Long video that will be cancelled",
            Audience = "Test",
            Goal = "Test cancellation"
        };

        var planSpec = new PlanSpec
        {
            TargetDuration = TimeSpan.FromMinutes(5), // Long duration
            Style = "test"
        };

        var voiceSpec = new VoiceSpec { VoiceName = "default", Speed = 1.0 };
        var renderSpec = new RenderSpec
        {
            Res = new VideoResolution(1280, 720),
            Fps = 30,
            Codec = "h264"
        };

        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = Environment.ProcessorCount
        };

        var cts = new CancellationTokenSource();

        // Cancel after 2 seconds
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _fixture.VideoOrchestrator.GenerateVideoAsync(
                brief,
                planSpec,
                voiceSpec,
                renderSpec,
                systemProfile,
                null,
                cts.Token
            );
        });
    }

    [Fact]
    public async Task GenerateVideo_WithInvalidBrief_ThrowsValidationException()
    {
        // Arrange
        var brief = new Brief
        {
            Topic = "", // Invalid: empty topic
            Audience = "Test",
            Goal = "Test"
        };

        var planSpec = new PlanSpec
        {
            TargetDuration = TimeSpan.FromSeconds(30),
            Style = "test"
        };

        var voiceSpec = new VoiceSpec { VoiceName = "default", Speed = 1.0 };
        var renderSpec = new RenderSpec
        {
            Res = new VideoResolution(1280, 720),
            Fps = 30,
            Codec = "h264"
        };

        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = Environment.ProcessorCount
        };

        // Act & Assert
        await Assert.ThrowsAsync<Core.Errors.ValidationException>(async () =>
        {
            await _fixture.VideoOrchestrator.GenerateVideoAsync(
                brief,
                planSpec,
                voiceSpec,
                renderSpec,
                systemProfile,
                null,
                CancellationToken.None
            );
        });
    }

    [Fact]
    public async Task ProviderRetryWrapper_RetriesTransientFailures()
    {
        // Arrange
        var retryWrapper = _fixture.ProviderRetryWrapper;
        int attemptCount = 0;

        // Act
        var result = await retryWrapper.ExecuteWithRetryAsync(
            async (ct) =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new HttpRequestException("Simulated transient error");
                }
                return "Success";
            },
            "Test Operation",
            CancellationToken.None,
            maxRetries: 3
        );

        // Assert
        Assert.Equal("Success", result);
        Assert.Equal(3, attemptCount);
    }

    [Fact]
    public void CircuitBreaker_OpensAfterFailureThreshold()
    {
        // Arrange
        var circuitBreaker = _fixture.CircuitBreakerService;
        var providerName = "TestProvider";

        // Act - Record failures to trip circuit breaker
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.RecordFailure(providerName);
        }

        var canExecute = circuitBreaker.CanExecute(providerName);
        var state = circuitBreaker.GetState(providerName);

        // Assert
        Assert.False(canExecute);
        Assert.Equal(Core.Services.Providers.CircuitState.Open, state);
    }
}

/// <summary>
/// Test fixture for integration tests
/// Sets up necessary services and dependencies
/// </summary>
public class TestFixture : IAsyncDisposable
{
    public VideoOrchestrator VideoOrchestrator { get; private set; } = null!;
    public ProviderRetryWrapper ProviderRetryWrapper { get; private set; } = null!;
    public Core.Services.Providers.ProviderCircuitBreakerService CircuitBreakerService { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Initialize test services
        // In a real implementation, this would set up proper DI container
        // For now, this is a placeholder
        
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        ProviderRetryWrapper = new ProviderRetryWrapper(
            loggerFactory.CreateLogger<ProviderRetryWrapper>()
        );

        CircuitBreakerService = new Core.Services.Providers.ProviderCircuitBreakerService(
            loggerFactory.CreateLogger<Core.Services.Providers.ProviderCircuitBreakerService>()
        );

        // TODO: Initialize VideoOrchestrator with test dependencies
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        // Cleanup
        await Task.CompletedTask;
    }
}
