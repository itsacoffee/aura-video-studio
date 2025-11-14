using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Storage;

/// <summary>
/// Local file system storage implementation
/// </summary>
public class LocalStorageService : IStorageService
{
    private readonly ILogger<LocalStorageService> _logger;
    private readonly string _storageRoot;
    private readonly string _mediaPath;
    private readonly string _thumbnailPath;
    private readonly string _tempPath;
    private readonly ConcurrentDictionary<Guid, ChunkUploadSession> _uploadSessions = new();

    private class ChunkUploadSession
    {
        public List<string> ChunkPaths { get; set; } = new();
        public string FinalPath { get; set; } = string.Empty;
    }

    public LocalStorageService(
        IConfiguration configuration,
        ILogger<LocalStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _storageRoot = configuration["Storage:LocalPath"] 
            ?? Path.Combine(userProfile, "AuraVideoStudio", "MediaLibrary");
        
        _mediaPath = Path.Combine(_storageRoot, "Media");
        _thumbnailPath = Path.Combine(_storageRoot, "Thumbnails");
        _tempPath = Path.Combine(_storageRoot, "Temp");

        EnsureDirectoriesExist();
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_mediaPath);
        Directory.CreateDirectory(_thumbnailPath);
        Directory.CreateDirectory(_tempPath);
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        try
        {
            // Generate unique file name to avoid collisions
            var fileId = Guid.NewGuid();
            var extension = Path.GetExtension(fileName);
            var safeFileName = $"{fileId}{extension}";
            var fullPath = Path.Combine(_mediaPath, safeFileName);

            using (var fileStreamOut = File.Create(fullPath))
            {
                await fileStream.CopyToAsync(fileStreamOut, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Uploaded file: {FileName} -> {Path}", fileName, fullPath);
            
            // Return relative path as "blob URL"
            return $"local://media/{safeFileName}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<string> UploadChunkAsync(
        Guid sessionId,
        int chunkIndex,
        Stream chunkStream,
        CancellationToken ct = default)
    {
        try
        {
            if (!_uploadSessions.TryGetValue(sessionId, out var session))
            {
                session = new ChunkUploadSession();
                _uploadSessions[sessionId] = session;
            }

            var chunkPath = Path.Combine(_tempPath, $"{sessionId}_chunk_{chunkIndex}");
            using (var fileStream = File.Create(chunkPath))
            {
                await chunkStream.CopyToAsync(fileStream, ct).ConfigureAwait(false);
            }

            session.ChunkPaths.Add(chunkPath);
            
            _logger.LogDebug("Uploaded chunk {ChunkIndex} for session {SessionId}", chunkIndex, sessionId);
            
            return chunkPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload chunk {ChunkIndex} for session {SessionId}", chunkIndex, sessionId);
            throw;
        }
    }

    public async Task<string> CompleteChunkedUploadAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            if (!_uploadSessions.TryGetValue(sessionId, out var session))
            {
                throw new InvalidOperationException($"Upload session {sessionId} not found");
            }

            // Sort chunks by name to ensure correct order
            var sortedChunks = session.ChunkPaths.OrderBy(p => p).ToList();
            
            // Generate final file name
            var fileId = Guid.NewGuid();
            var finalPath = Path.Combine(_mediaPath, fileId.ToString());
            
            // Combine all chunks into final file
            using (var finalStream = File.Create(finalPath))
            {
                foreach (var chunkPath in sortedChunks)
                {
                    using (var chunkStream = File.OpenRead(chunkPath))
                    {
                        await chunkStream.CopyToAsync(finalStream, ct).ConfigureAwait(false);
                    }
                    
                    // Delete chunk after merging
                    File.Delete(chunkPath);
                }
            }

            // Clean up session
            _uploadSessions.TryRemove(sessionId, out _);

            _logger.LogInformation("Completed chunked upload for session {SessionId} -> {Path}", sessionId, finalPath);

            return $"local://media/{Path.GetFileName(finalPath)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete chunked upload for session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<Stream> DownloadFileAsync(
        string blobUrl,
        CancellationToken ct = default)
    {
        try
        {
            var localPath = ConvertBlobUrlToPath(blobUrl);
            
            if (!File.Exists(localPath))
            {
                throw new FileNotFoundException($"File not found: {blobUrl}");
            }

            var memoryStream = new MemoryStream();
            using (var fileStream = File.OpenRead(localPath))
            {
                await fileStream.CopyToAsync(memoryStream, ct).ConfigureAwait(false);
            }
            
            memoryStream.Position = 0;
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public Task DeleteFileAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var localPath = ConvertBlobUrlToPath(blobUrl);
            
            if (File.Exists(localPath))
            {
                File.Delete(localPath);
                _logger.LogInformation("Deleted file: {BlobUrl}", blobUrl);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public Task<string> GetDownloadUrlAsync(
        string blobUrl,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        // For local storage, just return the blob URL as-is
        // In production with Azure Blob Storage, this would return a SAS URL
        return Task.FromResult(blobUrl);
    }

    public Task<long> GetFileSizeAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var localPath = ConvertBlobUrlToPath(blobUrl);
            var fileInfo = new FileInfo(localPath);
            return Task.FromResult(fileInfo.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file size: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public Task<bool> FileExistsAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var localPath = ConvertBlobUrlToPath(blobUrl);
            return Task.FromResult(File.Exists(localPath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file existence: {BlobUrl}", blobUrl);
            return Task.FromResult(false);
        }
    }

    public async Task<string> CopyFileAsync(
        string sourceBlobUrl,
        string destinationFileName,
        CancellationToken ct = default)
    {
        try
        {
            var sourcePath = ConvertBlobUrlToPath(sourceBlobUrl);
            var destId = Guid.NewGuid();
            var extension = Path.GetExtension(destinationFileName);
            var destPath = Path.Combine(_mediaPath, $"{destId}{extension}");

            File.Copy(sourcePath, destPath);
            
            _logger.LogInformation("Copied file from {Source} to {Destination}", sourceBlobUrl, destPath);

            return $"local://media/{Path.GetFileName(destPath)}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file from {Source}", sourceBlobUrl);
            throw;
        }
    }

    private string ConvertBlobUrlToPath(string blobUrl)
    {
        // Convert local://media/filename to actual file path
        if (blobUrl.StartsWith("local://"))
        {
            var relativePath = blobUrl.Substring("local://".Length);
            var parts = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 2)
            {
                var folder = parts[0]; // "media" or "thumbnails"
                var fileName = string.Join("/", parts.Skip(1));
                
                return folder.ToLower() switch
                {
                    "media" => Path.Combine(_mediaPath, fileName),
                    "thumbnails" => Path.Combine(_thumbnailPath, fileName),
                    _ => Path.Combine(_storageRoot, relativePath)
                };
            }
        }
        
        // If not a local:// URL, treat as absolute path
        return blobUrl;
    }
}
