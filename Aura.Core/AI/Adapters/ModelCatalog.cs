using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Adapters;

/// <summary>
/// Service for discovering and managing available LLM models from providers.
/// Implements dynamic model discovery with caching and preflight validation.
/// </summary>
public class ModelCatalog
{
    private readonly ILogger<ModelCatalog> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly object _lock = new();
    private readonly Dictionary<string, CachedModelCapabilities> _capabilityCache = new();
    private readonly TimeSpan _cacheTtl = TimeSpan.FromHours(6);
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly List<ModelRegistry.ModelInfo> _dynamicModels = new();
    
    /// <summary>
    /// Cache entry for model capabilities
    /// </summary>
    private class CachedModelCapabilities
    {
        public required int MaxTokens { get; init; }
        public required int ContextWindow { get; init; }
        public required DateTime CachedAt { get; init; }
        public bool IsAvailable { get; set; }
    }

    public ModelCatalog(ILogger<ModelCatalog> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
    }

    /// <summary>
    /// Discover available models from all providers and merge with static registry
    /// </summary>
    public async Task<bool> RefreshCatalogAsync(
        Dictionary<string, string> apiKeys,
        string? ollamaBaseUrl = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting model catalog refresh");
        var discoveredCount = 0;
        
        lock (_lock)
        {
            _dynamicModels.Clear();
        }

        try
        {
            // Discover OpenAI models if API key available
            if (apiKeys.TryGetValue("openai", out var openAiKey) && !string.IsNullOrWhiteSpace(openAiKey))
            {
                var openAiModels = await DiscoverOpenAiModelsAsync(openAiKey, ct);
                lock (_lock)
                {
                    _dynamicModels.AddRange(openAiModels);
                    discoveredCount += openAiModels.Count;
                }
            }

            // Discover Anthropic models if API key available
            if (apiKeys.TryGetValue("anthropic", out var anthropicKey) && !string.IsNullOrWhiteSpace(anthropicKey))
            {
                var anthropicModels = await DiscoverAnthropicModelsAsync(anthropicKey, ct);
                lock (_lock)
                {
                    _dynamicModels.AddRange(anthropicModels);
                    discoveredCount += anthropicModels.Count;
                }
            }

            // Discover Gemini models if API key available
            if (apiKeys.TryGetValue("gemini", out var geminiKey) && !string.IsNullOrWhiteSpace(geminiKey))
            {
                var geminiModels = await DiscoverGeminiModelsAsync(geminiKey, ct);
                lock (_lock)
                {
                    _dynamicModels.AddRange(geminiModels);
                    discoveredCount += geminiModels.Count;
                }
            }

            // Discover Ollama models if base URL provided
            if (!string.IsNullOrWhiteSpace(ollamaBaseUrl))
            {
                var ollamaModels = await DiscoverOllamaModelsAsync(ollamaBaseUrl, ct);
                lock (_lock)
                {
                    _dynamicModels.AddRange(ollamaModels);
                    discoveredCount += ollamaModels.Count;
                }
            }

            lock (_lock)
            {
                _lastRefresh = DateTime.UtcNow;
            }

            _logger.LogInformation("Model catalog refresh completed. Discovered {Count} models", discoveredCount);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Model catalog refresh failed. Continuing with static registry only");
            return false;
        }
    }

