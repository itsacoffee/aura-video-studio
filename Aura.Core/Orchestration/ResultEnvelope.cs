using System;

namespace Aura.Core.Orchestration;

/// <summary>
/// Unified result envelope that tracks provider selection and fallback/downgrade information
/// </summary>
/// <typeparam name="T">The type of the actual result data</typeparam>
public record ResultEnvelope<T>
{
    /// <summary>
    /// Whether the operation succeeded
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The actual result data (null if failed)
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Error code if operation failed
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message if operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// The provider that was originally selected/requested
    /// </summary>
    public string? SourceProvider { get; init; }

    /// <summary>
    /// The provider that actually produced the result (may differ from SourceProvider if downgrade occurred)
    /// </summary>
    public string? ActualProvider { get; init; }

    /// <summary>
    /// Whether a fallback/downgrade occurred
    /// </summary>
    public bool WasDowngraded { get; init; }

    /// <summary>
    /// Reason for downgrade if one occurred
    /// </summary>
    public string? DowngradeReason { get; init; }

    /// <summary>
    /// Timestamp when the operation completed
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Extension methods for creating result envelopes
/// </summary>
public static class ResultEnvelopeExtensions
{
    /// <summary>
    /// Create a successful result envelope
    /// </summary>
    public static ResultEnvelope<T> Success<T>(
        T data,
        string actualProvider,
        string? sourceProvider = null,
        bool wasDowngraded = false,
        string? downgradeReason = null)
    {
        return new ResultEnvelope<T>
        {
            Success = true,
            Data = data,
            SourceProvider = sourceProvider ?? actualProvider,
            ActualProvider = actualProvider,
            WasDowngraded = wasDowngraded,
            DowngradeReason = downgradeReason
        };
    }

    /// <summary>
    /// Create a failed result envelope
    /// </summary>
    public static ResultEnvelope<T> Failure<T>(
        string errorCode,
        string errorMessage,
        string? sourceProvider = null)
    {
        return new ResultEnvelope<T>
        {
            Success = false,
            Data = default,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            SourceProvider = sourceProvider,
            ActualProvider = null,
            WasDowngraded = false
        };
    }
}
