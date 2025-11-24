using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI.Templates;
using Aura.Core.AI.Validation;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

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

    private const int MaxRetries = 3;

    public ScriptGenerationPipeline(
        ILlmProvider llmProvider,
        ScriptSchemaValidator validator,
        FallbackScriptGenerator fallbackGenerator,
        ILogger<ScriptGenerationPipeline> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _fallbackGenerator = fallbackGenerator ?? throw new ArgumentNullException(nameof(fallbackGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a script with validation and retry logic
    /// </summary>
    public async Task<ScriptGenerationResult> GenerateAsync(
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
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

                // Build prompt with feedback from previous attempts
                var prompt = BuildPrompt(brief, spec, attempts);

                // Generate script from LLM
                var script = await _llmProvider.DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);

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
    /// Builds prompt with feedback from previous attempts
    /// </summary>
    private string BuildPrompt(Brief brief, PlanSpec spec, List<GenerationAttempt> previousAttempts)
    {
        var basePrompt = EnhancedPromptTemplates.GetSystemPromptForScriptGeneration();

        if (previousAttempts.Count > 0)
        {
            var lastAttempt = previousAttempts.Last();
            if (lastAttempt.Validation != null && lastAttempt.Validation.Errors.Count > 0)
            {
                var feedbackPrompt = "\n\nPREVIOUS ATTEMPT FEEDBACK:\n";
                feedbackPrompt += "The previous attempt had the following issues:\n";
                
                foreach (var error in lastAttempt.Validation.Errors)
                {
                    feedbackPrompt += $"  - {error}\n";
                }

                feedbackPrompt += "\nPlease address these issues in your response:\n";
                feedbackPrompt += "- Ensure the script has a clear title starting with '# '\n";
                feedbackPrompt += "- Include at least 2-3 scenes marked with '## Scene Name'\n";
                feedbackPrompt += "- Reference the topic: " + brief.Topic + "\n";
                feedbackPrompt += "- Avoid placeholder text, AI refusal language, or excessive repetition\n";
                feedbackPrompt += "- Match the target duration with appropriate word count\n";

                return basePrompt + feedbackPrompt;
            }
        }

        return basePrompt;
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

