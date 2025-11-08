using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Service for detecting and managing Ollama installation and models
/// </summary>
public class OllamaDetectionService
{
    private readonly ILogger<OllamaDetectionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public OllamaDetectionService(
        ILogger<OllamaDetectionService> logger,
        HttpClient httpClient,
        string baseUrl = "http://localhost:11434")
    {
        _logger = logger;
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Checks if Ollama service is running and accessible
    /// </summary>
    public async Task<OllamaStatus> DetectOllamaAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Checking Ollama availability at {BaseUrl}", _baseUrl);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/version", cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(ct);
                var versionDoc = JsonDocument.Parse(content);
                var version = versionDoc.RootElement.TryGetProperty("version", out var versionProp)
                    ? versionProp.GetString() ?? "unknown"
                    : "unknown";

                _logger.LogInformation("Ollama is running at {BaseUrl}, version: {Version}", _baseUrl, version);
                
                return new OllamaStatus(
                    IsRunning: true,
                    IsInstalled: true,
                    Version: version,
                    BaseUrl: _baseUrl,
                    ErrorMessage: null
                );
            }

            _logger.LogWarning("Ollama service responded but with status code {StatusCode}", response.StatusCode);
            return new OllamaStatus(
                IsRunning: false,
                IsInstalled: true,
                Version: null,
                BaseUrl: _baseUrl,
                ErrorMessage: $"Ollama responded with status code {response.StatusCode}"
            );
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Ollama service not responding at {BaseUrl} (timeout)", _baseUrl);
            return new OllamaStatus(
                IsRunning: false,
                IsInstalled: false,
                Version: null,
                BaseUrl: _baseUrl,
                ErrorMessage: "Ollama service not responding (timeout)"
            );
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Cannot connect to Ollama at {BaseUrl}", _baseUrl);
            return new OllamaStatus(
                IsRunning: false,
                IsInstalled: false,
                Version: null,
                BaseUrl: _baseUrl,
                ErrorMessage: $"Cannot connect to Ollama: {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting Ollama at {BaseUrl}", _baseUrl);
            return new OllamaStatus(
                IsRunning: false,
                IsInstalled: false,
                Version: null,
                BaseUrl: _baseUrl,
                ErrorMessage: $"Error detecting Ollama: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Lists all available models from Ollama
    /// </summary>
    public async Task<List<OllamaModel>> ListModelsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching Ollama models from {BaseUrl}/api/tags", _baseUrl);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cts.Token);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var tagsDoc = JsonDocument.Parse(content);

            var models = new List<OllamaModel>();

            if (tagsDoc.RootElement.TryGetProperty("models", out var modelsArray) &&
                modelsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var modelElement in modelsArray.EnumerateArray())
                {
                    var name = modelElement.TryGetProperty("name", out var nameProp)
                        ? nameProp.GetString() ?? ""
                        : "";
                    
                    var size = modelElement.TryGetProperty("size", out var sizeProp)
                        ? sizeProp.GetInt64()
                        : 0;

                    var modifiedAt = modelElement.TryGetProperty("modified_at", out var modifiedProp)
                        ? modifiedProp.GetString()
                        : null;

                    var digest = modelElement.TryGetProperty("digest", out var digestProp)
                        ? digestProp.GetString()
                        : null;

                    if (!string.IsNullOrEmpty(name))
                    {
                        models.Add(new OllamaModel(
                            Name: name,
                            Size: size,
                            ModifiedAt: modifiedAt,
                            Digest: digest
                        ));
                    }
                }
            }

            _logger.LogInformation("Found {Count} Ollama models", models.Count);
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Ollama models");
            return new List<OllamaModel>();
        }
    }

    /// <summary>
    /// Checks if a specific model is available locally
    /// </summary>
    public async Task<bool> IsModelAvailableAsync(string modelName, CancellationToken ct = default)
    {
        var models = await ListModelsAsync(ct);
        return models.Any(m => m.Name.Equals(modelName, StringComparison.OrdinalIgnoreCase) ||
                              m.Name.StartsWith(modelName + ":", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Pulls a model from the Ollama library (streaming operation)
    /// </summary>
    public async Task<bool> PullModelAsync(
        string modelName, 
        IProgress<OllamaPullProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Pulling Ollama model: {ModelName}", modelName);

            var requestBody = new { name = modelName, stream = true };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromMinutes(30)); // Long timeout for large models

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/pull")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var progressDoc = JsonDocument.Parse(line);
                    var root = progressDoc.RootElement;

                    var status = root.TryGetProperty("status", out var statusProp)
                        ? statusProp.GetString() ?? ""
                        : "";

                    var completed = root.TryGetProperty("completed", out var completedProp)
                        ? completedProp.GetInt64()
                        : 0;

                    var total = root.TryGetProperty("total", out var totalProp)
                        ? totalProp.GetInt64()
                        : 0;

                    if (progress != null && total > 0)
                    {
                        var percentComplete = (double)completed / total * 100;
                        progress.Report(new OllamaPullProgress(
                            Status: status,
                            Completed: completed,
                            Total: total,
                            PercentComplete: percentComplete
                        ));
                    }

                    if (status.Contains("success", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Successfully pulled model: {ModelName}", modelName);
                        return true;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse progress line: {Line}", line);
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling Ollama model: {ModelName}", modelName);
            return false;
        }
    }

    /// <summary>
    /// Gets model information including context window size
    /// </summary>
    public async Task<OllamaModelInfo?> GetModelInfoAsync(string modelName, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Fetching info for Ollama model: {ModelName}", modelName);

            var requestBody = new { name = modelName };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/show", content, cts.Token);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct);
            var infoDoc = JsonDocument.Parse(responseContent);
            var root = infoDoc.RootElement;

            var parameters = root.TryGetProperty("parameters", out var paramsProp)
                ? paramsProp.GetString()
                : null;

            var modelfile = root.TryGetProperty("modelfile", out var modelfileProp)
                ? modelfileProp.GetString()
                : null;

            int? contextWindow = null;
            if (!string.IsNullOrEmpty(parameters))
            {
                var numCtxMatch = System.Text.RegularExpressions.Regex.Match(parameters, @"num_ctx\s+(\d+)");
                if (numCtxMatch.Success && int.TryParse(numCtxMatch.Groups[1].Value, out var numCtx))
                {
                    contextWindow = numCtx;
                }
            }

            return new OllamaModelInfo(
                Name: modelName,
                Parameters: parameters,
                Modelfile: modelfile,
                ContextWindow: contextWindow ?? 2048
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Ollama model info for: {ModelName}", modelName);
            return null;
        }
    }
}

/// <summary>
/// Status of Ollama installation and service
/// </summary>
public record OllamaStatus(
    bool IsRunning,
    bool IsInstalled,
    string? Version,
    string BaseUrl,
    string? ErrorMessage
);

/// <summary>
/// Information about an Ollama model
/// </summary>
public record OllamaModel(
    string Name,
    long Size,
    string? ModifiedAt,
    string? Digest
);

/// <summary>
/// Detailed information about a model including context window
/// </summary>
public record OllamaModelInfo(
    string Name,
    string? Parameters,
    string? Modelfile,
    int ContextWindow
);

/// <summary>
/// Progress information for model pulling operation
/// </summary>
public record OllamaPullProgress(
    string Status,
    long Completed,
    long Total,
    double PercentComplete
);
