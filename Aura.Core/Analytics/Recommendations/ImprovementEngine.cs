using Microsoft.Extensions.Logging;
using Aura.Core.Analytics.Retention;
using Aura.Core.Analytics.Platforms;
using Aura.Core.Analytics.Content;

namespace Aura.Core.Analytics.Recommendations;

/// <summary>
/// Generates actionable improvement recommendations for content
/// </summary>
public class ImprovementEngine
{
    private readonly ILogger<ImprovementEngine> _logger;
    private readonly RetentionPredictor _retentionPredictor;
    private readonly PlatformOptimizer _platformOptimizer;
    private readonly ContentAnalyzer _contentAnalyzer;

    public ImprovementEngine(
        ILogger<ImprovementEngine> logger,
        RetentionPredictor retentionPredictor,
        PlatformOptimizer platformOptimizer,
        ContentAnalyzer contentAnalyzer)
    {
        _logger = logger;
        _retentionPredictor = retentionPredictor;
        _platformOptimizer = platformOptimizer;
        _contentAnalyzer = contentAnalyzer;
    }

    /// <summary>
    /// Generates comprehensive improvement roadmap
    /// </summary>
    public async Task<ImprovementRoadmap> GenerateImprovementRoadmapAsync(
        string content,
        string contentType,
        TimeSpan videoDuration,
        List<string> targetPlatforms,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating improvement roadmap for {ContentType}", contentType);

        // Gather analyses from all sources
        var retention = await _retentionPredictor.PredictRetentionAsync(content, contentType, videoDuration, null, ct);
        var structure = await _contentAnalyzer.AnalyzeContentStructureAsync(content, contentType, ct);
        var comparative = await _contentAnalyzer.CompareWithSuccessfulPatternsAsync(content, contentType, ct);

        // Platform-specific optimizations
        var platformOptimizations = new List<PlatformOptimization>();
        foreach (var platform in targetPlatforms)
        {
            var optimization = await _platformOptimizer.GetPlatformOptimizationAsync(platform, content, videoDuration, ct);
            platformOptimizations.Add(optimization);
        }

        // Generate prioritized action items
        var actions = GenerateActionItems(retention, structure, comparative, platformOptimizations);

        return new ImprovementRoadmap(
            CurrentScore: CalculateCurrentScore(retention, structure),
            PotentialScore: CalculatePotentialScore(actions),
            PrioritizedActions: actions,
            QuickWins: actions.Where(a => a.Difficulty == "Easy" && a.Impact == "High").ToList(),
            LongTermGoals: actions.Where(a => a.Difficulty == "Hard").ToList(),
            EstimatedTimeToImprove: EstimateTimeToImprove(actions)
        );
    }

    /// <summary>
    /// Provides real-time feedback for content being created
    /// </summary>
    public async Task<RealTimeFeedback> GetRealTimeFeedbackAsync(
        string currentContent,
        int currentWordCount,
        TimeSpan currentDuration,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Providing real-time feedback for content");

        var issues = new List<FeedbackIssue>();
        var strengths = new List<string>();

        // Quick checks
        if (currentWordCount < 50)
        {
            issues.Add(new FeedbackIssue(
                Type: "Length",
                Severity: "Info",
                Message: "Content seems short - ensure you've covered your main points",
                Suggestion: "Aim for at least 100 words for meaningful content"
            ));
        }

        if (!currentContent.Contains('?') && currentWordCount > 50)
        {
            issues.Add(new FeedbackIssue(
                Type: "Engagement",
                Severity: "Warning",
                Message: "No questions detected - content may lack engagement",
                Suggestion: "Add questions to create dialogue with viewers"
            ));
        }
        else if (currentContent.Contains('?'))
        {
            strengths.Add("Good use of questions for engagement");
        }

        // Check pacing
        var wordsPerMinute = currentDuration.TotalMinutes > 0 
            ? currentWordCount / currentDuration.TotalMinutes 
            : 0;

        if (wordsPerMinute > 180)
        {
            issues.Add(new FeedbackIssue(
                Type: "Pacing",
                Severity: "Warning",
                Message: "Content may be too fast-paced",
                Suggestion: "Slow down narration or reduce word density"
            ));
        }
        else if (wordsPerMinute > 0 && wordsPerMinute < 100)
        {
            issues.Add(new FeedbackIssue(
                Type: "Pacing",
                Severity: "Info",
                Message: "Content may be slow-paced",
                Suggestion: "Consider adding more content or increasing narration speed"
            ));
        }

        return new RealTimeFeedback(
            Issues: issues,
            Strengths: strengths,
            CurrentQualityScore: CalculateQuickQualityScore(currentContent, issues),
            Suggestions: GenerateQuickSuggestions(issues, currentWordCount)
        );
    }

