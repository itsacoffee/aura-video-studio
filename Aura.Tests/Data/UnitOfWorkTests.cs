using System;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Data;

/// <summary>
/// Tests for Unit of Work pattern implementation
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly AuraDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Mock<ILogger<UnitOfWork>> _loggerMock;

    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AuraDbContext(options);
        _loggerMock = new Mock<ILogger<UnitOfWork>>();
        _unitOfWork = new UnitOfWork(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void UnitOfWork_ProvidesAccessToRepositories()
    {
        // Assert
        Assert.NotNull(_unitOfWork.ProjectStates);
        Assert.NotNull(_unitOfWork.ProjectVersions);
        Assert.NotNull(_unitOfWork.Configurations);
        Assert.NotNull(_unitOfWork.ExportHistory);
        Assert.NotNull(_unitOfWork.Templates);
        Assert.NotNull(_unitOfWork.UserSetups);
        Assert.NotNull(_unitOfWork.CustomTemplates);
        Assert.NotNull(_unitOfWork.ActionLogs);
        Assert.NotNull(_unitOfWork.SystemConfigurations);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsChanges()
    {
        // Arrange
        var template = new TemplateEntity
        {
            Name = "Test Template",
            Description = "Test Description",
            Category = "Test",
            TemplateData = "{}"
        };
        await _unitOfWork.Templates.AddAsync(template);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, result); // 1 entity saved
        var saved = await _unitOfWork.Templates.GetByIdAsync(template.Id);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task BeginTransactionAsync_CommitAsync_CommitsTransaction()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        var template1 = new TemplateEntity
        {
            Name = "Template 1",
            Description = "Desc 1",
            Category = "Test",
            TemplateData = "{}"
        };
        var template2 = new TemplateEntity
        {
            Name = "Template 2",
            Description = "Desc 2",
            Category = "Test",
            TemplateData = "{}"
        };

        await _unitOfWork.Templates.AddAsync(template1);
        await _unitOfWork.Templates.AddAsync(template2);

        // Act
        await _unitOfWork.CommitAsync();

        // Assert
        var all = await _unitOfWork.Templates.GetAllAsync();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task BeginTransactionAsync_RollbackAsync_RollsBackTransaction()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        var template = new TemplateEntity
        {
            Name = "Template",
            Description = "Desc",
            Category = "Test",
            TemplateData = "{}"
        };
        await _unitOfWork.Templates.AddAsync(template);

        // Act
        await _unitOfWork.RollbackAsync();

        // Assert
        var all = await _unitOfWork.Templates.GetAllAsync();
        Assert.Empty(all); // Should be rolled back
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenTransactionAlreadyStarted_ThrowsException()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _unitOfWork.BeginTransactionAsync());
    }

    [Fact]
    public async Task CommitAsync_WhenNoTransaction_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _unitOfWork.CommitAsync());
    }

    [Fact]
    public async Task RollbackAsync_WhenNoTransaction_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _unitOfWork.RollbackAsync());
    }

    [Fact]
    public async Task MultipleRepositories_ShareSameContext()
    {
        // Arrange
        var project = new ProjectStateEntity
        {
            Title = "Test Project",
            Status = "InProgress"
        };
        
        var template = new TemplateEntity
        {
            Name = "Test Template",
            Description = "Test Description",
            Category = "Test",
            TemplateData = "{}"
        };

        // Act
        await _unitOfWork.ProjectStates.CreateAsync(project);
        await _unitOfWork.Templates.AddAsync(template);

        // Assert - both should be persisted in the same context
        var savedProject = await _unitOfWork.ProjectStates.GetByIdAsync(project.Id);
        var savedTemplate = await _unitOfWork.Templates.GetByIdAsync(template.Id);
        
        Assert.NotNull(savedProject);
        Assert.NotNull(savedTemplate);
    }

    [Fact]
    public async Task AuditableEntity_AutomaticallySetTimestamps()
    {
        // Arrange
        var template = new CustomTemplateEntity
        {
            Name = "Test Template",
            Description = "Test Description",
            Category = "Test"
        };

        // Act
        await _unitOfWork.CustomTemplates.AddAsync(template);

        // Assert
        var saved = await _unitOfWork.CustomTemplates.GetByIdAsync(template.Id);
        Assert.NotNull(saved);
        Assert.NotEqual(DateTime.MinValue, saved.CreatedAt);
        Assert.NotEqual(DateTime.MinValue, saved.UpdatedAt);
        Assert.Equal(saved.CreatedAt, saved.UpdatedAt); // Should be same on creation
    }

    [Fact]
    public async Task AuditableEntity_UpdatesTimestampOnModification()
    {
        // Arrange
        var template = new CustomTemplateEntity
        {
            Name = "Original Name",
            Description = "Original Description",
            Category = "Test"
        };
        await _unitOfWork.CustomTemplates.AddAsync(template);
        var originalUpdatedAt = template.UpdatedAt;

        // Wait a bit to ensure timestamp difference
        await Task.Delay(10);

        // Act
        template.Name = "Updated Name";
        await _unitOfWork.CustomTemplates.UpdateAsync(template);

        // Assert
        var updated = await _unitOfWork.CustomTemplates.GetByIdAsync(template.Id);
        Assert.NotNull(updated);
        Assert.True(updated.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task SoftDelete_MarksEntityAsDeleted()
    {
        // Arrange
        var template = new CustomTemplateEntity
        {
            Name = "To Delete",
            Description = "Will be soft deleted",
            Category = "Test"
        };
        await _unitOfWork.CustomTemplates.AddAsync(template);
        var id = template.Id;

        // Act
        await _unitOfWork.CustomTemplates.DeleteAsync(template);

        // Assert - soft deleted entities should not be returned by default
        var deleted = await _unitOfWork.CustomTemplates.GetByIdAsync(id);
        Assert.Null(deleted); // Should be null due to global query filter
    }

    [Fact]
    public async Task Transaction_RollbackOnError_PreservesDataIntegrity()
    {
        // Arrange
        var template1 = new TemplateEntity
        {
            Name = "Template 1",
            Description = "Desc 1",
            Category = "Test",
            TemplateData = "{}"
        };

        await _unitOfWork.BeginTransactionAsync();
        await _unitOfWork.Templates.AddAsync(template1);

        // Act - simulate error and rollback
        try
        {
            // Simulate an error
            throw new Exception("Simulated error");
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
        }

        // Assert
        var all = await _unitOfWork.Templates.GetAllAsync();
        Assert.Empty(all); // Should be empty due to rollback
    }
}
