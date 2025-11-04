using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Aura.Core.AI.Orchestration;

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
            case PlanSchema schema:
                ValidatePlan(schema, errors);
                break;
            case SceneBreakdownSchema schema:
                ValidateSceneBreakdown(schema, errors);
                break;
            case VoiceStyleSchema schema:
                ValidateVoiceStyle(schema, errors);
                break;
            case SSMLSpecSchema schema:
                ValidateSSMLSpec(schema, errors);
                break;
            case VisualPromptSpecSchema schema:
                ValidateVisualPromptSpec(schema, errors);
                break;
            case RenderTimelineSchema schema:
                ValidateRenderTimeline(schema, errors);
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
        if (string.IsNullOrWhiteSpace(schema.InformationDensity) || 
            !validDensities.Contains(schema.InformationDensity.ToLowerInvariant()))
            errors.Add($"InformationDensity must be one of: {string.Join(", ", validDensities)}");
        
        if (schema.OptimalDurationSeconds < 0.5 || schema.OptimalDurationSeconds > 120.0)
            errors.Add($"OptimalDurationSeconds must be between 0.5 and 120.0, got {schema.OptimalDurationSeconds}");
        
        var validTransitions = new[] { "cut", "fade", "dissolve", "wipe" };
        if (string.IsNullOrWhiteSpace(schema.TransitionType) ||
            !validTransitions.Contains(schema.TransitionType.ToLowerInvariant()))
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

    private void ValidatePlan(PlanSchema schema, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(schema.Outline) || schema.Outline.Length < 50)
            errors.Add("Outline must be at least 50 characters");
        
        if (schema.SceneCount < 1 || schema.SceneCount > 50)
            errors.Add($"SceneCount must be between 1 and 50, got {schema.SceneCount}");
        
        if (schema.EstimatedDurationSeconds < 5.0 || schema.EstimatedDurationSeconds > 3600.0)
            errors.Add($"EstimatedDurationSeconds must be between 5.0 and 3600.0, got {schema.EstimatedDurationSeconds}");
        
        var validPacing = new[] { "slow", "moderate", "fast", "dynamic" };
        if (string.IsNullOrWhiteSpace(schema.TargetPacing) || 
            !validPacing.Contains(schema.TargetPacing.ToLowerInvariant()))
            errors.Add($"TargetPacing must be one of: {string.Join(", ", validPacing)}");
        
        var validDensity = new[] { "sparse", "moderate", "dense" };
        if (string.IsNullOrWhiteSpace(schema.ContentDensity) || 
            !validDensity.Contains(schema.ContentDensity.ToLowerInvariant()))
            errors.Add($"ContentDensity must be one of: {string.Join(", ", validDensity)}");
        
        if (string.IsNullOrWhiteSpace(schema.NarrativeStructure) || schema.NarrativeStructure.Length < 10)
            errors.Add("NarrativeStructure must be at least 10 characters");
        
        if (schema.KeyMessages.Length < 1 || schema.KeyMessages.Length > 10)
            errors.Add($"KeyMessages must contain 1-10 items, got {schema.KeyMessages.Length}");
        
        foreach (var msg in schema.KeyMessages)
        {
            if (string.IsNullOrWhiteSpace(msg) || msg.Length < 5)
                errors.Add("Each KeyMessage must be at least 5 characters");
        }
    }

    private void ValidateSceneBreakdown(SceneBreakdownSchema schema, List<string> errors)
    {
        if (schema.Scenes.Length < 1)
            errors.Add("Scenes array must contain at least one scene");
        
        foreach (var scene in schema.Scenes)
        {
            if (scene.Index < 0)
                errors.Add($"Scene index must be non-negative, got {scene.Index}");
            
            if (string.IsNullOrWhiteSpace(scene.Heading) || scene.Heading.Length < 3)
                errors.Add($"Scene {scene.Index}: Heading must be at least 3 characters");
            
            if (string.IsNullOrWhiteSpace(scene.Script) || scene.Script.Length < 10)
                errors.Add($"Scene {scene.Index}: Script must be at least 10 characters");
            
            if (scene.DurationSeconds < 1.0 || scene.DurationSeconds > 300.0)
                errors.Add($"Scene {scene.Index}: DurationSeconds must be between 1.0 and 300.0, got {scene.DurationSeconds}");
            
            if (string.IsNullOrWhiteSpace(scene.Purpose) || scene.Purpose.Length < 10)
                errors.Add($"Scene {scene.Index}: Purpose must be at least 10 characters");
            
            if (!string.IsNullOrEmpty(scene.TransitionType))
            {
                var validTransitions = new[] { "cut", "fade", "dissolve", "wipe" };
                if (!validTransitions.Contains(scene.TransitionType.ToLowerInvariant()))
                    errors.Add($"Scene {scene.Index}: TransitionType must be one of: {string.Join(", ", validTransitions)}");
            }
        }
    }

    private void ValidateVoiceStyle(VoiceStyleSchema schema, List<string> errors)
    {
        if (schema.VoiceCharacteristics.Rate < 0.5 || schema.VoiceCharacteristics.Rate > 2.0)
            errors.Add($"VoiceCharacteristics.Rate must be between 0.5 and 2.0, got {schema.VoiceCharacteristics.Rate}");
        
        if (schema.VoiceCharacteristics.Pitch < 0.5 || schema.VoiceCharacteristics.Pitch > 2.0)
            errors.Add($"VoiceCharacteristics.Pitch must be between 0.5 and 2.0, got {schema.VoiceCharacteristics.Pitch}");
        
        if (schema.VoiceCharacteristics.Volume < 0.0 || schema.VoiceCharacteristics.Volume > 1.0)
            errors.Add($"VoiceCharacteristics.Volume must be between 0.0 and 1.0, got {schema.VoiceCharacteristics.Volume}");
        
        if (schema.PacingGuidelines.DefaultPauseMs < 0 || schema.PacingGuidelines.DefaultPauseMs > 2000)
            errors.Add($"PacingGuidelines.DefaultPauseMs must be between 0 and 2000, got {schema.PacingGuidelines.DefaultPauseMs}");
        
        if (schema.PacingGuidelines.SentencePauseMs < 0 || schema.PacingGuidelines.SentencePauseMs > 3000)
            errors.Add($"PacingGuidelines.SentencePauseMs must be between 0 and 3000, got {schema.PacingGuidelines.SentencePauseMs}");
        
        if (schema.PacingGuidelines.ParagraphPauseMs < 0 || schema.PacingGuidelines.ParagraphPauseMs > 5000)
            errors.Add($"PacingGuidelines.ParagraphPauseMs must be between 0 and 5000, got {schema.PacingGuidelines.ParagraphPauseMs}");
        
        if (string.IsNullOrWhiteSpace(schema.EmotionalTone) || schema.EmotionalTone.Length < 3)
            errors.Add("EmotionalTone must be at least 3 characters");
        
        foreach (var emphasis in schema.Emphasis)
        {
            if (string.IsNullOrWhiteSpace(emphasis.Text))
                errors.Add("Emphasis text cannot be empty");
            
            var validStrength = new[] { "weak", "moderate", "strong" };
            if (string.IsNullOrWhiteSpace(emphasis.Strength) || 
                !validStrength.Contains(emphasis.Strength.ToLowerInvariant()))
                errors.Add($"Emphasis strength must be one of: {string.Join(", ", validStrength)}");
        }
    }

    private void ValidateSSMLSpec(SSMLSpecSchema schema, List<string> errors)
    {
        if (schema.SSMLSegments.Length < 1)
            errors.Add("SSMLSegments array must contain at least one segment");
        
        foreach (var segment in schema.SSMLSegments)
        {
            if (segment.SceneIndex < 0)
                errors.Add($"Segment scene index must be non-negative, got {segment.SceneIndex}");
            
            if (string.IsNullOrWhiteSpace(segment.Text))
                errors.Add($"Segment {segment.SceneIndex}: Text cannot be empty");
            
            if (string.IsNullOrWhiteSpace(segment.SSML))
                errors.Add($"Segment {segment.SceneIndex}: SSML cannot be empty");
            
            if (segment.EstimatedDurationMs.HasValue && segment.EstimatedDurationMs.Value < 0)
                errors.Add($"Segment {segment.SceneIndex}: EstimatedDurationMs must be non-negative");
        }
    }

    private void ValidateVisualPromptSpec(VisualPromptSpecSchema schema, List<string> errors)
    {
        if (schema.VisualPrompts.Length < 1)
            errors.Add("VisualPrompts array must contain at least one prompt");
        
        foreach (var prompt in schema.VisualPrompts)
        {
            if (prompt.SceneIndex < 0)
                errors.Add($"Visual prompt scene index must be non-negative, got {prompt.SceneIndex}");
            
            if (string.IsNullOrWhiteSpace(prompt.Prompt) || prompt.Prompt.Length < 20)
                errors.Add($"Visual prompt {prompt.SceneIndex}: Prompt must be at least 20 characters");
            
            if (prompt.StyleKeywords.Length < 1)
                errors.Add($"Visual prompt {prompt.SceneIndex}: StyleKeywords must contain at least one keyword");
        }
    }

    private void ValidateRenderTimeline(RenderTimelineSchema schema, List<string> errors)
    {
        if (schema.TimelineSegments.Length < 1)
            errors.Add("TimelineSegments array must contain at least one segment");
        
        if (schema.TotalDurationSeconds < 1.0)
            errors.Add($"TotalDurationSeconds must be at least 1.0, got {schema.TotalDurationSeconds}");
        
        foreach (var segment in schema.TimelineSegments)
        {
            if (segment.SceneIndex < 0)
                errors.Add($"Timeline segment scene index must be non-negative, got {segment.SceneIndex}");
            
            if (segment.StartTimeSeconds < 0.0)
                errors.Add($"Segment {segment.SceneIndex}: StartTimeSeconds must be non-negative");
            
            if (segment.DurationSeconds < 0.1)
                errors.Add($"Segment {segment.SceneIndex}: DurationSeconds must be at least 0.1");
            
            if (string.IsNullOrWhiteSpace(segment.AudioPath))
                errors.Add($"Segment {segment.SceneIndex}: AudioPath cannot be empty");
            
            if (string.IsNullOrWhiteSpace(segment.VisualPath))
                errors.Add($"Segment {segment.SceneIndex}: VisualPath cannot be empty");
        }
        
        foreach (var transition in schema.TransitionPlan)
        {
            if (transition.FromSceneIndex < 0)
                errors.Add($"Transition FromSceneIndex must be non-negative, got {transition.FromSceneIndex}");
            
            if (transition.ToSceneIndex < 0)
                errors.Add($"Transition ToSceneIndex must be non-negative, got {transition.ToSceneIndex}");
            
            var validTransitions = new[] { "cut", "fade", "dissolve", "wipe" };
            if (string.IsNullOrWhiteSpace(transition.TransitionType) || 
                !validTransitions.Contains(transition.TransitionType.ToLowerInvariant()))
                errors.Add($"Transition type must be one of: {string.Join(", ", validTransitions)}");
            
            if (transition.DurationMs < 0 || transition.DurationMs > 2000)
                errors.Add($"Transition duration must be between 0 and 2000ms, got {transition.DurationMs}");
        }
    }
}
