using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Director;

/// <summary>
/// AI Director service that automatically applies professional cinematographic decisions
/// to video generation based on emotional analysis and director presets.
/// </summary>
public class AIDirectorService : IAIDirectorService
{
    private readonly ILogger<AIDirectorService> _logger;
    private readonly EmotionalArcAnalyzer _emotionalArcAnalyzer;
    
    // Duration constants per preset (in seconds)
    private const double TikTokMaxSceneDuration = 4.0;
    private const double CinematicMinSceneDuration = 8.0;
    private const double StandardSceneDuration = 6.0;
    private const double MinSceneDuration = 2.0;
    private const double MaxSceneDuration = 20.0;
    private const double KeyPointEmphasisMultiplier = 1.15;

    public AIDirectorService(
        ILogger<AIDirectorService> logger,
        EmotionalArcAnalyzer emotionalArcAnalyzer)
    {
        _logger = logger;
        _emotionalArcAnalyzer = emotionalArcAnalyzer;
    }

    /// <inheritdoc />
    public async Task<DirectorDecisions> AnalyzeAndDirectAsync(
        IReadOnlyList<Scene> scenes,
        Brief brief,
        DirectorPreset preset,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "AI Director analyzing {SceneCount} scenes with preset {Preset}",
            scenes.Count, preset);

        // Step 1: Analyze emotional arc
        var emotionalArc = await _emotionalArcAnalyzer.AnalyzeAsync(scenes, brief, ct)
            .ConfigureAwait(false);

        // Step 2: Determine direction for each scene
        var directions = new List<SceneDirection>();
        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            var emotion = i < emotionalArc.SceneEmotions.Count
                ? emotionalArc.SceneEmotions[i]
                : new SceneEmotion(0.5, "neutral", false, "center");

