using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Models.Export;
using Aura.Core.Providers;
using Aura.Core.Rendering;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Render;

/// <summary>
/// Request for preset recommendation
/// </summary>
public record PresetRecommendationRequest
{
    public string? TargetPlatform { get; init; }
    public string? ContentType { get; init; }
    public string? AspectRatioPreference { get; init; }
    public TimeSpan? VideoDuration { get; init; }
    public string? ProjectGoal { get; init; }
    public string? Audience { get; init; }
    public bool RequireHighQuality { get; init; }
    public bool RequireArchival { get; init; }
}

/// <summary>
/// Preset recommendation response
/// </summary>
public record PresetRecommendation
{
    public string PresetName { get; init; } = string.Empty;
    public ExportPreset Preset { get; init; } = ExportPresets.YouTube1080p;
    public string Reasoning { get; init; } = string.Empty;
    public List<string> AlternativePresets { get; init; } = new();
    public double ConfidenceScore { get; init; }
    public Dictionary<string, string> OptimizationSuggestions { get; init; } = new();
}

/// <summary>
/// Service for recommending optimal presets based on project requirements
/// </summary>
public class PresetRecommendationService
{
    private readonly ILogger<PresetRecommendationService> _logger;
    private readonly IHardwareDetector _hardwareDetector;
    private readonly ILlmProvider? _llmProvider;

    public PresetRecommendationService(
        ILogger<PresetRecommendationService> logger,
        IHardwareDetector hardwareDetector,
        ILlmProvider? llmProvider = null)
    {
        _logger = logger;
        _hardwareDetector = hardwareDetector;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Recommends the best preset based on project requirements
    /// </summary>
    public async Task<PresetRecommendation> RecommendPresetAsync(
        PresetRecommendationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Recommending preset: Platform={Platform}, ContentType={ContentType}, Duration={Duration}",
            request.TargetPlatform, request.ContentType, request.VideoDuration);

        if (_llmProvider != null)
        {
            try
            {
                var llmRecommendation = await GetLlmRecommendationAsync(request, cancellationToken).ConfigureAwait(false);
                if (llmRecommendation != null)
                {
                    _logger.LogInformation("Using LLM-based recommendation: {Preset}", llmRecommendation.PresetName);
                    return llmRecommendation;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM recommendation failed, falling back to rule-based");
            }
        }

        return await GetRuleBasedRecommendationAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PresetRecommendation?> GetLlmRecommendationAsync(
        PresetRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        if (_llmProvider == null) return null;

        var prompt = BuildRecommendationPrompt(request);
        
        try
        {
            var response = await _llmProvider.CompleteAsync(prompt, cancellationToken).ConfigureAwait(false);
            return ParseLlmResponse(response);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get LLM recommendation");
            return null;
        }
    }

    private string BuildRecommendationPrompt(PresetRecommendationRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a video export preset recommendation expert. Based on the following project requirements, recommend the most suitable export preset.");
        sb.AppendLine();
        sb.AppendLine("Available Presets:");
        
        var allPresets = ExportPresets.GetAllPresets();
        foreach (var preset in allPresets)
        {
            sb.AppendLine($"- {preset.Name}: {preset.Description} ({preset.Resolution.Width}x{preset.Resolution.Height}, {preset.AspectRatio})");
        }

        sb.AppendLine();
        sb.AppendLine("Project Requirements:");
        
        if (!string.IsNullOrEmpty(request.TargetPlatform))
            sb.AppendLine($"- Target Platform: {request.TargetPlatform}");
        
        if (!string.IsNullOrEmpty(request.ContentType))
            sb.AppendLine($"- Content Type: {request.ContentType}");
        
        if (!string.IsNullOrEmpty(request.AspectRatioPreference))
            sb.AppendLine($"- Aspect Ratio Preference: {request.AspectRatioPreference}");
        
        if (request.VideoDuration.HasValue)
            sb.AppendLine($"- Video Duration: {request.VideoDuration.Value.TotalSeconds:F0} seconds");
        
        if (!string.IsNullOrEmpty(request.ProjectGoal))
            sb.AppendLine($"- Project Goal: {request.ProjectGoal}");
        
        if (!string.IsNullOrEmpty(request.Audience))
            sb.AppendLine($"- Target Audience: {request.Audience}");
        
        if (request.RequireHighQuality)
            sb.AppendLine("- Requires High Quality: Yes");
        
        if (request.RequireArchival)
            sb.AppendLine("- Requires Archival Quality: Yes");

        sb.AppendLine();
        sb.AppendLine("Please respond in the following format:");
        sb.AppendLine("PRESET: [preset name]");
        sb.AppendLine("REASONING: [explanation of why this preset is best]");
        sb.AppendLine("ALTERNATIVES: [comma-separated list of alternative presets]");
        sb.AppendLine("CONFIDENCE: [0-100 confidence score]");
        sb.AppendLine("SUGGESTIONS: [any optimization suggestions]");

        return sb.ToString();
    }

    private PresetRecommendation? ParseLlmResponse(string response)
    {
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string? presetName = null;
        string reasoning = string.Empty;
        var alternatives = new List<string>();
        double confidence = 75.0;
        var suggestions = new Dictionary<string, string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("PRESET:", StringComparison.OrdinalIgnoreCase))
            {
                presetName = line.Substring(7).Trim();
            }
            else if (line.StartsWith("REASONING:", StringComparison.OrdinalIgnoreCase))
            {
                reasoning = line.Substring(10).Trim();
            }
            else if (line.StartsWith("ALTERNATIVES:", StringComparison.OrdinalIgnoreCase))
            {
                var altList = line.Substring(13).Trim();
                alternatives = altList.Split(',').Select(a => a.Trim()).ToList();
            }
            else if (line.StartsWith("CONFIDENCE:", StringComparison.OrdinalIgnoreCase))
            {
                if (double.TryParse(line.Substring(11).Trim(), out var conf))
                {
                    confidence = conf;
                }
            }
            else if (line.StartsWith("SUGGESTIONS:", StringComparison.OrdinalIgnoreCase))
            {
                suggestions["general"] = line.Substring(12).Trim();
            }
        }

        if (string.IsNullOrEmpty(presetName))
        {
            return null;
        }

        var preset = ExportPresets.GetPresetByName(presetName);
        if (preset == null)
        {
            _logger.LogWarning("LLM recommended unknown preset: {PresetName}", presetName);
            return null;
        }

        return new PresetRecommendation
        {
            PresetName = preset.Name,
            Preset = preset,
            Reasoning = reasoning,
            AlternativePresets = alternatives,
            ConfidenceScore = confidence,
            OptimizationSuggestions = suggestions
        };
    }

