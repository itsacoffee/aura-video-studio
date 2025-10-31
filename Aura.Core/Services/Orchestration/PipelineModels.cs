using System;
using System.Collections.Generic;
using Aura.Core.Models;

namespace Aura.Core.Services.Orchestration;

/// <summary>
/// Represents a stage in the pipeline execution
/// </summary>
public enum PipelineStage
{
    ScriptGeneration,
    ScriptAnalysis,
    ScriptOptimization,
    VisualPlanning,
    NarrationOptimization,
    Finalization
}

/// <summary>
/// Represents the execution status of a service
/// </summary>
public enum ServiceExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped
}

/// <summary>
/// Defines a service that can be executed in the pipeline
/// </summary>
public class PipelineService
{
    public required string ServiceId { get; init; }
    public required string Name { get; init; }
    public required PipelineStage Stage { get; init; }
    public required bool IsRequired { get; init; }
    public required List<string> DependsOn { get; init; }
    public int Priority { get; init; }
    public ServiceExecutionStatus Status { get; set; } = ServiceExecutionStatus.Pending;
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Configuration for pipeline execution
/// </summary>
public class PipelineConfiguration
{
    public int MaxConcurrentLlmCalls { get; set; } = 3;
    public bool EnableCaching { get; set; } = true;
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);
    public bool ContinueOnOptionalFailure { get; set; } = true;
    public bool EnableParallelExecution { get; set; } = true;
}

/// <summary>
/// Result of service execution
/// </summary>
public class ServiceExecutionResult
{
    public required string ServiceId { get; init; }
    public bool Success { get; init; }
    public object? Result { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public bool FromCache { get; init; }
}

/// <summary>
/// Result of pipeline execution
/// </summary>
public class PipelineExecutionResult
{
    public required bool Success { get; init; }
    public required Dictionary<string, ServiceExecutionResult> ServiceResults { get; init; }
    public required TimeSpan TotalExecutionTime { get; init; }
    public required Dictionary<PipelineStage, TimeSpan> StageTimings { get; init; }
    public required List<string> Warnings { get; init; }
    public required List<string> Errors { get; init; }
    public int CacheHits { get; init; }
    public int ParallelExecutions { get; init; }
}

/// <summary>
/// Context for pipeline execution containing all required data
/// </summary>
public class PipelineExecutionContext
{
    public required Brief Brief { get; init; }
    public required PlanSpec PlanSpec { get; init; }
    public required VoiceSpec VoiceSpec { get; init; }
    public required RenderSpec RenderSpec { get; init; }
    public required SystemProfile SystemProfile { get; init; }
    
    public string? GeneratedScript { get; set; }
    public List<Scene>? ParsedScenes { get; set; }
    public string? NarrationPath { get; set; }
    public Dictionary<int, IReadOnlyList<Asset>> SceneAssets { get; set; } = new();
}

/// <summary>
/// Progress information for pipeline execution
/// </summary>
public class PipelineProgress
{
    public required PipelineStage CurrentStage { get; init; }
    public required string CurrentService { get; init; }
    public required int CompletedServices { get; init; }
    public required int TotalServices { get; init; }
    public required double PercentComplete { get; init; }
}

/// <summary>
/// Health check result for pipeline services
/// </summary>
public class PipelineHealthCheckResult
{
    public required bool IsHealthy { get; init; }
    public required Dictionary<string, bool> ServiceAvailability { get; init; }
    public required List<string> MissingRequiredServices { get; init; }
    public required List<string> Warnings { get; init; }
}

/// <summary>
/// Cache entry for pipeline service results
/// </summary>
internal class CacheEntry
{
    public required object Value { get; init; }
    public required DateTime CreatedAt { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
