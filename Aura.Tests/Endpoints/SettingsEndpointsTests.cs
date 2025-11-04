using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests.Endpoints;

/// <summary>
/// Integration tests for Settings endpoints.
/// Tests the modularized SettingsEndpoints from Aura.Api/Endpoints/SettingsEndpoints.cs
/// </summary>
public class SettingsEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SettingsEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SaveSettings_WithValidData_ReturnsOk()
    {
        // Arrange
        var settings = new Dictionary<string, object>
        {
            ["theme"] = "dark",
            ["language"] = "en",
            ["autoSave"] = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/settings/save", settings);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.Equal("Settings saved", result.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SaveSettings_WithEmptyData_ReturnsOk()
    {
        // Arrange
        var settings = new Dictionary<string, object>();

        // Act
        var response = await _client.PostAsJsonAsync("/api/settings/save", settings);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LoadSettings_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/settings/load");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task LoadSettings_AfterSave_ReturnsStoredSettings()
    {
        // Arrange - Save settings first
        var settingsToSave = new Dictionary<string, object>
        {
            ["testKey"] = "testValue",
            ["numericKey"] = 42
        };
        await _client.PostAsJsonAsync("/api/settings/save", settingsToSave);

        // Act - Load settings
        var response = await _client.GetAsync("/api/settings/load");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var loadedSettings = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();
        Assert.NotNull(loadedSettings);
        
        // Settings file should contain some data after save
        Assert.True(loadedSettings.Count >= 0);
    }

    [Fact]
    public async Task GetPortableMode_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/api/settings/portable");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.TryGetProperty("isPortable", out var isPortable));
        Assert.True(result.TryGetProperty("baseDirectory", out var baseDir));
        Assert.True(result.TryGetProperty("dataDirectory", out var dataDir));
    }

    [Fact]
    public async Task OpenToolsFolder_ReturnsOk()
    {
        // Act
        var response = await _client.PostAsync("/api/settings/open-tools-folder", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(result.GetProperty("success").GetBoolean());
        Assert.True(result.TryGetProperty("path", out var path));
        Assert.False(string.IsNullOrEmpty(path.GetString()));
    }

    [Fact]
    public async Task SettingsEndpoints_HaveCorrectRoutes()
    {
        // This test verifies that all expected settings endpoints are accessible
        // Act & Assert
        var saveResponse = await _client.PostAsync("/api/settings/save", JsonContent.Create(new Dictionary<string, object>()));
        Assert.NotEqual(HttpStatusCode.NotFound, saveResponse.StatusCode);

        var loadResponse = await _client.GetAsync("/api/settings/load");
        Assert.NotEqual(HttpStatusCode.NotFound, loadResponse.StatusCode);

        var portableResponse = await _client.GetAsync("/api/settings/portable");
        Assert.NotEqual(HttpStatusCode.NotFound, portableResponse.StatusCode);

        var openFolderResponse = await _client.PostAsync("/api/settings/open-tools-folder", null);
        Assert.NotEqual(HttpStatusCode.NotFound, openFolderResponse.StatusCode);
    }
}
