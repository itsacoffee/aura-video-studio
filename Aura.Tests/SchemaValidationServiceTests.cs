using System;
using System.Linq;
using Aura.Core.Services.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for SchemaValidationService to ensure correct validation of script JSON
/// </summary>
public class SchemaValidationServiceTests
{
    private readonly SchemaValidationService _validationService;

    public SchemaValidationServiceTests()
    {
        _validationService = new SchemaValidationService(
            NullLogger<SchemaValidationService>.Instance);
    }

    [Fact]
    public void ValidateScriptJson_ValidScript_ReturnsValid()
    {
        // Arrange
        var validScript = @"{
            ""title"": ""Introduction to AI"",
            ""hook"": ""Discover how AI is transforming our world today!"",
            ""scenes"": [
                {
                    ""narration"": ""Artificial Intelligence is everywhere."",
                    ""visualDescription"": ""Show modern city with AI technology"",
                    ""duration"": 5.0,
                    ""transition"": ""fade""
                },
                {
                    ""narration"": ""From smartphones to self-driving cars."",
                    ""visualDescription"": ""Display various AI applications"",
                    ""duration"": 6.0,
                    ""transition"": ""dissolve""
                }
            ],
            ""callToAction"": ""Learn more about AI in our next video!"",
            ""totalDuration"": 15.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(validScript);

        // Assert
        Assert.True(result.IsValid, $"Validation failed: {string.Join(", ", result.Errors)}");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateScriptJson_NullOrEmpty_ReturnsInvalid()
    {
        // Act
        var result1 = _validationService.ValidateScriptJson(null!);
        var result2 = _validationService.ValidateScriptJson("");
        var result3 = _validationService.ValidateScriptJson("   ");

        // Assert
        Assert.False(result1.IsValid);
        Assert.False(result2.IsValid);
        Assert.False(result3.IsValid);
        Assert.Contains(result1.Errors, e => e.Contains("null or empty"));
    }

    [Fact]
    public void ValidateScriptJson_InvalidJson_ReturnsInvalid()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";

        // Act
        var result = _validationService.ValidateScriptJson(invalidJson);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Invalid JSON"));
    }

