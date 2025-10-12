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
        var manifest = await _componentDownloader.LoadManifestAsync();
        
        // Rescan each component
        foreach (var component in manifest.Components)
        {
            var dependencyReport = await RescanDependencyAsync(component, ct);
            report.Dependencies.Add(dependencyReport);
        }

        // Also check for FFprobe (related to FFmpeg)
        var ffprobeReport = await RescanFfprobeAsync(ct);
        report.Dependencies.Add(ffprobeReport);

        // Save last scan time
        await SaveLastScanTimeAsync(report.ScanTime);

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
                var json = await File.ReadAllTextAsync(_lastScanPath);
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
            await File.WriteAllTextAsync(_lastScanPath, json);
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
                return await RescanFfmpegAsync(ct);
            
            case "ollama":
                return await RescanOllamaAsync(ct);
            
            case "stable-diffusion-webui":
            case "sd-webui":
                return await RescanStableDiffusionAsync(ct);
            
            case "piper":
                return await RescanPiperAsync(ct);
            
            case "python":
                return await RescanPythonAsync(ct);
            
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
            var result = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct);
            
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
            var ffmpegResult = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct);
            
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
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            var response = await httpClient.GetAsync("http://127.0.0.1:11434/api/tags", ct);
            
            if (response.IsSuccessStatusCode)
            {
                return new DependencyReport
                {
                    Id = "ollama",
                    DisplayName = "Ollama",
                    Status = DependencyStatus.Installed,
                    Path = "http://127.0.0.1:11434",
                    Provenance = "Running service",
                    ValidationOutput = "API responding"
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
        catch (Exception ex)
        {
            return new DependencyReport
            {
                Id = "ollama",
                DisplayName = "Ollama",
                Status = DependencyStatus.Missing,
                ErrorMessage = $"Cannot connect to Ollama: {ex.Message}"
            };
        }
    }

    private async Task<DependencyReport> RescanStableDiffusionAsync(CancellationToken ct)
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);
            
            // Check if SD WebUI is running
            var response = await httpClient.GetAsync("http://127.0.0.1:7860", ct);
            
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
        catch (Exception ex)
        {
            return new DependencyReport
            {
                Id = "stable-diffusion-webui",
                DisplayName = "Stable Diffusion WebUI",
                Status = DependencyStatus.Missing,
                ErrorMessage = $"Cannot connect to SD WebUI: {ex.Message}"
            };
        }
    }

    private async Task<DependencyReport> RescanPiperAsync(CancellationToken ct)
    {
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

            await process.WaitForExitAsync(ct);
            var output = await process.StandardOutput.ReadToEndAsync(ct);

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
