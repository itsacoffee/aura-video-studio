using System;

namespace Aura.Core.Models;

/// <summary>
/// Quality preset for proxy media generation
/// </summary>
public enum ProxyQuality
{
    /// <summary>
    /// Draft quality - lowest resolution, fastest generation (480p)
    /// </summary>
    Draft,
    
    /// <summary>
    /// Preview quality - balanced resolution and quality (720p)
    /// </summary>
    Preview,
    
    /// <summary>
    /// High quality - higher resolution for detailed review (1080p)
    /// </summary>
    High
}

/// <summary>
/// Status of proxy media generation
/// </summary>
public enum ProxyStatus
{
    /// <summary>
    /// Proxy generation not started
    /// </summary>
    NotStarted,
    
    /// <summary>
    /// Proxy generation queued
    /// </summary>
    Queued,
    
    /// <summary>
    /// Proxy generation in progress
    /// </summary>
    Processing,
    
    /// <summary>
    /// Proxy generation completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Proxy generation failed
    /// </summary>
    Failed
}

/// <summary>
/// Metadata for proxy media files
/// </summary>
public class ProxyMediaMetadata
{
    /// <summary>
    /// Unique identifier for the proxy
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to source media file
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to proxy media file
    /// </summary>
    public string ProxyPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Quality level of the proxy
    /// </summary>
    public ProxyQuality Quality { get; set; }
    
    /// <summary>
    /// Current status of proxy generation
    /// </summary>
    public ProxyStatus Status { get; set; }
    
    /// <summary>
    /// When the proxy was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// When the proxy was last accessed
    /// </summary>
    public DateTime LastAccessedAt { get; set; }
    
    /// <summary>
    /// Size of proxy file in bytes
    /// </summary>
    public long FileSizeBytes { get; set; }
    
    /// <summary>
    /// Original file size in bytes
    /// </summary>
    public long SourceFileSizeBytes { get; set; }
    
    /// <summary>
    /// Resolution width
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Resolution height
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Video bitrate in kbps
    /// </summary>
    public int BitrateKbps { get; set; }
    
    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double ProgressPercent { get; set; }
}

/// <summary>
/// Configuration for proxy media generation
/// </summary>
public class ProxyGenerationOptions
{
    /// <summary>
    /// Target quality for proxy
    /// </summary>
    public ProxyQuality Quality { get; set; } = ProxyQuality.Preview;
    
    /// <summary>
    /// Whether to generate in background
    /// </summary>
    public bool BackgroundGeneration { get; set; } = true;
    
    /// <summary>
    /// Priority for generation queue (higher = more important)
    /// </summary>
    public int Priority { get; set; } = 0;
    
    /// <summary>
    /// Whether to overwrite existing proxy
    /// </summary>
    public bool Overwrite { get; set; } = false;
}
