using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.PromptManagement;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.PromptManagement;

/// <summary>
/// Service for tracking and analyzing prompt template performance
/// </summary>
public class PromptAnalyticsService
{
    private readonly ILogger<PromptAnalyticsService> _logger;
    private readonly IPromptRepository _repository;

    public PromptAnalyticsService(
        ILogger<PromptAnalyticsService> logger,
        IPromptRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// Track usage of a template
    /// </summary>
    public async Task TrackUsageAsync(string templateId, CancellationToken ct = default)
    {
        var template = await _repository.GetByIdAsync(templateId, ct);
        if (template == null)
            return;

        template.Metrics.UsageCount++;
        template.Metrics.LastUsedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(template, ct);

        _logger.LogDebug("Tracked usage for template {TemplateId}", templateId);
    }

    /// <summary>
    /// Record feedback on template performance
    /// </summary>
    public async Task RecordFeedbackAsync(
        string templateId,
        bool thumbsUp,
        double? qualityScore = null,
        double? generationTimeMs = null,
        int? tokenUsage = null,
        CancellationToken ct = default)
    {
        var template = await _repository.GetByIdAsync(templateId, ct);
        if (template == null)
            return;

        if (thumbsUp)
            template.Metrics.ThumbsUpCount++;
        else
            template.Metrics.ThumbsDownCount++;

        if (qualityScore.HasValue)
        {
            var currentTotal = template.Metrics.AverageQualityScore * template.Metrics.UsageCount;
            template.Metrics.AverageQualityScore = 
                (currentTotal + qualityScore.Value) / (template.Metrics.UsageCount + 1);
        }

        if (generationTimeMs.HasValue)
        {
            var currentTotal = template.Metrics.AverageGenerationTimeMs * template.Metrics.UsageCount;
            template.Metrics.AverageGenerationTimeMs = 
                (currentTotal + generationTimeMs.Value) / (template.Metrics.UsageCount + 1);
        }

        if (tokenUsage.HasValue)
        {
            var currentTotal = template.Metrics.AverageTokenUsage * template.Metrics.UsageCount;
            template.Metrics.AverageTokenUsage = 
                (int)((currentTotal + tokenUsage.Value) / (template.Metrics.UsageCount + 1));
        }

        var totalFeedback = template.Metrics.ThumbsUpCount + template.Metrics.ThumbsDownCount;
        template.Metrics.SuccessRate = totalFeedback > 0
            ? (double)template.Metrics.ThumbsUpCount / totalFeedback
            : 1.0;

        await _repository.UpdateAsync(template, ct);

        _logger.LogInformation("Recorded feedback for template {TemplateId}: {ThumbsUp}, Score: {Score}",
            templateId, thumbsUp, qualityScore);
    }

    /// <summary>
    /// Get analytics for templates
    /// </summary>
    public async Task<PromptAnalytics> GetAnalyticsAsync(
        PromptAnalyticsQuery query,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting analytics for date range {Start} to {End}",
            query.StartDate, query.EndDate);

        var templates = await _repository.ListAsync(
            category: query.Category,
            stage: query.Stage,
            source: query.Source,
            createdBy: query.CreatedBy,
            ct: ct);

        if (query.StartDate.HasValue || query.EndDate.HasValue)
        {
            templates = templates.Where(t =>
            {
                if (query.StartDate.HasValue && t.CreatedAt < query.StartDate.Value)
                    return false;
                if (query.EndDate.HasValue && t.CreatedAt > query.EndDate.Value)
                    return false;
                return true;
            }).ToList();
        }

        var analytics = new PromptAnalytics
        {
            TotalTemplates = templates.Count,
            ActiveTemplates = templates.Count(t => t.Status == TemplateStatus.Active),
            TotalUsages = templates.Sum(t => t.Metrics.UsageCount),
            AverageQualityScore = templates.Any()
                ? templates.Average(t => t.Metrics.AverageQualityScore)
                : 0,
            AverageSuccessRate = templates.Any()
                ? templates.Average(t => t.Metrics.SuccessRate)
                : 1.0,
            TopPerformingTemplates = templates
                .OrderByDescending(t => t.Metrics.AverageQualityScore)
                .Take(query.Top)
                .Select(MapToStats)
                .ToList(),
            MostUsedTemplates = templates
                .OrderByDescending(t => t.Metrics.UsageCount)
                .Take(query.Top)
                .Select(MapToStats)
                .ToList(),
            TemplatesByCategory = templates
                .GroupBy(t => t.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            AverageScoresByStage = templates
                .Where(t => t.Metrics.UsageCount > 0)
                .GroupBy(t => t.Stage)
                .ToDictionary(
                    g => g.Key,
                    g => g.Average(t => t.Metrics.AverageQualityScore))
        };

        return analytics;
    }

    /// <summary>
    /// Get performance comparison between templates
    /// </summary>
    public async Task<List<TemplateUsageStats>> CompareTemplatesAsync(
        List<string> templateIds,
        CancellationToken ct = default)
    {
        var stats = new List<TemplateUsageStats>();

        foreach (var templateId in templateIds)
        {
            var template = await _repository.GetByIdAsync(templateId, ct);
            if (template != null)
            {
                stats.Add(MapToStats(template));
            }
        }

        return stats;
    }

    /// <summary>
    /// Map template to usage stats
    /// </summary>
    private TemplateUsageStats MapToStats(PromptTemplate template)
    {
        return new TemplateUsageStats
        {
            TemplateId = template.Id,
            TemplateName = template.Name,
            UsageCount = template.Metrics.UsageCount,
            QualityScore = template.Metrics.AverageQualityScore,
            SuccessRate = template.Metrics.SuccessRate,
            TokenUsage = template.Metrics.AverageTokenUsage
        };
    }
}
