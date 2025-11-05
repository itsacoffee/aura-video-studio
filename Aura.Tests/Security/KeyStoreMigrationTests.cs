using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Security;

/// <summary>
/// Tests for migration of legacy plaintext API keys to encrypted storage on non-Windows platforms
/// </summary>
[Collection("KeyStoreMigrationTests")]
public class KeyStoreMigrationTests : IDisposable
{
    private readonly Mock<ILogger<KeyStore>> _mockLogger;
    private readonly string _testStorageDir;
    private readonly string _originalLocalAppData;
    private readonly string _originalHome;

    public KeyStoreMigrationTests()
    {
        _mockLogger = new Mock<ILogger<KeyStore>>();
        
        // Save original environment variables
        _originalLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _originalHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        
        // Create a unique temporary directory for each test
        _testStorageDir = Path.Combine(Path.GetTempPath(), "AuraKeyStoreTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testStorageDir);
        
        // Set environment variables to test directory
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _testStorageDir, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("HOME", _testStorageDir, EnvironmentVariableTarget.Process);
    }

    public void Dispose()
    {
        // Restore original environment variables
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _originalLocalAppData, EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("HOME", _originalHome, EnvironmentVariableTarget.Process);
        
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

    [Fact(Skip = "Only runs on Linux/macOS")]
    public void Migration_LegacyPlaintextFile_MigratesToEncryptedStorage()
    {
        // Only run on non-Windows platforms
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return;
        }

        // Arrange - Create legacy plaintext file
        var legacyDir = Path.Combine(_testStorageDir, ".aura-dev");
        Directory.CreateDirectory(legacyDir);
        var legacyFile = Path.Combine(legacyDir, "apikeys.json");
        
        var legacyKeys = new Dictionary<string, string>
        {
            { "openai", "sk-test-openai-123456" },
            { "anthropic", "sk-ant-test-456789" },
            { "gemini", "AIza-test-abc123" }
        };
        
        var json = JsonSerializer.Serialize(legacyKeys, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(legacyFile, json);

        // Act - Create KeyStore which should trigger migration
        var keyStore = new KeyStore(_mockLogger.Object);
        var retrievedKeys = keyStore.GetAllKeys();

        // Assert
        Assert.Equal(3, retrievedKeys.Count);
        Assert.Equal("sk-test-openai-123456", retrievedKeys["openai"]);
        Assert.Equal("sk-ant-test-456789", retrievedKeys["anthropic"]);
        Assert.Equal("AIza-test-abc123", retrievedKeys["gemini"]);
        
        // Verify legacy file was deleted
        Assert.False(File.Exists(legacyFile), "Legacy plaintext file should be deleted after migration");
        
        // Verify encrypted storage exists
        var encryptedFile = Path.Combine(_testStorageDir, "Aura", "secure", "apikeys.dat");
        Assert.True(File.Exists(encryptedFile), "Encrypted storage file should exist");
    }

    [Fact(Skip = "Only runs on Linux/macOS")]
    public void Migration_EmptyLegacyFile_SkipsMigration()
    {
        // Only run on non-Windows platforms
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return;
        }

        // Arrange - Create empty legacy file
        var legacyDir = Path.Combine(_testStorageDir, ".aura-dev");
        Directory.CreateDirectory(legacyDir);
        var legacyFile = Path.Combine(legacyDir, "apikeys.json");
        File.WriteAllText(legacyFile, "{}");

        // Act
        var keyStore = new KeyStore(_mockLogger.Object);
        var retrievedKeys = keyStore.GetAllKeys();

        // Assert
        Assert.Empty(retrievedKeys);
    }

    [Fact(Skip = "Only runs on Linux/macOS")]
    public void Migration_NoLegacyFile_DoesNothing()
    {
        // Only run on non-Windows platforms
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return;
        }

        // Act - No legacy file exists
        var keyStore = new KeyStore(_mockLogger.Object);
        var retrievedKeys = keyStore.GetAllKeys();

        // Assert - Should return empty dictionary
        Assert.Empty(retrievedKeys);
    }

    [Fact(Skip = "Only runs on Linux/macOS")]
    public async Task Migration_AfterMigration_CanSaveNewKeys()
    {
        // Only run on non-Windows platforms
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return;
        }

        // Arrange - Create legacy plaintext file
        var legacyDir = Path.Combine(_testStorageDir, ".aura-dev");
        Directory.CreateDirectory(legacyDir);
        var legacyFile = Path.Combine(legacyDir, "apikeys.json");
        
        var legacyKeys = new Dictionary<string, string>
        {
            { "openai", "sk-test-old-key" }
        };
        
        File.WriteAllText(legacyFile, JsonSerializer.Serialize(legacyKeys));

        // Act - Trigger migration
        var keyStore = new KeyStore(_mockLogger.Object);
        var keys = keyStore.GetAllKeys();
        
        // Add new key after migration
        await keyStore.SetKeyAsync("elevenlabs", "el-test-new-key");
        
        // Retrieve all keys
        var updatedKeys = keyStore.GetAllKeys();

        // Assert
        Assert.Equal(2, updatedKeys.Count);
        Assert.Equal("sk-test-old-key", updatedKeys["openai"]);
        Assert.Equal("el-test-new-key", updatedKeys["elevenlabs"]);
    }

    [Fact]
    public void Windows_DoesNotAttemptMigration()
    {
        // Only run on Windows platforms
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return;
        }

        // Arrange - Create legacy file (should be ignored on Windows)
        var legacyDir = Path.Combine(_testStorageDir, ".aura-dev");
        Directory.CreateDirectory(legacyDir);
        var legacyFile = Path.Combine(legacyDir, "apikeys.json");
        File.WriteAllText(legacyFile, "{\"openai\": \"sk-test\"}");

        // Act
        var keyStore = new KeyStore(_mockLogger.Object);
        var keys = keyStore.GetAllKeys();

        // Assert - Windows should not migrate from legacy file
        Assert.Empty(keys);
        
        // Legacy file should still exist (not touched)
        Assert.True(File.Exists(legacyFile));
    }
}
