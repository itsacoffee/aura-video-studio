using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Core.Models.Audience;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Service for generating preview and comparison of content adaptations
/// Shows before/after with explanations of why changes were made
/// </summary>
public class AdaptationPreviewService
{
    private readonly ILogger<AdaptationPreviewService> _logger;

    public AdaptationPreviewService(ILogger<AdaptationPreviewService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate detailed comparison report of adaptation
    /// </summary>
    public AdaptationComparisonReport GenerateComparisonReport(ContentAdaptationResult result)
    {
        _logger.LogInformation("Generating adaptation comparison report with {ChangeCount} changes", result.Changes.Count);

        var report = new AdaptationComparisonReport
        {
            OriginalContent = result.OriginalContent,
            AdaptedContent = result.AdaptedContent,
            ProcessingTime = result.ProcessingTime,
            OverallRelevanceScore = result.OverallRelevanceScore
        };

        report.Sections = GenerateComparisonSections(result);
        report.MetricsComparison = GenerateMetricsComparison(result.OriginalMetrics, result.AdaptedMetrics);
        report.ChangesByCategory = GroupChangesByCategory(result.Changes);
        report.Summary = GenerateSummary(result);

        return report;
    }

    /// <summary>
    /// Generate side-by-side comparison sections
    /// </summary>
    private List<ComparisonSection> GenerateComparisonSections(ContentAdaptationResult result)
    {
        var sections = new List<ComparisonSection>();

        var originalParagraphs = SplitIntoParagraphs(result.OriginalContent);
        var adaptedParagraphs = SplitIntoParagraphs(result.AdaptedContent);

        for (int i = 0; i < Math.Min(originalParagraphs.Count, adaptedParagraphs.Count); i++)
        {
            var original = originalParagraphs[i];
            var adapted = adaptedParagraphs[i];

            if (!original.Equals(adapted, StringComparison.Ordinal))
            {
                var relatedChanges = result.Changes
                    .Where(c => c.OriginalText.Contains(original.Substring(0, Math.Min(50, original.Length))))
                    .ToList();

                sections.Add(new ComparisonSection
                {
                    OriginalText = original,
                    AdaptedText = adapted,
                    Changes = relatedChanges,
                    HighlightedDifferences = GenerateHighlights(original, adapted)
                });
            }
        }

        return sections;
    }

    /// <summary>
    /// Generate metrics comparison
    /// </summary>
    private MetricsComparison GenerateMetricsComparison(ReadabilityMetrics original, ReadabilityMetrics adapted)
    {
        return new MetricsComparison
        {
            FleschKincaidChange = new MetricChange
            {
                Name = "Flesch-Kincaid Grade Level",
                OriginalValue = original.FleschKincaidGradeLevel,
                AdaptedValue = adapted.FleschKincaidGradeLevel,
                Direction = adapted.FleschKincaidGradeLevel < original.FleschKincaidGradeLevel ? "Simplified" : "Advanced",
                PercentageChange = CalculatePercentageChange(original.FleschKincaidGradeLevel, adapted.FleschKincaidGradeLevel)
            },
            SmogChange = new MetricChange
            {
                Name = "SMOG Score",
                OriginalValue = original.SmogScore,
                AdaptedValue = adapted.SmogScore,
                Direction = adapted.SmogScore < original.SmogScore ? "Simplified" : "Advanced",
                PercentageChange = CalculatePercentageChange(original.SmogScore, adapted.SmogScore)
            },
            ComplexityChange = new MetricChange
            {
                Name = "Overall Complexity",
                OriginalValue = original.OverallComplexity,
                AdaptedValue = adapted.OverallComplexity,
                Direction = adapted.OverallComplexity < original.OverallComplexity ? "Reduced" : "Increased",
                PercentageChange = CalculatePercentageChange(original.OverallComplexity, adapted.OverallComplexity)
            },
            WordsPerSentenceChange = new MetricChange
            {
                Name = "Average Words Per Sentence",
                OriginalValue = original.AverageWordsPerSentence,
                AdaptedValue = adapted.AverageWordsPerSentence,
                Direction = adapted.AverageWordsPerSentence < original.AverageWordsPerSentence ? "Shorter" : "Longer",
                PercentageChange = CalculatePercentageChange(original.AverageWordsPerSentence, adapted.AverageWordsPerSentence)
            }
        };
    }

    /// <summary>
    /// Group changes by category
    /// </summary>
    private Dictionary<string, List<AdaptationChange>> GroupChangesByCategory(List<AdaptationChange> changes)
    {
        return changes
            .GroupBy(c => c.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// Generate summary of adaptations
    /// </summary>
    private string GenerateSummary(ContentAdaptationResult result)
    {
        var sb = new StringBuilder();

        sb.AppendLine("ADAPTATION SUMMARY:");
        sb.AppendLine();

        sb.AppendLine($"Total Changes: {result.Changes.Count}");
        sb.AppendLine($"Processing Time: {result.ProcessingTime.TotalSeconds:F2} seconds");
        
        if (result.OverallRelevanceScore > 0)
        {
            sb.AppendLine($"Overall Relevance Score: {result.OverallRelevanceScore:P0}");
        }
        sb.AppendLine();

        sb.AppendLine("Changes by Category:");
        var grouped = GroupChangesByCategory(result.Changes);
        foreach (var category in grouped.Keys.OrderBy(k => k))
        {
            sb.AppendLine($"  - {category}: {grouped[category].Count} changes");
        }
        sb.AppendLine();

        sb.AppendLine("Readability Impact:");
        var metricsChange = GenerateMetricsComparison(result.OriginalMetrics, result.AdaptedMetrics);
        sb.AppendLine($"  - Grade Level: {result.OriginalMetrics.FleschKincaidGradeLevel:F1} → {result.AdaptedMetrics.FleschKincaidGradeLevel:F1} ({metricsChange.FleschKincaidChange.Direction})");
        sb.AppendLine($"  - SMOG Score: {result.OriginalMetrics.SmogScore:F1} → {result.AdaptedMetrics.SmogScore:F1} ({metricsChange.SmogChange.Direction})");
        sb.AppendLine($"  - Complexity: {result.OriginalMetrics.OverallComplexity:F1} → {result.AdaptedMetrics.OverallComplexity:F1} ({metricsChange.ComplexityChange.Direction})");

        return sb.ToString();
    }

    /// <summary>
    /// Generate highlighted differences between texts
    /// </summary>
    private List<TextHighlight> GenerateHighlights(string original, string adapted)
    {
        var highlights = new List<TextHighlight>();

        var originalWords = original.Split(' ');
        var adaptedWords = adapted.Split(' ');

        for (int i = 0; i < Math.Min(originalWords.Length, adaptedWords.Length); i++)
        {
            if (!originalWords[i].Equals(adaptedWords[i], StringComparison.OrdinalIgnoreCase))
            {
                highlights.Add(new TextHighlight
                {
                    OriginalText = originalWords[i],
                    AdaptedText = adaptedWords[i],
                    Position = i,
                    Type = DetermineHighlightType(originalWords[i], adaptedWords[i])
                });
            }
        }

        return highlights;
    }

    /// <summary>
    /// Determine type of highlight based on change
    /// </summary>
    private string DetermineHighlightType(string original, string adapted)
    {
        if (adapted.Length < original.Length)
        {
            return "Simplified";
        }
        else if (adapted.Length > original.Length)
        {
            return "Expanded";
        }
        else
        {
            return "Replaced";
        }
    }

    /// <summary>
    /// Calculate percentage change
    /// </summary>
    private double CalculatePercentageChange(double original, double adapted)
    {
        if (original == 0)
        {
            return 0;
        }

        return ((adapted - original) / original) * 100;
    }

    /// <summary>
    /// Split text into paragraphs
    /// </summary>
    private List<string> SplitIntoParagraphs(string text)
    {
        return text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToList();
    }

    /// <summary>
    /// Allow manual override of specific adaptations
    /// </summary>
    public ContentAdaptationResult ApplyManualOverride(
        ContentAdaptationResult result,
        int changeIndex,
        bool acceptChange)
    {
        if (changeIndex < 0 || changeIndex >= result.Changes.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(changeIndex));
        }

        var change = result.Changes[changeIndex];
        
        if (!acceptChange)
        {
            _logger.LogInformation("Reverting change at index {Index}: {Description}", changeIndex, change.Description);
            
            result.AdaptedContent = result.AdaptedContent.Replace(change.AdaptedText, change.OriginalText);
            result.Changes.RemoveAt(changeIndex);
        }

        return result;
    }
}

/// <summary>
/// Detailed comparison report
/// </summary>
public class AdaptationComparisonReport
{
    public string OriginalContent { get; set; } = string.Empty;
    public string AdaptedContent { get; set; } = string.Empty;
    public TimeSpan ProcessingTime { get; set; }
    public double OverallRelevanceScore { get; set; }
    public List<ComparisonSection> Sections { get; set; } = new();
    public MetricsComparison MetricsComparison { get; set; } = new();
    public Dictionary<string, List<AdaptationChange>> ChangesByCategory { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// Side-by-side comparison section
/// </summary>
public class ComparisonSection
{
    public string OriginalText { get; set; } = string.Empty;
    public string AdaptedText { get; set; } = string.Empty;
    public List<AdaptationChange> Changes { get; set; } = new();
    public List<TextHighlight> HighlightedDifferences { get; set; } = new();
}

/// <summary>
/// Text highlight for differences
/// </summary>
public class TextHighlight
{
    public string OriginalText { get; set; } = string.Empty;
    public string AdaptedText { get; set; } = string.Empty;
    public int Position { get; set; }
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// Metrics comparison
/// </summary>
public class MetricsComparison
{
    public MetricChange FleschKincaidChange { get; set; } = new();
    public MetricChange SmogChange { get; set; } = new();
    public MetricChange ComplexityChange { get; set; } = new();
    public MetricChange WordsPerSentenceChange { get; set; } = new();
}

/// <summary>
/// Individual metric change
/// </summary>
public class MetricChange
{
    public string Name { get; set; } = string.Empty;
    public double OriginalValue { get; set; }
    public double AdaptedValue { get; set; }
    public string Direction { get; set; } = string.Empty;
    public double PercentageChange { get; set; }
}
