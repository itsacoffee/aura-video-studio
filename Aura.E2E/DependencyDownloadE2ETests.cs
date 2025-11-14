using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Aura.Core.Dependencies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.E2E;

public class DependencyDownloadE2ETests : IDisposable
{
    private readonly ILogger<DependencyManager> _logger;
    private readonly string _testDirectory;
    private readonly string _manifestPath;
    private readonly string _downloadDirectory;
    private readonly HttpClient _httpClient;

    public DependencyDownloadE2ETests()
    {
        _logger = NullLogger<DependencyManager>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-e2e-tests-" + Guid.NewGuid().ToString());
        _manifestPath = Path.Combine(_testDirectory, "manifest.json");
        _downloadDirectory = Path.Combine(_testDirectory, "downloads");
        _httpClient = new HttpClient();
        
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_downloadDirectory);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public async Task ManifestDrivenFlow_Should_LoadAndVerifyComponents()
    {
        // Arrange
        var manager = new DependencyManager(_logger, _httpClient, _manifestPath, _downloadDirectory);

        // Act - Load manifest
        var manifest = await manager.LoadManifestAsync().ConfigureAwait(false);

        // Assert
        Assert.NotNull(manifest);
        Assert.NotEmpty(manifest.Components);
        
        // Verify required component exists
        var ffmpeg = manifest.Components.Find(c => c.Name == "FFmpeg");
        Assert.NotNull(ffmpeg);
        Assert.True(ffmpeg.IsRequired);
        Assert.NotNull(ffmpeg.InstallPath);
        Assert.NotEmpty(ffmpeg.Files);
        
        // Verify optional components exist
        var ollama = manifest.Components.Find(c => c.Name == "Ollama");
        Assert.NotNull(ollama);
        Assert.False(ollama.IsRequired);
    }

    [Fact]
    public async Task VerifyComponent_Should_DetectUninstalledComponent()
    {
        // Arrange
        var manager = new DependencyManager(_logger, _httpClient, _manifestPath, _downloadDirectory);

        // Act
        var result = await manager.VerifyComponentAsync("FFmpeg").ConfigureAwait(false);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("FFmpeg", result.ComponentName);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.MissingFiles);
        Assert.Empty(result.CorruptedFiles);
    }

    [Fact]
    public async Task RepairWorkflow_Should_DetectInvalidComponent()
    {
        // Arrange
        // Create a test manifest with a small dummy file
        var testManifest = @"{
  ""components"": [
    {
      ""name"": ""TestComponent"",
      ""version"": ""1.0"",
      ""isRequired"": false,
      ""installPath"": ""test"",
      ""postInstallProbe"": null,
      ""files"": [
        {
          ""filename"": ""test-file.txt"",
          ""url"": ""http://invalid.url/file.txt"",
          ""sha256"": ""e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"",
          ""extractPath"": """",
          ""sizeBytes"": 0
        }
      ]
    }
  ]
}";
        await File.WriteAllTextAsync(_manifestPath, testManifest).ConfigureAwait(false);

        // Create manager after writing manifest
        var manager = new DependencyManager(_logger, _httpClient, _manifestPath, _downloadDirectory);

        // Act - Verify component (should detect component is not installed)
        var verifyResult = await manager.VerifyComponentAsync("TestComponent").ConfigureAwait(false);

        // Assert - Either missing files detected or invalid status
        Assert.False(verifyResult.IsValid);
        Assert.Equal("TestComponent", verifyResult.ComponentName);
    }

    [Fact]
    public async Task ManualInstructions_Should_ProvideOfflineInstallPath()
    {
        // Arrange
        var manager = new DependencyManager(_logger, _httpClient, _manifestPath, _downloadDirectory);
        await manager.LoadManifestAsync().ConfigureAwait(false); // Ensure manifest is created

        // Act
        var instructions = manager.GetManualInstallInstructions("FFmpeg");

        // Assert
        Assert.NotNull(instructions);
        Assert.Equal("FFmpeg", instructions.ComponentName);
        Assert.Equal("latest", instructions.Version); // Version is dynamically resolved
        Assert.NotEmpty(instructions.InstallPath);
        Assert.NotEmpty(instructions.Steps);
    }

    [Fact]
    public async Task ComponentLifecycle_Should_HandleVerifyAndRemove()
    {
        // Arrange
        // Create a minimal test manifest
        var testManifest = @"{
  ""components"": [
    {
      ""name"": ""TestComponent"",
      ""version"": ""1.0"",
      ""isRequired"": false,
      ""installPath"": ""test"",
      ""postInstallProbe"": null,
      ""files"": [
        {
          ""filename"": ""test-file.txt"",
          ""url"": ""http://invalid.url/file.txt"",
          ""sha256"": ""e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"",
          ""extractPath"": """",
          ""sizeBytes"": 0
        }
      ]
    }
  ]
}";
        await File.WriteAllTextAsync(_manifestPath, testManifest).ConfigureAwait(false);

        // Create manager after writing manifest
        var manager = new DependencyManager(_logger, _httpClient, _manifestPath, _downloadDirectory);

        // Step 1: Verify - should be invalid (not installed)
        var verifyResult1 = await manager.VerifyComponentAsync("TestComponent").ConfigureAwait(false);
        Assert.False(verifyResult1.IsValid);

        // Step 2: Simulate install by creating the file
        var testFilePath = Path.Combine(_downloadDirectory, "test-file.txt");
        await File.WriteAllTextAsync(testFilePath, "").ConfigureAwait(false); // Empty file matches the SHA-256

        // Step 3: Verify again - should be valid now
        var verifyResult2 = await manager.VerifyComponentAsync("TestComponent").ConfigureAwait(false);
        Assert.True(verifyResult2.IsValid);

        // Step 4: Corrupt the file
        await File.WriteAllTextAsync(testFilePath, "corrupted").ConfigureAwait(false);

        // Step 5: Verify - should detect corruption
        var verifyResult3 = await manager.VerifyComponentAsync("TestComponent").ConfigureAwait(false);
        Assert.False(verifyResult3.IsValid);

        // Step 6: Remove component
        await manager.RemoveComponentAsync("TestComponent").ConfigureAwait(false);

        // Step 7: Verify file is deleted
        Assert.False(File.Exists(testFilePath));
    }

    [Fact]
    public void GetComponentDirectory_Should_ReturnValidPath()
    {
        // Arrange
        var manager = new DependencyManager(_logger, _httpClient, _manifestPath, _downloadDirectory);

        // Act
        var directory = manager.GetComponentDirectory("FFmpeg");

        // Assert
        Assert.NotNull(directory);
        Assert.NotEmpty(directory);
        Assert.True(Directory.Exists(directory));
    }

    [Fact]
    public async Task PostInstallProbe_Configuration_Should_BePresent()
    {
        // Arrange
        var manager = new DependencyManager(_logger, _httpClient, _manifestPath, _downloadDirectory);

        // Act
        var manifest = await manager.LoadManifestAsync().ConfigureAwait(false);

        // Assert - Check that components have postInstallProbe configured
        var ffmpeg = manifest.Components.Find(c => c.Name == "FFmpeg");
        Assert.NotNull(ffmpeg);
        Assert.Equal("ffmpeg", ffmpeg.PostInstallProbe?.ToLower());

        var ollama = manifest.Components.Find(c => c.Name == "Ollama");
        Assert.NotNull(ollama);
        Assert.Equal("ollama", ollama.PostInstallProbe?.ToLower());
    }
}
