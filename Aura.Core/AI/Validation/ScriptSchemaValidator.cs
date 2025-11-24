using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.AI.Validation;

/// <summary>
/// Validates LLM-generated scripts against expected schema and quality criteria
/// Provides detailed validation results with quality scoring and metrics
/// </summary>
public class ScriptSchemaValidator
{
    private readonly ILogger<ScriptSchemaValidator>? _logger;

    public ScriptSchemaValidator(ILogger<ScriptSchemaValidator>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validation result with detailed error information and quality metrics
    /// </summary>
    public record ValidationResult(
        bool IsValid,
        List<string> Errors,
        double QualityScore,
        ScriptMetrics Metrics
    );

    /// <summary>
    /// Quality metrics for the generated script
    /// </summary>
    public record ScriptMetrics(
        int SceneCount,
        int TotalCharacters,
        int AverageSceneLength,
        bool HasIntroduction,
        bool HasConclusion,
        double ReadabilityScore
    );

    /// <summary>
    /// Validates a script against expected schema and quality criteria
    /// </summary>
    public ValidationResult Validate(string script, Brief brief, PlanSpec spec)
    {
        var errors = new List<string>();

        // Basic structure validation
        if (string.IsNullOrWhiteSpace(script))
        {
            errors.Add("Script is empty");
            return new ValidationResult(
                IsValid: false,
                Errors: errors,
                QualityScore: 0.0,
                Metrics: new ScriptMetrics(0, 0, 0, false, false, 0.0)
            );
        }

        // Parse scenes for analysis
        var scenes = ParseScenes(script);

        // Structure validation
        if (scenes.Count < GetMinScenes(spec))
        {
            errors.Add($"Too few scenes: {scenes.Count} < {GetMinScenes(spec)}");
        }

        // Check for title
        var trimmedScript = script.Trim();
        if (!trimmedScript.StartsWith("# "))
        {
            errors.Add("Script missing title (should start with '# Title')");
        }

        // Content validation
        if (!ContainsTopicReference(script, brief.Topic))
        {
            errors.Add($"Script doesn't reference the topic: '{brief.Topic}'");
        }

        // Check word count is approximately correct for target duration
        var words = script.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        int wordCount = words.Length;
        
        // Expected words: 150 words per minute = 2.5 words per second
        double expectedWords = spec.TargetDuration.TotalSeconds * 2.5;
        double difference = expectedWords > 0 ? Math.Abs(wordCount - expectedWords) / expectedWords : 1.0;
        
        // Tolerance of 50% deviation
        if (difference > 0.5)
        {
            errors.Add($"Word count significantly off target. Found {wordCount} words, expected approximately {(int)expectedWords} words for {(int)spec.TargetDuration.TotalSeconds} seconds.");
        }

        // Check for placeholder text
        if (script.Contains("[PLACEHOLDER]", StringComparison.OrdinalIgnoreCase) ||
            script.Contains("TODO", StringComparison.OrdinalIgnoreCase) ||
            script.Contains("FIXME", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Script contains placeholder text, indicating incomplete generation");
        }

        // Check for AI refusal language
        if (script.Contains("I cannot", StringComparison.OrdinalIgnoreCase) ||
            script.Contains("I apologize", StringComparison.OrdinalIgnoreCase) ||
            script.Contains("as an AI", StringComparison.OrdinalIgnoreCase) ||
            script.Contains("I'm not able", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Script contains AI refusal language instead of actual content");
        }

        // Check for excessive repetition
        if (HasExcessiveRepetition(script))
        {
            errors.Add("Script contains excessive repetition, suggesting generation error");
        }

        // Quality scoring
        var qualityScore = CalculateQualityScore(script, scenes, brief, spec);

        // Calculate metrics
        var metrics = CalculateMetrics(script, scenes);

        var isValid = errors.Count == 0 && qualityScore >= 0.6;

        if (!isValid && _logger != null)
        {
            _logger.LogWarning(
                "Script validation failed with {ErrorCount} errors and quality score {QualityScore:F2}",
                errors.Count,
                qualityScore);
        }

        return new ValidationResult(
            IsValid: isValid,
            Errors: errors,
            QualityScore: qualityScore,
            Metrics: metrics
        );
    }

    /// <summary>
    /// Parses scenes from script text, handling various formats
    /// </summary>
    private List<ParsedScene> ParseScenes(string script)
    {
        var scenes = new List<ParsedScene>();
        var lines = script.Split('\n', StringSplitOptions.None);

        string? currentHeading = null;
        var currentContent = new List<string>();
        int sceneIndex = 0;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            // Check for scene markers: ## Scene Name, Scene 1:, etc.
            if (trimmedLine.StartsWith("## ", StringComparison.OrdinalIgnoreCase))
            {
                // Save previous scene
                if (currentHeading != null && currentContent.Count > 0)
                {
                    scenes.Add(new ParsedScene(
                        Index: sceneIndex++,
                        Heading: currentHeading,
                        Content: string.Join("\n", currentContent).Trim()
                    ));
                    currentContent.Clear();
                }

                currentHeading = trimmedLine.Substring(3).Trim();
            }
            else if (Regex.IsMatch(trimmedLine, @"^Scene\s+\d+[:]", RegexOptions.IgnoreCase))
            {
                // Save previous scene
                if (currentHeading != null && currentContent.Count > 0)
                {
                    scenes.Add(new ParsedScene(
                        Index: sceneIndex++,
                        Heading: currentHeading,
                        Content: string.Join("\n", currentContent).Trim()
                    ));
                    currentContent.Clear();
                }

                var match = Regex.Match(trimmedLine, @"^Scene\s+\d+[:]\s*(.+)", RegexOptions.IgnoreCase);
                currentHeading = match.Success ? match.Groups[1].Value.Trim() : trimmedLine;
            }
            else if (!trimmedLine.StartsWith("#", StringComparison.Ordinal) && 
                     !string.IsNullOrWhiteSpace(trimmedLine))
            {
                // Regular content line
                currentContent.Add(line);
            }
        }

        // Add the last scene
        if (currentHeading != null && currentContent.Count > 0)
        {
            scenes.Add(new ParsedScene(
                Index: sceneIndex++,
                Heading: currentHeading,
                Content: string.Join("\n", currentContent).Trim()
            ));
        }

        // If no scenes found, treat entire script as one scene
        if (scenes.Count == 0 && !string.IsNullOrWhiteSpace(script))
        {
            scenes.Add(new ParsedScene(
                Index: 0,
                Heading: "Introduction",
                Content: script.Trim()
            ));
        }

        return scenes;
    }

    /// <summary>
    /// Checks if script contains references to the topic
    /// </summary>
    private bool ContainsTopicReference(string script, string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return true; // No topic to check

        var topicWords = topic.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length > 3) // Ignore short words
            .ToList();

        if (topicWords.Count == 0)
            return true;

        var scriptLower = script.ToLowerInvariant();

        // Check if at least one significant word from topic appears in script
        return topicWords.Any(word => scriptLower.Contains(word, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Detects excessive repetition in text
    /// </summary>
    private bool HasExcessiveRepetition(string text)
    {
        // Split into sentences
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToLowerInvariant())
            .Where(s => s.Length > 10)
            .ToList();

        if (sentences.Count < 3)
        {
            return false;
        }

        // Count repeated sentences
        var uniqueSentences = sentences.Distinct().Count();
        double repetitionRate = 1.0 - ((double)uniqueSentences / sentences.Count);

        // If more than 30% of sentences are repeated, flag it
        return repetitionRate > 0.3;
    }

    /// <summary>
    /// Calculates quality score (0.0 to 1.0) based on various factors
    /// </summary>
    private double CalculateQualityScore(string script, List<ParsedScene> scenes, Brief brief, PlanSpec spec)
    {
        double score = 0.0;

        // Scene count score (0.2 weight)
        var minScenes = GetMinScenes(spec);
        var sceneCountScore = scenes.Count >= minScenes ? 1.0 : (double)scenes.Count / minScenes;
        score += sceneCountScore * 0.2;

        // Length score (0.2 weight)
        var words = script.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var expectedWords = spec.TargetDuration.TotalSeconds * 2.5;
        var lengthRatio = expectedWords > 0 ? Math.Min(words.Length / expectedWords, 1.0) : 0.0;
        score += lengthRatio * 0.2;

        // Structure score (0.2 weight)
        var hasTitle = script.Trim().StartsWith("# ");
        var hasMultipleScenes = scenes.Count >= 2;
        var structureScore = (hasTitle ? 0.5 : 0.0) + (hasMultipleScenes ? 0.5 : 0.0);
        score += structureScore * 0.2;

        // Content relevance score (0.2 weight)
        var relevanceScore = ContainsTopicReference(script, brief.Topic) ? 1.0 : 0.0;
        score += relevanceScore * 0.2;

        // Readability score (0.2 weight)
        var readabilityScore = CalculateReadabilityScore(script);
        score += readabilityScore * 0.2;

        return Math.Min(score, 1.0);
    }

    /// <summary>
    /// Calculates readability score based on sentence length and variety
    /// </summary>
    private double CalculateReadabilityScore(string script)
    {
        var sentences = script.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        if (sentences.Count == 0)
            return 0.0;

        // Calculate average sentence length
        var avgLength = sentences.Average(s => s.Split(' ').Length);

        // Ideal sentence length is 15-20 words
        var lengthScore = avgLength >= 10 && avgLength <= 25 ? 1.0 : 
                         avgLength < 10 ? avgLength / 10.0 : 
                         25.0 / avgLength;

        // Check for variety in sentence length
        var lengths = sentences.Select(s => s.Split(' ').Length).ToList();
        var lengthVariance = CalculateVariance(lengths);
        var varietyScore = Math.Min(lengthVariance / 50.0, 1.0); // Normalize variance

        return (lengthScore * 0.6) + (varietyScore * 0.4);
    }

    /// <summary>
    /// Calculates variance of a list of numbers
    /// </summary>
    private double CalculateVariance(List<int> values)
    {
        if (values.Count == 0)
            return 0.0;

        var mean = values.Average();
        var sumSquaredDiff = values.Sum(v => Math.Pow(v - mean, 2));
        return sumSquaredDiff / values.Count;
    }

    /// <summary>
    /// Calculates script metrics
    /// </summary>
    private ScriptMetrics CalculateMetrics(string script, List<ParsedScene> scenes)
    {
        var totalChars = script.Length;
        var avgSceneLength = scenes.Count > 0 
            ? scenes.Average(s => s.Content.Length) 
            : 0;

        var hasIntroduction = scenes.Count > 0 && 
            (scenes[0].Heading.Contains("Introduction", StringComparison.OrdinalIgnoreCase) ||
             scenes[0].Heading.Contains("Intro", StringComparison.OrdinalIgnoreCase) ||
             scenes[0].Heading.Contains("Opening", StringComparison.OrdinalIgnoreCase));

        var hasConclusion = scenes.Count > 0 && 
            (scenes.Last().Heading.Contains("Conclusion", StringComparison.OrdinalIgnoreCase) ||
             scenes.Last().Heading.Contains("Closing", StringComparison.OrdinalIgnoreCase) ||
             scenes.Last().Heading.Contains("Summary", StringComparison.OrdinalIgnoreCase) ||
             scenes.Last().Heading.Contains("Wrap", StringComparison.OrdinalIgnoreCase));

        var readabilityScore = CalculateReadabilityScore(script);

        return new ScriptMetrics(
            SceneCount: scenes.Count,
            TotalCharacters: totalChars,
            AverageSceneLength: (int)avgSceneLength,
            HasIntroduction: hasIntroduction,
            HasConclusion: hasConclusion,
            ReadabilityScore: readabilityScore
        );
    }

    /// <summary>
    /// Gets minimum number of scenes based on plan spec
    /// </summary>
    private int GetMinScenes(PlanSpec spec)
    {
        // Minimum 2 scenes, but scale with duration
        var minScenes = 2;
        var durationMinutes = spec.TargetDuration.TotalMinutes;
        
        if (durationMinutes > 2)
            minScenes = 3;
        if (durationMinutes > 5)
            minScenes = 4;
        if (durationMinutes > 10)
            minScenes = 5;

        return minScenes;
    }

    /// <summary>
    /// Internal representation of a parsed scene
    /// </summary>
    private record ParsedScene(int Index, string Heading, string Content);
}

