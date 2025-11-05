using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Cache;
using Aura.Core.AI.Validation;
using Aura.Core.Providers;
using Aura.Core.Services.CostTracking;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Orchestration;

/// <summary>
/// Request for an LLM operation through the unified orchestrator
/// </summary>
public record LlmOperationRequest
{
    /// <summary>
    /// Session/job identifier for budget tracking
    /// </summary>
    public string SessionId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Type of operation to perform
    /// </summary>
    public LlmOperationType OperationType { get; init; }
    
    /// <summary>
    /// The prompt to send to the LLM
    /// </summary>
    public string Prompt { get; init; } = string.Empty;
    
    /// <summary>
    /// System prompt (optional)
    /// </summary>
    public string? SystemPrompt { get; init; }
    
    /// <summary>
    /// Custom operation preset overrides
    /// </summary>
    public LlmOperationPreset? CustomPreset { get; init; }
    
    /// <summary>
    /// Budget constraints for this operation
    /// </summary>
    public LlmBudgetConstraint? BudgetConstraint { get; init; }
    
    /// <summary>
    /// Provider chain (ordered list of providers to try)
    /// </summary>
    public string[]? ProviderChain { get; init; }
    
    /// <summary>
    /// Whether to enable caching for this operation
    /// </summary>
    public bool EnableCache { get; init; } = true;
    
    /// <summary>
    /// Cache TTL override (seconds)
    /// </summary>
    public int? CacheTtlSeconds { get; init; }
}

/// <summary>
/// Response from an LLM operation
/// </summary>
public record LlmOperationResponse
{
    public bool Success { get; init; }
    public string Content { get; init; } = string.Empty;
    public LlmOperationTelemetry Telemetry { get; init; } = null!;
    public string? ErrorMessage { get; init; }
    public bool WasCached { get; init; }
}

/// <summary>
/// Unified orchestrator for all LLM operations with prompt governance, cost controls, and telemetry
/// </summary>
public class UnifiedLlmOrchestrator
{
    private readonly ILogger<UnifiedLlmOrchestrator> _logger;
    private readonly ILlmCache _cache;
    private readonly LlmBudgetManager _budgetManager;
    private readonly LlmTelemetryCollector _telemetryCollector;
    private readonly EnhancedCostTrackingService? _costTrackingService;
    private readonly TokenTrackingService? _tokenTrackingService;
    private readonly SchemaValidator _schemaValidator;
    
    public UnifiedLlmOrchestrator(
        ILogger<UnifiedLlmOrchestrator> logger,
        ILlmCache cache,
        LlmBudgetManager budgetManager,
        LlmTelemetryCollector telemetryCollector,
        SchemaValidator schemaValidator,
        EnhancedCostTrackingService? costTrackingService = null,
        TokenTrackingService? tokenTrackingService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _budgetManager = budgetManager ?? throw new ArgumentNullException(nameof(budgetManager));
        _telemetryCollector = telemetryCollector ?? throw new ArgumentNullException(nameof(telemetryCollector));
        _schemaValidator = schemaValidator ?? throw new ArgumentNullException(nameof(schemaValidator));
        _costTrackingService = costTrackingService;
        _tokenTrackingService = tokenTrackingService;
    }
    
    /// <summary>
    /// Executes an LLM operation with full orchestration
    /// </summary>
    public async Task<LlmOperationResponse> ExecuteAsync(
        LlmOperationRequest request,
        ILlmProvider provider,
        CancellationToken ct = default)
    {
        var preset = request.CustomPreset ?? LlmOperationPresets.GetPreset(request.OperationType);
        var providerName = GetProviderName(provider);
        var modelName = "default";
        
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        var operationId = Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "Starting LLM operation {OperationId}: {OperationType} via {Provider} (session: {SessionId})",
            operationId, request.OperationType, providerName, request.SessionId);
        
        var estimatedTokens = EstimateTokens(request.Prompt, request.SystemPrompt);
        var estimatedCost = EstimateCost(providerName, estimatedTokens, preset.MaxTokens);
        
