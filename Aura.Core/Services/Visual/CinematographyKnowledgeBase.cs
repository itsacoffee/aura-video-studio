using System.Collections.Generic;
using System.Linq;
using Aura.Core.Models.Visual;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Knowledge base of cinematography techniques, shot types, and lighting patterns
/// </summary>
public class CinematographyKnowledgeBase
{
    private readonly IReadOnlyDictionary<ShotType, CinematographyKnowledge> _shotKnowledge;
    private readonly IReadOnlyList<LightingPattern> _lightingPatterns;

    public CinematographyKnowledgeBase()
    {
        _shotKnowledge = InitializeShotKnowledge();
        _lightingPatterns = InitializeLightingPatterns();
    }

    /// <summary>
    /// Get cinematography knowledge for a specific shot type
    /// </summary>
    public CinematographyKnowledge GetShotKnowledge(ShotType shotType)
    {
        return _shotKnowledge.TryGetValue(shotType, out var knowledge)
            ? knowledge
            : _shotKnowledge[ShotType.MediumShot];
    }

    /// <summary>
    /// Get lighting pattern by name
    /// </summary>
    public LightingPattern? GetLightingPattern(string patternName)
    {
        return _lightingPatterns.FirstOrDefault(p =>
            p.Name.Equals(patternName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Get all lighting patterns
    /// </summary>
    public IReadOnlyList<LightingPattern> GetAllLightingPatterns() => _lightingPatterns;

    /// <summary>
    /// Recommend shot type based on scene importance and emotional intensity
    /// </summary>
    public ShotType RecommendShotType(double importance, double emotionalIntensity, int sceneIndex)
    {
        if (importance > 80 && emotionalIntensity > 70)
        {
            return ShotType.CloseUp;
        }
        else if (importance > 70)
        {
            return ShotType.MediumCloseUp;
        }
        else if (importance < 40 && sceneIndex == 0)
        {
            return ShotType.WideShot;
        }
        else if (emotionalIntensity > 60)
        {
            return ShotType.MediumCloseUp;
        }
        else
        {
            return ShotType.MediumShot;
        }
    }

    /// <summary>
    /// Recommend camera angle based on emotional tone
    /// </summary>
    public CameraAngle RecommendCameraAngle(string tone, double emotionalIntensity)
    {
        var lowerTone = tone.ToLowerInvariant();

        if (lowerTone.Contains("dramatic") || emotionalIntensity > 80)
        {
            return CameraAngle.LowAngle;
        }
        else if (lowerTone.Contains("professional") || lowerTone.Contains("corporate"))
        {
            return CameraAngle.EyeLevel;
        }
        else if (lowerTone.Contains("vulnerable") || lowerTone.Contains("overwhelming"))
        {
            return CameraAngle.HighAngle;
        }
        else if (lowerTone.Contains("creative") || lowerTone.Contains("artistic"))
        {
            return CameraAngle.DutchAngle;
        }
        else
        {
            return CameraAngle.EyeLevel;
        }
    }

    /// <summary>
    /// Recommend lighting based on tone and scene importance
    /// </summary>
    public LightingSetup RecommendLighting(string tone, double importance, double emotionalIntensity)
    {
        var lowerTone = tone.ToLowerInvariant();

        string mood;
        string timeOfDay;
        string quality;
        string direction;

        if (lowerTone.Contains("dramatic") || emotionalIntensity > 80)
        {
            mood = "dramatic";
            quality = "hard";
            direction = "side";
            timeOfDay = importance > 70 ? "golden hour" : "evening";
        }
        else if (lowerTone.Contains("professional") || lowerTone.Contains("corporate"))
        {
            mood = "neutral";
            quality = "soft";
            direction = "front";
            timeOfDay = "day";
        }
        else if (lowerTone.Contains("warm") || lowerTone.Contains("friendly"))
        {
            mood = "warm";
            quality = "soft";
            direction = "front";
            timeOfDay = "golden hour";
        }
        else if (lowerTone.Contains("casual") || lowerTone.Contains("playful"))
        {
            mood = "bright";
            quality = "diffused";
            direction = "top";
            timeOfDay = "day";
        }
        else
        {
            mood = "balanced";
            quality = "soft";
            direction = "front";
            timeOfDay = "day";
        }

        return new LightingSetup
        {
            Mood = mood,
            Quality = quality,
            Direction = direction,
            TimeOfDay = timeOfDay
        };
    }

    private static IReadOnlyDictionary<ShotType, CinematographyKnowledge> InitializeShotKnowledge()
    {
        return new Dictionary<ShotType, CinematographyKnowledge>
        {
            [ShotType.ExtremeWideShot] = new CinematographyKnowledge
            {
                ShotType = ShotType.ExtremeWideShot,
                Description = "Very wide shot showing entire scene with subject small in frame",
                TypicalUsage = "Establishing shots, showing environment and context",
                EmotionalImpact = "Makes subject feel small, emphasizes environment, creates isolation",
                CompositionTips = new[] { "Rule of thirds", "Leading lines to subject", "Environmental context" }
            },
            [ShotType.WideShot] = new CinematographyKnowledge
            {
                ShotType = ShotType.WideShot,
                Description = "Full subject in frame with surrounding environment visible",
                TypicalUsage = "Establishing locations, showing subject in context, action sequences",
                EmotionalImpact = "Balanced, neutral, shows relationships between elements",
                CompositionTips = new[] { "Balance subject and environment", "Use negative space", "Frame within frame" }
            },
            [ShotType.FullShot] = new CinematographyKnowledge
            {
                ShotType = ShotType.FullShot,
                Description = "Subject from head to toe with minimal surrounding space",
                TypicalUsage = "Character introduction, showing body language and movement",
                EmotionalImpact = "Neutral to positive, focuses on subject while maintaining context",
                CompositionTips = new[] { "Center subject", "Leave headroom", "Watch the feet" }
            },
            [ShotType.MediumShot] = new CinematographyKnowledge
            {
                ShotType = ShotType.MediumShot,
                Description = "Subject from waist up, standard conversational framing",
                TypicalUsage = "Dialogue, standard coverage, most versatile shot",
                EmotionalImpact = "Neutral, balanced, natural viewing distance",
                CompositionTips = new[] { "Eye line at upper third", "Shoulder room", "Clean background" }
            },
            [ShotType.MediumCloseUp] = new CinematographyKnowledge
            {
                ShotType = ShotType.MediumCloseUp,
                Description = "Subject from chest up, emphasizing face and upper body",
                TypicalUsage = "Important dialogue, emotional moments, engaging viewer",
                EmotionalImpact = "Engaging, intimate without being uncomfortable",
                CompositionTips = new[] { "Eyes at upper third", "Watch the headroom", "Frame tightly" }
            },
            [ShotType.CloseUp] = new CinematographyKnowledge
            {
                ShotType = ShotType.CloseUp,
                Description = "Face fills most of frame, emphasis on facial expressions",
                TypicalUsage = "Emotional beats, reactions, dramatic moments, revealing details",
                EmotionalImpact = "Intimate, intense, draws viewer into subject's experience",
                CompositionTips = new[] { "Eyes as focal point", "Minimal headroom", "Shallow depth of field" }
            },
            [ShotType.ExtremeCloseUp] = new CinematographyKnowledge
            {
                ShotType = ShotType.ExtremeCloseUp,
                Description = "Very tight on facial feature or object detail",
                TypicalUsage = "Extreme emotion, revealing critical details, artistic emphasis",
                EmotionalImpact = "Very intense, uncomfortable, demands attention",
                CompositionTips = new[] { "Focus on single element", "Use as punctuation", "High contrast" }
            },
            [ShotType.OverTheShoulder] = new CinematographyKnowledge
            {
                ShotType = ShotType.OverTheShoulder,
                Description = "Shot from behind one subject looking at another",
                TypicalUsage = "Conversations, establishing spatial relationships, point of view",
                EmotionalImpact = "Involving, creates viewer participation in conversation",
                CompositionTips = new[] { "Foreground shoulder frames shot", "Clear sight line", "Depth layering" }
            },
            [ShotType.PointOfView] = new CinematographyKnowledge
            {
                ShotType = ShotType.PointOfView,
                Description = "Camera represents character's perspective",
                TypicalUsage = "Subjective experience, immersion, revealing what character sees",
                EmotionalImpact = "Highly immersive, creates empathy and identification",
                CompositionTips = new[] { "Match eye level", "Natural movement", "Focus on what draws attention" }
            }
        };
    }

    private static IReadOnlyList<LightingPattern> InitializeLightingPatterns()
    {
        return new List<LightingPattern>
        {
            new LightingPattern
            {
                Name = "Golden Hour",
                Description = "Warm, soft light during first/last hour of sunlight",
                MoodEffect = "Romantic, nostalgic, warm, beautiful, dreamlike",
                BestUseCases = new[] { "Outdoor scenes", "Emotional moments", "Beauty shots", "Establishing shots" }
            },
            new LightingPattern
            {
                Name = "Three-Point Lighting",
                Description = "Classic setup with key, fill, and back light",
                MoodEffect = "Professional, balanced, flattering, clear",
                BestUseCases = new[] { "Interviews", "Product shots", "Corporate content", "Standard coverage" }
            },
            new LightingPattern
            {
                Name = "Low Key",
                Description = "High contrast with predominantly dark tones",
                MoodEffect = "Dramatic, mysterious, intense, film noir",
                BestUseCases = new[] { "Dramatic scenes", "Thriller content", "Artistic pieces", "Night scenes" }
            },
            new LightingPattern
            {
                Name = "High Key",
                Description = "Low contrast with predominantly bright tones",
                MoodEffect = "Cheerful, optimistic, clean, commercial",
                BestUseCases = new[] { "Comedy", "Commercials", "Beauty content", "Upbeat videos" }
            },
            new LightingPattern
            {
                Name = "Rembrandt",
                Description = "45-degree key light creating triangle of light on shadow side",
                MoodEffect = "Classic, dramatic, artistic, dimensional",
                BestUseCases = new[] { "Portraits", "Dramatic dialogue", "Character studies", "Art pieces" }
            },
            new LightingPattern
            {
                Name = "Butterfly",
                Description = "Light directly in front and above, creating butterfly shadow under nose",
                MoodEffect = "Glamorous, flattering, feminine, beauty-focused",
                BestUseCases = new[] { "Beauty shots", "Fashion", "Glamour", "Close-ups" }
            },
            new LightingPattern
            {
                Name = "Silhouette",
                Description = "Strong backlight with no front fill, subject in shadow",
                MoodEffect = "Mysterious, dramatic, anonymous, artistic",
                BestUseCases = new[] { "Dramatic reveals", "Mystery", "Artistic scenes", "Transitions" }
            },
            new LightingPattern
            {
                Name = "Natural Window Light",
                Description = "Soft directional light from window or opening",
                MoodEffect = "Authentic, intimate, realistic, warm",
                BestUseCases = new[] { "Interviews", "Documentary", "Intimate scenes", "Realistic content" }
            }
        };
    }
}
