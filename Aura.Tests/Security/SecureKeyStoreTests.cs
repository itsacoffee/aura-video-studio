using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Security;
using Aura.Core.Services;
using Aura.Core.Services.Providers.Stickiness;
using Aura.Tests.TestUtilities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Security;

[Collection("SecureKeyStoreTests")]
public class SecureKeyStoreTests : IDisposable
{
    private readonly Mock<ILogger<SecureKeyStore>> _mockLogger;
    private readonly Mock<ISecureStorageService> _mockSecureStorage;
    private readonly ProviderSettingsTestContext _settingsContext;
    private readonly ProviderSettings _providerSettings;
    private readonly string _dataDirectory;
    private readonly SecureKeyStore _service;

    public SecureKeyStoreTests()
    {
        _mockLogger = new Mock<ILogger<SecureKeyStore>>();
        _mockSecureStorage = new Mock<ISecureStorageService>();
        _settingsContext = new ProviderSettingsTestContext();
        _providerSettings = _settingsContext.Settings;
        _dataDirectory = _providerSettings.GetAuraDataDirectory();

        _service = new SecureKeyStore(
            _mockLogger.Object,
            _mockSecureStorage.Object,
            _providerSettings);
    }

    public void Dispose()
    {
        _settingsContext.Dispose();

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task SaveAtomicAsync_WithKeys_SavesSuccessfully()
    {
        // Arrange
        var apiKeys = new Dictionary<string, string>
        {
            ["openai"] = "sk-test123",
            ["anthropic"] = "sk-ant-456"
        };
        var selectedProvider = "openai";

        _mockSecureStorage
            .Setup(x => x.SaveApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAtomicAsync(apiKeys, selectedProvider, null, CancellationToken.None);

        // Assert
        _mockSecureStorage.Verify(x => x.SaveApiKeyAsync("openai", "sk-test123"), Times.Once);
        _mockSecureStorage.Verify(x => x.SaveApiKeyAsync("anthropic", "sk-ant-456"), Times.Once);
    }

    [Fact]
    public async Task SaveAtomicAsync_WithProfileLock_PersistsLock()
    {
        // Arrange
        var apiKeys = new Dictionary<string, string> { ["openai"] = "sk-test" };
        var profileLock = new ProviderProfileLock(
            "job123",
            "OpenAI",
            "cloud_llm",
            true,
            false,
            new[] { "script_generation" },
            new ProviderProfileLockMetadata
            {
                Reason = "User preference",
                Source = "User",
                AllowManualFallback = true
            });

        _mockSecureStorage
            .Setup(x => x.SaveApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAtomicAsync(apiKeys, "openai", profileLock, CancellationToken.None);

        // Assert - keystore file should exist
        var keystorePath = Path.Combine(_dataDirectory, "secure-keystore.dat");
        Assert.True(File.Exists(keystorePath));
    }

    [Fact]
    public async Task LoadAtomicAsync_WithIntegrityCheck_ReturnsData()
    {
        // Arrange
        var apiKeys = new Dictionary<string, string> { ["openai"] = "sk-test" };

        _mockSecureStorage
            .Setup(x => x.SaveApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        await _service.SaveAtomicAsync(apiKeys, "openai", null, CancellationToken.None);

        // Act
        var loaded = await _service.LoadAtomicAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("openai", loaded.SelectedProviderId);
        Assert.Single(loaded.ApiKeys);
    }

    [Fact]
    public async Task LoadAtomicAsync_CorruptedData_ReturnsNull()
    {
        // Arrange - create corrupted keystore file
        var keystorePath = Path.Combine(_dataDirectory, "secure-keystore.dat");
        var integrityPath = Path.Combine(_dataDirectory, "secure-keystore.integrity");

        await File.WriteAllBytesAsync(keystorePath, new byte[] { 0x00, 0x01, 0x02 });
        await File.WriteAllTextAsync(integrityPath, "invalid-hash");

        // Act
        var loaded = await _service.LoadAtomicAsync(CancellationToken.None);

        // Assert
        Assert.Null(loaded);
    }

    [Fact]
    public async Task SaveAtomicAsync_CreatesIntegrityFile()
    {
        // Arrange
        var apiKeys = new Dictionary<string, string> { ["openai"] = "sk-test" };

        _mockSecureStorage
            .Setup(x => x.SaveApiKeyAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        await _service.SaveAtomicAsync(apiKeys, null, null, CancellationToken.None);

        // Assert
        var integrityPath = Path.Combine(_dataDirectory, "secure-keystore.integrity");
        Assert.True(File.Exists(integrityPath));

        var integrityHash = await File.ReadAllTextAsync(integrityPath);
        Assert.False(string.IsNullOrWhiteSpace(integrityHash));
    }
}
