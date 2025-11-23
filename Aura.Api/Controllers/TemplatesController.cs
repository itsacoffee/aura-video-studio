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
    /// Get all available video templates with pagination support
    /// </summary>
    [HttpGet]
    public IActionResult GetTemplates(
        [FromQuery] string? category = null,
        [FromQuery] string? subCategory = null,
        [FromQuery] bool? systemOnly = null,
        [FromQuery] bool? communityOnly = null,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null)
    {
        try
        {
            var templates = GetAllTemplates();

            // Filter by category (YouTube, SocialMedia, Business, Creative)
            if (!string.IsNullOrEmpty(category))
            {
                templates = templates.Where(t => t.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Filter by subCategory
            if (!string.IsNullOrEmpty(subCategory))
            {
                templates = templates.Where(t => t.SubCategory?.Equals(subCategory, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            // Filter by system/community templates
            if (systemOnly == true)
            {
                templates = templates.Where(t => t.IsSystemTemplate).ToList();
            }
            if (communityOnly == true)
            {
                templates = templates.Where(t => t.IsCommunityTemplate).ToList();
            }

            // Pagination with validation
            var totalCount = templates.Count;
            var pageNum = page ?? 1;
            var size = pageSize ?? 20;

            // Validate pagination parameters to prevent division by zero and invalid requests
            if (pageNum < 1)
            {
                return BadRequest(new { error = "Page number must be greater than 0" });
            }

            if (size < 1)
            {
                return BadRequest(new { error = "Page size must be greater than 0" });
            }

            // Enforce maximum page size to prevent performance issues
            const int maxPageSize = 100;
            if (size > maxPageSize)
            {
                size = maxPageSize;
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)size);

            var paginatedTemplates = templates
                .Skip((pageNum - 1) * size)
                .Take(size)
                .Select(t => new
                {
                    id = t.Id,
                    name = t.Name,
                    description = t.Description,
                    category = t.Category,
                    subCategory = t.SubCategory ?? string.Empty,
                    previewImage = t.PreviewImage,
                    previewVideo = t.PreviewVideo,
                    tags = t.Tags,
                    usageCount = t.UsageCount,
                    rating = t.Rating,
                    isSystemTemplate = t.IsSystemTemplate,
                    isCommunityTemplate = t.IsCommunityTemplate
                })
                .ToList();

            return Ok(new
            {
                items = paginatedTemplates,
                page = pageNum,
                pageSize = size,
                totalCount = totalCount,
                totalPages = totalPages,
                hasNextPage = pageNum < totalPages,
                hasPreviousPage = pageNum > 1
            });
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

            // Return in the format expected by frontend
            return Ok(new
            {
                id = template.Id,
                name = template.Name,
                description = template.Description,
                category = template.Category,
                subCategory = template.SubCategory ?? string.Empty,
                previewImage = template.PreviewImage,
                previewVideo = template.PreviewVideo,
                tags = template.Tags,
                templateData = "{}", // Placeholder - would contain actual template structure
                createdAt = template.CreatedAt,
                updatedAt = template.UpdatedAt,
                author = template.Author,
                isSystemTemplate = template.IsSystemTemplate,
                isCommunityTemplate = template.IsCommunityTemplate,
                usageCount = template.UsageCount,
                rating = template.Rating,
                ratingCount = template.RatingCount
            });
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
            // YouTube Category Templates
            new VideoTemplate
            {
                Id = "youtube-tutorial-advanced",
                Name = "Advanced YouTube Tutorial",
                Description = "Professional tutorial format with intro, main content, and outro. Perfect for educational channels with clear structure and engaging visuals.",
                Category = "YouTube",
                SubCategory = "Tutorial",
                Duration = 300,
                Difficulty = "intermediate",
                PromptExample = "Create a comprehensive tutorial video about advanced photography techniques, including camera settings, composition rules, and post-processing tips",
                Tags = new[] { "youtube", "tutorial", "education", "how-to", "professional" },
                EstimatedTime = "5-7 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/youtube-tutorial.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.8,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "youtube-product-review",
                Name = "Product Review",
                Description = "Structured product review format with unboxing, features overview, pros/cons, and final verdict. Ideal for tech and lifestyle channels.",
                Category = "YouTube",
                SubCategory = "Review",
                Duration = 480,
                Difficulty = "intermediate",
                PromptExample = "Create a detailed product review video for a new smartphone, covering design, performance, camera quality, battery life, and value proposition",
                Tags = new[] { "youtube", "review", "product", "tech", "unboxing" },
                EstimatedTime = "8-10 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/youtube-review.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.6,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "youtube-vlog-style",
                Name = "Vlog Style",
                Description = "Casual, personal vlog format with day-in-the-life content, storytelling, and authentic moments. Great for lifestyle and travel channels.",
                Category = "YouTube",
                SubCategory = "Vlog",
                Duration = 600,
                Difficulty = "beginner",
                PromptExample = "Create a vlog-style video documenting a day exploring a new city, including morning routine, activities, food experiences, and evening reflections",
                Tags = new[] { "youtube", "vlog", "lifestyle", "travel", "personal" },
                EstimatedTime = "10-12 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/youtube-vlog.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.5,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "youtube-educational-series",
                Name = "Educational Series",
                Description = "Multi-part educational content with clear learning objectives, examples, and key takeaways. Perfect for knowledge-based channels.",
                Category = "YouTube",
                SubCategory = "Education",
                Duration = 900,
                Difficulty = "advanced",
                PromptExample = "Create an educational video series about machine learning fundamentals, explaining concepts with real-world examples and practical applications",
                Tags = new[] { "youtube", "education", "series", "learning", "academic" },
                EstimatedTime = "15-20 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/youtube-education.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.9,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Social Media Category Templates
            new VideoTemplate
            {
                Id = "social-instagram-reel",
                Name = "Instagram Reel",
                Description = "Fast-paced, vertical format perfect for Instagram Reels. Hook viewers in the first 3 seconds with trending audio and quick cuts.",
                Category = "SocialMedia",
                SubCategory = "Reels",
                Duration = 30,
                Difficulty = "beginner",
                PromptExample = "Create a 30-second Instagram Reel showcasing 5 quick morning routine tips with upbeat music and dynamic transitions",
                Tags = new[] { "instagram", "reels", "short-form", "trending", "quick" },
                EstimatedTime = "30 seconds",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/instagram-reel.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.7,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "social-tiktok-trend",
                Name = "TikTok Trend Format",
                Description = "Viral-ready TikTok format with trending sounds, text overlays, and rapid scene changes. Optimized for maximum engagement.",
                Category = "SocialMedia",
                SubCategory = "TikTok",
                Duration = 60,
                Difficulty = "beginner",
                PromptExample = "Create a TikTok-style video following the 'Day in My Life' trend with quick cuts, trending audio, and engaging text overlays",
                Tags = new[] { "tiktok", "trending", "viral", "short-form", "entertainment" },
                EstimatedTime = "1 minute",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/tiktok-trend.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.8,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "social-youtube-shorts",
                Name = "YouTube Shorts",
                Description = "Vertical short-form content optimized for YouTube Shorts algorithm. High-energy, attention-grabbing format with strong hook.",
                Category = "SocialMedia",
                SubCategory = "Shorts",
                Duration = 60,
                Difficulty = "beginner",
                PromptExample = "Create a YouTube Short about '3 Productivity Hacks That Changed My Life' with bold text, quick transitions, and engaging visuals",
                Tags = new[] { "youtube-shorts", "short-form", "vertical", "engaging", "tips" },
                EstimatedTime = "1 minute",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/youtube-shorts.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.6,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "social-linkedin-video",
                Name = "LinkedIn Professional",
                Description = "Professional, informative format perfect for LinkedIn. Clean, corporate-friendly style with clear messaging and professional visuals.",
                Category = "SocialMedia",
                SubCategory = "LinkedIn",
                Duration = 120,
                Difficulty = "intermediate",
                PromptExample = "Create a professional LinkedIn video about industry insights and trends, with clean graphics and professional narration",
                Tags = new[] { "linkedin", "professional", "business", "b2b", "corporate" },
                EstimatedTime = "2 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/linkedin-video.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.5,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Business Category Templates
            new VideoTemplate
            {
                Id = "business-product-launch",
                Name = "Product Launch",
                Description = "Professional product launch video with feature highlights, benefits, and call-to-action. Perfect for marketing campaigns.",
                Category = "Business",
                SubCategory = "Marketing",
                Duration = 180,
                Difficulty = "intermediate",
                PromptExample = "Create a product launch video for a new SaaS platform, showcasing key features, user benefits, and a clear call-to-action",
                Tags = new[] { "business", "product-launch", "marketing", "saas", "b2b" },
                EstimatedTime = "3 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/product-launch.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.8,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "business-corporate-training",
                Name = "Corporate Training",
                Description = "Professional training video format with clear learning objectives, step-by-step instructions, and key takeaways.",
                Category = "Business",
                SubCategory = "Training",
                Duration = 600,
                Difficulty = "advanced",
                PromptExample = "Create a corporate training video about workplace safety protocols, including demonstrations, best practices, and compliance requirements",
                Tags = new[] { "business", "training", "corporate", "hr", "compliance" },
                EstimatedTime = "10 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/corporate-training.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.7,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "business-client-testimonial",
                Name = "Client Testimonial",
                Description = "Professional testimonial format showcasing customer success stories with authentic quotes and visual storytelling.",
                Category = "Business",
                SubCategory = "Testimonial",
                Duration = 120,
                Difficulty = "intermediate",
                PromptExample = "Create a client testimonial video featuring customer success stories, highlighting results and satisfaction with your service",
                Tags = new[] { "business", "testimonial", "case-study", "customer-success", "b2b" },
                EstimatedTime = "2 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/testimonial.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.6,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "business-explainer-video",
                Name = "Business Explainer",
                Description = "Clear, concise explainer video format for complex business concepts. Uses simple visuals and analogies for maximum clarity.",
                Category = "Business",
                SubCategory = "Explainer",
                Duration = 180,
                Difficulty = "intermediate",
                PromptExample = "Create an explainer video about how cloud computing works, using simple analogies and clear visuals to make complex concepts accessible",
                Tags = new[] { "business", "explainer", "b2b", "saas", "education" },
                EstimatedTime = "3 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/business-explainer.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.9,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Creative Category Templates
            new VideoTemplate
            {
                Id = "creative-cinematic-short",
                Name = "Cinematic Short",
                Description = "Artistic, cinematic format with dramatic visuals, moody lighting, and emotional storytelling. Perfect for film festivals and creative portfolios.",
                Category = "Creative",
                SubCategory = "Cinematic",
                Duration = 180,
                Difficulty = "advanced",
                PromptExample = "Create a cinematic short film about a day in the life of an artist, with dramatic lighting, emotional depth, and visual poetry",
                Tags = new[] { "creative", "cinematic", "artistic", "film", "dramatic" },
                EstimatedTime = "3 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/cinematic-short.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.9,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "creative-music-video",
                Name = "Music Video Style",
                Description = "Dynamic music video format with rhythm-based editing, creative transitions, and visual storytelling synchronized to music.",
                Category = "Creative",
                SubCategory = "Music",
                Duration = 240,
                Difficulty = "advanced",
                PromptExample = "Create a music video-style video with dynamic editing, creative transitions, and visuals that sync with the rhythm and mood of the music",
                Tags = new[] { "creative", "music-video", "artistic", "rhythm", "dynamic" },
                EstimatedTime = "4 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/music-video.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.8,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "creative-documentary-style",
                Name = "Documentary Style",
                Description = "Authentic documentary format with interview segments, b-roll, and narrative storytelling. Great for real stories and human interest content.",
                Category = "Creative",
                SubCategory = "Documentary",
                Duration = 600,
                Difficulty = "advanced",
                PromptExample = "Create a documentary-style video about a local community project, featuring interviews, behind-the-scenes footage, and narrative storytelling",
                Tags = new[] { "creative", "documentary", "storytelling", "authentic", "real" },
                EstimatedTime = "10 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/documentary.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.7,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new VideoTemplate
            {
                Id = "creative-animated-explainer",
                Name = "Animated Explainer",
                Description = "Vibrant animated explainer format with motion graphics, illustrations, and engaging visual metaphors. Perfect for creative brands.",
                Category = "Creative",
                SubCategory = "Animation",
                Duration = 120,
                Difficulty = "intermediate",
                PromptExample = "Create an animated explainer video about sustainable living, using colorful illustrations, motion graphics, and engaging visual metaphors",
                Tags = new[] { "creative", "animation", "motion-graphics", "illustration", "vibrant" },
                EstimatedTime = "2 minutes",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                PreviewImage = "/assets/templates/animated-explainer.jpg",
                PreviewVideo = "",
                UsageCount = 0,
                Rating = 4.8,
                RatingCount = 0,
                Author = "Aura Team",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
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
        public string? SubCategory { get; set; }
        public int Duration { get; set; }
        public string Difficulty { get; set; } = string.Empty;
        public string PromptExample { get; set; } = string.Empty;
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string EstimatedTime { get; set; } = string.Empty;
        public bool IsSystemTemplate { get; set; } = true;
        public bool IsCommunityTemplate { get; set; } = false;
        public string PreviewImage { get; set; } = string.Empty;
        public string PreviewVideo { get; set; } = string.Empty;
        public int UsageCount { get; set; } = 0;
        public double Rating { get; set; } = 0.0;
        public int RatingCount { get; set; } = 0;
        public string Author { get; set; } = "Aura Team";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
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
