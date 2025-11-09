using System;
using System.Net.Http;
using Aura.Core.Configuration;
using Aura.Core.Providers;
using Aura.Providers.Images;
using Aura.Providers.Llm;
using Aura.Providers.Tts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aura.Providers;

/// <summary>
/// Extension methods for registering all providers with dependency injection
/// Provides clean separation of provider registration from application startup
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all provider services including LLM, TTS, Image, and Rendering providers
    /// This is the single entry point for provider registration
    /// </summary>
    public static IServiceCollection AddAuraProviders(this IServiceCollection services)
    {
        services.AddLlmProviders();
        services.AddTtsProviders();
        services.AddImageProviders();
        services.AddRenderingProviders();
        
        return services;
    }

    /// <summary>
    /// Registers all LLM providers with their dependencies
    /// Only registers providers if their required configuration (API keys, etc.) is available
    /// </summary>
    public static IServiceCollection AddLlmProviders(this IServiceCollection services)
    {
        // Always register RuleBased provider (GUARANTEED - offline fallback)
        services.AddSingleton<ILlmProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<RuleBasedLlmProvider>>();
            return new RuleBasedLlmProvider(logger);
        });

        // Register Ollama provider (local, requires Ollama installation)
        // Always register since it can fail gracefully
        services.AddSingleton<ILlmProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<OllamaLlmProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var ollamaUrl = settings.GetOllamaUrl();
            var ollamaModel = settings.GetOllamaModel();
            
            return new OllamaLlmProvider(
                logger,
                httpClient,
                ollamaUrl,
                ollamaModel,
                maxRetries: 2,
                timeoutSeconds: 120
            );
        });

        // Register OpenAI provider conditionally (only if API key is available)
        services.AddSingleton<ILlmProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<OpenAiLlmProvider>>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var apiKey = settings.GetOpenAiApiKey();
            
            // Only create if API key is available - do NOT register null providers
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("OpenAI API key not configured, provider will not be available");
                // Return a marker object that will be filtered out
                return null!;
            }
            
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            return new OpenAiLlmProvider(
                logger,
                httpClient,
                apiKey,
                model: "gpt-4o-mini"
            );
        });

        // Register Azure OpenAI provider conditionally (only if credentials are available)
        services.AddSingleton<ILlmProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AzureOpenAiLlmProvider>>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var apiKey = settings.GetAzureOpenAiApiKey();
            var endpoint = settings.GetAzureOpenAiEndpoint();
            
            // Only create if both API key and endpoint are available
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(endpoint))
            {
                logger.LogDebug("Azure OpenAI credentials not configured, provider will not be available");
                return null!;
            }
            
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            return new AzureOpenAiLlmProvider(
                logger,
                httpClient,
                apiKey,
                endpoint,
                deploymentName: "gpt-4"
            );
        });

        // Register Gemini provider conditionally (only if API key is available)
        services.AddSingleton<ILlmProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<GeminiLlmProvider>>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var apiKey = settings.GetGeminiApiKey();
            
            // Only create if API key is available
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("Gemini API key not configured, provider will not be available");
                return null!;
            }
            
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            return new GeminiLlmProvider(
                logger,
                httpClient,
                apiKey,
                model: "gemini-pro"
            );
        });

        return services;
    }

    /// <summary>
    /// Registers all TTS providers with their dependencies
    /// Providers are registered as ITtsProvider implementations for factory enumeration
    /// </summary>
    public static IServiceCollection AddTtsProviders(this IServiceCollection services)
    {
        // Always register NullTtsProvider as final fallback
        services.AddSingleton<ITtsProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<NullTtsProvider>>();
            var silentWavGenerator = sp.GetRequiredService<Aura.Core.Audio.SilentWavGenerator>();
            return new NullTtsProvider(logger, silentWavGenerator);
        });

        // Register Windows TTS provider (platform-specific)
        if (OperatingSystem.IsWindows())
        {
            services.AddSingleton<ITtsProvider>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<WindowsTtsProvider>>();
                return new WindowsTtsProvider(logger);
            });
        }

        // Register ElevenLabs provider (requires API key)
        services.AddSingleton<ITtsProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ElevenLabsTtsProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var apiKey = settings.GetElevenLabsApiKey();
            
            // Only create if API key is available
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("ElevenLabs API key not configured, skipping provider registration");
                return null!;
            }
            
            return new ElevenLabsTtsProvider(logger, httpClient, apiKey);
        });

        // Register PlayHT provider (requires API key and user ID)
        services.AddSingleton<ITtsProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PlayHTTtsProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var apiKey = settings.GetPlayHTApiKey();
            var userId = settings.GetPlayHTUserId();
            
            // Only create if both API key and user ID are available
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(userId))
            {
                logger.LogDebug("PlayHT credentials not configured, skipping provider registration");
                return null!;
            }
            
            return new PlayHTTtsProvider(logger, httpClient, apiKey, userId);
        });

        // Register Azure TTS provider (requires API key and region)
        services.AddSingleton<ITtsProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AzureTtsProvider>>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var apiKey = settings.GetAzureSpeechKey();
            var region = settings.GetAzureSpeechRegion();
            var offlineOnly = settings.IsOfflineOnly();
            
            // Only create if API key and region are available
            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(region))
            {
                logger.LogDebug("Azure Speech credentials not configured, skipping provider registration");
                return null!;
            }
            
            return new AzureTtsProvider(logger, apiKey, region, offlineOnly);
        });

        // Register Piper provider (requires executable path)
        services.AddSingleton<ITtsProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<PiperTtsProvider>>();
            var silentWavGenerator = sp.GetRequiredService<Aura.Core.Audio.SilentWavGenerator>();
            var wavValidator = sp.GetRequiredService<Aura.Core.Audio.WavValidator>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var piperPath = settings.PiperExecutablePath;
            var modelPath = settings.PiperVoiceModelPath;
            
            // Only create if paths are configured
            if (string.IsNullOrWhiteSpace(piperPath) || string.IsNullOrWhiteSpace(modelPath))
            {
                logger.LogDebug("Piper TTS paths not configured, skipping provider registration");
                return null!;
            }
            
            return new PiperTtsProvider(logger, silentWavGenerator, wavValidator, piperPath, modelPath);
        });

        // Register Mimic3 provider (requires base URL)
        services.AddSingleton<ITtsProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Mimic3TtsProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var silentWavGenerator = sp.GetRequiredService<Aura.Core.Audio.SilentWavGenerator>();
            var wavValidator = sp.GetRequiredService<Aura.Core.Audio.WavValidator>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var baseUrl = settings.Mimic3BaseUrl;
            
            // Only create if base URL is configured
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                logger.LogDebug("Mimic3 base URL not configured, skipping provider registration");
                return null!;
            }
            
            return new Mimic3TtsProvider(logger, httpClient, silentWavGenerator, wavValidator, baseUrl);
        });

        return services;
    }

    /// <summary>
    /// Registers all Image providers with their dependencies
    /// Providers are registered as IImageProvider implementations for factory enumeration
    /// </summary>
    public static IServiceCollection AddImageProviders(this IServiceCollection services)
    {
        // Note: Image providers are created dynamically by ImageProviderFactory
        // based on API keys available in ProviderSettings
        // No direct registration needed here - factory handles instantiation
        
        return services;
    }

    /// <summary>
    /// Registers provider factories for creating and managing provider instances
    /// </summary>
    public static IServiceCollection AddProviderFactories(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Orchestrator.LlmProviderFactory>();
        services.AddSingleton<TtsProviderFactory>();
        services.AddSingleton<ImageProviderFactory>();
        
        return services;
    }

    /// <summary>
    /// Registers provider health monitoring and recommendation services
    /// </summary>
    public static IServiceCollection AddProviderHealthServices(this IServiceCollection services)
    {
        services.AddSingleton<Aura.Core.Services.Providers.ProviderHealthMonitoringService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderCostTrackingService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderStatusService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderCircuitBreakerService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderFallbackService>();
        services.AddSingleton<Aura.Core.Services.Health.ProviderHealthMonitor>();
        services.AddSingleton<Aura.Core.Services.Health.ProviderHealthService>();
        
        return services;
    }

    /// <summary>
    /// Registers all rendering providers with their dependencies
    /// Providers are registered with IRenderingProvider interface for selector enumeration
    /// </summary>
    public static IServiceCollection AddRenderingProviders(this IServiceCollection services)
    {
        // Register all rendering providers as IRenderingProvider
        // Priority order: FFmpegProvider (auto-detect) > NVENC > AMF > QSV > BasicFFmpeg
        
        services.AddSingleton<Rendering.IRenderingProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Rendering.FFmpegProvider>>();
            var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
            return new Rendering.FFmpegProvider(logger, ffmpegLocator);
        });

        services.AddSingleton<Rendering.IRenderingProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Rendering.FFmpegNvidiaProvider>>();
            var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
            return new Rendering.FFmpegNvidiaProvider(logger, ffmpegLocator);
        });

        services.AddSingleton<Rendering.IRenderingProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Rendering.FFmpegAmdProvider>>();
            var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
            return new Rendering.FFmpegAmdProvider(logger, ffmpegLocator);
        });

        services.AddSingleton<Rendering.IRenderingProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Rendering.FFmpegIntelProvider>>();
            var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
            return new Rendering.FFmpegIntelProvider(logger, ffmpegLocator);
        });

        services.AddSingleton<Rendering.IRenderingProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Rendering.BasicFFmpegProvider>>();
            var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
            return new Rendering.BasicFFmpegProvider(logger, ffmpegLocator);
        });

        // Register the selector service
        services.AddSingleton<Rendering.RenderingProviderSelector>();

        return services;
    }
}
