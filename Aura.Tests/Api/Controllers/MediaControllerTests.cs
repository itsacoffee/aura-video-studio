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

namespace Aura.Tests.Api.Controllers;

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
        
        // Setup HttpContext
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
            Id = mediaId,
            FileName = "test.mp4",
            Type = MediaType.Video,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockMediaService
            .Setup(s => s.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaResponse);

        // Act
        var result = await _controller.GetMedia(mediaId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedMedia = Assert.IsType<MediaItemResponse>(okResult.Value);
        Assert.Equal(mediaId, returnedMedia.Id);
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
        Assert.Equal(404, notFoundResult.StatusCode);
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

        var response = new MediaSearchResponse
        {
            Items = new List<MediaItemResponse>
            {
                new MediaItemResponse
                {
                    Id = Guid.NewGuid(),
                    FileName = "test.mp4",
                    Type = MediaType.Video,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            },
            TotalItems = 1,
            Page = 1,
            PageSize = 10,
            TotalPages = 1
        };

        _mockMediaService
            .Setup(s => s.SearchMediaAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.SearchMedia(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var searchResponse = Assert.IsType<MediaSearchResponse>(okResult.Value);
        Assert.Single(searchResponse.Items);
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
        _mockMediaService.Verify(s => s.DeleteMediaAsync(mediaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetCollections_ReturnsAllCollections()
    {
        // Arrange
        var collections = new List<MediaCollectionResponse>
        {
            new MediaCollectionResponse
            {
                Id = Guid.NewGuid(),
                Name = "Collection 1",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new MediaCollectionResponse
            {
                Id = Guid.NewGuid(),
                Name = "Collection 2",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _mockMediaService
            .Setup(s => s.GetAllCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _controller.GetCollections(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCollections = Assert.IsType<List<MediaCollectionResponse>>(okResult.Value);
        Assert.Equal(2, returnedCollections.Count);
    }

    [Fact]
    public async Task CreateCollection_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new MediaCollectionRequest
        {
            Name = "New Collection",
            Description = "Test collection"
        };

        var response = new MediaCollectionResponse
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockMediaService
            .Setup(s => s.CreateCollectionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateCollection(request, CancellationToken.None);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var collection = Assert.IsType<MediaCollectionResponse>(createdResult.Value);
        Assert.Equal(request.Name, collection.Name);
    }

    [Fact]
    public async Task GetStats_ReturnsStorageStats()
    {
        // Arrange
        var stats = new StorageStats
        {
            TotalSizeBytes = 1000000,
            QuotaBytes = 10000000,
            AvailableBytes = 9000000,
            UsagePercentage = 10.0,
            TotalFiles = 5,
            FilesByType = new Dictionary<MediaType, int>
            {
                { MediaType.Video, 3 },
                { MediaType.Image, 2 }
            },
            SizeByType = new Dictionary<MediaType, long>
            {
                { MediaType.Video, 800000 },
                { MediaType.Image, 200000 }
            }
        };

        _mockMediaService
            .Setup(s => s.GetStorageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetStats(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedStats = Assert.IsType<StorageStats>(okResult.Value);
        Assert.Equal(5, returnedStats.TotalFiles);
        Assert.Equal(10.0, returnedStats.UsagePercentage);
    }
}
