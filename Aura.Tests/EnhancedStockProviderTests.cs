using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.StockMedia;
using Aura.Providers.Images;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class EnhancedStockProviderTests
{
    #region EnhancedPexelsProvider Tests

    [Fact]
    public void EnhancedPexelsProvider_Should_Support_Video()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new EnhancedPexelsProvider(
            NullLogger<EnhancedPexelsProvider>.Instance,
            httpClient,
            apiKey: "test-key");

        // Assert
        Assert.True(provider.SupportsVideo);
        Assert.Equal(StockMediaProvider.Pexels, provider.ProviderName);
    }

    [Fact]
    public async Task EnhancedPexelsProvider_Should_Return_Empty_Without_ApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new EnhancedPexelsProvider(
            NullLogger<EnhancedPexelsProvider>.Instance,
            httpClient,
            apiKey: null);

        var request = new StockMediaSearchRequest
        {
            Query = "test",
            Type = StockMediaType.Image,
            Count = 10
        };

        // Act
        var results = await provider.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void EnhancedPexelsProvider_Should_Return_RateLimitStatus()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new EnhancedPexelsProvider(
            NullLogger<EnhancedPexelsProvider>.Instance,
            httpClient,
            apiKey: "test-key");

        // Act
        var status = provider.GetRateLimitStatus();

        // Assert
        Assert.Equal(StockMediaProvider.Pexels, status.Provider);
        Assert.Equal(200, status.RequestsRemaining);
        Assert.Equal(200, status.RequestsLimit);
        Assert.False(status.IsLimited);
    }

    #endregion

    #region EnhancedUnsplashProvider Tests

    [Fact]
    public void EnhancedUnsplashProvider_Should_Not_Support_Video()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new EnhancedUnsplashProvider(
            NullLogger<EnhancedUnsplashProvider>.Instance,
            httpClient,
            apiKey: "test-key");

        // Assert
        Assert.False(provider.SupportsVideo);
        Assert.Equal(StockMediaProvider.Unsplash, provider.ProviderName);
    }

    [Fact]
    public async Task EnhancedUnsplashProvider_Should_Return_Empty_Without_ApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new EnhancedUnsplashProvider(
            NullLogger<EnhancedUnsplashProvider>.Instance,
            httpClient,
            apiKey: null);

        var request = new StockMediaSearchRequest
        {
            Query = "test",
            Type = StockMediaType.Image,
            Count = 10
        };

        // Act
        var results = await provider.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region EnhancedPixabayProvider Tests

    [Fact]
    public void EnhancedPixabayProvider_Should_Support_Video()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new EnhancedPixabayProvider(
            NullLogger<EnhancedPixabayProvider>.Instance,
            httpClient,
            apiKey: "test-key");

        // Assert
        Assert.True(provider.SupportsVideo);
        Assert.Equal(StockMediaProvider.Pixabay, provider.ProviderName);
    }

    [Fact]
    public async Task EnhancedPixabayProvider_Should_Return_Empty_Without_ApiKey()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new EnhancedPixabayProvider(
            NullLogger<EnhancedPixabayProvider>.Instance,
            httpClient,
            apiKey: null);

        var request = new StockMediaSearchRequest
        {
            Query = "test",
            Type = StockMediaType.Image,
            Count = 10
        };

        // Act
        var results = await provider.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region Mock Response Tests

    [Fact]
    public async Task EnhancedPexelsProvider_Should_Parse_Image_Results()
    {
        // Arrange
        var mockResponse = @"{
            ""photos"": [
                {
                    ""id"": 12345,
                    ""width"": 1920,
                    ""height"": 1080,
                    ""photographer"": ""John Doe"",
                    ""photographer_url"": ""https://pexels.com/john"",
                    ""src"": {
                        ""tiny"": ""https://example.com/tiny.jpg"",
                        ""medium"": ""https://example.com/medium.jpg"",
                        ""large2x"": ""https://example.com/large.jpg""
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
                Content = new StringContent(mockResponse, Encoding.UTF8, "application/json")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var provider = new EnhancedPexelsProvider(
            NullLogger<EnhancedPexelsProvider>.Instance,
            httpClient,
            apiKey: "test-key");

        var request = new StockMediaSearchRequest
        {
            Query = "nature",
            Type = StockMediaType.Image,
            Count = 10
        };

        // Act
        var results = await provider.SearchAsync(request, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("12345", results[0].Id);
        Assert.Equal(StockMediaType.Image, results[0].Type);
        Assert.Equal(StockMediaProvider.Pexels, results[0].Provider);
        Assert.Equal(1920, results[0].Width);
        Assert.Equal(1080, results[0].Height);
        Assert.True(results[0].Licensing.CommercialUseAllowed);
        Assert.False(results[0].Licensing.AttributionRequired);
        Assert.Equal("John Doe", results[0].Licensing.CreatorName);
    }

    #endregion
}
