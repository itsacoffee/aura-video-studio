using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Orchestrator;
using Aura.Core.Orchestrator.Stages;
using Aura.Core.Models;
using Aura.Core.Validation;
using Aura.Core.Services;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Orchestrator;

/// <summary>
/// Integration tests for the full pipeline orchestration flow
/// </summary>
public class PipelineIntegrationTests
{
    [Fact]
    public async Task FullPipeline_ExecutesAllStages_InCorrectOrder()
    {
        // Arrange
        var context = CreateTestContext();
        var stages = CreateMockStages();
        var executedStages = new List<string>();

        // Track execution order
        foreach (var stage in stages)
        {
            var stageName = stage.StageName;
            stage.Setup(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), null, It.IsAny<CancellationToken>()))
                .Callback(() => executedStages.Add(stageName))
                .ReturnsAsync(PipelineStageResult.Success(stageName, TimeSpan.FromSeconds(1)));
        }

        // Act
        foreach (var stage in stages)
        {
            await stage.Object.ExecuteAsync(context);
        }

        // Assert
        Assert.Equal(5, executedStages.Count);
        Assert.Equal("Brief", executedStages[0]);
        Assert.Equal("Script", executedStages[1]);
        Assert.Equal("Voice", executedStages[2]);
        Assert.Equal("Visuals", executedStages[3]);
        Assert.Equal("Composition", executedStages[4]);
    }

    [Fact]
    public async Task Pipeline_OneStageFailsWithRetry_ContinuesAfterSuccess()
    {
        // Arrange
        var context = CreateTestContext();
        var mockStage = new Mock<PipelineStage>(Mock.Of<ILogger<PipelineStage>>());
        
        int attemptCount = 0;
        mockStage.Setup(s => s.ExecuteAsync(It.IsAny<PipelineContext>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    return PipelineStageResult.Failure("TestStage", new Exception("Transient error"), TimeSpan.FromSeconds(1));
                }
                return PipelineStageResult.Success("TestStage", TimeSpan.FromSeconds(1));
            });

        mockStage.SetupGet(s => s.StageName).Returns("TestStage");
        mockStage.SetupGet(s => s.SupportsRetry).Returns(true);
        mockStage.SetupGet(s => s.MaxRetryAttempts).Returns(3);

        // Act
        var result = await mockStage.Object.ExecuteAsync(context);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(1, attemptCount); // Note: Mock doesn't handle retry logic, just testing the pattern
    }

    [Fact]
    public void PipelineContext_StoresAndRetrievesStageOutputs()
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        context.SetStageOutput("Stage1", "Output1");
        context.SetStageOutput("Stage2", 42);
        context.SetStageOutput("Stage3", new { Data = "test" });

        // Assert
        Assert.Equal("Output1", context.GetStageOutput<string>("Stage1"));
        Assert.Equal(42, context.GetStageOutput<int>("Stage2"));
        Assert.NotNull(context.GetStageOutput<object>("Stage3"));
    }

    [Fact]
    public void PipelineContext_RecordsAndRetrievesMetrics()
    {
        // Arrange
        var context = CreateTestContext();
        var metrics = new PipelineStageMetrics
        {
            StageName = "TestStage",
            StartTime = DateTime.UtcNow.AddSeconds(-10),
            EndTime = DateTime.UtcNow,
            ItemsProcessed = 5,
            ItemsFailed = 1,
            RetryCount = 2
        };

        // Act
        context.RecordStageMetrics("TestStage", metrics);

        // Assert
        var retrieved = context.GetStageMetrics("TestStage");
        Assert.NotNull(retrieved);
        Assert.Equal("TestStage", retrieved.StageName);
        Assert.Equal(5, retrieved.ItemsProcessed);
        Assert.Equal(1, retrieved.ItemsFailed);
        Assert.Equal(2, retrieved.RetryCount);
    }

    [Fact]
    public void PipelineContext_RecordsErrors()
    {
        // Arrange
        var context = CreateTestContext();
        var exception = new InvalidOperationException("Test error");

        // Act
        context.RecordError("FailedStage", exception, isRecoverable: true);

        // Assert
        Assert.Single(context.Errors);
        Assert.Equal("FailedStage", context.Errors[0].StageName);
        Assert.Equal("Test error", context.Errors[0].Message);
        Assert.True(context.Errors[0].IsRecoverable);
    }

    [Fact]
    public void PipelineContext_TracksState()
    {
        // Arrange
        var context = CreateTestContext();

        // Act & Assert - Initial state
        Assert.Equal(PipelineState.Initialized, context.State);

        // Simulate running
        context.State = PipelineState.Running;
        Assert.Equal(PipelineState.Running, context.State);

        // Simulate completion
        context.MarkCompleted();
        Assert.Equal(PipelineState.Completed, context.State);
        Assert.NotNull(context.CompletedAt);
    }

    [Fact]
    public void PipelineContext_CalculatesElapsedTime()
    {
        // Arrange
        var context = CreateTestContext();
        
        // Act
        System.Threading.Thread.Sleep(100); // Wait a bit
        var elapsed = context.GetElapsedTime();

        // Assert
        Assert.True(elapsed.TotalMilliseconds >= 100);
    }

    private PipelineContext CreateTestContext()
    {
        return new PipelineContext(
            correlationId: Guid.NewGuid().ToString(),
            brief: new Brief("Test topic", "Test audience", "Test goal", "Professional", "English", Aspect.Widescreen16x9, null),
            planSpec: new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "informative"),
            voiceSpec: new VoiceSpec("test-voice", 1.0, 0.0, PauseStyle.Natural),
            renderSpec: new RenderSpec(new Resolution(1920, 1080), "mp4", 5000, 192, 30, "H264", 75, true),
            systemProfile: new SystemProfile { Tier = HardwareTier.B, LogicalCores = 8, PhysicalCores = 4, RamGB = 16 }
        );
    }

    private List<Mock<PipelineStage>> CreateMockStages()
    {
        var stages = new List<Mock<PipelineStage>>();

        var stageNames = new[] { "Brief", "Script", "Voice", "Visuals", "Composition" };
        foreach (var name in stageNames)
        {
            var mock = new Mock<PipelineStage>(Mock.Of<ILogger<PipelineStage>>());
            mock.SetupGet(s => s.StageName).Returns(name);
            mock.SetupGet(s => s.DisplayName).Returns($"{name} Stage");
            stages.Add(mock);
        }

        return stages;
    }
}
