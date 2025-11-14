using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models.Media;
using Aura.Core.Services.Storage;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Media;

/// <summary>
/// Service interface for media library operations
/// </summary>
public interface IMediaService
{
    // Media management
    Task<MediaItemResponse?> GetMediaByIdAsync(Guid id, CancellationToken ct = default);
    Task<MediaSearchResponse> SearchMediaAsync(MediaSearchRequest request, CancellationToken ct = default);
    Task<MediaItemResponse> UploadMediaAsync(Stream fileStream, MediaUploadRequest request, CancellationToken ct = default);
    Task<MediaItemResponse> UpdateMediaAsync(Guid id, MediaUploadRequest request, CancellationToken ct = default);
    Task DeleteMediaAsync(Guid id, CancellationToken ct = default);
    Task<List<MediaItemResponse>> BulkOperationAsync(BulkMediaOperationRequest request, CancellationToken ct = default);
    
    // Collections
    Task<List<MediaCollectionResponse>> GetAllCollectionsAsync(CancellationToken ct = default);
    Task<MediaCollectionResponse?> GetCollectionByIdAsync(Guid id, CancellationToken ct = default);
    Task<MediaCollectionResponse> CreateCollectionAsync(MediaCollectionRequest request, CancellationToken ct = default);
    Task<MediaCollectionResponse> UpdateCollectionAsync(Guid id, MediaCollectionRequest request, CancellationToken ct = default);
    Task DeleteCollectionAsync(Guid id, CancellationToken ct = default);
    
    // Tags
    Task<List<string>> GetAllTagsAsync(CancellationToken ct = default);
    
    // Usage tracking
    Task TrackMediaUsageAsync(Guid mediaId, string projectId, string? projectName, CancellationToken ct = default);
    Task<MediaUsageInfo> GetMediaUsageAsync(Guid mediaId, CancellationToken ct = default);
    
    // Statistics
    Task<StorageStats> GetStorageStatsAsync(CancellationToken ct = default);
    
    // Duplicate detection
    Task<DuplicateDetectionResult> CheckForDuplicateAsync(Stream fileStream, CancellationToken ct = default);
    
    // Chunked upload
    Task<UploadSession> InitiateChunkedUploadAsync(string fileName, long totalSize, int totalChunks, CancellationToken ct = default);
    Task UploadChunkAsync(Guid sessionId, int chunkIndex, Stream chunkStream, CancellationToken ct = default);
    Task<MediaItemResponse> CompleteChunkedUploadAsync(Guid sessionId, MediaUploadRequest request, CancellationToken ct = default);
}

