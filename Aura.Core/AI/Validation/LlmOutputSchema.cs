using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aura.Core.AI.Validation;

/// <summary>
/// Base class for LLM output schema definitions
/// </summary>
public abstract record LlmOutputSchema
{
    /// <summary>
    /// Gets the schema name for logging and error messages
    /// </summary>
    public abstract string SchemaName { get; }
    
    /// <summary>
    /// Gets the JSON schema definition as a string
    /// </summary>
    public abstract string GetSchemaDefinition();
}

/// <summary>
/// Schema for scene analysis results
/// </summary>
public record SceneAnalysisSchema : LlmOutputSchema
{
    public override string SchemaName => "SceneAnalysis";
    
    [JsonPropertyName("importance")]
    public double Importance { get; init; }
    
    [JsonPropertyName("complexity")]
    public double Complexity { get; init; }
    
    [JsonPropertyName("emotionalIntensity")]
    public double EmotionalIntensity { get; init; }
    
    [JsonPropertyName("informationDensity")]
    public string InformationDensity { get; init; } = string.Empty;
    
    [JsonPropertyName("optimalDurationSeconds")]
    public double OptimalDurationSeconds { get; init; }
    
    [JsonPropertyName("transitionType")]
    public string TransitionType { get; init; } = string.Empty;
    
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; init; } = string.Empty;
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""importance"", ""complexity"", ""emotionalIntensity"", ""informationDensity"", ""optimalDurationSeconds"", ""transitionType"", ""reasoning""],
  ""properties"": {
    ""importance"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""complexity"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""emotionalIntensity"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""informationDensity"": { ""type"": ""string"", ""enum"": [""low"", ""medium"", ""high""] },
    ""optimalDurationSeconds"": { ""type"": ""number"", ""minimum"": 0.5, ""maximum"": 120.0 },
    ""transitionType"": { ""type"": ""string"", ""enum"": [""cut"", ""fade"", ""dissolve"", ""wipe""] },
    ""reasoning"": { ""type"": ""string"", ""minLength"": 10 }
  }
}";
    }
}

/// <summary>
/// Schema for visual prompt results
/// </summary>
public record VisualPromptSchema : LlmOutputSchema
{
    public override string SchemaName => "VisualPrompt";
    
    [JsonPropertyName("detailedDescription")]
    public string DetailedDescription { get; init; } = string.Empty;
    
    [JsonPropertyName("compositionGuidelines")]
    public string CompositionGuidelines { get; init; } = string.Empty;
    
    [JsonPropertyName("lightingMood")]
    public string LightingMood { get; init; } = string.Empty;
    
    [JsonPropertyName("lightingDirection")]
    public string LightingDirection { get; init; } = string.Empty;
    
    [JsonPropertyName("lightingQuality")]
    public string LightingQuality { get; init; } = string.Empty;
    
    [JsonPropertyName("timeOfDay")]
    public string TimeOfDay { get; init; } = string.Empty;
    
    [JsonPropertyName("colorPalette")]
    public string[] ColorPalette { get; init; } = Array.Empty<string>();
    
    [JsonPropertyName("shotType")]
    public string ShotType { get; init; } = string.Empty;
    
    [JsonPropertyName("cameraAngle")]
    public string CameraAngle { get; init; } = string.Empty;
    
    [JsonPropertyName("depthOfField")]
    public string DepthOfField { get; init; } = string.Empty;
    
    [JsonPropertyName("styleKeywords")]
    public string[] StyleKeywords { get; init; } = Array.Empty<string>();
    
    [JsonPropertyName("negativeElements")]
    public string[] NegativeElements { get; init; } = Array.Empty<string>();
    
    [JsonPropertyName("continuityElements")]
    public string[] ContinuityElements { get; init; } = Array.Empty<string>();
    
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; init; } = string.Empty;
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""detailedDescription"", ""compositionGuidelines"", ""lightingMood"", ""shotType"", ""reasoning""],
  ""properties"": {
    ""detailedDescription"": { ""type"": ""string"", ""minLength"": 20 },
    ""compositionGuidelines"": { ""type"": ""string"", ""minLength"": 10 },
    ""lightingMood"": { ""type"": ""string"", ""minLength"": 3 },
    ""lightingDirection"": { ""type"": ""string"" },
    ""lightingQuality"": { ""type"": ""string"" },
    ""timeOfDay"": { ""type"": ""string"" },
    ""colorPalette"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""minItems"": 1 },
    ""shotType"": { ""type"": ""string"", ""minLength"": 3 },
    ""cameraAngle"": { ""type"": ""string"" },
    ""depthOfField"": { ""type"": ""string"" },
    ""styleKeywords"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""negativeElements"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""continuityElements"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""reasoning"": { ""type"": ""string"", ""minLength"": 10 }
  }
}";
    }
}

