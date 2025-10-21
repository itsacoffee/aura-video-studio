using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.EditingIntelligence;
using Aura.Core.Models.Timeline;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.EditingIntelligence;

/// <summary>
/// Service for recommending appropriate transitions between scenes
/// </summary>
public class TransitionRecommendationService
{
    private readonly ILogger<TransitionRecommendationService> _logger;

    public TransitionRecommendationService(ILogger<TransitionRecommendationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Recommend transitions for all scene changes in timeline
    /// </summary>
    public async Task<IReadOnlyList<TransitionSuggestion>> RecommendTransitionsAsync(EditableTimeline timeline)
    {
        _logger.LogInformation("Analyzing transitions for timeline");
        var suggestions = new List<TransitionSuggestion>();

        for (int i = 0; i < timeline.Scenes.Count - 1; i++)
        {
            var fromScene = timeline.Scenes[i];
            var toScene = timeline.Scenes[i + 1];

            var suggestion = AnalyzeTransition(fromScene, toScene);
            suggestions.Add(suggestion);
        }

        await Task.CompletedTask;
        return suggestions;
    }

    private TransitionSuggestion AnalyzeTransition(TimelineScene fromScene, TimelineScene toScene)
    {
        var transitionType = DetermineTransitionType(fromScene, toScene);
        var duration = CalculateOptimalDuration(transitionType, fromScene, toScene);
        var reasoning = GenerateReasoning(transitionType, fromScene, toScene);
        var confidence = CalculateConfidence(transitionType, fromScene, toScene);
        var location = fromScene.Start + fromScene.Duration;

        return new TransitionSuggestion(
            FromSceneIndex: fromScene.Index,
            ToSceneIndex: toScene.Index,
            Location: location,
            Type: transitionType,
            Duration: duration,
            Reasoning: reasoning,
            Confidence: confidence
        );
    }

    private TransitionType DetermineTransitionType(TimelineScene fromScene, TimelineScene toScene)
    {
        // Analyze scene content to determine best transition
        var fromWords = fromScene.Script.ToLower().Split(' ');
        var toWords = toScene.Script.ToLower().Split(' ');

        // Check for temporal transitions
        var timeIndicators = new[] { "later", "meanwhile", "earlier", "before", "after", "next" };
        if (toWords.Take(5).Any(w => timeIndicators.Contains(w.Trim(',', '.'))))
        {
            return TransitionType.Dissolve;
        }

        // Check for dramatic shifts
        var dramaticWords = new[] { "suddenly", "however", "but", "unfortunately", "fortunately" };
        if (toWords.Take(3).Any(w => dramaticWords.Contains(w.Trim(',', '.'))))
        {
            return TransitionType.Wipe;
        }

        // Check for thematic continuation
        var commonWords = fromWords.Intersect(toWords).Count();
        if (commonWords > 3)
        {
            return TransitionType.Cut; // Direct cut for continuity
        }

        // Check for scene changes by heading
        if (fromScene.Heading != toScene.Heading)
        {
            return TransitionType.Fade;
        }

        // Default to cut for natural flow
        return TransitionType.Cut;
    }

    private TimeSpan CalculateOptimalDuration(TransitionType type, TimelineScene fromScene, TimelineScene toScene)
    {
        return type switch
        {
            TransitionType.Cut => TimeSpan.Zero,
            TransitionType.Fade => TimeSpan.FromSeconds(0.5),
            TransitionType.Dissolve => TimeSpan.FromSeconds(0.8),
            TransitionType.Wipe => TimeSpan.FromSeconds(0.6),
            TransitionType.Zoom => TimeSpan.FromSeconds(0.7),
            TransitionType.Slide => TimeSpan.FromSeconds(0.5),
            _ => TimeSpan.Zero
        };
    }

    private string GenerateReasoning(TransitionType type, TimelineScene fromScene, TimelineScene toScene)
    {
        return type switch
        {
            TransitionType.Cut => "Direct cut maintains energy and narrative flow between related scenes.",
            TransitionType.Fade => "Fade transition indicates scene change or passage of time.",
            TransitionType.Dissolve => "Dissolve creates smooth transition showing temporal or thematic shift.",
            TransitionType.Wipe => "Wipe transition emphasizes dramatic change or shift in narrative.",
            TransitionType.Zoom => "Zoom transition draws attention to important detail or shift in focus.",
            TransitionType.Slide => "Slide transition indicates spatial or directional change.",
            _ => "Standard transition for general use."
        };
    }

    private double CalculateConfidence(TransitionType type, TimelineScene fromScene, TimelineScene toScene)
    {
        // Base confidence on transition type and scene characteristics
        var baseConfidence = type switch
        {
            TransitionType.Cut => 0.9,  // Cut is almost always safe
            TransitionType.Fade => 0.85,
            TransitionType.Dissolve => 0.8,
            TransitionType.Wipe => 0.7,
            TransitionType.Zoom => 0.75,
            TransitionType.Slide => 0.75,
            _ => 0.6
        };

        // Adjust based on scene similarity
        var fromWords = fromScene.Script.ToLower().Split(' ');
        var toWords = toScene.Script.ToLower().Split(' ');
        var similarity = fromWords.Intersect(toWords).Count() / (double)Math.Max(fromWords.Length, toWords.Length);
        
        // Higher similarity = higher confidence in transition choice
        return Math.Min(1.0, baseConfidence + (similarity * 0.1));
    }

    /// <summary>
    /// Detect jarring transitions that need fixing
    /// </summary>
    public async Task<IReadOnlyList<TransitionSuggestion>> DetectJarringTransitionsAsync(EditableTimeline timeline)
    {
        _logger.LogInformation("Detecting jarring transitions");
        var jarringTransitions = new List<TransitionSuggestion>();

        for (int i = 0; i < timeline.Scenes.Count - 1; i++)
        {
            var fromScene = timeline.Scenes[i];
            var toScene = timeline.Scenes[i + 1];

            // Check if current transition is jarring
            var currentTransition = fromScene.TransitionType ?? "None";
            var isJarring = IsTransitionJarring(fromScene, toScene, currentTransition);

            if (isJarring)
            {
                var betterTransition = AnalyzeTransition(fromScene, toScene);
                jarringTransitions.Add(betterTransition);
            }
        }

        await Task.CompletedTask;
        return jarringTransitions;
    }

    private bool IsTransitionJarring(TimelineScene fromScene, TimelineScene toScene, string currentTransition)
    {
        // A transition is jarring if it doesn't match the content
        var hasTemporalShift = toScene.Script.ToLower().Contains("later") || 
                               toScene.Script.ToLower().Contains("earlier");
        
        if (hasTemporalShift && currentTransition == "None")
            return true;

        // Dramatic shifts need transitions
        var hasDramaticShift = toScene.Script.ToLower().StartsWith("however") ||
                              toScene.Script.ToLower().StartsWith("but");
        
        if (hasDramaticShift && currentTransition == "None")
            return true;

        return false;
    }

    /// <summary>
    /// Enforce variety in transitions to avoid monotony
    /// </summary>
    public async Task<IReadOnlyList<TransitionSuggestion>> EnforceTransitionVarietyAsync(
        EditableTimeline timeline,
        IReadOnlyList<TransitionSuggestion> currentSuggestions)
    {
        _logger.LogInformation("Enforcing transition variety");
        var varied = new List<TransitionSuggestion>(currentSuggestions);

        // Count transition types
        var typeCounts = currentSuggestions
            .GroupBy(t => t.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        // If too many of the same type, suggest alternatives
        foreach (var (type, count) in typeCounts)
        {
            if (count > currentSuggestions.Count * 0.6) // More than 60% same type
            {
                _logger.LogInformation("Too many {Type} transitions ({Count}), suggesting variety", type, count);
                
                // Suggest alternatives for some instances
                var toVary = varied
                    .Where(t => t.Type == type)
                    .OrderBy(t => t.Confidence)
                    .Take(count / 3)
                    .ToList();

                foreach (var suggestion in toVary)
                {
                    var alternativeType = GetAlternativeTransition(type);
                    var index = varied.IndexOf(suggestion);
                    
                    varied[index] = suggestion with
                    {
                        Type = alternativeType,
                        Duration = CalculateOptimalDuration(
                            alternativeType, 
                            timeline.Scenes[suggestion.FromSceneIndex],
                            timeline.Scenes[suggestion.ToSceneIndex]
                        ),
                        Reasoning = $"Varied to {alternativeType} for visual interest. {suggestion.Reasoning}",
                        Confidence = suggestion.Confidence * 0.9
                    };
                }
            }
        }

        await Task.CompletedTask;
        return varied;
    }

    private TransitionType GetAlternativeTransition(TransitionType current)
    {
        return current switch
        {
            TransitionType.Cut => TransitionType.Fade,
            TransitionType.Fade => TransitionType.Dissolve,
            TransitionType.Dissolve => TransitionType.Fade,
            TransitionType.Wipe => TransitionType.Slide,
            TransitionType.Slide => TransitionType.Wipe,
            _ => TransitionType.Fade
        };
    }
}