/// <summary>
/// Implementation of media service
/// </summary>
public class MediaService : IMediaService
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IStorageService _storageService;
    private readonly IThumbnailGenerationService _thumbnailService;
    private readonly IMediaMetadataService _metadataService;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IMediaRepository mediaRepository,
        IStorageService storageService,
        IThumbnailGenerationService thumbnailService,
        IMediaMetadataService metadataService,
        ILogger<MediaService> logger)
    {
        _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _thumbnailService = thumbnailService ?? throw new ArgumentNullException(nameof(thumbnailService));
        _metadataService = metadataService ?? throw new ArgumentNullException(nameof(metadataService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MediaItemResponse?> GetMediaByIdAsync(Guid id, CancellationToken ct = default)
    {
        var media = await _mediaRepository.GetMediaByIdAsync(id, ct).ConfigureAwait(false);
        return media != null ? MapToResponse(media) : null;
    }

    public async Task<MediaSearchResponse> SearchMediaAsync(MediaSearchRequest request, CancellationToken ct = default)
    {
        var (items, total) = await _mediaRepository.SearchMediaAsync(request, ct).ConfigureAwait(false);
        
        var response = new MediaSearchResponse
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalItems = total,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling(total / (double)request.PageSize)
        };

        return response;
    }

    public async Task<MediaItemResponse> UploadMediaAsync(
        Stream fileStream,
        MediaUploadRequest request,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting media upload: {FileName}", request.FileName);

            // Calculate file size and hash
            var (size, hash) = await CalculateFileSizeAndHashAsync(fileStream, ct).ConfigureAwait(false);
            fileStream.Position = 0;

            // Check for duplicates
            if (!string.IsNullOrEmpty(hash))
            {
                var duplicate = await _mediaRepository.FindByContentHashAsync(hash, ct).ConfigureAwait(false);
                if (duplicate != null)
                {
                    _logger.LogWarning("Duplicate file detected: {FileName}, Hash: {Hash}", request.FileName, hash);
                    // For now, continue with upload; in production, you might want to handle this differently
                }
            }

            // Upload file to storage
            var contentType = GetContentType(request.FileName, request.Type);
            var blobUrl = await _storageService.UploadFileAsync(fileStream, request.FileName, contentType, ct).ConfigureAwait(false);
            fileStream.Position = 0;

            // Generate thumbnail
            string? thumbnailUrl = null;
            if (request.GenerateThumbnail)
            {
                thumbnailUrl = await _thumbnailService.GenerateThumbnailFromStreamAsync(
                    fileStream, request.Type, request.FileName, ct).ConfigureAwait(false);
                fileStream.Position = 0;
            }

            // Extract metadata
            MediaMetadata? metadata = null;
            if (request.ExtractMetadata)
            {
                metadata = await _metadataService.ExtractMetadataFromStreamAsync(
                    fileStream, request.Type, request.FileName, ct).ConfigureAwait(false);
            }

            // Create entity
            var entity = new MediaEntity
            {
                Id = Guid.NewGuid(),
                FileName = request.FileName,
                Type = request.Type.ToString(),
                Source = request.Source.ToString(),
                FileSize = size,
                Description = request.Description,
                BlobUrl = blobUrl,
                ThumbnailUrl = thumbnailUrl,
                ContentHash = hash,
                MetadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                ProcessingStatus = ProcessingStatus.Completed.ToString(),
                CollectionId = request.CollectionId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Save to database
            await _mediaRepository.AddMediaAsync(entity, ct).ConfigureAwait(false);

            // Add tags
            if (request.Tags.Count != 0)
            {
                await _mediaRepository.AddTagsAsync(entity.Id, request.Tags, ct).ConfigureAwait(false);
                entity = await _mediaRepository.GetMediaByIdAsync(entity.Id, ct).ConfigureAwait(false);
            }

            _logger.LogInformation("Media upload completed: {FileName}, ID: {Id}", request.FileName, entity.Id);

            return MapToResponse(entity!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload media: {FileName}", request.FileName);
            throw;
        }
    }

    public async Task<MediaItemResponse> UpdateMediaAsync(
        Guid id,
        MediaUploadRequest request,
        CancellationToken ct = default)
    {
        var media = await _mediaRepository.GetMediaByIdAsync(id, ct).ConfigureAwait(false);
        if (media == null)
        {
            throw new InvalidOperationException($"Media {id} not found");
        }

        media.FileName = request.FileName;
        media.Description = request.Description;
        media.CollectionId = request.CollectionId;
        media.UpdatedAt = DateTime.UtcNow;

        await _mediaRepository.UpdateMediaAsync(media, ct).ConfigureAwait(false);

        // Update tags
        var existingTags = media.Tags.Select(t => t.Tag).ToList();
        var tagsToRemove = existingTags.Except(request.Tags).ToList();
        var tagsToAdd = request.Tags.Except(existingTags).ToList();

        if (tagsToRemove.Count != 0)
        {
            await _mediaRepository.RemoveTagsAsync(id, tagsToRemove, ct).ConfigureAwait(false);
        }

        if (tagsToAdd.Count != 0)
        {
            await _mediaRepository.AddTagsAsync(id, tagsToAdd, ct).ConfigureAwait(false);
        }

        // Reload entity
        media = await _mediaRepository.GetMediaByIdAsync(id, ct).ConfigureAwait(false);
        return MapToResponse(media!);
    }

    public async Task DeleteMediaAsync(Guid id, CancellationToken ct = default)
    {
        var media = await _mediaRepository.GetMediaByIdAsync(id, ct).ConfigureAwait(false);
        if (media == null)
        {
            return;
        }

        // Delete from storage
        try
        {
            await _storageService.DeleteFileAsync(media.BlobUrl, ct).ConfigureAwait(false);
            
            if (!string.IsNullOrEmpty(media.ThumbnailUrl))
            {
                await _storageService.DeleteFileAsync(media.ThumbnailUrl, ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete files from storage for media {Id}", id);
        }

        // Delete from database
        await _mediaRepository.DeleteMediaAsync(id, ct).ConfigureAwait(false);
    }

    public async Task<List<MediaItemResponse>> BulkOperationAsync(
        BulkMediaOperationRequest request,
        CancellationToken ct = default)
    {
        var results = new List<MediaItemResponse>();

        foreach (var mediaId in request.MediaIds)
        {
            try
            {
                switch (request.Operation)
                {
                    case BulkOperation.Delete:
                        await DeleteMediaAsync(mediaId, ct).ConfigureAwait(false);
                        break;

                    case BulkOperation.Move:
                    case BulkOperation.ChangeCollection:
                        if (request.TargetCollectionId.HasValue)
                        {
                            var media = await _mediaRepository.GetMediaByIdAsync(mediaId, ct).ConfigureAwait(false);
                            if (media != null)
                            {
                                media.CollectionId = request.TargetCollectionId.Value;
                                await _mediaRepository.UpdateMediaAsync(media, ct).ConfigureAwait(false);
                                results.Add(MapToResponse(media));
                            }
                        }
                        break;

                    case BulkOperation.AddTags:
                        if (request.Tags != null && request.Tags.Count != 0)
                        {
                            await _mediaRepository.AddTagsAsync(mediaId, request.Tags, ct).ConfigureAwait(false);
                            var media = await _mediaRepository.GetMediaByIdAsync(mediaId, ct).ConfigureAwait(false);
                            if (media != null)
                            {
                                results.Add(MapToResponse(media));
                            }
                        }
                        break;

                    case BulkOperation.RemoveTags:
                        if (request.Tags != null && request.Tags.Count != 0)
                        {
                            await _mediaRepository.RemoveTagsAsync(mediaId, request.Tags, ct).ConfigureAwait(false);
                            var media = await _mediaRepository.GetMediaByIdAsync(mediaId, ct).ConfigureAwait(false);
                            if (media != null)
                            {
                                results.Add(MapToResponse(media));
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform bulk operation {Operation} on media {MediaId}",
                    request.Operation, mediaId);
            }
        }

        return results;
    }

    public async Task<List<MediaCollectionResponse>> GetAllCollectionsAsync(CancellationToken ct = default)
    {
        var collections = await _mediaRepository.GetAllCollectionsAsync(ct).ConfigureAwait(false);
        return collections.Select(MapToCollectionResponse).ToList();
    }

    public async Task<MediaCollectionResponse?> GetCollectionByIdAsync(Guid id, CancellationToken ct = default)
    {
        var collection = await _mediaRepository.GetCollectionByIdAsync(id, ct).ConfigureAwait(false);
        return collection != null ? MapToCollectionResponse(collection) : null;
    }

    public async Task<MediaCollectionResponse> CreateCollectionAsync(
        MediaCollectionRequest request,
        CancellationToken ct = default)
    {
        var entity = new MediaCollectionEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _mediaRepository.AddCollectionAsync(entity, ct).ConfigureAwait(false);
        return MapToCollectionResponse(entity);
    }

    public async Task<MediaCollectionResponse> UpdateCollectionAsync(
        Guid id,
        MediaCollectionRequest request,
        CancellationToken ct = default)
    {
        var collection = await _mediaRepository.GetCollectionByIdAsync(id, ct).ConfigureAwait(false);
        if (collection == null)
        {
            throw new InvalidOperationException($"Collection {id} not found");
        }

        collection.Name = request.Name;
        collection.Description = request.Description;
        collection.UpdatedAt = DateTime.UtcNow;

        await _mediaRepository.UpdateCollectionAsync(collection, ct).ConfigureAwait(false);
        return MapToCollectionResponse(collection);
    }

    public async Task DeleteCollectionAsync(Guid id, CancellationToken ct = default)
    {
        await _mediaRepository.DeleteCollectionAsync(id, ct).ConfigureAwait(false);
    }

    public async Task<List<string>> GetAllTagsAsync(CancellationToken ct = default)
    {
        return await _mediaRepository.GetAllTagsAsync(ct).ConfigureAwait(false);
    }

    public async Task TrackMediaUsageAsync(
        Guid mediaId,
        string projectId,
        string? projectName,
        CancellationToken ct = default)
    {
        await _mediaRepository.TrackUsageAsync(mediaId, projectId, projectName, ct).ConfigureAwait(false);
    }

    public async Task<MediaUsageInfo> GetMediaUsageAsync(Guid mediaId, CancellationToken ct = default)
    {
        var media = await _mediaRepository.GetMediaByIdAsync(mediaId, ct).ConfigureAwait(false);
        if (media == null)
        {
            throw new InvalidOperationException($"Media {mediaId} not found");
        }

        var usages = await _mediaRepository.GetUsageHistoryAsync(mediaId, ct).ConfigureAwait(false);

        return new MediaUsageInfo
        {
            MediaId = mediaId,
            TotalUsages = media.UsageCount,
            LastUsedAt = media.LastUsedAt ?? DateTime.MinValue,
            UsedInProjects = usages.Select(u => u.ProjectId).Distinct().ToList()
        };
    }

    public async Task<StorageStats> GetStorageStatsAsync(CancellationToken ct = default)
    {
        return await _mediaRepository.GetStorageStatsAsync(ct).ConfigureAwait(false);
    }

    public async Task<DuplicateDetectionResult> CheckForDuplicateAsync(
        Stream fileStream,
        CancellationToken ct = default)
    {
        var (_, hash) = await CalculateFileSizeAndHashAsync(fileStream, ct).ConfigureAwait(false);
        
        if (string.IsNullOrEmpty(hash))
        {
            return new DuplicateDetectionResult { IsDuplicate = false };
        }

        var duplicate = await _mediaRepository.FindByContentHashAsync(hash, ct).ConfigureAwait(false);
        
        return new DuplicateDetectionResult
        {
            IsDuplicate = duplicate != null,
            ExistingMediaId = duplicate?.Id,
            ContentHash = hash,
            SimilarityScore = duplicate != null ? 1.0 : 0.0
        };
    }

    public async Task<UploadSession> InitiateChunkedUploadAsync(
        string fileName,
        long totalSize,
        int totalChunks,
        CancellationToken ct = default)
    {
        var sessionEntity = new UploadSessionEntity
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            TotalSize = totalSize,
            TotalChunks = totalChunks,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            BlobUrl = string.Empty
        };

        await _mediaRepository.CreateUploadSessionAsync(sessionEntity, ct).ConfigureAwait(false);

        return new UploadSession
        {
            SessionId = sessionEntity.Id,
            FileName = fileName,
            TotalSize = totalSize,
            TotalChunks = totalChunks,
            ExpiresAt = sessionEntity.ExpiresAt
        };
    }

    public async Task UploadChunkAsync(
        Guid sessionId,
        int chunkIndex,
        Stream chunkStream,
        CancellationToken ct = default)
    {
        var session = await _mediaRepository.GetUploadSessionAsync(sessionId, ct).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Upload session {sessionId} not found");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Upload session {sessionId} has expired");
        }

        // Upload chunk to storage
        var blobUrl = await _storageService.UploadChunkAsync(sessionId, chunkIndex, chunkStream, ct).ConfigureAwait(false);

        // Update session
        session.BlobUrl = blobUrl;
        session.UploadedSize += chunkStream.Length;
        
        var completedChunks = JsonSerializer.Deserialize<List<int>>(session.CompletedChunksJson) ?? new List<int>();
        completedChunks.Add(chunkIndex);
        session.CompletedChunksJson = JsonSerializer.Serialize(completedChunks);

        await _mediaRepository.UpdateUploadSessionAsync(session, ct).ConfigureAwait(false);
    }

    public async Task<MediaItemResponse> CompleteChunkedUploadAsync(
        Guid sessionId,
        MediaUploadRequest request,
        CancellationToken ct = default)
    {
        var session = await _mediaRepository.GetUploadSessionAsync(sessionId, ct).ConfigureAwait(false);
        if (session == null)
        {
            throw new InvalidOperationException($"Upload session {sessionId} not found");
        }

        // Complete chunked upload in storage
        var blobUrl = await _storageService.CompleteChunkedUploadAsync(sessionId, ct).ConfigureAwait(false);

        // Create media entity
        var entity = new MediaEntity
        {
            Id = Guid.NewGuid(),
            FileName = request.FileName,
            Type = request.Type.ToString(),
            Source = request.Source.ToString(),
            FileSize = session.TotalSize,
            Description = request.Description,
            BlobUrl = blobUrl,
            ProcessingStatus = ProcessingStatus.Completed.ToString(),
            CollectionId = request.CollectionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _mediaRepository.AddMediaAsync(entity, ct).ConfigureAwait(false);

        if (request.Tags.Count != 0)
        {
            await _mediaRepository.AddTagsAsync(entity.Id, request.Tags, ct).ConfigureAwait(false);
        }

        // Clean up session (done in background)
        _ = Task.Run(async () =>
        {
            try
            {
                await _mediaRepository.DeleteExpiredSessionsAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Ignore
            }
        });

        entity = await _mediaRepository.GetMediaByIdAsync(entity.Id, ct).ConfigureAwait(false);
        return MapToResponse(entity!);
    }

    private MediaItemResponse MapToResponse(MediaEntity entity)
    {
        MediaMetadata? metadata = null;
        if (!string.IsNullOrEmpty(entity.MetadataJson))
        {
            try
            {
                metadata = JsonSerializer.Deserialize<MediaMetadata>(entity.MetadataJson);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new MediaItemResponse
        {
            Id = entity.Id,
            FileName = entity.FileName,
            Type = Enum.Parse<MediaType>(entity.Type),
            Source = Enum.Parse<MediaSource>(entity.Source),
            FileSize = entity.FileSize,
            Description = entity.Description,
            ThumbnailUrl = entity.ThumbnailUrl,
            PreviewUrl = entity.PreviewUrl,
            Url = entity.BlobUrl,
            Metadata = metadata,
            ProcessingStatus = Enum.Parse<ProcessingStatus>(entity.ProcessingStatus),
            Tags = entity.Tags.Select(t => t.Tag).ToList(),
            CollectionId = entity.CollectionId,
            CollectionName = entity.Collection?.Name,
            UsageCount = entity.UsageCount,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private MediaCollectionResponse MapToCollectionResponse(MediaCollectionEntity entity)
    {
        return new MediaCollectionResponse
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            MediaCount = entity.MediaItems?.Count ?? 0,
            ThumbnailUrl = entity.ThumbnailUrl,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    private async Task<(long Size, string Hash)> CalculateFileSizeAndHashAsync(Stream stream, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        var buffer = new byte[8192];
        long totalBytes = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
        {
            totalBytes += bytesRead;
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var hash = BitConverter.ToString(sha256.Hash!).Replace("-", "").ToLowerInvariant();

        return (totalBytes, hash);
    }

    private string GetContentType(string fileName, MediaType type)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        
        return type switch
        {
            MediaType.Video => extension switch
            {
                ".mp4" => "video/mp4",
                ".mov" => "video/quicktime",
                ".avi" => "video/x-msvideo",
                ".mkv" => "video/x-matroska",
                ".webm" => "video/webm",
                _ => "application/octet-stream"
            },
            MediaType.Image => extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            },
            MediaType.Audio => extension switch
            {
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                ".m4a" => "audio/mp4",
                _ => "application/octet-stream"
            },
            _ => "application/octet-stream"
        };
    }
}
