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

            var recordedAction = await _actionService.RecordActionAsync(action, cancellationToken).ConfigureAwait(false);

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
            var action = await _actionService.GetActionAsync(id, cancellationToken).ConfigureAwait(false);
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
            var success = await _actionService.UndoActionAsync(id, userId, cancellationToken).ConfigureAwait(false);

            var updatedAction = await _actionService.GetActionAsync(id, cancellationToken).ConfigureAwait(false);

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
                cancellationToken).ConfigureAwait(false);

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
            var action = await _actionService.GetActionAsync(id, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Exports action history in specified format (CSV or JSON)
    /// </summary>
    /// <param name="query">Query parameters for filtering</param>
    /// <param name="format">Export format (csv or json)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Exported action history file</returns>
    [HttpGet("export")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> ExportActionHistory(
        [FromQuery] ActionHistoryQuery query,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exporting action history in {Format} format, CorrelationId: {CorrelationId}",
            format, HttpContext.TraceIdentifier);

        try
        {
            var normalizedFormat = format.ToLowerInvariant();
            if (normalizedFormat != "csv" && normalizedFormat != "json")
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid format",
                    Detail = "Format must be 'csv' or 'json'",
                    Status = 400,
                    Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
                });
            }

            var (actions, _) = await _actionService.GetActionHistoryAsync(
                query.UserId,
                query.ActionType,
                query.Status,
                query.StartDate,
                query.EndDate,
                1,
                10000,
                cancellationToken).ConfigureAwait(false);

            if (normalizedFormat == "csv")
            {
                var csv = GenerateCsv(actions);
                var fileName = $"action-history-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            else
            {
                var json = System.Text.Json.JsonSerializer.Serialize(actions, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                var fileName = $"action-history-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
                return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", fileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export action history");
            return StatusCode(500, new ProblemDetails
            {
                Title = "Failed to export action history",
                Detail = ex.Message,
                Status = 500,
                Extensions = { ["correlationId"] = HttpContext.TraceIdentifier }
            });
        }
    }

    private static string GenerateCsv(System.Collections.Generic.List<ActionLogEntity> actions)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Id,UserId,ActionType,Description,Timestamp,Status,AffectedResourceIds,UndoneAt,UndoneByUserId,ExpiresAt");

        foreach (var action in actions)
        {
            sb.AppendLine($"\"{action.Id}\",\"{EscapeCsv(action.UserId)}\",\"{EscapeCsv(action.ActionType)}\"," +
                         $"\"{EscapeCsv(action.Description)}\",\"{action.Timestamp:yyyy-MM-dd HH:mm:ss}\",\"{action.Status}\"," +
                         $"\"{EscapeCsv(action.AffectedResourceIds ?? "")}\",\"{action.UndoneAt:yyyy-MM-dd HH:mm:ss}\"," +
                         $"\"{EscapeCsv(action.UndoneByUserId ?? "")}\",\"{action.ExpiresAt:yyyy-MM-dd HH:mm:ss}\"");
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Replace("\"", "\"\"");
    }
}
