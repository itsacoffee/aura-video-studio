using System;
using System.Linq;
using Aura.Core.Models.Generation;
using Xunit;
using TaskStatus = Aura.Core.Models.Generation.TaskStatus;

namespace Aura.Tests;

public class AssetDependencyGraphTests
{
    [Fact]
    public void AddNode_ShouldAddNodeToGraph()
    {
        // Arrange
        var graph = new AssetDependencyGraph();

        // Act
        graph.AddNode("task1", GenerationTaskType.ScriptGeneration, 100, 0.5);

        // Assert
        Assert.Equal(1, graph.NodeCount);
        Assert.True(graph.ContainsTask("task1"));
    }

    [Fact]
    public void AddNode_WithDuplicateId_ShouldThrowException()
    {
        // Arrange
        var graph = new AssetDependencyGraph();
        graph.AddNode("task1", GenerationTaskType.ScriptGeneration, 100, 0.5);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            graph.AddNode("task1", GenerationTaskType.AudioGeneration, 90, 0.6));
    }

    [Fact]
    public void AddDependency_ShouldLinkTwoNodes()
    {
        // Arrange
        var graph = new AssetDependencyGraph();
        graph.AddNode("task1", GenerationTaskType.ScriptGeneration, 100, 0.5);
        graph.AddNode("task2", GenerationTaskType.AudioGeneration, 90, 0.6);

        // Act
        graph.AddDependency("task1", "task2");

        // Assert
        var dependencies = graph.GetDependencies("task2");
        Assert.Single(dependencies);
        Assert.Contains("task1", dependencies);

        var dependents = graph.GetDependentTasks("task1");
        Assert.Single(dependents);
        Assert.Contains("task2", dependents);
    }

    [Fact]
    public void AddDependency_WithNonExistentNode_ShouldThrowException()
    {
        // Arrange
        var graph = new AssetDependencyGraph();
        graph.AddNode("task1", GenerationTaskType.ScriptGeneration, 100, 0.5);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            graph.AddDependency("task1", "nonexistent"));
    }

    [Fact]
    public void GetOptimalExecutionBatches_ShouldReturnCorrectBatches()
    {
        // Arrange
        var graph = new AssetDependencyGraph();
        graph.AddNode("script", GenerationTaskType.ScriptGeneration, 100, 0.3);
        graph.AddNode("audio", GenerationTaskType.AudioGeneration, 90, 0.5);
        graph.AddNode("visual1", GenerationTaskType.ImageGeneration, 50, 0.4);
        graph.AddNode("visual2", GenerationTaskType.ImageGeneration, 50, 0.4);
        graph.AddNode("composition", GenerationTaskType.VideoComposition, 10, 0.8);

        graph.AddDependency("script", "audio");
        graph.AddDependency("script", "visual1");
        graph.AddDependency("script", "visual2");
        graph.AddDependency("audio", "composition");
        graph.AddDependency("visual1", "composition");
        graph.AddDependency("visual2", "composition");

        // Act
        var batches = graph.GetOptimalExecutionBatches();

        // Assert
        Assert.Equal(3, batches.Count);
        
        // First batch should only contain script
        Assert.Single(batches[0]);
        Assert.Equal("script", batches[0][0].TaskId);

        // Second batch should contain audio, visual1, visual2 (parallel)
        Assert.Equal(3, batches[1].Count);
        Assert.Contains(batches[1], node => node.TaskId == "audio");
        Assert.Contains(batches[1], node => node.TaskId == "visual1");
        Assert.Contains(batches[1], node => node.TaskId == "visual2");

        // Third batch should contain composition
        Assert.Single(batches[2]);
        Assert.Equal("composition", batches[2][0].TaskId);
    }

    [Fact]
    public void GetOptimalExecutionBatches_WithCircularDependency_ShouldThrowException()
    {
        // Arrange
        var graph = new AssetDependencyGraph();
        graph.AddNode("task1", GenerationTaskType.ScriptGeneration, 100, 0.5);
        graph.AddNode("task2", GenerationTaskType.AudioGeneration, 90, 0.6);
        graph.AddNode("task3", GenerationTaskType.ImageGeneration, 80, 0.4);

        graph.AddDependency("task1", "task2");
        graph.AddDependency("task2", "task3");
        graph.AddDependency("task3", "task1"); // Circular dependency

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => graph.GetOptimalExecutionBatches());
    }

    [Fact]
    public void GetOptimalExecutionBatches_ShouldOrderByPriority()
    {
        // Arrange
        var graph = new AssetDependencyGraph();
        graph.AddNode("task1", GenerationTaskType.ScriptGeneration, 50, 0.5);
        graph.AddNode("task2", GenerationTaskType.AudioGeneration, 100, 0.6);
        graph.AddNode("task3", GenerationTaskType.ImageGeneration, 75, 0.4);

        // Act
        var batches = graph.GetOptimalExecutionBatches();

        // Assert
        Assert.Single(batches);
        Assert.Equal(3, batches[0].Count);
        
        // Should be ordered by priority (descending)
        Assert.Equal("task2", batches[0][0].TaskId);
        Assert.Equal("task3", batches[0][1].TaskId);
        Assert.Equal("task1", batches[0][2].TaskId);
    }

    [Fact]
    public void GetNode_WithExistingTask_ShouldReturnNode()
    {
        // Arrange
        var graph = new AssetDependencyGraph();
        graph.AddNode("task1", GenerationTaskType.ScriptGeneration, 100, 0.5);

        // Act
        var node = graph.GetNode("task1");

        // Assert
        Assert.NotNull(node);
        Assert.Equal("task1", node.TaskId);
        Assert.Equal(GenerationTaskType.ScriptGeneration, node.TaskType);
        Assert.Equal(100, node.Priority);
        Assert.Equal(0.5, node.EstimatedResourceCost);
    }

    [Fact]
    public void GetNode_WithNonExistentTask_ShouldReturnNull()
    {
        // Arrange
        var graph = new AssetDependencyGraph();

        // Act
        var node = graph.GetNode("nonexistent");

        // Assert
        Assert.Null(node);
    }

    [Fact]
    public void GenerationNode_ShouldInitializeWithPendingStatus()
    {
        // Arrange & Act
        var node = new GenerationNode("task1", GenerationTaskType.ScriptGeneration, 100, 0.5);

        // Assert
        Assert.Equal(TaskStatus.Pending, node.Status);
        Assert.Null(node.Result);
        Assert.Null(node.ErrorMessage);
        Assert.Null(node.StartedAt);
        Assert.Null(node.CompletedAt);
    }

    [Fact]
    public void GenerationNode_ShouldAllowStatusUpdates()
    {
        // Arrange
        var node = new GenerationNode("task1", GenerationTaskType.ScriptGeneration, 100, 0.5);
        var now = DateTime.UtcNow;

        // Act
        node.Status = TaskStatus.Running;
        node.StartedAt = now;

        // Assert
        Assert.Equal(TaskStatus.Running, node.Status);
        Assert.Equal(now, node.StartedAt);
    }
}
