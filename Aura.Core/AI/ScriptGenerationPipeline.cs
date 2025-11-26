using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Agents;
using Aura.Core.AI.Templates;
using Aura.Core.AI.Validation;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aura.Core.AI;

/// <summary>
/// Pipeline for generating scripts with validation, retry logic, and fallback
/// Handles LLM response validation, intelligent retries with prompt modification, and template fallback
/// </summary>
public class ScriptGenerationPipeline
{
    private readonly ILlmProvider _llmProvider;
    private readonly ScriptSchemaValidator _validator;
    private readonly FallbackScriptGenerator _fallbackGenerator;
    private readonly ILogger<ScriptGenerationPipeline> _logger;
    private readonly AgentOrchestrator? _agentOrchestrator;
    private readonly IOptions<AgenticModeOptions>? _agenticOptions;

    private const int MaxRetries = 3;

    public ScriptGenerationPipeline(
        ILlmProvider llmProvider,
        ScriptSchemaValidator validator,
        FallbackScriptGenerator fallbackGenerator,
        ILogger<ScriptGenerationPipeline> logger,
        AgentOrchestrator? agentOrchestrator = null,
        IOptions<AgenticModeOptions>? agenticOptions = null)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _fallbackGenerator = fallbackGenerator ?? throw new ArgumentNullException(nameof(fallbackGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentOrchestrator = agentOrchestrator;
        _agenticOptions = agenticOptions ?? Options.Create(new AgenticModeOptions());
    }

    /// <summary>
    /// Generates a script with validation and retry logic
    /// Supports both single-pass and agentic multi-agent modes
    /// </summary>
    public async Task<ScriptGenerationResult> GenerateAsync(
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        return await GenerateAsync(brief, spec, useAgenticMode: false, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a script with validation and retry logic
    /// Supports both single-pass and agentic multi-agent modes
    /// </summary>
    public async Task<ScriptGenerationResult> GenerateAsync(
        Brief brief,
        PlanSpec spec,
        bool useAgenticMode,
        CancellationToken ct)
    {
        // Check if agentic mode is requested and available
        if (useAgenticMode && 
            _agentOrchestrator != null && 
            _agenticOptions?.Value.Enabled == true)
        {
            _logger.LogInformation(
                "Using agentic multi-agent script generation for topic: {Topic}",
                brief.Topic);

            try
            {
                var result = await _agentOrchestrator.GenerateAsync(brief, spec, ct).ConfigureAwait(false);
                
                _logger.LogInformation(
                    "Agentic generation completed in {Iterations} iterations, approved: {Approved}",
                    result.Iterations.Count,
                    result.ApprovedByCritic);

                // Convert AgentOrchestratorResult to ScriptGenerationResult
                var validation = _validator.Validate(result.Script.RawText, brief, spec);
                
                return new ScriptGenerationResult(
                    Success: result.ApprovedByCritic,
                    Script: result.Script.RawText,
                    Attempts: result.Iterations.Select((iter, idx) => new GenerationAttempt(
                        AttemptNumber: idx + 1,
                        Script: iter.Script.RawText,
                        Validation: validation,
                        Error: iter.CriticFeedback,
                        Duration: TimeSpan.Zero // Agent iterations don't track individual durations
                    )).ToList(),
                    UsedFallback: false,
                    QualityScore: validation.QualityScore,
                    Metrics: validation.Metrics
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agentic generation failed");

                if (_agenticOptions?.Value.FallbackToSinglePass == true)
                {
                    _logger.LogWarning("Falling back to single-pass generation");
                }
                else
                {
                    throw;
                }
            }
        }

        // Fallback to existing single-pass generation
        return await GenerateSinglePassAsync(brief, spec, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a script using single-pass generation (original implementation)
    /// </summary>
    private async Task<ScriptGenerationResult> GenerateSinglePassAsync(
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Using single-pass script generation for topic: {Topic}",
            brief.Topic);
        var attempts = new List<GenerationAttempt>();

        _logger.LogInformation(
            "Starting script generation for topic: {Topic}, duration: {Duration}s",
            brief.Topic,
            spec.TargetDuration.TotalSeconds);

        for (int i = 0; i < MaxRetries; i++)
        {
            var attemptNumber = i + 1;
            var startTime = DateTime.UtcNow;

            try
            {
                _logger.LogDebug("Attempt {Attempt}/{MaxRetries} for script generation", attemptNumber, MaxRetries);

                // Build prompt with feedback from previous attempts and apply to brief
                var modifiedBrief = BuildBriefWithFeedback(brief, attempts);

                // Generate script from LLM with modified brief containing feedback
                var script = await _llmProvider.DraftScriptAsync(modifiedBrief, spec, ct).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(script))
                {
                    _logger.LogWarning("LLM provider returned empty script on attempt {Attempt}", attemptNumber);
                    attempts.Add(new GenerationAttempt(
                        AttemptNumber: attemptNumber,
                        Script: null,
                        Validation: null,
                        Error: "LLM provider returned empty script",
                        Duration: DateTime.UtcNow - startTime
                    ));
                    continue;
                }

                // Validate the generated script
                var validation = _validator.Validate(script, brief, spec);

                attempts.Add(new GenerationAttempt(
                    AttemptNumber: attemptNumber,
                    Script: script,
                    Validation: validation,
                    Error: null,
                    Duration: DateTime.UtcNow - startTime
                ));

                if (validation.IsValid)
                {
                    _logger.LogInformation(
                        "Script generated successfully on attempt {Attempt} with quality {Quality:F2}",
                        attemptNumber,
                        validation.QualityScore);

                    return new ScriptGenerationResult(
                        Success: true,
                        Script: script,
                        Attempts: attempts,
                        UsedFallback: false,
                        QualityScore: validation.QualityScore,
                        Metrics: validation.Metrics
                    );
                }

                _logger.LogWarning(
                    "Script validation failed on attempt {Attempt}: {Errors}. Quality score: {Quality:F2}",
                    attemptNumber,
                    string.Join(", ", validation.Errors),
                    validation.QualityScore);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Script generation cancelled on attempt {Attempt}", attemptNumber);
                attempts.Add(new GenerationAttempt(
                    AttemptNumber: attemptNumber,
                    Script: null,
                    Validation: null,
                    Error: "Operation was cancelled",
                    Duration: DateTime.UtcNow - startTime
                ));
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Script generation failed on attempt {Attempt}", attemptNumber);
                attempts.Add(new GenerationAttempt(
                    AttemptNumber: attemptNumber,
                    Script: null,
                    Validation: null,
                    Error: ex.Message,
                    Duration: DateTime.UtcNow - startTime
                ));
            }
        }

        // All retries exhausted - use fallback
        _logger.LogWarning(
            "All {MaxRetries} LLM attempts failed, using template fallback",
            MaxRetries);

        var fallbackScript = _fallbackGenerator.Generate(brief, spec);

        return new ScriptGenerationResult(
            Success: false,
            Script: fallbackScript,
            Attempts: attempts,
            UsedFallback: true,
            QualityScore: 0.5, // Fallback scripts have moderate quality
            Metrics: _validator.Validate(fallbackScript, brief, spec).Metrics
        );
    }

    /// <summary>
    /// Builds a modified brief with feedback from previous attempts
    /// Passes validation feedback through PromptModifiers so LLM providers can use it
    /// </summary>
    private Brief BuildBriefWithFeedback(Brief originalBrief, List<GenerationAttempt> previousAttempts)
    {
        if (previousAttempts.Count == 0)
        {
            return originalBrief;
        }

        var lastAttempt = previousAttempts.Last();
        if (lastAttempt.Validation == null || lastAttempt.Validation.Errors.Count == 0)
        {
            return originalBrief;
        }

        // Build feedback instructions
        var feedbackInstructions = new System.Text.StringBuilder();
        feedbackInstructions.AppendLine("PREVIOUS ATTEMPT FEEDBACK:");
        feedbackInstructions.AppendLine("The previous attempt had the following issues:");
        
        foreach (var error in lastAttempt.Validation.Errors)
        {
            feedbackInstructions.AppendLine($"  - {error}");
        }

        feedbackInstructions.AppendLine();
        feedbackInstructions.AppendLine("Please address these issues in your response:");
        feedbackInstructions.AppendLine("- Ensure the script has a clear title starting with '# '");
        feedbackInstructions.AppendLine("- Include at least 2-3 scenes marked with '## Scene Name'");
        feedbackInstructions.AppendLine($"- Reference the topic: {originalBrief.Topic}");
        feedbackInstructions.AppendLine("- Avoid placeholder text, AI refusal language, or excessive repetition");
        feedbackInstructions.AppendLine("- Match the target duration with appropriate word count");

        // Merge with existing PromptModifiers if present
        var existingInstructions = originalBrief.PromptModifiers?.AdditionalInstructions;
        var combinedInstructions = string.IsNullOrWhiteSpace(existingInstructions)
            ? feedbackInstructions.ToString()
            : $"{existingInstructions}\n\n{feedbackInstructions}";

        var modifiedPromptModifiers = new PromptModifiers(
            AdditionalInstructions: combinedInstructions,
            ExampleStyle: originalBrief.PromptModifiers?.ExampleStyle,
            EnableChainOfThought: originalBrief.PromptModifiers?.EnableChainOfThought ?? false,
            PromptVersion: originalBrief.PromptModifiers?.PromptVersion
        );

        _logger.LogDebug(
            "Applying feedback from attempt {Attempt} with {ErrorCount} errors",
            lastAttempt.AttemptNumber,
            lastAttempt.Validation.Errors.Count);

        // Return modified brief with feedback in PromptModifiers
        return originalBrief with { PromptModifiers = modifiedPromptModifiers };
    }
}

/// <summary>
/// Result of script generation with detailed attempt history
/// </summary>
public record ScriptGenerationResult(
    bool Success,
    string Script,
    List<GenerationAttempt> Attempts,
    bool UsedFallback,
    double QualityScore,
    ScriptSchemaValidator.ScriptMetrics Metrics
);

/// <summary>
/// Represents a single generation attempt
/// </summary>
public record GenerationAttempt(
    int AttemptNumber,
    string? Script,
    ScriptSchemaValidator.ValidationResult? Validation,
    string? Error,
    TimeSpan Duration
);

