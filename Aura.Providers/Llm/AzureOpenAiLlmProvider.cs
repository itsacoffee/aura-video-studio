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
/// LLM provider that uses Azure OpenAI API for script generation (Pro feature).
/// </summary>
public class AzureOpenAiLlmProvider : ILlmProvider
{
    private readonly ILogger<AzureOpenAiLlmProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _endpoint;
    private readonly string _deploymentName;

    public AzureOpenAiLlmProvider(
        ILogger<AzureOpenAiLlmProvider> logger,
        HttpClient httpClient,
        string apiKey,
        string endpoint,
        string deploymentName = "gpt-4")
    {
        _logger = logger;
        _httpClient = httpClient;
        _apiKey = apiKey;
        _endpoint = endpoint;
        _deploymentName = deploymentName;

        if (string.IsNullOrEmpty(_apiKey))
        {
            throw new ArgumentException("Azure OpenAI API key is required", nameof(apiKey));
        }

        if (string.IsNullOrEmpty(_endpoint))
        {
            throw new ArgumentException("Azure OpenAI endpoint is required", nameof(endpoint));
        }
    }

    public async Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        _logger.LogInformation("Generating script with Azure OpenAI (deployment: {Deployment}) for topic: {Topic}", _deploymentName, brief.Topic);

        try
        {
            // Build enhanced prompts for quality content
            string systemPrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();
            string userPrompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);

            // Call Azure OpenAI API
            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7,
                max_tokens = 2048
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version=2024-02-15-preview";
            var response = await _httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var responseDoc = JsonDocument.Parse(responseJson);

            if (responseDoc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentProp))
                {
                    string script = contentProp.GetString() ?? string.Empty;
                    _logger.LogInformation("Script generated successfully ({Length} characters)", script.Length);
                    return script;
                }
            }

            _logger.LogWarning("Azure OpenAI response did not contain expected structure");
            throw new Exception("Invalid response from Azure OpenAI");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Azure OpenAI API");
            throw new Exception("Failed to connect to Azure OpenAI API. Check your API key, endpoint, and internet connection.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating script with Azure OpenAI");
            throw;
        }
    }

    public async Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(
        string sceneText,
        string? previousSceneText,
        string videoGoal,
        CancellationToken ct)
    {
        _logger.LogInformation("Analyzing scene importance with Azure OpenAI");

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

            var requestBody = new
            {
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.3,
                max_tokens = 512
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_endpoint}/openai/deployments/{_deploymentName}/chat/completions?api-version=2024-02-15-preview";
            var response = await _httpClient.PostAsync(url, content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var responseDoc = JsonDocument.Parse(responseJson);

            if (responseDoc.RootElement.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                if (firstChoice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var contentProp))
                {
                    var analysisText = contentProp.GetString() ?? string.Empty;
                    
                    try
                    {
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
                        _logger.LogWarning(ex, "Failed to parse scene analysis JSON from Azure OpenAI: {Response}", analysisText);
                        return null;
                    }
                }
            }

            _logger.LogWarning("Azure OpenAI scene analysis response did not contain expected structure");
            return null;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to Azure OpenAI API for scene analysis");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing scene with Azure OpenAI");
            return null;
        }
    }

    // Removed legacy prompt building methods - now using EnhancedPromptTemplates
}
