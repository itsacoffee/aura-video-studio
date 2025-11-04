using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class ProjectVersionRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AuraDbContext _context;
    private readonly ProjectVersionRepository _repository;

    public ProjectVersionRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AuraDbContext(options);
        _context.Database.EnsureCreated();

        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ProjectVersionRepository>();
        _repository = new ProjectVersionRepository(_context, logger);
    }

    [Fact]
    public async Task CreateVersionAsync_WithNewVersion_AssignsVersionNumber()
    {
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity { Id = projectId, Title = "Test" };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var version1 = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Manual",
            BriefJson = "{\"topic\":\"test\"}"
        };

        var result1 = await _repository.CreateVersionAsync(version1, CancellationToken.None);
        Assert.Equal(1, result1.VersionNumber);

        var version2 = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Autosave",
            BriefJson = "{\"topic\":\"test2\"}"
        };

        var result2 = await _repository.CreateVersionAsync(version2, CancellationToken.None);
        Assert.Equal(2, result2.VersionNumber);
    }

    [Fact]
    public async Task GetVersionsAsync_ReturnsAllVersionsForProject()
    {
        var project1Id = Guid.NewGuid();
        var project2Id = Guid.NewGuid();

        var project1 = new ProjectStateEntity { Id = project1Id, Title = "Project 1" };
        var project2 = new ProjectStateEntity { Id = project2Id, Title = "Project 2" };
        _context.ProjectStates.Add(project1);
        _context.ProjectStates.Add(project2);
        await _context.SaveChangesAsync();

        var version1 = new ProjectVersionEntity
        {
            ProjectId = project1Id,
            VersionType = "Manual",
            VersionNumber = 1
        };

        var version2 = new ProjectVersionEntity
        {
            ProjectId = project1Id,
            VersionType = "Autosave",
            VersionNumber = 2
        };

        var version3 = new ProjectVersionEntity
        {
            ProjectId = project2Id,
            VersionType = "Manual",
            VersionNumber = 1
        };

        _context.ProjectVersions.AddRange(version1, version2, version3);
        await _context.SaveChangesAsync();

        var result = await _repository.GetVersionsAsync(project1Id, false, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, v => Assert.Equal(project1Id, v.ProjectId));
    }

    [Fact]
    public async Task GetVersionsAsync_ExcludesDeleted()
    {
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity { Id = projectId, Title = "Test" };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var version1 = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Manual",
            VersionNumber = 1,
            IsDeleted = false
        };

        var version2 = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Autosave",
            VersionNumber = 2,
            IsDeleted = true
        };

        _context.ProjectVersions.AddRange(version1, version2);
        await _context.SaveChangesAsync();

        var result = await _repository.GetVersionsAsync(projectId, false, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].VersionNumber);
    }

    [Fact]
    public async Task StoreContentBlobAsync_WithSameContent_ReusesSameHash()
    {
        var content = "{\"topic\":\"test\",\"audience\":\"developers\"}";

        var hash1 = await _repository.StoreContentBlobAsync(content, "Brief", CancellationToken.None);
        var hash2 = await _repository.StoreContentBlobAsync(content, "Brief", CancellationToken.None);

        Assert.Equal(hash1, hash2);

        var blob = await _repository.GetContentBlobAsync(hash1, CancellationToken.None);
        Assert.NotNull(blob);
        Assert.Equal(2, blob!.ReferenceCount);
    }

    [Fact]
    public async Task StoreContentBlobAsync_WithDifferentContent_CreatesDifferentHashes()
    {
        var content1 = "{\"topic\":\"test1\"}";
        var content2 = "{\"topic\":\"test2\"}";

        var hash1 = await _repository.StoreContentBlobAsync(content1, "Brief", CancellationToken.None);
        var hash2 = await _repository.StoreContentBlobAsync(content2, "Brief", CancellationToken.None);

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task DeleteVersionAsync_SetsIsDeletedFlag()
    {
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity { Id = projectId, Title = "Test" };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var version = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Manual",
            VersionNumber = 1
        };

        _context.ProjectVersions.Add(version);
        await _context.SaveChangesAsync();

        await _repository.DeleteVersionAsync(version.Id, CancellationToken.None);

        var result = await _repository.GetVersionByIdAsync(version.Id, CancellationToken.None);
        Assert.Null(result);

        var allVersions = await _repository.GetVersionsAsync(projectId, true, CancellationToken.None);
        Assert.Single(allVersions);
        Assert.True(allVersions[0].IsDeleted);
    }

    [Fact]
    public async Task GetVersionsByTypeAsync_FiltersCorrectly()
    {
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity { Id = projectId, Title = "Test" };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var manual = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Manual",
            VersionNumber = 1
        };

        var autosave = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Autosave",
            VersionNumber = 2
        };

        var restorePoint = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "RestorePoint",
            VersionNumber = 3
        };

        _context.ProjectVersions.AddRange(manual, autosave, restorePoint);
        await _context.SaveChangesAsync();

        var autosaves = await _repository.GetVersionsByTypeAsync(projectId, "Autosave", CancellationToken.None);
        Assert.Single(autosaves);
        Assert.Equal("Autosave", autosaves[0].VersionType);
    }

    [Fact]
    public async Task GetOldAutosavesAsync_ReturnsOldVersions()
    {
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity { Id = projectId, Title = "Test" };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var oldAutosave = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Autosave",
            VersionNumber = 1,
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var recentAutosave = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Autosave",
            VersionNumber = 2,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };

        var importantAutosave = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionType = "Autosave",
            VersionNumber = 3,
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            IsMarkedImportant = true
        };

        _context.ProjectVersions.AddRange(oldAutosave, recentAutosave, importantAutosave);
        await _context.SaveChangesAsync();

        var result = await _repository.GetOldAutosavesAsync(
            projectId,
            DateTime.UtcNow.AddDays(-7),
            CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].VersionNumber);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
