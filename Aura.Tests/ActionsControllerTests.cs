using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Data;
using Aura.Core.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for ActionsController
/// </summary>
public class ActionsControllerTests : IDisposable
{
    private readonly AuraDbContext _dbContext;
    private readonly IActionService _actionService;
    private readonly ActionsController _controller;

    public ActionsControllerTests()
    {
        var options = new DbContextOptionsBuilder<AuraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AuraDbContext(options);
        
        var loggerService = new LoggerFactory().CreateLogger<ActionService>();
        _actionService = new ActionService(_dbContext, loggerService);

        var loggerController = new LoggerFactory().CreateLogger<ActionsController>();
        _controller = new ActionsController(_actionService, loggerController);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task RecordAction_ValidRequest_ReturnsCreatedResult()
    {
        var request = new RecordActionRequest
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Create new project",
            AffectedResourceIds = "project-123",
            IsPersistent = true
        };

        var result = await _controller.RecordAction(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<RecordActionResponse>(createdResult.Value);
        
        Assert.NotEqual(Guid.Empty, response.ActionId);
        Assert.Equal("Applied", response.Status);
    }

    [Fact]
    public async Task UndoAction_ExistingAction_ReturnsSuccessResult()
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

        var result = await _controller.UndoAction(action.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<UndoActionResponse>(okResult.Value);
        
        Assert.True(response.Success);
        Assert.Equal("Undone", response.Status);
    }

    [Fact]
    public async Task UndoAction_NonExistentAction_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _controller.UndoAction(nonExistentId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UndoAction_AlreadyUndone_ReturnsBadRequest()
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

        var result = await _controller.UndoAction(action.Id, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetActionHistory_WithFilters_ReturnsFilteredResults()
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
            ActionType = "UpdateTemplate",
            Description = "Update template",
            Status = "Applied",
            Timestamp = DateTime.UtcNow.AddHours(-1)
        }, CancellationToken.None);

        await _actionService.RecordActionAsync(new ActionLogEntity
        {
            UserId = "user1",
            ActionType = "DeleteProject",
            Description = "Delete project",
            Status = "Undone",
            Timestamp = DateTime.UtcNow
        }, CancellationToken.None);

        var query = new ActionHistoryQuery
        {
            UserId = "user1",
            Status = "Applied"
        };

        var result = await _controller.GetActionHistory(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ActionHistoryResponse>(okResult.Value);
        
        Assert.Single(response.Actions);
        Assert.Equal("user1", response.Actions[0].UserId);
        Assert.Equal("Applied", response.Actions[0].Status);
    }

    [Fact]
    public async Task GetActionHistory_Pagination_ReturnsCorrectPage()
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

        var query = new ActionHistoryQuery
        {
            Page = 1,
            PageSize = 10
        };

        var result = await _controller.GetActionHistory(query, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ActionHistoryResponse>(okResult.Value);
        
        Assert.Equal(10, response.Actions.Count);
        Assert.Equal(25, response.TotalCount);
        Assert.Equal(3, response.TotalPages);
    }

    [Fact]
    public async Task GetAction_ExistingAction_ReturnsDetails()
    {
        var action = new ActionLogEntity
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Create new project",
            Status = "Applied",
            Timestamp = DateTime.UtcNow,
            PayloadJson = "{\"name\":\"Test Project\"}",
            InversePayloadJson = "{\"id\":\"project-123\"}"
        };

        await _actionService.RecordActionAsync(action, CancellationToken.None);

        var result = await _controller.GetAction(action.Id, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ActionDetailResponse>(okResult.Value);
        
        Assert.Equal(action.Id, response.Id);
        Assert.Equal("test-user", response.UserId);
        Assert.Equal("CreateProject", response.ActionType);
        Assert.Equal("{\"name\":\"Test Project\"}", response.PayloadJson);
    }

    [Fact]
    public async Task GetAction_NonExistentAction_ReturnsNotFound()
    {
        var nonExistentId = Guid.NewGuid();

        var result = await _controller.GetAction(nonExistentId, CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task RecordAction_WithRetentionPolicy_SetsExpirationDate()
    {
        var request = new RecordActionRequest
        {
            UserId = "test-user",
            ActionType = "CreateProject",
            Description = "Create new project",
            RetentionDays = 30
        };

        var result = await _controller.RecordAction(request, CancellationToken.None);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var response = Assert.IsType<RecordActionResponse>(createdResult.Value);
        
        Assert.NotNull(response.ExpiresAt);
        
        if (response.ExpiresAt.HasValue)
        {
            var expiresAt = response.ExpiresAt.Value;
            var expectedExpiration = DateTime.UtcNow.AddDays(30);
            
            Assert.True((expiresAt - expectedExpiration).TotalHours < 1);
        }
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
