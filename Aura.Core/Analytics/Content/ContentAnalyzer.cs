using Microsoft.Extensions.Logging;

namespace Aura.Core.Analytics.Content;

/// <summary>
/// Analyzes content structure and patterns for optimization
/// </summary>
public class ContentAnalyzer
{
    private readonly ILogger<ContentAnalyzer> _logger;

    public ContentAnalyzer(ILogger<ContentAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes content structure for retention optimization
    /// </summary>
    public Task<ContentStructureAnalysis> AnalyzeContentStructureAsync(
        string content,
        string contentType,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing content structure for type: {ContentType}", contentType);

        var hooks = DetectHooks(content);
        var pacing = AnalyzePacing(content);
        var structure = AnalyzeStructuralElements(content);

        return Task.FromResult(new ContentStructureAnalysis(
            HookQuality: hooks.Quality,
            HookSuggestions: hooks.Suggestions,
            PacingScore: pacing.Score,
            PacingIssues: pacing.Issues,
            StructuralStrength: structure.Strength,
            ImprovementAreas: structure.ImprovementAreas,
            OverallScore: CalculateOverallScore(hooks.Quality, pacing.Score, structure.Strength)
        ));
    }

    /// <summary>
    /// Provides detailed content improvement recommendations
    /// </summary>
    public Task<ContentRecommendations> GetContentRecommendationsAsync(
        string content,
        string targetAudience,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating content recommendations for audience: {Audience}", targetAudience);

        var recommendations = new List<ContentImprovement>();

        // Analyze different aspects
        var intro = AnalyzeIntro(content);
        if (intro.NeedsImprovement)
        {
            recommendations.Add(new ContentImprovement(
                Area: "Introduction",
                Priority: "High",
                CurrentState: intro.CurrentState,
                Suggestion: intro.Suggestion,
                ExpectedImpact: "Improved viewer retention in first 30 seconds"
            ));
        }

        var transitions = AnalyzeTransitions(content);
        if (transitions.NeedsImprovement)
        {
            recommendations.Add(new ContentImprovement(
                Area: "Transitions",
                Priority: "Medium",
                CurrentState: transitions.CurrentState,
                Suggestion: transitions.Suggestion,
                ExpectedImpact: "Smoother flow between segments"
            ));
        }

        var callToActions = AnalyzeCallToActions(content);
        if (callToActions.NeedsImprovement)
        {
            recommendations.Add(new ContentImprovement(
                Area: "Call to Action",
                Priority: "Medium",
                CurrentState: callToActions.CurrentState,
                Suggestion: callToActions.Suggestion,
                ExpectedImpact: "Increased viewer engagement and interaction"
            ));
        }

        return Task.FromResult(new ContentRecommendations(
            TargetAudience: targetAudience,
            Recommendations: recommendations,
            PriorityOrder: recommendations.OrderByDescending(r => r.Priority).ToList(),
            EstimatedImprovementScore: CalculateEstimatedImprovement(recommendations)
        ));
    }

    /// <summary>
    /// Compares content with successful patterns
    /// </summary>
    public Task<ComparativeAnalysis> CompareWithSuccessfulPatternsAsync(
        string content,
        string contentCategory,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Comparing with successful patterns in category: {Category}", contentCategory);

        var successPatterns = GetSuccessPatterns(contentCategory);
        var matches = new List<PatternMatch>();
        var gaps = new List<PatternGap>();

        foreach (var pattern in successPatterns)
        {
            var hasPattern = ContentMatchesPattern(content, pattern);
            if (hasPattern)
            {
                matches.Add(new PatternMatch(
                    Pattern: pattern.Name,
                    Confidence: 0.8,
                    Examples: pattern.Examples
                ));
            }
            else
            {
                gaps.Add(new PatternGap(
                    Pattern: pattern.Name,
                    Importance: pattern.Importance,
                    Suggestion: pattern.Suggestion
                ));
            }
        }

        return Task.FromResult(new ComparativeAnalysis(
            Category: contentCategory,
            MatchedPatterns: matches,
            MissingPatterns: gaps,
            AlignmentScore: CalculateAlignmentScore(matches, successPatterns),
            TopSuggestions: gaps.OrderByDescending(g => g.Importance).Take(3).Select(g => g.Suggestion).ToList()
        ));
    }

    private (double Quality, List<string> Suggestions) DetectHooks(string content)
    {
        var firstLines = content.Split('\n').Take(3).ToList();
        var hookText = string.Join(" ", firstLines);
        
        double quality = 0.5;
        var suggestions = new List<string>();

        // Check for question
        if (hookText.Contains('?'))
        {
            quality += 0.2;
        }
        else
        {
            suggestions.Add("Consider starting with an intriguing question");
        }

        // Check for bold statement
        if (hookText.Contains('!') || hookText.Split(' ').Any(w => w.Length > 10))
        {
            quality += 0.1;
        }

        // Check for specificity
        if (hookText.Any(char.IsDigit))
        {
            quality += 0.2;
        }
        else
        {
            suggestions.Add("Add specific data or numbers to make opening more concrete");
        }

        if (quality < 0.7)
        {
            suggestions.Add("Strengthen hook with a compelling promise or unexpected statement");
        }

        return (Math.Min(1.0, quality), suggestions);
    }

    private (double Score, List<string> Issues) AnalyzePacing(string content)
    {
        var paragraphs = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        var issues = new List<string>();
        double score = 0.8;

        // Check paragraph length variation
        var lengths = paragraphs.Select(p => p.Length).ToList();
        if (lengths.Count > 0)
        {
            var avgLength = lengths.Average();
            var variance = lengths.Select(l => Math.Abs(l - avgLength)).Average();
            
            if (variance < avgLength * 0.2)
            {
                score -= 0.2;
                issues.Add("Paragraphs are too uniform in length - vary pacing");
            }
        }

        // Check for very long sections
        if (paragraphs.Any(p => p.Length > 500))
        {
            score -= 0.1;
            issues.Add("Some sections are too long - consider breaking them up");
        }

        return (Math.Max(0, score), issues);
    }

    private (double Strength, List<string> ImprovementAreas) AnalyzeStructuralElements(string content)
    {
        var improvements = new List<string>();
        double strength = 0.6;

        // Check for clear sections
        if (content.Contains("\n\n"))
        {
            strength += 0.1;
        }
        else
        {
            improvements.Add("Add clear section breaks");
        }

        // Check for variety in sentence structure
        var sentences = content.Split(new[] { ". ", "! ", "? " }, StringSplitOptions.RemoveEmptyEntries);
        if (sentences.Length > 5)
        {
            var lengths = sentences.Select(s => s.Split(' ').Length);
            var hasVariety = lengths.Max() - lengths.Min() > 5;
            if (hasVariety)
            {
                strength += 0.2;
            }
            else
            {
                improvements.Add("Vary sentence length for better rhythm");
            }
        }

        // Check for conclusion
        var lastParagraph = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? "";
        if (lastParagraph.Contains("conclusion") || lastParagraph.Contains("summary") || lastParagraph.Length > 100)
        {
            strength += 0.1;
        }
        else
        {
            improvements.Add("Add a clear conclusion or summary");
        }

        return (Math.Min(1.0, strength), improvements);
    }

    private double CalculateOverallScore(double hookQuality, double pacingScore, double structuralStrength)
    {
        return (hookQuality * 0.4) + (pacingScore * 0.3) + (structuralStrength * 0.3);
    }

    private (bool NeedsImprovement, string CurrentState, string Suggestion) AnalyzeIntro(string content)
    {
        var intro = content.Length > 200 ? content.Substring(0, 200) : content;
        var wordCount = intro.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (wordCount > 50)
        {
            return (true, "Introduction is too long", 
                "Reduce intro to 30-40 words. Get to the point quickly.");
        }

        if (!intro.Contains('?') && !intro.Contains('!'))
        {
            return (true, "Introduction lacks energy",
                "Add a question or exclamation to create immediate engagement");
        }

        return (false, "Introduction is effective", "");
    }

    private (bool NeedsImprovement, string CurrentState, string Suggestion) AnalyzeTransitions(string content)
    {
        var paragraphs = content.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
        
        if (paragraphs.Length < 2)
        {
            return (false, "Not applicable", "");
        }

        var transitionWords = new[] { "however", "moreover", "therefore", "additionally", "meanwhile", "next", "then" };
        var hasTransitions = paragraphs.Skip(1).Any(p => 
            transitionWords.Any(t => p.ToLowerInvariant().Contains(t)));

        if (!hasTransitions)
        {
            return (true, "Abrupt transitions between sections",
                "Add transition words or phrases to connect ideas smoothly");
        }

        return (false, "Transitions are present", "");
    }

    private (bool NeedsImprovement, string CurrentState, string Suggestion) AnalyzeCallToActions(string content)
    {
        var lastPart = content.Length > 200 ? content.Substring(content.Length - 200) : content;
        var hasCTA = lastPart.ToLowerInvariant().Contains("subscribe") ||
                     lastPart.ToLowerInvariant().Contains("like") ||
                     lastPart.ToLowerInvariant().Contains("comment") ||
                     lastPart.ToLowerInvariant().Contains("share");

        if (!hasCTA)
        {
            return (true, "Missing call-to-action",
                "End with a clear call-to-action (subscribe, like, comment, etc.)");
        }

        return (false, "Call-to-action present", "");
    }

    private double CalculateEstimatedImprovement(List<ContentImprovement> recommendations)
    {
        if (recommendations.Count == 0) return 0;
        
        var highPriority = recommendations.Count(r => r.Priority == "High");
        var mediumPriority = recommendations.Count(r => r.Priority == "Medium");
        
        return (highPriority * 0.15) + (mediumPriority * 0.10);
    }

    private List<SuccessPattern> GetSuccessPatterns(string category)
    {
        return new List<SuccessPattern>
        {
            new SuccessPattern(
                Name: "Strong Opening Hook",
                Importance: 0.9,
                Suggestion: "Start with a question or bold statement",
                Examples: new List<string> { "What if I told you...", "Here's why..." }
            ),
            new SuccessPattern(
                Name: "Clear Structure",
                Importance: 0.8,
                Suggestion: "Use clear sections with transitions",
                Examples: new List<string> { "First...", "Next...", "Finally..." }
            ),
            new SuccessPattern(
                Name: "Pattern Interrupts",
                Importance: 0.7,
                Suggestion: "Add surprising elements every 2-3 minutes",
                Examples: new List<string> { "But here's the thing...", "Plot twist..." }
            ),
            new SuccessPattern(
                Name: "Call to Action",
                Importance: 0.8,
                Suggestion: "End with clear next steps",
                Examples: new List<string> { "Try this...", "Let me know..." }
            )
        };
    }

    private bool ContentMatchesPattern(string content, SuccessPattern pattern)
    {
        // Simple pattern matching
        return pattern.Examples.Any(example => 
            content.ToLowerInvariant().Contains(example.ToLowerInvariant().TrimEnd('.', '?', '!')));
    }

    private double CalculateAlignmentScore(List<PatternMatch> matches, List<SuccessPattern> allPatterns)
    {
        if (allPatterns.Count == 0) return 0;
        return (double)matches.Count / allPatterns.Count;
    }
}

// Models
public record ContentStructureAnalysis(
    double HookQuality,
    List<string> HookSuggestions,
    double PacingScore,
    List<string> PacingIssues,
    double StructuralStrength,
    List<string> ImprovementAreas,
    double OverallScore
);

public record ContentRecommendations(
    string TargetAudience,
    List<ContentImprovement> Recommendations,
    List<ContentImprovement> PriorityOrder,
    double EstimatedImprovementScore
);

public record ContentImprovement(
    string Area,
    string Priority,
    string CurrentState,
    string Suggestion,
    string ExpectedImpact
);

public record ComparativeAnalysis(
    string Category,
    List<PatternMatch> MatchedPatterns,
    List<PatternGap> MissingPatterns,
    double AlignmentScore,
    List<string> TopSuggestions
);

public record PatternMatch(
    string Pattern,
    double Confidence,
    List<string> Examples
);

public record PatternGap(
    string Pattern,
    double Importance,
    string Suggestion
);

public record SuccessPattern(
    string Name,
    double Importance,
    string Suggestion,
    List<string> Examples
);
