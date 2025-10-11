using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Validates that all selected providers/engines are available before starting generation
/// </summary>
public class GenerationValidator
{
    private readonly ILogger<GenerationValidator> _logger;

    public GenerationValidator(ILogger<GenerationValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates that all required providers are available for generation
    /// </summary>
    /// <returns>Validation result with details about any missing providers</returns>
    public ValidationResult ValidateProviders(
        Dictionary<string, ILlmProvider>? llmProviders,
        Dictionary<string, ITtsProvider>? ttsProviders,
        Dictionary<string, object>? visualProviders,
        string llmTier,
        string ttsTier,
        string visualTier,
        bool offlineOnly)
    {
        var issues = new List<string>();
        var warnings = new List<string>();

        // Validate LLM providers
        if (llmProviders == null || llmProviders.Count == 0)
        {
            issues.Add("No LLM providers registered");
        }
        else
        {
            var hasRuleBased = llmProviders.ContainsKey("RuleBased");
            if (!hasRuleBased)
            {
                warnings.Add("RuleBased provider not registered - fallback may not work");
            }

            // Check if requested tier is available
            if (llmTier == "Pro" && offlineOnly)
            {
                issues.Add("Pro LLM tier requested but offline mode is enabled");
            }
            else if (llmTier == "Pro" && !HasProLlmProvider(llmProviders))
            {
                issues.Add("Pro LLM tier requested but no Pro providers (OpenAI, Azure, Gemini) are registered");
            }
        }

        // Validate TTS providers
        if (ttsProviders == null || ttsProviders.Count == 0)
        {
            warnings.Add("No TTS providers registered");
        }
        else
        {
            if (ttsTier == "Pro" && offlineOnly)
            {
                issues.Add("Pro TTS tier requested but offline mode is enabled");
            }
            else if (ttsTier == "Pro" && !HasProTtsProvider(ttsProviders))
            {
                warnings.Add("Pro TTS tier requested but no Pro providers (ElevenLabs, PlayHT) are registered");
            }
        }

        // Validate Visual providers
        if (visualProviders == null || visualProviders.Count == 0)
        {
            warnings.Add("No Visual providers registered");
        }
        else
        {
            var hasStock = visualProviders.ContainsKey("Stock");
            if (!hasStock)
            {
                warnings.Add("Stock provider not registered - fallback may not work");
            }

            if ((visualTier == "Pro" || visualTier == "CloudPro") && offlineOnly)
            {
                issues.Add("Pro Visual tier requested but offline mode is enabled");
            }
        }

        // Log results
        if (issues.Count > 0)
        {
            _logger.LogError("Generation validation failed with {Count} issues", issues.Count);
            foreach (var issue in issues)
            {
                _logger.LogError("  - {Issue}", issue);
            }
        }

        if (warnings.Count > 0)
        {
            _logger.LogWarning("Generation validation has {Count} warnings", warnings.Count);
            foreach (var warning in warnings)
            {
                _logger.LogWarning("  - {Warning}", warning);
            }
        }

        if (issues.Count == 0 && warnings.Count == 0)
        {
            _logger.LogInformation("Generation validation passed - all providers available");
        }

        return new ValidationResult
        {
            IsValid = issues.Count == 0,
            Issues = issues.ToArray(),
            Warnings = warnings.ToArray()
        };
    }

    private static bool HasProLlmProvider(Dictionary<string, ILlmProvider> providers)
    {
        return providers.ContainsKey("OpenAI") ||
               providers.ContainsKey("Azure") ||
               providers.ContainsKey("Gemini");
    }

    private static bool HasProTtsProvider(Dictionary<string, ITtsProvider> providers)
    {
        return providers.ContainsKey("ElevenLabs") ||
               providers.ContainsKey("PlayHT");
    }
}

/// <summary>
/// Result of generation validation
/// </summary>
public record ValidationResult
{
    /// <summary>Whether validation passed (no blocking issues)</summary>
    public bool IsValid { get; init; }

    /// <summary>Blocking issues that prevent generation</summary>
    public string[] Issues { get; init; } = Array.Empty<string>();

    /// <summary>Non-blocking warnings about potential issues</summary>
    public string[] Warnings { get; init; } = Array.Empty<string>();

    /// <summary>Whether there are any warnings</summary>
    public bool HasWarnings => Warnings.Length > 0;
}
