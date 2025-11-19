using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Aura.Core.Models.Streaming;
using Aura.Core.AI.Orchestration;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Aura.Core.Models.Streaming;
using Aura.Core.AI.Validation;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Aura.Core.Models.Streaming;
using Aura.Core.Models;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Aura.Core.Models.Streaming;
using Aura.Core.Models.Narrative;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Aura.Core.Models.Streaming;
using Aura.Core.Models.Visual;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Aura.Core.Models.Streaming;
using Aura.Core.Providers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Aura.Core.Models.Streaming;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests to verify that middleware (content safety, telemetry, retry/backoff) 
/// is properly invoked for all orchestrator operations
/// </summary>
public class OrchestratorMiddlewareTests
{
    /// <summary>
    /// Verifies that telemetry is collected for successful LLM operations
    /// </summary>
    [Fact]
    public async Task LlmOrchestrator_SuccessfulOperation_CollectsTelemetry()
    {
        var telemetryCollector = new LlmTelemetryCollector();
        var orchestrator = CreateOrchestrator(telemetryCollector);
        var provider = new TestMockLlmProvider("Test response");
        
        var request = new LlmOperationRequest
        {
            SessionId = "telemetry-test",
            OperationType = LlmOperationType.Completion,
            Prompt = "Test prompt",
            EnableCache = false
        };
        
        var response = await orchestrator.ExecuteAsync(request, provider);
        
        Assert.True(response.Success);
        Assert.NotNull(response.Telemetry);
        Assert.Equal("telemetry-test", response.Telemetry.SessionId);
        Assert.Equal(LlmOperationType.Completion, response.Telemetry.OperationType);
        Assert.True(response.Telemetry.Success);
        Assert.True(response.Telemetry.LatencyMs > 0);
        
        var stats = telemetryCollector.GetStatistics();
        Assert.Equal(1, stats.TotalOperations);
    }
    
    /// <summary>
    /// Verifies that telemetry is collected even for failed operations
    /// </summary>
    [Fact]
    public async Task LlmOrchestrator_FailedOperation_CollectsTelemetry()
    {
        var telemetryCollector = new LlmTelemetryCollector();
        var orchestrator = CreateOrchestrator(telemetryCollector);
        var provider = new MockFailingLlmProvider("Operation failed");
        
        var request = new LlmOperationRequest
        {
            SessionId = "telemetry-failure-test",
            OperationType = LlmOperationType.Completion,
            Prompt = "Test prompt",
            EnableCache = false
        };
        
        var response = await orchestrator.ExecuteAsync(request, provider);
        
        Assert.False(response.Success);
        Assert.NotNull(response.Telemetry);
        Assert.False(response.Telemetry.Success);
        Assert.NotEmpty(response.Telemetry.ErrorMessage ?? string.Empty);
        
        var stats = telemetryCollector.GetStatistics();
        Assert.Equal(1, stats.TotalOperations);
        Assert.Equal(0, stats.SuccessfulOperations);
        Assert.Equal(1, stats.FailedOperations);
    }
    
    /// <summary>
    /// Verifies that retry logic is applied for transient failures
    /// </summary>
    [Fact]
    public async Task LlmOrchestrator_TransientFailure_RetriesOperation()
    {
        var telemetryCollector = new LlmTelemetryCollector();
        var orchestrator = CreateOrchestrator(telemetryCollector);
        var provider = new MockRetryableLlmProvider(failCount: 2, successResponse: "Success after retries");
        
        var request = new LlmOperationRequest
        {
            SessionId = "retry-test",
            OperationType = LlmOperationType.Completion,
            Prompt = "Test prompt",
            EnableCache = false,
            CustomPreset = new LlmOperationPreset
            {
                MaxRetries = 3,
                TimeoutSeconds = 30
            }
        };
        
        var response = await orchestrator.ExecuteAsync(request, provider);
        
        Assert.True(response.Success);
        Assert.Equal("Success after retries", response.Content);
        Assert.True(response.Telemetry.RetryCount >= 2);
    }
    
    /// <summary>
    /// Verifies that budget management middleware enforces limits
    /// </summary>
    [Fact]
    public async Task LlmOrchestrator_BudgetExceeded_BlocksOperation()
    {
        var telemetryCollector = new LlmTelemetryCollector();
        var orchestrator = CreateOrchestrator(telemetryCollector);
        var provider = new TestMockLlmProvider("Should not execute");
        
        var budgetConstraint = new LlmBudgetConstraint
        {
            MaxTokensPerOperation = 10,
            MaxCostPerOperation = 0.001m,
            EnforceHardLimits = true
        };
        
        var request = new LlmOperationRequest
        {
            SessionId = "budget-test",
            OperationType = LlmOperationType.Completion,
            Prompt = new string('x', 10000),
            BudgetConstraint = budgetConstraint,
            EnableCache = false
        };
        
        var response = await orchestrator.ExecuteAsync(request, provider);
        
        Assert.False(response.Success);
        Assert.Contains("Budget exceeded", response.ErrorMessage ?? string.Empty);
    }
    
