using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Models.Visual;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service for generating detailed, cinematically-informed visual prompts using LLMs
/// </summary>
public class VisualPromptGenerationService
{
    private readonly ILogger<VisualPromptGenerationService> _logger;
    private readonly CinematographyKnowledgeBase _cinematography;
    private readonly VisualContinuityEngine _continuityEngine;
    private readonly PromptOptimizer _promptOptimizer;
    private readonly TimeSpan _llmTimeout = TimeSpan.FromSeconds(30);
    private readonly int _maxRetries = 2;

    public VisualPromptGenerationService(
        ILogger<VisualPromptGenerationService> logger,
        CinematographyKnowledgeBase cinematography,
        VisualContinuityEngine continuityEngine,
        PromptOptimizer promptOptimizer)
    {
        _logger = logger;
        _cinematography = cinematography;
        _continuityEngine = continuityEngine;
        _promptOptimizer = promptOptimizer;
    }

    /// <summary>
    /// Generate visual prompts for all scenes
    /// </summary>
    public async Task<IReadOnlyList<VisualPrompt>> GenerateVisualPromptsAsync(
        IReadOnlyList<Scene> scenes,
        Brief brief,
        ILlmProvider? llmProvider = null,
        IReadOnlyList<SceneTimingSuggestion>? pacingData = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating visual prompts for {SceneCount} scenes", scenes.Count);

        var visualStyle = MapToneToVisualStyle(brief.Tone);
        var prompts = new List<VisualPrompt>();
        VisualPrompt? previousPrompt = null;

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            var importance = pacingData?.FirstOrDefault(p => p.SceneIndex == i)?.ImportanceScore ?? 50.0;
            var emotionalIntensity = pacingData?.FirstOrDefault(p => p.SceneIndex == i)?.EmotionalIntensity ?? 50.0;

            var prompt = await GenerateVisualPromptForSceneAsync(
                scene,
                scenes.ElementAtOrDefault(i - 1),
                brief.Tone,
                visualStyle,
                importance,
                emotionalIntensity,
                previousPrompt,
                llmProvider,
                ct);

            prompts.Add(prompt);
            previousPrompt = prompt;
        }

