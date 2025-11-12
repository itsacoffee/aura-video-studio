using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Factory for creating and managing LLM provider instances
/// Uses keyed services for provider resolution
/// </summary>
public class LlmProviderFactory
{
    private readonly ILogger<LlmProviderFactory> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProviderSettings _providerSettings;
    private readonly IKeyStore _keyStore;
    private readonly IServiceProvider _serviceProvider;

    public LlmProviderFactory(
        ILogger<LlmProviderFactory> logger,
        IHttpClientFactory httpClientFactory,
        ProviderSettings providerSettings,
        IKeyStore keyStore,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _providerSettings = providerSettings;
        _keyStore = keyStore;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Creates all available LLM providers based on configuration
    /// Uses keyed services registered in DI container
    /// </summary>
    public Dictionary<string, ILlmProvider> CreateAvailableProviders(ILoggerFactory loggerFactory)
    {
        var providers = new Dictionary<string, ILlmProvider>();

        // List of provider keys to attempt resolution
        var providerKeys = new[] { "RuleBased", "Ollama", "OpenAI", "Azure", "Gemini", "Anthropic" };

        foreach (var providerKey in providerKeys)
        {
            try
            {
                _logger.LogInformation("Attempting to resolve {Provider} provider...", providerKey);
                
                // Try to get provider from keyed services
                var provider = _serviceProvider.GetKeyedService<ILlmProvider>(providerKey);
                
                if (provider != null)
                {
                    providers[providerKey] = provider;
                    _logger.LogInformation("✓ {Provider} provider registered successfully", providerKey);
                }
                else
                {
                    _logger.LogDebug("✗ {Provider} provider not available (returned null)", providerKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "✗ {Provider} provider registration failed", providerKey);
            }
        }

        // Ensure RuleBased is always available as final fallback
        if (!providers.ContainsKey("RuleBased"))
        {
            _logger.LogWarning("RuleBased provider not found in keyed services, attempting fallback creation");
            try
            {
                providers["RuleBased"] = CreateRuleBasedProvider(loggerFactory);
                _logger.LogInformation("✓ RuleBased provider created as fallback");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "✗ CRITICAL: Failed to create RuleBased fallback provider");
            }
        }

        _logger.LogInformation("========================================");
        _logger.LogInformation("Registered {Count} LLM providers: {Providers}", 
            providers.Count, string.Join(", ", providers.Keys));
        _logger.LogInformation("========================================");

        return providers;
    }

    private ILlmProvider CreateRuleBasedProvider(ILoggerFactory loggerFactory)
    {
        var type = Type.GetType("Aura.Providers.Llm.RuleBasedLlmProvider, Aura.Providers");
        if (type == null)
        {
            throw new Exception("RuleBasedLlmProvider type not found");
        }

        // Use generic CreateLogger<T> method via reflection
        var createLoggerMethod = typeof(LoggerFactoryExtensions)
            .GetMethod("CreateLogger", new[] { typeof(ILoggerFactory) });
        if (createLoggerMethod == null)
        {
            throw new Exception("CreateLogger<T> method not found");
        }
        
        var genericMethod = createLoggerMethod.MakeGenericMethod(type);
        var logger = genericMethod.Invoke(null, new object[] { loggerFactory });
        
        return (ILlmProvider)Activator.CreateInstance(type, logger)!;
    }
}
