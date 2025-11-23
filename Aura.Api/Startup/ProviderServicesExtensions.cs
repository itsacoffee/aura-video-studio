using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Models;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Services.Providers;
using Aura.Providers.Images;
using Aura.Providers.Llm;
using Aura.Providers.Tts;
using Aura.Providers.Video;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for registering LLM, TTS, Image, and Video provider services.
/// </summary>
public static class ProviderServicesExtensions
{
    /// <summary>
    /// Registers all provider services including LLM, TTS, Image, and Video providers.
    /// </summary>
    public static IServiceCollection AddProviderServices(this IServiceCollection services)
    {
        // HTTP client for provider communication with proper configuration
        // Named client for API validations with retry policies and proper timeout
        services.AddHttpClient("ProviderValidation", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(120);
            client.DefaultRequestHeaders.Add("User-Agent", "AuraVideoStudio/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                // Disable automatic proxy detection to avoid network issues
                UseProxy = false,
                // Use system default credentials if needed
                UseDefaultCredentials = false,
                // Enable automatic decompression
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                // Configure SSL/TLS settings
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                // Allow redirects
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };
            return handler;
        })
        .AddStandardResilienceHandler(options =>
        {
            // Configure retry policy
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            
            // Configure circuit breaker
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            
            // Configure timeout
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
        });

        // Default HTTP client for other uses
        services.AddHttpClient();

        // Named HttpClient for Ollama with proper configuration
        // Timeout set to 300 seconds (5 minutes) to match providerTimeoutProfiles.json local_llm deepWaitThresholdMs
        // This ensures slow Ollama models have sufficient time to complete, especially for complex prompts
        services.AddHttpClient("OllamaClient", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(300); // 5 minutes - matches timeout profile for local_llm
            client.DefaultRequestHeaders.Add("User-Agent", "AuraVideoStudio/1.0");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                // Disable proxy for localhost connections
                UseProxy = false,
                UseDefaultCredentials = false,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };
            return handler;
        });

        // OpenAI key validation service using IHttpClientFactory
        services.AddSingleton<OpenAIKeyValidationService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<OpenAIKeyValidationService>>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            
            // Use the named client with resilience policies
            var httpClient = httpClientFactory.CreateClient("ProviderValidation");
            
            return new OpenAIKeyValidationService(logger, httpClient);
        });

        // Model catalog for dynamic model discovery
        services.AddSingleton<Aura.Core.AI.Adapters.ModelCatalog>();

        // LLM providers
        services.AddSingleton<LlmProviderFactory>();
        services.AddSingleton<ILlmProvider, CompositeLlmProvider>();

        // Provider mixing configuration
        services.AddSingleton(sp =>
        {
            var config = new ProviderMixingConfig
            {
                ActiveProfile = "Free-Only",
                AutoFallback = true,
                LogProviderSelection = true
            };
            return config;
        });

        services.AddSingleton<ProviderMixer>();

        // Provider recommendation, health monitoring, and cost tracking
        services.AddSingleton<Aura.Core.Services.Providers.ProviderHealthMonitoringService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderCircuitBreakerService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderCostTrackingService>();
        services.AddSingleton<Aura.Core.Services.CostTracking.EnhancedCostTrackingService>();
        
        // Provider stickiness and profile lock services
        services.AddSingleton<Aura.Core.Services.Providers.Stickiness.StallDetector>();
        services.AddSingleton<Aura.Core.Services.Providers.Stickiness.ProviderGateway>();
        services.AddSingleton<Aura.Core.Services.Providers.Stickiness.ProviderProfileLockService>();
        
        // Ollama detection service
        services.AddSingleton<Aura.Core.Services.Providers.OllamaDetectionService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Providers.OllamaDetectionService>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("OllamaClient");
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var baseUrl = settings.GetOllamaUrl();
            return new Aura.Core.Services.Providers.OllamaDetectionService(logger, httpClient, cache, baseUrl);
        });
        
        // Ollama health check service
        services.AddSingleton<Aura.Core.Services.Providers.OllamaHealthCheckService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Providers.OllamaHealthCheckService>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("OllamaClient");
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var baseUrl = settings.GetOllamaUrl();
            return new Aura.Core.Services.Providers.OllamaHealthCheckService(logger, httpClient, cache, baseUrl);
        });
        
        // Stable Diffusion detection service
        services.AddSingleton<Aura.Core.Services.Providers.StableDiffusionDetectionService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Providers.StableDiffusionDetectionService>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var baseUrl = settings.GetStableDiffusionUrl();
            return new Aura.Core.Services.Providers.StableDiffusionDetectionService(logger, httpClient, baseUrl);
        });
        
        // Image provider fallback service
        services.AddSingleton<Aura.Core.Services.Providers.ImageProviderFallbackService>();
        
        services.AddSingleton<Aura.Core.Services.Providers.LlmProviderRecommendationService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Providers.LlmProviderRecommendationService>>();
            var healthMonitor = sp.GetRequiredService<Aura.Core.Services.Providers.ProviderHealthMonitoringService>();
            var costTracker = sp.GetRequiredService<Aura.Core.Services.Providers.ProviderCostTrackingService>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var factory = sp.GetRequiredService<LlmProviderFactory>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

            var providers = factory.CreateAvailableProviders(loggerFactory);

            return new Aura.Core.Services.Providers.LlmProviderRecommendationService(
                logger,
                healthMonitor,
                costTracker,
                settings,
                providers);
        });

        // TTS providers with safe DI resolution
        services.AddSingleton<ITtsProvider, NullTtsProvider>();

        // Register WindowsTtsProvider (platform-dependent)
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ITtsProvider, WindowsTtsProvider>();
        }

        services.AddSingleton<TtsProviderFactory>();

        // Azure TTS provider and voice discovery
        services.AddSingleton<AzureTtsProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AzureTtsProvider>>();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var apiKey = providerSettings.GetAzureSpeechKey();
            var region = providerSettings.GetAzureSpeechRegion();
            var offlineOnly = providerSettings.IsOfflineOnly();

            return new AzureTtsProvider(logger, apiKey, region, offlineOnly);
        });

        services.AddSingleton<AzureVoiceDiscovery>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AzureVoiceDiscovery>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var apiKey = providerSettings.GetAzureSpeechKey();
            var region = providerSettings.GetAzureSpeechRegion();

            return new AzureVoiceDiscovery(logger, httpClient, region, apiKey);
        });

        // Image provider factory
        services.AddSingleton<ImageProviderFactory>();

        // Video composer
        services.AddSingleton<IVideoComposer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FfmpegVideoComposer>>();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var ffmpegLocator = sp.GetRequiredService<IFfmpegLocator>();
            var configuredFfmpegPath = providerSettings.GetFfmpegPath();
            var outputDirectory = providerSettings.GetOutputDirectory();
            return new FfmpegVideoComposer(logger, ffmpegLocator, configuredFfmpegPath, outputDirectory);
        });

        // Provider retry wrapper
        services.AddSingleton<Aura.Core.Services.ProviderRetryWrapper>();

        return services;
    }
}
