using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Runtime;

public enum EngineMode
{
    Managed,   // App-controlled: can start/stop
    External   // User-managed: app only detects/uses
}

public record EngineConfig(
    string Id,           // Instance ID (unique per instance, e.g. "sd-webui-1", "sd-webui-external")
    string EngineId,     // Engine type ID (e.g. "sd-webui", "comfyui", "ffmpeg")
    string Name,
    string Version,
    EngineMode Mode,
    string InstallPath,
    string? ExecutablePath,
    string? Arguments,
    int? Port,
    string? HealthCheckUrl,
    bool StartOnAppLaunch,
    bool AutoRestart,
    string? Notes = null,
    IDictionary<string, string>? EnvironmentVariables = null
);

public record EngineStatus(
    string Id,
    string Name,
    EngineMode Mode,
    bool IsInstalled,
    bool IsRunning,
    bool IsHealthy,
    string? Version,
    string? InstallPath,
    int? Port,
    DateTime? LastStarted,
    string? Error
);

/// <summary>
/// Registry for managing local engine instances and their settings
/// </summary>
public class LocalEnginesRegistry
{
    private readonly ILogger<LocalEnginesRegistry> _logger;
    private readonly ExternalProcessManager _processManager;
    private readonly string _configPath;
    private readonly ConcurrentDictionary<string, EngineConfig> _engines = new();
    private readonly Task _initializationTask;
    private volatile bool _isInitialized;
    private volatile bool _initializationFailed;
    private Exception? _initializationException;

    public LocalEnginesRegistry(
        ILogger<LocalEnginesRegistry> logger,
        ExternalProcessManager processManager,
        string configPath)
    {
        _logger = logger;
        _processManager = processManager;
        _configPath = configPath;

        // Load config asynchronously in background to avoid blocking constructor
        // Store the task so callers can await initialization if needed
        _initializationTask = Task.Run(async () =>
        {
            try
            {
                await LoadConfigAsync().ConfigureAwait(false);
                _isInitialized = true;
                _initializationFailed = false;
                _logger.LogInformation("Engine registry initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load engine config in background during initialization");
                _isInitialized = true; // Mark as initialized even on error to prevent infinite waiting
                _initializationFailed = true;
                _initializationException = ex;
            }
        });
    }

    /// <summary>
    /// Wait for initialization to complete. Call this before using GetAllEngines() if you need guaranteed initialization.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if initialization failed</exception>
    public async Task WaitForInitializationAsync(CancellationToken ct = default)
    {
        await _initializationTask.WaitAsync(ct).ConfigureAwait(false);
        
        // Check if initialization failed and throw if so
        if (_initializationFailed)
        {
            throw new InvalidOperationException(
                "Engine registry initialization failed. Cannot access engines.",
                _initializationException);
        }
    }

    /// <summary>
    /// Check if initialization is complete
    /// </summary>
    public bool IsInitialized => _isInitialized;

    public async Task RegisterEngineAsync(EngineConfig config)
    {
        // Ensure initialization is complete before modifying engines
        await WaitForInitializationAsync().ConfigureAwait(false);
        
        _engines[config.Id] = config;
        await SaveConfigAsync().ConfigureAwait(false);
        _logger.LogInformation("Registered engine {Id} ({Name})", config.Id, config.Name);
    }

    public async Task UnregisterEngineAsync(string engineId)
    {
        // Ensure initialization is complete before modifying engines
        await WaitForInitializationAsync().ConfigureAwait(false);
        
        if (_engines.TryRemove(engineId, out var config))
        {
            if (_processManager.GetStatus(engineId).IsRunning)
            {
                await _processManager.StopAsync(engineId).ConfigureAwait(false);
            }

            await SaveConfigAsync().ConfigureAwait(false);
            _logger.LogInformation("Unregistered engine {Id}", engineId);
        }
    }

    public EngineConfig? GetEngine(string engineId)
    {
        // Ensure initialization is complete before accessing engines
        // Use Task.Run to avoid deadlocks when called from async contexts
        if (!_isInitialized)
        {
            _logger.LogWarning("GetEngine called before initialization complete, waiting...");
            var waitTask = Task.Run(async () => await WaitForInitializationAsync().ConfigureAwait(false));
            
            if (!waitTask.Wait(TimeSpan.FromSeconds(5)))
            {
                _logger.LogError("GetEngine: Initialization wait timed out after 5 seconds");
                throw new InvalidOperationException(
                    "Engine registry initialization timed out. The registry may not be fully loaded.");
            }
            
            // Check if initialization failed
            if (_initializationFailed)
            {
                _logger.LogError("GetEngine: Initialization failed, cannot access engines");
                throw new InvalidOperationException(
                    "Engine registry initialization failed. Cannot access engines.",
                    _initializationException);
            }
        }
        else if (_initializationFailed)
        {
            // Initialization completed but failed
            _logger.LogError("GetEngine: Attempting to access engines after failed initialization");
            throw new InvalidOperationException(
                "Engine registry initialization failed. Cannot access engines.",
                _initializationException);
        }
        
        return _engines.TryGetValue(engineId, out var config) ? config : null;
    }

