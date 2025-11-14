using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PromptManagement;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PromptManagement;

/// <summary>
/// Comprehensive service for managing prompt templates with CRUD operations,
/// versioning, analytics, and testing capabilities
/// </summary>
public class PromptManagementService
{
    private readonly ILogger<PromptManagementService> _logger;
    private readonly IPromptRepository _repository;
    private readonly PromptVariableResolver _variableResolver;
    private readonly PromptValidator _validator;
    private readonly PromptAnalyticsService _analytics;

    public PromptManagementService(
        ILogger<PromptManagementService> logger,
        IPromptRepository repository,
        PromptVariableResolver variableResolver,
        PromptValidator validator,
        PromptAnalyticsService analytics)
    {
        _logger = logger;
        _repository = repository;
        _variableResolver = variableResolver;
        _validator = validator;
        _analytics = analytics;
    }

    /// <summary>
    /// Create a new prompt template
    /// </summary>
    public async Task<PromptTemplate> CreateTemplateAsync(
        PromptTemplate template,
        string createdBy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Creating new prompt template: {Name} by {User}", 
            template.Name, createdBy);

        var validationResult = await _validator.ValidateTemplateAsync(template, ct).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"Template validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        template.Id = Guid.NewGuid().ToString();
        template.CreatedBy = createdBy;
        template.CreatedAt = DateTime.UtcNow;
        template.Version = 1;
        template.Status = TemplateStatus.Active;

        await _repository.CreateAsync(template, ct).ConfigureAwait(false);

        await CreateVersionHistoryAsync(template, "Initial creation", createdBy, ct).ConfigureAwait(false);

