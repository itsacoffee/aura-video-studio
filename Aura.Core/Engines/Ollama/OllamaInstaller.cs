using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Engines.Ollama;

/// <summary>
/// Result of Ollama installation operations
/// </summary>
public record OllamaInstallResult(
    bool Success,
    string? InstallPath = null,
    string? ErrorMessage = null,
    string? Phase = null,
    string? Version = null
);

/// <summary>
/// Progress information for Ollama installation
/// </summary>
public record OllamaInstallProgress(
    string Phase,
    float PercentComplete,
    string Message,
    string? SubPhase = null
);

/// <summary>
/// Handles installation and management of Ollama
/// </summary>
public class OllamaInstaller
{
    private readonly ILogger<OllamaInstaller> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _installRoot;

    private const string OLLAMA_DIR = "ollama";
    private const string GITHUB_REPO = "ollama/ollama";
    private const string GITHUB_API_URL = "https://api.github.com/repos/{0}/releases/latest";
    private const string WINDOWS_ASSET_PATTERN = "ollama-windows-amd64.zip";

    public OllamaInstaller(
        ILogger<OllamaInstaller> logger,
        HttpClient httpClient,
        string installRoot)
    {
        _logger = logger;
        _httpClient = httpClient;
        _installRoot = installRoot;

        if (!Directory.Exists(_installRoot))
        {
            Directory.CreateDirectory(_installRoot);
        }
    }

    /// <summary>
    /// Get the installation path for Ollama
    /// </summary>
    public string GetInstallPath() => Path.Combine(_installRoot, OLLAMA_DIR);

    /// <summary>
    /// Check if Ollama is installed (managed installation)
    /// </summary>
    public bool IsInstalled()
    {
        var installPath = GetInstallPath();
        var executablePath = GetExecutablePath();

        return Directory.Exists(installPath) && File.Exists(executablePath);
    }

    /// <summary>
    /// Get the path to the Ollama executable
    /// </summary>
    public string GetExecutablePath()
    {
        var installPath = GetInstallPath();
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Path.Combine(installPath, "ollama.exe")
            : Path.Combine(installPath, "ollama");
    }

    /// <summary>
    /// Get installed Ollama version
    /// </summary>
    public async Task<string?> GetInstalledVersionAsync(CancellationToken ct = default)
    {
        if (!IsInstalled())
        {
            return null;
        }

        var executablePath = GetExecutablePath();

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            await process.WaitForExitAsync(ct).ConfigureAwait(false);

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                // Parse version from output (e.g., "ollama version 0.1.x")
                var version = output.Trim().Split('\n')[0];
                if (version.Contains("version"))
                {
                    var parts = version.Split(' ');
                    if (parts.Length >= 3)
                    {
                        return parts[2].Trim();
                    }
                }
                return version.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Ollama version");
        }

