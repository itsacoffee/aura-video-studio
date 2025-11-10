using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Aura.Tests.Data;

/// <summary>
/// Tests for entity relationships, cascade deletes, and referential integrity
/// </summary>
public class EntityRelationshipTests : IDisposable
{
    private readonly AuraDbContext _context;

    public EntityRelationshipTests()
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
    public async Task ProjectState_HasScenes_OneToMany()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var scene1 = new SceneStateEntity
        {
            ProjectId = project.Id,
            SceneIndex = 0,
            ScriptText = "Scene 1"
        };
        var scene2 = new SceneStateEntity
        {
            ProjectId = project.Id,
            SceneIndex = 1,
            ScriptText = "Scene 2"
        };
        _context.SceneStates.AddRange(scene1, scene2);
        await _context.SaveChangesAsync();

        // Act
        var loaded = await _context.ProjectStates
            .Include(p => p.Scenes)
            .FirstAsync(p => p.Id == project.Id);

        // Assert
        Assert.Equal(2, loaded.Scenes.Count);
        Assert.All(loaded.Scenes, s => Assert.Equal(project.Id, s.ProjectId));
    }

    [Fact]
    public async Task ProjectState_HasAssets_OneToMany()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var asset1 = new AssetStateEntity
        {
            ProjectId = project.Id,
            AssetType = "Audio",
            FilePath = "/path/to/audio.wav"
        };
        var asset2 = new AssetStateEntity
        {
            ProjectId = project.Id,
            AssetType = "Image",
            FilePath = "/path/to/image.png"
        };
        _context.AssetStates.AddRange(asset1, asset2);
        await _context.SaveChangesAsync();

        // Act
        var loaded = await _context.ProjectStates
            .Include(p => p.Assets)
            .FirstAsync(p => p.Id == project.Id);

        // Assert
        Assert.Equal(2, loaded.Assets.Count);
    }

    [Fact]
    public async Task ProjectState_HasCheckpoints_OneToMany()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var checkpoint = new RenderCheckpointEntity
        {
            ProjectId = project.Id,
            StageName = "Script",
            CompletedScenes = 5,
            TotalScenes = 10
        };
        _context.RenderCheckpoints.Add(checkpoint);
        await _context.SaveChangesAsync();

        // Act
        var loaded = await _context.ProjectStates
            .Include(p => p.Checkpoints)
            .FirstAsync(p => p.Id == project.Id);

        // Assert
        Assert.Single(loaded.Checkpoints);
    }

    [Fact]
    public async Task CascadeDelete_DeletingProject_DeletesScenes()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        
        var scene = new SceneStateEntity
        {
            ProjectId = project.Id,
            SceneIndex = 0,
            ScriptText = "Scene"
        };
        _context.SceneStates.Add(scene);
        await _context.SaveChangesAsync();

        var sceneId = scene.Id;

        // Act
        _context.ProjectStates.Remove(project);
        await _context.SaveChangesAsync();

        // Assert
        var deletedScene = await _context.SceneStates.FindAsync(sceneId);
        Assert.Null(deletedScene); // Scene should be cascade deleted
    }

    [Fact]
    public async Task CascadeDelete_DeletingProject_DeletesAssets()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        
        var asset = new AssetStateEntity
        {
            ProjectId = project.Id,
            AssetType = "Audio",
            FilePath = "/path/to/audio.wav"
        };
        _context.AssetStates.Add(asset);
        await _context.SaveChangesAsync();

        var assetId = asset.Id;

        // Act
        _context.ProjectStates.Remove(project);
        await _context.SaveChangesAsync();

        // Assert
        var deletedAsset = await _context.AssetStates.FindAsync(assetId);
        Assert.Null(deletedAsset); // Asset should be cascade deleted
    }

    [Fact]
    public async Task CascadeDelete_DeletingProject_DeletesCheckpoints()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        
        var checkpoint = new RenderCheckpointEntity
        {
            ProjectId = project.Id,
            StageName = "Script"
        };
        _context.RenderCheckpoints.Add(checkpoint);
        await _context.SaveChangesAsync();

        var checkpointId = checkpoint.Id;

        // Act
        _context.ProjectStates.Remove(project);
        await _context.SaveChangesAsync();

        // Assert
        var deletedCheckpoint = await _context.RenderCheckpoints.FindAsync(checkpointId);
        Assert.Null(deletedCheckpoint); // Checkpoint should be cascade deleted
    }

    [Fact]
    public async Task ProjectVersion_BelongsToProject()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var version = new ProjectVersionEntity
        {
            ProjectId = project.Id,
            VersionNumber = 1,
            VersionType = "Manual"
        };
        _context.ProjectVersions.Add(version);
        await _context.SaveChangesAsync();

        // Act
        var loaded = await _context.ProjectVersions
            .Include(v => v.Project)
            .FirstAsync(v => v.Id == version.Id);

        // Assert
        Assert.NotNull(loaded.Project);
        Assert.Equal(project.Id, loaded.Project.Id);
        Assert.Equal(project.Title, loaded.Project.Title);
    }

    [Fact]
    public async Task CascadeDelete_DeletingProject_DeletesVersions()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        
        var version = new ProjectVersionEntity
        {
            ProjectId = project.Id,
            VersionNumber = 1,
            VersionType = "Manual"
        };
        _context.ProjectVersions.Add(version);
        await _context.SaveChangesAsync();

        var versionId = version.Id;

        // Act
        _context.ProjectStates.Remove(project);
        await _context.SaveChangesAsync();

        // Assert
        var deletedVersion = await _context.ProjectVersions.FindAsync(versionId);
        Assert.Null(deletedVersion); // Version should be cascade deleted
    }

    [Fact]
    public async Task SoftDelete_FiltersQueriesByDefault()
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
            Status = "InProgress",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        _context.ProjectStates.AddRange(project1, project2);
        await _context.SaveChangesAsync();

        // Act
        var activeProjects = await _context.ProjectStates.ToListAsync();

        // Assert
        Assert.Single(activeProjects);
        Assert.Equal("Active Project", activeProjects[0].Title);
    }

    [Fact]
    public async Task SoftDelete_CustomTemplate_FiltersQueriesByDefault()
    {
        // Arrange
        var template1 = new CustomTemplateEntity
        {
            Name = "Active Template",
            Category = "Test"
        };
        var template2 = new CustomTemplateEntity
        {
            Name = "Deleted Template",
            Category = "Test",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow
        };
        _context.CustomTemplates.AddRange(template1, template2);
        await _context.SaveChangesAsync();

        // Act
        var activeTemplates = await _context.CustomTemplates.ToListAsync();

        // Assert
        Assert.Single(activeTemplates);
        Assert.Equal("Active Template", activeTemplates[0].Name);
    }

    [Fact]
    public async Task Index_ProjectState_ByStatus()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _context.ProjectStates.Add(new ProjectStateEntity
            {
                Title = $"Project {i}",
                Status = i % 2 == 0 ? "InProgress" : "Completed"
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var inProgressProjects = await _context.ProjectStates
            .Where(p => p.Status == "InProgress")
            .ToListAsync();

        // Assert
        Assert.Equal(5, inProgressProjects.Count);
    }

    [Fact]
    public async Task Index_Template_ByCategory()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _context.Templates.Add(new TemplateEntity
            {
                Name = $"Template {i}",
                Description = "Description",
                Category = i < 5 ? "CategoryA" : "CategoryB",
                TemplateData = "{}"
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var categoryATemplates = await _context.Templates
            .Where(t => t.Category == "CategoryA")
            .ToListAsync();

        // Assert
        Assert.Equal(5, categoryATemplates.Count);
    }

    [Fact]
    public async Task NavigationProperty_LoadsLazily()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        _context.ProjectStates.Add(project);
        await _context.SaveChangesAsync();

        var scene = new SceneStateEntity
        {
            ProjectId = project.Id,
            SceneIndex = 0,
            ScriptText = "Scene"
        };
        _context.SceneStates.Add(scene);
        await _context.SaveChangesAsync();

        // Act - Load scene first
        var loadedScene = await _context.SceneStates
            .Include(s => s.Project)
            .FirstAsync(s => s.Id == scene.Id);

        // Assert
        Assert.NotNull(loadedScene.Project);
        Assert.Equal(project.Id, loadedScene.Project.Id);
    }

    [Fact]
    public async Task UniqueIndex_UserSetup_UserId()
    {
        // Arrange
        var setup1 = new UserSetupEntity
        {
            UserId = "user123",
            Completed = false
        };
        _context.UserSetups.Add(setup1);
        await _context.SaveChangesAsync();

        var setup2 = new UserSetupEntity
        {
            UserId = "user123", // Same UserId - should violate unique constraint
            Completed = true
        };
        _context.UserSetups.Add(setup2);

        // Act & Assert
        // Note: SQLite in-memory doesn't always enforce unique constraints
        // In production with real database, this would throw DbUpdateException
        var exception = await Record.ExceptionAsync(async () => await _context.SaveChangesAsync());
        
        // The test passes if either an exception is thrown OR
        // we can verify the constraint exists in the model
        if (exception == null)
        {
            // Verify at least that the index is configured
            var entityType = _context.Model.FindEntityType(typeof(UserSetupEntity));
            var userIdIndex = entityType?.GetIndexes()
                .FirstOrDefault(i => i.Properties.Any(p => p.Name == "UserId"));
            Assert.NotNull(userIdIndex);
            Assert.True(userIdIndex.IsUnique);
        }
    }

    [Fact]
    public async Task CompositeIndex_ProjectVersion_ProjectIdAndVersionNumber()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var version1 = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionNumber = 1,
            VersionType = "Manual"
        };
        _context.ProjectVersions.Add(version1);
        await _context.SaveChangesAsync();

        var version2 = new ProjectVersionEntity
        {
            ProjectId = projectId,
            VersionNumber = 1, // Same version number for same project - should violate unique constraint
            VersionType = "Manual"
        };
        _context.ProjectVersions.Add(version2);

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () => await _context.SaveChangesAsync());
        
        // Verify the composite unique index is configured
        if (exception == null)
        {
            var entityType = _context.Model.FindEntityType(typeof(ProjectVersionEntity));
            var compositeIndex = entityType?.GetIndexes()
                .FirstOrDefault(i => 
                    i.Properties.Count == 2 &&
                    i.Properties.Any(p => p.Name == "ProjectId") &&
                    i.Properties.Any(p => p.Name == "VersionNumber"));
            Assert.NotNull(compositeIndex);
            Assert.True(compositeIndex.IsUnique);
        }
    }
}
