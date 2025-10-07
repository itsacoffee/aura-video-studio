using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm;

/// <summary>
/// LLM provider that uses OpenAI API for script generation (Pro feature).
/// </summary>
public class OpenAiLlmProvider : ILlmProvider
{
    private readonly ILogger<OpenAiLlmProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public OpenAiLlmProvider(
        ILogger<OpenAiLlmProvider> logger,
        HttpClient httpClient,
        string apiKey,
        string model = "gpt-4o-mini")
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

    public async Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        _logger.LogInformation("Generating script with OpenAI (model: {Model}) for topic: {Topic}", _model, brief.Topic);

        try
        {
            // Build the system and user prompts
            string systemPrompt = BuildSystemPrompt();
            string userPrompt = BuildUserPrompt(brief, spec);

            // Call OpenAI API
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = 0.7,
                max_tokens = 2048
            };

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, ct);
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

            _logger.LogWarning("OpenAI response did not contain expected structure");
            throw new Exception("Invalid response from OpenAI");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Failed to connect to OpenAI API");
            throw new Exception("Failed to connect to OpenAI API. Check your API key and internet connection.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating script with OpenAI");
            throw;
        }
    }

    private string BuildSystemPrompt()
    {
        return @"You are an expert YouTube video script writer. Your scripts are:
- Engaging and well-structured with clear sections
- Written in natural, conversational language suitable for voiceover
- Optimized for the specified pacing and density
- Formatted with markdown headers (# for title, ## for scenes)
- Include a strong hook in the introduction and a clear call-to-action in the conclusion";
    }

    private string BuildUserPrompt(Brief brief, PlanSpec spec)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Create a YouTube video script about: {brief.Topic}");
        sb.AppendLine();
        sb.AppendLine($"Requirements:");
        sb.AppendLine($"- Target duration: {spec.TargetDuration.TotalMinutes:F1} minutes");
        sb.AppendLine($"- Tone: {brief.Tone}");
        sb.AppendLine($"- Pacing: {spec.Pacing}");
        sb.AppendLine($"- Content density: {spec.Density}");
        sb.AppendLine($"- Language: {brief.Language}");

        if (!string.IsNullOrEmpty(brief.Audience))
        {
            sb.AppendLine($"- Target audience: {brief.Audience}");
        }

        if (!string.IsNullOrEmpty(brief.Goal))
        {
            sb.AppendLine($"- Goal: {brief.Goal}");
        }

        sb.AppendLine();
        sb.AppendLine("Structure:");
        sb.AppendLine("# [Title]");
        sb.AppendLine("## Introduction");
        sb.AppendLine("[Hook and overview]");
        sb.AppendLine("## [Scene headings...]");
        sb.AppendLine("[Scene content...]");
        sb.AppendLine("## Conclusion");
        sb.AppendLine("[Summary and call-to-action]");

        return sb.ToString();
    }
}
