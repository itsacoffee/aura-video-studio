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

    // Removed legacy prompt building methods - now using EnhancedPromptTemplates
}
