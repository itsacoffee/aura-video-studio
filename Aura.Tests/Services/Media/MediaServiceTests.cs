using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models.Media;
using Aura.Core.Services.Media;
using Aura.Core.Services.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.Media;

public class MediaServiceTests
{
    private readonly Mock<IMediaRepository> _mockMediaRepository;
    private readonly Mock<IStorageService> _mockStorageService;
    private readonly Mock<IThumbnailGenerationService> _mockThumbnailService;
    private readonly Mock<IMediaMetadataService> _mockMetadataService;
    private readonly Mock<ILogger<MediaService>> _mockLogger;
    private readonly MediaService _mediaService;

    public MediaServiceTests()
    {
        _mockMediaRepository = new Mock<IMediaRepository>();
        _mockStorageService = new Mock<IStorageService>();
        _mockThumbnailService = new Mock<IThumbnailGenerationService>();
        _mockMetadataService = new Mock<IMediaMetadataService>();
        _mockLogger = new Mock<ILogger<MediaService>>();

        _mediaService = new MediaService(
            _mockMediaRepository.Object,
            _mockStorageService.Object,
            _mockThumbnailService.Object,
            _mockMetadataService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetMediaByIdAsync_ExistingMedia_ReturnsMedia()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var mediaEntity = CreateTestMediaEntity(mediaId);
        _mockMediaRepository
            .Setup(r => r.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaEntity);

        // Act
        var result = await _mediaService.GetMediaByIdAsync(mediaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaId.ToString(), result.Id);
        Assert.Equal("test-video.mp4", result.FileName);
        Assert.Equal(MediaType.Video, result.Type);
    }

    [Fact]
    public async Task GetMediaByIdAsync_NonExistentMedia_ReturnsNull()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        _mockMediaRepository
            .Setup(r => r.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaEntity?)null);

