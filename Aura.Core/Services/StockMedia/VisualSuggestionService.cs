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

namespace Aura.Core.Services.StockMedia;

/// <summary>
/// LLM-driven service for suggesting optimal visual strategies per scene.
/// Recommends stock, generative AI, or solid color visuals based on content and context.
/// </summary>
public class VisualSuggestionService
{
    private readonly ILogger<VisualSuggestionService> _logger;
    private readonly ILlmProvider _llmProvider;

    public VisualSuggestionService(
        ILogger<VisualSuggestionService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Generates visual strategy suggestions for all scenes in a script
    /// </summary>
    public async Task<List<VisualSuggestion>> SuggestVisualsAsync(
        Script script,
        OrchestrationContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Generating visual suggestions for {SceneCount} scenes (Budget Sensitive: {BudgetSensitive}, Advanced Visuals: {AdvancedVisuals})",
            script.Scenes.Count,
            context.BudgetSensitive,
            context.UseAdvancedVisuals);

        if (_llmProvider.GetType().Name.Contains("RuleBased") || _llmProvider.GetType().Name.Contains("Mock"))
        {
            _logger.LogInformation("Using deterministic visual suggestions (LLM unavailable)");
            return GenerateDeterministicSuggestions(script, context);
        }

        try
        {
            return await GenerateLlmSuggestionsAsync(script, context, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM visual suggestions failed, falling back to deterministic");
            return GenerateDeterministicSuggestions(script, context);
        }
    }

    private async Task<List<VisualSuggestion>> GenerateLlmSuggestionsAsync(
        Script script,
        OrchestrationContext context,
        CancellationToken ct)
    {
        var batchSize = context.BudgetSensitive ? 5 : 3;
        var suggestions = new List<VisualSuggestion>();

        for (int i = 0; i < script.Scenes.Count; i += batchSize)
        {
            var batch = script.Scenes.Skip(i).Take(batchSize).ToList();
            var batchSuggestions = await ProcessSceneBatchAsync(batch, context, ct).ConfigureAwait(false);
            suggestions.AddRange(batchSuggestions);
        }

        return suggestions;
    }

    private async Task<List<VisualSuggestion>> ProcessSceneBatchAsync(
        List<ScriptScene> scenes,
        OrchestrationContext context,
        CancellationToken ct)
    {
        var prompt = BuildVisualSuggestionPrompt(scenes, context);
        var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
        
        return ParseVisualSuggestionResponse(response, scenes, context);
    }

    private string BuildVisualSuggestionPrompt(List<ScriptScene> scenes, OrchestrationContext context)
    {
        var scenesList = string.Join("\n", scenes.Select((s, i) => 
            $"{i + 1}. [Scene {s.Number}] {s.Narration.Substring(0, Math.Min(150, s.Narration.Length))}..."));

        var availableStrategies = GetAvailableStrategies(context);

        return $@"You are a visual content strategist. Recommend the best visual approach for each scene.

{context.ToContextSummary()}

Available Visual Strategies:
{string.Join("\n", availableStrategies.Select(s => $"- {s}"))}

Scenes to analyze:
{scenesList}

For each scene, recommend:
1. Strategy: ""Stock"", ""Generative"", or ""SolidColor""
2. If Stock: provide search query
3. If Generative: provide detailed SD/DALL-E prompt
4. If SolidColor: provide hex color
5. Rationale: explain why this strategy works

Respond with JSON:
{{
  ""suggestions"": [
    {{
      ""sceneIndex"": 0,
      ""strategy"": ""Stock"" | ""Generative"" | ""SolidColor"",
      ""stockQuery"": ""search terms for stock images"",
      ""sdPrompt"": ""detailed generative AI prompt"",
      ""colorHex"": ""#RRGGBB"",
      ""rationale"": ""why this works for this scene""
    }}
  ]
}}";
    }

    private List<string> GetAvailableStrategies(OrchestrationContext context)
    {
        var strategies = new List<string> { "Stock (free/paid stock images)", "SolidColor (simple colored backgrounds)" };
        
        if (context.UseAdvancedVisuals)
        {
            strategies.Insert(1, "Generative (AI-generated images via Stable Diffusion or DALL-E)");
        }

        return strategies;
    }

    private List<VisualSuggestion> ParseVisualSuggestionResponse(
        string response,
        List<ScriptScene> scenes,
        OrchestrationContext context)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);
                var suggestionData = JsonSerializer.Deserialize<VisualSuggestionResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (suggestionData?.Suggestions != null && suggestionData.Suggestions.Count > 0)
                {
                    return suggestionData.Suggestions.Select(s => new VisualSuggestion
                    {
                        SceneIndex = s.SceneIndex,
                        Strategy = s.Strategy ?? "Stock",
                        StockQuery = s.StockQuery,
                        SdPrompt = s.SdPrompt,
                        ColorHex = s.ColorHex ?? "#1a1a1a",
                        Rationale = s.Rationale ?? "Default suggestion"
                    }).ToList();
                }
            }

            _logger.LogWarning("Failed to parse LLM visual suggestion response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing visual suggestion response");
        }

        return GenerateDeterministicSuggestions(scenes, context);
    }

    private List<VisualSuggestion> GenerateDeterministicSuggestions(Script script, OrchestrationContext context)
    {
        return script.Scenes.Select((scene, index) => GenerateDeterministicSuggestion(scene, index, context)).ToList();
    }

    private List<VisualSuggestion> GenerateDeterministicSuggestions(List<ScriptScene> scenes, OrchestrationContext context)
    {
        return scenes.Select((scene, index) => GenerateDeterministicSuggestion(scene, index, context)).ToList();
    }

    private VisualSuggestion GenerateDeterministicSuggestion(ScriptScene scene, int index, OrchestrationContext context)
    {
        var keywords = ExtractKeywords(scene.Narration);
        var stockQuery = string.Join(" ", keywords.Take(3));

        var strategy = context.UseAdvancedVisuals && keywords.Count > 3 ? "Generative" : "Stock";

        return new VisualSuggestion
        {
            SceneIndex = index,
            Strategy = strategy,
            StockQuery = stockQuery,
            SdPrompt = strategy == "Generative" 
                ? $"Professional, high-quality image of {stockQuery}, cinematic lighting, detailed" 
                : null,
            ColorHex = GetSceneColor(index),
            Rationale = $"Deterministic suggestion based on scene content analysis"
        };
    }

    private List<string> ExtractKeywords(string text)
    {
        var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "up", "about", "into", "through", "during",
            "is", "are", "was", "were", "be", "been", "being", "have", "has", "had",
            "do", "does", "did", "will", "would", "should", "could", "may", "might",
            "can", "this", "that", "these", "those", "it", "its", "they", "them"
        };

        return text.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 3 && !commonWords.Contains(w))
            .Distinct()
            .Take(5)
            .ToList();
    }

    private string GetSceneColor(int index)
    {
        var colors = new[] { "#2C3E50", "#34495E", "#7F8C8D", "#95A5A6", "#BDC3C7" };
        return colors[index % colors.Length];
    }

    private class VisualSuggestionResponse
    {
        public List<VisualSuggestionData> Suggestions { get; set; } = new();
    }

    private class VisualSuggestionData
    {
        public int SceneIndex { get; set; }
        public string? Strategy { get; set; }
        public string? StockQuery { get; set; }
        public string? SdPrompt { get; set; }
        public string? ColorHex { get; set; }
        public string? Rationale { get; set; }
    }
}

/// <summary>
/// Visual strategy recommendation for a scene
/// </summary>
public class VisualSuggestion
{
    /// <summary>
    /// Index of the scene this suggestion is for
    /// </summary>
    public int SceneIndex { get; init; }

    /// <summary>
    /// Recommended strategy: "Stock", "Generative", or "SolidColor"
    /// </summary>
    public string Strategy { get; init; } = "Stock";

    /// <summary>
    /// Search query for stock image providers (if Stock strategy)
    /// </summary>
    public string? StockQuery { get; init; }

    /// <summary>
    /// Detailed prompt for generative AI (if Generative strategy)
    /// </summary>
    public string? SdPrompt { get; init; }

    /// <summary>
    /// Hex color code for solid color background (fallback or primary)
    /// </summary>
    public string ColorHex { get; init; } = "#1a1a1a";

    /// <summary>
    /// Explanation of why this strategy was chosen
    /// </summary>
    public string Rationale { get; init; } = string.Empty;
}
