using Xunit;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Aura.Core.Validation;
using System;
using System.Collections.Generic;

namespace Aura.Tests.Validation;

public class TtsScriptValidatorTests
{
    private readonly TtsScriptValidator _validator;

    public TtsScriptValidatorTests()
    {
        _validator = new TtsScriptValidator();
    }

    [Fact]
    public void ValidateScript_EmptyScript_ReturnsInvalid()
    {
        // Arrange
        var script = new Script
        {
            Title = "Test",
            Scenes = new List<ScriptScene>(),
            TotalDuration = TimeSpan.FromSeconds(60)
        };
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Informative"
        );

        // Act
        var result = _validator.ValidateScript(script, planSpec);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Category == "Structure");
    }

    [Fact]
    public void ValidateScript_ValidScript_ReturnsValid()
    {
        // Arrange
        var script = new Script
        {
            Title = "Test Script",
            Scenes = new List<ScriptScene>
            {
                new ScriptScene
                {
                    Number = 1,
                    Narration = "Welcome to our video. Today we're going to explore an interesting topic that will help you understand the subject better. Let's get started with the basics.",
                    Duration = TimeSpan.FromSeconds(12),
                    Transition = TransitionType.Cut
                },
                new ScriptScene
                {
                    Number = 2,
                    Narration = "Here's the main content of our video. This section covers the key points you need to know. Pay attention to the details we're about to discuss.",
                    Duration = TimeSpan.FromSeconds(12),
                    Transition = TransitionType.Cut
                },
                new ScriptScene
                {
                    Number = 3,
                    Narration = "In conclusion, we've covered the essential information. Thank you for watching. We hope you found this helpful.",
                    Duration = TimeSpan.FromSeconds(8),
                    Transition = TransitionType.Fade
                }
            },
            TotalDuration = TimeSpan.FromSeconds(32)
        };
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(30),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Informative"
        );

        // Act
        var result = _validator.ValidateScript(script, planSpec);

        // Assert
        Assert.True(result.IsValid || result.Issues.All(i => i.Severity != TtsScriptValidator.TtsIssueSeverity.Error));
        Assert.True(result.TtsReadinessScore >= 50);
    }

    [Fact]
    public void ValidateNarration_EmptyNarration_ReturnsInvalid()
    {
        // Act
        var result = _validator.ValidateNarration("", 1);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Category == "Content");
    }

    [Fact]
    public void ValidateNarration_LongSentences_AddsWarning()
    {
        // Arrange - A sentence with more than 30 words
        var narration = "This is a very long sentence that contains more than thirty words because we want to test the sentence length validation feature that checks for run-on sentences that would be difficult for TTS engines to read naturally.";

        // Act
        var result = _validator.ValidateNarration(narration, 1);

        // Assert
        Assert.Contains(result.Warnings.Concat(result.Issues), 
            i => i.Category == "Sentence" || i.Message.Contains("sentence"));
    }

    [Fact]
    public void ValidateNarration_MissingEndPunctuation_AddsWarning()
    {
        // Arrange
        var narration = "This text does not end with punctuation";

        // Act
        var result = _validator.ValidateNarration(narration, 1);

        // Assert
        Assert.Contains(result.Warnings, i => i.Category == "Punctuation");
    }

    [Fact]
    public void ValidateNarration_MarketingFluff_AddsWarning()
    {
        // Arrange
        var narration = "This revolutionary product is a game-changer that will transform your life. It's cutting-edge technology at its finest.";

        // Act
        var result = _validator.ValidateNarration(narration, 1);

        // Assert
        Assert.Contains(result.Warnings, i => i.Category == "Content" && i.Message.Contains("marketing fluff"));
    }

    [Fact]
    public void ValidateNarration_WithMetadata_AddsIssue()
    {
        // Arrange
        var narration = "[VISUAL: Show product] This is the narration text. [PAUSE] More content here.";

        // Act
        var result = _validator.ValidateNarration(narration, 1);

        // Assert
        Assert.Contains(result.Issues, i => i.Category == "Content" && i.Message.Contains("metadata"));
    }

    [Fact]
    public void CalculateDurationFromWordCount_ReturnsCorrectDuration()
    {
        // 150 words at 150 WPM = 60 seconds
        var duration150 = TtsScriptValidator.CalculateDurationFromWordCount(150);
        Assert.Equal(TimeSpan.FromSeconds(60), duration150);

        // 75 words at 150 WPM = 30 seconds
        var duration75 = TtsScriptValidator.CalculateDurationFromWordCount(75);
        Assert.Equal(TimeSpan.FromSeconds(30), duration75);

        // 10 words should return minimum 3 seconds
        var durationMin = TtsScriptValidator.CalculateDurationFromWordCount(10);
        Assert.Equal(TimeSpan.FromSeconds(4), durationMin); // 10 words / 150 WPM * 60 = 4 seconds

        // Very short text returns minimum
        var durationVeryShort = TtsScriptValidator.CalculateDurationFromWordCount(2);
        Assert.Equal(TimeSpan.FromSeconds(3), durationVeryShort);

        // 0 words returns minimum
        var durationZero = TtsScriptValidator.CalculateDurationFromWordCount(0);
        Assert.Equal(TimeSpan.FromSeconds(3), durationZero);
    }

    [Fact]
    public void CalculateDurationFromWordCount_RespectsMaximum()
    {
        // 1000 words at 150 WPM = 400 seconds, but should be capped at 30
        var duration = TtsScriptValidator.CalculateDurationFromWordCount(1000);
        Assert.Equal(TimeSpan.FromSeconds(30), duration);
    }

    [Fact]
    public void GetWordCount_ReturnsCorrectCount()
    {
        Assert.Equal(5, TtsScriptValidator.GetWordCount("One two three four five"));
        Assert.Equal(0, TtsScriptValidator.GetWordCount(""));
        Assert.Equal(0, TtsScriptValidator.GetWordCount(null!));
        Assert.Equal(3, TtsScriptValidator.GetWordCount("  Words  with  spaces  "));
    }

    [Fact]
    public void ValidateNarration_ProperlyFormatted_ReturnsHighScore()
    {
        // Arrange - Well-formatted TTS-ready text
        var narration = "Welcome to our video. Today, we'll explore an interesting topic. You'll learn the basics in just a few minutes. Let's get started.";

        // Act
        var result = _validator.ValidateNarration(narration, 1);

        // Assert
        Assert.True(result.TtsReadinessScore >= 80);
    }

    [Fact]
    public void ValidateScript_TooManyScenes_AddsWarning()
    {
        // Arrange - Script with 30 scenes for a 60 second video
        var scenes = new List<ScriptScene>();
        for (int i = 1; i <= 30; i++)
        {
            scenes.Add(new ScriptScene
            {
                Number = i,
                Narration = "Short scene content for testing purposes here.",
                Duration = TimeSpan.FromSeconds(2),
                Transition = TransitionType.Cut
            });
        }
        var script = new Script
        {
            Title = "Test",
            Scenes = scenes,
            TotalDuration = TimeSpan.FromSeconds(60)
        };
        var planSpec = new PlanSpec(
            TargetDuration: TimeSpan.FromSeconds(60),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "Informative",
            MaxSceneCount: 20
        );

        // Act
        var result = _validator.ValidateScript(script, planSpec);

        // Assert
        Assert.Contains(result.Warnings.Concat(result.Issues), 
            i => i.Message.Contains("more scenes") || i.Message.Contains("deviates"));
    }
}
