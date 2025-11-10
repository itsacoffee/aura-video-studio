using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests.Integration.EndpointTests;

/// <summary>
/// Integration tests for Projects API endpoints
/// </summary>
public class ProjectsEndpointTests : ApiIntegrationTestBase
{
    public ProjectsEndpointTests(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProjects_ReturnsOk()
    {
        // Act
        var response = await Client.GetAsync("/api/projects");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json; charset=utf-8", 
            response.Content.Headers.ContentType?.ToString());
    }

    [Fact]
    public async Task GetProject_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var response = await Client.GetAsync($"/api/projects/{invalidId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithValidData_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            Name = "Test Project",
            Description = "Integration test project"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateProject_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var invalidRequest = new
        {
            Name = "" // Empty name should fail validation
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/projects", invalidRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_WithValidData_ReturnsOk()
    {
        // Arrange
        var createRequest = new
        {
            Name = "Original Name",
            Description = "Original description"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/projects", createRequest);
        var location = createResponse.Headers.Location;

        var updateRequest = new
        {
            Name = "Updated Name",
            Description = "Updated description"
        };

        // Act
        var response = await Client.PutAsJsonAsync(location, updateRequest);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task DeleteProject_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var createRequest = new
        {
            Name = "Project to Delete",
            Description = "Will be deleted"
        };
        var createResponse = await Client.PostAsJsonAsync("/api/projects", createRequest);
        var location = createResponse.Headers.Location;

        // Act
        var response = await Client.DeleteAsync(location);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify deletion
        var getResponse = await Client.GetAsync(location);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
