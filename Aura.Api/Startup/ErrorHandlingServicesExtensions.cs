using Aura.Core.Services.Diagnostics;
using Aura.Core.Services.ErrorHandling;

namespace Aura.Api.Startup;

/// <summary>
/// Extension methods for registering error handling services
/// </summary>
public static class ErrorHandlingServicesExtensions
{
    /// <summary>
    /// Add comprehensive error handling services to the DI container
    /// </summary>
    public static IServiceCollection AddErrorHandlingServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get error logging configuration
        var logPath = configuration.GetValue<string>("ErrorHandling:LogPath");
        var maxLogSizeMb = configuration.GetValue<int>("ErrorHandling:MaxLogSizeMb", 100);

        // Register error logging service
        services.AddSingleton(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<ErrorLoggingService>>();
            return new ErrorLoggingService(logger, logPath, maxLogSizeMb);
        });

        // Register error recovery service
        services.AddSingleton<ErrorRecoveryService>();

        // Register graceful degradation service
        services.AddSingleton<GracefulDegradationService>();

        // Register error aggregation service (already exists)
        services.AddSingleton<ErrorAggregationService>();

        // Register hosted service for periodic error log flushing
        services.AddHostedService<ErrorLoggingFlushService>();

        // Register hosted service for periodic cleanup
        services.AddHostedService<ErrorLoggingCleanupService>();

        return services;
    }
}

/// <summary>
/// Hosted service that periodically flushes error logs to disk
/// </summary>
internal class ErrorLoggingFlushService : BackgroundService
{
    private readonly ErrorLoggingService _errorLoggingService;
    private readonly ILogger<ErrorLoggingFlushService> _logger;
    private readonly TimeSpan _flushInterval;

    public ErrorLoggingFlushService(
        ErrorLoggingService errorLoggingService,
        ILogger<ErrorLoggingFlushService> logger,
        IConfiguration configuration)
    {
        _errorLoggingService = errorLoggingService;
        _logger = logger;
        _flushInterval = TimeSpan.FromSeconds(
            configuration.GetValue<int>("ErrorHandling:FlushIntervalSeconds", 30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Error logging flush service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_flushInterval, stoppingToken);
                await _errorLoggingService.FlushErrorsAsync();
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing error logs");
            }
        }

        // Final flush on shutdown
        try
        {
            _logger.LogInformation("Performing final error log flush before shutdown");
            await _errorLoggingService.FlushErrorsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform final error log flush");
        }

        _logger.LogInformation("Error logging flush service stopped");
    }
}

/// <summary>
/// Hosted service that periodically cleans up old error logs
/// </summary>
internal class ErrorLoggingCleanupService : BackgroundService
{
    private readonly ErrorLoggingService _errorLoggingService;
    private readonly ErrorAggregationService _errorAggregationService;
    private readonly ILogger<ErrorLoggingCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _retentionPeriod;

    public ErrorLoggingCleanupService(
        ErrorLoggingService errorLoggingService,
        ErrorAggregationService errorAggregationService,
        ILogger<ErrorLoggingCleanupService> logger,
        IConfiguration configuration)
    {
        _errorLoggingService = errorLoggingService;
        _errorAggregationService = errorAggregationService;
        _logger = logger;
        _cleanupInterval = TimeSpan.FromHours(
            configuration.GetValue<int>("ErrorHandling:CleanupIntervalHours", 24));
        _retentionPeriod = TimeSpan.FromDays(
            configuration.GetValue<int>("ErrorHandling:RetentionDays", 30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Error logging cleanup service started (cleanup every {Interval}, retention {Retention})",
            _cleanupInterval,
            _retentionPeriod);

        // Wait before first cleanup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting error log cleanup");

                var filesDeleted = await _errorLoggingService.CleanupOldLogsAsync(_retentionPeriod);
                var aggregatedCleared = _errorAggregationService.ClearOldErrors(_retentionPeriod);

                _logger.LogInformation(
                    "Error log cleanup completed: {FilesDeleted} files deleted, {AggregatedCleared} aggregated errors cleared",
                    filesDeleted,
                    aggregatedCleared);

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during error log cleanup");
                // Wait a bit before retrying
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }

        _logger.LogInformation("Error logging cleanup service stopped");
    }
}
