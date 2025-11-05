using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ContentSafety;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ContentSafety;

/// <summary>
/// Service for integrating content safety checks into LLM operations
/// Provides prompt validation, modification, and safety explanations
/// </summary>
public class LlmSafetyIntegrationService
{
    private readonly ILogger<LlmSafetyIntegrationService> _logger;
    private readonly ContentSafetyService _safetyService;

    public LlmSafetyIntegrationService(
        ILogger<LlmSafetyIntegrationService> logger,
        ContentSafetyService safetyService)
    {
        _logger = logger;
        _safetyService = safetyService;
    }

    /// <summary>
    /// Validates a prompt before sending to LLM
    /// </summary>
    public async Task<PromptValidationResult> ValidatePromptAsync(
        string prompt,
        SafetyPolicy policy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating LLM prompt with policy {PolicyName}", policy.Name);

        try
        {
            var analysisResult = await _safetyService.AnalyzeContentAsync(
                Guid.NewGuid().ToString(),
                prompt,
                policy,
                ct);

            var result = new PromptValidationResult
            {
                OriginalPrompt = prompt,
                IsValid = analysisResult.IsSafe,
                AnalysisResult = analysisResult,
                CanProceed = DetermineCanProceed(analysisResult, policy)
            };

            if (!result.IsValid)
            {
                result.ModifiedPrompt = await GenerateModifiedPromptAsync(prompt, analysisResult, ct);
                result.Explanation = GenerateSafetyExplanation(analysisResult, policy);
                result.Alternatives = GenerateAlternatives(prompt, analysisResult);
            }

            _logger.LogInformation(
                "Prompt validation complete. Valid: {IsValid}, CanProceed: {CanProceed}",
                result.IsValid,
                result.CanProceed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating prompt");
            throw;
        }
    }

    /// <summary>
    /// Validates LLM response for safety before using in pipeline
    /// </summary>
    public async Task<ResponseValidationResult> ValidateResponseAsync(
        string response,
        SafetyPolicy policy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating LLM response with policy {PolicyName}", policy.Name);

        try
        {
            var analysisResult = await _safetyService.AnalyzeContentAsync(
                Guid.NewGuid().ToString(),
                response,
                policy,
                ct);

            var result = new ResponseValidationResult
            {
                OriginalResponse = response,
                IsValid = analysisResult.IsSafe,
                AnalysisResult = analysisResult,
                CanUse = DetermineCanProceed(analysisResult, policy)
            };

            if (!result.IsValid && analysisResult.Violations.Any(v => v.RecommendedAction == SafetyAction.AutoFix))
            {
                result.ModifiedResponse = ApplyAutoFixes(response, analysisResult);
            }

            _logger.LogInformation(
                "Response validation complete. Valid: {IsValid}, CanUse: {CanUse}",
                result.IsValid,
                result.CanUse);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating response");
            throw;
        }
    }

    /// <summary>
    /// Suggests alternative prompts that would pass safety filters
    /// </summary>
    public async Task<List<string>> SuggestSafeAlternativesAsync(
        string originalPrompt,
        SafetyAnalysisResult analysisResult,
        int count = 3,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating {Count} safe alternatives for prompt", count);

        var alternatives = new List<string>();

        foreach (var violation in analysisResult.Violations.Take(count))
        {
            var alternative = await GenerateSafeAlternativeAsync(
                originalPrompt,
                violation,
                ct);

            if (!string.IsNullOrEmpty(alternative))
            {
                alternatives.Add(alternative);
            }
        }

        if (alternatives.Count == 0)
        {
            alternatives.Add(await GenerateGenericSafeAlternativeAsync(originalPrompt, ct));
        }

        return alternatives.Take(count).ToList();
    }

    /// <summary>
    /// Generates a modified prompt that addresses safety concerns while preserving intent
    /// </summary>
    private async Task<string?> GenerateModifiedPromptAsync(
        string originalPrompt,
        SafetyAnalysisResult analysisResult,
        CancellationToken ct)
    {
        var modified = originalPrompt;
        var hasModifications = false;

        foreach (var violation in analysisResult.Violations.OrderByDescending(v => v.SeverityScore))
        {
            if (violation.RecommendedAction == SafetyAction.AutoFix && !string.IsNullOrEmpty(violation.MatchedContent))
            {
                var replacement = violation.SuggestedFix ?? GetSafeReplacement(violation.MatchedContent, violation.Category);
                modified = modified.Replace(violation.MatchedContent, replacement);
                hasModifications = true;

                _logger.LogInformation(
                    "Modified prompt: replaced '{Original}' with '{Replacement}'",
                    violation.MatchedContent,
                    replacement);
            }
            else if (violation.RecommendedAction == SafetyAction.Block)
            {
                modified = RemoveProblematicSection(modified, violation);
                hasModifications = true;
            }
        }

        if (hasModifications)
        {
            modified = CleanupModifiedPrompt(modified);
        }

        await Task.CompletedTask;
        return hasModifications ? modified : null;
    }

    /// <summary>
    /// Generates a human-readable explanation of safety blocks
    /// </summary>
    private string GenerateSafetyExplanation(SafetyAnalysisResult analysisResult, SafetyPolicy policy)
    {
        var blockingViolations = analysisResult.Violations
            .Where(v => v.RecommendedAction == SafetyAction.Block)
            .ToList();

        if (blockingViolations.Count == 0)
        {
            return "Content has warnings but can proceed with modifications.";
        }

        var explanation = "This prompt was blocked for the following reasons:\n\n";

        foreach (var violation in blockingViolations.Take(3))
        {
            explanation += $"• {violation.Reason}";

            if (!string.IsNullOrEmpty(violation.MatchedContent))
            {
                explanation += $" (found: '{violation.MatchedContent}')";
            }

            explanation += "\n";
        }

        if (policy.AllowUserOverride)
        {
            explanation += "\nYou can override this decision in Advanced Mode if you believe this is a false positive.";
        }
        else
        {
            explanation += "\nThis policy does not allow overrides. Please modify your prompt to comply with the safety guidelines.";
        }

        return explanation;
    }

    /// <summary>
    /// Generates alternative prompts that avoid safety violations
    /// </summary>
    private List<string> GenerateAlternatives(string originalPrompt, SafetyAnalysisResult analysisResult)
    {
        var alternatives = new List<string>();

        var mainViolations = analysisResult.Violations
            .Where(v => v.SeverityScore >= 7)
            .Take(3)
            .ToList();

        foreach (var violation in mainViolations)
        {
            var alternative = GenerateAlternativeForViolation(originalPrompt, violation);
            if (!string.IsNullOrEmpty(alternative))
            {
                alternatives.Add(alternative);
            }
        }

        if (alternatives.Count == 0)
        {
            alternatives.Add(GenerateGenericSafePrompt(originalPrompt));
        }

        return alternatives;
    }

    /// <summary>
    /// Applies automatic fixes to content based on violations
    /// </summary>
    private string ApplyAutoFixes(string content, SafetyAnalysisResult analysisResult)
    {
        var modified = content;

        foreach (var violation in analysisResult.Violations)
        {
            if (violation.RecommendedAction == SafetyAction.AutoFix)
            {
                if (!string.IsNullOrEmpty(violation.MatchedContent) && !string.IsNullOrEmpty(violation.SuggestedFix))
                {
                    modified = modified.Replace(violation.MatchedContent, violation.SuggestedFix);
                }
            }
        }

        return modified;
    }

    private bool DetermineCanProceed(SafetyAnalysisResult analysisResult, SafetyPolicy policy)
    {
        if (analysisResult.IsSafe)
        {
            return true;
        }

        var blockingViolations = analysisResult.Violations
            .Where(v => v.RecommendedAction == SafetyAction.Block)
            .ToList();

        if (blockingViolations.Count == 0)
        {
            return true;
        }

        return policy.AllowUserOverride;
    }

    private async Task<string> GenerateSafeAlternativeAsync(
        string originalPrompt,
        SafetyViolation violation,
        CancellationToken ct)
    {
        await Task.CompletedTask;
        return GenerateAlternativeForViolation(originalPrompt, violation);
    }

    private string GenerateAlternativeForViolation(string originalPrompt, SafetyViolation violation)
    {
        var alternative = originalPrompt;

        if (!string.IsNullOrEmpty(violation.MatchedContent))
        {
            var safeReplacement = GetSafeReplacement(violation.MatchedContent, violation.Category);
            alternative = alternative.Replace(violation.MatchedContent, safeReplacement);
        }
        else
        {
            alternative = ReframePromptForCategory(originalPrompt, violation.Category);
        }

        return alternative;
    }

    private async Task<string> GenerateGenericSafeAlternativeAsync(string originalPrompt, CancellationToken ct)
    {
        await Task.CompletedTask;
        return GenerateGenericSafePrompt(originalPrompt);
    }

    private string GenerateGenericSafePrompt(string originalPrompt)
    {
        var intent = ExtractIntent(originalPrompt);
        return $"Create content about {intent} that is appropriate for all audiences.";
    }

    private string GetSafeReplacement(string problematicContent, SafetyCategoryType category)
    {
        return category switch
        {
            SafetyCategoryType.Profanity => "[appropriate language]",
            SafetyCategoryType.Violence => "conflict resolution",
            SafetyCategoryType.SexualContent => "relationships",
            SafetyCategoryType.HateSpeech => "respectful discussion",
            SafetyCategoryType.DrugAlcohol => "wellness",
            SafetyCategoryType.ControversialTopics => "balanced perspective",
            SafetyCategoryType.SelfHarm => "mental health support",
            SafetyCategoryType.GraphicImagery => "appropriate imagery",
            _ => "[content removed for safety]"
        };
    }

    private string RemoveProblematicSection(string content, SafetyViolation violation)
    {
        if (!string.IsNullOrEmpty(violation.MatchedContent))
        {
            var sentences = content.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);
            var safeSentences = sentences.Where(s => !s.Contains(violation.MatchedContent, StringComparison.OrdinalIgnoreCase));
            return string.Join(". ", safeSentences);
        }

        return content;
    }

