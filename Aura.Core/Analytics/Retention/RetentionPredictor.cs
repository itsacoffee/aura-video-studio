using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Analytics.Retention;

/// <summary>
/// Predicts audience retention patterns for video content
/// </summary>
public class RetentionPredictor
{
    private readonly ILogger<RetentionPredictor> _logger;

    public RetentionPredictor(ILogger<RetentionPredictor> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Predicts retention curve for given content
    /// </summary>
    public Task<RetentionPrediction> PredictRetentionAsync(
        string content,
        string contentType,
        TimeSpan videoDuration,
        string? targetDemographic = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Predicting retention for {ContentType} content, duration: {Duration}", 
            contentType, videoDuration);

        // Basic retention prediction based on content analysis
        var retentionCurve = GenerateRetentionCurve(content, videoDuration);
        var predictions = AnalyzeEngagementPoints(content, retentionCurve);
        
        return Task.FromResult(new RetentionPrediction(
            RetentionCurve: retentionCurve,
            PredictedAverageRetention: CalculateAverageRetention(retentionCurve),
            EngagementDips: predictions.Dips,
            OptimalLength: PredictOptimalLength(contentType, videoDuration),
            Recommendations: GenerateRecommendations(predictions.Dips, content)
        ));
    }

    /// <summary>
    /// Analyzes attention span patterns
    /// </summary>
    public Task<AttentionAnalysis> AnalyzeAttentionSpanAsync(
        string content,
        TimeSpan videoDuration,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing attention span for {Duration} content", videoDuration);

        var segments = SplitIntoSegments(content);
        var segmentScores = segments.Select((s, i) => new SegmentScore(
            SegmentIndex: i,
            StartTime: TimeSpan.FromSeconds(i * 30),
            Duration: TimeSpan.FromSeconds(30),
            EngagementScore: CalculateSegmentEngagement(s),
            Reasoning: GetEngagementReasoning(s)
        )).ToList();

        return Task.FromResult(new AttentionAnalysis(
            SegmentScores: segmentScores,
            CriticalDropPoints: segmentScores.Where(s => s.EngagementScore < 0.6).ToList(),
            AverageEngagement: segmentScores.Average(s => s.EngagementScore),
            Suggestions: GenerateAttentionSuggestions(segmentScores)
        ));
    }

    private List<RetentionPoint> GenerateRetentionCurve(string content, TimeSpan duration)
    {
        var points = new List<RetentionPoint>();
        var totalSeconds = (int)duration.TotalSeconds;
        var interval = Math.Max(1, totalSeconds / 20); // 20 data points

        for (int i = 0; i <= totalSeconds; i += interval)
        {
            var timePoint = TimeSpan.FromSeconds(i);
            var retention = CalculateRetentionAtTime(content, timePoint, duration);
            points.Add(new RetentionPoint(timePoint, retention));
        }

        return points;
    }

    private double CalculateRetentionAtTime(string content, TimeSpan time, TimeSpan totalDuration)
    {
        // Simple model: retention typically drops over time
        var progress = time.TotalSeconds / totalDuration.TotalSeconds;
        
        // Start high, decay based on content quality
        var baseRetention = 0.95 - (progress * 0.3); // Drops from 95% to 65%
        
        // Add variance based on content patterns
        var contentQuality = AnalyzeContentQuality(content);
        var adjustment = (contentQuality - 0.5) * 0.2; // ±10% based on quality
        
        return Math.Max(0.2, Math.Min(1.0, baseRetention + adjustment));
    }

    private double CalculateAverageRetention(List<RetentionPoint> curve)
    {
        return curve.Average(p => p.Retention);
    }

    private (List<EngagementDip> Dips, double Score) AnalyzeEngagementPoints(
        string content, 
        List<RetentionPoint> curve)
    {
        var dips = new List<EngagementDip>();
        
        for (int i = 1; i < curve.Count; i++)
        {
            var drop = curve[i - 1].Retention - curve[i].Retention;
            if (drop > 0.1) // Significant drop
            {
                dips.Add(new EngagementDip(
                    TimePoint: curve[i].TimePoint,
                    RetentionDrop: drop,
                    Severity: drop > 0.2 ? "High" : "Medium",
                    Reason: "Potential pacing or content issue"
                ));
            }
        }

        return (dips, curve.Average(p => p.Retention));
    }

