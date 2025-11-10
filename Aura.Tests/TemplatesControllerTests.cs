using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Aura.Tests;

/// <summary>
/// Integration tests for TemplatesController (PR #163 version)
/// </summary>
public class TemplatesControllerTests
{
    private readonly TemplatesController _controller;

    public TemplatesControllerTests()
    {
        var logger = new LoggerFactory().CreateLogger<TemplatesController>();
        _controller = new TemplatesController(logger);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public void GetTemplates_ReturnsOkResult()
    {
        var result = _controller.GetTemplates();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public void GetTemplates_ReturnsListOfTemplates()
    {
        var result = _controller.GetTemplates();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var templates = okResult.Value as System.Collections.IEnumerable;
        Assert.NotNull(templates);
        
        var templatesList = templates.Cast<object>().ToList();
        Assert.NotEmpty(templatesList);
    }

    [Fact]
    public void GetTemplates_FiltersByCategory()
    {
        var result = _controller.GetTemplates(category: "tutorial");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var templates = okResult.Value as System.Collections.IEnumerable;
        Assert.NotNull(templates);
        
        var templatesList = templates.Cast<object>().ToList();
        Assert.All(templatesList, template =>
        {
            var categoryProperty = template.GetType().GetProperty("Category");
            Assert.NotNull(categoryProperty);
            var category = (string)categoryProperty.GetValue(template);
            Assert.Equal("tutorial", category, ignoreCase: true);
        });
    }

    [Fact]
    public void GetTemplates_FiltersByDifficulty()
    {
        var result = _controller.GetTemplates(difficulty: "beginner");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var templates = okResult.Value as System.Collections.IEnumerable;
        Assert.NotNull(templates);
        
        var templatesList = templates.Cast<object>().ToList();
        Assert.All(templatesList, template =>
        {
            var difficultyProperty = template.GetType().GetProperty("Difficulty");
            Assert.NotNull(difficultyProperty);
            var difficulty = (string)difficultyProperty.GetValue(template);
            Assert.Equal("beginner", difficulty, ignoreCase: true);
        });
    }

    [Fact]
    public void GetTemplate_WithValidId_ReturnsOkResult()
    {
        var result = _controller.GetTemplate("youtube-tutorial");

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var template = okResult.Value;
        var idProperty = template.GetType().GetProperty("Id");
        Assert.NotNull(idProperty);
        
        var id = (string)idProperty.GetValue(template);
        Assert.Equal("youtube-tutorial", id);
    }

    [Fact]
    public void GetTemplate_WithInvalidId_ReturnsNotFound()
    {
        var result = _controller.GetTemplate("nonexistent-template");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void GetSampleProjects_ReturnsOkResult()
    {
        var result = _controller.GetSampleProjects();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var samples = okResult.Value as System.Collections.IEnumerable;
        Assert.NotNull(samples);
        Assert.NotEmpty(samples.Cast<object>());
    }

    [Fact]
    public void GetSampleProjects_ContainsLearningPoints()
    {
        var result = _controller.GetSampleProjects();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var samples = okResult.Value as System.Collections.IEnumerable;
        var samplesList = samples.Cast<object>().ToList();
        
        Assert.All(samplesList, sample =>
        {
            var learningPointsProperty = sample.GetType().GetProperty("LearningPoints");
            Assert.NotNull(learningPointsProperty);
        });
    }

    [Fact]
    public void GetExamplePrompts_ReturnsOkResult()
    {
        var result = _controller.GetExamplePrompts();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var prompts = okResult.Value as System.Collections.IEnumerable;
        Assert.NotNull(prompts);
        Assert.NotEmpty(prompts.Cast<object>());
    }

    [Fact]
    public void GetExamplePrompts_FiltersByCategory()
    {
        var result = _controller.GetExamplePrompts(category: "tutorial");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var prompts = okResult.Value as System.Collections.IEnumerable;
        var promptsList = prompts.Cast<object>().ToList();
        
        Assert.All(promptsList, prompt =>
        {
            var categoryProperty = prompt.GetType().GetProperty("Category");
            Assert.NotNull(categoryProperty);
            var category = (string)categoryProperty.GetValue(prompt);
            Assert.Equal("tutorial", category, ignoreCase: true);
        });
    }

    [Fact]
    public void GetExamplePrompts_ContainsTips()
    {
        var result = _controller.GetExamplePrompts();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var prompts = okResult.Value as System.Collections.IEnumerable;
        var promptsList = prompts.Cast<object>().ToList();
        
        Assert.All(promptsList, prompt =>
        {
            var tipsProperty = prompt.GetType().GetProperty("Tips");
            Assert.NotNull(tipsProperty);
        });
    }

    [Fact]
    public void GetTutorialGuides_ReturnsOkResult()
    {
        var result = _controller.GetTutorialGuides();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var guides = okResult.Value as System.Collections.IEnumerable;
        Assert.NotNull(guides);
        Assert.NotEmpty(guides.Cast<object>());
    }

    [Fact]
    public void GetTutorialGuides_ContainsSteps()
    {
        var result = _controller.GetTutorialGuides();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var guides = okResult.Value as System.Collections.IEnumerable;
        var guidesList = guides.Cast<object>().ToList();
        
        Assert.All(guidesList, guide =>
        {
            var stepsProperty = guide.GetType().GetProperty("Steps");
            Assert.NotNull(stepsProperty);
            
            var steps = stepsProperty.GetValue(guide) as System.Collections.IEnumerable;
            Assert.NotNull(steps);
            Assert.NotEmpty(steps.Cast<object>());
        });
    }

    [Fact]
    public async Task CreateProjectFromTemplate_WithValidRequest_ReturnsOkResult()
    {
        var request = new TemplatesController.CreateProjectFromTemplateRequest
        {
            TemplateId = "youtube-tutorial",
            ProjectName = "My Test Project"
        };

        var result = await _controller.CreateProjectFromTemplate(request, default);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        
        var response = okResult.Value;
        var successProperty = response.GetType().GetProperty("success");
        Assert.NotNull(successProperty);
        
        var success = (bool)successProperty.GetValue(response);
        Assert.True(success);
    }

    [Fact]
    public async Task CreateProjectFromTemplate_WithEmptyTemplateId_ReturnsBadRequest()
    {
        var request = new TemplatesController.CreateProjectFromTemplateRequest
        {
            TemplateId = "",
            ProjectName = "My Test Project"
        };

        var result = await _controller.CreateProjectFromTemplate(request, default);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task CreateProjectFromTemplate_WithNonExistentTemplate_ReturnsNotFound()
    {
        var request = new TemplatesController.CreateProjectFromTemplateRequest
        {
            TemplateId = "nonexistent-template",
            ProjectName = "My Test Project"
        };

        var result = await _controller.CreateProjectFromTemplate(request, default);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task CreateProjectFromTemplate_GeneratesProjectId()
    {
        var request = new TemplatesController.CreateProjectFromTemplateRequest
        {
            TemplateId = "youtube-tutorial",
            ProjectName = "My Test Project"
        };

        var result = await _controller.CreateProjectFromTemplate(request, default);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        
        var projectIdProperty = response.GetType().GetProperty("projectId");
        Assert.NotNull(projectIdProperty);
        
        var projectId = (string)projectIdProperty.GetValue(response);
        Assert.NotEmpty(projectId);
    }

    [Fact]
    public void Templates_HaveRequiredProperties()
    {
        var result = _controller.GetTemplates();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var templates = okResult.Value as System.Collections.IEnumerable;
        var templatesList = templates.Cast<object>().ToList();
        
        Assert.All(templatesList, template =>
        {
            Assert.NotNull(template.GetType().GetProperty("Id"));
            Assert.NotNull(template.GetType().GetProperty("Name"));
            Assert.NotNull(template.GetType().GetProperty("Description"));
            Assert.NotNull(template.GetType().GetProperty("Category"));
            Assert.NotNull(template.GetType().GetProperty("Duration"));
            Assert.NotNull(template.GetType().GetProperty("Difficulty"));
            Assert.NotNull(template.GetType().GetProperty("PromptExample"));
            Assert.NotNull(template.GetType().GetProperty("Tags"));
        });
    }
}
