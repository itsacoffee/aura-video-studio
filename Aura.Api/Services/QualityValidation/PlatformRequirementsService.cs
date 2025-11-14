using Aura.Api.Models.QualityValidation;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services.QualityValidation;

/// <summary>
/// Service for validating against platform-specific requirements
/// </summary>
public class PlatformRequirementsService
{
    private readonly ILogger<PlatformRequirementsService> _logger;

    // Platform specifications (simplified - in production would be more comprehensive)
    private static readonly Dictionary<string, PlatformSpec> PlatformSpecs = new()
    {
        ["youtube"] = new PlatformSpec
        {
            Name = "YouTube",
            MinWidth = 426,
            MinHeight = 240,
            MaxWidth = 7680,
            MaxHeight = 4320,
            RecommendedAspectRatios = new[] { "16:9", "9:16", "4:3", "1:1" },
            MaxFileSizeMB = 256 * 1024, // 256 GB
            MaxDurationSeconds = 12 * 60 * 60, // 12 hours
            SupportedCodecs = new[] { "H.264", "H.265", "VP9", "AV1" }
        },
        ["tiktok"] = new PlatformSpec
        {
            Name = "TikTok",
            MinWidth = 540,
            MinHeight = 960,
            MaxWidth = 1080,
            MaxHeight = 1920,
            RecommendedAspectRatios = new[] { "9:16" },
            MaxFileSizeMB = 287,
            MaxDurationSeconds = 10 * 60, // 10 minutes
            SupportedCodecs = new[] { "H.264", "H.265" }
        },
        ["instagram"] = new PlatformSpec
        {
            Name = "Instagram",
            MinWidth = 600,
            MinHeight = 315,
            MaxWidth = 1080,
            MaxHeight = 1920,
            RecommendedAspectRatios = new[] { "1:1", "4:5", "9:16" },
            MaxFileSizeMB = 650,
            MaxDurationSeconds = 60,
            SupportedCodecs = new[] { "H.264" }
        },
        ["twitter"] = new PlatformSpec
        {
            Name = "Twitter/X",
            MinWidth = 32,
            MinHeight = 32,
            MaxWidth = 1920,
            MaxHeight = 1200,
            RecommendedAspectRatios = new[] { "16:9", "1:1" },
            MaxFileSizeMB = 512,
            MaxDurationSeconds = 140,
            SupportedCodecs = new[] { "H.264" }
        }
    };

