namespace Aura.Core.Services;

/// <summary>
/// Interface for services that can perform health checks
/// </summary>
public interface IHealthCheckableService
{
    /// <summary>
    /// Checks if the service is healthy and can operate
    /// </summary>
    Task<ServiceHealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a service health check operation
/// </summary>
public record ServiceHealthCheckResult(
    bool IsHealthy,
    string? Message = null,
    Exception? Exception = null);
