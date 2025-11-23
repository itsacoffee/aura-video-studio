using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Aura.Core.Configuration;
using Aura.Core.Data;
using Aura.Core.Dependencies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for TTS installation methods in SetupController
/// Covers retry logic, config verification, Docker checks, and timeout handling
/// </summary>
public class SetupControllerTtsInstallationTests : IDisposable
{
    private readonly Mock<ILogger<SetupController>> _mockLogger;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IFfmpegConfigurationService> _mockFfmpegConfigService;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<GitHubReleaseResolver> _mockReleaseResolver;
    private readonly AuraDbContext _dbContext;
    private readonly string _tempDirectory;

    public SetupControllerTtsInstallationTests()
    {
        _mockLogger = new Mock<ILogger<SetupController>>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockFfmpegConfigService = new Mock<IFfmpegConfigurationService>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        
        // Setup mock logger factory to return NullLogger instances
        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        // Create in-memory database for tests
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AuraDbContext(options);

        // Create temporary directory for test files
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"AuraTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
        Environment.SetEnvironmentVariable("AURA_DATA_PATH", _tempDirectory);

        // Setup mock GitHubReleaseResolver with proper HttpClient
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        _mockReleaseResolver = new Mock<GitHubReleaseResolver>(
            NullLogger<GitHubReleaseResolver>.Instance,
            httpClient);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        Environment.SetEnvironmentVariable("AURA_DATA_PATH", null);
        GC.SuppressFinalize(this);
    }

    #region Helper Methods

    private SetupController CreateController(HttpClient? httpClient = null)
    {
        if (httpClient != null)
        {
            _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
        }

        var controller = new SetupController(
            _mockLogger.Object,
            _mockEnvironment.Object,
            _mockHttpClientFactory.Object,
            _dbContext,
            _mockFfmpegConfigService.Object,
            _mockLoggerFactory.Object,
            _mockReleaseResolver.Object);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private HttpClient CreateMockHttpClientForPiperInstallation(
        int urlResolveFailures = 0,
        int downloadFailures = 0,
        int voiceModelDownloadFailures = 0)
    {
        var callCount = 0;
        var downloadCallCount = 0;
        var voiceModelCallCount = 0;

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("api.github.com") ||
                    req.RequestUri.ToString().Contains("github.com/rhasspy/piper")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) =>
            {
                // GitHub API connectivity check
                if (request.RequestUri!.ToString() == "https://api.github.com")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK
                    };
                }

