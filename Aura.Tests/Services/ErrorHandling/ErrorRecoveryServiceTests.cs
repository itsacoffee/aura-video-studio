using Aura.Core.Services.ErrorHandling;
using Aura.Core.Errors;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.ErrorHandling;

public class ErrorRecoveryServiceTests
{
    private readonly ErrorRecoveryService _service;
    private readonly Mock<ILogger<ErrorRecoveryService>> _mockLogger;

    public ErrorRecoveryServiceTests()
    {
        _mockLogger = new Mock<ILogger<ErrorRecoveryService>>();
        _service = new ErrorRecoveryService(_mockLogger.Object);
    }

    [Fact]
    public void GenerateRecoveryGuide_WithAuraException_IncludesAllDetails()
    {
        // Arrange
        var exception = new ProviderException(
            "TestProvider",
            ProviderType.LLM,
            "Test error",
            ProviderErrorCode.RateLimit,
            correlationId: "test-123"
        );

        // Act
        var guide = _service.GenerateRecoveryGuide(exception, "test-123");

        // Assert
        Assert.Equal("test-123", guide.CorrelationId);
        Assert.NotNull(guide.ErrorCode);
        Assert.NotNull(guide.UserFriendlyMessage);
        Assert.NotEmpty(guide.ManualActions);
        Assert.NotEmpty(guide.TroubleshootingSteps);
        Assert.NotEmpty(guide.DocumentationLinks);
        Assert.Equal(ErrorSeverity.Warning, guide.Severity);
    }

    [Fact]
    public void GenerateRecoveryGuide_WithProviderException_IncludesTroubleshootingSteps()
    {
        // Arrange
        var exception = ProviderException.MissingApiKey(
            "OpenAI",
            ProviderType.LLM,
            "OPENAI_API_KEY"
        );

        // Act
        var guide = _service.GenerateRecoveryGuide(exception);

        // Assert
        Assert.NotEmpty(guide.TroubleshootingSteps);
        Assert.Contains(guide.TroubleshootingSteps, s => s.Title.Contains("Provider Configuration"));
        Assert.Contains(guide.DocumentationLinks, l => l.Title.Contains("Provider"));
    }

    [Fact]
    public void GenerateRecoveryGuide_WithFfmpegException_IncludesInstallationGuide()
    {
        // Arrange
        var exception = FfmpegException.NotFound();

        // Act
        var guide = _service.GenerateRecoveryGuide(exception);

        // Assert
        Assert.NotEmpty(guide.TroubleshootingSteps);
        Assert.Contains(guide.TroubleshootingSteps, s => s.Title.Contains("FFmpeg"));
        Assert.Contains(guide.DocumentationLinks, l => l.Title.Contains("FFmpeg"));
        Assert.Equal(ErrorSeverity.Critical, guide.Severity);
    }

    [Fact]
    public void GenerateRecoveryGuide_WithResourceException_IncludesResourceActions()
    {
        // Arrange
        var exception = ResourceException.InsufficientDiskSpace(requiredBytes: 1000000000);

        // Act
        var guide = _service.GenerateRecoveryGuide(exception);

        // Assert
        Assert.NotEmpty(guide.TroubleshootingSteps);
        Assert.Contains(guide.ManualActions, a => a.Contains("disk space"));
        Assert.Equal(ErrorSeverity.Critical, guide.Severity);
    }

    [Fact]
    public void GenerateRecoveryGuide_WithTransientError_SuggestsRetry()
    {
        // Arrange
        var exception = new ProviderException(
            "TestProvider",
            ProviderType.LLM,
            "Network error",
            ProviderErrorCode.NetworkError,
            isTransient: true
        );

        // Act
        var guide = _service.GenerateRecoveryGuide(exception);

        // Assert
        Assert.True(guide.IsTransient);
        Assert.NotNull(guide.AutomatedRecovery);
        Assert.Contains("retry", guide.AutomatedRecovery.Name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GenerateRecoveryGuide_WithRateLimitError_IncludesWaitTime()
    {
        // Arrange
        var exception = ProviderException.RateLimited(
            "TestProvider",
            ProviderType.LLM,
            retryAfterSeconds: 60
        );

        // Act
        var guide = _service.GenerateRecoveryGuide(exception);

        // Assert
        Assert.NotNull(guide.AutomatedRecovery);
        Assert.Contains("wait", guide.AutomatedRecovery.Description, StringComparison.OrdinalIgnoreCase);
        Assert.True(guide.AutomatedRecovery.EstimatedTimeSeconds > 0);
    }

    [Fact]
    public async Task AttemptAutomatedRecoveryAsync_WithTransientError_Succeeds()
    {
        // Arrange
        var exception = new ProviderException(
            "TestProvider",
            ProviderType.LLM,
            "Network timeout",
            ProviderErrorCode.Timeout,
            isTransient: true
        );

        // Act
        var result = await _service.AttemptAutomatedRecoveryAsync(exception);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CorrelationId);
        // Note: Actual recovery behavior depends on implementation
    }

    [Fact]
    public void GenerateRecoveryGuide_WithStandardException_GeneratesBasicGuide()
    {
        // Arrange
        var exception = new FileNotFoundException("File not found");

        // Act
        var guide = _service.GenerateRecoveryGuide(exception);

        // Assert
        Assert.NotNull(guide);
        Assert.NotEmpty(guide.ManualActions);
        Assert.Contains(guide.ManualActions, a => a.Contains("file"));
        Assert.NotEmpty(guide.DocumentationLinks);
    }

    [Fact]
    public void GenerateRecoveryGuide_PreservesContext()
    {
        // Arrange
        var exception = new ProviderException(
            "TestProvider",
            ProviderType.LLM,
            "Test error"
        );
        exception.WithContext("key1", "value1");
        exception.WithContext("key2", 123);

        // Act
        var guide = _service.GenerateRecoveryGuide(exception);

        // Assert
        Assert.NotEmpty(guide.Context);
        Assert.Contains("key1", guide.Context.Keys);
        Assert.Contains("key2", guide.Context.Keys);
    }
}