    /// <summary>
    /// Find a model by provider and name, with fallback to default if not found
    /// </summary>
    public (ModelRegistry.ModelInfo? Model, string Reasoning) FindOrDefault(
        string provider,
        string? requestedModel = null)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            return (null, "Provider name is required");
        }

        // Try to find requested model if specified
        if (!string.IsNullOrWhiteSpace(requestedModel))
        {
            // Check static registry first
            var staticModel = ModelRegistry.FindModel(provider, requestedModel);
            if (staticModel != null)
            {
                var isDeprecated = staticModel.DeprecationDate.HasValue && 
                                 staticModel.DeprecationDate.Value <= DateTime.UtcNow;
                
                if (isDeprecated)
                {
                    var replacement = staticModel.ReplacementModel ?? ModelRegistry.GetDefaultModel(provider);
                    _logger.LogWarning("Model {Model} is deprecated. Using replacement: {Replacement}", 
                        requestedModel, replacement);
                    
                    return (ModelRegistry.FindModel(provider, replacement), 
                        $"Requested model '{requestedModel}' is deprecated. Using replacement '{replacement}'");
                }

                return (staticModel, $"Found requested model '{requestedModel}' in static registry");
            }

            // Check dynamic registry
            lock (_lock)
            {
                var dynamicModel = _dynamicModels.FirstOrDefault(m => 
                    m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) &&
                    (m.ModelId.Equals(requestedModel, StringComparison.OrdinalIgnoreCase) ||
                     (m.Aliases != null && m.Aliases.Any(a => a.Equals(requestedModel, StringComparison.OrdinalIgnoreCase)))));
                
                if (dynamicModel != null)
                {
                    return (dynamicModel, $"Found requested model '{requestedModel}' in dynamic registry");
                }
            }

            // Model not found, fall back to default
            _logger.LogWarning("Requested model '{Model}' not found for provider {Provider}. Falling back to default", 
                requestedModel, provider);
        }

        // Use default model for provider
        var defaultModelId = ModelRegistry.GetDefaultModel(provider);
        var defaultModel = ModelRegistry.FindModel(provider, defaultModelId);
        
        if (defaultModel != null)
        {
            var reason = string.IsNullOrWhiteSpace(requestedModel) 
                ? $"Using default model '{defaultModelId}' for provider {provider}"
                : $"Requested model '{requestedModel}' not available. Using default '{defaultModelId}'";
            
            return (defaultModel, reason);
        }

        return (null, $"No models available for provider {provider}");
    }

    /// <summary>
    /// Get all known models (static + dynamic) for a provider
    /// </summary>
    public List<ModelRegistry.ModelInfo> GetAllModels(string provider)
    {
        var models = new List<ModelRegistry.ModelInfo>();
        
        // Add static models
        models.AddRange(ModelRegistry.GetModelsForProvider(provider));
        
        // Add dynamic models
        lock (_lock)
        {
            models.AddRange(_dynamicModels.Where(m => 
                m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase)));
        }
        
        return models;
    }

    /// <summary>
    /// Get capabilities for a specific model, using cache if available
    /// </summary>
    public (int MaxTokens, int ContextWindow, bool FromCache) GetModelCapabilities(
        string provider, 
        string modelId)
    {
        var cacheKey = $"{provider}:{modelId}";
        
        lock (_lock)
        {
            if (_capabilityCache.TryGetValue(cacheKey, out var cached))
            {
                var age = DateTime.UtcNow - cached.CachedAt;
                if (age < _cacheTtl)
                {
                    return (cached.MaxTokens, cached.ContextWindow, true);
                }
                
                // Cache expired, remove it
                _capabilityCache.Remove(cacheKey);
            }
        }

        // Not in cache or expired, look up model
        var model = ModelRegistry.FindModel(provider, modelId);
        if (model == null)
        {
            lock (_lock)
            {
                model = _dynamicModels.FirstOrDefault(m => 
                    m.Provider.Equals(provider, StringComparison.OrdinalIgnoreCase) &&
                    m.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));
            }
        }

        int maxTokens;
        int contextWindow;

        if (model != null)
        {
            maxTokens = model.MaxTokens;
            contextWindow = model.ContextWindow;
        }
        else
        {
            // Fallback to estimation
            (maxTokens, contextWindow) = ModelRegistry.EstimateCapabilities(modelId);
            _logger.LogWarning("Model {Provider}:{Model} not found in registry. Using estimated capabilities: {MaxTokens}/{ContextWindow}",
                provider, modelId, maxTokens, contextWindow);
        }

        // Cache the result
        lock (_lock)
        {
            _capabilityCache[cacheKey] = new CachedModelCapabilities
            {
                MaxTokens = maxTokens,
                ContextWindow = contextWindow,
                CachedAt = DateTime.UtcNow,
                IsAvailable = model != null
            };
        }

        return (maxTokens, contextWindow, false);
    }

    /// <summary>
    /// Check if model catalog needs refresh (based on TTL)
    /// </summary>
    public bool NeedsRefresh()
    {
        lock (_lock)
        {
            var age = DateTime.UtcNow - _lastRefresh;
            return age >= _cacheTtl;
        }
    }

    /// <summary>
    /// Get the timestamp of the last catalog refresh
    /// </summary>
    public DateTime? GetLastRefreshTime()
    {
        lock (_lock)
        {
            return _lastRefresh == DateTime.MinValue ? null : _lastRefresh;
        }
    }

    /// <summary>
    /// Discover available OpenAI models
    /// </summary>
    private async Task<List<ModelRegistry.ModelInfo>> DiscoverOpenAiModelsAsync(
        string apiKey, 
        CancellationToken ct)
    {
        var models = new List<ModelRegistry.ModelInfo>();
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var response = await httpClient.GetAsync("https://api.openai.com/v1/models", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch OpenAI models: {StatusCode}", response.StatusCode);
                return models;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("data", out var dataArray))
            {
                foreach (var item in dataArray.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idProp))
                    {
                        var modelId = idProp.GetString();
                        if (!string.IsNullOrWhiteSpace(modelId) && 
                            (modelId.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase) ||
                             modelId.StartsWith("text-", StringComparison.OrdinalIgnoreCase)))
                        {
                            var (maxTokens, contextWindow) = ModelRegistry.EstimateCapabilities(modelId);
                            models.Add(new ModelRegistry.ModelInfo
                            {
                                Provider = "OpenAI",
                                ModelId = modelId,
                                MaxTokens = maxTokens,
                                ContextWindow = contextWindow
                            });
                        }
                    }
                }
            }

            _logger.LogInformation("Discovered {Count} OpenAI models", models.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover OpenAI models. Using static registry");
        }

        return models;
    }

    /// <summary>
    /// Discover available Anthropic models
    /// </summary>
    private async Task<List<ModelRegistry.ModelInfo>> DiscoverAnthropicModelsAsync(
        string apiKey,
        CancellationToken ct)
    {
        var models = new List<ModelRegistry.ModelInfo>();
        
        try
        {
            // Anthropic doesn't have a public models list API yet
            // Returns empty list to rely on static registry
            _logger.LogDebug("Anthropic model discovery not implemented. Relying on static registry");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover Anthropic models");
        }

        return models;
    }

    /// <summary>
    /// Discover available Gemini models
    /// </summary>
    private async Task<List<ModelRegistry.ModelInfo>> DiscoverGeminiModelsAsync(
        string apiKey,
        CancellationToken ct)
    {
        var models = new List<ModelRegistry.ModelInfo>();
        
        try
        {
            // Gemini uses a different API pattern
            // Returns empty list to rely on static registry
            _logger.LogDebug("Gemini model discovery not implemented. Relying on static registry");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover Gemini models");
        }

        return models;
    }

    /// <summary>
    /// Discover available Ollama models via /api/tags endpoint
    /// </summary>
    private async Task<List<ModelRegistry.ModelInfo>> DiscoverOllamaModelsAsync(
        string baseUrl,
        CancellationToken ct)
    {
        var models = new List<ModelRegistry.ModelInfo>();
        
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = await httpClient.GetAsync($"{baseUrl}/api/tags", ct);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch Ollama models from {Url}: {StatusCode}", 
                    baseUrl, response.StatusCode);
                return models;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("models", out var modelsArray))
            {
                foreach (var item in modelsArray.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var nameProp))
                    {
                        var modelName = nameProp.GetString();
                        if (!string.IsNullOrWhiteSpace(modelName))
                        {
                            var (maxTokens, contextWindow) = ModelRegistry.EstimateCapabilities(modelName);
                            models.Add(new ModelRegistry.ModelInfo
                            {
                                Provider = "Ollama",
                                ModelId = modelName,
                                MaxTokens = maxTokens,
                                ContextWindow = contextWindow
                            });
                        }
                    }
                }
            }

            _logger.LogInformation("Discovered {Count} Ollama models from {Url}", models.Count, baseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to discover Ollama models from {Url}", baseUrl);
        }

        return models;
    }

    /// <summary>
    /// Perform preflight validation to check model availability
    /// </summary>
    public async Task<Dictionary<string, bool>> PreflightCheckAsync(
        Dictionary<string, string> providersToCheck,
        Dictionary<string, string> apiKeys,
        string? ollamaBaseUrl = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting preflight model availability check for {Count} providers", 
            providersToCheck.Count);
        
        var results = new Dictionary<string, bool>();

        foreach (var (provider, modelId) in providersToCheck)
        {
            try
            {
                var (model, reasoning) = FindOrDefault(provider, modelId);
                var isAvailable = model != null;
                
                results[provider] = isAvailable;
                
                if (isAvailable)
                {
                    _logger.LogInformation("✓ Provider {Provider} model check passed: {Reasoning}", 
                        provider, reasoning);
                }
                else
                {
                    _logger.LogWarning("✗ Provider {Provider} model check failed: {Reasoning}", 
                        provider, reasoning);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Preflight check failed for provider {Provider}", provider);
                results[provider] = false;
            }
        }

        _logger.LogInformation("Preflight check completed. {Available}/{Total} providers available",
            results.Count(r => r.Value), results.Count);
        
        return results;
    }

    /// <summary>
    /// Test a specific model with a lightweight probe to verify availability and capabilities
    /// </summary>
    public async Task<ModelTestResult> TestModelAsync(
        string provider,
        string modelId,
        string apiKey,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Testing model {Provider}:{ModelId}", provider, modelId);

        var result = new ModelTestResult
        {
            Provider = provider,
            ModelId = modelId,
            TestedAt = DateTime.UtcNow
        };

        try
        {
            // Validate model exists in catalog
            var (model, reasoning) = FindOrDefault(provider, modelId);
            if (model == null)
            {
                result.IsAvailable = false;
                result.ErrorMessage = $"Model not found: {reasoning}";
                return result;
            }

            result.ContextWindow = model.ContextWindow;
            result.MaxTokens = model.MaxTokens;
            result.IsDeprecated = model.DeprecationDate.HasValue && 
                                 model.DeprecationDate.Value <= DateTime.UtcNow;
            result.ReplacementModel = model.ReplacementModel;

            // Perform lightweight API test based on provider
            var testPrompt = "Reply with exactly one word: OK";
            var testSuccess = false;

            switch (provider.ToLowerInvariant())
            {
                case "openai":
                    testSuccess = await TestOpenAiModelAsync(modelId, apiKey, testPrompt, ct);
                    break;
                case "anthropic":
                    testSuccess = await TestAnthropicModelAsync(modelId, apiKey, testPrompt, ct);
                    break;
                case "gemini":
                    testSuccess = await TestGeminiModelAsync(modelId, apiKey, testPrompt, ct);
                    break;
                case "ollama":
                    testSuccess = await TestOllamaModelAsync(modelId, apiKey, testPrompt, ct);
                    break;
                default:
                    result.IsAvailable = false;
                    result.ErrorMessage = $"Testing not implemented for provider: {provider}";
                    return result;
            }

            result.IsAvailable = testSuccess;
            if (!testSuccess)
            {
                result.ErrorMessage = "Model test failed - API returned error or timeout";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Model test failed for {Provider}:{ModelId}", provider, modelId);
            result.IsAvailable = false;
            result.ErrorMessage = $"Test error: {ex.Message}";
        }

        return result;
    }

    private async Task<bool> TestOpenAiModelAsync(string modelId, string apiKey, string prompt, CancellationToken ct)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            var requestBody = new
            {
                model = modelId,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 10
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "OpenAI model test failed for {ModelId}", modelId);
            return false;
        }
    }

    private async Task<bool> TestAnthropicModelAsync(string modelId, string apiKey, string prompt, CancellationToken ct)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
            httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            var requestBody = new
            {
                model = modelId,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 10
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await httpClient.PostAsync("https://api.anthropic.com/v1/messages", content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Anthropic model test failed for {ModelId}", modelId);
            return false;
        }
    }

    private async Task<bool> TestGeminiModelAsync(string modelId, string apiKey, string prompt, CancellationToken ct)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

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
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";
            var response = await httpClient.PostAsync(url, content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Gemini model test failed for {ModelId}", modelId);
            return false;
        }
    }

    private async Task<bool> TestOllamaModelAsync(string modelId, string baseUrl, string prompt, CancellationToken ct)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            var requestBody = new
            {
                model = modelId,
                prompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var url = $"{baseUrl}/api/generate";
            var response = await httpClient.PostAsync(url, content, ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama model test failed for {ModelId}", modelId);
            return false;
        }
    }
}

/// <summary>
/// Result of testing a specific model
/// </summary>
public class ModelTestResult
{
    public required string Provider { get; set; }
    public required string ModelId { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsDeprecated { get; set; }
    public string? ReplacementModel { get; set; }
    public int ContextWindow { get; set; }
    public int MaxTokens { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime TestedAt { get; set; }
}
