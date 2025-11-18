using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Generation;
using Aura.Core.Orchestrator.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Thumbnails;

/// <summary>
/// LLM-assisted service for generating thumbnail prompts and layout hints.
/// Analyzes script content to suggest visually compelling thumbnail concepts.
/// </summary>
public class ThumbnailPromptService
{
    private readonly ILogger<ThumbnailPromptService> _logger;
    private readonly ILlmProvider _llmProvider;

    public ThumbnailPromptService(
        ILogger<ThumbnailPromptService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Generates thumbnail prompt suggestions based on script and strongest scenes
    /// </summary>
    public async Task<ThumbnailSuggestion> GenerateThumbnailPromptAsync(
        Script script,
        OrchestrationContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation("Generating thumbnail prompt for video on platform: {Platform}", context.TargetPlatform);

        if (_llmProvider.GetType().Name.Contains("RuleBased") || _llmProvider.GetType().Name.Contains("Mock"))
        {
            return GenerateDeterministicThumbnail(script, context);
        }

        try
        {
            return await GenerateLlmThumbnailAsync(script, context, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM thumbnail generation failed, falling back to deterministic");
            return GenerateDeterministicThumbnail(script, context);
        }
    }

    private async Task<ThumbnailSuggestion> GenerateLlmThumbnailAsync(
        Script script,
        OrchestrationContext context,
        CancellationToken ct)
    {
        var prompt = BuildThumbnailPrompt(script, context);
        var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
        
        return ParseThumbnailResponse(response, script, context);
    }

    private string BuildThumbnailPrompt(Script script, OrchestrationContext context)
    {
        var strongestScenes = script.Scenes
            .OrderByDescending(s => s.Narration.Length)
            .Take(3)
            .Select((s, i) => $"{i + 1}. Scene {s.Number}: {s.Narration.Substring(0, Math.Min(100, s.Narration.Length))}...");

        return $@"You are a thumbnail design expert. Create compelling thumbnail concepts for this video.

{context.ToContextSummary()}

Strongest Scenes:
{string.Join("\n", strongestScenes)}

Platform-specific guidelines for {context.TargetPlatform}:
{GetPlatformThumbnailGuidelines(context.TargetPlatform)}

Generate 3 thumbnail concepts with:
1. Visual prompt (for SD/DALL-E or stock search)
2. Text overlay suggestions (if any)
3. Layout hints (composition, focal points)
4. Color palette recommendations
5. Rationale for why this thumbnail will attract clicks

Respond with JSON:
{{
  ""thumbnails"": [
    {{
      ""visualPrompt"": ""Detailed description for image generation"",
      ""textOverlay"": ""Optional text to overlay"",
      ""layout"": ""Composition description"",
      ""colorPalette"": [""#RRGGBB"", ""#RRGGBB""],
      ""rationale"": ""Why this works""
    }}
  ],
  ""recommendation"": ""Which thumbnail concept is strongest and why""
}}";
    }

    private string GetPlatformThumbnailGuidelines(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "youtube" => "- 1280x720 resolution\n- Bold text, high contrast\n- Faces and emotions perform well\n- Avoid clutter",
            "tiktok" => "- 1080x1920 (vertical)\n- Minimal text\n- Action or intrigue\n- Bright, eye-catching",
            "instagram" => "- 1080x1080 (square)\n- Aesthetic appeal\n- Brand colors\n- Lifestyle focused",
            "linkedin" => "- Professional aesthetic\n- Data visualizations work well\n- Corporate appropriate\n- Clear value proposition",
            _ => "- Clear focal point\n- High contrast\n- Minimal text\n- Eye-catching colors"
        };
    }

    private ThumbnailSuggestion ParseThumbnailResponse(string response, Script script, OrchestrationContext context)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);
                var thumbnailData = JsonSerializer.Deserialize<ThumbnailResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (thumbnailData?.Thumbnails != null && thumbnailData.Thumbnails.Count > 0)
                {
                    return new ThumbnailSuggestion
                    {
                        Concepts = thumbnailData.Thumbnails.Select(t => new ThumbnailConcept
                        {
                            VisualPrompt = t.VisualPrompt ?? "Default thumbnail",
                            TextOverlay = t.TextOverlay,
                            Layout = t.Layout ?? "Centered",
                            ColorPalette = t.ColorPalette ?? new List<string> { "#FF6B35", "#004E89" },
                            Rationale = t.Rationale ?? "Standard thumbnail concept"
                        }).ToList(),
                        Recommendation = thumbnailData.Recommendation ?? "Use first concept"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing thumbnail response");
        }

        return GenerateDeterministicThumbnail(script, context);
    }

    private ThumbnailSuggestion GenerateDeterministicThumbnail(Script script, OrchestrationContext context)
    {
        var mainTopic = context.Brief.Topic;
        var firstScene = script.Scenes.FirstOrDefault();

        return new ThumbnailSuggestion
        {
            Concepts = new List<ThumbnailConcept>
            {
                new ThumbnailConcept
                {
                    VisualPrompt = $"Professional image representing {mainTopic}, high quality, well-lit",
                    TextOverlay = mainTopic,
                    Layout = "Centered with text overlay at bottom third",
                    ColorPalette = new List<string> { "#FF6B35", "#004E89", "#FFFFFF" },
                    Rationale = "Clear representation of video topic with bold text"
                },
                new ThumbnailConcept
                {
                    VisualPrompt = firstScene != null 
                        ? $"Visual representation of scene {firstScene.Number}" 
                        : $"Abstract representation of {mainTopic}",
                    TextOverlay = null,
                    Layout = "Rule of thirds composition",
                    ColorPalette = new List<string> { "#2ECC71", "#3498DB" },
                    Rationale = "Artistic approach without text overlay"
                }
            },
            Recommendation = "First concept provides clearest communication of video content"
        };
    }

    private class ThumbnailResponse
    {
        public List<ThumbnailConceptData> Thumbnails { get; set; } = new();
        public string? Recommendation { get; set; }
    }

    private class ThumbnailConceptData
    {
        public string? VisualPrompt { get; set; }
        public string? TextOverlay { get; set; }
        public string? Layout { get; set; }
        public List<string>? ColorPalette { get; set; }
        public string? Rationale { get; set; }
    }
}

/// <summary>
/// Collection of thumbnail concept suggestions
/// </summary>
public class ThumbnailSuggestion
{
    public List<ThumbnailConcept> Concepts { get; init; } = new();
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>
/// Individual thumbnail concept with visual and design details
/// </summary>
public class ThumbnailConcept
{
    public string VisualPrompt { get; init; } = string.Empty;
    public string? TextOverlay { get; init; }
    public string Layout { get; init; } = string.Empty;
    public List<string> ColorPalette { get; init; } = new();
    public string Rationale { get; init; } = string.Empty;
}
