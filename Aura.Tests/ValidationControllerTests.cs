using Aura.Api.Controllers;
using Aura.Core.Dependencies;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for the ValidationController
/// </summary>
public class ValidationControllerTests
{
    private readonly Mock<IFfmpegLocator> _mockFfmpegLocator;
    private readonly Mock<IHardwareDetector> _mockHardwareDetector;
    private readonly PreGenerationValidator _validator;
    private readonly ValidationController _controller;

    public ValidationControllerTests()
    {
        _mockFfmpegLocator = new Mock<IFfmpegLocator>();
        _mockHardwareDetector = new Mock<IHardwareDetector>();

        _validator = new PreGenerationValidator(
            NullLogger<PreGenerationValidator>.Instance,
            _mockFfmpegLocator.Object,
            _mockHardwareDetector.Object
        );

        _controller = new ValidationController(_validator);

        // Set HttpContext for correlation ID
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    /// <summary>
    /// Helper method to extract validation response properties from anonymous object
    /// </summary>
    private static (bool IsValid, List<string> Issues, int IssueCount) GetValidationResponse(object response)
    {
        var isValid = response.GetType().GetProperty("isValid")?.GetValue(response);
        var issues = response.GetType().GetProperty("issues")?.GetValue(response) as List<string>;
        var issueCount = response.GetType().GetProperty("issueCount")?.GetValue(response);

        Assert.NotNull(isValid);
        Assert.NotNull(issues);
        Assert.NotNull(issueCount);

        return ((bool)isValid, issues, (int)issueCount);
    }

    [Fact]
    public async Task ValidateBrief_WithValidRequest_ReturnsOkWithValidResult()
    {
        // Arrange
        SetupValidSystemMocks();

        var request = new ValidateBriefRequest(
            Topic: "Test Video Topic",
            Audience: "General",
            Goal: "Inform",
            Tone: "Informative",
            Language: "en-US",
            DurationMinutes: 1.0
        );

        // Act
        var result = await _controller.ValidateBrief(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var (isValid, issues, issueCount) = GetValidationResponse(okResult.Value);
        
        Assert.True(isValid);
        Assert.Empty(issues);
        Assert.Equal(0, issueCount);
    }

    [Fact]
    public async Task ValidateBrief_WithMissingTopic_ReturnsInvalidResult()
    {
        // Arrange
        SetupValidSystemMocks();

        var request = new ValidateBriefRequest(
            Topic: null,
            DurationMinutes: 1.0
        );

        // Act
        var result = await _controller.ValidateBrief(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var (isValid, issues, _) = GetValidationResponse(okResult.Value);
        
        Assert.False(isValid);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, i => i.Contains("Topic is required"));
    }

    [Fact]
    public async Task ValidateBrief_WithShortTopic_ReturnsInvalidResult()
    {
        // Arrange
        SetupValidSystemMocks();

        var request = new ValidateBriefRequest(
            Topic: "AB", // Only 2 characters, need at least 3
            DurationMinutes: 1.0
        );

        // Act
        var result = await _controller.ValidateBrief(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var (isValid, issues, _) = GetValidationResponse(okResult.Value);
        
        Assert.False(isValid);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, i => i.Contains("too short"));
    }

    [Fact]
    public async Task ValidateBrief_WithDurationTooShort_ReturnsInvalidResult()
    {
        // Arrange
        SetupValidSystemMocks();

        var request = new ValidateBriefRequest(
            Topic: "Test Topic",
            DurationMinutes: 0.1 // 6 seconds, less than minimum 10 seconds
        );

        // Act
        var result = await _controller.ValidateBrief(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var (isValid, issues, _) = GetValidationResponse(okResult.Value);
        
        Assert.False(isValid);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, i => i.Contains("too short") && i.Contains("10 seconds"));
    }

    [Fact]
    public async Task ValidateBrief_WithDurationTooLong_ReturnsInvalidResult()
    {
        // Arrange
        SetupValidSystemMocks();

        var request = new ValidateBriefRequest(
            Topic: "Test Topic",
            DurationMinutes: 35.0 // More than 30 minutes maximum
        );

        // Act
        var result = await _controller.ValidateBrief(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var (isValid, issues, _) = GetValidationResponse(okResult.Value);
        
        Assert.False(isValid);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, i => i.Contains("too long") && i.Contains("30 minutes"));
    }

    [Fact]
    public async Task ValidateBrief_WithQuickDemoValues_ReturnsValidResult()
    {
        // Arrange - This simulates the Quick Demo request
        SetupValidSystemMocks();

        var request = new ValidateBriefRequest(
            Topic: "Welcome to Aura Video Studio",
            Audience: "General",
            Goal: "Demonstrate",
            Tone: "Informative",
            Language: "en-US",
            DurationMinutes: 0.2 // 12 seconds
        );

        // Act
        var result = await _controller.ValidateBrief(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var (isValid, issues, _) = GetValidationResponse(okResult.Value);
        
        Assert.True(isValid);
        Assert.Empty(issues);
    }

    [Fact]
    public async Task ValidateBrief_WithoutFfmpeg_ReturnsInvalidResult()
    {
        // Arrange
        _mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = false,
                FfmpegPath = null,
                AttemptedPaths = new List<string>()
            });

        SetupValidHardwareMocks();

        var request = new ValidateBriefRequest(
            Topic: "Test Topic",
            DurationMinutes: 1.0
        );

        // Act
        var result = await _controller.ValidateBrief(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var (isValid, issues, _) = GetValidationResponse(okResult.Value);
        
        Assert.False(isValid);
        Assert.NotEmpty(issues);
        Assert.Contains(issues, i => i.Contains("FFmpeg"));
    }

    [Fact]
    public async Task ValidateBrief_ResponseIncludesIssueCount()
    {
        // Arrange
        SetupValidSystemMocks();

        var request = new ValidateBriefRequest(
            Topic: null, // Missing topic
            DurationMinutes: 0.05 // Too short
        );

        // Act
        var result = await _controller.ValidateBrief(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var (isValid, issues, issueCount) = GetValidationResponse(okResult.Value);
        
        Assert.False(isValid);
        Assert.NotEmpty(issues);
        Assert.Equal(issues.Count, issueCount);
        Assert.True(issueCount >= 2); // At least topic and duration issues
    }

    private void SetupValidSystemMocks()
    {
        // Mock FFmpeg as found
        _mockFfmpegLocator
            .Setup(x => x.CheckAllCandidatesAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegValidationResult
            {
                Found = true,
                FfmpegPath = "/usr/bin/ffmpeg",
                AttemptedPaths = new List<string>()
            });

        SetupValidHardwareMocks();
    }

    private void SetupValidHardwareMocks()
    {
        // Mock hardware as sufficient
        _mockHardwareDetector
            .Setup(x => x.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile
            {
                LogicalCores = 4,
                PhysicalCores = 2,
                RamGB = 8
            });
    }
}
