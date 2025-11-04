using Aura.Core.AI.Orchestration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class LlmBudgetManagerTests
{
    [Fact]
    public void CheckBudget_WithinLimits_ReturnsSuccess()
    {
        var logger = new LoggerFactory().CreateLogger<LlmBudgetManager>();
        var constraint = new LlmBudgetConstraint
        {
            MaxTokensPerOperation = 1000,
            MaxCostPerOperation = 0.10m
        };
        var manager = new LlmBudgetManager(logger, constraint);
        
        var result = manager.CheckBudget("session1", 500, 0.05m);
        
        Assert.True(result.IsWithinBudget);
        Assert.Empty(result.Warnings);
    }
    
    [Fact]
    public void CheckBudget_ExceedsTokenLimit_ReturnsWarning()
    {
        var logger = new LoggerFactory().CreateLogger<LlmBudgetManager>();
        var constraint = new LlmBudgetConstraint
        {
            MaxTokensPerOperation = 1000,
            EnforceHardLimits = false
        };
        var manager = new LlmBudgetManager(logger, constraint);
        
        var result = manager.CheckBudget("session1", 2000, 0.05m);
        
        Assert.True(result.IsWithinBudget);
        Assert.Single(result.Warnings);
        Assert.Contains("tokens", result.Warnings[0]);
    }
    
    [Fact]
    public void CheckBudget_ExceedsCostLimit_WithHardLimits_ReturnsFailure()
    {
        var logger = new LoggerFactory().CreateLogger<LlmBudgetManager>();
        var constraint = new LlmBudgetConstraint
        {
            MaxCostPerOperation = 0.10m,
            EnforceHardLimits = true
        };
        var manager = new LlmBudgetManager(logger, constraint);
        
        var result = manager.CheckBudget("session1", 1000, 0.20m, constraint);
        
        Assert.False(result.IsWithinBudget);
        Assert.Single(result.Warnings);
        Assert.Contains("cost", result.Warnings[0]);
    }
    
    [Fact]
    public void RecordUsage_UpdatesSessionBudget()
    {
        var logger = new LoggerFactory().CreateLogger<LlmBudgetManager>();
        var manager = new LlmBudgetManager(logger);
        
        manager.RecordUsage("session1", 100, 0.01m);
        manager.RecordUsage("session1", 200, 0.02m);
        
        var budget = manager.GetSessionBudget("session1");
        
        Assert.Equal(300, budget.TotalTokensUsed);
        Assert.Equal(0.03m, budget.TotalCostAccrued);
        Assert.Equal(2, budget.OperationCount);
    }
    
    [Fact]
    public void CheckBudget_ExceedsSessionLimit_ReturnsWarning()
    {
        var logger = new LoggerFactory().CreateLogger<LlmBudgetManager>();
        var constraint = new LlmBudgetConstraint
        {
            MaxTokensPerSession = 1000,
            EnforceHardLimits = false
        };
        var manager = new LlmBudgetManager(logger, constraint);
        
        manager.RecordUsage("session1", 800, 0.08m);
        
        var result = manager.CheckBudget("session1", 300, 0.03m);
        
        Assert.True(result.IsWithinBudget);
        Assert.Single(result.Warnings);
        Assert.Contains("session", result.Warnings[0]);
    }
    
    [Fact]
    public void ClearSession_RemovesBudget()
    {
        var logger = new LoggerFactory().CreateLogger<LlmBudgetManager>();
        var manager = new LlmBudgetManager(logger);
        
        manager.RecordUsage("session1", 100, 0.01m);
        manager.ClearSession("session1");
        
        var budget = manager.GetSessionBudget("session1");
        
        Assert.Equal(0, budget.TotalTokensUsed);
        Assert.Equal(0m, budget.TotalCostAccrued);
    }
    
    [Fact]
    public void GetActiveSessions_ReturnsAllSessions()
    {
        var logger = new LoggerFactory().CreateLogger<LlmBudgetManager>();
        var manager = new LlmBudgetManager(logger);
        
        manager.RecordUsage("session1", 100, 0.01m);
        manager.RecordUsage("session2", 200, 0.02m);
        
        var sessions = manager.GetActiveSessions();
        
        Assert.Equal(2, sessions.Count);
    }
}
