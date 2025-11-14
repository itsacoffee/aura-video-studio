using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Settings;

/// <summary>
/// Comprehensive export settings for video output configuration
/// </summary>
public class ExportSettings
{
    /// <summary>
    /// Watermark configuration
    /// </summary>
    public WatermarkSettings Watermark { get; set; } = new();

    /// <summary>
    /// Output file naming patterns
    /// </summary>
    public NamingPatternSettings NamingPattern { get; set; } = new();

    /// <summary>
    /// Auto-upload destinations after export
    /// </summary>
    public List<UploadDestination> UploadDestinations { get; set; } = new();

    /// <summary>
    /// Default export preset to use
    /// </summary>
    public string DefaultPreset { get; set; } = "YouTube1080p";

    /// <summary>
    /// Automatically open output folder after export
    /// </summary>
    public bool AutoOpenOutputFolder { get; set; } = true;

    /// <summary>
    /// Automatically start upload after export completion
    /// </summary>
    public bool AutoUploadOnComplete { get; set; }

    /// <summary>
    /// Generate thumbnail image alongside video
    /// </summary>
    public bool GenerateThumbnail { get; set; } = true;

    /// <summary>
    /// Generate SRT subtitle file if available
    /// </summary>
    public bool GenerateSubtitles { get; set; }

    /// <summary>
    /// Keep intermediate files after export
    /// </summary>
    public bool KeepIntermediateFiles { get; set; }
}

/// <summary>
/// Watermark configuration settings
/// </summary>
public class WatermarkSettings
{
    /// <summary>
    /// Enable watermark overlay on exported videos
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Watermark type (image or text)
    /// </summary>
    public WatermarkType Type { get; set; } = WatermarkType.Text;

    /// <summary>
    /// Path to watermark image file (for Image type)
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Watermark text content (for Text type)
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Watermark position on video
    /// </summary>
    public WatermarkPosition Position { get; set; } = WatermarkPosition.BottomRight;

    /// <summary>
    /// Opacity of watermark (0.0 to 1.0)
    /// </summary>
    public double Opacity { get; set; } = 0.7;

    /// <summary>
    /// Scale of watermark relative to video (0.0 to 1.0)
    /// </summary>
    public double Scale { get; set; } = 0.1;

    /// <summary>
    /// Horizontal offset from position in pixels
    /// </summary>
    public int OffsetX { get; set; } = 20;

    /// <summary>
    /// Vertical offset from position in pixels
    /// </summary>
    public int OffsetY { get; set; } = 20;

    /// <summary>
    /// Font family for text watermarks
    /// </summary>
    public string FontFamily { get; set; } = "Arial";

    /// <summary>
    /// Font size for text watermarks
    /// </summary>
    public int FontSize { get; set; } = 24;

    /// <summary>
    /// Font color for text watermarks (hex format)
    /// </summary>
    public string FontColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Enable shadow for text watermarks
    /// </summary>
    public bool EnableShadow { get; set; } = true;
}

/// <summary>
/// Watermark type enumeration
/// </summary>
public enum WatermarkType
{
    /// <summary>Image watermark</summary>
    Image,
    
    /// <summary>Text watermark</summary>
    Text
}

/// <summary>
/// Watermark position enumeration
/// </summary>
public enum WatermarkPosition
{
    /// <summary>Top left corner</summary>
    TopLeft,
    
    /// <summary>Top center</summary>
    TopCenter,
    
    /// <summary>Top right corner</summary>
    TopRight,
    
    /// <summary>Middle left</summary>
    MiddleLeft,
    
    /// <summary>Center</summary>
    Center,
    
    /// <summary>Middle right</summary>
    MiddleRight,
    
    /// <summary>Bottom left corner</summary>
    BottomLeft,
    
    /// <summary>Bottom center</summary>
    BottomCenter,
    
    /// <summary>Bottom right corner</summary>
    BottomRight
}