    private string CleanupModifiedPrompt(string prompt)
    {
        var cleaned = Regex.Replace(prompt, @"\s+", " ");
        cleaned = Regex.Replace(cleaned, @"\s+([.,!?])", "$1");
        return cleaned.Trim();
    }

    private string ReframePromptForCategory(string prompt, SafetyCategoryType category)
    {
        var intent = ExtractIntent(prompt);

        return category switch
        {
            SafetyCategoryType.Profanity => $"Create family-friendly content about {intent} using appropriate language.",
            SafetyCategoryType.Violence => $"Create content about {intent} focusing on peaceful resolution and positive outcomes.",
            SafetyCategoryType.SexualContent => $"Create age-appropriate content about {intent} suitable for general audiences.",
            SafetyCategoryType.HateSpeech => $"Create inclusive content about {intent} that respects all people and perspectives.",
            SafetyCategoryType.DrugAlcohol => $"Create content about {intent} with a focus on health and wellness.",
            SafetyCategoryType.ControversialTopics => $"Create balanced, educational content about {intent}.",
            SafetyCategoryType.SelfHarm => $"Create supportive content about {intent} with mental health resources.",
            SafetyCategoryType.GraphicImagery => $"Create content about {intent} using appropriate, non-graphic visuals.",
            _ => $"Create safe, appropriate content about {intent}."
        };
    }

    private string ExtractIntent(string prompt)
    {
        var words = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var intent = string.Join(" ", words.Take(Math.Min(5, words.Length)));

        intent = Regex.Replace(intent, @"[^\w\s]", "").Trim();

        return string.IsNullOrEmpty(intent) ? "the given topic" : intent;
    }
}

/// <summary>
/// Result of prompt validation
/// </summary>
public class PromptValidationResult
{
    public string OriginalPrompt { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool CanProceed { get; set; }
    public SafetyAnalysisResult? AnalysisResult { get; set; }
    public string? ModifiedPrompt { get; set; }
    public string? Explanation { get; set; }
    public List<string> Alternatives { get; set; } = new();
}

/// <summary>
/// Result of response validation
/// </summary>
public class ResponseValidationResult
{
    public string OriginalResponse { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool CanUse { get; set; }
    public SafetyAnalysisResult? AnalysisResult { get; set; }
    public string? ModifiedResponse { get; set; }
}
