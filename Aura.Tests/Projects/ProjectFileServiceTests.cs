using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Aura.Core.Models.Projects;
using Aura.Core.Services.Projects;
using Aura.Core.Services.Storage;

namespace Aura.Tests.Projects;

public class ProjectFileServiceTests : IDisposable
{
    private readonly Mock<ILogger<ProjectFileService>> _mockLogger;
    private readonly Mock<IEnhancedLocalStorageService> _mockStorageService;
    private readonly ProjectFileService _service;
    private readonly string _testStorageRoot;

    public ProjectFileServiceTests()
    {
        _testStorageRoot = Path.Combine(Path.GetTempPath(), $"AuraProjectTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testStorageRoot);

        _mockLogger = new Mock<ILogger<ProjectFileService>>();
        _mockStorageService = new Mock<IEnhancedLocalStorageService>();

        // Setup mock storage service
        _mockStorageService
            .Setup(s => s.GetWorkspacePathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string folder, CancellationToken ct) => 
            {
                var path = Path.Combine(_testStorageRoot, folder);
                Directory.CreateDirectory(path);
                return path;
            });

        _mockStorageService
            .Setup(s => s.SaveProjectFileAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, string content, CancellationToken ct) =>
            {
                var projectsPath = Path.Combine(_testStorageRoot, "Projects");
                Directory.CreateDirectory(projectsPath);
                var filePath = Path.Combine(projectsPath, $"{projectId}.aura");
                File.WriteAllText(filePath, content);
                return filePath;
            });

