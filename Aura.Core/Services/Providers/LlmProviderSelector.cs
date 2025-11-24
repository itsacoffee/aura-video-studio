using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers;

/// <summary>
/// Service for selecting the best available LLM provider based on configuration and availability
/// Implements fallback chain: Preferred → Ollama → OpenAI → RuleBased
/// </summary>
public class LlmProviderSelector
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ProviderSettings _settings;
    private readonly ILogger<LlmProviderSelector> _logger;
    private readonly OllamaDetectionService? _ollamaDetection;
    private readonly LlmProviderFactory _providerFactory;

    public LlmProviderSelector(
        IServiceProvider serviceProvider,
        ProviderSettings settings,
        ILogger<LlmProviderSelector> logger,
        OllamaDetectionService? ollamaDetection = null,
        LlmProviderFactory? providerFactory = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _ollamaDetection = ollamaDetection;
        _providerFactory = providerFactory ?? serviceProvider.GetRequiredService<LlmProviderFactory>();
    }

    /// <summary>
    /// Gets the best available LLM provider based on configuration and availability
    /// Tries preferred provider first, then falls back through the chain
    /// </summary>
    public async Task<ILlmProvider> GetProviderAsync(CancellationToken ct = default)
    {
        var preferredProvider = _settings.GetPreferredLlmProvider();

        _logger.LogInformation("Selecting LLM provider (preferred: {Preferred})",
            preferredProvider ?? "none");

        // Try preferred provider first if configured
        if (!string.IsNullOrWhiteSpace(preferredProvider))
        {
            var provider = await TryGetProviderAsync(preferredProvider, ct).ConfigureAwait(false);
            if (provider != null)
            {
                _logger.LogInformation("Using preferred provider: {Provider}", preferredProvider);
                return provider;
            }

            _logger.LogWarning("Preferred provider {Provider} is not available, falling back to alternatives",
                preferredProvider);
        }

        // Fallback chain: Ollama → OpenAI → RuleBased
        var fallbackChain = new[] { "Ollama", "OpenAI", "RuleBased" };

        // Remove preferred provider from fallback chain if it was already tried
        var providersToTry = fallbackChain.Where(n => n != preferredProvider).ToList();

        foreach (var name in providersToTry)
        {
            var provider = await TryGetProviderAsync(name, ct).ConfigureAwait(false);
            if (provider != null)
            {
                _logger.LogWarning("Using fallback provider {Provider}", name);
                return provider;
            }
        }

        // Last resort: ensure RuleBased is always available
        var ruleBasedProvider = await TryGetProviderAsync("RuleBased", ct).ConfigureAwait(false);
        if (ruleBasedProvider != null)
        {
            _logger.LogWarning("All other providers unavailable, using RuleBased as final fallback");
            return ruleBasedProvider;
        }

        throw new InvalidOperationException("No LLM providers available. RuleBased provider should always be available.");
    }

    /// <summary>
    /// Attempts to get a provider by name and verify its availability
    /// </summary>
    private async Task<ILlmProvider?> TryGetProviderAsync(string providerName, CancellationToken ct)
    {
        try
        {
            // Get provider from keyed services
            var provider = _serviceProvider.GetKeyedService<ILlmProvider>(providerName);
            if (provider == null)
            {
                _logger.LogDebug("Provider {Provider} not registered in DI container", providerName);
                return null;
            }

            // For Ollama, check actual service availability
            if (providerName == "Ollama")
            {
                // Use OllamaDetectionService if available for faster checks
                if (_ollamaDetection != null)
                {
                    var status = await _ollamaDetection.GetStatusAsync(ct).ConfigureAwait(false);
                    if (!status.IsRunning)
                    {
                        _logger.LogDebug("Ollama service is not running");
                        return null;
                    }
                }
                else
                {
                    // Fallback to direct availability check
                    var providerType = provider.GetType();
                    var availabilityMethod = providerType.GetMethod("IsServiceAvailableAsync",
                        new[] { typeof(CancellationToken), typeof(bool) });

                    if (availabilityMethod != null)
                    {
                        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                        cts.CancelAfter(TimeSpan.FromSeconds(2)); // Short timeout

                        var task = (Task<bool>)availabilityMethod.Invoke(provider, new object[] { cts.Token, false })!;
                        var isAvailable = await task.ConfigureAwait(false);

                        if (!isAvailable)
                        {
                            _logger.LogDebug("Ollama provider availability check returned false");
                            return null;
                        }
                    }
                }
            }

            // For API key providers, assume available if registered (they'll fail gracefully if keys are invalid)
            // RuleBased is always available
            _logger.LogDebug("Provider {Provider} is available", providerName);
            return provider;
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Availability check for {Provider} was cancelled", providerName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking availability for provider {Provider}", providerName);
            return null;
        }
    }
}

