using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Aura.Core.AI.Validation;

namespace Aura.Core.AI.Orchestration;

/// <summary>
/// Base class for orchestration schemas with versioning support
/// </summary>
public abstract record OrchestrationSchema : LlmOutputSchema
{
    /// <summary>
    /// Schema version for compatibility tracking
    /// </summary>
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; init; } = "1.0";
    
    /// <summary>
    /// Model/provider metadata for reproducibility
    /// </summary>
    [JsonPropertyName("metadata")]
    public ArtifactMetadata? Metadata { get; init; }
}

/// <summary>
/// Metadata for tracking model, provider, and generation details
/// </summary>
public record ArtifactMetadata(
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("temperature")] double? Temperature,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp
);

/// <summary>
/// Schema for Plan generation (brief -> plan)
/// </summary>
public record PlanSchema : OrchestrationSchema
{
    public override string SchemaName => "Plan";
    
    [JsonPropertyName("outline")]
    public string Outline { get; init; } = string.Empty;
    
    [JsonPropertyName("sceneCount")]
    public int SceneCount { get; init; }
    
    [JsonPropertyName("estimatedDurationSeconds")]
    public double EstimatedDurationSeconds { get; init; }
    
    [JsonPropertyName("targetPacing")]
    public string TargetPacing { get; init; } = string.Empty;
    
    [JsonPropertyName("contentDensity")]
    public string ContentDensity { get; init; } = string.Empty;
    
    [JsonPropertyName("narrativeStructure")]
    public string NarrativeStructure { get; init; } = string.Empty;
    
    [JsonPropertyName("keyMessages")]
    public string[] KeyMessages { get; init; } = Array.Empty<string>();
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""outline"", ""sceneCount"", ""estimatedDurationSeconds"", ""targetPacing"", ""contentDensity"", ""narrativeStructure"", ""keyMessages"", ""schema_version""],
  ""properties"": {
    ""outline"": { ""type"": ""string"", ""minLength"": 50 },
    ""sceneCount"": { ""type"": ""integer"", ""minimum"": 1, ""maximum"": 50 },
    ""estimatedDurationSeconds"": { ""type"": ""number"", ""minimum"": 5.0, ""maximum"": 3600.0 },
    ""targetPacing"": { ""type"": ""string"", ""enum"": [""slow"", ""moderate"", ""fast"", ""dynamic""] },
    ""contentDensity"": { ""type"": ""string"", ""enum"": [""sparse"", ""moderate"", ""dense""] },
    ""narrativeStructure"": { ""type"": ""string"", ""minLength"": 10 },
    ""keyMessages"": { ""type"": ""array"", ""items"": { ""type"": ""string"", ""minLength"": 5 }, ""minItems"": 1, ""maxItems"": 10 },
    ""schema_version"": { ""type"": ""string"" },
    ""metadata"": { ""type"": ""object"" }
  }
}";
    }
}

/// <summary>
/// Schema for SceneBreakdown (plan -> scenes)
/// </summary>
public record SceneBreakdownSchema : OrchestrationSchema
{
    public override string SchemaName => "SceneBreakdown";
    
    [JsonPropertyName("scenes")]
    public SceneDetail[] Scenes { get; init; } = Array.Empty<SceneDetail>();
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""scenes"", ""schema_version""],
  ""properties"": {
    ""scenes"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""required"": [""index"", ""heading"", ""script"", ""durationSeconds"", ""purpose""],
        ""properties"": {
          ""index"": { ""type"": ""integer"", ""minimum"": 0 },
          ""heading"": { ""type"": ""string"", ""minLength"": 3 },
          ""script"": { ""type"": ""string"", ""minLength"": 10 },
          ""durationSeconds"": { ""type"": ""number"", ""minimum"": 1.0, ""maximum"": 300.0 },
          ""purpose"": { ""type"": ""string"", ""minLength"": 10 },
          ""visualNotes"": { ""type"": ""string"" },
          ""transitionType"": { ""type"": ""string"", ""enum"": [""cut"", ""fade"", ""dissolve"", ""wipe""] }
        }
      },
      ""minItems"": 1
    },
    ""schema_version"": { ""type"": ""string"" },
    ""metadata"": { ""type"": ""object"" }
  }
}";
    }
}

/// <summary>
/// Individual scene detail
/// </summary>
public record SceneDetail(
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("heading")] string Heading,
    [property: JsonPropertyName("script")] string Script,
    [property: JsonPropertyName("durationSeconds")] double DurationSeconds,
    [property: JsonPropertyName("purpose")] string Purpose,
    [property: JsonPropertyName("visualNotes")] string? VisualNotes,
    [property: JsonPropertyName("transitionType")] string? TransitionType
);

/// <summary>
/// Schema for VoiceStyle configuration (scenes -> voice)
/// </summary>
public record VoiceStyleSchema : OrchestrationSchema
{
    public override string SchemaName => "VoiceStyle";
    
