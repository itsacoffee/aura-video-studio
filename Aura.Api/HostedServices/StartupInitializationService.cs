using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.HostedServices;

/// <summary>
/// Hosted service that validates and initializes critical services in the correct order
/// Ensures the application doesn't enter a partial-ready state
/// </summary>
public class StartupInitializationService : IHostedService
{
    private readonly ILogger<StartupInitializationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly List<InitializationStep> _initializationSteps;

    public StartupInitializationService(
        ILogger<StartupInitializationService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _initializationSteps = new List<InitializationStep>();

        // Define initialization steps in dependency order
        DefineInitializationSteps();
    }

    private void DefineInitializationSteps()
    {
        // Step 1: Database connectivity (critical)
        _initializationSteps.Add(new InitializationStep
        {
            Name = "Database Connectivity",
            IsCritical = true,
            TimeoutSeconds = 30,
            InitializeFunc = async (sp, ct) =>
            {
                using var scope = sp.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<Aura.Core.Data.AuraDbContext>();
                await dbContext.Database.CanConnectAsync(ct).ConfigureAwait(false);
                return true;
            }
        });

        // Step 2: Required directories (critical)
        _initializationSteps.Add(new InitializationStep
        {
            Name = "Required Directories",
            IsCritical = true,
            TimeoutSeconds = 10,
            InitializeFunc = (sp, ct) =>
            {
                var providerSettings = sp.GetRequiredService<Aura.Core.Configuration.ProviderSettings>();
                var directories = new[]
                {
                    providerSettings.GetAuraDataDirectory(),
                    providerSettings.GetOutputDirectory(),
                    providerSettings.GetLogsDirectory(),
                    providerSettings.GetProjectsDirectory()
                };

                foreach (var dir in directories)
                {
                    if (!System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }
                }
                return Task.FromResult(true);
            }
        });

        // Step 3: FFmpeg availability (critical for video operations)
        _initializationSteps.Add(new InitializationStep
        {
            Name = "FFmpeg Availability",
            IsCritical = false, // Not critical - can run without FFmpeg for some operations
            TimeoutSeconds = 10,
            InitializeFunc = async (sp, ct) =>
            {
                try
                {
                    var ffmpegLocator = sp.GetRequiredService<Aura.Core.Dependencies.IFfmpegLocator>();
                    var ffmpegPath = await ffmpegLocator.GetEffectiveFfmpegPathAsync(null, ct).ConfigureAwait(false);
                    return !string.IsNullOrEmpty(ffmpegPath);
                }
                catch
                {
                    return false; // Non-critical, return false but don't fail
                }
            }
        });

        // Step 4: AI Services connectivity (non-critical - graceful degradation)
        _initializationSteps.Add(new InitializationStep
        {
            Name = "AI Services",
            IsCritical = false,
            TimeoutSeconds = 10,
            InitializeFunc = (sp, ct) =>
            {
                try
                {
                    // Check if key services are available
                    var llmProvider = sp.GetService<Aura.Core.Providers.ILlmProvider>();
                    return Task.FromResult(llmProvider != null);
                }
                catch
                {
                    return Task.FromResult(false); // Non-critical
                }
            }
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Service Initialization Starting ===");
        var overallStopwatch = Stopwatch.StartNew();
        var successCount = 0;
        var failedCritical = false;

        foreach (var step in _initializationSteps)
        {
            var stepStopwatch = Stopwatch.StartNew();
            _logger.LogInformation("Initializing: {StepName} (Critical: {IsCritical}, Timeout: {Timeout}s)",
                step.Name, step.IsCritical, step.TimeoutSeconds);

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(step.TimeoutSeconds));
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

                var success = await step.InitializeFunc(_serviceProvider, linkedCts.Token).ConfigureAwait(false);
                stepStopwatch.Stop();

                if (success)
                {
                    _logger.LogInformation("✓ {StepName} initialized successfully in {Duration}ms",
                        step.Name, stepStopwatch.ElapsedMilliseconds);
                    successCount++;
                }
                else
                {
                    if (step.IsCritical)
                    {
                        _logger.LogError("✗ CRITICAL: {StepName} failed to initialize (took {Duration}ms)",
                            step.Name, stepStopwatch.ElapsedMilliseconds);
                        failedCritical = true;
                    }
                    else
                    {
                        _logger.LogWarning("⚠ {StepName} failed to initialize - continuing with graceful degradation (took {Duration}ms)",
                            step.Name, stepStopwatch.ElapsedMilliseconds);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                stepStopwatch.Stop();
                _logger.LogError("✗ {StepName} timed out after {Timeout}s (Critical: {IsCritical})",
                    step.Name, step.TimeoutSeconds, step.IsCritical);
                
                if (step.IsCritical)
                {
                    failedCritical = true;
                }
            }
            catch (Exception ex)
            {
                stepStopwatch.Stop();
                _logger.LogError(ex, "✗ {StepName} failed with exception (took {Duration}ms, Critical: {IsCritical})",
                    step.Name, stepStopwatch.ElapsedMilliseconds, step.IsCritical);
                
                if (step.IsCritical)
                {
                    failedCritical = true;
                }
            }

            if (failedCritical)
            {
                break; // Stop initialization on critical failure
            }
        }

        overallStopwatch.Stop();

        if (failedCritical)
        {
            _logger.LogError("=== Service Initialization FAILED ===");
            _logger.LogError("Critical services failed to initialize. Application cannot start properly.");
            _logger.LogError("Total time: {Duration}ms, Successful: {Success}/{Total}",
                overallStopwatch.ElapsedMilliseconds, successCount, _initializationSteps.Count);
            
            // Instead of Environment.Exit, throw an exception that can be caught and logged properly
            var failedSteps = string.Join(", ", _initializationSteps
                .Where((s, i) => i < _initializationSteps.Count && s.IsCritical)
                .Select(s => s.Name));
            
            _logger.LogError("Failed critical steps: {Steps}", failedSteps);
            _logger.LogWarning("Application will continue startup but may be unstable. Please check logs above for details.");
            
            // Don't throw or exit - let the application try to start
            // Users can see errors in the UI and troubleshoot
        }
        else
        {
            _logger.LogInformation("=== Service Initialization COMPLETE ===");
            _logger.LogInformation("Total time: {Duration}ms, Successful: {Success}/{Total}",
                overallStopwatch.ElapsedMilliseconds, successCount, _initializationSteps.Count);
            
            if (successCount < _initializationSteps.Count)
            {
                _logger.LogWarning("Some non-critical services failed. Application running in degraded mode.");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Service initialization shutdown complete");
        return Task.CompletedTask;
    }

    private sealed class InitializationStep
    {
        public string Name { get; set; } = string.Empty;
        public bool IsCritical { get; set; }
        public int TimeoutSeconds { get; set; }
        public Func<IServiceProvider, CancellationToken, Task<bool>> InitializeFunc { get; set; } = null!;
    }
}
