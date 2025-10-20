using System;

namespace Aura.Core.Models.Download;

/// <summary>
/// Event arguments for download progress reporting
/// </summary>
public class DownloadProgressEventArgs : EventArgs
{
    /// <summary>
    /// Number of bytes downloaded so far
    /// </summary>
    public long BytesDownloaded { get; set; }

    /// <summary>
    /// Total number of bytes to download
    /// </summary>
    public long TotalBytes { get; set; }

    /// <summary>
    /// Percentage complete (0-100)
    /// </summary>
    public float PercentComplete { get; set; }

    /// <summary>
    /// Download speed in bytes per second
    /// </summary>
    public double SpeedBytesPerSecond { get; set; }

    /// <summary>
    /// Estimated time remaining
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>
    /// Current status message
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Current URL being downloaded from
    /// </summary>
    public string? CurrentUrl { get; set; }

    /// <summary>
    /// Index of the current mirror being used (null if not using mirrors)
    /// </summary>
    public int? MirrorIndex { get; set; }

    /// <summary>
    /// Current download stage
    /// </summary>
    public DownloadStage Stage { get; set; } = DownloadStage.Downloading;

    /// <summary>
    /// File path being downloaded to
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Unique identifier for this download operation
    /// </summary>
    public string? DownloadId { get; set; }

    /// <summary>
    /// Whether this is the final progress update
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Whether an error occurred
    /// </summary>
    public bool HasError { get; set; }

    /// <summary>
    /// Error message if HasError is true
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Stage of the download process
/// </summary>
public enum DownloadStage
{
    /// <summary>
    /// Initializing download
    /// </summary>
    Initializing = 0,

    /// <summary>
    /// Checking mirrors
    /// </summary>
    CheckingMirrors = 1,

    /// <summary>
    /// Currently downloading
    /// </summary>
    Downloading = 2,

    /// <summary>
    /// Verifying checksum
    /// </summary>
    Verifying = 3,

    /// <summary>
    /// Extracting archive
    /// </summary>
    Extracting = 4,

    /// <summary>
    /// Finalizing installation
    /// </summary>
    Finalizing = 5,

    /// <summary>
    /// Download completed successfully
    /// </summary>
    Completed = 6,

    /// <summary>
    /// Download failed
    /// </summary>
    Failed = 7
}