        return null;
    }

    /// <summary>
    /// Install Ollama from GitHub releases
    /// </summary>
    public async Task<OllamaInstallResult> InstallAsync(
        IProgress<OllamaInstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Ollama installation");

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return new OllamaInstallResult(
                Success: false,
                ErrorMessage: "Managed Ollama installation is only supported on Windows. Please install Ollama manually from https://ollama.com",
                Phase: "prerequisites"
            );
        }

        var installPath = GetInstallPath();

        try
        {
            // Phase 1: Resolve download URL (5%)
            progress?.Report(new OllamaInstallProgress(
                "Resolving download URL", 5, "Fetching latest release from GitHub..."));

            var downloadUrl = await ResolveDownloadUrlAsync(ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(downloadUrl))
            {
                return new OllamaInstallResult(
                    Success: false,
                    ErrorMessage: "Failed to resolve download URL from GitHub releases",
                    Phase: "resolve"
                );
            }

            _logger.LogInformation("Resolved download URL: {Url}", downloadUrl);

            // Phase 2: Download (5-70%)
            progress?.Report(new OllamaInstallProgress(
                "Downloading", 10, "Downloading Ollama...", "download"));

            var tempZipPath = Path.Combine(Path.GetTempPath(), $"ollama-{Guid.NewGuid()}.zip");

            try
            {
                await DownloadFileAsync(downloadUrl, tempZipPath, progress, ct).ConfigureAwait(false);

                // Phase 3: Extract (70-90%)
                progress?.Report(new OllamaInstallProgress(
                    "Extracting", 70, "Extracting Ollama files...", "extract"));

                // Create install directory
                if (Directory.Exists(installPath))
                {
                    Directory.Delete(installPath, true);
                }
                Directory.CreateDirectory(installPath);

                // Extract zip
                ZipFile.ExtractToDirectory(tempZipPath, installPath);

                // Handle nested directory structure if present
                await FlattenDirectoryStructureAsync(installPath, ct).ConfigureAwait(false);

                // Phase 4: Verify (90-100%)
                progress?.Report(new OllamaInstallProgress(
                    "Verifying", 90, "Verifying installation..."));

                if (!File.Exists(GetExecutablePath()))
                {
                    return new OllamaInstallResult(
                        Success: false,
                        ErrorMessage: "Installation verification failed: ollama executable not found",
                        Phase: "verify"
                    );
                }

                var version = await GetInstalledVersionAsync(ct).ConfigureAwait(false);

                progress?.Report(new OllamaInstallProgress(
                    "Complete", 100, $"Ollama {version ?? "latest"} installed successfully"));

                _logger.LogInformation("Ollama installed successfully at {Path}", installPath);

                return new OllamaInstallResult(
                    Success: true,
                    InstallPath: installPath,
                    Phase: "complete",
                    Version: version
                );
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempZipPath))
                {
                    try { File.Delete(tempZipPath); } catch { /* Ignore cleanup errors */ }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Installation cancelled by user");

            // Clean up partial installation
            if (Directory.Exists(installPath))
            {
                try { Directory.Delete(installPath, true); } catch { /* Ignore cleanup errors */ }
            }

            return new OllamaInstallResult(
                Success: false,
                ErrorMessage: "Installation was cancelled.",
                Phase: "cancelled"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error installing Ollama");

            return new OllamaInstallResult(
                Success: false,
                ErrorMessage: $"Installation failed: {ex.Message}",
                Phase: "error"
            );
        }
    }

    /// <summary>
    /// Remove the Ollama installation
    /// </summary>
    public async Task<bool> RemoveAsync(CancellationToken ct = default)
    {
        var installPath = GetInstallPath();

        if (!Directory.Exists(installPath))
        {
            return true;
        }

        _logger.LogInformation("Removing Ollama from {Path}", installPath);

        try
        {
            await Task.Run(() => Directory.Delete(installPath, true), ct).ConfigureAwait(false);
            _logger.LogInformation("Ollama removed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing Ollama");
            return false;
        }
    }

    /// <summary>
    /// Resolve the download URL from GitHub releases API
    /// </summary>
    private async Task<string?> ResolveDownloadUrlAsync(CancellationToken ct)
    {
        try
        {
            var apiUrl = string.Format(GITHUB_API_URL, GITHUB_REPO);

            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("User-Agent", "Aura-Video-Studio");
            request.Headers.Add("Accept", "application/vnd.github.v3+json");

            using var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("assets", out var assets))
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var name) &&
                        name.GetString()?.Equals(WINDOWS_ASSET_PATTERN, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        if (asset.TryGetProperty("browser_download_url", out var url))
                        {
                            return url.GetString();
                        }
                    }
                }
            }

            _logger.LogWarning("Could not find matching asset in GitHub release");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving download URL from GitHub");
            return null;
        }
    }

    /// <summary>
    /// Download file with progress reporting
    /// </summary>
    private async Task DownloadFileAsync(
        string url,
        string destinationPath,
        IProgress<OllamaInstallProgress>? progress,
        CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? 0;
        var buffer = new byte[81920]; // 80KB buffer
        long bytesRead = 0;

        await using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        int read;
        while ((read = await contentStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            bytesRead += read;

            if (totalBytes > 0)
            {
                // Map download progress to 10-70% range
                var downloadPercent = (float)bytesRead / totalBytes;
                var overallPercent = 10 + (downloadPercent * 60);
                progress?.Report(new OllamaInstallProgress(
                    "Downloading",
                    overallPercent,
                    $"Downloaded {FormatBytes(bytesRead)} / {FormatBytes(totalBytes)}",
                    "download"
                ));
            }
        }
    }

    /// <summary>
    /// Flatten directory structure if files are nested inside a subdirectory
    /// </summary>
    private async Task FlattenDirectoryStructureAsync(string installPath, CancellationToken ct)
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "ollama.exe" : "ollama";

        // Check if executable exists at root level
        if (File.Exists(Path.Combine(installPath, executableName)))
        {
            return;
        }

        // Look for executable in subdirectories
        var subdirs = Directory.GetDirectories(installPath);
        foreach (var subdir in subdirs)
        {
            var nestedExecutable = Path.Combine(subdir, executableName);
            if (File.Exists(nestedExecutable))
            {
                _logger.LogInformation("Flattening directory structure from {SubDir}", subdir);

                // Move all files from subdirectory to root
                foreach (var file in Directory.GetFiles(subdir))
                {
                    var destPath = Path.Combine(installPath, Path.GetFileName(file));
                    if (!File.Exists(destPath))
                    {
                        File.Move(file, destPath);
                    }
                }

                // Move all subdirectories from nested directory to root
                foreach (var nestedSubdir in Directory.GetDirectories(subdir))
                {
                    var destPath = Path.Combine(installPath, Path.GetFileName(nestedSubdir));
                    if (!Directory.Exists(destPath))
                    {
                        Directory.Move(nestedSubdir, destPath);
                    }
                }

                // Remove the now-empty subdirectory
                try { Directory.Delete(subdir, true); } catch { /* Ignore */ }

                break;
            }
        }

        await Task.CompletedTask;
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
