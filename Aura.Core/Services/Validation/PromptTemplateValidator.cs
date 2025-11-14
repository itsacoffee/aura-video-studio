using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.AI;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Validation;

/// <summary>
/// Validates that EnhancedPromptTemplates generates valid prompts
/// </summary>
public class PromptTemplateValidator
{
    private readonly ILogger<PromptTemplateValidator> _logger;

    public PromptTemplateValidator(ILogger<PromptTemplateValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates prompt template generation
    /// </summary>
    public async Task<ValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting prompt template validation");

        var result = new ValidationResult
        {
            IsValid = true,
            ValidationName = "Prompt Template Validation"
        };

        // Test script generation prompt
        await ValidateScriptGenerationPromptAsync(result).ConfigureAwait(false);

        // Test visual selection prompt
        await ValidateVisualSelectionPromptAsync(result).ConfigureAwait(false);

        // Test quality validation prompt
        await ValidateQualityValidationPromptAsync(result).ConfigureAwait(false);

        result.CompletedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Prompt template validation complete: {IsValid} ({SuccessCount}/{TotalCount} checks passed)",
            result.IsValid,
            result.SuccessCount,
            result.TotalChecks);

        return result;
    }

    private async Task ValidateScriptGenerationPromptAsync(ValidationResult result)
    {
        result.TotalChecks++;

        try
        {
            var brief = new Brief(
                Topic: "Test Video Topic",
                Audience: "General Audience",
                Goal: "Educational",
                Tone: "informative",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var spec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(2),
                Pacing: Models.Pacing.Conversational,
                Density: Models.Density.Balanced,
                Style: "educational"
            );

            var prompt = EnhancedPromptTemplates.BuildScriptGenerationPrompt(brief, spec);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                result.Errors.Add("Script generation prompt is empty");
                result.IsValid = false;
            }
            else if (!prompt.Contains("Test Video Topic"))
            {
                result.Errors.Add("Script generation prompt doesn't include topic");
                result.IsValid = false;
            }
            else if (!prompt.Contains("Duration"))
            {
                result.Errors.Add("Script generation prompt doesn't include duration");
                result.IsValid = false;
            }
            else
            {
                result.SuccessCount++;
                result.Details.Add($"Script generation prompt is valid ({prompt.Length} characters)");
                _logger.LogDebug("Script generation prompt validation passed");
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error validating script generation prompt: {ex.Message}");
            result.IsValid = false;
            _logger.LogError(ex, "Error validating script generation prompt");
        }
    }

    private async Task ValidateVisualSelectionPromptAsync(ValidationResult result)
    {
        result.TotalChecks++;

        try
        {
            var prompt = EnhancedPromptTemplates.BuildVisualSelectionPrompt(
                "Test Scene",
                "Test scene content describing visuals",
                "dramatic",
                0);

            if (string.IsNullOrWhiteSpace(prompt))
            {
                result.Errors.Add("Visual selection prompt is empty");
                result.IsValid = false;
            }
            else if (!prompt.Contains("Test Scene"))
            {
                result.Errors.Add("Visual selection prompt doesn't include scene heading");
                result.IsValid = false;
            }
            else
            {
                result.SuccessCount++;
                result.Details.Add($"Visual selection prompt is valid ({prompt.Length} characters)");
                _logger.LogDebug("Visual selection prompt validation passed");
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error validating visual selection prompt: {ex.Message}");
            result.IsValid = false;
            _logger.LogError(ex, "Error validating visual selection prompt");
        }
    }

    private async Task ValidateQualityValidationPromptAsync(ValidationResult result)
    {
        result.TotalChecks++;

        try
        {
            var testScript = "This is a test video script to validate quality analysis.";
            var prompt = EnhancedPromptTemplates.BuildQualityValidationPrompt(testScript, "educational");

            if (string.IsNullOrWhiteSpace(prompt))
            {
                result.Errors.Add("Quality validation prompt is empty");
                result.IsValid = false;
            }
            else if (!prompt.Contains("EDUCATIONAL"))
            {
                result.Errors.Add("Quality validation prompt doesn't include content type");
                result.IsValid = false;
            }
            else if (!prompt.Contains("quality"))
            {
                result.Errors.Add("Quality validation prompt doesn't mention quality");
                result.IsValid = false;
            }
            else
            {
                result.SuccessCount++;
                result.Details.Add($"Quality validation prompt is valid ({prompt.Length} characters)");
                _logger.LogDebug("Quality validation prompt validation passed");
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Error validating quality validation prompt: {ex.Message}");
            result.IsValid = false;
            _logger.LogError(ex, "Error validating quality validation prompt");
        }
    }
}