    [JsonPropertyName("voiceCharacteristics")]
    public VoiceCharacteristics VoiceCharacteristics { get; init; } = new();
    
    [JsonPropertyName("pacingGuidelines")]
    public PacingGuidelines PacingGuidelines { get; init; } = new();
    
    [JsonPropertyName("emotionalTone")]
    public string EmotionalTone { get; init; } = string.Empty;
    
    [JsonPropertyName("emphasis")]
    public EmphasisPoint[] Emphasis { get; init; } = Array.Empty<EmphasisPoint>();
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""voiceCharacteristics"", ""pacingGuidelines"", ""emotionalTone"", ""schema_version""],
  ""properties"": {
    ""voiceCharacteristics"": {
      ""type"": ""object"",
      ""required"": [""rate"", ""pitch"", ""volume""],
      ""properties"": {
        ""rate"": { ""type"": ""number"", ""minimum"": 0.5, ""maximum"": 2.0 },
        ""pitch"": { ""type"": ""number"", ""minimum"": 0.5, ""maximum"": 2.0 },
        ""volume"": { ""type"": ""number"", ""minimum"": 0.0, ""maximum"": 1.0 }
      }
    },
    ""pacingGuidelines"": {
      ""type"": ""object"",
      ""required"": [""defaultPauseMs"", ""sentencePauseMs"", ""paragraphPauseMs""],
      ""properties"": {
        ""defaultPauseMs"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 2000 },
        ""sentencePauseMs"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 3000 },
        ""paragraphPauseMs"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 5000 }
      }
    },
    ""emotionalTone"": { ""type"": ""string"", ""minLength"": 3 },
    ""emphasis"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""required"": [""text"", ""strength""],
        ""properties"": {
          ""text"": { ""type"": ""string"", ""minLength"": 1 },
          ""strength"": { ""type"": ""string"", ""enum"": [""weak"", ""moderate"", ""strong""] }
        }
      }
    },
    ""schema_version"": { ""type"": ""string"" },
    ""metadata"": { ""type"": ""object"" }
  }
}";
    }
}

/// <summary>
/// Voice characteristics details
/// </summary>
public record VoiceCharacteristics(
    [property: JsonPropertyName("rate")] double Rate = 1.0,
    [property: JsonPropertyName("pitch")] double Pitch = 1.0,
    [property: JsonPropertyName("volume")] double Volume = 1.0
);

/// <summary>
/// Pacing guidelines details
/// </summary>
public record PacingGuidelines(
    [property: JsonPropertyName("defaultPauseMs")] int DefaultPauseMs = 300,
    [property: JsonPropertyName("sentencePauseMs")] int SentencePauseMs = 500,
    [property: JsonPropertyName("paragraphPauseMs")] int ParagraphPauseMs = 1000
);

/// <summary>
/// Emphasis point details
/// </summary>
public record EmphasisPoint(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("strength")] string Strength
);

/// <summary>
/// Schema for SSML specification (voice -> ssml)
/// </summary>
public record SSMLSpecSchema : OrchestrationSchema
{
    public override string SchemaName => "SSMLSpec";
    
    [JsonPropertyName("ssmlSegments")]
    public SSMLSegment[] SSMLSegments { get; init; } = Array.Empty<SSMLSegment>();
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""ssmlSegments"", ""schema_version""],
  ""properties"": {
    ""ssmlSegments"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""required"": [""sceneIndex"", ""text"", ""ssml""],
        ""properties"": {
          ""sceneIndex"": { ""type"": ""integer"", ""minimum"": 0 },
          ""text"": { ""type"": ""string"", ""minLength"": 1 },
          ""ssml"": { ""type"": ""string"", ""minLength"": 1 },
          ""estimatedDurationMs"": { ""type"": ""integer"", ""minimum"": 0 }
        }
      },
      ""minItems"": 1
    },
    ""schema_version"": { ""type"": ""string"" },
    ""metadata"": { ""type"": ""object"" }
  }
}";
    }
}

/// <summary>
/// SSML segment details
/// </summary>
public record SSMLSegment(
    [property: JsonPropertyName("sceneIndex")] int SceneIndex,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("ssml")] string SSML,
    [property: JsonPropertyName("estimatedDurationMs")] int? EstimatedDurationMs
);

/// <summary>
/// Schema for VisualPromptSpec (scenes -> visual prompts)
/// </summary>
public record VisualPromptSpecSchema : OrchestrationSchema
{
    public override string SchemaName => "VisualPromptSpec";
    
