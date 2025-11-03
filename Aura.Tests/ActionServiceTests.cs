using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Unit tests for ActionService
/// </summary>
public class ActionServiceTests : IDisposable
{
    private readonly AuraDbContext _dbContext;
    private readonly IActionService _actionService;

    public ActionServiceTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AuraDbContext(options);
        
        var logger = new LoggerFactory().CreateLogger<ActionService>();
        _actionService = new ActionService(_dbContext, logger);
    }

    [Fact]
    public async Task RecordActionAsync_ValidAction_SavesToDatabase()
    {
        var action = new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Create new project",
            Status = "Applied",
            Timestamp = DateTime.UtcNow
        };

        var result = await _actionService.RecordActionAsync(action, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        
        var savedAction = await _dbContext.ActionLogs.FindAsync(result.Id);
        Assert.NotNull(savedAction);
        Assert.Equal("test-user", savedAction.UserId);
        Assert.Equal("CreateProject", savedAction.ActionType);
    }

    [Fact]
    public async Task UndoActionAsync_ExistingAction_UpdatesStatus()
    {
        var action = new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Create new project",
            Status = "Applied",
            Timestamp = DateTime.UtcNow
        };

        await _actionService.RecordActionAsync(action, CancellationToken.None);

        var success = await _actionService.UndoActionAsync(action.Id, "test-user-2", CancellationToken.None);

        Assert.True(success);
        
        var updatedAction = await _dbContext.ActionLogs.FindAsync(action.Id);
        Assert.Equal("Undone", updatedAction!.Status);
        Assert.NotNull(updatedAction.UndoneAt);
        Assert.Equal("test-user-2", updatedAction.UndoneByUserId);
    }

    [Fact]
    public async Task UndoActionAsync_NonExistentAction_ReturnsFalse()
    {
        var nonExistentId = Guid.NewGuid();

        var success = await _actionService.UndoActionAsync(nonExistentId, "test-user", CancellationToken.None);

        Assert.False(success);
    }

    [Fact]
    public async Task UndoActionAsync_AlreadyUndone_ReturnsFalse()
    {
        var action = new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Create new project",
            Status = "Applied",
            Timestamp = DateTime.UtcNow
        };

        await _actionService.RecordActionAsync(action, CancellationToken.None);
        await _actionService.UndoActionAsync(action.Id, "test-user", CancellationToken.None);

        var success = await _actionService.UndoActionAsync(action.Id, "test-user", CancellationToken.None);

        Assert.False(success);
    }

    [Fact]
    public async Task GetActionHistoryAsync_FilterByUserId_ReturnsMatchingActions()
    {
        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "user1",
            ActionType = "CreateProject",
            Description = "Create project 1",
            Status = "Applied",
            Timestamp = DateTime.UtcNow.AddHours(-2)
        }, CancellationToken.None);

        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "user2",
            ActionType = "CreateProject",
            Description = "Create project 2",
            Status = "Applied",
            Timestamp = DateTime.UtcNow.AddHours(-1)
        }, CancellationToken.None);

        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "user1",
            ActionType = "UpdateProject",
            Description = "Update project",
            Status = "Applied",
            Timestamp = DateTime.UtcNow
        }, CancellationToken.None);

        var (actions, totalCount) = await _actionService.GetActionHistoryAsync(
            userId: "user1",
            cancellationToken: CancellationToken.None);

        Assert.Equal(2, totalCount);
        Assert.Equal(2, actions.Count);
        Assert.All(actions, a => Assert.Equal("user1", a.UserId));
    }

    [Fact]
    public async Task GetActionHistoryAsync_FilterByActionType_ReturnsMatchingActions()
    {
        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Create project",
            Status = "Applied",
            Timestamp = DateTime.UtcNow.AddHours(-2)
        }, CancellationToken.None);

        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "UpdateTemplate",
            Description = "Update template",
            Status = "Applied",
            Timestamp = DateTime.UtcNow.AddHours(-1)
        }, CancellationToken.None);

        var (actions, totalCount) = await _actionService.GetActionHistoryAsync(
            actionType: "CreateProject",
            cancellationToken: CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Single(actions);
        Assert.Equal("CreateProject", actions[0].ActionType);
    }

    [Fact]
    public async Task GetActionHistoryAsync_FilterByStatus_ReturnsMatchingActions()
    {
        var action1 = new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Create project",
            Status = "Applied",
            Timestamp = DateTime.UtcNow.AddHours(-2)
        };
        await _actionService.RecordActionAsync(action1, CancellationToken.None);

        var action2 = new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "DeleteProject",
            Description = "Delete project",
            Status = "Applied",
            Timestamp = DateTime.UtcNow.AddHours(-1)
        };
        await _actionService.RecordActionAsync(action2, CancellationToken.None);
        await _actionService.UndoActionAsync(action2.Id, "test-user", CancellationToken.None);

        var (actions, totalCount) = await _actionService.GetActionHistoryAsync(
            status: "Undone",
            cancellationToken: CancellationToken.None);

        Assert.Equal(1, totalCount);
        Assert.Single(actions);
        Assert.Equal("Undone", actions[0].Status);
    }

    [Fact]
    public async Task GetActionHistoryAsync_Pagination_ReturnsCorrectPage()
    {
        for (int i = 0; i < 25; i++)
        {
            await _actionService.RecordActionAsync(new ActionLogEntity
            {
                UserId = "test-user",
                ActionType = "CreateProject",
                Description = $"Create project {i}",
                Status = "Applied",
                Timestamp = DateTime.UtcNow.AddMinutes(-i)
            }, CancellationToken.None);
        }

        var (actions, totalCount) = await _actionService.GetActionHistoryAsync(
            page: 2,
            pageSize: 10,
            cancellationToken: CancellationToken.None);

        Assert.Equal(25, totalCount);
        Assert.Equal(10, actions.Count);
    }

    [Fact]
    public async Task GetActionAsync_ExistingAction_ReturnsAction()
    {
        var action = new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Create new project",
            Status = "Applied",
            Timestamp = DateTime.UtcNow
        };

        await _actionService.RecordActionAsync(action, CancellationToken.None);

        var result = await _actionService.GetActionAsync(action.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(action.Id, result.Id);
        Assert.Equal("test-user", result.UserId);
    }

    [Fact]
    public async Task GetActionAsync_NonExistentAction_ReturnsNull()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _actionService.GetActionAsync(nonExistentId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CleanupExpiredActionsAsync_MarksExpiredActions()
    {
        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Expired action",
            Status = "Applied",
            Timestamp = DateTime.UtcNow.AddDays(-40),
            ExpiresAt = DateTime.UtcNow.AddDays(-10)
        }, CancellationToken.None);

        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Valid action",
            Status = "Applied",
            Timestamp = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(10)
        }, CancellationToken.None);

        var cleanedCount = await _actionService.CleanupExpiredActionsAsync(CancellationToken.None);

        Assert.Equal(1, cleanedCount);
        
        var expiredActions = await _dbContext.ActionLogs
            .Where(a => a.Status == "Expired")
            .ToListAsync();
        
        Assert.Single(expiredActions);
    }

    [Fact]
    public async Task CleanupExpiredActionsAsync_NoExpiredActions_ReturnsZero()
    {
        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Valid action",
            Status = "Applied",
            Timestamp = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        }, CancellationToken.None);

        var cleanedCount = await _actionService.CleanupExpiredActionsAsync(CancellationToken.None);

        Assert.Equal(0, cleanedCount);
    }

    [Fact]
    public async Task GetActionHistoryAsync_OrderedByTimestamp_ReturnsDescending()
    {
        var timestamp1 = DateTime.UtcNow.AddHours(-3);
        var timestamp2 = DateTime.UtcNow.AddHours(-2);
        var timestamp3 = DateTime.UtcNow.AddHours(-1);

        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Action 1",
            Status = "Applied",
            Timestamp = timestamp1
        }, CancellationToken.None);

        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Action 2",
            Status = "Applied",
            Timestamp = timestamp2
        }, CancellationToken.None);

        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Action 3",
            Status = "Applied",
            Timestamp = timestamp3
        }, CancellationToken.None);

        var (actions, _) = await _actionService.GetActionHistoryAsync(
            cancellationToken: CancellationToken.None);

        Assert.Equal(3, actions.Count);
        Assert.True(actions[0].Timestamp > actions[1].Timestamp);
        Assert.True(actions[1].Timestamp > actions[2].Timestamp);
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