    public PlatformRequirementsService(ILogger<PlatformRequirementsService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates video properties against platform specifications
    /// </summary>
    public Task<PlatformRequirementsResult> ValidateAsync(
        string platform,
        int width,
        int height,
        long fileSizeBytes,
        double durationSeconds,
        string codec,
        CancellationToken ct = default)
    {
        // Sanitize platform name for logging to prevent log forging
        var sanitizedPlatform = platform.Replace("\n", "").Replace("\r", "");
        var sanitizedCodec = codec.Replace("\n", "").Replace("\r", "");
        
        _logger.LogInformation("Validating against {Platform} requirements: {Width}x{Height}, {FileSize}MB, {Duration}s, {Codec}",
            sanitizedPlatform, width, height, fileSizeBytes / 1024.0 / 1024.0, durationSeconds, sanitizedCodec);

        var platformKey = platform.ToLowerInvariant();
        
        if (!PlatformSpecs.TryGetValue(platformKey, out var spec))
        {
            throw new ArgumentException($"Unknown platform: {platform}. Supported platforms: {string.Join(", ", PlatformSpecs.Keys)}");
        }

        var aspectRatio = CalculateAspectRatio(width, height);
        var fileSizeMB = fileSizeBytes / 1024.0 / 1024.0;

        var issues = new List<string>();
        var warnings = new List<string>();
        var recommendations = new List<string>();

        // Resolution validation
        var meetsResolution = width >= spec.MinWidth && height >= spec.MinHeight &&
                             width <= spec.MaxWidth && height <= spec.MaxHeight;
        
        if (!meetsResolution)
        {
            if (width < spec.MinWidth || height < spec.MinHeight)
            {
                issues.Add($"Resolution {width}x{height} is below {spec.Name} minimum ({spec.MinWidth}x{spec.MinHeight})");
            }
            else
            {
                issues.Add($"Resolution {width}x{height} exceeds {spec.Name} maximum ({spec.MaxWidth}x{spec.MaxHeight})");
            }
        }

        // Aspect ratio validation
        var meetsAspectRatio = spec.RecommendedAspectRatios.Contains(aspectRatio);
        if (!meetsAspectRatio)
        {
            warnings.Add($"Aspect ratio {aspectRatio} is not in recommended list for {spec.Name}: {string.Join(", ", spec.RecommendedAspectRatios)}");
            recommendations.Add($"Consider using one of these aspect ratios: {string.Join(", ", spec.RecommendedAspectRatios)}");
        }

        // Duration validation
        var meetsDuration = durationSeconds <= spec.MaxDurationSeconds;
        if (!meetsDuration)
        {
            issues.Add($"Duration {durationSeconds:F0}s exceeds {spec.Name} maximum ({spec.MaxDurationSeconds}s)");
        }

        // File size validation
        var meetsFileSize = fileSizeMB <= spec.MaxFileSizeMB;
        if (!meetsFileSize)
        {
            issues.Add($"File size {fileSizeMB:F2}MB exceeds {spec.Name} maximum ({spec.MaxFileSizeMB}MB)");
            recommendations.Add("Consider using higher compression or lower bitrate");
        }
        else if (fileSizeMB > spec.MaxFileSizeMB * 0.8)
        {
            warnings.Add($"File size {fileSizeMB:F2}MB is approaching {spec.Name} limit ({spec.MaxFileSizeMB}MB)");
        }

        // Codec validation
        var meetsCodec = spec.SupportedCodecs.Any(c => c.Equals(codec, StringComparison.OrdinalIgnoreCase));
        if (!meetsCodec)
        {
            issues.Add($"Codec {codec} is not supported by {spec.Name}. Supported: {string.Join(", ", spec.SupportedCodecs)}");
            recommendations.Add($"Re-encode using one of these codecs: {string.Join(", ", spec.SupportedCodecs)}");
        }

        var score = CalculatePlatformScore(meetsResolution, meetsAspectRatio, meetsDuration, meetsFileSize, meetsCodec);

        return Task.FromResult(new PlatformRequirementsResult
        {
            Platform = spec.Name,
            MeetsResolutionRequirements = meetsResolution,
            MeetsAspectRatioRequirements = meetsAspectRatio,
            MeetsDurationRequirements = meetsDuration,
            MeetsFileSizeRequirements = meetsFileSize,
            MeetsCodecRequirements = meetsCodec,
            FileSizeBytes = fileSizeBytes,
            DurationSeconds = durationSeconds,
            Codec = codec,
            RecommendedOptimizations = recommendations,
            IsValid = meetsResolution && meetsDuration && meetsFileSize && meetsCodec,
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
        if (ratioWidth == 4 && ratioHeight == 5) return "4:5";

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

    private int CalculatePlatformScore(bool resolution, bool aspectRatio, bool duration, bool fileSize, bool codec)
    {
        var score = 0;
        if (resolution) score += 30;
        if (aspectRatio) score += 10;
        if (duration) score += 20;
        if (fileSize) score += 20;
        if (codec) score += 20;

        return score;
    }

    private sealed record PlatformSpec
    {
        public string Name { get; init; } = string.Empty;
        public int MinWidth { get; init; }
        public int MinHeight { get; init; }
        public int MaxWidth { get; init; }
        public int MaxHeight { get; init; }
        public string[] RecommendedAspectRatios { get; init; } = Array.Empty<string>();
        public double MaxFileSizeMB { get; init; }
        public double MaxDurationSeconds { get; init; }
        public string[] SupportedCodecs { get; init; } = Array.Empty<string>();
    }
}
