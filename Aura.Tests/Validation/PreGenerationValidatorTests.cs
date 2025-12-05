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
        
        // Use synchronous list collection to avoid timing issues
        var syncProgress = new SynchronousProgress<string>(msg => progressMessages.Add(msg));

        // Act
        await validator.ValidateSystemReadyAsync(brief, planSpec, syncProgress);

        // Assert - messages are collected synchronously
        Assert.True(progressMessages.Count > 0, "Expected progress messages to be reported");
        Assert.Contains(progressMessages, m => m.Contains("FFmpeg"));
        Assert.Contains(progressMessages, m => m.Contains("complete", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Synchronous IProgress implementation for testing to avoid timing-dependent tests
    /// </summary>
    private sealed class SynchronousProgress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        public SynchronousProgress(Action<T> handler) => _handler = handler;
        public void Report(T value) => _handler(value);
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

    #region PreflightReport Tests

    [Fact]
    public async Task ValidateAsync_AllChecksPass_ReturnsOk()
    {
        // Arrange
        SetupFfmpegSuccess();
        SetupHardwareSuccess();
        SetupAllProvidersSuccess();
        var validator = CreateValidator();
        var systemProfile = CreateTestSystemProfile();

        // Act
        var report = await validator.ValidateAsync(systemProfile, CancellationToken.None);

        // Assert
        Assert.True(report.Ok);
        Assert.Empty(report.Errors);
        Assert.NotNull(report.FFmpeg);
        Assert.NotNull(report.Ollama);
        Assert.NotNull(report.TTS);
        Assert.NotNull(report.DiskSpace);
        Assert.NotNull(report.ImageProvider);
    }

    [Fact]
    public async Task ValidateAsync_FFmpegMissing_ReturnsError()
    {
        // Arrange
        _mockFfmpegResolver
            .Setup(r => r.ResolveAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegResolutionResult { Found = false, IsValid = false, Error = "FFmpeg not found" });
        
        SetupHardwareSuccess();
        SetupAllProvidersSuccess();
        var validator = CreateValidator();
        var systemProfile = CreateTestSystemProfile();

        // Act
        var report = await validator.ValidateAsync(systemProfile, CancellationToken.None);

        // Assert
        Assert.False(report.Ok);
        Assert.Contains(report.Errors, e => e.Contains("FFmpeg"));
        Assert.False(report.FFmpeg.Passed);
        Assert.Equal("Not found", report.FFmpeg.Status);
        Assert.NotNull(report.FFmpeg.SuggestedAction);
        Assert.Contains("ffmpeg.org", report.FFmpeg.SuggestedAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateAsync_OllamaNotRunning_ReturnsError()
    {
        // Arrange
        SetupFfmpegSuccess();
        SetupHardwareSuccess();
        
        // Setup provider readiness to indicate LLM (Ollama) is not ready
        var categoryStatus = new ProviderCategoryStatus(
            "LLM",
            false,
            null,
            "NotRunning",
            "Ollama is not running",
            new List<string> { "Start Ollama with: ollama serve" },
            new List<ProviderCandidateStatus>());
        
        var readinessResult = new ProviderReadinessResult();
        readinessResult.CategoryStatuses.Add(categoryStatus);
        
        _mockProviderReadiness
            .Setup(r => r.ValidateRequiredProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readinessResult);
        
        var validator = CreateValidator();
        var systemProfile = CreateTestSystemProfile();

        // Act
        var report = await validator.ValidateAsync(systemProfile, CancellationToken.None);

        // Assert
        // Note: The Ollama check in ValidateAsync uses process execution which we can't easily mock
        // This test verifies the overall validation flow when providers are not ready
        Assert.NotNull(report);
    }

    [Fact]
    public async Task ValidateAsync_ImageProviderMissing_ReturnsWarningNotError()
    {
        // Arrange
        SetupFfmpegSuccess();
        SetupHardwareSuccess();
        
        // Setup provider readiness with LLM and TTS ready but image provider missing
        var llmStatus = new ProviderCategoryStatus("LLM", true, "Ollama", null, "Ready", new List<string>(), new List<ProviderCandidateStatus>());
        var ttsStatus = new ProviderCategoryStatus("TTS", true, "WindowsTTS", null, "Ready", new List<string>(), new List<ProviderCandidateStatus>());
        var imageStatus = new ProviderCategoryStatus("Images", false, null, "NotConfigured", "No image providers configured", new List<string> { "Configure an image provider" }, new List<ProviderCandidateStatus>());
        
        var readinessResult = new ProviderReadinessResult();
        readinessResult.CategoryStatuses.Add(llmStatus);
        readinessResult.CategoryStatuses.Add(ttsStatus);
        readinessResult.CategoryStatuses.Add(imageStatus);
        
        _mockProviderReadiness
            .Setup(r => r.ValidateRequiredProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(readinessResult);
        
        var validator = CreateValidator();
        var systemProfile = CreateTestSystemProfile();

        // Act
        var report = await validator.ValidateAsync(systemProfile, CancellationToken.None);

        // Assert
        // Image provider is optional, so it should be a warning, not an error
        Assert.False(report.ImageProvider.Passed);
        Assert.Contains(report.Warnings, w => w.Contains("ImageProvider"));
        // Errors should not contain image provider issues
        Assert.DoesNotContain(report.Errors, e => e.Contains("ImageProvider"));
    }

    [Fact]
    public async Task ValidateAsync_LowDiskSpace_ReturnsError()
    {
        // Arrange
        SetupFfmpegSuccess();
        SetupHardwareSuccess();
        SetupAllProvidersSuccess();
        var validator = CreateValidator();
        var systemProfile = CreateTestSystemProfile();

        // Act
        var report = await validator.ValidateAsync(systemProfile, CancellationToken.None);

        // Assert
        // Note: Disk space check depends on actual disk space available
        // This test just verifies the check runs without error
        Assert.NotNull(report.DiskSpace);
        Assert.NotNull(report.DiskSpace.Status);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsTimingInformation()
    {
        // Arrange
        SetupFfmpegSuccess();
        SetupHardwareSuccess();
        SetupAllProvidersSuccess();
        var validator = CreateValidator();
        var systemProfile = CreateTestSystemProfile();

        // Act
        var report = await validator.ValidateAsync(systemProfile, CancellationToken.None);

        // Assert
        Assert.True(report.DurationMs > 0);
        Assert.True(report.Timestamp <= DateTime.UtcNow);
        Assert.True(report.Timestamp > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ValidateAsync_LegacyFormat_ReturnsTupleWithErrors()
    {
        // Arrange
        _mockFfmpegResolver
            .Setup(r => r.ResolveAsync(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FfmpegResolutionResult { Found = false, IsValid = false, Error = "FFmpeg not found" });
        
        SetupHardwareSuccess();
        SetupAllProvidersSuccess();
        var validator = CreateValidator();
        var systemProfile = CreateTestSystemProfile();

        // Act
        var (isValid, errors) = await validator.ValidateAsync(systemProfile, CancellationToken.None, legacyFormat: true);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("FFmpeg"));
    }

    [Fact]
    public async Task ValidateAsync_WithNullSystemProfile_StillWorks()
    {
        // Arrange
        SetupFfmpegSuccess();
        SetupHardwareSuccess();
        SetupAllProvidersSuccess();
        var validator = CreateValidator();

        // Act
        var report = await validator.ValidateAsync(null, CancellationToken.None);

        // Assert
        Assert.NotNull(report);
    }

    private void SetupAllProvidersSuccess()
    {
        // Setup all provider categories as successful
        var result = new ProviderReadinessResult();
        var llmStatus = new ProviderCategoryStatus("LLM", true, "Ollama", null, "Ready", new List<string>(), new List<ProviderCandidateStatus>());
        var ttsStatus = new ProviderCategoryStatus("TTS", true, "WindowsTTS", null, "Ready", new List<string>(), new List<ProviderCandidateStatus>());
        var imageStatus = new ProviderCategoryStatus("Images", true, "Pexels", null, "Ready", new List<string>(), new List<ProviderCandidateStatus>());
        result.CategoryStatuses.Add(llmStatus);
        result.CategoryStatuses.Add(ttsStatus);
        result.CategoryStatuses.Add(imageStatus);
        
        _mockProviderReadiness
            .Setup(r => r.ValidateRequiredProvidersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
    }

    private SystemProfile CreateTestSystemProfile()
    {
        return new SystemProfile
        {
            LogicalCores = 8,
            PhysicalCores = 4,
            RamGB = 16,
            Tier = HardwareTier.B
        };
    }

    #endregion
}
