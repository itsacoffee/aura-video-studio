using Aura.Core.Services.ErrorHandling;
using Aura.Core.Errors;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Services.ErrorHandling;

public class ErrorLoggingServiceTests
{
    private readonly Mock<ILogger<ErrorLoggingService>> _mockLogger;
    private readonly string _testLogPath;

    public ErrorLoggingServiceTests()
    {
        _mockLogger = new Mock<ILogger<ErrorLoggingService>>();
        _testLogPath = Path.Combine(Path.GetTempPath(), "aura-test-logs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testLogPath);
    }

    [Fact]
    public async Task LogErrorAsync_WithAuraException_IncludesAllDetails()
    {
        // Arrange
        var service = new ErrorLoggingService(_mockLogger.Object, _testLogPath);
        var exception = new ProviderException(
            "TestProvider",
            ProviderType.LLM,
            "Test error",
            correlationId: "test-123"
        );

        // Act
        await service.LogErrorAsync(exception, ErrorCategory.Provider, "test-123", writeImmediately: true);
        var errors = await service.GetRecentErrorsAsync(10);

        // Assert
        Assert.Single(errors);
        var error = errors[0];
        Assert.Equal("ProviderException", error.ExceptionType);
        Assert.Equal("test-123", error.CorrelationId);
        Assert.NotNull(error.ErrorCode);
        Assert.NotNull(error.UserMessage);
        Assert.NotNull(error.SuggestedActions);
    }

    [Fact]
    public async Task GetRecentErrorsAsync_FiltersByCategory()
    {
        // Arrange
        var service = new ErrorLoggingService(_mockLogger.Object, _testLogPath);
        
        await service.LogErrorAsync(
            new Exception("User error"),
            ErrorCategory.User,
            writeImmediately: true);
        
        await service.LogErrorAsync(
            new Exception("System error"),
            ErrorCategory.System,
            writeImmediately: true);

        // Act
        var userErrors = await service.GetRecentErrorsAsync(10, ErrorCategory.User);
        var systemErrors = await service.GetRecentErrorsAsync(10, ErrorCategory.System);

        // Assert
        Assert.Single(userErrors);
        Assert.Single(systemErrors);
        Assert.Equal(ErrorCategory.User, userErrors[0].Category);
        Assert.Equal(ErrorCategory.System, systemErrors[0].Category);
    }

    [Fact]
    public async Task SearchByCorrelationIdAsync_FindsMatchingErrors()
    {
        // Arrange
        var service = new ErrorLoggingService(_mockLogger.Object, _testLogPath);
        var correlationId = Guid.NewGuid().ToString("N");

        await service.LogErrorAsync(
            new Exception("Test error 1"),
            ErrorCategory.Application,
            correlationId,
            writeImmediately: true);

        await service.LogErrorAsync(
            new Exception("Test error 2"),
            ErrorCategory.Application,
            correlationId,
            writeImmediately: true);

        // Act
        var errors = await service.SearchByCorrelationIdAsync(correlationId);

        // Assert
        Assert.Equal(2, errors.Count);
        Assert.All(errors, e => Assert.Equal(correlationId, e.CorrelationId));
    }

    [Fact]
    public async Task ExportDiagnosticsAsync_CreatesValidFile()
    {
        // Arrange
        var service = new ErrorLoggingService(_mockLogger.Object, _testLogPath);
        
        await service.LogErrorAsync(
            new Exception("Test error"),
            ErrorCategory.Application,
            writeImmediately: true);

        // Act
        var exportPath = await service.ExportDiagnosticsAsync();

        // Assert
        Assert.True(File.Exists(exportPath));
        var content = await File.ReadAllTextAsync(exportPath);
        Assert.Contains("ExportedAt", content);
        Assert.Contains("ErrorCount", content);
        Assert.Contains("SystemInfo", content);

        // Cleanup
        File.Delete(exportPath);
    }

    [Fact]
    public async Task CleanupOldLogsAsync_RemovesOldFiles()
    {
        // Arrange
        var service = new ErrorLoggingService(_mockLogger.Object, _testLogPath);
        
        // Create an old log file
        var oldLogPath = Path.Combine(_testLogPath, "errors-2020-01.jsonl");
        await File.WriteAllTextAsync(oldLogPath, "test");
        File.SetLastWriteTimeUtc(oldLogPath, DateTime.UtcNow.AddDays(-60));

        // Act
        var deletedCount = await service.CleanupOldLogsAsync(TimeSpan.FromDays(30));

        // Assert
        Assert.Equal(1, deletedCount);
        Assert.False(File.Exists(oldLogPath));
    }

    [Fact]
    public async Task FlushErrorsAsync_WritesQueuedErrors()
    {
        // Arrange
        var service = new ErrorLoggingService(_mockLogger.Object, _testLogPath);

        // Queue errors without immediate write
        await service.LogErrorAsync(
            new Exception("Test error 1"),
            ErrorCategory.Application,
            writeImmediately: false);

        await service.LogErrorAsync(
            new Exception("Test error 2"),
            ErrorCategory.Application,
            writeImmediately: false);

        // Act
        await service.FlushErrorsAsync();
        var errors = await service.GetRecentErrorsAsync(10);

        // Assert
        Assert.Equal(2, errors.Count);
    }

    public void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_testLogPath))
        {
            Directory.Delete(_testLogPath, true);
        }
    }
}
