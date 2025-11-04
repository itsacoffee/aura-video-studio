using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Orchestration;

/// <summary>
/// Budget constraint for LLM operations
/// </summary>
public record LlmBudgetConstraint
{
    /// <summary>
    /// Maximum tokens allowed per operation
    /// </summary>
    public int? MaxTokensPerOperation { get; init; }
    
    /// <summary>
    /// Maximum cost allowed per operation (USD)
    /// </summary>
    public decimal? MaxCostPerOperation { get; init; }
    
    /// <summary>
    /// Maximum tokens allowed per session/job
    /// </summary>
    public int? MaxTokensPerSession { get; init; }
    
    /// <summary>
    /// Maximum cost allowed per session/job (USD)
    /// </summary>
    public decimal? MaxCostPerSession { get; init; }
    
    /// <summary>
    /// Whether to enforce hard limits (throw exception) or soft limits (log warning)
    /// </summary>
    public bool EnforceHardLimits { get; init; } = true;
}

/// <summary>
/// Budget tracking for a session
/// </summary>
public class SessionBudget
{
    public string SessionId { get; }
    public int TotalTokensUsed { get; private set; }
    public decimal TotalCostAccrued { get; private set; }
    public int OperationCount { get; private set; }
    public DateTime StartedAt { get; }
    public DateTime LastUpdatedAt { get; private set; }
    
    public SessionBudget(string sessionId)
    {
        SessionId = sessionId;
        StartedAt = DateTime.UtcNow;
        LastUpdatedAt = DateTime.UtcNow;
    }
    
    public void RecordUsage(int tokens, decimal cost)
    {
        TotalTokensUsed += tokens;
        TotalCostAccrued += cost;
        OperationCount++;
        LastUpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Manages token budgets and cost tracking for LLM operations
/// </summary>
public class LlmBudgetManager
{
    private readonly ILogger<LlmBudgetManager> _logger;
    private readonly ConcurrentDictionary<string, SessionBudget> _sessionBudgets = new();
    private readonly LlmBudgetConstraint _defaultConstraint;
    
    public LlmBudgetManager(ILogger<LlmBudgetManager> logger, LlmBudgetConstraint? defaultConstraint = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _defaultConstraint = defaultConstraint ?? new LlmBudgetConstraint
        {
            MaxTokensPerOperation = 10000,
            MaxCostPerOperation = 1.0m,
            MaxTokensPerSession = 100000,
            MaxCostPerSession = 10.0m,
            EnforceHardLimits = false
        };
    }
    
    /// <summary>
    /// Checks if an operation is within budget
    /// </summary>
    public BudgetCheckResult CheckBudget(
        string sessionId,
        int estimatedTokens,
        decimal estimatedCost,
        LlmBudgetConstraint? customConstraint = null)
    {
        var constraint = customConstraint ?? _defaultConstraint;
        var sessionBudget = _sessionBudgets.GetOrAdd(sessionId, id => new SessionBudget(id));
        
        var result = new BudgetCheckResult
        {
            IsWithinBudget = true,
            Warnings = new System.Collections.Generic.List<string>()
        };
        
        if (constraint.MaxTokensPerOperation.HasValue && estimatedTokens > constraint.MaxTokensPerOperation.Value)
        {
            result.IsWithinBudget = false;
            result.Warnings.Add($"Estimated tokens ({estimatedTokens}) exceeds per-operation limit ({constraint.MaxTokensPerOperation.Value})");
        }
        
        if (constraint.MaxCostPerOperation.HasValue && estimatedCost > constraint.MaxCostPerOperation.Value)
        {
            result.IsWithinBudget = false;
            result.Warnings.Add($"Estimated cost (${estimatedCost:F4}) exceeds per-operation limit (${constraint.MaxCostPerOperation.Value:F2})");
        }
        
        if (constraint.MaxTokensPerSession.HasValue)
        {
            var projectedSessionTokens = sessionBudget.TotalTokensUsed + estimatedTokens;
            if (projectedSessionTokens > constraint.MaxTokensPerSession.Value)
            {
                result.IsWithinBudget = false;
                result.Warnings.Add($"Projected session tokens ({projectedSessionTokens}) would exceed session limit ({constraint.MaxTokensPerSession.Value})");
            }
        }
        
        if (constraint.MaxCostPerSession.HasValue)
        {
            var projectedSessionCost = sessionBudget.TotalCostAccrued + estimatedCost;
            if (projectedSessionCost > constraint.MaxCostPerSession.Value)
            {
                result.IsWithinBudget = false;
                result.Warnings.Add($"Projected session cost (${projectedSessionCost:F4}) would exceed session limit (${constraint.MaxCostPerSession.Value:F2})");
            }
        }
        
        if (!result.IsWithinBudget)
        {
            if (constraint.EnforceHardLimits)
            {
                _logger.LogError("Budget exceeded for session {SessionId}: {Warnings}", 
                    sessionId, string.Join("; ", result.Warnings));
            }
            else
            {
                _logger.LogWarning("Budget warning for session {SessionId}: {Warnings}", 
                    sessionId, string.Join("; ", result.Warnings));
                result.IsWithinBudget = true;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Records actual usage for a completed operation
    /// </summary>
    public void RecordUsage(string sessionId, int actualTokens, decimal actualCost)
    {
        var sessionBudget = _sessionBudgets.GetOrAdd(sessionId, id => new SessionBudget(id));
        sessionBudget.RecordUsage(actualTokens, actualCost);
        
        _logger.LogInformation(
            "Session {SessionId} usage: {Tokens} tokens, ${Cost:F4} (total: {TotalTokens} tokens, ${TotalCost:F4})",
            sessionId, actualTokens, actualCost, sessionBudget.TotalTokensUsed, sessionBudget.TotalCostAccrued);
    }
    
    /// <summary>
    /// Gets the current budget status for a session
    /// </summary>
    public SessionBudget GetSessionBudget(string sessionId)
    {
        return _sessionBudgets.GetOrAdd(sessionId, id => new SessionBudget(id));
    }
    
    /// <summary>
    /// Clears the budget for a completed session
    /// </summary>
    public void ClearSession(string sessionId)
    {
        if (_sessionBudgets.TryRemove(sessionId, out var budget))
        {
            _logger.LogInformation(
                "Cleared session {SessionId} budget: {TotalTokens} tokens, ${TotalCost:F4}, {Operations} operations",
                sessionId, budget.TotalTokensUsed, budget.TotalCostAccrued, budget.OperationCount);
        }
    }
    
    /// <summary>
    /// Gets all active sessions
    /// </summary>
    public System.Collections.Generic.IReadOnlyCollection<SessionBudget> GetActiveSessions()
    {
        return _sessionBudgets.Values.ToList();
    }
}

/// <summary>
/// Result of a budget check
/// </summary>
public class BudgetCheckResult
{
    public bool IsWithinBudget { get; set; }
    public System.Collections.Generic.List<string> Warnings { get; set; } = new();
}
