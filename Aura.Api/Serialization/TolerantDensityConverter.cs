using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aura.Core.Models;

namespace Aura.Api.Serialization;

/// <summary>
/// JSON converter that tolerantly parses Density enum values, accepting both canonical names and aliases.
/// Canonical: "Sparse", "Balanced", "Dense"
/// Alias: "Normal" -> "Balanced"
/// </summary>
public class TolerantDensityConverter : JsonConverter<Density>
{
    public override Density Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string value for Density, got {reader.TokenType}");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Density value cannot be empty");
        }

        // Try canonical names first (case-insensitive)
        if (Enum.TryParse<Density>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        // Handle aliases
        return value.Trim().ToLowerInvariant() switch
        {
            "normal" => Density.Balanced,
            _ => throw new JsonException($"Unknown Density value: '{value}'. Valid values are: Sparse, Balanced (or Normal), Dense")
        };
    }

    public override void Write(Utf8JsonWriter writer, Density value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
