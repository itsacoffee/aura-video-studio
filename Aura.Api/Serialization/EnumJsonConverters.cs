using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApiV1 = Aura.Api.Models.ApiModels.V1;

namespace Aura.Api.Serialization;

/// <summary>
/// Consolidated JSON converters for API V1 enums with tolerant parsing
/// Accepts both canonical names and legacy aliases for backward compatibility
/// </summary>

/// <summary>
/// JSON converter for Pacing enum
/// Canonical: "Chill", "Conversational", "Fast"
/// </summary>
public class TolerantPacingConverter : JsonConverter<ApiV1.Pacing>
{
    public override ApiV1.Pacing Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string value for Pacing, got {reader.TokenType}");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Pacing value cannot be empty");
        }

        // Try canonical names first (case-insensitive)
        if (Enum.TryParse<ApiV1.Pacing>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new JsonException($"Unknown Pacing value: '{value}'. Valid values are: Chill, Conversational, Fast");
    }

    public override void Write(Utf8JsonWriter writer, ApiV1.Pacing value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// JSON converter for Density enum
/// Canonical: "Sparse", "Balanced", "Dense"
/// Alias: "Normal" -> "Balanced"
/// </summary>
public class TolerantDensityConverterV1 : JsonConverter<ApiV1.Density>
{
    public override ApiV1.Density Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
        if (Enum.TryParse<ApiV1.Density>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        // Handle aliases
        return value.Trim().ToLowerInvariant() switch
        {
            "normal" => ApiV1.Density.Balanced,
            _ => throw new JsonException($"Unknown Density value: '{value}'. Valid values are: Sparse, Balanced (or Normal), Dense")
        };
    }

    public override void Write(Utf8JsonWriter writer, ApiV1.Density value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// JSON converter for Aspect enum
/// Canonical: "Widescreen16x9", "Vertical9x16", "Square1x1"
/// Aliases: "16:9" -> "Widescreen16x9", "9:16" -> "Vertical9x16", "1:1" -> "Square1x1"
/// </summary>
public class TolerantAspectConverterV1 : JsonConverter<ApiV1.Aspect>
{
    public override ApiV1.Aspect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
        if (Enum.TryParse<ApiV1.Aspect>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        // Handle aliases
        return value.Trim() switch
        {
            "16:9" => ApiV1.Aspect.Widescreen16x9,
            "9:16" => ApiV1.Aspect.Vertical9x16,
            "1:1" => ApiV1.Aspect.Square1x1,
            _ => throw new JsonException($"Unknown Aspect value: '{value}'. Valid values are: Widescreen16x9 (or 16:9), Vertical9x16 (or 9:16), Square1x1 (or 1:1)")
        };
    }

    public override void Write(Utf8JsonWriter writer, ApiV1.Aspect value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// JSON converter for PauseStyle enum
/// Canonical: "Natural", "Short", "Long", "Dramatic"
/// </summary>
public class TolerantPauseStyleConverter : JsonConverter<ApiV1.PauseStyle>
{
    public override ApiV1.PauseStyle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException($"Expected string value for PauseStyle, got {reader.TokenType}");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("PauseStyle value cannot be empty");
        }

        // Try canonical names first (case-insensitive)
        if (Enum.TryParse<ApiV1.PauseStyle>(value, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new JsonException($"Unknown PauseStyle value: '{value}'. Valid values are: Natural, Short, Long, Dramatic");
    }

    public override void Write(Utf8JsonWriter writer, ApiV1.PauseStyle value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
