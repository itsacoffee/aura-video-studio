using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.Conversation;
using Aura.Core.Services.Conversation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class ProjectContextManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ContextPersistence _persistence;
    private readonly ProjectContextManager _manager;

    public ProjectContextManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"aura-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var persistenceLogger = loggerFactory.CreateLogger<ContextPersistence>();
        var managerLogger = loggerFactory.CreateLogger<ProjectContextManager>();

        _persistence = new ContextPersistence(persistenceLogger, _testDirectory);
        _manager = new ProjectContextManager(managerLogger, _persistence);
    }

    [Fact]
    public async Task GetOrCreateContext_CreatesNewContext()
    {
        // Arrange
        var projectId = "test-project-1";

        // Act
        var context = await _manager.GetOrCreateContextAsync(projectId);

        // Assert
        Assert.Equal(projectId, context.ProjectId);
        Assert.Empty(context.DecisionHistory);
        Assert.Null(context.VideoMetadata);
    }

    [Fact]
    public async Task UpdateVideoMetadata_UpdatesContext()
    {
        // Arrange
        var projectId = "test-project-2";
        var metadata = new VideoMetadata(
            ContentType: "Tutorial",
            TargetPlatform: "YouTube",
            Audience: "Beginners",
            Tone: "Friendly",
            DurationSeconds: 300,
            Keywords: new[] { "programming", "tutorial" }
        );

        // Act
        await _manager.UpdateVideoMetadataAsync(projectId, metadata);
        var context = await _manager.GetContextAsync(projectId);

        // Assert
        Assert.NotNull(context.VideoMetadata);
        Assert.Equal("Tutorial", context.VideoMetadata.ContentType);
        Assert.Equal("YouTube", context.VideoMetadata.TargetPlatform);
        Assert.Equal(300, context.VideoMetadata.DurationSeconds);
    }

    [Fact]
    public async Task RecordDecision_AddsToHistory()
    {
        // Arrange
        var projectId = "test-project-3";

        // Act
        await _manager.RecordDecisionAsync(
            projectId,
            stage: "script",
            type: "suggestion",
            suggestion: "Add more examples",
            userAction: "accepted"
        );

        // Assert
        var decisions = await _manager.GetDecisionHistoryAsync(projectId);
        Assert.Single(decisions);
        Assert.Equal("script", decisions[0].Stage);
        Assert.Equal("accepted", decisions[0].UserAction);
    }

    [Fact]
    public async Task RecordDecision_WithModification()
    {
        // Arrange
        var projectId = "test-project-4";
        var modification = "Changed wording to be more concise";

        // Act
        await _manager.RecordDecisionAsync(
            projectId,
            stage: "script",
            type: "recommendation",
            suggestion: "Original suggestion",
            userAction: "modified",
            userModification: modification
        );

        // Assert
        var decisions = await _manager.GetDecisionHistoryAsync(projectId);
        Assert.Single(decisions);
        Assert.Equal("modified", decisions[0].UserAction);
        Assert.Equal(modification, decisions[0].UserModification);
    }

    [Fact]
    public async Task GetDecisionHistory_FiltersByStage()
    {
        // Arrange
        var projectId = "test-project-5";
        
        await _manager.RecordDecisionAsync(projectId, "script", "suggestion", "Sug 1", "accepted");
        await _manager.RecordDecisionAsync(projectId, "visuals", "suggestion", "Sug 2", "accepted");
        await _manager.RecordDecisionAsync(projectId, "script", "recommendation", "Sug 3", "rejected");

        // Act
        var scriptDecisions = await _manager.GetDecisionHistoryAsync(projectId, stage: "script");

        // Assert
        Assert.Equal(2, scriptDecisions.Count);
        Assert.All(scriptDecisions, d => Assert.Equal("script", d.Stage));
    }

    [Fact]
    public async Task DeleteContext_RemovesFromDisk()
    {
        // Arrange
        var projectId = "test-project-6";
        await _manager.UpdateVideoMetadataAsync(
            projectId,
            new VideoMetadata("Test", "YouTube", "All", "Casual", 60, null)
        );

        // Act
        await _manager.DeleteContextAsync(projectId);

        // Assert - Creating new manager to verify persistence
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var managerLogger = loggerFactory.CreateLogger<ProjectContextManager>();
        var newManager = new ProjectContextManager(managerLogger, _persistence);
        
        var context = await newManager.GetOrCreateContextAsync(projectId);
        Assert.Null(context.VideoMetadata); // Should be null since it was deleted
    }

    [Fact]
    public async Task Persistence_SurvivesManagerRecreation()
    {
        // Arrange
        var projectId = "test-project-7";
        var metadata = new VideoMetadata("Tutorial", "TikTok", "Students", "Fun", 30, null);
        
        await _manager.UpdateVideoMetadataAsync(projectId, metadata);
        await _manager.RecordDecisionAsync(projectId, "pacing", "suggestion", "Speed up", "accepted");

        // Act - Create new manager instance
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var managerLogger = loggerFactory.CreateLogger<ProjectContextManager>();
        var newManager = new ProjectContextManager(managerLogger, _persistence);
        
        var context = await newManager.GetContextAsync(projectId);

        // Assert
        Assert.NotNull(context.VideoMetadata);
        Assert.Equal("Tutorial", context.VideoMetadata.ContentType);
        Assert.Single(context.DecisionHistory);
        Assert.Equal("pacing", context.DecisionHistory[0].Stage);
    }

    [Fact]
    public async Task MultipleProjects_IsolatesContexts()
    {
        // Arrange
        var project1 = "project-1";
        var project2 = "project-2";

        // Act
        await _manager.UpdateVideoMetadataAsync(
            project1,
            new VideoMetadata("Type1", null, null, null, null, null));
        await _manager.UpdateVideoMetadataAsync(
            project2,
            new VideoMetadata("Type2", null, null, null, null, null));

        // Assert
        var context1 = await _manager.GetContextAsync(project1);
        var context2 = await _manager.GetContextAsync(project2);
        
        Assert.Equal("Type1", context1.VideoMetadata?.ContentType);
        Assert.Equal("Type2", context2.VideoMetadata?.ContentType);
    }

    [Fact]
    public async Task GetAllProjectIds_ReturnsAllProjects()
    {
        // Arrange
        await _manager.GetOrCreateContextAsync("project-1");
        await _manager.GetOrCreateContextAsync("project-2");
        await _manager.GetOrCreateContextAsync("project-3");

        // Act
        var projectIds = await _manager.GetAllProjectIdsAsync();

        // Assert
        Assert.Equal(3, projectIds.Count);
        Assert.Contains("project-1", projectIds);
        Assert.Contains("project-2", projectIds);
        Assert.Contains("project-3", projectIds);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch
        {
            // Cleanup is best-effort
        }
        GC.SuppressFinalize(this);
    }
}
