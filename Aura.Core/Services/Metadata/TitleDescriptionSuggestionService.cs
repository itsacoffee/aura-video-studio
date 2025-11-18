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

namespace Aura.Core.Services.Metadata;

/// <summary>
/// LLM-assisted service for generating SEO-aware titles, descriptions, and keywords.
/// Optimizes metadata for specific platforms and audience engagement.
/// </summary>
public class TitleDescriptionSuggestionService
{
    private readonly ILogger<TitleDescriptionSuggestionService> _logger;
    private readonly ILlmProvider _llmProvider;

    public TitleDescriptionSuggestionService(
        ILogger<TitleDescriptionSuggestionService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
    }

    /// <summary>
    /// Generates title, description, and keyword suggestions for video metadata
    /// </summary>
    public async Task<MetadataSuggestion> GenerateMetadataAsync(
        Script script,
        OrchestrationContext context,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Generating metadata suggestions for platform: {Platform}, language: {Language}",
            context.TargetPlatform,
            context.PrimaryLanguage);

        if (_llmProvider.GetType().Name.Contains("RuleBased") || _llmProvider.GetType().Name.Contains("Mock"))
        {
            return GenerateDeterministicMetadata(script, context);
        }

        try
        {
            return await GenerateLlmMetadataAsync(script, context, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM metadata generation failed, falling back to deterministic");
            return GenerateDeterministicMetadata(script, context);
        }
    }

    private async Task<MetadataSuggestion> GenerateLlmMetadataAsync(
        Script script,
        OrchestrationContext context,
        CancellationToken ct)
    {
        var prompt = BuildMetadataPrompt(script, context);
        var response = await _llmProvider.CompleteAsync(prompt, ct).ConfigureAwait(false);
        
        return ParseMetadataResponse(response, script, context);
    }

    private string BuildMetadataPrompt(Script script, OrchestrationContext context)
    {
        var scriptSummary = string.Join(" ", script.Scenes.Select(s => s.Narration).Take(3));
        if (scriptSummary.Length > 500)
        {
            scriptSummary = scriptSummary.Substring(0, 500) + "...";
        }

        return $@"You are an SEO and content marketing expert. Generate optimized metadata for this video.

{context.ToContextSummary()}

Script Summary:
{scriptSummary}

Platform-specific SEO guidelines for {context.TargetPlatform}:
{GetPlatformSeoGuidelines(context.TargetPlatform)}

Generate 5 alternative titles and descriptions, each with:
1. Title (optimized for clicks and SEO)
2. Short description (1-2 sentences for preview)
3. Long description (full context, includes keywords)
4. Keywords/tags
5. Rationale for this approach

Respond with JSON:
{{
  ""alternatives"": [
    {{
      ""title"": ""Engaging title under {GetTitleCharLimit(context.TargetPlatform)} chars"",
      ""shortDescription"": ""Preview text"",
      ""longDescription"": ""Full description with keywords"",
      ""keywords"": [""keyword1"", ""keyword2""],
      ""rationale"": ""Why this variant works""
    }}
  ],
  ""recommendation"": ""Which variant is strongest and why""
}}";
    }

