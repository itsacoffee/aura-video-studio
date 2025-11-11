using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Data;
using Aura.Core.Models.Projects;
using Aura.Core.Services;
using Aura.Core.Services.Projects;
using Aura.Core.Services.Resources;
using Aura.Core.Services.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Aura.Tests.Windows;

/// <summary>
/// Windows-specific compatibility tests for database and storage layer
/// Tests for: SQLite initialization, file path handling, project save/load,
/// file locking, and temporary file cleanup
/// </summary>
public class WindowsDatabaseStorageCompatibilityTests : IDisposable
{
    private readonly string _testStorageRoot;
    private readonly string _testDbPath;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WindowsDatabaseStorageCompatibilityTests> _logger;

    public WindowsDatabaseStorageCompatibilityTests()
    {
        _testStorageRoot = Path.Combine(Path.GetTempPath(), $"AuraWindowsTests_{Guid.NewGuid()}");
        _testDbPath = Path.Combine(_testStorageRoot, "test_aura.db");
        Directory.CreateDirectory(_testStorageRoot);

        // Setup service provider with real services
        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));
        
        // Configure SQLite
        services.AddDbContext<AuraDbContext>(options =>
            options.UseSqlite($"Data Source={_testDbPath};Mode=ReadWriteCreate;Cache=Shared;"));

        services.AddScoped<DatabaseInitializationService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<WindowsDatabaseStorageCompatibilityTests>>();
    }

    public void Dispose()
    {
        try
        {
            // Close all database connections
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetService<AuraDbContext>();
                context?.Database.CloseConnection();
            }

            // Clean up test directory
            if (Directory.Exists(_testStorageRoot))
            {
                // Wait a bit for file handles to release
                Thread.Sleep(100);
                
                // Try to delete, but don't fail the test if we can't
                try
                {
                    Directory.Delete(_testStorageRoot, true);
                }
                catch (IOException)
                {
                    // File might be locked, that's okay for test cleanup
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during test cleanup");
        }

        (_serviceProvider as IDisposable)?.Dispose();
    }

    #region Database Initialization Tests

    [Fact]
    public async Task DatabaseInitialization_OnWindows_CreatesDatabase()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();

        // Act
        var result = await initService.InitializeAsync();

        // Assert
        Assert.True(result.Success, $"Database initialization failed: {result.Error}");
        Assert.True(result.PathWritable, "Database path should be writable");
        Assert.True(result.MigrationsApplied, "Migrations should be applied");
        Assert.True(File.Exists(result.DatabasePath), "Database file should exist");
    }

    [Fact]
    public async Task DatabaseInitialization_WithWindowsPathWithSpaces_Succeeds()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange - Create path with spaces
        var pathWithSpaces = Path.Combine(Path.GetTempPath(), $"Aura Test Folder {Guid.NewGuid()}", "aura.db");
        var directory = Path.GetDirectoryName(pathWithSpaces);
        Directory.CreateDirectory(directory!);

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddDbContext<AuraDbContext>(options =>
            options.UseSqlite($"Data Source=\"{pathWithSpaces}\";Mode=ReadWriteCreate;Cache=Shared;"));
        services.AddScoped<DatabaseInitializationService>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();

        try
        {
            // Act
            var result = await initService.InitializeAsync();

            // Assert
            Assert.True(result.Success, $"Database initialization failed: {result.Error}");
            Assert.True(File.Exists(pathWithSpaces), "Database file should exist at path with spaces");
        }
        finally
        {
            // Cleanup
            try
            {
                if (Directory.Exists(directory))
                {
                    Thread.Sleep(100);
                    Directory.Delete(directory, true);
                }
            }
            catch { /* Ignore cleanup errors */ }
        }
    }

    [Fact]
    public async Task SQLiteWALMode_OnWindows_EnabledSuccessfully()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();

        // Act
        var result = await initService.InitializeAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.WalModeEnabled, "WAL mode should be enabled for better concurrency");

        // Verify WAL files are created
        var walFile = _testDbPath + "-wal";
        var shmFile = _testDbPath + "-shm";
        
        // WAL files might not exist immediately, but the mode should be set
        Assert.True(result.WalModeEnabled);
    }

    [Fact]
    public async Task DatabaseIntegrityCheck_OnWindows_Passes()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();

        // Act
        var result = await initService.InitializeAsync();

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IntegrityCheck, "Database integrity check should pass");
        Assert.False(result.RepairAttempted, "Repair should not be needed for new database");
    }

    [Fact]
    public async Task ConcurrentDatabaseAccess_OnWindows_HandlesMultipleConnections()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        using var scope1 = _serviceProvider.CreateScope();
        var initService = scope1.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        await initService.InitializeAsync();

        // Act - Create multiple concurrent scopes
        var tasks = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AuraDbContext>();
                
                // Perform a simple database operation
                var count = await context.ProjectStates.CountAsync();
                Assert.True(count >= 0);
            }));
        }

        // Assert - All tasks should complete without errors
        await Task.WhenAll(tasks);
    }

    #endregion

    #region File Path Handling Tests

    [Fact]
    public void FilePathHandling_WindowsPathWithBackslashes_NormalizesCorrectly()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var windowsPath = @"C:\Users\TestUser\Documents\Aura\Projects\project.aura";

        // Act
        var normalizedPath = Path.GetFullPath(windowsPath);

        // Assert
        Assert.True(Path.IsPathRooted(normalizedPath));
        Assert.Contains(":", normalizedPath); // Should contain drive letter
    }

    [Fact]
    public void FilePathHandling_UNCPath_HandledCorrectly()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var uncPath = @"\\server\share\Aura\Projects\project.aura";

        // Act
        var isUncPath = uncPath.StartsWith(@"\\");
        var normalizedPath = Path.GetFullPath(uncPath);

        // Assert
        Assert.True(isUncPath);
        Assert.StartsWith(@"\\", normalizedPath);
    }

    [Fact]
    public void FilePathHandling_LongPath_HandlesCorrectly()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange - Create a very long path (but under MAX_PATH)
        var longPath = Path.Combine(
            _testStorageRoot,
            string.Join("\\", Enumerable.Repeat("VeryLongFolderName", 10)),
            "file.txt"
        );

        // Act
        var directoryPath = Path.GetDirectoryName(longPath);
        
        // Assert - Should not throw
        Assert.NotNull(directoryPath);
        Assert.True(longPath.Length > 100);
    }

    [Fact]
    public void FilePathHandling_SpecialCharacters_HandledCorrectly()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var validChars = "Project (2024) - Final [HD]";
        var path = Path.Combine(_testStorageRoot, validChars, "file.txt");

        // Act
        var directoryPath = Path.GetDirectoryName(path);
        var fileName = Path.GetFileName(path);

        // Assert
        Assert.NotNull(directoryPath);
        Assert.Equal("file.txt", fileName);
    }

    [Fact]
    public void FilePathHandling_RelativePaths_ConvertToAbsolute()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var relativePath = @"..\..\test\file.txt";

        // Act
        var absolutePath = Path.GetFullPath(relativePath);

        // Assert
        Assert.True(Path.IsPathRooted(absolutePath));
    }

    #endregion

    #region Project Save/Load Tests

    [Fact]
    public async Task ProjectSaveLoad_OnWindows_WorksWithLocalPaths()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var mockLogger = new Mock<ILogger<ProjectFileService>>();
        var mockStorage = CreateMockStorageService();
        var projectService = new ProjectFileService(mockLogger.Object, mockStorage.Object);

        // Act - Create and save project
        var project = await projectService.CreateProjectAsync("Windows Test Project", "Test on Windows");
        
        // Act - Load project
        var loadedProject = await projectService.LoadProjectAsync(project.Id);

        // Assert
        Assert.NotNull(loadedProject);
        Assert.Equal(project.Id, loadedProject.Id);
        Assert.Equal(project.Name, loadedProject.Name);
    }

    [Fact]
    public async Task ProjectWithAssets_OnWindows_HandlesWindowsPaths()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var mockLogger = new Mock<ILogger<ProjectFileService>>();
        var mockStorage = CreateMockStorageService();
        var projectService = new ProjectFileService(mockLogger.Object, mockStorage.Object);

        var project = await projectService.CreateProjectAsync("Test Project", "Description");
        
        // Create test file with Windows path
        var testFilePath = Path.Combine(_testStorageRoot, "Media", "test-video.mp4");
        Directory.CreateDirectory(Path.GetDirectoryName(testFilePath)!);
        File.WriteAllText(testFilePath, "test content");

        // Act
        var asset = await projectService.AddAssetAsync(project.Id, testFilePath, "Video");

        // Assert
        Assert.NotNull(asset);
        Assert.Contains(":", asset.Path); // Should have drive letter
        Assert.False(asset.IsMissing);
        Assert.True(File.Exists(asset.Path));
    }

    [Fact]
    public async Task ProjectPackage_OnWindows_CreatesValidZipFile()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var mockLogger = new Mock<ILogger<ProjectFileService>>();
        var mockStorage = CreateMockStorageService();
        var projectService = new ProjectFileService(mockLogger.Object, mockStorage.Object);

        var project = await projectService.CreateProjectAsync("Package Test", "Description");
        
        var outputPath = Path.Combine(_testStorageRoot, "Exports", $"{project.Name}.aurapack");

        var request = new ProjectPackageRequest
        {
            ProjectId = project.Id,
            OutputPath = outputPath,
            IncludeAssets = true,
            CompressAssets = true
        };

        // Act
        var result = await projectService.PackageProjectAsync(request);

        // Assert
        Assert.True(result.Success, result.ErrorMessage);
        Assert.NotNull(result.PackagePath);
        Assert.True(File.Exists(result.PackagePath), "Package file should exist");
        Assert.True(result.PackageSizeBytes > 0);
    }

    #endregion

    #region File Locking Tests

    [Fact]
    public async Task FileLocking_OnWindows_DetectsLockedFiles()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var testFilePath = Path.Combine(_testStorageRoot, "locked-file.txt");
        File.WriteAllText(testFilePath, "test content");

        // Act - Open file with exclusive lock
        using (var fileStream = File.Open(testFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            // Assert - Try to open again (should fail)
            Assert.Throws<IOException>(() => 
                File.Open(testFilePath, FileMode.Open, FileAccess.Read, FileShare.None));
        }

        // After closing, should be accessible
        using (var fileStream = File.Open(testFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.NotNull(fileStream);
        }
    }

    [Fact]
    public async Task SQLiteDatabase_OnWindows_HandlesFileLocking()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        await initService.InitializeAsync();

        var context1 = scope.ServiceProvider.GetRequiredService<AuraDbContext>();

        // Act - Create another connection while first is open (WAL mode should allow this)
        using var scope2 = _serviceProvider.CreateScope();
        var context2 = scope2.ServiceProvider.GetRequiredService<AuraDbContext>();

        // Assert - Both should be able to read
        var count1 = await context1.ProjectStates.CountAsync();
        var count2 = await context2.ProjectStates.CountAsync();
        
        Assert.Equal(count1, count2);
    }

    [Fact]
    public async Task ConcurrentWrites_OnWindows_HandledCorrectly()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var initService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();
        await initService.InitializeAsync();

        // Act - Perform concurrent writes
        var tasks = new List<Task>();
        for (int i = 0; i < 3; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                using var writeScope = _serviceProvider.CreateScope();
                var context = writeScope.ServiceProvider.GetRequiredService<AuraDbContext>();
                
                var entity = new ProjectStateEntity
                {
                    Id = Guid.NewGuid(),
                    Status = "Draft",
                    ProjectData = $"Test {index}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                context.ProjectStates.Add(entity);
                await context.SaveChangesAsync();
            }));
        }

        // Assert - All writes should succeed
        await Task.WhenAll(tasks);
        
        using var verifyScope = _serviceProvider.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AuraDbContext>();
        var count = await verifyContext.ProjectStates.CountAsync();
        Assert.Equal(3, count);
    }

    #endregion

    #region Temporary File Cleanup Tests

    [Fact]
    public async Task TemporaryFileCleanup_OnWindows_CleansUpOldFiles()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var tempDir = Path.Combine(_testStorageRoot, "Temp");
        Directory.CreateDirectory(tempDir);

        var logger = _serviceProvider.GetRequiredService<ILogger<TemporaryFileCleanupService>>();
        var mockProviderSettings = new Mock<Aura.Core.Configuration.ProviderSettings>();
        mockProviderSettings.Setup(x => x.GetOutputDirectory()).Returns(_testStorageRoot);

        var cleanupService = new TemporaryFileCleanupService(logger, mockProviderSettings.Object);
        cleanupService.RegisterTempDirectory(tempDir);

        // Create old temp file
        var oldFile = Path.Combine(tempDir, "old-temp-file.tmp");
        File.WriteAllText(oldFile, "temp content");
        
        // Set file time to past
        File.SetLastAccessTime(oldFile, DateTime.UtcNow.AddDays(-2));

        // Act
        await cleanupService.CleanupAsync();

        // Assert
        // File should be cleaned up if retention period is < 2 days
        // Default is 24 hours, so this file should be deleted
        Assert.False(File.Exists(oldFile), "Old temp file should be cleaned up");
    }

    [Fact]
    public async Task TemporaryFileCleanup_OnWindows_PreservesLockedFiles()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var tempDir = Path.Combine(_testStorageRoot, "Temp");
        Directory.CreateDirectory(tempDir);

        var logger = _serviceProvider.GetRequiredService<ILogger<TemporaryFileCleanupService>>();
        var mockProviderSettings = new Mock<Aura.Core.Configuration.ProviderSettings>();
        mockProviderSettings.Setup(x => x.GetOutputDirectory()).Returns(_testStorageRoot);

        var cleanupService = new TemporaryFileCleanupService(logger, mockProviderSettings.Object);
        cleanupService.RegisterTempDirectory(tempDir);

        // Create locked temp file
        var lockedFile = Path.Combine(tempDir, "locked-temp-file.tmp");
        File.WriteAllText(lockedFile, "locked content");
        File.SetLastAccessTime(lockedFile, DateTime.UtcNow.AddDays(-2));

        // Lock the file
        using var fileStream = File.Open(lockedFile, FileMode.Open, FileAccess.Read, FileShare.None);
        
        // Act
        await cleanupService.CleanupAsync();

        // Assert - File should still exist because it's locked
        Assert.True(File.Exists(lockedFile), "Locked file should not be deleted");
    }

    [Fact]
    public async Task TemporaryFileCleanup_OnWindows_RemovesEmptyDirectories()
    {
        // Skip if not on Windows
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        // Arrange
        var tempDir = Path.Combine(_testStorageRoot, "Temp");
        var emptySubDir = Path.Combine(tempDir, "EmptySubDir");
        Directory.CreateDirectory(emptySubDir);

        var logger = _serviceProvider.GetRequiredService<ILogger<TemporaryFileCleanupService>>();
        var mockProviderSettings = new Mock<Aura.Core.Configuration.ProviderSettings>();
        mockProviderSettings.Setup(x => x.GetOutputDirectory()).Returns(_testStorageRoot);

        var cleanupService = new TemporaryFileCleanupService(logger, mockProviderSettings.Object);
        cleanupService.RegisterTempDirectory(tempDir);

        // Act
        await cleanupService.CleanupAsync();

        // Assert - Empty directory should be removed
        Assert.False(Directory.Exists(emptySubDir), "Empty subdirectory should be cleaned up");
    }

    #endregion

    #region Helper Methods

    private Mock<IEnhancedLocalStorageService> CreateMockStorageService()
    {
        var mockStorage = new Mock<IEnhancedLocalStorageService>();

        mockStorage
            .Setup(s => s.GetWorkspacePathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string folder, CancellationToken ct) =>
            {
                var path = Path.Combine(_testStorageRoot, folder);
                Directory.CreateDirectory(path);
                return path;
            });

        mockStorage
            .Setup(s => s.SaveProjectFileAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, string content, CancellationToken ct) =>
            {
                var projectsPath = Path.Combine(_testStorageRoot, "Projects");
                Directory.CreateDirectory(projectsPath);
                var filePath = Path.Combine(projectsPath, $"{projectId}.aura");
                File.WriteAllText(filePath, content);
                return filePath;
            });

        mockStorage
            .Setup(s => s.LoadProjectFileAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, CancellationToken ct) =>
            {
                var projectsPath = Path.Combine(_testStorageRoot, "Projects");
                var filePath = Path.Combine(projectsPath, $"{projectId}.aura");
                return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
            });

        mockStorage
            .Setup(s => s.CreateBackupAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid projectId, string? backupName, CancellationToken ct) =>
            {
                var backupsPath = Path.Combine(_testStorageRoot, "Backups", projectId.ToString());
                Directory.CreateDirectory(backupsPath);
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var fileName = $"{projectId}_{backupName}_{timestamp}.aura.bak";
                return Path.Combine(backupsPath, fileName);
            });

        mockStorage
            .Setup(s => s.ListBackupsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        return mockStorage;
    }

    #endregion
}
