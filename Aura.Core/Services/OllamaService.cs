using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for managing Ollama process lifecycle (start/stop/status).
/// Windows-first implementation with graceful degradation for other OS.
/// </summary>
public class OllamaService
{
    private readonly ILogger<OllamaService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _logsDirectory;
    private Process? _ollamaProcess;
    private int? _managedProcessId;
    private readonly object _processLock = new();

    public OllamaService(ILogger<OllamaService> logger, HttpClient httpClient, string logsDirectory)
    {
        _logger = logger;
        _httpClient = httpClient;
        _logsDirectory = logsDirectory;
        
        if (!Directory.Exists(_logsDirectory))
        {
            Directory.CreateDirectory(_logsDirectory);
        }
    }

    /// <summary>
    /// Get Ollama status (running, PID, model info)
    /// </summary>
    public async Task<OllamaStatus> GetStatusAsync(string baseUrl, CancellationToken ct)
    {
        _logger.LogInformation("Checking Ollama status at {BaseUrl}", baseUrl);

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{baseUrl}/api/tags", cts.Token);
            
            if (response.IsSuccessStatusCode)
            {
                var pid = GetOllamaProcessId();
                var isManagedByApp = _managedProcessId.HasValue && _managedProcessId == pid;
                
                _logger.LogInformation("Ollama is running (PID: {Pid}, ManagedByApp: {Managed})", pid, isManagedByApp);
                
                return new OllamaStatus(
                    Running: true,
                    Pid: pid,
                    ManagedByApp: isManagedByApp,
                    Model: null,
                    Error: null
                );
            }
            else
            {
                _logger.LogWarning("Ollama responded with status {StatusCode}", response.StatusCode);
                return new OllamaStatus(false, null, false, null, $"HTTP {response.StatusCode}");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Ollama status check timed out - not running");
            return new OllamaStatus(false, null, false, null, "Connection timeout");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogInformation("Ollama is not reachable: {Message}", ex.Message);
            return new OllamaStatus(false, null, false, null, "Not reachable");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Ollama status");
            return new OllamaStatus(false, null, false, null, ex.Message);
        }
    }

