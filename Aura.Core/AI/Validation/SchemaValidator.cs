using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Validation;

/// <summary>
/// Result of schema validation
/// </summary>
public record ValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> ValidationErrors { get; init; } = new();
    public TimeSpan ValidationDuration { get; init; }
}

/// <summary>
/// Validates LLM JSON outputs against defined schemas
/// </summary>
public class SchemaValidator
{
    private readonly ILogger<SchemaValidator> _logger;
    private const int MaxOverheadMs = 5;

    public SchemaValidator(ILogger<SchemaValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates JSON output against a schema and deserializes to the target type
    /// </summary>
    /// <typeparam name="T">Target schema type</typeparam>
    /// <param name="jsonOutput">JSON output from LLM</param>
    /// <returns>Validation result with deserialized object if valid</returns>
    public (ValidationResult Result, T? Data) ValidateAndDeserialize<T>(string jsonOutput) 
        where T : LlmOutputSchema
    {
        var stopwatch = Stopwatch.StartNew();
        var errors = new List<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(jsonOutput))
            {
                errors.Add("JSON output is null or empty");
                return (new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Empty JSON output",
                    ValidationErrors = errors,
                    ValidationDuration = stopwatch.Elapsed
                }, default);
            }

            // Try to deserialize
            T? data;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };
                data = JsonSerializer.Deserialize<T>(jsonOutput, options);
            }
            catch (JsonException ex)
            {
                errors.Add($"JSON deserialization failed: {ex.Message}");
                _logger.LogWarning(ex, "Failed to deserialize JSON output to {SchemaType}", typeof(T).Name);
                return (new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"Invalid JSON format: {ex.Message}",
                    ValidationErrors = errors,
                    ValidationDuration = stopwatch.Elapsed
                }, default);
            }

            if (data == null)
            {
                errors.Add("Deserialized data is null");
                return (new ValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Deserialization resulted in null",
                    ValidationErrors = errors,
                    ValidationDuration = stopwatch.Elapsed
                }, default);
            }

            // Perform type-specific validation
            var typeErrors = ValidateSchemaType(data);
            errors.AddRange(typeErrors);

            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > MaxOverheadMs)
            {
                _logger.LogWarning(
                    "Schema validation overhead ({ElapsedMs}ms) exceeds target of {MaxMs}ms for {SchemaType}",
                    stopwatch.ElapsedMilliseconds, MaxOverheadMs, typeof(T).Name);
            }

            var isValid = errors.Count == 0;
            return (new ValidationResult
            {
                IsValid = isValid,
                ErrorMessage = isValid ? null : string.Join("; ", errors),
                ValidationErrors = errors,
                ValidationDuration = stopwatch.Elapsed
            }, isValid ? data : default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during schema validation for {SchemaType}", typeof(T).Name);
            errors.Add($"Validation error: {ex.Message}");
            return (new ValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Validation error: {ex.Message}",
                ValidationErrors = errors,
                ValidationDuration = stopwatch.Elapsed
            }, default);
        }
    }

    /// <summary>
    /// Performs type-specific validation based on schema constraints
    /// </summary>
    private List<string> ValidateSchemaType<T>(T data) where T : LlmOutputSchema
    {
        var errors = new List<string>();

        switch (data)
        {
            case SceneAnalysisSchema schema:
                ValidateSceneAnalysis(schema, errors);
                break;
            case VisualPromptSchema schema:
                ValidateVisualPrompt(schema, errors);
                break;
            case ContentComplexitySchema schema:
                ValidateContentComplexity(schema, errors);
                break;
            case SceneCoherenceSchema schema:
                ValidateSceneCoherence(schema, errors);
                break;
            case NarrativeArcSchema schema:
                ValidateNarrativeArc(schema, errors);
                break;
        }

        return errors;
    }

    private void ValidateSceneAnalysis(SceneAnalysisSchema schema, List<string> errors)
    {
        if (schema.Importance < 0.0 || schema.Importance > 1.0)
            errors.Add($"Importance must be between 0.0 and 1.0, got {schema.Importance}");
        
        if (schema.Complexity < 0.0 || schema.Complexity > 1.0)
            errors.Add($"Complexity must be between 0.0 and 1.0, got {schema.Complexity}");
        
        if (schema.EmotionalIntensity < 0.0 || schema.EmotionalIntensity > 1.0)
            errors.Add($"EmotionalIntensity must be between 0.0 and 1.0, got {schema.EmotionalIntensity}");
        
        var validDensities = new[] { "low", "medium", "high" };
        if (!validDensities.Contains(schema.InformationDensity.ToLowerInvariant()))
            errors.Add($"InformationDensity must be one of: {string.Join(", ", validDensities)}");
        
        if (schema.OptimalDurationSeconds < 0.5 || schema.OptimalDurationSeconds > 120.0)
            errors.Add($"OptimalDurationSeconds must be between 0.5 and 120.0, got {schema.OptimalDurationSeconds}");
        
        var validTransitions = new[] { "cut", "fade", "dissolve", "wipe" };
        if (!validTransitions.Contains(schema.TransitionType.ToLowerInvariant()))
            errors.Add($"TransitionType must be one of: {string.Join(", ", validTransitions)}");
        
        if (string.IsNullOrWhiteSpace(schema.Reasoning) || schema.Reasoning.Length < 10)
            errors.Add("Reasoning must be at least 10 characters");
    }

    private void ValidateVisualPrompt(VisualPromptSchema schema, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(schema.DetailedDescription) || schema.DetailedDescription.Length < 20)
            errors.Add("DetailedDescription must be at least 20 characters");
        
        if (string.IsNullOrWhiteSpace(schema.CompositionGuidelines) || schema.CompositionGuidelines.Length < 10)
            errors.Add("CompositionGuidelines must be at least 10 characters");
        
        if (string.IsNullOrWhiteSpace(schema.LightingMood) || schema.LightingMood.Length < 3)
            errors.Add("LightingMood must be at least 3 characters");
        
        if (schema.ColorPalette.Length == 0)
            errors.Add("ColorPalette must contain at least one color");
        
        if (string.IsNullOrWhiteSpace(schema.ShotType) || schema.ShotType.Length < 3)
            errors.Add("ShotType must be at least 3 characters");
        
        if (string.IsNullOrWhiteSpace(schema.Reasoning) || schema.Reasoning.Length < 10)
            errors.Add("Reasoning must be at least 10 characters");
    }

    private void ValidateContentComplexity(ContentComplexitySchema schema, List<string> errors)
    {
        if (schema.OverallComplexityScore < 0.0 || schema.OverallComplexityScore > 1.0)
            errors.Add($"OverallComplexityScore must be between 0.0 and 1.0, got {schema.OverallComplexityScore}");
        
        if (schema.ConceptDifficulty < 0.0 || schema.ConceptDifficulty > 1.0)
            errors.Add($"ConceptDifficulty must be between 0.0 and 1.0, got {schema.ConceptDifficulty}");
        
        if (schema.TerminologyDensity < 0.0 || schema.TerminologyDensity > 1.0)
            errors.Add($"TerminologyDensity must be between 0.0 and 1.0, got {schema.TerminologyDensity}");
        
        if (schema.NewConceptsIntroduced < 0)
            errors.Add($"NewConceptsIntroduced must be non-negative, got {schema.NewConceptsIntroduced}");
        
        if (string.IsNullOrWhiteSpace(schema.DetailedBreakdown) || schema.DetailedBreakdown.Length < 10)
            errors.Add("DetailedBreakdown must be at least 10 characters");
    }

    private void ValidateSceneCoherence(SceneCoherenceSchema schema, List<string> errors)
    {
        if (schema.CoherenceScore < 0.0 || schema.CoherenceScore > 1.0)
            errors.Add($"CoherenceScore must be between 0.0 and 1.0, got {schema.CoherenceScore}");
        
        if (schema.ConnectionTypes.Length == 0)
            errors.Add("ConnectionTypes must contain at least one connection type");
        
        if (schema.ConfidenceScore < 0.0 || schema.ConfidenceScore > 1.0)
            errors.Add($"ConfidenceScore must be between 0.0 and 1.0, got {schema.ConfidenceScore}");
        
        if (string.IsNullOrWhiteSpace(schema.Reasoning) || schema.Reasoning.Length < 10)
            errors.Add("Reasoning must be at least 10 characters");
    }

    private void ValidateNarrativeArc(NarrativeArcSchema schema, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(schema.DetectedStructure) || schema.DetectedStructure.Length < 5)
            errors.Add("DetectedStructure must be at least 5 characters");
        
        if (string.IsNullOrWhiteSpace(schema.ExpectedStructure) || schema.ExpectedStructure.Length < 5)
            errors.Add("ExpectedStructure must be at least 5 characters");
        
        if (string.IsNullOrWhiteSpace(schema.Reasoning) || schema.Reasoning.Length < 10)
            errors.Add("Reasoning must be at least 10 characters");
    }

    /// <summary>
    /// Generates a repair prompt to help the LLM fix validation errors
    /// </summary>
    /// <param name="originalPrompt">Original prompt sent to LLM</param>
    /// <param name="failedOutput">The output that failed validation</param>
    /// <param name="validationErrors">List of validation errors</param>
    /// <param name="schemaDefinition">JSON schema definition</param>
    /// <returns>Modified prompt with repair instructions</returns>
    public string GenerateRepairPrompt(
        string originalPrompt, 
        string failedOutput, 
        List<string> validationErrors,
        string schemaDefinition)
    {
        var errorList = string.Join("\n- ", validationErrors);
        
        return $@"{originalPrompt}

IMPORTANT: Your previous response failed validation with the following errors:
- {errorList}

Please provide a corrected response that strictly conforms to this JSON schema:
{schemaDefinition}

Requirements:
1. Return ONLY valid JSON, no markdown code blocks or explanations
2. Ensure all required fields are present with correct types
3. Validate numeric ranges match schema constraints
4. Ensure string fields meet minimum length requirements
5. Provide arrays where specified, not single values

Previous failed output (for reference):
{failedOutput}";
    }
}
