using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Background service for detecting and managing Ollama installation and models with caching
/// </summary>
public class OllamaDetectionService : IHostedService, IDisposable
{
    private readonly ILogger<OllamaDetectionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly string _baseUrl;
    private readonly IServiceProvider? _serviceProvider;
    private Timer? _refreshTimer;
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(15);
    private bool _disposed;
    private volatile bool _isDetectionComplete;
    private readonly SemaphoreSlim _detectionLock = new SemaphoreSlim(1, 1);
    private Task? _initialDetectionTask;
    private bool _lastKnownStatus = false;

    private const string StatusCacheKey = "ollama:status";
    private const string ModelsCacheKey = "ollama:models";

    /// <summary>
    /// Event raised when Ollama availability status changes
    /// </summary>
    public event EventHandler<OllamaAvailabilityChangedEventArgs>? AvailabilityChanged;

    public OllamaDetectionService(
        ILogger<OllamaDetectionService> logger,
        HttpClient httpClient,
        IMemoryCache cache,
        string baseUrl = "http://localhost:11434",
        IServiceProvider? serviceProvider = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _cache = cache;
        _baseUrl = baseUrl;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Indicates whether the initial detection has completed
    /// </summary>
    public bool IsDetectionComplete => _isDetectionComplete;

    /// <summary>
    /// Start the background detection service
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OllamaDetectionService starting background detection");

        // Initial detection (fire and forget to not block startup, but save task for WaitForInitialDetectionAsync)
        _initialDetectionTask = Task.Run(async () => await RefreshDetectionAsync(CancellationToken.None).ConfigureAwait(false), cancellationToken);

        // Setup periodic refresh every 15 seconds to detect status changes quickly
        _refreshTimer = new Timer(
            async _ => await RefreshDetectionAsync(CancellationToken.None).ConfigureAwait(false),
            null,
            _refreshInterval,
            _refreshInterval);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the background detection service
    /// </summary>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OllamaDetectionService stopping background detection");
        _refreshTimer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Refresh detection of Ollama service and models
    /// </summary>
    private async Task RefreshDetectionAsync(CancellationToken ct)
    {
        try
        {
            var status = await DetectOllamaAsync(ct).ConfigureAwait(false);
            var previousStatus = _lastKnownStatus;
            _lastKnownStatus = status.IsRunning;

            // Update cache with shorter duration for faster invalidation
            _cache.Set(StatusCacheKey, status, TimeSpan.FromSeconds(20));

            if (status.IsRunning)
            {
                var models = await ListModelsAsync(ct).ConfigureAwait(false);
                _cache.Set(ModelsCacheKey, models, TimeSpan.FromSeconds(20));

                if (models.Count > 0)
                {
                    _logger.LogInformation("Ollama: {Count} models available", models.Count);
                }
                else
                {
                    _logger.LogInformation("Ollama: service running but no models installed");
                }
            }
            else
            {
                _cache.Set(ModelsCacheKey, new List<OllamaModel>(), TimeSpan.FromSeconds(20));
                _logger.LogInformation("Ollama: service not running");
            }

            // Emit event if availability status changed
            if (previousStatus != _lastKnownStatus)
            {
                _logger.LogInformation("Ollama availability changed: {PreviousStatus} -> {CurrentStatus}",
                    previousStatus ? "Available" : "Unavailable",
                    _lastKnownStatus ? "Available" : "Unavailable");

                // Invalidate OllamaLlmProvider cache when availability changes
                InvalidateProviderCache();

                AvailabilityChanged?.Invoke(this, new OllamaAvailabilityChangedEventArgs
                {
                    IsAvailable = _lastKnownStatus,
                    PreviousStatus = previousStatus,
                    Status = status,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing Ollama detection");
        }
        finally
        {
            _isDetectionComplete = true;
        }
    }

    /// <summary>
    /// Get cached Ollama status or perform fresh detection
    /// </summary>
    public async Task<OllamaStatus> GetStatusAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<OllamaStatus>(StatusCacheKey, out var cachedStatus) && cachedStatus != null)
        {
            return cachedStatus;
        }

        var status = await DetectOllamaAsync(ct).ConfigureAwait(false);
        _cache.Set(StatusCacheKey, status, TimeSpan.FromMinutes(5));
        return status;
    }

    /// <summary>
    /// Waits for the initial Ollama detection to complete, with optional timeout
    /// </summary>
    /// <param name="timeout">Maximum time to wait for detection. Default is 10 seconds.</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if detection completed within timeout, false otherwise</returns>
    public async Task<bool> WaitForInitialDetectionAsync(TimeSpan? timeout = null, CancellationToken ct = default)
    {
        timeout ??= TimeSpan.FromSeconds(10);

        if (_isDetectionComplete)
        {
            return true;
        }

        if (_initialDetectionTask == null)
        {
            _logger.LogWarning("Initial detection task not started");
            return false;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout.Value);

            await _initialDetectionTask.WaitAsync(cts.Token).ConfigureAwait(false);
            return _isDetectionComplete;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Ollama detection did not complete within {Timeout}s timeout", timeout.Value.TotalSeconds);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error waiting for Ollama detection");
            return false;
        }
    }

