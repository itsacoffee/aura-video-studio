using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Aura.Api.Data;

/// <summary>
/// Seeds the database with initial test data for local development
/// </summary>
public class SeedData
{
    private readonly AuraDbContext _context;
    private readonly ILogger<SeedData> _logger;

    public SeedData(AuraDbContext context, ILogger<SeedData> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Seed the database with test data if it's empty
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            // Run migrations if needed
            await _context.Database.MigrateAsync().ConfigureAwait(false);

            // Check if we already have data
            var hasData = await _context.ProjectStates.AnyAsync().ConfigureAwait(false);
            if (hasData)
            {
                _logger.LogInformation("Database already contains data, skipping seed");
                return;
            }

            _logger.LogInformation("Seeding database with test data...");

            // Seed test data
            await SeedUserSetup().ConfigureAwait(false);
            await SeedProjectStates().ConfigureAwait(false);
            await SeedTemplates().ConfigureAwait(false);
            await SeedCustomTemplates().ConfigureAwait(false);
            await SeedExportHistory().ConfigureAwait(false);
            await SeedConfiguration().ConfigureAwait(false);

            // Save changes
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private async Task SeedUserSetup()
    {
        var existingSetup = await _context.UserSetups.AnyAsync().ConfigureAwait(false);
        if (existingSetup)
        {
            return;
        }

        // Create a default user setup for development
        var userSetup = new UserSetupEntity
        {
            Id = Guid.NewGuid().ToString(),
            UserId = "default",
            Completed = false, // Not completed so wizard shows on first run
            CompletedAt = null,
            Version = "1.0.0",
            LastStep = 0,
            UpdatedAt = DateTime.UtcNow,
            SelectedTier = null,
            WizardState = null
        };

        _context.UserSetups.Add(userSetup);
        _logger.LogInformation("Seeded user setup data");
    }

    private async Task SeedProjectStates()
    {
        var existingProjects = await _context.ProjectStates.AnyAsync().ConfigureAwait(false);
        if (existingProjects)
        {
            return;
        }

        var projects = new[]
        {
            new ProjectStateEntity
            {
                Id = Guid.NewGuid(),
                Title = "Welcome to Aura - Sample Project",
                Description = "This is a sample project to help you get started with Aura Video Studio. " +
                             "It demonstrates a simple video workflow with script, voiceover, and visuals.",
                Status = "Draft",
                CurrentWizardStep = 0,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow.AddDays(-5),
                IsDeleted = false,
                ProgressPercent = 25,
                CurrentStage = "Script",
                BriefJson = @"{""topic"":""Introduction to Aura Video Studio"",""duration"":60}"
            },
            new ProjectStateEntity
            {
                Id = Guid.NewGuid(),
                Title = "Quick Start Tutorial",
                Description = "Learn how to create your first video in Aura using the Guided Mode.",
                Status = "InProgress",
                CurrentWizardStep = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow.AddDays(-2),
                IsDeleted = false,
                ProgressPercent = 50,
                CurrentStage = "TTS",
                BriefJson = @"{""topic"":""Quick Start Guide"",""duration"":90}",
                PlanSpecJson = @"{""scenes"":[{""index"":0,""text"":""Welcome to the quick start guide""}]}"
            },
            new ProjectStateEntity
            {
                Id = Guid.NewGuid(),
                Title = "Advanced Features Demo",
                Description = "Explore advanced features like ML Lab and custom prompt templates.",
                Status = "Completed",
                CurrentWizardStep = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow.AddHours(-12),
                CompletedAt = DateTime.UtcNow.AddHours(-12),
                IsDeleted = false,
                ProgressPercent = 100,
                CurrentStage = "Render",
                BriefJson = @"{""topic"":""Advanced Features Overview"",""duration"":120}",
                PlanSpecJson = @"{""scenes"":[{""index"":0,""text"":""Advanced features demo""}]}",
                VoiceSpecJson = @"{""provider"":""openai"",""voice"":""alloy""}"
            }
        };

        _context.ProjectStates.AddRange(projects);
        _logger.LogInformation("Seeded {Count} sample projects", projects.Length);
    }

    private async Task SeedTemplates()
    {
        var existingTemplates = await _context.Templates.AnyAsync().ConfigureAwait(false);
        if (existingTemplates)
        {
            return;
        }

        var templates = new[]
        {
            new TemplateEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Social Media Quick Post",
                Description = "Short-form video optimized for social media platforms",
                Category = "SocialMedia",
                SubCategory = "Short",
                Tags = "social,quick,short-form",
                TemplateData = @"{""duration"":30,""aspectRatio"":""9:16""}",
                Author = "Aura Team",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                UsageCount = 156,
                Rating = 4.5,
                RatingCount = 42,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new TemplateEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Educational Content",
                Description = "Template for creating educational and tutorial videos",
                Category = "Education",
                SubCategory = "Tutorial",
                Tags = "education,tutorial,learning",
                TemplateData = @"{""duration"":300,""aspectRatio"":""16:9""}",
                Author = "Aura Team",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                UsageCount = 89,
                Rating = 4.8,
                RatingCount = 31,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new TemplateEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Product Showcase",
                Description = "Highlight product features with dynamic visuals",
                Category = "Marketing",
                SubCategory = "Product",
                Tags = "marketing,product,showcase",
                TemplateData = @"{""duration"":90,""aspectRatio"":""16:9""}",
                Author = "Aura Team",
                IsSystemTemplate = true,
                IsCommunityTemplate = false,
                UsageCount = 124,
                Rating = 4.6,
                RatingCount = 38,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            }
        };

        _context.Templates.AddRange(templates);
        _logger.LogInformation("Seeded {Count} templates", templates.Length);
    }