    /// <summary>
    /// Verifies that caching middleware reduces redundant calls
    /// </summary>
    [Fact]
    public async Task LlmOrchestrator_CachingEnabled_ReducesProviderCalls()
    {
        var telemetryCollector = new LlmTelemetryCollector();
        var orchestrator = CreateOrchestrator(telemetryCollector);
        var provider = new CountingMockLlmProvider("Cached response");
        
        var request = new LlmOperationRequest
        {
            SessionId = "cache-test",
            OperationType = LlmOperationType.Completion,
            Prompt = "Deterministic prompt",
            EnableCache = true
        };
        
        var response1 = await orchestrator.ExecuteAsync(request, provider);
        Assert.True(response1.Success);
        Assert.False(response1.WasCached);
        Assert.Equal(1, provider.CallCount);
        
        var response2 = await orchestrator.ExecuteAsync(request, provider);
        Assert.True(response2.Success);
        Assert.True(response2.WasCached);
        Assert.Equal(1, provider.CallCount);
        
        var stats = telemetryCollector.GetStatistics();
        Assert.Equal(0.5, stats.CacheHitRate);
    }
    
    /// <summary>
    /// Verifies that session-level budget tracking works across multiple operations
    /// </summary>
    [Fact]
    public async Task LlmOrchestrator_SessionBudget_TracksAcrossOperations()
    {
        var telemetryCollector = new LlmTelemetryCollector();
        var orchestrator = CreateOrchestrator(telemetryCollector);
        var provider = new TestMockLlmProvider("Response");
        
        var sessionId = "session-budget-test";
        
        for (int i = 0; i < 3; i++)
        {
            var request = new LlmOperationRequest
            {
                SessionId = sessionId,
                OperationType = LlmOperationType.Completion,
                Prompt = $"Request {i}",
                EnableCache = false
            };
            
            var response = await orchestrator.ExecuteAsync(request, provider);
            Assert.True(response.Success);
        }
        
        var sessionStats = telemetryCollector.GetSessionStatistics(sessionId);
        Assert.Equal(3, sessionStats.TotalOperations);
        Assert.True(sessionStats.TotalEstimatedCost > 0);
    }
    
    private UnifiedLlmOrchestrator CreateOrchestrator(LlmTelemetryCollector telemetryCollector)
    {
        var logger = new LoggerFactory().CreateLogger<UnifiedLlmOrchestrator>();
        var cache = new MemoryLlmCache(
            new LoggerFactory().CreateLogger<MemoryLlmCache>(), 
            Microsoft.Extensions.Options.Options.Create(new LlmCacheOptions()));
        var budgetManager = new LlmBudgetManager(new LoggerFactory().CreateLogger<LlmBudgetManager>());
        var schemaValidator = new SchemaValidator(new LoggerFactory().CreateLogger<SchemaValidator>());
        
        return new UnifiedLlmOrchestrator(
            logger,
            cache,
            budgetManager,
            telemetryCollector,
            schemaValidator);
    }
}

internal sealed class TestMockLlmProvider : ILlmProvider
{
    private readonly string _response;
    
    public TestMockLlmProvider(string response)
    {
        _response = response;
    }
    
    public Task<string> CompleteAsync(string prompt, CancellationToken ct)
    {
        return Task.FromResult(_response);
    }
    
