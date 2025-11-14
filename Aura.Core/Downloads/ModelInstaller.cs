using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Downloads;

/// <summary>
/// Manages model installation, verification, and indexing
/// </summary>
public class ModelInstaller
{
    private readonly ILogger<ModelInstaller> _logger;
    private readonly HttpClient _httpClient;
    private readonly HttpDownloader _downloader;
    private readonly string _modelsBasePath;
    private readonly Dictionary<ModelKind, List<ExternalModelFolder>> _externalFolders;
    private readonly Dictionary<string, InstalledModel> _installedModels;

    public ModelInstaller(ILogger<ModelInstaller> logger, HttpClient httpClient, string installRoot)
    {
        _logger = logger;
        _httpClient = httpClient;
        
        // Create a logger for HttpDownloader
        var loggerFactory = LoggerFactory.Create(builder => { });
        var downloaderLogger = loggerFactory.CreateLogger<HttpDownloader>();
        
        _downloader = new HttpDownloader(downloaderLogger, httpClient);
        
        _modelsBasePath = Path.Combine(installRoot, "models");
        Directory.CreateDirectory(_modelsBasePath);
        
        _externalFolders = new Dictionary<ModelKind, List<ExternalModelFolder>>();
        _installedModels = new Dictionary<string, InstalledModel>();
        
        _logger.LogInformation("ModelInstaller initialized. Base path: {Path}", _modelsBasePath);
    }

    /// <summary>
    /// List all installed models for an engine
    /// </summary>
    public async Task<List<InstalledModel>> ListModelsAsync(string engineId, CancellationToken ct = default)
    {
        await Task.CompletedTask; // For async signature
        
        var kind = engineId.ToLowerInvariant() switch
        {
            "sd" or "stablediffusion" or "sd-webui" or "stable-diffusion-webui" => ModelKind.StableDiffusion,
            "piper" or "mimic3" => ModelKind.PIPER_VOICE,
            _ => throw new ArgumentException($"Unknown engine: {engineId}")
        };

        // Scan the default directory for this kind
        var defaultDir = GetDefaultDirectory(kind);
        if (Directory.Exists(defaultDir))
        {
            ScanDirectory(kind, defaultDir);
        }

        return _installedModels.Values
            .Where(m => m.Kind == kind || 
                       (kind == ModelKind.StableDiffusion && (m.Kind == ModelKind.SD_BASE || m.Kind == ModelKind.SD_REFINER)))
            .ToList();
    }

