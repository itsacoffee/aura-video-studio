using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Runtime;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Engines.Ollama;

/// <summary>
/// Status of the Ollama server
/// </summary>
public record OllamaServerStatus(
    bool IsInstalled,
    bool IsRunning,
    bool IsHealthy,
    int? Port,
    int? ProcessId,
    string? Version,
    string[] AvailableModels,
    string? ErrorMessage = null
);

/// <summary>
/// Information about a recommended Ollama model
/// </summary>
public record RecommendedOllamaModel(
    string Name,
    string DisplayName,
    string Description,
    string SizeDescription,
    long SizeBytes,
    bool IsRecommended
);

/// <summary>
/// Manages the lifecycle of the Ollama server
/// </summary>
public class OllamaManager
{
    private readonly ILogger<OllamaManager> _logger;
    private readonly OllamaInstaller _installer;
    private readonly OllamaService _ollamaService;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    private const int DEFAULT_PORT = 11434;
    private const int HEALTH_CHECK_TIMEOUT_SECONDS = 30;

    /// <summary>
    /// Recommended models for video script generation
    /// </summary>
    public static readonly RecommendedOllamaModel[] RecommendedModels = new[]
    {
        new RecommendedOllamaModel(
            "llama3.2:3b",
            "Llama 3.2 (3B)",
            "Fast and efficient. Best for systems with limited resources.",
            "2.0 GB",
            2L * 1024 * 1024 * 1024,
            true
        ),
        new RecommendedOllamaModel(
            "llama3.1:8b",
            "Llama 3.1 (8B)",
            "Balanced performance and quality. Recommended for most users.",
            "4.7 GB",
            (long)(4.7 * 1024 * 1024 * 1024),
            true
        ),
        new RecommendedOllamaModel(
            "mistral:7b",
            "Mistral (7B)",
            "Excellent for creative writing and script generation.",
            "4.1 GB",
            (long)(4.1 * 1024 * 1024 * 1024),
            true
        ),
        new RecommendedOllamaModel(
            "llama3.1:70b",
            "Llama 3.1 (70B)",
            "Highest quality, requires powerful hardware (32GB+ RAM).",
            "40 GB",
            40L * 1024 * 1024 * 1024,
            false
        )
    };

