using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aura.Tests.Data;

/// <summary>
/// Tests for audit trail and soft delete functionality
/// </summary>
public class AuditAndSoftDeleteTests : IDisposable
{
    private readonly AuraDbContext _context;

    public AuditAndSoftDeleteTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuraDbContext(options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task IAuditableEntity_CreatedAt_SetAutomatically()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };

        // Act
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(DateTime.MinValue, project.CreatedAt);
        Assert.True(project.CreatedAt <= DateTime.UtcNow);
        Assert.True(project.CreatedAt >= DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task IAuditableEntity_UpdatedAt_SetAutomatically()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };

        // Act
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(DateTime.MinValue, project.UpdatedAt);
        Assert.Equal(project.CreatedAt, project.UpdatedAt); // Should be same on creation
    }

    [Fact]
    public async Task IAuditableEntity_UpdatedAt_ChangesOnUpdate()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = project.UpdatedAt;
        await Task.Delay(10); // Small delay to ensure timestamp difference

        // Act
        project.Title = "Updated Title";
        _context.ProjectStates.Update(project);
        await _context.SaveChangesAsync();

        // Assert
        Assert.True(project.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task IAuditableEntity_CreatedAt_DoesNotChangeOnUpdate()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var originalCreatedAt = project.CreatedAt;
        await Task.Delay(10);

        // Act
        project.Title = "Updated Title";
        _context.ProjectStates.Update(project);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal(originalCreatedAt, project.CreatedAt); // Should not change
    }

    [Fact]
    public async Task IAuditableEntity_CanTrackCreatedBy()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress",
            CreatedBy = "user123"
        };

        // Act
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal("user123", project.CreatedBy);
    }

    [Fact]
    public async Task IAuditableEntity_CanTrackModifiedBy()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress",
            CreatedBy = "user123"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        // Act
        project.Title = "Updated Title";
        project.ModifiedBy = "user456";
        _context.ProjectStates.Update(project);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal("user123", project.CreatedBy);
        Assert.Equal("user456", project.ModifiedBy);
    }

    [Fact]
    public async Task ISoftDeletable_MarksEntityAsDeleted()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        // Act
        _context.ProjectStates.Remove(project);
        await _context.SaveChangesAsync();

        // Assert
        Assert.True(project.IsDeleted);
        Assert.NotNull(project.DeletedAt);
        Assert.True(project.DeletedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task ISoftDeletable_SetsDeletedAt()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        // Act
        _context.ProjectStates.Remove(project);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotNull(project.DeletedAt);
        Assert.True(project.DeletedAt.Value <= DateTime.UtcNow);
        Assert.True(project.DeletedAt.Value >= DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task ISoftDeletable_CanTrackDeletedBy()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        // Act
        project.DeletedBy = "user789";
        _context.ProjectStates.Remove(project);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal("user789", project.DeletedBy);
    }

    [Fact]
    public async Task ISoftDeletable_EntityStateRemainsModified()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        // Act
        _context.ProjectStates.Remove(project);

        // Assert
        var entry = _context.Entry(project);
        Assert.Equal(EntityState.Modified, entry.State); // Should be Modified, not Deleted
    }

    [Fact]
    public async Task ISoftDeletable_QueryFilterExcludesDeletedEntities()
    {
        // Arrange
        var project1 = new ProjectStateEntity
        {
            Title = "Active Project",
            Status = "InProgress"
        };
        var project2 = new ProjectStateEntity
        {
            Title = "Deleted Project",
            Status = "InProgress"
        };
        _context.ProjectStates.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        _context.ProjectStates.Remove(project2);
        await _context.SaveChangesAsync();

        // Act
        var activeProjects = await _context.ProjectStates.ToListAsync();

        // Assert
        Assert.Single(activeProjects);
        Assert.Equal("Active Project", activeProjects[0].Title);
    }

    [Fact]
    public async Task ISoftDeletable_IgnoreQueryFilters_IncludesDeletedEntities()
    {
        // Arrange
        var project1 = new ProjectStateEntity
        {
            Title = "Active Project",
            Status = "InProgress"
        };
        var project2 = new ProjectStateEntity
        {
            Title = "Deleted Project",
            Status = "InProgress"
        };
        _context.ProjectStates.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        _context.ProjectStates.Remove(project2);
        await _context.SaveChangesAsync();

        // Act
        var allProjects = await _context.ProjectStates
            .IgnoreQueryFilters()
            .ToListAsync();

        // Assert
        Assert.Equal(2, allProjects.Count);
        Assert.Single(allProjects.Where(p => p.IsDeleted));
    }

    [Fact]
    public async Task CustomTemplate_SoftDeleteWorks()
    {
        // Arrange
        var template = new CustomTemplateEntity
        {
            Name = "Test Template",
            Category = "Test"
        };
        _context.CustomTemplates.Add(template);
        await _context.SaveChangesAsync();

        // Act
        _context.CustomTemplates.Remove(template);
        await _context.SaveChangesAsync();

        // Assert
        Assert.True(template.IsDeleted);
        var found = await _context.CustomTemplates.FindAsync(template.Id);
        Assert.Null(found); // Should be filtered out
    }

    [Fact]
    public async Task ProjectVersion_SoftDeleteWorks()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity
        {
            Id = projectId,
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);

        var version = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionNumber = 1,
            VersionType = "Manual"
        };
        _context.ProjectVersions.Add(version);
        await _context.SaveChangesAsync();

        // Act
        _context.ProjectVersions.Remove(version);
        await _context.SaveChangesAsync();

        // Assert
        Assert.True(version.IsDeleted);
        var found = await _context.ProjectVersions.FindAsync(version.Id);
        Assert.Null(found); // Should be filtered out
    }

    [Fact]
    public async Task MultipleEntities_AuditFieldsSetIndependently()
    {
        // Arrange
        var project1 = new ProjectStateEntity
        {
            Title = "Project 1",
            Status = "InProgress",
            CreatedBy = "user1"
        };
        var project2 = new ProjectStateEntity
        {
            Title = "Project 2",
            Status = "InProgress",
            CreatedBy = "user2"
        };

        // Act
        _context.ProjectStates.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(Guid.Empty, project1.Id);
        Assert.NotEqual(Guid.Empty, project2.Id);
        Assert.NotEqual(project1.Id, project2.Id);
        Assert.Equal("user1", project1.CreatedBy);
        Assert.Equal("user2", project2.CreatedBy);
    }

    [Fact]
    public async Task ContentBlob_AuditFieldsWork()
    {
        // Arrange
        var blob = new ContentBlobEntity
        {
            ContentHash = "abc123",
            Content = "{\"test\": \"data\"}",
            ContentType = "Brief",
            SizeBytes = 100,
            ReferenceCount = 1
        };

        // Act
        _context.ContentBlobs.Add(blob);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(DateTime.MinValue, blob.CreatedAt);
        Assert.NotEqual(DateTime.MinValue, blob.UpdatedAt);
        Assert.NotEqual(DateTime.MinValue, blob.LastReferencedAt);
    }

    [Fact]
    public async Task ConfigurationEntity_AuditFieldsWork()
    {
        // Arrange
        var config = new ConfigurationEntity
        {
            Key = "test.key",
            Value = "test-value",
            Category = "Test",
            CreatedBy = "system"
        };

        // Act
        _context.Configurations.Add(config);
        await _context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(DateTime.MinValue, config.CreatedAt);
        Assert.NotEqual(DateTime.MinValue, config.UpdatedAt);
        Assert.Equal("system", config.CreatedBy);
    }

    [Fact]
    public async Task SoftDelete_DoesNotAffectHardDeletes()
    {
        // Arrange
        var template = new TemplateEntity
        {
            Name = "Test Template",
            Description = "Description",
            Category = "Test",
            TemplateData = "{}"
        };
        _context.Templates.Add(template);
        await _context.SaveChangesAsync();
        var id = template.Id;

        // Act
        _context.Templates.Remove(template); // Hard delete (no ISoftDeletable)
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Templates.FindAsync(id);
        Assert.Null(deleted); // Should be truly deleted
    }
}
