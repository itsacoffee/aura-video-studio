using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Aura.Core.Models.Conversation;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests;

public class ConversationApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ConversationApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SendMessage_CreatesConversationAndReturnsResponse()
    {
        // Arrange
        var projectId = $"test-project-{Guid.NewGuid()}";
        var request = new
        {
            message = "Hello, AI!"
        };

        // Act
        var response = await _client.PostAsJsonAsync(
            $"/api/conversation/{projectId}/message",
            request
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<SendMessageResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Response);
    }

    [Fact]
    public async Task GetHistory_ReturnsEmptyForNewProject()
    {
        // Arrange
        var projectId = $"test-project-{Guid.NewGuid()}";

        // Act
        var response = await _client.GetAsync($"/api/conversation/{projectId}/history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<GetHistoryResult>();
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Messages);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public async Task SendMessage_ThenGetHistory_ReturnsMessages()
    {
        // Arrange
        var projectId = $"test-project-{Guid.NewGuid()}";
        await _client.PostAsJsonAsync(
            $"/api/conversation/{projectId}/message",
            new { message = "First message" }
        );

        // Act
        var response = await _client.GetAsync($"/api/conversation/{projectId}/history");
        var result = await response.Content.ReadFromJsonAsync<GetHistoryResult>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.True(result.Messages.Count >= 2); // User message + AI response
    }

    [Fact]
    public async Task ClearConversation_RemovesHistory()
    {
        // Arrange
        var projectId = $"test-project-{Guid.NewGuid()}";
        await _client.PostAsJsonAsync(
            $"/api/conversation/{projectId}/message",
            new { message = "Test message" }
        );

        // Act
        var deleteResponse = await _client.DeleteAsync($"/api/conversation/{projectId}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var historyResponse = await _client.GetAsync($"/api/conversation/{projectId}/history");
        var result = await historyResponse.Content.ReadFromJsonAsync<GetHistoryResult>();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public async Task UpdateContext_SavesMetadata()
    {
        // Arrange
        var projectId = $"test-project-{Guid.NewGuid()}";
        var metadata = new
        {
            contentType = "Tutorial",
            targetPlatform = "YouTube",
            audience = "Developers",
            tone = "Professional",
            durationSeconds = 300,
            keywords = new[] { "coding", "tutorial" }
        };

        // Act
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/conversation/{projectId}/context",
            metadata
        );
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var contextResponse = await _client.GetAsync($"/api/conversation/{projectId}/context");
        var result = await contextResponse.Content.ReadFromJsonAsync<GetContextResult>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Project);
        Assert.NotNull(result.Project.VideoMetadata);
        Assert.Equal("Tutorial", result.Project.VideoMetadata.ContentType);
        Assert.Equal("YouTube", result.Project.VideoMetadata.TargetPlatform);
    }

    [Fact]
    public async Task RecordDecision_AddsToHistory()
    {
        // Arrange
        var projectId = $"test-project-{Guid.NewGuid()}";
        var decision = new
        {
            stage = "script",
            type = "suggestion",
            suggestion = "Add more examples",
            userAction = "accepted"
        };

        // Act
        var recordResponse = await _client.PostAsJsonAsync(
            $"/api/conversation/{projectId}/decision",
            decision
        );
        Assert.Equal(HttpStatusCode.OK, recordResponse.StatusCode);

        var contextResponse = await _client.GetAsync($"/api/conversation/{projectId}/context");
        var result = await contextResponse.Content.ReadFromJsonAsync<GetContextResult>();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Project);
        Assert.NotEmpty(result.Project.DecisionHistory);
        Assert.Equal("script", result.Project.DecisionHistory[0].Stage);
        Assert.Equal("accepted", result.Project.DecisionHistory[0].UserAction);
    }

    [Fact]
    public async Task MultipleProjects_MaintainSeparateContexts()
    {
        // Arrange
        var project1 = $"test-project-{Guid.NewGuid()}";
        var project2 = $"test-project-{Guid.NewGuid()}";

        // Act
        await _client.PostAsJsonAsync(
            $"/api/conversation/{project1}/message",
            new { message = "Project 1 message" }
        );
        await _client.PostAsJsonAsync(
            $"/api/conversation/{project2}/message",
            new { message = "Project 2 message" }
        );

        var history1 = await _client.GetAsync($"/api/conversation/{project1}/history");
        var history2 = await _client.GetAsync($"/api/conversation/{project2}/history");

        var result1 = await history1.Content.ReadFromJsonAsync<GetHistoryResult>();
        var result2 = await history2.Content.ReadFromJsonAsync<GetHistoryResult>();

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        
        // Each project should have its own messages
        Assert.True(result1.Messages.Count >= 2);
        Assert.True(result2.Messages.Count >= 2);
        
        // Messages should be different
        Assert.Contains("Project 1", result1.Messages[0].Content);
        Assert.Contains("Project 2", result2.Messages[0].Content);
    }

    // Response models for deserialization
    private record SendMessageResult(bool Success, string Response, DateTime Timestamp);
    private record GetHistoryResult(bool Success, List<Message> Messages, int Count);
    private record GetContextResult(
        bool Success,
        ProjectContext Project,
        ConversationContext Conversation);
}
