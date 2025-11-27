using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Aura.Core.Models;
using Aura.Core.Models.Generation;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Validation;

/// <summary>
/// Validates script quality for TTS (Text-to-Speech) readiness.
/// Checks sentence structure, punctuation, scene length, and content quality.
/// </summary>
public class TtsScriptValidator
{
    private readonly ILogger<TtsScriptValidator>? _logger;
    
    /// <summary>
    /// Configuration for TTS validation thresholds
    /// </summary>
    public record TtsValidationConfig(
        int MaxWordsPerSentence = 30,
        int MinWordsPerScene = 50,
        int MaxWordsPerScene = 200,
        int MinSceneDurationSeconds = 3,
        int MaxSceneDurationSeconds = 30,
        int TargetWordsPerMinute = 150);

    /// <summary>
    /// Result of TTS validation with detailed issues and warnings
    /// </summary>
    public record TtsValidationResult(
        bool IsValid,
        List<TtsValidationIssue> Issues,
        List<TtsValidationIssue> Warnings,
        double TtsReadinessScore);

    /// <summary>
    /// Individual validation issue with location and severity
    /// </summary>
    public record TtsValidationIssue(
        string Category,
        string Message,
        int? SceneNumber = null,
        TtsIssueSeverity Severity = TtsIssueSeverity.Warning);

    /// <summary>
    /// Severity levels for validation issues
    /// </summary>
    public enum TtsIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    private readonly TtsValidationConfig _config;

    // Patterns that indicate marketing fluff or filler content
    private static readonly string[] MarketingFluffPatterns = new[]
    {
        "game-chang", "revolutionary", "cutting-edge", "state-of-the-art",
        "best-in-class", "world-class", "industry-leading", "next-generation",
        "groundbreaking", "innovative solution", "seamlessly integrate",
        "unlock the power", "take your .* to the next level", "don't miss out",
        "act now", "limited time", "exclusive offer", "100% guaranteed"
    };

    // Patterns that indicate metadata mixed with narration
    private static readonly string[] MetadataPatterns = new[]
    {
        @"^\s*scene\s*\d+\s*[:.-]",
        @"^\s*\[scene",
        @"duration\s*[:=]\s*\d+",
        @"^\s*#\s",
        @"^\s*##\s",
        @"\[visual:",
        @"\[pause\]",
        @"\[music\]",
        @"\[sfx\]"
    };

    public TtsScriptValidator(ILogger<TtsScriptValidator>? logger = null, TtsValidationConfig? config = null)
    {
        _logger = logger;
        _config = config ?? new TtsValidationConfig();
    }

    /// <summary>
    /// Validates a script for TTS readiness
    /// </summary>
    public TtsValidationResult ValidateScript(Script script, PlanSpec planSpec)
    {
        var issues = new List<TtsValidationIssue>();
        var warnings = new List<TtsValidationIssue>();

        if (script.Scenes.Count == 0)
        {
            issues.Add(new TtsValidationIssue(
                "Structure",
                "Script has no scenes",
                Severity: TtsIssueSeverity.Error));
            return new TtsValidationResult(false, issues, warnings, 0.0);
        }

        // Validate overall structure
        ValidateSceneCount(script, planSpec, issues, warnings);

        // Validate each scene
        foreach (var scene in script.Scenes)
        {
            ValidateScene(scene, issues, warnings);
        }

        // Calculate TTS readiness score (0-100)
        var errorCount = issues.Count(i => i.Severity == TtsIssueSeverity.Error);
        var warningCount = warnings.Count + issues.Count(i => i.Severity == TtsIssueSeverity.Warning);
        
        // Base score of 100, subtract for issues
        var score = Math.Max(0, 100 - (errorCount * 15) - (warningCount * 5));

        var isValid = errorCount == 0;

        _logger?.LogInformation(
            "TTS validation complete: {IsValid}, Score: {Score}, Errors: {Errors}, Warnings: {Warnings}",
            isValid, score, errorCount, warningCount);

        return new TtsValidationResult(isValid, issues, warnings, score);
    }

