namespace Aura.Api.Models.ApiModels.V1;

/// <summary>
/// Consistent response model for job status polling.
/// This model provides a normalized structure for both video generation
/// and export jobs to ensure frontend can parse responses consistently.
/// </summary>
public record JobStatusResponse
{
    /// <summary>Unique job identifier</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Normalized job status. Always lowercase:
    /// queued, running, completed, failed, cancelled
    /// </summary>
    public string Status { get; init; } = "queued";

    /// <summary>Progress percentage (0-100)</summary>
    public int Percent { get; init; }

    /// <summary>Current execution stage (e.g., "Script", "Voice", "Rendering")</summary>
    public string Stage { get; init; } = string.Empty;

    /// <summary>Detailed progress message</summary>
    public string? ProgressMessage { get; init; }

    /// <summary>When the job was created</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>When the job started running (null if still queued)</summary>
    public DateTime? StartedAt { get; init; }

    /// <summary>When the job completed (null if not finished)</summary>
    public DateTime? CompletedAt { get; init; }

    /// <summary>Path to the output file for completed jobs</summary>
    public string? OutputPath { get; init; }

    /// <summary>Error message if job failed</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Artifacts produced by the job (video, subtitles, etc.)</summary>
    public List<ArtifactDto>? Artifacts { get; init; }

    /// <summary>Correlation ID for request tracing</summary>
    public string CorrelationId { get; init; } = string.Empty;
}

/// <summary>
/// Artifact information for job output files.
/// </summary>
public record ArtifactDto
{
    /// <summary>Relative path to the artifact</summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>Full file path (alias for compatibility)</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Type of artifact (e.g., "video/mp4", "text/srt")</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Size of the artifact in bytes</summary>
    public long SizeBytes { get; init; }
}
