using System;
using System.Collections.Generic;
using System.Linq;

namespace Aura.Core.Models.Generation;

/// <summary>
/// Models dependencies between video assets for optimal generation sequencing.
/// Uses topological sorting to determine the optimal order of asset generation.
/// </summary>
public class AssetDependencyGraph
{
    private readonly Dictionary<string, GenerationNode> _nodes = new();
    private readonly Dictionary<string, HashSet<string>> _dependencies = new();
    private readonly Dictionary<string, HashSet<string>> _dependents = new();

    /// <summary>
    /// Adds a generation task node to the dependency graph
    /// </summary>
    public void AddNode(string taskId, GenerationTaskType taskType, int priority, double estimatedResourceCost)
    {
        if (_nodes.ContainsKey(taskId))
        {
            throw new InvalidOperationException($"Node {taskId} already exists in the graph");
        }

        _nodes[taskId] = new GenerationNode(taskId, taskType, priority, estimatedResourceCost);
        _dependencies[taskId] = new HashSet<string>();
        _dependents[taskId] = new HashSet<string>();
    }

    /// <summary>
    /// Adds a dependency between two tasks (fromTask must complete before toTask can start)
    /// </summary>
    public void AddDependency(string fromTaskId, string toTaskId)
    {
        if (!_nodes.ContainsKey(fromTaskId))
        {
            throw new ArgumentException($"Task {fromTaskId} not found in graph", nameof(fromTaskId));
        }

        if (!_nodes.ContainsKey(toTaskId))
        {
            throw new ArgumentException($"Task {toTaskId} not found in graph", nameof(toTaskId));
        }

        _dependencies[toTaskId].Add(fromTaskId);
        _dependents[fromTaskId].Add(toTaskId);
    }

    /// <summary>
    /// Performs topological sort to determine optimal generation sequence.
    /// Returns batches of tasks that can be executed in parallel.
    /// </summary>
    public List<List<GenerationNode>> GetOptimalExecutionBatches()
    {
        var batches = new List<List<GenerationNode>>();
        var remaining = new HashSet<string>(_nodes.Keys);
        var completed = new HashSet<string>();

        while (remaining.Count > 0)
        {
            // Find all tasks that have no unmet dependencies
            var readyTasks = remaining
                .Where(taskId => _dependencies[taskId].All(dep => completed.Contains(dep)))
                .Select(taskId => _nodes[taskId])
                .OrderByDescending(node => node.Priority)
                .ThenBy(node => node.EstimatedResourceCost)
                .ToList();

            if (readyTasks.Count == 0)
            {
                // Circular dependency detected
                throw new InvalidOperationException("Circular dependency detected in task graph");
            }

            batches.Add(readyTasks);

            // Mark tasks as completed and remove from remaining
            foreach (var task in readyTasks)
            {
                completed.Add(task.TaskId);
                remaining.Remove(task.TaskId);
            }
        }

        return batches;
    }

    /// <summary>
    /// Gets all tasks that depend on the specified task
    /// </summary>
    public IReadOnlyList<string> GetDependentTasks(string taskId)
    {
        if (!_dependents.TryGetValue(taskId, out var value))
        {
            return Array.Empty<string>();
        }

        return value.ToList();
    }

    /// <summary>
    /// Gets all tasks that the specified task depends on
    /// </summary>
    public IReadOnlyList<string> GetDependencies(string taskId)
    {
        if (!_dependencies.TryGetValue(taskId, out var value))
        {
            return Array.Empty<string>();
        }

        return value.ToList();
    }

    /// <summary>
    /// Gets the total number of nodes in the graph
    /// </summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// Checks if a task exists in the graph
    /// </summary>
    public bool ContainsTask(string taskId) => _nodes.ContainsKey(taskId);

    /// <summary>
    /// Gets a specific node from the graph
    /// </summary>
    public GenerationNode? GetNode(string taskId)
    {
        return _nodes.TryGetValue(taskId, out var node) ? node : null;
    }
}

/// <summary>
/// Represents a single generation task node in the dependency graph
/// </summary>
public record GenerationNode(
    string TaskId,
    GenerationTaskType TaskType,
    int Priority,
    double EstimatedResourceCost)
{
    /// <summary>
    /// Current status of the task
    /// </summary>
    public TaskStatus Status { get; set; } = TaskStatus.Pending;

    /// <summary>
    /// Result data from completed task (e.g., file path, asset reference)
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Error information if task failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Timestamp when task started
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when task completed
    /// </summary>
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Types of generation tasks
/// </summary>
public enum GenerationTaskType
{
    ScriptGeneration,
    ImageGeneration,
    AudioGeneration,
    VideoComposition,
    CaptionGeneration,
    MusicGeneration,
    AssetRetrieval
}

/// <summary>
/// Status of a generation task
/// </summary>
public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled
}
