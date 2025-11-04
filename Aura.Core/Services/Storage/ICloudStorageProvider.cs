using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services.Storage;

/// <summary>
/// Upload progress information
/// </summary>
public record UploadProgress
{
    public long BytesTransferred { get; init; }
    public long TotalBytes { get; init; }
    public double PercentComplete => TotalBytes > 0 ? (double)BytesTransferred / TotalBytes * 100.0 : 0;
    public TimeSpan Elapsed { get; init; }
    public TimeSpan? EstimatedTimeRemaining { get; init; }
}

/// <summary>
/// Result of cloud storage upload operation
/// </summary>
public record CloudUploadResult
{
    public bool Success { get; init; }
    public string Url { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Cloud storage provider configuration
/// </summary>
public record CloudStorageConfig
{
    public required string ProviderName { get; init; }
    public required string BucketName { get; init; }
    public required string Region { get; init; }
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public string? ConnectionString { get; init; }
    public bool UsePublicUrls { get; init; } = true;
    public TimeSpan? UrlExpirationTime { get; init; }
    public Dictionary<string, string> CustomSettings { get; init; } = new();
}

/// <summary>
/// Interface for cloud storage providers (AWS S3, Azure Blob, Google Cloud Storage)
/// </summary>
public interface ICloudStorageProvider
{
    /// <summary>
    /// Provider name (e.g., "AWS S3", "Azure Blob Storage", "Google Cloud Storage")
    /// </summary>
    string ProviderName { get; }
    
    /// <summary>
    /// Check if the provider is properly configured and accessible
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upload a file to cloud storage
    /// </summary>
    /// <param name="filePath">Local file path to upload</param>
    /// <param name="destinationKey">Remote key/path for the file</param>
    /// <param name="progress">Progress reporting callback</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with URL and metadata</returns>
    Task<CloudUploadResult> UploadFileAsync(
        string filePath,
        string destinationKey,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Upload a stream to cloud storage
    /// </summary>
    Task<CloudUploadResult> UploadStreamAsync(
        Stream stream,
        string destinationKey,
        string contentType,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generate a pre-signed URL for sharing
    /// </summary>
    Task<string> GenerateShareableLinkAsync(
        string key,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Delete a file from cloud storage
    /// </summary>
    Task<bool> DeleteFileAsync(
        string key,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// List files in a folder/prefix
    /// </summary>
    Task<List<string>> ListFilesAsync(
        string prefix = "",
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if a file exists
    /// </summary>
    Task<bool> FileExistsAsync(
        string key,
        CancellationToken cancellationToken = default);
}