    [Fact]
    public void ValidateScriptJson_MissingTitle_ReturnsInvalid()
    {
        // Arrange
        var scriptWithoutTitle = @"{
            ""hook"": ""Test hook"",
            ""scenes"": [],
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 10.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithoutTitle);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("'title'") && e.Contains("missing"));
    }

    [Fact]
    public void ValidateScriptJson_MissingHook_ReturnsInvalid()
    {
        // Arrange
        var scriptWithoutHook = @"{
            ""title"": ""Test Title"",
            ""scenes"": [],
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 10.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithoutHook);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("'hook'") && e.Contains("missing"));
    }

    [Fact]
    public void ValidateScriptJson_MissingScenes_ReturnsInvalid()
    {
        // Arrange
        var scriptWithoutScenes = @"{
            ""title"": ""Test Title"",
            ""hook"": ""Test hook"",
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 10.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithoutScenes);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("'scenes'") && e.Contains("missing"));
    }

    [Fact]
    public void ValidateScriptJson_EmptyScenes_ReturnsInvalid()
    {
        // Arrange
        var scriptWithEmptyScenes = @"{
            ""title"": ""Test Title"",
            ""hook"": ""Test hook"",
            ""scenes"": [],
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 10.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithEmptyScenes);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("empty"));
    }

    [Fact]
    public void ValidateScriptJson_SceneMissingNarration_ReturnsInvalid()
    {
        // Arrange
        var scriptWithInvalidScene = @"{
            ""title"": ""Test Title"",
            ""hook"": ""Test hook"",
            ""scenes"": [
                {
                    ""visualDescription"": ""Test visual"",
                    ""duration"": 5.0,
                    ""transition"": ""fade""
                }
            ],
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 10.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithInvalidScene);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("narration"));
    }

    [Fact]
    public void ValidateScriptJson_SceneMissingVisualDescription_ReturnsInvalid()
    {
        // Arrange
        var scriptWithInvalidScene = @"{
            ""title"": ""Test Title"",
            ""hook"": ""Test hook"",
            ""scenes"": [
                {
                    ""narration"": ""Test narration"",
                    ""duration"": 5.0,
                    ""transition"": ""fade""
                }
            ],
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 10.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithInvalidScene);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("visualDescription"));
    }

    [Fact]
    public void ValidateScriptJson_SceneDurationOutOfRange_ReturnsInvalid()
    {
        // Arrange
        var scriptWithInvalidDuration = @"{
            ""title"": ""Test Title"",
            ""hook"": ""Test hook"",
            ""scenes"": [
                {
                    ""narration"": ""Test narration"",
                    ""visualDescription"": ""Test visual"",
                    ""duration"": 0.5,
                    ""transition"": ""fade""
                }
            ],
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 10.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithInvalidDuration);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duration") && e.Contains("between 1.0 and 60.0"));
    }

    [Fact]
    public void ValidateScriptJson_InvalidTransitionType_ReturnsInvalid()
    {
        // Arrange
        var scriptWithInvalidTransition = @"{
            ""title"": ""Test Title"",
            ""hook"": ""Test hook"",
            ""scenes"": [
                {
                    ""narration"": ""Test narration"",
                    ""visualDescription"": ""Test visual"",
                    ""duration"": 5.0,
                    ""transition"": ""invalid_transition""
                }
            ],
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 10.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithInvalidTransition);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Invalid transition type"));
    }

    [Fact]
    public void ValidateScriptJson_AllValidTransitionTypes_ReturnsValid()
    {
        // Arrange
        var validTransitions = new[] { "cut", "fade", "dissolve", "wipe", "slide", "zoom" };

        foreach (var transition in validTransitions)
        {
            var script = $@"{{
                ""title"": ""Test Title"",
                ""hook"": ""Test hook"",
                ""scenes"": [
                    {{
                        ""narration"": ""Test narration"",
                        ""visualDescription"": ""Test visual"",
                        ""duration"": 5.0,
                        ""transition"": ""{transition}""
                    }}
                ],
                ""callToAction"": ""Test CTA"",
                ""totalDuration"": 10.0
            }}";

            // Act
            var result = _validationService.ValidateScriptJson(script);

            // Assert
            Assert.True(result.IsValid, $"Valid transition '{transition}' was rejected: {string.Join(", ", result.Errors)}");
        }
    }

    [Fact]
    public void ValidateScriptJson_MultipleScenes_ValidatesAll()
    {
        // Arrange
        var scriptWithMultipleScenes = @"{
            ""title"": ""Test Title"",
            ""hook"": ""Test hook"",
            ""scenes"": [
                {
                    ""narration"": ""Scene 1"",
                    ""visualDescription"": ""Visual 1"",
                    ""duration"": 5.0,
                    ""transition"": ""fade""
                },
                {
                    ""narration"": ""Scene 2"",
                    ""visualDescription"": ""Visual 2"",
                    ""duration"": 6.0,
                    ""transition"": ""dissolve""
                },
                {
                    ""narration"": ""Scene 3"",
                    ""visualDescription"": ""Visual 3"",
                    ""duration"": 7.0,
                    ""transition"": ""cut""
                }
            ],
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 25.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithMultipleScenes);

        // Assert
        Assert.True(result.IsValid, $"Validation failed: {string.Join(", ", result.Errors)}");
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateScriptJson_EmptyStrings_ReturnsInvalid()
    {
        // Arrange
        var scriptWithEmptyStrings = @"{
            ""title"": """",
            ""hook"": ""Test hook"",
            ""scenes"": [
                {
                    ""narration"": ""Test narration"",
                    ""visualDescription"": ""Test visual"",
                    ""duration"": 5.0,
                    ""transition"": ""fade""
                }
            ],
            ""callToAction"": ""Test CTA"",
            ""totalDuration"": 10.0
        }";

        // Act
        var result = _validationService.ValidateScriptJson(scriptWithEmptyStrings);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("'title'") && e.Contains("empty"));
    }
}
