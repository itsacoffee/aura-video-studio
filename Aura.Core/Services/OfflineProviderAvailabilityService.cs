using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Hardware;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services;

/// <summary>
/// Service to detect and validate availability of offline providers
/// (Piper, Mimic3, Ollama, Stable Diffusion WebUI)
/// </summary>
public class OfflineProviderAvailabilityService
{
    private readonly ILogger<OfflineProviderAvailabilityService> _logger;
    private readonly HttpClient _httpClient;
    private readonly ProviderSettings _providerSettings;
    private readonly IHardwareDetector? _hardwareDetector;

    public OfflineProviderAvailabilityService(
        ILogger<OfflineProviderAvailabilityService> logger,
        HttpClient httpClient,
        ProviderSettings providerSettings,
        IHardwareDetector? hardwareDetector = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        _providerSettings = providerSettings;
        _hardwareDetector = hardwareDetector;
    }

    /// <summary>
    /// Check availability of all offline providers
    /// </summary>
    public async Task<OfflineProvidersStatus> CheckAllProvidersAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Checking availability of all offline providers");

        var results = new List<Task<OfflineProviderStatus>>
        {
            CheckPiperAsync(ct),
            CheckMimic3Async(ct),
            CheckOllamaAsync(ct),
            CheckStableDiffusionAsync(ct),
            CheckWindowsTtsAsync(ct)
        };

        var statuses = await Task.WhenAll(results);

        var overallStatus = new OfflineProvidersStatus
        {
            Piper = statuses[0],
            Mimic3 = statuses[1],
            Ollama = statuses[2],
            StableDiffusion = statuses[3],
            WindowsTts = statuses[4],
            CheckedAt = DateTime.UtcNow
        };

        var availableCount = statuses.Count(s => s.IsAvailable);
        _logger.LogInformation(
            "Offline providers check complete: {Available}/{Total} available",
            availableCount, statuses.Length);