    public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        return Task.FromResult(_response);
    }
    
    public Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<SceneAnalysisResult?>(null);
    
    public Task<VisualPromptResult?> GenerateVisualPromptAsync(string sceneText, string? previousSceneText, string videoTone, VisualStyle targetStyle, CancellationToken ct)
        => Task.FromResult<VisualPromptResult?>(null);
    
    public Task<ContentComplexityAnalysisResult?> AnalyzeContentComplexityAsync(string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<ContentComplexityAnalysisResult?>(null);
    
    public Task<SceneCoherenceResult?> AnalyzeSceneCoherenceAsync(string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<SceneCoherenceResult?>(null);
    
    public Task<NarrativeArcResult?> ValidateNarrativeArcAsync(System.Collections.Generic.IReadOnlyList<string> sceneTexts, string videoGoal, string videoType, CancellationToken ct)
        => Task.FromResult<NarrativeArcResult?>(null);
    
    public Task<string?> GenerateTransitionTextAsync(string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<string?>(null);
    
    public bool SupportsStreaming => true;
    
    public LlmProviderCharacteristics GetCharacteristics()
    {
        return new LlmProviderCharacteristics
        {
            IsLocal = true,
            ExpectedFirstTokenMs = 0,
            ExpectedTokensPerSec = 100,
            SupportsStreaming = true,
            ProviderTier = "Test",
            CostPer1KTokens = null
        };
    }
    
    public async IAsyncEnumerable<LlmStreamChunk> DraftScriptStreamAsync(
        Brief brief,
        PlanSpec spec,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var result = await DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);
        
        yield return new LlmStreamChunk
        {
            ProviderName = "TestMock",
            Content = result,
            AccumulatedContent = result,
            TokenIndex = result.Length / 4,
            IsFinal = true,
            Metadata = new LlmStreamMetadata
            {
                TotalTokens = result.Length / 4,
                EstimatedCost = null,
                IsLocalModel = true,
                ModelName = "mock",
                FinishReason = "stop"
            }
        };
    }
}

internal sealed class CountingMockLlmProvider : ILlmProvider
{
    private readonly string _response;
    public int CallCount { get; private set; }
    
    public CountingMockLlmProvider(string response)
    {
        _response = response;
    }
    
    public Task<string> CompleteAsync(string prompt, CancellationToken ct)
    {
        CallCount++;
        return Task.FromResult(_response);
    }
    
    public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        CallCount++;
        return Task.FromResult(_response);
    }
    
    public Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<SceneAnalysisResult?>(null);
    
    public Task<VisualPromptResult?> GenerateVisualPromptAsync(string sceneText, string? previousSceneText, string videoTone, VisualStyle targetStyle, CancellationToken ct)
        => Task.FromResult<VisualPromptResult?>(null);
    
    public Task<ContentComplexityAnalysisResult?> AnalyzeContentComplexityAsync(string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<ContentComplexityAnalysisResult?>(null);
    
    public Task<SceneCoherenceResult?> AnalyzeSceneCoherenceAsync(string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<SceneCoherenceResult?>(null);
    
    public Task<NarrativeArcResult?> ValidateNarrativeArcAsync(System.Collections.Generic.IReadOnlyList<string> sceneTexts, string videoGoal, string videoType, CancellationToken ct)
        => Task.FromResult<NarrativeArcResult?>(null);
    
    public Task<string?> GenerateTransitionTextAsync(string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<string?>(null);
    
    public bool SupportsStreaming => true;
    
    public LlmProviderCharacteristics GetCharacteristics()
    {
        return new LlmProviderCharacteristics
        {
            IsLocal = true,
            ExpectedFirstTokenMs = 0,
            ExpectedTokensPerSec = 100,
            SupportsStreaming = true,
            ProviderTier = "Test",
            CostPer1KTokens = null
        };
    }
    
    public async IAsyncEnumerable<LlmStreamChunk> DraftScriptStreamAsync(
        Brief brief,
        PlanSpec spec,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var result = await DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);
        
        yield return new LlmStreamChunk
        {
            ProviderName = "CountingMock",
            Content = result,
            AccumulatedContent = result,
            TokenIndex = result.Length / 4,
            IsFinal = true,
            Metadata = new LlmStreamMetadata
            {
                TotalTokens = result.Length / 4,
                EstimatedCost = null,
                IsLocalModel = true,
                ModelName = "mock",
                FinishReason = "stop"
            }
        };
    }
}

internal sealed class MockRetryableLlmProvider : ILlmProvider
{
    private int _callCount;
    private readonly int _failCount;
    private readonly string _successResponse;
    
    public MockRetryableLlmProvider(int failCount, string successResponse)
    {
        _failCount = failCount;
        _successResponse = successResponse;
    }
    
    public Task<string> CompleteAsync(string prompt, CancellationToken ct)
    {
        _callCount++;
        if (_callCount <= _failCount)
        {
            throw new InvalidOperationException($"Transient failure {_callCount}");
        }
        return Task.FromResult(_successResponse);
    }
    
    public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        return CompleteAsync("draft", ct);
    }
    
    public Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<SceneAnalysisResult?>(null);
    
    public Task<VisualPromptResult?> GenerateVisualPromptAsync(string sceneText, string? previousSceneText, string videoTone, VisualStyle targetStyle, CancellationToken ct)
        => Task.FromResult<VisualPromptResult?>(null);
    
    public Task<ContentComplexityAnalysisResult?> AnalyzeContentComplexityAsync(string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<ContentComplexityAnalysisResult?>(null);
    
    public Task<SceneCoherenceResult?> AnalyzeSceneCoherenceAsync(string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<SceneCoherenceResult?>(null);
    
    public Task<NarrativeArcResult?> ValidateNarrativeArcAsync(System.Collections.Generic.IReadOnlyList<string> sceneTexts, string videoGoal, string videoType, CancellationToken ct)
        => Task.FromResult<NarrativeArcResult?>(null);
    
    public Task<string?> GenerateTransitionTextAsync(string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct)
        => Task.FromResult<string?>(null);
    
    public bool SupportsStreaming => true;
    
    public LlmProviderCharacteristics GetCharacteristics()
    {
        return new LlmProviderCharacteristics
        {
            IsLocal = true,
            ExpectedFirstTokenMs = 0,
            ExpectedTokensPerSec = 100,
            SupportsStreaming = true,
            ProviderTier = "Test",
            CostPer1KTokens = null
        };
    }
    
    public async IAsyncEnumerable<LlmStreamChunk> DraftScriptStreamAsync(
        Brief brief,
        PlanSpec spec,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? errorMessage = null;
        string result = string.Empty;
        
        try
        {
            result = await DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);
        }
        catch (InvalidOperationException ex)
        {
            errorMessage = ex.Message;
        }
        
        yield return new LlmStreamChunk
        {
            ProviderName = "MockRetryable",
            Content = errorMessage == null ? result : string.Empty,
            AccumulatedContent = errorMessage == null ? result : string.Empty,
            TokenIndex = errorMessage == null ? result.Length / 4 : 0,
            IsFinal = true,
            ErrorMessage = errorMessage,
            Metadata = errorMessage == null ? new LlmStreamMetadata
            {
                TotalTokens = result.Length / 4,
                EstimatedCost = null,
                IsLocalModel = true,
                ModelName = "mock",
                FinishReason = "stop"
            } : null
        };
    }
}

internal sealed class MockFailingLlmProvider : ILlmProvider
{
    private readonly string _errorMessage;
    
    public MockFailingLlmProvider(string errorMessage)
    {
        _errorMessage = errorMessage;
    }
    
    public Task<string> CompleteAsync(string prompt, CancellationToken ct)
    {
        throw new InvalidOperationException(_errorMessage);
    }
    
    public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        throw new InvalidOperationException(_errorMessage);
    }
    
    public Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct)
        => throw new InvalidOperationException(_errorMessage);
    
    public Task<VisualPromptResult?> GenerateVisualPromptAsync(string sceneText, string? previousSceneText, string videoTone, VisualStyle targetStyle, CancellationToken ct)
        => throw new InvalidOperationException(_errorMessage);
    
    public Task<ContentComplexityAnalysisResult?> AnalyzeContentComplexityAsync(string sceneText, string? previousSceneText, string videoGoal, CancellationToken ct)
        => throw new InvalidOperationException(_errorMessage);
    
    public Task<SceneCoherenceResult?> AnalyzeSceneCoherenceAsync(string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct)
        => throw new InvalidOperationException(_errorMessage);
    
    public Task<NarrativeArcResult?> ValidateNarrativeArcAsync(System.Collections.Generic.IReadOnlyList<string> sceneTexts, string videoGoal, string videoType, CancellationToken ct)
        => throw new InvalidOperationException(_errorMessage);
    
    public Task<string?> GenerateTransitionTextAsync(string fromSceneText, string toSceneText, string videoGoal, CancellationToken ct)
        => throw new InvalidOperationException(_errorMessage);
    
    public bool SupportsStreaming => false;
    
    public LlmProviderCharacteristics GetCharacteristics()
    {
        return new LlmProviderCharacteristics
        {
            IsLocal = true,
            ExpectedFirstTokenMs = 0,
            ExpectedTokensPerSec = 0,
            SupportsStreaming = false,
            ProviderTier = "Test",
            CostPer1KTokens = null
        };
    }
    
    public async IAsyncEnumerable<LlmStreamChunk> DraftScriptStreamAsync(
        Brief brief,
        PlanSpec spec,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask;
        yield return new LlmStreamChunk
        {
            ProviderName = "MockFailing",
            Content = string.Empty,
            TokenIndex = 0,
            IsFinal = true,
            ErrorMessage = _errorMessage
        };
    }
}