    private async Task SeedExportHistory()
    {
        var existingHistory = await _context.ExportHistory.AnyAsync().ConfigureAwait(false);
        if (existingHistory)
        {
            return;
        }

        var history = new[]
        {
            new ExportHistoryEntity
            {
                Id = Guid.NewGuid().ToString(),
                InputFile = "/tmp/project1.mp4",
                OutputFile = "/output/project1_youtube.mp4",
                PresetName = "YouTube 1080p",
                Status = "Completed",
                Progress = 100,
                CreatedAt = DateTime.UtcNow.AddHours(-24),
                StartedAt = DateTime.UtcNow.AddHours(-24),
                CompletedAt = DateTime.UtcNow.AddHours(-23).AddMinutes(-45),
                FileSize = 52428800, // 50 MB
                DurationSeconds = 120,
                Platform = "YouTube",
                Resolution = "1920x1080",
                Codec = "h264"
            },
            new ExportHistoryEntity
            {
                Id = Guid.NewGuid().ToString(),
                InputFile = "/tmp/project2.mp4",
                OutputFile = "/output/project2_instagram.mp4",
                PresetName = "Instagram Reel",
                Status = "Completed",
                Progress = 100,
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                StartedAt = DateTime.UtcNow.AddHours(-12),
                CompletedAt = DateTime.UtcNow.AddHours(-11).AddMinutes(-50),
                FileSize = 15728640, // 15 MB
                DurationSeconds = 30,
                Platform = "Instagram",
                Resolution = "1080x1920",
                Codec = "h264"
            }
        };

        _context.ExportHistory.AddRange(history);
        _logger.LogInformation("Seeded {Count} export history records", history.Length);
    }

    private async Task SeedCustomTemplates()
    {
        var existingCustomTemplates = await _context.CustomTemplates.AnyAsync().ConfigureAwait(false);
        if (existingCustomTemplates)
        {
            return;
        }

        var customTemplates = new[]
        {
            new CustomTemplateEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "My Custom Educational Template",
                Description = "Custom template for my educational content series",
                Category = "Education",
                Tags = "education,custom,series",
                Author = "Demo User",
                IsDefault = false,
                ScriptStructureJson = @"{""scenes"":3,""style"":""educational""}",
                VideoStructureJson = @"{""intro"":5,""body"":80,""outro"":15}",
                LLMPipelineJson = @"{""provider"":""openai"",""model"":""gpt-4""}",
                VisualPreferencesJson = @"{""style"":""modern"",""color_scheme"":""blue""}",
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new CustomTemplateEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Quick Social Media Template",
                Description = "My go-to template for quick social media posts",
                Category = "SocialMedia",
                Tags = "social,quick,vertical",
                Author = "Demo User",
                IsDefault = true,
                ScriptStructureJson = @"{""scenes"":1,""style"":""casual""}",
                VideoStructureJson = @"{""format"":""vertical"",""duration"":30}",
                LLMPipelineJson = @"{""provider"":""openai"",""model"":""gpt-3.5-turbo""}",
                VisualPreferencesJson = @"{""style"":""vibrant"",""color_scheme"":""rainbow""}",
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        _context.CustomTemplates.AddRange(customTemplates);
        _logger.LogInformation("Seeded {Count} custom templates", customTemplates.Length);
    }

    private async Task SeedConfiguration()
    {
        var existingConfig = await _context.Configurations.AnyAsync().ConfigureAwait(false);
        if (existingConfig)
        {
            return;
        }

        var configurations = new[]
        {
            new ConfigurationEntity
            {
                Key = "app.theme",
                Value = "dark",
                Category = "Appearance",
                ValueType = "string",
                Description = "Application theme (light/dark)",
                IsSensitive = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ConfigurationEntity
            {
                Key = "app.language",
                Value = "en-US",
                Category = "Localization",
                ValueType = "string",
                Description = "Default application language",
                IsSensitive = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ConfigurationEntity
            {
                Key = "ffmpeg.threads",
                Value = "4",
                Category = "Performance",
                ValueType = "number",
                Description = "Number of FFmpeg encoding threads",
                IsSensitive = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ConfigurationEntity
            {
                Key = "llm.default_provider",
                Value = "openai",
                Category = "AI",
                ValueType = "string",
                Description = "Default LLM provider for content generation",
                IsSensitive = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ConfigurationEntity
            {
                Key = "cache.ttl_minutes",
                Value = "60",
                Category = "Performance",
                ValueType = "number",
                Description = "Cache time-to-live in minutes",
                IsSensitive = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _context.Configurations.AddRange(configurations);
        _logger.LogInformation("Seeded {Count} configuration entries", configurations.Length);
    }
}
