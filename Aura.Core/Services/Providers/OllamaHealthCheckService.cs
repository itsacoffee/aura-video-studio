using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Service for performing health checks on Ollama instance with detailed status reporting
/// </summary>
public class OllamaHealthCheckService
{
    private readonly ILogger<OllamaHealthCheckService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;
    
    private const string HealthCacheKey = "ollama:health";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    public OllamaHealthCheckService(
        ILogger<OllamaHealthCheckService> logger,
        HttpClient httpClient,
        IMemoryCache cache,
        string baseUrl = "http://localhost:11434")
    {
        _logger = logger;
        _httpClient = httpClient;
        _cache = cache;
        _baseUrl = baseUrl;
    }

    /// <summary>
    /// Perform comprehensive health check with cached results
    /// </summary>
    public async Task<OllamaHealthStatus> CheckHealthAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<OllamaHealthStatus>(HealthCacheKey, out var cachedHealth) && cachedHealth != null)
        {
            _logger.LogDebug("Returning cached Ollama health status");
            return cachedHealth;
        }

        var health = await PerformHealthCheckAsync(ct).ConfigureAwait(false);
        
        if (health.IsHealthy)
        {
            _cache.Set(HealthCacheKey, health, CacheDuration);
        }
        
        return health;
    }

    /// <summary>
    /// Perform health check without using cache
    /// </summary>
    public async Task<OllamaHealthStatus> PerformHealthCheckAsync(CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Performing Ollama health check at {BaseUrl}", _baseUrl);

            // Check version endpoint
            var versionResult = await CheckVersionEndpointAsync(ct).ConfigureAwait(false);
            if (!versionResult.Success)
            {
                return new OllamaHealthStatus(
                    IsHealthy: false,
                    Version: null,
                    AvailableModels: new List<string>(),
                    RunningModels: new List<string>(),
                    BaseUrl: _baseUrl,
                    ResponseTimeMs: (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                    ErrorMessage: versionResult.ErrorMessage,
                    LastChecked: DateTime.UtcNow
                );
            }

            // Check available models
            var modelsResult = await CheckTagsEndpointAsync(ct).ConfigureAwait(false);
            var availableModels = modelsResult.Success ? modelsResult.Models : new List<string>();

            // Check running models
            var runningResult = await CheckRunningModelsAsync(ct).ConfigureAwait(false);
            var runningModels = runningResult.Success ? runningResult.Models : new List<string>();

            var responseTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation(
                "Ollama health check complete: Version={Version}, Models={ModelsCount}, Running={RunningCount}, ResponseTime={ResponseTime}ms",
                versionResult.Version, availableModels.Count, runningModels.Count, responseTime);

            return new OllamaHealthStatus(
                IsHealthy: true,
                Version: versionResult.Version,
                AvailableModels: availableModels,
                RunningModels: runningModels,
                BaseUrl: _baseUrl,
                ResponseTimeMs: responseTime,
                ErrorMessage: null,
                LastChecked: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing Ollama health check");
            
            return new OllamaHealthStatus(
                IsHealthy: false,
                Version: null,
                AvailableModels: new List<string>(),
                RunningModels: new List<string>(),
                BaseUrl: _baseUrl,
                ResponseTimeMs: (long)(DateTime.UtcNow - startTime).TotalMilliseconds,
                ErrorMessage: $"Health check failed: {ex.Message}",
                LastChecked: DateTime.UtcNow
            );
        }
    }

    /// <summary>
    /// Check /api/version endpoint
    /// </summary>
    private async Task<VersionCheckResult> CheckVersionEndpointAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/version", cts.Token).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new VersionCheckResult(
                    Success: false,
                    Version: null,
                    ErrorMessage: $"Version endpoint returned status {response.StatusCode}"
                );
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var doc = JsonDocument.Parse(content);
            
            var version = doc.RootElement.TryGetProperty("version", out var versionProp)
                ? versionProp.GetString() ?? "unknown"
                : "unknown";

            return new VersionCheckResult(
                Success: true,
                Version: version,
                ErrorMessage: null
            );
        }
        catch (TaskCanceledException)
        {
            return new VersionCheckResult(
                Success: false,
                Version: null,
                ErrorMessage: "Version check timed out after 5 seconds"
            );
        }
        catch (HttpRequestException ex)
        {
            return new VersionCheckResult(
                Success: false,
                Version: null,
                ErrorMessage: $"Cannot connect to Ollama: {ex.Message}"
            );
        }
        catch (Exception ex)
        {
            return new VersionCheckResult(
                Success: false,
                Version: null,
                ErrorMessage: $"Version check error: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Check /api/tags endpoint for available models
    /// </summary>
    private async Task<ModelsCheckResult> CheckTagsEndpointAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cts.Token).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new ModelsCheckResult(
                    Success: false,
                    Models: new List<string>(),
                    ErrorMessage: $"Tags endpoint returned status {response.StatusCode}"
                );
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var doc = JsonDocument.Parse(content);

            var models = new List<string>();
            
            if (doc.RootElement.TryGetProperty("models", out var modelsArray) &&
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

            return new ModelsCheckResult(
                Success: true,
                Models: models,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking Ollama tags endpoint");
            
            return new ModelsCheckResult(
                Success: false,
                Models: new List<string>(),
                ErrorMessage: $"Tags check error: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Check /api/ps endpoint for running models
    /// </summary>
    private async Task<ModelsCheckResult> CheckRunningModelsAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/ps", cts.Token).ConfigureAwait(false);
            
            if (!response.IsSuccessStatusCode)
            {
                return new ModelsCheckResult(
                    Success: false,
                    Models: new List<string>(),
                    ErrorMessage: $"PS endpoint returned status {response.StatusCode}"
                );
            }

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var doc = JsonDocument.Parse(content);

            var models = new List<string>();
            
            if (doc.RootElement.TryGetProperty("models", out var modelsArray) &&
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

            return new ModelsCheckResult(
                Success: true,
                Models: models,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking running models (endpoint may not be supported)");
            
            return new ModelsCheckResult(
                Success: false,
                Models: new List<string>(),
                ErrorMessage: null
            );
        }
    }

    /// <summary>
    /// Clear the cached health status to force fresh check
    /// </summary>
    public void ClearCache()
    {
        _cache.Remove(HealthCacheKey);
    }

    private record VersionCheckResult(
        bool Success,
        string? Version,
        string? ErrorMessage
    );

    private record ModelsCheckResult(
        bool Success,
        List<string> Models,
        string? ErrorMessage
    );
}

/// <summary>
/// Health status result for Ollama service
/// </summary>
public record OllamaHealthStatus(
    bool IsHealthy,
    string? Version,
    List<string> AvailableModels,
    List<string> RunningModels,
    string BaseUrl,
    long ResponseTimeMs,
    string? ErrorMessage,
    DateTime LastChecked
);
