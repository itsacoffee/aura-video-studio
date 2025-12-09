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
                var providerKey = providerName;
                var providerInstance = provider;
                try
                {
                    Func<CancellationToken, Task<bool>> healthCheck = async ct =>
                    {
                        if (providerKey == "RuleBased")
                        {
                            return true;
                        }

                        if (providerKey == "Ollama")
                        {
                            var providerType = providerInstance.GetType();
                            var method = providerType.GetMethod("IsServiceAvailableAsync",
                                new[] { typeof(CancellationToken), typeof(bool) });

                            if (method != null)
                            {
                                try
                                {
                                    var task = method.Invoke(providerInstance, new object[] { ct, false }) as Task<bool>;
                                    if (task != null)
                                    {
                                        return await task;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Ollama health check failed for provider {ProviderName}", providerName);
                                    return false;
                                }
                            }
                        }

                        return true;
                    };

                    _healthMonitor.RegisterHealthCheck(providerKey, healthCheck);
                    TriggerSeedHealthCheck(providerKey, healthCheck);

                    _logger.LogInformation("✓ Registered health check for LLM provider: {ProviderName}", providerKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ Failed to register health check for LLM provider: {ProviderName}", providerKey);
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
                var providerType = provider.GetType().Name.Replace("Provider", "").Replace("Tts", "");
                try
                {
                    var providerKey = providerType;

                    Func<CancellationToken, Task<bool>> healthCheck = ct => Task.FromResult(true);

                    _healthMonitor.RegisterHealthCheck(providerKey, healthCheck);
                    TriggerSeedHealthCheck(providerKey, healthCheck);

                    _logger.LogInformation("✓ Registered health check for TTS provider: {ProviderType}", providerKey);
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
                var providerType = provider.GetType().Name.Replace("Provider", "");
                try
                {
                    var providerKey = providerType;

                    Func<CancellationToken, Task<bool>> healthCheck = ct => Task.FromResult(true);

                    _healthMonitor.RegisterHealthCheck(providerKey, healthCheck);
                    TriggerSeedHealthCheck(providerKey, healthCheck);

                    _logger.LogInformation("✓ Registered health check for Image provider: {ProviderType}", providerKey);
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

    private void TriggerSeedHealthCheck(string providerName, Func<CancellationToken, Task<bool>> healthCheck)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await _healthMonitor.CheckProviderHealthAsync(providerName, healthCheck, CancellationToken.None).ConfigureAwait(false);
                _logger.LogDebug("Initial health check seeded for provider {ProviderName}", providerName);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Initial health check failed for provider {ProviderName}", providerName);
            }
        });
    }
}
