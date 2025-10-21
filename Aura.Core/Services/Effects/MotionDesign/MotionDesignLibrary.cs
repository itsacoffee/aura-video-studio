using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services.Effects.MotionDesign;

/// <summary>
/// Library of motion design elements and effects
/// </summary>
public class MotionDesignLibrary
{
    public enum TransitionType
    {
        Fade,
        Dissolve,
        Wipe,
        Slide,
        Zoom,
        Blur,
        CrossDissolve
    }

    public enum AnimationStyle
    {
        Smooth,
        Bouncy,
        Snappy,
        Elastic,
        Linear
    }

    public class MotionEffect
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public float Duration { get; set; } = 1.0f; // seconds
        public AnimationStyle Style { get; set; }
        public Dictionary<string, float> Parameters { get; set; } = new();
    }

    public class LowerThird
    {
        public string Text { get; set; } = string.Empty;
        public string SubText { get; set; } = string.Empty;
        public float DisplayDuration { get; set; } = 3.0f; // seconds
        public AnimationStyle AnimationIn { get; set; }
        public AnimationStyle AnimationOut { get; set; }
        public string Position { get; set; } = "BottomLeft"; // BottomLeft, BottomCenter, BottomRight
    }

    public class KenBurnsEffect
    {
        public float StartZoom { get; set; } = 1.0f;
        public float EndZoom { get; set; } = 1.2f;
        public float StartX { get; set; } = 0.0f;
        public float StartY { get; set; } = 0.0f;
        public float EndX { get; set; } = 0.0f;
        public float EndY { get; set; } = 0.0f;
        public float Duration { get; set; } = 5.0f; // seconds
        public AnimationStyle Style { get; set; } = AnimationStyle.Smooth;
    }

    /// <summary>
    /// Gets professional transition based on content type
    /// </summary>
    public Task<MotionEffect> GetContentBasedTransitionAsync(
        string contentType,
        string fromScene,
        string toScene,
        CancellationToken cancellationToken = default)
    {
        var transitionType = DetermineTransitionType(contentType, fromScene, toScene);
        var effect = CreateTransitionEffect(transitionType);
        
        return Task.FromResult(effect);
    }

    /// <summary>
    /// Creates animated lower third for text overlay
    /// </summary>
    public Task<LowerThird> CreateLowerThirdAsync(
        string text,
        string subText,
        string style = "modern",
        CancellationToken cancellationToken = default)
    {
        var lowerThird = new LowerThird
        {
            Text = text,
            SubText = subText,
            DisplayDuration = CalculateOptimalDisplayDuration(text, subText),
            AnimationIn = AnimationStyle.Smooth,
            AnimationOut = AnimationStyle.Smooth,
            Position = "BottomLeft"
        };

        // Adjust animation based on style
        switch (style.ToLowerInvariant())
        {
            case "dynamic":
                lowerThird.AnimationIn = AnimationStyle.Bouncy;
                lowerThird.AnimationOut = AnimationStyle.Snappy;
                break;
            case "elegant":
                lowerThird.AnimationIn = AnimationStyle.Smooth;
                lowerThird.AnimationOut = AnimationStyle.Smooth;
                break;
            case "energetic":
                lowerThird.AnimationIn = AnimationStyle.Elastic;
                lowerThird.AnimationOut = AnimationStyle.Elastic;
                break;
        }

        return Task.FromResult(lowerThird);
    }

    /// <summary>
    /// Applies Ken Burns effect to static images
    /// </summary>
    public Task<KenBurnsEffect> ApplyKenBurnsEffectAsync(
        int imageWidth,
        int imageHeight,
        float duration,
        string focusPoint = "center",
        CancellationToken cancellationToken = default)
    {
        var effect = new KenBurnsEffect
        {
            Duration = duration,
            Style = AnimationStyle.Smooth
        };

        // Calculate zoom and pan based on focus point
        switch (focusPoint.ToLowerInvariant())
        {
            case "center":
                effect.StartZoom = 1.0f;
                effect.EndZoom = 1.15f;
                effect.StartX = 0.0f;
                effect.EndX = 0.0f;
                effect.StartY = 0.0f;
                effect.EndY = 0.0f;
                break;
                
            case "left":
                effect.StartZoom = 1.0f;
                effect.EndZoom = 1.2f;
                effect.StartX = -0.05f;
                effect.EndX = 0.0f;
                break;
                
            case "right":
                effect.StartZoom = 1.0f;
                effect.EndZoom = 1.2f;
                effect.StartX = 0.05f;
                effect.EndX = 0.0f;
                break;
                
            case "zoom":
                effect.StartZoom = 1.0f;
                effect.EndZoom = 1.3f;
                break;
        }

        return Task.FromResult(effect);
    }

    /// <summary>
    /// Generates library of motion design presets
    /// </summary>
    public Task<List<MotionEffect>> GetMotionDesignPresetsAsync(
        CancellationToken cancellationToken = default)
    {
        var presets = new List<MotionEffect>
        {
            new()
            {
                Name = "Smooth Fade",
                Description = "Gentle fade transition",
                Duration = 0.5f,
                Style = AnimationStyle.Smooth,
                Parameters = new() { ["opacity"] = 1.0f }
            },
            new()
            {
                Name = "Quick Wipe",
                Description = "Fast directional wipe",
                Duration = 0.3f,
                Style = AnimationStyle.Snappy,
                Parameters = new() { ["direction"] = 0.0f } // 0 = left to right
            },
            new()
            {
                Name = "Zoom Transition",
                Description = "Zoom out and in transition",
                Duration = 0.8f,
                Style = AnimationStyle.Smooth,
                Parameters = new() { ["maxZoom"] = 1.5f }
            },
            new()
            {
                Name = "Blur Transition",
                Description = "Blur and focus transition",
                Duration = 0.6f,
                Style = AnimationStyle.Smooth,
                Parameters = new() { ["blurAmount"] = 20.0f }
            }
        };

        return Task.FromResult(presets);
    }

    private TransitionType DetermineTransitionType(string contentType, string fromScene, string toScene)
    {
        // Intelligent transition selection based on content
        if (contentType.Contains("educational", StringComparison.OrdinalIgnoreCase))
        {
            return TransitionType.Fade;
        }
        else if (contentType.Contains("energetic", StringComparison.OrdinalIgnoreCase))
        {
            return TransitionType.Wipe;
        }
        else if (contentType.Contains("dramatic", StringComparison.OrdinalIgnoreCase))
        {
            return TransitionType.Blur;
        }
        
        return TransitionType.CrossDissolve; // Default
    }

    private MotionEffect CreateTransitionEffect(TransitionType type)
    {
        return type switch
        {
            TransitionType.Fade => new MotionEffect
            {
                Name = "Fade",
                Description = "Simple fade transition",
                Duration = 0.5f,
                Style = AnimationStyle.Smooth,
                Parameters = new() { ["opacity"] = 1.0f }
            },
            TransitionType.Wipe => new MotionEffect
            {
                Name = "Wipe",
                Description = "Directional wipe",
                Duration = 0.4f,
                Style = AnimationStyle.Linear,
                Parameters = new() { ["direction"] = 0.0f }
            },
            TransitionType.Zoom => new MotionEffect
            {
                Name = "Zoom",
                Description = "Zoom transition",
                Duration = 0.7f,
                Style = AnimationStyle.Smooth,
                Parameters = new() { ["scale"] = 1.5f }
            },
            TransitionType.Blur => new MotionEffect
            {
                Name = "Blur",
                Description = "Blur and focus",
                Duration = 0.6f,
                Style = AnimationStyle.Smooth,
                Parameters = new() { ["blur"] = 15.0f }
            },
            _ => new MotionEffect
            {
                Name = "Cross Dissolve",
                Description = "Smooth cross dissolve",
                Duration = 0.5f,
                Style = AnimationStyle.Smooth
            }
        };
    }

    private float CalculateOptimalDisplayDuration(string text, string subText)
    {
        // Calculate based on reading speed (average 250 words per minute)
        var totalChars = text.Length + subText.Length;
        var readingTime = (totalChars / 5.0f) / (250.0f / 60.0f); // Assuming average 5 chars per word
        
        // Add buffer time and clamp to reasonable range
        var duration = readingTime + 1.0f; // Add 1 second buffer
        return Math.Max(2.0f, Math.Min(5.0f, duration));
    }
}
