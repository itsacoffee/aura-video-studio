using System.Reflection;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for the ExtractNarration method in BaseLlmScriptProvider.
/// Verifies that narration text is correctly extracted and cleaned from scene content.
/// </summary>
public class ExtractNarrationTests
{
    private readonly RuleBasedLlmProvider _provider;
    private readonly MethodInfo _extractNarrationMethod;

    public ExtractNarrationTests()
    {
        _provider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        
        // Get the protected ExtractNarration method via reflection
        _extractNarrationMethod = typeof(BaseLlmScriptProvider).GetMethod(
            "ExtractNarration",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
    }

    private string InvokeExtractNarration(string content)
    {
        return (string)_extractNarrationMethod.Invoke(_provider, new object[] { content })!;
    }

    [Fact]
    public void ExtractNarration_WithSimpleNarrationFormat_ReturnsCleanNarration()
    {
        // Arrange
        var content = "Narration: Welcome to our tutorial. Visual: Classroom setting.";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert
        Assert.Equal("Welcome to our tutorial.", result);
    }

    [Fact]
    public void ExtractNarration_WithTransitionMarker_StopsAtTransition()
    {
        // Arrange
        var content = "Narration: This is the main content. Transition: Fade";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert
        Assert.Equal("This is the main content.", result);
    }

    [Fact]
    public void ExtractNarration_WithVisualMetadataMarker_CleansMarkerCorrectly()
    {
        // Arrange - Content with [VISUAL:] marker that should be cleaned
        var content = "Narration: [VISUAL: intro] Here is the content. Visual: diagram";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert - The [VISUAL:] marker should be removed by CleanNarration
        Assert.Equal("Here is the content.", result);
    }

    [Fact]
    public void ExtractNarration_WithMultipleMediaMarkers_RemovesAllMarkers()
    {
        // Arrange - Content with multiple marker types
        var content = "Narration: [MUSIC bg] Welcome [PAUSE 1s] to [SFX ding] the show! Visual: stage";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert - All markers should be removed
        Assert.DoesNotContain("[MUSIC", result);
        Assert.DoesNotContain("[PAUSE", result);
        Assert.DoesNotContain("[SFX", result);
        Assert.Equal("Welcome to the show!", result);
    }

    [Fact]
    public void ExtractNarration_WithNoNarrationLabel_ReturnsCleanedContent()
    {
        // Arrange - Content without Narration: prefix (fallback behavior)
        var content = "Just regular content here.";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert - Should return the cleaned content
        Assert.Equal("Just regular content here.", result);
    }

    [Fact]
    public void ExtractNarration_WithMultilineContent_CapturesAllLines()
    {
        // Arrange - Multiline narration content
        var content = @"Narration: Welcome to the tutorial.
This is an important concept.
Let me explain further. Visual: Educational diagram.";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert - Should capture all narration lines up to Visual:
        Assert.Contains("Welcome to the tutorial.", result);
        Assert.Contains("important concept", result);
        Assert.Contains("explain further", result);
        Assert.DoesNotContain("Visual:", result);
    }

    [Fact]
    public void ExtractNarration_CaseInsensitive_WorksWithDifferentCases()
    {
        // Arrange
        var content = "NARRATION: This works with uppercase. VISUAL: description";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert
        Assert.Equal("This works with uppercase.", result);
    }

    [Fact]
    public void ExtractNarration_WithVisualAndTransition_StopsAtFirst()
    {
        // Arrange - Content with both Visual and Transition
        var content = "Narration: Main content here. Transition: Cut Visual: something";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert - Should stop at Transition: which comes first
        Assert.Equal("Main content here.", result);
        Assert.DoesNotContain("Transition:", result);
        Assert.DoesNotContain("Visual:", result);
    }

    [Fact]
    public void ExtractNarration_WithExtraWhitespace_TrimsAndNormalizes()
    {
        // Arrange - Content with extra whitespace
        var content = "Narration:    This has    extra   spaces.    Visual: desc";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert - Should have normalized whitespace
        Assert.Equal("This has extra spaces.", result);
    }

    [Fact]
    public void ExtractNarration_WithFadeMarker_RemovesFadeMarker()
    {
        // Arrange
        var content = "Narration: Welcome [FADE in] to the show. Visual: stage";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert
        Assert.DoesNotContain("[FADE", result);
        Assert.Contains("Welcome", result);
        Assert.Contains("to the show", result);
    }

    [Fact]
    public void ExtractNarration_WithCutMarker_RemovesCutMarker()
    {
        // Arrange
        var content = "Narration: First part [CUT] second part. Visual: scene";

        // Act
        var result = InvokeExtractNarration(content);

        // Assert
        Assert.DoesNotContain("[CUT]", result);
        Assert.Contains("First part", result);
        Assert.Contains("second part", result);
    }
}
