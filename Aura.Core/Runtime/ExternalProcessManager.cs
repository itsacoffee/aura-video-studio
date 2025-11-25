using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Runtime;

public record ProcessConfig(
    string Id,
    string ExecutablePath,
    string Arguments,
    string WorkingDirectory,
    int? Port,
    string? HealthCheckUrl,
    int HealthCheckTimeoutSeconds,
    bool AutoRestart,
    IDictionary<string, string>? EnvironmentVariables = null
);

public record ProcessStatus(
    string Id,
    bool IsRunning,
    int? ProcessId,
    DateTime? StartTime,
    string? LastError,
    bool HealthCheckPassed
);

/// <summary>
/// Cross-platform external process manager with health checks, log capture, and auto-restart
/// </summary>
public class ExternalProcessManager : IDisposable
{
    private readonly ILogger<ExternalProcessManager> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, ManagedProcess> _processes = new();
    private readonly string _logDirectory;

    public ExternalProcessManager(ILogger<ExternalProcessManager> logger, HttpClient httpClient, string logDirectory)
    {
        _logger = logger;
        _httpClient = httpClient;
        _logDirectory = logDirectory;

        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }
    }

    public async Task<bool> StartAsync(ProcessConfig config, CancellationToken ct = default)
    {
        if (_processes.ContainsKey(config.Id))
        {
            _logger.LogWarning("Process {Id} is already running", config.Id);
            return false;
        }

        try
        {
            _logger.LogInformation("Starting process {Id}: {Exe} {Args}", config.Id, config.ExecutablePath, config.Arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = config.ExecutablePath,
                Arguments = config.Arguments,
                WorkingDirectory = config.WorkingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (config.EnvironmentVariables != null)
            {
                foreach (var kvp in config.EnvironmentVariables)
                {
                    startInfo.Environment[kvp.Key] = kvp.Value;
                }
            }

            var process = new Process { StartInfo = startInfo };
            var logPath = Path.Combine(_logDirectory, $"{config.Id}.log");
            var logWriter = new StreamWriter(logPath, append: true, Encoding.UTF8) { AutoFlush = true };

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    logWriter.WriteLine($"[OUT] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {e.Data}");
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    logWriter.WriteLine($"[ERR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {e.Data}");
                }
            };

            if (!process.Start())
            {
                _logger.LogError("Failed to start process {Id}", config.Id);
                logWriter.Dispose();
                return false;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var managedProcess = new ManagedProcess
            {
                Process = process,
                Config = config,
                LogWriter = logWriter,
                StartTime = DateTime.Now
            };

            _processes[config.Id] = managedProcess;

            _logger.LogInformation("Process {Id} started with PID {Pid}", config.Id, process.Id);

            if (!string.IsNullOrEmpty(config.HealthCheckUrl))
            {
                _logger.LogInformation("Waiting for health check at {Url}", config.HealthCheckUrl);
                bool healthy = await WaitForHealthAsync(config.Id, config.HealthCheckUrl, config.HealthCheckTimeoutSeconds, ct).ConfigureAwait(false);
                
                if (!healthy)
                {
                    _logger.LogWarning("Health check failed for {Id}, but process is running", config.Id);
                    managedProcess.HealthCheckPassed = false;
                }
                else
                {
                    _logger.LogInformation("Health check passed for {Id}", config.Id);
                    managedProcess.HealthCheckPassed = true;
                }
            }

            if (config.AutoRestart)
            {
                MonitorForRestart(config.Id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting process {Id}", config.Id);
            return false;
        }
    }

    public async Task<bool> StopAsync(string processId, int gracefulTimeoutSeconds = 10)
    {
        if (!_processes.TryRemove(processId, out var managedProcess))
        {
            _logger.LogWarning("Process {Id} not found", processId);
            return false;
        }

        try
        {
            _logger.LogInformation("Stopping process {Id} (PID: {Pid})", processId, managedProcess.Process.Id);

            managedProcess.CancellationTokenSource?.Cancel();

            if (!managedProcess.Process.HasExited)
            {
                managedProcess.Process.CloseMainWindow();
                
                bool exited = await Task.Run(() => managedProcess.Process.WaitForExit(gracefulTimeoutSeconds * 1000)).ConfigureAwait(false);
                
                if (!exited && !managedProcess.Process.HasExited)
                {
                    _logger.LogWarning("Process {Id} did not exit gracefully, killing", processId);
                    managedProcess.Process.Kill(entireProcessTree: true);
                    managedProcess.Process.WaitForExit();
                }
            }

            managedProcess.LogWriter.Dispose();
            managedProcess.Process.Dispose();

            _logger.LogInformation("Process {Id} stopped successfully", processId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping process {Id}", processId);
            return false;
        }
    }

    public ProcessStatus GetStatus(string processId)
    {
        if (!_processes.TryGetValue(processId, out var managedProcess))
        {
            return new ProcessStatus(processId, false, null, null, null, false);
        }

        return new ProcessStatus(
            processId,
            !managedProcess.Process.HasExited,
            managedProcess.Process.HasExited ? null : managedProcess.Process.Id,
            managedProcess.StartTime,
            managedProcess.LastError,
            managedProcess.HealthCheckPassed
        );
    }

    public async Task<bool> CheckHealthAsync(string processId, string healthCheckUrl, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await _httpClient.GetAsync(healthCheckUrl, cts.Token).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed for {Id}: {Message}", processId, ex.Message);
            return false;
        }
    }

    public string GetLogPath(string processId)
    {
        return Path.Combine(_logDirectory, $"{processId}.log");
    }

    public async Task<string> ReadLogsAsync(string processId, int tailLines = 500)
    {
        var logPath = GetLogPath(processId);
        
        if (!File.Exists(logPath))
        {
            return string.Empty;
        }

        var lines = await File.ReadAllLinesAsync(logPath).ConfigureAwait(false);
        var startIndex = Math.Max(0, lines.Length - tailLines);
        return string.Join(Environment.NewLine, lines[startIndex..]);
    }

    private async Task<bool> WaitForHealthAsync(string processId, string healthCheckUrl, int timeoutSeconds, CancellationToken ct)
    {
        var timeout = DateTime.Now.AddSeconds(timeoutSeconds);
        
        while (DateTime.Now < timeout && !ct.IsCancellationRequested)
        {
            if (await CheckHealthAsync(processId, healthCheckUrl, ct).ConfigureAwait(false))
            {
                return true;
            }

            await Task.Delay(2000, ct).ConfigureAwait(false);
        }

        return false;
    }

    private void MonitorForRestart(string processId)
    {
        if (!_processes.TryGetValue(processId, out var managedProcess))
        {
            return;
        }

        var cts = new CancellationTokenSource();
        managedProcess.CancellationTokenSource = cts;

        Task.Run(async () =>
        {
            try
            {
                await managedProcess.Process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                
                if (!cts.Token.IsCancellationRequested)
                {
                    _logger.LogWarning("Process {Id} exited unexpectedly, restarting", processId);
                    
                    _processes.TryRemove(processId, out _);
                    managedProcess.LogWriter.Dispose();
                    managedProcess.Process.Dispose();
                    
                    await Task.Delay(5000, cts.Token).ConfigureAwait(false);
                    
                    if (!cts.Token.IsCancellationRequested)
                    {
                        await StartAsync(managedProcess.Config, cts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring process {Id} for restart", processId);
                managedProcess.LastError = ex.Message;
            }
        }, cts.Token);
    }

    public void Dispose()
    {
        // Use Task.Run with timeout to avoid deadlocks during disposal
        // StopAsync doesn't accept CancellationToken, so we use a timeout wrapper
        // Use a single timeout (10 seconds) for all disposal operations
        var disposeTasks = _processes.Keys.Select(processId =>
            Task.Run(async () =>
            {
                try
                {
                    // StopAsync has its own gracefulTimeoutSeconds parameter (default 10s)
                    // We'll let it use its default timeout
                    await StopAsync(processId).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // Log but don't throw - we want to dispose all processes
                    // Logging is handled by StopAsync internally
                }
            })
        ).ToArray();
        
        try
        {
            // Wait for all disposal tasks with a single timeout
            // This matches the StopAsync default timeout
            Task.WaitAll(disposeTasks, TimeSpan.FromSeconds(10));
        }
        catch (Exception ex)
        {
            // Log but continue disposal
            // Individual process disposal errors are already logged
        }
        
        GC.SuppressFinalize(this);
    }

    private class ManagedProcess
    {
        public required Process Process { get; init; }
        public required ProcessConfig Config { get; init; }
        public required StreamWriter LogWriter { get; init; }
        public DateTime StartTime { get; init; }
        public string? LastError { get; set; }
        public bool HealthCheckPassed { get; set; }
        public CancellationTokenSource? CancellationTokenSource { get; set; }
    }
}
