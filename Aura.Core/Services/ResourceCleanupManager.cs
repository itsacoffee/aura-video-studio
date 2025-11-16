using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Manages cleanup of temporary files and resources during video generation.
/// Ensures atomic file operations and proper resource disposal.
/// </summary>
public class ResourceCleanupManager : IDisposable
{
    private readonly ILogger<ResourceCleanupManager> _logger;
    private readonly ConcurrentBag<string> _tempFiles = new();
    private readonly ConcurrentBag<string> _tempDirectories = new();
    private readonly object _lockObject = new();
    private bool _disposed;
    private readonly string _tempRoot;

    public ResourceCleanupManager(ILogger<ResourceCleanupManager> logger)
    {
        _logger = logger;
        _tempRoot = AuraEnvironmentPaths.ResolveTempPath(Path.Combine(AuraEnvironmentPaths.ResolveDataRoot(null), "Temp"));
    }

    /// <summary>
    /// Registers a temporary file for cleanup
    /// </summary>
    public void RegisterTempFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        _tempFiles.Add(filePath);
        _logger.LogTrace("Registered temp file for cleanup: {Path}", filePath);
    }

    /// <summary>
    /// Registers a temporary directory for cleanup
    /// </summary>
    public void RegisterTempDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
        {
            return;
        }

        _tempDirectories.Add(directoryPath);
        _logger.LogTrace("Registered temp directory for cleanup: {Path}", directoryPath);
    }

    /// <summary>
    /// Unregisters a file from cleanup (e.g., when it becomes a permanent artifact)
    /// </summary>
    public void UnregisterTempFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        // Remove from bag (create new bag without this file)
        var remaining = _tempFiles.Where(f => f != filePath).ToList();
        _tempFiles.Clear();
        foreach (var file in remaining)
        {
            _tempFiles.Add(file);
        }

        _logger.LogTrace("Unregistered temp file from cleanup: {Path}", filePath);
    }

    /// <summary>
    /// Creates a temporary file with atomic write operation
    /// </summary>
    public async Task<string> CreateTempFileAtomicallyAsync(
        string extension,
        Func<string, Task> writeOperation,
        CancellationToken ct = default)
    {
        string? tempPath = null;
        string? finalPath = null;

        try
        {
            // Create temp file with unique name
            var tempDir = _tempRoot;
            var fileName = $"aura_temp_{Guid.NewGuid():N}{extension}";
            tempPath = Path.Combine(tempDir, fileName);
            finalPath = Path.Combine(tempDir, $"aura_{Guid.NewGuid():N}{extension}");

            // Write to temp file
            await writeOperation(tempPath).ConfigureAwait(false);

            ct.ThrowIfCancellationRequested();

            // Atomically move to final location
            File.Move(tempPath, finalPath, overwrite: true);

            // Register for cleanup
            RegisterTempFile(finalPath);

            _logger.LogDebug("Created temp file atomically: {Path}", finalPath);
            return finalPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create temp file atomically");

            // Clean up on error
            if (tempPath != null && File.Exists(tempPath))
            {
                TryDeleteFile(tempPath);
            }
            if (finalPath != null && File.Exists(finalPath))
            {
                TryDeleteFile(finalPath);
            }

            throw;
        }
    }

    /// <summary>
    /// Copies a file to a permanent location and unregisters it from cleanup
    /// </summary>
    public async Task<string> PromoteToArtifactAsync(
        string tempFilePath,
        string destinationPath,
        CancellationToken ct = default)
    {
        if (!File.Exists(tempFilePath))
        {
            throw new FileNotFoundException($"Temp file not found: {tempFilePath}");
        }

        try
        {
            // Ensure destination directory exists
            var destinationDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destinationDir))
            {
                Directory.CreateDirectory(destinationDir);
            }

            // Copy with atomic operation (write to temp, then move)
            var tempDest = destinationPath + ".tmp";
            await CopyFileAsync(tempFilePath, tempDest, ct).ConfigureAwait(false);

            File.Move(tempDest, destinationPath, overwrite: true);

            // Unregister from cleanup
            UnregisterTempFile(tempFilePath);

            _logger.LogInformation("Promoted temp file to artifact: {Source} -> {Destination}",
                tempFilePath, destinationPath);

            return destinationPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to promote temp file to artifact: {Source} -> {Destination}",
                tempFilePath, destinationPath);
            throw;
        }
    }

    /// <summary>
    /// Cleans up all registered temporary resources
    /// </summary>
    public void CleanupAll()
    {
        lock (_lockObject)
        {
            int filesDeleted = 0;
            int dirsDeleted = 0;
            int filesFailed = 0;
            int dirsFailed = 0;

            // Clean up files
            foreach (var filePath in _tempFiles)
            {
                if (TryDeleteFile(filePath))
                {
                    filesDeleted++;
                }
                else
                {
                    filesFailed++;
                }
            }

            // Clean up directories
            foreach (var dirPath in _tempDirectories)
            {
                if (TryDeleteDirectory(dirPath))
                {
                    dirsDeleted++;
                }
                else
                {
                    dirsFailed++;
                }
            }

            _tempFiles.Clear();
            _tempDirectories.Clear();

            _logger.LogInformation(
                "Cleanup complete: {FilesDeleted} files deleted, {DirsDeleted} directories deleted, {FilesFailed} files failed, {DirsFailed} directories failed",
                filesDeleted, dirsDeleted, filesFailed, dirsFailed);
        }
    }

    /// <summary>
    /// Attempts to delete a file, logging but not throwing on error
    /// </summary>
    private bool TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogTrace("Deleted temp file: {Path}", filePath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp file: {Path}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Attempts to delete a directory, logging but not throwing on error
    /// </summary>
    private bool TryDeleteDirectory(string dirPath)
    {
        try
        {
            if (Directory.Exists(dirPath))
            {
                Directory.Delete(dirPath, recursive: true);
                _logger.LogTrace("Deleted temp directory: {Path}", dirPath);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp directory: {Path}", dirPath);
            return false;
        }
    }

    /// <summary>
    /// Async file copy operation
    /// </summary>
    private async Task CopyFileAsync(string source, string destination, CancellationToken ct)
    {
        const int bufferSize = 81920; // 80KB buffer

        using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
        using var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);

        await sourceStream.CopyToAsync(destinationStream, bufferSize, ct).ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        CleanupAll();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
