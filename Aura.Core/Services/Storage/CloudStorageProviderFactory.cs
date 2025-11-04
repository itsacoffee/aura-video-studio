using System;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Storage;

/// <summary>
/// Factory for creating cloud storage provider instances
/// </summary>
public class CloudStorageProviderFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public CloudStorageProviderFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    /// <summary>
    /// Create a cloud storage provider based on configuration
    /// </summary>
    public ICloudStorageProvider CreateProvider(CloudStorageConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return config.ProviderName.ToLowerInvariant() switch
        {
            "aws" or "s3" or "aws s3" => new AwsS3StorageProvider(
                _loggerFactory.CreateLogger<AwsS3StorageProvider>(),
                config
            ),
            "azure" or "azure blob" or "azure blob storage" => new AzureBlobStorageProvider(
                _loggerFactory.CreateLogger<AzureBlobStorageProvider>(),
                config
            ),
            "google" or "gcs" or "google cloud storage" => new GoogleCloudStorageProvider(
                _loggerFactory.CreateLogger<GoogleCloudStorageProvider>(),
                config
            ),
            _ => throw new ArgumentException(
                $"Unknown cloud storage provider: {config.ProviderName}. " +
                $"Supported providers: AWS S3, Azure Blob Storage, Google Cloud Storage",
                nameof(config)
            )
        };
    }

    /// <summary>
    /// Get available provider names
    /// </summary>
    public static string[] GetAvailableProviders()
    {
        return new[]
        {
            "AWS S3",
            "Azure Blob Storage",
            "Google Cloud Storage"
        };
    }
}
