using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Aura.Core.Services.Storage;
using Aura.Core.Models.Storage;

namespace Aura.Tests.Storage;

public class EnhancedLocalStorageServiceTests : IDisposable
{
    private readonly string _testStorageRoot;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<EnhancedLocalStorageService>> _mockLogger;
    private readonly EnhancedLocalStorageService _service;

    public EnhancedLocalStorageServiceTests()
    {
        _testStorageRoot = Path.Combine(Path.GetTempPath(), $"AuraTests_{Guid.NewGuid()}");
        
        var configDict = new System.Collections.Generic.Dictionary<string, string>
        {
            { "Storage:Local:StorageRoot", _testStorageRoot },
            { "Storage:Local:StorageQuotaBytes", "1073741824" }, // 1GB
            { "Storage:Local:LowSpaceThresholdBytes", "107374182" }, // 100MB
            { "Storage:Local:MaxCacheSizeBytes", "536870912" }, // 512MB
            { "Storage:Local:EnableAutoCacheCleanup", "true" },
            { "Storage:Local:CacheTtlDays", "30" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        _mockLogger = new Mock<ILogger<EnhancedLocalStorageService>>();
        _service = new EnhancedLocalStorageService(_configuration, _mockLogger.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testStorageRoot))
        {
            Directory.Delete(_testStorageRoot, true);
        }
    }

    [Fact]
    public async Task EnsureWorkspaceStructureAsync_CreatesAllFolders()
    {
        // Act
        await _service.EnsureWorkspaceStructureAsync();

        // Assert
        Assert.True(Directory.Exists(Path.Combine(_testStorageRoot, WorkspaceFolders.Projects)));
        Assert.True(Directory.Exists(Path.Combine(_testStorageRoot, WorkspaceFolders.Exports)));
        Assert.True(Directory.Exists(Path.Combine(_testStorageRoot, WorkspaceFolders.Cache)));
        Assert.True(Directory.Exists(Path.Combine(_testStorageRoot, WorkspaceFolders.Temp)));
        Assert.True(Directory.Exists(Path.Combine(_testStorageRoot, WorkspaceFolders.Media)));
        Assert.True(Directory.Exists(Path.Combine(_testStorageRoot, WorkspaceFolders.Thumbnails)));
        Assert.True(Directory.Exists(Path.Combine(_testStorageRoot, WorkspaceFolders.Backups)));
        Assert.True(Directory.Exists(Path.Combine(_testStorageRoot, WorkspaceFolders.Previews)));
    }

    [Fact]
    public async Task GetWorkspacePathAsync_ReturnsCorrectPath()
    {
        // Act
        var path = await _service.GetWorkspacePathAsync(WorkspaceFolders.Projects);

        // Assert
        Assert.Equal(Path.Combine(_testStorageRoot, WorkspaceFolders.Projects), path);
        Assert.True(Directory.Exists(path));
    }

    [Fact]
    public async Task UploadFileAsync_CreatesFile()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var blobUrl = await _service.UploadFileAsync(stream, fileName, "text/plain");

        // Assert
        Assert.StartsWith("local://media/", blobUrl);
        var fileExists = await _service.FileExistsAsync(blobUrl);
        Assert.True(fileExists);
    }

    [Fact]
    public async Task DownloadFileAsync_RetrievesContent()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Hello, World!";
        using var uploadStream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var blobUrl = await _service.UploadFileAsync(uploadStream, fileName, "text/plain");

        // Act
        using var downloadStream = await _service.DownloadFileAsync(blobUrl);
        using var reader = new StreamReader(downloadStream);
        var downloadedContent = await reader.ReadToEndAsync();

