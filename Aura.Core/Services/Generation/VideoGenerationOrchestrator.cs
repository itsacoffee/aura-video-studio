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
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(resourceMonitor);
        ArgumentNullException.ThrowIfNull(strategySelector);
        
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
        ArgumentNullException.ThrowIfNull(brief);
        ArgumentNullException.ThrowIfNull(planSpec);
        ArgumentNullException.ThrowIfNull(systemProfile);
        ArgumentNullException.ThrowIfNull(taskExecutor);
        
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting video generation orchestration for topic: {Topic}", brief.Topic);

            // Report initial progress immediately to show activity
            progress?.Report(new OrchestrationProgress("Starting orchestration", 0, 1, TimeSpan.Zero));

            // Select generation strategy
            var strategy = _strategySelector.SelectStrategy(brief, systemProfile, planSpec);
            UpdateConcurrencyLimit(strategy.MaxConcurrency);

            progress?.Report(new OrchestrationProgress("Analyzing dependencies and building task graph", 0, 1, stopwatch.Elapsed));

            // Build dependency graph
            var graph = BuildDependencyGraph(brief, planSpec, strategy);
            _logger.LogInformation("Built dependency graph with {NodeCount} tasks", graph.NodeCount);

            progress?.Report(new OrchestrationProgress($"Task graph ready with {graph.NodeCount} tasks", 0, graph.NodeCount, stopwatch.Elapsed));

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
                
                // CRITICAL: Validate all dependencies are in succeeded state before executing batch
                foreach (var node in batch)
                {
                    var dependencies = graph.GetDependencies(node.TaskId);
                    _logger.LogDebug("Task {TaskId} has {DependencyCount} dependencies: [{Dependencies}]", 
                        node.TaskId, dependencies.Count, string.Join(", ", dependencies));
                    
                    foreach (var depTaskId in dependencies)
                    {
                        var depNode = graph.GetNode(depTaskId);
                        if (depNode == null)
                        {
                            var error = $"Dependency validation failed: Task '{node.TaskId}' depends on '{depTaskId}' which was not found in graph";
                            _logger.LogError(error);
                            throw new OrchestrationException(error);
                        }
                        
                        if (depNode.Status != TaskStatus.Completed)
                        {
                            var error = $"Dependency validation failed: Task '{node.TaskId}' depends on '{depTaskId}' which has status '{depNode.Status}' (expected: Completed). Task execution order violation detected.";
                            _logger.LogError(error);
                            throw new OrchestrationException(error);
                        }
                        
                        _logger.LogDebug("Dependency validation passed: Task {TaskId} <- {DependencyTaskId} (Status: {Status})", 
                            node.TaskId, depTaskId, depNode.Status);
                    }
                }
                
                progress?.Report(new OrchestrationProgress(
                    $"Processing batch ({completedTasks}/{totalTasks} tasks completed)",
                    completedTasks,
                    totalTasks,
                    stopwatch.Elapsed));

                var batchResults = await ExecuteBatchAsync(
                    batch,
                    strategy,
                    taskExecutor,
                    progress,
                    completedTasks,
                    totalTasks,
                    stopwatch,
                    ct).ConfigureAwait(false);

                completedTasks += batchResults.Count(r => r.Succeeded);
                failedTasks += batchResults.Count(r => !r.Succeeded);

                // Report progress after batch completion
                progress?.Report(new OrchestrationProgress(
                    $"Batch completed ({completedTasks}/{totalTasks} tasks done)",
                    completedTasks,
                    totalTasks,
                    stopwatch.Elapsed));

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
                Strategy: strategy,
                TaskResults: _taskResults);

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
        IProgress<OrchestrationProgress>? progress,
        int baseCompletedTasks,
        int totalTasks,
        Stopwatch stopwatch,
        CancellationToken ct)
    {
        var results = new ConcurrentBag<TaskResult>();
        var batchCompletedCount = 0;

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

                    // Report task starting
                    var stageName = GetTaskStageName(node.TaskType);
                    progress?.Report(new OrchestrationProgress(
                        $"Executing: {stageName}",
                        baseCompletedTasks + batchCompletedCount,
                        totalTasks,
                        stopwatch.Elapsed));

                    _logger.LogInformation("Executing task: {TaskId} ({TaskType}) - Priority: {Priority}, ResourceCost: {ResourceCost}", 
                        node.TaskId, node.TaskType, node.Priority, node.EstimatedResourceCost);

                    var result = await taskExecutor(node, ct).ConfigureAwait(false);

                    node.Status = TaskStatus.Completed;
                    node.Result = result;
                    node.CompletedAt = DateTime.UtcNow;

                    var taskResult = new TaskResult(node.TaskId, true, result, null);
                    _taskResults[node.TaskId] = taskResult;
                    results.Add(taskResult);

                    // Increment completed count and report progress
                    Interlocked.Increment(ref batchCompletedCount);
                    progress?.Report(new OrchestrationProgress(
                        $"Completed: {stageName}",
                        baseCompletedTasks + batchCompletedCount,
                        totalTasks,
                        stopwatch.Elapsed));

                    _logger.LogInformation("Task completed successfully: {TaskId} (Duration: {Duration}ms)", 
                        node.TaskId, node.CompletedAt.HasValue && node.StartedAt.HasValue 
                            ? (node.CompletedAt.Value - node.StartedAt.Value).TotalMilliseconds 
                            : 0);
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
    /// Gets a human-readable stage name for a task type
    /// </summary>
    private static string GetTaskStageName(GenerationTaskType taskType)
    {
        return taskType switch
        {
            GenerationTaskType.ScriptGeneration => "Script generation",
            GenerationTaskType.AudioGeneration => "Audio generation (TTS)",
            GenerationTaskType.ImageGeneration => "Image generation",
            GenerationTaskType.CaptionGeneration => "Caption generation",
            GenerationTaskType.VideoComposition => "Video composition (rendering)",
            _ => taskType.ToString()
        };
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
        bool anyRecovered = false;

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
                anyRecovered = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Recovery failed for task: {TaskId}", failed.TaskId);
            }
        }

        return anyRecovered;
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
    GenerationStrategy Strategy,
    IReadOnlyDictionary<string, TaskResult> TaskResults)
{
    public double QualityScore => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks : 0;
    public IReadOnlyList<string> FailureReasons => TaskResults.Values
        .Where(r => !r.Succeeded && r.ErrorMessage != null)
        .Select(r => r.ErrorMessage!)
        .ToList();
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
