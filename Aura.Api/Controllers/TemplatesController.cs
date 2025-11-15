using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for video templates, sample projects, and tutorial guides
/// </summary>
[ApiController]
[Route("api/templates")]
public class TemplatesController : ControllerBase
{
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(ILogger<TemplatesController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get all available video templates
    /// </summary>
    [HttpGet]
    public IActionResult GetTemplates([FromQuery] string? category = null, [FromQuery] string? difficulty = null)
    {
        try
        {
            var templates = GetAllTemplates();

            if (!string.IsNullOrEmpty(category))
            {
                templates = templates.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(difficulty))
            {
                templates = templates.Where(t => t.Difficulty.Equals(difficulty, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(templates);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting templates");
            return StatusCode(500, new { error = "Failed to get templates" });
        }
    }

    /// <summary>
    /// Get a specific template by ID
    /// </summary>
    [HttpGet("{id}")]
    public IActionResult GetTemplate(string id)
    {
        try
        {
            var template = GetAllTemplates().FirstOrDefault(t => t.Id == id);

            if (template == null)
            {
                return NotFound(new { error = "Template not found" });
            }

            return Ok(template);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template {TemplateId}", id);
            return StatusCode(500, new { error = "Failed to get template" });
        }
    }

    /// <summary>
    /// Get sample projects
    /// </summary>
    [HttpGet("samples")]
    public IActionResult GetSampleProjects()
    {
        try
        {
            var samples = GetAllSampleProjects();
            return Ok(samples);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sample projects");
            return StatusCode(500, new { error = "Failed to get sample projects" });
        }
    }

    /// <summary>
    /// Get example prompts
    /// </summary>
    [HttpGet("prompts")]
    public IActionResult GetExamplePrompts([FromQuery] string? category = null)
    {
        try
        {
            var prompts = GetAllExamplePrompts();

            if (!string.IsNullOrEmpty(category))
            {
                prompts = prompts.Where(p => p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(prompts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting example prompts");
            return StatusCode(500, new { error = "Failed to get example prompts" });
        }
    }

    /// <summary>
    /// Get tutorial guides
    /// </summary>
    [HttpGet("tutorials")]
    public IActionResult GetTutorialGuides()
    {
        try
        {
            var tutorials = GetAllTutorialGuides();
            return Ok(tutorials);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tutorial guides");
            return StatusCode(500, new { error = "Failed to get tutorial guides" });
        }
    }

    /// <summary>
    /// Create a new project from a template
    /// </summary>
    [HttpPost("create-project")]
    public Task<IActionResult> CreateProjectFromTemplate(
        [FromBody] CreateProjectFromTemplateRequest request,
        CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(request.TemplateId))
            {
                return Task.FromResult<IActionResult>(BadRequest(new { error = "TemplateId is required" }));
            }

            var template = GetAllTemplates().FirstOrDefault(t => t.Id == request.TemplateId);
            if (template == null)
            {
                return Task.FromResult<IActionResult>(NotFound(new { error = "Template not found" }));
            }

            // Generate a project ID
            var projectId = Guid.NewGuid().ToString();
            var projectName = request.ProjectName ?? $"{template.Name} - {DateTime.Now:yyyy-MM-dd HH:mm}";

            _logger.LogInformation("Creating project from template {TemplateId}: {ProjectName}", request.TemplateId, projectName);

            // In a real implementation, this would create the project in the database
            // For now, just return success with the generated ID
            return Task.FromResult<IActionResult>(Ok(new
            {
                success = true,
                projectId,
                projectName,
                templateId = request.TemplateId,
                message = "Project created successfully from template"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project from template");
            return Task.FromResult<IActionResult>(StatusCode(500, new { error = "Failed to create project from template" }));
        }
    }

    // Private helper methods to return template data

    private List<VideoTemplate> GetAllTemplates()
    {
        return new List<VideoTemplate>
        {
            new VideoTemplate
            {
                Id = "youtube-tutorial",
                Name = "YouTube Tutorial",
                Description = "Create educational tutorial videos for YouTube with clear structure and engaging visuals",
                Category = "tutorial",
                Duration = 180,
                Difficulty = "beginner",
                PromptExample = "Create a tutorial video about how to make homemade pasta, including ingredients, step-by-step instructions, and cooking tips",
                Tags = new[] { "youtube", "education", "tutorial", "how-to" },
                EstimatedTime = "2-3 minutes"
            },
            new VideoTemplate
            {
                Id = "social-shorts",
                Name = "Social Media Shorts",
                Description = "Quick, engaging short-form content perfect for TikTok, Instagram Reels, and YouTube Shorts",
                Category = "social-media",
                Duration = 60,
                Difficulty = "beginner",
                PromptExample = "Create a 60-second video about 5 productivity hacks for remote workers",
                Tags = new[] { "social", "shorts", "tiktok", "reels", "quick" },
                EstimatedTime = "1-2 minutes"
            },
            new VideoTemplate
            {
                Id = "product-demo",
                Name = "Product Demonstration",
                Description = "Showcase product features and benefits with professional marketing style",
                Category = "marketing",
                Duration = 120,
                Difficulty = "intermediate",
                PromptExample = "Create a product demo video for a new smart home device, highlighting its key features and ease of use",
                Tags = new[] { "marketing", "product", "demo", "commercial" },
                EstimatedTime = "3-4 minutes"
            },
            new VideoTemplate
            {
                Id = "educational-explainer",
                Name = "Educational Explainer",
                Description = "Explain complex topics in simple, visual terms",
                Category = "educational",
                Duration = 240,
                Difficulty = "intermediate",
                PromptExample = "Explain how photosynthesis works in plants, using simple analogies and visual examples",
                Tags = new[] { "education", "explainer", "science", "learning" },
                EstimatedTime = "3-5 minutes"
            }
        };
    }

    private List<SampleProject> GetAllSampleProjects()
    {
        var templates = GetAllTemplates();
        return new List<SampleProject>
        {
            new SampleProject
            {
                Id = "sample-coffee-tutorial",
                Name = "How to Make Perfect Coffee",
                Description = "A beginner-friendly tutorial demonstrating the sample project workflow",
                TemplateId = "youtube-tutorial",
                Prompt = "Create a 3-minute tutorial video about how to make the perfect cup of coffee at home. Include equipment needed, step-by-step brewing instructions, and tips for choosing beans.",
                ExpectedOutput = "3-minute educational video with introduction, main content sections, and conclusion",
                LearningPoints = new[]
                {
                    "How to write effective prompts",
                    "Understanding video structure",
                    "Working with tutorial format",
                    "Basic editing and pacing"
                }
            },
            new SampleProject
            {
                Id = "sample-productivity-short",
                Name = "5 Morning Routine Hacks",
                Description = "Quick social media content example",
                TemplateId = "social-shorts",
                Prompt = "Create a 60-second video about 5 morning routine hacks to start your day productively",
                ExpectedOutput = "Fast-paced 1-minute video with quick transitions between tips",
                LearningPoints = new[]
                {
                    "Creating engaging short-form content",
                    "Pacing for social media",
                    "Hook and retention strategies",
                    "Quick tips format"
                }
            }
        };
    }

    private List<ExamplePrompt> GetAllExamplePrompts()
    {
        return new List<ExamplePrompt>
        {
            new ExamplePrompt
            {
                Id = "prompt-cooking-basic",
                Title = "Basic Cooking Tutorial",
                Prompt = "Create a video showing how to make scrambled eggs, including ingredient prep, cooking technique, and plating tips",
                Category = "tutorial",
                Tags = new[] { "cooking", "food", "beginner", "how-to" },
                ExpectedDuration = 120,
                Tips = new[]
                {
                    "Be specific about ingredients and measurements",
                    "Break down steps clearly",
                    "Include safety tips where relevant"
                }
            },
            new ExamplePrompt
            {
                Id = "prompt-tech-review",
                Title = "Tech Product Review",
                Prompt = "Review the latest smartphone, covering design, performance, camera quality, battery life, and value for money",
                Category = "educational",
                Tags = new[] { "tech", "review", "gadgets", "analysis" },
                ExpectedDuration = 240,
                Tips = new[]
                {
                    "Structure reviews with clear sections",
                    "Be objective and balanced",
                    "Include pros and cons",
                    "Mention target audience"
                }
            }
        };
    }

    private List<TutorialGuide> GetAllTutorialGuides()
    {
        return new List<TutorialGuide>
        {
            new TutorialGuide
            {
                Id = "getting-started",
                Title = "Getting Started with Aura",
                Description = "Learn the basics of creating your first video with Aura Video Studio",
                EstimatedTime = "10 minutes",
                Difficulty = "beginner",
                Steps = new[]
                {
                    new TutorialStep
                    {
                        Title = "Write Your Prompt",
                        Description = "Start by describing the video you want to create. Be specific about the topic, style, and duration.",
                        Duration = "2 min"
                    },
                    new TutorialStep
                    {
                        Title = "Choose Your Settings",
                        Description = "Select video resolution, aspect ratio, and any advanced options you need.",
                        Duration = "1 min"
                    },
                    new TutorialStep
                    {
                        Title = "Generate Script",
                        Description = "Aura will generate a script based on your prompt. Review and edit as needed.",
                        Duration = "3 min"
                    },
                    new TutorialStep
                    {
                        Title = "Review & Render",
                        Description = "Preview the timeline, make adjustments, then render your final video.",
                        Duration = "4 min"
                    }
                }
            }
        };
    }

    // DTOs

    public class VideoTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public string PromptExample { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string EstimatedTime { get; set; } = string.Empty;
    }

    public class SampleProject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string ExpectedOutput { get; set; } = string.Empty;
        public string[] LearningPoints { get; set; } = Array.Empty<string>();
    }

    public class ExamplePrompt
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public int ExpectedDuration { get; set; }
        public string[] Tips { get; set; } = Array.Empty<string>();
    }

    public class TutorialGuide
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EstimatedTime { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public TutorialStep[] Steps { get; set; } = Array.Empty<TutorialStep>();
    }

    public class TutorialStep
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Duration { get; set; }
    }

    public class CreateProjectFromTemplateRequest
    {
        public string TemplateId { get; set; } = string.Empty;
        public string? CustomPrompt { get; set; }
        public string? ProjectName { get; set; }
    }
}
