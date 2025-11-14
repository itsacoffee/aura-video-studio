using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aura.Core.Models.Projects;
using Aura.Core.Services.Storage;

namespace Aura.Core.Services.Projects;

/// <summary>
/// Service for managing .aura project files
/// </summary>
public interface IProjectFileService
{
    // Project File Operations
    Task<AuraProjectFile> CreateProjectAsync(string name, string? description = null, CancellationToken ct = default);
    Task<AuraProjectFile?> LoadProjectAsync(Guid projectId, CancellationToken ct = default);
    Task SaveProjectAsync(AuraProjectFile project, CancellationToken ct = default);
    Task<bool> DeleteProjectAsync(Guid projectId, CancellationToken ct = default);
    Task<List<AuraProjectFile>> ListProjectsAsync(CancellationToken ct = default);
    
    // Asset Management
    Task<ProjectAsset> AddAssetAsync(Guid projectId, string assetPath, string assetType, CancellationToken ct = default);
    Task<bool> RemoveAssetAsync(Guid projectId, Guid assetId, CancellationToken ct = default);
    Task<MissingAssetsReport> DetectMissingAssetsAsync(Guid projectId, CancellationToken ct = default);
    Task<AssetRelinkResult> RelinkAssetAsync(AssetRelinkRequest request, CancellationToken ct = default);
    
    // Project Consolidation
    Task<ProjectConsolidationResult> ConsolidateProjectAsync(ProjectConsolidationRequest request, CancellationToken ct = default);
    Task<ProjectPackageResult> PackageProjectAsync(ProjectPackageRequest request, CancellationToken ct = default);
    Task<Guid> UnpackageProjectAsync(string packagePath, CancellationToken ct = default);
}

/// <summary>
/// Implementation of project file service
/// </summary>
public class ProjectFileService : IProjectFileService
{
    private readonly ILogger<ProjectFileService> _logger;
    private readonly IEnhancedLocalStorageService _storageService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProjectFileService(
        ILogger<ProjectFileService> logger,
        IEnhancedLocalStorageService storageService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    #region Project File Operations

    public async Task<AuraProjectFile> CreateProjectAsync(string name, string? description = null, CancellationToken ct = default)
    {
        try
        {
            var project = new AuraProjectFile
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = description,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow,
                LastSavedAt = DateTime.UtcNow,
                Version = "1.0"
            };

            await SaveProjectAsync(project, ct);
            
            _logger.LogInformation("Created new project: {ProjectId} - {ProjectName}", project.Id, project.Name);
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project: {ProjectName}", name);
            throw;
        }
    }

    public async Task<AuraProjectFile?> LoadProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var projectJson = await _storageService.LoadProjectFileAsync(projectId, ct);
            if (projectJson == null)
            {
                _logger.LogWarning("Project file not found: {ProjectId}", projectId);
                return null;
            }

            var project = JsonSerializer.Deserialize<AuraProjectFile>(projectJson, _jsonOptions);
            if (project == null)
            {
                _logger.LogError("Failed to deserialize project file: {ProjectId}", projectId);
                return null;
            }

            // Update missing asset flags
            await UpdateMissingAssetFlagsAsync(project, ct);

