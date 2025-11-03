using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Data;
using Aura.Core.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for managing server-side action logging and undo operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ActionsController : ControllerBase
{
    private readonly IActionService _actionService;
    private readonly ILogger<ActionsController> _logger;

    public ActionsController(IActionService actionService, ILogger<ActionsController> logger)
    {
        _actionService = actionService;
        _logger = logger;
    }

    /// <summary>
    /// Records a new action in the action log
    /// </summary>
    /// <param name="request">Action details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Recorded action details</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RecordActionResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<RecordActionResponse>> RecordAction(
        [FromBody] RecordActionRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recording action {ActionType} for user {UserId}, CorrelationId: {CorrelationId}",
            request.ActionType, request.UserId, HttpContext.TraceIdentifier);

        try
        {
            var action = new ActionLogEntity
            {
                UserId = request.UserId,
                ActionType = request.ActionType,
                Description = request.Description,
                AffectedResourceIds = request.AffectedResourceIds,
                PayloadJson = request.PayloadJson,
                InverseActionType = request.InverseActionType,
                InversePayloadJson = request.InversePayloadJson,
                CanBatch = request.CanBatch,
                IsPersistent = request.IsPersistent,
                CorrelationId = request.CorrelationId ?? HttpContext.TraceIdentifier,
                Status = "Applied",
                Timestamp = DateTime.UtcNow
            };

            if (request.RetentionDays.HasValue)
            {
                action.ExpiresAt = DateTime.UtcNow.AddDays(request.RetentionDays.Value);
            }

            var recordedAction = await _actionService.RecordActionAsync(action, cancellationToken);

            var response = new RecordActionResponse
            {
                ActionId = recordedAction.Id,
                Timestamp = recordedAction.Timestamp,
                Status = recordedAction.Status,
                ExpiresAt = recordedAction.ExpiresAt
            };

            _logger.LogInformation("Action {ActionId} recorded successfully", recordedAction.Id);
            return CreatedAtAction(nameof(GetAction), new { id = recordedAction.Id }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record action {ActionType}", request.ActionType);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Failed to record action",
                Detail = ex.Message,
                Status = 500,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Undoes a previously recorded action
    /// </summary>
    /// <param name="id">Action ID to undo</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Undo operation result</returns>
    [HttpPost("{id}/undo")]
    [ProducesResponseType(typeof(UndoActionResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<UndoActionResponse>> UndoAction(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Undoing action {ActionId}, CorrelationId: {CorrelationId}",
            id, HttpContext.TraceIdentifier);

        try
        {
            var action = await _actionService.GetActionAsync(id, cancellationToken);
            if (action == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Action not found",
                    Detail = $"Action with ID {id} does not exist",
                    Status = 404,
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            if (action.Status == "Undone")
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Action already undone",
                    Detail = $"Action {id} was already undone at {action.UndoneAt}",
                    Status = 400,
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            if (action.Status == "Expired")
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Action expired",
                    Detail = $"Action {id} has expired and cannot be undone",
                    Status = 400,
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var userId = "anonymous";
            var success = await _actionService.UndoActionAsync(id, userId, cancellationToken);

            var updatedAction = await _actionService.GetActionAsync(id, cancellationToken);

            var response = new UndoActionResponse
            {
                ActionId = id,
                Success = success,
                UndoneAt = updatedAction?.UndoneAt ?? DateTime.UtcNow,
                Status = updatedAction?.Status ?? "Failed",
                ErrorMessage = success ? null : "Failed to undo action"
            };

            _logger.LogInformation("Action {ActionId} undo completed with success={Success}", id, success);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo action {ActionId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Failed to undo action",
                Detail = ex.Message,
                Status = 500,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Gets action history with optional filters
    /// </summary>
    /// <param name="query">Query parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated action history</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ActionHistoryResponse), 200)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ActionHistoryResponse>> GetActionHistory(
        [FromQuery] ActionHistoryQuery query,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting action history with filters, CorrelationId: {CorrelationId}",
            HttpContext.TraceIdentifier);

        try
        {
            var (actions, totalCount) = await _actionService.GetActionHistoryAsync(
                query.UserId,
                query.ActionType,
                query.Status,
                query.StartDate,
                query.EndDate,
                query.Page,
                Math.Min(query.PageSize, 100),
                cancellationToken);

            var items = actions.Select(a => new ActionHistoryItem
            {
                Id = a.Id,
                UserId = a.UserId,
                ActionType = a.ActionType,
                Description = a.Description,
                Timestamp = a.Timestamp,
                Status = a.Status,
                CanUndo = a.Status == "Applied" && !a.ExpiresAt.HasValue || a.ExpiresAt > DateTime.UtcNow,
                AffectedResourceIds = a.AffectedResourceIds,
                UndoneAt = a.UndoneAt,
                UndoneByUserId = a.UndoneByUserId
            }).ToList();

            var response = new ActionHistoryResponse
            {
                Actions = items,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
            };

            _logger.LogInformation("Retrieved {Count} actions out of {Total}", items.Count, totalCount);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get action history");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Failed to get action history",
                Detail = ex.Message,
                Status = 500,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    /// <summary>
    /// Gets detailed information about a specific action
    /// </summary>
    /// <param name="id">Action ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Action details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ActionDetailResponse), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<ActionDetailResponse>> GetAction(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting action {ActionId}, CorrelationId: {CorrelationId}",
            id, HttpContext.TraceIdentifier);

        try
        {
            var action = await _actionService.GetActionAsync(id, cancellationToken);
            if (action == null)
            {
                return NotFound(new ProblemDetails
                {
                    Title = "Action not found",
                    Detail = $"Action with ID {id} does not exist",
                    Status = 404,
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var response = new ActionDetailResponse
            {
                Id = action.Id,
                UserId = action.UserId,
                ActionType = action.ActionType,
                Description = action.Description,
                Timestamp = action.Timestamp,
                Status = action.Status,
                AffectedResourceIds = action.AffectedResourceIds,
                PayloadJson = action.PayloadJson,
                InverseActionType = action.InverseActionType,
                InversePayloadJson = action.InversePayloadJson,
                CanBatch = action.CanBatch,
                IsPersistent = action.IsPersistent,
                UndoneAt = action.UndoneAt,
                UndoneByUserId = action.UndoneByUserId,
                ExpiresAt = action.ExpiresAt,
                ErrorMessage = action.ErrorMessage,
                CorrelationId = action.CorrelationId
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get action {ActionId}", id);
            return StatusCode(500, new ProblemDetails
            {
                Title = "Failed to get action",
                Detail = ex.Message,
                Status = 500,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }
}
