using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models.Media;
using Aura.Core.Services.Media;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.Media;

public class MediaGenerationIntegrationServiceTests
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<IMediaRepository> _mockMediaRepository;
    private readonly Mock<ILogger<MediaGenerationIntegrationService>> _mockLogger;
    private readonly MediaGenerationIntegrationService _service;

    public MediaGenerationIntegrationServiceTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _mockMediaRepository = new Mock<IMediaRepository>();
        _mockLogger = new Mock<ILogger<MediaGenerationIntegrationService>>();

        _service = new MediaGenerationIntegrationService(
            _mockMediaService.Object,
            _mockMediaRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetProjectMediaAsync_ReturnsMediaForProject()
    {
        // Arrange
        var projectId = "test-project-123";
        var mediaItems = new List<MediaEntity>
        {
            CreateTestMediaEntity(Guid.NewGuid(), "video1.mp4"),
            CreateTestMediaEntity(Guid.NewGuid(), "video2.mp4")
        };

        _mockMediaRepository
            .Setup(r => r.SearchMediaAsync(
                It.Is<MediaSearchRequest>(req => req.SearchTerm == projectId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((mediaItems, mediaItems.Count));

        // Act
        var result = await _service.GetProjectMediaAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SaveGeneratedMediaAsync_SavesMediaWithGeneratedSource()
    {
        // Arrange
        var filePath = "/path/to/generated-video.mp4";
        var projectId = "test-project";
        var description = "Generated video";
        var tags = new List<string> { "generated", "test" };

        MediaItemResponse? capturedResult = null;
        _mockMediaService
            .Setup(s => s.UploadMediaAsync(
                It.IsAny<System.IO.Stream>(),
                It.Is<MediaUploadRequest>(req => 
                    req.Source == MediaSource.Generated && 
                    req.Tags.Contains("generated")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.IO.Stream stream, MediaUploadRequest req, CancellationToken ct) =>
            {
                capturedResult = new MediaItemResponse
                {
                    Id = Guid.NewGuid(),
                    FileName = req.FileName,
                    Type = req.Type,
                    Source = req.Source,
                    Tags = req.Tags,
                    Description = req.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                return capturedResult;
            });

        // Act
        var result = await _service.SaveGeneratedMediaAsync(
            filePath,
            MediaType.Video,
            projectId,
            description,
            tags);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(MediaSource.Generated, result.Source);
        Assert.Contains("generated", result.Tags);
        _mockMediaService.Verify(s => s.UploadMediaAsync(
            It.IsAny<System.IO.Stream>(),
            It.IsAny<MediaUploadRequest>(),
            It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task LinkMediaToProjectAsync_TracksMediaUsage()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var projectId = "test-project";
        var projectName = "Test Project";

        _mockMediaService
            .Setup(s => s.TrackMediaUsageAsync(
                mediaId,
                projectId,
                projectName,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.LinkMediaToProjectAsync(mediaId, projectId, projectName);

        // Assert
        _mockMediaService.Verify(s => s.TrackMediaUsageAsync(
            mediaId,
            projectId,
            projectName,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMediaUsedInProjectAsync_ReturnsProjectMedia()
    {
        // Arrange
        var projectId = "test-project";
        var mediaId = Guid.NewGuid();
        var usageEntity = new MediaUsageEntity
        {
            Id = Guid.NewGuid(),
            MediaId = mediaId,
            ProjectId = projectId,
            ProjectName = "Test Project",
            UsedAt = DateTime.UtcNow
        };

        var mediaEntity = CreateTestMediaEntity(mediaId, "video.mp4");

        _mockMediaRepository
            .Setup(r => r.GetUsageHistoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MediaUsageEntity> { usageEntity });

        _mockMediaService
            .Setup(s => s.SearchMediaAsync(
                It.IsAny<MediaSearchRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaSearchResponse
            {
                Items = new List<MediaItemResponse>
                {
                    new MediaItemResponse
                    {
                        Id = mediaId,
                        FileName = "video.mp4",
                        Type = MediaType.Video,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }
                },
                TotalItems = 1,
                Page = 1,
                PageSize = 1000,
                TotalPages = 1
            });

        // Act
        var result = await _service.GetMediaUsedInProjectAsync(projectId);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateProjectCollectionAsync_CreatesCollection()
    {
        // Arrange
        var projectId = "test-project";
        var projectName = "Test Project";
        var collectionResponse = new MediaCollectionResponse
        {
            Id = Guid.NewGuid(),
            Name = $"Project: {projectName}",
            Description = $"Media collection for project {projectId}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockMediaService
            .Setup(s => s.CreateCollectionAsync(
                It.Is<MediaCollectionRequest>(req => 
                    req.Name.Contains(projectName)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(collectionResponse);

        // Act
        var result = await _service.CreateProjectCollectionAsync(projectId, projectName);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(projectName, result.Name);
        _mockMediaService.Verify(s => s.CreateCollectionAsync(
            It.IsAny<MediaCollectionRequest>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private MediaEntity CreateTestMediaEntity(Guid id, string fileName)
    {
        return new MediaEntity
        {
            Id = id,
            FileName = fileName,
            Type = "Video",
            Source = "UserUpload",
            FileSize = 1024000,
            BlobUrl = $"local://media/{id}",
            ProcessingStatus = "Completed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Tags = new List<MediaTagEntity>(),
            Usages = new List<MediaUsageEntity>()
        };
    }
}
