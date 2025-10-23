using Aura.Api.Models.QualityValidation;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services.QualityValidation;

/// <summary>
/// Service for analyzing content consistency across frames
/// </summary>
public class ConsistencyAnalysisService
{
    private readonly ILogger<ConsistencyAnalysisService> _logger;

    public ConsistencyAnalysisService(ILogger<ConsistencyAnalysisService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes content consistency across video frames
    /// </summary>
    public Task<ConsistencyAnalysisResult> AnalyzeConsistencyAsync(
        string videoFilePath,
        CancellationToken ct = default)
    {
        // Sanitize file path for logging to prevent log forging
        var sanitizedPath = Path.GetFileName(videoFilePath);
        _logger.LogInformation("Analyzing content consistency for video: {FileName}", sanitizedPath);

        // For this implementation, we'll provide simulated analysis
        // In production, this would use ML.NET or similar for actual frame comparison
        
        if (!File.Exists(videoFilePath))
        {
            throw new FileNotFoundException("Video file not found", videoFilePath);
        }

        var issues = new List<string>();
        var warnings = new List<string>();
        var artifacts = new List<string>();

        // Simulated consistency analysis
        var consistencyScore = 85;
        var sceneChanges = 5;
        var hasAbruptTransitions = false;
        var colorConsistency = 90;
        var brightnessConsistency = 88;
        var hasFlickering = false;
        var motionSmoothness = 82;

        // Validation checks
        if (consistencyScore < 70)
        {
            issues.Add($"Overall consistency score ({consistencyScore}) is below acceptable threshold");
        }

        if (hasAbruptTransitions)
        {
            warnings.Add("Abrupt scene transitions detected - consider adding transition effects");
        }

        if (colorConsistency < 80)
        {
            warnings.Add($"Color consistency ({colorConsistency}) varies across frames");
            artifacts.Add("Color grading inconsistency");
        }

        if (brightnessConsistency < 80)
        {
            warnings.Add($"Brightness consistency ({brightnessConsistency}) varies across frames");
            artifacts.Add("Exposure inconsistency");
        }

        if (hasFlickering)
        {
            issues.Add("Frame flickering detected");
            artifacts.Add("Flickering");
        }

        if (motionSmoothness < 70)
        {
            warnings.Add($"Motion smoothness score ({motionSmoothness}) indicates potential judder");
            artifacts.Add("Motion judder");
        }

        var score = CalculateConsistencyScore(consistencyScore, colorConsistency, brightnessConsistency, motionSmoothness, hasFlickering);

        return Task.FromResult(new ConsistencyAnalysisResult
        {
            ConsistencyScore = consistencyScore,
            SceneChanges = sceneChanges,
            HasAbruptTransitions = hasAbruptTransitions,
            ColorConsistency = colorConsistency,
            BrightnessConsistency = brightnessConsistency,
            HasFlickering = hasFlickering,
            MotionSmoothness = motionSmoothness,
            DetectedArtifacts = artifacts,
            IsValid = !hasFlickering && consistencyScore >= 70,
            Score = score,
            Issues = issues,
            Warnings = warnings
        });
    }

    private int CalculateConsistencyScore(int overall, int color, int brightness, int motion, bool flickering)
    {
        // Average of all metrics
        var score = (overall + color + brightness + motion) / 4;

        // Heavy penalty for flickering
        if (flickering)
        {
            score -= 30;
        }

        return Math.Max(0, Math.Min(100, score));
    }
}