        return overallStatus;
    }

    /// <summary>
    /// Check if Piper TTS is available
    /// </summary>
    public async Task<OfflineProviderStatus> CheckPiperAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;

        var piperPath = _providerSettings.PiperExecutablePath;
        var voiceModelPath = _providerSettings.PiperVoiceModelPath;

        if (string.IsNullOrWhiteSpace(piperPath))
        {
            return new OfflineProviderStatus
            {
                Name = "Piper TTS",
                IsAvailable = false,
                Message = "Piper executable path not configured",
                InstallationGuideUrl = "https://github.com/rhasspy/piper",
                Recommendations = new List<string>
                {
                    "Download Piper from GitHub releases",
                    "Configure the executable path in Settings → Provider Paths",
                    "Download a voice model (e.g., en_US-lessac-medium)"
                }
            };
        }

        if (!File.Exists(piperPath))
        {
            return new OfflineProviderStatus
            {
                Name = "Piper TTS",
                IsAvailable = false,
                Message = $"Piper executable not found at: {piperPath}",
                InstallationGuideUrl = "https://github.com/rhasspy/piper",
                Recommendations = new List<string>
                {
                    "Verify the Piper executable path is correct",
                    "Download Piper if not installed",
                    "Check file permissions"
                }
            };
        }

        if (string.IsNullOrWhiteSpace(voiceModelPath) || !File.Exists(voiceModelPath))
        {
            return new OfflineProviderStatus
            {
                Name = "Piper TTS",
                IsAvailable = false,
                Message = "Piper voice model not found",
                InstallationGuideUrl = "https://github.com/rhasspy/piper",
                Recommendations = new List<string>
                {
                    "Download a Piper voice model (.onnx file)",
                    "Configure the voice model path in Settings → Provider Paths",
                    "Recommended: en_US-lessac-medium for quality/speed balance"
                }
            };
        }

        return new OfflineProviderStatus
        {
            Name = "Piper TTS",
            IsAvailable = true,
            Message = "Piper TTS is ready",
            Version = await GetPiperVersionAsync(piperPath, ct),
            Details = new Dictionary<string, object>
            {
                ["ExecutablePath"] = piperPath,
                ["VoiceModel"] = Path.GetFileName(voiceModelPath)
            }
        };
    }

    /// <summary>
    /// Check if Mimic3 TTS server is available
    /// </summary>
    public async Task<OfflineProviderStatus> CheckMimic3Async(CancellationToken ct = default)
    {
        var baseUrl = _providerSettings.Mimic3BaseUrl ?? "http://127.0.0.1:59125";

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{baseUrl}/api/voices", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cts.Token);
                var voicesCount = 0;
                
                try
                {
                    var voices = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    voicesCount = voices?.Count ?? 0;
                }
                catch
                {
                    // Ignore parsing errors
                }

                return new OfflineProviderStatus
                {
                    Name = "Mimic3 TTS",
                    IsAvailable = true,
                    Message = "Mimic3 server is running",
                    Details = new Dictionary<string, object>
                    {
                        ["Url"] = baseUrl,
                        ["VoicesAvailable"] = voicesCount
                    }
                };
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Mimic3 connection timeout at {Url}", baseUrl);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Mimic3 connection failed at {Url}", baseUrl);
        }

        return new OfflineProviderStatus
        {
            Name = "Mimic3 TTS",
            IsAvailable = false,
            Message = $"Mimic3 server not running at {baseUrl}",
            InstallationGuideUrl = "https://github.com/MycroftAI/mimic3",
            Recommendations = new List<string>
            {
                "Install Mimic3 using Docker or pip",
                "Start the Mimic3 server: mimic3-server",
                "Verify server is accessible at " + baseUrl,
                "Configure custom URL in Settings if using different port"
            }
        };
    }

    /// <summary>
    /// Check if Ollama is available and get model recommendations
    /// </summary>
    public async Task<OfflineProviderStatus> CheckOllamaAsync(CancellationToken ct = default)
    {
        var baseUrl = _providerSettings.OllamaBaseUrl ?? "http://127.0.0.1:11434";

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{baseUrl}/api/tags", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cts.Token);
                var models = new List<string>();
                
                try
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(content);
                    if (json.TryGetProperty("models", out var modelsArray))
                    {
                        foreach (var model in modelsArray.EnumerateArray())
                        {
                            if (model.TryGetProperty("name", out var name))
                            {
                                models.Add(name.GetString() ?? "");
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }

                var recommendations = GetOllamaModelRecommendations(models);

                return new OfflineProviderStatus
                {
                    Name = "Ollama",
                    IsAvailable = true,
                    Message = models.Count > 0 
                        ? $"Ollama is running with {models.Count} model(s)" 
                        : "Ollama is running but no models installed",
                    Details = new Dictionary<string, object>
                    {
                        ["Url"] = baseUrl,
                        ["InstalledModels"] = models
                    },
                    Recommendations = recommendations
                };
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Ollama connection timeout at {Url}", baseUrl);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Ollama connection failed at {Url}", baseUrl);
        }

        return new OfflineProviderStatus
        {
            Name = "Ollama",
            IsAvailable = false,
            Message = $"Ollama not running at {baseUrl}",
            InstallationGuideUrl = "https://ollama.ai",
            Recommendations = new List<string>
            {
                "Download and install Ollama from https://ollama.ai",
                "Start Ollama service",
                "Pull a recommended model: ollama pull llama3.1:8b-q4_k_m",
                "Verify installation: ollama list"
            }
        };
    }

    /// <summary>
    /// Check if Stable Diffusion WebUI is available
    /// </summary>
    public async Task<OfflineProviderStatus> CheckStableDiffusionAsync(CancellationToken ct = default)
    {
        var baseUrl = _providerSettings.StableDiffusionWebUiUrl ?? "http://127.0.0.1:7860";

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await _httpClient.GetAsync($"{baseUrl}/sdapi/v1/sd-models", cts.Token);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cts.Token);
                var modelsCount = 0;
                
                try
                {
                    var models = JsonSerializer.Deserialize<JsonElement>(content);
                    if (models.ValueKind == JsonValueKind.Array)
                    {
                        modelsCount = models.GetArrayLength();
                    }
                }
                catch
                {
                    // Ignore parsing errors
                }

                var systemProfile = _hardwareDetector != null 
                    ? await _hardwareDetector.DetectSystemAsync() 
                    : null;

                var recommendations = GetStableDiffusionRecommendations(systemProfile);

                return new OfflineProviderStatus
                {
                    Name = "Stable Diffusion WebUI",
                    IsAvailable = true,
                    Message = $"Stable Diffusion WebUI is running with {modelsCount} model(s)",
                    Details = new Dictionary<string, object>
                    {
                        ["Url"] = baseUrl,
                        ["ModelsCount"] = modelsCount
                    },
                    Recommendations = recommendations
                };
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogDebug("Stable Diffusion WebUI connection timeout at {Url}", baseUrl);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Stable Diffusion WebUI connection failed at {Url}", baseUrl);
        }

        return new OfflineProviderStatus
        {
            Name = "Stable Diffusion WebUI",
            IsAvailable = false,
            Message = $"Stable Diffusion WebUI not running at {baseUrl}",
            InstallationGuideUrl = "https://github.com/AUTOMATIC1111/stable-diffusion-webui",
            Recommendations = new List<string>
            {
                "Install Stable Diffusion WebUI (requires NVIDIA GPU with 6GB+ VRAM)",
                "Download at least one checkpoint model (e.g., Stable Diffusion 1.5)",
                "Start WebUI with API enabled: ./webui.sh --api",
                "Configure custom URL in Settings if using different port"
            }
        };
    }

    /// <summary>
    /// Check if Windows TTS (SAPI) is available
    /// </summary>
    public async Task<OfflineProviderStatus> CheckWindowsTtsAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;

        if (!OperatingSystem.IsWindows())
        {
            return new OfflineProviderStatus
            {
                Name = "Windows TTS",
                IsAvailable = false,
                Message = "Windows TTS is only available on Windows",
                Recommendations = new List<string>
                {
                    "Use Piper or Mimic3 for offline TTS on non-Windows systems"
                }
            };
        }

        return new OfflineProviderStatus
        {
            Name = "Windows TTS",
            IsAvailable = true,
            Message = "Windows TTS (SAPI) is available",
            Details = new Dictionary<string, object>
            {
                ["Platform"] = Environment.OSVersion.ToString()
            },
            Recommendations = new List<string>
            {
                "Windows TTS provides basic quality suitable for testing",
                "Consider Piper or Mimic3 for better quality offline TTS"
            }
        };
    }

    private async Task<string> GetPiperVersionAsync(string piperPath, CancellationToken ct)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = piperPath,
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync(ct);
                var output = await process.StandardOutput.ReadToEndAsync(ct);
                return output.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get Piper version");
        }

        return "Unknown";
    }

    private List<string> GetOllamaModelRecommendations(List<string> installedModels)
    {
        var recommendations = new List<string>();

        if (installedModels.Count == 0)
        {
            recommendations.Add("No models installed. Recommended: ollama pull llama3.1:8b-q4_k_m");
        }
        else
        {
            var hasRecommendedModel = installedModels.Any(m => 
                m.Contains("llama3.1", StringComparison.OrdinalIgnoreCase) ||
                m.Contains("llama3", StringComparison.OrdinalIgnoreCase));

            if (!hasRecommendedModel)
            {
                recommendations.Add("Consider installing llama3.1:8b-q4_k_m for best balance of quality and speed");
            }
        }

        var systemProfile = _hardwareDetector != null 
            ? _hardwareDetector.DetectSystemAsync().GetAwaiter().GetResult() 
            : null;

        if (systemProfile != null)
        {
            var ramGB = systemProfile.RamGB;
            
            if (ramGB < 8)
            {
                recommendations.Add("Low RAM detected. Recommend using smaller models (3B or smaller)");
            }
            else if (ramGB >= 16)
            {
                recommendations.Add("Good RAM available. Can use 8B models comfortably");
            }

            if (systemProfile.Gpu?.VramGB >= 8)
            {
                recommendations.Add("GPU with sufficient VRAM detected. Consider GPU acceleration for faster inference");
            }
        }

        recommendations.Add("Configure keep-alive in Settings to reduce model loading time");

        return recommendations;
    }

    private List<string> GetStableDiffusionRecommendations(Core.Models.SystemProfile? systemProfile)
    {
        var recommendations = new List<string>();

        if (systemProfile == null)
        {
            return recommendations;
        }

        var vramGB = systemProfile.Gpu?.VramGB ?? 0;

        if (vramGB < 6)
        {
            recommendations.Add("GPU VRAM below 6GB. Stable Diffusion may not work or be very slow");
        }
        else if (vramGB < 8)
        {
            recommendations.Add("6-8GB VRAM: Use 512x512 resolution for best performance");
        }
        else if (vramGB < 12)
        {
            recommendations.Add("8-12GB VRAM: Can use 768x768 resolution comfortably");
        }
        else
        {
            recommendations.Add("12GB+ VRAM: Can use high resolutions and advanced features");
        }

        recommendations.Add("Enable xformers or sdp-attention for better performance");
        recommendations.Add("Consider using quality presets based on your hardware tier");

        return recommendations;
    }
}