    private async Task<PresetRecommendation> GetRuleBasedRecommendationAsync(
        PresetRecommendationRequest request,
        CancellationToken cancellationToken)
    {
        var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
        
        var preset = SelectPresetByRules(request, systemProfile);
        var alternatives = GetAlternativePresets(preset);
        var reasoning = BuildRuleBasedReasoning(request, preset, systemProfile);
        var suggestions = GetOptimizationSuggestions(preset, systemProfile, request);

        return new PresetRecommendation
        {
            PresetName = preset.Name,
            Preset = preset,
            Reasoning = reasoning,
            AlternativePresets = alternatives,
            ConfidenceScore = 90.0,
            OptimizationSuggestions = suggestions
        };
    }

    private ExportPreset SelectPresetByRules(PresetRecommendationRequest request, SystemProfile systemProfile)
    {
        if (request.RequireArchival)
        {
            return systemProfile.Tier >= HardwareTier.A ? ExportPresets.ProRes422HQ : ExportPresets.MasterArchive;
        }

        var platform = request.TargetPlatform?.ToLowerInvariant();
        
        if (platform == "youtube")
        {
            if (request.AspectRatioPreference?.Contains("vertical", StringComparison.OrdinalIgnoreCase) == true ||
                request.AspectRatioPreference?.Contains("9:16", StringComparison.OrdinalIgnoreCase) == true)
            {
                return ExportPresets.InstagramStory;
            }

            if (systemProfile.Tier >= HardwareTier.A && request.RequireHighQuality)
            {
                return ExportPresets.YouTube4K;
            }

            return ExportPresets.YouTube1080p;
        }

        if (platform == "tiktok")
        {
            return ExportPresets.TikTok;
        }

        if (platform == "instagram")
        {
            if (request.AspectRatioPreference?.Contains("square", StringComparison.OrdinalIgnoreCase) == true ||
                request.AspectRatioPreference?.Contains("1:1", StringComparison.OrdinalIgnoreCase) == true)
            {
                return ExportPresets.InstagramFeed;
            }

            return ExportPresets.InstagramStory;
        }

        if (platform == "facebook")
        {
            return ExportPresets.Facebook;
        }

        if (platform == "twitter")
        {
            return ExportPresets.Twitter;
        }

        if (platform == "linkedin")
        {
            return ExportPresets.LinkedIn;
        }

        return ExportPresets.YouTube1080p;
    }

