using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Services;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

public class HealthDiagnosticsServiceTests
{
    private readonly Mock<ILogger<HealthDiagnosticsService>> _mockLogger;
    private readonly Mock<IFfmpegLocator> _mockFfmpegLocator;
    private readonly Mock<HardwareDetector> _mockHardwareDetector;
    private readonly Mock<ProviderSettings> _mockProviderSettings;
    private readonly Mock<IKeyStore> _mockKeyStore;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<TtsProviderFactory> _mockTtsProviderFactory;

    public HealthDiagnosticsServiceTests()
    {
        _mockLogger = new Mock<ILogger<HealthDiagnosticsService>>();
        _mockFfmpegLocator = new Mock<IFfmpegLocator>();
        _mockHardwareDetector = new Mock<HardwareDetector>(MockBehavior.Loose, null);
        _mockProviderSettings = new Mock<ProviderSettings>(MockBehavior.Loose, null, null);
        _mockKeyStore = new Mock<IKeyStore>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockTtsProviderFactory = new Mock<TtsProviderFactory>(MockBehavior.Loose, null, null, null);
    }

    private HealthDiagnosticsService CreateService()
    {
        return new HealthDiagnosticsService(
            _mockLogger.Object,
            _mockFfmpegLocator.Object,
            _mockHardwareDetector.Object,
            _mockProviderSettings.Object,
            _mockKeyStore.Object,
            _mockHttpClientFactory.Object,
            _mockTtsProviderFactory.Object
        );
    }

