using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Aura.Core.Planner;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Planner;

/// <summary>
/// Factory for creating planner provider instances with LLM routing
/// </summary>
public class PlannerProviderFactory
{
    private readonly ILogger<PlannerProviderFactory> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _apiKeysPath;

    public PlannerProviderFactory(
        ILogger<PlannerProviderFactory> logger,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _apiKeysPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura",
            "apikeys.json");
    }

    /// <summary>
    /// Creates all available planner providers based on API keys
    /// </summary>
    public Dictionary<string, ILlmPlannerProvider> CreateAvailableProviders(ILoggerFactory loggerFactory)
    {
        var providers = new Dictionary<string, ILlmPlannerProvider>();
        var apiKeys = LoadApiKeys();

        // Always add RuleBased (free, no API key required)
        providers["RuleBased"] = CreateRuleBasedProvider(loggerFactory);
        _logger.LogInformation("Created RuleBased planner provider (always available)");

        // Try to create OpenAI provider
        if (apiKeys.TryGetValue("OpenAI", out var openAiKey) && !string.IsNullOrWhiteSpace(openAiKey))
        {
            try
            {
                var provider = CreateOpenAiProvider(loggerFactory.CreateLogger<OpenAiPlannerProvider>(), openAiKey);
                if (provider != null)
                {
                    providers["OpenAI"] = provider;
                    _logger.LogInformation("Created OpenAI planner provider (Pro)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create OpenAI planner provider");
            }
        }

        // Try to create Azure OpenAI provider
        if (apiKeys.TryGetValue("AzureOpenAI", out var azureKey) &&
            apiKeys.TryGetValue("AzureOpenAI_Endpoint", out var azureEndpoint) &&
            !string.IsNullOrWhiteSpace(azureKey) &&
            !string.IsNullOrWhiteSpace(azureEndpoint))
        {
            try
            {
                var provider = CreateAzureOpenAiProvider(
                    loggerFactory.CreateLogger<AzureOpenAiPlannerProvider>(),
                    azureKey,
                    azureEndpoint);
                if (provider != null)
                {
                    providers["Azure"] = provider;
                    _logger.LogInformation("Created Azure OpenAI planner provider (Pro)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create Azure OpenAI planner provider");
            }
        }

        // Try to create Gemini provider
        if (apiKeys.TryGetValue("Gemini", out var geminiKey) && !string.IsNullOrWhiteSpace(geminiKey))
        {
            try
            {
                var provider = CreateGeminiProvider(loggerFactory.CreateLogger<GeminiPlannerProvider>(), geminiKey);
                if (provider != null)
                {
                    providers["Gemini"] = provider;
                    _logger.LogInformation("Created Gemini planner provider (Pro)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create Gemini planner provider");
            }
        }

        _logger.LogInformation("Created {Count} planner providers: {Providers}",
            providers.Count, string.Join(", ", providers.Keys));

        return providers;
    }

    private ILlmPlannerProvider CreateRuleBasedProvider(ILoggerFactory loggerFactory)
    {
        return new HeuristicRecommendationService(
            loggerFactory.CreateLogger<HeuristicRecommendationService>());
    }

    private ILlmPlannerProvider? CreateOpenAiProvider(ILogger<OpenAiPlannerProvider> logger, string apiKey)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            return new OpenAiPlannerProvider(logger, httpClient, apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create OpenAI planner provider");
            return null;
        }
    }

    private ILlmPlannerProvider? CreateAzureOpenAiProvider(
        ILogger<AzureOpenAiPlannerProvider> logger,
        string apiKey,
        string endpoint)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            return new AzureOpenAiPlannerProvider(logger, httpClient, apiKey, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Azure OpenAI planner provider");
            return null;
        }
    }

    private ILlmPlannerProvider? CreateGeminiProvider(ILogger<GeminiPlannerProvider> logger, string apiKey)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(60);
            return new GeminiPlannerProvider(logger, httpClient, apiKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Gemini planner provider");
            return null;
        }
    }

    private Dictionary<string, string> LoadApiKeys()
    {
        try
        {
            if (!File.Exists(_apiKeysPath))
            {
                _logger.LogDebug("API keys file not found at {Path}", _apiKeysPath);
                return new Dictionary<string, string>();
            }

            var json = File.ReadAllText(_apiKeysPath);
            var keys = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return keys ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load API keys from {Path}", _apiKeysPath);
            return new Dictionary<string, string>();
        }
    }
}
