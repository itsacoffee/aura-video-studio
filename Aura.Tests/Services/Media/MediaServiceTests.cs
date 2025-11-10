using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private readonly Mock<IMediaRepository> _mockRepository;
    private readonly Mock<IStorageService> _mockStorage;
    private readonly Mock<IThumbnailGenerationService> _mockThumbnail;
    private readonly Mock<IMediaMetadataService> _mockMetadata;
    private readonly Mock<ILogger<MediaService>> _mockLogger;
    private readonly MediaService _service;

    public MediaServiceTests()
    {
        _mockRepository = new Mock<IMediaRepository>();
        _mockStorage = new Mock<IStorageService>();
        _mockThumbnail = new Mock<IThumbnailGenerationService>();
        _mockMetadata = new Mock<IMediaMetadataService>();
        _mockLogger = new Mock<ILogger<MediaService>>();

        _service = new MediaService(
            _mockRepository.Object,
            _mockStorage.Object,
            _mockThumbnail.Object,
            _mockMetadata.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetMediaByIdAsync_ReturnsMedia_WhenExists()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var entity = new MediaEntity
        {
            Id = mediaId,
            FileName = "test.mp4",
            Type = "Video",
            Source = "UserUpload",
            FileSize = 1024,
            BlobUrl = "test-url",
            ProcessingStatus = "Completed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<MediaTagEntity>()
        };

        _mockRepository
            .Setup(r => r.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetMediaByIdAsync(mediaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mediaId, result.Id);
        Assert.Equal("test.mp4", result.FileName);
    }

    [Fact]
    public async Task UploadMediaAsync_UploadsSuccessfully()
    {
        // Arrange
        var content = new byte[] { 1, 2, 3, 4 };
        var stream = new MemoryStream(content);
        var request = new MediaUploadRequest
        {
            FileName = "test.mp4",
            Type = MediaType.Video,
            Source = MediaSource.UserUpload,
            GenerateThumbnail = true,
            ExtractMetadata = true
        };

        _mockStorage
            .Setup(s => s.UploadFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("blob-url");

        _mockThumbnail
            .Setup(t => t.GenerateThumbnailFromStreamAsync(
                It.IsAny<Stream>(),
                It.IsAny<MediaType>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("thumbnail-url");

        _mockMetadata
            .Setup(m => m.ExtractMetadataFromStreamAsync(
                It.IsAny<Stream>(),
                It.IsAny<MediaType>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaMetadata { Width = 1920, Height = 1080 });

        _mockRepository
            .Setup(r => r.AddMediaAsync(It.IsAny<MediaEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaEntity e, CancellationToken ct) => e);

        _mockRepository
            .Setup(r => r.GetMediaByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken ct) => new MediaEntity
            {
                Id = id,
                FileName = "test.mp4",
                Type = "Video",
                Source = "UserUpload",
                FileSize = 4,
                BlobUrl = "blob-url",
                ThumbnailUrl = "thumbnail-url",
                ProcessingStatus = "Completed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<MediaTagEntity>()
            });

        // Act
        var result = await _service.UploadMediaAsync(stream, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.mp4", result.FileName);
        Assert.Equal("blob-url", result.Url);
        Assert.Equal("thumbnail-url", result.ThumbnailUrl);
    }

    [Fact]
    public async Task SearchMediaAsync_ReturnsFilteredResults()
    {
        // Arrange
        var entities = new List<MediaEntity>
        {
            new MediaEntity
            {
                Id = Guid.NewGuid(),
                FileName = "video1.mp4",
                Type = "Video",
                Source = "UserUpload",
                FileSize = 1024,
                BlobUrl = "url1",
                ProcessingStatus = "Completed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<MediaTagEntity>()
            },
            new MediaEntity
            {
                Id = Guid.NewGuid(),
                FileName = "image1.jpg",
                Type = "Image",
                Source = "UserUpload",
                FileSize = 512,
                BlobUrl = "url2",
                ProcessingStatus = "Completed",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Tags = new List<MediaTagEntity>()
            }
        };

        _mockRepository
            .Setup(r => r.SearchMediaAsync(It.IsAny<MediaSearchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((entities, 2));

        var request = new MediaSearchRequest
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var result = await _service.SearchMediaAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalItems);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task DeleteMediaAsync_DeletesFromStorageAndDatabase()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var entity = new MediaEntity
        {
            Id = mediaId,
            FileName = "test.mp4",
            Type = "Video",
            Source = "UserUpload",
            FileSize = 1024,
            BlobUrl = "blob-url",
            ThumbnailUrl = "thumbnail-url",
            ProcessingStatus = "Completed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<MediaTagEntity>()
        };

        _mockRepository
            .Setup(r => r.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        await _service.DeleteMediaAsync(mediaId);

        // Assert
        _mockStorage.Verify(s => s.DeleteFileAsync("blob-url", It.IsAny<CancellationToken>()), Times.Once);
        _mockStorage.Verify(s => s.DeleteFileAsync("thumbnail-url", It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteMediaAsync(mediaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetStorageStatsAsync_ReturnsStats()
    {
        // Arrange
        var stats = new StorageStats
        {
            TotalSizeBytes = 1024 * 1024,
            QuotaBytes = 50L * 1024 * 1024 * 1024,
            TotalFiles = 10
        };

        _mockRepository
            .Setup(r => r.GetStorageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _service.GetStorageStatsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1024 * 1024, result.TotalSizeBytes);
        Assert.Equal(10, result.TotalFiles);
    }
}
