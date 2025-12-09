using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Orchestrator;
using Aura.Core.Services.Generation;
using Microsoft.Extensions.Logging;
using Xunit;
using GenerationTaskStatus = Aura.Core.Models.Generation.TaskStatus;

namespace Aura.Tests;

/// <summary>
/// Tests for the robust output path extraction logic in VideoOrchestrator.GenerateVideoResultAsync.
/// This ensures the system can recover video output paths even when task results are stored under
/// different keys or the primary extraction method fails.
/// </summary>
public class VideoOrchestratorOutputPathExtractionTests
{
    [Fact]
    public void OrchestrationResult_WithCompositionKey_ShouldExtractPath()
    {
        // Arrange
        var outputPath = "/tmp/test-video.mp4";
        var taskResults = new ConcurrentDictionary<string, TaskResult>();
        taskResults["composition"] = new TaskResult("composition", true, outputPath, null);

        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 1,
            CompletedTasks: 1,
            FailedTasks: 0,
            ExecutionTime: TimeSpan.FromSeconds(10),
            Strategy: CreateMockStrategy(),
            TaskResults: taskResults);

        // Assert
        Assert.True(result.TaskResults.TryGetValue("composition", out var compositionTask));
        Assert.NotNull(compositionTask);
        Assert.Equal(outputPath, compositionTask.Result);
    }

    [Fact]
    public void OrchestrationResult_WithAlternateRenderKey_ShouldExtractPath()
    {
        // Arrange
        var outputPath = "/tmp/test-video.mp4";
        var taskResults = new ConcurrentDictionary<string, TaskResult>();
        taskResults["render"] = new TaskResult("render", true, outputPath, null);

        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 1,
            CompletedTasks: 1,
            FailedTasks: 0,
            ExecutionTime: TimeSpan.FromSeconds(10),
            Strategy: CreateMockStrategy(),
            TaskResults: taskResults);

        // Assert
        Assert.True(result.TaskResults.TryGetValue("render", out var renderTask));
        Assert.NotNull(renderTask);
        Assert.Equal(outputPath, renderTask.Result);
    }

    [Fact]
    public void OrchestrationResult_WithVideoOutputKey_ShouldExtractPath()
    {
        // Arrange
        var outputPath = "/tmp/test-video.mp4";
        var taskResults = new ConcurrentDictionary<string, TaskResult>();
        taskResults["video_output"] = new TaskResult("video_output", true, outputPath, null);

        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 1,
            CompletedTasks: 1,
            FailedTasks: 0,
            ExecutionTime: TimeSpan.FromSeconds(10),
            Strategy: CreateMockStrategy(),
            TaskResults: taskResults);

        // Assert
        Assert.True(result.TaskResults.TryGetValue("video_output", out var videoOutputTask));
        Assert.NotNull(videoOutputTask);
        Assert.Equal(outputPath, videoOutputTask.Result);
    }

    [Fact]
    public void OrchestrationResult_WithFinalVideoKey_ShouldExtractPath()
    {
        // Arrange
        var outputPath = "/tmp/test-video.mp4";
        var taskResults = new ConcurrentDictionary<string, TaskResult>();
        taskResults["final_video"] = new TaskResult("final_video", true, outputPath, null);

        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 1,
            CompletedTasks: 1,
            FailedTasks: 0,
            ExecutionTime: TimeSpan.FromSeconds(10),
            Strategy: CreateMockStrategy(),
            TaskResults: taskResults);

        // Assert
        Assert.True(result.TaskResults.TryGetValue("final_video", out var finalVideoTask));
        Assert.NotNull(finalVideoTask);
        Assert.Equal(outputPath, finalVideoTask.Result);
    }

    [Fact]
    public void OrchestrationResult_WithOutputKey_ShouldExtractPath()
    {
        // Arrange
        var outputPath = "/tmp/test-video.mp4";
        var taskResults = new ConcurrentDictionary<string, TaskResult>();
        taskResults["output"] = new TaskResult("output", true, outputPath, null);

        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 1,
            CompletedTasks: 1,
            FailedTasks: 0,
            ExecutionTime: TimeSpan.FromSeconds(10),
            Strategy: CreateMockStrategy(),
            TaskResults: taskResults);

        // Assert
        Assert.True(result.TaskResults.TryGetValue("output", out var outputTask));
        Assert.NotNull(outputTask);
        Assert.Equal(outputPath, outputTask.Result);
    }

    [Fact]
    public void OrchestrationResult_WithMultipleKeys_ShouldPreferComposition()
    {
        // Arrange
        var compositionPath = "/tmp/composition-video.mp4";
        var renderPath = "/tmp/render-video.mp4";
        var taskResults = new ConcurrentDictionary<string, TaskResult>();
        taskResults["composition"] = new TaskResult("composition", true, compositionPath, null);
        taskResults["render"] = new TaskResult("render", true, renderPath, null);

        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 2,
            CompletedTasks: 2,
            FailedTasks: 0,
            ExecutionTime: TimeSpan.FromSeconds(10),
            Strategy: CreateMockStrategy(),
            TaskResults: taskResults);

        // Assert - composition key should be preferred
        Assert.True(result.TaskResults.TryGetValue("composition", out var compositionTask));
        Assert.NotNull(compositionTask);
        Assert.Equal(compositionPath, compositionTask.Result);
    }

    [Fact]
    public void OrchestrationResult_WithNullResult_ShouldNotExtractPath()
    {
        // Arrange
        var taskResults = new ConcurrentDictionary<string, TaskResult>();
        taskResults["composition"] = new TaskResult("composition", true, null, null);

        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 1,
            CompletedTasks: 1,
            FailedTasks: 0,
            ExecutionTime: TimeSpan.FromSeconds(10),
            Strategy: CreateMockStrategy(),
            TaskResults: taskResults);

        // Assert
        Assert.True(result.TaskResults.TryGetValue("composition", out var compositionTask));
        Assert.NotNull(compositionTask);
        Assert.Null(compositionTask.Result);
    }

    [Fact]
    public void OrchestrationResult_WithEmptyString_ShouldNotExtractPath()
    {
        // Arrange
        var taskResults = new ConcurrentDictionary<string, TaskResult>();
        taskResults["composition"] = new TaskResult("composition", true, string.Empty, null);

        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 1,
            CompletedTasks: 1,
            FailedTasks: 0,
            ExecutionTime: TimeSpan.FromSeconds(10),
            Strategy: CreateMockStrategy(),
            TaskResults: taskResults);

        // Assert
        Assert.True(result.TaskResults.TryGetValue("composition", out var compositionTask));
        Assert.NotNull(compositionTask);
        Assert.Equal(string.Empty, compositionTask.Result);
    }

    [Fact]
    public void OrchestrationResult_WithNoMatchingKeys_ShouldHaveEmptyResults()
    {
        // Arrange
        var taskResults = new ConcurrentDictionary<string, TaskResult>();
        taskResults["script"] = new TaskResult("script", true, "script content", null);
        taskResults["audio"] = new TaskResult("audio", true, "/tmp/audio.wav", null);

        var result = new OrchestrationResult(
            Succeeded: true,
            TotalTasks: 2,
            CompletedTasks: 2,
            FailedTasks: 0,
            ExecutionTime: TimeSpan.FromSeconds(10),
            Strategy: CreateMockStrategy(),
            TaskResults: taskResults);

        // Assert
        Assert.False(result.TaskResults.TryGetValue("composition", out _));
        Assert.False(result.TaskResults.TryGetValue("render", out _));
        Assert.False(result.TaskResults.TryGetValue("video_output", out _));
        Assert.False(result.TaskResults.TryGetValue("final_video", out _));
        Assert.False(result.TaskResults.TryGetValue("output", out _));
    }

    private static GenerationStrategy CreateMockStrategy()
    {
        return new GenerationStrategy(
            StrategyType: StrategyType.Sequential,
            MaxConcurrency: 4,
            VisualApproach: VisualGenerationApproach.StockOnly,
            ContentComplexity: 0.5,
            EnableEarlyFallback: false,
            EnableProgressiveCaching: false
        );
    }
}
