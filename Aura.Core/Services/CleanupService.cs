using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for cleaning up temporary files, orphaned artifacts, and proxy media
/// </summary>
public class CleanupService
{
    private readonly ILogger<CleanupService> _logger;
    private readonly string _tempBasePath;
    private readonly string _proxyBasePath;

    public CleanupService(ILogger<CleanupService> logger)
    {
        _logger = logger;
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _tempBasePath = Path.Combine(localAppData, "Aura", "temp");
        _proxyBasePath = Path.Combine(localAppData, "Aura", "proxy");
        
        // Ensure directories exist
        Directory.CreateDirectory(_tempBasePath);
        Directory.CreateDirectory(_proxyBasePath);
    }

    /// <summary>
    /// Cleans up temporary files for a specific job
    /// </summary>
    /// <param name="jobId">The job ID to clean up</param>
    public void CleanupJobTemp(string jobId)
    {
        try
        {
            var jobTempPath = Path.Combine(_tempBasePath, jobId);
            if (Directory.Exists(jobTempPath))
            {
                _logger.LogInformation("Cleaning up temporary files for job {JobId}", jobId);
                Directory.Delete(jobTempPath, recursive: true);
                _logger.LogInformation("Successfully cleaned up temporary files for job {JobId}", jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up temporary files for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Cleans up proxy media files for a specific job
    /// </summary>
    /// <param name="jobId">The job ID to clean up</param>
    public void CleanupJobProxies(string jobId)
    {
        try
        {
            var jobProxyPath = Path.Combine(_proxyBasePath, jobId);
            if (Directory.Exists(jobProxyPath))
            {
                _logger.LogInformation("Cleaning up proxy media for job {JobId}", jobId);
                Directory.Delete(jobProxyPath, recursive: true);
                _logger.LogInformation("Successfully cleaned up proxy media for job {JobId}", jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up proxy media for job {JobId}", jobId);
        }
    }

    /// <summary>
    /// Cleans up all temporary files and proxies for a job
    /// </summary>
    /// <param name="jobId">The job ID to clean up</param>
    public void CleanupJob(string jobId)
    {
        CleanupJobTemp(jobId);
        CleanupJobProxies(jobId);
    }

    /// <summary>
    /// Sweeps for orphaned temporary files older than the specified age
    /// </summary>
    /// <param name="maxAgeHours">Maximum age in hours (default 24)</param>
    /// <returns>Number of directories cleaned up</returns>
    public int SweepOrphanedTemp(int maxAgeHours = 24)
    {
        return SweepOrphanedFiles(_tempBasePath, maxAgeHours, "temporary");
    }

    /// <summary>
    /// Sweeps for orphaned proxy files older than the specified age
    /// </summary>
    /// <param name="maxAgeHours">Maximum age in hours (default 48)</param>
    /// <returns>Number of directories cleaned up</returns>
    public int SweepOrphanedProxies(int maxAgeHours = 48)
    {
        return SweepOrphanedFiles(_proxyBasePath, maxAgeHours, "proxy");
    }

    /// <summary>
    /// Performs a full sweep of all orphaned artifacts
    /// </summary>
    /// <returns>Total number of directories cleaned up</returns>
    public int SweepAllOrphaned()
    {
        var tempCount = SweepOrphanedTemp();
        var proxyCount = SweepOrphanedProxies();
        var total = tempCount + proxyCount;
        
        _logger.LogInformation("Sweep completed: {TempCount} temp directories, {ProxyCount} proxy directories, {Total} total",
            tempCount, proxyCount, total);
        
        return total;
    }

    private int SweepOrphanedFiles(string basePath, int maxAgeHours, string fileType)
    {
        var cleanedCount = 0;
        
        try
        {
            if (!Directory.Exists(basePath))
            {
                return 0;
            }

            var cutoffTime = DateTime.UtcNow.AddHours(-maxAgeHours);
            var directories = Directory.GetDirectories(basePath);

            _logger.LogInformation("Sweeping {FileType} files older than {MaxAgeHours} hours from {BasePath}",
                fileType, maxAgeHours, basePath);

            foreach (var dir in directories)
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    
                    // Check if directory is older than cutoff time
                    if (dirInfo.LastWriteTimeUtc < cutoffTime)
                    {
                        _logger.LogDebug("Deleting orphaned {FileType} directory: {Dir} (last write: {LastWrite})",
                            fileType, dir, dirInfo.LastWriteTimeUtc);
                        
                        Directory.Delete(dir, recursive: true);
                        cleanedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete orphaned {FileType} directory: {Dir}",
                        fileType, dir);
                }
            }

            if (cleanedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} orphaned {FileType} directories", cleanedCount, fileType);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {FileType} sweep from {BasePath}", fileType, basePath);
        }

        return cleanedCount;
    }

    /// <summary>
    /// Gets statistics about temporary and proxy storage
    /// </summary>
    public (long tempSizeBytes, long proxySizeBytes, int tempDirCount, int proxyDirCount) GetStorageStats()
    {
        long tempSize = 0;
        long proxySize = 0;
        int tempDirCount = 0;
        int proxyDirCount = 0;

        try
        {
            if (Directory.Exists(_tempBasePath))
            {
                var tempDirs = Directory.GetDirectories(_tempBasePath);
                tempDirCount = tempDirs.Length;
                
                foreach (var dir in tempDirs)
                {
                    tempSize += GetDirectorySize(dir);
                }
            }

            if (Directory.Exists(_proxyBasePath))
            {
                var proxyDirs = Directory.GetDirectories(_proxyBasePath);
                proxyDirCount = proxyDirs.Length;
                
                foreach (var dir in proxyDirs)
                {
                    proxySize += GetDirectorySize(dir);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get storage statistics");
        }

        return (tempSize, proxySize, tempDirCount, proxyDirCount);
    }

    private static long GetDirectorySize(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            return dirInfo.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }
        catch
        {
            return 0;
        }
    }
}