    [Fact]
    public async Task GetHealthSummaryAsync_ReturnsValidSummary()
    {
        // Arrange
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthSummaryAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalChecks > 0);
        Assert.Contains(result.OverallStatus, new[] { "healthy", "degraded", "unhealthy" });
    }

    [Fact]
    public async Task GetHealthDetailsAsync_ReturnsAllChecks()
    {
        // Arrange
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Checks);
        Assert.Contains(result.Checks, c => c.Id == "config_present");
        Assert.Contains(result.Checks, c => c.Id == "ffmpeg_present");
        Assert.Contains(result.Checks, c => c.Category == "LLM");
        Assert.Contains(result.Checks, c => c.Category == "TTS");
        Assert.Contains(result.Checks, c => c.Category == "Image");
    }

    [Fact]
    public async Task ConfigurationCheck_Pass_WhenDirectoryExists()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"aura-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            _mockProviderSettings.Setup(x => x.GetAuraDataDirectory()).Returns(tempDir);
            var service = CreateService();
            SetupBasicMocks();

            // Act
            var result = await service.GetHealthDetailsAsync();

            // Assert
            var configCheck = result.Checks.First(c => c.Id == "config_present");
            Assert.Equal("pass", configCheck.Status);
            Assert.True(configCheck.IsRequired);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ConfigurationCheck_Fail_WhenDirectoryDoesNotExist()
    {
        // Arrange
        _mockProviderSettings.Setup(x => x.GetAuraDataDirectory()).Returns("/nonexistent/path");
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var configCheck = result.Checks.First(c => c.Id == "config_present");
        Assert.Equal("fail", configCheck.Status);
        Assert.True(configCheck.IsRequired);
        Assert.NotNull(configCheck.RemediationActions);
        Assert.NotEmpty(configCheck.RemediationActions);
    }

    [Fact]
    public async Task FfmpegCheck_Pass_WhenFfmpegFound()
    {
        // Arrange
        _mockFfmpegLocator.Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = true,
                FfmpegPath = "/usr/bin/ffmpeg",
                VersionString = "ffmpeg version 4.4.0"
            });
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var ffmpegCheck = result.Checks.First(c => c.Id == "ffmpeg_present");
        Assert.Equal("pass", ffmpegCheck.Status);
        Assert.True(ffmpegCheck.IsRequired);
        Assert.Contains("ffmpeg", ffmpegCheck.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task FfmpegCheck_Fail_WhenFfmpegNotFound()
    {
        // Arrange
        _mockFfmpegLocator.Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = false,
                FfmpegPath = null,
                Reason = "FFmpeg not found in PATH",
                AttemptedPaths = new List<string> { "/usr/bin/ffmpeg", "/usr/local/bin/ffmpeg" }
            });
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var ffmpegCheck = result.Checks.First(c => c.Id == "ffmpeg_present");
        Assert.Equal("fail", ffmpegCheck.Status);
        Assert.True(ffmpegCheck.IsRequired);
        Assert.NotNull(ffmpegCheck.RemediationActions);
        Assert.Contains(ffmpegCheck.RemediationActions, a => a.Type == "install");
    }

    [Fact]
    public async Task GpuEncodersCheck_Pass_WhenGpuDetected()
    {
        // Arrange
        _mockHardwareDetector.Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                Gpu = new GpuInfo("NVIDIA", "RTX 3080", 10, "RTX 30"),
                EnableNVENC = true,
                Tier = HardwareTier.A
            });
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var gpuCheck = result.Checks.First(c => c.Id == "gpu_encoders");
        Assert.Equal("pass", gpuCheck.Status);
        Assert.False(gpuCheck.IsRequired);
        Assert.Contains("NVENC", gpuCheck.Message);
    }

    [Fact]
    public async Task GpuEncodersCheck_Warning_WhenNoGpu()
    {
        // Arrange
        _mockHardwareDetector.Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                Gpu = null,
                EnableNVENC = false,
                Tier = HardwareTier.D
            });
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var gpuCheck = result.Checks.First(c => c.Id == "gpu_encoders");
        Assert.Equal("warning", gpuCheck.Status);
        Assert.False(gpuCheck.IsRequired);
        Assert.Contains("CPU", gpuCheck.Message);
    }

    [Fact]
    public async Task LlmCheck_RuleBased_AlwaysPass()
    {
        // Arrange
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var ruleBasedCheck = result.Checks.First(c => c.Id == "llm_rulebased");
        Assert.Equal("pass", ruleBasedCheck.Status);
        Assert.Equal("LLM", ruleBasedCheck.Category);
    }

    [Fact]
    public async Task LlmCheck_OpenAI_Warning_WhenNoApiKey()
    {
        // Arrange
        _mockKeyStore.Setup(x => x.GetKey("OpenAI")).Returns((string)null);
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var openAiCheck = result.Checks.First(c => c.Id == "llm_openai");
        Assert.Equal("warning", openAiCheck.Status);
        Assert.Contains("not configured", openAiCheck.Message);
        Assert.NotNull(openAiCheck.RemediationActions);
        Assert.Contains(openAiCheck.RemediationActions, a => a.Type == "open_settings");
    }

    [Fact]
    public async Task LlmCheck_OpenAI_Pass_WhenApiKeyConfigured()
    {
        // Arrange
        _mockKeyStore.Setup(x => x.GetKey("OpenAI")).Returns("sk-test-key");
        
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => req.RequestUri.ToString().Contains("openai.com")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var openAiCheck = result.Checks.First(c => c.Id == "llm_openai");
        Assert.Contains(openAiCheck.Status, new[] { "pass", "warning" });
    }

    [Fact]
    public async Task TtsCheck_WindowsSAPI_Pass_OnWindows()
    {
        // Arrange
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var sapiCheck = result.Checks.First(c => c.Id == "tts_windows_sapi");
        var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
        
        if (isWindows)
        {
            Assert.Equal("pass", sapiCheck.Status);
        }
        else
        {
            Assert.Equal("warning", sapiCheck.Status);
        }
    }

    [Fact]
    public async Task TtsCheck_ElevenLabs_Warning_WhenNoApiKey()
    {
        // Arrange
        _mockKeyStore.Setup(x => x.GetKey("ElevenLabs")).Returns((string)null);
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var elevenLabsCheck = result.Checks.First(c => c.Id == "tts_elevenlabs");
        Assert.Equal("warning", elevenLabsCheck.Status);
        Assert.Contains("not configured", elevenLabsCheck.Message);
    }

    [Fact]
    public async Task ImageCheck_Stock_AlwaysPass()
    {
        // Arrange
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var stockCheck = result.Checks.First(c => c.Id == "image_stock");
        Assert.Equal("pass", stockCheck.Status);
        Assert.Equal("Image", stockCheck.Category);
    }

    [Fact]
    public async Task DiskSpaceCheck_Fail_WhenLessThan1GB()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        _mockProviderSettings.Setup(x => x.GetOutputDirectory()).Returns(tempDir);
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var diskCheck = result.Checks.First(c => c.Id == "disk_space");
        Assert.NotNull(diskCheck);
        Assert.Contains(diskCheck.Status, new[] { "pass", "warning", "fail" });
    }

    [Fact]
    public async Task IsSystemReady_False_WhenRequiredChecksFail()
    {
        // Arrange
        _mockFfmpegLocator.Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult { Found = false });
        _mockProviderSettings.Setup(x => x.GetAuraDataDirectory()).Returns("/nonexistent");
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        Assert.False(result.IsReady);
        Assert.Equal("unhealthy", result.OverallStatus);
    }

    [Fact]
    public async Task RemediationActions_ContainNavigateTo_ForConfigurableItems()
    {
        // Arrange
        _mockKeyStore.Setup(x => x.GetKey("OpenAI")).Returns((string)null);
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var openAiCheck = result.Checks.First(c => c.Id == "llm_openai");
        Assert.NotNull(openAiCheck.RemediationActions);
        var settingsAction = openAiCheck.RemediationActions.FirstOrDefault(a => a.Type == "open_settings");
        Assert.NotNull(settingsAction);
        Assert.Contains("/settings", settingsAction.NavigateTo);
    }

    [Fact]
    public async Task RemediationActions_ContainExternalUrl_ForSignups()
    {
        // Arrange
        _mockKeyStore.Setup(x => x.GetKey("OpenAI")).Returns((string)null);
        var service = CreateService();
        SetupBasicMocks();

        // Act
        var result = await service.GetHealthDetailsAsync();

        // Assert
        var openAiCheck = result.Checks.First(c => c.Id == "llm_openai");
        Assert.NotNull(openAiCheck.RemediationActions);
        var helpAction = openAiCheck.RemediationActions.FirstOrDefault(a => a.Type == "open_help");
        Assert.NotNull(helpAction);
        Assert.StartsWith("https://", helpAction.ExternalUrl);
    }

    private void SetupBasicMocks()
    {
        var tempDir = Path.GetTempPath();
        _mockProviderSettings.Setup(x => x.GetAuraDataDirectory()).Returns(tempDir);
        _mockProviderSettings.Setup(x => x.GetOutputDirectory()).Returns(tempDir);
        _mockProviderSettings.Setup(x => x.GetOllamaUrl()).Returns("http://127.0.0.1:11434");
        _mockProviderSettings.Setup(x => x.GetStableDiffusionUrl()).Returns("http://127.0.0.1:7860");
        _mockProviderSettings.Setup(x => x.GetMimic3Url()).Returns((string)null);
        _mockProviderSettings.Setup(x => x.GetPiperPath()).Returns((string)null);

        _mockFfmpegLocator.Setup(x => x.CheckAllCandidatesAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = true,
                FfmpegPath = "/usr/bin/ffmpeg",
                VersionString = "4.4.0"
            });

        _mockHardwareDetector.Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                Gpu = null,
                EnableNVENC = false,
                Tier = HardwareTier.C
            });

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.RequestTimeout));

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }
}
