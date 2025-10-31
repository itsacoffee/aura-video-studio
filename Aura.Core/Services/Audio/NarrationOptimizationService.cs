using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Voice;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audio;

/// <summary>
/// Service for optimizing narration text for TTS synthesis excellence
/// Uses LLMs to improve naturalness, pacing, and emotional delivery
/// </summary>
public class NarrationOptimizationService
{
    private readonly ILogger<NarrationOptimizationService> _logger;
    private readonly ILlmProvider _llmProvider;

    private static readonly HashSet<string> CommonAcronyms = new(StringComparer.OrdinalIgnoreCase)
    {
        "AI", "API", "CEO", "CFO", "CTO", "USA", "UK", "EU", "UN", "NASA", "FBI", "CIA",
        "HTML", "CSS", "JSON", "XML", "SQL", "HTTP", "HTTPS", "REST", "GPU", "CPU",
        "RAM", "ROM", "USB", "DVD", "CD", "TV", "PC", "OS", "iOS", "GPS"
    };

    private static readonly HashSet<string> Homographs = new(StringComparer.OrdinalIgnoreCase)
    {
        "read", "lead", "live", "wind", "tear", "bow", "close", "desert", "present",
        "record", "permit", "produce", "object", "subject", "content", "minute"
    };

    public NarrationOptimizationService(
        ILogger<NarrationOptimizationService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Optimize script lines for TTS synthesis
    /// </summary>
    public async Task<NarrationOptimizationResult> OptimizeForTtsAsync(
        IEnumerable<ScriptLine> lines,
        VoiceSpec voiceSpec,
        VoiceDescriptor? voiceDescriptor,
        NarrationOptimizationConfig? config,
        CancellationToken ct = default)
    {
        config ??= new NarrationOptimizationConfig();
        var stopwatch = Stopwatch.StartNew();
        var linesList = lines.ToList();

        _logger.LogInformation(
            "Starting narration optimization for {LineCount} lines with voice: {VoiceName}",
            linesList.Count, voiceSpec.VoiceName);

        try
        {
            var optimizedLines = new List<OptimizedScriptLine>();
            var issuesFixed = new List<string>();
            var warnings = new List<string>();
            var totalOptimizations = 0;

            foreach (var line in linesList)
            {
                var optimizedLine = await OptimizeSingleLineAsync(
                    line, voiceSpec, voiceDescriptor, config, ct);
                
                optimizedLines.Add(optimizedLine);
                totalOptimizations += optimizedLine.ActionsApplied.Count;

                if (optimizedLine.WasModified)
                {
                    issuesFixed.Add($"Line {line.SceneIndex}: Optimized for TTS naturalness");
                }
            }

            stopwatch.Stop();

            var score = CalculateOptimizationScore(linesList, optimizedLines, config);

            _logger.LogInformation(
                "Optimization complete in {ElapsedMs}ms. Score: {Score:F1}, Optimizations: {Count}",
                stopwatch.ElapsedMilliseconds, score, totalOptimizations);

            return new NarrationOptimizationResult
            {
                OptimizedLines = optimizedLines,
                OriginalLines = linesList,
                OptimizationScore = score,
                ProcessingTime = stopwatch.Elapsed,
                OptimizationsApplied = totalOptimizations,
                IssuesFixed = issuesFixed,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during narration optimization");
            throw;
        }
    }

    /// <summary>
    /// Optimize a single script line
    /// </summary>
    private async Task<OptimizedScriptLine> OptimizeSingleLineAsync(
        ScriptLine line,
        VoiceSpec voiceSpec,
        VoiceDescriptor? voiceDescriptor,
        NarrationOptimizationConfig config,
        CancellationToken ct)
    {
        var actions = new List<OptimizationAction>();
        var pronunciationHints = new Dictionary<string, string>();
        NarrationTone? emotionalTone = null;
        double emotionConfidence = 0.0;

        var optimizedText = line.Text;

        // Step 1: Detect TTS compatibility issues
        var issues = DetectTtsIssues(optimizedText, config);

        // Step 2: Use LLM to rewrite for TTS naturalness
        if (issues.Any() || IsComplexSentence(optimizedText, config))
        {
            var rewriteResult = await RewriteForTtsAsync(
                optimizedText, voiceSpec, voiceDescriptor, issues, config, ct);
            
            optimizedText = rewriteResult.OptimizedText;
            actions.AddRange(rewriteResult.ActionsApplied);
            
            foreach (var hint in rewriteResult.PronunciationHints)
            {
                pronunciationHints[hint.Key] = hint.Value;
            }
        }

        // Step 3: Detect emotional tone
        if (config.EnableEmotionalToneTagging)
        {
            var toneResult = DetectEmotionalTone(optimizedText);
            if (toneResult.Confidence >= config.MinEmotionConfidence)
            {
                emotionalTone = toneResult.Tone;
                emotionConfidence = toneResult.Confidence;
                actions.Add(OptimizationAction.EmotionalToneTagging);
            }
        }

        // Step 4: Generate SSML if supported
        string? ssmlMarkup = null;
        if (config.EnableSsml && voiceDescriptor?.SupportedFeatures.HasFlag(VoiceFeatures.Prosody) == true)
        {
            ssmlMarkup = GenerateSsml(optimizedText, emotionalTone, voiceSpec);
            if (ssmlMarkup != null)
            {
                actions.Add(OptimizationAction.SsmlEnhancement);
            }
        }

        return new OptimizedScriptLine
        {
            SceneIndex = line.SceneIndex,
            OriginalText = line.Text,
            OptimizedText = optimizedText,
            Start = line.Start,
            Duration = line.Duration,
            EmotionalTone = emotionalTone,
            EmotionConfidence = emotionConfidence,
            PronunciationHints = pronunciationHints,
            SsmlMarkup = ssmlMarkup,
            ActionsApplied = actions
        };
    }

    /// <summary>
    /// Use LLM to rewrite text for TTS naturalness
    /// </summary>
    private async Task<LlmRewriteResult> RewriteForTtsAsync(
        string text,
        VoiceSpec voiceSpec,
        VoiceDescriptor? voiceDescriptor,
        List<TtsCompatibilityIssue> issues,
        NarrationOptimizationConfig config,
        CancellationToken ct)
    {
        var prompt = BuildOptimizationPrompt(text, voiceSpec, voiceDescriptor, issues, config);
        
        try
        {
            var brief = new Brief(
                Topic: "TTS Optimization",
                Audience: null,
                Goal: "Optimize text for natural speech synthesis",
                Tone: GetVoiceTone(voiceDescriptor),
                Language: "English",
                Aspect: Aspect.Widescreen16x9
            );

            var spec = new PlanSpec(
                TargetDuration: TimeSpan.FromSeconds(30),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: "natural-speech"
            );

            var llmResponse = await _llmProvider.DraftScriptAsync(brief, spec, ct);
            
            return ParseLlmResponse(llmResponse, text);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM rewrite failed, using heuristic optimization");
            return HeuristicOptimization(text, issues, config);
        }
    }

    /// <summary>
    /// Build prompt for LLM optimization
    /// </summary>
    private string BuildOptimizationPrompt(
        string text,
        VoiceSpec voiceSpec,
        VoiceDescriptor? voiceDescriptor,
        List<TtsCompatibilityIssue> issues,
        NarrationOptimizationConfig config)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Optimize this text for Text-to-Speech synthesis. Make it natural, easy to speak, and engaging.");
        sb.AppendLine();
        sb.AppendLine($"Voice: {voiceSpec.VoiceName}");
        
        if (voiceDescriptor != null)
        {
            sb.AppendLine($"Voice Type: {voiceDescriptor.VoiceType}");
            sb.AppendLine($"Gender: {voiceDescriptor.Gender}");
            if (voiceDescriptor.Description != null)
            {
                sb.AppendLine($"Description: {voiceDescriptor.Description}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Original text:");
        sb.AppendLine(text);
        sb.AppendLine();
        sb.AppendLine("Requirements:");
        sb.AppendLine("- Break complex sentences (>25 words) into shorter, natural phrases");
        sb.AppendLine("- Add natural pauses using commas, ellipses, or em-dashes");
        sb.AppendLine("- Remove or rewrite tongue-twisters and difficult phonetic patterns");
        sb.AppendLine("- Use conversational vocabulary suitable for speech");
        sb.AppendLine("- Preserve 100% of the semantic meaning");
        sb.AppendLine("- Keep the same overall message and information");
        
        if (issues.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Fix these specific issues:");
            foreach (var issue in issues.Take(5))
            {
                sb.AppendLine($"- {issue.Type}: \"{issue.Text}\" - {issue.Explanation}");
            }
        }

        sb.AppendLine();
        sb.AppendLine("Return only the optimized text, nothing else.");

        return sb.ToString();
    }

    /// <summary>
    /// Parse LLM response and extract optimization details
    /// </summary>
    private const double MinResponseLengthRatio = 0.5;
    private const double MaxResponseLengthRatio = 2.0;

    private LlmRewriteResult ParseLlmResponse(string llmResponse, string originalText)
    {
        var optimizedText = llmResponse.Trim();
        var actions = new List<OptimizationAction>();
        var pronunciationHints = new Dictionary<string, string>();

        if (optimizedText.Length < originalText.Length * MinResponseLengthRatio || 
            optimizedText.Length > originalText.Length * MaxResponseLengthRatio)
        {
            _logger.LogWarning("LLM response length suspicious, using original");
            optimizedText = originalText;
        }
        else
        {
            if (CountWords(optimizedText) != CountWords(originalText))
            {
                actions.Add(OptimizationAction.SentenceSimplification);
            }

            if (CountPunctuation(optimizedText) > CountPunctuation(originalText))
            {
                actions.Add(OptimizationAction.PauseInsertion);
            }

            if (!string.Equals(optimizedText, originalText, StringComparison.Ordinal))
            {
                actions.Add(OptimizationAction.VocabularyAdjustment);
            }
        }

        ExtractPronunciationHints(optimizedText, pronunciationHints);

        return new LlmRewriteResult
        {
            OptimizedText = optimizedText,
            ActionsApplied = actions,
            PronunciationHints = pronunciationHints
        };
    }

    /// <summary>
    /// Heuristic-based optimization when LLM fails
    /// </summary>
    private LlmRewriteResult HeuristicOptimization(
        string text,
        List<TtsCompatibilityIssue> issues,
        NarrationOptimizationConfig config)
    {
        var optimizedText = text;
        var actions = new List<OptimizationAction>();
        var pronunciationHints = new Dictionary<string, string>();

        foreach (var issue in issues)
        {
            switch (issue.Type)
            {
                case TtsIssueType.LongSentence:
                    optimizedText = BreakLongSentence(optimizedText);
                    actions.Add(OptimizationAction.SentenceSimplification);
                    break;

                case TtsIssueType.MissingPauses:
                    optimizedText = AddNaturalPauses(optimizedText);
                    actions.Add(OptimizationAction.PauseInsertion);
                    break;

                case TtsIssueType.AmbiguousAcronym:
                    if (config.EnableAcronymClarification && issue.SuggestedFix != null)
                    {
                        optimizedText = optimizedText.Replace(issue.Text, issue.SuggestedFix);
                        actions.Add(OptimizationAction.AcronymClarification);
                    }
                    break;

                case TtsIssueType.NumberSpelling:
                    if (config.EnableNumberSpelling && issue.SuggestedFix != null)
                    {
                        optimizedText = optimizedText.Replace(issue.Text, issue.SuggestedFix);
                        actions.Add(OptimizationAction.NumberSpelling);
                    }
                    break;

                case TtsIssueType.TongueTwister:
                    if (config.EnableTongueTwisterDetection && issue.SuggestedFix != null)
                    {
                        optimizedText = optimizedText.Replace(issue.Text, issue.SuggestedFix);
                        actions.Add(OptimizationAction.TongueTwisterRemoval);
                    }
                    else if (config.EnableTongueTwisterDetection)
                    {
                        actions.Add(OptimizationAction.TongueTwisterRemoval);
                    }
                    break;
            }
        }

        ExtractPronunciationHints(optimizedText, pronunciationHints);

        return new LlmRewriteResult
        {
            OptimizedText = optimizedText,
            ActionsApplied = actions,
            PronunciationHints = pronunciationHints
        };
    }

    /// <summary>
    /// Detect TTS compatibility issues in text
    /// </summary>
    private List<TtsCompatibilityIssue> DetectTtsIssues(string text, NarrationOptimizationConfig config)
    {
        var issues = new List<TtsCompatibilityIssue>();

        if (IsComplexSentence(text, config))
        {
            issues.Add(new TtsCompatibilityIssue
            {
                Type = TtsIssueType.LongSentence,
                Text = text,
                Position = 0,
                Severity = 70,
                SuggestedFix = null,
                Explanation = $"Sentence has {CountWords(text)} words (recommended: <{config.MaxSentenceWords})"
            });
        }

        if (config.EnableAcronymClarification)
        {
            foreach (var acronym in CommonAcronyms)
            {
                var pattern = new Regex($@"\b{acronym}\b", RegexOptions.IgnoreCase);
                if (pattern.IsMatch(text))
                {
                    issues.Add(new TtsCompatibilityIssue
                    {
                        Type = TtsIssueType.AmbiguousAcronym,
                        Text = acronym,
                        Position = text.IndexOf(acronym, StringComparison.OrdinalIgnoreCase),
                        Severity = 40,
                        Explanation = "Acronym may need pronunciation guidance"
                    });
                }
            }
        }

        if (config.EnableHomographDisambiguation)
        {
            foreach (var homograph in Homographs)
            {
                var pattern = new Regex($@"\b{homograph}\b", RegexOptions.IgnoreCase);
                if (pattern.IsMatch(text))
                {
                    issues.Add(new TtsCompatibilityIssue
                    {
                        Type = TtsIssueType.Homograph,
                        Text = homograph,
                        Position = text.IndexOf(homograph, StringComparison.OrdinalIgnoreCase),
                        Severity = 50,
                        Explanation = "Homograph may be pronounced incorrectly without context"
                    });
                }
            }
        }

        if (config.EnableTongueTwisterDetection)
        {
            var tongueTwisters = DetectTongueTwisters(text);
            issues.AddRange(tongueTwisters);
        }

        if (config.EnableNumberSpelling)
        {
            var numberIssues = DetectNumberSpellingNeeds(text);
            issues.AddRange(numberIssues);
        }

        return issues;
    }

    /// <summary>
    /// Detect tongue-twisters using phonetic pattern analysis
    /// </summary>
    private List<TtsCompatibilityIssue> DetectTongueTwisters(string text)
    {
        var issues = new List<TtsCompatibilityIssue>();
        
        var patterns = new[]
        {
            @"(\b\w*sh\w*\s+){3,}",
            @"(\b\w*th\w*\s+){3,}",
            @"(\b\w*s\w*\s+){4,}",
            @"(\b[pbm]\w+\s+){3,}",
            @"(\b[fv]\w+\s+){3,}",
            @"(\b\w*r\w*\s+){4,}"
        };

        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                issues.Add(new TtsCompatibilityIssue
                {
                    Type = TtsIssueType.TongueTwister,
                    Text = match.Value.Trim(),
                    Position = match.Index,
                    Severity = 80,
                    Explanation = "Difficult phonetic pattern detected"
                });
            }
        }

        return issues;
    }

    /// <summary>
    /// Detect numbers that should be spelled out
    /// </summary>
    private List<TtsCompatibilityIssue> DetectNumberSpellingNeeds(string text)
    {
        var issues = new List<TtsCompatibilityIssue>();
        
        var phonePattern = new Regex(@"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b");
        var matches = phonePattern.Matches(text);
        foreach (Match match in matches)
        {
            issues.Add(new TtsCompatibilityIssue
            {
                Type = TtsIssueType.NumberSpelling,
                Text = match.Value,
                Position = match.Index,
                Severity = 60,
                SuggestedFix = SpellOutPhoneNumber(match.Value),
                Explanation = "Phone number should be spoken digit by digit"
            });
        }

        var datePattern = new Regex(@"\b\d{1,2}/\d{1,2}/\d{2,4}\b");
        matches = datePattern.Matches(text);
        foreach (Match match in matches)
        {
            issues.Add(new TtsCompatibilityIssue
            {
                Type = TtsIssueType.NumberSpelling,
                Text = match.Value,
                Position = match.Index,
                Severity = 50,
                Explanation = "Date should be spoken naturally"
            });
        }

        return issues;
    }

    /// <summary>
    /// Detect emotional tone in text
    /// </summary>
    private (NarrationTone Tone, double Confidence) DetectEmotionalTone(string text)
    {
        var lowerText = text.ToLowerInvariant();
        
        var excitedKeywords = new[] { "amazing", "incredible", "fantastic", "wow", "great", "awesome", "exciting" };
        var somberKeywords = new[] { "sadly", "unfortunately", "tragic", "difficult", "challenging", "serious" };
        var urgentKeywords = new[] { "important", "critical", "urgent", "immediately", "now", "must", "vital" };
        var relaxedKeywords = new[] { "calm", "peaceful", "gentle", "easy", "relaxed", "comfortable" };
        var cheerfulKeywords = new[] { "happy", "joyful", "delightful", "fun", "enjoy", "pleasant" };

        var scores = new Dictionary<NarrationTone, double>
        {
            { NarrationTone.Excited, CalculateKeywordScore(lowerText, excitedKeywords) },
            { NarrationTone.Somber, CalculateKeywordScore(lowerText, somberKeywords) },
            { NarrationTone.Urgent, CalculateKeywordScore(lowerText, urgentKeywords) },
            { NarrationTone.Relaxed, CalculateKeywordScore(lowerText, relaxedKeywords) },
            { NarrationTone.Cheerful, CalculateKeywordScore(lowerText, cheerfulKeywords) }
        };

        if (Regex.IsMatch(text, @"[!]{2,}"))
        {
            scores[NarrationTone.Excited] += 0.2;
        }

        if (Regex.IsMatch(text, @"\?+$"))
        {
            scores.TryAdd(NarrationTone.Thoughtful, 0.0);
            scores[NarrationTone.Thoughtful] = 0.6;
        }

        var maxScore = scores.Max(kvp => kvp.Value);
        var maxTone = scores.First(kvp => kvp.Value == maxScore).Key;

        if (maxScore < 0.3)
        {
            return (NarrationTone.Neutral, 0.9);
        }

        return (maxTone, Math.Min(maxScore, 1.0));
    }

    private double CalculateKeywordScore(string text, string[] keywords)
    {
        var count = keywords.Count(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        return Math.Min(count * 0.3, 1.0);
    }

    /// <summary>
    /// Generate SSML markup for enhanced prosody
    /// </summary>
    private string? GenerateSsml(string text, NarrationTone? tone, VoiceSpec voiceSpec)
    {
        if (tone == null)
        {
            return null;
        }

        var prosodyRate = voiceSpec.Rate switch
        {
            < 0.9 => "slow",
            > 1.1 => "fast",
            _ => "medium"
        };

        var prosodyPitch = voiceSpec.Pitch switch
        {
            < 0.9 => "low",
            > 1.1 => "high",
            _ => "medium"
        };

        var emphasisLevel = tone switch
        {
            NarrationTone.Excited => "strong",
            NarrationTone.Urgent => "strong",
            NarrationTone.Somber => "reduced",
            _ => "moderate"
        };

        return $"<speak><prosody rate=\"{prosodyRate}\" pitch=\"{prosodyPitch}\"><emphasis level=\"{emphasisLevel}\">{text}</emphasis></prosody></speak>";
    }

    /// <summary>
    /// Extract pronunciation hints from text
    /// </summary>
    private void ExtractPronunciationHints(string text, Dictionary<string, string> hints)
    {
        var technicalTermPattern = new Regex(@"\b[A-Z][a-z]+[A-Z][a-zA-Z]*\b");
        var matches = technicalTermPattern.Matches(text);
        
        foreach (Match match in matches)
        {
            if (!hints.ContainsKey(match.Value))
            {
                hints[match.Value] = $"Technical term: {match.Value}";
            }
        }
    }

    private bool IsComplexSentence(string text, NarrationOptimizationConfig config)
    {
        var wordCount = CountWords(text);
        return wordCount > config.MaxSentenceWords;
    }

    private int CountWords(string text)
    {
        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private int CountPunctuation(string text)
    {
        return text.Count(c => c is ',' or '.' or ';' or ':' or '!' or '?');
    }

    private const int DefaultMaxSentenceWords = 25;

    private string BreakLongSentence(string text)
    {
        var words = text.Split(' ');
        if (words.Length <= DefaultMaxSentenceWords)
        {
            return text;
        }

        var result = new StringBuilder();
        var currentLength = 0;

        foreach (var word in words)
        {
            result.Append(word).Append(' ');
            currentLength++;

            if (currentLength >= 15 && (word.EndsWith(',') || word.EndsWith('.')))
            {
                currentLength = 0;
            }
            else if (currentLength >= 20 && !word.EndsWith('.'))
            {
                result.Append(", ");
                currentLength = 0;
            }
        }

        return result.ToString().Trim();
    }

    private string AddNaturalPauses(string text)
    {
        var clauseConnectors = new[] { " and ", " but ", " or ", " so ", " yet " };
        
        foreach (var connector in clauseConnectors)
        {
            var pattern = $@"(?<!,){Regex.Escape(connector)}";
            text = Regex.Replace(text, pattern, "," + connector, RegexOptions.IgnoreCase);
        }

        return text;
    }

    private string SpellOutPhoneNumber(string phoneNumber)
    {
        var digits = phoneNumber.Where(char.IsDigit).ToArray();
        return string.Join(" ", digits);
    }

    private string GetVoiceTone(VoiceDescriptor? descriptor)
    {
        if (descriptor == null)
        {
            return "neutral";
        }

        if (descriptor.AvailableStyles != null && descriptor.AvailableStyles.Length > 0)
        {
            return descriptor.AvailableStyles[0];
        }

        return descriptor.Gender switch
        {
            VoiceGender.Male => "professional",
            VoiceGender.Female => "friendly",
            _ => "neutral"
        };
    }

    private double CalculateOptimizationScore(
        List<ScriptLine> originalLines,
        List<OptimizedScriptLine> optimizedLines,
        NarrationOptimizationConfig config)
    {
        if (optimizedLines.Count == 0)
        {
            return 100.0;
        }

        var score = 70.0;

        var modifiedCount = optimizedLines.Count(l => l.WasModified);
        var modificationRate = (double)modifiedCount / originalLines.Count;
        
        score += modificationRate * 10;

        var emotionalTaggingRate = optimizedLines.Count(l => l.EmotionalTone != null && 
            l.EmotionConfidence >= config.MinEmotionConfidence) / (double)optimizedLines.Count;
        score += emotionalTaggingRate * 10;

        var avgActionsPerLine = optimizedLines.Average(l => l.ActionsApplied.Count);
        score += Math.Min(avgActionsPerLine * 2, 10);

        return Math.Min(score, 100.0);
    }

    private record LlmRewriteResult
    {
        public required string OptimizedText { get; init; }
        public required List<OptimizationAction> ActionsApplied { get; init; }
        public required Dictionary<string, string> PronunciationHints { get; init; }
    }
}
