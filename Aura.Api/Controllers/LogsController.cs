using Microsoft.AspNetCore.Mvc;
using Aura.Core.Logging;
using System.Text.Json;

namespace Aura.Api.Controllers;

/// <summary>
/// API endpoint for receiving frontend logs and errors
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly ILogger<LogsController> _logger;

    public LogsController(ILogger<LogsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Receive batch logs from frontend
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ReceiveBatchLogs([FromBody] FrontendLogBatchRequest request)
    {
        if (request?.Logs == null || request.Logs.Count == 0)
        {
            return BadRequest("No logs provided");
        }

        foreach (var log in request.Logs)
        {
            LogFrontendEntry(log, request.ClientInfo);
        }

        return Ok(new { received = request.Logs.Count });
    }

    /// <summary>
    /// Receive critical error from frontend for immediate processing
    /// </summary>
    [HttpPost("error")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ReceiveError([FromBody] FrontendErrorRequest request)
    {
        if (request?.Error == null)
        {
            return BadRequest("No error provided");
        }

        var context = new Dictionary<string, object>
        {
            ["frontend"] = true,
            ["url"] = request.Url ?? "unknown",
            ["userAgent"] = request.UserAgent ?? "unknown",
            ["timestamp"] = request.Timestamp ?? DateTime.UtcNow.ToString("O")
        };

        if (request.Context != null)
        {
            foreach (var kvp in request.Context)
            {
                context[$"context_{kvp.Key}"] = kvp.Value;
            }
        }

        if (request.TraceContext != null)
        {
            context["traceId"] = request.TraceContext.TraceId;
            context["spanId"] = request.TraceContext.SpanId;
            if (!string.IsNullOrEmpty(request.TraceContext.ParentSpanId))
            {
                context["parentSpanId"] = request.TraceContext.ParentSpanId;
            }
        }

        _logger.LogErrorWithContext(
            new FrontendException(request.Error.Message, request.Error.Stack),
            $"Frontend error: {request.Error.Name}",
            context);

        return Ok(new { received = true });
    }

    private void LogFrontendEntry(FrontendLogEntry log, ClientInfo? clientInfo)
    {
        var context = new Dictionary<string, object>
        {
            ["frontend"] = true,
            ["component"] = log.Component ?? "unknown",
            ["action"] = log.Action ?? "unknown"
        };

        if (log.Context != null)
        {
            foreach (var kvp in log.Context)
            {
                try
                {
                    context[$"context_{kvp.Key}"] = kvp.Value?.ToString() ?? "null";
                }
                catch
                {
                    context[$"context_{kvp.Key}"] = "<serialization error>";
                }
            }
        }

        if (log.TraceContext != null)
        {
            context["traceId"] = log.TraceContext.TraceId;
            context["spanId"] = log.TraceContext.SpanId;
        }

        if (log.CorrelationId != null)
        {
            context["correlationId"] = log.CorrelationId;
        }

        if (clientInfo != null)
        {
            context["userAgent"] = clientInfo.UserAgent ?? "unknown";
            context["url"] = clientInfo.Url ?? "unknown";
        }

        var logLevel = log.Level.ToUpperInvariant() switch
        {
            "DEBUG" => LogLevel.Debug,
            "INFO" => LogLevel.Information,
            "WARN" => LogLevel.Warning,
            "ERROR" => LogLevel.Error,
            "PERFORMANCE" => LogLevel.Information,
            _ => LogLevel.Information
        };

        if (log.Error != null)
        {
            _logger.LogErrorWithContext(
                new FrontendException(log.Error.Message, log.Error.Stack),
                $"[Frontend] {log.Message}",
                context);
        }
        else
        {
            _logger.LogStructured(
                logLevel,
                $"[Frontend] {log.Message}",
                context);
        }
    }

    /// <summary>
    /// Exception class for frontend errors
    /// </summary>
    private sealed class FrontendException : Exception
    {
        public FrontendException(string message, string? stack) : base(message)
        {
            FrontendStack = stack;
        }

        public string? FrontendStack { get; }

        public override string? StackTrace => FrontendStack ?? base.StackTrace;
    }
}

/// <summary>
/// Request model for batch log submission
/// </summary>
public class FrontendLogBatchRequest
{
    public List<FrontendLogEntry> Logs { get; set; } = new();
    public ClientInfo? ClientInfo { get; set; }
}

/// <summary>
/// Frontend log entry model
/// </summary>
public class FrontendLogEntry
{
    public string Timestamp { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Component { get; set; }
    public string? Action { get; set; }
    public Dictionary<string, object>? Context { get; set; }
    public FrontendTraceContext? TraceContext { get; set; }
    public string? CorrelationId { get; set; }
    public FrontendError? Error { get; set; }
}

/// <summary>
/// Frontend error request model
/// </summary>
public class FrontendErrorRequest
{
    public string Timestamp { get; set; } = string.Empty;
    public FrontendError Error { get; set; } = null!;
    public Dictionary<string, object>? Context { get; set; }
    public FrontendTraceContext? TraceContext { get; set; }
    public string? UserAgent { get; set; }
    public string? Url { get; set; }
}

/// <summary>
/// Frontend error model
/// </summary>
public class FrontendError
{
    public string Name { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Stack { get; set; }
}

/// <summary>
/// Frontend trace context model
/// </summary>
public class FrontendTraceContext
{
    public string TraceId { get; set; } = string.Empty;
    public string SpanId { get; set; } = string.Empty;
    public string? ParentSpanId { get; set; }
    public string? OperationName { get; set; }
}

/// <summary>
/// Client information model
/// </summary>
public class ClientInfo
{
    public string? UserAgent { get; set; }
    public string? Url { get; set; }
    public ViewportInfo? Viewport { get; set; }
}

/// <summary>
/// Viewport information model
/// </summary>
public class ViewportInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
}
