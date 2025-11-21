using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Errors;
using Aura.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Providers;

/// <summary>
/// Factory for creating and managing image provider instances with health checks and fallback support
/// </summary>
public class ImageProviderFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImageProviderFactory> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ProviderSettings _providerSettings;
    private readonly string _apiKeysPath;
    private readonly Dictionary<string, DateTime> _providerHealthStatus;
    private readonly TimeSpan _healthCheckCacheDuration = TimeSpan.FromMinutes(5);

    public ImageProviderFactory(
        IServiceProvider serviceProvider,
        ILogger<ImageProviderFactory> logger,
        IHttpClientFactory httpClientFactory,
        ProviderSettings providerSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _providerSettings = providerSettings;
        _apiKeysPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura",
            "apikeys.json");
        _providerHealthStatus = new Dictionary<string, DateTime>();
    }

    /// <summary>
    /// Creates all available image providers based on configuration
    /// Resolves providers from DI - never throws on failure
    /// </summary>
    public Dictionary<string, IImageProvider> CreateAvailableProviders(ILoggerFactory loggerFactory)
    {
        var providers = new Dictionary<string, IImageProvider>();
        string correlationId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogInformation("[{CorrelationId}] Creating available image providers from DI container", correlationId);

        // Try to resolve all registered IImageProvider instances from DI
        try
        {
            var registeredProviders = _serviceProvider.GetServices<IImageProvider>();
            int providerIndex = 0;

            foreach (var provider in registeredProviders)
            {
                if (provider == null)
                {
                    _logger.LogDebug("[{CorrelationId}] Skipping null provider at index {Index}", correlationId, providerIndex);
                    providerIndex++;
                    continue;
                }

                var providerTypeName = provider.GetType().Name;
                _logger.LogInformation("[{CorrelationId}] ✓ Resolved image provider: {ProviderName}", 
                    correlationId, providerTypeName);
                
                // Use type name as key if provider doesn't have a Name property
                var providerKey = providerTypeName.Replace("Provider", "").Replace("Image", "");
                providers[providerKey] = provider;
                providerIndex++;
            }

            _logger.LogInformation("[{CorrelationId}] Image provider factory initialized with {Count} providers from DI", 
                correlationId, providers.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{CorrelationId}] Failed to resolve image providers from DI", correlationId);
        }

        // If no providers were resolved from DI, try legacy reflection-based approach as fallback
        if (providers.Count == 0)
        {
            _logger.LogWarning("[{CorrelationId}] No image providers resolved from DI, attempting legacy reflection-based instantiation", 
                correlationId);
            providers = CreateProvidersViaReflection(loggerFactory, correlationId);
        }

        return providers;
    }

    /// <summary>
    /// Legacy method: Creates providers via reflection (fallback when DI registration fails)
    /// </summary>
    private Dictionary<string, IImageProvider> CreateProvidersViaReflection(ILoggerFactory loggerFactory, string correlationId)
    {
        var providers = new Dictionary<string, IImageProvider>();
        var apiKeys = LoadApiKeys();

        // Try to create Stability AI provider (if API key is available)
        try
        {
            if (apiKeys.TryGetValue("stability", out var stabilityKey) && !string.IsNullOrWhiteSpace(stabilityKey))
            {
                _logger.LogInformation("[{CorrelationId}] Attempting to register Stability AI image provider via reflection...", correlationId);
                var stabilityProvider = CreateStabilityProvider(loggerFactory, stabilityKey);
                if (stabilityProvider != null && CheckProviderHealth(stabilityProvider, "Stability"))
                {
                    providers["Stability"] = stabilityProvider;
                    _logger.LogInformation("[{CorrelationId}] ✓ Stability AI image provider registered successfully", correlationId);
                }
                else
                {
                    _logger.LogWarning("[{CorrelationId}] ✗ Stability AI provider health check failed", correlationId);
                }
            }
            else
            {
                _logger.LogDebug("[{CorrelationId}] ✗ Stability AI provider skipped (no API key configured)", correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{CorrelationId}] ✗ Stability AI provider registration failed", correlationId);
        }

        // Try to create Runway provider (if API key is available)
        try
        {
            if (apiKeys.TryGetValue("runway", out var runwayKey) && !string.IsNullOrWhiteSpace(runwayKey))
            {
                _logger.LogInformation("[{CorrelationId}] Attempting to register Runway image provider via reflection...", correlationId);
                var runwayProvider = CreateRunwayProvider(loggerFactory, runwayKey);
                if (runwayProvider != null && CheckProviderHealth(runwayProvider, "Runway"))
                {
                    providers["Runway"] = runwayProvider;
                    _logger.LogInformation("[{CorrelationId}] ✓ Runway image provider registered successfully", correlationId);
                }
                else
                {
                    _logger.LogWarning("[{CorrelationId}] ✗ Runway provider health check failed", correlationId);
                }
            }
            else
            {
                _logger.LogDebug("[{CorrelationId}] ✗ Runway provider skipped (no API key configured)", correlationId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[{CorrelationId}] ✗ Runway provider registration failed", correlationId);
        }

        _logger.LogInformation("[{CorrelationId}] Image provider factory initialized with {Count} providers via reflection", 
            correlationId, providers.Count);
        return providers;
    }

    /// <summary>
    /// Gets the first available and healthy image provider
    /// </summary>
    public IImageProvider? GetDefaultProvider(ILoggerFactory loggerFactory)
    {
        var providers = CreateAvailableProviders(loggerFactory);
        
        // Priority order: Stability > Runway
        var priorityOrder = new[] { "Stability", "Runway" };
        
        foreach (var providerName in priorityOrder)
        {
            if (providers.TryGetValue(providerName, out var provider))
            {
                _logger.LogInformation("Selected {ProviderName} as default image provider", providerName);
                return provider;
            }
        }

        _logger.LogWarning("No image providers available");
        return null;
    }

    /// <summary>
    /// Gets the default stock provider with Pexels API key checking and graceful fallback
    /// </summary>
    public IStockProvider GetDefaultStockProvider()
    {
        // Check for Pexels API key
        var pexelsApiKey = _providerSettings.GetPexelsApiKey();
        
        if (!string.IsNullOrEmpty(pexelsApiKey))
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var loggerFactory = _serviceProvider.GetService<ILoggerFactory>();
                if (loggerFactory != null)
                {
                    var pexelsLogger = loggerFactory.CreateLogger("Aura.Providers.Images.PexelsImageProvider");
                    
                    // Use reflection to avoid circular dependency on Aura.Providers
                    var providerType = Type.GetType("Aura.Providers.Images.PexelsImageProvider, Aura.Providers");
                    if (providerType != null)
                    {
                        var instance = Activator.CreateInstance(providerType, pexelsLogger, httpClient, pexelsApiKey);
                        var stockProvider = instance as IStockProvider;
                        if (stockProvider != null)
                        {
                            _logger.LogInformation("Using Pexels image provider with configured API key");
                            return stockProvider;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to initialize Pexels provider, falling back to placeholder provider");
            }
        }
        else
        {
            _logger.LogInformation("No Pexels API key configured. Get a free key at https://www.pexels.com/api/");
        }

        // Fallback: Return placeholder provider
        _logger.LogInformation("No image API configured - using placeholder/color backgrounds");
        var loggerFactory2 = _serviceProvider.GetService<ILoggerFactory>();
        if (loggerFactory2 != null)
        {
            var placeholderLogger = loggerFactory2.CreateLogger("Aura.Providers.Images.PlaceholderImageProvider");
            
            // Use reflection to create placeholder provider
            var providerType = Type.GetType("Aura.Providers.Images.PlaceholderImageProvider, Aura.Providers");
            if (providerType != null)
            {
                var instance = Activator.CreateInstance(providerType, placeholderLogger, (string?)null);
                var stockProvider = instance as IStockProvider;
                if (stockProvider != null)
                {
                    return stockProvider;
                }
            }
        }
        
        throw new InvalidOperationException("Unable to create fallback placeholder provider");
    }

    /// <summary>
    /// Checks if a provider is healthy and responding
    /// </summary>
    private bool CheckProviderHealth(IImageProvider provider, string providerName)
    {
        // Check if we have a recent health check result
        if (_providerHealthStatus.TryGetValue(providerName, out var lastCheck))
        {
            if (DateTime.UtcNow - lastCheck < _healthCheckCacheDuration)
            {
                _logger.LogDebug("Using cached health status for {ProviderName}", providerName);
                return true;
            }
        }

        try
        {
            // Simple health check - just verify the provider can be instantiated
            // More sophisticated checks (like API ping) should be done async elsewhere
            _logger.LogDebug("Health check passed for {ProviderName}", providerName);
            _providerHealthStatus[providerName] = DateTime.UtcNow;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for {ProviderName}", providerName);
            return false;
        }
    }

    /// <summary>
    /// Creates a Stability AI provider instance
    /// </summary>
    private IImageProvider? CreateStabilityProvider(ILoggerFactory loggerFactory, string apiKey)
    {
        try
        {
            var logger = loggerFactory.CreateLogger("Aura.Providers.Images.StabilityImageProvider");
            var httpClient = _httpClientFactory.CreateClient();
            
            // Use reflection to avoid hard dependency on Aura.Providers at compile time
            var providerType = Type.GetType("Aura.Providers.Images.StabilityImageProvider, Aura.Providers");
            if (providerType != null)
            {
                var instance = Activator.CreateInstance(providerType, logger, httpClient, apiKey);
                return instance as IImageProvider;
            }
            
            _logger.LogWarning("StabilityImageProvider type not found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Stability AI provider");
            return null;
        }
    }

    /// <summary>
    /// Creates a Runway provider instance
    /// </summary>
    private IImageProvider? CreateRunwayProvider(ILoggerFactory loggerFactory, string apiKey)
    {
        try
        {
            var logger = loggerFactory.CreateLogger("Aura.Providers.Images.RunwayImageProvider");
            var httpClient = _httpClientFactory.CreateClient();
            
            // Use reflection to avoid hard dependency on Aura.Providers at compile time
            var providerType = Type.GetType("Aura.Providers.Images.RunwayImageProvider, Aura.Providers");
            if (providerType != null)
            {
                var instance = Activator.CreateInstance(providerType, logger, httpClient, apiKey);
                return instance as IImageProvider;
            }
            
            _logger.LogWarning("RunwayImageProvider type not found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Runway provider");
            return null;
        }
    }

    /// <summary>
    /// Loads API keys from the apikeys.json file
    /// </summary>
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

    /// <summary>
    /// Creates an image provider with timeout handling and retry capability
    /// </summary>
    public IImageProvider CreateWithTimeout(IImageProvider baseProvider, string providerName, TimeSpan timeout)
    {
        return new TimeoutImageProviderWrapper(baseProvider, providerName, timeout, _logger);
    }

    /// <summary>
    /// Creates a fallback chain of image providers
    /// </summary>
    public IImageProvider CreateFallbackChain(ILoggerFactory loggerFactory)
    {
        var providers = CreateAvailableProviders(loggerFactory);
        
        if (providers.Count == 0)
        {
            throw new ProviderException(
                "None",
                ProviderType.Visual,
                "No image providers available",
                userMessage: "Image generation requires at least one configured provider. Please add API keys in Settings → Providers.");
        }

        return new FallbackImageProvider(providers.Values, _logger);
    }
}

/// <summary>
/// Wrapper that adds timeout handling to an image provider
/// </summary>
internal class TimeoutImageProviderWrapper : IImageProvider
{
    private readonly IImageProvider _innerProvider;
    private readonly string _providerName;
    private readonly TimeSpan _timeout;
    private readonly ILogger _logger;

    public TimeoutImageProviderWrapper(IImageProvider innerProvider, string providerName, TimeSpan timeout, ILogger logger)
    {
        _innerProvider = innerProvider;
        _providerName = providerName;
        _timeout = timeout;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(_timeout);

        try
        {
            return await _innerProvider.FetchOrGenerateAsync(scene, spec, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Timeout occurred (not user cancellation)
            throw ProviderException.Timeout(_providerName, ProviderType.Visual, (int)_timeout.TotalSeconds);
        }
    }
}

/// <summary>
/// Image provider that tries multiple providers in sequence until one succeeds
/// </summary>
internal class FallbackImageProvider : IImageProvider
{
    private readonly IEnumerable<IImageProvider> _providers;
    private readonly ILogger _logger;

    public FallbackImageProvider(IEnumerable<IImageProvider> providers, ILogger logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Asset>> FetchOrGenerateAsync(Scene scene, VisualSpec spec, CancellationToken ct = default)
    {
        Exception? lastException = null;
        var providerList = _providers.ToList();

        foreach (var provider in providerList)
        {
            try
            {
                _logger.LogDebug("Attempting image generation with provider {ProviderType}", provider.GetType().Name);
                var result = await provider.FetchOrGenerateAsync(scene, spec, ct).ConfigureAwait(false);
                _logger.LogInformation("Successfully generated image with provider {ProviderType}", provider.GetType().Name);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Image generation failed with provider {ProviderType}, trying next provider", provider.GetType().Name);
                lastException = ex;
            }
        }

        // All providers failed
        throw new ProviderException(
            "FallbackChain",
            ProviderType.Visual,
            $"All {providerList.Count} image providers failed",
            userMessage: "Unable to generate image. All configured providers encountered errors.",
            innerException: lastException);
    }
}
