using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Runtime;

/// <summary>
/// Notification event for engine lifecycle changes
/// </summary>
public record EngineNotification(
    string EngineId,
    string EngineName,
    EngineNotificationType Type,
    string Message,
    DateTime Timestamp
);

public enum EngineNotificationType
{
    Started,
    Stopped,
    HealthCheckPassed,
    HealthCheckFailed,
    Crashed,
    Restarting,
    RestartLimitReached,
    Warning
}

/// <summary>
/// Diagnostics report for an engine
/// </summary>
public record EngineDiagnostic(
    string EngineId,
    string Name,
    bool IsRunning,
    bool IsHealthy,
    DateTime? LastStarted,
    int RestartCount,
    string? LastError,
    int? ProcessId,
    int? Port,
    string? HealthCheckUrl
);

/// <summary>
/// Overall diagnostics report for all engines
/// </summary>
public record SystemDiagnosticsReport(
    DateTime GeneratedAt,
    int TotalEngines,
    int RunningEngines,
    int HealthyEngines,
    List<EngineDiagnostic> Engines
);

/// <summary>
/// Manages the lifecycle of engines: auto-start on app launch, graceful shutdown, crash recovery
/// </summary>
public class EngineLifecycleManager : IDisposable
{
    private readonly ILogger<EngineLifecycleManager> _logger;
    private readonly LocalEnginesRegistry _registry;
    private readonly ExternalProcessManager _processManager;
    private readonly ConcurrentDictionary<string, int> _restartCounts = new();
    private readonly ConcurrentQueue<EngineNotification> _notifications = new();
    private readonly int _maxRestartAttempts;
    private CancellationTokenSource? _lifecycleCts;
    private Task? _monitoringTask;
    
    public event EventHandler<EngineNotification>? NotificationReceived;

    public EngineLifecycleManager(
        ILogger<EngineLifecycleManager> logger,
        LocalEnginesRegistry registry,
        ExternalProcessManager processManager,
        int maxRestartAttempts = 3)
    {
        _logger = logger;
        _registry = registry;
        _processManager = processManager;
        _maxRestartAttempts = maxRestartAttempts;
    }

    /// <summary>
    /// Start the lifecycle manager and auto-launch configured engines
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting Engine Lifecycle Manager");
        
        _lifecycleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // Start monitoring task for crash detection
        _monitoringTask = Task.Run(() => MonitorEnginesAsync(_lifecycleCts.Token), _lifecycleCts.Token);
        
        // Auto-launch engines marked for startup
        await _registry.StartAutoLaunchEnginesAsync(cancellationToken);
        
        // Wait a bit and check health of started engines
        await Task.Delay(2000, cancellationToken);
        
        var engines = _registry.GetAllEngines();
        foreach (var engine in engines.Where(e => e.StartOnAppLaunch))
        {
            await CheckEngineHealthAsync(engine.Id, cancellationToken);
        }
        
