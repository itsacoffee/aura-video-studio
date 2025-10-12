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

    public LocalEnginesRegistry(
        ILogger<LocalEnginesRegistry> logger,
        ExternalProcessManager processManager,
        string configPath)
    {
        _logger = logger;
        _processManager = processManager;
        _configPath = configPath;

        LoadConfigAsync().Wait();
    }

    public async Task RegisterEngineAsync(EngineConfig config)
    {
        _engines[config.Id] = config;
        await SaveConfigAsync();
        _logger.LogInformation("Registered engine {Id} ({Name})", config.Id, config.Name);
    }

    public async Task UnregisterEngineAsync(string engineId)
    {
        if (_engines.TryRemove(engineId, out var config))
        {
            if (_processManager.GetStatus(engineId).IsRunning)
            {
                await _processManager.StopAsync(engineId);
            }

            await SaveConfigAsync();
            _logger.LogInformation("Unregistered engine {Id}", engineId);
        }
    }

    public EngineConfig? GetEngine(string engineId)
    {
        return _engines.TryGetValue(engineId, out var config) ? config : null;
    }

    public IReadOnlyList<EngineConfig> GetAllEngines()
    {
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
            isHealthy = await _processManager.CheckHealthAsync(engineId, config.HealthCheckUrl, CancellationToken.None);
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

        return await _processManager.StartAsync(processConfig, ct);
    }

    public async Task<bool> StopEngineAsync(string engineId)
    {
        return await _processManager.StopAsync(engineId);
    }

    public async Task<string> GetEngineLogsAsync(string engineId, int tailLines = 500)
    {
        return await _processManager.ReadLogsAsync(engineId, tailLines);
    }

    public async Task StartAutoLaunchEnginesAsync(CancellationToken ct = default)
    {
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
                await StartEngineAsync(engine.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-launch engine {Id}", engine.Id);
            }
        }
    }

    public async Task StopAllEnginesAsync()
    {
        var runningEngines = _engines.Keys.ToList();
        
        _logger.LogInformation("Stopping all running engines");

        foreach (var engineId in runningEngines)
        {
            try
            {
                await StopEngineAsync(engineId);
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

        await RegisterEngineAsync(config);
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

        await RegisterEngineAsync(updatedConfig);
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
            var json = await File.ReadAllTextAsync(_configPath);
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

            await File.WriteAllTextAsync(_configPath, json);
            _logger.LogDebug("Saved engine config to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save engine config to {Path}", _configPath);
        }
    }
}
