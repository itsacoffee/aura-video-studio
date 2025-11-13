using System;

namespace Aura.Core.Services.Providers.Stickiness;

/// <summary>
/// Records a user's decision to switch providers during a job.
/// Provides audit trail and transparency for all provider transitions.
/// </summary>
public sealed class FallbackDecision
{
    /// <summary>
    /// Gets the unique identifier for the job
    /// </summary>
    public string JobId { get; }

    /// <summary>
    /// Gets the timestamp when the decision was made
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the provider being switched from
    /// </summary>
    public string FromProvider { get; }

    /// <summary>
    /// Gets the provider being switched to
    /// </summary>
    public string ToProvider { get; }

    /// <summary>
    /// Gets the reason code for the switch
    /// </summary>
    public FallbackReasonCode ReasonCode { get; }

    /// <summary>
    /// Gets the elapsed time before the switch was made (milliseconds)
    /// </summary>
    public long ElapsedBeforeSwitchMs { get; }

    /// <summary>
    /// Gets whether the user explicitly confirmed the switch
    /// </summary>
    public bool UserConfirmed { get; }

    /// <summary>
    /// Gets the pipeline stages affected by this switch
    /// </summary>
    public string[] AffectedStages { get; }

    /// <summary>
    /// Gets additional context about the decision
    /// </summary>
    public string? AdditionalContext { get; }

    /// <summary>
    /// Gets the correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; }

    /// <summary>
    /// Initializes a new fallback decision record
    /// </summary>
    public FallbackDecision(
        string jobId,
        string fromProvider,
        string toProvider,
        FallbackReasonCode reasonCode,
        long elapsedBeforeSwitchMs,
        bool userConfirmed,
        string correlationId,
        string[]? affectedStages = null,
        string? additionalContext = null)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("Job ID cannot be null or whitespace", nameof(jobId));
        
        if (string.IsNullOrWhiteSpace(fromProvider))
            throw new ArgumentException("From provider cannot be null or whitespace", nameof(fromProvider));
        
        if (string.IsNullOrWhiteSpace(toProvider))
            throw new ArgumentException("To provider cannot be null or whitespace", nameof(toProvider));

        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or whitespace", nameof(correlationId));

        JobId = jobId;
        Timestamp = DateTime.UtcNow;
        FromProvider = fromProvider;
        ToProvider = toProvider;
        ReasonCode = reasonCode;
        ElapsedBeforeSwitchMs = elapsedBeforeSwitchMs;
        UserConfirmed = userConfirmed;
        CorrelationId = correlationId;
        AffectedStages = affectedStages ?? Array.Empty<string>();
        AdditionalContext = additionalContext;
    }

    /// <summary>
    /// Creates a decision for user-initiated fallback
    /// </summary>
    public static FallbackDecision CreateUserRequested(
        string jobId,
        string fromProvider,
        string toProvider,
        long elapsedMs,
        string correlationId,
        string[]? affectedStages = null)
    {
        return new FallbackDecision(
            jobId,
            fromProvider,
            toProvider,
            FallbackReasonCode.USER_REQUEST,
            elapsedMs,
            userConfirmed: true,
            correlationId,
            affectedStages,
            "User explicitly chose to switch providers");
    }

    /// <summary>
    /// Creates a decision for provider fatal error
    /// </summary>
    public static FallbackDecision CreateAfterFatalError(
        string jobId,
        string fromProvider,
        string toProvider,
        long elapsedMs,
        string correlationId,
        string errorDetails,
        bool userConfirmed,
        string[]? affectedStages = null)
    {
        return new FallbackDecision(
            jobId,
            fromProvider,
            toProvider,
            FallbackReasonCode.PROVIDER_FATAL_ERROR,
            elapsedMs,
            userConfirmed,
            correlationId,
            affectedStages,
            $"Fatal error: {errorDetails}");
    }

    /// <summary>
    /// Creates a decision after stall detection
    /// </summary>
    public static FallbackDecision CreateAfterStall(
        string jobId,
        string fromProvider,
        string toProvider,
        long elapsedMs,
        string correlationId,
        long stallDurationMs,
        string[]? affectedStages = null)
    {
        return new FallbackDecision(
            jobId,
            fromProvider,
            toProvider,
            FallbackReasonCode.USER_AFTER_STALL,
            elapsedMs,
            userConfirmed: true,
            correlationId,
            affectedStages,
            $"User chose alternative after stall detection (stalled for {stallDurationMs}ms)");
    }

    /// <summary>
    /// Creates a decision for legacy automatic fallback (migration support)
    /// </summary>
    public static FallbackDecision CreateLegacyAuto(
        string jobId,
        string fromProvider,
        string toProvider,
        long elapsedMs,
        string correlationId,
        string[]? affectedStages = null)
    {
        return new FallbackDecision(
            jobId,
            fromProvider,
            toProvider,
            FallbackReasonCode.LEGACY_AUTO,
            elapsedMs,
            userConfirmed: false,
            correlationId,
            affectedStages,
            "Legacy automatic fallback - migrated to explicit control");
    }

    public override string ToString()
    {
        var stages = AffectedStages.Length > 0 ? string.Join(", ", AffectedStages) : "All";
        return $"FallbackDecision[{FromProvider} → {ToProvider}, Reason: {ReasonCode}, " +
               $"Elapsed: {ElapsedBeforeSwitchMs}ms, UserConfirmed: {UserConfirmed}, Stages: {stages}]";
    }
}

/// <summary>
/// Reason codes for provider fallback decisions
/// </summary>
public enum FallbackReasonCode
{
    /// <summary>
    /// User explicitly requested the provider switch
    /// </summary>
    USER_REQUEST,

    /// <summary>
    /// Provider returned a fatal error requiring fallback
    /// </summary>
    PROVIDER_FATAL_ERROR,

    /// <summary>
    /// User chose to fallback after stall detection dialog
    /// </summary>
    USER_AFTER_STALL,

    /// <summary>
    /// Legacy automatic fallback (pre-stickiness system)
    /// </summary>
    LEGACY_AUTO
}
