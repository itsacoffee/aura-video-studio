using Aura.Api.Models.QualityValidation;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services.QualityValidation;

/// <summary>
/// Service for validating video resolution
/// </summary>
public class ResolutionValidationService
{
    private readonly ILogger<ResolutionValidationService> _logger;

    public ResolutionValidationService(ILogger<ResolutionValidationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates video resolution against minimum requirements
    /// </summary>
    public Task<ResolutionValidationResult> ValidateResolutionAsync(
        int width,
        int height,
        int minWidth = 1280,
        int minHeight = 720,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating resolution: {Width}x{Height} (min: {MinWidth}x{MinHeight})",
            width, height, minWidth, minHeight);

        var totalPixels = width * height;
        var meetsMinimum = width >= minWidth && height >= minHeight;
        var aspectRatio = CalculateAspectRatio(width, height);
        var category = DetermineResolutionCategory(width, height);

        var issues = new List<string>();
        var warnings = new List<string>();

        if (!meetsMinimum)
        {
            issues.Add($"Resolution {width}x{height} is below minimum requirement of {minWidth}x{minHeight}");
        }

        if (width % 2 != 0 || height % 2 != 0)
        {
            warnings.Add("Resolution dimensions should be even numbers for better codec compatibility");
        }

        // Calculate score based on resolution quality
        var score = CalculateResolutionScore(width, height, minWidth, minHeight);

        return Task.FromResult(new ResolutionValidationResult
        {
            Width = width,
            Height = height,
            AspectRatio = aspectRatio,
            MeetsMinimumResolution = meetsMinimum,
            TotalPixels = totalPixels,
            ResolutionCategory = category,
            IsValid = meetsMinimum,
            Score = score,
            Issues = issues,
            Warnings = warnings
        });
    }

    private string CalculateAspectRatio(int width, int height)
    {
        var gcd = GCD(width, height);
        var ratioWidth = width / gcd;
        var ratioHeight = height / gcd;

        // Common aspect ratios
        if (ratioWidth == 16 && ratioHeight == 9) return "16:9";
        if (ratioWidth == 4 && ratioHeight == 3) return "4:3";
        if (ratioWidth == 21 && ratioHeight == 9) return "21:9";
        if (ratioWidth == 1 && ratioHeight == 1) return "1:1";
        if (ratioWidth == 9 && ratioHeight == 16) return "9:16";

        return $"{ratioWidth}:{ratioHeight}";
    }

    private int GCD(int a, int b)
    {
        while (b != 0)
        {
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    private string DetermineResolutionCategory(int width, int height)
    {
        var totalPixels = width * height;

        return totalPixels switch
        {
            >= 3840 * 2160 => "4K UHD",
            >= 2560 * 1440 => "2K QHD",
            >= 1920 * 1080 => "Full HD 1080p",
            >= 1280 * 720 => "HD 720p",
            >= 854 * 480 => "SD 480p",
            _ => "Sub-SD"
        };
    }

    private int CalculateResolutionScore(int width, int height, int minWidth, int minHeight)
    {
        var actualPixels = width * height;
        var minPixels = minWidth * minHeight;

        if (actualPixels < minPixels)
        {
            return (int)((double)actualPixels / minPixels * 50); // Max 50 if below minimum
        }

        // Above minimum, calculate score up to 100
        var referencePixels = 1920 * 1080; // Full HD as reference for score 100
        if (actualPixels >= referencePixels)
        {
            return 100;
        }

        return 50 + (int)((double)(actualPixels - minPixels) / (referencePixels - minPixels) * 50);
    }
}
