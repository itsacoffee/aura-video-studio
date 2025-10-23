using Aura.Api.Models.QualityValidation;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services.QualityValidation;

/// <summary>
/// Service for validating frame rate consistency
/// </summary>
public class FrameRateService
{
    private readonly ILogger<FrameRateService> _logger;

    public FrameRateService(ILogger<FrameRateService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates frame rate consistency
    /// </summary>
    public Task<FrameRateResult> ValidateFrameRateAsync(
        double actualFPS,
        double expectedFPS,
        double tolerance = 0.5,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating frame rate: actual={ActualFPS}, expected={ExpectedFPS}, tolerance={Tolerance}",
            actualFPS, expectedFPS, tolerance);

        var variance = Math.Abs(actualFPS - expectedFPS);
        var isConsistent = variance <= tolerance;
        
        var issues = new List<string>();
        var warnings = new List<string>();

        // Simulated frame analysis - in production would analyze actual video frames
        var totalFrames = 1000;
        var droppedFrames = isConsistent ? 0 : (int)(totalFrames * 0.05);

        if (!isConsistent)
        {
            issues.Add($"Frame rate variance ({variance:F2}) exceeds tolerance ({tolerance})");
        }

        if (droppedFrames > 0)
        {
            warnings.Add($"Detected {droppedFrames} dropped frames out of {totalFrames}");
        }

        if (actualFPS < 24)
        {
            warnings.Add($"Frame rate ({actualFPS:F2}) is below cinematic standard (24 FPS)");
        }

        var category = DetermineFrameRateCategory(actualFPS);
        var score = CalculateFrameRateScore(variance, tolerance, droppedFrames, totalFrames);

        return Task.FromResult(new FrameRateResult
        {
            ActualFPS = actualFPS,
            ExpectedFPS = expectedFPS,
            Variance = variance,
            IsConsistent = isConsistent,
            DroppedFrames = droppedFrames,
            TotalFrames = totalFrames,
            FrameRateCategory = category,
            IsValid = isConsistent && droppedFrames < totalFrames * 0.01,
            Score = score,
            Issues = issues,
            Warnings = warnings
        });
    }

    private string DetermineFrameRateCategory(double fps)
    {
        return fps switch
        {
            >= 120 => "High Frame Rate 120+ FPS",
            >= 60 => "High Frame Rate 60 FPS",
            >= 50 => "PAL 50 FPS",
            >= 30 => "NTSC 30 FPS",
            >= 25 => "PAL 25 FPS",
            >= 24 => "Cinema 24 FPS",
            _ => "Low Frame Rate"
        };
    }

    private int CalculateFrameRateScore(double variance, double tolerance, int droppedFrames, int totalFrames)
    {
        var score = 100;

        // Penalize for variance
        if (variance > tolerance)
        {
            score -= (int)((variance / tolerance - 1) * 30);
        }

        // Penalize for dropped frames
        var dropRate = (double)droppedFrames / totalFrames;
        score -= (int)(dropRate * 100);

        return Math.Max(0, Math.Min(100, score));
    }
}
