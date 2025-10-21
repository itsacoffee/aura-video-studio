using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;
using Asset = Aura.Core.Models.Asset;

namespace Aura.Core.Validation;

/// <summary>
/// Validates outputs from TTS providers
/// </summary>
public class TtsOutputValidator
{
    private readonly ILogger<TtsOutputValidator> _logger;
    private const long MinAudioFileSizeBytes = 1024; // 1KB minimum
    private const long MaxAudioFileSizeBytes = 100 * 1024 * 1024; // 100MB maximum

    public TtsOutputValidator(ILogger<TtsOutputValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates TTS audio output file
    /// </summary>
    public ValidationResult ValidateAudioFile(string audioPath, TimeSpan expectedMinDuration)
    {
        var issues = new List<string>();

        // Check file exists
        if (!File.Exists(audioPath))
        {
            issues.Add($"Audio file not found at path: {audioPath}");
            return new ValidationResult(false, issues);
        }

        try
        {
            var fileInfo = new FileInfo(audioPath);

            // Check file size
            if (fileInfo.Length < MinAudioFileSizeBytes)
            {
                issues.Add($"Audio file too small ({fileInfo.Length} bytes), likely empty or corrupted.");
            }

            if (fileInfo.Length > MaxAudioFileSizeBytes)
            {
                issues.Add($"Audio file too large ({fileInfo.Length / (1024 * 1024)}MB), exceeds 100MB limit.");
            }

            // Check file extension
            var extension = Path.GetExtension(audioPath).ToLowerInvariant();
            if (extension != ".wav" && extension != ".mp3" && extension != ".m4a")
            {
                issues.Add($"Unexpected audio format: {extension}. Expected .wav, .mp3, or .m4a");
            }

            // Estimate duration based on file size (rough heuristic for WAV files)
            // WAV at 44.1kHz, 16-bit, stereo ≈ 176KB per second
            if (extension == ".wav" && fileInfo.Length > MinAudioFileSizeBytes)
            {
                double estimatedSeconds = fileInfo.Length / (176.0 * 1024);
                if (estimatedSeconds < expectedMinDuration.TotalSeconds * 0.5)
                {
                    _logger.LogWarning(
                        "Audio file may be shorter than expected. Estimated {Estimated}s, expected at least {Expected}s",
                        estimatedSeconds, expectedMinDuration.TotalSeconds);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating audio file {Path}", audioPath);
            issues.Add($"Failed to validate audio file: {ex.Message}");
        }

        return new ValidationResult(issues.Count == 0, issues);
    }
}

/// <summary>
/// Validates outputs from image generation providers
/// </summary>
public class ImageOutputValidator
{
    private readonly ILogger<ImageOutputValidator> _logger;
    private const long MinImageFileSizeBytes = 512; // 512 bytes minimum
    private const long MaxImageFileSizeBytes = 50 * 1024 * 1024; // 50MB maximum

    public ImageOutputValidator(ILogger<ImageOutputValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates image assets
    /// </summary>
    public ValidationResult ValidateImageAssets(IReadOnlyList<Asset> assets, int expectedMinCount = 1)
    {
        var issues = new List<string>();

        // Check count
        if (assets.Count < expectedMinCount)
        {
            issues.Add($"Insufficient assets generated. Got {assets.Count}, expected at least {expectedMinCount}.");
        }

        // Validate each asset
        foreach (var asset in assets)
        {
            var assetPath = asset.PathOrUrl;
            
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                issues.Add("Asset has empty path.");
                continue;
            }

            // Skip validation for URLs
            if (assetPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                assetPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!File.Exists(assetPath))
            {
                issues.Add($"Asset file not found: {assetPath}");
                continue;
            }

            try
            {
                var fileInfo = new FileInfo(assetPath);

                // Check file size
                if (fileInfo.Length < MinImageFileSizeBytes)
                {
                    issues.Add($"Asset file too small ({fileInfo.Length} bytes): {assetPath}");
                }

                if (fileInfo.Length > MaxImageFileSizeBytes)
                {
                    issues.Add($"Asset file too large ({fileInfo.Length / (1024 * 1024)}MB): {assetPath}");
                }

                // Check file extension
                var extension = Path.GetExtension(assetPath).ToLowerInvariant();
                var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                if (!Array.Exists(validExtensions, ext => ext == extension))
                {
                    issues.Add($"Unexpected image format {extension} for: {assetPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating asset {Path}", assetPath);
                issues.Add($"Failed to validate asset {assetPath}: {ex.Message}");
            }
        }

        return new ValidationResult(issues.Count == 0, issues);
    }
}

/// <summary>
/// Validates LLM script generation outputs
/// </summary>
public class LlmOutputValidator
{
    private readonly ILogger<LlmOutputValidator> _logger;

    public LlmOutputValidator(ILogger<LlmOutputValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Performs deep validation of LLM-generated script
    /// </summary>
    public ValidationResult ValidateScriptContent(string script, PlanSpec planSpec)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(script))
        {
            issues.Add("Script is empty or whitespace.");
            return new ValidationResult(false, issues);
        }

        // Check for minimum content
        if (script.Length < 50)
        {
            issues.Add($"Script too short ({script.Length} characters). Minimum 50 characters required.");
        }

        // Check for structural markers
        var lines = script.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        bool hasTitle = lines.Any(l => l.TrimStart().StartsWith("# "));
        bool hasScenes = lines.Any(l => l.TrimStart().StartsWith("## "));

        if (!hasTitle)
        {
            issues.Add("Script missing title (should start with '# Title').");
        }

        if (!hasScenes)
        {
            issues.Add("Script missing scene markers (should have '## Scene Name' markers).");
        }

        // Check for common generation errors
        if (script.Contains("[PLACEHOLDER]", StringComparison.OrdinalIgnoreCase) ||
            script.Contains("TODO", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("Script contains placeholder text, indicating incomplete generation.");
        }

        // Check for repetitive content (potential generation loop)
        if (HasExcessiveRepetition(script))
        {
            issues.Add("Script contains excessive repetition, suggesting generation error.");
        }

        // Check for inappropriate or incomplete content
        if (script.Contains("I cannot", StringComparison.OrdinalIgnoreCase) ||
            script.Contains("I apologize", StringComparison.OrdinalIgnoreCase) ||
            script.Contains("as an AI", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add("Script contains AI refusal language instead of actual content.");
        }

        if (issues.Count > 0)
        {
            _logger.LogWarning("Script validation failed: {Issues}", string.Join("; ", issues));
        }

        return new ValidationResult(issues.Count == 0, issues);
    }

    /// <summary>
    /// Detects excessive repetition in text
    /// </summary>
    private bool HasExcessiveRepetition(string text)
    {
        // Split into sentences
        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToLowerInvariant())
            .Where(s => s.Length > 10)
            .ToList();

        if (sentences.Count < 3)
        {
            return false;
        }

        // Count repeated sentences
        var uniqueSentences = sentences.Distinct().Count();
        double repetitionRate = 1.0 - ((double)uniqueSentences / sentences.Count);

        // If more than 30% of sentences are repeated, flag it
        return repetitionRate > 0.3;
    }
}
