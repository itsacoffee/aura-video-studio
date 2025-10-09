using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class DependencyManagerTests : IDisposable
{
    private readonly ILogger<DependencyManager> _logger;
    private readonly string _testDirectory;
    private readonly string _manifestPath;
    private readonly string _downloadDirectory;

    public DependencyManagerTests()
    {
        _logger = NullLogger<DependencyManager>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-tests-" + Guid.NewGuid().ToString());
        _manifestPath = Path.Combine(_testDirectory, "manifest.json");
        _downloadDirectory = Path.Combine(_testDirectory, "downloads");
        
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_downloadDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task LoadManifestAsync_Should_CreateDefaultManifest_WhenFileDoesNotExist()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Act
        var manifest = await manager.LoadManifestAsync();

        // Assert
        Assert.NotNull(manifest);
        Assert.NotEmpty(manifest.Components);
        Assert.Contains(manifest.Components, c => c.Name == "FFmpeg" && c.IsRequired);
    }

    [Fact]
    public async Task VerifyChecksumAsync_Should_ReturnTrue_ForValidChecksum()
    {
        // Arrange
        var testContent = "Test content for checksum verification";
        var testFilePath = Path.Combine(_downloadDirectory, "test-file.txt");
        await File.WriteAllTextAsync(testFilePath, testContent);

        // Calculate expected SHA256
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(testContent));
        var expectedHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();

        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Act - Use reflection to call private method
        var method = typeof(DependencyManager).GetMethod("VerifyChecksumAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = await (Task<bool>)method!.Invoke(manager, new object[] { testFilePath, expectedHash })!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task VerifyChecksumAsync_Should_ReturnFalse_ForInvalidChecksum()
    {
        // Arrange
        var testContent = "Test content for checksum verification";
        var testFilePath = Path.Combine(_downloadDirectory, "test-file.txt");
        await File.WriteAllTextAsync(testFilePath, testContent);

        var invalidHash = "0000000000000000000000000000000000000000000000000000000000000000";

        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Act - Use reflection to call private method
        var method = typeof(DependencyManager).GetMethod("VerifyChecksumAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = await (Task<bool>)method!.Invoke(manager, new object[] { testFilePath, invalidHash })!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task IsComponentInstalledAsync_Should_ReturnFalse_WhenFilesDoNotExist()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Act
        var isInstalled = await manager.IsComponentInstalledAsync("FFmpeg");

        // Assert
        Assert.False(isInstalled);
    }

    [Fact]
    public async Task VerifyComponentAsync_Should_DetectMissingFiles()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Act
        var result = await manager.VerifyComponentAsync("FFmpeg");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.MissingFiles);
    }

    [Fact]
    public async Task VerifyComponentAsync_Should_DetectCorruptedFiles()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Create a file with wrong content
        var testFilePath = Path.Combine(_downloadDirectory, "ffmpeg.exe");
        await File.WriteAllTextAsync(testFilePath, "corrupted content");

        // Act
        var result = await manager.VerifyComponentAsync("FFmpeg");

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.CorruptedFiles);
    }

    [Fact]
    public async Task RemoveComponentAsync_Should_DeleteComponentFiles()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Create test files
        var testFilePath1 = Path.Combine(_downloadDirectory, "ffmpeg.exe");
        var testFilePath2 = Path.Combine(_downloadDirectory, "ffprobe.exe");
        await File.WriteAllTextAsync(testFilePath1, "test content 1");
        await File.WriteAllTextAsync(testFilePath2, "test content 2");

        // Act
        await manager.RemoveComponentAsync("FFmpeg");

        // Assert
        Assert.False(File.Exists(testFilePath1));
        Assert.False(File.Exists(testFilePath2));
    }

    [Fact]
    public void GetComponentDirectory_Should_ReturnDownloadDirectory()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Act
        var directory = manager.GetComponentDirectory("FFmpeg");

        // Assert
        Assert.Equal(_downloadDirectory, directory);
    }

    [Fact]
    public void GetManualInstallInstructions_Should_ReturnInstructions()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Act
        var instructions = manager.GetManualInstallInstructions("FFmpeg");

        // Assert
        Assert.NotNull(instructions);
        Assert.Equal("FFmpeg", instructions.ComponentName);
        Assert.NotEmpty(instructions.Steps);
    }

    [Fact]
    public void GetManualInstallInstructions_Should_ThrowException_ForInvalidComponent()
    {
        // Arrange
        var httpClient = new HttpClient();
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => manager.GetManualInstallInstructions("InvalidComponent"));
    }

    [Fact]
    public async Task DownloadFileAsync_Should_SupportResume()
    {
        // Arrange
        var testContent = new byte[1024]; // 1KB of test data
        new Random().NextBytes(testContent);
        
        var handlerMock = new Mock<HttpMessageHandler>();
        
        // First call - partial download (512 bytes)
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.PartialContent);
                
                // Check if Range header is present
                if (request.Headers.Range != null)
                {
                    var from = request.Headers.Range.Ranges.First().From ?? 0;
                    var remainingContent = new byte[testContent.Length - from];
                    Array.Copy(testContent, from, remainingContent, 0, remainingContent.Length);
                    response.Content = new ByteArrayContent(remainingContent);
                    response.Content.Headers.ContentLength = remainingContent.Length;
                }
                else
                {
                    response.Content = new ByteArrayContent(testContent);
                    response.Content.Headers.ContentLength = testContent.Length;
                }
                
                return response;
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var manager = new DependencyManager(_logger, httpClient, _manifestPath, _downloadDirectory);

        // Create a partial file (512 bytes)
        var testFilePath = Path.Combine(_downloadDirectory, "test-resume.bin");
        var partialContent = new byte[512];
        Array.Copy(testContent, 0, partialContent, 0, 512);
        await File.WriteAllBytesAsync(testFilePath, partialContent);

        // Act - Use reflection to call private method
        var progress = new Progress<DownloadProgress>();
        var method = typeof(DependencyManager).GetMethod("DownloadFileAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        await (Task)method!.Invoke(manager, new object[] 
        { 
            "http://test.com/file.bin", 
            testFilePath, 
            (long)1024, 
            progress, 
            CancellationToken.None 
        })!;

        // Assert
        Assert.True(File.Exists(testFilePath));
        var downloadedContent = await File.ReadAllBytesAsync(testFilePath);
        Assert.Equal(testContent.Length, downloadedContent.Length);
    }
}
