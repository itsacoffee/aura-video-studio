using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    public bool Validated { get; set; }
    public string? ValidationOutput { get; set; }
}

/// <summary>
/// Robust FFmpeg installer with mirror fallback, validation, and metadata tracking
/// </summary>
public class FfmpegInstaller
{
    private readonly ILogger<FfmpegInstaller> _logger;
    private readonly HttpDownloader _downloader;
    private readonly string _toolsDirectory;
    
    public FfmpegInstaller(
        ILogger<FfmpegInstaller> logger,
        HttpDownloader downloader,
        string? toolsDirectory = null)
    {
        _logger = logger;
        _downloader = downloader;
        _toolsDirectory = toolsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura", "Tools");
        
        Directory.CreateDirectory(_toolsDirectory);
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
            
            bool downloadSuccess = await _downloader.DownloadFileAsync(
                mirrors,
                archivePath,
                expectedSha256,
                progress,
                ct);
            
            if (!downloadSuccess)
            {
                return new FfmpegInstallResult
                {
                    Success = false,
                    ErrorMessage = "Failed to download FFmpeg from any mirror",
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
                ct);
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
                ct);
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
            ct);
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
        var validationResult = await ValidateFfmpegBinaryAsync(resolvedFfmpegPath, ct);
        
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
        
        // Write metadata
        var metadata = new FfmpegInstallMetadata
        {
            Id = "ffmpeg",
            Version = "external",
            InstallPath = installDir,
            FfmpegPath = resolvedFfmpegPath,
            FfprobePath = ffprobePath,
            SourceType = "AttachExisting",
            InstalledAt = DateTime.UtcNow,
            Validated = true,
            ValidationOutput = validationResult.output
        };
        
        var metadataPath = Path.Combine(installDir, "install.json");
        await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        }), ct);
        
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
            var validationResult = await ValidateFfmpegBinaryAsync(ffmpegPath, ct);
            
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
                var ffprobeValidation = await ValidateFfmpegBinaryAsync(ffprobePath, ct);
                if (!ffprobeValidation.success)
                {
                    _logger.LogWarning("ffprobe validation failed: {Error}", ffprobeValidation.error);
                    ffprobePath = null; // Don't fail, just warn
                }
            }
            
            // Write install metadata
            var metadata = new FfmpegInstallMetadata
            {
                Id = "ffmpeg",
                Version = version,
                InstallPath = installDir,
                FfmpegPath = ffmpegPath,
                FfprobePath = ffprobePath,
                SourceUrl = sourceUrl,
                SourceType = sourceType.ToString(),
                Sha256 = sha256,
                InstalledAt = DateTime.UtcNow,
                Validated = true,
                ValidationOutput = validationResult.output
            };
            
            var metadataPath = Path.Combine(installDir, "install.json");
            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            }), ct);
            
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
    /// Validate FFmpeg binary by running -version
    /// </summary>
    private async Task<(bool success, string? output, string? error)> ValidateFfmpegBinaryAsync(
        string ffmpegPath,
        CancellationToken ct)
    {
        _logger.LogInformation("Validating FFmpeg binary: {Path}", ffmpegPath);
        
        try
        {
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
            
            await process.WaitForExitAsync(ct);
            
            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            
            if (process.ExitCode != 0)
            {
                return (false, null, $"FFmpeg exited with code {process.ExitCode}: {stderr}");
            }
            
            // Check output contains "ffmpeg version"
            if (string.IsNullOrEmpty(stdout) || !stdout.Contains("ffmpeg version", StringComparison.OrdinalIgnoreCase))
            {
                return (false, stdout, "FFmpeg output does not contain version information");
            }
            
            _logger.LogInformation("FFmpeg validation successful");
            return (true, stdout, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FFmpeg validation failed");
            return (false, null, ex.Message);
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
            var json = await File.ReadAllTextAsync(metadataPath);
            return JsonSerializer.Deserialize<FfmpegInstallMetadata>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read install metadata from {Path}", metadataPath);
            return null;
        }
    }
}