        var budgetCheck = _budgetManager.CheckBudget(
            request.SessionId,
            estimatedTokens,
            estimatedCost,
            request.BudgetConstraint);
        
        if (!budgetCheck.IsWithinBudget && (request.BudgetConstraint?.EnforceHardLimits ?? false))
        {
            return CreateFailureResponse(
                operationId,
                request.SessionId,
                request.OperationType,
                providerName,
                modelName,
                preset,
                startTime,
                stopwatch.ElapsedMilliseconds,
                $"Budget exceeded: {string.Join("; ", budgetCheck.Warnings)}",
                0,
                0,
                0);
        }
        
        if (request.EnableCache)
        {
            var cacheKey = LlmCacheKeyGenerator.GenerateKey(
                providerName,
                modelName,
                request.OperationType.ToString(),
                request.SystemPrompt,
                request.Prompt,
                preset.Temperature,
                preset.MaxTokens);
            
            var cachedEntry = await _cache.GetAsync(cacheKey, ct);
            if (cachedEntry != null)
            {
                _logger.LogInformation(
                    "Cache hit for operation {OperationId}: {OperationType}",
                    operationId, request.OperationType);
                
                var telemetry = CreateTelemetry(
                    operationId,
                    request.SessionId,
                    request.OperationType,
                    providerName,
                    modelName,
                    preset,
                    startTime,
                    stopwatch.ElapsedMilliseconds,
                    estimatedTokens,
                    cachedEntry.Response.Length / 4,
                    0,
                    true,
                    true,
                    estimatedCost);
                
                _telemetryCollector.Record(telemetry);
                RecordTokenMetrics(telemetry);
                
                return new LlmOperationResponse
                {
                    Success = true,
                    Content = cachedEntry.Response,
                    Telemetry = telemetry,
                    WasCached = true
                };
            }
        }
        
        var retryCount = 0;
        string? lastError = null;
        
