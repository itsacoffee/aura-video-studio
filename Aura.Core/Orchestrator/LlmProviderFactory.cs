using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Gets the best available LLM provider based on configured preference and availability
    /// Tests provider availability with a short timeout (2s) before returning
    /// Falls back to alternative providers if the preferred one is unavailable
    /// </summary>
    public async Task<ILlmProvider?> GetBestAvailableProviderAsync(CancellationToken ct = default)
    {
        var providers = CreateAvailableProviders(LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning)));
        if (providers.Count == 0)
        {
            _logger.LogWarning("No LLM providers available");
            return null;
        }

        // Get configured preference
        var preferredProviderName = _providerSettings.GetPreferredLlmProvider();
        if (string.IsNullOrWhiteSpace(preferredProviderName))
        {
            _logger.LogDebug("No preferred LLM provider configured, using default fallback chain");
        }
        else
        {
            _logger.LogInformation("Preferred LLM provider configured: {Provider}", preferredProviderName);
        }

        // Try preferred provider first if configured and available
        if (!string.IsNullOrWhiteSpace(preferredProviderName) && providers.TryGetValue(preferredProviderName, out var preferredProvider))
        {
            var isAvailable = await CheckProviderAvailabilityAsync(preferredProvider, preferredProviderName, ct).ConfigureAwait(false);
            if (isAvailable)
            {
                _logger.LogInformation("Using preferred provider: {Provider}", preferredProviderName);
                return preferredProvider;
            }
            else
            {
                _logger.LogWarning("Preferred provider {Provider} is not available, falling back to alternatives", preferredProviderName);
            }
        }

        // Fallback chain: Ollama -> OpenAI -> RuleBased
        var fallbackChain = new[] { "Ollama", "OpenAI", "RuleBased" };

        // Remove preferred provider from fallback chain if it was already tried
        var providersToTry = fallbackChain.Where(p => p != preferredProviderName).ToList();

        foreach (var providerName in providersToTry)
        {
            if (!providers.TryGetValue(providerName, out var provider))
            {
                _logger.LogDebug("Provider {Provider} not registered, skipping", providerName);
                continue;
            }

            var isAvailable = await CheckProviderAvailabilityAsync(provider, providerName, ct).ConfigureAwait(false);
            if (isAvailable)
            {
                _logger.LogInformation("Using fallback provider: {Provider}", providerName);
                return provider;
            }
        }

        // Last resort: try RuleBased even if availability check failed (it's always available)
        if (providers.TryGetValue("RuleBased", out var ruleBasedProvider))
        {
            _logger.LogWarning("All other providers unavailable, using RuleBased as final fallback");
            return ruleBasedProvider;
        }

        _logger.LogError("No available LLM providers found, including RuleBased fallback");
        return null;
    }

    /// <summary>
    /// Checks if a provider is available with a short timeout (2 seconds)
    /// For Ollama, uses IsServiceAvailableAsync; for others, assumes available if registered
    /// </summary>
    private async Task<bool> CheckProviderAvailabilityAsync(ILlmProvider provider, string providerName, CancellationToken ct)
    {
        try
        {
            // For Ollama, check actual service availability
            if (providerName == "Ollama")
            {
                var providerType = provider.GetType();
                var availabilityMethod = providerType.GetMethod("IsServiceAvailableAsync", new[] { typeof(CancellationToken), typeof(bool) });

                if (availabilityMethod != null)
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    cts.CancelAfter(TimeSpan.FromSeconds(2)); // Short timeout for availability check

                    var task = (Task<bool>)availabilityMethod.Invoke(provider, new object[] { cts.Token, false })!;
                    return await task.ConfigureAwait(false);
                }
            }

            // For API key providers, assume available if registered (they'll fail gracefully if keys are invalid)
            // RuleBased is always available
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Availability check for {Provider} timed out", providerName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking availability for {Provider}", providerName);
            return false;
        }
    }
}
