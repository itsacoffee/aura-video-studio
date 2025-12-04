using Xunit;
using Aura.Core.Models.Templates;
using Aura.Core.Services.Templates;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aura.Tests.Templates;

[Trait("Category", "Unit")]
[Trait("Feature", "VideoTemplates")]
public class VideoTemplateServiceTests
{
    private readonly VideoTemplateService _service;
    private readonly Mock<ILogger<VideoTemplateService>> _loggerMock;

    public VideoTemplateServiceTests()
    {
        _loggerMock = new Mock<ILogger<VideoTemplateService>>();
        _service = new VideoTemplateService(_loggerMock.Object);
    }

    [Fact]
    public void GetAllTemplates_ReturnsAllBuiltInTemplates()
    {
        // Act
        var templates = _service.GetAllTemplates();

        // Assert
        Assert.NotNull(templates);
        Assert.Equal(6, templates.Count);
    }

    [Theory]
    [InlineData("explainer")]
    [InlineData("listicle")]
    [InlineData("comparison")]
    [InlineData("story-time")]
    [InlineData("tutorial")]
    [InlineData("product-showcase")]
    public void GetTemplateById_ReturnsCorrectTemplate(string templateId)
    {
        // Act
        var template = _service.GetTemplateById(templateId);

        // Assert
        Assert.NotNull(template);
        Assert.Equal(templateId, template.Id);
    }

    [Fact]
    public void GetTemplateById_WithInvalidId_ReturnsNull()
    {
        // Act
        var template = _service.GetTemplateById("non-existent");

        // Assert
        Assert.Null(template);
    }

    [Fact]
    public void GetTemplatesByCategory_ReturnsCorrectTemplates()
    {
        // Act
        var educationalTemplates = _service.GetTemplatesByCategory("Educational");

        // Assert
        Assert.NotNull(educationalTemplates);
        Assert.All(educationalTemplates, t => Assert.Equal("Educational", t.Category));
    }

    [Theory]
    [InlineData("explainer")]
    [InlineData("tutorial")]
    [InlineData("listicle")]
    public void SearchTemplates_FindsTemplatesByName(string query)
    {
        // Act
        var results = _service.SearchTemplates(query);

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        Assert.Contains(results, t => t.Name.ToLowerInvariant().Contains(query.ToLowerInvariant()));
    }

    [Fact]
    public void SearchTemplates_FindsTemplatesByTag()
    {
        // Act
        var results = _service.SearchTemplates("educational");

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
    }

    [Fact]
    public void SearchTemplates_WithEmptyQuery_ReturnsAllTemplates()
    {
        // Act
        var results = _service.SearchTemplates("");

        // Assert
        Assert.Equal(_service.GetAllTemplates().Count, results.Count);
    }

    [Fact]
    public void ValidateVariables_WithRequiredVariableMissing_ReturnsError()
    {
        // Arrange
        var variables = new Dictionary<string, string>();

        // Act
        var (isValid, errors) = _service.ValidateVariables("explainer", variables);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Topic"));
    }

    [Fact]
    public void ValidateVariables_WithRequiredVariablePresent_ReturnsValid()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            { "topic", "Machine Learning Basics" }
        };

        // Act
        var (isValid, errors) = _service.ValidateVariables("explainer", variables);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateVariables_WithNumberOutOfRange_ReturnsError()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            { "topic", "Top Tips" },
            { "count", "100" } // Max is 15
        };

        // Act
        var (isValid, errors) = _service.ValidateVariables("listicle", variables);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("at most"));
    }

    [Fact]
    public void ValidateVariables_WithInvalidNumber_ReturnsError()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            { "topic", "Top Tips" },
            { "count", "not-a-number" }
        };

        // Act
        var (isValid, errors) = _service.ValidateVariables("listicle", variables);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("number"));
    }

    [Fact]
    public async Task ApplyTemplateAsync_WithValidVariables_ReturnsTemplatedBrief()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            { "topic", "Introduction to Python Programming" },
            { "audience", "beginners" }
        };

        // Act
        var result = await _service.ApplyTemplateAsync("explainer", variables);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Brief);
        Assert.NotNull(result.PlanSpec);
        Assert.NotEmpty(result.Sections);
        Assert.Equal("explainer", result.SourceTemplate.Id);
        Assert.Contains("Python", result.Brief.Topic);
    }

    [Fact]
    public async Task ApplyTemplateAsync_WithInvalidTemplateId_ThrowsException()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            { "topic", "Test Topic" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<System.ArgumentException>(
            () => _service.ApplyTemplateAsync("non-existent", variables));
    }

    [Fact]
    public async Task ApplyTemplateAsync_WithMissingRequiredVariable_ThrowsException()
    {
        // Arrange
        var variables = new Dictionary<string, string>();

        // Act & Assert
        await Assert.ThrowsAsync<System.ArgumentException>(
            () => _service.ApplyTemplateAsync("explainer", variables));
    }

    [Fact]
    public async Task PreviewScriptAsync_ReturnsScriptWithSections()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            { "topic", "How to Bake a Cake" },
            { "stepCount", "5" },
            { "audience", "beginners" }
        };

        // Act
        var result = await _service.PreviewScriptAsync("tutorial", variables);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Script);
        Assert.NotEmpty(result.Sections);
        Assert.True(result.EstimatedDuration.TotalSeconds > 0);
        Assert.True(result.SceneCount > 0);
    }

    [Fact]
    public async Task ApplyTemplateAsync_WithListicle_ExpandsRepeatableSections()
    {
        // Arrange
        var variables = new Dictionary<string, string>
        {
            { "topic", "Productivity Tips" },
            { "count", "5" },
            { "audience", "professionals" }
        };

        // Act
        var result = await _service.ApplyTemplateAsync("listicle", variables);

        // Assert
        Assert.NotNull(result);
        // Should have 5 numbered items + hook + recap + CTA = 8 sections
        Assert.True(result.Sections.Count >= 7);
        
        // Check that item sections are present
        var itemSections = result.Sections.Where(s => s.Name.Contains("Item")).ToList();
        Assert.Equal(5, itemSections.Count);
    }
}
