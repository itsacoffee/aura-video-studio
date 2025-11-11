using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Utils;

/// <summary>
/// Windows-specific file system utilities for proper path handling, disk space checks, and file operations
/// </summary>
public class WindowsFileSystemHelper
{
    private readonly ILogger<WindowsFileSystemHelper> _logger;

    public WindowsFileSystemHelper(ILogger<WindowsFileSystemHelper> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get user-specific application data directory
    /// </summary>
    public string GetUserDataDirectory()
    {
        var baseDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        var appDir = Path.Combine(baseDir, "AuraVideoStudio");
        
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
            _logger.LogInformation("Created user data directory: {Path}", appDir);
        }

        return appDir;
    }

    /// <summary>
    /// Get user's videos directory
    /// </summary>
    public string GetUserVideosDirectory()
    {
        var videosPath = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
        
        if (string.IsNullOrEmpty(videosPath))
        {
            videosPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Videos");
        }

        return videosPath;
    }

    /// <summary>
    /// Get default output directory for generated videos
    /// </summary>
    public string GetDefaultOutputDirectory()
    {
        var outputDir = Path.Combine(GetUserVideosDirectory(), "Aura Video Studio");
        
        if (!Directory.Exists(outputDir))
        {
            try
            {
                Directory.CreateDirectory(outputDir);
                _logger.LogInformation("Created default output directory: {Path}", outputDir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create default output directory: {Path}", outputDir);
                
                // Fallback to user profile
                outputDir = Path.Combine(GetUserDataDirectory(), "Output");
                Directory.CreateDirectory(outputDir);
            }
        }

        return outputDir;
    }

    /// <summary>
    /// Get temporary directory for intermediate files
    /// </summary>
    public string GetTempDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "AuraVideoStudio");
        
        if (!Directory.Exists(tempDir))
        {
            Directory.CreateDirectory(tempDir);
        }

        return tempDir;
    }

    /// <summary>
    /// Check if sufficient disk space is available
    /// </summary>
    public bool HasSufficientDiskSpace(string path, long requiredBytes)
    {
        try
        {
            var drive = GetDriveForPath(path);
            if (drive == null)
            {
                _logger.LogWarning("Could not determine drive for path: {Path}", path);
                return true; // Assume sufficient space if we can't check
            }

            var availableSpace = drive.AvailableFreeSpace;
            var hasSufficientSpace = availableSpace >= requiredBytes;

            if (!hasSufficientSpace)
            {
                _logger.LogWarning(
                    "Insufficient disk space on {Drive}. Required: {Required:N0} bytes, Available: {Available:N0} bytes",
                    drive.Name, requiredBytes, availableSpace);
            }

            return hasSufficientSpace;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking disk space for path: {Path}", path);
            return true; // Assume sufficient space on error
        }
    }

    /// <summary>
    /// Get available disk space for a path in bytes
    /// </summary>
    public long GetAvailableDiskSpace(string path)
    {
        try
        {
            var drive = GetDriveForPath(path);
            return drive?.AvailableFreeSpace ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Get the drive info for a given path
    /// </summary>
    private DriveInfo? GetDriveForPath(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var drives = DriveInfo.GetDrives();

            foreach (var drive in drives)
            {
                if (drive.IsReady && fullPath.StartsWith(drive.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return drive;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting drive info for path: {Path}", path);
        }

        return null;
    }

    /// <summary>
    /// Safely delete a file with proper error handling
    /// </summary>
    public bool SafeDeleteFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return true;
            }

            // Remove read-only attribute if set
            var attributes = File.GetAttributes(filePath);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
            }

            File.Delete(filePath);
            _logger.LogDebug("Deleted file: {Path}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Path}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Safely delete a directory recursively with proper error handling
    /// </summary>
    public bool SafeDeleteDirectory(string directoryPath)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return true;
            }

            // Remove read-only attributes from all files
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                try
                {
                    var attributes = File.GetAttributes(file);
                    if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    {
                        File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove read-only attribute from: {Path}", file);
                }
            }

            Directory.Delete(directoryPath, recursive: true);
            _logger.LogDebug("Deleted directory: {Path}", directoryPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete directory: {Path}", directoryPath);
            return false;
        }
    }

    /// <summary>
    /// Sanitize filename to remove invalid characters
    /// </summary>
    public string SanitizeFilename(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return "unnamed";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new StringBuilder(filename.Length);

        foreach (var c in filename)
        {
            if (Array.IndexOf(invalidChars, c) >= 0)
            {
                sanitized.Append('_');
            }
            else
            {
                sanitized.Append(c);
            }
        }

        var result = sanitized.ToString().Trim();
        
        // Ensure filename is not empty after sanitization
        if (string.IsNullOrWhiteSpace(result))
        {
            result = "unnamed";
        }

        // Limit filename length (Windows MAX_PATH is 260, but we'll be conservative)
        if (result.Length > 200)
        {
            result = result.Substring(0, 200);
        }

        return result;
    }

    /// <summary>
    /// Sanitize path to ensure it's valid
    /// </summary>
    public string SanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or empty", nameof(path));
        }

        try
        {
            // Get full path to resolve relative paths and normalize
            var fullPath = Path.GetFullPath(path);
            return fullPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid path: {Path}", path);
            throw new ArgumentException($"Invalid path: {path}", nameof(path), ex);
        }
    }

    /// <summary>
    /// Ensure directory exists, creating it if necessary
    /// </summary>
    public bool EnsureDirectoryExists(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogDebug("Created directory: {Path}", path);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create directory: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Get a unique filename by appending a number if file already exists
    /// </summary>
    public string GetUniqueFilename(string directory, string baseFilename, string extension)
    {
        var filename = $"{baseFilename}{extension}";
        var fullPath = Path.Combine(directory, filename);

        if (!File.Exists(fullPath))
        {
            return fullPath;
        }

        // Append numbers until we find a unique filename
        for (int i = 1; i < 1000; i++)
        {
            filename = $"{baseFilename} ({i}){extension}";
            fullPath = Path.Combine(directory, filename);

            if (!File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // If we still haven't found a unique name, use a GUID
        filename = $"{baseFilename}_{Guid.NewGuid():N}{extension}";
        return Path.Combine(directory, filename);
    }

    /// <summary>
    /// Move file with fallback to copy+delete if move fails (cross-drive scenarios)
    /// </summary>
    public bool MoveFileSafe(string sourcePath, string destinationPath, bool overwrite = false)
    {
        try
        {
            if (overwrite && File.Exists(destinationPath))
            {
                SafeDeleteFile(destinationPath);
            }

            try
            {
                File.Move(sourcePath, destinationPath);
                return true;
            }
            catch (IOException)
            {
                // Move might fail cross-drive, try copy+delete
                _logger.LogDebug("Move failed, attempting copy+delete for {Source} to {Dest}", sourcePath, destinationPath);
                
                File.Copy(sourcePath, destinationPath, overwrite);
                SafeDeleteFile(sourcePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move file from {Source} to {Dest}", sourcePath, destinationPath);
            return false;
        }
    }

    /// <summary>
    /// Check if a path is on a removable drive or network location
    /// </summary>
    public bool IsRemovableOrNetworkPath(string path)
    {
        try
        {
            var drive = GetDriveForPath(path);
            if (drive == null)
            {
                return false;
            }

            return drive.DriveType == DriveType.Removable || 
                   drive.DriveType == DriveType.Network;
        }
        catch
        {
            return false;
        }
    }
}
