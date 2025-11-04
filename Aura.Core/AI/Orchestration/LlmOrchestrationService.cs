using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Validation;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Orchestration;

/// <summary>
/// Result of an orchestration step
/// </summary>
/// <typeparam name="T">The schema type returned</typeparam>
public record OrchestrationStepResult<T> where T : OrchestrationSchema
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public List<string> ValidationErrors { get; init; } = new();
    public int AttemptsUsed { get; init; }
    public TimeSpan Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Configuration for orchestration behavior
/// </summary>
public record OrchestrationConfig(
    int MaxRetries = 3,
    double Temperature = 0.7,
    bool EnableAutoRepair = true,
    TimeSpan Timeout = default
)
{
    public TimeSpan Timeout { get; init; } = Timeout == default ? TimeSpan.FromMinutes(5) : Timeout;
}

/// <summary>
/// Central orchestration service for LLM interactions with schema validation and auto-repair
/// </summary>
public class LlmOrchestrationService
{
    private readonly ILogger<LlmOrchestrationService> _logger;
    private readonly SchemaValidator _validator;
    private readonly Dictionary<string, OrchestrationStep> _stepRegistry;

    public LlmOrchestrationService(
        ILogger<LlmOrchestrationService> logger,
        SchemaValidator validator)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _stepRegistry = new Dictionary<string, OrchestrationStep>();
        
