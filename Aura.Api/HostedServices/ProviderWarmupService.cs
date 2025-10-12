using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aura.Core.Orchestrator;
using Aura.Providers.Planner;

namespace Aura.Api.HostedServices;

/// <summary>
/// Background service that warms up providers after application startup.
/// Logs any provider initialization issues but never throws exceptions that could crash the application.
/// </summary>
public class ProviderWarmupService : IHostedService
{
    private readonly ILogger<ProviderWarmupService> _logger;
    private readonly LlmProviderFactory _llmProviderFactory;
    private readonly PlannerProviderFactory _plannerProviderFactory;
    private readonly ILoggerFactory _loggerFactory;

    public ProviderWarmupService(
        ILogger<ProviderWarmupService> logger,
        LlmProviderFactory llmProviderFactory,
        PlannerProviderFactory plannerProviderFactory,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _llmProviderFactory = llmProviderFactory;
        _plannerProviderFactory = plannerProviderFactory;
        _loggerFactory = loggerFactory;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Run warmup in background - don't block startup
        _ = Task.Run(async () =>
        {
            try
            {
                // Small delay to let the application finish starting
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                _logger.LogInformation("Starting provider warmup...");

                // Warmup LLM providers
                try
                {
                    var llmProviders = _llmProviderFactory.CreateAvailableProviders(_loggerFactory);
                    _logger.LogInformation("LLM providers warmed up: {Count} providers available ({Providers})",
                        llmProviders.Count, string.Join(", ", llmProviders.Keys));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warmup LLM providers (non-critical): {Message}", ex.Message);
                }

                // Warmup Planner providers
                try
                {
                    var plannerProviders = _plannerProviderFactory.CreateAvailableProviders(_loggerFactory);
                    _logger.LogInformation("Planner providers warmed up: {Count} providers available ({Providers})",
                        plannerProviders.Count, string.Join(", ", plannerProviders.Keys));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to warmup Planner providers (non-critical): {Message}", ex.Message);
                }

                _logger.LogInformation("Provider warmup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider warmup service encountered an error (non-critical): {Message}", ex.Message);
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Provider warmup service stopping");
        return Task.CompletedTask;
    }
}
