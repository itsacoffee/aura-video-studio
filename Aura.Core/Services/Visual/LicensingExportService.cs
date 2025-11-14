using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Visual;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service for exporting licensing information
/// </summary>
public class LicensingExportService
{
    private readonly ILogger<LicensingExportService> _logger;

    public LicensingExportService(ILogger<LicensingExportService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Export licensing information to CSV format
    /// </summary>
    public async Task<string> ExportToCsvAsync(
        IReadOnlyList<SceneVisualSelection> selections,
        string jobId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting licensing info to CSV for job {JobId}, {Count} scenes", jobId, selections.Count);

        var sb = new StringBuilder();

        sb.AppendLine("Scene,ImageUrl,Source,LicenseType,CommercialUse,AttributionRequired,Creator,CreatorUrl,SourcePlatform,LicenseUrl,Attribution");

        foreach (var selection in selections.OrderBy(s => s.SceneIndex))
        {
            if (selection.SelectedCandidate?.Licensing == null)
            {
                continue;
            }

            var candidate = selection.SelectedCandidate;
            var licensing = candidate.Licensing;

            sb.AppendLine(string.Join(",",
                EscapeCsv(selection.SceneIndex.ToString()),
                EscapeCsv(candidate.ImageUrl),
                EscapeCsv(candidate.Source),
                EscapeCsv(licensing.LicenseType),
                EscapeCsv(licensing.CommercialUseAllowed.ToString()),
                EscapeCsv(licensing.AttributionRequired.ToString()),
                EscapeCsv(licensing.CreatorName ?? string.Empty),
                EscapeCsv(licensing.CreatorUrl ?? string.Empty),
                EscapeCsv(licensing.SourcePlatform),
                EscapeCsv(licensing.LicenseUrl ?? string.Empty),
                EscapeCsv(licensing.Attribution ?? string.Empty)
            ));
        }

        return await Task.FromResult(sb.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Export licensing information to JSON format
    /// </summary>
    public async Task<string> ExportToJsonAsync(
        IReadOnlyList<SceneVisualSelection> selections,
        string jobId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Exporting licensing info to JSON for job {JobId}, {Count} scenes", jobId, selections.Count);

        var exportData = new
        {
            jobId,
            exportedAt = DateTime.UtcNow,
            sceneCount = selections.Count,
            scenes = selections
                .Where(s => s.SelectedCandidate?.Licensing != null)
                .OrderBy(s => s.SceneIndex)
                .Select(s => new
                {
                    sceneIndex = s.SceneIndex,
                    image = new
                    {
                        url = s.SelectedCandidate!.ImageUrl,
                        source = s.SelectedCandidate.Source,
                        width = s.SelectedCandidate.Width,
                        height = s.SelectedCandidate.Height,
                        score = s.SelectedCandidate.OverallScore
                    },
                    licensing = new
                    {
                        licenseType = s.SelectedCandidate.Licensing!.LicenseType,
                        commercialUseAllowed = s.SelectedCandidate.Licensing.CommercialUseAllowed,
                        attributionRequired = s.SelectedCandidate.Licensing.AttributionRequired,
                        creator = new
                        {
                            name = s.SelectedCandidate.Licensing.CreatorName,
                            url = s.SelectedCandidate.Licensing.CreatorUrl
                        },
                        sourcePlatform = s.SelectedCandidate.Licensing.SourcePlatform,
                        licenseUrl = s.SelectedCandidate.Licensing.LicenseUrl,
                        attribution = s.SelectedCandidate.Licensing.Attribution
                    },
                    selection = new
                    {
                        selectedAt = s.SelectedAt,
                        selectedBy = s.SelectedBy,
                        state = s.State.ToString(),
                        rejectionReason = s.RejectionReason
                    }
                })
                .ToList()
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return await Task.FromResult(json).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate attribution text for video credits
    /// </summary>
    public async Task<string> GenerateAttributionTextAsync(
        IReadOnlyList<SceneVisualSelection> selections,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating attribution text for {Count} scenes", selections.Count);

        var sb = new StringBuilder();
        sb.AppendLine("IMAGE CREDITS");
        sb.AppendLine();

        var attributions = selections
            .Where(s => s.SelectedCandidate?.Licensing?.AttributionRequired == true)
            .OrderBy(s => s.SceneIndex)
            .ToList();

        if (attributions.Count == 0)
        {
            sb.AppendLine("No attribution required for images used in this video.");
        }
        else
        {
            foreach (var selection in attributions)
            {
                var licensing = selection.SelectedCandidate!.Licensing!;

                if (!string.IsNullOrWhiteSpace(licensing.Attribution))
                {
                    sb.AppendLine($"Scene {selection.SceneIndex}: {licensing.Attribution}");
                }
                else if (!string.IsNullOrWhiteSpace(licensing.CreatorName))
                {
                    var attribution = $"Scene {selection.SceneIndex}: Photo by {licensing.CreatorName}";
                    if (!string.IsNullOrWhiteSpace(licensing.SourcePlatform))
                    {
                        attribution += $" on {licensing.SourcePlatform}";
                    }
                    sb.AppendLine(attribution);
                }
            }
        }

        return await Task.FromResult(sb.ToString()).ConfigureAwait(false);
    }

    /// <summary>
    /// Generate licensing summary for review
    /// </summary>
    public async Task<LicensingSummary> GenerateSummaryAsync(
        IReadOnlyList<SceneVisualSelection> selections,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Generating licensing summary for {Count} scenes", selections.Count);

        var selectedScenes = selections.Where(s => s.SelectedCandidate != null).ToList();

        var commercialOk = selectedScenes.All(s => s.SelectedCandidate!.Licensing?.CommercialUseAllowed != false);
        var requiresAttribution = selectedScenes.Any(s => s.SelectedCandidate!.Licensing?.AttributionRequired == true);

        var licenseTypes = selectedScenes
            .Where(s => s.SelectedCandidate!.Licensing != null)
            .GroupBy(s => s.SelectedCandidate!.Licensing!.LicenseType)
            .Select(g => new LicenseTypeCount
            {
                LicenseType = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(l => l.Count)
            .ToList();

        var sources = selectedScenes
            .Where(s => s.SelectedCandidate!.Licensing != null)
            .GroupBy(s => s.SelectedCandidate!.Licensing!.SourcePlatform)
            .Select(g => new SourceCount
            {
                Source = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(s => s.Count)
            .ToList();

        var warnings = new List<string>();

        if (!commercialOk)
        {
            warnings.Add("Some images may not allow commercial use. Review licensing terms.");
        }

        if (requiresAttribution)
        {
            var count = selectedScenes.Count(s => s.SelectedCandidate!.Licensing?.AttributionRequired == true);
            warnings.Add($"{count} image(s) require attribution in video credits.");
        }

        var unlicensedCount = selectedScenes.Count(s => s.SelectedCandidate!.Licensing == null);
        if (unlicensedCount > 0)
        {
            warnings.Add($"{unlicensedCount} image(s) have no licensing information.");
        }

        return await Task.FromResult(new LicensingSummary
        {
            TotalScenes = selections.Count,
            ScenesWithSelection = selectedScenes.Count,
            CommercialUseAllowed = commercialOk,
            RequiresAttribution = requiresAttribution,
            LicenseTypes = licenseTypes,
            Sources = sources,
            Warnings = warnings
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Validate licensing for commercial use
    /// </summary>
    public async Task<LicensingValidationResult> ValidateForCommercialUseAsync(
        IReadOnlyList<SceneVisualSelection> selections,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Validating licensing for commercial use, {Count} scenes", selections.Count);

        var issues = new List<LicensingIssue>();

        foreach (var selection in selections)
        {
            if (selection.SelectedCandidate == null)
            {
                issues.Add(new LicensingIssue
                {
                    SceneIndex = selection.SceneIndex,
                    Severity = IssueSeverity.Warning,
                    Message = "No image selected for this scene"
                });
                continue;
            }

            var licensing = selection.SelectedCandidate.Licensing;

            if (licensing == null)
            {
                issues.Add(new LicensingIssue
                {
                    SceneIndex = selection.SceneIndex,
                    Severity = IssueSeverity.Error,
                    Message = "No licensing information available",
                    ImageUrl = selection.SelectedCandidate.ImageUrl
                });
                continue;
            }

            if (!licensing.CommercialUseAllowed)
            {
                issues.Add(new LicensingIssue
                {
                    SceneIndex = selection.SceneIndex,
                    Severity = IssueSeverity.Error,
                    Message = "Commercial use not allowed",
                    ImageUrl = selection.SelectedCandidate.ImageUrl,
                    LicenseType = licensing.LicenseType
                });
            }

            if (licensing.AttributionRequired && string.IsNullOrWhiteSpace(licensing.Attribution))
            {
                issues.Add(new LicensingIssue
                {
                    SceneIndex = selection.SceneIndex,
                    Severity = IssueSeverity.Warning,
                    Message = "Attribution required but text not generated",
                    ImageUrl = selection.SelectedCandidate.ImageUrl,
                    LicenseType = licensing.LicenseType
                });
            }
        }

        var isValid = !issues.Any(i => i.Severity == IssueSeverity.Error);

        return await Task.FromResult(new LicensingValidationResult
        {
            IsValid = isValid,
            Issues = issues,
            Summary = $"Found {issues.Count(i => i.Severity == IssueSeverity.Error)} error(s) and {issues.Count(i => i.Severity == IssueSeverity.Warning)} warning(s)"
        }).ConfigureAwait(false);
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}

/// <summary>
/// Summary of licensing information
/// </summary>
public record LicensingSummary
{
    public int TotalScenes { get; init; }
    public int ScenesWithSelection { get; init; }
    public bool CommercialUseAllowed { get; init; }
    public bool RequiresAttribution { get; init; }
    public IReadOnlyList<LicenseTypeCount> LicenseTypes { get; init; } = Array.Empty<LicenseTypeCount>();
    public IReadOnlyList<SourceCount> Sources { get; init; } = Array.Empty<SourceCount>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

public record LicenseTypeCount
{
    public string LicenseType { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record SourceCount
{
    public string Source { get; init; } = string.Empty;
    public int Count { get; init; }
}

public record LicensingValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<LicensingIssue> Issues { get; init; } = Array.Empty<LicensingIssue>();
    public string Summary { get; init; } = string.Empty;
}

public record LicensingIssue
{
    public int SceneIndex { get; init; }
    public IssueSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? LicenseType { get; init; }
}

public enum IssueSeverity
{
    Warning,
    Error
}
