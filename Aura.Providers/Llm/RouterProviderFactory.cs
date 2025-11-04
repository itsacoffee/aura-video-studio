using System;
using System.Net.Http;
using Aura.Core.AI.Routing;
using Aura.Core.Providers;
using Aura.Core.Services.AI;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm;

/// <summary>
/// Factory for creating LLM provider instances for router service.
/// </summary>
public class RouterProviderFactory : IRouterProviderFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IHttpClientFactory _httpClientFactory;

    public RouterProviderFactory(
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory)
    {
        _loggerFactory = loggerFactory;
        _httpClientFactory = httpClientFactory;
    }

    public ILlmProvider Create(string providerName, string modelName)
    {
        var httpClient = _httpClientFactory.CreateClient();

        return providerName.ToLowerInvariant() switch
        {
            "openai" => CreateOpenAiProvider(modelName, httpClient),
            "ollama" => CreateOllamaProvider(modelName, httpClient),
            "anthropic" => CreateAnthropicProvider(modelName, httpClient),
            "gemini" => CreateGeminiProvider(modelName, httpClient),
            "azureopenai" => CreateAzureOpenAiProvider(modelName, httpClient),
            "rulebased" => CreateRuleBasedProvider(httpClient),
            _ => throw new InvalidOperationException($"Unknown provider: {providerName}")
        };
    }

    public bool IsAvailable(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "openai" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")),
            "ollama" => true,
            "anthropic" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")),
            "gemini" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GEMINI_API_KEY")),
            "azureopenai" => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY")),
            "rulebased" => true,
            _ => false
        };
    }

    private OpenAiLlmProvider CreateOpenAiProvider(string modelName, HttpClient httpClient)
    {
        var logger = _loggerFactory.CreateLogger<OpenAiLlmProvider>();
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        
        return new OpenAiLlmProvider(
            logger,
            httpClient,
            apiKey,
            modelName);
    }

    private OllamaLlmProvider CreateOllamaProvider(string modelName, HttpClient httpClient)
    {
        var logger = _loggerFactory.CreateLogger<OllamaLlmProvider>();
        var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") ?? "http://127.0.0.1:11434";
        
        return new OllamaLlmProvider(
            logger,
            httpClient,
            baseUrl,
            modelName);
    }

    private AnthropicLlmProvider CreateAnthropicProvider(string modelName, HttpClient httpClient)
    {
        var logger = _loggerFactory.CreateLogger<AnthropicLlmProvider>();
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? string.Empty;
        
        return new AnthropicLlmProvider(
            logger,
            httpClient,
            apiKey,
            modelName);
    }

    private GeminiLlmProvider CreateGeminiProvider(string modelName, HttpClient httpClient)
    {
        var logger = _loggerFactory.CreateLogger<GeminiLlmProvider>();
        var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
        
        return new GeminiLlmProvider(
            logger,
            httpClient,
            apiKey,
            modelName);
    }

    private AzureOpenAiLlmProvider CreateAzureOpenAiProvider(string modelName, HttpClient httpClient)
    {
        var logger = _loggerFactory.CreateLogger<AzureOpenAiLlmProvider>();
        var apiKey = Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY") ?? string.Empty;
        var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? string.Empty;
        var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT") ?? modelName;
        
        return new AzureOpenAiLlmProvider(
            logger,
            httpClient,
            endpoint,
            apiKey,
            deploymentName);
    }

    private RuleBasedLlmProvider CreateRuleBasedProvider(HttpClient httpClient)
    {
        var logger = _loggerFactory.CreateLogger<RuleBasedLlmProvider>();
        
        return new RuleBasedLlmProvider(logger);
    }
}
