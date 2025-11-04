using System.Threading.Tasks;
using Aura.Core.AI.Orchestration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

public class LlmParameterOptimizerTests
{
    [Fact]
    public async Task OptimizeAsync_WithoutLlm_ReturnsRuleBasedOptimization()
    {
        var logger = new LoggerFactory().CreateLogger<LlmParameterOptimizer>();
        var optimizer = new LlmParameterOptimizer(logger);
        
        var request = new OptimizationRequest
        {
            OperationType = LlmOperationType.Planning
        };
        
        var suggestion = await optimizer.OptimizeAsync(request);
        
        Assert.NotNull(suggestion);
        Assert.InRange(suggestion.Temperature, 0.0, 1.0);
        Assert.InRange(suggestion.TopP, 0.0, 1.0);
        Assert.True(suggestion.MaxTokens > 0);
        Assert.NotEmpty(suggestion.Rationale);
        Assert.InRange(suggestion.Confidence, 0.0, 1.0);
    }
    
    [Fact]
    public async Task OptimizeAsync_WithMaxTokensConstraint_ReducesTokens()
    {
        var logger = new LoggerFactory().CreateLogger<LlmParameterOptimizer>();
        var optimizer = new LlmParameterOptimizer(logger);
        
        var request = new OptimizationRequest
        {
            OperationType = LlmOperationType.Planning,
            Constraints = new OptimizationConstraints
            {
                MaxTokens = 1000
            }
        };
        
        var suggestion = await optimizer.OptimizeAsync(request);
        
        Assert.True(suggestion.MaxTokens <= 1000);
        Assert.Contains("Token limit", suggestion.Rationale);
    }
    
    [Fact]
    public async Task OptimizeAsync_WithLatencyConstraint_ReducesTimeout()
    {
        var logger = new LoggerFactory().CreateLogger<LlmParameterOptimizer>();
        var optimizer = new LlmParameterOptimizer(logger);
        
        var request = new OptimizationRequest
        {
            OperationType = LlmOperationType.Planning,
            Constraints = new OptimizationConstraints
            {
                MaxLatencySeconds = 30
            }
        };
        
        var suggestion = await optimizer.OptimizeAsync(request);
        
        Assert.True(suggestion.TimeoutSeconds <= 30);
        Assert.Contains("Timeout", suggestion.Rationale);
    }
    
    [Fact]
    public async Task OptimizeAsync_PrioritizeQuality_LowersTemperature()
    {
        var logger = new LoggerFactory().CreateLogger<LlmParameterOptimizer>();
        var optimizer = new LlmParameterOptimizer(logger);
        
        var basePreset = LlmOperationPresets.GetPreset(LlmOperationType.Creative);
        
        var request = new OptimizationRequest
        {
            OperationType = LlmOperationType.Creative,
            Constraints = new OptimizationConstraints
            {
                PrioritizeQuality = true
            }
        };
        
        var suggestion = await optimizer.OptimizeAsync(request);
        
        Assert.True(suggestion.Temperature <= basePreset.Temperature);
    }
    
    [Fact]
    public async Task OptimizeAsync_PrioritizeSpeed_MaintainsOrIncreasesTemperature()
    {
        var logger = new LoggerFactory().CreateLogger<LlmParameterOptimizer>();
        var optimizer = new LlmParameterOptimizer(logger);
        
        var basePreset = LlmOperationPresets.GetPreset(LlmOperationType.SceneAnalysis);
        
        var request = new OptimizationRequest
        {
            OperationType = LlmOperationType.SceneAnalysis,
            Constraints = new OptimizationConstraints
            {
                PrioritizeQuality = false
            }
        };
        
        var suggestion = await optimizer.OptimizeAsync(request);
        
        Assert.True(suggestion.Temperature >= basePreset.Temperature);
    }
    
    [Fact]
    public async Task ExplainAdjustmentsAsync_WithoutLlm_ReturnsExplanation()
    {
        var logger = new LoggerFactory().CreateLogger<LlmParameterOptimizer>();
        var optimizer = new LlmParameterOptimizer(logger);
        
        var basePreset = LlmOperationPresets.GetPreset(LlmOperationType.Planning);
        var adjustedPreset = LlmOperationPresets.CreateCustomPreset(
            LlmOperationType.Planning,
            temperature: 0.3,
            maxTokens: 1000);
        
        var explanation = await optimizer.ExplainAdjustmentsAsync(
            basePreset,
            adjustedPreset,
            "cost constraints");
        
        Assert.NotEmpty(explanation);
        Assert.Contains("temperature", explanation.ToLower());
    }
    
    [Fact]
    public async Task OptimizeAsync_ForDifferentOperationTypes_ReturnsDifferentPresets()
    {
        var logger = new LoggerFactory().CreateLogger<LlmParameterOptimizer>();
        var optimizer = new LlmParameterOptimizer(logger);
        
        var planningRequest = new OptimizationRequest { OperationType = LlmOperationType.Planning };
        var creativRequest = new OptimizationRequest { OperationType = LlmOperationType.Creative };
        
        var planningSuggestion = await optimizer.OptimizeAsync(planningRequest);
        var creativeSuggestion = await optimizer.OptimizeAsync(creativRequest);
        
        Assert.True(creativeSuggestion.Temperature > planningSuggestion.Temperature);
    }
}
