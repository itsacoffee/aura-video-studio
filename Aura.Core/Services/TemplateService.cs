using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for managing project templates
/// </summary>
public class TemplateService
{
    private readonly ILogger<TemplateService> _logger;
    private readonly AuraDbContext _context;

    public TemplateService(ILogger<TemplateService> logger, AuraDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Get all templates with optional filtering and pagination
    /// </summary>
    public async Task<PaginatedTemplatesResponse> GetTemplatesAsync(
        TemplateCategory? category = null,
        string? subCategory = null,
        bool systemOnly = false,
        bool communityOnly = false,
        int page = 1,
        int pageSize = 50)
    {
        try
        {
            // Validate pagination parameters
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _context.Templates.AsQueryable();

            if (category.HasValue)
            {
                query = query.Where(t => t.Category == category.Value.ToString());
            }

            if (!string.IsNullOrWhiteSpace(subCategory))
            {
                query = query.Where(t => t.SubCategory == subCategory);
            }

            if (systemOnly)
            {
                query = query.Where(t => t.IsSystemTemplate);
            }

            if (communityOnly)
            {
                query = query.Where(t => t.IsCommunityTemplate);
            }

            // Apply stable sorting for reproducible pages
            query = query
                .OrderByDescending(t => t.UsageCount)
                .ThenByDescending(t => t.Rating)
                .ThenBy(t => t.Id); // Tie-breaker for stable sorting

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply pagination
            var skip = (page - 1) * pageSize;
            var templates = await query
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PaginatedTemplatesResponse
            {
                Items = templates.Select(MapToListItem).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get templates");
            throw;
        }
    }

    /// <summary>
    /// Get all templates without pagination (for backwards compatibility)
    /// Warning: This loads all templates into memory. Use with caution for large datasets.
    /// Consider using GetTemplatesAsync with pagination for better performance.
    /// </summary>
    public async Task<List<TemplateListItem>> GetAllTemplatesAsync(
        TemplateCategory? category = null,
        string? subCategory = null,
        bool systemOnly = false,
        bool communityOnly = false)
    {
        try
        {
            // Use a large but reasonable page size limit (10000 templates max)
            // This prevents potential memory issues while maintaining backwards compatibility
            var response = await GetTemplatesAsync(category, subCategory, systemOnly, communityOnly, 1, 10000);
            return response.Items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all templates");
            throw;
        }
    }

    /// <summary>
    /// Get template by ID
    /// </summary>
    public async Task<ProjectTemplate?> GetTemplateByIdAsync(string id)
    {
        try
        {
            var entity = await _context.Templates.FindAsync(id);
            return entity != null ? MapToModel(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get template {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Create new template
    /// </summary>
    public async Task<ProjectTemplate> CreateTemplateAsync(SaveAsTemplateRequest request)
    {
        try
        {
            var entity = new TemplateEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Category = request.Category.ToString(),
                SubCategory = request.SubCategory,
                Tags = string.Join(",", request.Tags),
                TemplateData = request.ProjectData,
                PreviewImage = request.PreviewImage,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Author = "User",
                IsSystemTemplate = false,
                IsCommunityTemplate = true,
                UsageCount = 0,
                Rating = 0.0,
                RatingCount = 0
            };

            _context.Templates.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created template {TemplateId} - {TemplateName}", entity.Id, entity.Name);

            return MapToModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create template {TemplateName}", request.Name);
            throw;
        }
    }

    /// <summary>
    /// Delete template
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(string id)
    {
        try
        {
            var entity = await _context.Templates.FindAsync(id);
            if (entity == null)
            {
                return false;
            }

            // Don't allow deleting system templates
            if (entity.IsSystemTemplate)
            {
                _logger.LogWarning("Attempted to delete system template {TemplateId}", id);
                return false;
            }

            _context.Templates.Remove(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted template {TemplateId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete template {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Increment usage count for template
    /// </summary>
    public async Task IncrementUsageAsync(string id)
    {
        try
        {
            var entity = await _context.Templates.FindAsync(id);
            if (entity != null)
            {
                entity.UsageCount++;
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment usage for template {TemplateId}", id);
            // Don't throw - this is not critical
        }
    }

    /// <summary>
    /// Get effect presets
    /// </summary>
    public List<EffectPreset> GetEffectPresets()
    {
        return new List<EffectPreset>
        {
            new EffectPreset
            {
                Id = "cinematic",
                Name = "Cinematic",
                Description = "Professional film look with color grading and vignette",
                Category = "Professional",
                Effects = new List<TemplateEffect>
                {
                    new TemplateEffect
                    {
                        Id = "colorgrade1",
                        Name = "Color Grade",
                        Type = "colorGrade",
                        Parameters = new Dictionary<string, object>
                        {
                            { "temperature", 5800 },
                            { "tint", 10 },
                            { "saturation", 1.2 },
                            { "contrast", 1.15 }
                        }
                    },
                    new TemplateEffect
                    {
                        Id = "vignette1",
                        Name = "Vignette",
                        Type = "vignette",
                        Parameters = new Dictionary<string, object>
                        {
                            { "intensity", 0.3 },
                            { "roundness", 0.5 }
                        }
                    }
                }
            },
            new EffectPreset
            {
                Id = "retro",
                Name = "Retro",
                Description = "Vintage look with film grain and desaturation",
                Category = "Vintage",
                Effects = new List<TemplateEffect>
                {
                    new TemplateEffect
                    {
                        Id = "grain1",
                        Name = "Film Grain",
                        Type = "grain",
                        Parameters = new Dictionary<string, object>
                        {
                            { "intensity", 0.15 },
                            { "size", 1.5 }
                        }
                    },
                    new TemplateEffect
                    {
                        Id = "desat1",
                        Name = "Desaturation",
                        Type = "colorGrade",
                        Parameters = new Dictionary<string, object>
                        {
                            { "saturation", 0.6 },
                            { "temperature", 6200 }
                        }
                    }
                }
            },
            new EffectPreset
            {
                Id = "dynamic",
                Name = "Dynamic",
                Description = "High-energy look with motion blur and chromatic aberration",
                Category = "Action",
                Effects = new List<TemplateEffect>
                {
                    new TemplateEffect
                    {
                        Id = "motionblur1",
                        Name = "Motion Blur",
                        Type = "motionBlur",
                        Parameters = new Dictionary<string, object>
                        {
                            { "amount", 0.4 },
                            { "angle", 0 }
                        }
                    },
                    new TemplateEffect
                    {
                        Id = "chromatic1",
                        Name = "Chromatic Aberration",
                        Type = "chromaticAberration",
                        Parameters = new Dictionary<string, object>
                        {
                            { "amount", 0.02 }
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// Get transition presets
    /// </summary>
    public List<TransitionPreset> GetTransitionPresets()
    {
        return new List<TransitionPreset>
        {
            new TransitionPreset
            {
                Id = "crossdissolve",
                Name = "Cross Dissolve",
                Type = "crossDissolve",
                DefaultDuration = 1.0
            },
            new TransitionPreset
            {
                Id = "wipe-left",
                Name = "Wipe Left",
                Type = "wipe",
                Direction = "left",
                DefaultDuration = 0.8
            },
            new TransitionPreset
            {
                Id = "wipe-right",
                Name = "Wipe Right",
                Type = "wipe",
                Direction = "right",
                DefaultDuration = 0.8
            },
            new TransitionPreset
            {
                Id = "wipe-up",
                Name = "Wipe Up",
                Type = "wipe",
                Direction = "up",
                DefaultDuration = 0.8
            },
            new TransitionPreset
            {
                Id = "wipe-down",
                Name = "Wipe Down",
                Type = "wipe",
                Direction = "down",
                DefaultDuration = 0.8
            },
            new TransitionPreset
            {
                Id = "zoom",
                Name = "Zoom",
                Type = "zoom",
                DefaultDuration = 1.0
            },
            new TransitionPreset
            {
                Id = "slide-left",
                Name = "Slide Left",
                Type = "slide",
                Direction = "left",
                DefaultDuration = 0.7
            },
            new TransitionPreset
            {
                Id = "slide-right",
                Name = "Slide Right",
                Type = "slide",
                Direction = "right",
                DefaultDuration = 0.7
            },
            new TransitionPreset
            {
                Id = "fade-black",
                Name = "Fade to Black",
                Type = "fade",
                Direction = "black",
                DefaultDuration = 1.5
            },
            new TransitionPreset
            {
                Id = "fade-white",
                Name = "Fade to White",
                Type = "fade",
                Direction = "white",
                DefaultDuration = 1.5
            }
        };
    }

    /// <summary>
    /// Get title templates
    /// </summary>
    public List<TitleTemplate> GetTitleTemplates()
    {
        return new List<TitleTemplate>
        {
            new TitleTemplate
            {
                Id = "lower-third",
                Name = "Lower Third",
                Category = "Informational",
                Duration = 5.0,
                TextLayers = new List<TemplateTextOverlay>
                {
                    new TemplateTextOverlay
                    {
                        Id = "lt-title",
                        Text = "Main Title",
                        FontSize = 36,
                        Color = "#FFFFFF",
                        Animation = "slideIn",
                        Position = new TemplatePosition { X = 0.1, Y = 0.85, Alignment = "left" },
                        StartTime = 0,
                        Duration = 5.0
                    },
                    new TemplateTextOverlay
                    {
                        Id = "lt-subtitle",
                        Text = "Subtitle",
                        FontSize = 24,
                        Color = "#CCCCCC",
                        Animation = "slideIn",
                        Position = new TemplatePosition { X = 0.1, Y = 0.9, Alignment = "left" },
                        StartTime = 0.2,
                        Duration = 4.8
                    }
                }
            },
            new TitleTemplate
            {
                Id = "end-credits",
                Name = "End Credits",
                Category = "Credits",
                Duration = 10.0,
                TextLayers = new List<TemplateTextOverlay>
                {
                    new TemplateTextOverlay
                    {
                        Id = "credits-text",
                        Text = "Credits text goes here",
                        FontSize = 32,
                        Color = "#FFFFFF",
                        Animation = "scrollUp",
                        Position = new TemplatePosition { X = 0.5, Y = 0.5, Alignment = "center" },
                        StartTime = 0,
                        Duration = 10.0
                    }
                }
            },
            new TitleTemplate
            {
                Id = "chapter-marker",
                Name = "Chapter Marker",
                Category = "Navigation",
                Duration = 3.0,
                TextLayers = new List<TemplateTextOverlay>
                {
                    new TemplateTextOverlay
                    {
                        Id = "chapter-text",
                        Text = "Chapter Title",
                        FontSize = 48,
                        Color = "#FFFFFF",
                        Animation = "fadeIn",
                        Position = new TemplatePosition { X = 0.5, Y = 0.5, Alignment = "center" },
                        StartTime = 0,
                        Duration = 3.0
                    }
                }
            },
            new TitleTemplate
            {
                Id = "subscribe-reminder",
                Name = "Subscribe Reminder",
                Category = "Call-to-Action",
                Duration = 5.0,
                TextLayers = new List<TemplateTextOverlay>
                {
                    new TemplateTextOverlay
                    {
                        Id = "subscribe-text",
                        Text = "Don't forget to subscribe!",
                        FontSize = 40,
                        Color = "#FF0000",
                        Animation = "bounce",
                        Position = new TemplatePosition { X = 0.5, Y = 0.2, Alignment = "center" },
                        StartTime = 0,
                        Duration = 5.0
                    }
                }
            }
        };
    }

    /// <summary>
    /// Seed database with sample templates
    /// </summary>
    public async Task SeedSampleTemplatesAsync()
    {
        try
        {
            // Check if templates already exist
            if (await _context.Templates.AnyAsync())
            {
                _logger.LogInformation("Templates already exist, skipping seed");
                return;
            }

            var sampleTemplates = GetSampleTemplates();

            foreach (var template in sampleTemplates)
            {
                _context.Templates.Add(template);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Seeded {Count} sample templates", sampleTemplates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed sample templates");
            throw;
        }
    }

    private List<TemplateEntity> GetSampleTemplates()
    {
        var templates = new List<TemplateEntity>();

        // YouTube Intro
        templates.Add(new TemplateEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = "YouTube Intro",
            Description = "Quick 5-second intro for YouTube videos",
            Category = "YouTube",
            SubCategory = "Intro",
            Tags = "intro,youtube,branding",
            PreviewImage = "/assets/templates/youtube-intro-preview.png",
            TemplateData = JsonSerializer.Serialize(new TemplateStructure
            {
                Duration = 5.0,
                Settings = new TemplateSettings { Width = 1920, Height = 1080, FrameRate = 30 },
                Tracks = new List<TemplateTrack>
                {
                    new TemplateTrack { Id = "video1", Label = "Video 1", Type = "video" },
                    new TemplateTrack { Id = "audio1", Label = "Audio 1", Type = "audio" }
                },
                Placeholders = new List<TemplatePlaceholder>
                {
                    new TemplatePlaceholder
                    {
                        Id = "intro-bg",
                        TrackId = "video1",
                        StartTime = 0,
                        Duration = 5.0,
                        Type = "video",
                        PlaceholderText = "Add your intro background"
                    }
                },
                TextOverlays = new List<TemplateTextOverlay>
                {
                    new TemplateTextOverlay
                    {
                        Id = "title",
                        TrackId = "video1",
                        StartTime = 1.0,
                        Duration = 3.0,
                        Text = "Your Channel Name",
                        FontSize = 72,
                        Color = "#FFFFFF",
                        Animation = "fadeIn",
                        Position = new TemplatePosition { X = 0.5, Y = 0.5, Alignment = "center" }
                    }
                },
                MusicTrack = new TemplateMusicTrack
                {
                    TrackId = "audio1",
                    StartTime = 0,
                    Duration = 5.0,
                    Volume = 0.6,
                    FadeIn = true,
                    FadeOut = true
                }
            }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Author = "System",
            IsSystemTemplate = true,
            IsCommunityTemplate = false,
            UsageCount = 0,
            Rating = 4.5,
            RatingCount = 100
        });

        // Instagram Story
        templates.Add(new TemplateEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Instagram Story",
            Description = "Vertical format template for Instagram Stories",
            Category = "SocialMedia",
            SubCategory = "Instagram Story",
            Tags = "instagram,story,social,vertical",
            PreviewImage = "/assets/templates/instagram-story-preview.png",
            TemplateData = JsonSerializer.Serialize(new TemplateStructure
            {
                Duration = 15.0,
                Settings = new TemplateSettings { Width = 1080, Height = 1920, FrameRate = 30, AspectRatio = "9:16" },
                Tracks = new List<TemplateTrack>
                {
                    new TemplateTrack { Id = "video1", Label = "Video 1", Type = "video" }
                },
                Placeholders = new List<TemplatePlaceholder>
                {
                    new TemplatePlaceholder
                    {
                        Id = "story-content",
                        TrackId = "video1",
                        StartTime = 0,
                        Duration = 15.0,
                        Type = "video",
                        PlaceholderText = "Add your story content"
                    }
                },
                TextOverlays = new List<TemplateTextOverlay>
                {
                    new TemplateTextOverlay
                    {
                        Id = "story-title",
                        TrackId = "video1",
                        StartTime = 2.0,
                        Duration = 5.0,
                        Text = "Swipe Up!",
                        FontSize = 48,
                        Color = "#FFFFFF",
                        Animation = "bounce",
                        Position = new TemplatePosition { X = 0.5, Y = 0.85, Alignment = "center" }
                    }
                }
            }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Author = "System",
            IsSystemTemplate = true,
            IsCommunityTemplate = false,
            UsageCount = 0,
            Rating = 4.7,
            RatingCount = 150
        });

        // Product Demo
        templates.Add(new TemplateEntity
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Product Demo",
            Description = "Professional product demonstration template",
            Category = "Business",
            SubCategory = "Product Demo",
            Tags = "product,demo,business,marketing",
            PreviewImage = "/assets/templates/product-demo-preview.png",
            TemplateData = JsonSerializer.Serialize(new TemplateStructure
            {
                Duration = 30.0,
                Settings = new TemplateSettings { Width = 1920, Height = 1080, FrameRate = 30 },
                Tracks = new List<TemplateTrack>
                {
                    new TemplateTrack { Id = "video1", Label = "Video 1", Type = "video" },
                    new TemplateTrack { Id = "video2", Label = "Video 2", Type = "video" }
                },
                Placeholders = new List<TemplatePlaceholder>
                {
                    new TemplatePlaceholder
                    {
                        Id = "product-shot1",
                        TrackId = "video1",
                        StartTime = 0,
                        Duration = 10.0,
                        Type = "video",
                        PlaceholderText = "Product shot 1"
                    },
                    new TemplatePlaceholder
                    {
                        Id = "product-shot2",
                        TrackId = "video1",
                        StartTime = 10.0,
                        Duration = 10.0,
                        Type = "video",
                        PlaceholderText = "Product shot 2"
                    },
                    new TemplatePlaceholder
                    {
                        Id = "product-shot3",
                        TrackId = "video1",
                        StartTime = 20.0,
                        Duration = 10.0,
                        Type = "video",
                        PlaceholderText = "Product shot 3"
                    }
                },
                TextOverlays = new List<TemplateTextOverlay>
                {
                    new TemplateTextOverlay
                    {
                        Id = "product-name",
                        TrackId = "video2",
                        StartTime = 0,
                        Duration = 30.0,
                        Text = "Product Name",
                        FontSize = 54,
                        Color = "#FFFFFF",
                        Animation = "slideIn",
                        Position = new TemplatePosition { X = 0.1, Y = 0.1, Alignment = "left" }
                    }
                }
            }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Author = "System",
            IsSystemTemplate = true,
            IsCommunityTemplate = false,
            UsageCount = 0,
            Rating = 4.3,
            RatingCount = 75
        });

        return templates;
    }

    private TemplateListItem MapToListItem(TemplateEntity entity)
    {
        return new TemplateListItem
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Category = Enum.Parse<TemplateCategory>(entity.Category),
            SubCategory = entity.SubCategory,
            PreviewImage = entity.PreviewImage,
            PreviewVideo = entity.PreviewVideo,
            Tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            UsageCount = entity.UsageCount,
            Rating = entity.Rating,
            IsSystemTemplate = entity.IsSystemTemplate,
            IsCommunityTemplate = entity.IsCommunityTemplate
        };
    }

    private ProjectTemplate MapToModel(TemplateEntity entity)
    {
        return new ProjectTemplate
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Category = Enum.Parse<TemplateCategory>(entity.Category),
            SubCategory = entity.SubCategory,
            PreviewImage = entity.PreviewImage,
            PreviewVideo = entity.PreviewVideo,
            Tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            TemplateData = entity.TemplateData,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Author = entity.Author,
            IsSystemTemplate = entity.IsSystemTemplate,
            IsCommunityTemplate = entity.IsCommunityTemplate,
            UsageCount = entity.UsageCount,
            Rating = entity.Rating,
            RatingCount = entity.RatingCount
        };
    }

    /// <summary>
    /// Get all custom video templates
    /// </summary>
    public async Task<List<CustomVideoTemplate>> GetCustomTemplatesAsync(string? category = null)
    {
        try
        {
            var query = _context.CustomTemplates.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                query = query.Where(t => t.Category == category);
            }

            var entities = await query.OrderByDescending(t => t.UpdatedAt).ToListAsync();

            return entities.Select(MapCustomTemplateToModel).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get custom templates");
            throw;
        }
    }

    /// <summary>
    /// Get custom template by ID
    /// </summary>
    public async Task<CustomVideoTemplate?> GetCustomTemplateByIdAsync(string id)
    {
        try
        {
            var entity = await _context.CustomTemplates.FindAsync(id);
            return entity != null ? MapCustomTemplateToModel(entity) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get custom template {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Create new custom template
    /// </summary>
    public async Task<CustomVideoTemplate> CreateCustomTemplateAsync(CreateCustomTemplateRequest request)
    {
        try
        {
            var entity = new CustomTemplateEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Category = request.Category,
                Tags = string.Join(",", request.Tags),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Author = "User",
                IsDefault = false,
                ScriptStructureJson = JsonSerializer.Serialize(request.ScriptStructure),
                VideoStructureJson = JsonSerializer.Serialize(request.VideoStructure),
                LLMPipelineJson = JsonSerializer.Serialize(request.LLMPipeline),
                VisualPreferencesJson = JsonSerializer.Serialize(request.VisualPrefs)
            };

            _context.CustomTemplates.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created custom template {TemplateId} - {TemplateName}", entity.Id, entity.Name);

            return MapCustomTemplateToModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create custom template {TemplateName}", request.Name);
            throw;
        }
    }

    /// <summary>
    /// Update existing custom template
    /// </summary>
    public async Task<CustomVideoTemplate?> UpdateCustomTemplateAsync(string id, UpdateCustomTemplateRequest request)
    {
        try
        {
            var entity = await _context.CustomTemplates.FindAsync(id);
            if (entity == null)
            {
                return null;
            }

            entity.Name = request.Name;
            entity.Description = request.Description;
            entity.Category = request.Category;
            entity.Tags = string.Join(",", request.Tags);
            entity.UpdatedAt = DateTime.UtcNow;
            entity.ScriptStructureJson = JsonSerializer.Serialize(request.ScriptStructure);
            entity.VideoStructureJson = JsonSerializer.Serialize(request.VideoStructure);
            entity.LLMPipelineJson = JsonSerializer.Serialize(request.LLMPipeline);
            entity.VisualPreferencesJson = JsonSerializer.Serialize(request.VisualPrefs);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated custom template {TemplateId} - {TemplateName}", entity.Id, entity.Name);

            return MapCustomTemplateToModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update custom template {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Delete custom template
    /// </summary>
    public async Task<bool> DeleteCustomTemplateAsync(string id)
    {
        try
        {
            var entity = await _context.CustomTemplates.FindAsync(id);
            if (entity == null)
            {
                return false;
            }

            _context.CustomTemplates.Remove(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted custom template {TemplateId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete custom template {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Duplicate custom template
    /// </summary>
    public async Task<CustomVideoTemplate> DuplicateCustomTemplateAsync(string id)
    {
        try
        {
            var original = await _context.CustomTemplates.FindAsync(id);
            if (original == null)
            {
                throw new ArgumentException($"Template {id} not found", nameof(id));
            }

            var duplicate = new CustomTemplateEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"{original.Name} (Copy)",
                Description = original.Description,
                Category = original.Category,
                Tags = original.Tags,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Author = "User",
                IsDefault = false,
                ScriptStructureJson = original.ScriptStructureJson,
                VideoStructureJson = original.VideoStructureJson,
                LLMPipelineJson = original.LLMPipelineJson,
                VisualPreferencesJson = original.VisualPreferencesJson
            };

            _context.CustomTemplates.Add(duplicate);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Duplicated custom template {OriginalId} to {NewId}", id, duplicate.Id);

            return MapCustomTemplateToModel(duplicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to duplicate custom template {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Set default custom template
    /// </summary>
    public async Task<bool> SetDefaultCustomTemplateAsync(string id)
    {
        try
        {
            var allTemplates = await _context.CustomTemplates.ToListAsync();
            
            foreach (var template in allTemplates)
            {
                template.IsDefault = template.Id == id;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Set default custom template to {TemplateId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set default custom template {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Export custom template to JSON
    /// </summary>
    public async Task<TemplateExportData> ExportCustomTemplateAsync(string id)
    {
        try
        {
            var entity = await _context.CustomTemplates.FindAsync(id);
            if (entity == null)
            {
                throw new ArgumentException($"Template {id} not found", nameof(id));
            }

            var template = MapCustomTemplateToModel(entity);

            return new TemplateExportData
            {
                Version = "1.0",
                Template = template,
                ExportedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export custom template {TemplateId}", id);
            throw;
        }
    }

    /// <summary>
    /// Import custom template from JSON
    /// </summary>
    public async Task<CustomVideoTemplate> ImportCustomTemplateAsync(TemplateExportData exportData)
    {
        try
        {
            var template = exportData.Template;
            
            var entity = new CustomTemplateEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = template.Name,
                Description = template.Description,
                Category = template.Category,
                Tags = string.Join(",", template.Tags),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Author = "User",
                IsDefault = false,
                ScriptStructureJson = JsonSerializer.Serialize(template.ScriptStructure),
                VideoStructureJson = JsonSerializer.Serialize(template.VideoStructure),
                LLMPipelineJson = JsonSerializer.Serialize(template.LLMPipeline),
                VisualPreferencesJson = JsonSerializer.Serialize(template.VisualPrefs)
            };

            _context.CustomTemplates.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Imported custom template {TemplateId} - {TemplateName}", entity.Id, entity.Name);

            return MapCustomTemplateToModel(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import custom template");
            throw;
        }
    }

    private CustomVideoTemplate MapCustomTemplateToModel(CustomTemplateEntity entity)
    {
        return new CustomVideoTemplate
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Category = entity.Category,
            Tags = entity.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Author = entity.Author,
            IsDefault = entity.IsDefault,
            ScriptStructure = JsonSerializer.Deserialize<ScriptStructureConfig>(entity.ScriptStructureJson) ?? new(),
            VideoStructure = JsonSerializer.Deserialize<VideoStructureConfig>(entity.VideoStructureJson) ?? new(),
            LLMPipeline = JsonSerializer.Deserialize<LLMPipelineConfig>(entity.LLMPipelineJson) ?? new(),
            VisualPrefs = JsonSerializer.Deserialize<VisualPreferences>(entity.VisualPreferencesJson) ?? new()
        };
    }
}
