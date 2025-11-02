using System;
using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models.Localization;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Adjusts scene timings for language expansion/contraction
/// Some languages require 20-30% more time (German, Arabic)
/// </summary>
public class TimingAdjuster
{
    private readonly ILogger _logger;

    public TimingAdjuster(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adjust timings based on language expansion factor
    /// </summary>
    public TimingAdjustment AdjustTimings(
        List<TranslatedScriptLine> translatedLines,
        double expansionFactor,
        double maxVariance)
    {
        _logger.LogInformation("Adjusting timings with expansion factor {Factor:F2}", expansionFactor);

        var adjustment = new TimingAdjustment
        {
            ExpansionFactor = expansionFactor
        };

        if (!translatedLines.Any())
        {
            return adjustment;
        }

        var originalTotalDuration = translatedLines.Sum(l => l.OriginalDurationSeconds);
        adjustment.OriginalTotalDuration = originalTotalDuration;

        // Calculate character ratios for each line
        for (int i = 0; i < translatedLines.Count; i++)
        {
            var line = translatedLines[i];
            var sourceLength = line.SourceText.Length;
            var translatedLength = line.TranslatedText.Length;

            if (sourceLength == 0)
            {
                continue;
            }

            var lineExpansionFactor = (double)translatedLength / sourceLength;
            
            // Apply expansion to duration
            var adjustedDuration = line.OriginalDurationSeconds * lineExpansionFactor;
            
            // Check if adjustment exceeds max variance
            var variance = Math.Abs(adjustedDuration - line.OriginalDurationSeconds) / 
                          line.OriginalDurationSeconds;
            
            line.AdjustedDurationSeconds = adjustedDuration;
            line.TimingVariance = variance;

            if (variance > maxVariance)
            {
                adjustment.Warnings.Add(new TimingWarning
                {
                    Severity = variance > maxVariance * 1.5 ? 
                        TimingWarningSeverity.Critical : 
                        TimingWarningSeverity.Warning,
                    Message = $"Line {i + 1} duration change {variance:P1} exceeds threshold {maxVariance:P1}",
                    LineNumber = i
                });
            }
        }

        // Adjust start times based on cumulative durations
        double cumulativeTime = 0;
        for (int i = 0; i < translatedLines.Count; i++)
        {
            translatedLines[i].AdjustedStartSeconds = cumulativeTime;
            cumulativeTime += translatedLines[i].AdjustedDurationSeconds;
        }

        adjustment.AdjustedTotalDuration = cumulativeTime;

        // Check if compression is needed
        var totalVariance = Math.Abs(adjustment.AdjustedTotalDuration - originalTotalDuration) / 
                           originalTotalDuration;

        if (totalVariance > maxVariance)
        {
            adjustment.RequiresCompression = true;
            adjustment.CompressionSuggestions = GenerateCompressionSuggestions(
                translatedLines, 
                originalTotalDuration,
                adjustment.AdjustedTotalDuration);

            adjustment.Warnings.Add(new TimingWarning
            {
                Severity = TimingWarningSeverity.Critical,
                Message = $"Total duration change {totalVariance:P1} exceeds threshold. Consider content compression.",
            });
        }

        _logger.LogInformation("Timing adjustment complete - Original: {Original:F1}s, Adjusted: {Adjusted:F1}s, Variance: {Variance:P1}",
            adjustment.OriginalTotalDuration, adjustment.AdjustedTotalDuration, totalVariance);

        return adjustment;
    }

    private List<string> GenerateCompressionSuggestions(
        List<TranslatedScriptLine> lines,
        double targetDuration,
        double currentDuration)
    {
        var suggestions = new List<string>();

        var excessDuration = currentDuration - targetDuration;
        var compressionNeeded = excessDuration / currentDuration;

        suggestions.Add($"Content is {compressionNeeded:P1} longer than target duration");
        suggestions.Add($"Reduce total duration by {excessDuration:F1} seconds");

        // Find longest lines as candidates for compression
        var longestLines = lines
            .Select((line, index) => new { Line = line, Index = index })
            .OrderByDescending(x => x.Line.AdjustedDurationSeconds)
            .Take(3)
            .ToList();

        suggestions.Add("Consider compressing these longer sections:");
        foreach (var item in longestLines)
        {
            suggestions.Add($"  - Line {item.Index + 1}: {item.Line.AdjustedDurationSeconds:F1}s - \"{TruncateText(item.Line.TranslatedText, 50)}\"");
        }

        // Suggest specific techniques
        suggestions.Add("Compression techniques:");
        suggestions.Add("  - Remove redundant phrases");
        suggestions.Add("  - Use more concise language");
        suggestions.Add("  - Eliminate transitional phrases");
        suggestions.Add("  - Increase narration speed slightly");

        return suggestions;
    }

    private string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return text.Substring(0, maxLength - 3) + "...";
    }
}
