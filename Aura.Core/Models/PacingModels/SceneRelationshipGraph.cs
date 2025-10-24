using System.Collections.Generic;

namespace Aura.Core.Models.PacingModels;

/// <summary>
/// Graph of scene relationships and flow analysis
/// </summary>
public record SceneRelationshipGraph
{
    public IReadOnlyList<SceneRelationship> Relationships { get; init; } = new List<SceneRelationship>();
    public IReadOnlyList<FlowIssue> FlowIssues { get; init; } = new List<FlowIssue>();
    public IReadOnlyList<InformationDependency> InformationDependencies { get; init; } = new List<InformationDependency>();
    public IReadOnlyList<ReorderingSuggestion> ReorderingSuggestions { get; init; } = new List<ReorderingSuggestion>();
    public bool IsCoherent { get; init; }
}

/// <summary>
/// Relationship between two scenes
/// </summary>
public record SceneRelationship
{
    public int FromSceneIndex { get; init; }
    public int ToSceneIndex { get; init; }
    public string ConnectionType { get; init; } = string.Empty;
    public double Strength { get; init; }
    public bool IsSequential { get; init; }
}

/// <summary>
/// Flow issue in scene progression
/// </summary>
public record FlowIssue
{
    public int SceneIndex { get; init; }
    public string IssueType { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Information dependency between scenes
/// </summary>
public record InformationDependency
{
    public int DependentSceneIndex { get; init; }
    public int RequiredSceneIndex { get; init; }
    public string DependencyType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// Suggestion for scene reordering
/// </summary>
public record ReorderingSuggestion
{
    public int SceneIndex { get; init; }
    public int SuggestedPosition { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
}
