using System.Text.Json;
using Aura.Core.Models.Ollama;
using Xunit;

namespace Aura.Tests.Models.Ollama;

/// <summary>
/// Tests for OllamaStreamResponse deserialization with PropertyNameCaseInsensitive
/// Verifies the fix for translation streaming timeout issues
/// </summary>
public class OllamaStreamResponseDeserializationTests
{
    [Fact]
    public void Deserialize_WithCaseInsensitiveOption_DeserializesCorrectly()
    {
        // Arrange - Typical Ollama streaming response
        var json = @"{
            ""model"": ""llama2"",
            ""created_at"": ""2024-01-01T00:00:00Z"",
            ""response"": ""Hello"",
            ""done"": false
        }";

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act
        var result = JsonSerializer.Deserialize<OllamaStreamResponse>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("llama2", result.Model);
        Assert.Equal("Hello", result.Response);
        Assert.False(result.Done);
    }

    [Fact]
    public void Deserialize_FinalChunk_IncludesMetrics()
    {
        // Arrange - Final chunk with metrics
        var json = @"{
            ""model"": ""llama2"",
            ""created_at"": ""2024-01-01T00:00:00Z"",
            ""response"": """",
            ""done"": true,
            ""total_duration"": 5000000000,
            ""eval_count"": 100,
            ""eval_duration"": 4000000000
        }";

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act
        var result = JsonSerializer.Deserialize<OllamaStreamResponse>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Done);
        Assert.Equal(5000000000, result.TotalDuration);
        Assert.Equal(100, result.EvalCount);
        Assert.NotNull(result.GetTokensPerSecond());
    }

    [Fact]
    public void Deserialize_EmptyResponse_HandlesGracefully()
    {
        // Arrange - Empty response chunk (can happen during streaming)
        var json = @"{
            ""model"": ""llama2"",
            ""created_at"": ""2024-01-01T00:00:00Z"",
            ""response"": """",
            ""done"": false
        }";

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Act
        var result = JsonSerializer.Deserialize<OllamaStreamResponse>(json, options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("", result.Response);
        Assert.False(result.Done);
    }

    [Fact]
    public void Deserialize_WithoutCaseInsensitive_UsesDefaultCasing()
    {
        // Arrange - PascalCase JSON (shouldn't happen with Ollama but test for completeness)
        var json = @"{
            ""Model"": ""llama2"",
            ""CreatedAt"": ""2024-01-01T00:00:00Z"",
            ""Response"": ""Hello"",
            ""Done"": false
        }";

        // Act - Without case insensitive option
        var result = JsonSerializer.Deserialize<OllamaStreamResponse>(json);

        // Assert - Should still work because property names match
        Assert.NotNull(result);
        Assert.Equal("llama2", result.Model);
        Assert.Equal("Hello", result.Response);
        Assert.False(result.Done);
    }
}
