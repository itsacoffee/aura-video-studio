using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Downloads;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aura.Tests;

public class ModelInstallerTests : IDisposable
{
    private readonly ILogger<ModelInstaller> _logger;
    private readonly string _testDirectory;
    private readonly string _installRoot;
    private readonly HttpClient _httpClient;
    private readonly ModelInstaller _installer;

    public ModelInstallerTests()
    {
        _logger = NullLogger<ModelInstaller>.Instance;
        _testDirectory = Path.Combine(Path.GetTempPath(), "aura-model-tests-" + Guid.NewGuid().ToString());
        _installRoot = Path.Combine(_testDirectory, "models");
        _httpClient = new HttpClient();
        
        Directory.CreateDirectory(_testDirectory);
        Directory.CreateDirectory(_installRoot);
        
        _installer = new ModelInstaller(_logger, _httpClient, _installRoot);
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
    public void GetDefaultDirectory_Should_ReturnCorrectPath_ForSDBase()
    {
        // Act
        var path = _installer.GetDefaultDirectory(ModelKind.SD_BASE);

        // Assert
        Assert.EndsWith(Path.Combine("stable-diffusion-webui", "models", "Stable-diffusion"), path);
    }

    [Fact]
    public void GetDefaultDirectory_Should_ReturnCorrectPath_ForPiperVoice()
    {
        // Act
        var path = _installer.GetDefaultDirectory(ModelKind.PIPER_VOICE);

        // Assert
        Assert.EndsWith(Path.Combine("piper", "voices"), path);
    }

    [Fact]
    public void GetDefaultDirectory_Should_ReturnCorrectPath_ForLORA()
    {
        // Act
        var path = _installer.GetDefaultDirectory(ModelKind.LORA);

        // Assert
        Assert.EndsWith(Path.Combine("stable-diffusion-webui", "models", "Lora"), path);
    }

    [Fact]
    public async Task AddExternalFolderAsync_Should_AddFolder()
    {
        // Arrange
        var externalFolder = Path.Combine(_testDirectory, "external-voices");
        Directory.CreateDirectory(externalFolder);

        // Create a test voice file
        var voiceFile = Path.Combine(externalFolder, "test-voice.onnx");
        await File.WriteAllTextAsync(voiceFile, "test content");

        // Act
        var count = await _installer.AddExternalFolderAsync(ModelKind.PIPER_VOICE, externalFolder);

        // Assert
        Assert.Equal(1, count);
        var folders = _installer.GetExternalFolders(ModelKind.PIPER_VOICE);
        Assert.Single(folders);
        Assert.Equal(externalFolder, folders[0].FolderPath);
    }

    [Fact]
    public async Task AddExternalFolderAsync_Should_NotAddDuplicates()
    {
        // Arrange
        var externalFolder = Path.Combine(_testDirectory, "external-voices");
        Directory.CreateDirectory(externalFolder);

        // Act
        await _installer.AddExternalFolderAsync(ModelKind.PIPER_VOICE, externalFolder);
        var count = await _installer.AddExternalFolderAsync(ModelKind.PIPER_VOICE, externalFolder);

        // Assert
        Assert.Equal(0, count); // No new models discovered since it's a duplicate
        var folders = _installer.GetExternalFolders(ModelKind.PIPER_VOICE);
        Assert.Single(folders);
    }

    [Fact]
    public void RemoveExternalFolder_Should_RemoveFolder()
    {
        // Arrange
        var externalFolder = Path.Combine(_testDirectory, "external-voices");
        Directory.CreateDirectory(externalFolder);
        _installer.AddExternalFolderAsync(ModelKind.PIPER_VOICE, externalFolder).Wait();

        // Act
        _installer.RemoveExternalFolder(ModelKind.PIPER_VOICE, externalFolder);

        // Assert
        var folders = _installer.GetExternalFolders(ModelKind.PIPER_VOICE);
        Assert.Empty(folders);
    }

    [Fact]
    public async Task ListModelsAsync_Should_FindModelsInDefaultDirectory()
    {
        // Arrange
        var defaultDir = _installer.GetDefaultDirectory(ModelKind.PIPER_VOICE);
        Directory.CreateDirectory(defaultDir);
        
        var voiceFile = Path.Combine(defaultDir, "test-voice.onnx");
        await File.WriteAllTextAsync(voiceFile, "test content");

        // Act
        var models = await _installer.ListModelsAsync("piper");

        // Assert
        Assert.Single(models);
        Assert.Equal("test-voice", models[0].Id);
        Assert.Equal(ModelKind.PIPER_VOICE, models[0].Kind);
        Assert.False(models[0].IsExternal);
    }

    [Fact]
    public async Task ListModelsAsync_Should_FindModelsInExternalFolder()
    {
        // Arrange
        var externalFolder = Path.Combine(_testDirectory, "external-voices");
        Directory.CreateDirectory(externalFolder);
        
        var voiceFile = Path.Combine(externalFolder, "external-voice.onnx");
        await File.WriteAllTextAsync(voiceFile, "test content");
        
        await _installer.AddExternalFolderAsync(ModelKind.PIPER_VOICE, externalFolder);

        // Act
        var models = await _installer.ListModelsAsync("piper");

        // Assert
        Assert.Single(models);
        Assert.Equal("external-voice", models[0].Id);
        Assert.True(models[0].IsExternal);
    }

    [Fact]
    public async Task RemoveModelAsync_Should_DeleteFile()
    {
        // Arrange
        var defaultDir = _installer.GetDefaultDirectory(ModelKind.PIPER_VOICE);
        Directory.CreateDirectory(defaultDir);
        
        var voiceFile = Path.Combine(defaultDir, "test-voice.onnx");
        await File.WriteAllTextAsync(voiceFile, "test content");

        // Act
        await _installer.RemoveModelAsync("test-voice", voiceFile);

        // Assert
        Assert.False(File.Exists(voiceFile));
    }

    [Fact]
    public async Task RemoveModelAsync_Should_ThrowForExternalFolder()
    {
        // Arrange
        var externalFolder = Path.Combine(_testDirectory, "external-voices");
        Directory.CreateDirectory(externalFolder);
        
        var voiceFile = Path.Combine(externalFolder, "external-voice.onnx");
        await File.WriteAllTextAsync(voiceFile, "test content");
        
        await _installer.AddExternalFolderAsync(ModelKind.PIPER_VOICE, externalFolder);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _installer.RemoveModelAsync("external-voice", voiceFile));
    }

