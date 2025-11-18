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

namespace Aura.Core.Services.Localization;

/// <summary>
/// LLM-assisted language naturalization service.
/// Adapts scripts to target locales with cultural references, idioms, and appropriate phrasing.
/// Supports hundreds of dialects and less common languages through LLM capabilities.
/// </summary>
public class LanguageNaturalizationService
{
    private readonly ILogger<LanguageNaturalizationService> _logger;
    private readonly ILlmProvider _llmProvider;

    public LanguageNaturalizationService(
        ILogger<LanguageNaturalizationService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Naturalizes script for target locale, adjusting idioms and cultural references.
    /// Supports any locale/dialect that the LLM can handle (hundreds of languages and dialects).
    /// </summary>
    public async Task<LocalizedScript> NaturalizeScriptAsync(
        Script script,
        string targetLocale,
        OrchestrationContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetLocale);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Naturalizing script to locale: {Locale} ({SceneCount} scenes)",
            targetLocale,
            script.Scenes.Count);

        // Only skip if source and target are exactly the same locale
        if (targetLocale.Equals(context.PrimaryLanguage, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Source and target locales are identical, skipping naturalization");
            return new LocalizedScript
            {
                Locale = targetLocale,
                Scenes = script.Scenes,
                NaturalizationApplied = false,
                Notes = "Source and target locales are identical"
            };
        }

        if (_llmProvider.GetType().Name.Contains("RuleBased") || _llmProvider.GetType().Name.Contains("Mock"))
        {
            _logger.LogWarning("LLM unavailable for locale '{Locale}', returning original script", targetLocale);
            return new LocalizedScript
            {
                Locale = targetLocale,
                Scenes = script.Scenes,
                NaturalizationApplied = false,
                Notes = $"Naturalization to '{targetLocale}' requires LLM provider, using original content"
            };
        }

        try
        {
            return await ApplyLlmNaturalizationAsync(script, targetLocale, context, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM naturalization failed for locale: {Locale}", targetLocale);
            return new LocalizedScript
            {
                Locale = targetLocale,
                Scenes = script.Scenes,
                NaturalizationApplied = false,
                Notes = $"Naturalization failed: {ex.Message}"
            };
        }
    }

    private async Task<LocalizedScript> ApplyLlmNaturalizationAsync(
        Script script,
        string targetLocale,
        OrchestrationContext context,
        CancellationToken ct)
    {
        var naturalizedScenes = new List<ScriptScene>();

        var batchSize = 3;
        for (int i = 0; i < script.Scenes.Count; i += batchSize)
        {
            var batch = script.Scenes.Skip(i).Take(batchSize).ToList();
            var batchResult = await NaturalizeBatchAsync(batch, targetLocale, context, ct).ConfigureAwait(false);
            
            foreach (var (scene, naturalizedText) in batchResult)
            {
                var naturalizedScene = new ScriptScene
                {
                    Number = naturalizedScenes.Count + 1,
                    Narration = naturalizedText,
                    VisualPrompt = scene.VisualPrompt,
                    Duration = scene.Duration,
                    Transition = scene.Transition,
                    ExtendedData = scene.ExtendedData
                };
                
                naturalizedScenes.Add(naturalizedScene);
            }
        }

        return new LocalizedScript
        {
            Locale = targetLocale,
            Scenes = naturalizedScenes,
            NaturalizationApplied = true,
            Notes = $"Successfully naturalized to '{targetLocale}' using LLM"
        };
    }

    private async Task<List<(ScriptScene, string)>> NaturalizeBatchAsync(
        List<ScriptScene> scenes,
        string targetLocale,
        OrchestrationContext context,
        CancellationToken ct)
    {
        var prompt = BuildNaturalizationPrompt(scenes, targetLocale, context);
        var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
        
        return ParseNaturalizationResponse(response, scenes);
    }

    private string BuildNaturalizationPrompt(List<ScriptScene> scenes, string targetLocale, OrchestrationContext context)
    {
        var scenesList = string.Join("\n\n", scenes.Select((s, i) => 
            $"Scene {i + 1}: [Scene {s.Number}]\n{s.Narration}"));

        return $@"You are an expert translator and cultural adaptation specialist fluent in hundreds of languages and dialects. Naturalize this script for the target locale.

Target Locale: {targetLocale}
IMPORTANT: Please provide the translation in the language/dialect specified by '{targetLocale}'. Support any language or dialect the user requests, including less common languages, regional dialects, and specialized linguistic variants.

Video Topic: {context.Brief.Topic}
Tone: {context.Brief.Tone}
Audience: {context.Brief.Audience ?? "General"}

Cultural Adaptation Guidelines:
1. Translate the text to '{targetLocale}' (language, dialect, or regional variant)
2. Adjust idioms and expressions to be culturally appropriate for this locale
3. Replace culture-specific references with local equivalents
4. Maintain technical accuracy and core message
5. Keep natural, conversational tone appropriate for the locale
6. Preserve the intent and emotional impact of the original
7. For dialects and regional variants, use authentic local phrasing

Scenes to naturalize:
{scenesList}

Respond with JSON:
{{
  ""naturalizedScenes"": [
    {{
      ""sceneIndex"": 0,
      ""naturalizedText"": ""Adapted text in target locale '{targetLocale}'"",
      ""notes"": ""What cultural/linguistic adaptations were made""
    }}
  ]
}}";
    }

    private List<(ScriptScene, string)> ParseNaturalizationResponse(string response, List<ScriptScene> originalScenes)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);
                var naturalizationData = JsonSerializer.Deserialize<NaturalizationResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (naturalizationData?.NaturalizedScenes != null && 
                    naturalizationData.NaturalizedScenes.Count > 0)
                {
                    var results = new List<(ScriptScene, string)>();
                    
                    foreach (var sceneData in naturalizationData.NaturalizedScenes)
                    {
                        if (sceneData.SceneIndex >= 0 && sceneData.SceneIndex < originalScenes.Count)
                        {
                            var originalScene = originalScenes[sceneData.SceneIndex];
                            var naturalizedText = sceneData.NaturalizedText ?? originalScene.Narration;
                            results.Add((originalScene, naturalizedText));
                            
                            if (!string.IsNullOrWhiteSpace(sceneData.Notes))
                            {
                                _logger.LogDebug("Scene {Index} adaptation: {Notes}", 
                                    sceneData.SceneIndex, sceneData.Notes);
                            }
                        }
                    }

                    if (results.Count > 0)
                    {
                        return results;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing naturalization response");
        }

        return originalScenes.Select(s => (s, s.Narration)).ToList();
    }

    private class NaturalizationResponse
    {
        public List<NaturalizedSceneData> NaturalizedScenes { get; set; } = new();
    }

    private class NaturalizedSceneData
    {
        public int SceneIndex { get; set; }
        public string? NaturalizedText { get; set; }
        public string? Notes { get; set; }
    }
}

/// <summary>
/// Result of script localization with naturalization applied
/// </summary>
public class LocalizedScript
{
    /// <summary>
    /// Target locale code (e.g., "es-MX", "ja-JP", "en-AU", "yi", "gd", etc.)
    /// Supports any language or dialect including less common ones
    /// </summary>
    public string Locale { get; init; } = string.Empty;

    /// <summary>
    /// Localized/naturalized scenes
    /// </summary>
    public List<ScriptScene> Scenes { get; init; } = new();

    /// <summary>
    /// Whether naturalization was actually applied
    /// </summary>
    public bool NaturalizationApplied { get; init; }

    /// <summary>
    /// Notes about the naturalization process
    /// </summary>
    public string Notes { get; init; } = string.Empty;
}
