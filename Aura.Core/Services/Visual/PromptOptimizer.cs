using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aura.Core.Models.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Optimizes visual prompts for specific image generation providers
/// (Stable Diffusion, DALL-E 3, Midjourney)
/// </summary>
public class PromptOptimizer
{
    private readonly ILogger<PromptOptimizer> _logger;

    public PromptOptimizer(ILogger<PromptOptimizer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Generate provider-specific optimized prompts
    /// </summary>
    public ProviderSpecificPrompts OptimizeForProviders(VisualPrompt prompt)
    {
        _logger.LogDebug("Optimizing prompt for scene {SceneIndex} for multiple providers", prompt.SceneIndex);

        return new ProviderSpecificPrompts
        {
            StableDiffusion = OptimizeForStableDiffusion(prompt),
            DallE3 = OptimizeForDallE3(prompt),
            Midjourney = OptimizeForMidjourney(prompt)
        };
    }

    /// <summary>
    /// Optimize for Stable Diffusion with emphasis syntax and quality tags
    /// </summary>
    private static string OptimizeForStableDiffusion(VisualPrompt prompt)
    {
        var sb = new StringBuilder();

        sb.Append(prompt.DetailedDescription);
        sb.Append(", ");

        if (prompt.Camera.ShotType != ShotType.MediumShot)
        {
            sb.Append($"({FormatShotType(prompt.Camera.ShotType)}:1.2), ");
        }
        else
        {
            sb.Append($"{FormatShotType(prompt.Camera.ShotType)}, ");
        }

        if (prompt.Camera.Angle != CameraAngle.EyeLevel)
        {
            sb.Append($"({FormatCameraAngle(prompt.Camera.Angle)}:1.1), ");
        }

        sb.Append($"({prompt.Lighting.Mood} lighting:1.2), ");
        sb.Append($"{prompt.Lighting.TimeOfDay}, ");

        if (!string.IsNullOrEmpty(prompt.CompositionGuidelines))
        {
            sb.Append($"({prompt.CompositionGuidelines}:1.1), ");
        }

        sb.Append($"({prompt.Camera.DepthOfField} depth of field:1.1), ");

        foreach (var keyword in prompt.StyleKeywords.Take(5))
        {
            sb.Append($"{keyword}, ");
        }

        if (prompt.QualityTier >= VisualQualityTier.Enhanced)
        {
            sb.Append("(masterpiece:1.3), (best quality:1.3), (ultra detailed:1.2), ");
        }
        else
        {
            sb.Append("high quality, detailed, ");
        }

        sb.Append($"8k uhd, {GetQualityTags(prompt.QualityTier)}");

        var negativePrompt = string.Join(", ", prompt.NegativeElements);
        if (!string.IsNullOrEmpty(negativePrompt))
        {
            sb.Append($"\nNegative prompt: {negativePrompt}");
        }

        return sb.ToString().TrimEnd(',', ' ');
    }

    /// <summary>
    /// Optimize for DALL-E 3 with natural language descriptions
    /// </summary>
    private static string OptimizeForDallE3(VisualPrompt prompt)
    {
        var sb = new StringBuilder();

        sb.Append("A ");
        sb.Append(FormatShotType(prompt.Camera.ShotType).ToLowerInvariant());
        sb.Append(" ");

        if (prompt.Camera.Angle != CameraAngle.EyeLevel)
        {
            sb.Append($"from a {FormatCameraAngle(prompt.Camera.Angle).ToLowerInvariant()} ");
        }

        sb.Append("showing ");
        sb.Append(prompt.DetailedDescription.ToLowerInvariant());
        sb.Append(". ");

        sb.Append($"The scene is lit with {prompt.Lighting.Mood} {prompt.Lighting.Quality} lighting ");
        sb.Append($"during {prompt.Lighting.TimeOfDay}");

        if (prompt.Lighting.Direction != "front")
        {
            sb.Append($" from the {prompt.Lighting.Direction}");
        }
        sb.Append(". ");

        if (!string.IsNullOrEmpty(prompt.CompositionGuidelines))
        {
            sb.Append($"Composed using {prompt.CompositionGuidelines.ToLowerInvariant()}. ");
        }

        if (prompt.ColorPalette.Any())
        {
            sb.Append($"Color palette: {string.Join(", ", prompt.ColorPalette.Take(3))}. ");
        }

        sb.Append($"Style: {string.Join(", ", prompt.StyleKeywords.Take(3))}. ");

        if (prompt.QualityTier >= VisualQualityTier.Enhanced)
        {
            sb.Append("Professional quality, highly detailed, photorealistic.");
        }
        else
        {
            sb.Append("High quality, detailed.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Optimize for Midjourney with parameter syntax
    /// </summary>
    private static string OptimizeForMidjourney(VisualPrompt prompt)
    {
        var sb = new StringBuilder();

        sb.Append(prompt.DetailedDescription);
        sb.Append(", ");
        sb.Append(FormatShotType(prompt.Camera.ShotType).ToLowerInvariant());
        sb.Append(", ");

        if (prompt.Camera.Angle != CameraAngle.EyeLevel)
        {
            sb.Append(FormatCameraAngle(prompt.Camera.Angle).ToLowerInvariant());
            sb.Append(", ");
        }

        sb.Append($"{prompt.Lighting.Mood} lighting");
        sb.Append(", ");
        sb.Append(prompt.Lighting.TimeOfDay);
        sb.Append(", ");

        if (prompt.Camera.DepthOfField == "shallow" || prompt.Camera.DepthOfField == "bokeh")
        {
            sb.Append("bokeh, shallow depth of field, ");
        }

        foreach (var keyword in prompt.StyleKeywords.Take(4))
        {
            sb.Append(keyword);
            sb.Append(", ");
        }

        sb.Append(GetMidjourneyQualityParams(prompt.QualityTier));

        var aspectRatio = GetMidjourneyAspectRatio(prompt.Style);
        sb.Append($" --ar {aspectRatio}");

        if (prompt.QualityTier >= VisualQualityTier.Enhanced)
        {
            sb.Append(" --q 2 --stylize 750");
        }
        else
        {
            sb.Append(" --q 1");
        }

        return sb.ToString().TrimEnd(',', ' ');
    }

    private static string FormatShotType(ShotType shotType)
    {
        return shotType switch
        {
            ShotType.ExtremeWideShot => "extreme wide shot",
            ShotType.WideShot => "wide shot",
            ShotType.FullShot => "full shot",
            ShotType.MediumShot => "medium shot",
            ShotType.MediumCloseUp => "medium close-up",
            ShotType.CloseUp => "close-up",
            ShotType.ExtremeCloseUp => "extreme close-up",
            ShotType.OverTheShoulder => "over-the-shoulder shot",
            ShotType.PointOfView => "point of view shot",
            _ => "medium shot"
        };
    }

    private static string FormatCameraAngle(CameraAngle angle)
    {
        return angle switch
        {
            CameraAngle.EyeLevel => "eye level",
            CameraAngle.HighAngle => "high angle",
            CameraAngle.LowAngle => "low angle",
            CameraAngle.BirdsEye => "bird's eye view",
            CameraAngle.WormsEye => "worm's eye view",
            CameraAngle.DutchAngle => "Dutch angle",
            CameraAngle.OverTheShoulder => "over-the-shoulder",
            _ => "eye level"
        };
    }

    private static string GetQualityTags(VisualQualityTier tier)
    {
        return tier switch
        {
            VisualQualityTier.Premium => "dslr, professional photography, award winning",
            VisualQualityTier.Enhanced => "sharp focus, professional, trending on artstation",
            VisualQualityTier.Standard => "professional, high resolution",
            _ => "good quality"
        };
    }

    private static string GetMidjourneyQualityParams(VisualQualityTier tier)
    {
        return tier switch
        {
            VisualQualityTier.Premium => "professional photography, award winning, ultra detailed",
            VisualQualityTier.Enhanced => "highly detailed, professional quality",
            VisualQualityTier.Standard => "detailed, high quality",
            _ => "good quality"
        };
    }

    private static string GetMidjourneyAspectRatio(VisualStyle style)
    {
        return style switch
        {
            VisualStyle.Cinematic => "16:9",
            VisualStyle.Documentary => "16:9",
            VisualStyle.Illustrated => "4:3",
            VisualStyle.Abstract => "1:1",
            _ => "16:9"
        };
    }
}
