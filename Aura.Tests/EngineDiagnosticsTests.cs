using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Aura.Core.Downloads;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class EngineDiagnosticsTests : IDisposable
{
    private readonly ILogger<EngineInstaller> _logger;
    private readonly string _testDirectory;
    private readonly string _installRoot;
    private readonly HttpClient _httpClient;

    public EngineDiagnosticsTests()
    {
        _logger = NullLogger<EngineInstaller>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-diagnostics-tests-" + Guid.NewGuid().ToString());
        _installRoot = Path.Combine(_testDirectory, "engines");
        _httpClient = new HttpClient();
        
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_installRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            try
            {
                Directory.Delete(_testDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
        _httpClient.Dispose();
    }

    [Fact]
    public async Task GetDiagnosticsAsync_Should_ReturnNotInstalled_WhenEngineDoesNotExist()
    {
        // Arrange
        var installer = new EngineInstaller(_logger, _httpClient, _installRoot);
        var engine = new EngineManifestEntry
        {
            Id = "test-engine",
            Name = "Test Engine",
            Version = "1.0",
            Entrypoint = "test.exe",
            SizeBytes = 1024 * 1024 * 100 // 100MB
        };

        // Act
        var result = await installer.GetDiagnosticsAsync(engine);

        // Assert
        Assert.False(result.IsInstalled);
        Assert.True(result.PathExists); // Created during diagnostics
        Assert.True(result.PathWritable);
        Assert.True(result.AvailableDiskSpaceBytes > 0);
        Assert.Null(result.ChecksumStatus);
        Assert.Null(result.ExpectedSha256); // No SHA256 configured
        Assert.Null(result.ActualSha256);
        Assert.Null(result.FailedUrl); // No URL configured in this test
    }

    [Fact]
    public async Task GetDiagnosticsAsync_Should_CheckDiskSpace()
    {
        // Arrange
        var installer = new EngineInstaller(_logger, _httpClient, _installRoot);
        var engine = new EngineManifestEntry
        {
            Id = "large-engine",
            Name = "Large Engine",
            Version = "1.0",
            Entrypoint = "test.exe",
            SizeBytes = 1024L * 1024L * 1024L * 1024L // 1TB - unrealistically large
        };

        // Act
        var result = await installer.GetDiagnosticsAsync(engine);

        // Assert
        Assert.Contains(result.Issues, issue => issue.Contains("Insufficient disk space"));
    }

    [Fact]
    public async Task GetDiagnosticsAsync_Should_VerifyChecksumWhenInstalled()
    {
        // Arrange
        var installer = new EngineInstaller(_logger, _httpClient, _installRoot);
        var engine = new EngineManifestEntry
        {
            Id = "installed-engine",
            Name = "Installed Engine",
            Version = "1.0",
            Entrypoint = "test.exe",
            SizeBytes = 1024 * 1024
        };
        
        var enginePath = installer.GetInstallPath(engine.Id);
        Directory.CreateDirectory(enginePath);
        File.WriteAllText(Path.Combine(enginePath, "test.exe"), "executable content");

        // Act
        var result = await installer.GetDiagnosticsAsync(engine);

        // Assert
        Assert.True(result.IsInstalled);
        Assert.Equal("Valid", result.ChecksumStatus);
        Assert.Empty(result.Issues);
        Assert.Null(result.ActualSha256); // Valid installation, no need to show actual
    }

    [Fact]
    public async Task GetDiagnosticsAsync_Should_DetectMissingEntrypoint()
    {
        // Arrange
        var installer = new EngineInstaller(_logger, _httpClient, _installRoot);
        var engine = new EngineManifestEntry
        {
            Id = "broken-engine",
            Name = "Broken Engine",
            Version = "1.0",
            Entrypoint = "missing.exe",
            SizeBytes = 1024 * 1024,
            Sha256 = "expected123"
        };
        
        var enginePath = installer.GetInstallPath(engine.Id);
        Directory.CreateDirectory(enginePath);
        File.WriteAllText(Path.Combine(enginePath, "other.txt"), "wrong file");

        // Act
        var result = await installer.GetDiagnosticsAsync(engine);

        // Assert
        Assert.True(result.IsInstalled); // Has files
        Assert.Equal("Invalid", result.ChecksumStatus);
        Assert.Contains(result.Issues, issue => issue.Contains("Entrypoint file not found"));
        Assert.Equal("expected123", result.ExpectedSha256); // Should show expected even if invalid
    }

    [Fact]
    public async Task GetDiagnosticsAsync_Should_CheckPathWritability()
    {
        // Arrange
        var installer = new EngineInstaller(_logger, _httpClient, _installRoot);
        var engine = new EngineManifestEntry
        {
            Id = "test-write",
            Name = "Test Write",
            Version = "1.0",
            Entrypoint = "test.exe",
            SizeBytes = 1024 * 1024
        };

        // Act
        var result = await installer.GetDiagnosticsAsync(engine);

        // Assert
        Assert.True(result.PathWritable);
        Assert.DoesNotContain(result.Issues, issue => issue.Contains("not writable"));
    }

    [Fact]
    public async Task RepairAsync_Should_ReinstallEngine()
    {
        // Arrange
        var installer = new EngineInstaller(_logger, _httpClient, _installRoot);
        var engine = new EngineManifestEntry
        {
            Id = "repair-test",
            Name = "Repair Test",
            Version = "1.0",
            Entrypoint = "test.exe",
            SizeBytes = 1024,
            ArchiveType = "zip",
            Urls = new System.Collections.Generic.Dictionary<string, string>()
        };
        
        // Create a broken installation
        var enginePath = installer.GetInstallPath(engine.Id);
        Directory.CreateDirectory(enginePath);
        File.WriteAllText(Path.Combine(enginePath, "broken.txt"), "broken");

        // Act - This will fail because we don't have a real URL, but it should at least clean up
        try
        {
            await installer.RepairAsync(engine);
        }
        catch
        {
            // Expected to fail without a real download URL
        }

        // Assert - The old broken installation should be removed
        var filesAfterRepair = Directory.Exists(enginePath) ? Directory.GetFiles(enginePath) : Array.Empty<string>();
        Assert.DoesNotContain(filesAfterRepair, f => Path.GetFileName(f) == "broken.txt");
    }

    [Fact]
    public async Task RepairAsync_Should_CleanupPartialDownloads()
    {
        // Arrange
        var installer = new EngineInstaller(_logger, _httpClient, _installRoot);
        var engine = new EngineManifestEntry
        {
            Id = "partial-test",
            Name = "Partial Test",
            Version = "1.0",
            Entrypoint = "test.exe",
            SizeBytes = 1024,
            ArchiveType = "zip",
            Urls = new System.Collections.Generic.Dictionary<string, string>()
        };
        
        // Create a partial download
        string downloadDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Aura", "Downloads", engine.Id, engine.Version);
        Directory.CreateDirectory(downloadDir);
        var partialFile = Path.Combine(downloadDir, $"{engine.Id}.archive.partial");
        File.WriteAllText(partialFile, "partial content");

        // Act - This will fail because we don't have a real URL, but it should clean up partial files
        try
        {
            await installer.RepairAsync(engine);
        }
        catch
        {
            // Expected to fail without a real download URL
        }

        // Assert - The partial file should be deleted
        Assert.False(File.Exists(partialFile));
    }

    [Fact]
    public async Task GetDiagnosticsAsync_Should_ShowExpectedSha256_WhenNotInstalled()
    {
        // Arrange
        var installer = new EngineInstaller(_logger, _httpClient, _installRoot);
        var engine = new EngineManifestEntry
        {
            Id = "sha-test",
            Name = "SHA Test",
            Version = "1.0",
            Entrypoint = "test.exe",
            SizeBytes = 1024,
            Sha256 = "abc123def456",
            Urls = new System.Collections.Generic.Dictionary<string, string>
            {
                { "windows", "http://example.com/test.zip" },
                { "linux", "http://example.com/test.tar.gz" }
            }
        };

        // Act
        var result = await installer.GetDiagnosticsAsync(engine);

        // Assert
        Assert.False(result.IsInstalled);
        Assert.Equal("abc123def456", result.ExpectedSha256);
        Assert.NotNull(result.FailedUrl); // Should show the URL
        Assert.Contains("example.com", result.FailedUrl);
    }
}
