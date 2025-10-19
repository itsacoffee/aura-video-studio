namespace Aura.Api.Models;

/// <summary>
/// Health check response model
/// </summary>
public record HealthCheckResponse(
    string Status,
    IReadOnlyList<SubCheckResult> Checks,
    IReadOnlyList<string> Errors
);

/// <summary>
/// Individual sub-check result
/// </summary>
public record SubCheckResult(
    string Name,
    string Status,
    string? Message = null,
    Dictionary<string, object>? Details = null
);

/// <summary>
/// Health check status constants
/// </summary>
public static class HealthStatus
{
    public const string Healthy = "healthy";
    public const string Degraded = "degraded";
    public const string Unhealthy = "unhealthy";
}
