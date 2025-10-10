using Xunit;
using System.Text.Json;
using Aura.Core.Models;
using Aura.Api.Serialization;

namespace Aura.Tests;

/// <summary>
/// Tests for tolerant enum converters that accept both canonical names and aliases
/// </summary>
public class EnumConverterTests
{
    private readonly JsonSerializerOptions _options;

    public EnumConverterTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = { new TolerantDensityConverter(), new TolerantAspectConverter() }
        };
    }

    #region Density Converter Tests

    [Theory]
    [InlineData("\"Sparse\"", Density.Sparse)]
    [InlineData("\"Balanced\"", Density.Balanced)]
    [InlineData("\"Dense\"", Density.Dense)]
    [InlineData("\"Normal\"", Density.Balanced)] // Alias
    [InlineData("\"sparse\"", Density.Sparse)] // Case insensitive
    [InlineData("\"balanced\"", Density.Balanced)]
    [InlineData("\"dense\"", Density.Dense)]
    [InlineData("\"normal\"", Density.Balanced)] // Alias case insensitive
    public void DensityConverter_Should_ParseValidValues(string json, Density expected)
    {
        // Act
        var result = JsonSerializer.Deserialize<Density>(json, _options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"Invalid\"")]
    [InlineData("\"Medium\"")]
    public void DensityConverter_Should_ThrowOnInvalidValues(string json)
    {
        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<Density>(json, _options));
        
        Assert.Contains("Density value", exception.Message);
        Assert.Contains("Valid values are:", exception.Message);
    }

    [Fact]
    public void DensityConverter_Should_ThrowOnEmptyValue()
    {
        // Arrange
        var json = "\"\"";

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<Density>(json, _options));
        
        Assert.Contains("Density value cannot be empty", exception.Message);
    }

    [Fact]
    public void DensityConverter_Should_RoundTrip_CanonicalValues()
    {
        // Arrange
        var values = new[] { Density.Sparse, Density.Balanced, Density.Dense };

        foreach (var value in values)
        {
            // Act
            var json = JsonSerializer.Serialize(value, _options);
            var result = JsonSerializer.Deserialize<Density>(json, _options);

            // Assert
            Assert.Equal(value, result);
        }
    }

    #endregion

    #region Aspect Converter Tests

    [Theory]
    [InlineData("\"Widescreen16x9\"", Aspect.Widescreen16x9)]
    [InlineData("\"Vertical9x16\"", Aspect.Vertical9x16)]
    [InlineData("\"Square1x1\"", Aspect.Square1x1)]
    [InlineData("\"16:9\"", Aspect.Widescreen16x9)] // Alias
    [InlineData("\"9:16\"", Aspect.Vertical9x16)] // Alias
    [InlineData("\"1:1\"", Aspect.Square1x1)] // Alias
    [InlineData("\"widescreen16x9\"", Aspect.Widescreen16x9)] // Case insensitive
    [InlineData("\"vertical9x16\"", Aspect.Vertical9x16)]
    [InlineData("\"square1x1\"", Aspect.Square1x1)]
    public void AspectConverter_Should_ParseValidValues(string json, Aspect expected)
    {
        // Act
        var result = JsonSerializer.Deserialize<Aspect>(json, _options);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("\"Invalid\"")]
    [InlineData("\"4:3\"")]
    public void AspectConverter_Should_ThrowOnInvalidValues(string json)
    {
        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<Aspect>(json, _options));
        
        Assert.Contains("Aspect value", exception.Message);
        Assert.Contains("Valid values are:", exception.Message);
    }

    [Fact]
    public void AspectConverter_Should_ThrowOnEmptyValue()
    {
        // Arrange
        var json = "\"\"";

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<Aspect>(json, _options));
        
        Assert.Contains("Aspect value cannot be empty", exception.Message);
    }

    [Fact]
    public void AspectConverter_Should_RoundTrip_CanonicalValues()
    {
        // Arrange
        var values = new[] { Aspect.Widescreen16x9, Aspect.Vertical9x16, Aspect.Square1x1 };

        foreach (var value in values)
        {
            // Act
            var json = JsonSerializer.Serialize(value, _options);
            var result = JsonSerializer.Deserialize<Aspect>(json, _options);

            // Assert
            Assert.Equal(value, result);
        }
    }

    #endregion

    #region DTO Tests

    [Fact]
    public void ScriptRequest_Should_DeserializeWithAliases()
    {
        // Arrange
        var json = @"{
            ""topic"": ""Test Topic"",
            ""audience"": ""General"",
            ""goal"": ""Inform"",
            ""tone"": ""Informative"",
            ""language"": ""en-US"",
            ""aspect"": ""16:9"",
            ""targetDurationMinutes"": 3.0,
            ""pacing"": ""Conversational"",
            ""density"": ""Normal"",
            ""style"": ""Standard""
        }";

        var options = new JsonSerializerOptions
        {
            Converters = 
            { 
                new TolerantDensityConverter(), 
                new TolerantAspectConverter(),
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            },
            PropertyNameCaseInsensitive = true
        };

        // Act
        var request = JsonSerializer.Deserialize<ScriptRequestDto>(json, options);

        // Assert
        Assert.NotNull(request);
        Assert.Equal("Test Topic", request.Topic);
        Assert.Equal(Aspect.Widescreen16x9, request.Aspect);
        Assert.Equal(Density.Balanced, request.Density);
        Assert.Equal(Pacing.Conversational, request.Pacing);
    }

    [Fact]
    public void ScriptRequest_Should_Fail_WithInvalidEnumValues()
    {
        // Arrange
        var json = @"{
            ""topic"": ""Test Topic"",
            ""audience"": ""General"",
            ""goal"": ""Inform"",
            ""tone"": ""Informative"",
            ""language"": ""en-US"",
            ""aspect"": ""4:3"",
            ""targetDurationMinutes"": 3.0,
            ""pacing"": ""Conversational"",
            ""density"": ""Normal"",
            ""style"": ""Standard""
        }";

        var options = new JsonSerializerOptions
        {
            Converters = 
            { 
                new TolerantDensityConverter(), 
                new TolerantAspectConverter(),
                new System.Text.Json.Serialization.JsonStringEnumConverter()
            },
            PropertyNameCaseInsensitive = true
        };

        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<ScriptRequestDto>(json, options));
        
        Assert.Contains("Unknown Aspect value: '4:3'", exception.Message);
        Assert.Contains("Valid values are:", exception.Message);
    }

    #endregion
}

// Test DTO to mirror the actual request structure
record ScriptRequestDto(
    string Topic, 
    string Audience, 
    string Goal, 
    string Tone, 
    string Language, 
    Aspect Aspect, 
    double TargetDurationMinutes, 
    Pacing Pacing, 
    Density Density, 
    string Style,
    string? ProviderTier = null);
