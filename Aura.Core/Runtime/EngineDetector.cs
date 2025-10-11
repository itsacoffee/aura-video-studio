using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Runtime;

/// <summary>
/// Detects installed and running engines (FFmpeg, Ollama, SD, TTS) on the system
/// </summary>
public class EngineDetector
{
    private readonly ILogger<EngineDetector> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _toolsRoot;

    public EngineDetector(
        ILogger<EngineDetector> logger,
        HttpClient httpClient,
        string toolsRoot)
    {
        _logger = logger;
        _httpClient = httpClient;
        _toolsRoot = toolsRoot;
    }

    /// <summary>
    /// Detect FFmpeg installation and version
    /// </summary>
    public async Task<EngineDetectionResult> DetectFFmpegAsync(string? configuredPath = null)
    {
        try
        {
            // Check configured path first
            if (!string.IsNullOrEmpty(configuredPath) && File.Exists(configuredPath))
            {
                var version = await GetFFmpegVersionAsync(configuredPath).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(version))
                {
                    _logger.LogInformation("FFmpeg found at configured path: {Path}", configuredPath);
                    return new EngineDetectionResult(
                        "ffmpeg",
                        "FFmpeg",
                        true,
                        false,
                        version,
                        configuredPath,
                        null,
                        null);
                }
            }

            // Check bundled path
            var bundledPath = Path.Combine(_toolsRoot, "ffmpeg", "ffmpeg" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
            if (File.Exists(bundledPath))
            {
                var version = await GetFFmpegVersionAsync(bundledPath).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(version))
                {
                    _logger.LogInformation("FFmpeg found in bundled path: {Path}", bundledPath);
                    return new EngineDetectionResult(
                        "ffmpeg",
                        "FFmpeg",
                        true,
                        false,
                        version,
                        bundledPath,
                        null,
                        null);
                }
            }

            // Check PATH
            var pathVersion = await GetFFmpegVersionAsync("ffmpeg").ConfigureAwait(false);
            if (!string.IsNullOrEmpty(pathVersion))
            {
                _logger.LogInformation("FFmpeg found on PATH");
                return new EngineDetectionResult(
                    "ffmpeg",
                    "FFmpeg",
                    true,
                    false,
                    pathVersion,
                    "PATH",
                    null,
                    null);
            }

            return new EngineDetectionResult(
                "ffmpeg",
                "FFmpeg",
                false,
                false,
                null,
                null,
                null,
                "FFmpeg not found. Install via Download Center or manually add to PATH.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error detecting FFmpeg");
            return new EngineDetectionResult(
                "ffmpeg",
                "FFmpeg",
                false,
                false,
                null,
                null,
                null,
                $"Detection error: {ex.Message}");
        }
    }

    /// <summary>
    /// Detect Ollama installation and running status
    /// </summary>
    public async Task<EngineDetectionResult> DetectOllamaAsync(string? configuredUrl = null, CancellationToken ct = default)
    {
        var baseUrl = configuredUrl ?? "http://127.0.0.1:11434";

        try
        {
            // Try to connect to Ollama API
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            var response = await _httpClient.GetAsync($"{baseUrl}/api/tags", cts.Token).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Ollama detected running at {Url}", baseUrl);
                
                // Try to get version
                string? version = null;
                try
                {
                    var versionResponse = await _httpClient.GetAsync($"{baseUrl}/api/version", cts.Token).ConfigureAwait(false);
                    if (versionResponse.IsSuccessStatusCode)
                    {
                        var versionJson = await versionResponse.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                        var versionDoc = System.Text.Json.JsonDocument.Parse(versionJson);
                        if (versionDoc.RootElement.TryGetProperty("version", out var versionProp))
                        {
                            version = versionProp.GetString();
                        }
                    }
                }
                catch
                {
                    // Version detection is optional
                }

                return new EngineDetectionResult(
                    "ollama",
                    "Ollama",
                    true,
                    true,
                    version,
                    null,
                    11434,
                    null);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            // Not running, continue to check if installed
            _logger.LogDebug("Ollama not responding at {Url}", baseUrl);
        }

        // Check if ollama process is in PATH (installed but not running)
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = "ollama",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process != null)
            {
                await process.WaitForExitAsync(ct).ConfigureAwait(false);
                
                if (process.ExitCode == 0)
                {
                    var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
                    var version = output.Trim();
                    
                    _logger.LogInformation("Ollama installed (version: {Version}) but not running", version);
                    return new EngineDetectionResult(
                        "ollama",
                        "Ollama",
                        true,
                        false,
                        version,
                        "PATH",
                        null,
                        "Ollama is installed but not running. Start it to use local LLM features.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Ollama not found in PATH");
        }

        return new EngineDetectionResult(
            "ollama",
            "Ollama",
            false,
            false,
            null,
            null,
            null,
            "Ollama not found. Download from ollama.ai to use local LLM features.");
    }

    /// <summary>
    /// Detect Stable Diffusion WebUI (AUTOMATIC1111)
    /// </summary>
    public async Task<EngineDetectionResult> DetectStableDiffusionWebUIAsync(int port = 7860, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var response = await _httpClient.GetAsync($"http://127.0.0.1:{port}/sdapi/v1/sd-models", cts.Token)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Stable Diffusion WebUI detected running on port {Port}", port);
                return new EngineDetectionResult(
                    "stable-diffusion-webui",
                    "Stable Diffusion WebUI",
                    true,
                    true,
                    null,
                    null,
                    port,
                    null);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            // Not running
            _logger.LogDebug("Stable Diffusion WebUI not responding on port {Port}", port);
        }

        // Check if installed in tools directory
        var installPath = Path.Combine(_toolsRoot, "stable-diffusion-webui");
        if (Directory.Exists(installPath))
        {
            var entrypointPath = Path.Combine(installPath, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
                ? "webui-user.bat" 
                : "webui.sh");
            
            if (File.Exists(entrypointPath))
            {
                _logger.LogInformation("Stable Diffusion WebUI installed at {Path}", installPath);
                return new EngineDetectionResult(
                    "stable-diffusion-webui",
                    "Stable Diffusion WebUI",
                    true,
                    false,
                    null,
                    installPath,
                    null,
                    "Stable Diffusion WebUI is installed but not running.");
            }
        }

        return new EngineDetectionResult(
            "stable-diffusion-webui",
            "Stable Diffusion WebUI",
            false,
            false,
            null,
            null,
            null,
            "Not installed. Install via Download Center to use local image generation.");
    }

    /// <summary>
    /// Detect ComfyUI
    /// </summary>
    public async Task<EngineDetectionResult> DetectComfyUIAsync(int port = 8188, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var response = await _httpClient.GetAsync($"http://127.0.0.1:{port}/system_stats", cts.Token)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("ComfyUI detected running on port {Port}", port);
                return new EngineDetectionResult(
                    "comfyui",
                    "ComfyUI",
                    true,
                    true,
                    null,
                    null,
                    port,
                    null);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            _logger.LogDebug("ComfyUI not responding on port {Port}", port);
        }

        // Check if installed in tools directory
        var installPath = Path.Combine(_toolsRoot, "comfyui");
        if (Directory.Exists(installPath))
        {
            var mainPath = Path.Combine(installPath, "main.py");
            if (File.Exists(mainPath))
            {
                _logger.LogInformation("ComfyUI installed at {Path}", installPath);
                return new EngineDetectionResult(
                    "comfyui",
                    "ComfyUI",
                    true,
                    false,
                    null,
                    installPath,
                    null,
                    "ComfyUI is installed but not running.");
            }
        }

        return new EngineDetectionResult(
            "comfyui",
            "ComfyUI",
            false,
            false,
            null,
            null,
            null,
            "Not installed. Install via Download Center for node-based image generation.");
    }

    /// <summary>
    /// Detect Piper TTS
    /// </summary>
    public async Task<EngineDetectionResult> DetectPiperAsync(CancellationToken ct = default)
    {
        try
        {
            // Check bundled path
            var bundledPath = Path.Combine(_toolsRoot, "piper", "piper" + (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : ""));
            if (File.Exists(bundledPath))
            {
                var version = await GetCommandVersionAsync(bundledPath, "--version", ct).ConfigureAwait(false);
                _logger.LogInformation("Piper found in bundled path: {Path}", bundledPath);
                return new EngineDetectionResult(
                    "piper",
                    "Piper TTS",
                    true,
                    false,
                    version,
                    bundledPath,
                    null,
                    null);
            }

            // Check PATH
            var pathVersion = await GetCommandVersionAsync("piper", "--version", ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(pathVersion))
            {
                _logger.LogInformation("Piper found on PATH");
                return new EngineDetectionResult(
                    "piper",
                    "Piper TTS",
                    true,
                    false,
                    pathVersion,
                    "PATH",
                    null,
                    null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error detecting Piper");
        }

        return new EngineDetectionResult(
            "piper",
            "Piper TTS",
            false,
            false,
            null,
            null,
            null,
            "Not installed. Install via Download Center for fast local TTS.");
    }

    /// <summary>
    /// Detect Mimic3 TTS
    /// </summary>
    public async Task<EngineDetectionResult> DetectMimic3Async(int port = 59125, CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));

            var response = await _httpClient.GetAsync($"http://127.0.0.1:{port}/api/voices", cts.Token)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Mimic3 detected running on port {Port}", port);
                return new EngineDetectionResult(
                    "mimic3",
                    "Mimic3 TTS",
                    true,
                    true,
                    null,
                    null,
                    port,
                    null);
            }
        }
        catch (Exception ex) when (ex is TaskCanceledException or HttpRequestException)
        {
            _logger.LogDebug("Mimic3 not responding on port {Port}", port);
        }

        // Check if installed in tools directory
        var installPath = Path.Combine(_toolsRoot, "mimic3");
        if (Directory.Exists(installPath))
        {
            _logger.LogInformation("Mimic3 installed at {Path}", installPath);
            return new EngineDetectionResult(
                "mimic3",
                "Mimic3 TTS",
                true,
                false,
                null,
                installPath,
                null,
                "Mimic3 is installed but not running.");
        }

        return new EngineDetectionResult(
            "mimic3",
            "Mimic3 TTS",
            false,
            false,
            null,
            null,
            null,
            "Not installed. Install via Download Center for high-quality local TTS.");
    }

    /// <summary>
    /// Detect all engines
    /// </summary>
    public async Task<List<EngineDetectionResult>> DetectAllEnginesAsync(
        string? ffmpegPath = null,
        string? ollamaUrl = null,
        CancellationToken ct = default)
    {
        var tasks = new[]
        {
            DetectFFmpegAsync(ffmpegPath),
            DetectOllamaAsync(ollamaUrl, ct),
            DetectStableDiffusionWebUIAsync(7860, ct),
            DetectComfyUIAsync(8188, ct),
            DetectPiperAsync(ct),
            DetectMimic3Async(59125, ct)
        };

        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.ToList();
    }

    private async Task<string?> GetFFmpegVersionAsync(string command)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
            await process.WaitForExitAsync().ConfigureAwait(false);

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                // Extract version from first line: "ffmpeg version N-xxxxx-..."
                var firstLine = output.Split('\n')[0];
                if (firstLine.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 3)
                    {
                        return parts[2]; // Version string
                    }
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }

    private async Task<string?> GetCommandVersionAsync(string command, string args, CancellationToken ct)
    {
        try
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null) return null;

            var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode == 0)
            {
                return output.Trim();
            }
        }
        catch
        {
            // Ignore errors
        }

        return null;
    }
}

/// <summary>
/// Result of engine detection
/// </summary>
public record EngineDetectionResult(
    string Id,
    string Name,
    bool IsInstalled,
    bool IsRunning,
    string? Version,
    string? InstallPath,
    int? Port,
    string? Message
);
