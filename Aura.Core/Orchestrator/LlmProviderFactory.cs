using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Factory for creating and managing LLM provider instances
/// </summary>
public class LlmProviderFactory
{
    private readonly ILogger<LlmProviderFactory> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProviderSettings _providerSettings;
    private readonly string _apiKeysPath;

    public LlmProviderFactory(
        ILogger<LlmProviderFactory> logger,
        IHttpClientFactory httpClientFactory,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _providerSettings = providerSettings;
        _apiKeysPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura",
            "apikeys.json");
    }

    /// <summary>
    /// Creates all available LLM providers based on configuration
    /// </summary>
    public Dictionary<string, ILlmProvider> CreateAvailableProviders(ILoggerFactory loggerFactory)
    {
        var providers = new Dictionary<string, ILlmProvider>();

        // Always available: RuleBased provider
        try
        {
            providers["RuleBased"] = CreateRuleBasedProvider(loggerFactory);
            _logger.LogInformation("RuleBased provider registered");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create RuleBased provider");
        }

        // Try to create Ollama provider (local)
        try
        {
            var ollamaLogger = loggerFactory.CreateLogger("OllamaLlmProvider");
            var ollamaProvider = CreateOllamaProvider(ollamaLogger);
            if (ollamaProvider != null)
            {
                providers["Ollama"] = ollamaProvider;
                _logger.LogInformation("Ollama provider registered");
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama provider not available");
        }

        // Load API keys
        var apiKeys = LoadApiKeys();

        // Try to create Pro providers if API keys are available
        try
        {
            if (apiKeys.TryGetValue("openai", out var openAiKey) && !string.IsNullOrWhiteSpace(openAiKey))
            {
                var openAiLogger = loggerFactory.CreateLogger("OpenAiLlmProvider");
                var openAiProvider = CreateOpenAiProvider(openAiLogger, openAiKey);
                if (openAiProvider != null)
                {
                    providers["OpenAI"] = openAiProvider;
                    _logger.LogInformation("OpenAI provider registered");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create OpenAI provider");
        }

        try
        {
            if (apiKeys.TryGetValue("azure_openai_key", out var azureKey) && 
                apiKeys.TryGetValue("azure_openai_endpoint", out var azureEndpoint) &&
                !string.IsNullOrWhiteSpace(azureKey) && 
                !string.IsNullOrWhiteSpace(azureEndpoint))
            {
                var azureLogger = loggerFactory.CreateLogger("AzureOpenAiLlmProvider");
                var azureProvider = CreateAzureOpenAiProvider(azureLogger, azureKey, azureEndpoint);
                if (azureProvider != null)
                {
                    providers["Azure"] = azureProvider;
                    _logger.LogInformation("Azure OpenAI provider registered");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create Azure OpenAI provider");
        }

        try
        {
            if (apiKeys.TryGetValue("gemini", out var geminiKey) && !string.IsNullOrWhiteSpace(geminiKey))
            {
                var geminiLogger = loggerFactory.CreateLogger("GeminiLlmProvider");
                var geminiProvider = CreateGeminiProvider(geminiLogger, geminiKey);
                if (geminiProvider != null)
                {
                    providers["Gemini"] = geminiProvider;
                    _logger.LogInformation("Gemini provider registered");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create Gemini provider");
        }

        _logger.LogInformation("Registered {Count} LLM providers: {Providers}", 
            providers.Count, string.Join(", ", providers.Keys));

        return providers;
    }

    private ILlmProvider CreateRuleBasedProvider(ILoggerFactory loggerFactory)
    {
        // Use reflection to create RuleBasedLlmProvider
        var type = Type.GetType("Aura.Providers.Llm.RuleBasedLlmProvider, Aura.Providers");
        if (type == null)
        {
            throw new Exception("RuleBasedLlmProvider type not found");
        }

        // Create a typed logger ILogger<RuleBasedLlmProvider>
        var loggerType = typeof(ILogger<>).MakeGenericType(type);
        var createLoggerMethod = typeof(ILoggerFactory).GetMethod("CreateLogger", Array.Empty<Type>());
        if (createLoggerMethod == null)
        {
            throw new Exception("CreateLogger method not found on ILoggerFactory");
        }
        var genericCreateLogger = createLoggerMethod.MakeGenericMethod(type);
        var typedLogger = genericCreateLogger.Invoke(loggerFactory, null);
        
        return (ILlmProvider)Activator.CreateInstance(type, typedLogger)!;
    }

    private ILlmProvider? CreateOllamaProvider(ILogger logger)
    {
        var ollamaUrl = _providerSettings.GetOllamaUrl();
        var httpClient = _httpClientFactory.CreateClient();
        
        // Use reflection to create OllamaLlmProvider
        var type = Type.GetType("Aura.Providers.Llm.OllamaLlmProvider, Aura.Providers");
        if (type == null)
        {
            _logger.LogWarning("OllamaLlmProvider type not found");
            return null;
        }

        return (ILlmProvider)Activator.CreateInstance(
            type, 
            logger, 
            httpClient, 
            ollamaUrl, 
            "llama3.1:8b-q4_k_m", 
            2, // maxRetries
            120 // timeoutSeconds
        )!;
    }

    private ILlmProvider? CreateOpenAiProvider(ILogger logger, string apiKey)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        // Use reflection to create OpenAiLlmProvider
        var type = Type.GetType("Aura.Providers.Llm.OpenAiLlmProvider, Aura.Providers");
        if (type == null)
        {
            _logger.LogWarning("OpenAiLlmProvider type not found");
            return null;
        }

        return (ILlmProvider)Activator.CreateInstance(
            type,
            logger,
            httpClient,
            apiKey,
            "gpt-4o-mini"
        )!;
    }

    private ILlmProvider? CreateAzureOpenAiProvider(ILogger logger, string apiKey, string endpoint)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        // Use reflection to create AzureOpenAiLlmProvider
        var type = Type.GetType("Aura.Providers.Llm.AzureOpenAiLlmProvider, Aura.Providers");
        if (type == null)
        {
            _logger.LogWarning("AzureOpenAiLlmProvider type not found");
            return null;
        }

        return (ILlmProvider)Activator.CreateInstance(
            type,
            logger,
            httpClient,
            apiKey,
            endpoint,
            "gpt-4"
        )!;
    }

    private ILlmProvider? CreateGeminiProvider(ILogger logger, string apiKey)
    {
        var httpClient = _httpClientFactory.CreateClient();
        
        // Use reflection to create GeminiLlmProvider
        var type = Type.GetType("Aura.Providers.Llm.GeminiLlmProvider, Aura.Providers");
        if (type == null)
        {
            _logger.LogWarning("GeminiLlmProvider type not found");
            return null;
        }

        return (ILlmProvider)Activator.CreateInstance(
            type,
            logger,
            httpClient,
            apiKey,
            "gemini-pro"
        )!;
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
