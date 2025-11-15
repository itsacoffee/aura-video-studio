using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Aura.Core.Downloads;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Dependencies;

/// <summary>
/// Install source type for FFmpeg
/// </summary>
public enum InstallSourceType
{
    Network,
    LocalArchive,
    AttachExisting
}

/// <summary>
/// Installation result with metadata
/// </summary>
public class FfmpegInstallResult
{
    public bool Success { get; set; }
    public string? InstallPath { get; set; }
    public string? FfmpegPath { get; set; }
    public string? FfprobePath { get; set; }
    public string? ValidationOutput { get; set; }
    public string? ErrorMessage { get; set; }
    public InstallSourceType SourceType { get; set; }
    public string? SourceUrl { get; set; }
    public string? Sha256 { get; set; }
    public DateTime InstalledAt { get; set; }
}

/// <summary>
/// Metadata stored in install.json
/// </summary>
public class FfmpegInstallMetadata
{
    public string Id { get; set; } = "ffmpeg";
    public string Version { get; set; } = "";
    public string InstallPath { get; set; } = "";
    public string FfmpegPath { get; set; } = "";
    public string? FfprobePath { get; set; }
    public string? SourceUrl { get; set; }
    public string SourceType { get; set; } = "";
    public string? Sha256 { get; set; }
    public DateTime InstalledAt { get; set; }
    public DateTime ValidatedAt { get; set; }
    public bool Validated { get; set; }
    public string? ValidationOutput { get; set; }
    public string? InstallLogPath { get; set; }
}

/// <summary>
/// Robust FFmpeg installer with mirror fallback, validation, and metadata tracking
/// </summary>
public class FfmpegInstaller
{
    private readonly ILogger<FfmpegInstaller> _logger;
    private readonly HttpDownloader _downloader;
    private readonly GitHubReleaseResolver? _releaseResolver;
    private readonly string _toolsDirectory;
    
    public FfmpegInstaller(
        ILogger<FfmpegInstaller> logger,
        HttpDownloader downloader,
        string? toolsDirectory = null,
        GitHubReleaseResolver? releaseResolver = null)
    {
        _logger = logger;
        _downloader = downloader;
        _releaseResolver = releaseResolver;
        _toolsDirectory = toolsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura", "Tools");
        
        Directory.CreateDirectory(_toolsDirectory);
    }
    
