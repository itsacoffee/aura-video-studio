using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Planner;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Planner;

/// <summary>
/// OpenAI-based planner provider (Pro feature)
/// Generates comprehensive recommendations using GPT models
/// </summary>
public class OpenAiPlannerProvider : ILlmPlannerProvider
{
    private readonly ILogger<OpenAiPlannerProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAiPlannerProvider(
        ILogger<OpenAiPlannerProvider> logger,
        HttpClient httpClient,
        string apiKey,
        string model = "gpt-3.5-turbo")
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model;

        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new ArgumentException("OpenAI API key is required", nameof(apiKey));
        }
    }

    public async Task<PlannerRecommendations> GenerateRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating planner recommendations with OpenAI (model: {Model}) for topic: {Topic}",
            _model, request.Brief.Topic);

        try
        {
            var prompt = BuildPrompt(request);

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = "You are an expert video content planner. Generate detailed, structured recommendations for video production." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 2000
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("OpenAI API error: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"OpenAI API error: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var responseText = result.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;

            _logger.LogInformation("Successfully generated recommendations with OpenAI");

            return ParseLlmResponse(responseText, request, "OpenAI");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendations with OpenAI");
            throw;
        }
    }

    private string BuildPrompt(RecommendationRequest request)
    {
        var duration = request.PlanSpec.TargetDuration.TotalMinutes;
        var sb = new StringBuilder();

        sb.AppendLine($"Create a comprehensive video production plan for the following:");
        sb.AppendLine($"Topic: {request.Brief.Topic}");
        sb.AppendLine($"Duration: {duration:F1} minutes");
        sb.AppendLine($"Audience: {request.Brief.Audience ?? "General"}");
        sb.AppendLine($"Tone: {request.Brief.Tone}");
        sb.AppendLine($"Pacing: {request.PlanSpec.Pacing}");
        sb.AppendLine($"Density: {request.PlanSpec.Density}");
        sb.AppendLine();
        sb.AppendLine("Generate a JSON response with:");
        sb.AppendLine("1. outline: A detailed markdown outline with scene breakdown");
        sb.AppendLine("2. sceneCount: Number of scenes (3-20)");
        sb.AppendLine("3. brollSuggestions: Specific B-roll footage ideas");
        sb.AppendLine("4. thumbnailPrompt: Detailed prompt for thumbnail generation");
        sb.AppendLine("5. seoTitle: Optimized video title (max 60 chars)");
        sb.AppendLine("6. seoDescription: Detailed description (150-200 words)");
        sb.AppendLine("7. tags: Array of 5-10 relevant tags");
        sb.AppendLine("8. chapters: Array of chapter timestamps and titles");
        sb.AppendLine();
        sb.AppendLine("Respond ONLY with valid JSON, no markdown formatting.");

        return sb.ToString();
    }

    private PlannerRecommendations ParseLlmResponse(string response, RecommendationRequest request, string providerName)
    {
        // Try to parse JSON from response
        try
        {
            var json = ExtractJson(response);
            var data = JsonSerializer.Deserialize<JsonElement>(json);

            var outline = data.TryGetProperty("outline", out var outlineEl) ? outlineEl.GetString() ?? "" : "";
            var sceneCount = data.TryGetProperty("sceneCount", out var scEl) ? scEl.GetInt32() : 5;
            var thumbnailPrompt = data.TryGetProperty("thumbnailPrompt", out var tpEl) ? tpEl.GetString() ?? "" : "";
            var seoTitle = data.TryGetProperty("seoTitle", out var stEl) ? stEl.GetString() ?? "" : "";
            var seoDescription = data.TryGetProperty("seoDescription", out var sdEl) ? sdEl.GetString() ?? "" : "";

            var tags = Array.Empty<string>();
            if (data.TryGetProperty("tags", out var tagsEl) && tagsEl.ValueKind == JsonValueKind.Array)
            {
                tags = tagsEl.EnumerateArray()
                    .Select(t => t.GetString() ?? "")
                    .Where(t => !string.IsNullOrEmpty(t))
                    .ToArray();
            }

            // Use heuristics for other fields
            var heuristic = new HeuristicRecommendationService(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<HeuristicRecommendationService>.Instance);
            var heuristicResult = heuristic.GenerateRecommendationsAsync(request, CancellationToken.None).Result;

            return new PlannerRecommendations(
                Outline: string.IsNullOrWhiteSpace(outline) ? heuristicResult.Outline : outline,
                SceneCount: sceneCount > 0 ? sceneCount : heuristicResult.SceneCount,
                ShotsPerScene: heuristicResult.ShotsPerScene,
                BRollPercentage: heuristicResult.BRollPercentage,
                OverlayDensity: heuristicResult.OverlayDensity,
                ReadingLevel: heuristicResult.ReadingLevel,
                Voice: heuristicResult.Voice,
                Music: heuristicResult.Music,
                Captions: heuristicResult.Captions,
                ThumbnailPrompt: string.IsNullOrWhiteSpace(thumbnailPrompt) ? heuristicResult.ThumbnailPrompt : thumbnailPrompt,
                Seo: new SeoRecommendations(
                    Title: string.IsNullOrWhiteSpace(seoTitle) ? heuristicResult.Seo.Title : seoTitle,
                    Description: string.IsNullOrWhiteSpace(seoDescription) ? heuristicResult.Seo.Description : seoDescription,
                    Tags: tags.Length > 0 ? tags : heuristicResult.Seo.Tags),
                QualityScore: 0.85,
                ProviderUsed: providerName,
                ExplainabilityNotes: $"Generated using {providerName} LLM with deterministic prompt templates. Combined LLM content generation with heuristic recommendations for technical parameters.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse LLM response as JSON, falling back to text parsing");

            // Fallback: use the response as outline
            var heuristic = new HeuristicRecommendationService(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<HeuristicRecommendationService>.Instance);
            var heuristicResult = heuristic.GenerateRecommendationsAsync(request, CancellationToken.None).Result;

            return heuristicResult with
            {
                Outline = response,
                QualityScore = 0.80,
                ProviderUsed = providerName,
                ExplainabilityNotes = $"Generated using {providerName} LLM (text mode). Used LLM output for outline, heuristics for other parameters."
            };
        }
    }

    private string ExtractJson(string response)
    {
        // Remove markdown code blocks if present
        var cleaned = response.Trim();
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(7);
        }
        else if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(3);
        }

        if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned.Substring(0, cleaned.Length - 3);
        }

        return cleaned.Trim();
    }
}