    public IReadOnlyList<EngineConfig> GetAllEngines()
    {
        // Ensure initialization is complete before accessing engines
        // Use Task.Run to avoid deadlocks when called from async contexts
        if (!_isInitialized)
        {
            _logger.LogWarning("GetAllEngines called before initialization complete, waiting...");
            var waitTask = Task.Run(async () => await WaitForInitializationAsync().ConfigureAwait(false));
            
            if (!waitTask.Wait(TimeSpan.FromSeconds(5)))
            {
                _logger.LogError("GetAllEngines: Initialization wait timed out after 5 seconds");
                throw new InvalidOperationException(
                    "Engine registry initialization timed out. The registry may not be fully loaded.");
            }
            
            // Check if initialization failed
            if (_initializationFailed)
            {
                _logger.LogError("GetAllEngines: Initialization failed, cannot access engines");
                throw new InvalidOperationException(
                    "Engine registry initialization failed. Cannot access engines.",
                    _initializationException);
            }
        }
        else if (_initializationFailed)
        {
            // Initialization completed but failed
            _logger.LogError("GetAllEngines: Attempting to access engines after failed initialization");
            throw new InvalidOperationException(
                "Engine registry initialization failed. Cannot access engines.",
                _initializationException);
        }
        
        return _engines.Values.ToList();
    }

    public async Task<EngineStatus> GetEngineStatusAsync(string engineId)
    {
        var config = GetEngine(engineId);
        if (config == null)
        {
            return new EngineStatus(engineId, engineId, EngineMode.Managed, false, false, false, null, null, null, null, "Engine not registered");
        }

        var processStatus = _processManager.GetStatus(engineId);
        bool isInstalled = !string.IsNullOrEmpty(config.InstallPath) && Directory.Exists(config.InstallPath);
        bool isHealthy = false;

        if (processStatus.IsRunning && !string.IsNullOrEmpty(config.HealthCheckUrl))
        {
            isHealthy = await _processManager.CheckHealthAsync(engineId, config.HealthCheckUrl, CancellationToken.None).ConfigureAwait(false);
        }

        return new EngineStatus(
            config.Id,
            config.Name,
            config.Mode,
            isInstalled,
            processStatus.IsRunning,
            isHealthy,
            config.Version,
            config.InstallPath,
            config.Port,
            processStatus.StartTime,
            processStatus.LastError
        );
    }

    public async Task<bool> StartEngineAsync(string engineId, CancellationToken ct = default)
    {
        var config = GetEngine(engineId);
        if (config == null)
        {
            _logger.LogWarning("Cannot start engine {Id}: not registered", engineId);
            return false;
        }

        if (string.IsNullOrEmpty(config.ExecutablePath))
        {
            _logger.LogWarning("Cannot start engine {Id}: executable path not configured", engineId);
            return false;
        }

        var processConfig = new ProcessConfig(
            config.Id,
            config.ExecutablePath,
            config.Arguments ?? string.Empty,
            config.InstallPath ?? Path.GetDirectoryName(config.ExecutablePath) ?? string.Empty,
            config.Port,
            config.HealthCheckUrl,
            HealthCheckTimeoutSeconds: 60,
            config.AutoRestart,
            config.EnvironmentVariables
        );

        return await _processManager.StartAsync(processConfig, ct).ConfigureAwait(false);
    }

    public async Task<bool> StopEngineAsync(string engineId)
    {
        return await _processManager.StopAsync(engineId).ConfigureAwait(false);
    }

    public async Task<string> GetEngineLogsAsync(string engineId, int tailLines = 500)
    {
        return await _processManager.ReadLogsAsync(engineId, tailLines).ConfigureAwait(false);
    }