        // Assert
        Assert.Equal(content, downloadedContent);
    }

    [Fact]
    public async Task DeleteFileAsync_RemovesFile()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var blobUrl = await _service.UploadFileAsync(stream, fileName, "text/plain");

        // Act
        await _service.DeleteFileAsync(blobUrl);

        // Assert
        var fileExists = await _service.FileExistsAsync(blobUrl);
        Assert.False(fileExists);
    }

    [Fact]
    public async Task GetStorageStatisticsAsync_ReturnsValidStats()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _service.UploadFileAsync(stream, fileName, "text/plain");

        // Act
        var stats = await _service.GetStorageStatisticsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.UsedSizeBytes > 0);
        Assert.True(stats.TotalFiles > 0);
        Assert.Equal(1073741824, stats.QuotaBytes);
    }

    [Fact]
    public async Task GetDiskSpaceInfoAsync_ReturnsValidInfo()
    {
        // Act
        var info = await _service.GetDiskSpaceInfoAsync();

        // Assert
        Assert.NotNull(info);
        Assert.NotEmpty(info.DriveName);
        Assert.True(info.TotalSize > 0);
        Assert.True(info.AvailableSpace >= 0);
    }

    [Fact]
    public async Task CheckStorageQuotaAsync_ReturnsTrueWhenSpaceAvailable()
    {
        // Arrange
        var requiredBytes = 1024; // 1KB

        // Act
        var hasSpace = await _service.CheckStorageQuotaAsync(requiredBytes);

        // Assert
        Assert.True(hasSpace);
    }

    [Fact]
    public async Task AddCacheEntryAsync_CreatesEntry()
    {
        // Arrange
        var key = "test-cache-key";
        var category = "TestCategory";
        var content = "Cached content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        // Act
        var filePath = await _service.AddCacheEntryAsync(key, category, stream);

        // Assert
        Assert.NotEmpty(filePath);
        Assert.True(File.Exists(filePath));

        var entry = await _service.GetCacheEntryAsync(key);
        Assert.NotNull(entry);
        Assert.Equal(key, entry.Key);
        Assert.Equal(category, entry.Category);
    }

    [Fact]
    public async Task RemoveCacheEntryAsync_DeletesEntry()
    {
        // Arrange
        var key = "test-cache-key";
        var category = "TestCategory";
        var content = "Cached content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _service.AddCacheEntryAsync(key, category, stream);

        // Act
        var removed = await _service.RemoveCacheEntryAsync(key);

        // Assert
        Assert.True(removed);
        var entry = await _service.GetCacheEntryAsync(key);
        Assert.Null(entry);
    }

    [Fact]
    public async Task GetCacheStatisticsAsync_ReturnsValidStats()
    {
        // Arrange
        var key = "test-cache-key";
        var category = "TestCategory";
        var content = "Cached content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _service.AddCacheEntryAsync(key, category, stream);

        // Act
        var stats = await _service.GetCacheStatisticsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalEntries > 0);
        Assert.True(stats.TotalSizeBytes > 0);
        Assert.Contains(category, stats.EntriesByCategory.Keys);
    }

    [Fact]
    public async Task CleanupCacheAsync_RemovesExpiredEntries()
    {
        // Arrange
        var key = "test-cache-key";
        var category = "TestCategory";
        var content = "Cached content";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await _service.AddCacheEntryAsync(key, category, stream);

        // Act
        var result = await _service.CleanupCacheAsync(forceAll: true);

        // Assert
        Assert.True(result.EntriesRemoved > 0);
        Assert.True(result.BytesFreed > 0);
    }

    [Fact]
    public async Task SaveProjectFileAsync_CreatesProjectFile()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var content = "{\"id\": \"test\", \"name\": \"Test Project\"}";

        // Act
        var filePath = await _service.SaveProjectFileAsync(projectId, content);

        // Assert
        Assert.NotEmpty(filePath);
        Assert.True(File.Exists(filePath));
    }

    [Fact]
    public async Task LoadProjectFileAsync_RetrievesContent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var content = "{\"id\": \"test\", \"name\": \"Test Project\"}";
        await _service.SaveProjectFileAsync(projectId, content);

        // Act
        var loadedContent = await _service.LoadProjectFileAsync(projectId);

        // Assert
        Assert.NotNull(loadedContent);
        Assert.Equal(content, loadedContent);
    }

    [Fact]
    public async Task ProjectFileExistsAsync_ReturnsTrueWhenExists()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var content = "{\"id\": \"test\", \"name\": \"Test Project\"}";
        await _service.SaveProjectFileAsync(projectId, content);

        // Act
        var exists = await _service.ProjectFileExistsAsync(projectId);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task CreateBackupAsync_CreatesBackupFile()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var content = "{\"id\": \"test\", \"name\": \"Test Project\"}";
        await _service.SaveProjectFileAsync(projectId, content);

        // Act
        var backupPath = await _service.CreateBackupAsync(projectId, "test-backup");

        // Assert
        Assert.NotEmpty(backupPath);
        Assert.True(File.Exists(backupPath));
    }

    [Fact]
    public async Task ListBackupsAsync_ReturnsBackupFiles()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var content = "{\"id\": \"test\", \"name\": \"Test Project\"}";
        await _service.SaveProjectFileAsync(projectId, content);
        await _service.CreateBackupAsync(projectId, "backup1");
        await _service.CreateBackupAsync(projectId, "backup2");

        // Act
        var backups = await _service.ListBackupsAsync(projectId);

        // Assert
        Assert.NotEmpty(backups);
        Assert.True(backups.Count >= 2);
    }

    [Fact]
    public async Task RestoreBackupAsync_RestoresProjectContent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var originalContent = "{\"id\": \"test\", \"name\": \"Original\"}";
        await _service.SaveProjectFileAsync(projectId, originalContent);
        var backupPath = await _service.CreateBackupAsync(projectId, "test-backup");
        var backupFileName = Path.GetFileName(backupPath);

        // Modify project
        var modifiedContent = "{\"id\": \"test\", \"name\": \"Modified\"}";
        await _service.SaveProjectFileAsync(projectId, modifiedContent);

        // Act
        var restoredContent = await _service.RestoreBackupAsync(projectId, backupFileName);

        // Assert
        Assert.NotNull(restoredContent);
        Assert.Equal(originalContent, restoredContent);
    }

    [Fact]
    public async Task CopyFileAsync_CreatesFileCopy()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var sourceBlobUrl = await _service.UploadFileAsync(stream, fileName, "text/plain");

        // Act
        var destBlobUrl = await _service.CopyFileAsync(sourceBlobUrl, "test-copy.txt");

        // Assert
        Assert.NotEqual(sourceBlobUrl, destBlobUrl);
        var destExists = await _service.FileExistsAsync(destBlobUrl);
        Assert.True(destExists);
    }

    [Fact]
    public async Task GetFileSizeAsync_ReturnsCorrectSize()
    {
        // Arrange
        var fileName = "test.txt";
        var content = "Hello, World!";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var blobUrl = await _service.UploadFileAsync(stream, fileName, "text/plain");

        // Act
        var size = await _service.GetFileSizeAsync(blobUrl);

        // Assert
        Assert.Equal(content.Length, size);
    }
}
