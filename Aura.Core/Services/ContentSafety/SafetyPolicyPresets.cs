using System.Collections.Generic;
using Aura.Core.Models.ContentSafety;

namespace Aura.Core.Services.ContentSafety;

/// <summary>
/// Provides preset safety policies
/// </summary>
public static class SafetyPolicyPresets
{
    /// <summary>
    /// Get unrestricted policy (no filtering)
    /// </summary>
    public static SafetyPolicy GetUnrestrictedPolicy()
    {
        return new SafetyPolicy
        {
            Name = "Unrestricted",
            Description = "No content filtering. User assumes all responsibility.",
            Preset = SafetyPolicyPreset.Unrestricted,
            IsEnabled = false,
            AllowUserOverride = true,
            Categories = new Dictionary<SafetyCategoryType, SafetyCategory>(),
            KeywordRules = new List<KeywordRule>(),
            TopicFilters = new List<TopicFilter>()
        };
    }

    /// <summary>
    /// Get minimal filtering policy (only illegal content)
    /// </summary>
    public static SafetyPolicy GetMinimalPolicy()
    {
        var policy = new SafetyPolicy
        {
            Name = "Minimal",
            Description = "Only blocks illegal content and extreme hate speech.",
            Preset = SafetyPolicyPreset.Minimal,
            IsEnabled = true,
            AllowUserOverride = true
        };

        policy.Categories[SafetyCategoryType.HateSpeech] = new SafetyCategory
        {
            Type = SafetyCategoryType.HateSpeech,
            Threshold = 8,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.Categories[SafetyCategoryType.Violence] = new SafetyCategory
        {
            Type = SafetyCategoryType.Violence,
            Threshold = 9,
            IsEnabled = true,
            DefaultAction = SafetyAction.Warn
        };

        return policy;
    }

    /// <summary>
    /// Get moderate filtering policy (common presets)
    /// </summary>
    public static SafetyPolicy GetModeratePolicy()
    {
        var policy = new SafetyPolicy
        {
            Name = "Moderate",
            Description = "Balanced filtering appropriate for general audiences and platforms like YouTube.",
            Preset = SafetyPolicyPreset.Moderate,
            IsEnabled = true,
            AllowUserOverride = true
        };

        policy.Categories[SafetyCategoryType.Profanity] = new SafetyCategory
        {
            Type = SafetyCategoryType.Profanity,
            Threshold = 5,
            IsEnabled = true,
            DefaultAction = SafetyAction.Warn
        };

        policy.Categories[SafetyCategoryType.Violence] = new SafetyCategory
        {
            Type = SafetyCategoryType.Violence,
            Threshold = 6,
            IsEnabled = true,
            DefaultAction = SafetyAction.Warn
        };

        policy.Categories[SafetyCategoryType.SexualContent] = new SafetyCategory
        {
            Type = SafetyCategoryType.SexualContent,
            Threshold = 3,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.Categories[SafetyCategoryType.HateSpeech] = new SafetyCategory
        {
            Type = SafetyCategoryType.HateSpeech,
            Threshold = 5,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.Categories[SafetyCategoryType.DrugAlcohol] = new SafetyCategory
        {
            Type = SafetyCategoryType.DrugAlcohol,
            Threshold = 5,
            IsEnabled = true,
            DefaultAction = SafetyAction.Warn
        };

        policy.Categories[SafetyCategoryType.SelfHarm] = new SafetyCategory
        {
            Type = SafetyCategoryType.SelfHarm,
            Threshold = 3,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        return policy;
    }

    /// <summary>
    /// Get strict filtering policy (family-friendly)
    /// </summary>
    public static SafetyPolicy GetStrictPolicy()
    {
        var policy = new SafetyPolicy
        {
            Name = "Strict",
            Description = "Family-friendly content only. Suitable for educational and children's content.",
            Preset = SafetyPolicyPreset.Strict,
            IsEnabled = true,
            AllowUserOverride = false
        };

        policy.Categories[SafetyCategoryType.Profanity] = new SafetyCategory
        {
            Type = SafetyCategoryType.Profanity,
            Threshold = 1,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.Categories[SafetyCategoryType.Violence] = new SafetyCategory
        {
            Type = SafetyCategoryType.Violence,
            Threshold = 2,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.Categories[SafetyCategoryType.SexualContent] = new SafetyCategory
        {
            Type = SafetyCategoryType.SexualContent,
            Threshold = 0,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.Categories[SafetyCategoryType.HateSpeech] = new SafetyCategory
        {
            Type = SafetyCategoryType.HateSpeech,
            Threshold = 0,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.Categories[SafetyCategoryType.DrugAlcohol] = new SafetyCategory
        {
            Type = SafetyCategoryType.DrugAlcohol,
            Threshold = 1,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.Categories[SafetyCategoryType.ControversialTopics] = new SafetyCategory
        {
            Type = SafetyCategoryType.ControversialTopics,
            Threshold = 2,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.Categories[SafetyCategoryType.SelfHarm] = new SafetyCategory
        {
            Type = SafetyCategoryType.SelfHarm,
            Threshold = 0,
            IsEnabled = true,
            DefaultAction = SafetyAction.Block
        };

        policy.AgeSettings = new AgeAppropriatenessSettings
        {
            MinimumAge = 0,
            MaximumAge = 13,
            TargetRating = ContentRating.General,
            RequireParentalGuidance = false
        };

        return policy;
    }

    /// <summary>
    /// Get all preset policies
    /// </summary>
    public static Dictionary<SafetyPolicyPreset, SafetyPolicy> GetAllPresets()
    {
        return new Dictionary<SafetyPolicyPreset, SafetyPolicy>
        {
            [SafetyPolicyPreset.Unrestricted] = GetUnrestrictedPolicy(),
            [SafetyPolicyPreset.Minimal] = GetMinimalPolicy(),
            [SafetyPolicyPreset.Moderate] = GetModeratePolicy(),
            [SafetyPolicyPreset.Strict] = GetStrictPolicy()
        };
    }
}
