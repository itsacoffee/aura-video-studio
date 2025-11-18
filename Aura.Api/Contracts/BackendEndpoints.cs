namespace Aura.Api.Contracts;

/// <summary>
/// Backend endpoint path constants.
/// Provides a single source of truth for endpoint paths shared across Electron main process,
/// backend API, and frontend to prevent divergence and ensure consistency.
/// </summary>
public static class BackendEndpoints
{
    /// <summary>
    /// Liveness health check endpoint.
    /// Returns 200 if the HTTP server is running, used for fast startup detection.
    /// </summary>
    public const string HealthLive = "/health/live";

    /// <summary>
    /// Readiness health check endpoint.
    /// Returns 200/503 based on dependency availability (database, services, etc.).
    /// </summary>
    public const string HealthReady = "/health/ready";

    /// <summary>
    /// Base path for jobs API endpoints.
    /// </summary>
    public const string JobsBase = "/api/jobs";

    /// <summary>
    /// Server-Sent Events (SSE) endpoint template for job progress updates.
    /// Use string.Replace("{id}", jobId) to build the full URL.
    /// </summary>
    public const string JobEventsTemplate = "/api/jobs/{id}/events";

    /// <summary>
    /// Builds the full job events endpoint path for a specific job ID.
    /// </summary>
    /// <param name="jobId">The job ID to subscribe to progress updates for.</param>
    /// <returns>The full SSE endpoint path with the job ID substituted.</returns>
    public static string BuildJobEventsPath(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Job ID cannot be null or empty", nameof(jobId));
        }

        return JobEventsTemplate.Replace("{id}", jobId);
    }
}
