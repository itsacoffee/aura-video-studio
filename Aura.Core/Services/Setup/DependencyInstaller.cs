using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Setup;

public record InstallProgress(
    int Percentage,
    string Status,
    string CurrentFile,
    long BytesDownloaded,
    long TotalBytes
);

/// <summary>
/// URLs for Piper TTS binary and voice model downloads
/// </summary>
public static class PiperDownloadUrls
{
    public const string WindowsBinaryUrl = "https://github.com/rhasspy/piper/releases/latest/download/piper_windows_amd64.tar.gz";
    public const string DefaultVoiceModelUrl = "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx";
    public const string VoiceModelsBaseUrl = "https://huggingface.co/rhasspy/piper-voices/resolve/main";
}

public class DependencyInstaller
{
    private readonly ILogger<DependencyInstaller> _logger;
    private readonly HttpClient _httpClient;
    private const int MaxRetries = 3;

    public DependencyInstaller(
        ILogger<DependencyInstaller> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> InstallFFmpegAsync(
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting FFmpeg installation");

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var installDir = Path.Combine(localAppData, "Aura", "ffmpeg");

            progress?.Report(new InstallProgress(0, "Determining platform...", "", 0, 0));

            // Determine download URL based on OS
            string downloadUrl;
            string fileName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                downloadUrl = "https://github.com/GyanD/codexffmpeg/releases/download/7.0.2/ffmpeg-7.0.2-full_build.zip";
                fileName = "ffmpeg-windows.zip";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                downloadUrl = "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz";
                fileName = "ffmpeg-linux.tar.xz";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                downloadUrl = "https://evermeet.cx/ffmpeg/getrelease/zip";
                fileName = "ffmpeg-macos.zip";
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported operating system");
            }

            progress?.Report(new InstallProgress(5, "Downloading FFmpeg...", fileName, 0, 0));

            // Download with retries
            var downloadPath = Path.Combine(Path.GetTempPath(), fileName);
            await DownloadWithRetriesAsync(downloadUrl, downloadPath, progress, ct).ConfigureAwait(false);

            progress?.Report(new InstallProgress(60, "Extracting FFmpeg...", "", 0, 0));

            // Extract
            if (Directory.Exists(installDir))
            {
                Directory.Delete(installDir, true);
            }
            Directory.CreateDirectory(installDir);

            if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                ExtractZipToDirectory(downloadPath, installDir);
            }
            else if (fileName.EndsWith(".tar.xz", StringComparison.OrdinalIgnoreCase))
            {
                // For tar.xz on Linux, we need to use tar command
                await ExtractTarXzAsync(downloadPath, installDir, ct).ConfigureAwait(false);
            }

            progress?.Report(new InstallProgress(90, "Setting up PATH...", "", 0, 0));

            // Add to PATH (user level)
            AddToUserPath(Path.Combine(installDir, "bin"));

            progress?.Report(new InstallProgress(100, "FFmpeg installed successfully", "", 0, 0));

