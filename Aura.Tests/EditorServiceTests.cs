using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services;
using Aura.Providers.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Tests for EditorService targeted script editing
/// </summary>
public class EditorServiceTests
{
    private readonly EditorService _editorService;
    private readonly RuleBasedLlmProvider _llmProvider;

    public EditorServiceTests()
    {
        _llmProvider = new RuleBasedLlmProvider(NullLogger<RuleBasedLlmProvider>.Instance);
        _editorService = new EditorService(
            NullLogger<EditorService>.Instance,
            _llmProvider
        );
    }

    [Fact]
    public async Task EditScript_WithCritique_ReturnsEditedScript()
    {
        // Arrange
        var script = "Original script content.";
        
        var critique = new CritiqueResult
        {
            OverallScore = 70.0,
            Issues = new List<CritiqueIssue>
            {
                new CritiqueIssue
                {
                    Category = "Clarity",
                    Severity = "High",
                    Description = "Opening is unclear"
                }
            },
            Suggestions = new List<CritiqueSuggestion>
            {
                new CritiqueSuggestion
                {
                    ChangeType = "rewrite",
                    Target = "opening",
                    Suggestion = "Make it more engaging",
                    ExpectedImpact = "Improved clarity"
                }
            }
        };

        var brief = new Brief(
            Topic: "Test",
            Audience: null,
            Goal: null,
            Tone: "neutral",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );

        // Act
        var result = await _editorService.EditScriptAsync(
            script, critique, brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotEmpty(result.EditedScript);
        Assert.NotNull(result.ValidationResult);
    }

    [Fact]
    public async Task ApplyEdits_WithReplaceEdit_ReplacesText()
    {
        // Arrange
        var script = "The quick brown fox jumps over the lazy dog.";
        var edits = new List<ScriptEdit>
        {
            new ScriptEdit
            {
                EditType = "replace",
                OriginalText = "lazy",
                NewText = "sleepy",
                Reason = "More descriptive"
            }
        };

        // Act
        var result = await _editorService.ApplyEditsAsync(script, edits, CancellationToken.None);

        // Assert
        Assert.Contains("sleepy", result);
        Assert.DoesNotContain("lazy", result);
    }

    [Fact]
    public async Task ApplyEdits_WithDeleteEdit_RemovesText()
    {
        // Arrange
        var script = "This is a test. This is only a test.";
        var edits = new List<ScriptEdit>
        {
            new ScriptEdit
            {
                EditType = "delete",
                OriginalText = "This is only a test.",
                Reason = "Redundant"
            }
        };

        // Act
        var result = await _editorService.ApplyEditsAsync(script, edits, CancellationToken.None);

        // Assert
        Assert.DoesNotContain("This is only a test", result);
        Assert.Contains("This is a test", result);
    }

    [Fact]
    public async Task ApplyEdits_WithInsertEdit_AddsText()
    {
        // Arrange
        var script = "Introduction. Conclusion.";
        var edits = new List<ScriptEdit>
        {
            new ScriptEdit
            {
                EditType = "insert",
                Target = "Introduction.",
                NewText = " Middle section here.",
                Reason = "Add missing content"
            }
        };

        // Act
        var result = await _editorService.ApplyEditsAsync(script, edits, CancellationToken.None);

        // Assert
        Assert.Contains("Middle section here", result);
    }

    [Fact]
    public async Task ValidateSchema_WithEmptyScript_ReturnsInvalid()
    {
        // Arrange
        var script = "";
        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );

        // Act
        var result = await _editorService.ValidateSchemaAsync(script, spec, CancellationToken.None);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Contains("empty"));
    }

    [Fact]
    public async Task ValidateSchema_WithValidScript_ReturnsValid()
    {
        // Arrange
        var words = string.Join(" ", Enumerable.Repeat("word", 150)); // 150 words for 1 minute
        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );

        // Act
        var result = await _editorService.ValidateSchemaAsync(words, spec, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.True(result.MeetsDurationConstraints);
        Assert.NotNull(result.EstimatedDuration);
        Assert.InRange(result.EstimatedDuration.Value.TotalSeconds, 50, 70); // ~1 minute ±10s
    }

    [Fact]
    public async Task ValidateSchema_WithTooLongScript_FailsDurationConstraint()
    {
        // Arrange
        var words = string.Join(" ", Enumerable.Repeat("word", 300)); // 300 words - double target
        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );

        // Act
        var result = await _editorService.ValidateSchemaAsync(words, spec, CancellationToken.None);

        // Assert
        Assert.True(result.IsValid); // Schema is valid, just too long
        Assert.False(result.MeetsDurationConstraints); // But duration constraint fails
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("variance") || w.Contains("exceeds"));
    }

    [Fact]
    public async Task ValidateSchema_EstimatesDuration_Accurately()
    {
        // Arrange - 300 words should be ~2 minutes at 150 words/minute
        var words = string.Join(" ", Enumerable.Repeat("word", 300));
        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(2),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );

        // Act
        var result = await _editorService.ValidateSchemaAsync(words, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result.EstimatedDuration);
        Assert.InRange(result.EstimatedDuration.Value.TotalMinutes, 1.8, 2.2); // ~2 minutes ±12s
        Assert.Equal(TimeSpan.FromMinutes(2), result.TargetDuration);
        Assert.True(result.MeetsDurationConstraints);
    }

    [Fact]
    public async Task EditScript_ValidatesSchemaByDefault()
    {
        // Arrange
        var script = string.Join(" ", Enumerable.Repeat("word", 50)); // 50 words - too short for 1 min
        
        var critique = new CritiqueResult
        {
            OverallScore = 60.0,
            Suggestions = new List<CritiqueSuggestion>()
        };

        var brief = new Brief(
            Topic: "Test",
            Audience: null,
            Goal: null,
            Tone: "neutral",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );

        // Act
        var result = await _editorService.EditScriptAsync(
            script, critique, brief, spec, CancellationToken.None);

        // Assert
        Assert.NotNull(result.ValidationResult);
        Assert.False(result.ValidationResult.MeetsDurationConstraints);
    }

    [Fact]
    public async Task EditScript_PreservesContentIntegrity()
    {
        // Arrange
        var script = "Important content that should be preserved.";
        
        var critique = new CritiqueResult
        {
            OverallScore = 80.0,
            Strengths = new List<string> { "Content is good" },
            Suggestions = new List<CritiqueSuggestion>()
        };

        var brief = new Brief(
            Topic: "Test",
            Audience: null,
            Goal: null,
            Tone: "neutral",
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var spec = new PlanSpec(
            TargetDuration: TimeSpan.FromMinutes(1),
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: "test"
        );

        // Act
        var result = await _editorService.EditScriptAsync(
            script, critique, brief, spec, CancellationToken.None);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.EditedScript);
    }
}