        RegisterDefaultSteps();
    }

    /// <summary>
    /// Registers default orchestration steps
    /// </summary>
    private void RegisterDefaultSteps()
    {
        RegisterStep("brief_to_plan", "Brief", "Plan");
        RegisterStep("plan_to_scenes", "Plan", "SceneBreakdown");
        RegisterStep("scenes_to_voice", "SceneBreakdown", "VoiceStyle");
        RegisterStep("scenes_to_visuals", "SceneBreakdown", "VisualPromptSpec");
        RegisterStep("voice_to_ssml", "VoiceStyle", "SSMLSpec");
        RegisterStep("assets_to_timeline", "Multiple", "RenderTimeline");
    }

    /// <summary>
    /// Registers an orchestration step
    /// </summary>
    public void RegisterStep(string stepId, string inputSchema, string outputSchema)
    {
        _stepRegistry[stepId] = new OrchestrationStep(stepId, inputSchema, outputSchema);
        _logger.LogDebug("Registered orchestration step: {StepId} ({InputSchema} -> {OutputSchema})", 
            stepId, inputSchema, outputSchema);
    }

    /// <summary>
    /// Executes an orchestration step with validation and auto-repair
    /// </summary>
    /// <typeparam name="T">Output schema type</typeparam>
    /// <param name="stepId">Step identifier</param>
    /// <param name="llmInvoker">Function to invoke the LLM</param>
    /// <param name="config">Orchestration configuration</param>
    /// <param name="provider">Provider name for metadata</param>
    /// <param name="model">Model name for metadata</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Orchestration result</returns>
    public async Task<OrchestrationStepResult<T>> ExecuteStepAsync<T>(
        string stepId,
        Func<string?, CancellationToken, Task<string>> llmInvoker,
        OrchestrationConfig? config = null,
        string? provider = null,
        string? model = null,
        CancellationToken ct = default) where T : OrchestrationSchema, new()
    {
        config ??= new OrchestrationConfig();
        var startTime = DateTime.UtcNow;
        var schema = new T();
        
        if (!_stepRegistry.TryGetValue(stepId, out var step))
        {
            _logger.LogWarning("Step {StepId} not found in registry", stepId);
        }

        _logger.LogInformation("Executing orchestration step: {StepId} -> {SchemaName}", 
            stepId, schema.SchemaName);

        string? repairPrompt = null;
        var attempts = 0;

        while (attempts < config.MaxRetries)
        {
            attempts++;
            
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(config.Timeout);

                var llmOutput = await llmInvoker(repairPrompt, cts.Token);
                
                if (string.IsNullOrWhiteSpace(llmOutput))
                {
                    _logger.LogWarning("LLM returned empty output for step {StepId}, attempt {Attempt}", 
                        stepId, attempts);
                    
                    if (!config.EnableAutoRepair || attempts >= config.MaxRetries)
                    {
                        return CreateFailureResult<T>(
                            "LLM returned empty output",
                            attempts,
                            DateTime.UtcNow - startTime);
                    }
                    
                    repairPrompt = "Please provide a valid JSON response according to the schema.";
                    continue;
                }

                var (validationResult, data) = _validator.ValidateAndDeserialize<T>(llmOutput);
                
                if (validationResult.IsValid && data != null)
                {
                    var enhancedData = data with
                    {
                        Metadata = new ArtifactMetadata(
                            provider ?? "unknown",
                            model ?? "unknown",
                            config.Temperature,
                            DateTime.UtcNow
                        )
                    };

                    _logger.LogInformation(
                        "Step {StepId} completed successfully on attempt {Attempt} in {Duration}ms",
                        stepId, attempts, validationResult.ValidationDuration.TotalMilliseconds);

                    return new OrchestrationStepResult<T>
                    {
                        Success = true,
                        Data = enhancedData,
                        AttemptsUsed = attempts,
                        Duration = DateTime.UtcNow - startTime
                    };
                }

                _logger.LogWarning(
                    "Step {StepId} validation failed on attempt {Attempt}: {Errors}",
                    stepId, attempts, string.Join("; ", validationResult.ValidationErrors));

                if (!config.EnableAutoRepair || attempts >= config.MaxRetries)
                {
                    return CreateFailureResult<T>(
                        validationResult.ErrorMessage ?? "Validation failed",
                        attempts,
                        DateTime.UtcNow - startTime,
                        validationResult.ValidationErrors);
                }

                repairPrompt = _validator.GenerateRepairPrompt(
                    repairPrompt ?? string.Empty,
                    llmOutput,
                    validationResult.ValidationErrors,
                    schema.GetSchemaDefinition());
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Step {StepId} timed out on attempt {Attempt}", stepId, attempts);
                
                if (attempts >= config.MaxRetries)
                {
                    return CreateFailureResult<T>(
                        "Operation timed out",
                        attempts,
                        DateTime.UtcNow - startTime);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Step {StepId} failed on attempt {Attempt}", stepId, attempts);
                
                if (attempts >= config.MaxRetries)
                {
                    return CreateFailureResult<T>(
                        $"Unexpected error: {ex.Message}",
                        attempts,
                        DateTime.UtcNow - startTime);
                }
            }
        }

        return CreateFailureResult<T>(
            "Max retries exceeded",
            attempts,
            DateTime.UtcNow - startTime);
    }

    /// <summary>
    /// Executes a step with raw JSON output (for backward compatibility)
    /// </summary>
    public async Task<string> ExecuteStepRawAsync(
        string stepId,
        Func<string?, CancellationToken, Task<string>> llmInvoker,
        OrchestrationConfig? config = null,
        CancellationToken ct = default)
    {
        config ??= new OrchestrationConfig();
        
        _logger.LogInformation("Executing raw orchestration step: {StepId}", stepId);

        var attempts = 0;
        while (attempts < config.MaxRetries)
        {
            attempts++;
            
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(config.Timeout);

                var output = await llmInvoker(null, cts.Token);
                
                if (!string.IsNullOrWhiteSpace(output))
                {
                    _logger.LogInformation("Raw step {StepId} completed on attempt {Attempt}", 
                        stepId, attempts);
                    return output;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Raw step {StepId} timed out on attempt {Attempt}", 
                    stepId, attempts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Raw step {StepId} failed on attempt {Attempt}", 
                    stepId, attempts);
            }
        }

        throw new InvalidOperationException($"Step {stepId} failed after {attempts} attempts");
    }

    /// <summary>
    /// Gets information about a registered step
    /// </summary>
    public OrchestrationStep? GetStepInfo(string stepId)
    {
        return _stepRegistry.TryGetValue(stepId, out var step) ? step : null;
    }

    /// <summary>
    /// Lists all registered steps
    /// </summary>
    public IReadOnlyDictionary<string, OrchestrationStep> GetAllSteps()
    {
        return _stepRegistry;
    }

    private static OrchestrationStepResult<T> CreateFailureResult<T>(
        string errorMessage,
        int attempts,
        TimeSpan duration,
        List<string>? validationErrors = null) where T : OrchestrationSchema
    {
        return new OrchestrationStepResult<T>
        {
            Success = false,
            ErrorMessage = errorMessage,
            AttemptsUsed = attempts,
            Duration = duration,
            ValidationErrors = validationErrors ?? new List<string>()
        };
    }
}

/// <summary>
/// Definition of an orchestration step
/// </summary>
public record OrchestrationStep(
    string StepId,
    string InputSchema,
    string OutputSchema
);
