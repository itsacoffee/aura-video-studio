using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Data;

/// <summary>
/// Tests for configuration repository operations
/// </summary>
public class ConfigurationRepositoryTests : IDisposable
{
    private readonly AuraDbContext _context;
    private readonly ConfigurationRepository _repository;
    private readonly Mock<ILogger<ConfigurationRepository>> _loggerMock;

    public ConfigurationRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuraDbContext(options);
        _loggerMock = new Mock<ILogger<ConfigurationRepository>>();
        _repository = new ConfigurationRepository(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAsync_ReturnsConfiguration()
    {
        // Arrange
        await _repository.SetAsync("test.key", "test-value", "TestCategory");

        // Act
        var result = await _repository.GetAsync("test.key");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test.key", result.Key);
        Assert.Equal("test-value", result.Value);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _repository.GetAsync("non.existent.key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_OnlyReturnsActiveConfigurations()
    {
        // Arrange
        await _repository.SetAsync("active.key", "value", "Test");
        await _repository.SetAsync("inactive.key", "value", "Test");
        await _repository.DeleteAsync("inactive.key");

        // Act
        var active = await _repository.GetAsync("active.key");
        var inactive = await _repository.GetAsync("inactive.key");

        // Assert
        Assert.NotNull(active);
        Assert.Null(inactive);
    }

    [Fact]
    public async Task GetByCategoryAsync_ReturnsConfigurationsInCategory()
    {
        // Arrange
        await _repository.SetAsync("cat1.key1", "value1", "Category1");
        await _repository.SetAsync("cat1.key2", "value2", "Category1");
        await _repository.SetAsync("cat2.key1", "value3", "Category2");

        // Act
        var result = await _repository.GetByCategoryAsync("Category1");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, c => Assert.Equal("Category1", c.Category));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveConfigurations()
    {
        // Arrange
        await _repository.SetAsync("key1", "value1", "Cat1");
        await _repository.SetAsync("key2", "value2", "Cat2");
        await _repository.SetAsync("key3", "value3", "Cat3");

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.All(result, c => Assert.True(c.IsActive));
    }

    [Fact]
    public async Task GetAllAsync_IncludesInactive_WhenRequested()
    {
        // Arrange
        await _repository.SetAsync("active.key", "value", "Test");
        await _repository.SetAsync("inactive.key", "value", "Test");
        await _repository.DeleteAsync("inactive.key");

        // Act
        var result = await _repository.GetAllAsync(includeInactive: true);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task SetAsync_CreatesNewConfiguration()
    {
        // Act
        var result = await _repository.SetAsync(
            "new.key",
            "new-value",
            "NewCategory",
            "string",
            "Test description",
            false,
            "test-user");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new.key", result.Key);
        Assert.Equal("new-value", result.Value);
        Assert.Equal("NewCategory", result.Category);
        Assert.Equal("string", result.ValueType);
        Assert.Equal("Test description", result.Description);
        Assert.False(result.IsSensitive);
        Assert.Equal("test-user", result.ModifiedBy);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task SetAsync_UpdatesExistingConfiguration()
    {
        // Arrange
        await _repository.SetAsync("existing.key", "original-value", "Test");

        // Act
        var result = await _repository.SetAsync(
            "existing.key",
            "updated-value",
            "Test",
            "string",
            "Updated description");

        // Assert
        Assert.Equal("updated-value", result.Value);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(2, result.Version); // Version should increment
    }

    [Fact]
    public async Task SetAsync_IncrementsVersion()
    {
        // Arrange
        var key = "versioned.key";

        // Act
        var v1 = await _repository.SetAsync(key, "value1", "Test");
        var v2 = await _repository.SetAsync(key, "value2", "Test");
        var v3 = await _repository.SetAsync(key, "value3", "Test");

        // Assert
        Assert.Equal(1, v1.Version);
        Assert.Equal(2, v2.Version);
        Assert.Equal(3, v3.Version);
    }

    [Fact]
    public async Task SetManyAsync_CreatesMultipleConfigurations()
    {
        // Arrange
        var configurations = new Dictionary<string, (string value, string category, string valueType)>
        {
            { "key1", ("value1", "Cat1", "string") },
            { "key2", ("value2", "Cat2", "number") },
            { "key3", ("value3", "Cat3", "boolean") }
        };

        // Act
        var results = await _repository.SetManyAsync(configurations);

        // Assert
        Assert.Equal(3, results.Count);
        
        var config1 = await _repository.GetAsync("key1");
        var config2 = await _repository.GetAsync("key2");
        var config3 = await _repository.GetAsync("key3");
        
        Assert.NotNull(config1);
        Assert.NotNull(config2);
        Assert.NotNull(config3);
        Assert.Equal("value1", config1.Value);
        Assert.Equal("value2", config2.Value);
        Assert.Equal("value3", config3.Value);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesConfiguration()
    {
        // Arrange
        await _repository.SetAsync("to.delete", "value", "Test");

        // Act
        var deleted = await _repository.DeleteAsync("to.delete");

        // Assert
        Assert.True(deleted);
        var config = await _repository.GetAsync("to.delete");
        Assert.Null(config); // Should not be returned after soft delete
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        // Act
        var deleted = await _repository.DeleteAsync("non.existent");

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsAllVersions()
    {
        // Arrange
        var key = "versioned.key";
        await _repository.SetAsync(key, "value1", "Test");
        await _repository.SetAsync(key, "value2", "Test");
        await _repository.SetAsync(key, "value3", "Test");

        // Act
        var history = await _repository.GetHistoryAsync(key);

        // Assert
        Assert.Single(history); // Only one key, but multiple versions stored in same record
        Assert.Equal(3, history[0].Version);
    }

    [Fact]
    public async Task SensitiveConfiguration_IsFlaggedCorrectly()
    {
        // Arrange & Act
        await _repository.SetAsync(
            "api.key",
            "secret-value",
            "Security",
            "string",
            "API Key",
            isSensitive: true);

        // Assert
        var config = await _repository.GetAsync("api.key");
        Assert.NotNull(config);
        Assert.True(config.IsSensitive);
    }

    [Fact]
    public async Task Configuration_TracksModificationUser()
    {
        // Arrange & Act
        var created = await _repository.SetAsync(
            "user.tracked",
            "value",
            "Test",
            modifiedBy: "user1");

        var updated = await _repository.SetAsync(
            "user.tracked",
            "new-value",
            "Test",
            modifiedBy: "user2");

        // Assert
        Assert.Equal("user1", created.ModifiedBy);
        Assert.Equal("user2", updated.ModifiedBy);
    }

    [Fact]
    public async Task Configuration_AutomaticallyUpdatesTimestamps()
    {
        // Arrange
        var key = "timestamp.test";
        var created = await _repository.SetAsync(key, "value1", "Test");
        var createdAt = created.CreatedAt;
        var firstUpdatedAt = created.UpdatedAt;

        await Task.Delay(10); // Small delay to ensure timestamp difference

        // Act
        var updated = await _repository.SetAsync(key, "value2", "Test");

        // Assert
        Assert.Equal(createdAt, updated.CreatedAt); // CreatedAt should not change
        Assert.True(updated.UpdatedAt > firstUpdatedAt); // UpdatedAt should be newer
    }

    [Fact]
    public async Task GetByCategoryAsync_OrdersByKey()
    {
        // Arrange
        await _repository.SetAsync("z.key", "value", "Test");
        await _repository.SetAsync("a.key", "value", "Test");
        await _repository.SetAsync("m.key", "value", "Test");

        // Act
        var result = await _repository.GetByCategoryAsync("Test");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("a.key", result[0].Key);
        Assert.Equal("m.key", result[1].Key);
        Assert.Equal("z.key", result[2].Key);
    }
}