    public async Task StartAutoLaunchEnginesAsync(CancellationToken ct = default)
    {
        // Ensure initialization is complete before accessing engines
        await WaitForInitializationAsync(ct).ConfigureAwait(false);
        
        var autoLaunchEngines = _engines.Values.Where(e => e.StartOnAppLaunch).ToList();
        
        if (autoLaunchEngines.Count == 0)
        {
            _logger.LogInformation("No engines configured for auto-launch");
            return;
        }

        _logger.LogInformation("Auto-launching {Count} engines", autoLaunchEngines.Count);

        foreach (var engine in autoLaunchEngines)
        {
            try
            {
                await StartEngineAsync(engine.Id, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-launch engine {Id}", engine.Id);
            }
        }
    }

    public async Task StopAllEnginesAsync()
    {
        // Ensure initialization is complete before accessing engines
        await WaitForInitializationAsync().ConfigureAwait(false);
        
        var runningEngines = _engines.Keys.ToList();
        
        _logger.LogInformation("Stopping all running engines");

        foreach (var engineId in runningEngines)
        {
            try
            {
                await StopEngineAsync(engineId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop engine {Id}", engineId);
            }
        }
    }

    /// <summary>
    /// Get all instances of a specific engine type
    /// </summary>
    public IReadOnlyList<EngineConfig> GetEngineInstances(string engineId)
    {
        // Ensure initialization is complete before accessing engines
        // Use Task.Run to avoid deadlocks when called from async contexts
        if (!_isInitialized)
        {
            _logger.LogWarning("GetEngineInstances called before initialization complete, waiting...");
            var waitTask = Task.Run(async () => await WaitForInitializationAsync().ConfigureAwait(false));
            
            if (!waitTask.Wait(TimeSpan.FromSeconds(5)))
            {
                _logger.LogError("GetEngineInstances: Initialization wait timed out after 5 seconds");
                throw new InvalidOperationException(
                    "Engine registry initialization timed out. The registry may not be fully loaded.");
            }
            
            // Check if initialization failed
            if (_initializationFailed)
            {
                _logger.LogError("GetEngineInstances: Initialization failed, cannot access engines");
                throw new InvalidOperationException(
                    "Engine registry initialization failed. Cannot access engines.",
                    _initializationException);
            }
        }
        else if (_initializationFailed)
        {
            // Initialization completed but failed
            _logger.LogError("GetEngineInstances: Attempting to access engines after failed initialization");
            throw new InvalidOperationException(
                "Engine registry initialization failed. Cannot access engines.",
                _initializationException);
        }
        
        return _engines.Values.Where(e => e.EngineId == engineId).ToList();
    }

    /// <summary>
    /// Attach an existing external engine installation
    /// </summary>
    public async Task<(bool success, string? error)> AttachExternalEngineAsync(
        string instanceId,
        string engineId,
        string name,
        string installPath,
        string? executablePath,
        int? port,
        string? healthCheckUrl,
        string? notes = null)
    {
        // Validate paths exist
        if (!Directory.Exists(installPath))
        {
            return (false, $"Install path does not exist: {installPath}");
        }

        if (!string.IsNullOrEmpty(executablePath) && !File.Exists(executablePath))
        {
            return (false, $"Executable not found: {executablePath}");
        }

        var config = new EngineConfig(
            Id: instanceId,
            EngineId: engineId,
            Name: name,
            Version: "External",
            Mode: EngineMode.External,
            InstallPath: Path.GetFullPath(installPath),
            ExecutablePath: !string.IsNullOrEmpty(executablePath) ? Path.GetFullPath(executablePath) : null,
            Arguments: null,
            Port: port,
            HealthCheckUrl: healthCheckUrl,
            StartOnAppLaunch: false,
            AutoRestart: false,
            Notes: notes
        );

        await RegisterEngineAsync(config).ConfigureAwait(false);
        _logger.LogInformation("Attached external engine {EngineId} as instance {InstanceId}", engineId, instanceId);
        
        return (true, null);
    }

    /// <summary>
    /// Reconfigure an existing engine instance
    /// </summary>
    public async Task<(bool success, string? error)> ReconfigureEngineAsync(
        string instanceId,
        string? installPath = null,
        string? executablePath = null,
        int? port = null,
        string? healthCheckUrl = null,
        string? notes = null)
    {
        var config = GetEngine(instanceId);
        if (config == null)
        {
            return (false, "Engine instance not found");
        }

        // Validate new paths if provided
        if (installPath != null && !Directory.Exists(installPath))
        {
            return (false, $"Install path does not exist: {installPath}");
        }

        if (executablePath != null && !File.Exists(executablePath))
        {
            return (false, $"Executable not found: {executablePath}");
        }

        var updatedConfig = config with
        {
            InstallPath = installPath != null ? Path.GetFullPath(installPath) : config.InstallPath,
            ExecutablePath = executablePath != null ? Path.GetFullPath(executablePath) : config.ExecutablePath,
            Port = port ?? config.Port,
            HealthCheckUrl = healthCheckUrl ?? config.HealthCheckUrl,
            Notes = notes ?? config.Notes
        };

        await RegisterEngineAsync(updatedConfig).ConfigureAwait(false);
        _logger.LogInformation("Reconfigured engine instance {InstanceId}", instanceId);
        
        return (true, null);
    }

    private async Task LoadConfigAsync()
    {
        if (!File.Exists(_configPath))
        {
            _logger.LogInformation("No engine config found at {Path}, starting with empty registry", _configPath);
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configPath).ConfigureAwait(false);
            var configs = JsonSerializer.Deserialize<List<EngineConfig>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (configs != null)
            {
                foreach (var config in configs)
                {
                    _engines[config.Id] = config;
                }

                _logger.LogInformation("Loaded {Count} engine configurations", configs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load engine config from {Path}", _configPath);
        }
    }

    private async Task SaveConfigAsync()
    {
        try
        {
            var configDir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(configDir) && !Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            var configs = _engines.Values.ToList();
            var json = JsonSerializer.Serialize(configs, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_configPath, json).ConfigureAwait(false);
            _logger.LogDebug("Saved engine config to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save engine config to {Path}", _configPath);
        }
    }
}
