using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PromptManagement;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PromptManagement;

/// <summary>
/// Validates prompt templates for syntax errors, security issues, and best practices
/// </summary>
public class PromptValidator
{
    private readonly ILogger<PromptValidator> _logger;
    private readonly PromptVariableResolver _variableResolver;

    private static readonly string[] MaliciousPatterns = new[]
    {
        @"ignore\s+(previous|all|above|prior)\s+(instructions?|prompts?|rules?)",
        @"disregard\s+(previous|all|above|prior)",
        @"forget\s+(everything|all|previous|above)",
        @"override\s+(instructions?|rules?|system)",
        @"new\s+(instructions?|rules?|system)",
        @"act\s+as\s+if",
        @"pretend\s+(to\s+be|you\s+are)",
        @"system\s*:\s*",
        @"admin\s+(mode|access|override)",
        @"<script[^>]*>",
        @"javascript:",
        @"on(load|error|click)\s*=",
    };

    private static readonly Regex VariablePattern = new(@"\{\{(\w+)(?:\s*\|\s*\w+(?::.+?)?)?\}\}", RegexOptions.Compiled);

    public PromptValidator(
        ILogger<PromptValidator> logger,
        PromptVariableResolver variableResolver)
    {
        _logger = logger;
        _variableResolver = variableResolver;
    }

    /// <summary>
    /// Validate a prompt template
    /// </summary>
    public async Task<ValidationResult> ValidateTemplateAsync(
        PromptTemplate template,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;

        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(template.Name))
            errors.Add("Template name is required");

        if (string.IsNullOrWhiteSpace(template.PromptText))
            errors.Add("Prompt text is required");

        if (template.PromptText.Length > 50000)
            errors.Add("Prompt text exceeds maximum length of 50,000 characters");

        ValidateSecurityIssues(template.PromptText, errors, warnings);

        ValidateVariableDefinitions(template, errors, warnings);

        ValidateVariableReferences(template, errors, warnings);

        ValidateContextWindow(template, warnings);

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Validate custom instructions for security issues
    /// </summary>
    public async Task<ValidationResult> ValidateCustomInstructionsAsync(
        string instructions,
        CancellationToken ct = default)
    {
        await Task.CompletedTask;

        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(instructions))
        {
            return new ValidationResult { IsValid = true };
        }

        if (instructions.Length > 5000)
            errors.Add("Custom instructions exceed maximum length of 5,000 characters");

        ValidateSecurityIssues(instructions, errors, warnings);

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    /// <summary>
    /// Check for prompt injection and malicious patterns
    /// </summary>
    private void ValidateSecurityIssues(string text, List<string> errors, List<string> warnings)
    {
        var lowerText = text.ToLowerInvariant();

        foreach (var pattern in MaliciousPatterns)
        {
            if (Regex.IsMatch(lowerText, pattern, RegexOptions.IgnoreCase))
            {
                errors.Add($"Potentially malicious pattern detected: '{pattern}'");
                _logger.LogWarning("Security validation failed: Pattern {Pattern} found", pattern);
            }
        }

        if (text.Contains("```") && text.Contains("system"))
        {
            warnings.Add("Template contains code blocks and system references, which may cause unexpected behavior");
        }

        if (Regex.IsMatch(text, @"https?://", RegexOptions.IgnoreCase))
        {
            warnings.Add("Template contains URLs, which may pose security risks");
        }
    }

    /// <summary>
    /// Validate variable definitions are correct
    /// </summary>
    private void ValidateVariableDefinitions(
        PromptTemplate template,
        List<string> errors,
        List<string> warnings)
    {
        var variableNames = new HashSet<string>();

        foreach (var variable in template.Variables)
        {
            if (string.IsNullOrWhiteSpace(variable.Name))
            {
                errors.Add("Variable name is required");
                continue;
            }

            if (!Regex.IsMatch(variable.Name, @"^[a-zA-Z_]\w*$"))
            {
                errors.Add($"Variable name '{variable.Name}' contains invalid characters");
            }

            if (!variableNames.Add(variable.Name))
            {
                errors.Add($"Duplicate variable name: {variable.Name}");
            }

            if (variable.Required && !string.IsNullOrEmpty(variable.DefaultValue))
            {
                warnings.Add($"Variable '{variable.Name}' is required but has a default value");
            }

            if (variable.MinLength.HasValue && variable.MaxLength.HasValue &&
                variable.MinLength.Value > variable.MaxLength.Value)
            {
                errors.Add($"Variable '{variable.Name}' min length is greater than max length");
            }

            if (variable.AllowedValues?.Any() == true && 
                !string.IsNullOrEmpty(variable.DefaultValue) &&
                !variable.AllowedValues.Contains(variable.DefaultValue))
            {
                errors.Add($"Variable '{variable.Name}' default value is not in allowed values");
            }
        }
    }

    /// <summary>
    /// Validate that all referenced variables are defined
    /// </summary>
    private void ValidateVariableReferences(
        PromptTemplate template,
        List<string> errors,
        List<string> warnings)
    {
        var referencedVars = _variableResolver.ExtractVariableNames(template.PromptText);
        var definedVars = template.Variables.Select(v => v.Name).ToHashSet();

        foreach (var refVar in referencedVars)
        {
            if (!definedVars.Contains(refVar))
            {
                errors.Add($"Variable '{{{{{refVar}}}}}' is referenced but not defined");
            }
        }

        foreach (var defVar in template.Variables)
        {
            if (!referencedVars.Contains(defVar.Name))
            {
                warnings.Add($"Variable '{defVar.Name}' is defined but never used");
            }
        }
    }

    /// <summary>
    /// Validate prompt length against typical context windows
    /// </summary>
    private void ValidateContextWindow(PromptTemplate template, List<string> warnings)
    {
        var estimatedTokens = EstimateTokenCount(template.PromptText);

        if (estimatedTokens > 4000)
        {
            warnings.Add($"Estimated token count ({estimatedTokens}) may exceed some model context windows");
        }

        if (template.TargetProvider == TargetLlmProvider.OpenAI && estimatedTokens > 8000)
        {
            warnings.Add("Prompt may be too long for some OpenAI models (GPT-3.5)");
        }

        if (template.TargetProvider == TargetLlmProvider.Anthropic && estimatedTokens > 100000)
        {
            warnings.Add("Prompt is approaching Claude's 100K token limit");
        }
    }

    /// <summary>
    /// Rough estimate of token count
    /// </summary>
    private int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)(wordCount * 1.3);
    }
}

/// <summary>
/// Result of validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
