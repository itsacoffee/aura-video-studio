using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Initializes provider health monitoring by registering all available providers
/// with the ProviderHealthMonitor for tracking
/// </summary>
public class ProviderHealthInitializer
{
    private readonly ILogger<ProviderHealthInitializer> _logger;
    private readonly ProviderHealthMonitor _healthMonitor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoggerFactory _loggerFactory;

    public ProviderHealthInitializer(
        ILogger<ProviderHealthInitializer> logger,
        ProviderHealthMonitor healthMonitor,
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _healthMonitor = healthMonitor;
        _serviceProvider = serviceProvider;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Register all available providers with the health monitor
    /// </summary>
    public void RegisterAllProviders()
    {
        _logger.LogInformation("=== Starting Provider Health Registration ===");
        
        RegisterLlmProviders();
        RegisterTtsProviders();
        RegisterImageProviders();
        
        _logger.LogInformation("=== Provider Health Registration Complete ===");
    }

    private void RegisterLlmProviders()
    {
        try
        {
            _logger.LogInformation("Registering LLM providers with health monitor...");
            
            var llmProviderFactory = _serviceProvider.GetService<LlmProviderFactory>();
            if (llmProviderFactory == null)
            {
                _logger.LogWarning("LlmProviderFactory not available - skipping LLM provider registration");
                return;
            }

            var providers = llmProviderFactory.CreateAvailableProviders(_loggerFactory);
            _logger.LogInformation("Found {Count} LLM providers to register", providers.Count);

            foreach (var (providerName, provider) in providers)
            {
                try
                {
                    _healthMonitor.RegisterHealthCheck(providerName, async (ct) =>
                    {
                        if (providerName == "RuleBased")
                        {
                            return true;
                        }

                        if (providerName == "Ollama")
                        {
                            var providerType = provider.GetType();
                            var method = providerType.GetMethod("IsServiceAvailableAsync", 
                                new[] { typeof(CancellationToken), typeof(bool) });
                            
                            if (method != null)
                            {
                                try
                                {
                                    var task = method.Invoke(provider, new object[] { ct, false }) as Task<bool>;
                                    if (task != null)
                                    {
                                        return await task;
                                    }
                                }
                                catch
                                {
                                    return false;
                                }
                            }
                        }

                        return true;
                    });

                    _logger.LogInformation("✓ Registered health check for LLM provider: {ProviderName}", providerName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ Failed to register health check for LLM provider: {ProviderName}", providerName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering LLM providers with health monitor");
        }
    }

    private void RegisterTtsProviders()
    {
        try
        {
            _logger.LogInformation("Registering TTS providers with health monitor...");
            
            var ttsProviders = _serviceProvider.GetServices<ITtsProvider>();
            var providersList = ttsProviders.Where(p => p != null).ToList();
            
            _logger.LogInformation("Found {Count} TTS providers to register", providersList.Count);

            foreach (var provider in providersList)
            {
                try
                {
                    var providerType = provider.GetType().Name.Replace("Provider", "").Replace("Tts", "");
                    
                    _healthMonitor.RegisterHealthCheck(providerType, async (ct) =>
                    {
                        if (providerType == "NullTts" || providerType == "Windows")
                        {
                            return true;
                        }

                        return true;
                    });

                    _logger.LogInformation("✓ Registered health check for TTS provider: {ProviderType}", providerType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ Failed to register health check for TTS provider");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering TTS providers with health monitor");
        }
    }

    private void RegisterImageProviders()
    {
        try
        {
            _logger.LogInformation("Registering Image providers with health monitor...");
            
            var imageProviders = _serviceProvider.GetServices<IImageProvider>();
            var providersList = imageProviders.Where(p => p != null).ToList();
            
            _logger.LogInformation("Found {Count} Image providers to register", providersList.Count);

            foreach (var provider in providersList)
            {
                try
                {
                    var providerType = provider.GetType().Name.Replace("Provider", "");
                    
                    _healthMonitor.RegisterHealthCheck(providerType, async (ct) =>
                    {
                        return true;
                    });

                    _logger.LogInformation("✓ Registered health check for Image provider: {ProviderType}", providerType);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ Failed to register health check for Image provider");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering Image providers with health monitor");
        }
    }
}
