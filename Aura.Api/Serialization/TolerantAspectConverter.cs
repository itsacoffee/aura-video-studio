using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Core.Models;

namespace Aura.Api.Serialization;

/// <summary>
/// JSON converter that tolerantly parses Aspect enum values, accepting both canonical names and aliases.
/// Canonical: "Widescreen16x9", "Vertical9x16", "Square1x1"
/// Aliases: "16:9" -> "Widescreen16x9", "9:16" -> "Vertical9x16", "1:1" -> "Square1x1"
/// </summary>
public class TolerantAspectConverter : JsonConverter<Aspect>
{
    public override Aspect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string value for Aspect, got {reader.TokenType}");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Aspect value cannot be empty");
        }

        // Try canonical names first (case-insensitive)
        if (Enum.TryParse<Aspect>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        // Handle aliases
        return value.Trim() switch
        {
            "16:9" => Aspect.Widescreen16x9,
            "9:16" => Aspect.Vertical9x16,
            "1:1" => Aspect.Square1x1,
            _ => throw new JsonException($"Unknown Aspect value: '{value}'. Valid values are: Widescreen16x9 (or 16:9), Vertical9x16 (or 9:16), Square1x1 (or 1:1)")
        };
    }

    public override void Write(Utf8JsonWriter writer, Aspect value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
