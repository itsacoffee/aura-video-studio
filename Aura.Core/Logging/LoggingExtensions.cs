using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Aura.Core.Logging;

/// <summary>
/// Extension methods for enhanced logging capabilities
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Logs a structured event with additional context
    /// </summary>
    public static void LogStructured(
        this ILogger logger,
        LogLevel logLevel,
        string messageTemplate,
        Dictionary<string, object> properties,
        Exception? exception = null)
    {
        if (!logger.IsEnabled(logLevel))
            return;

        using (logger.BeginScope(properties))
        {
            if (exception != null)
            {
                logger.Log(logLevel, exception, messageTemplate);
            }
            else
            {
                logger.Log(logLevel, messageTemplate);
            }
        }
    }

    /// <summary>
    /// Logs performance metrics with standardized format
    /// </summary>
    public static void LogPerformance(
        this ILogger logger,
        string operationName,
        TimeSpan duration,
        bool success = true,
        Dictionary<string, object>? additionalData = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["OperationName"] = operationName,
            ["Duration"] = duration,
            ["DurationMs"] = duration.TotalMilliseconds,
            ["Success"] = success
        };

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                properties[kvp.Key] = kvp.Value;
            }
        }

        var logLevel = duration.TotalSeconds > 5 ? LogLevel.Warning : LogLevel.Information;
        
        using (logger.BeginScope(properties))
        {
            logger.Log(logLevel,
                "Performance: {OperationName} completed in {DurationMs}ms (Success: {Success})",
                operationName, duration.TotalMilliseconds, success);
        }
    }

    /// <summary>
    /// Logs an audit event with standardized format
    /// </summary>
    public static void LogAudit(
        this ILogger logger,
        string action,
        string? userId = null,
        string? resourceId = null,
        Dictionary<string, object>? additionalData = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["EventType"] = "Audit",
            ["Action"] = action,
            ["Timestamp"] = DateTimeOffset.UtcNow
        };

        if (!string.IsNullOrEmpty(userId))
            properties["UserId"] = userId;

        if (!string.IsNullOrEmpty(resourceId))
            properties["ResourceId"] = resourceId;

        if (additionalData != null)
        {
            foreach (var kvp in additionalData)
            {
                properties[kvp.Key] = kvp.Value;
            }
        }

        using (logger.BeginScope(properties))
        {
            logger.LogInformation("Audit: {Action} by {UserId} on {ResourceId}",
                action, userId ?? "System", resourceId ?? "N/A");
        }
    }

    /// <summary>
    /// Logs a security event with standardized format
    /// </summary>
    public static void LogSecurity(
        this ILogger logger,
        string eventType,
        bool success,
        string? userId = null,
        string? ipAddress = null,
        string? details = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["EventType"] = "Security",
            ["SecurityEventType"] = eventType,
            ["Success"] = success,
            ["Timestamp"] = DateTimeOffset.UtcNow
        };

        if (!string.IsNullOrEmpty(userId))
            properties["UserId"] = userId;

        if (!string.IsNullOrEmpty(ipAddress))
            properties["IpAddress"] = ipAddress;

        if (!string.IsNullOrEmpty(details))
            properties["Details"] = details;

        var logLevel = success ? LogLevel.Information : LogLevel.Warning;

        using (logger.BeginScope(properties))
        {
            logger.Log(logLevel,
                "Security: {EventType} - {Status} from {IpAddress} (User: {UserId})",
                eventType, success ? "Success" : "Failure", ipAddress ?? "Unknown", userId ?? "Unknown");
        }
    }

    /// <summary>
    /// Logs an error with enriched context
    /// </summary>
    public static void LogErrorWithContext(
        this ILogger logger,
        Exception exception,
        string message,
        Dictionary<string, object>? context = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["ExceptionType"] = exception.GetType().Name,
            ["StackTrace"] = exception.StackTrace ?? "N/A",
            ["Source"] = exception.Source ?? "Unknown"
        };

        if (context != null)
        {
            foreach (var kvp in context)
            {
                properties[kvp.Key] = kvp.Value;
            }
        }

        using (logger.BeginScope(properties))
        {
            logger.LogError(exception, message);
        }
    }

    /// <summary>
    /// Creates a correlation scope for distributed tracing
    /// </summary>
    public static IDisposable BeginCorrelatedScope(
        this ILogger logger,
        string correlationId,
        string? operationName = null)
    {
        var properties = new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        };

        if (!string.IsNullOrEmpty(operationName))
        {
            properties["OperationName"] = operationName;
        }

        return logger.BeginScope(properties);
    }
}

/// <summary>
/// Extension methods for configuring Serilog with best practices
/// </summary>
public static class SerilogConfigurationExtensions
{
    /// <summary>
    /// Configures Serilog with standard enrichers and filters
    /// </summary>
    public static LoggerConfiguration ConfigureStructuredLogging(
        this LoggerConfiguration configuration,
        string applicationName)
    {
        return configuration
            .Enrich.WithProperty("Application", applicationName)
            .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProcessId()
            .Enrich.FromLogContext()
            .Enrich.With<TraceContextEnricher>()
            .Enrich.With<PerformanceEnricher>()
            .Enrich.With<RequestContextEnricher>()
            .Enrich.With<OperationContextEnricher>()
            .Filter.With<SensitiveDataFilter>();
    }

    /// <summary>
    /// Configures log sampling to reduce volume
    /// </summary>
    public static LoggerConfiguration ConfigureLogSampling(
        this LoggerConfiguration configuration,
        int sampleRate = 10)
    {
        return configuration.Filter.ByExcluding(logEvent =>
        {
            // Sample verbose and debug logs
            if (logEvent.Level is LogEventLevel.Verbose or LogEventLevel.Debug)
            {
                return Random.Shared.Next(sampleRate) != 0;
            }

            return false;
        });
    }
}
