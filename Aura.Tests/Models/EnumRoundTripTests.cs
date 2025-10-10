using Xunit;
using System.Text.Json;
using ApiV1 = Aura.Api.Models.ApiModels.V1;
using Aura.Api.Serialization;

namespace Aura.Tests.Models;

/// <summary>
/// Tests that every enum value in API V1 correctly round-trips through JSON serialization
/// </summary>
public class EnumRoundTripTests
{
    private readonly JsonSerializerOptions _options;

    public EnumRoundTripTests()
    {
        _options = new JsonSerializerOptions
        {
            Converters = 
            { 
                new TolerantPacingConverter(),
                new TolerantDensityConverterV1()
            }
        };
    }

    #region Pacing Round-Trip Tests

    [Fact]
    public void Pacing_RoundTrip_Chill()
    {
        // Arrange
        var value = ApiV1.Pacing.Chill;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.Pacing>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Chill\"", json);
    }

    [Fact]
    public void Pacing_RoundTrip_Conversational()
    {
        // Arrange
        var value = ApiV1.Pacing.Conversational;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.Pacing>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Conversational\"", json);
    }

    [Fact]
    public void Pacing_RoundTrip_Fast()
    {
        // Arrange
        var value = ApiV1.Pacing.Fast;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.Pacing>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Fast\"", json);
    }

    [Fact]
    public void Pacing_RoundTrip_AllValues()
    {
        // Arrange
        var values = new[] { ApiV1.Pacing.Chill, ApiV1.Pacing.Conversational, ApiV1.Pacing.Fast };

        foreach (var value in values)
        {
            // Act
            var json = JsonSerializer.Serialize(value, _options);
            var result = JsonSerializer.Deserialize<ApiV1.Pacing>(json, _options);

            // Assert
            Assert.Equal(value, result);
        }
    }

    #endregion

    #region Density Round-Trip Tests

    [Fact]
    public void Density_RoundTrip_Sparse()
    {
        // Arrange
        var value = ApiV1.Density.Sparse;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.Density>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Sparse\"", json);
    }

    [Fact]
    public void Density_RoundTrip_Balanced()
    {
        // Arrange
        var value = ApiV1.Density.Balanced;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.Density>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Balanced\"", json);
    }

    [Fact]
    public void Density_RoundTrip_Dense()
    {
        // Arrange
        var value = ApiV1.Density.Dense;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.Density>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Dense\"", json);
    }

    [Fact]
    public void Density_RoundTrip_AllValues()
    {
        // Arrange
        var values = new[] { ApiV1.Density.Sparse, ApiV1.Density.Balanced, ApiV1.Density.Dense };

        foreach (var value in values)
        {
            // Act
            var json = JsonSerializer.Serialize(value, _options);
            var result = JsonSerializer.Deserialize<ApiV1.Density>(json, _options);

            // Assert
            Assert.Equal(value, result);
        }
    }

    [Fact]
    public void Density_ParseAlias_Normal()
    {
        // Arrange
        var json = "\"Normal\"";

        // Act
        var result = JsonSerializer.Deserialize<ApiV1.Density>(json, _options);

        // Assert
        Assert.Equal(ApiV1.Density.Balanced, result);
    }

    #endregion

    #region Aspect Round-Trip Tests

    [Fact]
    public void Aspect_RoundTrip_Widescreen16x9()
    {
        // Arrange
        var value = ApiV1.Aspect.Widescreen16x9;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.Aspect>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Widescreen16x9\"", json);
    }

    [Fact]
    public void Aspect_RoundTrip_Vertical9x16()
    {
        // Arrange
        var value = ApiV1.Aspect.Vertical9x16;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.Aspect>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Vertical9x16\"", json);
    }

    [Fact]
    public void Aspect_RoundTrip_Square1x1()
    {
        // Arrange
        var value = ApiV1.Aspect.Square1x1;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.Aspect>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Square1x1\"", json);
    }

    [Fact]
    public void Aspect_RoundTrip_AllValues()
    {
        // Arrange
        var values = new[] { ApiV1.Aspect.Widescreen16x9, ApiV1.Aspect.Vertical9x16, ApiV1.Aspect.Square1x1 };

        foreach (var value in values)
        {
            // Act
            var json = JsonSerializer.Serialize(value, _options);
            var result = JsonSerializer.Deserialize<ApiV1.Aspect>(json, _options);

            // Assert
            Assert.Equal(value, result);
        }
    }

    [Theory]
    [InlineData("\"16:9\"", ApiV1.Aspect.Widescreen16x9)]
    [InlineData("\"9:16\"", ApiV1.Aspect.Vertical9x16)]
    [InlineData("\"1:1\"", ApiV1.Aspect.Square1x1)]
    public void Aspect_ParseAlias(string json, ApiV1.Aspect expected)
    {
        // Act
        var result = JsonSerializer.Deserialize<ApiV1.Aspect>(json, _options);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region PauseStyle Round-Trip Tests

    [Fact]
    public void PauseStyle_RoundTrip_Natural()
    {
        // Arrange
        var value = ApiV1.PauseStyle.Natural;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.PauseStyle>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Natural\"", json);
    }

    [Fact]
    public void PauseStyle_RoundTrip_Short()
    {
        // Arrange
        var value = ApiV1.PauseStyle.Short;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.PauseStyle>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Short\"", json);
    }

    [Fact]
    public void PauseStyle_RoundTrip_Long()
    {
        // Arrange
        var value = ApiV1.PauseStyle.Long;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.PauseStyle>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Long\"", json);
    }

    [Fact]
    public void PauseStyle_RoundTrip_Dramatic()
    {
        // Arrange
        var value = ApiV1.PauseStyle.Dramatic;

        // Act
        var json = JsonSerializer.Serialize(value, _options);
        var result = JsonSerializer.Deserialize<ApiV1.PauseStyle>(json, _options);

        // Assert
        Assert.Equal(value, result);
        Assert.Equal("\"Dramatic\"", json);
    }

    [Fact]
    public void PauseStyle_RoundTrip_AllValues()
    {
        // Arrange
        var values = new[] { ApiV1.PauseStyle.Natural, ApiV1.PauseStyle.Short, ApiV1.PauseStyle.Long, ApiV1.PauseStyle.Dramatic };

        foreach (var value in values)
        {
            // Act
            var json = JsonSerializer.Serialize(value, _options);
            var result = JsonSerializer.Deserialize<ApiV1.PauseStyle>(json, _options);

            // Assert
            Assert.Equal(value, result);
        }
    }

    #endregion

    #region Invalid Value Tests

    [Theory]
    [InlineData("\"InvalidPacing\"")]
    [InlineData("\"Slow\"")]
    public void Pacing_ThrowsOnInvalidValue(string json)
    {
        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<ApiV1.Pacing>(json, _options));
        
        Assert.Contains("Unknown Pacing value", exception.Message);
        Assert.Contains("Valid values are:", exception.Message);
    }

    [Theory]
    [InlineData("\"InvalidDensity\"")]
    [InlineData("\"Medium\"")]
    public void Density_ThrowsOnInvalidValue(string json)
    {
        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<ApiV1.Density>(json, _options));
        
        Assert.Contains("Unknown Density value", exception.Message);
        Assert.Contains("Valid values are:", exception.Message);
    }

    [Theory]
    [InlineData("\"InvalidAspect\"")]
    [InlineData("\"4:3\"")]
    public void Aspect_ThrowsOnInvalidValue(string json)
    {
        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<ApiV1.Aspect>(json, _options));
        
        Assert.Contains("Unknown Aspect value", exception.Message);
        Assert.Contains("Valid values are:", exception.Message);
    }

    [Theory]
    [InlineData("\"InvalidPauseStyle\"")]
    [InlineData("\"Medium\"")]
    public void PauseStyle_ThrowsOnInvalidValue(string json)
    {
        // Act & Assert
        var exception = Assert.Throws<JsonException>(() => 
            JsonSerializer.Deserialize<ApiV1.PauseStyle>(json, _options));
        
        Assert.Contains("Unknown PauseStyle value", exception.Message);
        Assert.Contains("Valid values are:", exception.Message);
    }

    #endregion
}