        _mockStorageService
            .Setup(s => s.LoadProjectFileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, CancellationToken ct) =>
            {
                var projectsPath = Path.Combine(_testStorageRoot, "Projects");
                var filePath = Path.Combine(projectsPath, $"{projectId}.aura");
                return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
            });

        _mockStorageService
            .Setup(s => s.ProjectFileExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, CancellationToken ct) =>
            {
                var projectsPath = Path.Combine(_testStorageRoot, "Projects");
                var filePath = Path.Combine(projectsPath, $"{projectId}.aura");
                return File.Exists(filePath);
            });

        _mockStorageService
            .Setup(s => s.CreateBackupAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, string? backupName, CancellationToken ct) =>
            {
                var backupsPath = Path.Combine(_testStorageRoot, "Backups", projectId.ToString());
                Directory.CreateDirectory(backupsPath);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = $"{projectId}_{backupName}_{timestamp}.aura.bak";
                var backupPath = Path.Combine(backupsPath, fileName);
                
                var projectsPath = Path.Combine(_testStorageRoot, "Projects");
                var projectFile = Path.Combine(projectsPath, $"{projectId}.aura");
                if (File.Exists(projectFile))
                {
                    File.Copy(projectFile, backupPath);
                }
                
                return backupPath;
            });

        _service = new ProjectFileService(_mockLogger.Object, _mockStorageService.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testStorageRoot))
        {
            Directory.Delete(_testStorageRoot, true);
        }
    }

    [Fact]
    public async Task CreateProjectAsync_CreatesNewProject()
    {
        // Arrange
        var projectName = "Test Project";
        var description = "Test Description";

        // Act
        var project = await _service.CreateProjectAsync(projectName, description);

        // Assert
        Assert.NotNull(project);
        Assert.NotEqual(Guid.Empty, project.Id);
        Assert.Equal(projectName, project.Name);
        Assert.Equal(description, project.Description);
        Assert.Equal("1.0", project.Version);
        Assert.True(project.AutoSaveEnabled);
    }

    [Fact]
    public async Task LoadProjectAsync_LoadsExistingProject()
    {
        // Arrange
        var project = await _service.CreateProjectAsync("Test Project", "Description");

        // Act
        var loadedProject = await _service.LoadProjectAsync(project.Id);

        // Assert
        Assert.NotNull(loadedProject);
        Assert.Equal(project.Id, loadedProject.Id);
        Assert.Equal(project.Name, loadedProject.Name);
    }

    [Fact]
    public async Task LoadProjectAsync_ReturnsNullForNonExistentProject()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var project = await _service.LoadProjectAsync(nonExistentId);

        // Assert
        Assert.Null(project);
    }

    [Fact]
    public async Task SaveProjectAsync_UpdatesProject()
    {
        // Arrange
        var project = await _service.CreateProjectAsync("Test Project", "Description");
        project.Name = "Updated Project";
        project.Description = "Updated Description";

        // Act
        await _service.SaveProjectAsync(project);
        var loadedProject = await _service.LoadProjectAsync(project.Id);

        // Assert
        Assert.NotNull(loadedProject);
        Assert.Equal("Updated Project", loadedProject.Name);
        Assert.Equal("Updated Description", loadedProject.Description);
    }

    [Fact]
    public async Task DeleteProjectAsync_RemovesProject()
    {
        // Arrange
        var project = await _service.CreateProjectAsync("Test Project", "Description");
        
        _mockStorageService
            .Setup(s => s.GetWorkspacePathAsync("Projects", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Path.Combine(_testStorageRoot, "Projects"));

        // Act
        var result = await _service.DeleteProjectAsync(project.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ListProjectsAsync_ReturnsAllProjects()
    {
        // Arrange
        await _service.CreateProjectAsync("Project 1", "Description 1");
        await _service.CreateProjectAsync("Project 2", "Description 2");
        await _service.CreateProjectAsync("Project 3", "Description 3");

        // Act
        var projects = await _service.ListProjectsAsync();

        // Assert
        Assert.NotEmpty(projects);
        Assert.True(projects.Count >= 3);
    }

    [Fact]
    public async Task AddAssetAsync_AddsAssetToProject()
    {
        // Arrange
        var project = await _service.CreateProjectAsync("Test Project", "Description");
        
        // Create a test file
        var testFilePath = Path.Combine(_testStorageRoot, "test-asset.txt");
        File.WriteAllText(testFilePath, "Test content");

        // Act
        var asset = await _service.AddAssetAsync(project.Id, testFilePath, "Document");

        // Assert
        Assert.NotNull(asset);
        Assert.NotEqual(Guid.Empty, asset.Id);
        Assert.Equal("test-asset.txt", asset.Name);
        Assert.Equal("Document", asset.Type);
        Assert.False(asset.IsMissing);
    }

    [Fact]
    public async Task RemoveAssetAsync_RemovesAssetFromProject()
    {
        // Arrange
        var project = await _service.CreateProjectAsync("Test Project", "Description");
        var testFilePath = Path.Combine(_testStorageRoot, "test-asset.txt");
        File.WriteAllText(testFilePath, "Test content");
        var asset = await _service.AddAssetAsync(project.Id, testFilePath, "Document");

        // Act
        var result = await _service.RemoveAssetAsync(project.Id, asset.Id);

        // Assert
        Assert.True(result);
        
        var updatedProject = await _service.LoadProjectAsync(project.Id);
        Assert.NotNull(updatedProject);
        Assert.DoesNotContain(updatedProject.Assets, a => a.Id == asset.Id);
    }

    [Fact]
    public async Task DetectMissingAssetsAsync_DetectsMissingFiles()
    {
        // Arrange
        var project = await _service.CreateProjectAsync("Test Project", "Description");
        
        // Add asset with file
        var testFilePath = Path.Combine(_testStorageRoot, "test-asset.txt");
        File.WriteAllText(testFilePath, "Test content");
        await _service.AddAssetAsync(project.Id, testFilePath, "Document");
        
        // Add asset with missing file
        var missingFilePath = Path.Combine(_testStorageRoot, "missing-asset.txt");
        var missingAsset = new ProjectAsset
        {
            Id = Guid.NewGuid(),
            Name = "missing-asset.txt",
            Type = "Document",
            Path = missingFilePath,
            FileSizeBytes = 0,
            ImportedAt = DateTime.UtcNow
        };
        
        var loadedProject = await _service.LoadProjectAsync(project.Id);
        loadedProject!.Assets.Add(missingAsset);
        await _service.SaveProjectAsync(loadedProject);

        // Act
        var report = await _service.DetectMissingAssetsAsync(project.Id);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(2, report.TotalAssets);
        Assert.Equal(1, report.MissingCount);
        Assert.Single(report.MissingAssets);
    }

    [Fact]
    public async Task RelinkAssetAsync_UpdatesAssetPath()
    {
        // Arrange
        var project = await _service.CreateProjectAsync("Test Project", "Description");
        
        var oldFilePath = Path.Combine(_testStorageRoot, "old-asset.txt");
        File.WriteAllText(oldFilePath, "Test content");
        var asset = await _service.AddAssetAsync(project.Id, oldFilePath, "Document");
        
        // Delete old file and create new one
        File.Delete(oldFilePath);
        var newFilePath = Path.Combine(_testStorageRoot, "new-asset.txt");
        File.WriteAllText(newFilePath, "Test content");

        var request = new AssetRelinkRequest
        {
            ProjectId = project.Id,
            AssetId = asset.Id,
            NewPath = newFilePath
        };

        // Act
        var result = await _service.RelinkAssetAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(oldFilePath, result.OldPath);
        Assert.Equal(newFilePath, result.NewPath);
        
        var updatedProject = await _service.LoadProjectAsync(project.Id);
        var updatedAsset = updatedProject!.Assets.First(a => a.Id == asset.Id);
        Assert.Equal(newFilePath, updatedAsset.Path);
        Assert.False(updatedAsset.IsMissing);
    }

    [Fact]
    public async Task ConsolidateProjectAsync_CopiesExternalAssets()
    {
        // Arrange
        var project = await _service.CreateProjectAsync("Test Project", "Description");
        
        // Add external asset
        var externalFilePath = Path.Combine(Path.GetTempPath(), $"external-{Guid.NewGuid()}.txt");
        File.WriteAllText(externalFilePath, "External content");
        await _service.AddAssetAsync(project.Id, externalFilePath, "Document");

        var request = new ProjectConsolidationRequest
        {
            ProjectId = project.Id,
            CopyExternalAssets = true,
            CreateBackup = false
        };

        // Act
        var result = await _service.ConsolidateProjectAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.AssetsCopied > 0);
        Assert.NotNull(result.ConsolidatedPath);
        
        // Cleanup
        if (File.Exists(externalFilePath))
        {
            File.Delete(externalFilePath);
        }
    }

    [Fact]
    public async Task PackageProjectAsync_CreatesPackageFile()
    {
        // Arrange
        var project = await _service.CreateProjectAsync("Test Project", "Description");
        
        var request = new ProjectPackageRequest
        {
            ProjectId = project.Id,
            IncludeAssets = true,
            CompressAssets = true
        };

        // Act
        var result = await _service.PackageProjectAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.PackagePath);
        Assert.True(result.PackageSizeBytes > 0);
        
        // Cleanup
        if (!string.IsNullOrEmpty(result.PackagePath) && File.Exists(result.PackagePath))
        {
            File.Delete(result.PackagePath);
        }
    }
}