            var direction = DetermineSceneDirection(
                scene, i, scenes.Count, emotion, preset, brief);
            directions.Add(direction);
        }

        // Step 3: Smooth transitions to prevent jarring sequences
        var smoothedDirections = SmoothTransitions(directions);

        _logger.LogInformation(
            "AI Director completed: {OverallStyle}, emotional arc: {Arc}",
            preset.ToString(), emotionalArc.Summary);

        return new DirectorDecisions(
            smoothedDirections,
            preset.ToString(),
            emotionalArc.Summary);
    }

    private SceneDirection DetermineSceneDirection(
        Scene scene,
        int index,
        int totalScenes,
        SceneEmotion emotion,
        DirectorPreset preset,
        Brief brief)
    {
        // Determine Ken Burns motion based on content and emotion
        var motion = SelectKenBurnsMotion(scene, emotion, preset, index, totalScenes);
        
        // Calculate motion intensity based on emotional intensity
        var kenBurnsIntensity = CalculateKenBurnsIntensity(emotion, preset);

        // Select transitions based on emotional shift
        var inTransition = SelectTransition(emotion, isEntry: true, preset, index, totalScenes);
        var outTransition = SelectTransition(emotion, isEntry: false, preset, index, totalScenes);

        // First scene always fades in
        if (index == 0)
        {
            inTransition = DirectorTransitionType.Fade;
        }
        
        // Last scene always fades out
        if (index == totalScenes - 1)
        {
            outTransition = DirectorTransitionType.Fade;
        }

        // Calculate suggested duration based on preset and emotion
        var suggestedDuration = CalculateDuration(scene, emotion, preset);

        return new SceneDirection(
            SceneIndex: index,
            Motion: motion,
            InTransition: inTransition,
            OutTransition: outTransition,
            EmotionalIntensity: emotion.Intensity,
            VisualFocus: emotion.FocusPoint,
            SuggestedDuration: suggestedDuration,
            KenBurnsIntensity: kenBurnsIntensity);
    }

    private KenBurnsMotion SelectKenBurnsMotion(
        Scene scene,
        SceneEmotion emotion,
        DirectorPreset preset,
        int index,
        int totalScenes)
    {
        // Custom preset means no automatic motion
        if (preset == DirectorPreset.Custom)
        {
            return KenBurnsMotion.None;
        }

        return preset switch
        {
            DirectorPreset.Documentary => SelectDocumentaryMotion(emotion, index, totalScenes),
            DirectorPreset.TikTokEnergy => SelectTikTokMotion(emotion, index),
            DirectorPreset.Cinematic => SelectCinematicMotion(emotion, index, totalScenes),
            DirectorPreset.Corporate => SelectCorporateMotion(emotion),
            DirectorPreset.Educational => SelectEducationalMotion(emotion),
            DirectorPreset.Storytelling => SelectStorytellingMotion(emotion, index, totalScenes),
            _ => KenBurnsMotion.None
        };
    }

    private static KenBurnsMotion SelectDocumentaryMotion(SceneEmotion emotion, int index, int totalScenes)
    {
        // Documentary: Steady, informative, minimal motion
        if (emotion.Intensity > 0.7)
        {
            return KenBurnsMotion.ZoomIn; // Emphasis on important moments
        }
        
        // Alternate pan directions for visual variety
        return index % 2 == 0 ? KenBurnsMotion.PanLeft : KenBurnsMotion.PanRight;
    }

    private static KenBurnsMotion SelectTikTokMotion(SceneEmotion emotion, int index)
    {
        // TikTok: Fast-paced, dynamic motion
        var motions = new[]
        {
            KenBurnsMotion.ZoomIn,
            KenBurnsMotion.ZoomOut,
            KenBurnsMotion.DiagonalTopLeftToBottomRight,
            KenBurnsMotion.DiagonalBottomRightToTopLeft,
            KenBurnsMotion.PanLeft,
            KenBurnsMotion.PanRight
        };

        // Use emotional intensity to pick more dynamic motions
        var motionIndex = emotion.Intensity > 0.6
            ? index % 4 // More varied motions for high intensity
            : index % 2; // Simpler alternation for lower intensity

        return motions[motionIndex % motions.Length];
    }

    private static KenBurnsMotion SelectCinematicMotion(SceneEmotion emotion, int index, int totalScenes)
    {
        // Cinematic: Slow, dramatic reveals
        if (index == 0)
        {
            return KenBurnsMotion.ZoomOut; // Epic opening reveal
        }
        
        if (index == totalScenes - 1)
        {
            return KenBurnsMotion.ZoomIn; // Intimate closing
        }

        if (emotion.IsKeyPoint)
        {
            return KenBurnsMotion.ZoomIn; // Draw attention to key moments
        }

        // Slow pans for non-key scenes
        return emotion.FocusPoint.Contains("left") 
            ? KenBurnsMotion.PanRight 
            : KenBurnsMotion.PanLeft;
    }

    private static KenBurnsMotion SelectCorporateMotion(SceneEmotion emotion)
    {
        // Corporate: Subtle, professional motion
        if (emotion.IsKeyPoint)
        {
            return KenBurnsMotion.ZoomIn; // Slight emphasis on key points
        }

        // Minimal motion for professional feel
        return KenBurnsMotion.None;
    }

    private static KenBurnsMotion SelectEducationalMotion(SceneEmotion emotion)
    {
        // Educational: Clear, focused on comprehension
        if (emotion.IsKeyPoint)
        {
            return KenBurnsMotion.ZoomIn; // Focus on key learning points
        }

        // No motion for better focus during explanation
        return KenBurnsMotion.None;
    }

    private static KenBurnsMotion SelectStorytellingMotion(SceneEmotion emotion, int index, int totalScenes)
    {
        // Storytelling: Emotion-driven pacing
        var intensity = emotion.Intensity;
        
        if (intensity > 0.8)
        {
            return KenBurnsMotion.ZoomIn; // High drama moments
        }
        
        if (intensity < 0.3)
        {
            return KenBurnsMotion.ZoomOut; // Reflective, wide shots
        }

        // Mid-intensity: gentle movement for flow
        return index % 2 == 0 
            ? KenBurnsMotion.PanLeft 
            : KenBurnsMotion.PanRight;
    }

    private static double CalculateKenBurnsIntensity(SceneEmotion emotion, DirectorPreset preset)
    {
        var baseIntensity = preset switch
        {
            DirectorPreset.Documentary => 0.05,
            DirectorPreset.TikTokEnergy => 0.15,
            DirectorPreset.Cinematic => 0.08,
            DirectorPreset.Corporate => 0.03,
            DirectorPreset.Educational => 0.05,
            DirectorPreset.Storytelling => 0.10,
            _ => 0.05
        };

        // Scale by emotional intensity (within reasonable bounds)
        return baseIntensity * (0.7 + emotion.Intensity * 0.6);
    }

    private static DirectorTransitionType SelectTransition(
        SceneEmotion emotion,
        bool isEntry,
        DirectorPreset preset,
        int index,
        int totalScenes)
    {
        return preset switch
        {
            DirectorPreset.Documentary => SelectDocumentaryTransition(emotion, isEntry),
            DirectorPreset.TikTokEnergy => DirectorTransitionType.Cut, // Fast cuts for energy
            DirectorPreset.Cinematic => SelectCinematicTransition(emotion, isEntry, index, totalScenes),
            DirectorPreset.Corporate => SelectCorporateTransition(emotion),
            DirectorPreset.Educational => DirectorTransitionType.CrossDissolve, // Smooth for comprehension
            DirectorPreset.Storytelling => SelectStorytellingTransition(emotion, isEntry),
            _ => DirectorTransitionType.Cut
        };
    }

    private static DirectorTransitionType SelectDocumentaryTransition(SceneEmotion emotion, bool isEntry)
    {
        // Documentary: Clean cuts, occasional dissolves for topic changes
        if (emotion.IsKeyPoint)
        {
            return DirectorTransitionType.CrossDissolve;
        }
        return DirectorTransitionType.Cut;
    }

    private static DirectorTransitionType SelectCinematicTransition(
        SceneEmotion emotion, 
        bool isEntry,
        int index,
        int totalScenes)
    {
        // Cinematic: Dramatic transitions
        if (emotion.Intensity > 0.7)
        {
            return DirectorTransitionType.Fade; // Dramatic fade for intense moments
        }
        
        if (emotion.IsKeyPoint)
        {
            return DirectorTransitionType.CrossDissolve;
        }

        return DirectorTransitionType.CrossDissolve; // Smooth, professional feel
    }

    private static DirectorTransitionType SelectCorporateTransition(SceneEmotion emotion)
    {
        // Corporate: Clean, professional
        if (emotion.IsKeyPoint)
        {
            return DirectorTransitionType.CrossDissolve;
        }
        return DirectorTransitionType.Cut;
    }

    private static DirectorTransitionType SelectStorytellingTransition(SceneEmotion emotion, bool isEntry)
    {
        // Storytelling: Match emotional shifts
        if (emotion.Intensity > 0.7)
        {
            return DirectorTransitionType.Fade; // Dramatic
        }
        
        if (emotion.Intensity < 0.3)
        {
            return DirectorTransitionType.CrossDissolve; // Contemplative
        }

        return DirectorTransitionType.Cut;
    }

    private static TimeSpan CalculateDuration(Scene scene, SceneEmotion emotion, DirectorPreset preset)
    {
        // Base duration from scene's existing duration if set
        var baseDuration = scene.Duration.TotalSeconds > 0 
            ? scene.Duration.TotalSeconds 
            : StandardSceneDuration;

        // Adjust based on preset
        var adjustedDuration = preset switch
        {
            DirectorPreset.TikTokEnergy => Math.Min(baseDuration * 0.7, TikTokMaxSceneDuration),
            DirectorPreset.Cinematic => Math.Max(baseDuration * 1.3, CinematicMinSceneDuration),
            DirectorPreset.Educational => baseDuration * 1.1, // Slightly longer for comprehension
            _ => baseDuration
        };

        // Key points get slightly longer for emphasis
        if (emotion.IsKeyPoint)
        {
            adjustedDuration *= 1.15;
        }

        // Clamp to reasonable bounds
        adjustedDuration = Math.Clamp(adjustedDuration, 2.0, 20.0);

        return TimeSpan.FromSeconds(adjustedDuration);
    }

    /// <inheritdoc />
    public IReadOnlyList<SceneDirection> SmoothTransitions(IReadOnlyList<SceneDirection> directions)
    {
        if (directions.Count < 2)
        {
            return directions;
        }

        var smoothed = new List<SceneDirection>();
        
        for (int i = 0; i < directions.Count; i++)
        {
            var current = directions[i];
            
            // Check for jarring back-to-back transitions
            if (i > 0)
            {
                var previous = smoothed[i - 1];
                
                // Avoid back-to-back wipes
                if (previous.OutTransition == DirectorTransitionType.Wipe && 
                    current.InTransition == DirectorTransitionType.Wipe)
                {
                    current = current with { InTransition = DirectorTransitionType.Cut };
                }
                
                // Avoid back-to-back slides
                if (previous.OutTransition == DirectorTransitionType.Slide && 
                    current.InTransition == DirectorTransitionType.Slide)
                {
                    current = current with { InTransition = DirectorTransitionType.CrossDissolve };
                }
                
                // Avoid repeated identical Ken Burns motions
                if (previous.Motion == current.Motion && 
                    current.Motion != KenBurnsMotion.None)
                {
                    current = current with { Motion = GetAlternateMotion(current.Motion) };
                }
            }
            
            smoothed.Add(current);
        }

        return smoothed;
    }

    private static KenBurnsMotion GetAlternateMotion(KenBurnsMotion current)
    {
        return current switch
        {
            KenBurnsMotion.ZoomIn => KenBurnsMotion.PanLeft,
            KenBurnsMotion.ZoomOut => KenBurnsMotion.PanRight,
            KenBurnsMotion.PanLeft => KenBurnsMotion.ZoomIn,
            KenBurnsMotion.PanRight => KenBurnsMotion.ZoomOut,
            KenBurnsMotion.PanUp => KenBurnsMotion.PanDown,
            KenBurnsMotion.PanDown => KenBurnsMotion.PanUp,
            KenBurnsMotion.DiagonalTopLeftToBottomRight => KenBurnsMotion.DiagonalBottomRightToTopLeft,
            KenBurnsMotion.DiagonalBottomRightToTopLeft => KenBurnsMotion.DiagonalTopLeftToBottomRight,
            _ => KenBurnsMotion.None
        };
    }
}