    private TimeSpan PredictOptimalLength(string contentType, TimeSpan currentDuration)
    {
        // Content-type based recommendations
        var optimalSeconds = contentType.ToLowerInvariant() switch
        {
            "tutorial" => 480,      // 8 minutes
            "entertainment" => 600, // 10 minutes
            "educational" => 720,   // 12 minutes
            "short" => 60,          // 1 minute
            _ => 600                // 10 minutes default
        };

        return TimeSpan.FromSeconds(optimalSeconds);
    }

    private List<string> GenerateRecommendations(List<EngagementDip> dips, string content)
    {
        var recommendations = new List<string>();

        if (dips.Any(d => d.Severity == "High"))
        {
            recommendations.Add("Add engaging visual transitions at identified drop points");
            recommendations.Add("Consider breaking long sections into shorter segments");
        }

        if (dips.Count > 5)
        {
            recommendations.Add("Content pacing may be inconsistent - review script flow");
        }

        recommendations.Add("Add hook elements in the first 30 seconds");
        recommendations.Add("Include pattern interrupts every 2-3 minutes");

        return recommendations;
    }

    private List<string> SplitIntoSegments(string content)
    {
        // Simple segmentation by paragraphs or sentences
        return content.Split(new[] { "\n\n", ". " }, StringSplitOptions.RemoveEmptyEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private double CalculateSegmentEngagement(string segment)
    {
        // Simple heuristics for engagement
        var score = 0.5; // Base score

        // Length factor
        var wordCount = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        if (wordCount > 20 && wordCount < 100) score += 0.2;
        else if (wordCount > 150) score -= 0.2;

        // Question or call-to-action
        if (segment.Contains('?')) score += 0.1;
        if (segment.Contains('!')) score += 0.05;

        // Numeric or specific data
        if (segment.Any(char.IsDigit)) score += 0.1;

        return Math.Max(0, Math.Min(1.0, score));
    }

    private string GetEngagementReasoning(string segment)
    {
        var wordCount = segment.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        
        if (wordCount < 20) return "Segment may be too short for impact";
        if (wordCount > 150) return "Segment may be too long, consider breaking up";
        if (segment.Contains('?')) return "Interactive element detected (good)";
        
        return "Standard engagement expected";
    }

    private List<string> GenerateAttentionSuggestions(List<SegmentScore> scores)
    {
        var suggestions = new List<string>();
        
        var lowScores = scores.Where(s => s.EngagementScore < 0.5).ToList();
        if (lowScores.Any())
        {
            suggestions.Add($"Improve {lowScores.Count} low-engagement segments with more dynamic content");
        }

        suggestions.Add("Vary pacing between segments to maintain interest");
        suggestions.Add("Add visual cues at segment transitions");

        return suggestions;
    }

    private double AnalyzeContentQuality(string content)
    {
        // Simple content quality heuristic
        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var uniqueWords = words.Distinct().Count();
        var variety = (double)uniqueWords / Math.Max(1, words.Length);
        
        return Math.Max(0.3, Math.Min(0.8, variety * 2));
    }
}

// Models
public record RetentionPrediction(
    List<RetentionPoint> RetentionCurve,
    double PredictedAverageRetention,
    List<EngagementDip> EngagementDips,
    TimeSpan OptimalLength,
    List<string> Recommendations
);

public record RetentionPoint(
    TimeSpan TimePoint,
    double Retention
);

public record EngagementDip(
    TimeSpan TimePoint,
    double RetentionDrop,
    string Severity,
    string Reason
);

public record AttentionAnalysis(
    List<SegmentScore> SegmentScores,
    List<SegmentScore> CriticalDropPoints,
    double AverageEngagement,
    List<string> Suggestions
);

public record SegmentScore(
    int SegmentIndex,
    TimeSpan StartTime,
    TimeSpan Duration,
    double EngagementScore,
    string Reasoning
);
