using System.Collections.Generic;
using Aura.Core.Models.OpenAI;

namespace Aura.Core.AI.SchemaBuilders;

/// <summary>
/// Builds JSON schemas for video script generation with OpenAI structured outputs
/// Ensures consistent and reliable script structure from LLM responses
/// </summary>
public static class ScriptSchemaBuilder
{
    /// <summary>
    /// Gets the JSON schema for video script generation
    /// Enforces strict structure with required fields for title, hook, scenes, callToAction, and totalDuration
    /// </summary>
    /// <returns>ResponseFormat object with complete script schema definition</returns>
    public static ResponseFormat GetScriptSchema()
    {
        return new ResponseFormat
        {
            Type = "json_schema",
            JsonSchema = new JsonSchemaDefinition
            {
                Name = "video_script",
                Strict = true,
                Schema = new JsonSchemaObject
                {
                    Type = "object",
                    Properties = new Dictionary<string, JsonSchemaProperty>
                    {
                        ["title"] = new JsonSchemaProperty
                        {
                            Type = "string",
                            Description = "The title of the video script"
                        },
                        ["hook"] = new JsonSchemaProperty
                        {
                            Type = "string",
                            Description = "Opening hook to capture viewer attention (1-2 sentences)"
                        },
                        ["scenes"] = new JsonSchemaProperty
                        {
                            Type = "array",
                            Description = "Array of scenes that make up the video script",
                            Items = new JsonSchemaProperty
                            {
                                Type = "object",
                                Properties = new Dictionary<string, JsonSchemaProperty>
                                {
                                    ["narration"] = new JsonSchemaProperty
                                    {
                                        Type = "string",
                                        Description = "The spoken narration text for this scene"
                                    },
                                    ["visualDescription"] = new JsonSchemaProperty
                                    {
                                        Type = "string",
                                        Description = "Description of what should be shown visually during this scene"
                                    },
                                    ["duration"] = new JsonSchemaProperty
                                    {
                                        Type = "number",
                                        Description = "Duration of the scene in seconds",
                                        Minimum = 1.0,
                                        Maximum = 60.0
                                    },
                                    ["transition"] = new JsonSchemaProperty
                                    {
                                        Type = "string",
                                        Description = "Type of transition to the next scene",
                                        Enum = new List<string> { "cut", "fade", "dissolve", "wipe", "slide", "zoom" }
                                    }
                                },
                                Required = new List<string> { "narration", "visualDescription", "duration", "transition" },
                                AdditionalProperties = false
                            }
                        },
                        ["callToAction"] = new JsonSchemaProperty
                        {
                            Type = "string",
                            Description = "Closing call-to-action statement (1-2 sentences)"
                        },
                        ["totalDuration"] = new JsonSchemaProperty
                        {
                            Type = "number",
                            Description = "Total duration of the script in seconds",
                            Minimum = 10.0
                        }
                    },
                    Required = new List<string> { "title", "hook", "scenes", "callToAction", "totalDuration" },
                    AdditionalProperties = false
                }
            }
        };
    }
}
