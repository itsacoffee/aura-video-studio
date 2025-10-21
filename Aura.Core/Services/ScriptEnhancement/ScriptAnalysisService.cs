using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.ScriptEnhancement;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ScriptEnhancement;

/// <summary>
/// Service for analyzing script structure, quality, and characteristics
/// </summary>
public class ScriptAnalysisService
{
    private readonly ILogger<ScriptAnalysisService> _logger;
    private readonly ILlmProvider _llmProvider;

    public ScriptAnalysisService(
        ILogger<ScriptAnalysisService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Analyzes a script comprehensively for structure, quality, and characteristics
    /// </summary>
    public async Task<ScriptAnalysis> AnalyzeScriptAsync(
        string script,
        string? contentType = null,
        string? targetAudience = null,
        string? currentTone = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing script (length: {Length} chars)", script.Length);

        try
        {
            // Perform basic analysis
            var readabilityMetrics = CalculateReadabilityMetrics(script);
            var hookStrength = AnalyzeHookStrength(script);
            var detectedFramework = DetectStoryFramework(script);
            var emotionalCurve = await AnalyzeEmotionalCurveAsync(script, contentType, ct);

            // Use AI for quality scoring
            var qualityScores = await GetAiQualityScoresAsync(
                script, contentType, targetAudience, currentTone, ct);

            // Generate issues and strengths
            var issues = IdentifyIssues(script, readabilityMetrics, hookStrength, qualityScores);
            var strengths = IdentifyStrengths(script, readabilityMetrics, hookStrength, qualityScores);

            return new ScriptAnalysis(
                StructureScore: qualityScores["structure"],
                EmotionalCurveScore: qualityScores["emotionalCurve"],
                EngagementScore: qualityScores["engagement"],
                ClarityScore: qualityScores["clarity"],
                HookStrength: hookStrength,
                Issues: issues,
                Strengths: strengths,
                DetectedFramework: detectedFramework,
                EmotionalCurve: emotionalCurve,
                ReadabilityMetrics: readabilityMetrics,
                AnalyzedAt: DateTime.UtcNow
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing script");
            throw;
        }
    }

    /// <summary>
    /// Calculates readability metrics (Flesch-Kincaid, etc.)
    /// </summary>
    private Dictionary<string, double> CalculateReadabilityMetrics(string script)
    {
        var sentences = SplitIntoSentences(script);
        var words = SplitIntoWords(script);
        var syllables = words.Sum(CountSyllables);

        var avgWordsPerSentence = sentences.Count > 0 
            ? (double)words.Count / sentences.Count 
            : 0;
        
        var avgSyllablesPerWord = words.Count > 0 
            ? (double)syllables / words.Count 
            : 0;

        // Flesch Reading Ease (0-100, higher is easier)
        var fleschReadingEase = sentences.Count > 0 && words.Count > 0
            ? 206.835 - (1.015 * avgWordsPerSentence) - (84.6 * avgSyllablesPerWord)
            : 50.0;

        // Flesch-Kincaid Grade Level
        var fleschKincaidGrade = sentences.Count > 0 && words.Count > 0
            ? (0.39 * avgWordsPerSentence) + (11.8 * avgSyllablesPerWord) - 15.59
            : 8.0;

        // Ensure scores are in valid ranges
        fleschReadingEase = Math.Max(0, Math.Min(100, fleschReadingEase));
        fleschKincaidGrade = Math.Max(0, Math.Min(20, fleschKincaidGrade));

        return new Dictionary<string, double>
        {
            ["fleschReadingEase"] = fleschReadingEase,
            ["fleschKincaidGrade"] = fleschKincaidGrade,
            ["avgWordsPerSentence"] = avgWordsPerSentence,
            ["avgSyllablesPerWord"] = avgSyllablesPerWord,
            ["totalWords"] = words.Count,
            ["totalSentences"] = sentences.Count
        };
    }

    /// <summary>
    /// Analyzes the strength of the opening hook
    /// </summary>
    private double AnalyzeHookStrength(string script)
    {
        var lines = script.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (lines.Count == 0) return 0;

        var firstFewLines = string.Join(" ", lines.Take(3));
        var hookScore = 50.0; // Base score

        // Check for hook elements
        if (ContainsQuestion(firstFewLines)) hookScore += 15;
        if (ContainsStatistic(firstFewLines)) hookScore += 10;
        if (ContainsBoldStatement(firstFewLines)) hookScore += 10;
        if (ContainsPromise(firstFewLines)) hookScore += 10;
        if (StartsWithAction(firstFewLines)) hookScore += 5;

        // Penalties
        if (IsGenericOpening(firstFewLines)) hookScore -= 15;
        if (IsTooLong(firstFewLines)) hookScore -= 10;

        return Math.Max(0, Math.Min(100, hookScore));
    }

    /// <summary>
    /// Detects the storytelling framework used in the script
    /// </summary>
    private StoryFrameworkType? DetectStoryFramework(string script)
    {
        var lowerScript = script.ToLower();

        // Check for problem-solution indicators
        if (lowerScript.Contains("problem") && lowerScript.Contains("solution"))
        {
            return StoryFrameworkType.ProblemSolution;
        }

        // Check for before-after pattern
        if (lowerScript.Contains("before") && lowerScript.Contains("after"))
        {
            return StoryFrameworkType.BeforeAfter;
        }

        // Check for AIDA pattern
        if (HasAIDAPattern(lowerScript))
        {
            return StoryFrameworkType.AIDA;
        }

        // Check for three-act structure
        if (HasThreeActStructure(script))
        {
            return StoryFrameworkType.ThreeAct;
        }

        // Default to chronological if no clear pattern
        return StoryFrameworkType.Chronological;
    }

    /// <summary>
    /// Analyzes the emotional curve throughout the script
    /// </summary>
    private async Task<List<EmotionalPoint>> AnalyzeEmotionalCurveAsync(
        string script,
        string? contentType,
        CancellationToken ct)
    {
        var scenes = SplitIntoScenes(script);
        var emotionalCurve = new List<EmotionalPoint>();

        for (int i = 0; i < scenes.Count; i++)
        {
            var timePosition = scenes.Count > 1 ? (double)i / (scenes.Count - 1) : 0.5;
            var tone = DetectEmotionalTone(scenes[i]);
            var intensity = CalculateEmotionalIntensity(scenes[i]);

            emotionalCurve.Add(new EmotionalPoint(
                TimePosition: timePosition,
                Tone: tone,
                Intensity: intensity,
                Context: GetSceneContext(scenes[i])
            ));
        }

        return emotionalCurve;
    }

    /// <summary>
    /// Uses AI to score script quality aspects
    /// </summary>
    private async Task<Dictionary<string, double>> GetAiQualityScoresAsync(
        string script,
        string? contentType,
        string? targetAudience,
        string? currentTone,
        CancellationToken ct)
    {
        // For now, use heuristic scoring
        // In a full implementation, this would use LLM for more accurate scoring
        var scores = new Dictionary<string, double>
        {
            ["structure"] = AnalyzeStructureScore(script),
            ["emotionalCurve"] = 70.0, // Placeholder
            ["engagement"] = AnalyzeEngagementScore(script),
            ["clarity"] = CalculateReadabilityMetrics(script)["fleschReadingEase"]
        };

        return scores;
    }

    /// <summary>
    /// Identifies issues in the script
    /// </summary>
    private List<string> IdentifyIssues(
        string script,
        Dictionary<string, double> readability,
        double hookStrength,
        Dictionary<string, double> qualityScores)
    {
        var issues = new List<string>();

        if (hookStrength < 40)
        {
            issues.Add("Opening hook is weak - consider adding a compelling question or statement");
        }

        if (readability["fleschKincaidGrade"] > 12)
        {
            issues.Add("Reading level is too high - simplify language for broader accessibility");
        }

        if (readability["avgWordsPerSentence"] > 25)
        {
            issues.Add("Sentences are too long - break them up for better clarity");
        }

        if (qualityScores["structure"] < 50)
        {
            issues.Add("Script lacks clear structure - consider applying a storytelling framework");
        }

        if (qualityScores["engagement"] < 50)
        {
            issues.Add("Engagement could be improved - add more questions, examples, or personal touches");
        }

        return issues;
    }

    /// <summary>
    /// Identifies strengths in the script
    /// </summary>
    private List<string> IdentifyStrengths(
        string script,
        Dictionary<string, double> readability,
        double hookStrength,
        Dictionary<string, double> qualityScores)
    {
        var strengths = new List<string>();

        if (hookStrength >= 70)
        {
            strengths.Add("Strong opening hook that grabs attention");
        }

        if (readability["fleschReadingEase"] >= 60)
        {
            strengths.Add("Easy to read and understand");
        }

        if (qualityScores["structure"] >= 70)
        {
            strengths.Add("Well-structured narrative flow");
        }

        if (qualityScores["engagement"] >= 70)
        {
            strengths.Add("Engaging content with good audience connection");
        }

        return strengths;
    }

    // Helper methods

    private List<string> SplitIntoSentences(string text)
    {
        return Regex.Split(text, @"[.!?]+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private List<string> SplitIntoWords(string text)
    {
        return Regex.Split(text, @"\s+")
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .ToList();
    }

    private int CountSyllables(string word)
    {
        word = word.ToLower().Trim();
        if (word.Length <= 3) return 1;

        word = Regex.Replace(word, @"[^a-z]", "");
        var vowels = new[] { 'a', 'e', 'i', 'o', 'u', 'y' };
        
        var count = 0;
        var previousWasVowel = false;

        foreach (var c in word)
        {
            var isVowel = vowels.Contains(c);
            if (isVowel && !previousWasVowel)
            {
                count++;
            }
            previousWasVowel = isVowel;
        }

        if (word.EndsWith("e"))
        {
            count--;
        }

        return Math.Max(1, count);
    }

    private bool ContainsQuestion(string text)
    {
        return text.Contains('?') || 
               Regex.IsMatch(text, @"\b(what|why|how|when|where|who)\b", RegexOptions.IgnoreCase);
    }

    private bool ContainsStatistic(string text)
    {
        return Regex.IsMatch(text, @"\d+%|\d+\s*(percent|million|billion|thousand)");
    }

    private bool ContainsBoldStatement(string text)
    {
        var boldWords = new[] { "never", "always", "shocking", "incredible", "amazing", "revolutionary" };
        return boldWords.Any(w => text.ToLower().Contains(w));
    }

    private bool ContainsPromise(string text)
    {
        var promiseWords = new[] { "learn", "discover", "find out", "show you", "teach you", "reveal" };
        return promiseWords.Any(w => text.ToLower().Contains(w));
    }

    private bool StartsWithAction(string text)
    {
        var actionWords = new[] { "imagine", "picture", "think about", "consider", "look at" };
        return actionWords.Any(w => text.ToLower().StartsWith(w));
    }

    private bool IsGenericOpening(string text)
    {
        var generic = new[] { "today we're going to", "in this video", "welcome back", "hi everyone" };
        return generic.Any(g => text.ToLower().Contains(g));
    }

    private bool IsTooLong(string text)
    {
        var words = SplitIntoWords(text);
        return words.Count > 50; // First few lines shouldn't exceed ~50 words
    }

    private bool HasAIDAPattern(string script)
    {
        var hasAttention = ContainsQuestion(script.Substring(0, Math.Min(200, script.Length)));
        var hasInterest = script.ToLower().Contains("because") || script.ToLower().Contains("here's why");
        var hasDesire = script.ToLower().Contains("benefit") || script.ToLower().Contains("help you");
        var hasAction = script.ToLower().Contains("try") || script.ToLower().Contains("start") 
                     || script.ToLower().Contains("click") || script.ToLower().Contains("subscribe");
        
        return hasAttention && hasInterest && hasAction;
    }

    private bool HasThreeActStructure(string script)
    {
        var scenes = SplitIntoScenes(script);
        return scenes.Count >= 3;
    }

    private List<string> SplitIntoScenes(string script)
    {
        // Split by markdown headers or blank lines
        return Regex.Split(script, @"(?:^|\n)##\s+|\n\n+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private EmotionalTone DetectEmotionalTone(string text)
    {
        var lower = text.ToLower();
        
        if (lower.Contains("exciting") || lower.Contains("amazing")) return EmotionalTone.Excited;
        if (lower.Contains("problem") || lower.Contains("issue")) return EmotionalTone.Concerned;
        if (lower.Contains("hope") || lower.Contains("better")) return EmotionalTone.Hopeful;
        if (lower.Contains("solution") || lower.Contains("answer")) return EmotionalTone.Satisfied;
        if (lower.Contains("inspire") || lower.Contains("dream")) return EmotionalTone.Inspired;
        if (lower.Contains("?")) return EmotionalTone.Curious;
        
        return EmotionalTone.Neutral;
    }

    private double CalculateEmotionalIntensity(string text)
    {
        var intensityWords = new[] { "very", "extremely", "incredibly", "absolutely", "completely" };
        var count = intensityWords.Count(w => text.ToLower().Contains(w));
        
        var exclamations = text.Count(c => c == '!');
        
        return Math.Min(100, 40 + (count * 10) + (exclamations * 5));
    }

    private string GetSceneContext(string scene)
    {
        var firstLine = scene.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));
        return firstLine?.Substring(0, Math.Min(100, firstLine.Length)) ?? "";
    }

    private double AnalyzeStructureScore(string script)
    {
        var score = 50.0;
        
        var scenes = SplitIntoScenes(script);
        if (scenes.Count >= 3) score += 20;
        
        if (script.Contains("introduction") || script.StartsWith("#")) score += 10;
        if (script.Contains("conclusion") || script.Contains("summary")) score += 10;
        
        var hasTransitions = Regex.IsMatch(script, @"\b(next|then|however|therefore|finally)\b", RegexOptions.IgnoreCase);
        if (hasTransitions) score += 10;
        
        return Math.Min(100, score);
    }

    private double AnalyzeEngagementScore(string script)
    {
        var score = 50.0;
        
        var questionCount = script.Count(c => c == '?');
        score += Math.Min(20, questionCount * 5);
        
        if (script.ToLower().Contains("you") || script.ToLower().Contains("your"))
        {
            score += 15;
        }
        
        if (ContainsStatistic(script)) score += 10;
        
        return Math.Min(100, score);
    }
}
