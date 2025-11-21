using System;
using System.IO;
using System.Net.Http;
using Aura.Core.Configuration;
using Aura.Core.Orchestrator;
using Aura.Core.Providers;
using Aura.Core.Models;
using Aura.Providers.Images;
using Aura.Providers.Llm;
using Aura.Providers.Tts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Aura.Providers;

/// <summary>
/// Extension methods for registering all providers with dependency injection
/// Provides clean separation of provider registration from application startup
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all provider services including LLM, TTS, Image, Music, SFX, and Rendering providers
    /// This is the single entry point for provider registration
    /// </summary>
    public static IServiceCollection AddAuraProviders(this IServiceCollection services)
    {
        services.AddLlmProviders();
        services.AddTtsProviders();
        services.AddImageProviders();
        services.AddMusicProviders();
        services.AddSfxProviders();
        services.AddRenderingProviders();

        return services;
    }

    /// <summary>
    /// Registers all LLM providers with their dependencies
    /// Providers are registered as keyed services for direct resolution by name
    /// </summary>
    public static IServiceCollection AddLlmProviders(this IServiceCollection services)
    {
        services.TryAddSingleton<ProviderMixingConfig>(_ => new ProviderMixingConfig
        {
            ActiveProfile = "Free-Only",
            AutoFallback = true,
            LogProviderSelection = true
        });
        services.TryAddSingleton<ProviderMixer>();
        services.TryAddSingleton<LlmProviderFactory>();
        services.TryAddSingleton<IKeyStore, KeyStore>();

        // Register individual LLM providers as keyed services
        // This allows direct resolution by provider name: sp.GetKeyedService<ILlmProvider>("OpenAI")

        // RuleBased provider (ALWAYS AVAILABLE - offline fallback)
        services.AddKeyedSingleton<ILlmProvider>("RuleBased", (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<RuleBasedLlmProvider>>();
            return new RuleBasedLlmProvider(logger);
        });

        // Ollama provider (local, checks availability at runtime)
        services.AddKeyedSingleton<ILlmProvider>("Ollama", (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<OllamaLlmProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var baseUrl = providerSettings.GetOllamaUrl();
            var model = providerSettings.GetOllamaModel();

            return new OllamaLlmProvider(logger, httpClient, baseUrl, model);
        });

        // OpenAI provider (requires API key)
        services.AddKeyedSingleton<ILlmProvider>("OpenAI", (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<OpenAiLlmProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var keyStore = sp.GetRequiredService<IKeyStore>();

            // Try to get API key from secure storage
            var apiKeys = keyStore.GetAllKeys();
            if (!apiKeys.TryGetValue("openai", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("OpenAI API key not configured, provider unavailable");
                return null!;
            }

            return new OpenAiLlmProvider(logger, httpClient, apiKey, "gpt-4o-mini");
        });

        // Azure OpenAI provider (requires API key and endpoint)
        services.AddKeyedSingleton<ILlmProvider>("Azure", (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<AzureOpenAiLlmProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var keyStore = sp.GetRequiredService<IKeyStore>();

            // Try to get credentials from secure storage
            var apiKeys = keyStore.GetAllKeys();
            if (!apiKeys.TryGetValue("azure_openai_key", out var apiKey) ||
                !apiKeys.TryGetValue("azure_openai_endpoint", out var endpoint) ||
                string.IsNullOrWhiteSpace(apiKey) ||
                string.IsNullOrWhiteSpace(endpoint))
            {
                logger.LogDebug("Azure OpenAI credentials not configured, provider unavailable");
                return null!;
            }

            return new AzureOpenAiLlmProvider(logger, httpClient, apiKey, endpoint, "gpt-4");
        });

        // Gemini provider (requires API key)
        services.AddKeyedSingleton<ILlmProvider>("Gemini", (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<GeminiLlmProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var keyStore = sp.GetRequiredService<IKeyStore>();

            // Try to get API key from secure storage
            var apiKeys = keyStore.GetAllKeys();
            if (!apiKeys.TryGetValue("gemini", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("Gemini API key not configured, provider unavailable");
                return null!;
            }

            return new GeminiLlmProvider(logger, httpClient, apiKey, "gemini-pro");
        });

        // Anthropic provider (requires API key)
        services.AddKeyedSingleton<ILlmProvider>("Anthropic", (sp, key) =>
        {
            var logger = sp.GetRequiredService<ILogger<AnthropicLlmProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var keyStore = sp.GetRequiredService<IKeyStore>();

            // Try to get API key from secure storage
            var apiKeys = keyStore.GetAllKeys();
            if (!apiKeys.TryGetValue("anthropic", out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("Anthropic API key not configured, provider unavailable");
                return null!;
            }

            return new AnthropicLlmProvider(logger, httpClient, apiKey);
        });

        services.TryAddSingleton<CompositeLlmProvider>();
        services.TryAddSingleton<ILlmProvider>(sp => sp.GetRequiredService<CompositeLlmProvider>());

        return services;
    }

    /// <summary>
    /// Registers all TTS providers with their dependencies
    /// Providers are registered as ITtsProvider implementations for factory enumeration
    /// </summary>
    public static IServiceCollection AddTtsProviders(this IServiceCollection services)
    {
        // Register TTS support services
        services.AddSingleton<VoiceCache>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<VoiceCache>>();
            return new VoiceCache(logger);
        });

        services.AddSingleton<AudioNormalizer>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AudioNormalizer>>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var ffmpegPath = settings.GetFfmpegPath();
            return new AudioNormalizer(logger, ffmpegPath);
        });

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
            var offlineOnly = settings.IsOfflineOnly();
            var voiceCache = sp.GetService<VoiceCache>();
            var ffmpegPath = settings.GetFfmpegPath();

            // Only create if API key is available
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("ElevenLabs API key not configured, skipping provider registration");
                return null!;
            }

            return new ElevenLabsTtsProvider(logger, httpClient, apiKey, offlineOnly, voiceCache, ffmpegPath);
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

            // Only register if both paths are configured AND files exist
            if (string.IsNullOrWhiteSpace(piperPath) || string.IsNullOrWhiteSpace(modelPath))
            {
                logger.LogDebug("Piper TTS paths not configured - provider will not be available");
                return null!;
            }

            if (!File.Exists(piperPath))
            {
                logger.LogWarning("Piper executable not found at {Path} - provider will not be available", piperPath);
                return null!;
            }

            if (!File.Exists(modelPath))
            {
                logger.LogWarning("Piper model not found at {Path} - provider will not be available", modelPath);
                return null!;
            }

            logger.LogInformation("Registering Piper TTS provider with executable at {Executable}", piperPath);
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
        // Register Stable Diffusion WebUI provider (local GPU-based generation)
        services.AddSingleton<IImageProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Images.StableDiffusionWebUiProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var settings = sp.GetRequiredService<ProviderSettings>();

            var sdUrl = settings.GetStableDiffusionUrl();

            // Check if SD WebUI is configured
            if (string.IsNullOrWhiteSpace(sdUrl))
            {
                logger.LogDebug("Stable Diffusion WebUI URL not configured, skipping provider registration");
                return null!;
            }

            // Try to get hardware info for optimization, but don't require it
            bool isNvidiaGpu = false;
            int vramGB = 0;
            try
            {
                var hardwareDetector = sp.GetService<Aura.Core.Hardware.HardwareDetector>();
                if (hardwareDetector != null)
                {
                    var systemProfile = hardwareDetector.DetectSystemAsync().GetAwaiter().GetResult();
                    isNvidiaGpu = systemProfile.Gpu?.Vendor?.ToUpperInvariant() == "NVIDIA";
                    vramGB = systemProfile.Gpu?.VramGB ?? 0;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not detect hardware, using defaults for SD WebUI provider");
            }

            return new Images.StableDiffusionWebUiProvider(
                logger,
                httpClient,
                sdUrl,
                isNvidiaGpu,
                vramGB,
                defaultParams: null,
                bypassHardwareChecks: false);
        });

        // Register stock image providers (Unsplash, Pexels, Pixabay) as IEnhancedStockProvider
        // These serve as fallback options when generation fails or for cost optimization

        // Unsplash provider (requires API key)
        services.AddSingleton<Aura.Core.Providers.IEnhancedStockProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Images.EnhancedUnsplashProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var keyStore = sp.GetRequiredService<IKeyStore>();

            var apiKeys = keyStore.GetAllKeys();
            apiKeys.TryGetValue("unsplash", out var apiKey);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("Unsplash API key not configured, skipping provider registration");
                return null!;
            }

            return new Images.EnhancedUnsplashProvider(logger, httpClient, apiKey);
        });

        // Pexels provider (requires API key)
        services.AddSingleton<Aura.Core.Providers.IEnhancedStockProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Images.EnhancedPexelsProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var keyStore = sp.GetRequiredService<IKeyStore>();

            var apiKeys = keyStore.GetAllKeys();
            apiKeys.TryGetValue("pexels", out var apiKey);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("Pexels API key not configured, skipping provider registration");
                return null!;
            }

            return new Images.EnhancedPexelsProvider(logger, httpClient, apiKey);
        });

        // Pixabay provider (requires API key)
        services.AddSingleton<Aura.Core.Providers.IEnhancedStockProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Images.EnhancedPixabayProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var keyStore = sp.GetRequiredService<IKeyStore>();

            var apiKeys = keyStore.GetAllKeys();
            apiKeys.TryGetValue("pixabay", out var apiKey);

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogDebug("Pixabay API key not configured, skipping provider registration");
                return null!;
            }

            return new Images.EnhancedPixabayProvider(logger, httpClient, apiKey);
        });

        // Register placeholder provider as final fallback (always available)
        services.AddSingleton<IStockProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Images.PlaceholderImageProvider>>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var outputDirectory = Path.Combine(settings.GetAuraDataDirectory(), "placeholders");

            return new Images.PlaceholderImageProvider(logger, outputDirectory);
        });

        return services;
    }

    /// <summary>
    /// Registers all Music providers with their dependencies
    /// Providers are registered as IMusicProvider implementations for enumeration
    /// </summary>
    public static IServiceCollection AddMusicProviders(this IServiceCollection services)
    {
        // Register Local Stock Music Provider (always available for local libraries)
        services.AddSingleton<Aura.Core.Providers.IMusicProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Music.LocalStockMusicProvider>>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var musicLibraryPath = System.IO.Path.Combine(settings.GetAuraDataDirectory(), "Music");
            return new Music.LocalStockMusicProvider(logger, musicLibraryPath);
        });

        return services;
    }

    /// <summary>
    /// Registers all SFX (Sound Effects) providers with their dependencies
    /// Providers are registered as ISfxProvider implementations for enumeration
    /// </summary>
    public static IServiceCollection AddSfxProviders(this IServiceCollection services)
    {
        // Register Freesound SFX Provider (requires API key)
        services.AddSingleton<Aura.Core.Providers.ISfxProvider>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Sfx.FreesoundSfxProvider>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var configuration = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var apiKey = configuration["Audio:FreesoundApiKey"];

            // Create provider even without API key (will return IsAvailable = false)
            return new Sfx.FreesoundSfxProvider(logger, httpClient, apiKey);
        });

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
        services.AddSingleton<Aura.Core.Services.Providers.ProviderConnectionValidationService>();
        services.AddSingleton<Aura.Core.Services.Providers.IProviderReadinessService, Aura.Core.Services.Providers.ProviderReadinessService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderHealthMonitoringService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderCostTrackingService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderStatusService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderCircuitBreakerService>();
        services.AddSingleton<Aura.Core.Services.Providers.ProviderFallbackService>();
        services.AddSingleton<Aura.Core.Services.Health.ProviderHealthMonitor>();
        services.AddSingleton<Aura.Core.Services.Health.ProviderHealthService>();

        // Register LLM provider-specific services
        services.AddSingleton<Aura.Core.Services.Providers.LlmProviderValidator>();
        services.AddSingleton<Aura.Core.Services.Providers.LlmProviderCircuitBreaker>();

        // Ollama detection service (for background detection and caching)
        services.AddSingleton<Aura.Core.Services.Providers.OllamaDetectionService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<Aura.Core.Services.Providers.OllamaDetectionService>>();
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var settings = sp.GetRequiredService<ProviderSettings>();
            var baseUrl = settings.GetOllamaUrl();
            return new Aura.Core.Services.Providers.OllamaDetectionService(logger, httpClient, cache, baseUrl);
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
