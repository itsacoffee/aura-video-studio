using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class EngineDetectorTests : IDisposable
{
    private readonly ILogger<EngineDetector> _logger;
    private readonly string _testDirectory;
    private readonly string _toolsRoot;

    public EngineDetectorTests()
    {
        _logger = NullLogger<EngineDetector>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-detector-tests-" + Guid.NewGuid().ToString());
        _toolsRoot = Path.Combine(_testDirectory, "Tools");
        Directory.CreateDirectory(_toolsRoot);
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
    public async Task DetectOllamaAsync_Should_DetectRunning_WhenApiResponds()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/api/tags")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"models\": []}")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var detector = new EngineDetector(_logger, httpClient, _toolsRoot);

        // Act
        var result = await detector.DetectOllamaAsync();

        // Assert
        Assert.Equal("ollama", result.Id);
        Assert.True(result.IsInstalled);
        Assert.True(result.IsRunning);
    }

    [Fact]
    public async Task DetectOllamaAsync_Should_DetectNotRunning_WhenApiUnreachable()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object);
        var detector = new EngineDetector(_logger, httpClient, _toolsRoot);

        // Act
        var result = await detector.DetectOllamaAsync();

        // Assert
        Assert.Equal("ollama", result.Id);
        Assert.False(result.IsRunning);
    }

    [Fact]
    public async Task DetectStableDiffusionWebUIAsync_Should_DetectRunning_WhenApiResponds()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/sdapi/v1/sd-models")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var detector = new EngineDetector(_logger, httpClient, _toolsRoot);

        // Act
        var result = await detector.DetectStableDiffusionWebUIAsync();

        // Assert
        Assert.Equal("stable-diffusion-webui", result.Id);
        Assert.True(result.IsRunning);
    }

    [Fact]
    public async Task DetectStableDiffusionWebUIAsync_Should_DetectInstalled_WhenFilesExist()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object);
        var detector = new EngineDetector(_logger, httpClient, _toolsRoot);

        // Create installation directory
        var installPath = Path.Combine(_toolsRoot, "stable-diffusion-webui");
        Directory.CreateDirectory(installPath);
        
        var entrypoint = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows) ? "webui-user.bat" : "webui.sh";
        File.WriteAllText(Path.Combine(installPath, entrypoint), "#!/bin/bash\necho 'test'");

        // Act
        var result = await detector.DetectStableDiffusionWebUIAsync();

        // Assert
        Assert.Equal("stable-diffusion-webui", result.Id);
        Assert.True(result.IsInstalled);
        Assert.False(result.IsRunning);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task DetectComfyUIAsync_Should_DetectRunning_WhenApiResponds()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/system_stats")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var detector = new EngineDetector(_logger, httpClient, _toolsRoot);

        // Act
        var result = await detector.DetectComfyUIAsync();

        // Assert
        Assert.Equal("comfyui", result.Id);
        Assert.True(result.IsRunning);
    }

    [Fact]
    public async Task DetectMimic3Async_Should_DetectRunning_WhenApiResponds()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains("/api/voices")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[]")
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var detector = new EngineDetector(_logger, httpClient, _toolsRoot);

        // Act
        var result = await detector.DetectMimic3Async();

        // Assert
        Assert.Equal("mimic3", result.Id);
        Assert.True(result.IsRunning);
    }

    [Fact]
    public async Task DetectAllEnginesAsync_Should_ReturnAllEngines()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(handlerMock.Object);
        var detector = new EngineDetector(_logger, httpClient, _toolsRoot);

        // Act
        var results = await detector.DetectAllEnginesAsync();

        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(6, results.Count); // FFmpeg, Ollama, SD WebUI, ComfyUI, Piper, Mimic3
        Assert.Contains(results, r => r.Id == "ffmpeg");
        Assert.Contains(results, r => r.Id == "ollama");
        Assert.Contains(results, r => r.Id == "stable-diffusion-webui");
        Assert.Contains(results, r => r.Id == "comfyui");
        Assert.Contains(results, r => r.Id == "piper");
        Assert.Contains(results, r => r.Id == "mimic3");
    }

    [Fact]
    public async Task DetectFFmpegAsync_Should_ReturnNotInstalled_WhenNotFound()
    {
        // Arrange
        var httpClient = new HttpClient();
        var detector = new EngineDetector(_logger, httpClient, _toolsRoot);

        // Act
        var result = await detector.DetectFFmpegAsync();

        // Assert
        Assert.Equal("ffmpeg", result.Id);
        Assert.False(result.IsInstalled);
        Assert.NotNull(result.Message);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectPiperAsync_Should_ReturnNotInstalled_WhenNotFound()
    {
        // Arrange
        var httpClient = new HttpClient();
        var detector = new EngineDetector(_logger, httpClient, _toolsRoot);

        // Act
        var result = await detector.DetectPiperAsync();

        // Assert
        Assert.Equal("piper", result.Id);
        Assert.False(result.IsInstalled);
        Assert.NotNull(result.Message);
    }
}