            _logger.LogInformation("Loaded project: {ProjectId} - {ProjectName}", project.Id, project.Name);
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load project: {ProjectId}", projectId);
            throw;
        }
    }

    public async Task SaveProjectAsync(AuraProjectFile project, CancellationToken ct = default)
    {
        try
        {
            project.ModifiedAt = DateTime.UtcNow;
            project.LastSavedAt = DateTime.UtcNow;
            
            // Update metadata
            project.Metadata.TotalAssets = project.Assets.Count;
            project.Metadata.ProjectSizeBytes = await CalculateProjectSizeAsync(project, ct);

            var projectJson = JsonSerializer.Serialize(project, _jsonOptions);
            await _storageService.SaveProjectFileAsync(project.Id, projectJson, ct);

            _logger.LogInformation("Saved project: {ProjectId} - {ProjectName}", project.Id, project.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save project: {ProjectId}", project.Id);
            throw;
        }
    }

    public async Task<bool> DeleteProjectAsync(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            // Create backup before deleting
            await _storageService.CreateBackupAsync(projectId, "pre_delete", ct);
            
            var projectsPath = await _storageService.GetWorkspacePathAsync("Projects", ct);
            var projectFile = Path.Combine(projectsPath, $"{projectId}.aura");
            
            if (File.Exists(projectFile))
            {
                File.Delete(projectFile);
                _logger.LogInformation("Deleted project: {ProjectId}", projectId);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete project: {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<List<AuraProjectFile>> ListProjectsAsync(CancellationToken ct = default)
    {
        try
        {
            var projectsPath = await _storageService.GetWorkspacePathAsync("Projects", ct);
            var projectFiles = Directory.GetFiles(projectsPath, "*.aura");
            
            var projects = new List<AuraProjectFile>();
            
            foreach (var file in projectFiles)
            {
                try
                {
                    var projectJson = await File.ReadAllTextAsync(file, ct);
                    var project = JsonSerializer.Deserialize<AuraProjectFile>(projectJson, _jsonOptions);
                    
                    if (project != null)
                    {
                        projects.Add(project);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load project file: {File}", file);
                }
            }
            
            return projects.OrderByDescending(p => p.ModifiedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list projects");
            throw;
        }
    }

    #endregion

    #region Asset Management

    public async Task<ProjectAsset> AddAssetAsync(Guid projectId, string assetPath, string assetType, CancellationToken ct = default)
    {
        try
        {
            var project = await LoadProjectAsync(projectId, ct);
            if (project == null)
            {
                throw new FileNotFoundException($"Project not found: {projectId}");
            }

            var fileInfo = new FileInfo(assetPath);
            if (!fileInfo.Exists)
            {
                throw new FileNotFoundException($"Asset file not found: {assetPath}");
            }

            // Calculate relative path
            var projectsPath = await _storageService.GetWorkspacePathAsync("Projects", ct);
            var relativePath = GetRelativePath(projectsPath, assetPath);

            // Calculate content hash
            var contentHash = await CalculateFileHashAsync(assetPath, ct);

            var asset = new ProjectAsset
            {
                Id = Guid.NewGuid(),
                Name = fileInfo.Name,
                Type = assetType,
                Path = assetPath,
                RelativePath = relativePath,
                FileSizeBytes = fileInfo.Length,
                ContentHash = contentHash,
                ImportedAt = DateTime.UtcNow,
                IsMissing = false
            };

            project.Assets.Add(asset);
            await SaveProjectAsync(project, ct);

            _logger.LogInformation("Added asset to project {ProjectId}: {AssetName}", projectId, asset.Name);
            return asset;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add asset to project {ProjectId}: {AssetPath}", projectId, assetPath);
            throw;
        }
    }

    public async Task<bool> RemoveAssetAsync(Guid projectId, Guid assetId, CancellationToken ct = default)
    {
        try
        {
            var project = await LoadProjectAsync(projectId, ct);
            if (project == null)
            {
                return false;
            }

            var asset = project.Assets.FirstOrDefault(a => a.Id == assetId);
            if (asset == null)
            {
                return false;
            }

            project.Assets.Remove(asset);
            await SaveProjectAsync(project, ct);

            _logger.LogInformation("Removed asset {AssetId} from project {ProjectId}", assetId, projectId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove asset {AssetId} from project {ProjectId}", assetId, projectId);
            throw;
        }
    }

    public async Task<MissingAssetsReport> DetectMissingAssetsAsync(Guid projectId, CancellationToken ct = default)
    {
        try
        {
            var project = await LoadProjectAsync(projectId, ct);
            if (project == null)
            {
                throw new FileNotFoundException($"Project not found: {projectId}");
            }

            var missingAssets = new List<ProjectAsset>();

            foreach (var asset in project.Assets)
            {
                if (!File.Exists(asset.Path))
                {
                    // Try relative path
                    if (!string.IsNullOrEmpty(asset.RelativePath))
                    {
                        var projectsPath = await _storageService.GetWorkspacePathAsync("Projects", ct);
                        var resolvedPath = Path.Combine(projectsPath, asset.RelativePath);
                        
                        if (File.Exists(resolvedPath))
                        {
                            // Update path
                            asset.Path = resolvedPath;
                            asset.IsMissing = false;
                            continue;
                        }
                    }
                    
                    asset.IsMissing = true;
                    missingAssets.Add(asset);
                }
                else
                {
                    asset.IsMissing = false;
                }
            }

            // Save updated project
            if (missingAssets.Count != 0)
            {
                await SaveProjectAsync(project, ct);
            }

            var report = new MissingAssetsReport
            {
                ProjectId = projectId,
                ProjectName = project.Name,
                MissingAssets = missingAssets,
                TotalAssets = project.Assets.Count,
                MissingCount = missingAssets.Count,
                GeneratedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Missing assets report for project {ProjectId}: {MissingCount}/{TotalCount}",
                projectId, report.MissingCount, report.TotalAssets);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect missing assets for project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<AssetRelinkResult> RelinkAssetAsync(AssetRelinkRequest request, CancellationToken ct = default)
    {
        try
        {
            var project = await LoadProjectAsync(request.ProjectId, ct);
            if (project == null)
            {
                return new AssetRelinkResult
                {
                    Success = false,
                    ErrorMessage = $"Project not found: {request.ProjectId}"
                };
            }

            var asset = project.Assets.FirstOrDefault(a => a.Id == request.AssetId);
            if (asset == null)
            {
                return new AssetRelinkResult
                {
                    Success = false,
                    ErrorMessage = $"Asset not found: {request.AssetId}"
                };
            }

            if (!File.Exists(request.NewPath))
            {
                return new AssetRelinkResult
                {
                    Success = false,
                    ErrorMessage = $"New path does not exist: {request.NewPath}"
                };
            }

            var oldPath = asset.Path;
            asset.Path = request.NewPath;
            asset.IsMissing = false;
            
            // Update relative path
            var projectsPath = await _storageService.GetWorkspacePathAsync("Projects", ct);
            asset.RelativePath = GetRelativePath(projectsPath, request.NewPath);

            await SaveProjectAsync(project, ct);

            _logger.LogInformation("Relinked asset {AssetId} in project {ProjectId}: {OldPath} -> {NewPath}",
                request.AssetId, request.ProjectId, oldPath, request.NewPath);

            return new AssetRelinkResult
            {
                Success = true,
                OldPath = oldPath,
                NewPath = request.NewPath
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to relink asset {AssetId} in project {ProjectId}",
                request.AssetId, request.ProjectId);
            
            return new AssetRelinkResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    #endregion

    #region Project Consolidation

    public async Task<ProjectConsolidationResult> ConsolidateProjectAsync(ProjectConsolidationRequest request, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new ProjectConsolidationResult();

        try
        {
            var project = await LoadProjectAsync(request.ProjectId, ct);
            if (project == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Project not found: {request.ProjectId}";
                return result;
            }

            // Create backup if requested
            if (request.CreateBackup)
            {
                await _storageService.CreateBackupAsync(request.ProjectId, "pre_consolidation", ct);
            }

            // Create project-specific assets folder
            var projectsPath = await _storageService.GetWorkspacePathAsync("Projects", ct);
            var projectAssetsPath = Path.Combine(projectsPath, $"{request.ProjectId}_assets");
            Directory.CreateDirectory(projectAssetsPath);

            // Copy external assets
            if (request.CopyExternalAssets)
            {
                foreach (var asset in project.Assets)
                {
                    if (!File.Exists(asset.Path))
                    {
                        _logger.LogWarning("Skipping missing asset: {AssetPath}", asset.Path);
                        continue;
                    }

                    // Check if asset is already in project folder
                    if (asset.Path.StartsWith(projectAssetsPath))
                    {
                        continue;
                    }

                    var fileName = Path.GetFileName(asset.Path);
                    var newPath = Path.Combine(projectAssetsPath, fileName);
                    
                    // Handle duplicate file names
                    int counter = 1;
                    while (File.Exists(newPath))
                    {
                        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        newPath = Path.Combine(projectAssetsPath, $"{nameWithoutExt}_{counter}{ext}");
                        counter++;
                    }

                    File.Copy(asset.Path, newPath);
                    
                    var fileInfo = new FileInfo(newPath);
                    result.TotalBytesCopied += fileInfo.Length;
                    result.AssetsCopied++;

                    // Update asset path
                    asset.Path = newPath;
                    asset.RelativePath = GetRelativePath(projectsPath, newPath);
                    asset.IsMissing = false;
                }
            }

            // Save updated project
            await SaveProjectAsync(project, ct);

            result.Success = true;
            result.ConsolidatedPath = projectAssetsPath;
            result.Duration = DateTime.UtcNow - startTime;

            _logger.LogInformation("Consolidated project {ProjectId}: {Count} assets copied, {Size} bytes",
                request.ProjectId, result.AssetsCopied, result.TotalBytesCopied);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to consolidate project {ProjectId}", request.ProjectId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<ProjectPackageResult> PackageProjectAsync(ProjectPackageRequest request, CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var result = new ProjectPackageResult();

        try
        {
            var project = await LoadProjectAsync(request.ProjectId, ct);
            if (project == null)
            {
                result.Success = false;
                result.ErrorMessage = $"Project not found: {request.ProjectId}";
                return result;
            }

            // Determine output path
            var outputPath = request.OutputPath;
            if (string.IsNullOrEmpty(outputPath))
            {
                var exportsPath = await _storageService.GetWorkspacePathAsync("Exports", ct);
                outputPath = Path.Combine(exportsPath, $"{project.Name}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.aurapack");
            }

            // Create temp directory for packaging
            var tempPath = Path.Combine(Path.GetTempPath(), $"aura_package_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempPath);

            try
            {
                // Copy project file
                var projectJson = JsonSerializer.Serialize(project, _jsonOptions);
                await File.WriteAllTextAsync(Path.Combine(tempPath, "project.json"), projectJson, ct);

                // Copy assets if requested
                if (request.IncludeAssets)
                {
                    var assetsPath = Path.Combine(tempPath, "assets");
                    Directory.CreateDirectory(assetsPath);

                    foreach (var asset in project.Assets.Where(a => !a.IsMissing && File.Exists(a.Path)))
                    {
                        var fileName = Path.GetFileName(asset.Path);
                        var destPath = Path.Combine(assetsPath, fileName);
                        File.Copy(asset.Path, destPath);
                    }
                }

                // Copy backups if requested
                if (request.IncludeBackups)
                {
                    var backups = await _storageService.ListBackupsAsync(request.ProjectId, ct);
                    if (backups.Count != 0)
                    {
                        var backupsPath = Path.Combine(tempPath, "backups");
                        Directory.CreateDirectory(backupsPath);

                        var backupRoot = await _storageService.GetWorkspacePathAsync("Backups", ct);
                        var projectBackupPath = Path.Combine(backupRoot, request.ProjectId.ToString());

                        foreach (var backup in backups)
                        {
                            var backupFile = Path.Combine(projectBackupPath, backup);
                            var destPath = Path.Combine(backupsPath, backup);
                            File.Copy(backupFile, destPath);
                        }
                    }
                }

                // Create zip archive
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                ZipFile.CreateFromDirectory(tempPath, outputPath,
                    request.CompressAssets ? CompressionLevel.Optimal : CompressionLevel.Fastest,
                    false);

                var packageInfo = new FileInfo(outputPath);

                result.Success = true;
                result.PackagePath = outputPath;
                result.PackageSizeBytes = packageInfo.Length;
                result.FormattedSize = FormatBytes(packageInfo.Length);
                result.Duration = DateTime.UtcNow - startTime;

                _logger.LogInformation("Packaged project {ProjectId}: {Size} at {Path}",
                    request.ProjectId, result.FormattedSize, outputPath);
            }
            finally
            {
                // Clean up temp directory
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to package project {ProjectId}", request.ProjectId);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<Guid> UnpackageProjectAsync(string packagePath, CancellationToken ct = default)
    {
        try
        {
            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException($"Package file not found: {packagePath}");
            }

            // Create temp directory for extraction
            var tempPath = Path.Combine(Path.GetTempPath(), $"aura_unpack_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempPath);

            try
            {
                // Extract package
                ZipFile.ExtractToDirectory(packagePath, tempPath);

                // Load project
                var projectJsonPath = Path.Combine(tempPath, "project.json");
                if (!File.Exists(projectJsonPath))
                {
                    throw new InvalidOperationException("Invalid package: project.json not found");
                }

                var projectJson = await File.ReadAllTextAsync(projectJsonPath, ct);
                var project = JsonSerializer.Deserialize<AuraProjectFile>(projectJson, _jsonOptions);
                
                if (project == null)
                {
                    throw new InvalidOperationException("Failed to deserialize project");
                }

                // Generate new project ID to avoid conflicts
                var newProjectId = Guid.NewGuid();
                project.Id = newProjectId;

                // Copy assets
                var assetsPath = Path.Combine(tempPath, "assets");
                if (Directory.Exists(assetsPath))
                {
                    var mediaPath = await _storageService.GetWorkspacePathAsync("Media", ct);
                    var projectAssetsPath = Path.Combine(mediaPath, newProjectId.ToString());
                    Directory.CreateDirectory(projectAssetsPath);

                    foreach (var asset in project.Assets)
                    {
                        var sourceFile = Path.Combine(assetsPath, Path.GetFileName(asset.Path));
                        if (File.Exists(sourceFile))
                        {
                            var destFile = Path.Combine(projectAssetsPath, Path.GetFileName(asset.Path));
                            File.Copy(sourceFile, destFile);
                            
                            asset.Path = destFile;
                            asset.RelativePath = GetRelativePath(mediaPath, destFile);
                            asset.IsMissing = false;
                        }
                    }
                }

                // Save project
                await SaveProjectAsync(project, ct);

                _logger.LogInformation("Unpackaged project {ProjectId} from {Package}", newProjectId, packagePath);
                return newProjectId;
            }
            finally
            {
                // Clean up temp directory
                if (Directory.Exists(tempPath))
                {
                    Directory.Delete(tempPath, true);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unpackage project from {Package}", packagePath);
            throw;
        }
    }

    #endregion

    #region Helper Methods

    private async Task UpdateMissingAssetFlagsAsync(AuraProjectFile project, CancellationToken ct)
    {
        foreach (var asset in project.Assets)
        {
            asset.IsMissing = !File.Exists(asset.Path);
            
            if (asset.IsMissing && !string.IsNullOrEmpty(asset.RelativePath))
            {
                var projectsPath = await _storageService.GetWorkspacePathAsync("Projects", ct);
                var resolvedPath = Path.Combine(projectsPath, asset.RelativePath);
                
                if (File.Exists(resolvedPath))
                {
                    asset.Path = resolvedPath;
                    asset.IsMissing = false;
                }
            }
        }
    }

    private async Task<long> CalculateProjectSizeAsync(AuraProjectFile project, CancellationToken ct)
    {
        long totalSize = 0;
        
        foreach (var asset in project.Assets.Where(a => !a.IsMissing && File.Exists(a.Path)))
        {
            try
            {
                var fileInfo = new FileInfo(asset.Path);
                totalSize += fileInfo.Length;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get size for asset: {AssetPath}", asset.Path);
            }
        }
        
        return await Task.FromResult(totalSize);
    }

    private static string GetRelativePath(string basePath, string fullPath)
    {
        try
        {
            var baseUri = new Uri(basePath.EndsWith(Path.DirectorySeparatorChar.ToString()) 
                ? basePath 
                : basePath + Path.DirectorySeparatorChar);
            var fullUri = new Uri(fullPath);
            
            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            return Uri.UnescapeDataString(relativeUri.ToString().Replace('/', Path.DirectorySeparatorChar));
        }
        catch
        {
            return fullPath;
        }
    }

    private static async Task<string> CalculateFileHashAsync(string filePath, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        
        return $"{len:0.##} {sizes[order]}";
    }

    #endregion
}
