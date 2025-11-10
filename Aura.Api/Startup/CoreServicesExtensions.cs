using Aura.Core.Configuration;
using Aura.Core.Data;
using Aura.Core.Dependencies;
using Aura.Core.Hardware;
using Aura.Core.Services;
using Aura.Core.Services.FFmpeg;
using Aura.Core.Services.Video;
using Microsoft.EntityFrameworkCore;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for registering core infrastructure services.
/// </summary>
public static class CoreServicesExtensions
{
    /// <summary>
    /// Registers core infrastructure services including hardware detection,
    /// dependency management, and data persistence.
    /// </summary>
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Hardware detection
        services.AddSingleton<HardwareDetector>();
        services.AddSingleton<IHardwareDetector>(sp => sp.GetRequiredService<HardwareDetector>());
        services.AddSingleton<DiagnosticsHelper>();

        // Configuration and settings
        services.AddSingleton<ProviderSettings>();
        services.AddSingleton<IKeyStore, KeyStore>();

        // Dependency management
        services.AddSingleton<IFfmpegLocator>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<FfmpegLocator>>();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var toolsDir = providerSettings.GetToolsDirectory();
            return new FfmpegLocator(logger, toolsDir);
        });

        // FFmpeg services
        services.AddSingleton<IProcessManager, ProcessManager>();
        services.AddSingleton<IFFmpegService, FFmpegService>();
        services.AddSingleton<IFFmpegExecutor, FFmpegExecutor>();
        
        // Video Effects Services
        services.AddSingleton<Aura.Core.Services.VideoEffects.IVideoEffectService, Aura.Core.Services.VideoEffects.VideoEffectService>();
        services.AddSingleton<Aura.Core.Services.VideoEffects.IEffectCacheService, Aura.Core.Services.VideoEffects.EffectCacheService>();
        services.AddSingleton<IHardwareAccelerationDetector, HardwareAccelerationDetector>();

        // Video services
        services.AddScoped<IVideoComposer, VideoComposer>();
        services.AddScoped<ISubtitleGenerator, SubtitleGenerator>();
        services.AddScoped<IAudioMixer, AudioMixer>();

        // Data persistence - Configure database with WAL mode for better concurrency
        const string MigrationsAssembly = "Aura.Api";
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "aura.db");
        services.AddDbContext<AuraDbContext>(options =>
        {
            var connectionString = $"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared;";
            options.UseSqlite(connectionString,
                sqliteOptions => sqliteOptions.MigrationsAssembly(MigrationsAssembly));
        });

        services.AddScoped<ProjectStateRepository>();
        services.AddScoped<CheckpointManager>();
        services.AddScoped<IActionService, ActionService>();

        // Ollama service for process control
        services.AddSingleton<OllamaService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<OllamaService>>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient();
            var providerSettings = sp.GetRequiredService<ProviderSettings>();
            var logsDirectory = Path.Combine(providerSettings.GetLogsDirectory(), "ollama");
            return new OllamaService(logger, httpClient, logsDirectory);
        });

        // Resource management
        services.AddSingleton<ResourceCleanupManager>();

        return services;
    }
}