    private string GetPlatformSeoGuidelines(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "youtube" => @"- Title: 60 chars (visible), up to 100 allowed
- Description: First 2-3 lines show in search
- Include keywords early
- Add timestamps for longer videos
- Include call-to-action and links",

            "tiktok" => @"- Title: 100 chars (but shorter is better)
- Focus on hooks and curiosity
- Use trending hashtags
- Minimal description needed",

            "instagram" => @"- Caption is more important than title
- First line should hook viewers
- Use 5-10 relevant hashtags
- Include brand hashtag",

            "linkedin" => @"- Professional, value-focused title
- Description emphasizes business value
- Use professional keywords
- Avoid clickbait",

            _ => @"- Clear, descriptive title
- Keywords in first 100 chars
- Provide context and value
- Include relevant tags"
        };
    }

    private int GetTitleCharLimit(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "youtube" => 60,
            "tiktok" => 100,
            "instagram" => 150,
            "linkedin" => 120,
            _ => 100
        };
    }

    private MetadataSuggestion ParseMetadataResponse(string response, Script script, OrchestrationContext context)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}') + 1;

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = response.Substring(jsonStart, jsonEnd - jsonStart);
                var metadataData = JsonSerializer.Deserialize<MetadataResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (metadataData?.Alternatives != null && metadataData.Alternatives.Count > 0)
                {
                    return new MetadataSuggestion
                    {
                        Alternatives = metadataData.Alternatives.Select(a => new MetadataVariant
                        {
                            Title = a.Title ?? context.Brief.Topic,
                            ShortDescription = a.ShortDescription ?? "Video description",
                            LongDescription = a.LongDescription ?? "Full video description",
                            Keywords = a.Keywords ?? new List<string>(),
                            Rationale = a.Rationale ?? "Standard metadata"
                        }).ToList(),
                        Recommendation = metadataData.Recommendation ?? "Use first variant"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing metadata response");
        }

        return GenerateDeterministicMetadata(script, context);
    }

    private MetadataSuggestion GenerateDeterministicMetadata(Script script, OrchestrationContext context)
    {
        var topic = context.Brief.Topic;
        var audience = context.Brief.Audience ?? "general audience";
        var firstScene = script.Scenes.FirstOrDefault();
        var keywords = ExtractKeywords(script);

        return new MetadataSuggestion
        {
            Alternatives = new List<MetadataVariant>
            {
                new MetadataVariant
                {
                    Title = $"{topic} - Complete Guide",
                    ShortDescription = $"Learn about {topic} in this comprehensive guide.",
                    LongDescription = $"This video covers {topic} for {audience}. " +
                        (firstScene != null ? $"We'll explore scene {firstScene.Number} and more. " : "") +
                        $"Perfect for anyone interested in {topic}.",
                    Keywords = keywords.Take(10).ToList(),
                    Rationale = "Clear, descriptive approach focusing on educational value"
                },
                new MetadataVariant
                {
                    Title = $"Everything You Need to Know About {topic}",
                    ShortDescription = $"A complete overview of {topic} explained simply.",
                    LongDescription = $"Discover {topic} explained in simple terms. " +
                        $"This video is designed for {audience} who want to understand the fundamentals and practical applications. " +
                        $"Watch to learn key concepts and actionable insights.",
                    Keywords = keywords.Take(10).ToList(),
                    Rationale = "Curiosity-driven approach emphasizing completeness"
                },
                new MetadataVariant
                {
                    Title = $"{topic}: Essential Insights",
                    ShortDescription = $"Key insights about {topic} you should know.",
                    LongDescription = $"Get essential insights on {topic}. " +
                        $"Tailored for {audience}, this video breaks down important concepts and provides practical takeaways. " +
                        $"Learn what matters most about {topic}.",
                    Keywords = keywords.Take(10).ToList(),
                    Rationale = "Value-focused approach emphasizing actionable content"
                }
            },
            Recommendation = "First variant provides balanced SEO and clarity"
        };
    }

    private List<string> ExtractKeywords(Script script)
    {
        var commonWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for",
            "of", "with", "by", "from", "up", "about", "into", "through", "during",
            "is", "are", "was", "were", "be", "been", "being", "have", "has", "had",
            "do", "does", "did", "will", "would", "should", "could", "may", "might",
            "can", "this", "that", "these", "those", "it", "its", "they", "them", "their"
        };

        var allText = string.Join(" ", script.Scenes.Select(s => s.Narration + " " + s.VisualPrompt));
        
        return allText.Split(new[] { ' ', ',', '.', '!', '?', ';', ':', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4 && !commonWords.Contains(w))
            .GroupBy(w => w.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Take(15)
            .Select(g => g.Key)
            .ToList();
    }

    private class MetadataResponse
    {
        public List<MetadataVariantData> Alternatives { get; set; } = new();
        public string? Recommendation { get; set; }
    }

    private class MetadataVariantData
    {
        public string? Title { get; set; }
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public List<string>? Keywords { get; set; }
        public string? Rationale { get; set; }
    }
}

/// <summary>
/// Collection of metadata suggestions with alternatives
/// </summary>
public class MetadataSuggestion
{
    public List<MetadataVariant> Alternatives { get; init; } = new();
    public string Recommendation { get; init; } = string.Empty;
}

/// <summary>
/// Individual metadata variant with title, descriptions, and keywords
/// </summary>
public class MetadataVariant
{
    public string Title { get; init; } = string.Empty;
    public string ShortDescription { get; init; } = string.Empty;
    public string LongDescription { get; init; } = string.Empty;
    public List<string> Keywords { get; init; } = new();
    public string Rationale { get; init; } = string.Empty;
}