        // Act
        var result = await _mediaService.GetMediaByIdAsync(mediaId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SearchMediaAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var request = new MediaSearchRequest
        {
            SearchTerm = "test",
            Types = new List<MediaType> { MediaType.Video },
            Page = 1,
            PageSize = 10
        };

        var mediaItems = new List<MediaEntity>
        {
            CreateTestMediaEntity(Guid.NewGuid(), "test-video-1.mp4"),
            CreateTestMediaEntity(Guid.NewGuid(), "test-video-2.mp4")
        };

        _mockMediaRepository
            .Setup(r => r.SearchMediaAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((mediaItems, 2));

        // Act
        var result = await _mediaService.SearchMediaAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(1, result.TotalPages);
    }

    [Fact]
    public async Task UploadMediaAsync_ValidFile_UploadsSuccessfully()
    {
        // Arrange
        var fileName = "test-video.mp4";
        var fileContent = "test content"u8.ToArray();
        using var stream = new MemoryStream(fileContent);

        var uploadRequest = new MediaUploadRequest
        {
            FileName = fileName,
            Type = MediaType.Video,
            Source = MediaSource.UserUpload,
            GenerateThumbnail = true,
            ExtractMetadata = true
        };

        var blobUrl = "https://storage.example.com/media/test-video.mp4";
        var thumbnailUrl = "https://storage.example.com/thumbnails/test-video.jpg";
        var metadata = new MediaMetadata
        {
            Width = 1920,
            Height = 1080,
            Duration = 120.5
        };

        _mockStorageService
            .Setup(s => s.UploadFileAsync(It.IsAny<Stream>(), fileName, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(blobUrl);

        _mockThumbnailService
            .Setup(s => s.GenerateThumbnailFromStreamAsync(It.IsAny<Stream>(), MediaType.Video, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(thumbnailUrl);

        _mockMetadataService
            .Setup(s => s.ExtractMetadataFromStreamAsync(It.IsAny<Stream>(), MediaType.Video, fileName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        _mockMediaRepository
            .Setup(r => r.AddMediaAsync(It.IsAny<MediaEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaEntity entity, CancellationToken ct) => entity);

        _mockMediaRepository
            .Setup(r => r.GetMediaByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => CreateTestMediaEntity(id, fileName));

        // Act
        var result = await _mediaService.UploadMediaAsync(stream, uploadRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileName, result.FileName);
        Assert.Equal(MediaType.Video, result.Type);
        Assert.Equal(MediaSource.UserUpload, result.Source);
        _mockStorageService.Verify(s => s.UploadFileAsync(It.IsAny<Stream>(), fileName, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockThumbnailService.Verify(s => s.GenerateThumbnailFromStreamAsync(It.IsAny<Stream>(), MediaType.Video, fileName, It.IsAny<CancellationToken>()), Times.Once);
        _mockMetadataService.Verify(s => s.ExtractMetadataFromStreamAsync(It.IsAny<Stream>(), MediaType.Video, fileName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteMediaAsync_ExistingMedia_DeletesSuccessfully()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var mediaEntity = CreateTestMediaEntity(mediaId);

        _mockMediaRepository
            .Setup(r => r.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaEntity);

        _mockStorageService
            .Setup(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockMediaRepository
            .Setup(r => r.DeleteMediaAsync(mediaId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mediaService.DeleteMediaAsync(mediaId);

        // Assert
        _mockStorageService.Verify(s => s.DeleteFileAsync(mediaEntity.BlobUrl, It.IsAny<CancellationToken>()), Times.Once);
        _mockMediaRepository.Verify(r => r.DeleteMediaAsync(mediaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TrackMediaUsageAsync_ValidInput_TracksUsage()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var projectId = "test-project-123";
        var projectName = "Test Project";

        _mockMediaRepository
            .Setup(r => r.TrackUsageAsync(mediaId, projectId, projectName, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _mediaService.TrackMediaUsageAsync(mediaId, projectId, projectName);

        // Assert
        _mockMediaRepository.Verify(r => r.TrackUsageAsync(mediaId, projectId, projectName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStorageStatsAsync_ReturnsStats()
    {
        // Arrange
        var stats = new StorageStats
        {
            TotalSizeBytes = 1024 * 1024 * 100, // 100 MB
            QuotaBytes = 1024 * 1024 * 1024, // 1 GB
            TotalFiles = 50,
            FilesByType = new Dictionary<MediaType, int>
            {
                { MediaType.Video, 20 },
                { MediaType.Image, 25 },
                { MediaType.Audio, 5 }
            },
            SizeByType = new Dictionary<MediaType, long>
            {
                { MediaType.Video, 1024 * 1024 * 80 },
                { MediaType.Image, 1024 * 1024 * 15 },
                { MediaType.Audio, 1024 * 1024 * 5 }
            }
        };

        _mockMediaRepository
            .Setup(r => r.GetStorageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _mediaService.GetStorageStatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(100 * 1024 * 1024, result.TotalSizeBytes);
        Assert.Equal(50, result.TotalFiles);
        Assert.Equal(3, result.FilesByType.Count);
    }

    [Fact]
    public async Task BulkOperationAsync_Delete_DeletesMultipleItems()
    {
        // Arrange
        var mediaIds = new List<Guid>
        {
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid()
        };

        var request = new BulkMediaOperationRequest
        {
            MediaIds = mediaIds,
            Operation = BulkOperation.Delete
        };

        foreach (var id in mediaIds)
        {
            _mockMediaRepository
                .Setup(r => r.GetMediaByIdAsync(id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(CreateTestMediaEntity(id));

            _mockMediaRepository
                .Setup(r => r.DeleteMediaAsync(id, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        _mockStorageService
            .Setup(s => s.DeleteFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _mediaService.BulkOperationAsync(request);

        // Assert
        Assert.NotNull(result);
        _mockMediaRepository.Verify(r => r.DeleteMediaAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(mediaIds.Count));
    }

    [Fact]
    public async Task CreateCollectionAsync_ValidRequest_CreatesCollection()
    {
        // Arrange
        var request = new MediaCollectionRequest
        {
            Name = "Test Collection",
            Description = "A test collection"
        };

        var collectionEntity = new MediaCollectionEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockMediaRepository
            .Setup(r => r.AddCollectionAsync(It.IsAny<MediaCollectionEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaCollectionEntity entity, CancellationToken ct) => entity);

        // Act
        var result = await _mediaService.CreateCollectionAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Name, result.Name);
        Assert.Equal(request.Description, result.Description);
    }

    private MediaEntity CreateTestMediaEntity(Guid id, string fileName = "test-video.mp4")
    {
        return new MediaEntity
        {
            Id = id,
            FileName = fileName,
            Type = MediaType.Video.ToString(),
            Source = MediaSource.UserUpload.ToString(),
            FileSize = 1024 * 1024 * 10, // 10 MB
            BlobUrl = $"https://storage.example.com/media/{fileName}",
            ThumbnailUrl = $"https://storage.example.com/thumbnails/{Path.GetFileNameWithoutExtension(fileName)}.jpg",
            ProcessingStatus = ProcessingStatus.Completed.ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<MediaTagEntity>(),
            Usages = new List<MediaUsageEntity>()
        };
    }
}
