using System;
using System.Collections.Generic;

namespace Aura.Core.Models;

/// <summary>
/// Kind of model (SD, TTS, etc.)
/// </summary>
public enum ModelKind
{
    StableDiffusion,
    TTS,
    VAE,
    LoRA,
    Refiner
}

/// <summary>
/// Definition of a model that can be installed
/// </summary>
public class ModelDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ModelKind Kind { get; set; }
    public string Version { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public List<string> Urls { get; set; } = new();
    public string? Description { get; set; }
    public string? Author { get; set; }
    public string? License { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// An installed model instance
/// </summary>
public class InstalledModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ModelKind Kind { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public DateTime InstalledAt { get; set; }
    public string? Version { get; set; }
    public bool IsDefault { get; set; }
    public bool IsExternal { get; set; }
}

/// <summary>
/// Progress for model installation
/// </summary>
public record ModelInstallProgress(
    string Phase,
    float PercentComplete,
    long BytesDownloaded = 0,
    long TotalBytes = 0,
    double SpeedBytesPerSecond = 0,
    string? Message = null
);

/// <summary>
/// External folder for models
/// </summary>
public class ExternalModelFolder
{
    public ModelKind Kind { get; set; }
    public string FolderPath { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; } = true;
    public int ModelCount { get; set; }
    public DateTime AddedAt { get; set; }
}
