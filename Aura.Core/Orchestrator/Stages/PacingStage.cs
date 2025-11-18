using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Orchestrator.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator.Stages;

/// <summary>
/// LLM-assisted pacing and scene restructuring stage.
/// Normalizes scene lengths, suggests merging/splitting, and marks peak attention moments.
/// Falls back to deterministic logic when LLM is unavailable.
/// </summary>
public class PacingStage
{
    private readonly ILogger<PacingStage> _logger;
    private readonly ILlmProvider _llmProvider;

    public PacingStage(
        ILogger<PacingStage> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Refines script pacing and structure using LLM or deterministic fallback
    /// </summary>
    public async Task<Script> RefineScriptPacingAsync(
        Script script,
        OrchestrationContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Refining script pacing for {SceneCount} scenes, target duration: {Duration}s, platform: {Platform}",
            script.Scenes.Count,
            context.PlanSpec.TargetDuration.TotalSeconds,
            context.TargetPlatform);

        if (_llmProvider.GetType().Name.Contains("RuleBased") || _llmProvider.GetType().Name.Contains("Mock"))
        {
            _logger.LogInformation("Using deterministic pacing refinement (LLM unavailable)");
            return ApplyDeterministicPacing(script, context);
        }

        try
        {
            return await ApplyLlmPacingAsync(script, context, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM pacing refinement failed, falling back to deterministic");
            return ApplyDeterministicPacing(script, context);
        }
    }

    private async Task<Script> ApplyLlmPacingAsync(
        Script script,
        OrchestrationContext context,
        CancellationToken ct)
    {
        var prompt = BuildPacingPrompt(script, context);

        var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);

        var refinedScenes = ParsePacingResponse(response, script, context);

        _logger.LogInformation(
            "LLM pacing complete. Original: {OriginalCount} scenes, Refined: {RefinedCount} scenes",
            script.Scenes.Count,
            refinedScenes.Count);

        return new Script(refinedScenes);
    }

    private string BuildPacingPrompt(Script script, OrchestrationContext context)
    {
        var scenesSummary = string.Join("\n", script.Scenes.Select((s, i) => 
            $"{i + 1}. Scene {s.Number} ({s.Duration.TotalSeconds:F1}s): {s.Narration.Substring(0, Math.Min(100, s.Narration.Length))}..."));

        return $@"You are an expert video content optimizer. Refine the pacing and structure of this video script.

{context.ToContextSummary()}

Current Script ({script.Scenes.Count} scenes):
{scenesSummary}

Instructions:
1. Normalize scene lengths to avoid overly short (<3s) or long (>{GetMaxSceneLength(context)}s) scenes
2. Suggest merging scenes that are too brief and related
3. Suggest splitting scenes that are too long or cover multiple distinct points
4. Mark 'peak attention' moments (most engaging content) for each scene
5. Optimize call-to-action placement based on platform ({context.TargetPlatform})
6. Consider target audience attention span: {GetAttentionSpanHint(context)}

Respond with JSON in this format:
{{
  ""scenes"": [
    {{
      ""index"": 0,
      ""action"": ""keep"" | ""merge_with_next"" | ""split"",
      ""heading"": ""Scene heading"",
      ""script"": ""Scene script text"",
      ""suggestedDuration"": 5.0,
      ""peakAttentionMoment"": ""Description of most engaging part"",
      ""rationale"": ""Why this pacing works""
    }}
  ],
  ""summary"": ""Overall pacing strategy explanation""
}}";
    }

    private List<ScriptScene> ParsePacingResponse(string response, Script originalScript, OrchestrationContext context)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);
                var pacingData = JsonSerializer.Deserialize<PacingResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (pacingData?.Scenes != null && pacingData.Scenes.Count > 0)
                {
                    return BuildRefinedScenes(pacingData.Scenes, originalScript, context);
                }
            }

            _logger.LogWarning("Failed to parse LLM pacing response, using original script");
            return originalScript.Scenes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing pacing response");
            return originalScript.Scenes;
        }
    }

    private List<ScriptScene> BuildRefinedScenes(
        List<PacingSceneData> pacingScenes,
        Script originalScript,
        OrchestrationContext context)
    {
        var refinedScenes = new List<ScriptScene>();

        for (int i = 0; i < pacingScenes.Count; i++)
        {
            var pacingScene = pacingScenes[i];
            var duration = TimeSpan.FromSeconds(pacingScene.SuggestedDuration ?? 5.0);
            
            var scene = new ScriptScene
            {
                Number = refinedScenes.Count + 1,
                Narration = pacingScene.Script ?? string.Empty,
                VisualPrompt = pacingScene.Heading ?? $"Scene {refinedScenes.Count + 1}",
                Duration = duration,
                Transition = TransitionType.Dissolve
            };
            
            refinedScenes.Add(scene);
        }

        return refinedScenes;
    }



    private Script ApplyDeterministicPacing(Script script, OrchestrationContext context)
    {
        var targetDuration = context.PlanSpec.TargetDuration;
        var sceneCount = script.Scenes.Count;
        var averageDuration = targetDuration.TotalSeconds / sceneCount;

        var refinedScenes = new List<ScriptScene>();

        foreach (var scene in script.Scenes)
        {
            var adjustedDuration = TimeSpan.FromSeconds(Math.Max(3.0, Math.Min(averageDuration * 1.5, scene.Duration.TotalSeconds)));
            
            var refinedScene = new ScriptScene
            {
                Number = refinedScenes.Count + 1,
                Narration = scene.Narration,
                VisualPrompt = scene.VisualPrompt,
                Duration = adjustedDuration,
                Transition = scene.Transition,
                ExtendedData = scene.ExtendedData
            };

            refinedScenes.Add(refinedScene);
        }

        _logger.LogInformation("Applied deterministic pacing to {SceneCount} scenes", refinedScenes.Count);
        return new Script 
        { 
            Scenes = refinedScenes,
            Title = script.Title,
            TotalDuration = TimeSpan.FromSeconds(refinedScenes.Sum(s => s.Duration.TotalSeconds)),
            Metadata = script.Metadata,
            CorrelationId = script.CorrelationId
        };
    }

    private double GetMaxSceneLength(OrchestrationContext context)
    {
        return context.TargetPlatform.ToLowerInvariant() switch
        {
            "tiktok" => 10.0,
            "youtube shorts" => 15.0,
            "instagram" => 12.0,
            "youtube" => 30.0,
            "linkedin" => 20.0,
            _ => 20.0
        };
    }

    private string GetAttentionSpanHint(OrchestrationContext context)
    {
        return context.TargetPlatform.ToLowerInvariant() switch
        {
            "tiktok" => "Very short (3-5s per point)",
            "youtube shorts" => "Short (5-10s per point)",
            "instagram" => "Short (5-10s per point)",
            "youtube" => "Medium (10-30s per point)",
            "linkedin" => "Professional (15-30s per point)",
            _ => "General (10-20s per point)"
        };
    }

    private class PacingResponse
    {
        public List<PacingSceneData> Scenes { get; set; } = new();
        public string? Summary { get; set; }
    }

    private class PacingSceneData
    {
        public int Index { get; set; }
        public string? Action { get; set; }
        public string? Heading { get; set; }
        public string? Script { get; set; }
        public double? SuggestedDuration { get; set; }
        public string? PeakAttentionMoment { get; set; }
        public string? Rationale { get; set; }
    }
}
