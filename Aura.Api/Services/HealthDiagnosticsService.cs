using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Api.Models.ApiModels.V1;
using Aura.Core.Configuration;
using Aura.Core.Dependencies;
using Aura.Core.Hardware;
using Aura.Core.Models;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Comprehensive health diagnostics service that validates all system components
/// </summary>
public class HealthDiagnosticsService
{
    private readonly ILogger<HealthDiagnosticsService> _logger;
    private readonly IFfmpegLocator _ffmpegLocator;
    private readonly HardwareDetector _hardwareDetector;
    private readonly ProviderSettings _providerSettings;
    private readonly IKeyStore _keyStore;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TtsProviderFactory _ttsProviderFactory;

    private const int ExternalCheckTimeoutMs = 2000;

    public HealthDiagnosticsService(
        ILogger<HealthDiagnosticsService> logger,
        IFfmpegLocator ffmpegLocator,
        HardwareDetector hardwareDetector,
        ProviderSettings providerSettings,
        IKeyStore keyStore,
        IHttpClientFactory httpClientFactory,
        TtsProviderFactory ttsProviderFactory)
    {
        _logger = logger;
        _ffmpegLocator = ffmpegLocator;
        _hardwareDetector = hardwareDetector;
        _providerSettings = providerSettings;
        _keyStore = keyStore;
        _httpClientFactory = httpClientFactory;
        _ttsProviderFactory = ttsProviderFactory;
    }

    /// <summary>
    /// Get high-level health summary
    /// </summary>
    public async Task<HealthSummaryResponse> GetHealthSummaryAsync(CancellationToken ct = default)
    {
        var details = await GetHealthDetailsAsync(ct).ConfigureAwait(false);
        
        var passed = details.Checks.Count(c => c.Status == HealthCheckStatus.Pass);
        var warnings = details.Checks.Count(c => c.Status == HealthCheckStatus.Warning);
        var failed = details.Checks.Count(c => c.Status == HealthCheckStatus.Fail);
        
        return new HealthSummaryResponse(
            OverallStatus: details.OverallStatus,
            IsReady: details.IsReady,
            TotalChecks: details.Checks.Count,
            PassedChecks: passed,
            WarningChecks: warnings,
            FailedChecks: failed,
            Timestamp: details.Timestamp);
    }

    /// <summary>
    /// Get detailed health information for all checks
    /// </summary>
    public async Task<HealthDetailsResponse> GetHealthDetailsAsync(CancellationToken ct = default)
    {
        var checks = new List<HealthCheckDetail>();

        // System checks
        checks.Add(await CheckConfigurationAsync(ct).ConfigureAwait(false));
        checks.Add(await CheckDiskSpaceAsync(ct).ConfigureAwait(false));

        // Video pipeline checks
        checks.Add(await CheckFfmpegAsync(ct).ConfigureAwait(false));
        checks.Add(await CheckGpuEncodersAsync(ct).ConfigureAwait(false));

        // LLM provider checks
        checks.AddRange(await CheckLlmProvidersAsync(ct).ConfigureAwait(false));

        // TTS provider checks
        checks.AddRange(await CheckTtsProvidersAsync(ct).ConfigureAwait(false));

        // Image provider checks
        checks.AddRange(await CheckImageProvidersAsync(ct).ConfigureAwait(false));

        // Determine overall status
        var hasFailedRequired = checks.Any(c => c.IsRequired && c.Status == HealthCheckStatus.Fail);
        var hasAnyFailed = checks.Any(c => c.Status == HealthCheckStatus.Fail);
        var hasWarnings = checks.Any(c => c.Status == HealthCheckStatus.Warning);

        string overallStatus;
        if (hasFailedRequired)
        {
            overallStatus = "unhealthy";
        }
        else if (hasAnyFailed || hasWarnings)
        {
            overallStatus = "degraded";
        }
        else
        {
            overallStatus = "healthy";
        }

        var isReady = !hasFailedRequired;

        return new HealthDetailsResponse(
            OverallStatus: overallStatus,
            IsReady: isReady,
            Checks: checks,
            Timestamp: DateTimeOffset.UtcNow);
    }

    private async Task<HealthCheckDetail> CheckConfigurationAsync(CancellationToken ct)
    {
        try
        {
            var hasMinimalConfig = Directory.Exists(_providerSettings.GetAuraDataDirectory());
            
            if (!hasMinimalConfig)
            {
                return new HealthCheckDetail(
                    Id: "config_present",
                    Name: "Configuration",
                    Category: HealthCheckCategory.Configuration,
                    Status: HealthCheckStatus.Fail,
                    IsRequired: true,
                    Message: "Configuration directory not found",
                    Data: new Dictionary<string, object>
                    {
                        ["expectedPath"] = _providerSettings.GetAuraDataDirectory()
                    },
                    RemediationHint: "Application needs to initialize. Try restarting the application.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.OpenSettings,
                            Label: "Open Settings",
                            Description: "Configure application settings",
                            NavigateTo: "/settings",
                            ExternalUrl: null,
                            Parameters: null)
                    });
            }

