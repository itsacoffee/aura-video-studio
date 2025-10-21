using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Conversation;
using Aura.Core.Services.Conversation;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class ConversationContextManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly ContextPersistence _persistence;
    private readonly ConversationContextManager _manager;

    public ConversationContextManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"aura-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var persistenceLogger = loggerFactory.CreateLogger<ContextPersistence>();
        var managerLogger = loggerFactory.CreateLogger<ConversationContextManager>();

        _persistence = new ContextPersistence(persistenceLogger, _testDirectory);
        _manager = new ConversationContextManager(managerLogger, _persistence);
    }

    [Fact]
    public async Task AddMessage_CreatesNewConversation()
    {
        // Arrange
        var projectId = "test-project-1";
        var message = "Hello, AI!";

        // Act
        await _manager.AddMessageAsync(projectId, "user", message);

        // Assert
        var history = await _manager.GetHistoryAsync(projectId);
        Assert.Single(history);
        Assert.Equal("user", history[0].Role);
        Assert.Equal(message, history[0].Content);
    }

    [Fact]
    public async Task AddMessage_AppendsToExistingConversation()
    {
        // Arrange
        var projectId = "test-project-2";
        
        // Act
        await _manager.AddMessageAsync(projectId, "user", "First message");
        await _manager.AddMessageAsync(projectId, "assistant", "Response");
        await _manager.AddMessageAsync(projectId, "user", "Second message");

        // Assert
        var history = await _manager.GetHistoryAsync(projectId);
        Assert.Equal(3, history.Count);
        Assert.Equal("user", history[0].Role);
        Assert.Equal("assistant", history[1].Role);
        Assert.Equal("user", history[2].Role);
    }

    [Fact]
    public async Task GetHistory_RespectsMaxMessages()
    {
        // Arrange
        var projectId = "test-project-3";
        
        // Add 20 messages
        for (int i = 0; i < 20; i++)
        {
            await _manager.AddMessageAsync(projectId, "user", $"Message {i}");
        }

        // Act
        var history = await _manager.GetHistoryAsync(projectId, maxMessages: 5);

        // Assert
        Assert.Equal(5, history.Count);
        Assert.Contains("Message 15", history[0].Content);
        Assert.Contains("Message 19", history[4].Content);
    }

    [Fact]
    public async Task ClearHistory_RemovesAllMessages()
    {
        // Arrange
        var projectId = "test-project-4";
        await _manager.AddMessageAsync(projectId, "user", "Message 1");
        await _manager.AddMessageAsync(projectId, "assistant", "Response 1");

        // Act
        await _manager.ClearHistoryAsync(projectId);
        
        // Assert
        var history = await _manager.GetHistoryAsync(projectId);
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetContext_ReturnsFullContext()
    {
        // Arrange
        var projectId = "test-project-5";
        await _manager.AddMessageAsync(projectId, "user", "Test message");

        // Act
        var context = await _manager.GetContextAsync(projectId);

        // Assert
        Assert.Equal(projectId, context.ProjectId);
        Assert.Single(context.Messages);
        Assert.True(context.CreatedAt <= DateTime.UtcNow);
        Assert.True(context.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task Persistence_SurvivesManagerRecreation()
    {
        // Arrange
        var projectId = "test-project-6";
        await _manager.AddMessageAsync(projectId, "user", "Persisted message");

        // Act - Create new manager instance
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var managerLogger = loggerFactory.CreateLogger<ConversationContextManager>();
        var newManager = new ConversationContextManager(managerLogger, _persistence);
        
        var history = await newManager.GetHistoryAsync(projectId);

        // Assert
        Assert.Single(history);
        Assert.Equal("Persisted message", history[0].Content);
    }

    [Fact]
    public async Task AddMessage_WithMetadata()
    {
        // Arrange
        var projectId = "test-project-7";
        var metadata = new Dictionary<string, object>
        {
            ["source"] = "web-ui",
            ["userId"] = "user123"
        };

        // Act
        await _manager.AddMessageAsync(projectId, "user", "Message with metadata", metadata);

        // Assert
        var history = await _manager.GetHistoryAsync(projectId);
        Assert.NotNull(history[0].Metadata);
        Assert.Equal("web-ui", history[0].Metadata["source"]);
    }

    [Fact]
    public async Task MultipleProjects_IsolatesContexts()
    {
        // Arrange
        var project1 = "project-1";
        var project2 = "project-2";

        // Act
        await _manager.AddMessageAsync(project1, "user", "Project 1 message");
        await _manager.AddMessageAsync(project2, "user", "Project 2 message");

        // Assert
        var history1 = await _manager.GetHistoryAsync(project1);
        var history2 = await _manager.GetHistoryAsync(project2);
        
        Assert.Single(history1);
        Assert.Single(history2);
        Assert.Contains("Project 1", history1[0].Content);
        Assert.Contains("Project 2", history2[0].Content);
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