    /// <summary>
    /// Resolve download mirrors dynamically via GitHub Releases API
    /// </summary>
    /// <param name="githubRepo">Repository in format "owner/repo"</param>
    /// <param name="assetPattern">Wildcard pattern to match assets</param>
    /// <param name="fallbackMirrors">Static fallback mirrors if API fails</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of resolved mirror URLs</returns>
    public async Task<List<string>> ResolveMirrorsAsync(
        string? githubRepo,
        string? assetPattern,
        string[] fallbackMirrors,
        CancellationToken ct = default)
    {
        var mirrors = new List<string>();
        
        // Try GitHub API resolution first
        if (_releaseResolver != null && !string.IsNullOrEmpty(githubRepo) && !string.IsNullOrEmpty(assetPattern))
        {
            try
            {
                _logger.LogInformation("Attempting to resolve FFmpeg asset URL via GitHub API: {Repo}", githubRepo);
                var resolvedUrl = await _releaseResolver.ResolveLatestAssetUrlAsync(githubRepo, assetPattern, ct).ConfigureAwait(false);
                
                if (!string.IsNullOrEmpty(resolvedUrl))
                {
                    _logger.LogInformation("Resolved asset URL via GitHub API: {Url}", resolvedUrl);
                    mirrors.Add(resolvedUrl);
                }
                else
                {
                    _logger.LogWarning("GitHub API resolution returned no matching assets for {Pattern}", assetPattern);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve FFmpeg URL via GitHub API, will use fallback mirrors");
            }
        }
        
        // Add fallback mirrors
        if (fallbackMirrors != null && fallbackMirrors.Length > 0)
        {
            mirrors.AddRange(fallbackMirrors);
        }
        
        // Log all mirrors
        _logger.LogInformation("Total {Count} mirrors available for download", mirrors.Count);
        for (int i = 0; i < mirrors.Count; i++)
        {
            _logger.LogDebug("Mirror {Index}: {Url}", i + 1, mirrors[i]);
        }
        
        return mirrors;
    }
    
    /// <summary>
    /// Install FFmpeg from network mirrors
    /// </summary>
    public async Task<FfmpegInstallResult> InstallFromMirrorsAsync(
        string[] mirrors,
        string version,
        string? expectedSha256 = null,
        IProgress<HttpDownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Installing FFmpeg {Version} from mirrors", version);
        
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraFFmpegInstall_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Download to temp directory
            var archivePath = Path.Combine(tempDir, "ffmpeg.zip");
            
            progress?.Report(new HttpDownloadProgress(0, 0, 0, 0, "Downloading FFmpeg..."));
            
            try
            {
                bool downloadSuccess = await _downloader.DownloadFileAsync(
                    mirrors,
                    archivePath,
                    expectedSha256,
                    progress,
                    ct).ConfigureAwait(false);
                
                if (!downloadSuccess)
                {
                    return new FfmpegInstallResult
                    {
                        Success = false,
                        ErrorMessage = "Failed to download FFmpeg from any mirror",
                        SourceType = InstallSourceType.Network
                    };
                }
            }
            catch (DownloadException dex)
            {
                _logger.LogError(dex, "Download failed with error code: {ErrorCode}", dex.ErrorCode);
                return new FfmpegInstallResult
                {
                    Success = false,
                    ErrorMessage = $"Download failed: {dex.Message} (Error: {dex.ErrorCode})",
                    SourceType = InstallSourceType.Network
                };
            }
            catch (HttpRequestException hex)
            {
                _logger.LogError(hex, "HTTP request failed during download");
                return new FfmpegInstallResult
                {
                    Success = false,
                    ErrorMessage = $"Network error during download: {hex.Message}",
                    SourceType = InstallSourceType.Network
                };
            }
            
            // Extract and install
            return await ExtractAndValidateAsync(
                archivePath,
                version,
                InstallSourceType.Network,
                mirrors[0],
                expectedSha256,
                progress,
                ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Installation cancelled by user");
            return new FfmpegInstallResult
            {
                Success = false,
                ErrorMessage = "Installation cancelled by user",
                SourceType = InstallSourceType.Network
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during FFmpeg installation");
            return new FfmpegInstallResult
            {
                Success = false,
                ErrorMessage = $"Installation failed: {ex.Message}",
                SourceType = InstallSourceType.Network
            };
        }
        finally
        {
            // Cleanup temp directory
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup temp directory: {TempDir}", tempDir);
            }
        }
    }
    
    /// <summary>
    /// Install FFmpeg from a local archive file
    /// </summary>
    public async Task<FfmpegInstallResult> InstallFromLocalArchiveAsync(
        string localArchivePath,
        string version,
        string? expectedSha256 = null,
        IProgress<HttpDownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Installing FFmpeg {Version} from local archive: {Path}", version, localArchivePath);
        
        if (!File.Exists(localArchivePath))
        {
            return new FfmpegInstallResult
            {
                Success = false,
                ErrorMessage = $"Local archive not found: {localArchivePath}",
                SourceType = InstallSourceType.LocalArchive
            };
        }
        
        progress?.Report(new HttpDownloadProgress(0, 0, 0, 0, "Importing local archive..."));
        
        // Verify checksum if provided (warning only for local files)
        string? actualSha256 = null;
        if (!string.IsNullOrEmpty(expectedSha256))
        {
            var importResult = await _downloader.ImportLocalFileAsync(
                localArchivePath,
                localArchivePath, // dummy, we just want checksum
                expectedSha256,
                progress,
                ct).ConfigureAwait(false);
            actualSha256 = importResult.actualSha256;
        }
        
        // Extract and install
        return await ExtractAndValidateAsync(
            localArchivePath,
            version,
            InstallSourceType.LocalArchive,
            localArchivePath,
            actualSha256,
            progress,
            ct).ConfigureAwait(false);
    }
    
    /// <summary>
    /// Attach an existing FFmpeg installation
    /// </summary>
    public async Task<FfmpegInstallResult> AttachExistingAsync(
        string ffmpegPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Attaching existing FFmpeg: {Path}", ffmpegPath);
        
        // Resolve path - could be exe or directory
        string resolvedFfmpegPath;
        string installDir;
        
        if (File.Exists(ffmpegPath))
        {
            // Direct path to ffmpeg.exe
            resolvedFfmpegPath = ffmpegPath;
            installDir = Path.GetDirectoryName(ffmpegPath) ?? "";
        }
        else if (Directory.Exists(ffmpegPath))
        {
            // Directory - look for ffmpeg.exe
            var exePath = Path.Combine(ffmpegPath, "ffmpeg.exe");
            if (!File.Exists(exePath))
            {
                // Try bin subdirectory
                exePath = Path.Combine(ffmpegPath, "bin", "ffmpeg.exe");
                if (!File.Exists(exePath))
                {
                    return new FfmpegInstallResult
                    {
                        Success = false,
                        ErrorMessage = $"Could not find ffmpeg.exe in {ffmpegPath}",
                        SourceType = InstallSourceType.AttachExisting
                    };
                }
            }
            resolvedFfmpegPath = exePath;
            installDir = ffmpegPath;
        }
        else
        {
            return new FfmpegInstallResult
            {
                Success = false,
                ErrorMessage = $"Path not found: {ffmpegPath}",
                SourceType = InstallSourceType.AttachExisting
            };
        }
        
        // Look for ffprobe
        var ffprobeDir = Path.GetDirectoryName(resolvedFfmpegPath);
        var ffprobePath = ffprobeDir != null ? Path.Combine(ffprobeDir, "ffprobe.exe") : null;
        if (ffprobePath != null && !File.Exists(ffprobePath))
        {
            ffprobePath = null;
        }
        
        // Validate FFmpeg
        var validationResult = await ValidateFfmpegBinaryAsync(resolvedFfmpegPath, ct).ConfigureAwait(false);
        
        if (!validationResult.success)
        {
            return new FfmpegInstallResult
            {
                Success = false,
                ErrorMessage = $"FFmpeg validation failed: {validationResult.error}",
                SourceType = InstallSourceType.AttachExisting,
                FfmpegPath = resolvedFfmpegPath
            };
        }
        
        // Extract version from validation output
        var detectedVersion = ExtractVersionFromOutput(validationResult.output) ?? "external";
        
        // Write metadata
        var metadata = new FfmpegInstallMetadata
        {
            Id = "ffmpeg",
            Version = detectedVersion,
            InstallPath = installDir,
            FfmpegPath = resolvedFfmpegPath,
            FfprobePath = ffprobePath,
            SourceType = "AttachExisting",
            InstalledAt = DateTime.UtcNow,
            ValidatedAt = DateTime.UtcNow,
            Validated = true,
            ValidationOutput = validationResult.output
        };
        
        var metadataPath = Path.Combine(installDir, "install.json");
        await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        }), ct).ConfigureAwait(false);
        
        _logger.LogInformation("Successfully attached FFmpeg: {Path}", resolvedFfmpegPath);
        
        return new FfmpegInstallResult
        {
            Success = true,
            InstallPath = installDir,
            FfmpegPath = resolvedFfmpegPath,
            FfprobePath = ffprobePath,
            ValidationOutput = validationResult.output,
            SourceType = InstallSourceType.AttachExisting,
            InstalledAt = DateTime.UtcNow
        };
    }
    
    /// <summary>
    /// Extract archive, locate binaries, validate, and write metadata
    /// </summary>
    private async Task<FfmpegInstallResult> ExtractAndValidateAsync(
        string archivePath,
        string version,
        InstallSourceType sourceType,
        string sourceUrl,
        string? sha256,
        IProgress<HttpDownloadProgress>? progress,
        CancellationToken ct)
    {
        progress?.Report(new HttpDownloadProgress(0, 0, 50, 0, "Extracting archive..."));
        
        // Create install directory
        var installDir = Path.Combine(_toolsDirectory, "ffmpeg", version);
        if (Directory.Exists(installDir))
        {
            _logger.LogInformation("Removing existing installation at {Path}", installDir);
            Directory.Delete(installDir, recursive: true);
        }
        Directory.CreateDirectory(installDir);
        
        try
        {
            // Extract zip
            _logger.LogInformation("Extracting {Archive} to {InstallDir}", archivePath, installDir);
            ZipFile.ExtractToDirectory(archivePath, installDir, overwriteFiles: true);
            
            progress?.Report(new HttpDownloadProgress(0, 0, 70, 0, "Locating FFmpeg binaries..."));
            
            // Locate ffmpeg.exe and ffprobe.exe (may be in nested folders)
            var ffmpegPath = FindExecutable(installDir, "ffmpeg.exe");
            var ffprobePath = FindExecutable(installDir, "ffprobe.exe");
            
            if (ffmpegPath == null)
            {
                return new FfmpegInstallResult
                {
                    Success = false,
                    ErrorMessage = "Could not find ffmpeg.exe in extracted archive",
                    SourceType = sourceType,
                    SourceUrl = sourceUrl,
                    InstallPath = installDir
                };
            }
            
            progress?.Report(new HttpDownloadProgress(0, 0, 80, 0, "Validating FFmpeg..."));
            
            // Validate FFmpeg binary
            var validationResult = await ValidateFfmpegBinaryAsync(ffmpegPath, ct).ConfigureAwait(false);
            
            if (!validationResult.success)
            {
                // Cleanup on validation failure
                Directory.Delete(installDir, recursive: true);
                
                return new FfmpegInstallResult
                {
                    Success = false,
                    ErrorMessage = $"FFmpeg validation failed: {validationResult.error}",
                    SourceType = sourceType,
                    SourceUrl = sourceUrl,
                    FfmpegPath = ffmpegPath
                };
            }
            
            // Validate ffprobe if found
            if (ffprobePath != null)
            {
                var ffprobeValidation = await ValidateFfmpegBinaryAsync(ffprobePath, ct).ConfigureAwait(false);
                if (!ffprobeValidation.success)
                {
                    _logger.LogWarning("ffprobe validation failed: {Error}", ffprobeValidation.error);
                    ffprobePath = null; // Don't fail, just warn
                }
            }
            
            // Extract actual version from validation output (prefer detected over manifest version)
            var detectedVersion = ExtractVersionFromOutput(validationResult.output) ?? version;
            
            // Write install metadata
            var metadata = new FfmpegInstallMetadata
            {
                Id = "ffmpeg",
                Version = detectedVersion,
                InstallPath = installDir,
                FfmpegPath = ffmpegPath,
                FfprobePath = ffprobePath,
                SourceUrl = sourceUrl,
                SourceType = sourceType.ToString(),
                Sha256 = sha256,
                InstalledAt = DateTime.UtcNow,
                ValidatedAt = DateTime.UtcNow,
                Validated = true,
                ValidationOutput = validationResult.output
            };
            
            var metadataPath = Path.Combine(installDir, "install.json");
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            }), ct).ConfigureAwait(false);
            
            _logger.LogInformation("FFmpeg installed successfully: {Path}", ffmpegPath);
            
            progress?.Report(new HttpDownloadProgress(0, 0, 100, 0, "Installation complete"));
            
            return new FfmpegInstallResult
            {
                Success = true,
                InstallPath = installDir,
                FfmpegPath = ffmpegPath,
                FfprobePath = ffprobePath,
                ValidationOutput = validationResult.output,
                SourceType = sourceType,
                SourceUrl = sourceUrl,
                Sha256 = sha256,
                InstalledAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract and validate FFmpeg");
            
            // Cleanup on failure
            try
            {
                if (Directory.Exists(installDir))
                {
                    Directory.Delete(installDir, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            return new FfmpegInstallResult
            {
                Success = false,
                ErrorMessage = $"Installation failed: {ex.Message}",
                SourceType = sourceType,
                SourceUrl = sourceUrl
            };
        }
    }
    
    /// <summary>
    /// Find an executable by searching recursively
    /// </summary>
    private string? FindExecutable(string rootDir, string fileName)
    {
        try
        {
            var files = Directory.GetFiles(rootDir, fileName, SearchOption.AllDirectories);
            return files.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to search for {FileName} in {RootDir}", fileName, rootDir);
            return null;
        }
    }
    
    /// <summary>
    /// Validate FFmpeg binary by running -version and smoke test
    /// </summary>
    private async Task<(bool success, string? output, string? error)> ValidateFfmpegBinaryAsync(
        string ffmpegPath,
        CancellationToken ct)
    {
        _logger.LogInformation("Validating FFmpeg binary: {Path}", ffmpegPath);
        
        try
        {
            // First check: -version
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(psi);
            if (process == null)
            {
                return (false, null, "Failed to start FFmpeg process");
            }
            
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            
            var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            
            if (process.ExitCode != 0)
            {
                return (false, null, $"FFmpeg exited with code {process.ExitCode}: {stderr}");
            }
            
            // Check output contains "ffmpeg version"
            if (string.IsNullOrEmpty(stdout) || !stdout.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                return (false, stdout, "FFmpeg output does not contain version information");
            }
            
            _logger.LogInformation("FFmpeg version check passed");
            
            // Second check: smoke test (generate short silent audio)
            var smokeTestResult = await RunSmokeTestAsync(ffmpegPath, ct).ConfigureAwait(false);
            if (!smokeTestResult.success)
            {
                _logger.LogWarning("FFmpeg smoke test failed: {Error}", smokeTestResult.error);
                return (false, stdout, $"Smoke test failed: {smokeTestResult.error}");
            }
            
            _logger.LogInformation("FFmpeg validation successful (version + smoke test passed)");
            return (true, stdout, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg validation failed");
            return (false, null, ex.Message);
        }
    }
    
    /// <summary>
    /// Run a smoke test to ensure FFmpeg can actually process media
    /// </summary>
    public async Task<(bool success, string? output, string? error)> RunSmokeTestAsync(
        string ffmpegPath,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Running FFmpeg smoke test: {Path}", ffmpegPath);
        
        var tempOut = Path.Combine(Path.GetTempPath(), $"ffmpeg_smoke_test_{Guid.NewGuid():N}.wav");
        
        try
        {
            // Generate 0.2 seconds of silent stereo audio at 48kHz
            var psi = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-hide_banner -loglevel error -f lavfi -i anullsrc=cl=stereo:r=48000 -t 0.2 -y \"{tempOut}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(psi);
            if (process == null)
            {
                return (false, null, "Failed to start FFmpeg process for smoke test");
            }
            
            // Use shorter timeout for smoke test (10 seconds)
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            
            await process.WaitForExitAsync(linkedCts.Token).ConfigureAwait(false);
            
            var stdout = await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false);
            var stderr = await process.StandardError.ReadToEndAsync(ct).ConfigureAwait(false);
            
            if (process.ExitCode != 0)
            {
                _logger.LogWarning("FFmpeg smoke test failed with exit code {ExitCode}: {Error}", 
                    process.ExitCode, stderr);
                return (false, null, $"Exit code {process.ExitCode}: {stderr}");
            }
            
            // Verify output file was created and has reasonable size
            if (!File.Exists(tempOut))
            {
                return (false, null, "Smoke test output file was not created");
            }
            
            var fileInfo = new FileInfo(tempOut);
            if (fileInfo.Length < 100)
            {
                return (false, null, $"Smoke test output file too small ({fileInfo.Length} bytes)");
            }
            
            _logger.LogInformation("FFmpeg smoke test passed (generated {Size} byte WAV)", fileInfo.Length);
            return (true, "Smoke test passed", null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("FFmpeg smoke test timed out");
            return (false, null, "Smoke test timed out after 10 seconds");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg smoke test exception");
            return (false, null, ex.Message);
        }
        finally
        {
            // Cleanup temp file
            try
            {
                if (File.Exists(tempOut))
                {
                    File.Delete(tempOut);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
    
    /// <summary>
    /// Get install metadata from existing installation
    /// </summary>
    public async Task<FfmpegInstallMetadata?> GetInstallMetadataAsync(string installPath)
    {
        var metadataPath = Path.Combine(installPath, "install.json");
        if (!File.Exists(metadataPath))
        {
            return null;
        }
        
        try
        {
            var json = await File.ReadAllTextAsync(metadataPath).ConfigureAwait(false);
            return JsonSerializer.Deserialize<FfmpegInstallMetadata>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read install metadata from {Path}", metadataPath);
            return null;
        }
    }
    
    /// <summary>
    /// Extract version string from ffmpeg -version output
    /// </summary>
    private string? ExtractVersionFromOutput(string? output)
    {
        if (string.IsNullOrEmpty(output))
            return null;

        try
        {
            // First line typically contains: "ffmpeg version N-xxxxx-..." or "ffmpeg version 6.0-..."
            var firstLine = output.Split('\n')[0];
            if (firstLine.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    return parts[2]; // Version string (e.g., "N-111617-gdd5a56c1b5", "6.0", "7.0.1")
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract version string from FFmpeg output");
        }

        return null;
    }
}
