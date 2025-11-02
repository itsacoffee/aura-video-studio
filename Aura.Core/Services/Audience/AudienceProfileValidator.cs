using System.Linq;
using Aura.Core.Models.Audience;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Validates audience profiles for completeness and consistency
/// </summary>
public class AudienceProfileValidator
{
    private readonly ILogger<AudienceProfileValidator> _logger;

    public AudienceProfileValidator(ILogger<AudienceProfileValidator> _logger)
    {
        this._logger = _logger;
    }

    /// <summary>
    /// Validate an audience profile
    /// </summary>
    public ValidationResult Validate(AudienceProfile profile)
    {
        var result = new ValidationResult();

        ValidateBasicFields(profile, result);
        ValidateConsistency(profile, result);
        ValidateCompleteness(profile, result);
        ProvideOptimizationSuggestions(profile, result);

        _logger.LogInformation(
            "Validated profile {ProfileId}: {ErrorCount} errors, {WarningCount} warnings, {InfoCount} info",
            profile.Id, result.Errors.Count, result.Warnings.Count, result.Infos.Count);

        return result;
    }

    private void ValidateBasicFields(AudienceProfile profile, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            result.Errors.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Field = "Name",
                Message = "Profile name is required",
                SuggestedFix = "Provide a descriptive name for the profile"
            });
        }

        if (profile.AgeRange != null && profile.AgeRange.MinAge > profile.AgeRange.MaxAge)
        {
            result.Errors.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Field = "AgeRange",
                Message = "Minimum age cannot be greater than maximum age",
                SuggestedFix = $"Adjust age range: min={profile.AgeRange.MinAge}, max={profile.AgeRange.MaxAge}"
            });
        }

        foreach (var painPoint in profile.PainPoints)
        {
            if (painPoint.Length > 500)
            {
                result.Errors.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Field = "PainPoints",
                    Message = "Pain point exceeds 500 character limit",
                    SuggestedFix = "Shorten the pain point description"
                });
            }
        }

        foreach (var motivation in profile.Motivations)
        {
            if (motivation.Length > 500)
            {
                result.Errors.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Field = "Motivations",
                    Message = "Motivation exceeds 500 character limit",
                    SuggestedFix = "Shorten the motivation description"
                });
            }
        }
    }

    private void ValidateConsistency(AudienceProfile profile, ValidationResult result)
    {
        if (profile.ExpertiseLevel == ExpertiseLevel.Expert && 
            profile.AccessibilityNeeds?.RequiresSimplifiedLanguage == true)
        {
            result.Warnings.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Field = "ExpertiseLevel, AccessibilityNeeds",
                Message = "Expert level with simplified language requirement may be inconsistent",
                SuggestedFix = "Review: Experts typically prefer technical terminology"
            });
        }

        if (profile.ExpertiseLevel == ExpertiseLevel.CompleteBeginner && 
            profile.TechnicalComfort == TechnicalComfort.Expert)
        {
            result.Warnings.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Field = "ExpertiseLevel, TechnicalComfort",
                Message = "Complete beginner with expert technical comfort seems inconsistent",
                SuggestedFix = "Review: Beginners usually have lower technical comfort"
            });
        }

        if (profile.AgeRange != null && profile.AgeRange.MinAge < 13 && 
            profile.TechnicalComfort == TechnicalComfort.Expert)
        {
            result.Warnings.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Field = "AgeRange, TechnicalComfort",
                Message = "Children under 13 with expert technical comfort is unusual",
                SuggestedFix = "Review age range or technical comfort level"
            });
        }

        if (profile.EducationLevel == EducationLevel.Doctorate && 
            profile.ExpertiseLevel == ExpertiseLevel.CompleteBeginner)
        {
            result.Warnings.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Field = "EducationLevel, ExpertiseLevel",
                Message = "Doctorate education with complete beginner expertise may be inconsistent",
                SuggestedFix = "Consider if this profile accurately reflects the target audience"
            });
        }
    }

    private void ValidateCompleteness(AudienceProfile profile, ValidationResult result)
    {
        var missingFields = 0;

        if (profile.AgeRange == null) missingFields++;
        if (profile.EducationLevel == null) missingFields++;
        if (profile.ExpertiseLevel == null) missingFields++;
        if (profile.TechnicalComfort == null) missingFields++;
        if (profile.PreferredLearningStyle == null) missingFields++;

        if (missingFields >= 3)
        {
            result.Warnings.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Field = "Profile",
                Message = $"Profile is incomplete ({missingFields} key fields missing)",
                SuggestedFix = "Consider adding age range, education, expertise, technical comfort, and learning style"
            });
        }

        if (profile.Interests.Count == 0 && profile.PainPoints.Count == 0 && profile.Motivations.Count == 0)
        {
            result.Infos.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Field = "Psychographics",
                Message = "No interests, pain points, or motivations specified",
                SuggestedFix = "Adding psychographic details helps create more targeted content"
            });
        }
    }

    private void ProvideOptimizationSuggestions(AudienceProfile profile, ValidationResult result)
    {
        if (profile.Interests.Count > 0 && profile.PainPoints.Count == 0)
        {
            result.Infos.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Field = "PainPoints",
                Message = "Consider adding pain points for better content targeting",
                SuggestedFix = "Pain points help create content that addresses specific problems"
            });
        }

        if (profile.PainPoints.Count > 0 && profile.Motivations.Count == 0)
        {
            result.Infos.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Field = "Motivations",
                Message = "Consider adding motivations to understand audience goals",
                SuggestedFix = "Motivations help align content with audience aspirations"
            });
        }

        if (profile.AttentionSpan == null && profile.AgeRange != null)
        {
            result.Infos.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Field = "AttentionSpan",
                Message = "Attention span not specified",
                SuggestedFix = "Setting attention span helps optimize video length"
            });
        }

        if (profile.CulturalBackground == null && profile.GeographicRegion != null)
        {
            result.Infos.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Field = "CulturalBackground",
                Message = "Cultural sensitivities not specified",
                SuggestedFix = "Adding cultural context ensures appropriate content"
            });
        }
    }
}
