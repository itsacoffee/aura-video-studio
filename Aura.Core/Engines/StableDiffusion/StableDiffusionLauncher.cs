using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Engines.StableDiffusion;

/// <summary>
/// Launcher for Stable Diffusion WebUI with managed and attached modes
/// </summary>
public class StableDiffusionLauncher
{
    private readonly ILogger<StableDiffusionLauncher> _logger;
    private readonly ExternalProcessManager _processManager;
    private readonly string _installPath;
    private readonly int _port;

    public StableDiffusionLauncher(
        ILogger<StableDiffusionLauncher> logger,
        ExternalProcessManager processManager,
        string installPath,
        int port = 7860)
    {
        _logger = logger;
        _processManager = processManager;
        _installPath = installPath;
        _port = port;
    }

    /// <summary>
    /// Start Stable Diffusion WebUI in managed mode
    /// </summary>
    public async Task<bool> StartManagedAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Stable Diffusion WebUI in managed mode at {Path}", _installPath);

        if (!Directory.Exists(_installPath))
        {
            _logger.LogError("Stable Diffusion installation not found at {Path}", _installPath);
            return false;
        }

        // Determine the launcher script based on OS
        string launcherScript;
        string arguments = $"--port {_port} --api --nowebui";

        if (OperatingSystem.IsWindows())
        {
            launcherScript = Path.Combine(_installPath, "webui.bat");
            if (!File.Exists(launcherScript))
            {
                launcherScript = Path.Combine(_installPath, "run.bat");
            }
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            launcherScript = Path.Combine(_installPath, "webui.sh");
            if (!File.Exists(launcherScript))
            {
                launcherScript = Path.Combine(_installPath, "run.sh");
            }
        }
        else
        {
            _logger.LogError("Unsupported operating system for Stable Diffusion WebUI");
            return false;
        }

        if (!File.Exists(launcherScript))
        {
            _logger.LogError("Launcher script not found: {Script}", launcherScript);
            return false;
        }

        var config = new ProcessConfig(
            Id: "stable-diffusion-webui",
            ExecutablePath: launcherScript,
            Arguments: arguments,
            WorkingDirectory: _installPath,
            Port: _port,
            HealthCheckUrl: $"http://127.0.0.1:{_port}/sdapi/v1/sd-models",
            HealthCheckTimeoutSeconds: 120,
            AutoRestart: false,
            EnvironmentVariables: new Dictionary<string, string>
            {
                ["PYTHONUNBUFFERED"] = "1",
                ["COMMANDLINE_ARGS"] = arguments
            }
        );

        return await _processManager.StartAsync(config, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Stop managed Stable Diffusion WebUI
    /// </summary>
    public async Task<bool> StopAsync()
    {
        _logger.LogInformation("Stopping Stable Diffusion WebUI");
        return await _processManager.StopAsync("stable-diffusion-webui", gracefulTimeoutSeconds: 30).ConfigureAwait(false);
    }

    /// <summary>
    /// Get status of Stable Diffusion WebUI process
    /// </summary>
    public ProcessStatus GetStatus()
    {
        return _processManager.GetStatus("stable-diffusion-webui");
    }

    /// <summary>
    /// Check if Stable Diffusion WebUI is healthy (attached or managed mode)
    /// </summary>
    public async Task<bool> CheckHealthAsync(CancellationToken ct = default)
    {
        var healthUrl = $"http://127.0.0.1:{_port}/sdapi/v1/sd-models";
        return await _processManager.CheckHealthAsync("stable-diffusion-webui", healthUrl, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get logs from Stable Diffusion WebUI
    /// </summary>
    public async Task<string> GetLogsAsync(int tailLines = 500)
    {
        return await _processManager.ReadLogsAsync("stable-diffusion-webui", tailLines).ConfigureAwait(false);
    }
}
