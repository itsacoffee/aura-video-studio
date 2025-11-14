using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Providers.Stickiness;

/// <summary>
/// Event raised when a stall is suspected
/// </summary>
public sealed class StallSuspectedEvent
{
    public string ProviderName { get; init; } = string.Empty;
    public string ProviderType { get; init; } = string.Empty;
    public TimeSpan ElapsedSinceLastHeartbeat { get; init; }
    public TimeSpan TotalElapsed { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Detects when providers appear to have stalled based on heartbeat analysis.
/// Emits events but never automatically switches providers.
/// </summary>
public sealed class StallDetector
{
    private readonly ILogger<StallDetector> _logger;
    private readonly TimeSpan _checkInterval;

    /// <summary>
    /// Event raised when a stall is suspected
    /// </summary>
    public event EventHandler<StallSuspectedEvent>? StallSuspected;

    /// <summary>
    /// Initializes a new stall detector
    /// </summary>
    /// <param name="logger">Logger instance</param>
    /// <param name="checkIntervalMs">Interval for checking stalls (default 5s)</param>
    public StallDetector(ILogger<StallDetector> logger, int checkIntervalMs = 5000)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _checkInterval = TimeSpan.FromMilliseconds(checkIntervalMs);
    }

    /// <summary>
    /// Starts monitoring a provider state for stalls
    /// </summary>
    /// <param name="providerState">The provider state to monitor</param>
    /// <param name="heartbeatStrategy">The heartbeat strategy for this provider</param>
    /// <param name="ct">Cancellation token</param>
    public async Task MonitorAsync(
        ProviderState providerState,
        IHeartbeatStrategy heartbeatStrategy,
        CancellationToken ct = default)
    {
        if (providerState == null)
            throw new ArgumentNullException(nameof(providerState));

        if (heartbeatStrategy == null)
            throw new ArgumentNullException(nameof(heartbeatStrategy));

        _logger.LogInformation(
            "[{CorrelationId}] Starting stall detection for {Provider} (type: {Type})",
            providerState.CorrelationId,
            providerState.ProviderName,
            providerState.ProviderType);

        if (!heartbeatStrategy.SupportsHeartbeat)
        {
            _logger.LogInformation(
                "[{CorrelationId}] Provider {Provider} does not support heartbeat - using basic timeout",
                providerState.CorrelationId,
                providerState.ProviderName);
        }

        try
        {
            while (!providerState.IsComplete && !ct.IsCancellationRequested)
            {
                await Task.Delay(_checkInterval, ct).ConfigureAwait(false);

                if (heartbeatStrategy.SupportsHeartbeat)
                {
                    var progress = await heartbeatStrategy.CheckHeartbeatAsync(ct).ConfigureAwait(false);
                    
                    if (progress != null)
                    {
                        providerState.RecordHeartbeat(progress);
                        
                        _logger.LogDebug(
                            "[{CorrelationId}] PROVIDER_HEARTBEAT {Provider} - {Progress}",
                            providerState.CorrelationId,
                            providerState.ProviderName,
                            progress);
                    }
                }

                UpdateLatencyCategory(providerState, heartbeatStrategy);

                if (providerState.IsStallSuspected(heartbeatStrategy.StallThreshold))
                {
                    HandleStallSuspicion(providerState);
                }
            }

            _logger.LogInformation(
                "[{CorrelationId}] Stall detection completed for {Provider}",
                providerState.CorrelationId,
                providerState.ProviderName);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "[{CorrelationId}] Stall detection cancelled for {Provider}",
                providerState.CorrelationId,
                providerState.ProviderName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[{CorrelationId}] Error in stall detection for {Provider}: {Message}",
                providerState.CorrelationId,
                providerState.ProviderName,
                ex.Message);
        }
    }

    /// <summary>
    /// Updates the latency category based on elapsed time
    /// </summary>
    private void UpdateLatencyCategory(ProviderState state, IHeartbeatStrategy strategy)
    {
        var elapsed = state.ElapsedTime;
        var previousCategory = state.CurrentCategory;

        LatencyCategory newCategory;
        
        if (elapsed.TotalMilliseconds < 30000)
        {
            newCategory = LatencyCategory.Normal;
        }
        else if (elapsed.TotalMilliseconds < 180000)
        {
            newCategory = LatencyCategory.Extended;
        }
        else
        {
            newCategory = LatencyCategory.DeepWait;
        }

        if (state.IsStallSuspected(strategy.StallThreshold))
        {
            newCategory = LatencyCategory.StallSuspected;
        }

        if (previousCategory != newCategory)
        {
            state.UpdateCategory(newCategory);
            
            _logger.LogInformation(
                "[{CorrelationId}] PROVIDER_LATENCY_CATEGORY_CHANGE {Provider}: {From} → {To} (elapsed: {Elapsed}ms)",
                state.CorrelationId,
                state.ProviderName,
                previousCategory,
                newCategory,
                elapsed.TotalMilliseconds);
        }
    }

    /// <summary>
    /// Handles stall suspicion by raising event (does not auto-switch provider)
    /// </summary>
    private void HandleStallSuspicion(ProviderState state)
    {
        if (state.CurrentCategory == LatencyCategory.StallSuspected)
        {
            return;
        }

        state.UpdateCategory(LatencyCategory.StallSuspected);

        var timeSinceHeartbeat = state.TimeSinceLastHeartbeat ?? state.ElapsedTime;

        _logger.LogWarning(
            "[{CorrelationId}] PROVIDER_STALL_SUSPECTED {Provider} - " +
            "No heartbeat for {ElapsedMs}ms (total elapsed: {TotalMs}ms)",
            state.CorrelationId,
            state.ProviderName,
            timeSinceHeartbeat.TotalMilliseconds,
            state.ElapsedTime.TotalMilliseconds);

        var stallEvent = new StallSuspectedEvent
        {
            ProviderName = state.ProviderName,
            ProviderType = state.ProviderType,
            ElapsedSinceLastHeartbeat = timeSinceHeartbeat,
            TotalElapsed = state.ElapsedTime,
            CorrelationId = state.CorrelationId
        };

        OnStallSuspected(stallEvent);
    }

    /// <summary>
    /// Raises the StallSuspected event
    /// </summary>
    private void OnStallSuspected(StallSuspectedEvent e)
    {
        StallSuspected?.Invoke(this, e);
    }
}
