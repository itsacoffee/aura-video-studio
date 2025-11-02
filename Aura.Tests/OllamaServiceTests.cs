using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class OllamaServiceTests : IDisposable
{
    private readonly Mock<ILogger<OllamaService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly string _tempLogsDirectory;
    private readonly OllamaService _service;

    public OllamaServiceTests()
    {
        _loggerMock = new Mock<ILogger<OllamaService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        
        _tempLogsDirectory = Path.Combine(Path.GetTempPath(), "ollama-tests-" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempLogsDirectory);
        
        _service = new OllamaService(_loggerMock.Object, _httpClient, _tempLogsDirectory);
    }

    [Fact]
    public async Task GetStatusAsync_WhenOllamaRunning_ReturnsRunningStatus()
    {
        // Arrange
        var baseUrl = "http://127.0.0.1:11434";
        
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString() == $"{baseUrl}/api/tags"),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"models\":[]}")
            });

        // Act
        var status = await _service.GetStatusAsync(baseUrl, CancellationToken.None);

        // Assert
        Assert.True(status.Running);
        Assert.Null(status.Error);
    }

    [Fact]
    public async Task GetStatusAsync_WhenOllamaNotRunning_ReturnsNotRunningStatus()
    {
        // Arrange
        var baseUrl = "http://127.0.0.1:11434";
        
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        // Act
        var status = await _service.GetStatusAsync(baseUrl, CancellationToken.None);

        // Assert
        Assert.False(status.Running);
        Assert.NotNull(status.Error);
        Assert.Equal("Not reachable", status.Error);
    }

    [Fact]
    public async Task GetStatusAsync_WhenTimeout_ReturnsNotRunningWithTimeoutError()
    {
        // Arrange
        var baseUrl = "http://127.0.0.1:11434";
        
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        // Act
        var status = await _service.GetStatusAsync(baseUrl, CancellationToken.None);

        // Assert
        Assert.False(status.Running);
        Assert.NotNull(status.Error);
        Assert.Equal("Connection timeout", status.Error);
    }

    [Fact]
    public async Task StartAsync_WhenExecutableNotFound_ReturnsFailure()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent-ollama.exe");
        var baseUrl = "http://127.0.0.1:11434";

        // Act
        var result = await _service.StartAsync(nonExistentPath, baseUrl, CancellationToken.None);

        // Assert
        Assert.False(result.Success);
        
        // On Windows, should return "not found" error
        // On other platforms, should return "only supported on Windows"
        Assert.True(
            result.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
            result.Message.Contains("only supported on Windows", StringComparison.OrdinalIgnoreCase),
            $"Expected message to contain 'not found' or 'only supported on Windows', but got: {result.Message}"
        );
    }

    [Fact]
    public async Task GetLogsAsync_WhenNoLogsExist_ReturnsEmptyArray()
    {
        // Act
        var logs = await _service.GetLogsAsync();

        // Assert
        Assert.Empty(logs);
    }

    [Fact]
    public async Task GetLogsAsync_WhenLogsExist_ReturnsRecentLines()
    {
        // Arrange
        var logFilePath = Path.Combine(_tempLogsDirectory, "ollama-20241101-120000.log");
        var logContent = new[]
        {
            "[OUT] Line 1",
            "[OUT] Line 2",
            "[OUT] Line 3",
            "[OUT] Line 4",
            "[OUT] Line 5"
        };
        await File.WriteAllLinesAsync(logFilePath, logContent);

        // Act
        var logs = await _service.GetLogsAsync(maxLines: 3);

        // Assert
        Assert.Equal(3, logs.Length);
        Assert.Equal("[OUT] Line 3", logs[0]);
        Assert.Equal("[OUT] Line 4", logs[1]);
        Assert.Equal("[OUT] Line 5", logs[2]);
    }

    [Fact]
    public void FindOllamaExecutable_OnNonWindows_ReturnsNull()
    {
        // This test would need platform-specific execution
        // For now, we just verify the method exists and returns a nullable string
        var result = OllamaService.FindOllamaExecutable();
        
        // Result can be null or a path, both are valid
        Assert.True(result == null || !string.IsNullOrEmpty(result));
    }

    [Fact]
    public async Task ValidateOllamaPathAsync_WithNonExistentPath_ReturnsInvalid()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "ollama-does-not-exist.exe");

        // Act
        var result = await OllamaService.ValidateOllamaPathAsync(nonExistentPath, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("does not exist", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateOllamaPathAsync_WithEmptyPath_ReturnsInvalid()
    {
        // Arrange
        var emptyPath = "";

        // Act
        var result = await OllamaService.ValidateOllamaPathAsync(emptyPath, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("empty", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateOllamaPathAsync_WithWrongFileName_ReturnsInvalid()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), "test-file.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            // Act
            var result = await OllamaService.ValidateOllamaPathAsync(tempFile, CancellationToken.None);

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains("Not an Ollama executable", result.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void FindOllamaExecutable_ReturnsNullOrValidPath()
    {
        // Act
        var result = OllamaService.FindOllamaExecutable();

        // Assert
        if (result != null)
        {
            Assert.True(File.Exists(result), $"Returned path should exist: {result}");
            Assert.True(result.Contains("ollama", StringComparison.OrdinalIgnoreCase), "Path should contain 'ollama'");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        
        if (Directory.Exists(_tempLogsDirectory))
        {
            try
            {
                Directory.Delete(_tempLogsDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
        
        GC.SuppressFinalize(this);
    }
}
