using System;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aura.Tests.Configuration;

/// <summary>
/// Tests to verify the CreatedBy column in ConfigurationEntity
/// This test validates the migration 20251121045900_AddCreatedByToConfigurations
/// </summary>
public class ConfigurationEntityCreatedByTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly AuraDbContext _context;

    public ConfigurationEntityCreatedByTests()
    {
        var services = new ServiceCollection();

        // Setup in-memory database
        services.AddDbContext<AuraDbContext>(options =>
            options.UseInMemoryDatabase($"CreatedByTest_{Guid.NewGuid()}"));

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<AuraDbContext>();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task ConfigurationEntity_Should_AcceptCreatedByProperty()
    {
        // Arrange
        var config = new ConfigurationEntity
        {
            Key = "test.key",
            Value = "test.value",
            Category = "Testing",
            ValueType = "string",
            CreatedBy = "TestUser",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        _context.Configurations.Add(config);
        await _context.SaveChangesAsync();

        // Assert
        var savedConfig = await _context.Configurations.FindAsync("test.key");
        Assert.NotNull(savedConfig);
        Assert.Equal("TestUser", savedConfig.CreatedBy);
    }

    [Fact]
    public async Task ConfigurationEntity_Should_AllowNullCreatedBy()
    {
        // Arrange
        var config = new ConfigurationEntity
        {
            Key = "test.key2",
            Value = "test.value",
            Category = "Testing",
            ValueType = "string",
            CreatedBy = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        _context.Configurations.Add(config);
        await _context.SaveChangesAsync();

        // Assert
        var savedConfig = await _context.Configurations.FindAsync("test.key2");
        Assert.NotNull(savedConfig);
        Assert.Null(savedConfig.CreatedBy);
    }
}
