using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Downloads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class HttpDownloaderTests : IDisposable
{
    private readonly ILogger<HttpDownloader> _logger;
    private readonly string _testDirectory;

    public HttpDownloaderTests()
    {
        _logger = NullLogger<HttpDownloader>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-downloader-tests-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task DownloadFileAsync_Should_DownloadFile_Successfully()
    {
        // Arrange
        var testContent = new byte[1024];
        new Random().NextBytes(testContent);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(testContent)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new HttpDownloader(_logger, httpClient);
        var outputPath = Path.Combine(_testDirectory, "test-file.bin");

        // Act
        var success = await downloader.DownloadFileAsync("http://test.com/file.bin", outputPath);

        // Assert
        Assert.True(success);
        Assert.True(File.Exists(outputPath));
        var downloadedContent = await File.ReadAllBytesAsync(outputPath);
        Assert.Equal(testContent.Length, downloadedContent.Length);
    }

    [Fact]
    public async Task DownloadFileAsync_Should_ResumeDownload_WhenPartialFileExists()
    {
        // Arrange
        var testContent = new byte[2048];
        new Random().NextBytes(testContent);
        var partialSize = 1024;

        var outputPath = Path.Combine(_testDirectory, "test-resume.bin");
        var partialPath = outputPath + ".partial";

        // Create partial file
        await File.WriteAllBytesAsync(partialPath, testContent.Take(partialSize).ToArray());

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Headers.Range != null && 
                    req.Headers.Range.Ranges.First().From == partialSize),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                var remainingContent = testContent.Skip(partialSize).ToArray();
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.PartialContent,
                    Content = new ByteArrayContent(remainingContent)
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new HttpDownloader(_logger, httpClient);

        // Act
        var success = await downloader.DownloadFileAsync("http://test.com/file.bin", outputPath);

        // Assert
        Assert.True(success);
        Assert.True(File.Exists(outputPath));
        Assert.False(File.Exists(partialPath)); // Partial file should be moved to final location
        var downloadedContent = await File.ReadAllBytesAsync(outputPath);
        Assert.Equal(testContent.Length, downloadedContent.Length);
    }

    [Fact]
    public async Task DownloadFileAsync_Should_VerifyChecksum_WhenProvided()
    {
        // Arrange
        var testContent = new byte[1024];
        new Random().NextBytes(testContent);

        // Calculate expected SHA256
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(testContent);
        var expectedSha256 = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(testContent)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new HttpDownloader(_logger, httpClient);
        var outputPath = Path.Combine(_testDirectory, "test-checksum.bin");

        // Act
        var success = await downloader.DownloadFileAsync("http://test.com/file.bin", outputPath, expectedSha256);

        // Assert
        Assert.True(success);
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task DownloadFileAsync_Should_ReturnFalse_WhenChecksumMismatch()
    {
        // Arrange
        var testContent = new byte[1024];
        new Random().NextBytes(testContent);
        var wrongSha256 = "0000000000000000000000000000000000000000000000000000000000000000";

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(testContent)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new HttpDownloader(_logger, httpClient);
        var outputPath = Path.Combine(_testDirectory, "test-bad-checksum.bin");

        // Act
        var success = await downloader.DownloadFileAsync("http://test.com/file.bin", outputPath, wrongSha256);

        // Assert
        Assert.False(success);
        Assert.False(File.Exists(outputPath)); // File should not be created if checksum fails
    }

    [Fact]
    public async Task DownloadFileAsync_Should_RetryOnFailure()
    {
        // Arrange
        var testContent = new byte[1024];
        new Random().NextBytes(testContent);
        var callCount = 0;

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    // First call fails
                    throw new HttpRequestException("Network error");
                }
                // Second call succeeds
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(testContent)
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new HttpDownloader(_logger, httpClient);
        var outputPath = Path.Combine(_testDirectory, "test-retry.bin");

        // Act
        var success = await downloader.DownloadFileAsync("http://test.com/file.bin", outputPath);

        // Assert
        Assert.True(success);
        Assert.True(callCount >= 2); // Should have retried
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public async Task DownloadFileAsync_Should_ReportProgress()
    {
        // Arrange
        var testContent = new byte[1024 * 100]; // 100KB
        new Random().NextBytes(testContent);

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(testContent)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new HttpDownloader(_logger, httpClient);
        var outputPath = Path.Combine(_testDirectory, "test-progress.bin");

        var progressReports = new System.Collections.Generic.List<HttpDownloadProgress>();
        var progress = new Progress<HttpDownloadProgress>(p => progressReports.Add(p));

        // Act
        var success = await downloader.DownloadFileAsync("http://test.com/file.bin", outputPath, null, progress);

        // Assert
        Assert.True(success);
        Assert.NotEmpty(progressReports);
        Assert.True(progressReports.Any(p => p.PercentComplete > 0));
    }

    [Fact]
    public async Task DownloadFileAsync_Should_HandleCancellation()
    {
        // Arrange
        var testContent = new byte[1024 * 1024 * 10]; // 10MB
        new Random().NextBytes(testContent);

        var cts = new CancellationTokenSource();
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                cts.Cancel(); // Cancel during download
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(testContent)
                };
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var downloader = new HttpDownloader(_logger, httpClient);
        var outputPath = Path.Combine(_testDirectory, "test-cancel.bin");

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await downloader.DownloadFileAsync("http://test.com/file.bin", outputPath, null, null, cts.Token);
        });
    }
}