    /// <summary>
    /// Analyzes weak sections and provides specific improvements
    /// </summary>
    public async Task<SectionAnalysis> AnalyzeWeakSectionsAsync(
        string content,
        List<TimeSpan> weakPoints,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing {Count} weak sections", weakPoints.Count);

        var sections = SplitContentIntoSections(content, weakPoints);
        var analyses = new List<WeakSectionAnalysis>();

        foreach (var (section, timePoint) in sections)
        {
            var analysis = AnalyzeSection(section, timePoint);
            analyses.Add(analysis);
        }

        return new SectionAnalysis(
            WeakSections: analyses,
            OverallRecommendation: GenerateOverallRecommendation(analyses),
            PriorityFixes: analyses.OrderByDescending(a => a.Severity).Take(3).ToList()
        );
    }

    private List<ActionItem> GenerateActionItems(
        RetentionPrediction retention,
        ContentStructureAnalysis structure,
        ComparativeAnalysis comparative,
        List<PlatformOptimization> platforms)
    {
        var actions = new List<ActionItem>();

        // From retention analysis
        if (retention.PredictedAverageRetention < 0.7)
        {
            actions.Add(new ActionItem(
                Title: "Improve Opening Hook",
                Description: "Current retention prediction is low. Strengthen the first 30 seconds.",
                Impact: "High",
                Difficulty: "Medium",
                Category: "Retention",
                EstimatedTime: TimeSpan.FromHours(2)
            ));
        }

        foreach (var dip in retention.EngagementDips.Where(d => d.Severity == "High"))
        {
            actions.Add(new ActionItem(
                Title: $"Address engagement dip at {dip.TimePoint}",
                Description: dip.Reason,
                Impact: "High",
                Difficulty: "Medium",
                Category: "Engagement",
                EstimatedTime: TimeSpan.FromHours(1)
            ));
        }

        // From structure analysis
        if (structure.HookQuality < 0.7)
        {
            actions.Add(new ActionItem(
                Title: "Strengthen Opening Hook",
                Description: string.Join("; ", structure.HookSuggestions),
                Impact: "High",
                Difficulty: "Easy",
                Category: "Structure",
                EstimatedTime: TimeSpan.FromMinutes(30)
            ));
        }

        if (structure.PacingScore < 0.7)
        {
            actions.Add(new ActionItem(
                Title: "Improve Content Pacing",
                Description: string.Join("; ", structure.PacingIssues),
                Impact: "Medium",
                Difficulty: "Medium",
                Category: "Pacing",
                EstimatedTime: TimeSpan.FromHours(1.5)
            ));
        }

        // From comparative analysis
        foreach (var gap in comparative.MissingPatterns.Take(3))
        {
            actions.Add(new ActionItem(
                Title: $"Add {gap.Pattern}",
                Description: gap.Suggestion,
                Impact: gap.Importance > 0.8 ? "High" : "Medium",
                Difficulty: "Easy",
                Category: "Best Practices",
                EstimatedTime: TimeSpan.FromMinutes(45)
            ));
        }

        // Platform-specific
        foreach (var platform in platforms)
        {
            if (platform.Recommendations.Count > 0)
            {
                actions.Add(new ActionItem(
                    Title: $"Optimize for {platform.Platform}",
                    Description: string.Join("; ", platform.Recommendations.Take(2)),
                    Impact: "Medium",
                    Difficulty: "Easy",
                    Category: "Platform",
                    EstimatedTime: TimeSpan.FromMinutes(30)
                ));
            }
        }

        return actions.OrderByDescending(a => GetImpactScore(a.Impact))
                     .ThenBy(a => GetDifficultyScore(a.Difficulty))
                     .ToList();
    }

    private double CalculateCurrentScore(RetentionPrediction retention, ContentStructureAnalysis structure)
    {
        return (retention.PredictedAverageRetention * 0.6) + (structure.OverallScore * 0.4);
    }

    private double CalculatePotentialScore(List<ActionItem> actions)
    {
        var highImpactActions = actions.Count(a => a.Impact == "High");
        var mediumImpactActions = actions.Count(a => a.Impact == "Medium");
        
        var potentialGain = (highImpactActions * 0.15) + (mediumImpactActions * 0.08);
        return Math.Min(1.0, 0.6 + potentialGain); // Assuming current baseline of 0.6
    }

