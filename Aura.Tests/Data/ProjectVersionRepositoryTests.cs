using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Data;

/// <summary>
/// Tests for project version repository with deduplication
/// </summary>
public class ProjectVersionRepositoryTests : IDisposable
{
    private readonly AuraDbContext _context;
    private readonly ProjectVersionRepository _repository;
    private readonly Mock<ILogger<ProjectVersionRepository>> _loggerMock;

    public ProjectVersionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuraDbContext(options);
        _loggerMock = new Mock<ILogger<ProjectVersionRepository>>();
        _repository = new ProjectVersionRepository(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateVersionAsync_CreatesVersionWithIncrementedNumber()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var version = new ProjectVersionEntity
        {
            ProjectId = projectId,
            Name = "Version 1",
            VersionType = "Manual"
        };

        // Act
        var result = await _repository.CreateVersionAsync(version);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.VersionNumber);
    }

    [Fact]
    public async Task CreateVersionAsync_AutoIncrementsVersionNumber()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        
        var v1 = new ProjectVersionEntity { ProjectId = projectId, Name = "V1", VersionType = "Manual" };
        var v2 = new ProjectVersionEntity { ProjectId = projectId, Name = "V2", VersionType = "Manual" };
        var v3 = new ProjectVersionEntity { ProjectId = projectId, Name = "V3", VersionType = "Manual" };

        // Act
        var result1 = await _repository.CreateVersionAsync(v1);
        var result2 = await _repository.CreateVersionAsync(v2);
        var result3 = await _repository.CreateVersionAsync(v3);

        // Assert
        Assert.Equal(1, result1.VersionNumber);
        Assert.Equal(2, result2.VersionNumber);
        Assert.Equal(3, result3.VersionNumber);
    }

    [Fact]
    public async Task GetVersionsAsync_ReturnsAllVersions()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V1", VersionType = "Manual" });
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V2", VersionType = "Manual" });
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V3", VersionType = "Manual" });

        // Act
        var versions = await _repository.GetVersionsAsync(projectId);

        // Assert
        Assert.Equal(3, versions.Count);
    }

    [Fact]
    public async Task GetVersionsAsync_ExcludesDeletedByDefault()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var v1 = await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V1", VersionType = "Manual" });
        var v2 = await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V2", VersionType = "Manual" });
        await _repository.DeleteVersionAsync(v1.Id);

        // Act
        var versions = await _repository.GetVersionsAsync(projectId);

        // Assert
        Assert.Single(versions);
        Assert.Equal("V2", versions[0].Name);
    }

    [Fact]
    public async Task GetVersionsAsync_IncludesDeletedWhenRequested()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var v1 = await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V1", VersionType = "Manual" });
        await _repository.DeleteVersionAsync(v1.Id);

        // Act
        var versions = await _repository.GetVersionsAsync(projectId, includeDeleted: true);

        // Assert
        Assert.Single(versions);
        Assert.True(versions[0].IsDeleted);
    }

    [Fact]
    public async Task GetVersionByIdAsync_ReturnsVersion()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var version = await _repository.CreateVersionAsync(
            new ProjectVersionEntity { ProjectId = projectId, Name = "Test Version", VersionType = "Manual" });

        // Act
        var result = await _repository.GetVersionByIdAsync(version.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(version.Id, result.Id);
        Assert.Equal("Test Version", result.Name);
    }

    [Fact]
    public async Task GetVersionByNumberAsync_ReturnsCorrectVersion()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V1", VersionType = "Manual" });
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V2", VersionType = "Manual" });
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V3", VersionType = "Manual" });

        // Act
        var v2 = await _repository.GetVersionByNumberAsync(projectId, 2);

        // Assert
        Assert.NotNull(v2);
        Assert.Equal("V2", v2.Name);
        Assert.Equal(2, v2.VersionNumber);
    }

    [Fact]
    public async Task GetLatestVersionAsync_ReturnsNewestVersion()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V1", VersionType = "Manual" });
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V2", VersionType = "Manual" });
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "V3", VersionType = "Manual" });

        // Act
        var latest = await _repository.GetLatestVersionAsync(projectId);

        // Assert
        Assert.NotNull(latest);
        Assert.Equal("V3", latest.Name);
        Assert.Equal(3, latest.VersionNumber);
    }

    [Fact]
    public async Task UpdateVersionMetadataAsync_UpdatesNameAndDescription()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var version = await _repository.CreateVersionAsync(
            new ProjectVersionEntity { ProjectId = projectId, Name = "Original", VersionType = "Manual" });

        // Act
        await _repository.UpdateVersionMetadataAsync(
            version.Id,
            "Updated Name",
            "Updated Description",
            null);

        // Assert
        var updated = await _repository.GetVersionByIdAsync(version.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
        Assert.Equal("Updated Description", updated.Description);
    }

    [Fact]
    public async Task UpdateVersionMetadataAsync_CanMarkAsImportant()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var version = await _repository.CreateVersionAsync(
            new ProjectVersionEntity { ProjectId = projectId, Name = "Version", VersionType = "Manual" });

        // Act
        await _repository.UpdateVersionMetadataAsync(version.Id, null, null, isMarkedImportant: true);

        // Assert
        var updated = await _repository.GetVersionByIdAsync(version.Id);
        Assert.NotNull(updated);
        Assert.True(updated.IsMarkedImportant);
    }

    [Fact]
    public async Task DeleteVersionAsync_SoftDeletesVersion()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var version = await _repository.CreateVersionAsync(
            new ProjectVersionEntity { ProjectId = projectId, Name = "To Delete", VersionType = "Manual" });

        // Act
        await _repository.DeleteVersionAsync(version.Id);

        // Assert
        var deleted = await _repository.GetVersionByIdAsync(version.Id);
        Assert.Null(deleted); // Should be filtered out by query filter
    }

    [Fact]
    public async Task StoreContentBlobAsync_CreatesNewBlob()
    {
        // Arrange
        var content = "{\"test\": \"data\"}";

        // Act
        var hash = await _repository.StoreContentBlobAsync(content, "Brief");

        // Assert
        Assert.NotEmpty(hash);
        var blob = await _repository.GetContentBlobAsync(hash);
        Assert.NotNull(blob);
        Assert.Equal(content, blob.Content);
        Assert.Equal(1, blob.ReferenceCount);
    }

    [Fact]
    public async Task StoreContentBlobAsync_DeduplicatesIdenticalContent()
    {
        // Arrange
        var content = "{\"test\": \"data\"}";

        // Act
        var hash1 = await _repository.StoreContentBlobAsync(content, "Brief");
        var hash2 = await _repository.StoreContentBlobAsync(content, "Brief");

        // Assert
        Assert.Equal(hash1, hash2); // Same content = same hash
        var blob = await _repository.GetContentBlobAsync(hash1);
        Assert.NotNull(blob);
        Assert.Equal(2, blob.ReferenceCount); // Reference count incremented
    }

    [Fact]
    public async Task GetContentBlobAsync_ReturnsBlob()
    {
        // Arrange
        var content = "{\"data\": \"test\"}";
        var hash = await _repository.StoreContentBlobAsync(content, "Plan");

        // Act
        var blob = await _repository.GetContentBlobAsync(hash);

        // Assert
        Assert.NotNull(blob);
        Assert.Equal(hash, blob.ContentHash);
        Assert.Equal(content, blob.Content);
        Assert.Equal("Plan", blob.ContentType);
    }

    [Fact]
    public async Task DecrementBlobReferenceAsync_DecrementsCount()
    {
        // Arrange
        var content = "{\"test\": \"data\"}";
        var hash = await _repository.StoreContentBlobAsync(content, "Brief");
        await _repository.StoreContentBlobAsync(content, "Brief"); // Increment to 2

        // Act
        await _repository.DecrementBlobReferenceAsync(hash);

        // Assert
        var blob = await _repository.GetContentBlobAsync(hash);
        Assert.NotNull(blob);
        Assert.Equal(1, blob.ReferenceCount);
    }

    [Fact]
    public async Task DecrementBlobReferenceAsync_RemovesBlobWhenCountReachesZero()
    {
        // Arrange
        var content = "{\"test\": \"data\"}";
        var hash = await _repository.StoreContentBlobAsync(content, "Brief");

        // Act
        await _repository.DecrementBlobReferenceAsync(hash);

        // Assert
        var blob = await _repository.GetContentBlobAsync(hash);
        Assert.Null(blob); // Should be removed
    }

    [Fact]
    public async Task GetVersionsByTypeAsync_ReturnsVersionsOfSpecificType()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "Manual 1", VersionType = "Manual" });
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "Autosave 1", VersionType = "Autosave" });
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "Manual 2", VersionType = "Manual" });
        await _repository.CreateVersionAsync(new ProjectVersionEntity { ProjectId = projectId, Name = "RestorePoint 1", VersionType = "RestorePoint" });

        // Act
        var manualVersions = await _repository.GetVersionsByTypeAsync(projectId, "Manual");
        var autosaveVersions = await _repository.GetVersionsByTypeAsync(projectId, "Autosave");

        // Assert
        Assert.Equal(2, manualVersions.Count);
        Assert.Single(autosaveVersions);
        Assert.All(manualVersions, v => Assert.Equal("Manual", v.VersionType));
        Assert.All(autosaveVersions, v => Assert.Equal("Autosave", v.VersionType));
    }

    [Fact]
    public async Task GetOldAutosavesAsync_ReturnsOldAutosavesNotMarkedImportant()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var oldDate = DateTime.UtcNow.AddDays(-10);
        var recentDate = DateTime.UtcNow;

        // Create old autosave (should be returned)
        var oldAutosave = await _repository.CreateVersionAsync(new ProjectVersionEntity
        {
            ProjectId = projectId,
            Name = "Old Autosave",
            VersionType = "Autosave",
            CreatedAt = oldDate
        });
        _context.Entry(oldAutosave).Property("CreatedAt").CurrentValue = oldDate;
        await _context.SaveChangesAsync();

        // Create old but important autosave (should NOT be returned)
        var importantAutosave = await _repository.CreateVersionAsync(new ProjectVersionEntity
        {
            ProjectId = projectId,
            Name = "Important Autosave",
            VersionType = "Autosave",
            IsMarkedImportant = true,
            CreatedAt = oldDate
        });
        _context.Entry(importantAutosave).Property("CreatedAt").CurrentValue = oldDate;
        await _context.SaveChangesAsync();

        // Create recent autosave (should NOT be returned)
        await _repository.CreateVersionAsync(new ProjectVersionEntity
        {
            ProjectId = projectId,
            Name = "Recent Autosave",
            VersionType = "Autosave",
            CreatedAt = recentDate
        });

        // Act
        var oldAutosaves = await _repository.GetOldAutosavesAsync(projectId, DateTime.UtcNow.AddDays(-7));

        // Assert
        Assert.Single(oldAutosaves);
        Assert.Equal("Old Autosave", oldAutosaves[0].Name);
    }

    [Fact]
    public async Task GetProjectStorageSizeAsync_ReturnsTotal()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        await _repository.CreateVersionAsync(new ProjectVersionEntity
        {
            ProjectId = projectId,
            Name = "V1",
            VersionType = "Manual",
            StorageSizeBytes = 1000
        });
        await _repository.CreateVersionAsync(new ProjectVersionEntity
        {
            ProjectId = projectId,
            Name = "V2",
            VersionType = "Manual",
            StorageSizeBytes = 2000
        });
        await _repository.CreateVersionAsync(new ProjectVersionEntity
        {
            ProjectId = projectId,
            Name = "V3",
            VersionType = "Manual",
            StorageSizeBytes = 3000
        });

        // Act
        var totalSize = await _repository.GetProjectStorageSizeAsync(projectId);

        // Assert
        Assert.Equal(6000, totalSize);
    }
}
