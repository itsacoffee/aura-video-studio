using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Core.Providers;

/// <summary>
/// Settings for Ollama direct client configuration.
/// </summary>
public class OllamaSettings
{
    /// <summary>Base URL for Ollama API (default: http://127.0.0.1:11434)</summary>
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
    
    /// <summary>Default model to use if not specified</summary>
    public string? DefaultModel { get; set; }
    
    /// <summary>Timeout for requests (default: 3 minutes)</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(3);
    
    /// <summary>Maximum retry attempts (default: 3)</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Enable GPU acceleration (default: true)</summary>
    public bool GpuEnabled { get; set; } = true;

    /// <summary>Number of GPUs to use (-1 = all, 0 = CPU only)</summary>
    public int NumGpu { get; set; } = -1;

    /// <summary>Context window size (default: 4096)</summary>
    public int NumCtx { get; set; } = 4096;
}

/// <summary>
/// Direct HTTP client for Ollama API with proper dependency injection.
/// 
/// ARCHITECTURAL FIX: This replaces reflection-based access to OllamaLlmProvider.
/// Uses IHttpClientFactory for proper lifetime management and configuration.
/// Implements retry logic with Polly-style exponential backoff.
/// </summary>
public class OllamaDirectClient : IOllamaDirectClient
{
    private readonly ILogger<OllamaDirectClient> _logger;
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;

    public OllamaDirectClient(
        ILogger<OllamaDirectClient> logger,
        HttpClient httpClient,
        IOptions<OllamaSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        // Configure HttpClient from settings
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = _settings.Timeout;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(
        string model,
        string prompt,
        string? systemPrompt = null,
        OllamaGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(prompt);

        var requestBody = new OllamaGenerateRequest
        {
            Model = model,
            Prompt = prompt,
            System = systemPrompt,
            Stream = false, // Non-streaming for simplicity
            Options = options != null ? new OllamaRequestOptions
            {
                Temperature = options.Temperature,
                TopP = options.TopP,
                TopK = options.TopK,
                NumPredict = options.MaxTokens,
                RepeatPenalty = options.RepeatPenalty,
                Stop = options.Stop,
                NumGpu = options.NumGpu ?? _settings.NumGpu,
                NumCtx = options.NumCtx ?? _settings.NumCtx
            } : null
        };

        _logger.LogInformation(
            "Calling Ollama API: model={Model}, promptLength={PromptLength}, timeout={Timeout}",
            model, prompt.Length, _settings.Timeout);

        // Retry with exponential backoff
        var attempt = 0;
        var maxAttempts = _settings.MaxRetries;
        var delay = TimeSpan.FromSeconds(1);

        while (attempt < maxAttempts)
        {
            attempt++;
            
            try
            {
                var startTime = DateTime.UtcNow;
                
                // Heartbeat logging for long-running requests
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var heartbeatTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), cts.Token).ConfigureAwait(false);
                        var elapsed = DateTime.UtcNow - startTime;
                        _logger.LogDebug("Ollama request in progress... elapsed={ElapsedSeconds}s", elapsed.TotalSeconds);
                    }
                }, cts.Token);

                var response = await _httpClient.PostAsJsonAsync(
                    "/api/generate",
                    requestBody,
                    cancellationToken).ConfigureAwait(false);

                cts.Cancel(); // Stop heartbeat
                
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (result == null || string.IsNullOrEmpty(result.Response))
                {
                    throw new InvalidOperationException("Ollama returned empty response");
                }

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation(
                    "Ollama generation completed: model={Model}, duration={Duration}s, responseLength={Length}",
                    model, duration.TotalSeconds, result.Response.Length);

                return result.Response;
            }
            catch (Exception ex) when (attempt < maxAttempts && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex,
                    "Ollama request failed (attempt {Attempt}/{MaxAttempts}), retrying in {Delay}s...",
                    attempt, maxAttempts, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2); // Exponential backoff
            }
        }

        throw new InvalidOperationException(
            $"Ollama request failed after {maxAttempts} attempts. Check that Ollama is running and the model is available.");
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/version", cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama availability check failed");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/tags", cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaModelsResponse>(
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return result?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Ollama models");
            return new List<string>();
        }
    }

    #region DTOs for Ollama API

    private class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("system")]
        public string? System { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("options")]
        public OllamaRequestOptions? Options { get; set; }
    }

    private class OllamaRequestOptions
    {
        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public double? TopP { get; set; }

        [JsonPropertyName("top_k")]
        public int? TopK { get; set; }

        [JsonPropertyName("num_predict")]
        public int? NumPredict { get; set; }

        [JsonPropertyName("repeat_penalty")]
        public int? RepeatPenalty { get; set; }

        [JsonPropertyName("stop")]
        public List<string>? Stop { get; set; }

        [JsonPropertyName("num_gpu")]
        public int? NumGpu { get; set; }

        [JsonPropertyName("num_ctx")]
        public int? NumCtx { get; set; }
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string Response { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string? Model { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    private class OllamaModelsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelInfo>? Models { get; set; }
    }

    private class OllamaModelInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
