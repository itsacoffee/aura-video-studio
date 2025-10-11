using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Downloads;

public record EngineInstallProgress(
    string EngineId,
    string Phase, // downloading, extracting, verifying, complete
    long BytesProcessed,
    long TotalBytes,
    float PercentComplete,
    string? Message = null
);

public record EngineVerificationResult(
    string EngineId,
    bool IsValid,
    string Status,
    List<string> MissingFiles,
    List<string> Issues
);

/// <summary>
/// Handles installation, verification, repair, and removal of engines
/// </summary>
public class EngineInstaller
{
    private readonly ILogger<EngineInstaller> _logger;
    private readonly HttpClient _httpClient;
    private readonly HttpDownloader _downloader;
    private readonly string _installRoot;

    public EngineInstaller(
        ILogger<EngineInstaller> logger,
        HttpClient httpClient,
        string installRoot)
    {
        _logger = logger;
        _httpClient = httpClient;
        _installRoot = installRoot;

        // Create HttpDownloader for robust downloads
        var downloaderLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<HttpDownloader>.Instance;
        if (logger is ILogger<HttpDownloader> typedLogger)
        {
            _downloader = new HttpDownloader(typedLogger, httpClient);
        }
        else
        {
            var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var downloaderLoggerTyped = loggerFactory.CreateLogger<HttpDownloader>();
            _downloader = new HttpDownloader(downloaderLoggerTyped, httpClient);
        }

        if (!Directory.Exists(_installRoot))
        {
            Directory.CreateDirectory(_installRoot);
        }
    }