        _logger.LogInformation("Created prompt template {Id}: {Name}", template.Id, template.Name);
        return template;
    }

    /// <summary>
    /// Get a prompt template by ID
    /// </summary>
    public async Task<PromptTemplate?> GetTemplateAsync(string templateId, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(templateId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// List prompt templates with optional filtering
    /// </summary>
    public async Task<List<PromptTemplate>> ListTemplatesAsync(
        PromptCategory? category = null,
        PipelineStage? stage = null,
        TemplateSource? source = null,
        TemplateStatus? status = null,
        string? createdBy = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Listing templates with filters - Category: {Category}, Stage: {Stage}, Source: {Source}",
            category, stage, source);

        return await _repository.ListAsync(
            category, stage, source, status, createdBy, searchTerm, skip, take, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Update an existing prompt template
    /// </summary>
    public async Task<PromptTemplate> UpdateTemplateAsync(
        string templateId,
        PromptTemplate updates,
        string modifiedBy,
        string changeNotes,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Updating prompt template {Id} by {User}", templateId, modifiedBy);

        var existing = await _repository.GetByIdAsync(templateId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            throw new ArgumentException($"Template {templateId} not found");
        }

        if (existing.Source == TemplateSource.System)
        {
            throw new InvalidOperationException(
                "Cannot modify system templates. Clone the template first.");
        }

        var validationResult = await _validator.ValidateTemplateAsync(updates, ct).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new ArgumentException(
                $"Template validation failed: {string.Join(", ", validationResult.Errors)}");
        }

        existing.Name = updates.Name;
        existing.Description = updates.Description;
        existing.PromptText = updates.PromptText;
        existing.Variables = updates.Variables;
        existing.Tags = updates.Tags;
        existing.Status = updates.Status;
        existing.ModifiedBy = modifiedBy;
        existing.ModifiedAt = DateTime.UtcNow;
        existing.Version++;

        await _repository.UpdateAsync(existing, ct).ConfigureAwait(false);

        await CreateVersionHistoryAsync(existing, changeNotes, modifiedBy, ct).ConfigureAwait(false);

        _logger.LogInformation("Updated prompt template {Id} to version {Version}", 
            templateId, existing.Version);

        return existing;
    }

    /// <summary>
    /// Delete a prompt template
    /// </summary>
    public async Task DeleteTemplateAsync(string templateId, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting prompt template {Id}", templateId);

        var existing = await _repository.GetByIdAsync(templateId, ct).ConfigureAwait(false);
        if (existing == null)
        {
            throw new ArgumentException($"Template {templateId} not found");
        }

        if (existing.Source == TemplateSource.System)
        {
            throw new InvalidOperationException("Cannot delete system templates");
        }

        await _repository.DeleteAsync(templateId, ct).ConfigureAwait(false);

        _logger.LogInformation("Deleted prompt template {Id}", templateId);
    }

    /// <summary>
    /// Clone a template (useful for customizing system templates)
    /// </summary>
    public async Task<PromptTemplate> CloneTemplateAsync(
        string sourceTemplateId,
        string clonedBy,
        string? newName = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Cloning prompt template {SourceId} by {User}", 
            sourceTemplateId, clonedBy);

        var source = await _repository.GetByIdAsync(sourceTemplateId, ct).ConfigureAwait(false);
        if (source == null)
        {
            throw new ArgumentException($"Source template {sourceTemplateId} not found");
        }

        var cloned = new PromptTemplate
        {
            Id = Guid.NewGuid().ToString(),
            Name = newName ?? $"{source.Name} (Copy)",
            Description = source.Description,
            PromptText = source.PromptText,
            Category = source.Category,
            Stage = source.Stage,
            Source = TemplateSource.Cloned,
            TargetProvider = source.TargetProvider,
            Status = TemplateStatus.Active,
            Variables = new List<PromptVariable>(source.Variables),
            Tags = new List<string>(source.Tags),
            CreatedBy = clonedBy,
            CreatedAt = DateTime.UtcNow,
            Version = 1,
            ParentTemplateId = sourceTemplateId,
            IsDefault = false,
            Metadata = new Dictionary<string, string>(source.Metadata)
        };

        await _repository.CreateAsync(cloned, ct).ConfigureAwait(false);

        await CreateVersionHistoryAsync(cloned, $"Cloned from {source.Name}", clonedBy, ct).ConfigureAwait(false);

        _logger.LogInformation("Cloned template {SourceId} to {NewId}", sourceTemplateId, cloned.Id);
        return cloned;
    }

    /// <summary>
    /// Get version history for a template
    /// </summary>
    public async Task<List<PromptTemplateVersion>> GetVersionHistoryAsync(
        string templateId,
        CancellationToken ct = default)
    {
        return await _repository.GetVersionHistoryAsync(templateId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Rollback template to a previous version
    /// </summary>
    public async Task<PromptTemplate> RollbackTemplateAsync(
        string templateId,
        int targetVersion,
        string modifiedBy,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Rolling back template {Id} to version {Version} by {User}",
            templateId, targetVersion, modifiedBy);

        var template = await _repository.GetByIdAsync(templateId, ct).ConfigureAwait(false);
        if (template == null)
        {
            throw new ArgumentException($"Template {templateId} not found");
        }

        if (template.Source == TemplateSource.System)
        {
            throw new InvalidOperationException("Cannot rollback system templates");
        }

        var versions = await _repository.GetVersionHistoryAsync(templateId, ct).ConfigureAwait(false);
        var targetVersionData = versions.FirstOrDefault(v => v.VersionNumber == targetVersion);
        if (targetVersionData == null)
        {
            throw new ArgumentException($"Version {targetVersion} not found for template {templateId}");
        }

        template.PromptText = targetVersionData.PromptText;
        template.Variables = targetVersionData.Variables;
        template.ModifiedBy = modifiedBy;
        template.ModifiedAt = DateTime.UtcNow;
        template.Version++;

        await _repository.UpdateAsync(template, ct).ConfigureAwait(false);

        await CreateVersionHistoryAsync(
            template, 
            $"Rolled back to version {targetVersion}", 
            modifiedBy, 
            ct).ConfigureAwait(false);

        _logger.LogInformation("Rolled back template {Id} to version {Version}", 
            templateId, targetVersion);

        return template;
    }

    /// <summary>
    /// Resolve variables in a template with provided values
    /// </summary>
    public async Task<string> ResolveTemplateAsync(
        string templateId,
        Dictionary<string, object> variables,
        VariableResolverOptions? options = null,
        CancellationToken ct = default)
    {
        var template = await _repository.GetByIdAsync(templateId, ct).ConfigureAwait(false);
        if (template == null)
        {
            throw new ArgumentException($"Template {templateId} not found");
        }

        return await _variableResolver.ResolveAsync(
            template.PromptText,
            template.Variables,
            variables,
            options ?? new VariableResolverOptions(),
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the appropriate template for a pipeline stage with fallback logic
    /// </summary>
    public async Task<PromptTemplate> GetTemplateForStageAsync(
        PipelineStage stage,
        string? userId = null,
        TargetLlmProvider? preferredProvider = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting template for stage {Stage}, user {UserId}", stage, userId);

        PromptTemplate? template = null;

        if (!string.IsNullOrEmpty(userId))
        {
            var userTemplates = await ListTemplatesAsync(
                stage: stage,
                source: TemplateSource.User,
                status: TemplateStatus.Active,
                createdBy: userId,
                ct: ct).ConfigureAwait(false);

            template = userTemplates
                .Where(t => t.IsDefault)
                .FirstOrDefault();
        }

        if (template == null)
        {
            var systemTemplates = await ListTemplatesAsync(
                stage: stage,
                source: TemplateSource.System,
                status: TemplateStatus.Active,
                ct: ct).ConfigureAwait(false);

            template = systemTemplates
                .Where(t => preferredProvider == null || 
                            t.TargetProvider == TargetLlmProvider.Any || 
                            t.TargetProvider == preferredProvider)
                .OrderByDescending(t => t.IsDefault)
                .FirstOrDefault();
        }

        if (template == null)
        {
            throw new InvalidOperationException(
                $"No template found for stage {stage}");
        }

        await _analytics.TrackUsageAsync(template.Id, ct).ConfigureAwait(false);

        return template;
    }

    /// <summary>
    /// Record feedback on a template's performance
    /// </summary>
    public async Task RecordFeedbackAsync(
        string templateId,
        bool thumbsUp,
        double? qualityScore = null,
        double? generationTimeMs = null,
        int? tokenUsage = null,
        CancellationToken ct = default)
    {
        await _analytics.RecordFeedbackAsync(
            templateId, thumbsUp, qualityScore, generationTimeMs, tokenUsage, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Create version history entry
    /// </summary>
    private async Task CreateVersionHistoryAsync(
        PromptTemplate template,
        string changeNotes,
        string changedBy,
        CancellationToken ct)
    {
        var version = new PromptTemplateVersion
        {
            TemplateId = template.Id,
            VersionNumber = template.Version,
            PromptText = template.PromptText,
            ChangeNotes = changeNotes,
            ChangedBy = changedBy,
            ChangedAt = DateTime.UtcNow,
            Variables = new List<PromptVariable>(template.Variables),
            Metadata = new Dictionary<string, string>(template.Metadata)
        };

        await _repository.CreateVersionAsync(version, ct).ConfigureAwait(false);
    }
}
