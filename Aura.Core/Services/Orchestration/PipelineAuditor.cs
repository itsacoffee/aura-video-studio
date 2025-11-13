using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Orchestration;

/// <summary>
/// Audits the pipeline codebase for placeholder markers and stub logic
/// </summary>
public class PipelineAuditor
{
    private readonly ILogger<PipelineAuditor> _logger;
    private static readonly Regex PlaceholderRegex = new(
        @"//\s*(TODO|FIXME|HACK|WIP|XXX|placeholder|stub|temporary|dummy)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Regex CommentPlaceholderRegex = new(
        @"/\*\s*(TODO|FIXME|HACK|WIP|XXX|placeholder|stub)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public PipelineAuditor(ILogger<PipelineAuditor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Audits pipeline services for placeholder markers and generates a report
    /// </summary>
    public PipelineAuditReport AuditPipeline(string basePath)
    {
        _logger.LogInformation("Starting pipeline audit at: {BasePath}", basePath);
        
        var report = new PipelineAuditReport
        {
            AuditDate = DateTime.UtcNow,
            BasePath = basePath
        };

        var pipelineDirectories = new[]
        {
            Path.Combine(basePath, "Aura.Core", "Orchestrator"),
            Path.Combine(basePath, "Aura.Core", "Services"),
            Path.Combine(basePath, "Aura.Core", "Timeline"),
            Path.Combine(basePath, "Aura.Core", "Validation"),
            Path.Combine(basePath, "Aura.Providers")
        };

        foreach (var directory in pipelineDirectories.Where(Directory.Exists))
        {
            ScanDirectory(directory, basePath, report);
        }

        report.TotalFilesScanned = report.Components.Count;
        report.TotalIssuesFound = report.Components.Sum(c => c.Issues.Count);

        _logger.LogInformation(
            "Pipeline audit complete: {FilesScanned} files scanned, {IssuesFound} issues found",
            report.TotalFilesScanned,
            report.TotalIssuesFound);

        return report;
    }

    private void ScanDirectory(string directory, string basePath, PipelineAuditReport report)
    {
        var csFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csFiles)
        {
            ScanFile(file, basePath, report);
        }
    }

    private void ScanFile(string filePath, string basePath, PipelineAuditReport report)
    {
        try
        {
            var lines = File.ReadAllLines(filePath);
            var issues = new List<PlaceholderIssue>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                
                // Check for single-line comment placeholders
                var match = PlaceholderRegex.Match(line);
                if (match.Success)
                {
                    issues.Add(new PlaceholderIssue
                    {
                        LineNumber = i + 1,
                        Content = line.Trim(),
                        Type = match.Groups[1].Value.ToUpperInvariant(),
                        Severity = GetSeverity(match.Groups[1].Value)
                    });
                }
                
                // Check for block comment placeholders
                var blockMatch = CommentPlaceholderRegex.Match(line);
                if (blockMatch.Success)
                {
                    issues.Add(new PlaceholderIssue
                    {
                        LineNumber = i + 1,
                        Content = line.Trim(),
                        Type = blockMatch.Groups[1].Value.ToUpperInvariant(),
                        Severity = GetSeverity(blockMatch.Groups[1].Value)
                    });
                }
            }

            if (issues.Count > 0)
            {
                var relativePath = Path.GetRelativePath(basePath, filePath);
                report.Components.Add(new ComponentAuditResult
                {
                    ComponentName = Path.GetFileNameWithoutExtension(filePath),
                    FilePath = relativePath,
                    Issues = issues
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to scan file: {FilePath}", filePath);
        }
    }

    private static IssueSeverity GetSeverity(string markerType)
    {
        return markerType.ToUpperInvariant() switch
        {
            "TODO" => IssueSeverity.Low,
            "FIXME" => IssueSeverity.High,
            "HACK" => IssueSeverity.Medium,
            "WIP" => IssueSeverity.High,
            "XXX" => IssueSeverity.High,
            "PLACEHOLDER" => IssueSeverity.High,
            "STUB" => IssueSeverity.High,
            "TEMPORARY" => IssueSeverity.Medium,
            "DUMMY" => IssueSeverity.Medium,
            _ => IssueSeverity.Low
        };
    }
}

/// <summary>
/// Report of pipeline audit results
/// </summary>
public class PipelineAuditReport
{
    public DateTime AuditDate { get; set; }
    public string BasePath { get; set; } = string.Empty;
    public int TotalFilesScanned { get; set; }
    public int TotalIssuesFound { get; set; }
    public List<ComponentAuditResult> Components { get; set; } = new();

    public string GenerateMarkdownReport()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Pipeline Audit Report");
        sb.AppendLine();
        sb.AppendLine($"**Audit Date:** {AuditDate:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Base Path:** {BasePath}");
        sb.AppendLine($"**Files Scanned:** {TotalFilesScanned}");
        sb.AppendLine($"**Issues Found:** {TotalIssuesFound}");
        sb.AppendLine();

        if (TotalIssuesFound == 0)
        {
            sb.AppendLine("✅ **No placeholder markers found in pipeline code.**");
            return sb.ToString();
        }

        sb.AppendLine("## Issues by Component");
        sb.AppendLine();

        var sortedComponents = Components
            .OrderByDescending(c => c.Issues.Count)
            .ThenBy(c => c.ComponentName);

        foreach (var component in sortedComponents)
        {
            sb.AppendLine($"### {component.ComponentName}");
            sb.AppendLine($"**File:** `{component.FilePath}`");
            sb.AppendLine($"**Issue Count:** {component.Issues.Count}");
            sb.AppendLine();

            var groupedIssues = component.Issues
                .GroupBy(i => i.Severity)
                .OrderByDescending(g => g.Key);

            foreach (var group in groupedIssues)
            {
                sb.AppendLine($"#### {group.Key} Priority");
                sb.AppendLine();

                foreach (var issue in group.OrderBy(i => i.LineNumber))
                {
                    sb.AppendLine($"- Line {issue.LineNumber} (`{issue.Type}`): {issue.Content}");
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Summary by Severity");
        sb.AppendLine();

        var severityCounts = Components
            .SelectMany(c => c.Issues)
            .GroupBy(i => i.Severity)
            .OrderByDescending(g => g.Key)
            .Select(g => new { Severity = g.Key, Count = g.Count() });

        foreach (var item in severityCounts)
        {
            sb.AppendLine($"- **{item.Severity}:** {item.Count}");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Audit result for a single component
/// </summary>
public class ComponentAuditResult
{
    public string ComponentName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public List<PlaceholderIssue> Issues { get; set; } = new();
}

/// <summary>
/// A single placeholder or stub issue found in code
/// </summary>
public class PlaceholderIssue
{
    public int LineNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public IssueSeverity Severity { get; set; }
}

/// <summary>
/// Severity level of an issue
/// </summary>
public enum IssueSeverity
{
    Low = 1,
    Medium = 2,
    High = 3
}
