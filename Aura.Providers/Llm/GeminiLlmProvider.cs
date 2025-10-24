using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm;

/// <summary>
/// LLM provider that uses Google Gemini API for script generation (Pro feature).
/// </summary>
public class GeminiLlmProvider : ILlmProvider
{
    private readonly ILogger<GeminiLlmProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiLlmProvider(
        ILogger<GeminiLlmProvider> logger,
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

    public async Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        _logger.LogInformation("Generating script with Gemini (model: {Model}) for topic: {Topic}", _model, brief.Topic);

        try
        {
            // Build enhanced prompt for quality content
            string systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
            string userPrompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);
            string prompt = $"{systemPrompt}\n\n{userPrompt}";

            // Call Gemini API
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 2048,
                    topP = 0.9
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var responseDoc = JsonDocument.Parse(responseJson);

            if (responseDoc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var textProp))
                    {
                        string script = textProp.GetString() ?? string.Empty;
                        _logger.LogInformation("Script generated successfully ({Length} characters)", script.Length);
                        return script;
                    }
                }
            }

            _logger.LogWarning("Gemini response did not contain expected structure");
            throw new Exception("Invalid response from Gemini");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Gemini API");
            throw new Exception("Failed to connect to Gemini API. Check your API key and internet connection.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating script with Gemini");
            throw;
        }
    }

    public async Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(
        string sceneText,
        string? previousSceneText,
        string videoGoal,
        CancellationToken ct)
    {
        _logger.LogInformation("Analyzing scene importance with Gemini");

        try
        {
            var systemPrompt = "You are a video pacing expert. Analyze scenes for optimal timing. " +
                              "Return your response ONLY as valid JSON with no additional text.";
            
            var userPrompt = $@"Analyze this scene and return JSON with:
- importance (0-100): How critical is this scene to the video's message
- complexity (0-100): How complex is the information presented
- emotionalIntensity (0-100): Emotional impact level
- informationDensity (""low""|""medium""|""high""): Amount of information
- optimalDurationSeconds (number): Recommended duration in seconds
- transitionType (""cut""|""fade""|""dissolve""): Recommended transition
- reasoning (string): Brief explanation

Scene: {sceneText}
{(previousSceneText != null ? $"Previous scene: {previousSceneText}" : "")}
Video goal: {videoGoal}

Respond with ONLY the JSON object, no other text:";

            var prompt = $"{systemPrompt}\n\n{userPrompt}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.3,
                    maxOutputTokens = 512,
                    topP = 0.9
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
            var response = await _httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var responseDoc = JsonDocument.Parse(responseJson);

            if (responseDoc.RootElement.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0)
            {
                var firstCandidate = candidates[0];
                if (firstCandidate.TryGetProperty("content", out var contentObj) &&
                    contentObj.TryGetProperty("parts", out var parts) &&
                    parts.GetArrayLength() > 0)
                {
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("text", out var textProp))
                    {
                        var analysisText = textProp.GetString() ?? string.Empty;
                        
                        try
                        {
                            // Clean up potential markdown code blocks
                            analysisText = analysisText.Trim();
                            if (analysisText.StartsWith("```json"))
                            {
                                analysisText = analysisText.Substring(7);
                            }
                            if (analysisText.StartsWith("```"))
                            {
                                analysisText = analysisText.Substring(3);
                            }
                            if (analysisText.EndsWith("```"))
                            {
                                analysisText = analysisText.Substring(0, analysisText.Length - 3);
                            }
                            analysisText = analysisText.Trim();

                            var analysisDoc = JsonDocument.Parse(analysisText);
                            var root = analysisDoc.RootElement;

                            var result = new SceneAnalysisResult(
                                Importance: root.TryGetProperty("importance", out var imp) ? imp.GetDouble() : 50.0,
                                Complexity: root.TryGetProperty("complexity", out var comp) ? comp.GetDouble() : 50.0,
                                EmotionalIntensity: root.TryGetProperty("emotionalIntensity", out var emo) ? emo.GetDouble() : 50.0,
                                InformationDensity: root.TryGetProperty("informationDensity", out var info) ? info.GetString() ?? "medium" : "medium",
                                OptimalDurationSeconds: root.TryGetProperty("optimalDurationSeconds", out var dur) ? dur.GetDouble() : 10.0,
                                TransitionType: root.TryGetProperty("transitionType", out var trans) ? trans.GetString() ?? "cut" : "cut",
                                Reasoning: root.TryGetProperty("reasoning", out var reas) ? reas.GetString() ?? "" : ""
                            );

                            _logger.LogInformation("Scene analysis complete. Importance: {Importance}, Complexity: {Complexity}", 
                                result.Importance, result.Complexity);
                            
                            return result;
                        }
                        catch (JsonException ex)
                        {
                            _logger.LogWarning(ex, "Failed to parse scene analysis JSON from Gemini: {Response}", analysisText);
                            return null;
                        }
                    }
                }
            }

            _logger.LogWarning("Gemini scene analysis response did not contain expected structure");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Gemini API for scene analysis");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing scene with Gemini");
            return null;
        }
    }

    // Removed legacy prompt building method - now using EnhancedPromptTemplates
}