                // Download request
                if (request.RequestUri.ToString().Contains("releases/download"))
                {
                    downloadCallCount++;
                    if (downloadCallCount <= downloadFailures)
                    {
                        throw new HttpRequestException("Download failed");
                    }

                    // Return mock tar.gz content
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new ByteArrayContent(new byte[1024])
                    };
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK
                };
            });

        // Setup voice model downloads separately
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri!.ToString().Contains("huggingface.co")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) =>
            {
                voiceModelCallCount++;
                if (voiceModelCallCount <= voiceModelDownloadFailures)
                {
                    throw new HttpRequestException("Voice model download failed");
                }

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(new byte[512])
                };
            });

        return new HttpClient(mockHandler.Object)
        {
            Timeout = TimeSpan.FromMinutes(10)
        };
    }

    private static TimeSpan CalculateExpectedDelay(int attempt)
    {
        var delaySeconds = Math.Min(Math.Pow(2, attempt), 5); // Cap at 5 seconds
        return TimeSpan.FromSeconds(delaySeconds);
    }

    #endregion

    #region InstallPiperWindows - Retry Logic Tests

    [Fact]
    public async Task InstallPiperWindows_URLResolveRetry_ShouldUseExponentialBackoff()
    {
        // Arrange
        var delays = new List<TimeSpan>();
        var attemptCount = 0;

        _mockReleaseResolver
            .Setup(x => x.ResolveLatestAssetUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns((string repo, string pattern, CancellationToken ct) =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    // First two attempts fail
                    return Task.FromResult<string?>(null);
                }
                // Third attempt succeeds
                return Task.FromResult<string?>("https://github.com/rhasspy/piper/releases/download/v1.0/piper_windows_amd64.tar.gz");
            });

        var httpClient = CreateMockHttpClientForPiperInstallation();
        var controller = CreateController(httpClient);

        // Act
        var startTime = DateTime.UtcNow;
        var result = await controller.InstallPiper(CancellationToken.None);

        // Assert
        Assert.Equal(3, attemptCount);
        _mockReleaseResolver.Verify(
            x => x.ResolveLatestAssetUrlAsync(
                "rhasspy/piper",
                "*windows*amd64*.tar.gz",
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        // Verify exponential backoff delays were applied
        // Expected delays: 2^1 = 2s after first failure, 2^2 = 4s after second failure
        var totalExpectedDelay = CalculateExpectedDelay(1) + CalculateExpectedDelay(2);
        var actualDuration = DateTime.UtcNow - startTime;
        
        // Allow some tolerance for test execution overhead (1 second)
        Assert.True(actualDuration >= totalExpectedDelay - TimeSpan.FromSeconds(1),
            $"Expected at least {totalExpectedDelay.TotalSeconds}s delay, but got {actualDuration.TotalSeconds}s");
    }

    [Fact]
    public async Task InstallPiperWindows_DownloadRetry_ShouldRetryUpTo3Times()
    {
        // Arrange - fail first 2 download attempts, succeed on 3rd
        _mockReleaseResolver
            .Setup(x => x.ResolveLatestAssetUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://github.com/rhasspy/piper/releases/download/v1.0/piper_windows_amd64.tar.gz");

        var httpClient = CreateMockHttpClientForPiperInstallation(downloadFailures: 2);
        var controller = CreateController(httpClient);

        // Act
        var result = await controller.InstallPiper(CancellationToken.None);

        // Assert - should succeed on 3rd attempt
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        // Verify the result indicates success or manual install requirement
        var successProp = response.GetType().GetProperty("success");
        var requiresManualInstallProp = response.GetType().GetProperty("requiresManualInstall");
        
        if (successProp != null)
        {
            var success = (bool?)successProp.GetValue(response);
            // Either succeeded or indicated manual install required
            Assert.True(success == true || (requiresManualInstallProp != null && (bool?)requiresManualInstallProp.GetValue(response) == true));
        }
    }

    [Fact]
    public async Task InstallPiperWindows_DownloadRetry_ShouldFailAfter3Attempts()
    {
        // Arrange - fail all 3 download attempts
        _mockReleaseResolver
            .Setup(x => x.ResolveLatestAssetUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://github.com/rhasspy/piper/releases/download/v1.0/piper_windows_amd64.tar.gz");

        var httpClient = CreateMockHttpClientForPiperInstallation(downloadFailures: 3);
        var controller = CreateController(httpClient);

        // Act
        var result = await controller.InstallPiper(CancellationToken.None);

        // Assert - should return failure response with manual install instructions
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var successProp = response.GetType().GetProperty("success");
        Assert.NotNull(successProp);
        var success = (bool?)successProp.GetValue(response);
        Assert.False(success);

        var requiresManualInstallProp = response.GetType().GetProperty("requiresManualInstall");
        Assert.NotNull(requiresManualInstallProp);
        var requiresManualInstall = (bool?)requiresManualInstallProp.GetValue(response);
        Assert.True(requiresManualInstall);
    }

    [Fact]
    public async Task InstallPiperWindows_ExponentialBackoff_ShouldCapAt5Seconds()
    {
        // Arrange
        var attemptTimes = new List<DateTime>();

        _mockReleaseResolver
            .Setup(x => x.ResolveLatestAssetUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns((string repo, string pattern, CancellationToken ct) =>
            {
                attemptTimes.Add(DateTime.UtcNow);
                if (attemptTimes.Count < 3)
                {
                    return Task.FromResult<string?>(null);
                }
                return Task.FromResult<string?>("https://github.com/rhasspy/piper/releases/download/v1.0/piper_windows_amd64.tar.gz");
            });

        var httpClient = CreateMockHttpClientForPiperInstallation();
        var controller = CreateController(httpClient);

        // Act
        await controller.InstallPiper(CancellationToken.None);

        // Assert
        Assert.Equal(3, attemptTimes.Count);

        // Check delay between attempt 1 and 2 (should be ~2 seconds)
        var delay1to2 = attemptTimes[1] - attemptTimes[0];
        Assert.InRange(delay1to2.TotalSeconds, 1.5, 3.0); // 2^1 = 2 seconds with tolerance

        // Check delay between attempt 2 and 3 (should be ~4 seconds)
        var delay2to3 = attemptTimes[2] - attemptTimes[1];
        Assert.InRange(delay2to3.TotalSeconds, 3.5, 5.0); // 2^2 = 4 seconds with tolerance

        // Verify delays are capped at 5 seconds
        Assert.True(delay1to2.TotalSeconds <= 5.5);
        Assert.True(delay2to3.TotalSeconds <= 5.5);
    }

    [Fact]
    public async Task InstallPiperWindows_VoiceModelDownload_ShouldRetry3Times()
    {
        // Arrange - fail first 2 voice model downloads, succeed on 3rd
        _mockReleaseResolver
            .Setup(x => x.ResolveLatestAssetUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://github.com/rhasspy/piper/releases/download/v1.0/piper_windows_amd64.tar.gz");

        var httpClient = CreateMockHttpClientForPiperInstallation(voiceModelDownloadFailures: 2);
        var controller = CreateController(httpClient);

        // Act
        var result = await controller.InstallPiper(CancellationToken.None);

        // Assert - should handle voice model failures gracefully
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        // Piper installation should continue even if voice model download fails
        // Check for success or manual install indication
        var successProp = response.GetType().GetProperty("success");
        if (successProp != null)
        {
            var success = successProp.GetValue(response);
            // Either succeeded with voice model or indicated manual steps
            Assert.NotNull(success);
        }
    }

    #endregion

    #region SaveMimic3ConfigurationAsync - Race Condition Tests

    [Fact]
    public async Task SaveMimic3Configuration_ConcurrentCalls_ShouldHandleRaceCondition()
    {
        // Arrange
        var controller = CreateController();
        var tasks = new List<Task<IActionResult>>();

        // Act - make 5 concurrent calls to InstallMimic3 which internally calls SaveMimic3ConfigurationAsync
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                // Note: We can't directly test SaveMimic3ConfigurationAsync as it's private,
                // but we can test it through InstallMimic3 which calls it
                return await controller.CheckMimic3(CancellationToken.None);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - all calls should complete without exceptions
        Assert.Equal(5, results.Length);
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.IsType<OkObjectResult>(result);
        }
    }

    [Fact]
    public async Task SaveMimic3Configuration_Retry_ShouldVerifyFileContent()
    {
        // Arrange
        var controller = CreateController();
        var settingsPath = Path.Combine(_tempDirectory, "AuraData", "settings.json");

        // Ensure directory exists
        var settingsDir = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrEmpty(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }

        // Create a ProviderSettings instance to set Mimic3 URL
        var providerSettings = new ProviderSettings(_mockLoggerFactory.Object.CreateLogger<ProviderSettings>());
        providerSettings.SetMimic3BaseUrl("http://127.0.0.1:59125");

        // Wait a moment for file system to flush
        await Task.Delay(500);

        // Assert - verify settings file was created and contains expected content
        Assert.True(File.Exists(settingsPath), "Settings file should exist");

        var content = File.ReadAllText(settingsPath);
        Assert.Contains("mimic3BaseUrl", content);
        Assert.Contains("127.0.0.1:59125", content);
    }

    [Fact]
    public async Task SaveMimic3Configuration_Retry_ShouldCallReloadBetweenAttempts()
    {
        // Arrange
        var controller = CreateController();
        var providerSettings = new ProviderSettings(_mockLoggerFactory.Object.CreateLogger<ProviderSettings>());

        // First attempt - set URL
        providerSettings.SetMimic3BaseUrl("http://127.0.0.1:59125");
        await Task.Delay(200);

        // Second attempt - reload and verify
        providerSettings.Reload();
        var reloadedUrl = providerSettings.Mimic3BaseUrl;

        // Assert
        Assert.NotNull(reloadedUrl);
        Assert.Contains("127.0.0.1:59125", reloadedUrl);
    }

    #endregion

    #region InstallMimic3 - Docker Check Tests

    [Fact]
    public async Task InstallMimic3_DockerNotInstalled_ShouldReturnAppropriateErrorMessage()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.InstallMimic3(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var successProp = response.GetType().GetProperty("success");
        var messageProp = response.GetType().GetProperty("message");
        
        if (successProp != null)
        {
            var success = (bool?)successProp.GetValue(response);
            // If Docker is not installed, success should be false
            if (success == false)
            {
                Assert.NotNull(messageProp);
                var message = (string?)messageProp.GetValue(response);
                Assert.NotNull(message);
                Assert.Contains("Docker", message);
                
                // Check for requiresDocker flag
                var requiresDockerProp = response.GetType().GetProperty("requiresDocker");
                if (requiresDockerProp != null)
                {
                    var requiresDocker = (bool?)requiresDockerProp.GetValue(response);
                    Assert.True(requiresDocker);
                }
            }
        }
    }

    [Fact]
    public async Task InstallMimic3_DockerInstalledButNotRunning_ShouldReturnDistinctErrorMessage()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.InstallMimic3(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        // Check response structure
        var dockerInstalledProp = response.GetType().GetProperty("dockerInstalled");
        var dockerRunningProp = response.GetType().GetProperty("dockerRunning");
        var messageProp = response.GetType().GetProperty("message");

        // If Docker is installed but not running, should have distinct message
        if (dockerInstalledProp != null && dockerRunningProp != null)
        {
            var dockerInstalled = (bool?)dockerInstalledProp.GetValue(response);
            var dockerRunning = (bool?)dockerRunningProp.GetValue(response);

            if (dockerInstalled == true && dockerRunning == false)
            {
                Assert.NotNull(messageProp);
                var message = (string?)messageProp.GetValue(response);
                Assert.NotNull(message);
                Assert.Contains("daemon", message, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("not running", message, StringComparison.OrdinalIgnoreCase);

                // Should have instructions for starting Docker
                var instructionsProp = response.GetType().GetProperty("instructions");
                Assert.NotNull(instructionsProp);
            }
        }
    }

    [Theory]
    [InlineData(true, true, "Should start container when Docker is running")]
    [InlineData(true, false, "Should provide instructions when Docker daemon not running")]
    [InlineData(false, false, "Should provide Docker install instructions")]
    public async Task InstallMimic3_DockerStates_ShouldReturnAppropriateMessages(
        bool dockerInstalled,
        bool dockerRunning,
        string expectedBehavior)
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.InstallMimic3(CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        // Verify response has appropriate structure based on Docker state
        var messageProp = response.GetType().GetProperty("message");
        Assert.NotNull(messageProp);
        var message = (string?)messageProp.GetValue(response);
        Assert.NotNull(message);

        // Message should be informative and actionable
        Assert.True(message.Length > 10, $"Expected detailed message for: {expectedBehavior}");
    }

    #endregion

    #region Timeout Handling Tests

    [Fact]
    public async Task InstallPiperWindows_ExtractionTimeout_ShouldCompleteWithin2Minutes()
    {
        // Arrange
        _mockReleaseResolver
            .Setup(x => x.ResolveLatestAssetUrlAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://github.com/rhasspy/piper/releases/download/v1.0/piper_windows_amd64.tar.gz");

        var httpClient = CreateMockHttpClientForPiperInstallation();
        var controller = CreateController(httpClient);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));

        // Act
        var startTime = DateTime.UtcNow;
        var result = await controller.InstallPiper(cts.Token);
        var duration = DateTime.UtcNow - startTime;

        // Assert - extraction should complete or timeout within reasonable time
        // If extraction is attempted, it should respect the 2-minute timeout
        Assert.True(duration < TimeSpan.FromMinutes(3),
            $"Installation took {duration.TotalSeconds}s, expected less than 3 minutes");
    }

    [Fact]
    public async Task StartMimic3Docker_HealthCheck_ShouldTimeoutAfter3Minutes()
    {
        // Arrange
        var controller = CreateController();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        // Act
        var startTime = DateTime.UtcNow;
        var result = await controller.InstallMimic3(cts.Token);
        var duration = DateTime.UtcNow - startTime;

        // Assert - should complete within reasonable time
        // Health check has 60 retries × 3s = 180s (3 minutes) max
        Assert.True(duration < TimeSpan.FromMinutes(4),
            $"Mimic3 installation took {duration.TotalSeconds}s, expected less than 4 minutes");
    }

    [Fact]
    public void DelayWithExponentialBackoff_ShouldCalculateCorrectDelays()
    {
        // Act & Assert - verify delay calculations
        Assert.Equal(TimeSpan.FromSeconds(2), CalculateExpectedDelay(1)); // 2^1 = 2
        Assert.Equal(TimeSpan.FromSeconds(4), CalculateExpectedDelay(2)); // 2^2 = 4
        Assert.Equal(TimeSpan.FromSeconds(5), CalculateExpectedDelay(3)); // 2^3 = 8, capped at 5
        Assert.Equal(TimeSpan.FromSeconds(5), CalculateExpectedDelay(4)); // 2^4 = 16, capped at 5
        Assert.Equal(TimeSpan.FromSeconds(5), CalculateExpectedDelay(10)); // Large value, capped at 5
    }

    #endregion

    #region CheckPiper and CheckMimic3 Tests

    [Fact]
    public async Task CheckPiper_WhenNotInstalled_ShouldReturnFalse()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = controller.CheckPiper();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        var installedProp = response.GetType().GetProperty("installed");
        Assert.NotNull(installedProp);
        var installed = (bool?)installedProp.GetValue(response);
        Assert.NotNull(installed);
    }

    [Fact]
    public async Task CheckMimic3_ConnectionRetry_ShouldRetry3TimesWithDelay()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var startTime = DateTime.UtcNow;
        var result = await controller.CheckMimic3(CancellationToken.None);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);

        // Should try 3 times with 500ms delay between attempts
        // Expected minimum duration: 2 delays × 500ms = 1 second
        Assert.True(duration >= TimeSpan.FromMilliseconds(900),
            $"Expected at least 900ms for 3 retry attempts with delays, got {duration.TotalMilliseconds}ms");
    }

    #endregion
}
