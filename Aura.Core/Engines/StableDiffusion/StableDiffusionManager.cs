using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Engines.StableDiffusion;

/// <summary>
/// Status of the Stable Diffusion WebUI server
/// </summary>
public record StableDiffusionServerStatus(
    bool IsInstalled,
    bool IsRunning,
    bool IsHealthy,
    int? Port,
    int? ProcessId,
    string? CurrentModel,
    string[] AvailableModels,
    string? ErrorMessage = null
);

/// <summary>
/// Manages the lifecycle of the Stable Diffusion WebUI server
/// </summary>
public class StableDiffusionManager
{
    private readonly ILogger<StableDiffusionManager> _logger;
    private readonly StableDiffusionInstaller _installer;
    private readonly ExternalProcessManager _processManager;
    private readonly HttpClient _httpClient;
    
    private const string PROCESS_ID = "stable-diffusion-webui";
    private const int DEFAULT_PORT = 7860;
    private const int HEALTH_CHECK_TIMEOUT_SECONDS = 180; // SD can take a while to start

    public StableDiffusionManager(
        ILogger<StableDiffusionManager> logger,
        StableDiffusionInstaller installer,
        ExternalProcessManager processManager,
        HttpClient httpClient)
    {
        _logger = logger;
        _installer = installer;
        _processManager = processManager;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Get the current status of the Stable Diffusion server
    /// </summary>
    public async Task<StableDiffusionServerStatus> GetStatusAsync(CancellationToken ct = default)
    {
        bool isInstalled = _installer.IsInstalled();
        
        if (!isInstalled)
        {
            return new StableDiffusionServerStatus(
                IsInstalled: false,
                IsRunning: false,
                IsHealthy: false,
                Port: null,
                ProcessId: null,
                CurrentModel: null,
                AvailableModels: Array.Empty<string>()
            );
        }
        
        var processStatus = _processManager.GetStatus(PROCESS_ID);
        bool isRunning = processStatus.IsRunning;
        bool isHealthy = false;
        string? currentModel = null;
        string[] availableModels = Array.Empty<string>();
        
        if (isRunning)
        {
            isHealthy = await CheckHealthAsync(DEFAULT_PORT, ct).ConfigureAwait(false);
            
            if (isHealthy)
            {
                try
                {
                    var models = await GetModelsFromApiAsync(DEFAULT_PORT, ct).ConfigureAwait(false);
                    availableModels = models.available;
                    currentModel = models.current;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch models from SD API");
                }
            }
        }
        else
        {
            // Even if not running, we can list installed models
            availableModels = _installer.GetInstalledModels();
        }
        
        return new StableDiffusionServerStatus(
            IsInstalled: isInstalled,
            IsRunning: isRunning,
            IsHealthy: isHealthy,
            Port: isRunning ? DEFAULT_PORT : null,
            ProcessId: processStatus.ProcessId,
            CurrentModel: currentModel,
            AvailableModels: availableModels,
            ErrorMessage: processStatus.LastError
        );
    }

    /// <summary>
    /// Start the Stable Diffusion WebUI server
    /// </summary>
    public async Task<bool> StartServerAsync(int port = DEFAULT_PORT, CancellationToken ct = default)
    {
        if (!_installer.IsInstalled())
        {
            _logger.LogError("Cannot start SD WebUI - not installed");
            return false;
        }
        
        var status = _processManager.GetStatus(PROCESS_ID);
        if (status.IsRunning)
        {
            _logger.LogInformation("SD WebUI is already running");
            return true;
        }
        
        _logger.LogInformation("Starting Stable Diffusion WebUI on port {Port}", port);
        
        var installPath = _installer.GetInstallPath();
        
        // Determine the launcher script
        string launcherScript;
        string arguments = $"--api --listen --port {port}";
        
        if (OperatingSystem.IsWindows())
        {
            launcherScript = System.IO.Path.Combine(installPath, "webui.bat");
        }
        else
        {
            launcherScript = System.IO.Path.Combine(installPath, "webui.sh");
        }
        
        if (!System.IO.File.Exists(launcherScript))
        {
            _logger.LogError("Launcher script not found: {Script}", launcherScript);
            return false;
        }
        
        var config = new ProcessConfig(
            Id: PROCESS_ID,
            ExecutablePath: launcherScript,
            Arguments: arguments,
            WorkingDirectory: installPath,
            Port: port,
            HealthCheckUrl: $"http://127.0.0.1:{port}/sdapi/v1/sd-models",
            HealthCheckTimeoutSeconds: HEALTH_CHECK_TIMEOUT_SECONDS,
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
    /// Stop the Stable Diffusion WebUI server
    /// </summary>
    public async Task<bool> StopServerAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Stopping Stable Diffusion WebUI");
        
        var status = _processManager.GetStatus(PROCESS_ID);
        if (!status.IsRunning)
        {
            _logger.LogInformation("SD WebUI is not running");
            return true;
        }
        
        // SD can take a while to shut down gracefully
        return await _processManager.StopAsync(PROCESS_ID, gracefulTimeoutSeconds: 30).ConfigureAwait(false);
    }

    /// <summary>
    /// Check if the server is healthy
    /// </summary>
    public async Task<bool> CheckHealthAsync(int port = DEFAULT_PORT, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            
            var response = await _httpClient.GetAsync(
                $"http://127.0.0.1:{port}/sdapi/v1/sd-models", 
                cts.Token).ConfigureAwait(false);
                
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed for SD WebUI");
            return false;
        }
    }

    /// <summary>
    /// Get server logs
    /// </summary>
    public async Task<string> GetLogsAsync(int tailLines = 500)
    {
        return await _processManager.ReadLogsAsync(PROCESS_ID, tailLines).ConfigureAwait(false);
    }

    /// <summary>
    /// Wait for the server to become healthy
    /// </summary>
    public async Task<bool> WaitForHealthyAsync(int timeoutSeconds = 120, int port = DEFAULT_PORT, CancellationToken ct = default)
    {
        _logger.LogInformation("Waiting for SD WebUI to become healthy (timeout: {Timeout}s)", timeoutSeconds);
        
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        
        while (DateTime.UtcNow < deadline && !ct.IsCancellationRequested)
        {
            if (await CheckHealthAsync(port, ct).ConfigureAwait(false))
            {
                _logger.LogInformation("SD WebUI is healthy");
                return true;
            }
            
            await Task.Delay(2000, ct).ConfigureAwait(false);
        }
        
        _logger.LogWarning("SD WebUI did not become healthy within timeout");
        return false;
    }

    /// <summary>
    /// Change the active model
    /// </summary>
    public async Task<bool> SetModelAsync(string modelName, int port = DEFAULT_PORT, CancellationToken ct = default)
    {
        _logger.LogInformation("Setting SD model to: {Model}", modelName);
        
        try
        {
            var request = new
            {
                sd_model_checkpoint = modelName
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");
            
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(60)); // Model loading can take a while
            
            var response = await _httpClient.PostAsync(
                $"http://127.0.0.1:{port}/sdapi/v1/options",
                content,
                cts.Token).ConfigureAwait(false);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Model changed successfully to: {Model}", modelName);
                return true;
            }
            
            var error = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            _logger.LogWarning("Failed to change model: {Error}", error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing model to {Model}", modelName);
            return false;
        }
    }

    private async Task<(string? current, string[] available)> GetModelsFromApiAsync(int port, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            
            // Get available models
            var modelsResponse = await _httpClient.GetAsync(
                $"http://127.0.0.1:{port}/sdapi/v1/sd-models", 
                cts.Token).ConfigureAwait(false);
            
            var modelsJson = await modelsResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var modelsDoc = JsonDocument.Parse(modelsJson);
            
            var models = new List<string>();
            foreach (var element in modelsDoc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("model_name", out var nameElement))
                {
                    models.Add(nameElement.GetString() ?? "");
                }
                else if (element.TryGetProperty("title", out var titleElement))
                {
                    models.Add(titleElement.GetString() ?? "");
                }
            }
            
            // Get current options to find active model
            string? currentModel = null;
            var optionsResponse = await _httpClient.GetAsync(
                $"http://127.0.0.1:{port}/sdapi/v1/options", 
                cts.Token).ConfigureAwait(false);
                
            if (optionsResponse.IsSuccessStatusCode)
            {
                var optionsJson = await optionsResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                using var optionsDoc = JsonDocument.Parse(optionsJson);
                
                if (optionsDoc.RootElement.TryGetProperty("sd_model_checkpoint", out var modelElement))
                {
                    currentModel = modelElement.GetString();
                }
            }
            
            return (currentModel, models.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching models from SD API");
            return (null, Array.Empty<string>());
        }
    }
}