        _logger.LogInformation("Engine Lifecycle Manager started successfully");
    }

    /// <summary>
    /// Stop the lifecycle manager and gracefully shutdown all running engines
    /// </summary>
    public async Task StopAsync()
    {
        _logger.LogInformation("Stopping Engine Lifecycle Manager");
        
        // Cancel monitoring
        _lifecycleCts?.Cancel();
        
        // Wait for monitoring task to complete
        if (_monitoringTask != null)
        {
            try
            {
                await _monitoringTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
        
        // Stop all engines gracefully
        await _registry.StopAllEnginesAsync();
        
        _logger.LogInformation("Engine Lifecycle Manager stopped");
    }

    /// <summary>
    /// Get recent notifications (last 100)
    /// </summary>
    public IReadOnlyList<EngineNotification> GetRecentNotifications(int count = 100)
    {
        return _notifications.TakeLast(count).ToList();
    }

    /// <summary>
    /// Generate a comprehensive diagnostics report
    /// </summary>
    public async Task<SystemDiagnosticsReport> GenerateDiagnosticsAsync()
    {
        _logger.LogInformation("Generating system diagnostics report");
        
        var allEngines = _registry.GetAllEngines();
        var diagnostics = new List<EngineDiagnostic>();
        
        foreach (var engine in allEngines)
        {
            var status = await _registry.GetEngineStatusAsync(engine.Id);
            var processStatus = _processManager.GetStatus(engine.Id);
            var restartCount = _restartCounts.GetValueOrDefault(engine.Id, 0);
            
            diagnostics.Add(new EngineDiagnostic(
                engine.Id,
                engine.Name,
                status.IsRunning,
                status.IsHealthy,
                status.LastStarted,
                restartCount,
                status.Error,
                processStatus.ProcessId,
                engine.Port,
                engine.HealthCheckUrl
            ));
        }
        
        var report = new SystemDiagnosticsReport(
            DateTime.UtcNow,
            allEngines.Count,
            diagnostics.Count(d => d.IsRunning),
            diagnostics.Count(d => d.IsHealthy),
            diagnostics
        );
        
        _logger.LogInformation("Diagnostics report generated: {Total} engines, {Running} running, {Healthy} healthy",
            report.TotalEngines, report.RunningEngines, report.HealthyEngines);
        
        return report;
    }

    /// <summary>
    /// Restart a specific engine
    /// </summary>
    public async Task<bool> RestartEngineAsync(string engineId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Manual restart requested for engine {EngineId}", engineId);
        
        var engine = _registry.GetEngine(engineId);
        if (engine == null)
        {
            _logger.LogWarning("Cannot restart engine {EngineId}: not found", engineId);
            return false;
        }
        
        // Stop the engine
        await _registry.StopEngineAsync(engineId);
        await Task.Delay(1000, cancellationToken);
        
        // Start it again
        var started = await _registry.StartEngineAsync(engineId, cancellationToken);
        
        if (started)
        {
            AddNotification(engineId, engine.Name, EngineNotificationType.Started,
                $"{engine.Name} restarted manually");
        }
        
        return started;
    }

    private async Task MonitorEnginesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Engine monitoring task started");
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(5000, cancellationToken); // Check every 5 seconds
                
                var engines = _registry.GetAllEngines()
                    .Where(e => e.AutoRestart)
                    .ToList();
                
                foreach (var engine in engines)
                {
                    var status = _processManager.GetStatus(engine.Id);
                    
                    // Check if engine was running but has now stopped (crashed)
                    if (!status.IsRunning && _restartCounts.ContainsKey(engine.Id))
                    {
                        await HandleEngineCrashAsync(engine, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in engine monitoring task");
            }
        }
        
        _logger.LogDebug("Engine monitoring task stopped");
    }

    private async Task HandleEngineCrashAsync(EngineConfig engine, CancellationToken cancellationToken)
    {
        var restartCount = _restartCounts.AddOrUpdate(engine.Id, 1, (_, count) => count + 1);
        
        _logger.LogWarning("Engine {EngineId} crashed (restart attempt {Count}/{Max})",
            engine.Id, restartCount, _maxRestartAttempts);
        
        if (restartCount > _maxRestartAttempts)
        {
            _logger.LogError("Engine {EngineId} has exceeded maximum restart attempts ({Max})",
                engine.Id, _maxRestartAttempts);
            
            AddNotification(engine.Id, engine.Name, EngineNotificationType.RestartLimitReached,
                $"{engine.Name} has crashed {_maxRestartAttempts} times and will not be restarted automatically");
            
            return;
        }
        
        AddNotification(engine.Id, engine.Name, EngineNotificationType.Restarting,
            $"{engine.Name} crashed and is being restarted (attempt {restartCount}/{_maxRestartAttempts})");
        
        // Wait a bit before restarting
        await Task.Delay(5000, cancellationToken);
        
        try
        {
            var started = await _registry.StartEngineAsync(engine.Id, cancellationToken);
            
            if (started)
            {
                _logger.LogInformation("Engine {EngineId} restarted successfully", engine.Id);
                AddNotification(engine.Id, engine.Name, EngineNotificationType.Started,
                    $"{engine.Name} restarted after crash");
            }
            else
            {
                _logger.LogError("Failed to restart engine {EngineId}", engine.Id);
                AddNotification(engine.Id, engine.Name, EngineNotificationType.Crashed,
                    $"Failed to restart {engine.Name}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting engine {EngineId}", engine.Id);
            AddNotification(engine.Id, engine.Name, EngineNotificationType.Crashed,
                $"Error restarting {engine.Name}: {ex.Message}");
        }
    }

    private async Task CheckEngineHealthAsync(string engineId, CancellationToken cancellationToken)
    {
        var engine = _registry.GetEngine(engineId);
        if (engine == null) return;
        
        var status = await _registry.GetEngineStatusAsync(engineId);
        
        if (status.IsRunning && status.IsHealthy)
        {
            var port = engine.Port ?? 0;
            AddNotification(engineId, engine.Name, EngineNotificationType.HealthCheckPassed,
                $"{engine.Name} started successfully on port {port}");
            
            // Reset restart count on successful health check
            _restartCounts[engineId] = 0;
        }
        else if (status.IsRunning && !status.IsHealthy)
        {
            _logger.LogWarning("Engine {EngineId} is running but health check failed", engineId);
            AddNotification(engineId, engine.Name, EngineNotificationType.HealthCheckFailed,
                $"{engine.Name} is running but health check failed");
        }
    }

    private void AddNotification(string engineId, string engineName, EngineNotificationType type, string message)
    {
        var notification = new EngineNotification(engineId, engineName, type, message, DateTime.UtcNow);
        _notifications.Enqueue(notification);
        
        // Keep only last 1000 notifications
        while (_notifications.Count > 1000)
        {
            _notifications.TryDequeue(out _);
        }
        
        // Raise event
        NotificationReceived?.Invoke(this, notification);
        
        // Log based on severity
        var logMessage = $"[{engineName}] {message}";
        switch (type)
        {
            case EngineNotificationType.Crashed:
            case EngineNotificationType.RestartLimitReached:
            case EngineNotificationType.HealthCheckFailed:
                _logger.LogError(logMessage);
                break;
            case EngineNotificationType.Warning:
            case EngineNotificationType.Restarting:
                _logger.LogWarning(logMessage);
                break;
            default:
                _logger.LogInformation(logMessage);
                break;
        }
    }

    public void Dispose()
    {
        _lifecycleCts?.Cancel();
        _lifecycleCts?.Dispose();
        GC.SuppressFinalize(this);
    }
}
