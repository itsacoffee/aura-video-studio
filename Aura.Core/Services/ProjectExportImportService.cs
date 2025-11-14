using Aura.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Aura.Core.Services;

/// <summary>
/// Service for exporting and importing projects
/// </summary>
public class ProjectExportImportService
{
    private readonly AuraDbContext _dbContext;
    private readonly ILogger<ProjectExportImportService> _logger;

    public ProjectExportImportService(
        AuraDbContext dbContext,
        ILogger<ProjectExportImportService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Export a project to a JSON file
    /// </summary>
    public async Task<string> ExportProjectAsync(Guid projectId, string outputPath, CancellationToken ct = default)
    {
        var project = await _dbContext.ProjectStates
            .Include(p => p.Scenes)
            .Include(p => p.Assets)
            .Include(p => p.Checkpoints)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct).ConfigureAwait(false);

        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        var exportData = new ProjectExportData
        {
            Version = "1.0",
            ExportedAt = DateTime.UtcNow,
            Project = new ExportedProject
            {
                Title = project.Title,
                Description = project.Description,
                Category = project.Category,
                Tags = project.Tags,
                TemplateId = project.TemplateId,
                BriefJson = project.BriefJson,
                PlanSpecJson = project.PlanSpecJson,
                VoiceSpecJson = project.VoiceSpecJson,
                RenderSpecJson = project.RenderSpecJson,
                Scenes = project.Scenes.Select(s => new ExportedScene
                {
                    SceneIndex = s.SceneIndex,
                    ScriptText = s.ScriptText,
                    DurationSeconds = s.DurationSeconds
                }).ToList()
            }
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(outputPath, json, ct).ConfigureAwait(false);

        _logger.LogInformation("Exported project {ProjectId} to {Path}", projectId, outputPath);

        return outputPath;
    }

    /// <summary>
    /// Export a project as a package (ZIP file with all assets)
    /// </summary>
    public async Task<string> ExportProjectPackageAsync(
        Guid projectId,
        string outputPath,
        bool includeAssets = true,
        CancellationToken ct = default)
    {
        var project = await _dbContext.ProjectStates
            .Include(p => p.Scenes)
            .Include(p => p.Assets)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct).ConfigureAwait(false);

        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"aura_export_{projectId}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Export project metadata
            var metadataPath = Path.Combine(tempDir, "project.json");
            await ExportProjectAsync(projectId, metadataPath, ct).ConfigureAwait(false);

            // Copy assets if requested
            if (includeAssets)
            {
                var assetsDir = Path.Combine(tempDir, "assets");
                Directory.CreateDirectory(assetsDir);

                foreach (var asset in project.Assets.Where(a => File.Exists(a.FilePath)))
                {
                    var fileName = Path.GetFileName(asset.FilePath);
                    var destPath = Path.Combine(assetsDir, fileName);
                    File.Copy(asset.FilePath, destPath, true);
                }

                // Copy thumbnail if exists
                if (!string.IsNullOrEmpty(project.ThumbnailPath) && File.Exists(project.ThumbnailPath))
                {
                    var thumbName = Path.GetFileName(project.ThumbnailPath);
                    var thumbDest = Path.Combine(tempDir, thumbName);
                    File.Copy(project.ThumbnailPath, thumbDest, true);
                }
            }

            // Create ZIP package
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            ZipFile.CreateFromDirectory(tempDir, outputPath);

            _logger.LogInformation("Exported project package {ProjectId} to {Path}", projectId, outputPath);

            return outputPath;
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    /// <summary>
    /// Import a project from a JSON file
    /// </summary>
    public async Task<ProjectStateEntity> ImportProjectAsync(
        string filePath,
        string? newTitle = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Import file not found: {filePath}");
        }

        var json = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
        var exportData = JsonSerializer.Deserialize<ProjectExportData>(json);

        if (exportData == null || exportData.Project == null)
        {
            throw new InvalidOperationException("Invalid project export data");
        }

        var project = new ProjectStateEntity
        {
            Id = Guid.NewGuid(),
            Title = newTitle ?? $"{exportData.Project.Title} (Imported)",
            Description = exportData.Project.Description,
            Category = exportData.Project.Category,
            Tags = exportData.Project.Tags,
            TemplateId = exportData.Project.TemplateId,
            Status = "Draft",
            CurrentWizardStep = 0,
            BriefJson = exportData.Project.BriefJson,
            PlanSpecJson = exportData.Project.PlanSpecJson,
            VoiceSpecJson = exportData.Project.VoiceSpecJson,
            RenderSpecJson = exportData.Project.RenderSpecJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ProjectStates.Add(project);

        // Import scenes
        foreach (var exportedScene in exportData.Project.Scenes)
        {
            var scene = new SceneStateEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                SceneIndex = exportedScene.SceneIndex,
                ScriptText = exportedScene.ScriptText,
                DurationSeconds = exportedScene.DurationSeconds,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.SceneStates.Add(scene);
        }

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Imported project from {Path}: {ProjectId} - {Title}",
            filePath, project.Id, project.Title);

        return project;
    }

    /// <summary>
    /// Import a project package (ZIP file)
    /// </summary>
    public async Task<ProjectStateEntity> ImportProjectPackageAsync(
        string packagePath,
        string? newTitle = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(packagePath))
        {
            throw new FileNotFoundException($"Package file not found: {packagePath}");
        }

        var tempDir = Path.Combine(Path.GetTempPath(), $"aura_import_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Extract package
            ZipFile.ExtractToDirectory(packagePath, tempDir);

            // Import project
            var metadataPath = Path.Combine(tempDir, "project.json");
            var project = await ImportProjectAsync(metadataPath, newTitle, ct).ConfigureAwait(false);

            // TODO: Copy assets to project directory if needed

            _logger.LogInformation("Imported project package from {Path}: {ProjectId}",
                packagePath, project.Id);

            return project;
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }
}

/// <summary>
/// Project export data structure
/// </summary>
public class ProjectExportData
{
    public string Version { get; set; } = string.Empty;
    public DateTime ExportedAt { get; set; }
    public ExportedProject Project { get; set; } = new();
}

/// <summary>
/// Exported project data
/// </summary>
public class ExportedProject
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Tags { get; set; }
    public string? TemplateId { get; set; }
    public string? BriefJson { get; set; }
    public string? PlanSpecJson { get; set; }
    public string? VoiceSpecJson { get; set; }
    public string? RenderSpecJson { get; set; }
    public List<ExportedScene> Scenes { get; set; } = new();
}

/// <summary>
/// Exported scene data
/// </summary>
public class ExportedScene
{
    public int SceneIndex { get; set; }
    public string ScriptText { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
}
