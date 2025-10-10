using System.Text.Json.Serialization;
using Aura.Core.Models;

namespace Aura.Api.Serialization;

/// <summary>
/// Aggregator file that re-exports all tolerant enum converters for convenient registration.
/// This ensures consistent enum handling across the API with support for both canonical names and legacy aliases.
/// 
/// Supported conversions:
/// - Aspect: Widescreen16x9 (or 16:9), Vertical9x16 (or 9:16), Square1x1 (or 1:1)
/// - Density: Sparse, Balanced (or Normal), Dense
/// - Pacing: Uses standard JsonStringEnumConverter (case-insensitive)
/// 
/// Error codes for enum validation failures:
/// - E303: Invalid enum value provided
/// </summary>
public static class EnumJsonConverters
{
    /// <summary>
    /// Gets all custom enum converters that should be registered in the JSON serialization options.
    /// </summary>
    public static JsonConverter[] GetConverters()
    {
        return new JsonConverter[]
        {
            new TolerantDensityConverter(),
            new TolerantAspectConverter(),
            // Pacing uses standard JsonStringEnumConverter for case-insensitive parsing
            new JsonStringEnumConverter()
        };
    }

    /// <summary>
    /// Adds all tolerant enum converters to the provided options.
    /// </summary>
    public static void AddToOptions(System.Text.Json.JsonSerializerOptions options)
    {
        foreach (var converter in GetConverters())
        {
            options.Converters.Add(converter);
        }
        options.PropertyNameCaseInsensitive = true;
    }
}
