using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models.Media;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Media;

/// <summary>
/// Service for integrating media library with video generation pipeline
/// </summary>
public interface IMediaGenerationIntegrationService
{
    /// <summary>
    /// Get media items suitable for use in a video project
    /// </summary>
    Task<List<MediaItemResponse>> GetProjectMediaAsync(
        string projectId,
        CancellationToken ct = default);

    /// <summary>
    /// Save generated media to the library
    /// </summary>
    Task<MediaItemResponse> SaveGeneratedMediaAsync(
        string filePath,
        MediaType type,
        string? projectId = null,
        string? description = null,
        List<string>? tags = null,
        CancellationToken ct = default);

    /// <summary>
    /// Link existing media to a project for tracking usage
    /// </summary>
    Task LinkMediaToProjectAsync(
        Guid mediaId,
        string projectId,
        string? projectName = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get all media used in a project
    /// </summary>
    Task<List<MediaItemResponse>> GetMediaUsedInProjectAsync(
        string projectId,
        CancellationToken ct = default);

    /// <summary>
    /// Create a collection for a project's media
    /// </summary>
    Task<MediaCollectionResponse> CreateProjectCollectionAsync(
        string projectId,
        string projectName,
        CancellationToken ct = default);

    /// <summary>
    /// Get downloadable URLs for media items
    /// </summary>
    Task<Dictionary<Guid, string>> GetDownloadUrlsAsync(
        List<Guid> mediaIds,
        CancellationToken ct = default);
}

/// <summary>
/// Implementation of media generation integration service
/// </summary>
public class MediaGenerationIntegrationService : IMediaGenerationIntegrationService
{
    private readonly IMediaService _mediaService;
    private readonly IMediaRepository _mediaRepository;
    private readonly ILogger<MediaGenerationIntegrationService> _logger;

    public MediaGenerationIntegrationService(
        IMediaService mediaService,
        IMediaRepository mediaRepository,
        ILogger<MediaGenerationIntegrationService> logger)
    {
        _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        _mediaRepository = mediaRepository ?? throw new ArgumentNullException(nameof(mediaRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<MediaItemResponse>> GetProjectMediaAsync(
        string projectId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting media for project {ProjectId}", projectId);

        // Search for media by collection (project)
        var searchRequest = new MediaSearchRequest
        {
            SearchTerm = projectId,
            PageSize = 1000,
            SortBy = "CreatedAt",
            SortDescending = true
        };

        var response = await _mediaService.SearchMediaAsync(searchRequest, ct);
        return response.Items;
    }

    public async Task<MediaItemResponse> SaveGeneratedMediaAsync(
        string filePath,
        MediaType type,
        string? projectId = null,
        string? description = null,
        List<string>? tags = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Saving generated media to library: {FilePath}, Type: {Type}, Project: {ProjectId}",
            filePath, type, projectId);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Generated media file not found: {filePath}");
        }

        // Create collection for project if specified
        Guid? collectionId = null;
        if (!string.IsNullOrEmpty(projectId))
        {
            var collections = await _mediaService.GetAllCollectionsAsync(ct);
            var projectCollection = collections.FirstOrDefault(c =>
                c.Name.Contains(projectId, StringComparison.OrdinalIgnoreCase));

            if (projectCollection != null)
            {
                collectionId = projectCollection.Id;
            }
        }

        // Prepare upload request
        var fileName = Path.GetFileName(filePath);
        var uploadRequest = new MediaUploadRequest
        {
            FileName = fileName,
            Type = type,
            Source = MediaSource.Generated,
            Description = description ?? $"Generated {type.ToString().ToLower()} from project {projectId}",
            Tags = tags ?? new List<string> { "generated", projectId ?? "unknown" },
            CollectionId = collectionId,
            GenerateThumbnail = true,
            ExtractMetadata = true
        };

        // Upload the file
        using var fileStream = File.OpenRead(filePath);
        var result = await _mediaService.UploadMediaAsync(fileStream, uploadRequest, ct);

        _logger.LogInformation(
            "Generated media saved to library: {MediaId}, File: {FileName}",
            result.Id, result.FileName);

        return result;
    }

    public async Task LinkMediaToProjectAsync(
        Guid mediaId,
        string projectId,
        string? projectName = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Linking media {MediaId} to project {ProjectId}",
            mediaId, projectId);

        await _mediaService.TrackMediaUsageAsync(mediaId, projectId, projectName, ct);
    }

    public async Task<List<MediaItemResponse>> GetMediaUsedInProjectAsync(
        string projectId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting media used in project {ProjectId}", projectId);

        // Get all media items
        var searchRequest = new MediaSearchRequest
        {
            PageSize = 10000
        };

        var allMedia = await _mediaService.SearchMediaAsync(searchRequest, ct);

        // Filter by usage in the project
        var usedMedia = new List<MediaItemResponse>();
        foreach (var media in allMedia.Items)
        {
            var usage = await _mediaService.GetMediaUsageAsync(media.Id, ct);
            if (usage.UsedInProjects.Contains(projectId))
            {
                usedMedia.Add(media);
            }
        }

        return usedMedia;
    }

    public async Task<MediaCollectionResponse> CreateProjectCollectionAsync(
        string projectId,
        string projectName,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating collection for project {ProjectId}: {ProjectName}",
            projectId, projectName);

        var request = new MediaCollectionRequest
        {
            Name = $"{projectName} - {projectId}",
            Description = $"Media collection for project: {projectName}"
        };

        return await _mediaService.CreateCollectionAsync(request, ct);
    }

    public async Task<Dictionary<Guid, string>> GetDownloadUrlsAsync(
        List<Guid> mediaIds,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting download URLs for {Count} media items", mediaIds.Count);

        var urls = new Dictionary<Guid, string>();

        foreach (var mediaId in mediaIds)
        {
            var media = await _mediaService.GetMediaByIdAsync(mediaId, ct);
            if (media != null)
            {
                urls[mediaId] = media.Url;
            }
        }

        return urls;
    }
}