    public OllamaManager(
        ILogger<OllamaManager> logger,
        OllamaInstaller installer,
        OllamaService ollamaService,
        HttpClient httpClient,
        string baseUrl = "http://localhost:11434")
    {
        _logger = logger;
        _installer = installer;
        _ollamaService = ollamaService;
        _httpClient = httpClient;
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Get the current status of the Ollama server
    /// </summary>
    public async Task<OllamaServerStatus> GetStatusAsync(CancellationToken ct = default)
    {
        bool isInstalled = _installer.IsInstalled();
        string? version = null;
        bool isRunning = false;
        bool isHealthy = false;
        int? processId = null;
        string[] availableModels = Array.Empty<string>();
        string? errorMessage = null;

        if (isInstalled)
        {
            version = await _installer.GetInstalledVersionAsync(ct).ConfigureAwait(false);
        }

        // Check if Ollama is running (either managed or external)
        var status = await _ollamaService.GetStatusAsync(_baseUrl, ct).ConfigureAwait(false);
        isRunning = status.Running;
        processId = status.Pid;

        if (isRunning)
        {
            isHealthy = await CheckHealthAsync(ct).ConfigureAwait(false);

            if (isHealthy)
            {
                try
                {
                    availableModels = await ListLocalModelsAsync(ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch models from Ollama API");
                }
            }
        }
        else if (!string.IsNullOrEmpty(status.Error))
        {
            errorMessage = status.Error;
        }

        return new OllamaServerStatus(
            IsInstalled: isInstalled,
            IsRunning: isRunning,
            IsHealthy: isHealthy,
            Port: isRunning ? DEFAULT_PORT : null,
            ProcessId: processId,
            Version: version,
            AvailableModels: availableModels,
            ErrorMessage: errorMessage
        );
    }

    /// <summary>
    /// Start the Ollama server
    /// </summary>
    public async Task<(bool Success, string Message, int? ProcessId)> StartServerAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Ollama server");

        // First check if already running
        var status = await _ollamaService.GetStatusAsync(_baseUrl, ct).ConfigureAwait(false);
        if (status.Running)
        {
            _logger.LogInformation("Ollama is already running (PID: {Pid})", status.Pid);
            return (true, "Ollama is already running", status.Pid);
        }

        // Try to find executable path
        var executablePath = _installer.GetExecutablePath();
        if (!System.IO.File.Exists(executablePath))
        {
            // Try auto-detection
            executablePath = OllamaService.FindOllamaExecutable();
            if (string.IsNullOrEmpty(executablePath))
            {
                return (false, "Ollama executable not found. Please install Ollama first.", null);
            }
        }

        var result = await _ollamaService.StartAsync(executablePath, _baseUrl, ct).ConfigureAwait(false);

        return (result.Success, result.Message, result.Pid);
    }

    /// <summary>
    /// Stop the Ollama server (only if managed by this app)
    /// </summary>
    public async Task<(bool Success, string Message)> StopServerAsync()
    {
        _logger.LogInformation("Stopping Ollama server");

        var result = await _ollamaService.StopAsync().ConfigureAwait(false);
        return (result.Success, result.Message);
    }

    /// <summary>
    /// Check if the server is healthy
    /// </summary>
    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cts.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed for Ollama");
            return false;
        }
    }

    /// <summary>
    /// List locally installed models
    /// </summary>
    public async Task<string[]> ListLocalModelsAsync(CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);

            var models = new List<string>();
            if (doc.RootElement.TryGetProperty("models", out var modelsArray))
            {
                foreach (var model in modelsArray.EnumerateArray())
                {
                    if (model.TryGetProperty("name", out var name))
                    {
                        var modelName = name.GetString();
                        if (!string.IsNullOrEmpty(modelName))
                        {
                            models.Add(modelName);
                        }
                    }
                }
            }

            return models.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error listing Ollama models");
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Delete a model from local storage
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteModelAsync(string modelName, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting Ollama model: {ModelName}", modelName);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var requestBody = new { name = modelName };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}/api/delete")
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, cts.Token).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Model {ModelName} deleted successfully", modelName);
                return (true, $"Model '{modelName}' deleted successfully");
            }

            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Failed to delete model {ModelName}: {Error}", modelName, error);
            return (false, $"Failed to delete model: {error}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Ollama model: {ModelName}", modelName);
            return (false, $"Error deleting model: {ex.Message}");
        }
    }

    /// <summary>
    /// Wait for the server to become healthy
    /// </summary>
    public async Task<bool> WaitForHealthyAsync(int timeoutSeconds = 30, CancellationToken ct = default)
    {
        _logger.LogInformation("Waiting for Ollama to become healthy (timeout: {Timeout}s)", timeoutSeconds);

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            if (await CheckHealthAsync(ct).ConfigureAwait(false))
            {
                _logger.LogInformation("Ollama is healthy");
                return true;
            }

            await Task.Delay(1000, ct).ConfigureAwait(false);
        }

        _logger.LogWarning("Ollama did not become healthy within timeout");
        return false;
    }

    /// <summary>
    /// Get recommended models based on system resources
    /// </summary>
    public RecommendedOllamaModel[] GetRecommendedModels(long availableMemoryBytes)
    {
        var models = new List<RecommendedOllamaModel>();

        foreach (var model in RecommendedModels)
        {
            // Model requires approximately 1.2x its size in memory to run
            var requiredMemory = (long)(model.SizeBytes * 1.2);
            var canRun = availableMemoryBytes >= requiredMemory;

            // Only include models that can run on this system, or mark them as not recommended
            models.Add(model with { IsRecommended = canRun && model.IsRecommended });
        }

        return models.ToArray();
    }
}
