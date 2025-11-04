using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.StockMedia;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.StockMedia;

/// <summary>
/// LLM-assisted service for composing provider-specific search queries
/// </summary>
public class QueryCompositionService
{
    private readonly ILogger<QueryCompositionService> _logger;
    private readonly ILlmProvider _llmProvider;

    public QueryCompositionService(
        ILogger<QueryCompositionService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Composes optimized search queries for a specific provider
    /// </summary>
    public async Task<QueryCompositionResult> ComposeQueryAsync(
        QueryCompositionRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Composing query for {Provider}: {Scene}",
            request.TargetProvider, request.SceneDescription);

        var prompt = BuildQueryCompositionPrompt(request);

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, ct);
            var result = ParseQueryCompositionResponse(response);

            _logger.LogInformation(
                "Query composition complete. Primary: '{Query}', Alternatives: {Count}",
                result.PrimaryQuery, result.AlternativeQueries.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error composing query with LLM");
            return FallbackQueryComposition(request);
        }
    }

    /// <summary>
    /// Generates blend set recommendations (stock vs generative mix)
    /// </summary>
    public async Task<BlendSetRecommendation> RecommendBlendSetAsync(
        BlendSetRequest request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Generating blend set recommendation for {SceneCount} scenes",
            request.SceneDescriptions.Count);

        var prompt = BuildBlendSetPrompt(request);

        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, ct);
            var result = ParseBlendSetResponse(response, request.SceneDescriptions.Count);

            _logger.LogInformation(
                "Blend set recommendation complete. Strategy: {Strategy}",
                result.Strategy);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating blend set recommendation");
            return FallbackBlendSetRecommendation(request);
        }
    }

    private string BuildQueryCompositionPrompt(QueryCompositionRequest request)
    {
        var providerGuidelines = GetProviderGuidelines(request.TargetProvider);

        return $@"You are an expert at composing search queries for stock media platforms.

TASK: Create optimized search queries for {request.TargetProvider} to find {request.MediaType.ToString().ToLower()} content.

SCENE DESCRIPTION:
{request.SceneDescription}

KEYWORDS: {string.Join(", ", request.Keywords)}
STYLE: {request.Style ?? "any"}
MOOD: {request.Mood ?? "any"}

PROVIDER GUIDELINES ({request.TargetProvider}):
{providerGuidelines}

Generate a response in this exact JSON format:
{{
    ""primaryQuery"": ""the main search query (3-5 keywords)"",
    ""alternativeQueries"": [""alternative 1"", ""alternative 2""],
    ""negativeFilters"": [""term to avoid"", ""another term to avoid""],
    ""reasoning"": ""brief explanation of query composition strategy"",
    ""confidence"": 0.85
}}

Focus on:
1. Use concrete, visual terms
2. Keep queries short and focused (3-5 keywords)
3. Avoid abstract concepts
4. Consider provider-specific preferences
5. Generate 2-3 alternative queries with different approaches

Respond with ONLY the JSON, no additional text.";
    }

    private string BuildBlendSetPrompt(BlendSetRequest request)
    {
        var scenesText = string.Join("\n", request.SceneDescriptions.Select((s, i) => $"{i + 1}. {s}"));

        return $@"You are an expert at optimizing visual content sourcing for video production.

TASK: Recommend the optimal mix of stock media vs AI-generated content for each scene.

VIDEO GOAL: {request.VideoGoal}
VIDEO STYLE: {request.VideoStyle}
BUDGET: ${request.Budget}

SCENES:
{scenesText}

CONSTRAINTS:
- Stock media: $0-5 per asset (free to premium)
- AI generation: $0.02-0.10 per image, slower
- Allow stock: {request.AllowStock}
- Allow generative: {request.AllowGenerative}

Generate a response in this exact JSON format:
{{
    ""strategy"": ""brief description of overall strategy"",
    ""sceneRecommendations"": [
        {{
            ""sceneIndex"": 0,
            ""useStock"": true,
            ""useGenerative"": false,
            ""preferredSource"": ""stock"",
            ""reasoning"": ""why this choice"",
            ""confidence"": 0.85
        }}
    ],
    ""estimatedCost"": 15.50,
    ""narrativeCoverageScore"": 0.92,
    ""reasoning"": ""overall strategy explanation""
}}

Consider:
1. Generic scenes (landscapes, objects) → prefer stock (cheaper, faster)
2. Unique/specific concepts → prefer generative (more control)
3. Budget constraints
4. Narrative coverage and visual consistency
5. Time constraints

Respond with ONLY the JSON, no additional text.";
    }

    private string GetProviderGuidelines(StockMediaProvider provider)
    {
        return provider switch
        {
            StockMediaProvider.Pexels => @"
- Prefers natural language queries
- Works well with descriptive phrases
- Supports color filters
- Strong in nature, business, technology categories",

            StockMediaProvider.Unsplash => @"
- Best with abstract concepts and artistic content
- Prefers single-word or 2-3 word queries
- Strong in lifestyle, nature, architecture
- High-quality curated content",

            StockMediaProvider.Pixabay => @"
- Works with both simple and detailed queries
- Strong in illustrations and vectors
- Good for specific object searches
- Supports video content",

            _ => "General stock media platform"
        };
    }

    private QueryCompositionResult ParseQueryCompositionResponse(string response)
    {
        try
        {
            var json = ExtractJson(response);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new QueryCompositionResult
            {
                PrimaryQuery = root.GetProperty("primaryQuery").GetString() ?? string.Empty,
                AlternativeQueries = root.TryGetProperty("alternativeQueries", out var alts)
                    ? alts.EnumerateArray().Select(a => a.GetString() ?? string.Empty).ToList()
                    : new List<string>(),
                NegativeFilters = root.TryGetProperty("negativeFilters", out var negs)
                    ? negs.EnumerateArray().Select(n => n.GetString() ?? string.Empty).ToList()
                    : new List<string>(),
                Reasoning = root.TryGetProperty("reasoning", out var r) ? r.GetString() ?? string.Empty : string.Empty,
                Confidence = root.TryGetProperty("confidence", out var c) ? c.GetDouble() : 0.5
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response, using fallback");
            throw;
        }
    }

    private BlendSetRecommendation ParseBlendSetResponse(string response, int sceneCount)
    {
        try
        {
            var json = ExtractJson(response);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var recommendations = new Dictionary<int, SourceRecommendation>();

            if (root.TryGetProperty("sceneRecommendations", out var scenes))
            {
                foreach (var scene in scenes.EnumerateArray())
                {
                    var index = scene.GetProperty("sceneIndex").GetInt32();
                    recommendations[index] = new SourceRecommendation
                    {
                        UseStock = scene.GetProperty("useStock").GetBoolean(),
                        UseGenerative = scene.GetProperty("useGenerative").GetBoolean(),
                        PreferredSource = scene.GetProperty("preferredSource").GetString() ?? "stock",
                        Reasoning = scene.TryGetProperty("reasoning", out var r) ? r.GetString() ?? string.Empty : string.Empty,
                        Confidence = scene.TryGetProperty("confidence", out var c) ? c.GetDouble() : 0.5
                    };
                }
            }

            return new BlendSetRecommendation
            {
                SceneRecommendations = recommendations,
                Strategy = root.TryGetProperty("strategy", out var s) ? s.GetString() ?? string.Empty : string.Empty,
                Reasoning = root.TryGetProperty("reasoning", out var reas) ? reas.GetString() ?? string.Empty : string.Empty,
                EstimatedCost = root.TryGetProperty("estimatedCost", out var cost) ? cost.GetDouble() : 0.0,
                NarrativeCoverageScore = root.TryGetProperty("narrativeCoverageScore", out var score) ? score.GetDouble() : 0.5
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse blend set response, using fallback");
            throw;
        }
    }

    private string ExtractJson(string response)
    {
        var trimmed = response.Trim();
        
        var jsonStart = trimmed.IndexOf('{');
        var jsonEnd = trimmed.LastIndexOf('}');
        
        if (jsonStart >= 0 && jsonEnd > jsonStart)
        {
            return trimmed.Substring(jsonStart, jsonEnd - jsonStart + 1);
        }

        return trimmed;
    }

    private QueryCompositionResult FallbackQueryComposition(QueryCompositionRequest request)
    {
        var keywords = request.Keywords.Take(3).ToList();
        var primaryQuery = string.Join(" ", keywords);
        
        var alternatives = new List<string>();
        
        if (keywords.Count >= 2)
        {
            alternatives.Add(string.Join(" ", keywords.Take(2)));
        }
        
        var sceneWords = request.SceneDescription.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (sceneWords.Length >= 2)
        {
            alternatives.Add(string.Join(" ", sceneWords.Take(Math.Min(3, sceneWords.Length))));
        }

        return new QueryCompositionResult
        {
            PrimaryQuery = primaryQuery,
            AlternativeQueries = alternatives,
            NegativeFilters = new List<string>(),
            Reasoning = "Fallback query composition using basic keywords",
            Confidence = 0.5
        };
    }

    private BlendSetRecommendation FallbackBlendSetRecommendation(BlendSetRequest request)
    {
        var recommendations = new Dictionary<int, SourceRecommendation>();

        for (int i = 0; i < request.SceneDescriptions.Count; i++)
        {
            recommendations[i] = new SourceRecommendation
            {
                UseStock = request.AllowStock,
                UseGenerative = request.AllowGenerative,
                PreferredSource = request.AllowStock ? "stock" : "generative",
                Reasoning = "Default recommendation due to LLM unavailability",
                Confidence = 0.5
            };
        }

        return new BlendSetRecommendation
        {
            SceneRecommendations = recommendations,
            Strategy = "Use available sources with preference for cost-effectiveness",
            Reasoning = "Fallback recommendation using default strategy",
            EstimatedCost = request.Budget * 0.5,
            NarrativeCoverageScore = 0.7
        };
    }
}
