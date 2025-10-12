using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class GitHubReleaseResolverTests
{
    private readonly ILogger<GitHubReleaseResolver> _logger;

    public GitHubReleaseResolverTests()
    {
        _logger = NullLogger<GitHubReleaseResolver>.Instance;
    }

    [Fact]
    public async Task ResolveLatestAssetUrlAsync_Should_ReturnMatchingAssetUrl()
    {
        // Arrange
        var mockResponse = new
        {
            tag_name = "v6.0",
            name = "FFmpeg 6.0 Release",
            prerelease = false,
            assets = new[]
            {
                new
                {
                    name = "ffmpeg-n6.0-latest-win64-gpl-6.0.zip",
                    size = 83558400L,
                    browser_download_url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-n6.0-latest-win64-gpl-6.0.zip",
                    content_type = "application/zip"
                },
                new
                {
                    name = "ffmpeg-n6.0-latest-linux64-gpl-6.0.tar.xz",
                    size = 78000000L,
                    browser_download_url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-n6.0-latest-linux64-gpl-6.0.tar.xz",
                    content_type = "application/x-tar"
                }
            }
        };

        var httpClient = CreateMockHttpClient(mockResponse);
        var resolver = new GitHubReleaseResolver(_logger, httpClient);

        // Act
        var result = await resolver.ResolveLatestAssetUrlAsync(
            "BtbN/FFmpeg-Builds", 
            "ffmpeg-*-win64-gpl-*.zip");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("ffmpeg-n6.0-latest-win64-gpl-6.0.zip", result);
    }

    [Fact]
    public async Task ResolveLatestAssetUrlAsync_Should_MatchWildcardPattern()
    {
        // Arrange
        var mockResponse = new
        {
            tag_name = "v0.1.19",
            name = "Ollama Release",
            prerelease = false,
            assets = new[]
            {
                new
                {
                    name = "ollama-windows-amd64.zip",
                    size = 53620736L,
                    browser_download_url = "https://github.com/ollama/ollama/releases/download/v0.1.19/ollama-windows-amd64.zip",
                    content_type = "application/zip"
                },
                new
                {
                    name = "ollama-linux-amd64.tar.gz",
                    size = 51000000L,
                    browser_download_url = "https://github.com/ollama/ollama/releases/download/v0.1.19/ollama-linux-amd64.tar.gz",
                    content_type = "application/gzip"
                }
            }
        };

        var httpClient = CreateMockHttpClient(mockResponse);
        var resolver = new GitHubReleaseResolver(_logger, httpClient);

        // Act
        var result = await resolver.ResolveLatestAssetUrlAsync(
            "ollama/ollama", 
            "ollama-windows-amd64.zip");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://github.com/ollama/ollama/releases/download/v0.1.19/ollama-windows-amd64.zip", result);
    }

    [Fact]
    public async Task ResolveLatestAssetUrlAsync_Should_ReturnNull_WhenNoMatchingAsset()
    {
        // Arrange
        var mockResponse = new
        {
            tag_name = "v1.0",
            name = "Test Release",
            prerelease = false,
            assets = new[]
            {
                new
                {
                    name = "some-other-file.zip",
                    size = 1000L,
                    browser_download_url = "https://example.com/file.zip",
                    content_type = "application/zip"
                }
            }
        };

        var httpClient = CreateMockHttpClient(mockResponse);
        var resolver = new GitHubReleaseResolver(_logger, httpClient);

        // Act
        var result = await resolver.ResolveLatestAssetUrlAsync(
            "owner/repo", 
            "nonexistent-*.zip");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveLatestAssetUrlAsync_Should_ReturnNull_When404()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var resolver = new GitHubReleaseResolver(_logger, httpClient);

        // Act
        var result = await resolver.ResolveLatestAssetUrlAsync(
            "owner/nonexistent-repo", 
            "*.zip");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetLatestReleaseAsync_Should_ReturnReleaseInfo()
    {
        // Arrange
        var mockResponse = new
        {
            tag_name = "v1.2.0",
            name = "Version 1.2.0",
            prerelease = false,
            assets = new[]
            {
                new
                {
                    name = "asset.zip",
                    size = 1000L,
                    browser_download_url = "https://example.com/asset.zip",
                    content_type = "application/zip"
                }
            }
        };

        var httpClient = CreateMockHttpClient(mockResponse);
        var resolver = new GitHubReleaseResolver(_logger, httpClient);

        // Act
        var result = await resolver.GetLatestReleaseAsync("owner/repo");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("v1.2.0", result.TagName);
        Assert.Equal("Version 1.2.0", result.Name);
        Assert.False(result.Prerelease);
        Assert.Single(result.Assets);
        Assert.Equal("asset.zip", result.Assets[0].Name);
    }

    [Fact]
    public async Task ResolveLatestAssetUrlAsync_Should_HandleComplexWildcards()
    {
        // Arrange
        var mockResponse = new
        {
            tag_name = "v1.0",
            name = "Test",
            prerelease = false,
            assets = new[]
            {
                new
                {
                    name = "piper_windows_amd64.zip",
                    size = 50000000L,
                    browser_download_url = "https://github.com/rhasspy/piper/releases/download/v1.2.0/piper_windows_amd64.zip",
                    content_type = "application/zip"
                },
                new
                {
                    name = "piper_linux_x86_64.tar.gz",
                    size = 48000000L,
                    browser_download_url = "https://github.com/rhasspy/piper/releases/download/v1.2.0/piper_linux_x86_64.tar.gz",
                    content_type = "application/gzip"
                }
            }
        };

        var httpClient = CreateMockHttpClient(mockResponse);
        var resolver = new GitHubReleaseResolver(_logger, httpClient);

        // Act - Test with single character wildcard ?
        var result = await resolver.ResolveLatestAssetUrlAsync(
            "rhasspy/piper", 
            "piper_windows_amd??.zip");

        // Assert
        Assert.NotNull(result);
        Assert.Contains("piper_windows_amd64.zip", result);
    }

    private HttpClient CreateMockHttpClient(object responseObject)
    {
        var json = JsonSerializer.Serialize(responseObject);
        
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        return new HttpClient(mockHandler.Object);
    }
}
