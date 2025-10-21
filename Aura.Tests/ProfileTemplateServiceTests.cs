using System.Linq;
using Aura.Core.Services.Profiles;
using Xunit;

namespace Aura.Tests;

public class ProfileTemplateServiceTests
{
    [Fact]
    public void GetAllTemplates_ShouldReturn8Templates()
    {
        // Act
        var templates = ProfileTemplateService.GetAllTemplates();

        // Assert
        Assert.Equal(8, templates.Count);
    }

    [Fact]
    public void GetTemplate_YouTubeGaming_ShouldReturnGamingTemplate()
    {
        // Act
        var template = ProfileTemplateService.GetTemplate("youtube-gaming");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("YouTube Gaming", template.Name);
        Assert.Equal("gaming", template.Category);
        Assert.Equal("gaming", template.DefaultPreferences.ContentType);
        Assert.Equal(90, template.DefaultPreferences.Tone?.Energy);
    }

    [Fact]
    public void GetTemplate_CorporateTraining_ShouldHaveHighFormality()
    {
        // Act
        var template = ProfileTemplateService.GetTemplate("corporate-training");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Corporate Training", template.Name);
        Assert.Equal(80, template.DefaultPreferences.Tone?.Formality);
        Assert.Equal("authoritative", template.DefaultPreferences.Audio?.VoiceStyle);
    }

    [Fact]
    public void GetTemplate_EducationalTutorial_ShouldHaveBalancedSettings()
    {
        // Act
        var template = ProfileTemplateService.GetTemplate("educational-tutorial");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Educational Tutorial", template.Name);
        Assert.Equal("tutorial", template.DefaultPreferences.ContentType);
        Assert.Equal(60, template.DefaultPreferences.Tone?.Formality);
        Assert.Equal("heavy", template.DefaultPreferences.Visual?.BRollUsage);
    }

    [Fact]
    public void GetTemplate_QuickTipsShorts_ShouldBeVerticalAndFast()
    {
        // Act
        var template = ProfileTemplateService.GetTemplate("quick-tips-shorts");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Quick Tips/Shorts", template.Name);
        Assert.Equal("9:16", template.DefaultPreferences.Platform?.AspectRatio);
        Assert.Equal(90, template.DefaultPreferences.Editing?.Pacing);
        Assert.Equal(30, template.DefaultPreferences.Platform?.TargetDurationSeconds);
        Assert.Equal("TikTok", template.DefaultPreferences.Platform?.PrimaryPlatform);
    }

    [Fact]
    public void GetTemplate_Documentary_ShouldHaveCinematicStyle()
    {
        // Act
        var template = ProfileTemplateService.GetTemplate("documentary");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Documentary", template.Name);
        Assert.Equal("cinematic", template.DefaultPreferences.Visual?.Aesthetic);
        Assert.Equal(35, template.DefaultPreferences.Editing?.Pacing);
        Assert.Equal(10, template.DefaultPreferences.Editing?.SceneDuration);
    }

    [Fact]
    public void GetTemplate_VlogPersonal_ShouldBeCasualAndAuthentic()
    {
        // Act
        var template = ProfileTemplateService.GetTemplate("vlog-personal");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Vlog/Personal", template.Name);
        Assert.Equal(15, template.DefaultPreferences.Tone?.Formality);
        Assert.Contains("authentic", template.DefaultPreferences.Tone?.PersonalityTraits ?? new System.Collections.Generic.List<string>());
    }

    [Fact]
    public void GetTemplate_MarketingPromotional_ShouldBePersuasive()
    {
        // Act
        var template = ProfileTemplateService.GetTemplate("marketing-promotional");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Marketing/Promotional", template.Name);
        Assert.Equal("promotional", template.DefaultPreferences.ContentType);
        Assert.Contains("persuasive", template.DefaultPreferences.Tone?.PersonalityTraits ?? new System.Collections.Generic.List<string>());
        Assert.Equal(75, template.DefaultPreferences.Tone?.Energy);
    }

    [Fact]
    public void GetTemplate_ProductReview_ShouldBeBalanced()
    {
        // Act
        var template = ProfileTemplateService.GetTemplate("product-review");

        // Assert
        Assert.NotNull(template);
        Assert.Equal("Product Review", template.Name);
        Assert.Equal("review", template.DefaultPreferences.ContentType);
        Assert.Contains("honest", template.DefaultPreferences.Tone?.PersonalityTraits ?? new System.Collections.Generic.List<string>());
        Assert.Equal("heavy", template.DefaultPreferences.Visual?.BRollUsage);
    }

    [Fact]
    public void GetTemplate_InvalidId_ShouldReturnNull()
    {
        // Act
        var template = ProfileTemplateService.GetTemplate("nonexistent-template");

        // Assert
        Assert.Null(template);
    }

    [Fact]
    public void GetTemplatesByCategory_Gaming_ShouldReturnGamingTemplates()
    {
        // Act
        var templates = ProfileTemplateService.GetTemplatesByCategory("gaming");

        // Assert
        Assert.Single(templates);
        Assert.Equal("youtube-gaming", templates[0].TemplateId);
    }

    [Fact]
    public void AllTemplates_ShouldHaveValidPreferences()
    {
        // Act
        var templates = ProfileTemplateService.GetAllTemplates();

        // Assert
        foreach (var template in templates)
        {
            Assert.NotNull(template.TemplateId);
            Assert.NotNull(template.Name);
            Assert.NotNull(template.Description);
            Assert.NotNull(template.Category);
            Assert.NotNull(template.DefaultPreferences);
            Assert.NotNull(template.DefaultPreferences.Tone);
            Assert.NotNull(template.DefaultPreferences.Visual);
            Assert.NotNull(template.DefaultPreferences.Audio);
            Assert.NotNull(template.DefaultPreferences.Editing);
            Assert.NotNull(template.DefaultPreferences.Platform);
            Assert.NotNull(template.DefaultPreferences.AIBehavior);
        }
    }

    [Fact]
    public void AllTemplates_ShouldHaveUniqueIds()
    {
        // Act
        var templates = ProfileTemplateService.GetAllTemplates();
        var ids = templates.Select(t => t.TemplateId).ToList();

        // Assert
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }
}
