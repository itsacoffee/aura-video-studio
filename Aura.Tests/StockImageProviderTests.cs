using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Providers.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class StockImageProviderTests
{
    #region PexelsImageProvider Tests

    [Fact]
    public void PexelsImageProvider_Should_Validate_ApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new PexelsImageProvider(
            NullLogger<PexelsImageProvider>.Instance,
            httpClient,
            apiKey: null);

        // Act
        var isValid = provider.ValidateApiKey(out var error);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("not configured", error);
    }

    [Fact]
    public void PexelsImageProvider_Should_Pass_ApiKey_Validation()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new PexelsImageProvider(
            NullLogger<PexelsImageProvider>.Instance,
            httpClient,
            apiKey: "test-api-key");

        // Act
        var isValid = provider.ValidateApiKey(out var error);

        // Assert
        Assert.True(isValid);
        Assert.Null(error);
    }

    [Fact]
    public async Task PexelsImageProvider_Should_Return_Empty_Without_ApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new PexelsImageProvider(
            NullLogger<PexelsImageProvider>.Instance,
            httpClient,
            apiKey: null);

        // Act
        var results = await provider.SearchAsync("test", 10, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task PexelsImageProvider_Should_Return_Empty_On_RateLimit()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = (HttpStatusCode)429,
                Content = new StringContent("Rate limit exceeded")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new PexelsImageProvider(
            NullLogger<PexelsImageProvider>.Instance,
            httpClient,
            apiKey: "test-api-key");

        // Act
        // The provider will retry 3 times then return empty results
        var results = await provider.SearchAsync("test", 10, CancellationToken.None);
        
        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task PexelsImageProvider_Should_Parse_Search_Results()
    {
        // Arrange
        var responseJson = @"{
            ""photos"": [
                {
                    ""id"": 1,
                    ""width"": 1920,
                    ""height"": 1080,
                    ""photographer"": ""John Doe"",
                    ""src"": {
                        ""original"": ""https://example.com/original.jpg"",
                        ""large2x"": ""https://example.com/large2x.jpg"",
                        ""medium"": ""https://example.com/medium.jpg"",
                        ""small"": ""https://example.com/small.jpg""
                    }
                }
            ]
        }";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new PexelsImageProvider(
            NullLogger<PexelsImageProvider>.Instance,
            httpClient,
            apiKey: "test-api-key");

        // Act
        var results = await provider.SearchAsync("test", 10, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("image", results[0].Kind);
        Assert.Contains("Pexels", results[0].License);
    }

    #endregion

    #region PixabayImageProvider Tests

    [Fact]
    public void PixabayImageProvider_Should_Validate_ApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new PixabayImageProvider(
            NullLogger<PixabayImageProvider>.Instance,
            httpClient,
            apiKey: null);

        // Act
        var isValid = provider.ValidateApiKey(out var error);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("not configured", error);
    }

    [Fact]
    public async Task PixabayImageProvider_Should_Parse_Image_Results()
    {
        // Arrange
        var responseJson = @"{
            ""hits"": [
                {
                    ""id"": 1,
                    ""largeImageURL"": ""https://example.com/large.jpg"",
                    ""webformatURL"": ""https://example.com/web.jpg"",
                    ""previewURL"": ""https://example.com/preview.jpg"",
                    ""imageWidth"": 1920,
                    ""imageHeight"": 1080,
                    ""user"": ""testuser"",
                    ""user_id"": 123
                }
            ]
        }";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new PixabayImageProvider(
            NullLogger<PixabayImageProvider>.Instance,
            httpClient,
            apiKey: "test-api-key");

        // Act
        var results = await provider.SearchAsync("test", 10, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("image", results[0].Kind);
        Assert.Contains("Pixabay", results[0].License);
    }

    [Fact]
    public async Task PixabayImageProvider_Should_Search_Videos()
    {
        // Arrange
        var responseJson = @"{
            ""hits"": [
                {
                    ""id"": 1,
                    ""user"": ""creator"",
                    ""user_id"": 456,
                    ""videos"": {
                        ""large"": {
                            ""url"": ""https://example.com/video-large.mp4""
                        }
                    }
                }
            ]
        }";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new PixabayImageProvider(
            NullLogger<PixabayImageProvider>.Instance,
            httpClient,
            apiKey: "test-api-key");

        // Act
        var results = await provider.SearchVideosAsync("test", 10, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("video", results[0].Kind);
        Assert.Contains("Pixabay", results[0].License);
    }

    #endregion

    #region UnsplashImageProvider Tests

    [Fact]
    public void UnsplashImageProvider_Should_Validate_ApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new UnsplashImageProvider(
            NullLogger<UnsplashImageProvider>.Instance,
            httpClient,
            apiKey: null);

        // Act
        var isValid = provider.ValidateApiKey(out var error);

        // Assert
        Assert.False(isValid);
        Assert.NotNull(error);
        Assert.Contains("not configured", error);
    }

    [Fact]
    public async Task UnsplashImageProvider_Should_Parse_Search_Results()
    {
        // Arrange
        var responseJson = @"{
            ""results"": [
                {
                    ""id"": ""abc123"",
                    ""urls"": {
                        ""regular"": ""https://example.com/photo.jpg""
                    },
                    ""user"": {
                        ""name"": ""Jane Smith""
                    },
                    ""width"": 3000,
                    ""height"": 2000,
                    ""links"": {
                        ""download_location"": ""https://api.unsplash.com/photos/abc123/download""
                    }
                }
            ]
        }";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
                Headers = { { "X-Ratelimit-Remaining", "45" }, { "X-Ratelimit-Limit", "50" } }
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new UnsplashImageProvider(
            NullLogger<UnsplashImageProvider>.Instance,
            httpClient,
            apiKey: "test-api-key");

        // Act
        var results = await provider.SearchAsync("test", 10, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("image", results[0].Kind);
        Assert.Contains("Unsplash", results[0].License);
        Assert.Contains("tracking", results[0].Attribution);
    }

    [Fact]
    public void UnsplashImageProvider_Should_Track_Quota()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new UnsplashImageProvider(
            NullLogger<UnsplashImageProvider>.Instance,
            httpClient,
            apiKey: "test-api-key");

        // Act
        var (remaining, limit) = provider.GetQuotaStatus();

        // Assert - default values before any API calls
        Assert.Equal(50, remaining);
        Assert.Equal(50, limit);
    }

    #endregion
}