    private TimeSpan EstimateTimeToImprove(List<ActionItem> actions)
    {
        return TimeSpan.FromMinutes(actions.Sum(a => a.EstimatedTime.TotalMinutes));
    }

    private double CalculateQuickQualityScore(string content, List<FeedbackIssue> issues)
    {
        var baseScore = 0.7;
        var warningPenalty = issues.Count(i => i.Severity == "Warning") * 0.1;
        
        return Math.Max(0.3, baseScore - warningPenalty);
    }

    private List<string> GenerateQuickSuggestions(List<FeedbackIssue> issues, int wordCount)
    {
        var suggestions = new List<string>();
        
        if (wordCount < 100)
        {
            suggestions.Add("Continue developing your main ideas");
        }

        if (issues.Any(i => i.Type == "Engagement"))
        {
            suggestions.Add("Add questions or interactive elements");
        }

        suggestions.Add("Review for clarity and conciseness");
        
        return suggestions;
    }

    private List<(string Section, TimeSpan TimePoint)> SplitContentIntoSections(
        string content, 
        List<TimeSpan> weakPoints)
    {
        // Simple split by paragraphs, matched to time points
        var paragraphs = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var result = new List<(string, TimeSpan)>();

        for (int i = 0; i < Math.Min(paragraphs.Length, weakPoints.Count); i++)
        {
            result.Add((paragraphs[i], weakPoints[i]));
        }

        return result;
    }

    private WeakSectionAnalysis AnalyzeSection(string section, TimeSpan timePoint)
    {
        var issues = new List<string>();
        var severity = "Low";

        // Check section length
        if (section.Length > 500)
        {
            issues.Add("Section is too long - consider breaking up");
            severity = "Medium";
        }

        // Check engagement elements
        if (!section.Contains('?') && !section.Contains('!'))
        {
            issues.Add("Lacks engagement elements - add questions or emphasis");
            severity = "Medium";
        }

        var improvement = issues.Count > 0 
            ? "Add visual interest or break into smaller segments"
            : "Consider adding a transition element";

        return new WeakSectionAnalysis(
            TimePoint: timePoint,
            Content: section.Length > 100 ? section.Substring(0, 100) + "..." : section,
            Issues: issues,
            Severity: severity,
            SuggestedImprovement: improvement
        );
    }

    private string GenerateOverallRecommendation(List<WeakSectionAnalysis> analyses)
    {
        var highSeverity = analyses.Count(a => a.Severity == "High");
        var mediumSeverity = analyses.Count(a => a.Severity == "Medium");

        if (highSeverity > 0)
        {
            return $"Focus on {highSeverity} critical sections first for maximum impact";
        }
        else if (mediumSeverity > 2)
        {
            return "Multiple sections need attention - prioritize based on timing";
        }
        else
        {
            return "Minor improvements needed - content is generally strong";
        }
    }

    private int GetImpactScore(string impact)
    {
        return impact switch
        {
            "High" => 3,
            "Medium" => 2,
            "Low" => 1,
            _ => 0
        };
    }

    private int GetDifficultyScore(string difficulty)
    {
        return difficulty switch
        {
            "Easy" => 1,
            "Medium" => 2,
            "Hard" => 3,
            _ => 2
        };
    }
}

// Models
public record ImprovementRoadmap(
    double CurrentScore,
    double PotentialScore,
    List<ActionItem> PrioritizedActions,
    List<ActionItem> QuickWins,
    List<ActionItem> LongTermGoals,
    TimeSpan EstimatedTimeToImprove
);

public record ActionItem(
    string Title,
    string Description,
    string Impact,
    string Difficulty,
    string Category,
    TimeSpan EstimatedTime
);

public record RealTimeFeedback(
    List<FeedbackIssue> Issues,
    List<string> Strengths,
    double CurrentQualityScore,
    List<string> Suggestions
);

public record FeedbackIssue(
    string Type,
    string Severity,
    string Message,
    string Suggestion
);

public record SectionAnalysis(
    List<WeakSectionAnalysis> WeakSections,
    string OverallRecommendation,
    List<WeakSectionAnalysis> PriorityFixes
);

public record WeakSectionAnalysis(
    TimeSpan TimePoint,
    string Content,
    List<string> Issues,
    string Severity,
    string SuggestedImprovement
);
