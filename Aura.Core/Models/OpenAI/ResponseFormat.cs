using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Aura.Core.Models.OpenAI;

/// <summary>
/// Represents the response format for OpenAI API requests
/// Used to enforce structured JSON outputs with schema validation
/// </summary>
public class ResponseFormat
{
    /// <summary>
    /// Type of response format. For structured outputs, use "json_schema"
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_schema";

    /// <summary>
    /// JSON schema definition for structured outputs
    /// </summary>
    [JsonPropertyName("json_schema")]
    public JsonSchemaDefinition? JsonSchema { get; set; }
}

/// <summary>
/// Definition of a JSON schema for OpenAI structured outputs
/// </summary>
public class JsonSchemaDefinition
{
    /// <summary>
    /// Name of the schema (e.g., "video_script")
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use strict mode for schema validation
    /// Strict mode ensures all fields are exactly as specified
    /// </summary>
    [JsonPropertyName("strict")]
    public bool Strict { get; set; }

    /// <summary>
    /// The JSON schema object defining the structure
    /// </summary>
    [JsonPropertyName("schema")]
    public JsonSchemaObject Schema { get; set; } = new();
}

/// <summary>
/// Represents a JSON schema object structure
/// </summary>
public class JsonSchemaObject
{
    /// <summary>
    /// Type of the schema object (typically "object")
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    /// <summary>
    /// Properties defined in the schema
    /// </summary>
    [JsonPropertyName("properties")]
    public Dictionary<string, JsonSchemaProperty> Properties { get; set; } = new();

    /// <summary>
    /// List of required property names
    /// </summary>
    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();

    /// <summary>
    /// Additional properties allowed in the schema
    /// </summary>
    [JsonPropertyName("additionalProperties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AdditionalProperties { get; set; }
}

/// <summary>
/// Represents a property definition in a JSON schema
/// </summary>
public class JsonSchemaProperty
{
    /// <summary>
    /// Type of the property (string, number, object, array, etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Description of the property
    /// </summary>
    [JsonPropertyName("description")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>
    /// For array types, defines the items structure
    /// </summary>
    [JsonPropertyName("items")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public JsonSchemaProperty? Items { get; set; }

    /// <summary>
    /// For object types, defines nested properties
    /// </summary>
    [JsonPropertyName("properties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, JsonSchemaProperty>? Properties { get; set; }

    /// <summary>
    /// For object types, list of required nested properties
    /// </summary>
    [JsonPropertyName("required")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Required { get; set; }

    /// <summary>
    /// For string types with limited values, enum of allowed values
    /// </summary>
    [JsonPropertyName("enum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Enum { get; set; }

    /// <summary>
    /// Minimum value for number types
    /// </summary>
    [JsonPropertyName("minimum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Minimum { get; set; }

    /// <summary>
    /// Maximum value for number types
    /// </summary>
    [JsonPropertyName("maximum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? Maximum { get; set; }

    /// <summary>
    /// Additional properties allowed for nested objects
    /// </summary>
    [JsonPropertyName("additionalProperties")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? AdditionalProperties { get; set; }
}
