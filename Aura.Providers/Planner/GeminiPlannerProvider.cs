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
/// Google Gemini-based planner provider (Pro feature)
/// </summary>
public class GeminiPlannerProvider : ILlmPlannerProvider
{
    private readonly ILogger<GeminiPlannerProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiPlannerProvider(
        ILogger<GeminiPlannerProvider> logger,
        HttpClient httpClient,
        string apiKey,
        string model = "gemini-pro")
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
        _model = model;

        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new ArgumentException("Gemini API key is required", nameof(apiKey));
        }
    }

    public async Task<PlannerRecommendations> GenerateRecommendationsAsync(
        RecommendationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating planner recommendations with Gemini (model: {Model}) for topic: {Topic}",
            _model, request.Brief.Topic);

        try
        {
            var prompt = BuildPrompt(request);

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[] { new { text = prompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 2048
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1/models/{_model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsync(url, content, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("Gemini API error: {StatusCode} - {Error}", response.StatusCode, error);
                throw new HttpRequestException($"Gemini API error: {response.StatusCode}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var result = JsonSerializer.Deserialize<JsonElement>(responseJson);

            var responseText = result.GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;

            _logger.LogInformation("Successfully generated recommendations with Gemini");

            return ParseLlmResponse(responseText, request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate recommendations with Gemini");
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
        sb.AppendLine("3. thumbnailPrompt: Detailed prompt for thumbnail generation");
        sb.AppendLine("4. seoTitle: Optimized video title (max 60 chars)");
        sb.AppendLine("5. seoDescription: Detailed description (150-200 words)");
        sb.AppendLine("6. tags: Array of 5-10 relevant tags");

        return sb.ToString();
    }

    private PlannerRecommendations ParseLlmResponse(string response, RecommendationRequest request)
    {
        // Use OpenAI provider's parsing logic
        var openAiProvider = new OpenAiPlannerProvider(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<OpenAiPlannerProvider>.Instance,
            _httpClient,
            "dummy",
            "gpt-3.5-turbo");

        // Use reflection to call private ParseLlmResponse method
        var method = typeof(OpenAiPlannerProvider).GetMethod(
            "ParseLlmResponse",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (method != null)
        {
            var result = method.Invoke(openAiProvider, new object[] { response, request, "Gemini" });
            if (result is PlannerRecommendations recommendations)
            {
                return recommendations;
            }
        }

        // Fallback
        var heuristic = new HeuristicRecommendationService(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<HeuristicRecommendationService>.Instance);
        var heuristicResult = heuristic.GenerateRecommendationsAsync(request, CancellationToken.None).Result;

        return heuristicResult with
        {
            Outline = response,
            QualityScore = 0.85,
            ProviderUsed = "Gemini",
            ExplainabilityNotes = "Generated using Gemini LLM with deterministic prompt templates. Combined LLM content generation with heuristic recommendations for technical parameters."
        };
    }
}
