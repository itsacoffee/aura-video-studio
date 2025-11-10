using Aura.Core.Validation;
using Xunit;

namespace Aura.Tests.Validation;

public class ApiKeyValidatorTests
{
    [Theory]
    [InlineData("OPENAI_KEY", "sk-1234567890abcdefghij", true)]
    [InlineData("OPENAI_KEY", "sk-proj-1234567890abcdefghij", true)]
    [InlineData("OPENAI_KEY", "invalid-key", false)]
    [InlineData("OPENAI_KEY", "", false)]
    [InlineData("OPENAI_KEY", null, false)]
    public void ValidateKey_OpenAI_ValidatesCorrectly(string keyName, string? keyValue, bool shouldBeValid)
    {
        // Act
        var result = ApiKeyValidator.ValidateKey(keyName, keyValue);

        // Assert
        Assert.Equal(shouldBeValid, result.IsSuccess);
    }

    [Theory]
    [InlineData("ANTHROPIC_KEY", "sk-ant-1234567890abcdefghij", true)]
    [InlineData("ANTHROPIC_KEY", "sk-1234567890abcdefghij", false)]
    [InlineData("ANTHROPIC_KEY", "", false)]
    public void ValidateKey_Anthropic_ValidatesCorrectly(string keyName, string? keyValue, bool shouldBeValid)
    {
        // Act
        var result = ApiKeyValidator.ValidateKey(keyName, keyValue);

        // Assert
        Assert.Equal(shouldBeValid, result.IsSuccess);
    }

    [Fact]
    public void ValidateKey_WithWhitespace_ReturnsFailure()
    {
        // Arrange
        var keyName = "OPENAI_KEY";
        var keyValue = "sk-1234567890 abcdefghij"; // Space in middle

        // Act
        var result = ApiKeyValidator.ValidateKey(keyName, keyValue);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("whitespace", result.Message);
    }

    [Fact]
    public void ValidateKey_WithBearerPrefix_ReturnsFailure()
    {
        // Arrange
        var keyName = "OPENAI_KEY";
        var keyValue = "Bearer sk-1234567890abcdefghij";

        // Act
        var result = ApiKeyValidator.ValidateKey(keyName, keyValue);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Bearer", result.Message);
    }

    [Fact]
    public void ValidateKey_TooShort_ReturnsFailure()
    {
        // Arrange
        var keyName = "OPENAI_KEY";
        var keyValue = "sk-123"; // Too short

        // Act
        var result = ApiKeyValidator.ValidateKey(keyName, keyValue);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("short", result.Message);
    }

    [Fact]
    public void ValidateKeys_MultiplKeys_ReturnsAllResults()
    {
        // Arrange
        var keys = new Dictionary<string, string?>
        {
            ["OPENAI_KEY"] = "sk-1234567890abcdefghij",
            ["ANTHROPIC_KEY"] = "invalid",
            ["ELEVENLABS_KEY"] = null
        };

        // Act
        var results = ApiKeyValidator.ValidateKeys(keys);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.True(results["OPENAI_KEY"].IsSuccess);
        Assert.False(results["ANTHROPIC_KEY"].IsSuccess);
        Assert.False(results["ELEVENLABS_KEY"].IsSuccess);
    }

    [Theory]
    [InlineData("OpenAI", true)]
    [InlineData("Anthropic Claude", true)]
    [InlineData("Ollama (Local)", false)]
    [InlineData("RuleBased", false)]
    public void IsKeyRequired_ReturnsCorrectValue(string providerName, bool expected)
    {
        // Act
        var isRequired = ApiKeyValidator.IsKeyRequired(providerName);

        // Assert
        Assert.Equal(expected, isRequired);
    }

    [Theory]
    [InlineData("OpenAI", "OPENAI_KEY")]
    [InlineData("Anthropic Claude", "ANTHROPIC_KEY")]
    [InlineData("ElevenLabs", "ELEVENLABS_KEY")]
    [InlineData("Ollama (Local)", null)]
    public void GetKeyNameForProvider_ReturnsCorrectKeyName(string providerName, string? expectedKeyName)
    {
        // Act
        var keyName = ApiKeyValidator.GetKeyNameForProvider(providerName);

        // Assert
        Assert.Equal(expectedKeyName, keyName);
    }

    [Theory]
    [InlineData("sk-1234567890abcdefghijklmnop", "sk-1**********************mnop")]
    [InlineData("short", "******")]
    [InlineData("", "")]
    public void MaskKey_MasksCorrectly(string keyValue, string expected)
    {
        // Act
        var masked = ApiKeyValidator.MaskKey(keyValue);

        // Assert
        Assert.Equal(expected, masked);
    }

    [Fact]
    public void MaskKey_PreservesFirstAndLastFourCharacters()
    {
        // Arrange
        var keyValue = "sk-1234567890abcdefghijklmnop";

        // Act
        var masked = ApiKeyValidator.MaskKey(keyValue);

        // Assert
        Assert.StartsWith("sk-1", masked);
        Assert.EndsWith("mnop", masked);
        Assert.Contains("*", masked);
    }
}
