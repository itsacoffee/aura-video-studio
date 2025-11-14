using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services;

/// <summary>
/// Service for managing project templates
/// </summary>
public class TemplateManagementService
{
    private readonly AuraDbContext _dbContext;
    private readonly ILogger<TemplateManagementService> _logger;

    public TemplateManagementService(
        AuraDbContext dbContext,
        ILogger<TemplateManagementService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all templates with optional filtering
    /// </summary>
    public async Task<List<TemplateEntity>> GetTemplatesAsync(
        string? category = null,
        string? subCategory = null,
        bool? isSystemTemplate = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.Templates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(t => t.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(subCategory))
        {
            query = query.Where(t => t.SubCategory == subCategory);
        }

        if (isSystemTemplate.HasValue)
        {
            query = query.Where(t => t.IsSystemTemplate == isSystemTemplate.Value);
        }

        return await query
            .OrderByDescending(t => t.UsageCount)
            .ThenBy(t => t.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Get a single template by ID
    /// </summary>
    public async Task<TemplateEntity?> GetTemplateByIdAsync(string templateId, CancellationToken ct = default)
    {
        return await _dbContext.Templates
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);
    }

    /// <summary>
    /// Create a new template
    /// </summary>
    public async Task<TemplateEntity> CreateTemplateAsync(
        string name,
        string description,
        string category,
        string subCategory,
        string templateData,
        List<string>? tags = null,
        string? previewImage = null,
        string? previewVideo = null,
        bool isSystemTemplate = false,
        CancellationToken ct = default)
    {
        var template = new TemplateEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Description = description,
            Category = category,
            SubCategory = subCategory,
            TemplateData = templateData,
            Tags = tags != null && tags.Count != 0 ? string.Join(",", tags) : string.Empty,
            PreviewImage = previewImage ?? string.Empty,
            PreviewVideo = previewVideo ?? string.Empty,
            IsSystemTemplate = isSystemTemplate,
            IsCommunityTemplate = !isSystemTemplate,
            Author = isSystemTemplate ? "System" : "User",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.Templates.Add(template);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Created new template: {TemplateId} - {Name}", template.Id, template.Name);

        return template;
    }

    /// <summary>
    /// Update an existing template
    /// </summary>
    public async Task<TemplateEntity?> UpdateTemplateAsync(
        string templateId,
        string? name = null,
        string? description = null,
        string? templateData = null,
        CancellationToken ct = default)
    {
        var template = await _dbContext.Templates.FirstOrDefaultAsync(t => t.Id == templateId, ct);
        if (template == null)
        {
            _logger.LogWarning("Template not found for update: {TemplateId}", templateId);
            return null;
        }

        if (name != null) template.Name = name;
        if (description != null) template.Description = description;
        if (templateData != null) template.TemplateData = templateData;

        template.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Updated template: {TemplateId} - {Name}", template.Id, template.Name);

        return template;
    }

    /// <summary>
    /// Delete a template
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(string templateId, CancellationToken ct = default)
    {
        var template = await _dbContext.Templates.FirstOrDefaultAsync(t => t.Id == templateId, ct);
        if (template == null)
        {
            _logger.LogWarning("Template not found for deletion: {TemplateId}", templateId);
            return false;
        }

        // Don't allow deletion of system templates
        if (template.IsSystemTemplate)
        {
            _logger.LogWarning("Cannot delete system template: {TemplateId}", templateId);
            return false;
        }

        _dbContext.Templates.Remove(template);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted template: {TemplateId} - {Name}", template.Id, template.Name);

        return true;
    }

    /// <summary>
    /// Increment template usage count
    /// </summary>
    public async Task<bool> IncrementUsageCountAsync(string templateId, CancellationToken ct = default)
    {
        var template = await _dbContext.Templates.FirstOrDefaultAsync(t => t.Id == templateId, ct);
        if (template == null)
        {
            return false;
        }

        template.UsageCount++;
        template.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    /// <summary>
    /// Create predefined system templates
    /// </summary>
    public async Task SeedSystemTemplatesAsync(CancellationToken ct = default)
    {
        // Check if system templates already exist
        var existingTemplates = await _dbContext.Templates
            .Where(t => t.IsSystemTemplate)
            .CountAsync(ct);

        if (existingTemplates > 0)
        {
            _logger.LogInformation("System templates already exist, skipping seed");
            return;
        }

        var templates = new List<TemplateEntity>
        {
            // Product Demo Template
            new TemplateEntity
            {
                Id = "template-product-demo",
                Name = "Product Demo",
                Description = "Professional product demonstration video with feature highlights",
                Category = "Business",
                SubCategory = "Product Demo",
                Tags = "product,demo,business,marketing",
                TemplateData = @"{
                    ""sections"": [
                        {""name"": ""Hook"", ""duration"": 5, ""tone"": ""exciting"", ""style"": ""engaging""},
                        {""name"": ""Problem"", ""duration"": 10, ""tone"": ""empathetic"", ""style"": ""relatable""},
                        {""name"": ""Solution"", ""duration"": 15, ""tone"": ""confident"", ""style"": ""informative""},
                        {""name"": ""Features"", ""duration"": 20, ""tone"": ""professional"", ""style"": ""detailed""},
                        {""name"": ""Call to Action"", ""duration"": 10, ""tone"": ""persuasive"", ""style"": ""direct""}
                    ],
                    ""visualStyle"": ""modern-clean"",
                    ""pacing"": ""medium"",
                    ""musicStyle"": ""upbeat""
                }",
                PreviewImage = "/templates/product-demo.jpg",
                IsSystemTemplate = true,
                Author = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Tutorial Template
            new TemplateEntity
            {
                Id = "template-tutorial",
                Name = "Tutorial Video",
                Description = "Step-by-step tutorial format for educational content",
                Category = "YouTube",
                SubCategory = "Tutorial",
                Tags = "tutorial,education,how-to,learning",
                TemplateData = @"{
                    ""sections"": [
                        {""name"": ""Introduction"", ""duration"": 8, ""tone"": ""friendly"", ""style"": ""welcoming""},
                        {""name"": ""Overview"", ""duration"": 12, ""tone"": ""informative"", ""style"": ""clear""},
                        {""name"": ""Step 1"", ""duration"": 15, ""tone"": ""instructional"", ""style"": ""detailed""},
                        {""name"": ""Step 2"", ""duration"": 15, ""tone"": ""instructional"", ""style"": ""detailed""},
                        {""name"": ""Step 3"", ""duration"": 15, ""tone"": ""instructional"", ""style"": ""detailed""},
                        {""name"": ""Conclusion"", ""duration"": 10, ""tone"": ""encouraging"", ""style"": ""summary""}
                    ],
                    ""visualStyle"": ""clean-educational"",
                    ""pacing"": ""slow"",
                    ""musicStyle"": ""soft-background""
                }",
                PreviewImage = "/templates/tutorial.jpg",
                IsSystemTemplate = true,
                Author = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Social Media Ad Template
            new TemplateEntity
            {
                Id = "template-social-media-ad",
                Name = "Social Media Ad",
                Description = "Short, punchy advertisement optimized for social media platforms",
                Category = "SocialMedia",
                SubCategory = "Advertisement",
                Tags = "ad,social-media,marketing,short-form",
                TemplateData = @"{
                    ""sections"": [
                        {""name"": ""Hook"", ""duration"": 3, ""tone"": ""attention-grabbing"", ""style"": ""dynamic""},
                        {""name"": ""Value Proposition"", ""duration"": 7, ""tone"": ""confident"", ""style"": ""concise""},
                        {""name"": ""Social Proof"", ""duration"": 5, ""tone"": ""trustworthy"", ""style"": ""credible""},
                        {""name"": ""Call to Action"", ""duration"": 5, ""tone"": ""urgent"", ""style"": ""direct""}
                    ],
                    ""visualStyle"": ""vibrant-dynamic"",
                    ""pacing"": ""fast"",
                    ""musicStyle"": ""energetic"",
                    ""aspectRatio"": ""9:16""
                }",
                PreviewImage = "/templates/social-media-ad.jpg",
                IsSystemTemplate = true,
                Author = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Story Narrative Template
            new TemplateEntity
            {
                Id = "template-story-narrative",
                Name = "Story Narrative",
                Description = "Compelling storytelling format for narrative-driven content",
                Category = "Creative",
                SubCategory = "Storytelling",
                Tags = "story,narrative,creative,emotional",
                TemplateData = @"{
                    ""sections"": [
                        {""name"": ""Opening"", ""duration"": 10, ""tone"": ""intriguing"", ""style"": ""atmospheric""},
                        {""name"": ""Setup"", ""duration"": 15, ""tone"": ""engaging"", ""style"": ""descriptive""},
                        {""name"": ""Conflict"", ""duration"": 15, ""tone"": ""dramatic"", ""style"": ""intense""},
                        {""name"": ""Climax"", ""duration"": 12, ""tone"": ""emotional"", ""style"": ""powerful""},
                        {""name"": ""Resolution"", ""duration"": 13, ""tone"": ""satisfying"", ""style"": ""reflective""}
                    ],
                    ""visualStyle"": ""cinematic"",
                    ""pacing"": ""variable"",
                    ""musicStyle"": ""orchestral-emotional""
                }",
                PreviewImage = "/templates/story-narrative.jpg",
                IsSystemTemplate = true,
                Author = "System",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        _dbContext.Templates.AddRange(templates);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Seeded {Count} system templates", templates.Count);
    }
}
