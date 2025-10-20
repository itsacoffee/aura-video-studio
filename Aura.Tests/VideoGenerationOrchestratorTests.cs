using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Services.Generation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class VideoGenerationOrchestratorTests
{
    private readonly ILogger<VideoGenerationOrchestrator> _orchestratorLogger;
    private readonly ILogger<ResourceMonitor> _monitorLogger;
    private readonly ILogger<StrategySelector> _selectorLogger;

    public VideoGenerationOrchestratorTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _orchestratorLogger = loggerFactory.CreateLogger<VideoGenerationOrchestrator>();
        _monitorLogger = loggerFactory.CreateLogger<ResourceMonitor>();
        _selectorLogger = loggerFactory.CreateLogger<StrategySelector>();
    }

    [Fact]
    public async Task OrchestrateGenerationAsync_WithSimpleBrief_ShouldComplete()
    {
        // Arrange
        var monitor = new ResourceMonitor(_monitorLogger);
        var selector = new StrategySelector(_selectorLogger);
        var orchestrator = new VideoGenerationOrchestrator(_orchestratorLogger, monitor, selector);

        var brief = new Brief("Test Video", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(2), Pacing.Conversational, Density.Balanced, "Modern");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = 4,
            PhysicalCores = 2,
            RamGB = 8,
            OfflineOnly = false
        };

        int executedTasks = 0;
        Func<GenerationNode, CancellationToken, Task<object>> mockExecutor = async (node, ct) =>
        {
            executedTasks++;
            await Task.Delay(10, ct).ConfigureAwait(false);
            return $"Result_{node.TaskId}";
        };

        // Act
        var result = await orchestrator.OrchestrateGenerationAsync(
            brief,
            planSpec,
            systemProfile,
            mockExecutor,
            null,
            CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalTasks > 0);
        Assert.True(executedTasks > 0);
        Assert.True(result.ExecutionTime > TimeSpan.Zero);
    }

    [Fact]
    public async Task OrchestrateGenerationAsync_ShouldReportProgress()
    {
        // Arrange
        var monitor = new ResourceMonitor(_monitorLogger);
        var selector = new StrategySelector(_selectorLogger);
        var orchestrator = new VideoGenerationOrchestrator(_orchestratorLogger, monitor, selector);

        var brief = new Brief("Test Video", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "Modern");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = 4,
            PhysicalCores = 2,
            RamGB = 8,
            OfflineOnly = false
        };

        int progressReports = 0;
        var progress = new Progress<OrchestrationProgress>(p => progressReports++);

        Func<GenerationNode, CancellationToken, Task<object>> mockExecutor = async (node, ct) =>
        {
            await Task.Delay(5, ct).ConfigureAwait(false);
            return $"Result_{node.TaskId}";
        };

        // Act
        await orchestrator.OrchestrateGenerationAsync(
            brief,
            planSpec,
            systemProfile,
            mockExecutor,
            progress,
            CancellationToken.None).ConfigureAwait(false);

        // Assert
        Assert.True(progressReports > 0);
    }

    [Fact]
    public async Task OrchestrateGenerationAsync_WithCancellation_ShouldThrow()
    {
        // Arrange
        var monitor = new ResourceMonitor(_monitorLogger);
        var selector = new StrategySelector(_selectorLogger);
        var orchestrator = new VideoGenerationOrchestrator(_orchestratorLogger, monitor, selector);

        var brief = new Brief("Test Video", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "Modern");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = 4,
            PhysicalCores = 2,
            RamGB = 8,
            OfflineOnly = false
        };

        var cts = new CancellationTokenSource();

        Func<GenerationNode, CancellationToken, Task<object>> mockExecutor = async (node, ct) =>
        {
            // Cancel after first task starts
            cts.Cancel();
            await Task.Delay(10, ct).ConfigureAwait(false);
            return $"Result_{node.TaskId}";
        };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await orchestrator.OrchestrateGenerationAsync(
                brief,
                planSpec,
                systemProfile,
                mockExecutor,
                null,
                cts.Token).ConfigureAwait(false));
    }

    [Fact]
    public async Task OrchestrateGenerationAsync_WithFailingTask_ShouldHandleFailure()
    {
        // Arrange
        var monitor = new ResourceMonitor(_monitorLogger);
        var selector = new StrategySelector(_selectorLogger);
        var orchestrator = new VideoGenerationOrchestrator(_orchestratorLogger, monitor, selector);

        var brief = new Brief("Test Video", null, null, "Professional", "English", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "Modern");
        var systemProfile = new SystemProfile
        {
            Tier = HardwareTier.B,
            LogicalCores = 4,
            PhysicalCores = 2,
            RamGB = 8,
            OfflineOnly = false
        };

        int attemptCount = 0;
        Func<GenerationNode, CancellationToken, Task<object>> mockExecutor = async (node, ct) =>
        {
            attemptCount++;
            await Task.Delay(5, ct).ConfigureAwait(false);
            
            // Fail critical task permanently to trigger recovery attempt
            if (node.TaskId == "script")
            {
                throw new InvalidOperationException("Simulated script generation failure");
            }
            
            return $"Result_{node.TaskId}";
        };

        // Act & Assert
        // The orchestrator should attempt recovery but ultimately fail
        var exception = await Assert.ThrowsAsync<OrchestrationException>(async () =>
            await orchestrator.OrchestrateGenerationAsync(
                brief,
                planSpec,
                systemProfile,
                mockExecutor,
                null,
                CancellationToken.None).ConfigureAwait(false));

        // Should have attempted the script task multiple times (initial + retry)
        Assert.True(attemptCount >= 2);
        Assert.Contains("Critical task failures", exception.Message);
    }

    [Fact]
    public void OrchestrationProgress_ShouldCalculatePercentage()
    {
        // Arrange & Act
        var progress = new OrchestrationProgress("Test stage", 5, 10, TimeSpan.FromSeconds(30));

        // Assert
        Assert.Equal("Test stage", progress.CurrentStage);
        Assert.Equal(5, progress.CompletedTasks);
        Assert.Equal(10, progress.TotalTasks);
        Assert.Equal(50.0, progress.ProgressPercentage);
    }

    [Fact]
    public void OrchestrationResult_ShouldCalculateQualityScore()
    {
        // Arrange
        var strategy = new GenerationStrategy(
            StrategyType.Parallel,
            4,
            VisualGenerationApproach.HybridStockFirst,
            0.5,
            false,
            true);

        var taskResults = new Dictionary<string, TaskResult>
        {
            ["task1"] = new TaskResult("task1", true, "result1", null),
            ["task2"] = new TaskResult("task2", false, null, "error"),
        };

        // Act
        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 10,
            CompletedTasks: 8,
            FailedTasks: 2,
            ExecutionTime: TimeSpan.FromMinutes(5),
            Strategy: strategy,
            TaskResults: taskResults);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(10, result.TotalTasks);
        Assert.Equal(8, result.CompletedTasks);
        Assert.Equal(2, result.FailedTasks);
        Assert.Equal(0.8, result.QualityScore);
    }

    [Fact]
    public void TaskResult_ShouldStoreTaskOutcome()
    {
        // Arrange & Act
        var successResult = new TaskResult("task1", true, "output.mp4", null);
        var failureResult = new TaskResult("task2", false, null, "Error occurred");

        // Assert
        Assert.Equal("task1", successResult.TaskId);
        Assert.True(successResult.Succeeded);
        Assert.Equal("output.mp4", successResult.Result);
        Assert.Null(successResult.ErrorMessage);

        Assert.Equal("task2", failureResult.TaskId);
        Assert.False(failureResult.Succeeded);
        Assert.Null(failureResult.Result);
        Assert.Equal("Error occurred", failureResult.ErrorMessage);
    }
}