/// <summary>
/// Schema for content complexity analysis results
/// </summary>
public record ContentComplexitySchema : LlmOutputSchema
{
    public override string SchemaName => "ContentComplexity";
    
    [JsonPropertyName("overallComplexityScore")]
    public double OverallComplexityScore { get; init; }
    
    [JsonPropertyName("conceptDifficulty")]
    public double ConceptDifficulty { get; init; }
    
    [JsonPropertyName("terminologyDensity")]
    public double TerminologyDensity { get; init; }
    
    [JsonPropertyName("prerequisiteKnowledgeLevel")]
    public double PrerequisiteKnowledgeLevel { get; init; }
    
    [JsonPropertyName("multiStepReasoningRequired")]
    public double MultiStepReasoningRequired { get; init; }
    
    [JsonPropertyName("newConceptsIntroduced")]
    public int NewConceptsIntroduced { get; init; }
    
    [JsonPropertyName("cognitiveProcessingTimeSeconds")]
    public double CognitiveProcessingTimeSeconds { get; init; }
    
    [JsonPropertyName("optimalAttentionWindowSeconds")]
    public double OptimalAttentionWindowSeconds { get; init; }
    
    [JsonPropertyName("detailedBreakdown")]
    public string DetailedBreakdown { get; init; } = string.Empty;
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""overallComplexityScore"", ""conceptDifficulty"", ""terminologyDensity"", ""newConceptsIntroduced"", ""detailedBreakdown""],
  ""properties"": {
    ""overallComplexityScore"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""conceptDifficulty"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""terminologyDensity"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""prerequisiteKnowledgeLevel"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""multiStepReasoningRequired"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""newConceptsIntroduced"": { ""type"": ""integer"", ""minimum"": 0 },
    ""cognitiveProcessingTimeSeconds"": { ""type"": ""number"", ""minimum"": 0.0 },
    ""optimalAttentionWindowSeconds"": { ""type"": ""number"", ""minimum"": 0.0 },
    ""detailedBreakdown"": { ""type"": ""string"", ""minLength"": 10 }
  }
}";
    }
}

/// <summary>
/// Schema for scene coherence results
/// </summary>
public record SceneCoherenceSchema : LlmOutputSchema
{
    public override string SchemaName => "SceneCoherence";
    
    [JsonPropertyName("coherenceScore")]
    public double CoherenceScore { get; init; }
    
    [JsonPropertyName("connectionTypes")]
    public string[] ConnectionTypes { get; init; } = Array.Empty<string>();
    
    [JsonPropertyName("confidenceScore")]
    public double ConfidenceScore { get; init; }
    
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; init; } = string.Empty;
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""coherenceScore"", ""connectionTypes"", ""confidenceScore"", ""reasoning""],
  ""properties"": {
    ""coherenceScore"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""connectionTypes"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""minItems"": 1 },
    ""confidenceScore"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 },
    ""reasoning"": { ""type"": ""string"", ""minLength"": 10 }
  }
}";
    }
}

/// <summary>
/// Schema for narrative arc validation results
/// </summary>
public record NarrativeArcSchema : LlmOutputSchema
{
    public override string SchemaName => "NarrativeArc";
    
    [JsonPropertyName("isValid")]
    public bool IsValid { get; init; }
    
    [JsonPropertyName("detectedStructure")]
    public string DetectedStructure { get; init; } = string.Empty;
    
    [JsonPropertyName("expectedStructure")]
    public string ExpectedStructure { get; init; } = string.Empty;
    
    [JsonPropertyName("structuralIssues")]
    public string[] StructuralIssues { get; init; } = Array.Empty<string>();
    
    [JsonPropertyName("recommendations")]
    public string[] Recommendations { get; init; } = Array.Empty<string>();
    
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; init; } = string.Empty;
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""isValid"", ""detectedStructure"", ""expectedStructure"", ""reasoning""],
  ""properties"": {
    ""isValid"": { ""type"": ""boolean"" },
    ""detectedStructure"": { ""type"": ""string"", ""minLength"": 5 },
    ""expectedStructure"": { ""type"": ""string"", ""minLength"": 5 },
    ""structuralIssues"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""recommendations"": { ""type"": ""array"", ""items"": { ""type"": ""string"" } },
    ""reasoning"": { ""type"": ""string"", ""minLength"": 10 }
  }
}";
    }
}