    /// <summary>
    /// Get cached list of models or fetch fresh list
    /// </summary>
    public async Task<List<OllamaModel>> GetModelsAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue<List<OllamaModel>>(ModelsCacheKey, out var cachedModels) && cachedModels != null)
        {
            return cachedModels;
        }

        var models = await ListModelsAsync(ct).ConfigureAwait(false);
        _cache.Set(ModelsCacheKey, models, TimeSpan.FromMinutes(5));
        return models;
    }

    /// <summary>
    /// Checks if Ollama service is running and accessible using multiple endpoint checks
    /// </summary>
    public async Task<OllamaStatus> DetectOllamaAsync(CancellationToken ct = default)
    {
        var endpoints = GetDetectionEndpoints();

        foreach (var endpoint in endpoints)
        {
            try
            {
                _logger.LogInformation("Checking Ollama availability at {Endpoint}", endpoint);

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var response = await _httpClient.GetAsync($"{endpoint}/api/version", cts.Token).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                    var versionDoc = JsonDocument.Parse(content);
                    var version = versionDoc.RootElement.TryGetProperty("version", out var versionProp)
                        ? versionProp.GetString() ?? "unknown"
                        : "unknown";

                    _logger.LogInformation("Ollama is running at {Endpoint}, version: {Version}", endpoint, version);

                    return new OllamaStatus(
                        IsRunning: true,
                        IsInstalled: true,
                        Version: version,
                        BaseUrl: endpoint,
                        ErrorMessage: null
                    );
                }

                _logger.LogWarning("Ollama service at {Endpoint} responded but with status code {StatusCode}", endpoint, response.StatusCode);
            }
            catch (TaskCanceledException)
            {
                _logger.LogDebug("Ollama service not responding at {Endpoint} (timeout)", endpoint);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogDebug(ex, "Cannot connect to Ollama at {Endpoint}", endpoint);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error detecting Ollama at {Endpoint}", endpoint);
            }
        }

        _logger.LogWarning("Ollama service not detected at any endpoint");
        return new OllamaStatus(
            IsRunning: false,
            IsInstalled: false,
            Version: null,
            BaseUrl: _baseUrl,
            ErrorMessage: "Ollama service not running. Please start Ollama with 'ollama serve' or install from https://ollama.com"
        );
    }

    /// <summary>
    /// Checks Ollama availability (convenience method for consistency with other dependency checks)
    /// </summary>
    public async Task<bool> CheckAvailabilityAsync(CancellationToken ct = default)
    {
        try
        {
            var status = await DetectOllamaAsync(ct).ConfigureAwait(false);
            return status.IsRunning;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error checking Ollama availability");
            return false;
        }
    }

    /// <summary>
    /// Get list of endpoints to check for Ollama detection
    /// </summary>
    private List<string> GetDetectionEndpoints()
    {
        var endpoints = new List<string>();

        if (!string.IsNullOrEmpty(_baseUrl))
        {
            endpoints.Add(_baseUrl);
        }

        if (!endpoints.Contains("http://localhost:11434"))
        {
            endpoints.Add("http://localhost:11434");
        }

        if (!endpoints.Contains("http://127.0.0.1:11434"))
        {
            endpoints.Add("http://127.0.0.1:11434");
        }

        return endpoints;
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

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags", cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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
        var models = await ListModelsAsync(ct).ConfigureAwait(false);
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

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(ct).ConfigureAwait(false);
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

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/show", content, cts.Token).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
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

    /// <summary>
    /// Invalidates the OllamaLlmProvider availability cache when availability status changes
    /// This ensures the provider re-checks availability after Ollama starts/stops
    /// </summary>
    private void InvalidateProviderCache()
    {
        if (_serviceProvider == null)
        {
            _logger.LogDebug("ServiceProvider not available, skipping provider cache invalidation");
            return;
        }

        try
        {
            // Get the Ollama provider instance from keyed services
            var ollamaProvider = _serviceProvider.GetKeyedService<Aura.Core.Providers.ILlmProvider>("Ollama");
            if (ollamaProvider != null)
            {
                // Use reflection to call InvalidateAvailabilityCache if it exists
                var providerType = ollamaProvider.GetType();
                var invalidateMethod = providerType.GetMethod("InvalidateAvailabilityCache", Array.Empty<Type>());
                
                if (invalidateMethod != null)
                {
                    invalidateMethod.Invoke(ollamaProvider, null);
                    _logger.LogDebug("Invalidated OllamaLlmProvider availability cache after status change");
                }
                else
                {
                    _logger.LogDebug("OllamaLlmProvider does not have InvalidateAvailabilityCache method");
                }
            }
            else
            {
                _logger.LogDebug("Ollama provider not found in service container");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error invalidating OllamaLlmProvider cache");
        }
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _refreshTimer?.Dispose();
            _detectionLock?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
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

/// <summary>
/// Event arguments for Ollama availability change events
/// </summary>
public class OllamaAvailabilityChangedEventArgs : EventArgs
{
    public required bool IsAvailable { get; init; }
    public required bool PreviousStatus { get; init; }
    public required OllamaStatus Status { get; init; }
    public required DateTime Timestamp { get; init; }
}
