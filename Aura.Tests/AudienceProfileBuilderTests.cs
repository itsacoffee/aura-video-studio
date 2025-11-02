using System;
using Aura.Core.Models.Audience;
using Aura.Core.Services.Audience;
using Xunit;

namespace Aura.Tests;

public class AudienceProfileBuilderTests
{
    [Fact]
    public void Build_WithMinimalData_CreatesValidProfile()
    {
        var profile = new AudienceProfileBuilder("Test Profile")
            .Build();

        Assert.NotNull(profile);
        Assert.Equal("Test Profile", profile.Name);
        Assert.NotEqual(Guid.Empty.ToString(), profile.Id);
        Assert.Equal(1, profile.Version);
    }

    [Fact]
    public void SetAgeRange_WithValidRange_SetsCorrectly()
    {
        var profile = new AudienceProfileBuilder()
            .SetAgeRange(25, 34, "Adults 25-34")
            .Build();

        Assert.NotNull(profile.AgeRange);
        Assert.Equal(25, profile.AgeRange.MinAge);
        Assert.Equal(34, profile.AgeRange.MaxAge);
        Assert.Equal("Adults 25-34", profile.AgeRange.DisplayName);
        Assert.Equal(ContentRating.Adult, profile.AgeRange.ContentRating);
    }

    [Fact]
    public void SetAgeRange_WithPredefined_SetsCorrectly()
    {
        var profile = new AudienceProfileBuilder()
            .SetAgeRange(AgeRange.Seniors)
            .Build();

        Assert.NotNull(profile.AgeRange);
        Assert.Equal(55, profile.AgeRange.MinAge);
        Assert.Equal(120, profile.AgeRange.MaxAge);
    }

    [Fact]
    public void FluentAPI_ChainsMethods_BuildsCompleteProfile()
    {
        var profile = new AudienceProfileBuilder("Tech Professionals")
            .SetAgeRange(25, 45)
            .SetEducation(EducationLevel.BachelorDegree)
            .SetIndustry("Technology")
            .SetExpertise(ExpertiseLevel.Advanced)
            .SetTechnicalComfort(TechnicalComfort.TechSavvy)
            .AddInterest("Programming")
            .AddInterest("AI")
            .AddPainPoint("Keeping up with rapid tech changes")
            .AddMotivation("Career advancement")
            .SetLearningStyle(LearningStyle.Visual)
            .Build();

        Assert.Equal("Tech Professionals", profile.Name);
        Assert.NotNull(profile.AgeRange);
        Assert.Equal(EducationLevel.BachelorDegree, profile.EducationLevel);
        Assert.Equal("Technology", profile.Industry);
        Assert.Equal(ExpertiseLevel.Advanced, profile.ExpertiseLevel);
        Assert.Equal(TechnicalComfort.TechSavvy, profile.TechnicalComfort);
        Assert.Equal(2, profile.Interests.Count);
        Assert.Contains("Programming", profile.Interests);
        Assert.Contains("AI", profile.Interests);
        Assert.Single(profile.PainPoints);
        Assert.Single(profile.Motivations);
        Assert.Equal(LearningStyle.Visual, profile.PreferredLearningStyle);
    }

    [Fact]
    public void AddPainPoint_WithTooLongText_ThrowsException()
    {
        var builder = new AudienceProfileBuilder();
        var longText = new string('a', 501);

        var exception = Assert.Throws<ArgumentException>(() => 
            builder.AddPainPoint(longText));
        
        Assert.Contains("500 characters or less", exception.Message);
    }

    [Fact]
    public void AddMotivation_WithTooLongText_ThrowsException()
    {
        var builder = new AudienceProfileBuilder();
        var longText = new string('a', 501);

        var exception = Assert.Throws<ArgumentException>(() => 
            builder.AddMotivation(longText));
        
        Assert.Contains("500 characters or less", exception.Message);
    }

    [Fact]
    public void AddInterest_PreventsDuplicates()
    {
        var profile = new AudienceProfileBuilder()
            .AddInterest("Programming")
            .AddInterest("Programming")
            .Build();

        Assert.Single(profile.Interests);
        Assert.Equal("Programming", profile.Interests[0]);
    }

    [Fact]
    public void SetCulturalBackground_WithParameters_SetsCorrectly()
    {
        var sensitivities = new System.Collections.Generic.List<string> { "Religious topics", "Political issues" };
        var taboos = new System.Collections.Generic.List<string> { "Alcohol", "Gambling" };

        var profile = new AudienceProfileBuilder()
            .SetCulturalBackground(sensitivities, taboos, CommunicationStyle.Formal)
            .Build();

        Assert.NotNull(profile.CulturalBackground);
        Assert.Equal(2, profile.CulturalBackground.Sensitivities.Count);
        Assert.Equal(2, profile.CulturalBackground.TabooTopics.Count);
        Assert.Equal(CommunicationStyle.Formal, profile.CulturalBackground.PreferredCommunicationStyle);
    }

    [Fact]
    public void SetAccessibilityNeeds_WithFlags_SetsCorrectly()
    {
        var profile = new AudienceProfileBuilder()
            .SetAccessibilityNeeds(
                requiresCaptions: true,
                requiresLargeText: true,
                requiresSimplifiedLanguage: true)
            .Build();

        Assert.NotNull(profile.AccessibilityNeeds);
        Assert.True(profile.AccessibilityNeeds.RequiresCaptions);
        Assert.True(profile.AccessibilityNeeds.RequiresLargeText);
        Assert.True(profile.AccessibilityNeeds.RequiresSimplifiedLanguage);
        Assert.False(profile.AccessibilityNeeds.RequiresAudioDescriptions);
        Assert.False(profile.AccessibilityNeeds.RequiresHighContrast);
    }

    [Fact]
    public void AsTemplate_MarksProfileAsTemplate()
    {
        var profile = new AudienceProfileBuilder()
            .AsTemplate()
            .Build();

        Assert.True(profile.IsTemplate);
    }

    [Fact]
    public void AddTags_AddsMultipleTags_PreventsDuplicates()
    {
        var profile = new AudienceProfileBuilder()
            .AddTags("tech", "professional", "advanced")
            .AddTags("tech", "expert")
            .Build();

        Assert.Equal(4, profile.Tags.Count);
        Assert.Contains("tech", profile.Tags);
        Assert.Contains("professional", profile.Tags);
        Assert.Contains("advanced", profile.Tags);
        Assert.Contains("expert", profile.Tags);
    }
}
