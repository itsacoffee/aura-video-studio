using System;

namespace Aura.Core.Configuration;

/// <summary>
/// FFmpeg configuration mode indicating how FFmpeg was detected/configured
/// </summary>
public enum FFmpegMode
{
    /// <summary>
    /// FFmpeg not configured or detected
    /// </summary>
    None,
    
    /// <summary>
    /// System-wide installation (found on PATH or common directories)
    /// </summary>
    System,
    
    /// <summary>
    /// App-managed local installation
    /// </summary>
    Local,
    
    /// <summary>
    /// User-specified custom path
    /// </summary>
    Custom
}

/// <summary>
/// Validation result for FFmpeg executable
/// </summary>
public enum FFmpegValidationResult
{
    /// <summary>
    /// FFmpeg is valid and functional
    /// </summary>
    Ok,
    
    /// <summary>
    /// FFmpeg executable not found at specified path
    /// </summary>
    NotFound,
    
    /// <summary>
    /// File exists but is not a valid FFmpeg binary
    /// </summary>
    InvalidBinary,
    
    /// <summary>
    /// FFmpeg found but execution failed or timed out
    /// </summary>
    ExecutionError,
    
    /// <summary>
    /// Network error during download/installation
    /// </summary>
    NetworkError,
    
    /// <summary>
    /// Unknown error occurred
    /// </summary>
    Unknown
}

/// <summary>
/// Persistent FFmpeg configuration with validation tracking
/// </summary>
public class FFmpegConfiguration
{
    /// <summary>
    /// Current FFmpeg mode (how it was detected/configured)
    /// </summary>
    public FFmpegMode Mode { get; set; } = FFmpegMode.None;
    
    /// <summary>
    /// Absolute path to FFmpeg executable (null if not configured)
    /// </summary>
    public string? Path { get; set; }
    
    /// <summary>
    /// FFmpeg version string (if available)
    /// </summary>
    public string? Version { get; set; }
    
    /// <summary>
    /// When FFmpeg was last successfully validated
    /// </summary>
    public DateTime? LastValidatedAt { get; set; }
    
    /// <summary>
    /// Result of last validation attempt
    /// </summary>
    public FFmpegValidationResult LastValidationResult { get; set; } = FFmpegValidationResult.Unknown;
    
    /// <summary>
    /// Detailed error message from last validation (if failed)
    /// </summary>
    public string? LastValidationError { get; set; }
    
    /// <summary>
    /// Source description (e.g., "PATH", "Managed", "Custom")
    /// </summary>
    public string? Source { get; set; }
    
    /// <summary>
    /// Full validation output from ffmpeg -version
    /// </summary>
    public string? ValidationOutput { get; set; }
    
    /// <summary>
    /// Checks if FFmpeg is currently valid based on last validation
    /// </summary>
    public bool IsValid =>
        Mode != FFmpegMode.None &&
        !string.IsNullOrEmpty(Path) &&
        LastValidationResult == FFmpegValidationResult.Ok &&
        LastValidatedAt.HasValue;
    
    /// <summary>
    /// Checks if validation is stale (older than 24 hours)
    /// </summary>
    public bool IsValidationStale =>
        !LastValidatedAt.HasValue ||
        (DateTime.UtcNow - LastValidatedAt.Value) > TimeSpan.FromHours(24);
}
