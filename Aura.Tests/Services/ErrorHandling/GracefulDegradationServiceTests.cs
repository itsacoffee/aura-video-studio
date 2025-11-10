using Aura.Core.Services.ErrorHandling;
using Aura.Core.Errors;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.ErrorHandling;

public class GracefulDegradationServiceTests
{
    private readonly GracefulDegradationService _service;
    private readonly Mock<ILogger<GracefulDegradationService>> _mockLogger;

    public GracefulDegradationServiceTests()
    {
        _mockLogger = new Mock<ILogger<GracefulDegradationService>>();
        _service = new GracefulDegradationService(_mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_PrimarySucceeds_NoFallback()
    {
        // Arrange
        var primaryCalled = false;
        Task<string> PrimaryOperation()
        {
            primaryCalled = true;
            return Task.FromResult("success");
        }

        // Act
        var result = await _service.ExecuteWithFallbackAsync(
            PrimaryOperation,
            new List<FallbackStrategy<string>>(),
            "TestOperation");

        // Assert
        Assert.True(result.Success);
        Assert.False(result.UsedFallback);
        Assert.Equal("success", result.Result);
        Assert.True(primaryCalled);
        Assert.Single(result.AttemptHistory);
        Assert.True(result.AttemptHistory[0].Success);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_PrimaryFails_UsesFallback()
    {
        // Arrange
        var fallbackCalled = false;
        Task<string> PrimaryOperation()
        {
            throw new Exception("Primary failed");
        }

        var fallback = new FallbackStrategy<string>
        {
            Name = "TestFallback",
            Execute = _ =>
            {
                fallbackCalled = true;
                return Task.FromResult("fallback-success");
            },
            IsApplicable = _ => true,
            QualityDegradation = QualityDegradation.Minor,
            UserNotification = "Using fallback"
        };

        // Act
        var result = await _service.ExecuteWithFallbackAsync(
            PrimaryOperation,
            new List<FallbackStrategy<string>> { fallback },
            "TestOperation");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.UsedFallback);
        Assert.Equal("fallback-success", result.Result);
        Assert.True(fallbackCalled);
        Assert.Equal(2, result.AttemptHistory.Count);
        Assert.False(result.AttemptHistory[0].Success); // Primary failed
        Assert.True(result.AttemptHistory[1].Success);  // Fallback succeeded
        Assert.Equal("TestFallback", result.FallbackStrategy);
        Assert.Equal(QualityDegradation.Minor, result.QualityDegradation);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_MultipleFallbacks_UsesFirstApplicable()
    {
        // Arrange
        Task<string> PrimaryOperation()
        {
            throw new FfmpegException(
                "FFmpeg not found",
                FfmpegErrorCategory.NotFound);
        }

        var fallback1 = new FallbackStrategy<string>
        {
            Name = "NetworkFallback",
            Execute = _ => Task.FromResult("network-fallback"),
            IsApplicable = ex => ex.Message.Contains("network"),
            QualityDegradation = QualityDegradation.None
        };

        var fallback2 = new FallbackStrategy<string>
        {
            Name = "FfmpegFallback",
            Execute = _ => Task.FromResult("ffmpeg-fallback"),
            IsApplicable = ex => ex is FfmpegException,
            QualityDegradation = QualityDegradation.Moderate,
            UserNotification = "Using alternative rendering"
        };

        // Act
        var result = await _service.ExecuteWithFallbackAsync(
            PrimaryOperation,
            new List<FallbackStrategy<string>> { fallback1, fallback2 },
            "TestOperation");

        // Assert
        Assert.True(result.Success);
        Assert.True(result.UsedFallback);
        Assert.Equal("ffmpeg-fallback", result.Result);
        Assert.Equal("FfmpegFallback", result.FallbackStrategy);
        Assert.Equal(QualityDegradation.Moderate, result.QualityDegradation);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_AllFallbacksFail_ReturnsFailure()
    {
        // Arrange
        Task<string> PrimaryOperation()
        {
            throw new Exception("Primary failed");
        }

        var fallback = new FallbackStrategy<string>
        {
            Name = "FailingFallback",
            Execute = _ => throw new Exception("Fallback also failed"),
            IsApplicable = _ => true,
            QualityDegradation = QualityDegradation.Minor
        };

        // Act
        var result = await _service.ExecuteWithFallbackAsync(
            PrimaryOperation,
            new List<FallbackStrategy<string>> { fallback },
            "TestOperation");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Result);
        Assert.NotNull(result.Error);
        Assert.Equal(2, result.AttemptHistory.Count);
        Assert.All(result.AttemptHistory, a => Assert.False(a.Success));
    }

    [Fact]
    public void CreateFfmpegFallback_ApplicableForFfmpegErrors()
    {
        // Arrange
        var ffmpegException = FfmpegException.NotFound();
        var otherException = new Exception("Other error");

        var fallback = _service.CreateFfmpegFallback<string>(
            () => Task.FromResult("fallback-result"));

        // Act & Assert
        Assert.True(fallback.IsApplicable(ffmpegException));
        Assert.False(fallback.IsApplicable(otherException));
        Assert.Equal(QualityDegradation.Moderate, fallback.QualityDegradation);
    }

    [Fact]
    public void CreateGpuToCpuFallback_ApplicableForGpuErrors()
    {
        // Arrange
        var gpuException = new Exception("GPU not available");
        var cudaException = new Exception("CUDA initialization failed");
        var nvencException = new Exception("NVENC encoder not found");
        var otherException = new Exception("Other error");

        var fallback = _service.CreateGpuToCpuFallback<string>(
            () => Task.FromResult("cpu-result"));

        // Act & Assert
        Assert.True(fallback.IsApplicable(gpuException));
        Assert.True(fallback.IsApplicable(cudaException));
        Assert.True(fallback.IsApplicable(nvencException));
        Assert.False(fallback.IsApplicable(otherException));
        Assert.Equal(QualityDegradation.Minor, fallback.QualityDegradation);
    }

    [Fact]
    public void CreateProviderFallback_ApplicableForProviderErrors()
    {
        // Arrange
        var providerException = new ProviderException(
            "TestProvider",
            ProviderType.LLM,
            "Provider error");
        var otherException = new Exception("Other error");

        var fallback = _service.CreateProviderFallback<string>(
            () => Task.FromResult("alternative-provider"),
            "AlternativeProvider");

        // Act & Assert
        Assert.True(fallback.IsApplicable(providerException));
        Assert.False(fallback.IsApplicable(otherException));
        Assert.Equal(QualityDegradation.Minor, fallback.QualityDegradation);
    }

    [Fact]
    public void CreateLowQualityFallback_ApplicableForResourceErrors()
    {
        // Arrange
        var resourceException = ResourceException.InsufficientMemory();
        var memoryException = new Exception("Out of memory");
        var diskException = new Exception("Insufficient disk space");
        var otherException = new Exception("Other error");

        var fallback = _service.CreateLowQualityFallback<string>(
            () => Task.FromResult("low-quality"));

        // Act & Assert
        Assert.True(fallback.IsApplicable(resourceException));
        Assert.True(fallback.IsApplicable(memoryException));
        Assert.True(fallback.IsApplicable(diskException));
        Assert.False(fallback.IsApplicable(otherException));
        Assert.Equal(QualityDegradation.Significant, fallback.QualityDegradation);
    }
}