    private void ScanDirectory(ModelKind kind, string directory)
    {
        var extensions = new[] { ".safetensors", ".ckpt", ".pt", ".pth", ".onnx" };
        var files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            var modelId = Path.GetFileNameWithoutExtension(file);
            
            // Skip if already indexed
            if (_installedModels.ContainsKey(modelId))
            {
                continue;
            }

            // Determine the specific kind based on directory structure
            var specificKind = kind;
            if (kind == ModelKind.StableDiffusion)
            {
                // Try to determine if it's SD_BASE or SD_REFINER from directory
                if (directory.Contains("Stable-diffusion"))
                {
                    specificKind = ModelKind.SD_BASE;
                }
            }

            var model = new InstalledModel
            {
                Id = modelId,
                Name = modelId,
                Kind = specificKind,
                FilePath = file,
                SizeBytes = fileInfo.Length,
                InstalledAt = fileInfo.CreationTimeUtc,
                IsExternal = false
            };

            // Try to parse metadata for Piper voices
            if (kind == ModelKind.PIPER_VOICE)
            {
                var metadataPath = file + ".json";
                if (File.Exists(metadataPath))
                {
                    try
                    {
                        var json = File.ReadAllText(metadataPath);
                        var metadata = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                        if (metadata != null)
                        {
                            if (metadata.TryGetValue("language", out var lang))
                            {
                                model.Language = lang.GetString();
                            }
                            if (metadata.TryGetValue("quality", out var quality))
                            {
                                model.Quality = quality.GetString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse metadata for {File}", file);
                    }
                }
            }

            _installedModels[modelId] = model;
        }
    }

    /// <summary>
    /// Install a model
    /// </summary>
    public async Task<InstalledModel> InstallAsync(
        ModelDefinition model,
        string? destination,
        IProgress<ModelInstallProgress>? progress,
        CancellationToken ct)
    {
        _logger.LogInformation("Installing model {ModelId} ({Name})", model.Id, model.Name);
        
        // Determine destination path
        var destDir = string.IsNullOrEmpty(destination)
            ? Path.Combine(_modelsBasePath, model.Kind.ToString())
            : destination;
        
        Directory.CreateDirectory(destDir);
        
        var fileName = $"{model.Id}.safetensors";
        var destPath = Path.Combine(destDir, fileName);

        // Check if already installed
        if (File.Exists(destPath))
        {
            _logger.LogInformation("Model {ModelId} already exists at {Path}", model.Id, destPath);
            
            var existing = new InstalledModel
            {
                Id = model.Id,
                Name = model.Name,
                Kind = model.Kind,
                FilePath = destPath,
                SizeBytes = new FileInfo(destPath).Length,
                Sha256 = model.Sha256,
                Version = model.Version,
                InstalledAt = File.GetCreationTimeUtc(destPath),
                IsExternal = false
            };
            
            _installedModels[model.Id] = existing;
            return existing;
        }

        // Download the model
        progress?.Report(new ModelInstallProgress("Downloading", 0, 0, model.SizeBytes));
        
        var downloadProgress = new Progress<HttpDownloadProgress>(p =>
        {
            progress?.Report(new ModelInstallProgress(
                "Downloading",
                p.PercentComplete,
                p.BytesDownloaded,
                p.TotalBytes,
                p.SpeedBytesPerSecond,
                p.Message
            ));
        });

        var success = await _downloader.DownloadFileAsync(
            model.Urls.ToArray(),
            destPath,
            model.Sha256,
            downloadProgress,
            ct
        );

        if (!success)
        {
            throw new InvalidOperationException($"Failed to download model {model.Id}");
        }

        progress?.Report(new ModelInstallProgress("Complete", 100, model.SizeBytes, model.SizeBytes));

        var installed = new InstalledModel
        {
            Id = model.Id,
            Name = model.Name,
            Kind = model.Kind,
            FilePath = destPath,
            SizeBytes = model.SizeBytes,
            Sha256 = model.Sha256,
            Version = model.Version,
            InstalledAt = DateTime.UtcNow,
            IsExternal = false
        };

        _installedModels[model.Id] = installed;
        
        _logger.LogInformation("Model {ModelId} installed successfully to {Path}", model.Id, destPath);
        return installed;
    }

    /// <summary>
    /// Add an external folder to index models from
    /// </summary>
    public async Task<int> AddExternalFolderAsync(ModelKind kind, string folderPath, bool isReadOnly = true, CancellationToken ct = default)
    {
        await Task.CompletedTask; // For async signature
        
        if (!Directory.Exists(folderPath))
        {
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");
        }

        if (!_externalFolders.TryGetValue(kind, out var value))
        {
            value = new List<ExternalModelFolder>();
            _externalFolders[kind] = value;
        }

        // Check if already added - return 0 if duplicate
        if (value.Any(f => f.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Folder already added: {Path}", folderPath);
            return 0;
        }

        var folder = new ExternalModelFolder
        {
            Kind = kind,
            FolderPath = folderPath,
            IsReadOnly = isReadOnly,
            AddedAt = DateTime.UtcNow
        };

        // Index models in this folder
        IndexExternalFolder(folder);
        value.Add(folder);
        _logger.LogInformation("Added external folder: {Path} for {Kind}", folderPath, kind);
        
        return folder.ModelCount;
    }

    /// <summary>
    /// Remove an external folder
    /// </summary>
    public bool RemoveExternalFolder(ModelKind kind, string folderPath)
    {
        if (!_externalFolders.TryGetValue(kind, out var folders))
        {
            return false;
        }

        var folder = folders.FirstOrDefault(f => f.FolderPath.Equals(folderPath, StringComparison.OrdinalIgnoreCase));
        if (folder == null)
        {
            return false;
        }

        // Remove models from this folder
        var modelsToRemove = _installedModels.Values
            .Where(m => m.FilePath.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Id)
            .ToList();

        foreach (var id in modelsToRemove)
        {
            _installedModels.Remove(id);
        }

        folders.Remove(folder);
        _logger.LogInformation("Removed external folder: {Path}", folderPath);
        return true;
    }

    /// <summary>
    /// List all external folders
    /// </summary>
    public List<ExternalModelFolder> ListExternalFolders(ModelKind? kind = null)
    {
        if (kind.HasValue)
        {
            return _externalFolders.TryGetValue(kind.Value, out var folders) 
                ? folders 
                : new List<ExternalModelFolder>();
        }

        return _externalFolders.Values.SelectMany(f => f).ToList();
    }

    /// <summary>
    /// Get external folders (alias for ListExternalFolders for backward compatibility)
    /// </summary>
    public List<ExternalModelFolder> GetExternalFolders(ModelKind? kind = null)
    {
        return ListExternalFolders(kind);
    }

    /// <summary>
    /// Get the default directory for a specific model kind
    /// </summary>
    public string GetDefaultDirectory(ModelKind kind)
    {
        var basePath = Path.GetDirectoryName(_modelsBasePath) ?? _modelsBasePath;
        
        return kind switch
        {
            ModelKind.SD_BASE => Path.Combine(basePath, "stable-diffusion-webui", "models", "Stable-diffusion"),
            ModelKind.StableDiffusion => Path.Combine(basePath, "stable-diffusion-webui", "models", "Stable-diffusion"),
            ModelKind.VAE => Path.Combine(basePath, "stable-diffusion-webui", "models", "VAE"),
            ModelKind.LORA => Path.Combine(basePath, "stable-diffusion-webui", "models", "Lora"),
            ModelKind.LoRA => Path.Combine(basePath, "stable-diffusion-webui", "models", "Lora"),
            ModelKind.Refiner => Path.Combine(basePath, "stable-diffusion-webui", "models", "Stable-diffusion"),
            ModelKind.PIPER_VOICE => Path.Combine(basePath, "piper", "voices"),
            ModelKind.TTS => Path.Combine(basePath, "tts", "models"),
            _ => Path.Combine(_modelsBasePath, kind.ToString())
        };
    }

    /// <summary>
    /// Verify a model file against expected SHA256
    /// </summary>
    public async Task<(bool isValid, string status)> VerifyModelAsync(string filePath, string? expectedSha256, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Model file not found: {Path}", filePath);
            return (false, "File not found");
        }

        if (string.IsNullOrEmpty(expectedSha256))
        {
            _logger.LogInformation("No checksum provided for verification");
            return (true, "Unknown checksum (user-supplied)");
        }

        _logger.LogInformation("Verifying model: {Path}", filePath);
        
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        
        var hash = await sha256.ComputeHashAsync(stream, ct);
        var hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        
        var matches = hashString.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase);
        
        if (matches)
        {
            _logger.LogInformation("Model verification successful");
            return (true, "Checksum matches");
        }
        else
        {
            _logger.LogWarning("Model verification failed. Expected: {Expected}, Got: {Actual}", 
                expectedSha256, hashString);
            return (false, $"Checksum mismatch. Expected: {expectedSha256}, Got: {hashString}");
        }
    }

