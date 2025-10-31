using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;
using PacingEnum = Aura.Core.Models.Pacing;
using DensityEnum = Aura.Core.Models.Density;

namespace Aura.Core.AI;

/// <summary>
/// Intelligent content advisor that uses AI to ensure video content is high-quality,
/// engaging, and doesn't feel AI-generated or like "slop".
/// </summary>
public class IntelligentContentAdvisor
{
    private readonly ILogger<IntelligentContentAdvisor> _logger;
    private readonly ILlmProvider _llmProvider;

    public IntelligentContentAdvisor(
        ILogger<IntelligentContentAdvisor> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Analyzes a script for quality issues and provides intelligent recommendations
    /// </summary>
    public async Task<ContentQualityAnalysis> AnalyzeContentQualityAsync(
        string script,
        Brief brief,
        PlanSpec spec,
        CancellationToken ct = default)
    {
        // Sanitize input for logging to prevent log forging
        var sanitizedTopic = brief.Topic?.Replace("\n", "").Replace("\r", "") ?? "null";
        _logger.LogInformation("Analyzing content quality for topic: {Topic}", sanitizedTopic);

        try
        {
            // Run multiple quality checks in parallel
            var heuristicAnalysis = AnalyzeHeuristics(script);
            var aiAnalysisTask = PerformAiQualityAnalysisAsync(script, brief, ct);

            var aiAnalysis = await aiAnalysisTask;

            // Combine heuristic and AI analysis
            var combinedScore = (heuristicAnalysis.OverallScore + aiAnalysis.OverallScore) / 2.0;
            var allIssues = heuristicAnalysis.Issues.Concat(aiAnalysis.Issues).ToList();
            var allSuggestions = heuristicAnalysis.Suggestions.Concat(aiAnalysis.Suggestions).ToList();

            var analysis = new ContentQualityAnalysis
            {
                OverallScore = combinedScore,
                AuthenticityScore = aiAnalysis.AuthenticityScore,
                EngagementScore = aiAnalysis.EngagementScore,
                ValueScore = aiAnalysis.ValueScore,
                PacingScore = aiAnalysis.PacingScore,
                OriginalityScore = aiAnalysis.OriginalityScore,
                Issues = allIssues,
                Suggestions = allSuggestions,
                Strengths = aiAnalysis.Strengths,
                PassesQualityThreshold = combinedScore >= 75.0,
                AnalyzedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Content quality analysis complete. Overall: {Overall:F1}, Authenticity: {Auth:F1}, Engagement: {Eng:F1}",
                analysis.OverallScore, analysis.AuthenticityScore, analysis.EngagementScore);

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content quality");
            throw;
        }
    }

    /// <summary>
    /// Heuristic-based analysis for common quality issues
    /// </summary>
    private ContentQualityAnalysis AnalyzeHeuristics(string script)
    {
        var issues = new List<string>();
        var suggestions = new List<string>();
        var scores = new List<double>();

        // Check for AI detection red flags
        var aiRedFlags = DetectAiRedFlags(script);
        if (aiRedFlags.Count > 0)
        {
            issues.AddRange(aiRedFlags.Select(flag => $"AI detection risk: {flag}"));
            suggestions.Add("Rewrite flagged sections to sound more natural and human");
            scores.Add(60.0);
        }
        else
        {
            scores.Add(90.0);
        }

        // Check for generic phrases
        var genericPhrases = DetectGenericPhrases(script);
        if (genericPhrases.Count > 0)
        {
            issues.Add($"Found {genericPhrases.Count} generic or cliché phrases");
            suggestions.Add("Replace generic phrases with specific, original language");
            scores.Add(70.0);
        }
        else
        {
            scores.Add(95.0);
        }

        // Check for repetitive structure
        if (HasRepetitiveStructure(script))
        {
            issues.Add("Detected repetitive sentence or paragraph structure");
            suggestions.Add("Vary sentence length and structure for more natural flow");
            scores.Add(65.0);
        }
        else
        {
            scores.Add(90.0);
        }

        // Check for sufficient specificity
        var specificityScore = AnalyzeSpecificity(script);
        if (specificityScore < 70)
        {
            issues.Add("Content lacks specific details and examples");
            suggestions.Add("Add concrete examples, data points, or specific scenarios");
            scores.Add(specificityScore);
        }
        else
        {
            scores.Add(specificityScore);
        }

        // Check pacing and rhythm
        var pacingScore = AnalyzePacing(script);
        if (pacingScore < 70)
        {
            issues.Add("Pacing feels unnatural or monotonous");
            suggestions.Add("Vary sentence length and add rhythm with strategic pauses");
            scores.Add(pacingScore);
        }
        else
        {
            scores.Add(pacingScore);
        }

        var overallScore = scores.Average();

        return new ContentQualityAnalysis
        {
            OverallScore = overallScore,
            Issues = issues,
            Suggestions = suggestions,
            PassesQualityThreshold = overallScore >= 75.0
        };
    }

    /// <summary>
    /// Detect AI-generation red flags
    /// </summary>
    private List<string> DetectAiRedFlags(string script)
    {
        var redFlags = new List<string>();

        // Check for common AI phrases
        var aiPhrases = new[]
        {
            "delve into", "delving into",
            "it's important to note",
            "it's worth noting",
            "in today's digital age",
            "in conclusion, it's clear",
            "moreover, it is essential",
            "furthermore, one must consider"
        };

        foreach (var phrase in aiPhrases)
        {
            if (script.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            {
                redFlags.Add($"Uses AI-common phrase: '{phrase}'");
            }
        }

        // Check for excessive rhetorical questions (more than one per 200 words)
        var words = script.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var questions = Regex.Matches(script, @"\?").Count;
        if (words.Length > 0 && questions / (words.Length / 200.0) > 1.5)
        {
            redFlags.Add("Excessive use of rhetorical questions");
        }

        // Check for robotic transitions
        var roboticTransitions = new[] { "firstly,", "secondly,", "thirdly,", "lastly,", "in conclusion," };
        var transitionCount = roboticTransitions.Count(t => script.Contains(t, StringComparison.OrdinalIgnoreCase));
        if (transitionCount > 2)
        {
            redFlags.Add("Uses mechanical numbered transitions");
        }

        return redFlags;
    }

    /// <summary>
    /// Detect generic or cliché phrases
    /// </summary>
    private List<string> DetectGenericPhrases(string script)
    {
        var genericPhrases = new[]
        {
            "in today's video",
            "don't forget to like and subscribe",
            "smash that like button",
            "at the end of the day",
            "game changer",
            "cutting-edge",
            "revolutionary",
            "unlock the secrets",
            "mind-blowing",
            "you won't believe"
        };

        return genericPhrases
            .Where(phrase => script.Contains(phrase, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>
    /// Check for repetitive sentence structure
    /// </summary>
    private bool HasRepetitiveStructure(string script)
    {
        var sentences = script.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 10)
            .ToList();

        if (sentences.Count < 5) return false;

        // Check if many sentences start with the same word
        var firstWords = sentences
            .Select(s => s.Split(' ').First().ToLowerInvariant())
            .ToList();

        var mostCommonFirstWord = firstWords
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .First();

        // If more than 30% of sentences start with the same word, it's repetitive
        return (double)mostCommonFirstWord.Count() / sentences.Count > 0.3;
    }

    /// <summary>
    /// Analyze content specificity (vs. vague generalizations)
    /// </summary>
    private double AnalyzeSpecificity(string script)
    {
        var score = 100.0;

        // Check for vague quantifiers
        var vagueQuantifiers = new[] { "many", "some", "several", "various", "numerous", "multiple" };
        var vagueCount = vagueQuantifiers.Sum(q => 
            Regex.Matches(script, $@"\b{q}\b", RegexOptions.IgnoreCase).Count);
        
        var words = script.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var vagueRatio = words.Length > 0 ? (double)vagueCount / words.Length : 0;
        
        score -= Math.Min(30, vagueRatio * 1000); // Penalize excessive vagueness

        // Check for specific numbers and data
        var hasNumbers = Regex.IsMatch(script, @"\b\d+(\.\d+)?(%|k|M|B)?\b");
        if (hasNumbers) score += 10;

        // Check for proper nouns and specific references
        var properNouns = Regex.Matches(script, @"\b[A-Z][a-z]+\b").Count;
        if (properNouns > words.Length * 0.05) score += 10;

        return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Analyze pacing and rhythm
    /// </summary>
    private double AnalyzePacing(string script)
    {
        var sentences = script.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 5)
            .ToList();

        if (sentences.Count < 3) return 75.0; // Not enough to analyze

        // Calculate sentence length variation
        var lengths = sentences.Select(s => s.Split(' ').Length).ToList();
        var avgLength = lengths.Average();
        var variance = lengths.Select(l => Math.Pow(l - avgLength, 2)).Average();
        var stdDev = Math.Sqrt(variance);

        // Good pacing has variety (coefficient of variation between 0.3 and 0.7)
        var coefficientOfVariation = stdDev / avgLength;
        
        if (coefficientOfVariation < 0.2)
        {
            return 60.0; // Too monotonous
        }
        else if (coefficientOfVariation > 0.8)
        {
            return 70.0; // Too erratic
        }
        else
        {
            return 90.0; // Good variety
        }
    }

    /// <summary>
    /// Perform AI-powered quality analysis
    /// </summary>
    private async Task<ContentQualityAnalysis> PerformAiQualityAnalysisAsync(
        string script,
        Brief brief,
        CancellationToken ct)
    {
        try
        {
            var prompt = EnhancedPromptTemplates.BuildQualityValidationPrompt(script, brief.Tone);
            
            var analysisBrief = new Brief(
                Topic: "Quality Analysis",
                Audience: null,
                Goal: null,
                Tone: "analytical",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(1),
                Pacing: PacingEnum.Conversational,
                Density: DensityEnum.Balanced,
                Style: prompt
            );

            var response = await _llmProvider.DraftScriptAsync(analysisBrief, planSpec, ct);

            return ParseAiQualityAnalysis(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI quality analysis failed, using default scores");
            return new ContentQualityAnalysis
            {
                OverallScore = 75.0,
                AuthenticityScore = 75.0,
                EngagementScore = 75.0,
                ValueScore = 75.0,
                PacingScore = 75.0,
                OriginalityScore = 75.0
            };
        }
    }

    /// <summary>
    /// Parse AI quality analysis response
    /// </summary>
    private ContentQualityAnalysis ParseAiQualityAnalysis(string response)
    {
        var analysis = new ContentQualityAnalysis();

        // Extract overall score
        var scoreMatch = Regex.Match(response, @"overall.*?score.*?(\d+)", RegexOptions.IgnoreCase);
        if (scoreMatch.Success && int.TryParse(scoreMatch.Groups[1].Value, out var score))
        {
            analysis.OverallScore = score;
        }
        else
        {
            analysis.OverallScore = 75.0; // Default
        }

        // Extract component scores
        analysis.AuthenticityScore = ExtractScore(response, "authenticity") ?? 75.0;
        analysis.EngagementScore = ExtractScore(response, "engagement") ?? 75.0;
        analysis.ValueScore = ExtractScore(response, "value") ?? 75.0;
        analysis.PacingScore = ExtractScore(response, "pacing") ?? 75.0;
        analysis.OriginalityScore = ExtractScore(response, "originality") ?? 75.0;

        // Extract issues
        analysis.Issues = ExtractBulletPoints(response, "issues");

        // Extract suggestions
        analysis.Suggestions = ExtractBulletPoints(response, "suggestions");

        // Extract strengths
        analysis.Strengths = ExtractBulletPoints(response, "works well");

        return analysis;
    }

    private double? ExtractScore(string text, string category)
    {
        var pattern = $@"{category}.*?(\d+)";
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
        if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
        {
            return score;
        }
        return null;
    }

    private List<string> ExtractBulletPoints(string text, string section)
    {
        var items = new List<string>();
        var sectionMatch = Regex.Match(text, $@"{section}:?(.*?)(?=\n\n|\z)", 
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        
        if (sectionMatch.Success)
        {
            var content = sectionMatch.Groups[1].Value;
            var bulletPoints = Regex.Matches(content, @"[-•*]\s*(.+?)(?=\n[-•*]|\n\n|\z)", RegexOptions.Singleline);
            
            foreach (Match bullet in bulletPoints)
            {
                var item = bullet.Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(item))
                {
                    items.Add(item);
                }
            }
        }

        return items;
    }

    /// <summary>
    /// Analyze script for TTS-specific quality metrics
    /// </summary>
    public TtsQualityMetrics AnalyzeTtsQuality(string script)
    {
        _logger.LogInformation("Analyzing TTS quality metrics");
        
        var metrics = new TtsQualityMetrics();
        var issues = new List<string>();
        var suggestions = new List<string>();
        
        // Analyze readability for TTS
        var readabilityScore = AnalyzeTtsReadability(script, issues, suggestions);
        metrics.ReadabilityScore = readabilityScore;
        
        // Analyze pronunciation complexity
        var pronunciationComplexity = AnalyzePronunciationComplexity(script, issues, suggestions);
        metrics.PronunciationComplexity = pronunciationComplexity;
        
        // Analyze sentence structure for TTS
        var sentenceStructureScore = AnalyzeTtsSentenceStructure(script, issues, suggestions);
        metrics.SentenceStructureScore = sentenceStructureScore;
        
        // Analyze natural flow
        var naturalFlowScore = AnalyzeNaturalFlow(script, issues, suggestions);
        metrics.NaturalFlowScore = naturalFlowScore;
        
        metrics.IssuesDetected = issues.Count;
        metrics.TtsIssues = issues;
        metrics.TtsSuggestions = suggestions;
        
        _logger.LogInformation(
            "TTS quality analysis complete. Readability: {Read:F1}, Pronunciation: {Pron:F1}, Structure: {Struct:F1}, Flow: {Flow:F1}",
            readabilityScore, pronunciationComplexity, sentenceStructureScore, naturalFlowScore);
        
        return metrics;
    }

    private double AnalyzeTtsReadability(string script, List<string> issues, List<string> suggestions)
    {
        var score = 100.0;
        var sentences = script.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        
        // Check for overly long sentences
        var longSentences = sentences.Where(s => s.Split(' ').Length > 25).ToList();
        if (longSentences.Count > 0)
        {
            score -= Math.Min(30, longSentences.Count * 10);
            issues.Add($"{longSentences.Count} sentences exceed 25 words");
            suggestions.Add("Break long sentences into shorter, more natural phrases");
        }
        
        // Check for complex punctuation patterns
        if (Regex.Matches(script, @"[;:]").Count > sentences.Length * 0.5)
        {
            score -= 15;
            issues.Add("Heavy use of semicolons and colons may confuse TTS");
            suggestions.Add("Use simpler punctuation like commas and periods");
        }
        
        return Math.Max(0, score);
    }

    private double AnalyzePronunciationComplexity(string script, List<string> issues, List<string> suggestions)
    {
        var complexity = 0.0;
        
        // Check for technical terms that might be hard to pronounce
        var technicalTerms = Regex.Matches(script, @"\b[A-Z][a-z]+[A-Z][a-zA-Z]*\b").Count;
        complexity += technicalTerms * 5;
        if (technicalTerms > 5)
        {
            issues.Add($"{technicalTerms} technical terms may need pronunciation guidance");
            suggestions.Add("Consider adding pronunciation hints for technical terms");
        }
        
        // Check for acronyms
        var acronyms = Regex.Matches(script, @"\b[A-Z]{2,}\b").Count;
        complexity += acronyms * 3;
        if (acronyms > 3)
        {
            issues.Add($"{acronyms} acronyms detected");
            suggestions.Add("Spell out acronyms on first use or provide pronunciation hints");
        }
        
        // Check for complex consonant clusters
        var complexClusters = Regex.Matches(script, @"\b\w*[bcdfghjklmnpqrstvwxyz]{4,}\w*\b", RegexOptions.IgnoreCase).Count;
        complexity += complexClusters * 2;
        if (complexClusters > 5)
        {
            issues.Add($"{complexClusters} words with complex consonant clusters");
            suggestions.Add("Consider simpler word alternatives for difficult pronunciations");
        }
        
        return Math.Max(0, Math.Min(100, complexity));
    }

    private double AnalyzeTtsSentenceStructure(string script, List<string> issues, List<string> suggestions)
    {
        var score = 100.0;
        var sentences = script.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 10)
            .ToList();
        
        if (sentences.Count < 2)
        {
            return score;
        }
        
        // Check for repetitive sentence structure
        var firstWords = sentences.Select(s => s.Split(' ').First().ToLowerInvariant()).ToList();
        var mostCommon = firstWords.GroupBy(w => w).OrderByDescending(g => g.Count()).First();
        
        if ((double)mostCommon.Count() / sentences.Count > 0.4)
        {
            score -= 20;
            issues.Add("Repetitive sentence structure detected");
            suggestions.Add("Vary sentence beginnings and structure for more natural speech");
        }
        
        // Check for missing pauses (commas, etc.)
        var pauseRatio = (double)Regex.Matches(script, @"[,;:]").Count / sentences.Count;
        if (pauseRatio < 0.5)
        {
            score -= 15;
            issues.Add("Insufficient natural pauses in speech");
            suggestions.Add("Add commas and natural pauses to improve TTS delivery");
        }
        
        return Math.Max(0, score);
    }

    private double AnalyzeNaturalFlow(string script, List<string> issues, List<string> suggestions)
    {
        var score = 100.0;
        
        // Check for robotic transition words
        var roboticTransitions = new[] { "firstly", "secondly", "thirdly", "lastly", "in conclusion" };
        var roboticCount = roboticTransitions.Count(t => script.Contains(t, StringComparison.OrdinalIgnoreCase));
        
        if (roboticCount > 2)
        {
            score -= 20;
            issues.Add("Mechanical transition words detected");
            suggestions.Add("Use more natural conversational transitions");
        }
        
        // Check for AI-common phrases that sound unnatural when spoken
        var unnaturalPhrases = new[] { "delve into", "it's important to note", "in today's digital age" };
        var unnaturalCount = unnaturalPhrases.Count(p => script.Contains(p, StringComparison.OrdinalIgnoreCase));
        
        if (unnaturalCount > 0)
        {
            score -= unnaturalCount * 15;
            issues.Add("Unnatural phrases detected that sound awkward when spoken");
            suggestions.Add("Replace formal phrases with conversational language");
        }
        
        // Check for varied sentence length (good for natural speech)
        var sentences = script.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split(' ').Length)
            .ToList();
        
        if (sentences.Count > 2)
        {
            var variance = sentences.Select(l => Math.Pow(l - sentences.Average(), 2)).Average();
            var stdDev = Math.Sqrt(variance);
            var cv = stdDev / sentences.Average();
            
            if (cv < 0.2)
            {
                score -= 15;
                issues.Add("Monotonous sentence length pattern");
                suggestions.Add("Vary sentence length for more dynamic speech rhythm");
            }
        }
        
        return Math.Max(0, score);
    }
}

/// <summary>
/// Content quality analysis result
/// </summary>
public class ContentQualityAnalysis
{
    public double OverallScore { get; set; }
    public double AuthenticityScore { get; set; }
    public double EngagementScore { get; set; }
    public double ValueScore { get; set; }
    public double PacingScore { get; set; }
    public double OriginalityScore { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public bool PassesQualityThreshold { get; set; }
    public DateTime AnalyzedAt { get; set; }
    
    /// <summary>
    /// TTS-specific quality metrics
    /// </summary>
    public TtsQualityMetrics? TtsMetrics { get; set; }
}

/// <summary>
/// TTS-specific quality metrics
/// </summary>
public class TtsQualityMetrics
{
    /// <summary>
    /// TTS readability score (0-100) - how easy the text is to synthesize naturally
    /// </summary>
    public double ReadabilityScore { get; set; }
    
    /// <summary>
    /// Pronunciation complexity score (0-100) - lower is better for TTS
    /// </summary>
    public double PronunciationComplexity { get; set; }
    
    /// <summary>
    /// Sentence structure score (0-100) - optimal sentence length and structure
    /// </summary>
    public double SentenceStructureScore { get; set; }
    
    /// <summary>
    /// Natural flow score (0-100) - how naturally the text flows when spoken
    /// </summary>
    public double NaturalFlowScore { get; set; }
    
    /// <summary>
    /// Number of potential TTS issues detected
    /// </summary>
    public int IssuesDetected { get; set; }
    
    /// <summary>
    /// List of specific TTS issues
    /// </summary>
    public List<string> TtsIssues { get; set; } = new();
    
    /// <summary>
    /// Suggestions for improving TTS quality
    /// </summary>
    public List<string> TtsSuggestions { get; set; } = new();
}
