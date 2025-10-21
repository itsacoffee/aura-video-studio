using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Models.Conversation;
using Aura.Core.Services.Conversation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class ContextPersistenceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ContextPersistence _persistence;

    public ContextPersistenceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"aura-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<ContextPersistence>();
        _persistence = new ContextPersistence(logger, _testDirectory);
    }

    [Fact]
    public async Task SaveAndLoad_ConversationContext()
    {
        // Arrange
        var projectId = "test-project-1";
        var messages = new System.Collections.Generic.List<Message>
        {
            new Message("user", "Hello", DateTime.UtcNow, null),
            new Message("assistant", "Hi there!", DateTime.UtcNow, null)
        };
        
        var context = new ConversationContext(
            ProjectId: projectId,
            Messages: messages,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Metadata: null
        );

        // Act
        await _persistence.SaveConversationAsync(context);
        var loaded = await _persistence.LoadConversationAsync(projectId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(projectId, loaded.ProjectId);
        Assert.Equal(2, loaded.Messages.Count);
        Assert.Equal("Hello", loaded.Messages[0].Content);
    }

    [Fact]
    public async Task SaveAndLoad_ProjectContext()
    {
        // Arrange
        var projectId = "test-project-2";
        var metadata = new VideoMetadata(
            ContentType: "Tutorial",
            TargetPlatform: "YouTube",
            Audience: "Developers",
            Tone: "Professional",
            DurationSeconds: 600,
            Keywords: new[] { "coding", "tutorial" }
        );
        
        var decisions = new System.Collections.Generic.List<AiDecision>
        {
            new AiDecision(
                DecisionId: Guid.NewGuid().ToString(),
                Stage: "script",
                Type: "suggestion",
                Suggestion: "Add more examples",
                UserAction: "accepted",
                UserModification: null,
                Timestamp: DateTime.UtcNow
            )
        };
        
        var context = new ProjectContext(
            ProjectId: projectId,
            VideoMetadata: metadata,
            DecisionHistory: decisions,
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Metadata: null
        );

        // Act
        await _persistence.SaveProjectContextAsync(context);
        var loaded = await _persistence.LoadProjectContextAsync(projectId);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(projectId, loaded.ProjectId);
        Assert.NotNull(loaded.VideoMetadata);
        Assert.Equal("Tutorial", loaded.VideoMetadata.ContentType);
        Assert.Single(loaded.DecisionHistory);
    }

    [Fact]
    public async Task DeleteConversation_RemovesFile()
    {
        // Arrange
        var projectId = "test-project-3";
        var context = new ConversationContext(
            ProjectId: projectId,
            Messages: new System.Collections.Generic.List<Message>(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Metadata: null
        );
        
        await _persistence.SaveConversationAsync(context);

        // Act
        await _persistence.DeleteConversationAsync(projectId);
        var loaded = await _persistence.LoadConversationAsync(projectId);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task DeleteProjectContext_RemovesFile()
    {
        // Arrange
        var projectId = "test-project-4";
        var context = new ProjectContext(
            ProjectId: projectId,
            VideoMetadata: null,
            DecisionHistory: new System.Collections.Generic.List<AiDecision>(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Metadata: null
        );
        
        await _persistence.SaveProjectContextAsync(context);

        // Act
        await _persistence.DeleteProjectContextAsync(projectId);
        var loaded = await _persistence.LoadProjectContextAsync(projectId);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task GetAllProjectIds_ReturnsAllProjects()
    {
        // Arrange
        var project1 = new ConversationContext(
            ProjectId: "project-1",
            Messages: new System.Collections.Generic.List<Message>(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Metadata: null
        );
        
        var project2 = new ProjectContext(
            ProjectId: "project-2",
            VideoMetadata: null,
            DecisionHistory: new System.Collections.Generic.List<AiDecision>(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Metadata: null
        );
        
        await _persistence.SaveConversationAsync(project1);
        await _persistence.SaveProjectContextAsync(project2);

        // Act
        var projectIds = await _persistence.GetAllProjectIdsAsync();

        // Assert
        Assert.Equal(2, projectIds.Count);
        Assert.Contains("project-1", projectIds);
        Assert.Contains("project-2", projectIds);
    }

    [Fact]
    public async Task SaveConversation_HandlesInvalidCharactersInProjectId()
    {
        // Arrange
        var projectId = "project/with\\invalid:characters";
        var context = new ConversationContext(
            ProjectId: projectId,
            Messages: new System.Collections.Generic.List<Message>(),
            CreatedAt: DateTime.UtcNow,
            UpdatedAt: DateTime.UtcNow,
            Metadata: null
        );

        // Act & Assert - Should not throw
        await _persistence.SaveConversationAsync(context);
        var loaded = await _persistence.LoadConversationAsync(projectId);
        Assert.NotNull(loaded);
    }

    [Fact]
    public async Task LoadConversation_ReturnsNull_WhenFileDoesNotExist()
    {
        // Act
        var loaded = await _persistence.LoadConversationAsync("non-existent-project");

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task LoadProjectContext_ReturnsNull_WhenFileDoesNotExist()
    {
        // Act
        var loaded = await _persistence.LoadProjectContextAsync("non-existent-project");

        // Assert
        Assert.Null(loaded);
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
