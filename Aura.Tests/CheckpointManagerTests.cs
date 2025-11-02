using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models;
using Aura.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class CheckpointManagerTests : IDisposable
{
    private readonly AuraDbContext _context;
    private readonly ProjectStateRepository _repository;
    private readonly CheckpointManager _checkpointManager;
    private readonly Mock<ILogger<ProjectStateRepository>> _repoLoggerMock;
    private readonly Mock<ILogger<CheckpointManager>> _managerLoggerMock;

    public CheckpointManagerTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuraDbContext(options);
        _repoLoggerMock = new Mock<ILogger<ProjectStateRepository>>();
        _managerLoggerMock = new Mock<ILogger<CheckpointManager>>();
        _repository = new ProjectStateRepository(_context, _repoLoggerMock.Object);
        _checkpointManager = new CheckpointManager(_repository, _managerLoggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CreateProjectStateAsync_CreatesProjectSuccessfully()
    {
        // Arrange
        var brief = new Brief("Test Topic", "General", "Educate", "Professional", "en-US", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(30), Pacing.Conversational, Density.Balanced, "Standard");
        var voiceSpec = new VoiceSpec("default", 1.0, 1.0, PauseStyle.Natural);
        var renderSpec = new RenderSpec(new Resolution(1920, 1080), "mp4", 5000, 192);

        // Act
        var projectId = await _checkpointManager.CreateProjectStateAsync(
            "Test Project",
            "job-123",
            brief,
            planSpec,
            voiceSpec,
            renderSpec);

        // Assert
        Assert.NotEqual(Guid.Empty, projectId);
        var project = await _repository.GetByIdAsync(projectId);
        Assert.NotNull(project);
        Assert.Equal("Test Project", project.Title);
        Assert.Equal("job-123", project.JobId);
        Assert.Equal("InProgress", project.Status);
    }

    [Fact]
    public async Task SaveCheckpointAsync_SavesCheckpointWithData()
    {
        // Arrange
        var projectId = await CreateTestProjectAsync();

        // Act
        var startTime = DateTime.UtcNow;
        await _checkpointManager.SaveCheckpointAsync(
            projectId,
            "TTS",
            5,
            10,
            new System.Collections.Generic.Dictionary<string, object>
            {
                { "currentScene", 5 },
                { "totalDuration", 30.5 }
            },
            "/path/to/output.mp4");
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        Assert.True(elapsed.TotalMilliseconds < 100, $"Checkpoint save took {elapsed.TotalMilliseconds}ms, exceeding 100ms target");
        
        var checkpoint = await _repository.GetLatestCheckpointAsync(projectId);
        Assert.NotNull(checkpoint);
        Assert.Equal("TTS", checkpoint.StageName);
        Assert.Equal(5, checkpoint.CompletedScenes);
        Assert.Equal(10, checkpoint.TotalScenes);
        Assert.Equal("/path/to/output.mp4", checkpoint.OutputFilePath);
    }

    [Fact]
    public async Task UpdateProgressAsync_UpdatesProjectProgress()
    {
        // Arrange
        var projectId = await CreateTestProjectAsync();

        // Act
        await _checkpointManager.UpdateProgressAsync(projectId, "TTS", 45);

        // Assert
        var project = await _repository.GetByIdAsync(projectId);
        Assert.NotNull(project);
        Assert.Equal("TTS", project.CurrentStage);
        Assert.Equal(45, project.ProgressPercent);
    }

    [Fact]
    public async Task CompleteProjectAsync_MarksProjectAsCompleted()
    {
        // Arrange
        var projectId = await CreateTestProjectAsync();

        // Act
        await _checkpointManager.CompleteProjectAsync(projectId);

        // Assert
        var project = await _repository.GetByIdAsync(projectId);
        Assert.NotNull(project);
        Assert.Equal("Completed", project.Status);
        Assert.NotNull(project.CompletedAt);
    }

    [Fact]
    public async Task FailProjectAsync_MarksProjectAsFailed()
    {
        // Arrange
        var projectId = await CreateTestProjectAsync();

        // Act
        await _checkpointManager.FailProjectAsync(projectId, "Test error message");

        // Assert
        var project = await _repository.GetByIdAsync(projectId);
        Assert.NotNull(project);
        Assert.Equal("Failed", project.Status);
        Assert.Equal("Test error message", project.ErrorMessage);
        Assert.NotNull(project.CompletedAt);
    }

    [Fact]
    public async Task CancelProjectAsync_MarksProjectAsCancelled()
    {
        // Arrange
        var projectId = await CreateTestProjectAsync();

        // Act
        await _checkpointManager.CancelProjectAsync(projectId);

        // Assert
        var project = await _repository.GetByIdAsync(projectId);
        Assert.NotNull(project);
        Assert.Equal("Cancelled", project.Status);
        Assert.NotNull(project.CompletedAt);
    }

    [Fact]
    public async Task GetLatestCheckpointAsync_ReturnsLatestCheckpoint()
    {
        // Arrange
        var projectId = await CreateTestProjectAsync();
        
        await _checkpointManager.SaveCheckpointAsync(projectId, "Script", 0, 10);
        await Task.Delay(10);
        await _checkpointManager.SaveCheckpointAsync(projectId, "TTS", 5, 10);
        await Task.Delay(10);
        await _checkpointManager.SaveCheckpointAsync(projectId, "Images", 10, 10);

        // Act
        var checkpoint = await _checkpointManager.GetLatestCheckpointAsync(projectId);

        // Assert
        Assert.NotNull(checkpoint);
        Assert.Equal("Images", checkpoint.StageName);
        Assert.Equal(10, checkpoint.CompletedScenes);
    }

    [Fact]
    public async Task GetProjectForRecoveryAsync_ReturnsCompleteRecoveryInfo()
    {
        // Arrange
        var projectId = await CreateTestProjectAsync();
        await _checkpointManager.SaveCheckpointAsync(projectId, "TTS", 5, 10);
        await _checkpointManager.AddSceneAsync(projectId, 0, "Scene 1", 3.5, "/audio/scene1.wav", "/images/scene1.jpg");

        // Act
        var recoveryInfo = await _checkpointManager.GetProjectForRecoveryAsync(projectId);

        // Assert
        Assert.NotNull(recoveryInfo);
        Assert.Equal(projectId, recoveryInfo.ProjectId);
        Assert.Equal("Test Project", recoveryInfo.Title);
        Assert.NotNull(recoveryInfo.LatestCheckpoint);
        Assert.Equal("TTS", recoveryInfo.LatestCheckpoint.StageName);
        Assert.Single(recoveryInfo.Scenes);
    }

    [Fact]
    public async Task GetIncompleteProjectsAsync_ReturnsOnlyInProgressProjects()
    {
        // Arrange
        var projectId1 = await CreateTestProjectAsync();
        var projectId2 = await CreateTestProjectAsync();
        var projectId3 = await CreateTestProjectAsync();
        
        await _checkpointManager.CompleteProjectAsync(projectId2);
        await _checkpointManager.FailProjectAsync(projectId3, "Test error");

        // Act
        var incompleteProjects = await _checkpointManager.GetIncompleteProjectsAsync();

        // Assert
        Assert.Single(incompleteProjects);
        Assert.Equal(projectId1, incompleteProjects[0].ProjectId);
    }

    [Fact]
    public async Task AddSceneAsync_AddsSceneToProject()
    {
        // Arrange
        var projectId = await CreateTestProjectAsync();

        // Act
        await _checkpointManager.AddSceneAsync(
            projectId,
            0,
            "Test scene text",
            3.5,
            "/audio/scene.wav",
            "/images/scene.jpg");

        // Assert
        var project = await _repository.GetByIdAsync(projectId);
        Assert.NotNull(project);
        Assert.Single(project.Scenes);
        
        var scene = project.Scenes.First();
        Assert.Equal(0, scene.SceneIndex);
        Assert.Equal("Test scene text", scene.ScriptText);
        Assert.Equal(3.5, scene.DurationSeconds);
        Assert.Equal("/audio/scene.wav", scene.AudioFilePath);
        Assert.Equal("/images/scene.jpg", scene.ImageFilePath);
        Assert.True(scene.IsCompleted);
    }

    [Fact]
    public async Task AddAssetAsync_AddsAssetToProject()
    {
        // Arrange
        var projectId = await CreateTestProjectAsync();

        // Act
        await _checkpointManager.AddAssetAsync(
            projectId,
            "Audio",
            "/path/to/audio.wav",
            1024000,
            "audio/wav",
            true);

        // Assert
        var project = await _repository.GetByIdAsync(projectId);
        Assert.NotNull(project);
        Assert.Single(project.Assets);
        
        var asset = project.Assets.First();
        Assert.Equal("Audio", asset.AssetType);
        Assert.Equal("/path/to/audio.wav", asset.FilePath);
        Assert.Equal(1024000, asset.FileSizeBytes);
        Assert.Equal("audio/wav", asset.MimeType);
        Assert.True(asset.IsTemporary);
    }

    private async Task<Guid> CreateTestProjectAsync()
    {
        var brief = new Brief("Test Topic", "General", "Educate", "Professional", "en-US", Aspect.Widescreen16x9);
        var planSpec = new PlanSpec(TimeSpan.FromSeconds(30), Pacing.Conversational, Density.Balanced, "Standard");
        var voiceSpec = new VoiceSpec("default", 1.0, 1.0, PauseStyle.Natural);
        var renderSpec = new RenderSpec(new Resolution(1920, 1080), "mp4", 5000, 192);

        return await _checkpointManager.CreateProjectStateAsync(
            "Test Project",
            $"job-{Guid.NewGuid()}",
            brief,
            planSpec,
            voiceSpec,
            renderSpec);
    }
}
