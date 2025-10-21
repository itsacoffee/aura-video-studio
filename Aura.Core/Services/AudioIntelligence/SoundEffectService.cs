using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audio;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.AudioIntelligence;

/// <summary>
/// Service for suggesting sound effects based on content
/// </summary>
public class SoundEffectService
{
    private readonly ILogger<SoundEffectService> _logger;
    private readonly ILlmProvider _llmProvider;

    public SoundEffectService(
        ILogger<SoundEffectService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Suggests sound effects based on script content
    /// </summary>
    public async Task<List<SoundEffect>> SuggestSoundEffectsAsync(
        string script,
        List<TimeSpan>? sceneDurations = null,
        string? contentType = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Suggesting sound effects for script (length: {Length})", script.Length);

        try
        {
            var suggestions = new List<SoundEffect>();
            var lines = SplitScriptIntoScenes(script);
            var currentTime = TimeSpan.Zero;

            for (int i = 0; i < lines.Count; i++)
            {
                var scene = lines[i];
                var duration = sceneDurations != null && i < sceneDurations.Count 
                    ? sceneDurations[i] 
                    : TimeSpan.FromSeconds(10);

                var sceneEffects = AnalyzeSceneForSoundEffects(scene, i, currentTime, duration);
                suggestions.AddRange(sceneEffects);

                currentTime += duration;
            }

            // Add transition effects between scenes
            var transitionEffects = GenerateTransitionEffects(lines.Count, sceneDurations);
            suggestions.AddRange(transitionEffects);

            // Sort by timestamp
            return suggestions.OrderBy(s => s.Timestamp).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error suggesting sound effects");
            throw;
        }
    }

    /// <summary>
    /// Analyzes a scene for sound effect opportunities
    /// </summary>
    private List<SoundEffect> AnalyzeSceneForSoundEffects(
        string scene,
        int sceneIndex,
        TimeSpan startTime,
        TimeSpan duration)
    {
        var effects = new List<SoundEffect>();
        var sceneLower = scene.ToLowerInvariant();

        // Technology/UI sounds
        if (ContainsKeywords(sceneLower, "click", "button", "tap", "select", "choose"))
        {
            effects.Add(new SoundEffect(
                EffectId: $"sfx_click_{sceneIndex}",
                Type: SoundEffectType.UI,
                Description: "UI click sound",
                Timestamp: startTime + TimeSpan.FromSeconds(1),
                Duration: TimeSpan.FromMilliseconds(100),
                Volume: 50,
                Purpose: "Emphasize user interaction",
                FilePath: "/sfx/ui_click.wav"
            ));
        }

        // Impact/reveal sounds
        if (ContainsKeywords(sceneLower, "reveal", "unveil", "present", "introduce", "show"))
        {
            effects.Add(new SoundEffect(
                EffectId: $"sfx_reveal_{sceneIndex}",
                Type: SoundEffectType.Impact,
                Description: "Reveal impact sound",
                Timestamp: startTime + TimeSpan.FromSeconds(0.5),
                Duration: TimeSpan.FromMilliseconds(500),
                Volume: 60,
                Purpose: "Emphasize reveal moment",
                FilePath: "/sfx/impact_reveal.wav"
            ));
        }

        // Whoosh sounds for motion
        if (ContainsKeywords(sceneLower, "move", "fly", "zoom", "slide", "transition"))
        {
            effects.Add(new SoundEffect(
                EffectId: $"sfx_whoosh_{sceneIndex}",
                Type: SoundEffectType.Whoosh,
                Description: "Motion whoosh",
                Timestamp: startTime,
                Duration: TimeSpan.FromMilliseconds(400),
                Volume: 45,
                Purpose: "Enhance visual motion",
                FilePath: "/sfx/whoosh_fast.wav"
            ));
        }

        // Technology sounds
        if (ContainsKeywords(sceneLower, "data", "analyze", "compute", "process", "scan"))
        {
            effects.Add(new SoundEffect(
                EffectId: $"sfx_tech_{sceneIndex}",
                Type: SoundEffectType.Technology,
                Description: "Technology processing sound",
                Timestamp: startTime + TimeSpan.FromSeconds(0.5),
                Duration: TimeSpan.FromSeconds(2),
                Volume: 35,
                Purpose: "Ambient technology atmosphere",
                FilePath: "/sfx/tech_processing.wav"
            ));
        }

        // Success/completion sounds
        if (ContainsKeywords(sceneLower, "complete", "done", "success", "achieve", "finish"))
        {
            effects.Add(new SoundEffect(
                EffectId: $"sfx_success_{sceneIndex}",
                Type: SoundEffectType.Notification,
                Description: "Success notification",
                Timestamp: startTime + duration - TimeSpan.FromSeconds(1),
                Duration: TimeSpan.FromMilliseconds(800),
                Volume: 55,
                Purpose: "Indicate completion",
                FilePath: "/sfx/success_chime.wav"
            ));
        }

        // Action sounds
        if (ContainsKeywords(sceneLower, "hit", "strike", "impact", "crash", "bang"))
        {
            effects.Add(new SoundEffect(
                EffectId: $"sfx_impact_{sceneIndex}",
                Type: SoundEffectType.Action,
                Description: "Impact sound",
                Timestamp: startTime + TimeSpan.FromSeconds(2),
                Duration: TimeSpan.FromMilliseconds(300),
                Volume: 70,
                Purpose: "Emphasize impact",
                FilePath: "/sfx/impact_heavy.wav"
            ));
        }

        // Nature/ambient sounds
        if (ContainsKeywords(sceneLower, "nature", "outdoor", "forest", "water", "wind"))
        {
            effects.Add(new SoundEffect(
                EffectId: $"sfx_ambient_{sceneIndex}",
                Type: SoundEffectType.Ambient,
                Description: "Nature ambient sound",
                Timestamp: startTime,
                Duration: duration,
                Volume: 25,
                Purpose: "Environmental atmosphere",
                FilePath: "/sfx/ambient_nature.wav"
            ));
        }

        return effects;
    }

    /// <summary>
    /// Generates transition effects between scenes
    /// </summary>
    private List<SoundEffect> GenerateTransitionEffects(int sceneCount, List<TimeSpan>? sceneDurations)
    {
        var effects = new List<SoundEffect>();
        var currentTime = TimeSpan.Zero;

        for (int i = 0; i < sceneCount - 1; i++)
        {
            if (sceneDurations != null && i < sceneDurations.Count)
            {
                currentTime += sceneDurations[i];
            }
            else
            {
                currentTime += TimeSpan.FromSeconds(10);
            }

            // Add a subtle transition sound between scenes
            effects.Add(new SoundEffect(
                EffectId: $"sfx_transition_{i}",
                Type: SoundEffectType.Transition,
                Description: "Scene transition whoosh",
                Timestamp: currentTime - TimeSpan.FromMilliseconds(200),
                Duration: TimeSpan.FromMilliseconds(400),
                Volume: 40,
                Purpose: "Smooth scene transition",
                FilePath: "/sfx/transition_whoosh.wav"
            ));
        }

        return effects;
    }

    /// <summary>
    /// Checks if text contains any of the given keywords
    /// </summary>
    private bool ContainsKeywords(string text, params string[] keywords)
    {
        return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Splits script into scenes
    /// </summary>
    private List<string> SplitScriptIntoScenes(string script)
    {
        // Split by double newlines (scene breaks) or numbered scenes
        var scenes = Regex.Split(script, @"\n\n+|(?:Scene \d+:)", RegexOptions.IgnoreCase)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        if (scenes.Count == 0)
        {
            scenes.Add(script);
        }

        return scenes;
    }

    /// <summary>
    /// Optimizes sound effect timing to avoid conflicts
    /// </summary>
    public List<SoundEffect> OptimizeTiming(List<SoundEffect> effects, TimeSpan minGap = default)
    {
        if (minGap == default)
        {
            minGap = TimeSpan.FromMilliseconds(500); // Default 500ms minimum gap
        }

        var optimized = new List<SoundEffect>();
        var sorted = effects.OrderBy(e => e.Timestamp).ToList();

        foreach (var effect in sorted)
        {
            // Check if too close to previous effect
            var lastEffect = optimized.LastOrDefault();
            if (lastEffect != null)
            {
                var gap = effect.Timestamp - (lastEffect.Timestamp + lastEffect.Duration);
                if (gap < minGap)
                {
                    // Adjust timestamp to maintain minimum gap
                    var newTimestamp = lastEffect.Timestamp + lastEffect.Duration + minGap;
                    optimized.Add(effect with { Timestamp = newTimestamp });
                    continue;
                }
            }

            optimized.Add(effect);
        }

        return optimized;
    }

    /// <summary>
    /// Suggests layered sound effects for richness
    /// </summary>
    public List<SoundEffect> GenerateLayeredEffects(SoundEffect baseEffect)
    {
        var layers = new List<SoundEffect> { baseEffect };

        // Add supporting layers based on effect type
        if (baseEffect.Type == SoundEffectType.Impact)
        {
            // Add a low rumble layer
            layers.Add(baseEffect with
            {
                EffectId = $"{baseEffect.EffectId}_rumble",
                Description = "Impact rumble layer",
                Volume = baseEffect.Volume * 0.5,
                FilePath = "/sfx/impact_rumble.wav"
            });
        }
        else if (baseEffect.Type == SoundEffectType.Whoosh)
        {
            // Add a tail/reverb layer
            layers.Add(baseEffect with
            {
                EffectId = $"{baseEffect.EffectId}_tail",
                Description = "Whoosh tail",
                Timestamp = baseEffect.Timestamp + TimeSpan.FromMilliseconds(200),
                Duration = TimeSpan.FromMilliseconds(600),
                Volume = baseEffect.Volume * 0.3,
                FilePath = "/sfx/whoosh_tail.wav"
            });
        }

        return layers;
    }
}