        while (retryCount < preset.MaxRetries)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(preset.TimeoutSeconds));
                
                var response = await provider.CompleteAsync(request.Prompt, cts.Token);
                
                if (string.IsNullOrWhiteSpace(response))
                {
                    throw new InvalidOperationException("LLM returned empty response");
                }
                
                stopwatch.Stop();
                
                var tokensIn = estimatedTokens;
                var tokensOut = response.Length / 4;
                var actualCost = EstimateCost(providerName, tokensIn, tokensOut);
                
                _budgetManager.RecordUsage(request.SessionId, tokensIn + tokensOut, actualCost);
                
                if (request.EnableCache)
                {
                    var cacheKey = LlmCacheKeyGenerator.GenerateKey(
                        providerName,
                        modelName,
                        request.OperationType.ToString(),
                        request.SystemPrompt,
                        request.Prompt,
                        preset.Temperature,
                        preset.MaxTokens);
                    
                    var cacheMetadata = new CacheMetadata
                    {
                        ProviderName = providerName,
                        ModelName = modelName,
                        OperationType = request.OperationType.ToString(),
                        TtlSeconds = request.CacheTtlSeconds ?? 3600,
                        ResponseSizeBytes = response.Length
                    };
                    
                    await _cache.SetAsync(cacheKey, response, cacheMetadata, ct);
                }
                
                var telemetry = CreateTelemetry(
                    operationId,
                    request.SessionId,
                    request.OperationType,
                    providerName,
                    modelName,
                    preset,
                    startTime,
                    stopwatch.ElapsedMilliseconds,
                    tokensIn,
                    tokensOut,
                    retryCount,
                    false,
                    true,
                    actualCost);
                
                _telemetryCollector.Record(telemetry);
                RecordTokenMetrics(telemetry);
                
                LogCostTracking(request.SessionId, providerName, request.OperationType.ToString(), actualCost, tokensIn + tokensOut);
                
                _logger.LogInformation(
                    "LLM operation {OperationId} completed successfully: {OperationType} via {Provider} " +
                    "({TokensIn} in, {TokensOut} out, ${Cost:F4}, {Latency}ms, {Retries} retries)",
                    operationId, request.OperationType, providerName, tokensIn, tokensOut, actualCost,
                    stopwatch.ElapsedMilliseconds, retryCount);
                
                return new LlmOperationResponse
                {
                    Success = true,
                    Content = response,
                    Telemetry = telemetry,
                    WasCached = false
                };
            }
            catch (OperationCanceledException)
            {
                lastError = "Operation timed out";
                _logger.LogWarning(
                    "LLM operation {OperationId} timed out (attempt {Attempt}/{MaxRetries}): {OperationType}",
                    operationId, retryCount + 1, preset.MaxRetries, request.OperationType);
                retryCount++;
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogError(ex,
                    "LLM operation {OperationId} failed (attempt {Attempt}/{MaxRetries}): {OperationType}",
                    operationId, retryCount + 1, preset.MaxRetries, request.OperationType);
                retryCount++;
            }
            
            if (retryCount < preset.MaxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)), ct);
            }
        }
        
        stopwatch.Stop();
        
        return CreateFailureResponse(
            operationId,
            request.SessionId,
            request.OperationType,
            providerName,
            modelName,
            preset,
            startTime,
            stopwatch.ElapsedMilliseconds,
            lastError ?? "Unknown error",
            estimatedTokens,
            0,
            retryCount);
    }
    
    /// <summary>
    /// Gets telemetry statistics for a session
    /// </summary>
    public LlmTelemetryStatistics GetSessionStatistics(string sessionId)
    {
        return _telemetryCollector.GetSessionStatistics(sessionId);
    }
    
    /// <summary>
    /// Gets overall telemetry statistics
    /// </summary>
    public LlmTelemetryStatistics GetStatistics()
    {
        return _telemetryCollector.GetStatistics();
    }
    
    /// <summary>
    /// Gets budget status for a session
    /// </summary>
    public SessionBudget GetSessionBudget(string sessionId)
    {
        return _budgetManager.GetSessionBudget(sessionId);
    }
    
    /// <summary>
    /// Clears session data (budget and telemetry)
    /// </summary>
    public void ClearSession(string sessionId)
    {
        _budgetManager.ClearSession(sessionId);
        _telemetryCollector.ClearSession(sessionId);
    }
    
    private static string GetProviderName(ILlmProvider provider)
    {
        var typeName = provider.GetType().Name;
        return typeName.Replace("LlmProvider", "").Replace("Provider", "");
    }
    
    private static int EstimateTokens(string prompt, string? systemPrompt)
    {
        var totalLength = prompt.Length + (systemPrompt?.Length ?? 0);
        return (int)(totalLength / 4.0 * 1.2);
    }
    
    private static decimal EstimateCost(string providerName, int tokensIn, int tokensOut)
    {
        var ratePerMillionIn = providerName.ToLowerInvariant() switch
        {
            "openai" => 2.50m,
            "anthropic" => 3.00m,
            "gemini" => 0.50m,
            "ollama" => 0.00m,
            "rulebased" => 0.00m,
            _ => 1.00m
        };
        
        var ratePerMillionOut = ratePerMillionIn * 3;
        
        var costIn = (tokensIn / 1_000_000.0m) * ratePerMillionIn;
        var costOut = (tokensOut / 1_000_000.0m) * ratePerMillionOut;
        
        return costIn + costOut;
    }
    
    private static LlmOperationTelemetry CreateTelemetry(
        string operationId,
        string sessionId,
        LlmOperationType operationType,
        string providerName,
        string modelName,
        LlmOperationPreset preset,
        DateTime startedAt,
        long latencyMs,
        int tokensIn,
        int tokensOut,
        int retryCount,
        bool cacheHit,
        bool success,
        decimal cost)
    {
        return new LlmOperationTelemetry
        {
            OperationId = operationId,
            SessionId = sessionId,
            OperationType = operationType,
            ProviderName = providerName,
            ModelName = modelName,
            TokensIn = tokensIn,
            TokensOut = tokensOut,
            RetryCount = retryCount,
            LatencyMs = latencyMs,
            Success = success,
            CacheHit = cacheHit,
            EstimatedCost = cost,
            StartedAt = startedAt,
            CompletedAt = DateTime.UtcNow,
            Temperature = preset.Temperature,
            TopP = preset.TopP
        };
    }
    
    private LlmOperationResponse CreateFailureResponse(
        string operationId,
        string sessionId,
        LlmOperationType operationType,
        string providerName,
        string modelName,
        LlmOperationPreset preset,
        DateTime startedAt,
        long latencyMs,
        string errorMessage,
        int tokensIn,
        int tokensOut,
        int retryCount)
    {
        var telemetry = CreateTelemetry(
            operationId,
            sessionId,
            operationType,
            providerName,
            modelName,
            preset,
            startedAt,
            latencyMs,
            tokensIn,
            tokensOut,
            retryCount,
            false,
            false,
            0);
        
        telemetry = telemetry with { ErrorMessage = errorMessage };
        
        _telemetryCollector.Record(telemetry);
        RecordTokenMetrics(telemetry);
        
        return new LlmOperationResponse
        {
            Success = false,
            Content = string.Empty,
            Telemetry = telemetry,
            ErrorMessage = errorMessage,
            WasCached = false
        };
    }
    
    private void LogCostTracking(string sessionId, string providerName, string feature, decimal cost, int tokens)
    {
        if (_costTrackingService == null)
            return;
        
        try
        {
            var featureType = ParseFeatureType(feature);
            var costLog = new Models.CostTracking.CostLog
            {
                Timestamp = DateTime.UtcNow,
                ProviderName = providerName,
                Feature = featureType,
                Cost = cost,
                TokensUsed = tokens,
                RequestDetails = $"SessionId: {sessionId}"
            };
            
            _costTrackingService.LogCost(costLog);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log cost tracking");
        }
    }
    
    private void RecordTokenMetrics(LlmOperationTelemetry telemetry, string projectId = "")
    {
        if (_tokenTrackingService == null)
            return;
        
        try
        {
            var tokenMetrics = new Models.CostTracking.TokenUsageMetrics
            {
                Timestamp = telemetry.StartedAt,
                ProviderName = telemetry.ProviderName,
                ModelName = telemetry.ModelName,
                OperationType = telemetry.OperationType.ToString(),
                InputTokens = telemetry.TokensIn,
                OutputTokens = telemetry.TokensOut,
                ResponseTimeMs = telemetry.LatencyMs,
                RetryCount = telemetry.RetryCount,
                CacheHit = telemetry.CacheHit,
                EstimatedCost = telemetry.EstimatedCost,
                JobId = telemetry.SessionId,
                ProjectId = string.IsNullOrEmpty(projectId) ? null : projectId,
                Success = telemetry.Success,
                ErrorMessage = telemetry.ErrorMessage
            };
            
            _tokenTrackingService.RecordTokenUsage(tokenMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record token metrics");
        }
    }
    
    private static Models.CostTracking.CostFeatureType ParseFeatureType(string feature)
    {
        return feature switch
        {
            "Planning" => Models.CostTracking.CostFeatureType.ScriptGeneration,
            "Scripting" => Models.CostTracking.CostFeatureType.ScriptGeneration,
            "ScriptRefinement" => Models.CostTracking.CostFeatureType.ScriptRefinement,
            "VisualPrompts" => Models.CostTracking.CostFeatureType.VisualPrompts,
            "SceneAnalysis" => Models.CostTracking.CostFeatureType.SceneAnalysis,
            "ComplexityAnalysis" => Models.CostTracking.CostFeatureType.SceneAnalysis,
            "CoherenceValidation" => Models.CostTracking.CostFeatureType.SceneAnalysis,
            "NarrativeValidation" => Models.CostTracking.CostFeatureType.SceneAnalysis,
            _ => Models.CostTracking.CostFeatureType.Other
        };
    }
}
