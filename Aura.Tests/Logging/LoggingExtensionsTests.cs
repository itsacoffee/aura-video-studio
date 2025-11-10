using Aura.Core.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Logging;

public class LoggingExtensionsTests
{
    private readonly Mock<ILogger> _mockLogger;

    public LoggingExtensionsTests()
    {
        _mockLogger = new Mock<ILogger>();
        _mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
    }

    [Fact]
    public void LogStructured_Should_Log_With_Properties()
    {
        // Arrange
        var properties = new Dictionary<string, object>
        {
            ["ProjectId"] = "proj123",
            ["UserId"] = "user456"
        };

        // Act
        _mockLogger.Object.LogStructured(
            LogLevel.Information,
            "Test message",
            properties);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogStructured_Should_Include_Exception_If_Provided()
    {
        // Arrange
        var properties = new Dictionary<string, object>();
        var exception = new InvalidOperationException("Test exception");

        // Act
        _mockLogger.Object.LogStructured(
            LogLevel.Error,
            "Error occurred",
            properties,
            exception);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPerformance_Should_Log_With_Duration()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(150);
        var additionalData = new Dictionary<string, object>
        {
            ["Operation"] = "TestOp"
        };

        // Act
        _mockLogger.Object.LogPerformance(
            "VideoGeneration",
            duration,
            success: true,
            additionalData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogPerformance_Should_Use_Warning_For_Slow_Operations()
    {
        // Arrange
        var slowDuration = TimeSpan.FromSeconds(6);

        // Act
        _mockLogger.Object.LogPerformance(
            "SlowOperation",
            slowDuration,
            success: true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogAudit_Should_Log_Audit_Event()
    {
        // Arrange
        var additionalData = new Dictionary<string, object>
        {
            ["Reason"] = "UserRequest"
        };

        // Act
        _mockLogger.Object.LogAudit(
            action: "DeleteProject",
            userId: "user123",
            resourceId: "proj456",
            additionalData);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Audit") && v.ToString()!.Contains("DeleteProject")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogSecurity_Should_Log_Security_Event()
    {
        // Act
        _mockLogger.Object.LogSecurity(
            eventType: "LoginAttempt",
            success: true,
            userId: "user123",
            ipAddress: "192.168.1.1",
            details: "Two-factor authentication used");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Security") && v.ToString()!.Contains("LoginAttempt")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogSecurity_Should_Use_Warning_For_Failed_Events()
    {
        // Act
        _mockLogger.Object.LogSecurity(
            eventType: "LoginAttempt",
            success: false,
            userId: "user123",
            ipAddress: "192.168.1.1");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogErrorWithContext_Should_Include_Exception_Context()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error");
        var context = new Dictionary<string, object>
        {
            ["ProjectId"] = "proj123",
            ["UserId"] = "user456"
        };

        // Act
        _mockLogger.Object.LogErrorWithContext(
            exception,
            "Operation failed",
            context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void BeginCorrelatedScope_Should_Create_Scope_With_CorrelationId()
    {
        // Arrange
        var correlationId = "corr123";
        var operationName = "TestOperation";

        // Act
        using var scope = _mockLogger.Object.BeginCorrelatedScope(correlationId, operationName);

        // Assert
        Assert.NotNull(scope);
        _mockLogger.Verify(
            x => x.BeginScope(It.IsAny<Dictionary<string, object>>()),
            Times.Once);
    }

    [Fact]
    public void LogStructured_Should_Not_Log_When_Disabled()
    {
        // Arrange
        _mockLogger.Setup(x => x.IsEnabled(LogLevel.Debug)).Returns(false);
        var properties = new Dictionary<string, object>();

        // Act
        _mockLogger.Object.LogStructured(
            LogLevel.Debug,
            "Debug message",
            properties);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public void LogAudit_Should_Handle_Null_Optional_Parameters()
    {
        // Act
        _mockLogger.Object.LogAudit(
            action: "SystemAction",
            userId: null,
            resourceId: null,
            additionalData: null);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SystemAction")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogSecurity_Should_Handle_Null_Optional_Parameters()
    {
        // Act
        _mockLogger.Object.LogSecurity(
            eventType: "SystemEvent",
            success: true,
            userId: null,
            ipAddress: null,
            details: null);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SystemEvent")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