        _logger.LogInformation("Generated {Count} visual prompts successfully", prompts.Count);
        return prompts;
    }

    /// <summary>
    /// Generate a visual prompt for a single scene
    /// </summary>
    public async Task<VisualPrompt> GenerateVisualPromptForSceneAsync(
        Scene scene,
        Scene? previousScene,
        string tone,
        VisualStyle visualStyle,
        double importance,
        double emotionalIntensity,
        VisualPrompt? previousPrompt,
        ILlmProvider? llmProvider,
        CancellationToken ct = default)
    {
        _logger.LogDebug("Generating visual prompt for scene {SceneIndex}", scene.Index);

        VisualPromptResult? llmResult = null;

        if (llmProvider != null)
        {
            llmResult = await TryGenerateWithLlmAsync(
                scene,
                previousScene,
                tone,
                visualStyle,
                llmProvider,
                ct);
        }

        var shotType = llmResult != null
            ? ParseShotType(llmResult.ShotType)
            : _cinematography.RecommendShotType(importance, emotionalIntensity, scene.Index);

        var cameraAngle = llmResult != null
            ? ParseCameraAngle(llmResult.CameraAngle)
            : _cinematography.RecommendCameraAngle(tone, emotionalIntensity);

        var lighting = llmResult != null
            ? new LightingSetup
            {
                Mood = llmResult.LightingMood,
                Direction = llmResult.LightingDirection,
                Quality = llmResult.LightingQuality,
                TimeOfDay = llmResult.TimeOfDay
            }
            : _cinematography.RecommendLighting(tone, importance, emotionalIntensity);

        var camera = new CameraSetup
        {
            ShotType = shotType,
            Angle = cameraAngle,
            DepthOfField = llmResult?.DepthOfField ?? "medium",
            Movement = "static"
        };

        var detailedDescription = llmResult?.DetailedDescription ?? GenerateFallbackDescription(scene, tone, shotType);
        var compositionGuidelines = llmResult?.CompositionGuidelines ?? GetDefaultComposition(shotType);
        var colorPalette = llmResult?.ColorPalette?.ToList() ?? GenerateColorPalette(tone, lighting.TimeOfDay);
        var styleKeywords = llmResult?.StyleKeywords?.ToList() ?? GenerateStyleKeywords(visualStyle, tone);
        var negativeElements = llmResult?.NegativeElements?.ToList() ?? GetDefaultNegativeElements();

        var continuity = _continuityEngine.AnalyzeContinuity(
            scene,
            previousScene,
            previousPrompt,
            llmResult?.ContinuityElements?.ToList());

        var qualityTier = DetermineQualityTier(importance);

        var prompt = new VisualPrompt
        {
            SceneIndex = scene.Index,
            DetailedDescription = detailedDescription,
            CompositionGuidelines = compositionGuidelines,
            Lighting = lighting,
            ColorPalette = colorPalette,
            Camera = camera,
            StyleKeywords = styleKeywords,
            Style = visualStyle,
            QualityTier = qualityTier,
            ImportanceScore = importance,
            NegativeElements = negativeElements,
            Continuity = continuity,
            Reasoning = llmResult?.Reasoning ?? $"Generated using cinematography guidelines for {tone} tone"
        };

        var providerPrompts = _promptOptimizer.OptimizeForProviders(prompt);
        return prompt with { ProviderPrompts = providerPrompts };
    }

    private async Task<VisualPromptResult?> TryGenerateWithLlmAsync(
        Scene scene,
        Scene? previousScene,
        string tone,
        VisualStyle visualStyle,
        ILlmProvider llmProvider,
        CancellationToken ct)
    {
        for (int attempt = 0; attempt < _maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    _logger.LogDebug("Retry attempt {Attempt} for scene {SceneIndex}", attempt, scene.Index);
                    await Task.Delay(TimeSpan.FromSeconds(1 * attempt), ct);
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(_llmTimeout);

                var result = await llmProvider.GenerateVisualPromptAsync(
                    scene.Script,
                    previousScene?.Script,
                    tone,
                    visualStyle,
                    cts.Token);

                if (result != null)
                {
                    _logger.LogDebug("LLM generated visual prompt for scene {SceneIndex}", scene.Index);
                    return result;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("Visual prompt generation cancelled for scene {SceneIndex}", scene.Index);
                throw;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Visual prompt generation timed out for scene {SceneIndex} (attempt {Attempt})",
                    scene.Index, attempt + 1);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating visual prompt for scene {SceneIndex} (attempt {Attempt})",
                    scene.Index, attempt + 1);
            }
        }

        _logger.LogWarning("Failed to generate visual prompt with LLM for scene {SceneIndex}, using fallback",
            scene.Index);
        return null;
    }

    private static VisualStyle MapToneToVisualStyle(string tone)
    {
        var lowerTone = tone.ToLowerInvariant();

        if (lowerTone.Contains("dramatic"))
            return VisualStyle.Dramatic;
        else if (lowerTone.Contains("professional") || lowerTone.Contains("corporate"))
            return VisualStyle.Realistic;
        else if (lowerTone.Contains("casual") || lowerTone.Contains("playful"))
            return VisualStyle.Animated;
        else if (lowerTone.Contains("artistic"))
            return VisualStyle.Abstract;
        else if (lowerTone.Contains("documentary"))
            return VisualStyle.Documentary;
        else if (lowerTone.Contains("minimal"))
            return VisualStyle.Minimalist;
        else
            return VisualStyle.Cinematic;
    }

    private static string GenerateFallbackDescription(Scene scene, string tone, ShotType shotType)
    {
        var shotDesc = shotType.ToString().Replace("Shot", " shot").ToLowerInvariant();
        return $"{shotDesc} of {scene.Script}. {tone} tone with professional quality and clear composition.";
    }

    private static string GetDefaultComposition(ShotType shotType)
    {
        return shotType switch
        {
            ShotType.CloseUp or ShotType.ExtremeCloseUp => "Eyes as focal point, shallow depth of field, minimal headroom",
            ShotType.WideShot or ShotType.ExtremeWideShot => "Rule of thirds, leading lines, environmental context",
            ShotType.MediumShot => "Eye line at upper third, balanced composition, clean background",
            _ => "Balanced framing, natural composition, clear focal point"
        };
    }

    private static List<string> GenerateColorPalette(string tone, string timeOfDay)
    {
        var lowerTone = tone.ToLowerInvariant();

        if (lowerTone.Contains("dramatic"))
            return new List<string> { "#1a1a1a", "#8b0000", "#ffd700", "#2f4f4f", "#8b4513" };
        else if (lowerTone.Contains("professional"))
            return new List<string> { "#2c3e50", "#ecf0f1", "#3498db", "#95a5a6", "#34495e" };
        else if (timeOfDay.Contains("golden"))
            return new List<string> { "#ffa500", "#ff8c00", "#ffd700", "#ffb347", "#8b4513" };
        else if (lowerTone.Contains("playful"))
            return new List<string> { "#ff6b6b", "#4ecdc4", "#ffe66d", "#a8e6cf", "#ff8b94" };
        else
            return new List<string> { "#34495e", "#ecf0f1", "#3498db", "#2ecc71", "#e74c3c" };
    }

    private static List<string> GenerateStyleKeywords(VisualStyle style, string tone)
    {
        var keywords = new List<string> { "high quality", "professional", "detailed" };

        switch (style)
        {
            case VisualStyle.Cinematic:
                keywords.AddRange(new[] { "cinematic", "film grain", "color graded", "atmospheric" });
                break;
            case VisualStyle.Realistic:
                keywords.AddRange(new[] { "photorealistic", "natural lighting", "authentic" });
                break;
            case VisualStyle.Dramatic:
                keywords.AddRange(new[] { "dramatic lighting", "high contrast", "moody" });
                break;
            case VisualStyle.Documentary:
                keywords.AddRange(new[] { "documentary style", "natural", "authentic" });
                break;
            case VisualStyle.Illustrated:
                keywords.AddRange(new[] { "illustrated", "artistic", "stylized" });
                break;
        }

        return keywords;
    }

    private static List<string> GetDefaultNegativeElements()
    {
        return new List<string>
        {
            "blurry", "low quality", "distorted", "watermark", "text", "logo",
            "oversaturated", "noisy", "artifacts", "duplicated", "cropped badly"
        };
    }

    private static VisualQualityTier DetermineQualityTier(double importance)
    {
        if (importance > 85)
            return VisualQualityTier.Premium;
        else if (importance > 70)
            return VisualQualityTier.Enhanced;
        else if (importance > 40)
            return VisualQualityTier.Standard;
        else
            return VisualQualityTier.Basic;
    }

    private static ShotType ParseShotType(string shotTypeStr)
    {
        var lower = shotTypeStr.ToLowerInvariant().Replace(" ", "").Replace("-", "");

        if (lower.Contains("extremewide") || lower.Contains("ews"))
            return ShotType.ExtremeWideShot;
        else if (lower.Contains("wide") || lower.Contains("ws"))
            return ShotType.WideShot;
        else if (lower.Contains("full") || lower.Contains("fs"))
            return ShotType.FullShot;
        else if (lower.Contains("mediumcloseup") || lower.Contains("mcu"))
            return ShotType.MediumCloseUp;
        else if (lower.Contains("closeup") || lower.Contains("cu"))
            return ShotType.CloseUp;
        else if (lower.Contains("extremecloseup") || lower.Contains("ecu"))
            return ShotType.ExtremeCloseUp;
        else if (lower.Contains("overtheshoulder") || lower.Contains("ots"))
            return ShotType.OverTheShoulder;
        else if (lower.Contains("pov") || lower.Contains("pointofview"))
            return ShotType.PointOfView;
        else
            return ShotType.MediumShot;
    }

    private static CameraAngle ParseCameraAngle(string angleStr)
    {
        var lower = angleStr.ToLowerInvariant().Replace(" ", "").Replace("-", "");

        if (lower.Contains("high"))
            return CameraAngle.HighAngle;
        else if (lower.Contains("low"))
            return CameraAngle.LowAngle;
        else if (lower.Contains("birds") || lower.Contains("overhead"))
            return CameraAngle.BirdsEye;
        else if (lower.Contains("worms") || lower.Contains("ground"))
            return CameraAngle.WormsEye;
        else if (lower.Contains("dutch") || lower.Contains("tilt"))
            return CameraAngle.DutchAngle;
        else if (lower.Contains("shoulder") || lower.Contains("ots"))
            return CameraAngle.OverTheShoulder;
        else
            return CameraAngle.EyeLevel;
    }
}
