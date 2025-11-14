using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Dependencies;

/// <summary>
/// Status of a dependency after rescan
/// </summary>
public enum DependencyStatus
{
    Installed,
    Missing,
    PartiallyInstalled
}

/// <summary>
/// Report entry for a single dependency
/// </summary>
public class DependencyReport
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public DependencyStatus Status { get; set; }
    public string? Path { get; set; }
    public string? ValidationOutput { get; set; }
    public string? Provenance { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Full rescan report for all dependencies
/// </summary>
public class DependencyRescanReport
{
    public DateTime ScanTime { get; set; }
    public List<DependencyReport> Dependencies { get; set; } = new();
}

/// <summary>
/// Service for rescanning all known dependencies
/// </summary>
public class DependencyRescanService
{
    private readonly ILogger<DependencyRescanService> _logger;
    private readonly FfmpegLocator _ffmpegLocator;
    private readonly ComponentDownloader _componentDownloader;
    private readonly string _appDataPath;
    private readonly string _lastScanPath;

    public DependencyRescanService(
        ILogger<DependencyRescanService> logger,
        FfmpegLocator ffmpegLocator,
        ComponentDownloader componentDownloader)
    {
        _logger = logger;
        _ffmpegLocator = ffmpegLocator;
        _componentDownloader = componentDownloader;
        
        _appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura");
        
        _lastScanPath = Path.Combine(_appDataPath, "last-dependency-scan.json");
    }

    /// <summary>
    /// Perform full rescan of all dependencies
    /// </summary>
    public async Task<DependencyRescanReport> RescanAllAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting full dependency rescan");
        
        var report = new DependencyRescanReport
        {
            ScanTime = DateTime.UtcNow
        };

        // Load components.json to get list of dependencies
        var manifest = await _componentDownloader.LoadManifestAsync().ConfigureAwait(false);
        
        // Rescan each component
        foreach (var component in manifest.Components)
        {
            var dependencyReport = await RescanDependencyAsync(component, ct).ConfigureAwait(false);
            report.Dependencies.Add(dependencyReport);
        }

        // Also check for FFprobe (related to FFmpeg)
        var ffprobeReport = await RescanFfprobeAsync(ct).ConfigureAwait(false);
        report.Dependencies.Add(ffprobeReport);

        // Save last scan time
        await SaveLastScanTimeAsync(report.ScanTime).ConfigureAwait(false);

        _logger.LogInformation("Dependency rescan completed. {InstalledCount} installed, {MissingCount} missing, {PartialCount} partially installed",
            report.Dependencies.Count(d => d.Status == DependencyStatus.Installed),
            report.Dependencies.Count(d => d.Status == DependencyStatus.Missing),
            report.Dependencies.Count(d => d.Status == DependencyStatus.PartiallyInstalled));

        return report;
    }

    /// <summary>
    /// Get the last scan time
    /// </summary>
    public async Task<DateTime?> GetLastScanTimeAsync()
    {
        try
        {
            if (File.Exists(_lastScanPath))
            {
                var json = await File.ReadAllTextAsync(_lastScanPath).ConfigureAwait(false);
                var data = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(json);
                if (data != null && data.TryGetValue("lastScanTime", out var scanTime))
                {
                    return scanTime;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read last scan time");
        }
        
        return null;
    }

    private async Task SaveLastScanTimeAsync(DateTime scanTime)
    {
        try
        {
            var data = new Dictionary<string, DateTime>
            {
                { "lastScanTime", scanTime }
            };
            
            var json = JsonSerializer.Serialize(data);
            await File.WriteAllTextAsync(_lastScanPath, json).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save last scan time");
        }
    }

    private async Task<DependencyReport> RescanDependencyAsync(ComponentManifestEntry component, CancellationToken ct)
    {
        _logger.LogDebug("Rescanning {Component}", component.Name);
        
        switch (component.Id.ToLowerInvariant())
        {
            case "ffmpeg":
                return await RescanFfmpegAsync(ct).ConfigureAwait(false);
            
            case "ollama":
                return await RescanOllamaAsync(ct).ConfigureAwait(false);
            
            case "stable-diffusion-webui":
            case "sd-webui":
                return await RescanStableDiffusionAsync(ct).ConfigureAwait(false);
            
            case "piper":
                return await RescanPiperAsync(ct).ConfigureAwait(false);
            
            case "python":
                return await RescanPythonAsync(ct).ConfigureAwait(false);
            
            default:
                return new DependencyReport
                {
                    Id = component.Id,
                    DisplayName = component.Name,
                    Status = DependencyStatus.Missing,
                    ErrorMessage = "Rescan not implemented for this component"
                };
        }
    }

    private async Task<DependencyReport> RescanFfmpegAsync(CancellationToken ct)
    {
        try
        {
            var result = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct).ConfigureAwait(false);
            
            return new DependencyReport
            {
                Id = "ffmpeg",
                DisplayName = "FFmpeg",
                Status = result.Found ? DependencyStatus.Installed : DependencyStatus.Missing,
                Path = result.FfmpegPath,
                ValidationOutput = result.VersionString,
                Provenance = result.Found ? "Detected" : null,
                ErrorMessage = result.Found ? null : result.Reason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rescan FFmpeg");
            return new DependencyReport
            {
                Id = "ffmpeg",
                DisplayName = "FFmpeg",
                Status = DependencyStatus.Missing,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<DependencyReport> RescanFfprobeAsync(CancellationToken ct)
    {
        try
        {
            // FFprobe is typically bundled with FFmpeg
            var ffmpegResult = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct).ConfigureAwait(false);
            
            if (!ffmpegResult.Found || string.IsNullOrEmpty(ffmpegResult.FfmpegPath))
            {
                return new DependencyReport
                {
                    Id = "ffprobe",
                    DisplayName = "FFprobe",
                    Status = DependencyStatus.Missing,
                    ErrorMessage = "FFmpeg not found (FFprobe is bundled with FFmpeg)"
                };
            }

            // Check for ffprobe in same directory as ffmpeg
            var ffmpegDir = Path.GetDirectoryName(ffmpegResult.FfmpegPath);
            if (string.IsNullOrEmpty(ffmpegDir))
            {
                return new DependencyReport
                {
                    Id = "ffprobe",
                    DisplayName = "FFprobe",
                    Status = DependencyStatus.Missing,
                    ErrorMessage = "Could not determine FFmpeg directory"
                };
            }

            var ffprobeExe = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows) 
                ? "ffprobe.exe" 
                : "ffprobe";
            var ffprobePath = Path.Combine(ffmpegDir, ffprobeExe);

            if (File.Exists(ffprobePath))
            {
                return new DependencyReport
                {
                    Id = "ffprobe",
                    DisplayName = "FFprobe",
                    Status = DependencyStatus.Installed,
                    Path = ffprobePath,
                    Provenance = "Bundled with FFmpeg"
                };
            }
            else
            {
                return new DependencyReport
                {
                    Id = "ffprobe",
                    DisplayName = "FFprobe",
                    Status = DependencyStatus.PartiallyInstalled,
                    ErrorMessage = $"FFprobe not found at {ffprobePath}"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rescan FFprobe");
            return new DependencyReport
            {
                Id = "ffprobe",
                DisplayName = "FFprobe",
                Status = DependencyStatus.Missing,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<DependencyReport> RescanOllamaAsync(CancellationToken ct)
    {
        try
        {
            // First, check if Ollama process is running
            var processes = System.Diagnostics.Process.GetProcessesByName("ollama");
            var isProcessRunning = processes.Length > 0;
            int? processId = null;
            
            if (isProcessRunning)
            {
                try
                {
                    // Get the process ID while the process is still valid
                    processId = processes[0].Id;
                    _logger.LogInformation("Ollama process detected (PID: {Pid})", processId);
                }
                catch (InvalidOperationException)
                {
                    // Process may have exited between check and ID access
                    _logger.LogDebug("Ollama process found but exited before we could get its ID");
                    isProcessRunning = false;
                }
            }
            else
            {
                _logger.LogDebug("No Ollama process found running");
            }
            
            // Try to connect to Ollama API
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            try
            {
                var response = await httpClient.GetAsync("http://127.0.0.1:11434/api/tags", ct).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    return new DependencyReport
                    {
                        Id = "ollama",
                        DisplayName = "Ollama",
                        Status = DependencyStatus.Installed,
                        Path = "http://127.0.0.1:11434",
                        Provenance = "Running service",
                        ValidationOutput = $"API responding{(processId.HasValue ? $" (PID: {processId.Value})" : "")}"
                    };
                }
                else
                {
                    return new DependencyReport
                    {
                        Id = "ollama",
                        DisplayName = "Ollama",
                        Status = DependencyStatus.PartiallyInstalled,
                        ErrorMessage = $"Ollama API returned status {response.StatusCode}"
                    };
                }
            }
            catch (System.Net.Http.HttpRequestException)
            {
                // API not responding, check if process is running or if executable exists
                if (isProcessRunning && processId.HasValue)
                {
                    return new DependencyReport
                    {
                        Id = "ollama",
                        DisplayName = "Ollama",
                        Status = DependencyStatus.PartiallyInstalled,
                        Path = $"Process running (PID: {processId.Value})",
                        ErrorMessage = "Ollama process detected but API not responding. It may still be starting up."
                    };
                }
                
                // Check for Ollama executable in default locations
                var executablePath = FindOllamaExecutable();
                if (!string.IsNullOrEmpty(executablePath))
                {
                    return new DependencyReport
                    {
                        Id = "ollama",
                        DisplayName = "Ollama",
                        Status = DependencyStatus.PartiallyInstalled,
                        Path = executablePath,
                        ErrorMessage = "Ollama installed but not running. Start Ollama to use local AI features."
                    };
                }
                
                return new DependencyReport
                {
                    Id = "ollama",
                    DisplayName = "Ollama",
                    Status = DependencyStatus.Missing,
                    ErrorMessage = "Ollama not found. Install Ollama to use local AI models."
                };
            }
            catch (OperationCanceledException)
            {
                return new DependencyReport
                {
                    Id = "ollama",
                    DisplayName = "Ollama",
                    Status = DependencyStatus.Missing,
                    ErrorMessage = "Connection timeout while checking Ollama"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Ollama rescan");
            return new DependencyReport
            {
                Id = "ollama",
                DisplayName = "Ollama",
                Status = DependencyStatus.Missing,
                ErrorMessage = $"Error checking Ollama: {ex.Message}"
            };
        }
    }
    
    private string? FindOllamaExecutable()
    {
        try
        {
            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
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
                    _logger.LogInformation("Found Ollama executable at {Path}", path);
                    return path;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error searching for Ollama executable");
        }

        return null;
    }

    private async Task<DependencyReport> RescanStableDiffusionAsync(CancellationToken ct)
    {
        try
        {
            // First check if SD WebUI process is running
            var pythonProcesses = System.Diagnostics.Process.GetProcessesByName("python");
            var isProbablyRunning = pythonProcesses.Any(p => 
            {
                try
                {
                    // Accessing MainModule can throw SecurityException or InvalidOperationException
                    // for processes owned by other users or system processes
                    return p.MainModule?.FileName?.Contains("stable-diffusion-webui", StringComparison.OrdinalIgnoreCase) ?? false;
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // Access denied - likely a process we don't have permission to inspect
                    _logger.LogDebug("Cannot access process {PID} module: {Error}", p.Id, ex.Message);
                    return false;
                }
                catch (InvalidOperationException ex)
                {
                    // Process has exited
                    _logger.LogDebug("Process {PID} has exited: {Error}", p.Id, ex.Message);
                    return false;
                }
            });
            
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            try
            {
                // Check if SD WebUI is running and accessible
                var response = await httpClient.GetAsync("http://127.0.0.1:7860", ct).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    return new DependencyReport
                    {
                        Id = "stable-diffusion-webui",
                        DisplayName = "Stable Diffusion WebUI",
                        Status = DependencyStatus.Installed,
                        Path = "http://127.0.0.1:7860",
                        Provenance = "Running service",
                        ValidationOutput = "Web interface responding"
                    };
                }
                else
                {
                    return new DependencyReport
                    {
                        Id = "stable-diffusion-webui",
                        DisplayName = "Stable Diffusion WebUI",
                        Status = DependencyStatus.PartiallyInstalled,
                        ErrorMessage = $"SD WebUI returned status {response.StatusCode}"
                    };
                }
            }
            catch (System.Net.Http.HttpRequestException)
            {
                // API not responding, check if we can find the installation
                if (isProbablyRunning)
                {
                    return new DependencyReport
                    {
                        Id = "stable-diffusion-webui",
                        DisplayName = "Stable Diffusion WebUI",
                        Status = DependencyStatus.PartiallyInstalled,
                        ErrorMessage = "SD WebUI process detected but web interface not responding. It may still be starting up."
                    };
                }
                
                // Check for SD WebUI in default locations
                var installPath = FindStableDiffusionWebUI();
                if (!string.IsNullOrEmpty(installPath))
                {
                    return new DependencyReport
                    {
                        Id = "stable-diffusion-webui",
                        DisplayName = "Stable Diffusion WebUI",
                        Status = DependencyStatus.PartiallyInstalled,
                        Path = installPath,
                        ErrorMessage = "Stable Diffusion WebUI installed but not running. Start the WebUI to generate images locally."
                    };
                }
                
                return new DependencyReport
                {
                    Id = "stable-diffusion-webui",
                    DisplayName = "Stable Diffusion WebUI",
                    Status = DependencyStatus.Missing,
                    ErrorMessage = "Stable Diffusion WebUI not found. Install to generate images locally."
                };
            }
            catch (OperationCanceledException)
            {
                return new DependencyReport
                {
                    Id = "stable-diffusion-webui",
                    DisplayName = "Stable Diffusion WebUI",
                    Status = DependencyStatus.Missing,
                    ErrorMessage = "Connection timeout while checking Stable Diffusion WebUI"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Stable Diffusion WebUI rescan");
            return new DependencyReport
            {
                Id = "stable-diffusion-webui",
                DisplayName = "Stable Diffusion WebUI",
                Status = DependencyStatus.Missing,
                ErrorMessage = $"Error checking SD WebUI: {ex.Message}"
            };
        }
    }
    
    private string? FindStableDiffusionWebUI()
    {
        try
        {
            var searchPaths = new[]
            {
                Path.Combine(_appDataPath, "dependencies", "stable-diffusion-webui"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "stable-diffusion-webui"),
                "C:\\stable-diffusion-webui",
            };

            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path))
                {
                    // Check for webui.py or webui-user.bat
                    var webUiPy = Path.Combine(path, "webui.py");
                    var webUiBat = Path.Combine(path, "webui-user.bat");
                    
                    if (File.Exists(webUiPy) || File.Exists(webUiBat))
                    {
                        _logger.LogInformation("Found Stable Diffusion WebUI at {Path}", path);
                        return path;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error searching for Stable Diffusion WebUI");
        }

        return null;
    }

    private async Task<DependencyReport> RescanPiperAsync(CancellationToken ct)
    {
        await Task.CompletedTask;
        try
        {
            // Check common installation paths for Piper
            var dependenciesDir = Path.Combine(_appDataPath, "dependencies");
            var piperExe = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? "piper.exe"
                : "piper";
            
            var candidatePaths = new[]
            {
                Path.Combine(dependenciesDir, "piper", piperExe),
                Path.Combine(dependenciesDir, "bin", piperExe),
                Path.Combine(dependenciesDir, piperExe)
            };

            foreach (var path in candidatePaths)
            {
                if (File.Exists(path))
                {
                    return new DependencyReport
                    {
                        Id = "piper",
                        DisplayName = "Piper TTS",
                        Status = DependencyStatus.Installed,
                        Path = path,
                        Provenance = "File system"
                    };
                }
            }

            return new DependencyReport
            {
                Id = "piper",
                DisplayName = "Piper TTS",
                Status = DependencyStatus.Missing,
                ErrorMessage = "Piper executable not found in expected locations"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rescan Piper");
            return new DependencyReport
            {
                Id = "piper",
                DisplayName = "Piper TTS",
                Status = DependencyStatus.Missing,
                ErrorMessage = ex.Message
            };
        }
    }

    private async Task<DependencyReport> RescanPythonAsync(CancellationToken ct)
    {
        try
        {
            // Try to find Python in PATH
            var pythonExe = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)
                ? "python.exe"
                : "python3";
            
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                return new DependencyReport
                {
                    Id = "python",
                    DisplayName = "Python",
                    Status = DependencyStatus.Missing,
                    ErrorMessage = "Failed to start Python process"
                };
            }

            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                return new DependencyReport
                {
                    Id = "python",
                    DisplayName = "Python",
                    Status = DependencyStatus.Installed,
                    Path = pythonExe,
                    ValidationOutput = output.Trim(),
                    Provenance = "System PATH"
                };
            }
            else
            {
                return new DependencyReport
                {
                    Id = "python",
                    DisplayName = "Python",
                    Status = DependencyStatus.PartiallyInstalled,
                    ErrorMessage = $"Python exited with code {process.ExitCode}"
                };
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new DependencyReport
            {
                Id = "python",
                DisplayName = "Python",
                Status = DependencyStatus.Missing,
                ErrorMessage = "Python not found in PATH"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rescan Python");
            return new DependencyReport
            {
                Id = "python",
                DisplayName = "Python",
                Status = DependencyStatus.Missing,
                ErrorMessage = ex.Message
            };
        }
    }
}