    private List<string> GetAlternativePresets(ExportPreset selectedPreset)
    {
        var alternatives = new List<string>();

        var sameAspect = ExportPresets.GetAllPresets()
            .Where(p => p.AspectRatio == selectedPreset.AspectRatio && p.Name != selectedPreset.Name)
            .OrderByDescending(p => p.VideoBitrate)
            .Take(2)
            .Select(p => p.Name);

        alternatives.AddRange(sameAspect);

        if (alternatives.Count < 3)
        {
            var samePlatform = ExportPresets.GetAllPresets()
                .Where(p => p.Platform == selectedPreset.Platform && p.Name != selectedPreset.Name)
                .Take(3 - alternatives.Count)
                .Select(p => p.Name);

            alternatives.AddRange(samePlatform);
        }

        return alternatives;
    }

    private string BuildRuleBasedReasoning(
        PresetRecommendationRequest request,
        ExportPreset selectedPreset,
        SystemProfile systemProfile)
    {
        var reasons = new List<string>();

        if (request.TargetPlatform != null)
        {
            reasons.Add($"Optimized for {request.TargetPlatform}");
        }

        reasons.Add($"{selectedPreset.AspectRatio} aspect ratio");
        reasons.Add($"{selectedPreset.Resolution.Width}x{selectedPreset.Resolution.Height} resolution");

        if (selectedPreset.Quality == QualityLevel.High || selectedPreset.Quality == QualityLevel.Maximum)
        {
            reasons.Add("High quality encoding");
        }

        if (systemProfile.Tier >= HardwareTier.A)
        {
            reasons.Add("Your hardware supports high-quality encoding");
        }

        return string.Join(". ", reasons) + ".";
    }

    private Dictionary<string, string> GetOptimizationSuggestions(
        ExportPreset preset,
        SystemProfile systemProfile,
        PresetRecommendationRequest request)
    {
        var suggestions = new Dictionary<string, string>();

        if (systemProfile.EnableNVENC && !preset.VideoCodec.Contains("nvenc"))
        {
            suggestions["encoder"] = "Enable NVENC hardware acceleration for 5-10x faster encoding";
        }

        if (preset.VideoBitrate > 20000 && systemProfile.Tier < HardwareTier.B)
        {
            suggestions["quality"] = "Consider a lower bitrate preset for better performance on this hardware";
        }

        if (request.VideoDuration.HasValue && request.VideoDuration.Value.TotalMinutes > 30)
        {
            suggestions["duration"] = "For long videos, consider using draft quality for preview exports";
        }

        if (preset.Platform != Aura.Core.Models.Export.Platform.Generic && request.VideoDuration.HasValue)
        {
            try
            {
                var profile = PlatformExportProfileFactory.GetProfile(preset.Platform);
                if (profile.MaxDuration.HasValue && request.VideoDuration.Value.TotalSeconds > profile.MaxDuration.Value * 0.9)
                {
                    suggestions["platform"] = $"{preset.Platform} has a {profile.MaxDuration}s duration limit - you're close to the limit";
                }
            }
            catch (ArgumentException)
            {
                // Generic platform doesn't have a profile
            }
        }

        return suggestions;
    }
}
