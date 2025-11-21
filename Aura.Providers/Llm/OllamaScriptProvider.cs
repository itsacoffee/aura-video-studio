using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Interfaces;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Models.Ollama;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm;

/// <summary>
/// Ollama-based script provider with streaming support
/// Extends BaseLlmScriptProvider to integrate with script generation workflows
/// </summary>
public class OllamaScriptProvider : BaseLlmScriptProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _model;
    private readonly TimeSpan _timeout;

    public OllamaScriptProvider(
        ILogger<OllamaScriptProvider> logger,
        HttpClient httpClient,
        string baseUrl = "http://127.0.0.1:11434",
        string model = "llama3.1:8b-q4_k_m",
        int maxRetries = 3,
        int timeoutSeconds = 120)
        : base(logger, maxRetries, baseRetryDelayMs: 1000)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = ValidateBaseUrl(baseUrl);
        _model = model;
        _timeout = TimeSpan.FromSeconds(timeoutSeconds);

        _logger.LogInformation("OllamaScriptProvider initialized with baseUrl={BaseUrl}, model={Model}", 
            _baseUrl, _model);
    }

    /// <summary>
    /// Validate and normalize the base URL
    /// </summary>
    private static string ValidateBaseUrl(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "http://127.0.0.1:11434";
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid base URL: {baseUrl}", nameof(baseUrl));
        }

        return baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Core script generation logic using Ollama
    /// </summary>
    protected override async Task<Script> GenerateScriptCoreAsync(
        ScriptGenerationRequest request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating script with Ollama for topic: {Topic}", request.Brief.Topic);

        var isAvailable = await IsServiceAvailableAsync(cancellationToken).ConfigureAwait(false);
        if (!isAvailable)
        {
            throw new InvalidOperationException(
                $"Cannot connect to Ollama at {_baseUrl}. Please ensure Ollama is running: 'ollama serve'");
        }

        var startTime = DateTime.UtcNow;
        var prompt = BuildPrompt(request);
        var model = request.ModelOverride ?? _model;

        var requestBody = new
        {
            model = model,
            prompt = prompt,
            stream = false,
            options = new
            {
                temperature = request.TemperatureOverride ?? 0.7,
                top_p = 0.9,
                num_predict = 2048
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_timeout);

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/generate", content, cts.Token)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (errorContent.Contains("model") && errorContent.Contains("not found"))
            {
                throw new InvalidOperationException(
                    $"Model '{model}' not found. Please pull the model first using: ollama pull {model}");
            }
            response.EnsureSuccessStatusCode();
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var responseDoc = JsonDocument.Parse(responseJson);

        if (!responseDoc.RootElement.TryGetProperty("response", out var responseText))
        {
            throw new InvalidOperationException("Invalid response from Ollama: missing 'response' field");
        }

        var scriptText = responseText.GetString() ?? string.Empty;
        var duration = DateTime.UtcNow - startTime;

        _logger.LogInformation("Script generated successfully ({Length} characters) in {Duration}s",
            scriptText.Length, duration.TotalSeconds);

        var scenes = ParseScriptIntoScenes(scriptText, request.PlanSpec);

        return new Script
        {
            Title = request.Brief.Topic,
            Scenes = scenes,
            TotalDuration = scenes.Count > 0
                ? TimeSpan.FromSeconds(scenes.Sum(s => s.Duration.TotalSeconds))
                : request.PlanSpec.TargetDuration,
            Metadata = new ScriptMetadata
            {
                ProviderName = "Ollama",
                ModelUsed = model,
                GenerationTime = duration,
                TokensUsed = EstimateTokens(scriptText),
                Tier = ProviderTier.Free
            }
        };
    }

    /// <summary>
    /// Stream-based script generation with real-time updates
    /// </summary>
    public override async IAsyncEnumerable<ScriptGenerationProgress> StreamGenerateAsync(
        ScriptGenerationRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting streaming generation with Ollama for topic: {Topic}", 
            request.Brief.Topic);

        var isAvailable = await IsServiceAvailableAsync(cancellationToken).ConfigureAwait(false);
        if (!isAvailable)
        {
            throw new InvalidOperationException(
                $"Cannot connect to Ollama at {_baseUrl}. Please ensure Ollama is running: 'ollama serve'");
        }

        var prompt = BuildPrompt(request);
        var model = request.ModelOverride ?? _model;

        var requestBody = new
        {
            model = model,
            prompt = prompt,
            stream = true,
            options = new
            {
                temperature = request.TemperatureOverride ?? 0.7,
                top_p = 0.9,
                num_predict = 2048
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
        {
            Content = content
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to Ollama at {BaseUrl}", _baseUrl);
            throw new InvalidOperationException(
                $"Cannot connect to Ollama at {_baseUrl}. Please ensure Ollama is running: 'ollama serve'", ex);
        }

        using (response)
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var reader = new StreamReader(stream);

            var buffer = new StringBuilder();
            var tokenCount = 0;

            while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync().ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                OllamaStreamResponse? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<OllamaStreamResponse>(line);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse streaming JSON line: {Line}", line);
                    continue;
                }

                if (chunk == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(chunk.Response))
                {
                    buffer.Append(chunk.Response);
                    tokenCount++;

                    const int expectedMaxTokens = 2048;
                    var percentComplete = chunk.Done ? 100 : Math.Min(95, (tokenCount * 100) / expectedMaxTokens);

                    yield return new ScriptGenerationProgress
                    {
                        Stage = "Generating",
                        PercentComplete = percentComplete,
                        PartialScript = buffer.ToString(),
                        Message = chunk.Done 
                            ? $"Generation complete ({tokenCount} tokens)"
                            : $"Generating... ({tokenCount} tokens)"
                    };
                }

                if (chunk.Done)
                {
                    _logger.LogInformation("Streaming complete. Total tokens: {Tokens}", 
                        chunk.EvalCount ?? tokenCount);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Get available models from Ollama
    /// </summary>
    public override async Task<IReadOnlyList<string>> GetAvailableModelsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching available Ollama models from {BaseUrl}/api/tags", _baseUrl);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var tagsDoc = JsonDocument.Parse(content);

            var models = new List<string>();

            if (tagsDoc.RootElement.TryGetProperty("models", out var modelsArray) &&
                modelsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var modelElement in modelsArray.EnumerateArray())
                {
                    if (modelElement.TryGetProperty("name", out var nameProp))
                    {
                        var name = nameProp.GetString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            models.Add(name);
                        }
                    }
                }
            }

            _logger.LogInformation("Found {Count} Ollama models", models.Count);
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Ollama models from {BaseUrl}", _baseUrl);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Validate provider configuration
    /// </summary>
    public override async Task<ProviderValidationResult> ValidateConfigurationAsync(CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        try
        {
            var isAvailable = await IsServiceAvailableAsync(cancellationToken).ConfigureAwait(false);
            if (!isAvailable)
            {
                errors.Add($"Cannot connect to Ollama at {_baseUrl}");
                errors.Add("Please ensure Ollama is running: 'ollama serve'");
                return new ProviderValidationResult
                {
                    IsValid = false,
                    Errors = errors,
                    Warnings = warnings
                };
            }

            var models = await GetAvailableModelsAsync(cancellationToken).ConfigureAwait(false);
            if (models.Count == 0)
            {
                warnings.Add("No models found. Please pull a model using: ollama pull llama3.1");
            }
            else if (!models.Contains(_model))
            {
                warnings.Add($"Configured model '{_model}' not found. Available models: {string.Join(", ", models)}");
            }

            return new ProviderValidationResult
            {
                IsValid = true,
                Errors = errors,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Ollama configuration");
            errors.Add($"Validation failed: {ex.Message}");
            return new ProviderValidationResult
            {
                IsValid = false,
                Errors = errors,
                Warnings = warnings
            };
        }
    }

    /// <summary>
    /// Get provider metadata
    /// </summary>
    public override ProviderMetadata GetProviderMetadata()
    {
        return new ProviderMetadata
        {
            Name = "Ollama",
            Tier = ProviderTier.Free,
            RequiresInternet = false,
            RequiresApiKey = false,
            Capabilities = new List<string> { "streaming", "local-execution", "offline" },
            DefaultModel = _model,
            EstimatedCostPer1KTokens = 0m
        };
    }

    /// <summary>
    /// Check if Ollama service is available
    /// </summary>
    public override async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
    {
        return await IsServiceAvailableAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Check if Ollama service is available at the configured base URL
    /// </summary>
    private async Task<bool> IsServiceAvailableAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/version", cts.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Build prompt from request
    /// </summary>
    private string BuildPrompt(ScriptGenerationRequest request)
    {
        var brief = request.Brief;
        var spec = request.PlanSpec;

        var systemPrompt = @"You are a professional scriptwriter creating engaging video scripts.
Follow these guidelines:
- Create clear, concise narration suitable for video
- Structure content into logical scenes
- Match the requested tone and style
- Consider the target audience
- Keep scenes focused and digestible";

        var userPrompt = $@"Create a video script for the following:

Topic: {brief.Topic}
Audience: {brief.Audience}
Goal: {brief.Goal}
Tone: {brief.Tone}
Duration: {spec.TargetDuration.TotalSeconds} seconds
Style: {spec.Style}

Please provide a well-structured script with clear narration for each scene.";

        return $"{systemPrompt}\n\n{userPrompt}";
    }
}