    /// <summary>
    /// Validates a single narration text for TTS readiness
    /// </summary>
    public TtsValidationResult ValidateNarration(string narration, int? sceneNumber = null)
    {
        var issues = new List<TtsValidationIssue>();
        var warnings = new List<TtsValidationIssue>();

        if (string.IsNullOrWhiteSpace(narration))
        {
            issues.Add(new TtsValidationIssue(
                "Content",
                "Narration is empty",
                sceneNumber,
                TtsIssueSeverity.Error));
            return new TtsValidationResult(false, issues, warnings, 0.0);
        }

        // Check sentence structure
        ValidateSentenceStructure(narration, sceneNumber, issues, warnings);

        // Check punctuation
        ValidatePunctuation(narration, sceneNumber, issues, warnings);

        // Check for marketing fluff
        ValidateNoMarketingFluff(narration, sceneNumber, warnings);

        // Check for metadata in narration
        ValidateNoMetadata(narration, sceneNumber, issues);

        // Calculate score
        var errorCount = issues.Count(i => i.Severity == TtsIssueSeverity.Error);
        var warningCount = warnings.Count + issues.Count(i => i.Severity == TtsIssueSeverity.Warning);
        var score = Math.Max(0, 100 - (errorCount * 15) - (warningCount * 5));

        return new TtsValidationResult(errorCount == 0, issues, warnings, score);
    }

    private void ValidateSceneCount(Script script, PlanSpec planSpec, 
        List<TtsValidationIssue> issues, List<TtsValidationIssue> warnings)
    {
        var targetSceneCount = planSpec.GetCalculatedSceneCount();
        var actualSceneCount = script.Scenes.Count;

        // Check if scene count is way off (more than 50% deviation)
        var deviation = Math.Abs(actualSceneCount - targetSceneCount) / (double)targetSceneCount;
        if (deviation > 0.5)
        {
            issues.Add(new TtsValidationIssue(
                "Structure",
                $"Scene count ({actualSceneCount}) deviates significantly from target ({targetSceneCount})",
                Severity: TtsIssueSeverity.Warning));
        }

        // Check minimum and maximum scene counts
        var minScenes = planSpec.MinSceneCount ?? 3;
        var maxScenes = planSpec.MaxSceneCount ?? 20;

        if (actualSceneCount < minScenes)
        {
            warnings.Add(new TtsValidationIssue(
                "Structure",
                $"Script has fewer scenes ({actualSceneCount}) than minimum ({minScenes})"));
        }
        else if (actualSceneCount > maxScenes)
        {
            warnings.Add(new TtsValidationIssue(
                "Structure",
                $"Script has more scenes ({actualSceneCount}) than maximum ({maxScenes})"));
        }
    }

    private void ValidateScene(ScriptScene scene, 
        List<TtsValidationIssue> issues, List<TtsValidationIssue> warnings)
    {
        var narration = scene.Narration;
        
        if (string.IsNullOrWhiteSpace(narration))
        {
            issues.Add(new TtsValidationIssue(
                "Content",
                "Scene has empty narration",
                scene.Number,
                TtsIssueSeverity.Error));
            return;
        }

        // Check word count
        var wordCount = CountWords(narration);
        if (wordCount < _config.MinWordsPerScene)
        {
            warnings.Add(new TtsValidationIssue(
                "Length",
                $"Scene narration too short ({wordCount} words, minimum {_config.MinWordsPerScene})",
                scene.Number));
        }
        else if (wordCount > _config.MaxWordsPerScene)
        {
            warnings.Add(new TtsValidationIssue(
                "Length",
                $"Scene narration too long ({wordCount} words, maximum {_config.MaxWordsPerScene})",
                scene.Number));
        }

        // Check duration bounds
        var durationSeconds = scene.Duration.TotalSeconds;
        if (durationSeconds < _config.MinSceneDurationSeconds)
        {
            warnings.Add(new TtsValidationIssue(
                "Duration",
                $"Scene duration too short ({durationSeconds:F1}s, minimum {_config.MinSceneDurationSeconds}s)",
                scene.Number));
        }
        else if (durationSeconds > _config.MaxSceneDurationSeconds)
        {
            warnings.Add(new TtsValidationIssue(
                "Duration",
                $"Scene duration too long ({durationSeconds:F1}s, maximum {_config.MaxSceneDurationSeconds}s)",
                scene.Number));
        }

        // Validate sentence structure and punctuation
        ValidateSentenceStructure(narration, scene.Number, issues, warnings);
        ValidatePunctuation(narration, scene.Number, issues, warnings);
        ValidateNoMarketingFluff(narration, scene.Number, warnings);
        ValidateNoMetadata(narration, scene.Number, issues);
    }

