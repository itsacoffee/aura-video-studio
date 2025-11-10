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
/// Service for managing video projects with full CRUD operations
/// </summary>
public class ProjectManagementService
{
    private readonly AuraDbContext _dbContext;
    private readonly ILogger<ProjectManagementService> _logger;

    public ProjectManagementService(
        AuraDbContext dbContext,
        ILogger<ProjectManagementService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects with optional filtering and sorting
    /// </summary>
    public async Task<(List<ProjectStateEntity> Projects, int TotalCount)> GetProjectsAsync(
        string? searchQuery = null,
        string? status = null,
        string? category = null,
        List<string>? tags = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string sortBy = "UpdatedAt",
        bool ascending = false,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = _dbContext.ProjectStates
            .Include(p => p.Scenes)
            .Include(p => p.Assets)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(p =>
                p.Title.Contains(searchQuery) ||
                (p.Description != null && p.Description.Contains(searchQuery)));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (tags != null && tags.Any())
        {
            foreach (var tag in tags)
            {
                var tagToMatch = tag;
                query = query.Where(p => p.Tags != null && p.Tags.Contains(tagToMatch));
            }
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt <= toDate.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply sorting
        query = sortBy.ToLowerInvariant() switch
        {
            "title" => ascending ? query.OrderBy(p => p.Title) : query.OrderByDescending(p => p.Title),
            "createdat" => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt),
            "updatedat" => ascending ? query.OrderBy(p => p.UpdatedAt) : query.OrderByDescending(p => p.UpdatedAt),
            "status" => ascending ? query.OrderBy(p => p.Status) : query.OrderByDescending(p => p.Status),
            "category" => ascending ? query.OrderBy(p => p.Category) : query.OrderByDescending(p => p.Category),
            _ => ascending ? query.OrderBy(p => p.UpdatedAt) : query.OrderByDescending(p => p.UpdatedAt)
        };

        // Apply pagination
        var projects = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (projects, totalCount);
    }

    /// <summary>
    /// Get a single project by ID
    /// </summary>
    public async Task<ProjectStateEntity?> GetProjectByIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _dbContext.ProjectStates
            .Include(p => p.Scenes)
            .Include(p => p.Assets)
            .Include(p => p.Checkpoints)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct);
    }

    /// <summary>
    /// Create a new project
    /// </summary>
    public async Task<ProjectStateEntity> CreateProjectAsync(
        string title,
        string? description = null,
        string? category = null,
        List<string>? tags = null,
        string? templateId = null,
        CancellationToken ct = default)
    {
        var project = new ProjectStateEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = description,
            Category = category,
            Tags = tags != null && tags.Any() ? string.Join(",", tags) : null,
            TemplateId = templateId,
            Status = "Draft",
            CurrentWizardStep = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ProjectStates.Add(project);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Created new project: {ProjectId} - {Title}", project.Id, project.Title);

        return project;
    }

    /// <summary>
    /// Update an existing project
    /// </summary>
    public async Task<ProjectStateEntity?> UpdateProjectAsync(
        Guid projectId,
        string? title = null,
        string? description = null,
        string? category = null,
        List<string>? tags = null,
        string? status = null,
        int? currentWizardStep = null,
        string? thumbnailPath = null,
        string? outputFilePath = null,
        double? durationSeconds = null,
        CancellationToken ct = default)
    {
        var project = await _dbContext.ProjectStates.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null)
        {
            _logger.LogWarning("Project not found for update: {ProjectId}", projectId);
            return null;
        }

        // Update fields if provided
        if (title != null) project.Title = title;
        if (description != null) project.Description = description;
        if (category != null) project.Category = category;
        if (tags != null) project.Tags = string.Join(",", tags);
        if (status != null) project.Status = status;
        if (currentWizardStep.HasValue) project.CurrentWizardStep = currentWizardStep.Value;
        if (thumbnailPath != null) project.ThumbnailPath = thumbnailPath;
        if (outputFilePath != null) project.OutputFilePath = outputFilePath;
        if (durationSeconds.HasValue) project.DurationSeconds = durationSeconds.Value;

        project.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Updated project: {ProjectId} - {Title}", project.Id, project.Title);

        return project;
    }

    /// <summary>
    /// Auto-save project data
    /// </summary>
    public async Task<bool> AutoSaveProjectAsync(
        Guid projectId,
        string? briefJson = null,
        string? planSpecJson = null,
        string? voiceSpecJson = null,
        string? renderSpecJson = null,
        CancellationToken ct = default)
    {
        var project = await _dbContext.ProjectStates.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null)
        {
            _logger.LogWarning("Project not found for auto-save: {ProjectId}", projectId);
            return false;
        }

        // Update JSON data if provided
        if (briefJson != null) project.BriefJson = briefJson;
        if (planSpecJson != null) project.PlanSpecJson = planSpecJson;
        if (voiceSpecJson != null) project.VoiceSpecJson = voiceSpecJson;
        if (renderSpecJson != null) project.RenderSpecJson = renderSpecJson;

        project.LastAutoSaveAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogDebug("Auto-saved project: {ProjectId}", project.Id);

        return true;
    }

    /// <summary>
    /// Duplicate an existing project
    /// </summary>
    public async Task<ProjectStateEntity?> DuplicateProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var original = await GetProjectByIdAsync(projectId, ct);
        if (original == null)
        {
            _logger.LogWarning("Project not found for duplication: {ProjectId}", projectId);
            return null;
        }

        var duplicate = new ProjectStateEntity
        {
            Id = Guid.NewGuid(),
            Title = $"{original.Title} (Copy)",
            Description = original.Description,
            Category = original.Category,
            Tags = original.Tags,
            TemplateId = original.TemplateId,
            Status = "Draft",
            CurrentWizardStep = 0,
            BriefJson = original.BriefJson,
            PlanSpecJson = original.PlanSpecJson,
            VoiceSpecJson = original.VoiceSpecJson,
            RenderSpecJson = original.RenderSpecJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ProjectStates.Add(duplicate);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Duplicated project: {OriginalId} -> {DuplicateId}", projectId, duplicate.Id);

        return duplicate;
    }

    /// <summary>
    /// Soft delete a project
    /// </summary>
    public async Task<bool> DeleteProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        var project = await _dbContext.ProjectStates.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project == null)
        {
            _logger.LogWarning("Project not found for deletion: {ProjectId}", projectId);
            return false;
        }

        // Soft delete will be handled by EF Core interceptor
        _dbContext.ProjectStates.Remove(project);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted project: {ProjectId} - {Title}", project.Id, project.Title);

        return true;
    }

    /// <summary>
    /// Bulk delete projects
    /// </summary>
    public async Task<int> BulkDeleteProjectsAsync(List<Guid> projectIds, CancellationToken ct = default)
    {
        var projects = await _dbContext.ProjectStates
            .Where(p => projectIds.Contains(p.Id))
            .ToListAsync(ct);

        if (!projects.Any())
        {
            return 0;
        }

        _dbContext.ProjectStates.RemoveRange(projects);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Bulk deleted {Count} projects", projects.Count);

        return projects.Count;
    }

    /// <summary>
    /// Get unique categories
    /// </summary>
    public async Task<List<string>> GetCategoriesAsync(CancellationToken ct = default)
    {
        return await _dbContext.ProjectStates
            .Where(p => p.Category != null)
            .Select(p => p.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Get unique tags
    /// </summary>
    public async Task<List<string>> GetTagsAsync(CancellationToken ct = default)
    {
        var projectsWithTags = await _dbContext.ProjectStates
            .Where(p => p.Tags != null)
            .Select(p => p.Tags!)
            .ToListAsync(ct);

        var allTags = projectsWithTags
            .SelectMany(tags => tags.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(tag => tag.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct()
            .OrderBy(tag => tag)
            .ToList();

        return allTags;
    }

    /// <summary>
    /// Get project statistics
    /// </summary>
    public async Task<ProjectStatistics> GetStatisticsAsync(CancellationToken ct = default)
    {
        var stats = new ProjectStatistics
        {
            TotalProjects = await _dbContext.ProjectStates.CountAsync(ct),
            DraftProjects = await _dbContext.ProjectStates.CountAsync(p => p.Status == "Draft", ct),
            InProgressProjects = await _dbContext.ProjectStates.CountAsync(p => p.Status == "InProgress", ct),
            CompletedProjects = await _dbContext.ProjectStates.CountAsync(p => p.Status == "Completed", ct),
            FailedProjects = await _dbContext.ProjectStates.CountAsync(p => p.Status == "Failed", ct)
        };

        return stats;
    }
}

/// <summary>
/// Project statistics
/// </summary>
public class ProjectStatistics
{
    public int TotalProjects { get; set; }
    public int DraftProjects { get; set; }
    public int InProgressProjects { get; set; }
    public int CompletedProjects { get; set; }
    public int FailedProjects { get; set; }
}
