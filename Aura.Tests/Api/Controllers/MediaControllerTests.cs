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
    private readonly Mock<IMediaService> _mockService;
    private readonly Mock<ILogger<MediaController>> _mockLogger;
    private readonly MediaController _controller;

    public MediaControllerTests()
    {
        _mockService = new Mock<IMediaService>();
        _mockLogger = new Mock<ILogger<MediaController>>();
        _controller = new MediaController(_mockService.Object, _mockLogger.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetMedia_ReturnsOk_WhenMediaExists()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        var mediaResponse = new MediaItemResponse
        {
            Id = mediaId.ToString(),
            FileName = "test.mp4",
            Type = MediaType.Video,
            FileSize = 1024,
            Url = "test-url",
            ProcessingStatus = ProcessingStatus.Completed,
            Tags = new List<string>(),
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            UpdatedAt = DateTime.UtcNow.ToString("O")
        };

        _mockService
            .Setup(s => s.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(mediaResponse);

        // Act
        var result = await _controller.GetMedia(mediaId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedMedia = Assert.IsType<MediaItemResponse>(okResult.Value);
        Assert.Equal(mediaId.ToString(), returnedMedia.Id);
    }

    [Fact]
    public async Task GetMedia_ReturnsNotFound_WhenMediaDoesNotExist()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        _mockService
            .Setup(s => s.GetMediaByIdAsync(mediaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaItemResponse?)null);

        // Act
        var result = await _controller.GetMedia(mediaId, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task SearchMedia_ReturnsOk_WithResults()
    {
        // Arrange
        var request = new MediaSearchRequest
        {
            Page = 1,
            PageSize = 10
        };

        var response = new MediaSearchResponse
        {
            Items = new List<MediaItemResponse>(),
            TotalItems = 0,
            Page = 1,
            PageSize = 10,
            TotalPages = 0
        };

        _mockService
            .Setup(s => s.SearchMediaAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.SearchMedia(request, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedResponse = Assert.IsType<MediaSearchResponse>(okResult.Value);
        Assert.Equal(0, returnedResponse.TotalItems);
    }

    [Fact]
    public async Task DeleteMedia_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var mediaId = Guid.NewGuid();
        _mockService
            .Setup(s => s.DeleteMediaAsync(mediaId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteMedia(mediaId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetCollections_ReturnsOk_WithCollections()
    {
        // Arrange
        var collections = new List<MediaCollectionResponse>
        {
            new MediaCollectionResponse
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Collection",
                MediaCount = 5,
                CreatedAt = DateTime.UtcNow.ToString("O"),
                UpdatedAt = DateTime.UtcNow.ToString("O")
            }
        };

        _mockService
            .Setup(s => s.GetAllCollectionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(collections);

        // Act
        var result = await _controller.GetCollections(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCollections = Assert.IsType<List<MediaCollectionResponse>>(okResult.Value);
        Assert.Single(returnedCollections);
    }

    [Fact]
    public async Task GetStats_ReturnsOk_WithStats()
    {
        // Arrange
        var stats = new StorageStats
        {
            TotalSizeBytes = 1024,
            QuotaBytes = 1024 * 1024,
            TotalFiles = 10,
            FilesByType = new Dictionary<MediaType, int>(),
            SizeByType = new Dictionary<MediaType, long>()
        };

        _mockService
            .Setup(s => s.GetStorageStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        var result = await _controller.GetStats(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedStats = Assert.IsType<StorageStats>(okResult.Value);
        Assert.Equal(10, returnedStats.TotalFiles);
    }
}