    /// <summary>
    /// Start Ollama server process (Windows only)
    /// </summary>
    public async Task<OllamaStartResult> StartAsync(string executablePath, string baseUrl, CancellationToken ct)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("Ollama start attempted on non-Windows platform");
            return new OllamaStartResult(false, "Start operation only supported on Windows. Please start Ollama manually.");
        }

        if (!File.Exists(executablePath))
        {
            _logger.LogWarning("Ollama executable not found at {Path}", executablePath);
            return new OllamaStartResult(false, $"Ollama executable not found at: {executablePath}");
        }

        lock (_processLock)
        {
            if (_ollamaProcess != null && !_ollamaProcess.HasExited)
            {
                _logger.LogInformation("Ollama process already running (PID: {Pid})", _ollamaProcess.Id);
                return new OllamaStartResult(false, "Ollama is already running");
            }
        }

        var logFilePath = Path.Combine(_logsDirectory, $"ollama-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");
        
        try
        {
            _logger.LogInformation("Starting Ollama from {Path}", executablePath);

            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "serve",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath) ?? string.Empty
            };

            var process = new Process { StartInfo = startInfo };
            
            var logWriter = new StreamWriter(logFilePath, false, Encoding.UTF8) { AutoFlush = true };
            
            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    logWriter.WriteLine($"[OUT] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {args.Data}");
                }
            };

            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    logWriter.WriteLine($"[ERR] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} {args.Data}");
                }
            };

            process.Exited += (sender, args) =>
            {
                logWriter.WriteLine($"[SYS] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} Process exited with code {process.ExitCode}");
                logWriter.Dispose();
            };

            process.EnableRaisingEvents = true;

            if (!process.Start())
            {
                _logger.LogError("Failed to start Ollama process");
                logWriter.Dispose();
                return new OllamaStartResult(false, "Failed to start Ollama process");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            lock (_processLock)
            {
                _ollamaProcess = process;
                _managedProcessId = process.Id;
            }

            _logger.LogInformation("Ollama process started (PID: {Pid}), waiting for readiness...", process.Id);
            logWriter.WriteLine($"[SYS] {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} Started Ollama process (PID: {process.Id})");

            var ready = await WaitForReadinessAsync(baseUrl, TimeSpan.FromSeconds(10), ct);

            if (ready)
            {
                _logger.LogInformation("Ollama is ready and responding");
                return new OllamaStartResult(true, "Ollama started successfully", process.Id);
            }
            else
            {
                _logger.LogWarning("Ollama started but did not become ready within timeout");
                return new OllamaStartResult(true, "Ollama started but not yet ready (may still be loading)", process.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Ollama");
            return new OllamaStartResult(false, $"Error starting Ollama: {ex.Message}");
        }
    }

    /// <summary>
    /// Stop Ollama server process (only if managed by this app)
    /// </summary>
    public Task<OllamaStopResult> StopAsync()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("Ollama stop attempted on non-Windows platform");
            return Task.FromResult(new OllamaStopResult(false, "Stop operation only supported on Windows"));
        }

        lock (_processLock)
        {
            if (_ollamaProcess == null || _ollamaProcess.HasExited)
            {
                _logger.LogInformation("No managed Ollama process to stop");
                return Task.FromResult(new OllamaStopResult(false, "No managed Ollama process running"));
            }

            try
            {
                _logger.LogInformation("Stopping Ollama process (PID: {Pid})", _ollamaProcess.Id);
                
                _ollamaProcess.Kill(entireProcessTree: true);
                
                var exited = _ollamaProcess.WaitForExit(5000);
                
                if (exited)
                {
                    _logger.LogInformation("Ollama process stopped successfully");
                    _managedProcessId = null;
                    _ollamaProcess = null;
                    return Task.FromResult(new OllamaStopResult(true, "Ollama stopped successfully"));
                }
                else
                {
                    _logger.LogWarning("Ollama process did not exit within timeout");
                    return Task.FromResult(new OllamaStopResult(false, "Process did not stop within timeout"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Ollama");
                return Task.FromResult(new OllamaStopResult(false, $"Error stopping Ollama: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Get recent log entries from Ollama logs
    /// </summary>
    public Task<string[]> GetLogsAsync(int maxLines = 200)
    {
        try
        {
            var logFiles = Directory.GetFiles(_logsDirectory, "ollama-*.log")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .ToArray();

            if (logFiles.Length == 0)
            {
                return Task.FromResult(Array.Empty<string>());
            }

            var latestLogFile = logFiles[0];
            var allLines = File.ReadAllLines(latestLogFile);
            var recentLines = allLines.TakeLast(maxLines).ToArray();

            _logger.LogInformation("Retrieved {Count} log lines from {File}", recentLines.Length, Path.GetFileName(latestLogFile));
            
            return Task.FromResult(recentLines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Ollama logs");
            return Task.FromResult(new[] { $"Error reading logs: {ex.Message}" });
        }
    }

    /// <summary>
    /// Find Ollama executable path using default locations
    /// </summary>
    public static string? FindOllamaExecutable()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        var searchPaths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ollama", "ollama.exe"),
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private async Task<bool> WaitForReadinessAsync(string baseUrl, TimeSpan timeout, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var retryDelay = TimeSpan.FromMilliseconds(500);

        while (stopwatch.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(2));

                var response = await _httpClient.GetAsync($"{baseUrl}/api/tags", cts.Token);
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch
            {
                // Ignore errors during readiness check
            }

            await Task.Delay(retryDelay, ct);
        }

        return false;
    }

    private int? GetOllamaProcessId()
    {
        try
        {
            var processes = Process.GetProcessesByName("ollama");
            if (processes.Length > 0)
            {
                return processes[0].Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting Ollama process ID");
        }

        return null;
    }
}

public record OllamaStatus(
    bool Running,
    int? Pid,
    bool ManagedByApp,
    string? Model,
    string? Error);

public record OllamaStartResult(
    bool Success,
    string Message,
    int? Pid = null);

public record OllamaStopResult(
    bool Success,
    string Message);
