using System;

namespace Aura.Core.Services.Providers.Stickiness;

/// <summary>
/// Represents the current latency category of a provider operation
/// </summary>
public enum LatencyCategory
{
    /// <summary>
    /// Normal response time (0-30s typically)
    /// </summary>
    Normal,

    /// <summary>
    /// Extended wait but still acceptable (30-180s typically)
    /// </summary>
    Extended,

    /// <summary>
    /// Deep wait for long-running operations (180s+ typically)
    /// </summary>
    DeepWait,

    /// <summary>
    /// Stall suspected - no heartbeat detected
    /// </summary>
    StallSuspected,

    /// <summary>
    /// Hard error occurred
    /// </summary>
    Error
}

/// <summary>
/// Provider state information including latency categorization
/// </summary>
public sealed class ProviderState
{
    /// <summary>
    /// Gets the provider name
    /// </summary>
    public string ProviderName { get; }

    /// <summary>
    /// Gets the provider type (local_llm, cloud_llm, tts, etc.)
    /// </summary>
    public string ProviderType { get; }

    /// <summary>
    /// Gets the current latency category
    /// </summary>
    public LatencyCategory CurrentCategory { get; private set; }

    /// <summary>
    /// Gets the timestamp when the request started
    /// </summary>
    public DateTime RequestStarted { get; }

    /// <summary>
    /// Gets the timestamp of the last heartbeat
    /// </summary>
    public DateTime? LastHeartbeat { get; private set; }

    /// <summary>
    /// Gets the total elapsed time since request started
    /// </summary>
    public TimeSpan ElapsedTime => DateTime.UtcNow - RequestStarted;

    /// <summary>
    /// Gets the time since last heartbeat (null if no heartbeat yet)
    /// </summary>
    public TimeSpan? TimeSinceLastHeartbeat => LastHeartbeat.HasValue 
        ? DateTime.UtcNow - LastHeartbeat.Value 
        : null;

    /// <summary>
    /// Gets the heartbeat count
    /// </summary>
    public int HeartbeatCount { get; private set; }

    /// <summary>
    /// Gets whether the operation is complete
    /// </summary>
    public bool IsComplete { get; private set; }

    /// <summary>
    /// Gets the error message if in error state
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Gets the correlation ID for tracking
    /// </summary>
    public string CorrelationId { get; }

    /// <summary>
    /// Gets custom progress data (tokens generated, chunks processed, etc.)
    /// </summary>
    public ProviderProgress? Progress { get; private set; }

    /// <summary>
    /// Initializes a new provider state
    /// </summary>
    public ProviderState(string providerName, string providerType, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("Provider name cannot be null or whitespace", nameof(providerName));
        
        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("Provider type cannot be null or whitespace", nameof(providerType));

        if (string.IsNullOrWhiteSpace(correlationId))
            throw new ArgumentException("Correlation ID cannot be null or whitespace", nameof(correlationId));

        ProviderName = providerName;
        ProviderType = providerType;
        CorrelationId = correlationId;
        RequestStarted = DateTime.UtcNow;
        CurrentCategory = LatencyCategory.Normal;
        HeartbeatCount = 0;
        IsComplete = false;
    }

    /// <summary>
    /// Records a heartbeat from the provider
    /// </summary>
    public void RecordHeartbeat(ProviderProgress? progress = null)
    {
        LastHeartbeat = DateTime.UtcNow;
        HeartbeatCount++;
        Progress = progress;

        if (CurrentCategory == LatencyCategory.StallSuspected)
        {
            CurrentCategory = LatencyCategory.Normal;
        }
    }

    /// <summary>
    /// Updates the latency category based on elapsed time and thresholds
    /// </summary>
    public void UpdateCategory(LatencyCategory newCategory)
    {
        if (CurrentCategory != newCategory)
        {
            CurrentCategory = newCategory;
        }
    }

    /// <summary>
    /// Marks the operation as complete
    /// </summary>
    public void MarkComplete()
    {
        IsComplete = true;
    }

    /// <summary>
    /// Marks the operation as errored
    /// </summary>
    public void MarkError(string errorMessage)
    {
        CurrentCategory = LatencyCategory.Error;
        ErrorMessage = errorMessage;
        IsComplete = true;
    }

    /// <summary>
    /// Checks if a stall is suspected based on heartbeat timing
    /// </summary>
    public bool IsStallSuspected(TimeSpan stallThreshold)
    {
        if (!LastHeartbeat.HasValue)
        {
            return ElapsedTime > stallThreshold;
        }

        return TimeSinceLastHeartbeat!.Value > stallThreshold;
    }

    public override string ToString()
    {
        var heartbeat = LastHeartbeat.HasValue 
            ? $"Last heartbeat {TimeSinceLastHeartbeat!.Value.TotalSeconds:F1}s ago"
            : "No heartbeat yet";
        
        return $"ProviderState[{ProviderName} ({ProviderType}), {CurrentCategory}, " +
               $"Elapsed: {ElapsedTime.TotalSeconds:F1}s, {heartbeat}, Heartbeats: {HeartbeatCount}]";
    }
}

/// <summary>
/// Progress information reported by a provider via heartbeat
/// </summary>
public sealed class ProviderProgress
{
    /// <summary>
    /// Gets the number of tokens generated (for LLM providers)
    /// </summary>
    public int? TokensGenerated { get; init; }

    /// <summary>
    /// Gets the number of chunks processed (for TTS/audio providers)
    /// </summary>
    public int? ChunksProcessed { get; init; }

    /// <summary>
    /// Gets the percentage complete (0-100, null if unknown)
    /// </summary>
    public double? PercentComplete { get; init; }

    /// <summary>
    /// Gets the estimated time remaining (null if unknown)
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Gets custom progress message from provider
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the timestamp when this progress was reported
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public override string ToString()
    {
        var parts = new List<string>();
        
        if (TokensGenerated.HasValue)
            parts.Add($"{TokensGenerated} tokens");
        
        if (ChunksProcessed.HasValue)
            parts.Add($"{ChunksProcessed} chunks");
        
        if (PercentComplete.HasValue)
            parts.Add($"{PercentComplete:F1}%");
        
        if (!string.IsNullOrEmpty(Message))
            parts.Add(Message);

        return parts.Count > 0 ? string.Join(", ", parts) : "Progress update";
    }
}