    /// <summary>
    /// Install an engine from manifest entry
    /// </summary>
    public async Task InstallAsync(
        EngineManifestEntry engine,
        IProgress<EngineInstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Installing engine: {EngineId}", engine.Id);

        string installPath = Path.Combine(_installRoot, engine.Id);
        
        // Check if already installed
        if (Directory.Exists(installPath) && Directory.GetFiles(installPath).Length > 0)
        {
            _logger.LogWarning("Engine {EngineId} is already installed at {Path}", engine.Id, installPath);
            progress?.Report(new EngineInstallProgress(engine.Id, "complete", 0, 0, 100, "Already installed"));
            return;
        }

        Directory.CreateDirectory(installPath);

        try
        {
            // Get platform-specific URL
            string platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" : "linux";
            if (!engine.Urls.TryGetValue(platform, out string? url) || string.IsNullOrEmpty(url))
            {
                throw new InvalidOperationException($"No URL found for platform {platform}");
            }

            // Handle git repositories differently
            if (engine.ArchiveType == "git")
            {
                await InstallFromGitAsync(engine, url, installPath, progress, ct).ConfigureAwait(false);
            }
            else
            {
                await InstallFromArchiveAsync(engine, url, installPath, progress, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Engine {EngineId} installed successfully", engine.Id);
            progress?.Report(new EngineInstallProgress(engine.Id, "complete", engine.SizeBytes, engine.SizeBytes, 100, "Installation complete"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install engine {EngineId}", engine.Id);
            
            // Cleanup on failure
            if (Directory.Exists(installPath))
            {
                try
                {
                    Directory.Delete(installPath, true);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Failed to cleanup after failed install");
                }
            }
            
            throw;
        }
    }

    private async Task InstallFromGitAsync(
        EngineManifestEntry engine,
        string gitUrl,
        string installPath,
        IProgress<EngineInstallProgress>? progress,
        CancellationToken ct)
    {
        progress?.Report(new EngineInstallProgress(engine.Id, "downloading", 0, engine.SizeBytes, 0, "Cloning repository..."));

        // Use git clone
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = $"clone --depth 1 \"{gitUrl}\" \"{installPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        string output = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
        string error = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
        
        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            _logger.LogError("Git clone failed: {Error}", error);
            throw new InvalidOperationException($"Git clone failed: {error}");
        }

        _logger.LogInformation("Git clone completed for {EngineId}", engine.Id);
    }

    private async Task InstallFromArchiveAsync(
        EngineManifestEntry engine,
        string url,
        string installPath,
        IProgress<EngineInstallProgress>? progress,
        CancellationToken ct)
    {
        // Download archive with built-in checksum verification
        string archiveFile = Path.Combine(Path.GetTempPath(), $"{engine.Id}-{Guid.NewGuid()}.tmp");
        
        try
        {
            progress?.Report(new EngineInstallProgress(engine.Id, "downloading", 0, engine.SizeBytes, 0, "Downloading..."));
            
            // Use HttpDownloader with SHA256 verification
            var downloadProgress = new Progress<HttpDownloadProgress>(p =>
            {
                var phase = p.Message?.Contains("Verifying") == true ? "verifying" : "downloading";
                progress?.Report(new EngineInstallProgress(engine.Id, phase, p.BytesDownloaded, p.TotalBytes, p.PercentComplete, p.Message));
            });

            var success = await _downloader.DownloadFileAsync(url, archiveFile, engine.Sha256, downloadProgress, ct)
                .ConfigureAwait(false);

            if (!success)
            {
                throw new InvalidOperationException("Download or checksum verification failed");
            }

            // Extract archive
            progress?.Report(new EngineInstallProgress(engine.Id, "extracting", 0, 0, 0, "Extracting..."));
            await ExtractArchiveAsync(archiveFile, installPath, engine.ArchiveType).ConfigureAwait(false);
        }
        finally
        {
            if (File.Exists(archiveFile))
            {
                File.Delete(archiveFile);
            }
        }
    }

    private async Task DownloadFileAsync(
        string url,
        string filePath,
        long expectedSize,
        Action<(long BytesProcessed, long TotalBytes, float PercentComplete)>? progress,
        CancellationToken ct)
    {
        // Use HttpDownloader for robust downloading with resume support
        var downloadProgress = new Progress<HttpDownloadProgress>(p =>
        {
            progress?.Invoke((p.BytesDownloaded, p.TotalBytes, p.PercentComplete));
        });

        var success = await _downloader.DownloadFileAsync(url, filePath, null, downloadProgress, ct)
            .ConfigureAwait(false);

        if (!success)
        {
            throw new InvalidOperationException($"Failed to download file from {url}");
        }
    }

    private async Task<bool> VerifyChecksumAsync(string filePath, string expectedSha256, Action<(long BytesProcessed, long TotalBytes, float PercentComplete)>? progress = null)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var sha256 = SHA256.Create();
        
        long totalBytes = fs.Length;
        long bytesRead = 0;
        var buffer = new byte[8192];
        
        while (true)
        {
            int read = await fs.ReadAsync(buffer).ConfigureAwait(false);
            if (read == 0) break;
            
            sha256.TransformBlock(buffer, 0, read, null, 0);
            bytesRead += read;
            
            float percent = totalBytes > 0 ? (float)bytesRead / totalBytes * 100 : 0;
            progress?.Invoke((bytesRead, totalBytes, percent));
        }
        
        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        byte[] hashBytes = sha256.Hash!;
        string computedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();
        
        return computedHash.Equals(expectedSha256.ToLowerInvariant(), StringComparison.Ordinal);
    }

    private async Task ExtractArchiveAsync(string archiveFile, string targetDir, string archiveType)
    {
        await Task.Run(() =>
        {
            if (archiveType == "zip")
            {
                ZipFile.ExtractToDirectory(archiveFile, targetDir, overwriteFiles: true);
            }
            else if (archiveType == "tar.gz")
            {
                // For tar.gz support, install SharpZipLib or use system tar command
                throw new NotSupportedException($"Archive type {archiveType} is not supported. Please extract manually or use a zip archive.");
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Verify engine installation
    /// </summary>
    public async Task<EngineVerificationResult> VerifyAsync(EngineManifestEntry engine)
    {
        _logger.LogInformation("Verifying engine: {EngineId}", engine.Id);

        string installPath = Path.Combine(_installRoot, engine.Id);
        var missingFiles = new List<string>();
        var issues = new List<string>();

        if (!Directory.Exists(installPath))
        {
            return new EngineVerificationResult(engine.Id, false, "Not installed", new List<string>(), new List<string> { "Installation directory not found" });
        }

        // Check for entrypoint
        string entrypointPath = Path.Combine(installPath, engine.Entrypoint);
        if (!File.Exists(entrypointPath))
        {
            missingFiles.Add(engine.Entrypoint);
            issues.Add($"Entrypoint file not found: {engine.Entrypoint}");
        }

        // Check if directory has any files
        int fileCount = Directory.GetFiles(installPath, "*", SearchOption.AllDirectories).Length;
        if (fileCount == 0)
        {
            issues.Add("Installation directory is empty");
        }

        bool isValid = missingFiles.Count == 0 && issues.Count == 0;
        string status = isValid ? "Valid" : "Invalid";

        return await Task.FromResult(new EngineVerificationResult(engine.Id, isValid, status, missingFiles, issues));
    }

    /// <summary>
    /// Repair engine installation by reinstalling
    /// </summary>
    public async Task RepairAsync(
        EngineManifestEntry engine,
        IProgress<EngineInstallProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Repairing engine: {EngineId}", engine.Id);

        // Remove and reinstall
        await RemoveAsync(engine).ConfigureAwait(false);
        await InstallAsync(engine, progress, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Remove engine installation
    /// </summary>
    public async Task RemoveAsync(EngineManifestEntry engine)
    {
        _logger.LogInformation("Removing engine: {EngineId}", engine.Id);

        string installPath = Path.Combine(_installRoot, engine.Id);
        
        if (Directory.Exists(installPath))
        {
            await Task.Run(() =>
            {
                try
                {
                    Directory.Delete(installPath, true);
                    _logger.LogInformation("Engine {EngineId} removed successfully", engine.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to remove engine {EngineId}", engine.Id);
                    throw;
                }
            }).ConfigureAwait(false);
        }
        else
        {
            _logger.LogWarning("Engine {EngineId} installation directory not found", engine.Id);
        }
    }

    /// <summary>
    /// Get installation path for an engine
    /// </summary>
    public string GetInstallPath(string engineId)
    {
        return Path.Combine(_installRoot, engineId);
    }

    /// <summary>
    /// Check if engine is installed
    /// </summary>
    public bool IsInstalled(string engineId)
    {
        string installPath = GetInstallPath(engineId);
        return Directory.Exists(installPath) && Directory.GetFiles(installPath, "*", SearchOption.AllDirectories).Length > 0;
    }
}
