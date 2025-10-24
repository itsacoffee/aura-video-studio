using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PacingServices;

/// <summary>
/// Service for mapping scene relationships and dependencies
/// Builds a graph of logical connections and validates narrative coherence
/// </summary>
public class SceneRelationshipMapper
{
    private readonly ILogger<SceneRelationshipMapper> _logger;

    public SceneRelationshipMapper(ILogger<SceneRelationshipMapper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes relationships and dependencies between scenes
    /// </summary>
    public async Task<SceneRelationshipGraph> MapRelationshipsAsync(
        IReadOnlyList<Scene> scenes,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;
        ct.ThrowIfCancellationRequested();

        _logger.LogInformation("Mapping scene relationships for {SceneCount} scenes", scenes.Count);

        var relationships = BuildRelationshipGraph(scenes);
        var flowIssues = DetectFlowIssues(scenes, relationships);
        var dependencies = IdentifyInformationDependencies(scenes);
        var reorderingSuggestions = SuggestReordering(scenes, relationships, dependencies);

        var graph = new SceneRelationshipGraph
        {
            Relationships = relationships,
            FlowIssues = flowIssues,
            InformationDependencies = dependencies,
            ReorderingSuggestions = reorderingSuggestions,
            IsCoherent = flowIssues.Count == 0
        };

        _logger.LogInformation("Identified {RelationshipCount} relationships, {IssueCount} flow issues",
            relationships.Count, flowIssues.Count);

        return graph;
    }

    private List<SceneRelationship> BuildRelationshipGraph(IReadOnlyList<Scene> scenes)
    {
        var relationships = new List<SceneRelationship>();

        for (int i = 0; i < scenes.Count; i++)
        {
            for (int j = i + 1; j < scenes.Count; j++)
            {
                var fromScene = scenes[i];
                var toScene = scenes[j];

                var connectionType = AnalyzeConnection(fromScene, toScene);
                var strength = CalculateConnectionStrength(fromScene, toScene);

                if (strength > 0.1) // Only include meaningful relationships
                {
                    relationships.Add(new SceneRelationship
                    {
                        FromSceneIndex = i,
                        ToSceneIndex = j,
                        ConnectionType = connectionType,
                        Strength = strength,
                        IsSequential = j == i + 1
                    });
                }
            }
        }

        return relationships;
    }

    private string AnalyzeConnection(Scene fromScene, Scene toScene)
    {
        var fromWords = GetSignificantWords(fromScene.Script);
        var toWords = GetSignificantWords(toScene.Script);

        var commonWords = fromWords.Intersect(toWords, StringComparer.OrdinalIgnoreCase).ToList();
        var overlapRatio = commonWords.Count / (double)Math.Max(fromWords.Count, 1);

        // Check for callback/reference patterns
        if (HasCallback(fromScene, toScene))
            return "callback";

        // Check for setup/payoff pattern
        if (HasSetupPayoff(fromScene, toScene))
            return "setup-payoff";

        // Check for cause-effect
        if (HasCauseEffect(fromScene, toScene))
            return "cause-effect";

        // Check for topic continuation
        if (overlapRatio > 0.4)
            return "continuation";

        // Check for contrast
        if (HasContrast(fromScene, toScene))
            return "contrast";

        return "unrelated";
    }

    private double CalculateConnectionStrength(Scene fromScene, Scene toScene)
    {
        var fromWords = GetSignificantWords(fromScene.Script);
        var toWords = GetSignificantWords(toScene.Script);

        var commonWords = fromWords.Intersect(toWords, StringComparer.OrdinalIgnoreCase).ToList();
        var overlapRatio = commonWords.Count / (double)Math.Max(fromWords.Count, 1);

        // Boost strength for specific patterns
        if (HasCallback(fromScene, toScene))
            overlapRatio += 0.3;
        if (HasSetupPayoff(fromScene, toScene))
            overlapRatio += 0.4;
        if (HasCauseEffect(fromScene, toScene))
            overlapRatio += 0.2;

        return Math.Clamp(overlapRatio, 0, 1);
    }

    private List<FlowIssue> DetectFlowIssues(IReadOnlyList<Scene> scenes, List<SceneRelationship> relationships)
    {
        var issues = new List<FlowIssue>();

        // Check for non-sequitur transitions
        for (int i = 0; i < scenes.Count - 1; i++)
        {
            var sequential = relationships.FirstOrDefault(r => 
                r.FromSceneIndex == i && r.ToSceneIndex == i + 1 && r.IsSequential);

            if (sequential == null || sequential.Strength < 0.2)
            {
                issues.Add(new FlowIssue
                {
                    SceneIndex = i,
                    IssueType = "non-sequitur",
                    Severity = sequential?.Strength < 0.1 ? "high" : "medium",
                    Description = $"Scene {i} to {i + 1} transition is abrupt with weak connection"
                });
            }
        }

        // Check for orphaned scenes (no strong connections)
        for (int i = 0; i < scenes.Count; i++)
        {
            var connections = relationships.Where(r => 
                r.FromSceneIndex == i || r.ToSceneIndex == i).ToList();
            
            var strongConnections = connections.Count(c => c.Strength > 0.3);

            if (strongConnections == 0 && scenes.Count > 1)
            {
                issues.Add(new FlowIssue
                {
                    SceneIndex = i,
                    IssueType = "orphaned",
                    Severity = "high",
                    Description = $"Scene {i} has no strong connections to other scenes"
                });
            }
        }

        return issues;
    }

    private List<InformationDependency> IdentifyInformationDependencies(IReadOnlyList<Scene> scenes)
    {
        var dependencies = new List<InformationDependency>();

        for (int i = 0; i < scenes.Count; i++)
        {
            for (int j = i + 1; j < scenes.Count; j++)
            {
                var fromScene = scenes[i];
                var toScene = scenes[j];

                // Check if toScene references something from fromScene
                if (HasReference(toScene, fromScene))
                {
                    dependencies.Add(new InformationDependency
                    {
                        DependentSceneIndex = j,
                        RequiredSceneIndex = i,
                        DependencyType = "reference",
                        Description = $"Scene {j} references content from scene {i}"
                    });
                }

                // Check for setup-payoff dependencies
                if (HasSetupPayoff(fromScene, toScene))
                {
                    dependencies.Add(new InformationDependency
                    {
                        DependentSceneIndex = j,
                        RequiredSceneIndex = i,
                        DependencyType = "setup-payoff",
                        Description = $"Scene {j} payoff depends on scene {i} setup"
                    });
                }
            }
        }

        return dependencies;
    }

    private List<ReorderingSuggestion> SuggestReordering(
        IReadOnlyList<Scene> scenes,
        List<SceneRelationship> relationships,
        List<InformationDependency> dependencies)
    {
        var suggestions = new List<ReorderingSuggestion>();

        // Check for misplaced payoffs (payoff before setup)
        foreach (var dep in dependencies.Where(d => d.DependencyType == "setup-payoff"))
        {
            if (dep.DependentSceneIndex < dep.RequiredSceneIndex)
            {
                suggestions.Add(new ReorderingSuggestion
                {
                    SceneIndex = dep.DependentSceneIndex,
                    SuggestedPosition = dep.RequiredSceneIndex + 1,
                    Reason = "Payoff scene appears before setup scene",
                    Priority = "high"
                });
            }
        }

        // Suggest grouping related scenes
        for (int i = 0; i < scenes.Count; i++)
        {
            var strongConnections = relationships
                .Where(r => (r.FromSceneIndex == i || r.ToSceneIndex == i) && r.Strength > 0.5)
                .ToList();

            // If scene has strong connection to non-adjacent scene
            foreach (var conn in strongConnections.Where(c => !c.IsSequential))
            {
                var otherIndex = conn.FromSceneIndex == i ? conn.ToSceneIndex : conn.FromSceneIndex;
                var distance = Math.Abs(otherIndex - i);

                if (distance > 2)
                {
                    suggestions.Add(new ReorderingSuggestion
                    {
                        SceneIndex = otherIndex,
                        SuggestedPosition = i + 1,
                        Reason = $"Scene {otherIndex} is strongly related to scene {i} but separated",
                        Priority = "medium"
                    });
                }
            }
        }

        return suggestions;
    }

    private List<string> GetSignificantWords(string text)
    {
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "is", "are", "was", "were", "be", "been",
            "this", "that", "these", "those", "we", "you", "they", "it"
        };

        return text
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !stopWords.Contains(w))
            .ToList();
    }

    private bool HasCallback(Scene fromScene, Scene toScene)
    {
        var callbackPhrases = new[] { "remember", "as I mentioned", "like we said", "earlier", "before" };
        return callbackPhrases.Any(p => toScene.Script.Contains(p, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasSetupPayoff(Scene fromScene, Scene toScene)
    {
        var setupWords = new[] { "will", "going to", "plan", "prepare", "setup", "introduce" };
        var payoffWords = new[] { "finally", "result", "outcome", "achieved", "completed", "revealed" };

        var hasSetup = setupWords.Any(w => fromScene.Script.Contains(w, StringComparison.OrdinalIgnoreCase));
        var hasPayoff = payoffWords.Any(w => toScene.Script.Contains(w, StringComparison.OrdinalIgnoreCase));

        return hasSetup && hasPayoff;
    }

    private bool HasCauseEffect(Scene fromScene, Scene toScene)
    {
        var causeWords = new[] { "because", "since", "due to", "caused by", "therefore", "so", "thus" };
        return causeWords.Any(w => toScene.Script.Contains(w, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasContrast(Scene fromScene, Scene toScene)
    {
        var contrastWords = new[] { "however", "but", "although", "despite", "contrary", "opposite", "different" };
        return contrastWords.Any(w => toScene.Script.Contains(w, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasReference(Scene toScene, Scene fromScene)
    {
        var fromSignificantWords = GetSignificantWords(fromScene.Script).Take(5).ToList();
        return fromSignificantWords.Any(w => toScene.Script.Contains(w, StringComparison.OrdinalIgnoreCase));
    }
}

