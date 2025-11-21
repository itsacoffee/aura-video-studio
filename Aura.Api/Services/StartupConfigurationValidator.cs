using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Data;
using Aura.Core.Services.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Validates and ensures critical configuration is loaded before the application accepts requests
/// Runs during startup to detect and persist FFmpeg configuration
/// </summary>
public class StartupConfigurationValidator : IHostedService
{
    private readonly ILogger<StartupConfigurationValidator> _logger;
    private readonly IConfiguration _configuration;
    private readonly IFFmpegDetectionService _ffmpegDetection;
    private readonly FFmpegConfigurationStore _configStore;
    private readonly IServiceProvider _serviceProvider;

    public StartupConfigurationValidator(
        ILogger<StartupConfigurationValidator> logger,
        IConfiguration configuration,
        IFFmpegDetectionService ffmpegDetection,
        FFmpegConfigurationStore configStore,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _ffmpegDetection = ffmpegDetection ?? throw new ArgumentNullException(nameof(ffmpegDetection));
        _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Validating startup configuration...");

        try
        {
            // 1. Check database connectivity
            await ValidateDatabaseAsync(cancellationToken).ConfigureAwait(false);

            // 2. Load or detect FFmpeg
            await EnsureFFmpegConfigurationAsync(cancellationToken).ConfigureAwait(false);

            // 3. Validate essential directories exist
            await EnsureDirectoriesAsync(cancellationToken).ConfigureAwait(false);

            // 4. Log startup configuration summary
            LogStartupSummary();

            _logger.LogInformation("Startup configuration validation complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup configuration validation failed");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping startup configuration validator");
        return Task.CompletedTask;
    }

    private async Task ValidateDatabaseAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Checking database connectivity...");
            
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
            
            var canConnect = await dbContext.Database.CanConnectAsync(ct).ConfigureAwait(false);
            
            if (canConnect)
            {
                _logger.LogInformation("Database connectivity verified");
            }
            else
            {
                _logger.LogWarning("Database connectivity check returned false");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database connectivity check failed - will retry via health checks");
        }
    }

    private async Task EnsureFFmpegConfigurationAsync(CancellationToken ct)
    {
        _logger.LogInformation("Ensuring FFmpeg configuration...");

        // Check environment variable first (Electron or external process)
        var envPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        {
            _logger.LogInformation("FFmpeg path from environment variable: {Path}", envPath);
            await _configStore.SaveAsync(new FFmpegConfiguration
            {
                Path = envPath,
                Mode = FFmpegMode.System,
                Source = "Environment",
                LastValidatedAt = DateTime.UtcNow,
                LastValidationResult = FFmpegValidationResult.Ok
            }, ct).ConfigureAwait(false);
            return;
        }

        // Check stored configuration
        var stored = await _configStore.LoadAsync(ct).ConfigureAwait(false);
        if (stored != null && !string.IsNullOrEmpty(stored.Path) && File.Exists(stored.Path))
        {
            _logger.LogInformation("FFmpeg configuration loaded from storage: {Path} (Source: {Source})", 
                stored.Path, stored.Source ?? "Unknown");
            return;
        }

        // Auto-detect and persist
        _logger.LogWarning("No valid FFmpeg configuration found, attempting auto-detection...");
        var detected = await _ffmpegDetection.DetectFFmpegAsync(ct).ConfigureAwait(false);
        
        if (detected.IsInstalled && !string.IsNullOrEmpty(detected.Path))
        {
            _logger.LogInformation("FFmpeg auto-detected at: {Path}, Version: {Version}", 
                detected.Path, detected.Version ?? "Unknown");
        }
        else
        {
            _logger.LogWarning(
                "FFmpeg not found during startup. Video generation will fail. " +
                "Error: {Error}", 
                detected.Error ?? "Unknown error");
        }
    }

    private Task EnsureDirectoriesAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Validating essential directories...");

            var outputDir = _configuration["OutputDirectory"] 
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    "AuraVideoStudio", "Output");

            var logsDir = _configuration["LogsDirectory"]
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AuraVideoStudio", "Logs");

            var directories = new[] { outputDir, logsDir };

            foreach (var dir in directories)
            {
                if (!Directory.Exists(dir))
                {
                    _logger.LogInformation("Creating directory: {Directory}", dir);
                    Directory.CreateDirectory(dir);
                }
            }

            _logger.LogInformation("Essential directories validated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate essential directories");
            throw;
        }

        return Task.CompletedTask;
    }

    private void LogStartupSummary()
    {
        try
        {
            _logger.LogInformation("=== Startup Configuration Summary ===");
            
            var outputDir = _configuration["OutputDirectory"];
            if (!string.IsNullOrEmpty(outputDir))
            {
                _logger.LogInformation("Output Directory: {OutputDir}", outputDir);
            }

            var apiUrl = Environment.GetEnvironmentVariable("AURA_API_URL") 
                ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") 
                ?? "http://127.0.0.1:5005";
            _logger.LogInformation("API URL: {ApiUrl}", apiUrl);

            var databaseProvider = _configuration.GetValue<string>("Database:Provider") ?? "SQLite";
            _logger.LogInformation("Database Provider: {Provider}", databaseProvider);

            _logger.LogInformation("=====================================");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log startup summary");
        }
    }
}
