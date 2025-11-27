using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Services.Providers;
using Aura.Core.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Validation;

/// <summary>
/// Tests for PreGenerationValidator timeout and progress functionality
/// </summary>
public class PreGenerationValidatorTests
{
    private readonly Mock<ILogger<PreGenerationValidator>> _mockLogger;
    private readonly Mock<IFfmpegLocator> _mockFfmpegLocator;
    private readonly Mock<FFmpegResolver> _mockFfmpegResolver;
    private readonly Mock<IHardwareDetector> _mockHardwareDetector;
    private readonly Mock<IProviderReadinessService> _mockProviderReadiness;

    public PreGenerationValidatorTests()
    {
        _mockLogger = new Mock<ILogger<PreGenerationValidator>>();
        _mockFfmpegLocator = new Mock<IFfmpegLocator>();
        _mockFfmpegResolver = new Mock<FFmpegResolver>(MockBehavior.Loose);
        _mockHardwareDetector = new Mock<IHardwareDetector>();
        _mockProviderReadiness = new Mock<IProviderReadinessService>();
    }

    [Fact]
    public async Task ValidateSystemReadyAsync_WithValidInput_ReturnsSuccess()
    {
        // Arrange
        SetupSuccessfulValidation();
        var validator = CreateValidator();
        var brief = CreateTestBrief();
        var planSpec = CreateTestPlanSpec();

        // Act
        var result = await validator.ValidateSystemReadyAsync(brief, planSpec, progress: null);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ValidateSystemReadyAsync_WithProgress_ReportsProgress()
    {
        // Arrange
        SetupSuccessfulValidation();
        var validator = CreateValidator();
        var brief = CreateTestBrief();
        var planSpec = CreateTestPlanSpec();
        var progressMessages = new List<string>();
        var progress = new Progress<string>(msg => progressMessages.Add(msg));

        // Act
        await validator.ValidateSystemReadyAsync(brief, planSpec, progress);

        // Wait a bit for progress reports to be collected
        await Task.Delay(100);

        // Assert
        Assert.True(progressMessages.Count > 0, "Expected progress messages to be reported");
        Assert.Contains(progressMessages, m => m.Contains("FFmpeg"));
        Assert.Contains(progressMessages, m => m.Contains("complete", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateSystemReadyAsync_WithEmptyTopic_ReturnsIssue()
    {
        // Arrange
        SetupSuccessfulValidation();
        var validator = CreateValidator();
        var brief = new Brief("", "General", "Educational", "Professional", "en", Aspect.Widescreen16x9, null);
        var planSpec = CreateTestPlanSpec();

        // Act
        var result = await validator.ValidateSystemReadyAsync(brief, planSpec, progress: null);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Topic"));
    }

    [Fact]
    public async Task ValidateSystemReadyAsync_WithDurationTooShort_ReturnsIssue()
    {
        // Arrange
        SetupSuccessfulValidation();
        var validator = CreateValidator();
        var brief = CreateTestBrief();
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(5), Pacing.Conversational, Density.Balanced, "informative");

        // Act
        var result = await validator.ValidateSystemReadyAsync(brief, planSpec, progress: null);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Duration") || i.Contains("short"));
    }

    [Fact]
    public async Task ValidateSystemReadyAsync_WhenFFmpegTimesOut_ReturnsTimeoutIssue()
    {
        // Arrange
        var timeoutSettings = new ValidationTimeoutSettings { FfmpegCheckTimeoutSeconds = 1 };
        
        _mockFfmpegResolver
            .Setup(r => r.ResolveAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(async (string? _, bool _, CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return new FfmpegResolutionResult { Found = true, IsValid = true, Path = "/usr/bin/ffmpeg", Source = "PATH" };
            });

        SetupHardwareAndProviders();
        var validator = CreateValidator(timeoutSettings);
        var brief = CreateTestBrief();
        var planSpec = CreateTestPlanSpec();

        // Act
        var result = await validator.ValidateSystemReadyAsync(brief, planSpec, progress: null);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("FFmpeg") && i.Contains("timed out"));
    }

    [Fact]
    public async Task ValidateSystemReadyAsync_WhenProviderTimesOut_ReturnsTimeoutIssue()
    {
        // Arrange
        var timeoutSettings = new ValidationTimeoutSettings { ProviderCheckTimeoutSeconds = 1 };
        
        SetupFfmpegSuccess();
        SetupHardwareSuccess();
        
        _mockProviderReadiness
            .Setup(r => r.ValidateRequiredProvidersAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
                return new ProviderReadinessResult();
            });

        var validator = CreateValidator(timeoutSettings);
        var brief = CreateTestBrief();
        var planSpec = CreateTestPlanSpec();

        // Act
        var result = await validator.ValidateSystemReadyAsync(brief, planSpec, progress: null);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Contains("Provider") && i.Contains("timed out"));
    }

    [Fact]
    public async Task ValidateSystemReadyAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        SetupSuccessfulValidation();
        var validator = CreateValidator();
        var brief = CreateTestBrief();
        var planSpec = CreateTestPlanSpec();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => 
            validator.ValidateSystemReadyAsync(brief, planSpec, null, cts.Token));
    }

    #region Helper Methods

    private PreGenerationValidator CreateValidator(ValidationTimeoutSettings? timeoutSettings = null)
    {
        return new PreGenerationValidator(
            _mockLogger.Object,
            _mockFfmpegLocator.Object,
            _mockFfmpegResolver.Object,
            _mockHardwareDetector.Object,
            _mockProviderReadiness.Object,
            timeoutSettings);
    }

    private void SetupSuccessfulValidation()
    {
        SetupFfmpegSuccess();
        SetupHardwareSuccess();
        SetupProviderSuccess();
    }

    private void SetupFfmpegSuccess()
    {
        _mockFfmpegResolver
            .Setup(r => r.ResolveAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegResolutionResult { Found = true, IsValid = true, Path = "/usr/bin/ffmpeg", Source = "PATH" });
    }

    private void SetupHardwareSuccess()
    {
        _mockHardwareDetector
            .Setup(h => h.DetectSystemAsync())
            .ReturnsAsync(new SystemProfile { LogicalCores = 8, PhysicalCores = 4, RamGB = 16, Tier = HardwareTier.B });
    }

    private void SetupProviderSuccess()
    {
        _mockProviderReadiness
            .Setup(r => r.ValidateRequiredProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderReadinessResult());
    }

    private void SetupHardwareAndProviders()
    {
        SetupHardwareSuccess();
        SetupProviderSuccess();
    }

    private Brief CreateTestBrief()
    {
        return new Brief("Test Topic", "General", "Educational", "Professional", "en", Aspect.Widescreen16x9, null);
    }

    private PlanSpec CreateTestPlanSpec()
    {
        return new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "informative");
    }

    #endregion
}
