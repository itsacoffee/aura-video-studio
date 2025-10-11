using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Downloads;

/// <summary>
/// Loads and manages engine manifests
/// </summary>
public class EngineManifestLoader
{
    private readonly ILogger<EngineManifestLoader> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _localManifestPath;
    private EngineManifest? _cachedManifest;

    public EngineManifestLoader(
        ILogger<EngineManifestLoader> logger,
        HttpClient httpClient,
        string localManifestPath)
    {
        _logger = logger;
        _httpClient = httpClient;
        _localManifestPath = localManifestPath;
    }

    /// <summary>
    /// Load manifest from local file, creating default if it doesn't exist
    /// </summary>
    public async Task<EngineManifest> LoadManifestAsync()
    {
        if (_cachedManifest != null)
        {
            return _cachedManifest;
        }

        if (File.Exists(_localManifestPath))
        {
            try
            {
                _logger.LogInformation("Loading engine manifest from {Path}", _localManifestPath);
                string json = await File.ReadAllTextAsync(_localManifestPath).ConfigureAwait(false);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                _cachedManifest = JsonSerializer.Deserialize<EngineManifest>(json, options);
                if (_cachedManifest != null)
                {
                    return _cachedManifest;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load engine manifest from {Path}", _localManifestPath);
            }
        }

        // Create default manifest
        _logger.LogInformation("Creating default engine manifest");
        _cachedManifest = CreateDefaultManifest();
        await SaveManifestAsync(_cachedManifest).ConfigureAwait(false);
        return _cachedManifest;
    }

    /// <summary>
    /// Refresh manifest from remote URL (if configured)
    /// </summary>
    public async Task<EngineManifest> RefreshFromRemoteAsync(string remoteUrl)
    {
        try
        {
            _logger.LogInformation("Fetching engine manifest from {Url}", remoteUrl);
            string json = await _httpClient.GetStringAsync(remoteUrl).ConfigureAwait(false);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var manifest = JsonSerializer.Deserialize<EngineManifest>(json, options);
            if (manifest != null)
            {
                _cachedManifest = manifest;
                await SaveManifestAsync(manifest).ConfigureAwait(false);
                return manifest;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh engine manifest from {Url}", remoteUrl);
        }

        return await LoadManifestAsync().ConfigureAwait(false);
    }

    private async Task SaveManifestAsync(EngineManifest manifest)
    {
        try
        {
            string? directory = Path.GetDirectoryName(_localManifestPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            string json = JsonSerializer.Serialize(manifest, options);
            await File.WriteAllTextAsync(_localManifestPath, json).ConfigureAwait(false);
            _logger.LogInformation("Saved engine manifest to {Path}", _localManifestPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save engine manifest to {Path}", _localManifestPath);
        }
    }

    private static EngineManifest CreateDefaultManifest()
    {
        string platform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "windows" : "linux";
        
        return new EngineManifest
        {
            Version = "1.0",
            Engines = new List<EngineManifestEntry>
            {
                new EngineManifestEntry
                {
                    Id = "stable-diffusion-webui",
                    Name = "Stable Diffusion WebUI",
                    Version = "1.7.0",
                    Description = "AUTOMATIC1111's Stable Diffusion WebUI",
                    SizeBytes = 2500000000, // ~2.5GB
                    Sha256 = "", // Will be verified during install
                    ArchiveType = "git",
                    Urls = new Dictionary<string, string>
                    {
                        { "windows", "https://github.com/AUTOMATIC1111/stable-diffusion-webui.git" },
                        { "linux", "https://github.com/AUTOMATIC1111/stable-diffusion-webui.git" }
                    },
                    Entrypoint = platform == "windows" ? "webui.bat" : "webui.sh",
                    DefaultPort = 7860,
                    ArgsTemplate = "--api --listen",
                    HealthCheck = new HealthCheckConfig
                    {
                        Url = "/sdapi/v1/sd-models",
                        TimeoutSeconds = 120
                    },
                    LicenseUrl = "https://github.com/AUTOMATIC1111/stable-diffusion-webui/blob/master/LICENSE.txt",
                    RequiredVRAMGB = 4
                },
                new EngineManifestEntry
                {
                    Id = "comfyui",
                    Name = "ComfyUI",
                    Version = "latest",
                    Description = "ComfyUI - A powerful and modular stable diffusion GUI",
                    SizeBytes = 1500000000, // ~1.5GB
                    Sha256 = "",
                    ArchiveType = "git",
                    Urls = new Dictionary<string, string>
                    {
                        { "windows", "https://github.com/comfyanonymous/ComfyUI.git" },
                        { "linux", "https://github.com/comfyanonymous/ComfyUI.git" }
                    },
                    Entrypoint = "main.py",
                    DefaultPort = 8188,
                    ArgsTemplate = "--listen",
                    HealthCheck = new HealthCheckConfig
                    {
                        Url = "/system_stats",
                        TimeoutSeconds = 60
                    },
                    LicenseUrl = "https://github.com/comfyanonymous/ComfyUI/blob/master/LICENSE",
                    RequiredVRAMGB = 4
                },
                new EngineManifestEntry
                {
                    Id = "piper",
                    Name = "Piper TTS",
                    Version = "1.2.0",
                    Description = "Fast, local neural text-to-speech",
                    SizeBytes = 50000000, // ~50MB
                    Sha256 = "",
                    ArchiveType = "zip",
                    Urls = new Dictionary<string, string>
                    {
                        { "windows", "https://github.com/rhasspy/piper/releases/download/v1.2.0/piper_windows_amd64.zip" },
                        { "linux", "https://github.com/rhasspy/piper/releases/download/v1.2.0/piper_linux_x86_64.tar.gz" }
                    },
                    ExtractDir = "piper",
                    Entrypoint = platform == "windows" ? "piper.exe" : "piper",
                    LicenseUrl = "https://github.com/rhasspy/piper/blob/master/LICENSE.md",
                    Models = new List<ModelEntry>
                    {
                        new ModelEntry
                        {
                            Id = "en_US-lessac-medium",
                            Name = "English US (Lessac Medium)",
                            Url = "https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/lessac/medium/en_US-lessac-medium.onnx",
                            SizeBytes = 63201308
                        }
                    }
                },
                new EngineManifestEntry
                {
                    Id = "mimic3",
                    Name = "Mimic3 TTS",
                    Version = "latest",
                    Description = "Privacy-focused local TTS by Mycroft AI",
                    SizeBytes = 100000000, // ~100MB
                    Sha256 = "",
                    ArchiveType = "git",
                    Urls = new Dictionary<string, string>
                    {
                        { "windows", "https://github.com/MycroftAI/mimic3.git" },
                        { "linux", "https://github.com/MycroftAI/mimic3.git" }
                    },
                    Entrypoint = "mimic3-server",
                    DefaultPort = 59125,
                    HealthCheck = new HealthCheckConfig
                    {
                        Url = "/api/voices",
                        TimeoutSeconds = 30
                    },
                    LicenseUrl = "https://github.com/MycroftAI/mimic3/blob/master/LICENSE"
                }
            }
        };
    }

    public void ClearCache()
    {
        _cachedManifest = null;
    }
}
