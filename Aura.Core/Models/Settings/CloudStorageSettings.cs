using System;
using System.Collections.Generic;

namespace Aura.Core.Models.Settings;

/// <summary>
/// Cloud storage configuration settings
/// </summary>
public class CloudStorageSettings
{
    /// <summary>
    /// Whether cloud storage integration is enabled
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Default cloud storage provider to use
    /// </summary>
    public string DefaultProvider { get; set; } = "AWS S3";

    /// <summary>
    /// Auto-upload exports to cloud after completion
    /// </summary>
    public bool AutoUploadOnExport { get; set; }

    /// <summary>
    /// Delete local file after successful cloud upload
    /// </summary>
    public bool DeleteLocalAfterUpload { get; set; }

    /// <summary>
    /// AWS S3 configuration
    /// </summary>
    public AwsS3Settings? AwsS3 { get; set; }

    /// <summary>
    /// Azure Blob Storage configuration
    /// </summary>
    public AzureBlobSettings? AzureBlob { get; set; }

    /// <summary>
    /// Google Cloud Storage configuration
    /// </summary>
    public GoogleCloudSettings? GoogleCloud { get; set; }
}

/// <summary>
/// AWS S3 configuration
/// </summary>
public class AwsS3Settings
{
    public string BucketName { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string FolderPrefix { get; set; } = "aura-exports";
    public bool UsePublicUrls { get; set; } = true;
    public int UrlExpirationHours { get; set; } = 24;
}

/// <summary>
/// Azure Blob Storage configuration
/// </summary>
public class AzureBlobSettings
{
    public string ContainerName { get; set; } = string.Empty;
    public string? ConnectionString { get; set; }
    public string FolderPrefix { get; set; } = "aura-exports";
    public bool UsePublicUrls { get; set; } = true;
    public int UrlExpirationHours { get; set; } = 24;
}

/// <summary>
/// Google Cloud Storage configuration
/// </summary>
public class GoogleCloudSettings
{
    public string BucketName { get; set; } = string.Empty;
    public string? ProjectId { get; set; }
    public string? CredentialsJson { get; set; }
    public string FolderPrefix { get; set; } = "aura-exports";
    public bool UsePublicUrls { get; set; } = true;
    public int UrlExpirationHours { get; set; } = 24;
}