            // Clean up
            try
            {
                File.Delete(downloadPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up download file");
            }

            _logger.LogInformation("FFmpeg installation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg installation failed");
            progress?.Report(new InstallProgress(0, $"Installation failed: {ex.Message}", "", 0, 0));
            return false;
        }
    }

    public async Task<bool> InstallPiperTtsAsync(
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting Piper TTS installation");

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var installDir = Path.Combine(localAppData, "Aura", "Tools", "piper");
            var voicesDir = Path.Combine(installDir, "voices");

            progress?.Report(new InstallProgress(0, "Preparing installation...", "", 0, 0));

            // Only supported on Windows for managed installation
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogWarning("Piper TTS managed installation is only available on Windows. Please install manually from https://github.com/rhasspy/piper/releases");
                progress?.Report(new InstallProgress(0, "Managed installation only available on Windows. Please install manually.", "", 0, 0));
                return false;
            }

            progress?.Report(new InstallProgress(5, "Resolving download URL...", "", 0, 0));

            progress?.Report(new InstallProgress(10, "Downloading Piper...", "piper_windows_amd64.tar.gz", 0, 0));

            var downloadPath = Path.Combine(Path.GetTempPath(), $"piper_{Guid.NewGuid():N}.tar.gz");

            // Download Piper with retries using constant URL
            await DownloadWithRetriesAsync(PiperDownloadUrls.WindowsBinaryUrl, downloadPath, progress, ct).ConfigureAwait(false);

            progress?.Report(new InstallProgress(60, "Extracting Piper...", "", 0, 0));

            // Create installation directory
            Directory.CreateDirectory(installDir);
            Directory.CreateDirectory(voicesDir);

            // Extract using tar command (available on Windows 10+)
            await ExtractTarGzAsync(downloadPath, installDir, ct).ConfigureAwait(false);

            // Find piper.exe
            var piperExePath = Directory.GetFiles(installDir, "piper.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (piperExePath == null)
            {
                _logger.LogError("Piper executable not found after extraction");
                progress?.Report(new InstallProgress(0, "Extraction failed: piper.exe not found", "", 0, 0));
                return false;
            }

            // Move to root of installDir if nested
            var targetPath = Path.Combine(installDir, "piper.exe");
            if (piperExePath != targetPath)
            {
                File.Copy(piperExePath, targetPath, overwrite: true);
            }

            progress?.Report(new InstallProgress(75, "Downloading default voice model...", "en_US-lessac-medium.onnx", 0, 0));

            // Download default voice model using constant URL
            var voiceModelPath = Path.Combine(voicesDir, "en_US-lessac-medium.onnx");

            try
            {
                using var response = await _httpClient.GetAsync(PiperDownloadUrls.DefaultVoiceModelUrl, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var fileStream = new FileStream(voiceModelPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
                await contentStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);

                _logger.LogInformation("Voice model downloaded successfully: {Path}", voiceModelPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download voice model, installation will continue without it");
            }

            progress?.Report(new InstallProgress(95, "Verifying installation...", "", 0, 0));

            // Verify Piper works
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = targetPath,
                        Arguments = "--help",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync(ct).ConfigureAwait(false);

                if (process.ExitCode != 0)
                {
                    _logger.LogWarning("Piper verification returned non-zero exit code: {ExitCode}", process.ExitCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Piper verification failed, but installation may still work");
            }

            // Clean up download file
            try
            {
                File.Delete(downloadPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up download file");
            }

            progress?.Report(new InstallProgress(100, "Piper TTS installed successfully", "", 0, 0));

            _logger.LogInformation("Piper TTS installation completed successfully at {Path}", targetPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Piper TTS installation failed");
            progress?.Report(new InstallProgress(0, $"Installation failed: {ex.Message}", "", 0, 0));
            return false;
        }
    }

    public async Task<bool> DownloadStockAssetsAsync(
        IProgress<InstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Starting stock assets download");

        try
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var assetsDir = Path.Combine(localAppData, "Aura", "assets");

            progress?.Report(new InstallProgress(0, "Downloading stock assets...", "stock-pack.zip", 0, 0));

            // Note: This URL should be configured via appsettings or CDN
            // For now, using a placeholder that would need to be replaced
            var downloadUrl = "https://github.com/Coffee285/aura-video-studio/releases/download/v1.0.0/stock-assets.zip";
            var downloadPath = Path.Combine(Path.GetTempPath(), "stock-assets.zip");

            try
            {
                await DownloadWithRetriesAsync(downloadUrl, downloadPath, progress, ct).ConfigureAwait(false);

                progress?.Report(new InstallProgress(70, "Extracting stock assets...", "", 0, 0));

                if (Directory.Exists(assetsDir))
                {
                    Directory.Delete(assetsDir, true);
                }
                Directory.CreateDirectory(assetsDir);

                ExtractZipToDirectory(downloadPath, assetsDir);

                progress?.Report(new InstallProgress(100, "Stock assets downloaded successfully", "", 0, 0));

                // Clean up
                try
                {
                    File.Delete(downloadPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to clean up download file");
                }

                _logger.LogInformation("Stock assets download completed successfully");
                return true;
            }
            catch (HttpRequestException)
            {
                // Stock assets are optional, so we'll just log and return success
                _logger.LogInformation("Stock assets not available, skipping");
                progress?.Report(new InstallProgress(100, "Stock assets not available (optional)", "", 0, 0));
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock assets download failed");
            progress?.Report(new InstallProgress(0, $"Download failed: {ex.Message}", "", 0, 0));
            return false;
        }
    }

    private async Task DownloadWithRetriesAsync(
        string url,
        string destinationPath,
        IProgress<InstallProgress>? progress,
        CancellationToken ct)
    {
        for (int attempt = 1; attempt <= MaxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Downloading {Url} (attempt {Attempt}/{MaxRetries})", url, attempt, MaxRetries);

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var bytesDownloaded = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct).ConfigureAwait(false);
                    bytesDownloaded += bytesRead;

                    if (progress != null && totalBytes > 0)
                    {
                        var percentage = (int)((bytesDownloaded * 100) / totalBytes);
                        // Scale percentage to appropriate range (e.g., 5-60 for FFmpeg download)
                        progress.Report(new InstallProgress(
                            5 + (percentage * 55 / 100),
                            "Downloading...",
                            Path.GetFileName(url),
                            bytesDownloaded,
                            totalBytes
                        ));
                    }
                }

                _logger.LogInformation("Download completed successfully");
                return;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "Download attempt {Attempt} failed, retrying...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(2 * attempt), ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download failed after {MaxRetries} attempts", MaxRetries);

                // Clean up partial download
                try
                {
                    if (File.Exists(destinationPath))
                    {
                        File.Delete(destinationPath);
                    }
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to clean up partial download");
                }

                throw;
            }
        }
    }

    private static void ExtractZipToDirectory(string zipPath, string destinationDirectory)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            // Skip directory entries
            if (string.IsNullOrEmpty(entry.Name))
            {
                continue;
            }

            var destinationPath = Path.Combine(destinationDirectory, entry.FullName);
            var destinationDir = Path.GetDirectoryName(destinationPath);

            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            entry.ExtractToFile(destinationPath, true);
        }
    }

    private static async Task ExtractTarXzAsync(string tarXzPath, string destinationDirectory, CancellationToken ct)
    {
        // Extract tar.xz files using tar command on Linux
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "tar",
            Arguments = $"-xJf \"{tarXzPath}\" -C \"{destinationDirectory}\" --strip-components=1",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start tar process");
        }

        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"tar extraction failed: {error}");
        }
    }

    private static async Task ExtractTarGzAsync(string tarGzPath, string destinationDirectory, CancellationToken ct)
    {
        // Extract tar.gz files using tar command
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "tar",
            Arguments = $"-xzf \"{tarGzPath}\" -C \"{destinationDirectory}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start tar process");
        }

        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            throw new InvalidOperationException($"tar extraction failed: {error}");
        }
    }

    private void AddToUserPath(string directory)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var scope = EnvironmentVariableTarget.User;
                var currentPath = Environment.GetEnvironmentVariable("PATH", scope) ?? "";

                // Check if directory is already in PATH
                if (currentPath.Split(';').Any(p => p.Equals(directory, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Directory already in PATH");
                    return;
                }

                var newPath = string.IsNullOrEmpty(currentPath)
                    ? directory
                    : $"{currentPath};{directory}";

                Environment.SetEnvironmentVariable("PATH", newPath, scope);
                _logger.LogInformation("Added {Directory} to user PATH", directory);
            }
            else
            {
                // On Unix systems, we would need to modify shell rc files
                _logger.LogInformation("PATH modification on Unix systems requires manual configuration");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to add directory to PATH");
        }
    }
}
