using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.AI.Aesthetics.Composition;

/// <summary>
/// Analyzes and enhances visual composition using golden ratio and rule of thirds
/// </summary>
public class CompositionAnalyzer
{
    private const float GoldenRatio = 1.618f;
    private const float RuleOfThirdsMargin = 0.05f; // 5% tolerance

    /// <summary>
    /// Analyzes image composition and suggests improvements
    /// </summary>
    public Task<CompositionAnalysisResult> AnalyzeCompositionAsync(
        int imageWidth,
        int imageHeight,
        Point? subjectPosition = null,
        CancellationToken cancellationToken = default)
    {
        var result = new CompositionAnalysisResult();
        
        // Detect focal point (if not provided)
        var focalPoint = subjectPosition ?? DetectFocalPoint(imageWidth, imageHeight);
        result.FocalPoint = focalPoint;

        // Analyze against different composition rules
        var ruleOfThirdsScore = AnalyzeRuleOfThirds(focalPoint, imageWidth, imageHeight);
        var goldenRatioScore = AnalyzeGoldenRatio(focalPoint, imageWidth, imageHeight);
        var balanceScore = AnalyzeBalance(focalPoint, imageWidth, imageHeight);

        // Select best composition rule
        if (goldenRatioScore > ruleOfThirdsScore)
        {
            result.SuggestedRule = CompositionRule.GoldenRatio;
            result.CompositionScore = goldenRatioScore;
        }
        else
        {
            result.SuggestedRule = CompositionRule.RuleOfThirds;
            result.CompositionScore = ruleOfThirdsScore;
        }

        result.BalanceScore = balanceScore;

        // Generate recommendations
        result.Recommendations = GenerateRecommendations(result, imageWidth, imageHeight);

        // Suggest crop if composition is poor
        if (result.CompositionScore < 0.6f)
        {
            result.SuggestedCrop = SuggestOptimalCrop(focalPoint, imageWidth, imageHeight, result.SuggestedRule);
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Detects focal point in image (simplified heuristic)
    /// </summary>
    public Task<Point> DetectFocalPointAsync(
        int imageWidth,
        int imageHeight,
        CancellationToken cancellationToken = default)
    {
        var focalPoint = DetectFocalPoint(imageWidth, imageHeight);
        return Task.FromResult(focalPoint);
    }

    /// <summary>
    /// Generates automatic reframing suggestion
    /// </summary>
    public Task<Rectangle> SuggestReframingAsync(
        Point focalPoint,
        int imageWidth,
        int imageHeight,
        CompositionRule rule = CompositionRule.RuleOfThirds,
        CancellationToken cancellationToken = default)
    {
        var crop = SuggestOptimalCrop(focalPoint, imageWidth, imageHeight, rule);
        return Task.FromResult(crop);
    }

    private Point DetectFocalPoint(int imageWidth, int imageHeight)
    {
        // In a real implementation, this would use computer vision
        // For now, return a point slightly off-center
        return new Point
        {
            X = imageWidth * 0.4f,
            Y = imageHeight * 0.4f
        };
    }

    private float AnalyzeRuleOfThirds(Point focalPoint, int width, int height)
    {
        // Calculate rule of thirds intersection points
        var thirdWidth = width / 3.0f;
        var thirdHeight = height / 3.0f;

        var intersectionPoints = new List<Point>
        {
            new() { X = thirdWidth, Y = thirdHeight },
            new() { X = thirdWidth * 2, Y = thirdHeight },
            new() { X = thirdWidth, Y = thirdHeight * 2 },
            new() { X = thirdWidth * 2, Y = thirdHeight * 2 }
        };

        // Find closest intersection point
        var minDistance = float.MaxValue;
        foreach (var point in intersectionPoints)
        {
            var distance = Distance(focalPoint, point);
            if (distance < minDistance)
                minDistance = distance;
        }

        // Normalize score (closer to intersection = higher score)
        var maxPossibleDistance = (float)Math.Sqrt(width * width + height * height);
        var score = 1.0f - (minDistance / (maxPossibleDistance * 0.3f));
        return Math.Max(0, Math.Min(1, score));
    }

    private float AnalyzeGoldenRatio(Point focalPoint, int width, int height)
    {
        // Calculate golden ratio points
        var goldenX = width / GoldenRatio;
        var goldenY = height / GoldenRatio;

        var goldenPoints = new List<Point>
        {
            new() { X = goldenX, Y = goldenY },
            new() { X = width - goldenX, Y = goldenY },
            new() { X = goldenX, Y = height - goldenY },
            new() { X = width - goldenX, Y = height - goldenY }
        };

        // Find closest golden ratio point
        var minDistance = float.MaxValue;
        foreach (var point in goldenPoints)
        {
            var distance = Distance(focalPoint, point);
            if (distance < minDistance)
                minDistance = distance;
        }

        // Normalize score
        var maxPossibleDistance = (float)Math.Sqrt(width * width + height * height);
        var score = 1.0f - (minDistance / (maxPossibleDistance * 0.3f));
        return Math.Max(0, Math.Min(1, score));
    }

    private float AnalyzeBalance(Point focalPoint, int width, int height)
    {
        // Check how well the focal point is balanced in the frame
        var centerX = width / 2.0f;
        var centerY = height / 2.0f;

        var distanceFromCenter = Distance(focalPoint, new Point { X = centerX, Y = centerY });
        var maxDistance = (float)Math.Sqrt(centerX * centerX + centerY * centerY);

        // Slight off-center is better than perfect center
        var optimalDistance = maxDistance * 0.3f;
        var deviation = Math.Abs(distanceFromCenter - optimalDistance);
        
        var score = 1.0f - (deviation / maxDistance);
        return Math.Max(0, Math.Min(1, score));
    }

    private List<string> GenerateRecommendations(CompositionAnalysisResult result, int width, int height)
    {
        var recommendations = new List<string>();

        if (result.CompositionScore < 0.7f)
        {
            recommendations.Add($"Consider repositioning subject to align with {result.SuggestedRule}");
        }

        if (result.BalanceScore < 0.6f)
        {
            recommendations.Add("Improve visual balance by adjusting framing");
        }

        if (result.FocalPoint != null)
        {
            var centerX = width / 2.0f;
            if (Math.Abs(result.FocalPoint.X - centerX) < width * 0.1f)
            {
                recommendations.Add("Subject is too centered - apply rule of thirds for more dynamic composition");
            }
        }

        if (recommendations.Count == 0)
        {
            recommendations.Add("Composition follows best practices");
        }

        return recommendations;
    }

    private Rectangle SuggestOptimalCrop(Point focalPoint, int width, int height, CompositionRule rule)
    {
        // Calculate optimal crop based on composition rule
        float targetX, targetY;

        if (rule == CompositionRule.GoldenRatio)
        {
            targetX = width / GoldenRatio;
            targetY = height / GoldenRatio;
        }
        else // Rule of Thirds
        {
            targetX = width / 3.0f;
            targetY = height / 3.0f;
        }

        // Calculate crop rectangle that places focal point at target position
        var cropWidth = width * 0.8f; // Slight zoom
        var cropHeight = height * 0.8f;
        
        var offsetX = focalPoint.X - targetX;
        var offsetY = focalPoint.Y - targetY;

        return new Rectangle
        {
            X = Math.Max(0, Math.Min(offsetX, width - cropWidth)),
            Y = Math.Max(0, Math.Min(offsetY, height - cropHeight)),
            Width = cropWidth,
            Height = cropHeight
        };
    }

    private float Distance(Point p1, Point p2)
    {
        var dx = p1.X - p2.X;
        var dy = p1.Y - p2.Y;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }
}
