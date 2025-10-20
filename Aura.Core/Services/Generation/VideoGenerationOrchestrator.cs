using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Microsoft.Extensions.Logging;
using TaskStatus = Aura.Core.Models.Generation.TaskStatus;

namespace Aura.Core.Services.Generation;

/// <summary>
/// Central coordinator for AI-generated video components.
/// Manages dependencies, optimizes task scheduling, and ensures efficient resource utilization.
/// </summary>
public class VideoGenerationOrchestrator
{
    private readonly ILogger<VideoGenerationOrchestrator> _logger;
    private readonly ResourceMonitor _resourceMonitor;
    private readonly StrategySelector _strategySelector;
    private readonly ConcurrentDictionary<string, TaskResult> _taskResults = new();
    private readonly SemaphoreSlim _concurrencyLimiter;

    public VideoGenerationOrchestrator(
        ILogger<VideoGenerationOrchestrator> logger,
        ResourceMonitor resourceMonitor,
        StrategySelector strategySelector)
    {
        _logger = logger;
        _resourceMonitor = resourceMonitor;
        _strategySelector = strategySelector;
        _concurrencyLimiter = new SemaphoreSlim(4, 4); // Default concurrency
    }

    /// <summary>
    /// Orchestrates the generation of video components with intelligent scheduling
    /// </summary>
    public async Task<OrchestrationResult> OrchestrateGenerationAsync(
        Brief brief,
        PlanSpec planSpec,
        SystemProfile systemProfile,
        Func<GenerationNode, CancellationToken, Task<object>> taskExecutor,
        IProgress<OrchestrationProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting video generation orchestration for topic: {Topic}", brief.Topic);

            // Select generation strategy
            var strategy = _strategySelector.SelectStrategy(brief, systemProfile, planSpec);
            UpdateConcurrencyLimit(strategy.MaxConcurrency);

            progress?.Report(new OrchestrationProgress("Analyzing dependencies", 0, 0, TimeSpan.Zero));

            // Build dependency graph
            var graph = BuildDependencyGraph(brief, planSpec, strategy);
            _logger.LogInformation("Built dependency graph with {NodeCount} tasks", graph.NodeCount);

            // Get optimal execution batches
            var batches = graph.GetOptimalExecutionBatches();
            _logger.LogInformation("Organized tasks into {BatchCount} execution batches", batches.Count);

            // Execute batches
            int totalTasks = batches.Sum(b => b.Count);
            int completedTasks = 0;
            int failedTasks = 0;

            foreach (var batch in batches)
            {
                _logger.LogInformation("Executing batch with {TaskCount} tasks", batch.Count);
                progress?.Report(new OrchestrationProgress(
                    $"Processing batch ({completedTasks}/{totalTasks} tasks completed)",
                    completedTasks,
                    totalTasks,
                    stopwatch.Elapsed));

                var batchResults = await ExecuteBatchAsync(
                    batch,
                    strategy,
                    taskExecutor,
                    ct).ConfigureAwait(false);

                completedTasks += batchResults.Count(r => r.Succeeded);
                failedTasks += batchResults.Count(r => !r.Succeeded);

                // Check for critical failures
                if (HasCriticalFailures(batchResults, graph))
                {
                    _logger.LogError("Critical task failures detected, attempting recovery");

                    if (!await AttemptRecoveryAsync(batchResults, graph, strategy, taskExecutor, ct).ConfigureAwait(false))
                    {
                        _logger.LogError("Recovery failed, aborting orchestration");
                        throw new OrchestrationException("Critical task failures could not be recovered");
                    }

                    completedTasks++;
                }
            }

            stopwatch.Stop();

            var result = new OrchestrationResult(
                Succeeded: failedTasks == 0,
                TotalTasks: totalTasks,
                CompletedTasks: completedTasks,
                FailedTasks: failedTasks,
                ExecutionTime: stopwatch.Elapsed,
                Strategy: strategy);

            // Record strategy performance
            _strategySelector.RecordStrategyPerformance(
                strategy,
                stopwatch.Elapsed,
                result.Succeeded,
                result.QualityScore);

            _logger.LogInformation(
                "Orchestration completed: {Status}, {Completed}/{Total} tasks, Time: {Time}",
                result.Succeeded ? "Success" : "Failed",
                completedTasks,
                totalTasks,
                stopwatch.Elapsed);

            progress?.Report(new OrchestrationProgress(
                "Orchestration completed",
                completedTasks,
                totalTasks,
                stopwatch.Elapsed));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Orchestration failed with exception");
            throw;
        }
    }

    /// <summary>
    /// Builds the dependency graph for video generation tasks
    /// </summary>
    private AssetDependencyGraph BuildDependencyGraph(Brief brief, PlanSpec planSpec, GenerationStrategy strategy)
    {
        var graph = new AssetDependencyGraph();

        // Estimate number of scenes
        int estimatedScenes = EstimateSceneCount(planSpec.TargetDuration);

        // Add script generation task (highest priority, must complete first)
        graph.AddNode("script", GenerationTaskType.ScriptGeneration, priority: 100, estimatedResourceCost: 0.3);

        // Add audio generation task (depends on script)
        graph.AddNode("audio", GenerationTaskType.AudioGeneration, priority: 90, estimatedResourceCost: 0.5);
        graph.AddDependency("script", "audio");

        // Add visual generation tasks (can run in parallel, depend on script)
        for (int i = 0; i < estimatedScenes; i++)
        {
            string visualTaskId = $"visual_{i}";
            graph.AddNode(visualTaskId, GenerationTaskType.ImageGeneration, priority: 50, estimatedResourceCost: 0.4);
            graph.AddDependency("script", visualTaskId);
        }

        // Add caption generation task (depends on audio)
        if (planSpec.Style.Contains("caption", StringComparison.OrdinalIgnoreCase))
        {
            graph.AddNode("captions", GenerationTaskType.CaptionGeneration, priority: 40, estimatedResourceCost: 0.2);
            graph.AddDependency("audio", "captions");
        }

        // Add video composition task (depends on all assets)
        graph.AddNode("composition", GenerationTaskType.VideoComposition, priority: 10, estimatedResourceCost: 0.8);
        graph.AddDependency("audio", "composition");

        for (int i = 0; i < estimatedScenes; i++)
        {
            graph.AddDependency($"visual_{i}", "composition");
        }

        if (graph.ContainsTask("captions"))
        {
            graph.AddDependency("captions", "composition");
        }

        return graph;
    }

    /// <summary>
    /// Executes a batch of tasks in parallel with resource management
    /// </summary>
    private async Task<List<TaskResult>> ExecuteBatchAsync(
        List<GenerationNode> batch,
        GenerationStrategy strategy,
        Func<GenerationNode, CancellationToken, Task<object>> taskExecutor,
        CancellationToken ct)
    {
        var results = new ConcurrentBag<TaskResult>();

        var tasks = batch.Select(async node =>
        {
            try
            {
                // Wait for resources if needed
                await _resourceMonitor.WaitForResourcesAsync(node.EstimatedResourceCost, ct).ConfigureAwait(false);

                // Acquire concurrency slot
                await _concurrencyLimiter.WaitAsync(ct).ConfigureAwait(false);

                try
                {
                    node.Status = TaskStatus.Running;
                    node.StartedAt = DateTime.UtcNow;

                    _logger.LogDebug("Executing task: {TaskId} ({TaskType})", node.TaskId, node.TaskType);

                    var result = await taskExecutor(node, ct).ConfigureAwait(false);

                    node.Status = TaskStatus.Completed;
                    node.Result = result;
                    node.CompletedAt = DateTime.UtcNow;

                    var taskResult = new TaskResult(node.TaskId, true, result, null);
                    _taskResults[node.TaskId] = taskResult;
                    results.Add(taskResult);

                    _logger.LogDebug("Task completed: {TaskId}", node.TaskId);
                }
                finally
                {
                    _concurrencyLimiter.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Task failed: {TaskId}", node.TaskId);

                node.Status = TaskStatus.Failed;
                node.ErrorMessage = ex.Message;
                node.CompletedAt = DateTime.UtcNow;

                var taskResult = new TaskResult(node.TaskId, false, null, ex.Message);
                _taskResults[node.TaskId] = taskResult;
                results.Add(taskResult);
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);

        return results.ToList();
    }

    /// <summary>
    /// Checks if any critical tasks have failed
    /// </summary>
    private bool HasCriticalFailures(List<TaskResult> results, AssetDependencyGraph graph)
    {
        // Script and audio are critical
        var criticalTasks = new[] { "script", "audio", "composition" };

        return results.Any(r => !r.Succeeded && criticalTasks.Contains(r.TaskId));
    }

    /// <summary>
    /// Attempts to recover from task failures using fallback strategies
    /// </summary>
    private async Task<bool> AttemptRecoveryAsync(
        List<TaskResult> failedResults,
        AssetDependencyGraph graph,
        GenerationStrategy strategy,
        Func<GenerationNode, CancellationToken, Task<object>> taskExecutor,
        CancellationToken ct)
    {
        _logger.LogInformation("Attempting recovery from {Count} failed tasks", failedResults.Count(r => !r.Succeeded));

        // For now, we'll implement a simple retry mechanism
        // In a full implementation, this would include:
        // - Fallback to alternative providers
        // - Partial result caching
        // - Quality degradation strategies

        var failedTasks = failedResults.Where(r => !r.Succeeded).ToList();

        foreach (var failed in failedTasks)
        {
            var node = graph.GetNode(failed.TaskId);
            if (node == null) continue;

            try
            {
                _logger.LogInformation("Retrying task: {TaskId}", failed.TaskId);

                // Reset node state
                node.Status = TaskStatus.Pending;
                node.ErrorMessage = null;

                // Retry with reduced resource requirements
                var result = await taskExecutor(node, ct).ConfigureAwait(false);

                node.Status = TaskStatus.Completed;
                node.Result = result;

                _taskResults[node.TaskId] = new TaskResult(node.TaskId, true, result, null);

                _logger.LogInformation("Recovery successful for task: {TaskId}", failed.TaskId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Recovery failed for task: {TaskId}", failed.TaskId);
            }
        }

        return false;
    }

    /// <summary>
    /// Estimates the number of scenes based on target duration
    /// </summary>
    private int EstimateSceneCount(TimeSpan targetDuration)
    {
        // Rough estimate: 1 scene per 20-30 seconds
        int sceneCount = (int)Math.Ceiling(targetDuration.TotalSeconds / 25);
        return Math.Max(1, Math.Min(sceneCount, 20)); // Cap between 1-20 scenes
    }

    /// <summary>
    /// Updates the concurrency limiter based on strategy
    /// </summary>
    private void UpdateConcurrencyLimit(int maxConcurrency)
    {
        _logger.LogInformation("Setting max concurrency to {Concurrency}", maxConcurrency);
        // Note: SemaphoreSlim doesn't support dynamic resizing, so this is a simplified version
        // In production, we'd need to recreate the semaphore or use a different synchronization mechanism
    }
}

/// <summary>
/// Progress information for orchestration
/// </summary>
public record OrchestrationProgress(
    string CurrentStage,
    int CompletedTasks,
    int TotalTasks,
    TimeSpan ElapsedTime)
{
    public double ProgressPercentage => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
}

/// <summary>
/// Result of an orchestration run
/// </summary>
public record OrchestrationResult(
    bool Succeeded,
    int TotalTasks,
    int CompletedTasks,
    int FailedTasks,
    TimeSpan ExecutionTime,
    GenerationStrategy Strategy)
{
    public double QualityScore => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks : 0;
}

/// <summary>
/// Result of a single task execution
/// </summary>
public record TaskResult(
    string TaskId,
    bool Succeeded,
    object? Result,
    string? ErrorMessage);

/// <summary>
/// Exception thrown when orchestration fails critically
/// </summary>
public class OrchestrationException : Exception
{
    public OrchestrationException(string message) : base(message)
    {
    }

    public OrchestrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
