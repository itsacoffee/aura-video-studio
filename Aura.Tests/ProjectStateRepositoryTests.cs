using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class ProjectStateRepositoryTests : IDisposable
{
    private readonly AuraDbContext _context;
    private readonly ProjectStateRepository _repository;
    private readonly Mock<ILogger<ProjectStateRepository>> _loggerMock;

    public ProjectStateRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuraDbContext(options);
        _loggerMock = new Mock<ILogger<ProjectStateRepository>>();
        _repository = new ProjectStateRepository(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateAsync_CreatesProjectSuccessfully()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            JobId = "job-123",
            Status = "InProgress"
        };

        // Act
        var result = await _repository.CreateAsync(project);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Project", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsProjectWithRelatedEntities()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new ProjectStateEntity
        {
            Id = projectId,
            Title = "Test Project",
            JobId = "job-123"
        };

        await _context.ProjectStates.AddAsync(project);
        
        await _context.SceneStates.AddAsync(new SceneStateEntity
        {
            ProjectId = projectId,
            SceneIndex = 0,
            ScriptText = "Scene 1"
        });
        
        await _context.RenderCheckpoints.AddAsync(new RenderCheckpointEntity
        {
            ProjectId = projectId,
            StageName = "Script"
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Scenes);
        Assert.Single(result.Checkpoints);
    }

    [Fact]
    public async Task GetByJobIdAsync_ReturnsCorrectProject()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            JobId = "job-123"
        };
        await _repository.CreateAsync(project);

        // Act
        var result = await _repository.GetByJobIdAsync("job-123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("job-123", result.JobId);
        Assert.Equal("Test Project", result.Title);
    }

    [Fact]
    public async Task GetIncompleteProjectsAsync_ReturnsOnlyInProgressProjects()
    {
        // Arrange
        await _repository.CreateAsync(new ProjectStateEntity
        {
            Title = "Project 1",
            JobId = "job-1",
            Status = "InProgress"
        });

        await _repository.CreateAsync(new ProjectStateEntity
        {
            Title = "Project 2",
            JobId = "job-2",
            Status = "Completed"
        });

        await _repository.CreateAsync(new ProjectStateEntity
        {
            Title = "Project 3",
            JobId = "job-3",
            Status = "InProgress"
        });

        // Act
        var result = await _repository.GetIncompleteProjectsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal("InProgress", p.Status));
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesProjectStatus()
    {
        // Arrange
        var project = await _repository.CreateAsync(new ProjectStateEntity
        {
            Title = "Test Project",
            JobId = "job-123",
            Status = "InProgress"
        });

        // Act
        await _repository.UpdateStatusAsync(project.Id, "Completed");

        // Assert
        var updated = await _repository.GetByIdAsync(project.Id);
        Assert.NotNull(updated);
        Assert.Equal("Completed", updated.Status);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public async Task SaveCheckpointAsync_CreatesCheckpoint()
    {
        // Arrange
        var project = await _repository.CreateAsync(new ProjectStateEntity
        {
            Title = "Test Project",
            JobId = "job-123"
        });

        // Act
        var checkpoint = await _repository.SaveCheckpointAsync(
            project.Id,
            "TTS",
            5,
            10,
            "{\"key\":\"value\"}",
            "/path/to/output.mp4");

        // Assert
        Assert.NotNull(checkpoint);
        Assert.Equal("TTS", checkpoint.StageName);
        Assert.Equal(5, checkpoint.CompletedScenes);
        Assert.Equal(10, checkpoint.TotalScenes);
        Assert.True(checkpoint.IsValid);
    }

    [Fact]
    public async Task GetLatestCheckpointAsync_ReturnsNewestCheckpoint()
    {
        // Arrange
        var project = await _repository.CreateAsync(new ProjectStateEntity
        {
            Title = "Test Project",
            JobId = "job-123"
        });

        await _repository.SaveCheckpointAsync(project.Id, "Script", 0, 10);
        await Task.Delay(10);
        await _repository.SaveCheckpointAsync(project.Id, "TTS", 5, 10);
        await Task.Delay(10);
        await _repository.SaveCheckpointAsync(project.Id, "Images", 10, 10);

        // Act
        var latest = await _repository.GetLatestCheckpointAsync(project.Id);

        // Assert
        Assert.NotNull(latest);
        Assert.Equal("Images", latest.StageName);
    }

    [Fact]
    public async Task GetOldProjectsByStatusAsync_ReturnsProjectsOlderThanTimespan()
    {
        // Arrange
        var oldDate = DateTime.UtcNow.AddDays(-10);
        
        var oldProject = new ProjectStateEntity
        {
            Title = "Old Project",
            JobId = "job-old",
            Status = "Failed",
            UpdatedAt = oldDate
        };
        _context.ProjectStates.Add(oldProject);

        var recentProject = new ProjectStateEntity
        {
            Title = "Recent Project",
            JobId = "job-recent",
            Status = "Failed",
            UpdatedAt = DateTime.UtcNow
        };
        _context.ProjectStates.Add(recentProject);
        
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetOldProjectsByStatusAsync("Failed", TimeSpan.FromDays(7));

        // Assert
        Assert.Single(result);
        Assert.Equal("job-old", result[0].JobId);
    }

    [Fact]
    public async Task DeleteAsync_RemovesProjectAndRelatedEntities()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            JobId = "job-123"
        };
        await _repository.CreateAsync(project);
        await _repository.AddSceneAsync(new SceneStateEntity
        {
            ProjectId = project.Id,
            SceneIndex = 0,
            ScriptText = "Scene"
        });

        // Act
        await _repository.DeleteAsync(project.Id);

        // Assert
        var deleted = await _repository.GetByIdAsync(project.Id);
        Assert.Null(deleted);
        
        var scenes = await _context.SceneStates.Where(s => s.ProjectId == project.Id).ToListAsync();
        Assert.Empty(scenes);
    }

    [Fact]
    public async Task AddSceneAsync_AddsSceneToProject()
    {
        // Arrange
        var project = await _repository.CreateAsync(new ProjectStateEntity
        {
            Title = "Test Project",
            JobId = "job-123"
        });

        var scene = new SceneStateEntity
        {
            ProjectId = project.Id,
            SceneIndex = 0,
            ScriptText = "Test scene"
        };

        // Act
        await _repository.AddSceneAsync(scene);

        // Assert
        var updated = await _repository.GetByIdAsync(project.Id);
        Assert.NotNull(updated);
        Assert.Single(updated.Scenes);
    }

    [Fact]
    public async Task AddAssetAsync_AddsAssetToProject()
    {
        // Arrange
        var project = await _repository.CreateAsync(new ProjectStateEntity
        {
            Title = "Test Project",
            JobId = "job-123"
        });

        var asset = new AssetStateEntity
        {
            ProjectId = project.Id,
            AssetType = "Audio",
            FilePath = "/path/to/file.wav"
        };

        // Act
        await _repository.AddAssetAsync(asset);

        // Assert
        var updated = await _repository.GetByIdAsync(project.Id);
        Assert.NotNull(updated);
        Assert.Single(updated.Assets);
    }
}
