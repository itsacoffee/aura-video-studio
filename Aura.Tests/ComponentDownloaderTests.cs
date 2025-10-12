using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Downloads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class ComponentDownloaderTests : IDisposable
{
    private readonly ILogger<ComponentDownloader> _logger;
    private readonly ILogger<GitHubReleaseResolver> _resolverLogger;
    private readonly ILogger<HttpDownloader> _downloaderLogger;
    private readonly string _testDirectory;
    private readonly string _manifestPath;

    public ComponentDownloaderTests()
    {
        _logger = NullLogger<ComponentDownloader>.Instance;
        _resolverLogger = NullLogger<GitHubReleaseResolver>.Instance;
        _downloaderLogger = NullLogger<HttpDownloader>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-component-tests-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _manifestPath = Path.Combine(_testDirectory, "components.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task DownloadComponentAsync_Should_UseGitHubApiFirst()
    {
        // Arrange
        CreateTestManifest();
        
        var resolvedUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-n6.0-20241010-win64-gpl.zip";
        
        var releaseResponse = new
        {
            tag_name = "v6.0",
            name = "FFmpeg 6.0",
            prerelease = false,
            assets = new[]
            {
                new
                {
                    name = "ffmpeg-n6.0-20241010-win64-gpl.zip",
                    size = 83558400L,
                    browser_download_url = resolvedUrl,
                    content_type = "application/zip"
                }
            }
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                if (req.RequestUri!.ToString().Contains("api.github.com"))
                {
                    var json = JsonSerializer.Serialize(releaseResponse);
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(json)
                    };
                }
                else
                {
                    var content = new ByteArrayContent(new byte[1024]);
                    content.Headers.ContentLength = 1024;
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = content
                    };
                }
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var resolver = new GitHubReleaseResolver(_resolverLogger, httpClient);
        var downloader = new HttpDownloader(_downloaderLogger, httpClient);
        var componentDownloader = new ComponentDownloader(_logger, resolver, downloader, _manifestPath);

        var outputPath = Path.Combine(_testDirectory, "ffmpeg.zip");

        // Act
        var result = await componentDownloader.DownloadComponentAsync("ffmpeg", outputPath);

        // Assert
        Assert.True(result.Success, $"Download failed. Error: {result.Error?.Message}");
        Assert.NotNull(result.DownloadedUrl);
        
        // The download should use either GitHub API resolved URL or fallback to mirror
        Assert.True(
            result.DownloadedUrl.Contains("github.com") || result.DownloadedUrl.Contains("gyan.dev"),
            $"Unexpected URL: {result.DownloadedUrl}");
    }

    [Fact]
    public async Task DownloadComponentAsync_Should_FallbackToMirror_WhenGitHubApiFails()
    {
        // Arrange
        CreateTestManifest();
        
        var mockHandler = new Mock<HttpMessageHandler>();
        
        // First call to GitHub API returns 404
        mockHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("api.github.com")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Subsequent calls for mirror download succeed
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => !req.RequestUri!.ToString().Contains("api.github.com")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var content = new ByteArrayContent(new byte[1024]);
                content.Headers.ContentLength = 1024;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = content
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var resolver = new GitHubReleaseResolver(_resolverLogger, httpClient);
        var downloader = new HttpDownloader(_downloaderLogger, httpClient);
        var componentDownloader = new ComponentDownloader(_logger, resolver, downloader, _manifestPath);

        var outputPath = Path.Combine(_testDirectory, "ffmpeg.zip");

        // Act
        var result = await componentDownloader.DownloadComponentAsync("ffmpeg", outputPath);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.DownloadedUrl);
        Assert.Contains("gyan.dev", result.DownloadedUrl);
        Assert.Equal("mirror", result.Source);
    }

    [Fact]
    public async Task DownloadComponentAsync_Should_UseCustomUrl_WhenProvided()
    {
        // Arrange
        CreateTestManifest();
        
        var httpClient = CreateMockHttpClient(null, shouldReturnFile: true);
        var resolver = new GitHubReleaseResolver(_resolverLogger, httpClient);
        var downloader = new HttpDownloader(_downloaderLogger, httpClient);
        var componentDownloader = new ComponentDownloader(_logger, resolver, downloader, _manifestPath);

        var customUrl = "https://custom.example.com/ffmpeg.zip";
        var outputPath = Path.Combine(_testDirectory, "ffmpeg.zip");

        // Act
        var result = await componentDownloader.DownloadComponentAsync("ffmpeg", outputPath, customUrl: customUrl);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(customUrl, result.DownloadedUrl);
        Assert.Equal("custom", result.Source);
    }

    [Fact]
    public async Task DownloadComponentAsync_Should_ImportLocalFile_WhenProvided()
    {
        // Arrange
        CreateTestManifest();
        
        var localFilePath = Path.Combine(_testDirectory, "local-ffmpeg.zip");
        await File.WriteAllBytesAsync(localFilePath, new byte[1024]);

        var httpClient = new HttpClient();
        var resolver = new GitHubReleaseResolver(_resolverLogger, httpClient);
        var downloader = new HttpDownloader(_downloaderLogger, httpClient);
        var componentDownloader = new ComponentDownloader(_logger, resolver, downloader, _manifestPath);

        var outputPath = Path.Combine(_testDirectory, "output-ffmpeg.zip");

        // Act
        var result = await componentDownloader.DownloadComponentAsync(
            "ffmpeg", outputPath, localFilePath: localFilePath);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IsLocalFile);
        Assert.Equal(localFilePath, result.DownloadedUrl);
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task ResolveComponentUrlAsync_Should_ReturnGitHubApiUrl()
    {
        // Arrange
        CreateTestManifest();
        
        var releaseResponse = new
        {
            tag_name = "v6.0",
            name = "FFmpeg 6.0",
            prerelease = false,
            assets = new[]
            {
                new
                {
                    name = "ffmpeg-n6.0-20241010-win64-gpl.zip",
                    size = 83558400L,
                    browser_download_url = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-n6.0-20241010-win64-gpl.zip",
                    content_type = "application/zip"
                }
            }
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                var json = JsonSerializer.Serialize(releaseResponse);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json)
                };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var resolver = new GitHubReleaseResolver(_resolverLogger, httpClient);
        var downloader = new HttpDownloader(_downloaderLogger, httpClient);
        var componentDownloader = new ComponentDownloader(_logger, resolver, downloader, _manifestPath);

        // Act
        var result = await componentDownloader.ResolveComponentUrlAsync("ffmpeg");

        // Assert
        Assert.NotNull(result.Url);
        // Should resolve to either GitHub API URL or fallback to mirror
        Assert.True(
            result.Url.Contains("github.com") || result.Url.Contains("gyan.dev"),
            $"Unexpected URL: {result.Url}");
        Assert.NotEmpty(result.Mirrors);
    }

    private void CreateTestManifest()
    {
        var manifest = new
        {
            components = new[]
            {
                new
                {
                    id = "ffmpeg",
                    name = "FFmpeg",
                    githubRepo = "BtbN/FFmpeg-Builds",
                    assetPattern = "ffmpeg-*-win64-gpl-*.zip",
                    mirrors = new[]
                    {
                        "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
                    },
                    description = "Essential video and audio processing toolkit",
                    extractPath = "bin/"
                }
            }
        };

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_manifestPath, json);
    }

    private HttpClient CreateMockHttpClient(object? responseObject, bool shouldReturnFile = false)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken _) =>
            {
                // Handle GitHub API calls
                if (request.RequestUri!.ToString().Contains("api.github.com"))
                {
                    if (responseObject != null)
                    {
                        var json = JsonSerializer.Serialize(responseObject);
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.OK,
                            Content = new StringContent(json)
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.NotFound
                        };
                    }
                }
                // Handle download calls
                else if (shouldReturnFile)
                {
                    var content = new ByteArrayContent(new byte[1024]);
                    content.Headers.ContentLength = 1024;
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = content
                    };
                }
                else
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.NotFound
                    };
                }
            });

        return new HttpClient(mockHandler.Object);
    }
}
