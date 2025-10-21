using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content;

/// <summary>
/// Analyzes video scripts for quality metrics including coherence, pacing, engagement, and readability
/// </summary>
public class ContentAnalyzer
{
    private readonly ILogger<ContentAnalyzer> _logger;
    private readonly ILlmProvider _llmProvider;

    public ContentAnalyzer(ILogger<ContentAnalyzer> logger, ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Analyzes a script and returns detailed quality scores and suggestions
    /// </summary>
    public async Task<ScriptAnalysis> AnalyzeScriptAsync(string script, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting script analysis");

        try
        {
            // Calculate statistics first
            var statistics = CalculateStatistics(script);

            // Create analysis prompt
            var prompt = $@"Analyze this video script for coherence, pacing, engagement, and readability. 
Provide specific scores (0-100) and suggestions for improvement.

Script:
{script}

Please respond in the following format:
COHERENCE: [score 0-100]
PACING: [score 0-100]
ENGAGEMENT: [score 0-100]
READABILITY: [score 0-100]
ISSUES:
- [list specific issues found, one per line]
SUGGESTIONS:
- [list specific improvement suggestions, one per line]";

            // Create a minimal brief and plan spec for the LLM call
            var brief = new Brief(
                Topic: "Script Analysis",
                Audience: null,
                Goal: null,
                Tone: "analytical",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(5),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: prompt
            );

            // Call LLM for analysis
            var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);

            // Parse the response
            var (coherence, pacing, engagement, readability, issues, suggestions) = ParseAnalysisResponse(response);

            // Calculate overall quality score
            var overallScore = (coherence + pacing + engagement + readability) / 4.0;

            var analysis = new ScriptAnalysis(
                CoherenceScore: coherence,
                PacingScore: pacing,
                EngagementScore: engagement,
                ReadabilityScore: readability,
                OverallQualityScore: overallScore,
                Issues: issues,
                Suggestions: suggestions,
                Statistics: statistics
            );

            _logger.LogInformation(
                "Script analysis complete. Overall quality: {Score:F1}, Coherence: {Coherence:F1}, Pacing: {Pacing:F1}, Engagement: {Engagement:F1}, Readability: {Readability:F1}",
                overallScore, coherence, pacing, engagement, readability
            );

            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing script");
            
            // Return default analysis on error
            var statistics = CalculateStatistics(script);
            return new ScriptAnalysis(
                CoherenceScore: 75,
                PacingScore: 75,
                EngagementScore: 75,
                ReadabilityScore: 75,
                OverallQualityScore: 75,
                Issues: new List<string> { "Unable to perform detailed analysis" },
                Suggestions: new List<string> { "Review script manually for improvements" },
                Statistics: statistics
            );
        }
    }

    private ScriptStatistics CalculateStatistics(string script)
    {
        // Count words
        var words = Regex.Split(script, @"\s+").Where(w => !string.IsNullOrWhiteSpace(w)).ToArray();
        var totalWords = words.Length;

        // Count scenes (marked with ##)
        var sceneCount = Regex.Matches(script, @"^##\s+", RegexOptions.Multiline).Count;
        if (sceneCount == 0) sceneCount = 1; // At least one scene

        var avgWordsPerScene = (double)totalWords / sceneCount;

        // Estimate reading time at 150 words per minute
        var readingTimeMinutes = totalWords / 150.0;
        var estimatedReadingTime = TimeSpan.FromMinutes(readingTimeMinutes);

        // Calculate complexity score based on average word length and sentence length
        var avgWordLength = words.Length > 0 ? words.Average(w => w.Length) : 0;
        var sentences = Regex.Split(script, @"[.!?]+").Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
        var avgSentenceLength = sentences.Length > 0 ? (double)words.Length / sentences.Length : 0;
        
        // Complexity score: higher word length and sentence length = higher complexity
        var complexityScore = Math.Min(100, (avgWordLength * 10) + (avgSentenceLength * 0.5));

        return new ScriptStatistics(
            TotalWordCount: totalWords,
            AverageWordsPerScene: avgWordsPerScene,
            EstimatedReadingTime: estimatedReadingTime,
            ComplexityScore: complexityScore
        );
    }

    private (double coherence, double pacing, double engagement, double readability, List<string> issues, List<string> suggestions) 
        ParseAnalysisResponse(string response)
    {
        var coherence = ExtractScore(response, "COHERENCE");
        var pacing = ExtractScore(response, "PACING");
        var engagement = ExtractScore(response, "ENGAGEMENT");
        var readability = ExtractScore(response, "READABILITY");

        var issues = ExtractListItems(response, "ISSUES:", "SUGGESTIONS:");
        var suggestions = ExtractListItems(response, "SUGGESTIONS:", null);

        return (coherence, pacing, engagement, readability, issues, suggestions);
    }

    private double ExtractScore(string response, string label)
    {
        // Try to find "LABEL: 85" or "LABEL: [85]" format
        var pattern = $@"{label}:\s*\[?(\d+)\]?";
        var match = Regex.Match(response, pattern, RegexOptions.IgnoreCase);
        
        if (match.Success && double.TryParse(match.Groups[1].Value, out var score))
        {
            return Math.Clamp(score, 0, 100);
        }

        // Default score if not found
        return 75.0;
    }

    private List<string> ExtractListItems(string response, string startMarker, string? endMarker)
    {
        var items = new List<string>();

        var startIndex = response.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return items;

        startIndex += startMarker.Length;

        var endIndex = endMarker != null 
            ? response.IndexOf(endMarker, startIndex, StringComparison.OrdinalIgnoreCase)
            : response.Length;
        
        if (endIndex == -1) endIndex = response.Length;

        var section = response.Substring(startIndex, endIndex - startIndex);
        var lines = section.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("-") || trimmed.StartsWith("•") || trimmed.StartsWith("*"))
            {
                items.Add(trimmed.Substring(1).Trim());
            }
            else if (!string.IsNullOrWhiteSpace(trimmed))
            {
                items.Add(trimmed);
            }
        }

        // Add defaults if empty
        if (items.Count == 0 && startMarker.Contains("ISSUES"))
        {
            items.Add("No major issues detected");
        }
        else if (items.Count == 0 && startMarker.Contains("SUGGESTIONS"))
        {
            items.Add("Consider reviewing script for potential improvements");
        }

        return items;
    }
}