/// <summary>
/// Overall status of all offline providers
/// </summary>
public record OfflineProvidersStatus
{
    public OfflineProviderStatus Piper { get; init; } = null!;
    public OfflineProviderStatus Mimic3 { get; init; } = null!;
    public OfflineProviderStatus Ollama { get; init; } = null!;
    public OfflineProviderStatus StableDiffusion { get; init; } = null!;
    public OfflineProviderStatus WindowsTts { get; init; } = null!;
    public DateTime CheckedAt { get; init; }

    /// <summary>
    /// True if at least one TTS provider is available
    /// </summary>
    public bool HasTtsProvider => Piper.IsAvailable || Mimic3.IsAvailable || WindowsTts.IsAvailable;

    /// <summary>
    /// True if Ollama is available
    /// </summary>
    public bool HasLlmProvider => Ollama.IsAvailable;

    /// <summary>
    /// True if Stable Diffusion is available
    /// </summary>
    public bool HasImageProvider => StableDiffusion.IsAvailable;

    /// <summary>
    /// True if all critical offline providers are available
    /// </summary>
    public bool IsFullyOperational => HasTtsProvider && HasLlmProvider;
}

/// <summary>
/// Status of a single offline provider
/// </summary>
public record OfflineProviderStatus
{
    public string Name { get; init; } = string.Empty;
    public bool IsAvailable { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Version { get; init; }
    public Dictionary<string, object> Details { get; init; } = new();
    public List<string> Recommendations { get; init; } = new();
    public string? InstallationGuideUrl { get; init; }
}
