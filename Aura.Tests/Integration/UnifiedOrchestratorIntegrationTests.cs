using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Aura.Core.AI.Orchestration;
using Aura.Core.AI.Validation;
using Aura.Core.Models;
using Aura.Core.Models.Narrative;
using Aura.Core.Models.Visual;
using Aura.Core.Providers;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests.Integration;

/// <summary>
/// Integration tests for UnifiedLlmOrchestrator with real pipeline steps
/// </summary>
public class UnifiedOrchestratorIntegrationTests
{
    private readonly Brief _testBrief = new Brief(
        Topic: "Introduction to Machine Learning",
        Audience: "Beginners",
        Goal: "Educational",
        Tone: "Friendly",
        Language: "en-US",
        Aspect: Aspect.Widescreen16x9
    );

    private readonly PlanSpec _testSpec = new PlanSpec(
        TargetDuration: TimeSpan.FromMinutes(3),
        Pacing: Pacing.Conversational,
        Density: Density.Balanced,
        Style: "Educational"
    );

    [Fact]
    public async Task Orchestrator_WithRuleBasedProvider_CompletesOperation()
    {
        var orchestrator = CreateOrchestrator();
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var request = new LlmOperationRequest
        {
            SessionId = "test-session",
            OperationType = LlmOperationType.Completion,
            Prompt = "Generate a brief script introduction",
            EnableCache = false
        };
        
        var response = await orchestrator.ExecuteAsync(request, provider);
        
        Assert.True(response.Success);
        Assert.NotEmpty(response.Content);
        Assert.False(response.WasCached);
        Assert.NotNull(response.Telemetry);
        Assert.Equal("test-session", response.Telemetry.SessionId);
        Assert.True(response.Telemetry.LatencyMs >= 0);
    }

    [Fact]
    public async Task Orchestrator_WithCache_ReturnsCachedOnSecondCall()
    {
        var orchestrator = CreateOrchestrator();
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var request = new LlmOperationRequest
        {
            SessionId = "test-session",
            OperationType = LlmOperationType.Completion,
            Prompt = "Generate a consistent output",
            EnableCache = true
        };
        
        var response1 = await orchestrator.ExecuteAsync(request, provider);
        var response2 = await orchestrator.ExecuteAsync(request, provider);
        
        Assert.True(response1.Success);
        Assert.False(response1.WasCached);
        
        Assert.True(response2.Success);
        Assert.True(response2.WasCached);
        
        Assert.Equal(response1.Content, response2.Content);
        Assert.True(response2.Telemetry.LatencyMs <= response1.Telemetry.LatencyMs);
    }

    [Fact]
    public async Task Orchestrator_TracksBudgetAcrossSessions()
    {
        var orchestrator = CreateOrchestrator();
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var request = new LlmOperationRequest
        {
            SessionId = "budget-test",
            OperationType = LlmOperationType.Completion,
            Prompt = "Test prompt",
            EnableCache = false
        };
        
        await orchestrator.ExecuteAsync(request, provider);
        await orchestrator.ExecuteAsync(request, provider);
        
        var budget = orchestrator.GetSessionBudget("budget-test");
        
        Assert.Equal(2, budget.OperationCount);
        Assert.True(budget.TotalTokensUsed > 0);
        Assert.True(budget.TotalCostAccrued >= 0);
    }

    [Fact]
    public async Task Orchestrator_CollectsTelemetryForMultipleOperations()
    {
        var orchestrator = CreateOrchestrator();
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var sessionId = "telemetry-test";
        
        await ExecuteOperationAsync(orchestrator, provider, sessionId, LlmOperationType.Planning);
        await ExecuteOperationAsync(orchestrator, provider, sessionId, LlmOperationType.Scripting);
        await ExecuteOperationAsync(orchestrator, provider, sessionId, LlmOperationType.SceneAnalysis);
        
        var stats = orchestrator.GetSessionStatistics(sessionId);
        
        Assert.Equal(3, stats.TotalOperations);
        Assert.Equal(3, stats.SuccessfulOperations);
        Assert.True(stats.TotalTokensIn > 0);
        Assert.True(stats.TotalTokensOut >= 0);
        Assert.True(stats.AverageLatencyMs >= 0);
    }

    [Fact]
    public async Task Orchestrator_EnforcesBudgetLimits()
    {
        var constraint = new LlmBudgetConstraint
        {
            MaxTokensPerOperation = 10,
            EnforceHardLimits = true
        };
        
        var orchestrator = CreateOrchestrator(constraint);
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var request = new LlmOperationRequest
        {
            SessionId = "budget-limit-test",
            OperationType = LlmOperationType.Completion,
            Prompt = "This is a very long prompt that will exceed the token budget significantly",
            EnableCache = false,
            BudgetConstraint = constraint
        };
        
        var response = await orchestrator.ExecuteAsync(request, provider);
        
        Assert.False(response.Success);
        Assert.Contains("Budget exceeded", response.ErrorMessage);
    }