    private void ValidateSentenceStructure(string text, int? sceneNumber,
        List<TtsValidationIssue> issues, List<TtsValidationIssue> warnings)
    {
        // Split into sentences (simple approach - split on sentence-ending punctuation)
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        var longSentenceCount = 0;
        foreach (var sentence in sentences)
        {
            var wordCount = CountWords(sentence);
            if (wordCount > _config.MaxWordsPerSentence)
            {
                longSentenceCount++;
            }
        }

        if (longSentenceCount > 0)
        {
            var severity = longSentenceCount > 2 ? TtsIssueSeverity.Warning : TtsIssueSeverity.Info;
            (severity == TtsIssueSeverity.Warning ? warnings : issues).Add(new TtsValidationIssue(
                "Sentence",
                $"{longSentenceCount} sentence(s) exceed {_config.MaxWordsPerSentence} words (may affect TTS naturalness)",
                sceneNumber,
                severity));
        }
    }

    private void ValidatePunctuation(string text, int? sceneNumber,
        List<TtsValidationIssue> issues, List<TtsValidationIssue> warnings)
    {
        // Check for sentences without proper ending punctuation
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        // If the text doesn't end with punctuation
        var trimmed = text.Trim();
        if (!string.IsNullOrEmpty(trimmed) && !trimmed.EndsWith('.') && !trimmed.EndsWith('!') && !trimmed.EndsWith('?'))
        {
            warnings.Add(new TtsValidationIssue(
                "Punctuation",
                "Text does not end with proper punctuation (., !, or ?)",
                sceneNumber));
        }

        // Check for run-on text (very long stretches without punctuation)
        var wordsWithoutPunctuation = 0;
        var maxWordsWithoutPunctuation = 0;
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (word.EndsWith('.') || word.EndsWith(',') || word.EndsWith('!') || 
                word.EndsWith('?') || word.EndsWith(';') || word.EndsWith(':'))
            {
                maxWordsWithoutPunctuation = Math.Max(maxWordsWithoutPunctuation, wordsWithoutPunctuation);
                wordsWithoutPunctuation = 0;
            }
            else
            {
                wordsWithoutPunctuation++;
            }
        }
        maxWordsWithoutPunctuation = Math.Max(maxWordsWithoutPunctuation, wordsWithoutPunctuation);

        if (maxWordsWithoutPunctuation > 20)
        {
            warnings.Add(new TtsValidationIssue(
                "Punctuation",
                $"Long stretch without punctuation ({maxWordsWithoutPunctuation} words) - add commas for natural TTS pauses",
                sceneNumber));
        }
    }

    private void ValidateNoMarketingFluff(string text, int? sceneNumber,
        List<TtsValidationIssue> warnings)
    {
        var textLower = text.ToLowerInvariant();
        var foundPatterns = new List<string>();

        foreach (var pattern in MarketingFluffPatterns)
        {
            if (Regex.IsMatch(textLower, pattern, RegexOptions.IgnoreCase))
            {
                foundPatterns.Add(pattern);
            }
        }

        if (foundPatterns.Count > 0)
        {
            warnings.Add(new TtsValidationIssue(
                "Content",
                $"Contains marketing fluff phrases: {string.Join(", ", foundPatterns.Take(3))}",
                sceneNumber));
        }
    }

    private void ValidateNoMetadata(string text, int? sceneNumber,
        List<TtsValidationIssue> issues)
    {
        var foundMetadata = new List<string>();

        foreach (var pattern in MetadataPatterns)
        {
            if (Regex.IsMatch(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
            {
                foundMetadata.Add(pattern);
            }
        }

        if (foundMetadata.Count > 0)
        {
            issues.Add(new TtsValidationIssue(
                "Content",
                "Narration contains metadata/formatting that TTS engines cannot read properly",
                sceneNumber,
                TtsIssueSeverity.Warning));
        }
    }

    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
            
        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Calculates optimal scene duration based on word count at 150 WPM
    /// </summary>
    public static TimeSpan CalculateDurationFromWordCount(int wordCount, int wordsPerMinute = 150)
    {
        if (wordCount <= 0)
            return TimeSpan.FromSeconds(3); // Minimum duration
            
        var seconds = (wordCount / (double)wordsPerMinute) * 60;
        
        // Apply bounds (3-30 seconds)
        seconds = Math.Clamp(seconds, 3, 30);
        
        return TimeSpan.FromSeconds(seconds);
    }

    /// <summary>
    /// Calculates word count from text
    /// </summary>
    public static int GetWordCount(string text)
    {
        return CountWords(text);
    }
}
