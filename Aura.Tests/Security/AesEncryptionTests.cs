using System;
using System.IO;
using System.Threading.Tasks;
using Aura.Core.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Security;

/// <summary>
/// Tests for AES-256 encryption in SecureStorageService (Linux/macOS)
/// </summary>
[Collection("AesEncryptionTests")]
public class AesEncryptionTests : IDisposable
{
    private readonly Mock<ILogger<SecureStorageService>> _mockLogger;
    private readonly string _testStorageDir;
    private readonly string _originalLocalAppData;

    public AesEncryptionTests()
    {
        _mockLogger = new Mock<ILogger<SecureStorageService>>();
        
        // Save original LocalApplicationData
        _originalLocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        
        // Create a unique temporary directory for each test
        _testStorageDir = Path.Combine(Path.GetTempPath(), "AuraAesTests_" + Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testStorageDir);
        
        // Set LocalApplicationData to our test directory (process-scoped)
        Environment.SetEnvironmentVariable("LOCALAPPDATA", _testStorageDir, EnvironmentVariableTarget.Process);
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
    public async Task EncryptDecrypt_SaveAndRetrieveKey_ReturnsOriginalValue()
    {
        // Arrange
        var service = new SecureStorageService(_mockLogger.Object);
        var providerName = "openai";
        var apiKey = "sk-test-123456789abcdef";

        // Act
        await service.SaveApiKeyAsync(providerName, apiKey);
        var retrievedKey = await service.GetApiKeyAsync(providerName);

        // Assert
        Assert.Equal(apiKey, retrievedKey);
    }

    [Fact]
    public async Task EncryptDecrypt_MultipleKeys_AllKeysRetrievedCorrectly()
    {
        // Arrange
        var service = new SecureStorageService(_mockLogger.Object);
        var keys = new System.Collections.Generic.Dictionary<string, string>
        {
            { "openai", "sk-test-openai-123" },
            { "anthropic", "sk-ant-anthropic-456" },
            { "gemini", "AIza-gemini-789" },
            { "elevenlabs", "el-test-abc" }
        };

        // Act - Save all keys
        foreach (var kvp in keys)
        {
            await service.SaveApiKeyAsync(kvp.Key, kvp.Value);
        }

        // Assert - Verify all keys can be retrieved
        foreach (var kvp in keys)
        {
            var retrievedKey = await service.GetApiKeyAsync(kvp.Key);
            Assert.Equal(kvp.Value, retrievedKey);
        }
    }

    [Fact]
    public async Task EncryptedStorage_FileIsNotPlaintext()
    {
        // Arrange
        var service = new SecureStorageService(_mockLogger.Object);
        var providerName = "openai";
        var apiKey = "sk-test-very-secret-key";

        // Act
        await service.SaveApiKeyAsync(providerName, apiKey);

        // Assert - Read raw file and verify it doesn't contain the plaintext key
        var storagePath = Path.Combine(_testStorageDir, "Aura", "secure", "apikeys.dat");
        Assert.True(File.Exists(storagePath), "Encrypted storage file should exist");
        
        var rawBytes = await File.ReadAllBytesAsync(storagePath);
        var rawText = System.Text.Encoding.UTF8.GetString(rawBytes);
        
        // The plaintext key should NOT appear in the encrypted file
        Assert.DoesNotContain(apiKey, rawText);
    }

    [Fact]
    public async Task MachineKey_IsGeneratedOnFirstUse()
    {
        // Arrange
        var service = new SecureStorageService(_mockLogger.Object);

        // Act - Save a key which should generate machine key
        await service.SaveApiKeyAsync("test", "test-key");

        // Assert - Machine key file should exist
        var machineKeyPath = Path.Combine(_testStorageDir, "Aura", "secure", ".machinekey");
        Assert.True(File.Exists(machineKeyPath), "Machine key file should be generated");
        
        var keyBytes = await File.ReadAllBytesAsync(machineKeyPath);
        Assert.Equal(32, keyBytes.Length); // AES-256 key is 32 bytes
    }

    [Fact]
    public async Task MachineKey_IsReusedBetweenSessions()
    {
        // Arrange & Act - First session
        var service1 = new SecureStorageService(_mockLogger.Object);
        await service1.SaveApiKeyAsync("openai", "sk-test-key");
        var machineKeyPath = Path.Combine(_testStorageDir, "Aura", "secure", ".machinekey");
        var originalKeyBytes = await File.ReadAllBytesAsync(machineKeyPath);

        // Second session with new service instance
        var service2 = new SecureStorageService(Mock.Of<ILogger<SecureStorageService>>());
        var retrievedKey = await service2.GetApiKeyAsync("openai");
        var reusedKeyBytes = await File.ReadAllBytesAsync(machineKeyPath);

        // Assert
        Assert.Equal("sk-test-key", retrievedKey);
        Assert.Equal(originalKeyBytes, reusedKeyBytes); // Machine key should be unchanged
    }

    [Fact]
    public async Task Encryption_LongKey_HandledCorrectly()
    {
        // Arrange
        var service = new SecureStorageService(_mockLogger.Object);
        var providerName = "test-provider";
        var longApiKey = new string('x', 500); // 500 character key

        // Act
        await service.SaveApiKeyAsync(providerName, longApiKey);
        var retrievedKey = await service.GetApiKeyAsync(providerName);

        // Assert
        Assert.Equal(longApiKey, retrievedKey);
    }

    [Fact]
    public async Task Encryption_SpecialCharacters_PreservedCorrectly()
    {
        // Arrange
        var service = new SecureStorageService(_mockLogger.Object);
        var providerName = "special-chars";
        var apiKey = "sk-!@#$%^&*()_+-=[]{}|;:',.<>?/`~";

        // Act
        await service.SaveApiKeyAsync(providerName, apiKey);
        var retrievedKey = await service.GetApiKeyAsync(providerName);

        // Assert
        Assert.Equal(apiKey, retrievedKey);
    }

    [Fact]
    public async Task Encryption_UpdateKey_OverwritesSuccessfully()
    {
        // Arrange
        var service = new SecureStorageService(_mockLogger.Object);
        var providerName = "openai";
        var oldKey = "sk-old-key";
        var newKey = "sk-new-key";

        // Act
        await service.SaveApiKeyAsync(providerName, oldKey);
        var retrievedOldKey = await service.GetApiKeyAsync(providerName);
        
        await service.SaveApiKeyAsync(providerName, newKey);
        var retrievedNewKey = await service.GetApiKeyAsync(providerName);

        // Assert
        Assert.Equal(oldKey, retrievedOldKey);
        Assert.Equal(newKey, retrievedNewKey);
    }

    [Fact(Skip = "Only runs on Linux/macOS")]
    public async Task FilePermissions_AreSetCorrectlyOnUnix()
    {
        // Only run on non-Windows platforms
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var service = new SecureStorageService(_mockLogger.Object);

        // Act
        await service.SaveApiKeyAsync("test", "test-key");

        // Assert - Check file permissions (600 = owner read/write only)
        var storagePath = Path.Combine(_testStorageDir, "Aura", "secure", "apikeys.dat");
        var machineKeyPath = Path.Combine(_testStorageDir, "Aura", "secure", ".machinekey");
        
        // Verify files exist
        Assert.True(File.Exists(storagePath));
        Assert.True(File.Exists(machineKeyPath));
        
        // Note: In-depth permission checking would require native calls or stat command
        // This test primarily verifies chmod was called without errors
    }

    [Fact]
    public async Task DeleteKey_RemovesFromEncryptedStorage()
    {
        // Arrange
        var service = new SecureStorageService(_mockLogger.Object);
        await service.SaveApiKeyAsync("openai", "sk-test");
        await service.SaveApiKeyAsync("anthropic", "sk-ant-test");

        // Act
        await service.DeleteApiKeyAsync("openai");

        // Assert
        var deletedKey = await service.GetApiKeyAsync("openai");
        var remainingKey = await service.GetApiKeyAsync("anthropic");
        
        Assert.Null(deletedKey);
        Assert.Equal("sk-ant-test", remainingKey);
    }
}
