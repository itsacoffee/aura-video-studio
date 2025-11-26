using Aura.Core.Configuration;
using Xunit;

namespace Aura.Tests.Configuration;

/// <summary>
/// Tests for the OperatingMode enum and helper class
/// </summary>
public class OperatingModeTests
{
    [Theory]
    [InlineData("Ollama", "llm", true)]
    [InlineData("RuleBased", "llm", true)]
    [InlineData("OpenAI", "llm", false)]
    [InlineData("Anthropic", "llm", false)]
    [InlineData("Gemini", "llm", false)]
    [InlineData("Azure", "llm", false)]
    public void IsOfflineProvider_Llm_ReturnsCorrectResult(string providerName, string providerType, bool expected)
    {
        // Act
        var result = OperatingModeHelper.IsOfflineProvider(providerName, providerType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Windows", "tts", true)]
    [InlineData("WindowsSAPI", "tts", true)]
    [InlineData("Piper", "tts", true)]
    [InlineData("Mimic3", "tts", true)]
    [InlineData("ElevenLabs", "tts", false)]
    [InlineData("PlayHT", "tts", false)]
    [InlineData("Azure", "tts", false)]
    public void IsOfflineProvider_Tts_ReturnsCorrectResult(string providerName, string providerType, bool expected)
    {
        // Act
        var result = OperatingModeHelper.IsOfflineProvider(providerName, providerType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Placeholder", "images", true)]
    [InlineData("Stock", "images", false)]
    [InlineData("LocalSD", "images", false)]
    [InlineData("CloudPro", "images", false)]
    public void IsOfflineProvider_Images_ReturnsCorrectResult(string providerName, string providerType, bool expected)
    {
        // Act
        var result = OperatingModeHelper.IsOfflineProvider(providerName, providerType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("script")]
    [InlineData("voice")]
    [InlineData("visuals")]
    public void IsOfflineProvider_AlternateTypeNames_Works(string providerType)
    {
        // Script/llm, voice/tts, visuals/images should work with alternate names
        var llmResult = OperatingModeHelper.IsOfflineProvider("Ollama", providerType);
        var ttsResult = OperatingModeHelper.IsOfflineProvider("Windows", providerType);
        var imageResult = OperatingModeHelper.IsOfflineProvider("Placeholder", providerType);

        // At least one should be true based on the type
        Assert.True(llmResult || ttsResult || imageResult);
    }

    [Fact]
    public void IsOfflineProvider_NullProvider_ReturnsFalse()
    {
        // Act
        var result = OperatingModeHelper.IsOfflineProvider(null!, "llm");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsOfflineProvider_EmptyProvider_ReturnsFalse()
    {
        // Act
        var result = OperatingModeHelper.IsOfflineProvider("", "llm");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsOfflineProvider_InvalidType_ReturnsFalse()
    {
        // Act
        var result = OperatingModeHelper.IsOfflineProvider("Ollama", "invalid");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsOfflineProvider_CaseInsensitive()
    {
        // Act & Assert
        Assert.True(OperatingModeHelper.IsOfflineProvider("OLLAMA", "llm"));
        Assert.True(OperatingModeHelper.IsOfflineProvider("ollama", "llm"));
        Assert.True(OperatingModeHelper.IsOfflineProvider("Ollama", "LLM"));
    }

    [Fact]
    public void FilterOfflineProviders_Llm_ReturnsOnlyLocalProviders()
    {
        // Arrange
        var providers = new[] { "OpenAI", "Ollama", "Anthropic", "RuleBased", "Gemini" };

        // Act
        var result = OperatingModeHelper.FilterOfflineProviders(providers, "llm");

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Contains("Ollama", result);
        Assert.Contains("RuleBased", result);
    }

    [Fact]
    public void FilterOfflineProviders_Tts_ReturnsOnlyLocalProviders()
    {
        // Arrange
        var providers = new[] { "ElevenLabs", "Windows", "PlayHT", "Piper", "Mimic3" };

        // Act
        var result = OperatingModeHelper.FilterOfflineProviders(providers, "tts");

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Contains("Windows", result);
        Assert.Contains("Piper", result);
        Assert.Contains("Mimic3", result);
    }

    [Fact]
    public void FilterOfflineProviders_Images_ReturnsOnlyPlaceholder()
    {
        // Arrange
        var providers = new[] { "Stock", "Placeholder", "LocalSD", "CloudPro" };

        // Act
        var result = OperatingModeHelper.FilterOfflineProviders(providers, "images");

        // Assert
        Assert.Single(result);
        Assert.Contains("Placeholder", result);
    }

    [Fact]
    public void FilterOfflineProviders_EmptyArray_ReturnsEmpty()
    {
        // Act
        var result = OperatingModeHelper.FilterOfflineProviders(Array.Empty<string>(), "llm");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void FilterOfflineProviders_NullArray_ReturnsEmpty()
    {
        // Act
        var result = OperatingModeHelper.FilterOfflineProviders(null!, "llm");

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData("llm", "Ollama")]
    [InlineData("script", "Ollama")]
    [InlineData("tts", "Windows")]
    [InlineData("voice", "Windows")]
    [InlineData("images", "Placeholder")]
    [InlineData("visuals", "Placeholder")]
    public void GetDefaultOfflineProvider_ReturnsCorrectProvider(string providerType, string expected)
    {
        // Act
        var result = OperatingModeHelper.GetDefaultOfflineProvider(providerType);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetDefaultOfflineProvider_UnknownType_ReturnsEmpty()
    {
        // Act
        var result = OperatingModeHelper.GetDefaultOfflineProvider("unknown");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(true, OperatingMode.Offline)]
    [InlineData(false, OperatingMode.Online)]
    public void FromSettings_ReturnsCorrectMode(bool offlineModeSetting, OperatingMode expected)
    {
        // Act
        var result = OperatingModeHelper.FromSettings(offlineModeSetting);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void OfflineLlmProviders_ContainsExpectedProviders()
    {
        // Assert
        Assert.Contains("Ollama", OperatingModeHelper.OfflineLlmProviders);
        Assert.Contains("RuleBased", OperatingModeHelper.OfflineLlmProviders);
        Assert.Equal(2, OperatingModeHelper.OfflineLlmProviders.Length);
    }

    [Fact]
    public void OfflineTtsProviders_ContainsExpectedProviders()
    {
        // Assert
        Assert.Contains("Windows", OperatingModeHelper.OfflineTtsProviders);
        Assert.Contains("WindowsSAPI", OperatingModeHelper.OfflineTtsProviders);
        Assert.Contains("Piper", OperatingModeHelper.OfflineTtsProviders);
        Assert.Contains("Mimic3", OperatingModeHelper.OfflineTtsProviders);
        Assert.Equal(4, OperatingModeHelper.OfflineTtsProviders.Length);
    }

    [Fact]
    public void OfflineImageProviders_ContainsExpectedProviders()
    {
        // Assert
        Assert.Contains("Placeholder", OperatingModeHelper.OfflineImageProviders);
        Assert.Single(OperatingModeHelper.OfflineImageProviders);
    }
}
