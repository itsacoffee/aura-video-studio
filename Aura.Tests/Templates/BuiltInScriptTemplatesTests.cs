using Xunit;
using Aura.Core.Models.Templates;
using Aura.Core.Services.Templates;
using System;
using System.Linq;

namespace Aura.Tests.Templates;

[Trait("Category", "Unit")]
[Trait("Feature", "VideoTemplates")]
public class BuiltInScriptTemplatesTests
{
    private readonly IReadOnlyList<VideoTemplate> _templates;

    public BuiltInScriptTemplatesTests()
    {
        _templates = BuiltInScriptTemplates.GetAll();
    }

    [Fact]
    public void GetAll_Returns6Templates()
    {
        Assert.Equal(6, _templates.Count);
    }

    [Theory]
    [InlineData("explainer", "Explainer")]
    [InlineData("listicle", "Listicle (Top N List)")]
    [InlineData("comparison", "Comparison")]
    [InlineData("story-time", "Story Time")]
    [InlineData("tutorial", "Tutorial")]
    [InlineData("product-showcase", "Product Showcase")]
    public void AllTemplates_HaveCorrectIdAndName(string expectedId, string expectedName)
    {
        var template = _templates.FirstOrDefault(t => t.Id == expectedId);
        Assert.NotNull(template);
        Assert.Equal(expectedName, template.Name);
    }

    [Fact]
    public void AllTemplates_HaveRequiredFields()
    {
        foreach (var template in _templates)
        {
            // Basic fields
            Assert.NotEmpty(template.Id);
            Assert.NotEmpty(template.Name);
            Assert.NotEmpty(template.Description);
            Assert.NotEmpty(template.Category);

            // Structure
            Assert.NotNull(template.Structure);
            Assert.NotEmpty(template.Structure.Sections);
            Assert.True(template.Structure.EstimatedDuration.TotalSeconds > 0,
                $"Template {template.Id} should have positive estimated duration");
            Assert.True(template.Structure.RecommendedSceneCount > 0,
                $"Template {template.Id} should have positive recommended scene count");

            // Variables
            Assert.NotNull(template.Variables);

            // Metadata
            Assert.NotNull(template.Metadata);
            Assert.NotEmpty(template.Metadata.RecommendedAudiences);
            Assert.NotEmpty(template.Metadata.RecommendedTones);
            Assert.NotEmpty(template.Metadata.SupportedAspects);
            Assert.NotEmpty(template.Metadata.Tags);
        }
    }

    [Fact]
    public void AllSections_HaveRequiredFields()
    {
        foreach (var template in _templates)
        {
            foreach (var section in template.Structure.Sections)
            {
                Assert.NotEmpty(section.Name);
                Assert.NotEmpty(section.Purpose);
                Assert.NotEmpty(section.PromptTemplate);
                Assert.True(section.SuggestedDuration.TotalSeconds > 0,
                    $"Section {section.Name} in template {template.Id} should have positive duration");
            }
        }
    }

    [Fact]
    public void AllVariables_HaveRequiredFields()
    {
        foreach (var template in _templates)
        {
            foreach (var variable in template.Variables)
            {
                Assert.NotEmpty(variable.Name);
                Assert.NotEmpty(variable.DisplayName);
            }
        }
    }

    [Fact]
    public void AllTemplates_HaveAtLeastOneRequiredVariable()
    {
        foreach (var template in _templates)
        {
            var hasRequired = template.Variables.Any(v => v.IsRequired);
            Assert.True(hasRequired,
                $"Template {template.Id} should have at least one required variable");
        }
    }

    [Fact]
    public void ExplainerTemplate_HasCorrectStructure()
    {
        var template = _templates.First(t => t.Id == "explainer");

        // Check sections
        var sectionNames = template.Structure.Sections.Select(s => s.Name).ToList();
        Assert.Contains("Hook", sectionNames);
        Assert.Contains("Problem", sectionNames);
        Assert.Contains("Solution", sectionNames);
        Assert.Contains("Benefits", sectionNames);
        Assert.Contains("Call to Action", sectionNames);

        // Check variables
        var variableNames = template.Variables.Select(v => v.Name).ToList();
        Assert.Contains("topic", variableNames);
        Assert.Contains("audience", variableNames);
    }

