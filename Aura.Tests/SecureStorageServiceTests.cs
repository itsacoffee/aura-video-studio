using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

[Collection("SecureStorageTests")]
public class SecureStorageServiceTests : IDisposable
{
    private readonly Mock<ILogger<SecureStorageService>> _mockLogger;
    private readonly string _testStorageDir;
    private readonly string _originalLocalAppData;
    private readonly SecureStorageService _service;

    public SecureStorageServiceTests()
    {
        _mockLogger = new Mock<ILogger<SecureStorageService>>();
        
        // Save original LocalApplicationData
        _originalLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        // Create a unique temporary directory for each test
        _testStorageDir = Path.Combine(Path.GetTempPath(), "AuraTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testStorageDir);
        
        // Set LocalApplicationData to our test directory (process-scoped)
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _testStorageDir, EnvironmentVariableTarget.Process);
        
        _service = new SecureStorageService(_mockLogger.Object);
    }

    public void Dispose()
    {
        // Restore original LocalApplicationData
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _originalLocalAppData, EnvironmentVariableTarget.Process);
        
        // Clean up test directory
        try
        {
            if (Directory.Exists(_testStorageDir))
            {
                Directory.Delete(_testStorageDir, true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
        
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveApiKeyAsync_ValidKey_SavesSuccessfully()
    {
        // Arrange
        var providerName = "openai";
        var apiKey = "sk-test123456789";

        // Act
        await _service.SaveApiKeyAsync(providerName, apiKey);

        // Assert
        var hasKey = await _service.HasApiKeyAsync(providerName);
        Assert.True(hasKey);
    }

    [Fact]
    public async Task GetApiKeyAsync_SavedKey_ReturnsCorrectKey()
    {
        // Arrange
        var providerName = "anthropic";
        var apiKey = "sk-ant-test123";
        await _service.SaveApiKeyAsync(providerName, apiKey);

        // Act
        var retrievedKey = await _service.GetApiKeyAsync(providerName);

        // Assert
        Assert.Equal(apiKey, retrievedKey);
    }

    [Fact]
    public async Task GetApiKeyAsync_NonExistentKey_ReturnsNull()
    {
        // Act
        var retrievedKey = await _service.GetApiKeyAsync("nonexistent");

        // Assert
        Assert.Null(retrievedKey);
    }

    [Fact]
    public async Task HasApiKeyAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var providerName = "gemini";
        var apiKey = "AIza123456789";
        await _service.SaveApiKeyAsync(providerName, apiKey);

        // Act
        var hasKey = await _service.HasApiKeyAsync(providerName);

        // Assert
        Assert.True(hasKey);
    }

    [Fact]
    public async Task HasApiKeyAsync_NonExistentKey_ReturnsFalse()
    {
        // Act
        var hasKey = await _service.HasApiKeyAsync("nonexistent");

        // Assert
        Assert.False(hasKey);
    }

    [Fact]
    public async Task DeleteApiKeyAsync_ExistingKey_RemovesKey()
    {
        // Arrange
        var providerName = "elevenlabs";
        var apiKey = "el_test123";
        await _service.SaveApiKeyAsync(providerName, apiKey);

        // Act
        await _service.DeleteApiKeyAsync(providerName);

        // Assert
        var hasKey = await _service.HasApiKeyAsync(providerName);
        Assert.False(hasKey);
    }

    [Fact]
    public async Task GetConfiguredProvidersAsync_MultipleKeys_ReturnsAllProviders()
    {
        // Arrange
        await _service.SaveApiKeyAsync("openai", "sk-test1");
        await _service.SaveApiKeyAsync("anthropic", "sk-ant-test2");
        await _service.SaveApiKeyAsync("gemini", "AIza-test3");

        // Act
        var providers = await _service.GetConfiguredProvidersAsync();

        // Assert
        Assert.Equal(3, providers.Count);
        Assert.Contains("openai", providers);
        Assert.Contains("anthropic", providers);
        Assert.Contains("gemini", providers);
    }

    [Fact]
    public async Task SaveApiKeyAsync_UpdateExistingKey_OverwritesSuccessfully()
    {
        // Arrange
        var providerName = "replicate";
        var oldKey = "r8_old123";
        var newKey = "r8_new456";
        await _service.SaveApiKeyAsync(providerName, oldKey);

        // Act
        await _service.SaveApiKeyAsync(providerName, newKey);
        var retrievedKey = await _service.GetApiKeyAsync(providerName);

        // Assert
        Assert.Equal(newKey, retrievedKey);
    }

    [Fact]
    public async Task SaveApiKeyAsync_EmptyProviderName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SaveApiKeyAsync("", "sk-test")
        );
    }

