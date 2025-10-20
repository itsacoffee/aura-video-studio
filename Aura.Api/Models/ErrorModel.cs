using System;
using System.Text.Json.Serialization;

namespace Aura.Api.Models;

/// <summary>
/// Structured error response model for API errors with correlation tracking
/// </summary>
public class ErrorModel
{
    /// <summary>
    /// A URI reference that identifies the problem type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "https://docs.aura.studio/errors/E500";

    /// <summary>
    /// A short, human-readable summary of the problem type
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = "Internal Server Error";

    /// <summary>
    /// HTTP status code
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; init; } = 500;

    /// <summary>
    /// A human-readable explanation specific to this occurrence of the problem
    /// </summary>
    [JsonPropertyName("detail")]
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    /// Correlation ID for tracking this error across services
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string CorrelationId { get; init; } = string.Empty;

    /// <summary>
    /// Trace ID for request tracing
    /// </summary>
    [JsonPropertyName("traceId")]
    public string? TraceId { get; init; }

    /// <summary>
    /// Error code for specific error identification
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Additional error details
    /// </summary>
    [JsonPropertyName("details")]
    public object? Details { get; init; }

    /// <summary>
    /// Timestamp when the error occurred
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a new ErrorModel
    /// </summary>
    public ErrorModel()
    {
    }

    /// <summary>
    /// Creates a new ErrorModel with specified parameters
    /// </summary>
    public ErrorModel(
        string type,
        string title,
        int status,
        string detail,
        string correlationId,
        string? traceId = null,
        string? errorCode = null,
        object? details = null)
    {
        Type = type;
        Title = title;
        Status = status;
        Detail = detail;
        CorrelationId = correlationId;
        TraceId = traceId;
        ErrorCode = errorCode;
        Details = details;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a standard 404 Not Found error
    /// </summary>
    public static ErrorModel NotFound(string detail, string correlationId, string? traceId = null)
    {
        return new ErrorModel(
            type: "https://docs.aura.studio/errors/E404",
            title: "Not Found",
            status: 404,
            detail: detail,
            correlationId: correlationId,
            traceId: traceId);
    }

    /// <summary>
    /// Creates a standard 400 Bad Request error
    /// </summary>
    public static ErrorModel BadRequest(string detail, string correlationId, string? traceId = null, object? details = null)
    {
        return new ErrorModel(
            type: "https://docs.aura.studio/errors/E400",
            title: "Bad Request",
            status: 400,
            detail: detail,
            correlationId: correlationId,
            traceId: traceId,
            details: details);
    }

    /// <summary>
    /// Creates a standard 500 Internal Server Error
    /// </summary>
    public static ErrorModel InternalServerError(string detail, string correlationId, string? traceId = null, string? errorCode = null)
    {
        return new ErrorModel(
            type: "https://docs.aura.studio/errors/E500",
            title: "Internal Server Error",
            status: 500,
            detail: detail,
            correlationId: correlationId,
            traceId: traceId,
            errorCode: errorCode);
    }
}