    [Fact]
    public async Task Orchestrator_UsesOperationPresets()
    {
        var orchestrator = CreateOrchestrator();
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var planningRequest = new LlmOperationRequest
        {
            SessionId = "preset-test",
            OperationType = LlmOperationType.Planning,
            Prompt = "Generate plan",
            EnableCache = false
        };
        
        var creativeRequest = new LlmOperationRequest
        {
            SessionId = "preset-test",
            OperationType = LlmOperationType.Creative,
            Prompt = "Generate creative content",
            EnableCache = false
        };
        
        var planningResponse = await orchestrator.ExecuteAsync(planningRequest, provider);
        var creativeResponse = await orchestrator.ExecuteAsync(creativeRequest, provider);
        
        Assert.True(planningResponse.Success);
        Assert.True(creativeResponse.Success);
        
        var planningPreset = LlmOperationPresets.GetPreset(LlmOperationType.Planning);
        var creativePreset = LlmOperationPresets.GetPreset(LlmOperationType.Creative);
        
        Assert.Equal(planningPreset.Temperature, planningResponse.Telemetry.Temperature);
        Assert.Equal(creativePreset.Temperature, creativeResponse.Telemetry.Temperature);
        Assert.True(creativeResponse.Telemetry.Temperature > planningResponse.Telemetry.Temperature);
    }

    [Fact]
    public async Task Orchestrator_HandlesCustomPreset()
    {
        var orchestrator = CreateOrchestrator();
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var customPreset = LlmOperationPresets.CreateCustomPreset(
            LlmOperationType.Planning,
            temperature: 0.5,
            maxTokens: 1500);
        
        var request = new LlmOperationRequest
        {
            SessionId = "custom-preset-test",
            OperationType = LlmOperationType.Planning,
            Prompt = "Test with custom preset",
            CustomPreset = customPreset,
            EnableCache = false
        };
        
        var response = await orchestrator.ExecuteAsync(request, provider);
        
        Assert.True(response.Success);
        Assert.Equal(0.5, response.Telemetry.Temperature);
    }

    [Fact]
    public async Task Orchestrator_ClearsSessionData()
    {
        var orchestrator = CreateOrchestrator();
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        var sessionId = "clear-test";
        
        await ExecuteOperationAsync(orchestrator, provider, sessionId, LlmOperationType.Completion);
        
        var budgetBefore = orchestrator.GetSessionBudget(sessionId);
        Assert.True(budgetBefore.OperationCount > 0);
        
        orchestrator.ClearSession(sessionId);
        
        var budgetAfter = orchestrator.GetSessionBudget(sessionId);
        Assert.Equal(0, budgetAfter.OperationCount);
        Assert.Equal(0, budgetAfter.TotalTokensUsed);
    }

    [Fact]
    public async Task Orchestrator_TracksProviderUsage()
    {
        var orchestrator = CreateOrchestrator();
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        await ExecuteOperationAsync(orchestrator, provider, "provider-test", LlmOperationType.Planning);
        await ExecuteOperationAsync(orchestrator, provider, "provider-test", LlmOperationType.Scripting);
        
        var stats = orchestrator.GetStatistics();
        
        Assert.True(stats.OperationsByProvider.ContainsKey("RuleBased"));
        Assert.True(stats.OperationsByProvider["RuleBased"] >= 2);
    }

    [Fact]
    public async Task Orchestrator_TracksOperationTypes()
    {
        var orchestrator = CreateOrchestrator();
        var provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        await ExecuteOperationAsync(orchestrator, provider, "type-test", LlmOperationType.Planning);
        await ExecuteOperationAsync(orchestrator, provider, "type-test", LlmOperationType.Planning);
        await ExecuteOperationAsync(orchestrator, provider, "type-test", LlmOperationType.Scripting);
        
        var stats = orchestrator.GetStatistics();
        
        Assert.True(stats.OperationsByType.ContainsKey(LlmOperationType.Planning));
        Assert.True(stats.OperationsByType.ContainsKey(LlmOperationType.Scripting));
        Assert.Equal(2, stats.OperationsByType[LlmOperationType.Planning]);
        Assert.Equal(1, stats.OperationsByType[LlmOperationType.Scripting]);
    }

    private static UnifiedLlmOrchestrator CreateOrchestrator(LlmBudgetConstraint? budgetConstraint = null)
    {
        var logger = NullLogger<UnifiedLlmOrchestrator>.Instance;
        var cache = new MemoryLlmCache(
            NullLogger<MemoryLlmCache>.Instance,
            Microsoft.Extensions.Options.Options.Create(new LlmCacheOptions()));
        var budgetManager = new LlmBudgetManager(
            NullLogger<LlmBudgetManager>.Instance,
            budgetConstraint);
        var telemetryCollector = new LlmTelemetryCollector();
        var schemaValidator = new SchemaValidator(NullLogger<SchemaValidator>.Instance);
        
        return new UnifiedLlmOrchestrator(
            logger,
            cache,
            budgetManager,
            telemetryCollector,
            schemaValidator);
    }

    private static async Task ExecuteOperationAsync(
        UnifiedLlmOrchestrator orchestrator,
        ILlmProvider provider,
        string sessionId,
        LlmOperationType operationType)
    {
        var request = new LlmOperationRequest
        {
            SessionId = sessionId,
            OperationType = operationType,
            Prompt = $"Test prompt for {operationType}",
            EnableCache = false
        };
        
        await orchestrator.ExecuteAsync(request, provider);
    }
}
