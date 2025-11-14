using System;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services.Providers.Stickiness;

/// <summary>
/// Interface for provider-specific heartbeat strategies.
/// Providers implement this to report progress and detect stalls.
/// </summary>
public interface IHeartbeatStrategy
{
    /// <summary>
    /// Gets the recommended heartbeat check interval for this provider type
    /// </summary>
    TimeSpan HeartbeatInterval { get; }

    /// <summary>
    /// Gets the threshold for stall detection (typically N × HeartbeatInterval)
    /// </summary>
    TimeSpan StallThreshold { get; }

    /// <summary>
    /// Checks if the provider is responsive and returns progress information
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Progress information if available, null if no progress to report</returns>
    Task<ProviderProgress?> CheckHeartbeatAsync(CancellationToken ct = default);

    /// <summary>
    /// Determines if the provider supports heartbeat monitoring
    /// </summary>
    bool SupportsHeartbeat { get; }
}

/// <summary>
/// Heartbeat strategy for LLM providers that support token streaming
/// </summary>
public sealed class LlmStreamingHeartbeatStrategy : IHeartbeatStrategy
{
    private readonly Func<Task<int?>> _getTokenCount;
    private int _lastTokenCount;

    public TimeSpan HeartbeatInterval { get; }
    public TimeSpan StallThreshold { get; }
    public bool SupportsHeartbeat => true;

    /// <summary>
    /// Initializes a new LLM streaming heartbeat strategy
    /// </summary>
    /// <param name="getTokenCount">Function to retrieve current token count</param>
    /// <param name="heartbeatIntervalMs">Heartbeat check interval in milliseconds</param>
    /// <param name="stallMultiplier">Multiplier for stall threshold (e.g., 3× heartbeat interval)</param>
    public LlmStreamingHeartbeatStrategy(
        Func<Task<int?>> getTokenCount,
        int heartbeatIntervalMs = 5000,
        int stallMultiplier = 4)
    {
        _getTokenCount = getTokenCount ?? throw new ArgumentNullException(nameof(getTokenCount));
        HeartbeatInterval = TimeSpan.FromMilliseconds(heartbeatIntervalMs);
        StallThreshold = TimeSpan.FromMilliseconds(heartbeatIntervalMs * stallMultiplier);
        _lastTokenCount = 0;
    }

    public async Task<ProviderProgress?> CheckHeartbeatAsync(CancellationToken ct = default)
    {
        var currentTokenCount = await _getTokenCount().ConfigureAwait(false);
        
        if (!currentTokenCount.HasValue)
            return null;

        var tokensGenerated = currentTokenCount.Value - _lastTokenCount;
        _lastTokenCount = currentTokenCount.Value;

        if (tokensGenerated > 0)
        {
            return new ProviderProgress
            {
                TokensGenerated = currentTokenCount.Value,
                Message = $"Generated {tokensGenerated} tokens since last heartbeat"
            };
        }

        return null;
    }
}

/// <summary>
/// Heartbeat strategy for TTS providers that emit audio chunks
/// </summary>
public sealed class TtsChunkHeartbeatStrategy : IHeartbeatStrategy
{
    private readonly Func<Task<int?>> _getChunkCount;
    private int _lastChunkCount;

    public TimeSpan HeartbeatInterval { get; }
    public TimeSpan StallThreshold { get; }
    public bool SupportsHeartbeat => true;

    /// <summary>
    /// Initializes a new TTS chunk heartbeat strategy
    /// </summary>
    /// <param name="getChunkCount">Function to retrieve current chunk count</param>
    /// <param name="heartbeatIntervalMs">Heartbeat check interval in milliseconds</param>
    /// <param name="stallMultiplier">Multiplier for stall threshold</param>
    public TtsChunkHeartbeatStrategy(
        Func<Task<int?>> getChunkCount,
        int heartbeatIntervalMs = 10000,
        int stallMultiplier = 3)
    {
        _getChunkCount = getChunkCount ?? throw new ArgumentNullException(nameof(getChunkCount));
        HeartbeatInterval = TimeSpan.FromMilliseconds(heartbeatIntervalMs);
        StallThreshold = TimeSpan.FromMilliseconds(heartbeatIntervalMs * stallMultiplier);
        _lastChunkCount = 0;
    }

    public async Task<ProviderProgress?> CheckHeartbeatAsync(CancellationToken ct = default)
    {
        var currentChunkCount = await _getChunkCount().ConfigureAwait(false);
        
        if (!currentChunkCount.HasValue)
            return null;

        var chunksGenerated = currentChunkCount.Value - _lastChunkCount;
        _lastChunkCount = currentChunkCount.Value;

        if (chunksGenerated > 0)
        {
            return new ProviderProgress
            {
                ChunksProcessed = currentChunkCount.Value,
                Message = $"Processed {chunksGenerated} chunks since last heartbeat"
            };
        }

        return null;
    }
}

/// <summary>
/// Heartbeat strategy for providers that report percentage progress
/// </summary>
public sealed class PercentageHeartbeatStrategy : IHeartbeatStrategy
{
    private readonly Func<Task<double?>> _getPercentComplete;
    private double? _lastPercent;

    public TimeSpan HeartbeatInterval { get; }
    public TimeSpan StallThreshold { get; }
    public bool SupportsHeartbeat => true;

    /// <summary>
    /// Initializes a new percentage-based heartbeat strategy
    /// </summary>
    /// <param name="getPercentComplete">Function to retrieve current percentage (0-100)</param>
    /// <param name="heartbeatIntervalMs">Heartbeat check interval in milliseconds</param>
    /// <param name="stallMultiplier">Multiplier for stall threshold</param>
    public PercentageHeartbeatStrategy(
        Func<Task<double?>> getPercentComplete,
        int heartbeatIntervalMs = 20000,
        int stallMultiplier = 2)
    {
        _getPercentComplete = getPercentComplete ?? throw new ArgumentNullException(nameof(getPercentComplete));
        HeartbeatInterval = TimeSpan.FromMilliseconds(heartbeatIntervalMs);
        StallThreshold = TimeSpan.FromMilliseconds(heartbeatIntervalMs * stallMultiplier);
    }

    public async Task<ProviderProgress?> CheckHeartbeatAsync(CancellationToken ct = default)
    {
        var currentPercent = await _getPercentComplete().ConfigureAwait(false);
        
        if (!currentPercent.HasValue)
            return null;

        var hasProgress = !_lastPercent.HasValue || currentPercent.Value > _lastPercent.Value;
        _lastPercent = currentPercent.Value;

        if (hasProgress)
        {
            return new ProviderProgress
            {
                PercentComplete = currentPercent.Value,
                Message = $"{currentPercent.Value:F1}% complete"
            };
        }

        return null;
    }
}

/// <summary>
/// Null heartbeat strategy for providers that don't support progress monitoring
/// </summary>
public sealed class NoHeartbeatStrategy : IHeartbeatStrategy
{
    public TimeSpan HeartbeatInterval => TimeSpan.FromSeconds(30);
    public TimeSpan StallThreshold => TimeSpan.FromMinutes(10);
    public bool SupportsHeartbeat => false;

    public Task<ProviderProgress?> CheckHeartbeatAsync(CancellationToken ct = default)
    {
        return Task.FromResult<ProviderProgress?>(null);
    }
}