    [Fact]
    public async Task VerifyModelAsync_Should_ReturnValidForExistingFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "test-file.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        // Act
        var (isValid, status) = await _installer.VerifyModelAsync(testFile, null);

        // Assert
        Assert.True(isValid);
        Assert.Equal("Unknown checksum (user-supplied)", status);
    }

    [Fact]
    public async Task VerifyModelAsync_Should_ReturnInvalidForNonExistingFile()
    {
        // Arrange
        var testFile = Path.Combine(_testDirectory, "non-existing.txt");

        // Act
        var (isValid, status) = await _installer.VerifyModelAsync(testFile, null);

        // Assert
        Assert.False(isValid);
        Assert.Equal("File not found", status);
    }

    [Fact]
    public async Task ListModelsAsync_Should_FilterByEngine()
    {
        // Arrange - Create SD models
        var sdDir = _installer.GetDefaultDirectory(ModelKind.SD_BASE);
        Directory.CreateDirectory(sdDir);
        await File.WriteAllTextAsync(Path.Combine(sdDir, "sd-model.safetensors"), "test");

        // Create Piper voices
        var piperDir = _installer.GetDefaultDirectory(ModelKind.PIPER_VOICE);
        Directory.CreateDirectory(piperDir);
        await File.WriteAllTextAsync(Path.Combine(piperDir, "voice.onnx"), "test");

        // Act
        var sdModels = await _installer.ListModelsAsync("stable-diffusion-webui");
        var piperModels = await _installer.ListModelsAsync("piper");

        // Assert
        // SD should return models from multiple kinds (SD_BASE, SD_REFINER, VAE, LORA)
        // Since they share the same directory, we should find at least one
        Assert.NotEmpty(sdModels);
        Assert.Contains(sdModels, m => m.Kind == ModelKind.SD_BASE || m.Kind == ModelKind.SD_REFINER);
        
        Assert.Single(piperModels);
        Assert.Equal(ModelKind.PIPER_VOICE, piperModels[0].Kind);
    }

    [Fact]
    public async Task ListModelsAsync_Should_ParsePiperMetadata()
    {
        // Arrange
        var defaultDir = _installer.GetDefaultDirectory(ModelKind.PIPER_VOICE);
        Directory.CreateDirectory(defaultDir);
        
        var voiceFile = Path.Combine(defaultDir, "en_US-test.onnx");
        await File.WriteAllTextAsync(voiceFile, "test content");
        
        // Create metadata file
        var metadataFile = voiceFile + ".json";
        await File.WriteAllTextAsync(metadataFile, @"{""language"": ""en_US"", ""quality"": ""medium""}");

        // Act
        var models = await _installer.ListModelsAsync("piper");

        // Assert
        Assert.Single(models);
        Assert.Equal("en_US", models[0].Language);
        Assert.Equal("medium", models[0].Quality);
    }

    [Fact]
    public void GetExternalFolders_Should_FilterByKind()
    {
        // Arrange
        var folder1 = Path.Combine(_testDirectory, "folder1");
        var folder2 = Path.Combine(_testDirectory, "folder2");
        Directory.CreateDirectory(folder1);
        Directory.CreateDirectory(folder2);

        _installer.AddExternalFolderAsync(ModelKind.PIPER_VOICE, folder1).Wait();
        _installer.AddExternalFolderAsync(ModelKind.SD_BASE, folder2).Wait();

        // Act
        var piperFolders = _installer.GetExternalFolders(ModelKind.PIPER_VOICE);
        var sdFolders = _installer.GetExternalFolders(ModelKind.SD_BASE);
        var allFolders = _installer.GetExternalFolders();

        // Assert
        Assert.Single(piperFolders);
        Assert.Single(sdFolders);
        Assert.Equal(2, allFolders.Count);
    }
}
