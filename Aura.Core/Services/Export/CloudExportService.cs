using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Settings;
using Aura.Core.Services.Storage;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Export;

/// <summary>
/// Service for handling cloud-based export operations
/// </summary>
public interface ICloudExportService
{
    /// <summary>
    /// Upload exported file to cloud storage
    /// </summary>
    Task<CloudUploadResult> UploadExportAsync(
        string filePath,
        string? destinationKey = null,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if cloud storage is configured and available
    /// </summary>
    Task<bool> IsCloudStorageAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get shareable link for an uploaded export
    /// </summary>
    Task<string> GetShareableLinkAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of cloud export service
/// </summary>
public class CloudExportService : ICloudExportService
{
    private readonly ILogger<CloudExportService> _logger;
    private readonly CloudStorageSettings _settings;
    private readonly CloudStorageProviderFactory _providerFactory;

    public CloudExportService(
        ILogger<CloudExportService> logger,
        CloudStorageSettings settings,
        CloudStorageProviderFactory providerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
    }

    public async Task<CloudUploadResult> UploadExportAsync(
        string filePath,
        string? destinationKey = null,
        IProgress<UploadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogWarning("Cloud storage is disabled in settings");
            return new CloudUploadResult
            {
                Success = false,
                ErrorMessage = "Cloud storage is disabled"
            };
        }

        _logger.LogInformation("Uploading export file {FilePath} to cloud storage", filePath);

        try
        {
            var provider = CreateProvider();
            
            var fileName = Path.GetFileName(filePath);
            var key = destinationKey ?? GenerateDestinationKey(fileName);

            _logger.LogInformation("Using cloud provider: {Provider}, Key: {Key}", 
                provider.ProviderName, key);

            var result = await provider.UploadFileAsync(
                filePath,
                key,
                progress,
                cancellationToken
            );

            if (result.Success)
            {
                _logger.LogInformation("Successfully uploaded {FilePath} to cloud storage", filePath);
                
                if (_settings.DeleteLocalAfterUpload)
                {
                    try
                    {
                        File.Delete(filePath);
                        _logger.LogInformation("Deleted local file after successful upload: {FilePath}", filePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete local file after upload: {FilePath}", filePath);
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload export to cloud storage");
            return new CloudUploadResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> IsCloudStorageAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            return false;
        }

        try
        {
            var provider = CreateProvider();
            return await provider.IsAvailableAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check cloud storage availability");
            return false;
        }
    }

    public async Task<string> GetShareableLinkAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            throw new InvalidOperationException("Cloud storage is disabled");
        }

        try
        {
            var provider = CreateProvider();
            var expiration = GetUrlExpiration();
            
            return await provider.GenerateShareableLinkAsync(key, expiration, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate shareable link for key: {Key}", key);
            throw;
        }
    }

    private ICloudStorageProvider CreateProvider()
    {
        var config = _settings.DefaultProvider.ToLowerInvariant() switch
        {
            "aws" or "s3" or "aws s3" => CreateAwsConfig(),
            "azure" or "azure blob" or "azure blob storage" => CreateAzureConfig(),
            "google" or "gcs" or "google cloud storage" => CreateGoogleConfig(),
            _ => throw new InvalidOperationException(
                $"Unknown cloud storage provider: {_settings.DefaultProvider}")
        };

        return _providerFactory.CreateProvider(config);
    }

    private CloudStorageConfig CreateAwsConfig()
    {
        if (_settings.AwsS3 == null)
        {
            throw new InvalidOperationException("AWS S3 settings are not configured");
        }

        return new CloudStorageConfig
        {
            ProviderName = "AWS S3",
            BucketName = _settings.AwsS3.BucketName,
            Region = _settings.AwsS3.Region,
            AccessKey = _settings.AwsS3.AccessKey,
            SecretKey = _settings.AwsS3.SecretKey,
            UsePublicUrls = _settings.AwsS3.UsePublicUrls,
            UrlExpirationTime = TimeSpan.FromHours(_settings.AwsS3.UrlExpirationHours)
        };
    }

    private CloudStorageConfig CreateAzureConfig()
    {
        if (_settings.AzureBlob == null)
        {
            throw new InvalidOperationException("Azure Blob settings are not configured");
        }

        return new CloudStorageConfig
        {
            ProviderName = "Azure Blob Storage",
            BucketName = _settings.AzureBlob.ContainerName,
            Region = "default",
            ConnectionString = _settings.AzureBlob.ConnectionString,
            UsePublicUrls = _settings.AzureBlob.UsePublicUrls,
            UrlExpirationTime = TimeSpan.FromHours(_settings.AzureBlob.UrlExpirationHours)
        };
    }

    private CloudStorageConfig CreateGoogleConfig()
    {
        if (_settings.GoogleCloud == null)
        {
            throw new InvalidOperationException("Google Cloud settings are not configured");
        }

        return new CloudStorageConfig
        {
            ProviderName = "Google Cloud Storage",
            BucketName = _settings.GoogleCloud.BucketName,
            Region = "default",
            UsePublicUrls = _settings.GoogleCloud.UsePublicUrls,
            UrlExpirationTime = TimeSpan.FromHours(_settings.GoogleCloud.UrlExpirationHours),
            CustomSettings = new System.Collections.Generic.Dictionary<string, string>
            {
                ["ProjectId"] = _settings.GoogleCloud.ProjectId ?? string.Empty,
                ["CredentialsJson"] = _settings.GoogleCloud.CredentialsJson ?? string.Empty
            }
        };
    }

    private string GenerateDestinationKey(string fileName)
    {
        var prefix = GetFolderPrefix();
        var timestamp = DateTime.UtcNow.ToString("yyyy/MM/dd");
        return $"{prefix}/{timestamp}/{fileName}";
    }

    private string GetFolderPrefix()
    {
        return _settings.DefaultProvider.ToLowerInvariant() switch
        {
            "aws" or "s3" or "aws s3" => _settings.AwsS3?.FolderPrefix ?? "aura-exports",
            "azure" or "azure blob" or "azure blob storage" => _settings.AzureBlob?.FolderPrefix ?? "aura-exports",
            "google" or "gcs" or "google cloud storage" => _settings.GoogleCloud?.FolderPrefix ?? "aura-exports",
            _ => "aura-exports"
        };
    }

    private TimeSpan? GetUrlExpiration()
    {
        return _settings.DefaultProvider.ToLowerInvariant() switch
        {
            "aws" or "s3" or "aws s3" => TimeSpan.FromHours(_settings.AwsS3?.UrlExpirationHours ?? 24),
            "azure" or "azure blob" or "azure blob storage" => TimeSpan.FromHours(_settings.AzureBlob?.UrlExpirationHours ?? 24),
            "google" or "gcs" or "google cloud storage" => TimeSpan.FromHours(_settings.GoogleCloud?.UrlExpirationHours ?? 24),
            _ => TimeSpan.FromHours(24)
        };
    }
}