            return new HealthCheckDetail(
                Id: "config_present",
                Name: "Configuration",
                Category: HealthCheckCategory.Configuration,
                Status: HealthCheckStatus.Pass,
                IsRequired: true,
                Message: "Configuration initialized",
                Data: new Dictionary<string, object>
                {
                    ["path"] = _providerSettings.GetAuraDataDirectory()
                },
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking configuration");
            return CreateErrorCheck("config_present", "Configuration", HealthCheckCategory.Configuration, true, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckDiskSpaceAsync(CancellationToken ct)
    {
        try
        {
            var outputDir = _providerSettings.GetOutputDirectory();
            var driveInfo = new DriveInfo(Path.GetPathRoot(outputDir) ?? "/");
            
            var freeSpaceGB = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            var totalSpaceGB = driveInfo.TotalSize / (1024.0 * 1024.0 * 1024.0);
            
            var data = new Dictionary<string, object>
            {
                ["freeSpaceGB"] = Math.Round(freeSpaceGB, 2),
                ["totalSpaceGB"] = Math.Round(totalSpaceGB, 2),
                ["drive"] = driveInfo.Name
            };

            if (freeSpaceGB < 1.0)
            {
                return new HealthCheckDetail(
                    Id: "disk_space",
                    Name: "Disk Space",
                    Category: HealthCheckCategory.System,
                    Status: HealthCheckStatus.Fail,
                    IsRequired: true,
                    Message: $"Critical: Only {freeSpaceGB:F2} GB free. Need at least 1 GB.",
                    Data: data,
                    RemediationHint: "Free up disk space to continue. Video rendering requires at least 1 GB free space.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.OpenHelp,
                            Label: "Disk Cleanup Guide",
                            Description: "Learn how to free up disk space",
                            NavigateTo: null,
                            ExternalUrl: "https://support.microsoft.com/windows/disk-cleanup",
                            Parameters: null)
                    });
            }

            if (freeSpaceGB < 5.0)
            {
                return new HealthCheckDetail(
                    Id: "disk_space",
                    Name: "Disk Space",
                    Category: HealthCheckCategory.System,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: $"Low: {freeSpaceGB:F2} GB free. Recommend at least 5 GB.",
                    Data: data,
                    RemediationHint: "Consider freeing up disk space for optimal performance.",
                    RemediationActions: null);
            }

            return new HealthCheckDetail(
                Id: "disk_space",
                Name: "Disk Space",
                Category: HealthCheckCategory.System,
                Status: HealthCheckStatus.Pass,
                IsRequired: false,
                Message: $"Sufficient: {freeSpaceGB:F2} GB free",
                Data: data,
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking disk space");
            return CreateErrorCheck("disk_space", "Disk Space", HealthCheckCategory.System, true, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckFfmpegAsync(CancellationToken ct)
    {
        try
        {
            var result = await _ffmpegLocator.CheckAllCandidatesAsync(null, ct).ConfigureAwait(false);
            
            if (!result.Found || string.IsNullOrEmpty(result.FfmpegPath))
            {
                return new HealthCheckDetail(
                    Id: "ffmpeg_present",
                    Name: "FFmpeg",
                    Category: HealthCheckCategory.Video,
                    Status: HealthCheckStatus.Fail,
                    IsRequired: true,
                    Message: "FFmpeg not found. Video rendering disabled.",
                    Data: new Dictionary<string, object>
                    {
                        ["attemptedPaths"] = result.AttemptedPaths
                    },
                    RemediationHint: "Download and install FFmpeg to enable video rendering.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.Install,
                            Label: "Install FFmpeg",
                            Description: "Download FFmpeg from the Downloads page",
                            NavigateTo: "/downloads",
                            ExternalUrl: null,
                            Parameters: new Dictionary<string, string> { ["component"] = "ffmpeg" }),
                        new RemediationAction(
                            Type: RemediationActionType.Configure,
                            Label: "Configure Path",
                            Description: "Set FFmpeg path manually in Settings",
                            NavigateTo: "/settings?tab=video",
                            ExternalUrl: null,
                            Parameters: null)
                    });
            }

            return new HealthCheckDetail(
                Id: "ffmpeg_present",
                Name: "FFmpeg",
                Category: HealthCheckCategory.Video,
                Status: HealthCheckStatus.Pass,
                IsRequired: true,
                Message: $"FFmpeg found: {Path.GetFileName(result.FfmpegPath)}",
                Data: new Dictionary<string, object>
                {
                    ["path"] = result.FfmpegPath,
                    ["version"] = result.VersionString ?? "unknown"
                },
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking FFmpeg");
            return CreateErrorCheck("ffmpeg_present", "FFmpeg", HealthCheckCategory.Video, true, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckGpuEncodersAsync(CancellationToken ct)
    {
        try
        {
            var systemProfile = await _hardwareDetector.DetectSystemAsync().ConfigureAwait(false);
            
            var hasGpu = systemProfile.Gpu != null;
            var hasNvenc = systemProfile.EnableNVENC;
            var hasAmd = systemProfile.Gpu?.Vendor.Contains("AMD", StringComparison.OrdinalIgnoreCase) ?? false;
            var hasIntel = systemProfile.Gpu?.Vendor.Contains("Intel", StringComparison.OrdinalIgnoreCase) ?? false;

            var encoders = new List<string>();
            if (hasNvenc) encoders.Add("NVENC");
            if (hasAmd) encoders.Add("AMF");
            if (hasIntel) encoders.Add("QuickSync");

            var data = new Dictionary<string, object>
            {
                ["hasGpu"] = hasGpu,
                ["gpuVendor"] = systemProfile.Gpu?.Vendor ?? "None",
                ["availableEncoders"] = encoders.ToArray()
            };

            if (systemProfile.Gpu != null)
            {
                data["gpuModel"] = systemProfile.Gpu.Model;
                data["vramGB"] = systemProfile.Gpu.VramGB;
            }

            if (!hasGpu || encoders.Count == 0)
            {
                return new HealthCheckDetail(
                    Id: "gpu_encoders",
                    Name: "Hardware Video Encoding",
                    Category: HealthCheckCategory.Video,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: "No GPU hardware encoders detected. Will use CPU encoding (slower).",
                    Data: data,
                    RemediationHint: "GPU encoding is optional but significantly faster. Software encoding (x264) will be used.",
                    RemediationActions: null);
            }

            return new HealthCheckDetail(
                Id: "gpu_encoders",
                Name: "Hardware Video Encoding",
                Category: HealthCheckCategory.Video,
                Status: HealthCheckStatus.Pass,
                IsRequired: false,
                Message: $"Hardware encoders available: {string.Join(", ", encoders)}",
                Data: data,
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking GPU encoders");
            return CreateErrorCheck("gpu_encoders", "Hardware Video Encoding", HealthCheckCategory.Video, false, ex);
        }
    }

    private async Task<IEnumerable<HealthCheckDetail>> CheckLlmProvidersAsync(CancellationToken ct)
    {
        var checks = new List<HealthCheckDetail>();

        // RuleBased - always available
        checks.Add(new HealthCheckDetail(
            Id: "llm_rulebased",
            Name: "RuleBased LLM (Offline)",
            Category: HealthCheckCategory.LLM,
            Status: HealthCheckStatus.Pass,
            IsRequired: false,
            Message: "Template-based script generation available",
            Data: new Dictionary<string, object> { ["isLocal"] = true },
            RemediationHint: null,
            RemediationActions: null));

        // OpenAI
        checks.Add(await CheckOpenAIAsync(ct).ConfigureAwait(false));

        // Anthropic/Claude
        checks.Add(await CheckAnthropicAsync(ct).ConfigureAwait(false));

        // Google Gemini
        checks.Add(await CheckGeminiAsync(ct).ConfigureAwait(false));

        // Ollama
        checks.Add(await CheckOllamaAsync(ct).ConfigureAwait(false));

        return checks;
    }

    private async Task<HealthCheckDetail> CheckOpenAIAsync(CancellationToken ct)
    {
        try
        {
            var apiKey = _keyStore.GetKey("OpenAI");
            if (string.IsNullOrEmpty(apiKey))
            {
                return new HealthCheckDetail(
                    Id: "llm_openai",
                    Name: "OpenAI (GPT)",
                    Category: HealthCheckCategory.LLM,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: "API key not configured",
                    Data: new Dictionary<string, object> { ["configured"] = false },
                    RemediationHint: "Configure OpenAI API key for GPT-4 script generation.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.OpenSettings,
                            Label: "Add API Key",
                            Description: "Configure OpenAI API key in Settings",
                            NavigateTo: "/settings?tab=api-keys",
                            ExternalUrl: null,
                            Parameters: new Dictionary<string, string> { ["provider"] = "OpenAI" }),
                        new RemediationAction(
                            Type: RemediationActionType.OpenHelp,
                            Label: "Get API Key",
                            Description: "Sign up for OpenAI API access",
                            NavigateTo: null,
                            ExternalUrl: "https://platform.openai.com/api-keys",
                            Parameters: null)
                    });
            }

            // Quick connectivity check with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(ExternalCheckTimeoutMs);
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                var response = await client.GetAsync("https://api.openai.com/v1/models", timeoutCts.Token).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    return new HealthCheckDetail(
                        Id: "llm_openai",
                        Name: "OpenAI (GPT)",
                        Category: HealthCheckCategory.LLM,
                        Status: HealthCheckStatus.Pass,
                        IsRequired: false,
                        Message: "Connected and ready",
                        Data: new Dictionary<string, object> { ["configured"] = true, ["reachable"] = true },
                        RemediationHint: null,
                        RemediationActions: null);
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout - not critical
            }
            catch (HttpRequestException)
            {
                // Network error - not critical
            }

            return new HealthCheckDetail(
                Id: "llm_openai",
                Name: "OpenAI (GPT)",
                Category: HealthCheckCategory.LLM,
                Status: HealthCheckStatus.Warning,
                IsRequired: false,
                Message: "API key configured but connection check failed",
                Data: new Dictionary<string, object> { ["configured"] = true, ["reachable"] = false },
                RemediationHint: "API key is configured but connectivity could not be verified. May still work.",
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking OpenAI");
            return CreateErrorCheck("llm_openai", "OpenAI (GPT)", HealthCheckCategory.LLM, false, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckAnthropicAsync(CancellationToken ct)
    {
        try
        {
            var apiKey = _keyStore.GetKey("Anthropic");
            if (string.IsNullOrEmpty(apiKey))
            {
                return new HealthCheckDetail(
                    Id: "llm_anthropic",
                    Name: "Anthropic (Claude)",
                    Category: HealthCheckCategory.LLM,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: "API key not configured",
                    Data: new Dictionary<string, object> { ["configured"] = false },
                    RemediationHint: "Configure Anthropic API key for Claude script generation.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.OpenSettings,
                            Label: "Add API Key",
                            Description: "Configure Anthropic API key in Settings",
                            NavigateTo: "/settings?tab=api-keys",
                            ExternalUrl: null,
                            Parameters: new Dictionary<string, string> { ["provider"] = "Anthropic" }),
                        new RemediationAction(
                            Type: RemediationActionType.OpenHelp,
                            Label: "Get API Key",
                            Description: "Sign up for Anthropic API access",
                            NavigateTo: null,
                            ExternalUrl: "https://console.anthropic.com",
                            Parameters: null)
                    });
            }

            return new HealthCheckDetail(
                Id: "llm_anthropic",
                Name: "Anthropic (Claude)",
                Category: HealthCheckCategory.LLM,
                Status: HealthCheckStatus.Pass,
                IsRequired: false,
                Message: "API key configured",
                Data: new Dictionary<string, object> { ["configured"] = true },
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Anthropic");
            return CreateErrorCheck("llm_anthropic", "Anthropic (Claude)", HealthCheckCategory.LLM, false, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckGeminiAsync(CancellationToken ct)
    {
        try
        {
            var apiKey = _keyStore.GetKey("Gemini");
            if (string.IsNullOrEmpty(apiKey))
            {
                return new HealthCheckDetail(
                    Id: "llm_gemini",
                    Name: "Google Gemini",
                    Category: HealthCheckCategory.LLM,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: "API key not configured",
                    Data: new Dictionary<string, object> { ["configured"] = false },
                    RemediationHint: "Configure Google Gemini API key for AI script generation.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.OpenSettings,
                            Label: "Add API Key",
                            Description: "Configure Gemini API key in Settings",
                            NavigateTo: "/settings?tab=api-keys",
                            ExternalUrl: null,
                            Parameters: new Dictionary<string, string> { ["provider"] = "Gemini" }),
                        new RemediationAction(
                            Type: RemediationActionType.OpenHelp,
                            Label: "Get API Key",
                            Description: "Get Gemini API key from Google",
                            NavigateTo: null,
                            ExternalUrl: "https://makersuite.google.com/app/apikey",
                            Parameters: null)
                    });
            }

            return new HealthCheckDetail(
                Id: "llm_gemini",
                Name: "Google Gemini",
                Category: HealthCheckCategory.LLM,
                Status: HealthCheckStatus.Pass,
                IsRequired: false,
                Message: "API key configured",
                Data: new Dictionary<string, object> { ["configured"] = true },
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Gemini");
            return CreateErrorCheck("llm_gemini", "Google Gemini", HealthCheckCategory.LLM, false, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckOllamaAsync(CancellationToken ct)
    {
        try
        {
            var ollamaUrl = _providerSettings.GetOllamaUrl() ?? "http://127.0.0.1:11434";
            
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(ExternalCheckTimeoutMs);
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{ollamaUrl}/api/tags", timeoutCts.Token).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    return new HealthCheckDetail(
                        Id: "llm_ollama",
                        Name: "Ollama (Local AI)",
                        Category: HealthCheckCategory.LLM,
                        Status: HealthCheckStatus.Pass,
                        IsRequired: false,
                        Message: "Ollama service is running",
                        Data: new Dictionary<string, object> 
                        { 
                            ["url"] = ollamaUrl,
                            ["isLocal"] = true,
                            ["reachable"] = true
                        },
                        RemediationHint: null,
                        RemediationActions: null);
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout
            }
            catch (HttpRequestException)
            {
                // Connection failed
            }

            return new HealthCheckDetail(
                Id: "llm_ollama",
                Name: "Ollama (Local AI)",
                Category: HealthCheckCategory.LLM,
                Status: HealthCheckStatus.Warning,
                IsRequired: false,
                Message: "Ollama not running",
                Data: new Dictionary<string, object> { ["url"] = ollamaUrl, ["isLocal"] = true, ["reachable"] = false },
                RemediationHint: "Start Ollama service for local AI script generation.",
                RemediationActions: new[]
                {
                    new RemediationAction(
                        Type: RemediationActionType.Install,
                        Label: "Install Ollama",
                        Description: "Download and install Ollama",
                        NavigateTo: "/downloads",
                        ExternalUrl: null,
                        Parameters: new Dictionary<string, string> { ["component"] = "ollama" }),
                    new RemediationAction(
                        Type: RemediationActionType.Start,
                        Label: "Start Ollama",
                        Description: "Run 'ollama serve' to start the service",
                        NavigateTo: null,
                        ExternalUrl: null,
                        Parameters: new Dictionary<string, string> { ["command"] = "ollama serve" }),
                    new RemediationAction(
                        Type: RemediationActionType.OpenHelp,
                        Label: "Help",
                        Description: "Ollama installation guide",
                        NavigateTo: null,
                        ExternalUrl: "https://ollama.ai",
                        Parameters: null)
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Ollama");
            return CreateErrorCheck("llm_ollama", "Ollama (Local AI)", HealthCheckCategory.LLM, false, ex);
        }
    }

    private async Task<IEnumerable<HealthCheckDetail>> CheckTtsProvidersAsync(CancellationToken ct)
    {
        var checks = new List<HealthCheckDetail>();

        // Windows SAPI - always available on Windows
        checks.Add(CheckWindowsSAPI());

        // ElevenLabs
        checks.Add(await CheckElevenLabsAsync(ct).ConfigureAwait(false));

        // PlayHT
        checks.Add(await CheckPlayHTAsync(ct).ConfigureAwait(false));

        // Piper
        checks.Add(CheckPiper());

        // Mimic3
        checks.Add(await CheckMimic3Async(ct).ConfigureAwait(false));

        return checks;
    }

    private HealthCheckDetail CheckWindowsSAPI()
    {
        try
        {
            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            
            if (!isWindows)
            {
                return new HealthCheckDetail(
                    Id: "tts_windows_sapi",
                    Name: "Windows SAPI (Built-in TTS)",
                    Category: HealthCheckCategory.TTS,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: "Not available on this platform",
                    Data: new Dictionary<string, object> { ["platform"] = Environment.OSVersion.Platform.ToString() },
                    RemediationHint: "Windows SAPI only available on Windows. Use Piper or Mimic3 instead.",
                    RemediationActions: null);
            }

            return new HealthCheckDetail(
                Id: "tts_windows_sapi",
                Name: "Windows SAPI (Built-in TTS)",
                Category: HealthCheckCategory.TTS,
                Status: HealthCheckStatus.Pass,
                IsRequired: false,
                Message: "Windows Speech Synthesis available",
                Data: new Dictionary<string, object> { ["isLocal"] = true, ["isBuiltIn"] = true },
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Windows SAPI");
            return CreateErrorCheck("tts_windows_sapi", "Windows SAPI", HealthCheckCategory.TTS, false, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckElevenLabsAsync(CancellationToken ct)
    {
        try
        {
            var apiKey = _keyStore.GetKey("ElevenLabs");
            if (string.IsNullOrEmpty(apiKey))
            {
                return new HealthCheckDetail(
                    Id: "tts_elevenlabs",
                    Name: "ElevenLabs (Premium TTS)",
                    Category: HealthCheckCategory.TTS,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: "API key not configured",
                    Data: new Dictionary<string, object> { ["configured"] = false },
                    RemediationHint: "Configure ElevenLabs API key for premium voice synthesis.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.OpenSettings,
                            Label: "Add API Key",
                            Description: "Configure ElevenLabs API key in Settings",
                            NavigateTo: "/settings?tab=api-keys",
                            ExternalUrl: null,
                            Parameters: new Dictionary<string, string> { ["provider"] = "ElevenLabs" }),
                        new RemediationAction(
                            Type: RemediationActionType.OpenHelp,
                            Label: "Get API Key",
                            Description: "Sign up for ElevenLabs",
                            NavigateTo: null,
                            ExternalUrl: "https://elevenlabs.io",
                            Parameters: null)
                    });
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(ExternalCheckTimeoutMs);
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("xi-api-key", apiKey);
                var response = await client.GetAsync("https://api.elevenlabs.io/v1/voices", timeoutCts.Token).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    return new HealthCheckDetail(
                        Id: "tts_elevenlabs",
                        Name: "ElevenLabs (Premium TTS)",
                        Category: HealthCheckCategory.TTS,
                        Status: HealthCheckStatus.Pass,
                        IsRequired: false,
                        Message: "Connected and ready",
                        Data: new Dictionary<string, object> { ["configured"] = true, ["reachable"] = true },
                        RemediationHint: null,
                        RemediationActions: null);
                }
            }
            catch (OperationCanceledException) { }
            catch (HttpRequestException) { }

            return new HealthCheckDetail(
                Id: "tts_elevenlabs",
                Name: "ElevenLabs (Premium TTS)",
                Category: HealthCheckCategory.TTS,
                Status: HealthCheckStatus.Warning,
                IsRequired: false,
                Message: "API key configured but connection check failed",
                Data: new Dictionary<string, object> { ["configured"] = true, ["reachable"] = false },
                RemediationHint: "API key is configured but connectivity could not be verified. May still work.",
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ElevenLabs");
            return CreateErrorCheck("tts_elevenlabs", "ElevenLabs", HealthCheckCategory.TTS, false, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckPlayHTAsync(CancellationToken ct)
    {
        try
        {
            var apiKey = _keyStore.GetKey("PlayHT");
            if (string.IsNullOrEmpty(apiKey))
            {
                return new HealthCheckDetail(
                    Id: "tts_playht",
                    Name: "PlayHT (Premium TTS)",
                    Category: HealthCheckCategory.TTS,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: "API key not configured",
                    Data: new Dictionary<string, object> { ["configured"] = false },
                    RemediationHint: "Configure PlayHT API key for voice cloning and premium voices.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.OpenSettings,
                            Label: "Add API Key",
                            Description: "Configure PlayHT API key in Settings",
                            NavigateTo: "/settings?tab=api-keys",
                            ExternalUrl: null,
                            Parameters: new Dictionary<string, string> { ["provider"] = "PlayHT" }),
                        new RemediationAction(
                            Type: RemediationActionType.OpenHelp,
                            Label: "Get API Key",
                            Description: "Sign up for PlayHT",
                            NavigateTo: null,
                            ExternalUrl: "https://play.ht",
                            Parameters: null)
                    });
            }

            return new HealthCheckDetail(
                Id: "tts_playht",
                Name: "PlayHT (Premium TTS)",
                Category: HealthCheckCategory.TTS,
                Status: HealthCheckStatus.Pass,
                IsRequired: false,
                Message: "API key configured",
                Data: new Dictionary<string, object> { ["configured"] = true },
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking PlayHT");
            return CreateErrorCheck("tts_playht", "PlayHT", HealthCheckCategory.TTS, false, ex);
        }
    }

    private HealthCheckDetail CheckPiper()
    {
        try
        {
            var piperPath = _providerSettings.GetPiperPath();
            var hasPiper = !string.IsNullOrEmpty(piperPath) && File.Exists(piperPath);
            
            if (!hasPiper)
            {
                return new HealthCheckDetail(
                    Id: "tts_piper",
                    Name: "Piper (Offline TTS)",
                    Category: HealthCheckCategory.TTS,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: "Piper executable not found",
                    Data: new Dictionary<string, object> { ["configured"] = false, ["isLocal"] = true },
                    RemediationHint: "Install Piper for fast local text-to-speech.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.Install,
                            Label: "Install Piper",
                            Description: "Download Piper from Downloads page",
                            NavigateTo: "/downloads",
                            ExternalUrl: null,
                            Parameters: new Dictionary<string, string> { ["component"] = "piper" }),
                        new RemediationAction(
                            Type: RemediationActionType.Configure,
                            Label: "Configure Path",
                            Description: "Set Piper path in Settings",
                            NavigateTo: "/settings?tab=local-engines",
                            ExternalUrl: null,
                            Parameters: null)
                    });
            }

            return new HealthCheckDetail(
                Id: "tts_piper",
                Name: "Piper (Offline TTS)",
                Category: HealthCheckCategory.TTS,
                Status: HealthCheckStatus.Pass,
                IsRequired: false,
                Message: "Piper executable found",
                Data: new Dictionary<string, object> 
                { 
                    ["configured"] = true, 
                    ["isLocal"] = true,
                    ["path"] = piperPath 
                },
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Piper");
            return CreateErrorCheck("tts_piper", "Piper", HealthCheckCategory.TTS, false, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckMimic3Async(CancellationToken ct)
    {
        try
        {
            var mimic3Url = _providerSettings.GetMimic3Url() ?? "http://127.0.0.1:59125";
            
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(ExternalCheckTimeoutMs);
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{mimic3Url}/api/voices", timeoutCts.Token).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    return new HealthCheckDetail(
                        Id: "tts_mimic3",
                        Name: "Mimic3 (Offline TTS)",
                        Category: HealthCheckCategory.TTS,
                        Status: HealthCheckStatus.Pass,
                        IsRequired: false,
                        Message: "Mimic3 service is running",
                        Data: new Dictionary<string, object> 
                        { 
                            ["url"] = mimic3Url,
                            ["isLocal"] = true,
                            ["reachable"] = true
                        },
                        RemediationHint: null,
                        RemediationActions: null);
                }
            }
            catch (OperationCanceledException) { }
            catch (HttpRequestException) { }

            return new HealthCheckDetail(
                Id: "tts_mimic3",
                Name: "Mimic3 (Offline TTS)",
                Category: HealthCheckCategory.TTS,
                Status: HealthCheckStatus.Warning,
                IsRequired: false,
                Message: "Mimic3 not running",
                Data: new Dictionary<string, object> { ["url"] = mimic3Url, ["isLocal"] = true, ["reachable"] = false },
                RemediationHint: "Start Mimic3 service for neural offline TTS.",
                RemediationActions: new[]
                {
                    new RemediationAction(
                        Type: RemediationActionType.Install,
                        Label: "Install Mimic3",
                        Description: "Download Mimic3 from Downloads page",
                        NavigateTo: "/downloads",
                        ExternalUrl: null,
                        Parameters: new Dictionary<string, string> { ["component"] = "mimic3" }),
                    new RemediationAction(
                        Type: RemediationActionType.Start,
                        Label: "Start Mimic3",
                        Description: "Run 'mimic3-server' to start the service",
                        NavigateTo: null,
                        ExternalUrl: null,
                        Parameters: new Dictionary<string, string> { ["command"] = "mimic3-server" })
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Mimic3");
            return CreateErrorCheck("tts_mimic3", "Mimic3", HealthCheckCategory.TTS, false, ex);
        }
    }

    private async Task<IEnumerable<HealthCheckDetail>> CheckImageProvidersAsync(CancellationToken ct)
    {
        var checks = new List<HealthCheckDetail>();

        // Stock images - always available
        checks.Add(new HealthCheckDetail(
            Id: "image_stock",
            Name: "Stock Images",
            Category: HealthCheckCategory.Image,
            Status: HealthCheckStatus.Pass,
            IsRequired: false,
            Message: "Built-in stock images available",
            Data: new Dictionary<string, object> { ["isBuiltIn"] = true },
            RemediationHint: null,
            RemediationActions: null));

        // Stable Diffusion WebUI
        checks.Add(await CheckStableDiffusionAsync(ct).ConfigureAwait(false));

        // Replicate
        checks.Add(await CheckReplicateAsync(ct).ConfigureAwait(false));

        return checks;
    }

    private async Task<HealthCheckDetail> CheckStableDiffusionAsync(CancellationToken ct)
    {
        try
        {
            var sdUrl = _providerSettings.GetStableDiffusionUrl() ?? "http://127.0.0.1:7860";
            
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(ExternalCheckTimeoutMs);
            
            try
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"{sdUrl}/sdapi/v1/sd-models", timeoutCts.Token).ConfigureAwait(false);
                
                if (response.IsSuccessStatusCode)
                {
                    return new HealthCheckDetail(
                        Id: "image_stable_diffusion",
                        Name: "Stable Diffusion WebUI",
                        Category: HealthCheckCategory.Image,
                        Status: HealthCheckStatus.Pass,
                        IsRequired: false,
                        Message: "SD WebUI is running with API enabled",
                        Data: new Dictionary<string, object> 
                        { 
                            ["url"] = sdUrl,
                            ["isLocal"] = true,
                            ["reachable"] = true
                        },
                        RemediationHint: null,
                        RemediationActions: null);
                }
            }
            catch (OperationCanceledException) { }
            catch (HttpRequestException) { }

            return new HealthCheckDetail(
                Id: "image_stable_diffusion",
                Name: "Stable Diffusion WebUI",
                Category: HealthCheckCategory.Image,
                Status: HealthCheckStatus.Warning,
                IsRequired: false,
                Message: "SD WebUI not running or API not enabled",
                Data: new Dictionary<string, object> { ["url"] = sdUrl, ["isLocal"] = true, ["reachable"] = false },
                RemediationHint: "Start SD WebUI with --api flag for local image generation.",
                RemediationActions: new[]
                {
                    new RemediationAction(
                        Type: RemediationActionType.Install,
                        Label: "Install SD WebUI",
                        Description: "Download Stable Diffusion WebUI",
                        NavigateTo: "/downloads",
                        ExternalUrl: null,
                        Parameters: new Dictionary<string, string> { ["component"] = "stable-diffusion" }),
                    new RemediationAction(
                        Type: RemediationActionType.Start,
                        Label: "Start SD WebUI",
                        Description: "Launch with --api flag",
                        NavigateTo: null,
                        ExternalUrl: null,
                        Parameters: new Dictionary<string, string> { ["command"] = "webui.bat --api" }),
                    new RemediationAction(
                        Type: RemediationActionType.SwitchProvider,
                        Label: "Use Stock Images",
                        Description: "Switch to built-in stock images",
                        NavigateTo: null,
                        ExternalUrl: null,
                        Parameters: new Dictionary<string, string> { ["provider"] = "Stock" })
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Stable Diffusion");
            return CreateErrorCheck("image_stable_diffusion", "Stable Diffusion WebUI", HealthCheckCategory.Image, false, ex);
        }
    }

    private async Task<HealthCheckDetail> CheckReplicateAsync(CancellationToken ct)
    {
        try
        {
            var apiKey = _keyStore.GetKey("Replicate");
            if (string.IsNullOrEmpty(apiKey))
            {
                return new HealthCheckDetail(
                    Id: "image_replicate",
                    Name: "Replicate (Cloud AI)",
                    Category: HealthCheckCategory.Image,
                    Status: HealthCheckStatus.Warning,
                    IsRequired: false,
                    Message: "API key not configured",
                    Data: new Dictionary<string, object> { ["configured"] = false },
                    RemediationHint: "Configure Replicate API key for cloud-based image generation.",
                    RemediationActions: new[]
                    {
                        new RemediationAction(
                            Type: RemediationActionType.OpenSettings,
                            Label: "Add API Key",
                            Description: "Configure Replicate API key in Settings",
                            NavigateTo: "/settings?tab=api-keys",
                            ExternalUrl: null,
                            Parameters: new Dictionary<string, string> { ["provider"] = "Replicate" }),
                        new RemediationAction(
                            Type: RemediationActionType.OpenHelp,
                            Label: "Get API Key",
                            Description: "Sign up for Replicate",
                            NavigateTo: null,
                            ExternalUrl: "https://replicate.com",
                            Parameters: null)
                    });
            }

            return new HealthCheckDetail(
                Id: "image_replicate",
                Name: "Replicate (Cloud AI)",
                Category: HealthCheckCategory.Image,
                Status: HealthCheckStatus.Pass,
                IsRequired: false,
                Message: "API key configured",
                Data: new Dictionary<string, object> { ["configured"] = true },
                RemediationHint: null,
                RemediationActions: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Replicate");
            return CreateErrorCheck("image_replicate", "Replicate", HealthCheckCategory.Image, false, ex);
        }
    }

    private HealthCheckDetail CreateErrorCheck(string id, string name, string category, bool isRequired, Exception ex)
    {
        return new HealthCheckDetail(
            Id: id,
            Name: name,
            Category: category,
            Status: HealthCheckStatus.Fail,
            IsRequired: isRequired,
            Message: $"Error during check: {ex.Message}",
            Data: new Dictionary<string, object> { ["error"] = ex.GetType().Name },
            RemediationHint: "An unexpected error occurred. Check logs for details.",
            RemediationActions: null);
    }
}