    [Fact]
    public void ListicleTemplate_HasRepeatableSections()
    {
        var template = _templates.First(t => t.Id == "listicle");

        var repeatableSection = template.Structure.Sections.FirstOrDefault(s => s.IsRepeatable);
        Assert.NotNull(repeatableSection);
        Assert.Equal("Item", repeatableSection.Name);
        Assert.Equal("count", repeatableSection.RepeatCountVariable);

        // Check count variable has min/max
        var countVariable = template.Variables.First(v => v.Name == "count");
        Assert.Equal(VariableType.Number, countVariable.Type);
        Assert.NotNull(countVariable.MinValue);
        Assert.NotNull(countVariable.MaxValue);
    }

    [Fact]
    public void ComparisonTemplate_HasOptionVariables()
    {
        var template = _templates.First(t => t.Id == "comparison");

        var variableNames = template.Variables.Select(v => v.Name).ToList();
        Assert.Contains("optionA", variableNames);
        Assert.Contains("optionB", variableNames);

        var optionA = template.Variables.First(v => v.Name == "optionA");
        var optionB = template.Variables.First(v => v.Name == "optionB");
        Assert.True(optionA.IsRequired);
        Assert.True(optionB.IsRequired);
    }

    [Fact]
    public void TutorialTemplate_HasStepSections()
    {
        var template = _templates.First(t => t.Id == "tutorial");

        var stepSection = template.Structure.Sections.FirstOrDefault(s => s.IsRepeatable && s.Name == "Step");
        Assert.NotNull(stepSection);
        Assert.Equal("stepCount", stepSection.RepeatCountVariable);

        var stepCountVariable = template.Variables.First(v => v.Name == "stepCount");
        Assert.Equal(VariableType.Number, stepCountVariable.Type);
    }

    [Fact]
    public void ProductShowcaseTemplate_FollowsAIDAStructure()
    {
        var template = _templates.First(t => t.Id == "product-showcase");

        var sectionNames = template.Structure.Sections.Select(s => s.Name).ToList();
        Assert.Contains("Attention", sectionNames);
        Assert.Contains("Interest", sectionNames);
        Assert.Contains("Desire", sectionNames);
        Assert.Contains("Action", sectionNames);

        // Verify correct order (AIDA)
        Assert.True(
            sectionNames.IndexOf("Attention") <
            sectionNames.IndexOf("Interest") &&
            sectionNames.IndexOf("Interest") <
            sectionNames.IndexOf("Desire") &&
            sectionNames.IndexOf("Desire") <
            sectionNames.IndexOf("Action"),
            "AIDA sections should be in correct order");
    }

    [Fact]
    public void StoryTimeTemplate_HasNarrativeStructure()
    {
        var template = _templates.First(t => t.Id == "story-time");

        var sectionNames = template.Structure.Sections.Select(s => s.Name).ToList();
        Assert.Contains("Hook", sectionNames);
        Assert.Contains("Setup", sectionNames);
        Assert.Contains("Rising Action", sectionNames);
        Assert.Contains("Climax", sectionNames);
        Assert.Contains("Resolution", sectionNames);
        Assert.Contains("Lesson", sectionNames);
    }

    [Fact]
    public void AllTemplates_HaveThumbnails()
    {
        foreach (var template in _templates)
        {
            Assert.NotNull(template.Thumbnail);
            Assert.NotEmpty(template.Thumbnail.IconName);
            Assert.NotEmpty(template.Thumbnail.AccentColor);
            Assert.StartsWith("#", template.Thumbnail.AccentColor);
        }
    }

    [Fact]
    public void AllTemplates_HaveValidCategories()
    {
        var validCategories = new[] { "Educational", "Entertainment", "Reviews", "Marketing" };

        foreach (var template in _templates)
        {
            Assert.Contains(template.Category, validCategories);
        }
    }

    [Fact]
    public void AllTemplates_HaveReasonableDurations()
    {
        foreach (var template in _templates)
        {
            // Min duration should be less than or equal to max duration
            Assert.True(
                template.Metadata.MinDuration <= template.Metadata.MaxDuration,
                $"Template {template.Id}: MinDuration should be <= MaxDuration");

            // Estimated duration should be within the min/max range
            Assert.True(
                template.Structure.EstimatedDuration >= template.Metadata.MinDuration &&
                template.Structure.EstimatedDuration <= template.Metadata.MaxDuration,
                $"Template {template.Id}: EstimatedDuration should be within min/max range");
        }
    }
}
