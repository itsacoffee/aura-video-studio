using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for managing server-side action logging and undo operations
/// </summary>
public interface IActionService
{
    /// <summary>
    /// Records a new action in the action log
    /// </summary>
    Task<ActionLogEntity> RecordActionAsync(ActionLogEntity action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Undoes an action by executing its inverse operation
    /// </summary>
    Task<bool> UndoActionAsync(Guid actionId, string undoneByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets action history for a user
    /// </summary>
    Task<(List<ActionLogEntity> Actions, int TotalCount)> GetActionHistoryAsync(
        string? userId = null,
        string? actionType = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information about an action
    /// </summary>
    Task<ActionLogEntity?> GetActionAsync(Guid actionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired actions based on retention policy
    /// </summary>
    Task<int> CleanupExpiredActionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of action service
/// </summary>
public class ActionService : IActionService
{
    private readonly AuraDbContext _dbContext;
    private readonly ILogger<ActionService> _logger;

    public ActionService(AuraDbContext dbContext, ILogger<ActionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ActionLogEntity> RecordActionAsync(ActionLogEntity action, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Recording action {ActionType} for user {UserId}", 
            action.ActionType, action.UserId);

        try
        {
            _dbContext.ActionLogs.Add(action);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Action {ActionId} recorded successfully", action.Id);
            return action;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record action {ActionType}", action.ActionType);
            throw;
        }
    }

    public async Task<bool> UndoActionAsync(Guid actionId, string undoneByUserId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to undo action {ActionId} by user {UserId}", 
            actionId, undoneByUserId);

        try
        {
            var action = await _dbContext.ActionLogs
                .FirstOrDefaultAsync(a => a.Id == actionId, cancellationToken).ConfigureAwait(false);

            if (action == null)
            {
                _logger.LogWarning("Action {ActionId} not found", actionId);
                return false;
            }

            if (action.Status == "Undone")
            {
                _logger.LogWarning("Action {ActionId} already undone", actionId);
                return false;
            }

            if (action.Status == "Expired")
            {
                _logger.LogWarning("Action {ActionId} has expired", actionId);
                return false;
            }

            action.Status = "Undone";
            action.UndoneAt = DateTime.UtcNow;
            action.UndoneByUserId = undoneByUserId;

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Action {ActionId} undone successfully", actionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to undo action {ActionId}", actionId);
            throw;
        }
    }

    public async Task<(List<ActionLogEntity> Actions, int TotalCount)> GetActionHistoryAsync(
        string? userId = null,
        string? actionType = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting action history with filters: userId={UserId}, actionType={ActionType}, status={Status}",
            userId, actionType, status);

        try
        {
            var query = _dbContext.ActionLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            if (!string.IsNullOrEmpty(actionType))
            {
                query = query.Where(a => a.ActionType == actionType);
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

            var actions = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Retrieved {Count} actions out of {Total}", actions.Count, totalCount);
            return (actions, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve action history");
            throw;
        }
    }

    public async Task<ActionLogEntity?> GetActionAsync(Guid actionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting action {ActionId}", actionId);

        try
        {
            return await _dbContext.ActionLogs
                .FirstOrDefaultAsync(a => a.Id == actionId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get action {ActionId}", actionId);
            throw;
        }
    }

    public async Task<int> CleanupExpiredActionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up expired actions");

        try
        {
            var now = DateTime.UtcNow;
            var expiredActions = await _dbContext.ActionLogs
                .Where(a => a.ExpiresAt.HasValue && a.ExpiresAt.Value < now)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            if (expiredActions.Count == 0)
            {
                _logger.LogInformation("No expired actions found");
                return 0;
            }

            foreach (var action in expiredActions)
            {
                action.Status = "Expired";
            }

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Marked {Count} actions as expired", expiredActions.Count);
            return expiredActions.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup expired actions");
            throw;
        }
    }
}
