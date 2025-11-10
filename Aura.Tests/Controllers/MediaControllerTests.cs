using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Aura.Core.Models.Media;
using Aura.Core.Services.Media;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Controllers;

public class MediaControllerTests
{
    private readonly Mock<IMediaService> _mockMediaService;
    private readonly Mock<ILogger<MediaController>> _mockLogger;
    private readonly MediaController _controller;

    public MediaControllerTests()
    {
        _mockMediaService = new Mock<IMediaService>();
        _mockLogger = new Mock<ILogger<MediaController>>();
        _controller = new MediaController(_mockMediaService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetMedia_ExistingId_ReturnsOk()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var mediaResponse = new MediaItemResponse
        {
            Id = mediaId.ToString(),
            FileName = "test.mp4",
            Type = MediaType.Video,
            Source = MediaSource.UserUpload,
            FileSize = 1024 * 1024,
            Url = "https://example.com/media/test.mp4",
            ProcessingStatus = ProcessingStatus.Completed,
            Tags = new List<string>(),
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o")
        };

        _mockMediaService
            .Setup(s => s.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaResponse);

        // Act
        var result = await _controller.GetMedia(mediaId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<MediaItemResponse>(okResult.Value);
        Assert.Equal(mediaId.ToString(), returnValue.Id);
    }

    [Fact]
    public async Task GetMedia_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        _mockMediaService
            .Setup(s => s.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItemResponse?)null);

        // Act
        var result = await _controller.GetMedia(mediaId, CancellationToken.None);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
        Assert.Equal(404, problemDetails.Status);
    }

    [Fact]
    public async Task SearchMedia_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new MediaSearchRequest
        {
            SearchTerm = "test",
            Page = 1,
            PageSize = 10
        };

        var searchResponse = new MediaSearchResponse
        {
            Items = new List<MediaItemResponse>
            {
                new MediaItemResponse
                {
                    Id = Guid.NewGuid().ToString(),
                    FileName = "test.mp4",
                    Type = MediaType.Video,
                    Source = MediaSource.UserUpload,
                    FileSize = 1024 * 1024,
                    Url = "https://example.com/media/test.mp4",
                    ProcessingStatus = ProcessingStatus.Completed,
                    Tags = new List<string>(),
                    UsageCount = 0,
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    UpdatedAt = DateTime.UtcNow.ToString("o")
                }
            },
            TotalItems = 1,
            Page = 1,
            PageSize = 10,
            TotalPages = 1
        };

        _mockMediaService
            .Setup(s => s.SearchMediaAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResponse);

        // Act
        var result = await _controller.SearchMedia(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<MediaSearchResponse>(okResult.Value);
        Assert.Equal(1, returnValue.TotalItems);
    }

    [Fact]
    public async Task UploadMedia_NullFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.UploadMedia(
            null!,
            null,
            null,
            null,
            "Video",
            true,
            true,
            CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(400, problemDetails.Status);
    }

    [Fact]
    public async Task UploadMedia_ValidFile_ReturnsOk()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        var content = "test content"u8.ToArray();
        var ms = new MemoryStream(content);
        var fileName = "test.mp4";

        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(content.Length);
        mockFile.Setup(f => f.OpenReadStream()).Returns(ms);

        var mediaResponse = new MediaItemResponse
        {
            Id = Guid.NewGuid().ToString(),
            FileName = fileName,
            Type = MediaType.Video,
            Source = MediaSource.UserUpload,
            FileSize = content.Length,
            Url = "https://example.com/media/test.mp4",
            ProcessingStatus = ProcessingStatus.Completed,
            Tags = new List<string>(),
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow.ToString("o"),
            UpdatedAt = DateTime.UtcNow.ToString("o")
        };

        _mockMediaService
            .Setup(s => s.UploadMediaAsync(It.IsAny<Stream>(), It.IsAny<MediaUploadRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaResponse);

        // Act
        var result = await _controller.UploadMedia(
            mockFile.Object,
            "Test description",
            "tag1,tag2",
            null,
            "Video",
            true,
            true,
            CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<MediaItemResponse>(okResult.Value);
        Assert.Equal(fileName, returnValue.FileName);
    }

    [Fact]
    public async Task DeleteMedia_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        _mockMediaService
            .Setup(s => s.DeleteMediaAsync(mediaId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteMedia(mediaId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetCollections_ReturnsOk()
    {
        // Arrange
        var collections = new List<MediaCollectionResponse>
        {
            new MediaCollectionResponse
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Collection",
                Description = "A test collection",
                MediaCount = 5,
                CreatedAt = DateTime.UtcNow.ToString("o"),
                UpdatedAt = DateTime.UtcNow.ToString("o")
            }
        };

        _mockMediaService
            .Setup(s => s.GetAllCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _controller.GetCollections(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<List<MediaCollectionResponse>>(okResult.Value);
        Assert.Single(returnValue);
    }

    [Fact]
    public async Task GetStats_ReturnsOk()
    {
        // Arrange
        var stats = new StorageStats
        {
            TotalSizeBytes = 1024 * 1024 * 100,
            QuotaBytes = 1024 * 1024 * 1024,
            AvailableBytes = 1024 * 1024 * 924,
            UsagePercentage = 9.77,
            TotalFiles = 50,
            FilesByType = new Dictionary<MediaType, int>
            {
                { MediaType.Video, 20 },
                { MediaType.Image, 30 }
            },
            SizeByType = new Dictionary<MediaType, long>
            {
                { MediaType.Video, 1024 * 1024 * 80 },
                { MediaType.Image, 1024 * 1024 * 20 }
            }
        };

        _mockMediaService
            .Setup(s => s.GetStorageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetStats(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<StorageStats>(okResult.Value);
        Assert.Equal(50, returnValue.TotalFiles);
    }
}