    [Fact]
    public async Task SaveApiKeyAsync_EmptyApiKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.SaveApiKeyAsync("openai", "")
        );
    }

    [Fact]
    public async Task DeleteApiKeyAsync_EmptyProviderName_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DeleteApiKeyAsync("")
        );
    }

    [Fact]
    public async Task GetConfiguredProvidersAsync_NoKeys_ReturnsEmptyList()
    {
        // Create a fresh service instance with a clean directory
        var cleanTestDir = Path.Combine(Path.GetTempPath(), "AuraCleanTest_" + Guid.NewGuid());
        Directory.CreateDirectory(cleanTestDir);
        
        try
        {
            var originalLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Environment.SetEnvironmentVariable("LOCALAPPDATA", cleanTestDir, EnvironmentVariableTarget.Process);
            
            var freshService = new SecureStorageService(Mock.Of<ILogger<SecureStorageService>>());
            
            // Act
            var providers = await freshService.GetConfiguredProvidersAsync();
            
            // Restore
            Environment.SetEnvironmentVariable("LOCALAPPDATA", originalLocalAppData, EnvironmentVariableTarget.Process);
            
            // Assert
            Assert.Empty(providers);
        }
        finally
        {
            if (Directory.Exists(cleanTestDir))
            {
                Directory.Delete(cleanTestDir, true);
            }
        }
    }

    [Fact]
    public async Task SaveAndRetrieveMultipleKeys_VerifyEncryption()
    {
        // Arrange
        var keys = new Dictionary<string, string>
        {
            { "openai", "sk-test-openai-123" },
            { "anthropic", "sk-ant-anthropic-456" },
            { "gemini", "AIza-gemini-789" },
            { "elevenlabs", "el-test-123" },
            { "playht", "playht-test-456" },
            { "replicate", "r8-test-789" }
        };

        // Act - Save all keys
        foreach (var kvp in keys)
        {
            await _service.SaveApiKeyAsync(kvp.Key, kvp.Value);
        }

        // Assert - Verify all keys can be retrieved correctly
        foreach (var kvp in keys)
        {
            var retrievedKey = await _service.GetApiKeyAsync(kvp.Key);
            Assert.Equal(kvp.Value, retrievedKey);
        }
    }

    [Fact]
    public async Task GetApiKeyAsync_WithWhitespaceProviderName_ReturnsNull()
    {
        // Act
        var result = await _service.GetApiKeyAsync("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HasApiKeyAsync_WithWhitespaceProviderName_ReturnsFalse()
    {
        // Act
        var result = await _service.HasApiKeyAsync("   ");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetConfiguredProvidersAsync_AfterDeletion_DoesNotIncludeDeletedProvider()
    {
        // Arrange
        await _service.SaveApiKeyAsync("openai", "sk-test1");
        await _service.SaveApiKeyAsync("anthropic", "sk-ant-test2");
        await _service.DeleteApiKeyAsync("openai");

        // Act
        var providers = await _service.GetConfiguredProvidersAsync();

        // Assert
        Assert.Single(providers);
        Assert.Contains("anthropic", providers);
        Assert.DoesNotContain("openai", providers);
    }
}