/// <summary>
/// Output file naming pattern settings
/// </summary>
public class NamingPatternSettings
{
    /// <summary>
    /// Naming pattern template with placeholders
    /// Available: {project}, {date}, {time}, {preset}, {resolution}, {duration}, {counter}
    /// </summary>
    public string Pattern { get; set; } = "{project}_{date}_{time}";

    /// <summary>
    /// Use sanitized filenames (remove special characters)
    /// </summary>
    public bool SanitizeFilenames { get; set; } = true;

    /// <summary>
    /// Date format for {date} placeholder
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd";

    /// <summary>
    /// Time format for {time} placeholder
    /// </summary>
    public string TimeFormat { get; set; } = "HHmmss";

    /// <summary>
    /// Starting number for {counter} placeholder
    /// </summary>
    public int CounterStart { get; set; } = 1;

    /// <summary>
    /// Number of digits for {counter} placeholder (zero-padded)
    /// </summary>
    public int CounterDigits { get; set; } = 3;

    /// <summary>
    /// Custom prefix to prepend to all filenames
    /// </summary>
    public string CustomPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Custom suffix to append before extension
    /// </summary>
    public string CustomSuffix { get; set; } = string.Empty;

    /// <summary>
    /// Replace spaces with underscores
    /// </summary>
    public bool ReplaceSpaces { get; set; } = true;

    /// <summary>
    /// Convert to lowercase
    /// </summary>
    public bool ForceLowercase { get; set; }
}

/// <summary>
/// Upload destination configuration
/// </summary>
public class UploadDestination
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name for this destination
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Destination type
    /// </summary>
    public UploadDestinationType Type { get; set; } = UploadDestinationType.LocalFolder;

    /// <summary>
    /// Whether this destination is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Local folder path (for LocalFolder type)
    /// </summary>
    public string LocalPath { get; set; } = string.Empty;

    /// <summary>
    /// FTP/SFTP host
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// FTP/SFTP port
    /// </summary>
    public int Port { get; set; } = 22;

    /// <summary>
    /// FTP/SFTP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// FTP/SFTP password (encrypted)
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Remote path on FTP/SFTP server
    /// </summary>
    public string RemotePath { get; set; } = "/";

    /// <summary>
    /// AWS S3 bucket name
    /// </summary>
    public string S3BucketName { get; set; } = string.Empty;

    /// <summary>
    /// AWS S3 region
    /// </summary>
    public string S3Region { get; set; } = "us-east-1";

    /// <summary>
    /// AWS S3 access key
    /// </summary>
    public string S3AccessKey { get; set; } = string.Empty;

    /// <summary>
    /// AWS S3 secret key (encrypted)
    /// </summary>
    public string S3SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Azure blob container name
    /// </summary>
    public string AzureContainerName { get; set; } = string.Empty;

    /// <summary>
    /// Azure connection string (encrypted)
    /// </summary>
    public string AzureConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Google Drive folder ID
    /// </summary>
    public string GoogleDriveFolderId { get; set; } = string.Empty;

    /// <summary>
    /// Dropbox folder path
    /// </summary>
    public string DropboxPath { get; set; } = string.Empty;

    /// <summary>
    /// Whether to delete local file after successful upload
    /// </summary>
    public bool DeleteAfterUpload { get; set; }

    /// <summary>
    /// Maximum retries on upload failure
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Upload timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 300;
}

/// <summary>
/// Upload destination type enumeration
/// </summary>
public enum UploadDestinationType
{
    /// <summary>Local folder</summary>
    LocalFolder,
    
    /// <summary>FTP server</summary>
    FTP,
    
    /// <summary>SFTP server</summary>
    SFTP,
    
    /// <summary>Amazon S3</summary>
    S3,
    
    /// <summary>Azure Blob Storage</summary>
    AzureBlob,
    
    /// <summary>Google Drive</summary>
    GoogleDrive,
    
    /// <summary>Dropbox</summary>
    Dropbox,
    
    /// <summary>Custom webhook</summary>
    Webhook
}
