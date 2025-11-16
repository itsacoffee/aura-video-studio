using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models.Storage;
using Aura.Core.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service for exporting and importing projects
/// </summary>
public class ProjectExportImportService
{
    private readonly AuraDbContext _dbContext;
    private readonly ILogger<ProjectExportImportService> _logger;
    private readonly IEnhancedLocalStorageService _storageService;

    public ProjectExportImportService(
        AuraDbContext dbContext,
        ILogger<ProjectExportImportService> logger,
        IEnhancedLocalStorageService storageService)
    {
        _dbContext = dbContext;
        _logger = logger;
        _storageService = storageService;
    }

    private async Task<ProjectStateEntity> LoadProjectGraphAsync(Guid projectId, CancellationToken ct)
    {
        var project = await _dbContext.ProjectStates
            .Include(p => p.Scenes)
            .Include(p => p.Assets)
            .Include(p => p.Checkpoints)
            .FirstOrDefaultAsync(p => p.Id == projectId, ct)
            .ConfigureAwait(false);

        if (project == null)
        {
            throw new InvalidOperationException($"Project {projectId} not found");
        }

        return project;
    }

    private static ProjectExportData BuildExportData(
        ProjectStateEntity project,
        Func<AssetStateEntity, string?>? packagePathResolver,
        string? thumbnailPackagePath)
    {
        var export = new ProjectExportData
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
                ThumbnailPath = project.ThumbnailPath,
                ThumbnailPackagePath = thumbnailPackagePath,
                Scenes = project.Scenes
                    .OrderBy(s => s.SceneIndex)
                    .Select(s => new ExportedScene
                    {
                        SceneIndex = s.SceneIndex,
                        ScriptText = s.ScriptText,
                        DurationSeconds = s.DurationSeconds
                    })
                    .ToList(),
                Assets = project.Assets
                    .Select(asset =>
                    {
                        var packagePath = packagePathResolver?.Invoke(asset);
                        return new ExportedAsset
                        {
                            AssetId = asset.Id,
                            AssetType = asset.AssetType,
                            FileName = Path.GetFileName(asset.FilePath),
                            OriginalPath = asset.FilePath,
                            MimeType = asset.MimeType,
                            FileSizeBytes = asset.FileSizeBytes,
                            IsTemporary = asset.IsTemporary,
                            PackagePath = packagePath,
                            IncludedInPackage = packagePath != null
                        };
                    })
                    .ToList()
            }
        };

        return export;
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        return relativePath.Replace('\\', '/');
    }

    private async Task<string> PrepareProjectAssetsDirectoryAsync(Guid projectId, CancellationToken ct)
    {
        var projectsRoot = await _storageService.GetWorkspacePathAsync(WorkspaceFolders.Projects, ct).ConfigureAwait(false);
        var projectRoot = Path.Combine(projectsRoot, projectId.ToString());
        var assetRoot = Path.Combine(projectRoot, "Assets");
        Directory.CreateDirectory(assetRoot);
        return assetRoot;
    }

    private static string EnsureUniqueFileName(string destinationPath)
    {
        if (!File.Exists(destinationPath))
        {
            return destinationPath;
        }

        var directory = Path.GetDirectoryName(destinationPath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(destinationPath);
        var extension = Path.GetExtension(destinationPath);
        var counter = 1;

        while (true)
        {
            var candidate = Path.Combine(directory, $"{fileName}_{counter}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }

    private string? ResolveAssetSourcePath(ExportedAsset asset, string? packageRoot)
    {
        if (!string.IsNullOrWhiteSpace(packageRoot) && !string.IsNullOrWhiteSpace(asset.PackagePath))
        {
            var candidate = Path.GetFullPath(Path.Combine(packageRoot, asset.PackagePath));
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        if (!string.IsNullOrWhiteSpace(asset.OriginalPath) && File.Exists(asset.OriginalPath))
        {
            return asset.OriginalPath;
        }

        return null;
    }

    private async Task ImportAssetsAsync(
        ProjectStateEntity project,
        ExportedProject exportedProject,
        string? packageRoot,
        CancellationToken ct)
    {
        if (exportedProject.Assets == null || exportedProject.Assets.Count == 0)
        {
            return;
        }

        var assetRoot = await PrepareProjectAssetsDirectoryAsync(project.Id, ct).ConfigureAwait(false);
        var assetsToPersist = new List<AssetStateEntity>();

        foreach (var exportedAsset in exportedProject.Assets)
        {
            var sourcePath = ResolveAssetSourcePath(exportedAsset, packageRoot);
            if (sourcePath == null)
            {
                _logger.LogWarning("Skipping asset {AssetId} during import - source file not found", exportedAsset.AssetId);
                continue;
            }

            var fileName = !string.IsNullOrWhiteSpace(exportedAsset.FileName)
                ? exportedAsset.FileName
                : $"{exportedAsset.AssetId}{Path.GetExtension(sourcePath)}";

            var destinationPath = EnsureUniqueFileName(Path.Combine(assetRoot, fileName));
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(sourcePath, destinationPath, overwrite: true);

            var fileInfo = new FileInfo(destinationPath);

            var assetEntity = new AssetStateEntity
            {
                ProjectId = project.Id,
                AssetType = exportedAsset.AssetType ?? "Unknown",
                FilePath = destinationPath,
                FileSizeBytes = fileInfo.Length,
                MimeType = exportedAsset.MimeType,
                IsTemporary = false,
                CreatedAt = DateTime.UtcNow
            };

            assetsToPersist.Add(assetEntity);
        }

        if (assetsToPersist.Count != 0)
        {
            _dbContext.AssetStates.AddRange(assetsToPersist);
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    private async Task RestoreThumbnailAsync(
        ProjectStateEntity project,
        ExportedProject exportedProject,
        string? packageRoot,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(exportedProject.ThumbnailPackagePath) || string.IsNullOrWhiteSpace(packageRoot))
        {
            return;
        }

        var sourcePath = Path.GetFullPath(Path.Combine(packageRoot, exportedProject.ThumbnailPackagePath));
        if (!File.Exists(sourcePath))
        {
            _logger.LogWarning("Package thumbnail not found at {Path} for project {ProjectId}", sourcePath, project.Id);
            return;
        }

        var projectsRoot = await _storageService.GetWorkspacePathAsync(WorkspaceFolders.Projects, ct).ConfigureAwait(false);
        var projectRoot = Path.Combine(projectsRoot, project.Id.ToString());
        var thumbnailsRoot = Path.Combine(projectRoot, "Thumbnails");
        Directory.CreateDirectory(thumbnailsRoot);

        var destinationPath = Path.Combine(thumbnailsRoot, $"thumbnail{Path.GetExtension(sourcePath)}");
        File.Copy(sourcePath, destinationPath, overwrite: true);

        project.ThumbnailPath = destinationPath;
        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<ProjectStateEntity> ImportProjectFromExportAsync(
        ProjectExportData exportData,
        string? newTitle,
        string? packageRoot,
        CancellationToken ct)
    {
        if (exportData.Project == null)
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

        if (exportData.Project.Scenes != null)
        {
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
        }

        await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);

        await ImportAssetsAsync(project, exportData.Project, packageRoot, ct).ConfigureAwait(false);
        await RestoreThumbnailAsync(project, exportData.Project, packageRoot, ct).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(project.ThumbnailPath) &&
            !string.IsNullOrWhiteSpace(exportData.Project.ThumbnailPath) &&
            File.Exists(exportData.Project.ThumbnailPath))
        {
            project.ThumbnailPath = exportData.Project.ThumbnailPath;
            await _dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        _logger.LogInformation("Imported project {ProjectId}: {Title}", project.Id, project.Title);
        return project;
    }

    /// <summary>
    /// Export a project to a JSON file
    /// </summary>
    public async Task<string> ExportProjectAsync(Guid projectId, string outputPath, CancellationToken ct = default)
    {
        var project = await LoadProjectGraphAsync(projectId, ct).ConfigureAwait(false);
        var exportData = BuildExportData(project, packagePathResolver: null, thumbnailPackagePath: null);
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
        var project = await LoadProjectGraphAsync(projectId, ct).ConfigureAwait(false);

        var tempDir = Path.Combine(Path.GetTempPath(), $"aura_export_{projectId}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var assetRelativePaths = new Dictionary<Guid, string>();

            if (includeAssets)
            {
                var assetsDir = Path.Combine(tempDir, "assets");
                Directory.CreateDirectory(assetsDir);

                foreach (var asset in project.Assets)
                {
                    if (!File.Exists(asset.FilePath))
                    {
                        _logger.LogWarning("Skipping asset {AssetId} during export - file not found: {Path}", asset.Id, asset.FilePath);
                        continue;
                    }

                    var extension = Path.GetExtension(asset.FilePath);
                    var fileName = $"{asset.Id}{extension}";
                    var relativePath = NormalizeRelativePath(Path.Combine("assets", fileName));
                    var destinationPath = Path.Combine(tempDir, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                    File.Copy(asset.FilePath, destinationPath, overwrite: true);
                    assetRelativePaths[asset.Id] = relativePath;
                }
            }

            string? thumbnailRelativePath = null;
            if (!string.IsNullOrWhiteSpace(project.ThumbnailPath) && File.Exists(project.ThumbnailPath))
            {
                var thumbnailDir = Path.Combine(tempDir, "thumbnails");
                Directory.CreateDirectory(thumbnailDir);
                var thumbnailFileName = $"{project.Id}{Path.GetExtension(project.ThumbnailPath)}";
                var thumbnailDestination = Path.Combine(thumbnailDir, thumbnailFileName);
                File.Copy(project.ThumbnailPath, thumbnailDestination, overwrite: true);
                thumbnailRelativePath = NormalizeRelativePath(Path.Combine("thumbnails", thumbnailFileName));
            }

            var metadataPath = Path.Combine(tempDir, "project.json");
            var exportData = BuildExportData(
                project,
                asset => assetRelativePaths.TryGetValue(asset.Id, out var relPath) ? relPath : null,
                thumbnailRelativePath);

            var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(metadataPath, json, ct).ConfigureAwait(false);

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
        var exportData = JsonSerializer.Deserialize<ProjectExportData>(json)
            ?? throw new InvalidOperationException("Invalid project export data");

        var packageRoot = Path.GetDirectoryName(filePath);
        return await ImportProjectFromExportAsync(exportData, newTitle, packageRoot, ct).ConfigureAwait(false);
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
            var json = await File.ReadAllTextAsync(metadataPath, ct).ConfigureAwait(false);
            var exportData = JsonSerializer.Deserialize<ProjectExportData>(json)
                ?? throw new InvalidOperationException("Invalid project export data");
            var project = await ImportProjectFromExportAsync(exportData, newTitle, tempDir, ct).ConfigureAwait(false);

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
    public string? ThumbnailPath { get; set; }
    public string? ThumbnailPackagePath { get; set; }
    public List<ExportedScene> Scenes { get; set; } = new();
    public List<ExportedAsset> Assets { get; set; } = new();
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

/// <summary>
/// Exported asset metadata
/// </summary>
public class ExportedAsset
{
    public Guid AssetId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public string? OriginalPath { get; set; }
    public string? PackagePath { get; set; }
    public bool IncludedInPackage { get; set; }
    public long FileSizeBytes { get; set; }
    public string? MimeType { get; set; }
    public bool IsTemporary { get; set; }
}
