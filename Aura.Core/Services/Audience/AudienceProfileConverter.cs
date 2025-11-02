using System;
using System.Linq;
using Aura.Core.Models.Audience;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Converts between simple string-based audience and structured AudienceProfile
/// Provides backward compatibility and automatic inference
/// </summary>
public class AudienceProfileConverter
{
    private readonly ILogger<AudienceProfileConverter> _logger;

    public AudienceProfileConverter(ILogger<AudienceProfileConverter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Convert a simple audience string to a basic AudienceProfile with inferences
    /// </summary>
    public AudienceProfile ConvertFromString(string audienceString)
    {
        if (string.IsNullOrWhiteSpace(audienceString))
        {
            return CreateGenericProfile();
        }

        _logger.LogInformation("Converting audience string to profile: {Audience}", audienceString);

        var lowerAudience = audienceString.ToLowerInvariant();
        var builder = new AudienceProfileBuilder($"Auto: {audienceString}");

        InferExpertiseLevel(lowerAudience, builder);
        InferAgeRange(lowerAudience, builder);
        InferEducationLevel(lowerAudience, builder);
        InferTechnicalComfort(lowerAudience, builder);
        InferIndustryAndProfession(lowerAudience, builder);

        var profile = builder
            .SetDescription($"Auto-generated profile from audience: {audienceString}")
            .Build();

        return profile;
    }

    /// <summary>
    /// Convert an AudienceProfile to a simple descriptive string
    /// </summary>
    public string ConvertToString(AudienceProfile profile)
    {
        var parts = new System.Collections.Generic.List<string>();

        if (profile.ExpertiseLevel.HasValue)
        {
            parts.Add(profile.ExpertiseLevel.Value.ToString());
        }

        if (profile.AgeRange != null)
        {
            parts.Add(profile.AgeRange.DisplayName);
        }

        if (!string.IsNullOrWhiteSpace(profile.Profession))
        {
            parts.Add(profile.Profession);
        }
        else if (!string.IsNullOrWhiteSpace(profile.Industry))
        {
            parts.Add($"{profile.Industry} professionals");
        }

        if (parts.Count == 0)
        {
            return "General audience";
        }

        return string.Join(" ", parts);
    }

    private void InferExpertiseLevel(string audience, AudienceProfileBuilder builder)
    {
        if (audience.Contains("beginner") || audience.Contains("novice") || audience.Contains("new to"))
        {
            builder.SetExpertise(ExpertiseLevel.CompleteBeginner);
            builder.AddPainPoint("Overwhelmed by complex information");
            builder.AddMotivation("Learn fundamentals");
        }
        else if (audience.Contains("intermediate") || audience.Contains("some experience"))
        {
            builder.SetExpertise(ExpertiseLevel.Intermediate);
            builder.AddMotivation("Expand existing knowledge");
        }
        else if (audience.Contains("advanced") || audience.Contains("expert") || audience.Contains("professional"))
        {
            builder.SetExpertise(ExpertiseLevel.Advanced);
            builder.AddMotivation("Master advanced concepts");
        }
        else
        {
            builder.SetExpertise(ExpertiseLevel.Intermediate);
        }
    }

    private void InferAgeRange(string audience, AudienceProfileBuilder builder)
    {
        if (audience.Contains("student") || audience.Contains("college") || audience.Contains("university"))
        {
            builder.SetAgeRange(AgeRange.YoungAdults);
            builder.AddPainPoint("Limited time and budget");
        }
        else if (audience.Contains("senior") || audience.Contains("elderly") || audience.Contains("retired"))
        {
            builder.SetAgeRange(AgeRange.Seniors);
            builder.SetAccessibilityNeeds(requiresCaptions: true, requiresLargeText: true);
        }
        else if (audience.Contains("teen") || audience.Contains("teenager") || audience.Contains("youth"))
        {
            builder.SetAgeRange(AgeRange.Teens);
        }
        else if (audience.Contains("child") || audience.Contains("kid"))
        {
            builder.SetAgeRange(AgeRange.Children);
        }
        else if (audience.Contains("professional") || audience.Contains("corporate") || audience.Contains("business"))
        {
            builder.SetAgeRange(25, 44, "Professional Age (25-44)");
        }
        else
        {
            builder.SetAgeRange(AgeRange.Adults);
        }
    }

    private void InferEducationLevel(string audience, AudienceProfileBuilder builder)
    {
        if (audience.Contains("student") || audience.Contains("college") || audience.Contains("university"))
        {
            builder.SetEducation(EducationLevel.InProgress);
        }
        else if (audience.Contains("phd") || audience.Contains("doctorate") || audience.Contains("researcher"))
        {
            builder.SetEducation(EducationLevel.Doctorate);
        }
        else if (audience.Contains("graduate") || audience.Contains("master"))
        {
            builder.SetEducation(EducationLevel.MasterDegree);
        }
        else if (audience.Contains("professional") || audience.Contains("engineer") || audience.Contains("developer"))
        {
            builder.SetEducation(EducationLevel.BachelorDegree);
        }
        else
        {
            builder.SetEducation(EducationLevel.SomeCollege);
        }
    }

    private void InferTechnicalComfort(string audience, AudienceProfileBuilder builder)
    {
        if (audience.Contains("tech") || audience.Contains("developer") || audience.Contains("engineer") || 
            audience.Contains("programmer") || audience.Contains("it "))
        {
            builder.SetTechnicalComfort(TechnicalComfort.TechSavvy);
        }
        else if (audience.Contains("beginner") || audience.Contains("non-technical") || audience.Contains("senior"))
        {
            builder.SetTechnicalComfort(TechnicalComfort.BasicUser);
        }
        else
        {
            builder.SetTechnicalComfort(TechnicalComfort.Moderate);
        }
    }

    private void InferIndustryAndProfession(string audience, AudienceProfileBuilder builder)
    {
        if (audience.Contains("healthcare") || audience.Contains("medical") || audience.Contains("doctor") || audience.Contains("nurse"))
        {
            builder.SetIndustry("Healthcare");
            builder.SetProfession("Healthcare Professional");
        }
        else if (audience.Contains("teacher") || audience.Contains("educator") || audience.Contains("instructor"))
        {
            builder.SetIndustry("Education");
            builder.SetProfession("Educator");
        }
        else if (audience.Contains("developer") || audience.Contains("programmer") || audience.Contains("engineer"))
        {
            builder.SetIndustry("Technology");
            builder.SetProfession("Software Developer");
        }
        else if (audience.Contains("business") || audience.Contains("manager") || audience.Contains("executive"))
        {
            builder.SetIndustry("Business");
            builder.SetProfession("Business Professional");
        }
        else if (audience.Contains("student"))
        {
            builder.SetProfession("Student");
        }
    }

    private AudienceProfile CreateGenericProfile()
    {
        return new AudienceProfileBuilder("General Audience")
            .SetAgeRange(AgeRange.Adults)
            .SetExpertise(ExpertiseLevel.Intermediate)
            .SetTechnicalComfort(TechnicalComfort.Moderate)
            .SetDescription("Generic profile for unspecified audience")
            .Build();
    }
}