    [JsonPropertyName("visualPrompts")]
    public VisualPromptDetail[] VisualPrompts { get; init; } = Array.Empty<VisualPromptDetail>();
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""visualPrompts"", ""schema_version""],
  ""properties"": {
    ""visualPrompts"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""required"": [""sceneIndex"", ""prompt"", ""styleKeywords""],
        ""properties"": {
          ""sceneIndex"": { ""type"": ""integer"", ""minimum"": 0 },
          ""prompt"": { ""type"": ""string"", ""minLength"": 20 },
          ""styleKeywords"": { ""type"": ""array"", ""items"": { ""type"": ""string"" }, ""minItems"": 1 },
          ""negativePrompt"": { ""type"": ""string"" },
          ""aspectRatio"": { ""type"": ""string"" },
          ""compositionNotes"": { ""type"": ""string"" }
        }
      },
      ""minItems"": 1
    },
    ""schema_version"": { ""type"": ""string"" },
    ""metadata"": { ""type"": ""object"" }
  }
}";
    }
}

/// <summary>
/// Visual prompt details for a scene
/// </summary>
public record VisualPromptDetail(
    [property: JsonPropertyName("sceneIndex")] int SceneIndex,
    [property: JsonPropertyName("prompt")] string Prompt,
    [property: JsonPropertyName("styleKeywords")] string[] StyleKeywords,
    [property: JsonPropertyName("negativePrompt")] string? NegativePrompt,
    [property: JsonPropertyName("aspectRatio")] string? AspectRatio,
    [property: JsonPropertyName("compositionNotes")] string? CompositionNotes
);

/// <summary>
/// Schema for RenderTimeline (all assets -> render plan)
/// </summary>
public record RenderTimelineSchema : OrchestrationSchema
{
    public override string SchemaName => "RenderTimeline";
    
    [JsonPropertyName("timelineSegments")]
    public TimelineSegment[] TimelineSegments { get; init; } = Array.Empty<TimelineSegment>();
    
    [JsonPropertyName("totalDurationSeconds")]
    public double TotalDurationSeconds { get; init; }
    
    [JsonPropertyName("transitionPlan")]
    public TransitionPlan[] TransitionPlan { get; init; } = Array.Empty<TransitionPlan>();
    
    public override string GetSchemaDefinition()
    {
        return @"{
  ""type"": ""object"",
  ""required"": [""timelineSegments"", ""totalDurationSeconds"", ""schema_version""],
  ""properties"": {
    ""timelineSegments"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""required"": [""sceneIndex"", ""startTimeSeconds"", ""durationSeconds"", ""audioPath"", ""visualPath""],
        ""properties"": {
          ""sceneIndex"": { ""type"": ""integer"", ""minimum"": 0 },
          ""startTimeSeconds"": { ""type"": ""number"", ""minimum"": 0.0 },
          ""durationSeconds"": { ""type"": ""number"", ""minimum"": 0.1 },
          ""audioPath"": { ""type"": ""string"", ""minLength"": 1 },
          ""visualPath"": { ""type"": ""string"", ""minLength"": 1 },
          ""subtitlePath"": { ""type"": ""string"" }
        }
      },
      ""minItems"": 1
    },
    ""totalDurationSeconds"": { ""type"": ""number"", ""minimum"": 1.0 },
    ""transitionPlan"": {
      ""type"": ""array"",
      ""items"": {
        ""type"": ""object"",
        ""required"": [""fromSceneIndex"", ""toSceneIndex"", ""transitionType"", ""durationMs""],
        ""properties"": {
          ""fromSceneIndex"": { ""type"": ""integer"", ""minimum"": 0 },
          ""toSceneIndex"": { ""type"": ""integer"", ""minimum"": 0 },
          ""transitionType"": { ""type"": ""string"", ""enum"": [""cut"", ""fade"", ""dissolve"", ""wipe""] },
          ""durationMs"": { ""type"": ""integer"", ""minimum"": 0, ""maximum"": 2000 }
        }
      }
    },
    ""schema_version"": { ""type"": ""string"" },
    ""metadata"": { ""type"": ""object"" }
  }
}";
    }
}

/// <summary>
/// Timeline segment details
/// </summary>
public record TimelineSegment(
    [property: JsonPropertyName("sceneIndex")] int SceneIndex,
    [property: JsonPropertyName("startTimeSeconds")] double StartTimeSeconds,
    [property: JsonPropertyName("durationSeconds")] double DurationSeconds,
    [property: JsonPropertyName("audioPath")] string AudioPath,
    [property: JsonPropertyName("visualPath")] string VisualPath,
    [property: JsonPropertyName("subtitlePath")] string? SubtitlePath
);

/// <summary>
/// Transition plan details
/// </summary>
public record TransitionPlan(
    [property: JsonPropertyName("fromSceneIndex")] int FromSceneIndex,
    [property: JsonPropertyName("toSceneIndex")] int ToSceneIndex,
    [property: JsonPropertyName("transitionType")] string TransitionType,
    [property: JsonPropertyName("durationMs")] int DurationMs
);
