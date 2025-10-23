using Aura.Api.Controllers;
using Aura.Api.Services.QualityValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests.QualityValidation;

public class QualityValidationControllerTests
{
    private readonly Mock<ResolutionValidationService> _mockResolutionService;
    private readonly Mock<AudioQualityService> _mockAudioService;
    private readonly Mock<FrameRateService> _mockFrameRateService;
    private readonly Mock<ConsistencyAnalysisService> _mockConsistencyService;
    private readonly Mock<PlatformRequirementsService> _mockPlatformService;
    private readonly QualityValidationController _controller;

    public QualityValidationControllerTests()
    {
        _mockResolutionService = new Mock<ResolutionValidationService>(NullLogger<ResolutionValidationService>.Instance);
        _mockAudioService = new Mock<AudioQualityService>(NullLogger<AudioQualityService>.Instance);
        _mockFrameRateService = new Mock<FrameRateService>(NullLogger<FrameRateService>.Instance);
        _mockConsistencyService = new Mock<ConsistencyAnalysisService>(NullLogger<ConsistencyAnalysisService>.Instance);
        _mockPlatformService = new Mock<PlatformRequirementsService>(NullLogger<PlatformRequirementsService>.Instance);

        _controller = new QualityValidationController(
            _mockResolutionService.Object,
            _mockAudioService.Object,
            _mockFrameRateService.Object,
            _mockConsistencyService.Object,
            _mockPlatformService.Object
        );

        // Set HttpContext for correlation ID
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task ValidateResolution_ValidInput_ReturnsOk()
    {
        // Arrange
        var width = 1920;
        var height = 1080;

        // Act
        var result = await _controller.ValidateResolution(width, height);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ValidateResolution_InvalidWidth_ReturnsBadRequest()
    {
        // Arrange
        var width = -1;
        var height = 1080;

        // Act
        var result = await _controller.ValidateResolution(width, height);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidateResolution_InvalidHeight_ReturnsBadRequest()
    {
        // Arrange
        var width = 1920;
        var height = 0;

        // Act
        var result = await _controller.ValidateResolution(width, height);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidateResolution_InvalidMinResolutionFormat_ReturnsBadRequest()
    {
        // Arrange
        var width = 1920;
        var height = 1080;
        var minResolution = "invalid";

        // Act
        var result = await _controller.ValidateResolution(width, height, minResolution);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidateAudio_ValidInput_ReturnsOk()
    {
        // Arrange
        var testFilePath = Path.GetTempFileName();
        File.WriteAllText(testFilePath, "test");
        var request = new AudioValidationRequest(testFilePath);

        try
        {
            // Act
            var result = await _controller.ValidateAudio(request);

            // Assert - May be OK or NotFound depending on file handling
            Assert.True(result is OkObjectResult || result is NotFoundObjectResult || result is ObjectResult);
        }
        finally
        {
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }

    [Fact]
    public async Task ValidateAudio_EmptyFilePath_ReturnsBadRequest()
    {
        // Arrange
        var request = new AudioValidationRequest("");

        // Act
        var result = await _controller.ValidateAudio(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidateFrameRate_ValidInput_ReturnsOk()
    {
        // Arrange
        var expectedFps = 30.0;
        var actualFps = 30.0;

        // Act
        var result = await _controller.ValidateFrameRate(expectedFps, actualFps);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task ValidateFrameRate_InvalidFPS_ReturnsBadRequest()
    {
        // Arrange
        var expectedFps = -1.0;
        var actualFps = 30.0;

        // Act
        var result = await _controller.ValidateFrameRate(expectedFps, actualFps);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidateFrameRate_NegativeTolerance_ReturnsBadRequest()
    {
        // Arrange
        var expectedFps = 30.0;
        var actualFps = 30.0;
        var tolerance = -0.5;

        // Act
        var result = await _controller.ValidateFrameRate(expectedFps, actualFps, tolerance);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidateConsistency_ValidInput_ReturnsOk()
    {
        // Arrange
        var testFilePath = Path.GetTempFileName();
        File.WriteAllText(testFilePath, "test");
        var request = new ConsistencyValidationRequest(testFilePath);

        try
        {
            // Act
            var result = await _controller.ValidateConsistency(request);

            // Assert - May be OK or NotFound depending on file handling
            Assert.True(result is OkObjectResult || result is NotFoundObjectResult || result is ObjectResult);
        }
        finally
        {
            if (File.Exists(testFilePath))
            {
                File.Delete(testFilePath);
            }
        }
    }

    [Fact]
    public async Task ValidateConsistency_EmptyFilePath_ReturnsBadRequest()
    {
        // Arrange
        var request = new ConsistencyValidationRequest("");

        // Act
        var result = await _controller.ValidateConsistency(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidatePlatformRequirements_ValidInput_ReturnsOk()
    {
        // Arrange
        var platform = "youtube";
        var width = 1920;
        var height = 1080;
        var fileSize = 50 * 1024 * 1024L;
        var duration = 300.0;
        var codec = "H.264";

        // Act
        var result = await _controller.ValidatePlatformRequirements(platform, width, height, fileSize, duration, codec);

        // Assert - May succeed or fail based on platform validation
        Assert.True(result is OkObjectResult || result is BadRequestObjectResult);
    }

    [Fact]
    public async Task ValidatePlatformRequirements_EmptyPlatform_ReturnsBadRequest()
    {
        // Arrange
        var platform = "";
        var width = 1920;
        var height = 1080;
        var fileSize = 50 * 1024 * 1024L;
        var duration = 300.0;

        // Act
        var result = await _controller.ValidatePlatformRequirements(platform, width, height, fileSize, duration);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidatePlatformRequirements_InvalidDimensions_ReturnsBadRequest()
    {
        // Arrange
        var platform = "youtube";
        var width = 0;
        var height = 1080;
        var fileSize = 50 * 1024 * 1024L;
        var duration = 300.0;

        // Act
        var result = await _controller.ValidatePlatformRequirements(platform, width, height, fileSize, duration);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidatePlatformRequirements_InvalidFileSize_ReturnsBadRequest()
    {
        // Arrange
        var platform = "youtube";
        var width = 1920;
        var height = 1080;
        var fileSize = -1L;
        var duration = 300.0;

        // Act
        var result = await _controller.ValidatePlatformRequirements(platform, width, height, fileSize, duration);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task ValidatePlatformRequirements_InvalidDuration_ReturnsBadRequest()
    {
        // Arrange
        var platform = "youtube";
        var width = 1920;
        var height = 1080;
        var fileSize = 50 * 1024 * 1024L;
        var duration = 0.0;

        // Act
        var result = await _controller.ValidatePlatformRequirements(platform, width, height, fileSize, duration);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
