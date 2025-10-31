using System;
using System.Collections.Generic;
using Aura.Core.Models.Audience;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Fluent API builder for creating audience profiles
/// Supports method chaining for intuitive profile construction
/// </summary>
public class AudienceProfileBuilder
{
    private readonly AudienceProfile _profile;

    public AudienceProfileBuilder(string? name = null)
    {
        _profile = new AudienceProfile
        {
            Name = name ?? "Untitled Profile"
        };
    }

    /// <summary>
    /// Set the age range
    /// </summary>
    public AudienceProfileBuilder SetAgeRange(int minAge, int maxAge, string? displayName = null)
    {
        _profile.AgeRange = new AgeRange
        {
            MinAge = minAge,
            MaxAge = maxAge,
            DisplayName = displayName ?? $"{minAge}-{maxAge}",
            ContentRating = DetermineContentRating(minAge)
        };
        return this;
    }

    /// <summary>
    /// Set predefined age range
    /// </summary>
    public AudienceProfileBuilder SetAgeRange(AgeRange ageRange)
    {
        _profile.AgeRange = ageRange;
        return this;
    }

    /// <summary>
    /// Set education level
    /// </summary>
    public AudienceProfileBuilder SetEducation(EducationLevel level)
    {
        _profile.EducationLevel = level;
        return this;
    }

    /// <summary>
    /// Set profession
    /// </summary>
    public AudienceProfileBuilder SetProfession(string profession)
    {
        _profile.Profession = profession;
        return this;
    }

    /// <summary>
    /// Set industry
    /// </summary>
    public AudienceProfileBuilder SetIndustry(string industry)
    {
        _profile.Industry = industry;
        return this;
    }

    /// <summary>
    /// Set expertise level
    /// </summary>
    public AudienceProfileBuilder SetExpertise(ExpertiseLevel level)
    {
        _profile.ExpertiseLevel = level;
        return this;
    }

    /// <summary>
    /// Set income bracket
    /// </summary>
    public AudienceProfileBuilder SetIncomeBracket(IncomeBracket bracket)
    {
        _profile.IncomeBracket = bracket;
        return this;
    }

    /// <summary>
    /// Set geographic region
    /// </summary>
    public AudienceProfileBuilder SetRegion(GeographicRegion region)
    {
        _profile.GeographicRegion = region;
        return this;
    }

    /// <summary>
    /// Set language fluency
    /// </summary>
    public AudienceProfileBuilder SetLanguage(string language, FluencyLevel level = FluencyLevel.Native)
    {
        _profile.LanguageFluency = new LanguageFluency
        {
            Language = language,
            Level = level
        };
        return this;
    }

    /// <summary>
    /// Add an interest
    /// </summary>
    public AudienceProfileBuilder AddInterest(string interest)
    {
        if (!_profile.Interests.Contains(interest))
        {
            _profile.Interests.Add(interest);
        }
        return this;
    }

    /// <summary>
    /// Add multiple interests
    /// </summary>
    public AudienceProfileBuilder AddInterests(params string[] interests)
    {
        foreach (var interest in interests)
        {
            AddInterest(interest);
        }
        return this;
    }

    /// <summary>
    /// Add a pain point
    /// </summary>
    public AudienceProfileBuilder AddPainPoint(string painPoint)
    {
        if (painPoint.Length > 500)
        {
            throw new ArgumentException("Pain point must be 500 characters or less", nameof(painPoint));
        }
        
        if (!_profile.PainPoints.Contains(painPoint))
        {
            _profile.PainPoints.Add(painPoint);
        }
        return this;
    }

    /// <summary>
    /// Add a motivation
    /// </summary>
    public AudienceProfileBuilder AddMotivation(string motivation)
    {
        if (motivation.Length > 500)
        {
            throw new ArgumentException("Motivation must be 500 characters or less", nameof(motivation));
        }
        
        if (!_profile.Motivations.Contains(motivation))
        {
            _profile.Motivations.Add(motivation);
        }
        return this;
    }

    /// <summary>
    /// Set cultural background
    /// </summary>
    public AudienceProfileBuilder SetCulturalBackground(
        List<string>? sensitivities = null,
        List<string>? tabooTopics = null,
        CommunicationStyle style = CommunicationStyle.Direct)
    {
        _profile.CulturalBackground = new CulturalBackground
        {
            Sensitivities = sensitivities ?? new List<string>(),
            TabooTopics = tabooTopics ?? new List<string>(),
            PreferredCommunicationStyle = style
        };
        return this;
    }

    /// <summary>
    /// Set learning style
    /// </summary>
    public AudienceProfileBuilder SetLearningStyle(LearningStyle style)
    {
        _profile.PreferredLearningStyle = style;
        return this;
    }

    /// <summary>
    /// Set attention span
    /// </summary>
    public AudienceProfileBuilder SetAttentionSpan(AttentionSpan span)
    {
        _profile.AttentionSpan = span;
        return this;
    }

    /// <summary>
    /// Set technical comfort level
    /// </summary>
    public AudienceProfileBuilder SetTechnicalComfort(TechnicalComfort level)
    {
        _profile.TechnicalComfort = level;
        return this;
    }

    /// <summary>
    /// Set accessibility needs
    /// </summary>
    public AudienceProfileBuilder SetAccessibilityNeeds(
        bool requiresCaptions = false,
        bool requiresAudioDescriptions = false,
        bool requiresHighContrast = false,
        bool requiresSimplifiedLanguage = false,
        bool requiresLargeText = false)
    {
        _profile.AccessibilityNeeds = new AccessibilityNeeds
        {
            RequiresCaptions = requiresCaptions,
            RequiresAudioDescriptions = requiresAudioDescriptions,
            RequiresHighContrast = requiresHighContrast,
            RequiresSimplifiedLanguage = requiresSimplifiedLanguage,
            RequiresLargeText = requiresLargeText
        };
        return this;
    }

    /// <summary>
    /// Set description
    /// </summary>
    public AudienceProfileBuilder SetDescription(string description)
    {
        _profile.Description = description;
        return this;
    }

    /// <summary>
    /// Mark as template
    /// </summary>
    public AudienceProfileBuilder AsTemplate()
    {
        _profile.IsTemplate = true;
        return this;
    }

    /// <summary>
    /// Add tags
    /// </summary>
    public AudienceProfileBuilder AddTags(params string[] tags)
    {
        foreach (var tag in tags)
        {
            if (!_profile.Tags.Contains(tag))
            {
                _profile.Tags.Add(tag);
            }
        }
        return this;
    }

    /// <summary>
    /// Build the audience profile
    /// </summary>
    public AudienceProfile Build()
    {
        _profile.UpdatedAt = DateTime.UtcNow;
        return _profile;
    }

    /// <summary>
    /// Determine content rating based on minimum age
    /// </summary>
    private static ContentRating DetermineContentRating(int minAge)
    {
        return minAge switch
        {
            < 13 => ContentRating.ChildSafe,
            < 18 => ContentRating.TeenAppropriate,
            _ => ContentRating.Adult
        };
    }
}
