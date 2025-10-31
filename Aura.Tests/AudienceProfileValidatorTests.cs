using Aura.Core.Models.Audience;
using Aura.Core.Services.Audience;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests;

public class AudienceProfileValidatorTests
{
    private readonly Mock<ILogger<AudienceProfileValidator>> _mockLogger;
    private readonly AudienceProfileValidator _validator;

    public AudienceProfileValidatorTests()
    {
        _mockLogger = new Mock<ILogger<AudienceProfileValidator>>();
        _validator = new AudienceProfileValidator(_mockLogger.Object);
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var profile = new AudienceProfileBuilder("")
            .Build();

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
        Assert.Contains(result.Errors, e => e.Field == "Name");
    }

    [Fact]
    public void Validate_WithInvalidAgeRange_ReturnsError()
    {
        var profile = new AudienceProfileBuilder("Test")
            .SetAgeRange(50, 20)
            .Build();

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "AgeRange");
    }

    [Fact]
    public void Validate_WithTooLongPainPoint_ReturnsError()
    {
        var profile = new AudienceProfile
        {
            Name = "Test",
            PainPoints = new System.Collections.Generic.List<string>
            {
                new string('a', 501)
            }
        };

        var result = _validator.Validate(profile);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Field == "PainPoints");
    }

    [Fact]
    public void Validate_ExpertWithSimplifiedLanguage_ReturnsWarning()
    {
        var profile = new AudienceProfileBuilder("Test")
            .SetExpertise(ExpertiseLevel.Expert)
            .SetAccessibilityNeeds(requiresSimplifiedLanguage: true)
            .Build();

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => 
            w.Message.Contains("Expert level with simplified language"));
    }

    [Fact]
    public void Validate_BeginnerWithExpertTechnical_ReturnsWarning()
    {
        var profile = new AudienceProfileBuilder("Test")
            .SetExpertise(ExpertiseLevel.CompleteBeginner)
            .SetTechnicalComfort(TechnicalComfort.Expert)
            .Build();

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => 
            w.Message.Contains("Complete beginner with expert technical comfort"));
    }

    [Fact]
    public void Validate_ChildWithExpertTechnical_ReturnsWarning()
    {
        var profile = new AudienceProfileBuilder("Test")
            .SetAgeRange(AgeRange.Children)
            .SetTechnicalComfort(TechnicalComfort.Expert)
            .Build();

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => 
            w.Message.Contains("Children under 13 with expert technical comfort"));
    }

    [Fact]
    public void Validate_IncompleteProfile_ReturnsWarning()
    {
        var profile = new AudienceProfileBuilder("Test")
            .Build();

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => 
            w.Message.Contains("Profile is incomplete"));
    }

    [Fact]
    public void Validate_NoPsychographics_ReturnsInfo()
    {
        var profile = new AudienceProfileBuilder("Test")
            .SetAgeRange(AgeRange.Adults)
            .SetEducation(EducationLevel.BachelorDegree)
            .SetExpertise(ExpertiseLevel.Intermediate)
            .Build();

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.NotEmpty(result.Infos);
        Assert.Contains(result.Infos, i => 
            i.Message.Contains("No interests, pain points, or motivations"));
    }

    [Fact]
    public void Validate_CompleteProfile_Passes()
    {
        var profile = new AudienceProfileBuilder("Complete Profile")
            .SetAgeRange(AgeRange.Adults)
            .SetEducation(EducationLevel.BachelorDegree)
            .SetExpertise(ExpertiseLevel.Intermediate)
            .SetTechnicalComfort(TechnicalComfort.Moderate)
            .SetLearningStyle(LearningStyle.Visual)
            .AddInterest("Technology")
            .AddPainPoint("Limited time")
            .AddMotivation("Career growth")
            .Build();

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_InterestsWithoutPainPoints_ReturnsInfo()
    {
        var profile = new AudienceProfileBuilder("Test")
            .AddInterest("Tech")
            .Build();

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.Contains(result.Infos, i => 
            i.Field == "PainPoints" && 
            i.Message.Contains("Consider adding pain points"));
    }

    [Fact]
    public void Validate_PainPointsWithoutMotivations_ReturnsInfo()
    {
        var profile = new AudienceProfileBuilder("Test")
            .AddPainPoint("Time constraints")
            .Build();

        var result = _validator.Validate(profile);

        Assert.True(result.IsValid);
        Assert.Contains(result.Infos, i => 
            i.Field == "Motivations" && 
            i.Message.Contains("Consider adding motivations"));
    }
}
