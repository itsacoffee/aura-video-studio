using System;
using System.Linq;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Data;

/// <summary>
/// Tests for generic repository CRUD operations
/// </summary>
public class GenericRepositoryTests : IDisposable
{
    private readonly AuraDbContext _context;
    private readonly IRepository<TemplateEntity, string> _repository;
    private readonly Mock<ILogger<GenericRepository<TemplateEntity, string>>> _loggerMock;

    public GenericRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuraDbContext(options);
        _loggerMock = new Mock<ILogger<GenericRepository<TemplateEntity, string>>>();
        _repository = new GenericRepository<TemplateEntity, string>(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task AddAsync_CreatesEntity()
    {
        // Arrange
        var entity = new TemplateEntity
        {
            Name = "Test Template",
            Description = "Test Description",
            Category = "Test",
            TemplateData = "{}"
        };

        // Act
        var result = await _repository.AddAsync(entity);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Id);
        Assert.Equal("Test Template", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEntity()
    {
        // Arrange
        var entity = new TemplateEntity
        {
            Id = "test-id",
            Name = "Test Template",
            Description = "Test Description",
            Category = "Test",
            TemplateData = "{}"
        };
        await _repository.AddAsync(entity);

        // Act
        var result = await _repository.GetByIdAsync("test-id");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-id", result.Id);
        Assert.Equal("Test Template", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEntities()
    {
        // Arrange
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 1",
            Description = "Desc 1",
            Category = "Cat1",
            TemplateData = "{}"
        });
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 2",
            Description = "Desc 2",
            Category = "Cat2",
            TemplateData = "{}"
        });

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        // Arrange
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 1",
            Description = "Desc 1",
            Category = "Category A",
            TemplateData = "{}"
        });
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 2",
            Description = "Desc 2",
            Category = "Category B",
            TemplateData = "{}"
        });
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 3",
            Description = "Desc 3",
            Category = "Category A",
            TemplateData = "{}"
        });

        // Act
        var result = await _repository.FindAsync(t => t.Category == "Category A");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal("Category A", t.Category));
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsFirstMatch()
    {
        // Arrange
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 1",
            Description = "Desc 1",
            Category = "Test",
            TemplateData = "{}"
        });

        // Act
        var result = await _repository.FirstOrDefaultAsync(t => t.Category == "Test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Category);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ReturnsNull_WhenNoMatch()
    {
        // Act
        var result = await _repository.FirstOrDefaultAsync(t => t.Category == "NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity()
    {
        // Arrange
        var entity = await _repository.AddAsync(new TemplateEntity
        {
            Name = "Original Name",
            Description = "Original Description",
            Category = "Test",
            TemplateData = "{}"
        });

        // Act
        entity.Name = "Updated Name";
        await _repository.UpdateAsync(entity);

        // Assert
        var updated = await _repository.GetByIdAsync(entity.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        // Arrange
        var entity = await _repository.AddAsync(new TemplateEntity
        {
            Name = "To Delete",
            Description = "Will be deleted",
            Category = "Test",
            TemplateData = "{}"
        });
        var id = entity.Id;

        // Act
        await _repository.DeleteAsync(entity);

        // Assert
        var deleted = await _repository.GetByIdAsync(id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteRangeAsync_RemovesMultipleEntities()
    {
        // Arrange
        var entity1 = await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 1",
            Description = "Desc 1",
            Category = "Test",
            TemplateData = "{}"
        });
        var entity2 = await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 2",
            Description = "Desc 2",
            Category = "Test",
            TemplateData = "{}"
        });

        // Act
        await _repository.DeleteRangeAsync(new[] { entity1, entity2 });

        // Assert
        var all = await _repository.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task AddRangeAsync_CreatesMultipleEntities()
    {
        // Arrange
        var entities = new[]
        {
            new TemplateEntity
            {
                Name = "Template 1",
                Description = "Desc 1",
                Category = "Test",
                TemplateData = "{}"
            },
            new TemplateEntity
            {
                Name = "Template 2",
                Description = "Desc 2",
                Category = "Test",
                TemplateData = "{}"
            }
        };

        // Act
        await _repository.AddRangeAsync(entities);

        // Assert
        var all = await _repository.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task CountAsync_WithoutPredicate_ReturnsTotal()
    {
        // Arrange
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 1",
            Description = "Desc 1",
            Category = "Test",
            TemplateData = "{}"
        });
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 2",
            Description = "Desc 2",
            Category = "Test",
            TemplateData = "{}"
        });

        // Act
        var count = await _repository.CountAsync();

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsMatchingCount()
    {
        // Arrange
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 1",
            Description = "Desc 1",
            Category = "Category A",
            TemplateData = "{}"
        });
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 2",
            Description = "Desc 2",
            Category = "Category B",
            TemplateData = "{}"
        });
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 3",
            Description = "Desc 3",
            Category = "Category A",
            TemplateData = "{}"
        });

        // Act
        var count = await _repository.CountAsync(t => t.Category == "Category A");

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task AnyAsync_ReturnsTrueWhenMatch()
    {
        // Arrange
        await _repository.AddAsync(new TemplateEntity
        {
            Name = "Template 1",
            Description = "Desc 1",
            Category = "Test",
            TemplateData = "{}"
        });

        // Act
        var exists = await _repository.AnyAsync(t => t.Category == "Test");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task AnyAsync_ReturnsFalseWhenNoMatch()
    {
        // Act
        var exists = await _repository.AnyAsync(t => t.Category == "NonExistent");

        // Assert
        Assert.False(exists);
    }
}
