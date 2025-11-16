using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Resources;

/// <summary>
/// Background service that periodically cleans up temporary files and directories
/// </summary>
public class TemporaryFileCleanupService
{
    private readonly ILogger<TemporaryFileCleanupService> _logger;
    private readonly ProviderSettings _providerSettings;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _fileRetentionPeriod = TimeSpan.FromHours(24);
    private readonly HashSet<string> _knownTempDirectories;
    private readonly string _defaultTempDirectory;

    public TemporaryFileCleanupService(
        ILogger<TemporaryFileCleanupService> logger,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _providerSettings = providerSettings;
        _knownTempDirectories = new HashSet<string>();
        _defaultTempDirectory = AuraEnvironmentPaths.ResolveTempPath(Path.Combine(AuraEnvironmentPaths.ResolveDataRoot(null), "Temp"));
    }

    /// <summary>
    /// Starts the cleanup service in the background
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting temporary file cleanup service");
        
        // Run cleanup loop in background
        _ = Task.Run(async () => await CleanupLoopAsync(cancellationToken).ConfigureAwait(false), cancellationToken);
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Main cleanup loop that runs periodically
    /// </summary>
    private async Task CleanupLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Temporary file cleanup service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during temporary file cleanup");
            }

            try
            {
                await Task.Delay(_cleanupInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    /// <summary>
    /// Performs a single cleanup pass
    /// </summary>
    public async Task CleanupAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting temporary file cleanup pass");
        var startTime = DateTime.UtcNow;
        var cleanedFiles = 0;
        var cleanedDirs = 0;
        var freedBytes = 0L;

        try
        {
            // Cleanup standard temp directory
            var tempDir = _defaultTempDirectory;

            if (Directory.Exists(tempDir))
            {
                var (files, dirs, bytes) = await CleanupDirectoryAsync(tempDir, cancellationToken).ConfigureAwait(false);
                cleanedFiles += files;
                cleanedDirs += dirs;
                freedBytes += bytes;
            }

            // Cleanup any registered temp directories
            foreach (var dir in _knownTempDirectories.ToList())
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (Directory.Exists(dir))
                {
                    var (files, dirs, bytes) = await CleanupDirectoryAsync(dir, cancellationToken).ConfigureAwait(false);
                    cleanedFiles += files;
                    cleanedDirs += dirs;
                    freedBytes += bytes;
                }
                else
                {
                    // Remove from known directories if it no longer exists
                    _knownTempDirectories.Remove(dir);
                }
            }

            // Cleanup orphaned render outputs
            await CleanupOrphanedOutputsAsync(cancellationToken).ConfigureAwait(false);

            var duration = DateTime.UtcNow - startTime;
            _logger.LogInformation(
                "Temporary file cleanup completed: {Files} files, {Dirs} directories, {SizeMB:F2} MB freed in {Duration:F1}s",
                cleanedFiles, cleanedDirs, freedBytes / (1024.0 * 1024.0), duration.TotalSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during temporary file cleanup");
        }
    }

    /// <summary>
    /// Cleans up old files and directories in the specified directory
    /// </summary>
    private async Task<(int files, int dirs, long bytes)> CleanupDirectoryAsync(string directory, CancellationToken cancellationToken)
    {
        var cleanedFiles = 0;
        var cleanedDirs = 0;
        var freedBytes = 0L;

        try
        {
            var cutoffTime = DateTime.UtcNow - _fileRetentionPeriod;

            // Get all files in the directory and subdirectories
            var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var fileInfo = new FileInfo(file);
                    
                    // Skip files that are still recent
                    if (fileInfo.LastAccessTimeUtc > cutoffTime)
                        continue;

                    // Skip files that are currently locked (in use)
                    if (IsFileLocked(file))
                    {
                        _logger.LogDebug("Skipping locked file: {File}", file);
                        continue;
                    }

                    var size = fileInfo.Length;
                    File.Delete(file);
                    freedBytes += size;
                    cleanedFiles++;
                    
                    _logger.LogDebug("Deleted temporary file: {File} ({Size} bytes)", file, size);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary file: {File}", file);
                }
            }

            // Clean up empty directories
            var dirs = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length); // Process deepest directories first

            foreach (var dir in dirs)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    
                    // Only delete if empty
                    if (!dirInfo.EnumerateFileSystemInfos().Any())
                    {
                        Directory.Delete(dir);
                        cleanedDirs++;
                        _logger.LogDebug("Deleted empty directory: {Dir}", dir);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete empty directory: {Dir}", dir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up directory: {Directory}", directory);
        }

        // Use Task.Yield to make this method truly async
        await Task.Yield();
        
        return (cleanedFiles, cleanedDirs, freedBytes);
    }

    /// <summary>
    /// Cleans up orphaned output files (renders that didn't complete)
    /// </summary>
    private async Task CleanupOrphanedOutputsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var outputDir = _providerSettings.GetOutputDirectory();
            
            if (!Directory.Exists(outputDir))
                return;

            var cutoffTime = DateTime.UtcNow - TimeSpan.FromDays(7); // Keep for 7 days

            var files = Directory.GetFiles(outputDir, "*.mp4", SearchOption.TopDirectoryOnly)
                .Concat(Directory.GetFiles(outputDir, "*.avi", SearchOption.TopDirectoryOnly))
                .Concat(Directory.GetFiles(outputDir, "*.mov", SearchOption.TopDirectoryOnly));

            foreach (var file in files)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var fileInfo = new FileInfo(file);
                    
                    // Check if file is old and appears incomplete (very small size)
                    if (fileInfo.LastWriteTimeUtc < cutoffTime && fileInfo.Length < 1024)
                    {
                        _logger.LogInformation("Deleting orphaned output file: {File} ({Size} bytes)", file, fileInfo.Length);
                        File.Delete(file);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete orphaned output file: {File}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned outputs");
        }

        await Task.Yield();
    }

    /// <summary>
    /// Checks if a file is currently locked (in use by another process)
    /// </summary>
    private bool IsFileLocked(string filePath)
    {
        try
        {
            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Registers a directory for cleanup
    /// </summary>
    public void RegisterTempDirectory(string directory)
    {
        if (!string.IsNullOrEmpty(directory))
        {
            _knownTempDirectories.Add(directory);
            _logger.LogDebug("Registered temporary directory for cleanup: {Directory}", directory);
        }
    }

    /// <summary>
    /// Unregisters a directory from cleanup
    /// </summary>
    public void UnregisterTempDirectory(string directory)
    {
        if (!string.IsNullOrEmpty(directory))
        {
            _knownTempDirectories.Remove(directory);
            _logger.LogDebug("Unregistered temporary directory from cleanup: {Directory}", directory);
        }
    }

    /// <summary>
    /// Immediately cleans up a specific file if it's eligible
    /// </summary>
    public async Task CleanupFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (File.Exists(filePath) && !IsFileLocked(filePath))
            {
                var size = new FileInfo(filePath).Length;
                File.Delete(filePath);
                _logger.LogDebug("Cleaned up file: {File} ({Size} bytes)", filePath, size);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup file: {File}", filePath);
        }

        await Task.Yield();
    }

    /// <summary>
    /// Gets statistics about temporary files
    /// </summary>
    public async Task<TemporaryFileStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new TemporaryFileStats();

        try
        {
            var tempDir = _defaultTempDirectory;

            if (Directory.Exists(tempDir))
            {
                var files = Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories);
                stats.TotalFiles = files.Length;
                stats.TotalBytes = files.Sum(f => new FileInfo(f).Length);
            }

            foreach (var dir in _knownTempDirectories)
            {
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                    stats.TotalFiles += files.Length;
                    stats.TotalBytes += files.Sum(f => new FileInfo(f).Length);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting temporary file statistics");
        }

        await Task.Yield();
        return stats;
    }
}

/// <summary>
/// Statistics about temporary files
/// </summary>
public class TemporaryFileStats
{
    public int TotalFiles { get; set; }
    public long TotalBytes { get; set; }
    public double TotalMegabytes => TotalBytes / (1024.0 * 1024.0);
}
