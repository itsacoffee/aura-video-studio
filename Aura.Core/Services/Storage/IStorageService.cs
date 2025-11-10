using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services.Storage;

/// <summary>
/// Storage service interface for managing media files
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Upload a file to storage
    /// </summary>
    Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    /// <summary>
    /// Upload a chunk of a file
    /// </summary>
    Task<string> UploadChunkAsync(
        Guid sessionId,
        int chunkIndex,
        Stream chunkStream,
        CancellationToken ct = default);

    /// <summary>
    /// Complete a chunked upload
    /// </summary>
    Task<string> CompleteChunkedUploadAsync(
        Guid sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Download a file from storage
    /// </summary>
    Task<Stream> DownloadFileAsync(
        string blobUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a file from storage
    /// </summary>
    Task DeleteFileAsync(
        string blobUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Get a temporary download URL with SAS token
    /// </summary>
    Task<string> GetDownloadUrlAsync(
        string blobUrl,
        TimeSpan expiresIn,
        CancellationToken ct = default);

    /// <summary>
    /// Get file size
    /// </summary>
    Task<long> GetFileSizeAsync(
        string blobUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Check if file exists
    /// </summary>
    Task<bool> FileExistsAsync(
        string blobUrl,
        CancellationToken ct = default);

    /// <summary>
    /// Copy file to a new location
    /// </summary>
    Task<string> CopyFileAsync(
        string sourceBlobUrl,
        string destinationFileName,
        CancellationToken ct = default);
}
