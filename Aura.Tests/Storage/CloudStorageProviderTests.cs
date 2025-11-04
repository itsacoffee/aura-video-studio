using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Services.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Storage;

public class CloudStorageProviderTests
{
    private readonly Mock<ILogger<AwsS3StorageProvider>> _s3LoggerMock;
    private readonly Mock<ILogger<AzureBlobStorageProvider>> _azureLoggerMock;
    private readonly Mock<ILogger<GoogleCloudStorageProvider>> _gcsLoggerMock;

    public CloudStorageProviderTests()
    {
        _s3LoggerMock = new Mock<ILogger<AwsS3StorageProvider>>();
        _azureLoggerMock = new Mock<ILogger<AzureBlobStorageProvider>>();
        _gcsLoggerMock = new Mock<ILogger<GoogleCloudStorageProvider>>();
    }

    [Fact]
    public void AwsS3Provider_Constructor_RequiresBucketName()
    {
        // Arrange
        var config = new CloudStorageConfig
        {
            ProviderName = "AWS S3",
            BucketName = "",
            Region = "us-east-1"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AwsS3StorageProvider(_s3LoggerMock.Object, config)
        );
    }

    [Fact]
    public void AzureBlobProvider_Constructor_RequiresContainerName()
    {
        // Arrange
        var config = new CloudStorageConfig
        {
            ProviderName = "Azure Blob Storage",
            BucketName = "",
            Region = "eastus"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new AzureBlobStorageProvider(_azureLoggerMock.Object, config)
        );
    }

    [Fact]
    public void GoogleCloudProvider_Constructor_RequiresBucketName()
    {
        // Arrange
        var config = new CloudStorageConfig
        {
            ProviderName = "Google Cloud Storage",
            BucketName = "",
            Region = "us-central1"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new GoogleCloudStorageProvider(_gcsLoggerMock.Object, config)
        );
    }

    [Fact]
    public async Task AwsS3Provider_IsAvailable_ReturnsFalseWithoutCredentials()
    {
        // Arrange
        var config = new CloudStorageConfig
        {
            ProviderName = "AWS S3",
            BucketName = "test-bucket",
            Region = "us-east-1"
        };
        var provider = new AwsS3StorageProvider(_s3LoggerMock.Object, config);

        // Act
        var isAvailable = await provider.IsAvailableAsync();

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task AzureBlobProvider_IsAvailable_ReturnsFalseWithoutConnectionString()
    {
        // Arrange
        var config = new CloudStorageConfig
        {
            ProviderName = "Azure Blob Storage",
            BucketName = "test-container",
            Region = "eastus"
        };
        var provider = new AzureBlobStorageProvider(_azureLoggerMock.Object, config);

        // Act
        var isAvailable = await provider.IsAvailableAsync();

        // Assert
        Assert.False(isAvailable);
    }

    [Fact]
    public async Task UploadProgress_CalculatesPercentCorrectly()
    {
        // Arrange
        var progress = new UploadProgress
        {
            BytesTransferred = 5000,
            TotalBytes = 10000,
            Elapsed = TimeSpan.FromSeconds(5)
        };

        // Act
        var percent = progress.PercentComplete;

        // Assert
        Assert.Equal(50.0, percent);
    }

    [Fact]
    public void CloudStorageConfig_RequiredProperties_AreValidated()
    {
        // Arrange & Act
        var config = new CloudStorageConfig
        {
            ProviderName = "AWS S3",
            BucketName = "my-bucket",
            Region = "us-west-2",
            AccessKey = "test-key",
            SecretKey = "test-secret"
        };

        // Assert
        Assert.NotNull(config);
        Assert.Equal("AWS S3", config.ProviderName);
        Assert.Equal("my-bucket", config.BucketName);
        Assert.Equal("us-west-2", config.Region);
    }

    [Fact]
    public async Task AwsS3Provider_UploadFile_ReturnsErrorForNonExistentFile()
    {
        // Arrange
        var config = new CloudStorageConfig
        {
            ProviderName = "AWS S3",
            BucketName = "test-bucket",
            Region = "us-east-1",
            AccessKey = "test-key",
            SecretKey = "test-secret"
        };
        var provider = new AwsS3StorageProvider(_s3LoggerMock.Object, config);

        // Act
        var result = await provider.UploadFileAsync(
            "/non/existent/file.mp4",
            "test/file.mp4"
        );

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File not found", result.ErrorMessage);
    }

    [Fact]
    public async Task CloudUploadResult_ContainsExpectedMetadata()
    {
        // Arrange
        var result = new CloudUploadResult
        {
            Success = true,
            Url = "https://example.com/file.mp4",
            Key = "test/file.mp4",
            FileSize = 1024000,
            Metadata = new System.Collections.Generic.Dictionary<string, string>
            {
                ["ContentType"] = "video/mp4",
                ["UploadedAt"] = DateTime.UtcNow.ToString("O")
            }
        };

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.Url);
        Assert.NotEmpty(result.Key);
        Assert.True(result.FileSize > 0);
        Assert.Contains("ContentType", result.Metadata.Keys);
    }

    [Theory]
    [InlineData("aws", typeof(AwsS3StorageProvider))]
    [InlineData("s3", typeof(AwsS3StorageProvider))]
    [InlineData("azure", typeof(AzureBlobStorageProvider))]
    [InlineData("google", typeof(GoogleCloudStorageProvider))]
    [InlineData("gcs", typeof(GoogleCloudStorageProvider))]
    public void CloudStorageProviderFactory_CreatesCorrectProvider(string providerName, Type expectedType)
    {
        // Arrange
        var loggerFactory = new LoggerFactory();
        var factory = new CloudStorageProviderFactory(loggerFactory);
        var config = new CloudStorageConfig
        {
            ProviderName = providerName,
            BucketName = "test-bucket",
            Region = "us-east-1"
        };

        // Act
        var provider = factory.CreateProvider(config);

        // Assert
        Assert.IsType(expectedType, provider);
    }

    [Fact]
    public void CloudStorageProviderFactory_ThrowsForUnknownProvider()
    {
        // Arrange
        var loggerFactory = new LoggerFactory();
        var factory = new CloudStorageProviderFactory(loggerFactory);
        var config = new CloudStorageConfig
        {
            ProviderName = "unknown-provider",
            BucketName = "test-bucket",
            Region = "us-east-1"
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.CreateProvider(config));
    }

    [Fact]
    public void CloudStorageProviderFactory_GetAvailableProviders_ReturnsExpectedList()
    {
        // Act
        var providers = CloudStorageProviderFactory.GetAvailableProviders();

        // Assert
        Assert.NotEmpty(providers);
        Assert.Contains("AWS S3", providers);
        Assert.Contains("Azure Blob Storage", providers);
        Assert.Contains("Google Cloud Storage", providers);
    }
}