    /// <summary>
    /// Remove a model
    /// </summary>
    public async Task RemoveModelAsync(string modelId, string? filePath = null, CancellationToken ct = default)
    {
        await Task.CompletedTask; // For async signature
        
        // If filePath is provided but model is not in index, just delete the file
        if (filePath != null && !_installedModels.ContainsKey(modelId))
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("Deleted model file: {Path}", filePath);
            }
            return;
        }
        
        if (!_installedModels.TryGetValue(modelId, out var model))
        {
            throw new FileNotFoundException($"Model not found: {modelId}");
        }

        var pathToDelete = filePath ?? model.FilePath;
        
        if (model.IsExternal)
        {
            throw new InvalidOperationException($"Cannot delete external model: {pathToDelete}");
        }

        if (File.Exists(pathToDelete))
        {
            File.Delete(pathToDelete);
            _logger.LogInformation("Deleted model file: {Path}", pathToDelete);
        }

        _installedModels.Remove(modelId);
    }

    /// <summary>
    /// Set default model for a kind
    /// </summary>
    public void SetDefaultModel(ModelKind kind, string modelId)
    {
        // Clear existing defaults for this kind
        foreach (var model in _installedModels.Values.Where(m => m.Kind == kind))
        {
            model.IsDefault = false;
        }

        // Set new default
        if (_installedModels.TryGetValue(modelId, out var targetModel))
        {
            targetModel.IsDefault = true;
            _logger.LogInformation("Set default model for {Kind}: {ModelId}", kind, modelId);
        }
    }

    private void IndexExternalFolder(ExternalModelFolder folder)
    {
        var extensions = new[] { ".safetensors", ".ckpt", ".pt", ".pth", ".onnx" };
        var files = Directory.GetFiles(folder.FolderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .ToList();

        folder.ModelCount = files.Count;

        foreach (var file in files)
        {
            var fileInfo = new FileInfo(file);
            var modelId = Path.GetFileNameWithoutExtension(file);
            
            if (!_installedModels.ContainsKey(modelId))
            {
                _installedModels[modelId] = new InstalledModel
                {
                    Id = modelId,
                    Name = modelId,
                    Kind = folder.Kind,
                    FilePath = file,
                    SizeBytes = fileInfo.Length,
                    InstalledAt = fileInfo.CreationTimeUtc,
                    IsExternal = true
                };
            }
        }

        _logger.LogInformation("Indexed {Count} models from {Path}", files.Count, folder.FolderPath);
    }
}
