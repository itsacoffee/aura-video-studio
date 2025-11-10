using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Storage;

/// <summary>
/// Azure Blob Storage implementation for production use
/// </summary>
public class AzureBlobStorageService : IStorageService
{
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _mediaContainer;
    private readonly BlobContainerClient _thumbnailContainer;
    private readonly string _containerName = "media";
    private readonly string _thumbnailContainerName = "thumbnails";
    private readonly ConcurrentDictionary<Guid, string> _uploadSessions = new();

    public AzureBlobStorageService(
        IConfiguration configuration,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var connectionString = configuration["Storage:AzureBlobStorage:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Azure Blob Storage connection string is not configured");
        }

        _blobServiceClient = new BlobServiceClient(connectionString);
        _mediaContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
        _thumbnailContainer = _blobServiceClient.GetBlobContainerClient(_thumbnailContainerName);

        EnsureContainersExistAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureContainersExistAsync()
    {
        await _mediaContainer.CreateIfNotExistsAsync(PublicAccessType.None);
        await _thumbnailContainer.CreateIfNotExistsAsync(PublicAccessType.None);
    }

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        try
        {
            var blobName = $"{Guid.NewGuid()}/{fileName}";
            var blobClient = _mediaContainer.GetBlobClient(blobName);

            var options = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                }
            };

            await blobClient.UploadAsync(fileStream, options, ct);

            _logger.LogInformation("Uploaded file to Azure Blob Storage: {BlobName}", blobName);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to Azure Blob Storage: {FileName}", fileName);
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
            string blobName;
            
            if (!_uploadSessions.TryGetValue(sessionId, out var existingBlobName))
            {
                blobName = $"{sessionId}/{Guid.NewGuid()}";
                _uploadSessions[sessionId] = blobName;
            }
            else
            {
                blobName = existingBlobName;
            }

            var blobClient = _mediaContainer.GetBlockBlobClient(blobName);
            
            // Stage block for eventual commit
            var blockId = Convert.ToBase64String(BitConverter.GetBytes(chunkIndex));
            await blobClient.StageBlockAsync(blockId, chunkStream, cancellationToken: ct);

            _logger.LogDebug("Uploaded chunk {ChunkIndex} for session {SessionId}", chunkIndex, sessionId);

            return blobName;
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
            if (!_uploadSessions.TryGetValue(sessionId, out var blobName))
            {
                throw new InvalidOperationException($"Upload session {sessionId} not found");
            }

            var blobClient = _mediaContainer.GetBlockBlobClient(blobName);
            
            // Get list of staged blocks
            var blockList = await blobClient.GetBlockListAsync(BlockListTypes.Uncommitted, cancellationToken: ct);
            var blockIds = new System.Collections.Generic.List<string>();
            
            foreach (var block in blockList.Value.UncommittedBlocks)
            {
                blockIds.Add(block.Name);
            }

            // Commit all blocks
            await blobClient.CommitBlockListAsync(blockIds, cancellationToken: ct);

            // Clean up session
            _uploadSessions.TryRemove(sessionId, out _);

            _logger.LogInformation("Completed chunked upload for session {SessionId}", sessionId);

            return blobClient.Uri.ToString();
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
            var blobClient = new BlobClient(new Uri(blobUrl));
            var response = await blobClient.DownloadStreamingAsync(cancellationToken: ct);
            
            var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;
            
            return memoryStream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file from Azure Blob Storage: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public async Task DeleteFileAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(blobUrl));
            await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
            
            _logger.LogInformation("Deleted file from Azure Blob Storage: {BlobUrl}", blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from Azure Blob Storage: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public Task<string> GetDownloadUrlAsync(
        string blobUrl,
        TimeSpan expiresIn,
        CancellationToken ct = default)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(blobUrl));
            
            // Generate SAS token
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = blobClient.BlobContainerName,
                    BlobName = blobClient.Name,
                    Resource = "b",
                    ExpiresOn = DateTimeOffset.UtcNow.Add(expiresIn)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return Task.FromResult(sasUri.ToString());
            }

            // If can't generate SAS, return original URL
            return Task.FromResult(blobUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate download URL for: {BlobUrl}", blobUrl);
            return Task.FromResult(blobUrl);
        }
    }

    public async Task<long> GetFileSizeAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(blobUrl));
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);
            return properties.Value.ContentLength;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file size: {BlobUrl}", blobUrl);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string blobUrl, CancellationToken ct = default)
    {
        try
        {
            var blobClient = new BlobClient(new Uri(blobUrl));
            return await blobClient.ExistsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check file existence: {BlobUrl}", blobUrl);
            return false;
        }
    }

    public async Task<string> CopyFileAsync(
        string sourceBlobUrl,
        string destinationFileName,
        CancellationToken ct = default)
    {
        try
        {
            var sourceBlobClient = new BlobClient(new Uri(sourceBlobUrl));
            var destBlobName = $"{Guid.NewGuid()}/{destinationFileName}";
            var destBlobClient = _mediaContainer.GetBlobClient(destBlobName);

            var copyOperation = await destBlobClient.StartCopyFromUriAsync(sourceBlobClient.Uri, cancellationToken: ct);
            await copyOperation.WaitForCompletionAsync(ct);

            _logger.LogInformation("Copied file from {Source} to {Destination}", sourceBlobUrl, destBlobName);

            return destBlobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file from {Source}", sourceBlobUrl);
            throw;
        }
    }
}
