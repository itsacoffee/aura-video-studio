using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for SetupController directory validation with environment variable expansion
/// </summary>
public class SetupControllerDirectoryValidationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SetupControllerDirectoryValidationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CheckDirectory_WithValidPath_ReturnsSuccess()
    {
        // Arrange - use temp directory which should always exist
        var tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var request = new { Path = tempPath };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/check-directory", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DirectoryCheckResponse>();
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.NotNull(result.ExpandedPath);
    }

    [Fact]
    public async Task CheckDirectory_WithEnvironmentVariable_Windows_ExpandsCorrectly()
    {
        // Skip this test on non-Windows platforms
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange - use %TEMP% environment variable
        var request = new { Path = "%TEMP%" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/check-directory", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DirectoryCheckResponse>();
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.NotNull(result.ExpandedPath);
        Assert.DoesNotContain("%TEMP%", result.ExpandedPath);
        Assert.Contains(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), result.ExpandedPath);
    }

    [Fact]
    public async Task CheckDirectory_WithTildeExpansion_Unix_ExpandsCorrectly()
    {
        // Skip this test on Windows
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange - use ~ for home directory
        var request = new { Path = "~" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/check-directory", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DirectoryCheckResponse>();
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Null(result.Error);
        Assert.NotNull(result.ExpandedPath);
        Assert.DoesNotContain("~", result.ExpandedPath);
    }

    [Fact]
    public async Task CheckDirectory_WithNonExistentPath_CreatesDirectory()
    {
        // Arrange - create a unique temporary directory name
        var tempDir = Path.Combine(Path.GetTempPath(), $"aura-test-{Guid.NewGuid()}");
        var request = new { Path = tempDir };

        try
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/setup/check-directory", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<DirectoryCheckResponse>();
            Assert.NotNull(result);
            Assert.True(result.IsValid);
            Assert.Null(result.Error);
            Assert.NotNull(result.ExpandedPath);
            
            // Verify directory was actually created
            Assert.True(Directory.Exists(tempDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CheckDirectory_WithInvalidPath_ReturnsError()
    {
        // Arrange - use an invalid path (colon is invalid on Unix, angle brackets invalid on Windows)
        var invalidPath = OperatingSystem.IsWindows() 
            ? "C:\\Invalid<>Path" 
            : "/invalid:path";
        var request = new { Path = invalidPath };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/check-directory", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DirectoryCheckResponse>();
        Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task CompleteSetup_WithEnvironmentVariablePath_ExpandsAndValidates()
    {
        // Skip this test on non-Windows platforms
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange - use environment variable in output directory
        var outputDir = "%TEMP%\\AuraTest-" + Guid.NewGuid().ToString();
        var request = new 
        { 
            FFmpegPath = (string?)null,
            OutputDirectory = outputDir
        };

        try
        {
            // Act
            var response = await _client.PostAsJsonAsync("/api/setup/complete", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SetupCompleteResponse>();
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Empty(result.Errors);
            
            // Verify the expanded directory was created
            var expandedPath = Environment.ExpandEnvironmentVariables(outputDir);
            Assert.True(Directory.Exists(expandedPath));
        }
        finally
        {
            // Cleanup
            var expandedPath = Environment.ExpandEnvironmentVariables(outputDir);
            if (Directory.Exists(expandedPath))
            {
                Directory.Delete(expandedPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CompleteSetup_WithInvalidEnvironmentVariable_ReturnsError()
    {
        // Arrange - use non-existent environment variable
        var request = new 
        { 
            FFmpegPath = (string?)null,
            OutputDirectory = "%NONEXISTENT_VAR%\\Videos"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/setup/complete", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SetupCompleteResponse>();
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
    }

    // Response DTOs for deserialization
    private class DirectoryCheckResponse
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public string? ExpandedPath { get; set; }
        public string? CorrelationId { get; set; }
    }

    private class SetupCompleteResponse
    {
        public bool Success { get; set; }
        public string[] Errors { get; set; } = Array.Empty<string>();
        public string? CorrelationId { get; set; }
    }
}
