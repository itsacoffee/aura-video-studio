using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Models.Visual;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Agents;

/// <summary>
/// Agent responsible for generating visual prompts for each scene
/// </summary>
public class VisualDirectorAgent : IAgent
{
    private readonly ILlmProvider _llmProvider;
    private readonly ILogger<VisualDirectorAgent> _logger;

    public string Name => "VisualDirector";

    public VisualDirectorAgent(
        ILlmProvider llmProvider,
        ILogger<VisualDirectorAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResponse> ProcessAsync(AgentMessage message, CancellationToken ct)
    {
        _logger.LogInformation("VisualDirectorAgent processing message type: {MessageType}", message.MessageType);

        if (message.MessageType != "GeneratePrompts")
        {
            throw new ArgumentException($"Unknown message type: {message.MessageType}");
        }

        var scriptDocument = (ScriptDocument)message.Payload;
        var brief = (Brief)message.Context!["brief"];

        _logger.LogInformation("Generating visual prompts for {SceneCount} scenes", scriptDocument.Scenes.Count);

        var visualPrompts = new List<VisualPrompt>();

        // Generate visual prompts for each scene
        foreach (var scene in scriptDocument.Scenes)
        {
            try
            {
                var visualPrompt = await GenerateVisualPromptAsync(scene, brief, ct).ConfigureAwait(false);
                visualPrompts.Add(visualPrompt);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate visual prompt for scene {SceneNumber}, using fallback", scene.Number);
                // Fallback to basic prompt
                visualPrompts.Add(new VisualPrompt(
                    SceneNumber: scene.Number,
                    DetailedPrompt: $"Visual representation of: {scene.Narration.Substring(0, Math.Min(100, scene.Narration.Length))}",
                    CameraAngle: "Medium shot",
                    Lighting: "Natural lighting"
                ));
            }
        }

        _logger.LogInformation("Generated {Count} visual prompts", visualPrompts.Count);

        return new AgentResponse(
            Success: true,
            Result: visualPrompts,
            FeedbackForRevision: null,
            RequiresRevision: false
        );
    }

    private async Task<VisualPrompt> GenerateVisualPromptAsync(
        Models.Generation.ScriptScene scene,
        Brief brief,
        CancellationToken ct)
    {
        // Use the LLM provider's visual prompt generation if available
        var previousSceneText = string.Empty; // Could be enhanced to pass previous scene context
        
        // Map tone to visual style
        var visualStyle = MapToneToVisualStyle(brief.Tone);

        var visualResult = await _llmProvider.GenerateVisualPromptAsync(
            scene.Narration,
            previousSceneText,
            brief.Tone ?? "Professional",
            visualStyle,
            ct
        ).ConfigureAwait(false);

        if (visualResult != null)
        {
            return new VisualPrompt(
                SceneNumber: scene.Number,
                DetailedPrompt: visualResult.DetailedDescription,
                CameraAngle: visualResult.CameraAngle,
                Lighting: visualResult.LightingMood,
                NegativePrompt: string.Join(", ", visualResult.NegativeElements),
                StyleKeywords: visualResult.StyleKeywords != null && visualResult.StyleKeywords.Length > 0 
                    ? string.Join(", ", visualResult.StyleKeywords) 
                    : null
            );
        }

        // Fallback if visual prompt generation is not available
        return new VisualPrompt(
            SceneNumber: scene.Number,
            DetailedPrompt: $"Visual representation of: {scene.Narration}",
            CameraAngle: "Medium shot",
            Lighting: "Natural lighting"
        );
    }

    private VisualStyle MapToneToVisualStyle(string tone)
    {
        return tone?.ToLowerInvariant() switch
        {
            "professional" or "educational" => VisualStyle.Realistic,
            "dramatic" or "cinematic" => VisualStyle.Cinematic,
            "creative" or "artistic" => VisualStyle.Illustrated,
            "modern" or "contemporary" => VisualStyle.Modern,
            "vintage" or "retro" => VisualStyle.Vintage,
            _ => VisualStyle.Cinematic
        };
    }
}

