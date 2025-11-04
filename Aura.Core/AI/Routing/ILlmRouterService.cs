using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Providers;

namespace Aura.Core.AI.Routing;

/// <summary>
/// Service that routes LLM requests to the best available provider based on task type,
/// health, latency, cost, and quality scoring.
/// </summary>
public interface ILlmRouterService
{
    /// <summary>
    /// Select the best provider for a given task type and constraints.
    /// </summary>
    Task<RoutingDecision> SelectProviderAsync(
        TaskType taskType,
        RoutingConstraints? constraints = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get the provider instance for a routing decision.
    /// </summary>
    ILlmProvider GetProvider(RoutingDecision decision);

    /// <summary>
    /// Get health status for all providers.
    /// </summary>
    Task<IReadOnlyList<ProviderHealthStatus>> GetHealthStatusAsync(CancellationToken ct = default);

    /// <summary>
    /// Get performance metrics for all providers.
    /// </summary>
    Task<IReadOnlyList<ProviderMetrics>> GetMetricsAsync(CancellationToken ct = default);

    /// <summary>
    /// Record the result of a provider request for metrics tracking.
    /// </summary>
    Task RecordRequestAsync(
        string providerName,
        TaskType taskType,
        bool success,
        double latencyMs,
        decimal cost,
        CancellationToken ct = default);

    /// <summary>
    /// Manually mark a provider as unavailable (e.g., after API key expiration).
    /// </summary>
    Task MarkProviderUnavailableAsync(string providerName, TimeSpan? duration = null, CancellationToken ct = default);

    /// <summary>
    /// Manually reset a provider to healthy state.
    /// </summary>
    Task ResetProviderHealthAsync(string providerName, CancellationToken ct = default);
}
