using System;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Validation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Generation;

/// <summary>
/// Service for generating video scripts with schema validation
/// Orchestrates LLM provider calls and validates responses against expected structure
/// </summary>
public class ScriptGenerationService
{
    private readonly ILogger<ScriptGenerationService> _logger;
    private readonly SchemaValidationService _validationService;

    public ScriptGenerationService(
        ILogger<ScriptGenerationService> logger,
        SchemaValidationService validationService)
    {
        _logger = logger;
        _validationService = validationService;
    }

    /// <summary>
    /// Generates a video script using the specified LLM provider with schema validation
    /// </summary>
    /// <param name="provider">LLM provider to use for generation</param>
    /// <param name="brief">Brief describing the video topic and requirements</param>
    /// <param name="spec">Specification for the video plan (duration, pacing, etc.)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Generated script JSON string if successful</returns>
    /// <exception cref="InvalidOperationException">Thrown if validation fails or generation encounters an error</exception>
    public async Task<string> GenerateScriptAsync(
        ILlmProvider provider,
        Brief brief,
        PlanSpec spec,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Generating script for topic '{Topic}' using provider",
            brief.Topic);

        try
        {
            // Use standard draft script method
            _logger.LogInformation("Generating script with LLM provider");
            var scriptJson = await provider.DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(scriptJson))
            {
                _logger.LogError("Script generation returned empty or null result");
                throw new InvalidOperationException("Script generation returned empty result");
            }

            _logger.LogInformation("Script generated successfully, validating schema ({Length} characters)", scriptJson.Length);

            // Validate the generated script
            var validationResult = _validationService.ValidateScriptJson(scriptJson);

            if (!validationResult.IsValid)
            {
                _logger.LogError(
                    "Script validation failed with {ErrorCount} errors: {Errors}",
                    validationResult.Errors.Count,
                    string.Join("; ", validationResult.Errors));

                throw new InvalidOperationException(
                    $"Generated script failed validation: {string.Join(", ", validationResult.Errors)}");
            }

            _logger.LogInformation("Script validation passed successfully");
            return scriptJson;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error during script generation");
            throw new InvalidOperationException("Script generation failed due to an unexpected error", ex);
        }
    }
}
