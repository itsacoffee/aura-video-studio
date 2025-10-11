using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Providers.Validation;
using Microsoft.Extensions.Logging;

namespace Aura.Api.Services;

/// <summary>
/// Service for running preflight checks on providers based on the selected profile
/// </summary>
public class PreflightService
{
    private readonly ILogger<PreflightService> _logger;
    private readonly ProviderValidationService _validationService;
    private readonly IKeyStore _keyStore;
    private readonly ProviderSettings _providerSettings;

    public PreflightService(
        ILogger<PreflightService> logger,
        ProviderValidationService validationService,
        IKeyStore keyStore,
        ProviderSettings providerSettings)
    {
        _logger = logger;
        _validationService = validationService;
        _keyStore = keyStore;
        _providerSettings = providerSettings;
    }

    /// <summary>
    /// Run preflight checks for the given profile
    /// </summary>
    public async Task<PreflightReport> RunPreflightAsync(string profileName, CancellationToken ct = default)
    {
        _logger.LogInformation("Running preflight checks for profile: {Profile}", profileName);

        var profile = GetProfileByName(profileName);
        if (profile == null)
        {
            return new PreflightReport
            {
                Ok = false,
                Stages = new[]
                {
                    new StageCheck
                    {
                        Stage = "Profile",
                        Status = CheckStatus.Fail,
                        Provider = profileName,
                        Message = $"Unknown profile: {profileName}",
                        Hint = "Valid profiles are: Free-Only, Balanced Mix, Pro-Max"
                    }
                }
            };
        }

        var stageChecks = new List<StageCheck>();

        // Check Script (LLM) stage
        var scriptCheck = await CheckScriptStageAsync(profile, ct);
        stageChecks.Add(scriptCheck);

        // Check TTS stage
        var ttsCheck = await CheckTtsStageAsync(profile, ct);
        stageChecks.Add(ttsCheck);

        // Check Visuals stage
        var visualsCheck = await CheckVisualsStageAsync(profile, ct);
        stageChecks.Add(visualsCheck);

        // Overall status: fail if any critical checks fail
        var hasCriticalFailure = stageChecks.Any(c => c.Status == CheckStatus.Fail);

        return new PreflightReport
        {
            Ok = !hasCriticalFailure,
            Stages = stageChecks.ToArray()
        };
    }

    /// <summary>
    /// Get full health matrix for all configured engines/providers
    /// Returns detailed status for all available providers across all categories
    /// </summary>
    public async Task<HealthMatrix> GetHealthMatrixAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Generating full health matrix for all providers");

        var llmProviders = new List<ProviderHealth>();
        var ttsProviders = new List<ProviderHealth>();
        var visualProviders = new List<ProviderHealth>();

        // Check all LLM providers
        foreach (var providerName in new[] { "OpenAI", "Ollama", "RuleBased" })
        {
            var health = await GetProviderHealthAsync(providerName, "Script", ct);
            llmProviders.Add(health);
        }

        // Check all TTS providers
        foreach (var providerName in new[] { "ElevenLabs", "PlayHT", "Mimic3", "Piper", "Windows" })
        {
            var health = await GetProviderHealthAsync(providerName, "TTS", ct);
            ttsProviders.Add(health);
        }

        // Check all Visual providers
        foreach (var providerName in new[] { "Stability", "Runway", "StableDiffusion", "Stock" })
        {
            var health = await GetProviderHealthAsync(providerName, "Visuals", ct);
            visualProviders.Add(health);
        }

        return new HealthMatrix
        {
            LlmProviders = llmProviders.ToArray(),
            TtsProviders = ttsProviders.ToArray(),
            VisualProviders = visualProviders.ToArray(),
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    private async Task<ProviderHealth> GetProviderHealthAsync(string providerName, string category, CancellationToken ct)
    {
        // Special handling for always-available providers
        if (providerName == "RuleBased")
        {
            return new ProviderHealth
            {
                Name = providerName,
                Category = category,
                Status = ProviderStatus.Available,
                IsLocal = true,
                Message = "Rule-based provider - always available offline",
                LastChecked = DateTimeOffset.UtcNow
            };
        }

        if (providerName == "Windows")
        {
            return new ProviderHealth
            {
                Name = providerName,
                Category = category,
                Status = ProviderStatus.Available,
                IsLocal = true,
                Message = "Windows Speech Synthesis - always available",
                LastChecked = DateTimeOffset.UtcNow
            };
        }

        if (providerName == "Stock")
        {
            return new ProviderHealth
            {
                Name = providerName,
                Category = category,
                Status = ProviderStatus.Available,
                IsLocal = true,
                Message = "Stock images - always available",
                LastChecked = DateTimeOffset.UtcNow
            };
        }

        // For other providers, validate using the validation service
        try
        {
            var result = await _validationService.ValidateProvidersAsync(new[] { providerName }, ct);
            var providerResult = result.Results.FirstOrDefault();

            if (providerResult == null || !providerResult.Ok)
            {
                return new ProviderHealth
                {
                    Name = providerName,
                    Category = category,
                    Status = ProviderStatus.Unavailable,
                    IsLocal = IsLocalProvider(providerName),
                    Message = providerResult?.Details ?? "Validation failed",
                    LastChecked = DateTimeOffset.UtcNow
                };
            }

            return new ProviderHealth
            {
                Name = providerName,
                Category = category,
                Status = ProviderStatus.Available,
                IsLocal = IsLocalProvider(providerName),
                Message = providerResult.Details,
                LastChecked = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking health for provider {Provider}", providerName);
            return new ProviderHealth
            {
                Name = providerName,
                Category = category,
                Status = ProviderStatus.Error,
                IsLocal = IsLocalProvider(providerName),
                Message = $"Health check error: {ex.Message}",
                LastChecked = DateTimeOffset.UtcNow
            };
        }
    }

    private static bool IsLocalProvider(string providerName)
    {
        return providerName switch
        {
            "Ollama" or "RuleBased" or "StableDiffusion" or "Mimic3" or "Piper" or "Windows" or "Stock" => true,
            _ => false
        };
    }

    private async Task<StageCheck> CheckScriptStageAsync(ProviderProfile profile, CancellationToken ct)
    {
        var stageTier = profile.Stages.GetValueOrDefault("Script", "Free");

        switch (stageTier)
        {
            case "Pro":
                // Require OpenAI to be configured and reachable
                return await ValidateProviderAsync("Script", "OpenAI", CheckStatus.Fail, ct);

            case "ProIfAvailable":
                // Try OpenAI first, fallback to Ollama
                var openAiCheck = await ValidateProviderAsync("Script", "OpenAI", CheckStatus.Warn, ct);
                if (openAiCheck.Status == CheckStatus.Pass)
                {
                    return openAiCheck;
                }

                // OpenAI not available, check Ollama as fallback
                var ollamaCheck = await ValidateProviderAsync("Script", "Ollama", CheckStatus.Warn, ct);
                if (ollamaCheck.Status == CheckStatus.Pass)
                {
                    return new StageCheck
                    {
                        Stage = "Script",
                        Status = CheckStatus.Pass,
                        Provider = "Ollama",
                        Message = "Using Ollama (OpenAI not available)",
                        Hint = "Configure OpenAI key for better quality"
                    };
                }

                return new StageCheck
                {
                    Stage = "Script",
                    Status = CheckStatus.Warn,
                    Provider = "None",
                    Message = "No LLM providers available",
                    Hint = "Configure OpenAI key or start Ollama at http://127.0.0.1:11434"
                };

            case "Free":
            default:
                // Use Ollama (local/free)
                return await ValidateProviderAsync("Script", "Ollama", CheckStatus.Warn, ct);
        }
    }

    private async Task<StageCheck> CheckTtsStageAsync(ProviderProfile profile, CancellationToken ct)
    {
        var stageTier = profile.Stages.GetValueOrDefault("TTS", "Windows");

        switch (stageTier)
        {
            case "Pro":
                // Require ElevenLabs to be configured and reachable
                return await ValidateProviderAsync("TTS", "ElevenLabs", CheckStatus.Fail, ct);

            case "ProIfAvailable":
                // Try ElevenLabs first
                var elevenLabsCheck = await ValidateProviderAsync("TTS", "ElevenLabs", CheckStatus.Warn, ct);
                if (elevenLabsCheck.Status == CheckStatus.Pass)
                {
                    return elevenLabsCheck;
                }

                // Fall back to PlayHT
                var playHtCheck = await ValidateProviderAsync("TTS", "PlayHT", CheckStatus.Warn, ct);
                if (playHtCheck.Status == CheckStatus.Pass)
                {
                    return new StageCheck
                    {
                        Stage = "TTS",
                        Status = CheckStatus.Pass,
                        Provider = "PlayHT",
                        Message = "Using PlayHT (ElevenLabs not available)",
                        Hint = "Configure ElevenLabs key for best quality"
                    };
                }

                // Fall back to Mimic3 (local)
                var mimic3Check = await ValidateProviderAsync("TTS", "Mimic3", CheckStatus.Warn, ct);
                if (mimic3Check.Status == CheckStatus.Pass)
                {
                    return new StageCheck
                    {
                        Stage = "TTS",
                        Status = CheckStatus.Pass,
                        Provider = "Mimic3 (local)",
                        Message = "Using local Mimic3 (Pro TTS not available)",
                        Hint = "Configure ElevenLabs or PlayHT for cloud TTS"
                    };
                }

                // Fall back to Piper (local)
                var piperCheck = await ValidateProviderAsync("TTS", "Piper", CheckStatus.Warn, ct);
                if (piperCheck.Status == CheckStatus.Pass)
                {
                    return new StageCheck
                    {
                        Stage = "TTS",
                        Status = CheckStatus.Pass,
                        Provider = "Piper (local)",
                        Message = "Using local Piper (Pro TTS not available)",
                        Hint = "Configure ElevenLabs or PlayHT for cloud TTS"
                    };
                }

                // Fall back to Windows TTS
                return new StageCheck
                {
                    Stage = "TTS",
                    Status = CheckStatus.Pass,
                    Provider = "Windows TTS",
                    Message = "Using Windows Speech Synthesis (Pro/local TTS not available)",
                    Hint = "Configure ElevenLabs/PlayHT or install Piper/Mimic3 for better quality"
                };

            case "Mimic3":
                // Use Mimic3 (local)
                return await ValidateProviderAsync("TTS", "Mimic3", CheckStatus.Warn, ct);

            case "Piper":
                // Use Piper (local)
                return await ValidateProviderAsync("TTS", "Piper", CheckStatus.Warn, ct);

            case "Windows":
            default:
                // Windows TTS is always available (no check needed)
                return new StageCheck
                {
                    Stage = "TTS",
                    Status = CheckStatus.Pass,
                    Provider = "Windows TTS",
                    Message = "Using Windows Speech Synthesis",
                    Hint = null
                };
        }
    }

    private async Task<StageCheck> CheckVisualsStageAsync(ProviderProfile profile, CancellationToken ct)
    {
        var stageTier = profile.Stages.GetValueOrDefault("Visuals", "Stock");

        switch (stageTier)
        {
            case "Pro":
            case "CloudPro":
                // Check for cloud providers (Stability or Runway)
                var stabilityCheck = await ValidateProviderAsync("Visuals", "Stability", CheckStatus.Warn, ct);
                if (stabilityCheck.Status == CheckStatus.Pass)
                {
                    return stabilityCheck;
                }

                var runwayCheck = await ValidateProviderAsync("Visuals", "Runway", CheckStatus.Warn, ct);
                if (runwayCheck.Status == CheckStatus.Pass)
                {
                    return runwayCheck;
                }

                // Cloud providers not available, try local SD as fallback
                var sdCheck = await ValidateProviderAsync("Visuals", "StableDiffusion", CheckStatus.Warn, ct);
                if (sdCheck.Status == CheckStatus.Pass)
                {
                    return new StageCheck
                    {
                        Stage = "Visuals",
                        Status = CheckStatus.Pass,
                        Provider = "StableDiffusion (local)",
                        Message = "Using local Stable Diffusion (cloud providers not configured)",
                        Hint = "Configure Stability AI or Runway API key for cloud generation"
                    };
                }

                // No cloud or local, fall back to stock
                return new StageCheck
                {
                    Stage = "Visuals",
                    Status = CheckStatus.Pass,
                    Provider = "Stock",
                    Message = "Using stock images (cloud providers not configured)",
                    Hint = "Configure Stability AI or Runway API key in Settings for cloud generation"
                };

            case "StockOrLocal":
                // Try Stable Diffusion, fallback to stock
                var localSdCheck = await ValidateProviderAsync("Visuals", "StableDiffusion", CheckStatus.Warn, ct);
                if (localSdCheck.Status == CheckStatus.Pass)
                {
                    return localSdCheck;
                }

                return new StageCheck
                {
                    Stage = "Visuals",
                    Status = CheckStatus.Pass,
                    Provider = "Stock",
                    Message = "Using stock images (SD WebUI not available)",
                    Hint = "Start SD WebUI at http://127.0.0.1:7860 with --api flag for local generation"
                };

            case "Stock":
            default:
                // Stock images always available
                return new StageCheck
                {
                    Stage = "Visuals",
                    Status = CheckStatus.Pass,
                    Provider = "Stock",
                    Message = "Using stock images",
                    Hint = null
                };
        }
    }

    private async Task<StageCheck> ValidateProviderAsync(
        string stage,
        string providerName,
        CheckStatus failureStatus,
        CancellationToken ct)
    {
        try
        {
            var result = await _validationService.ValidateProvidersAsync(new[] { providerName }, ct);
            var providerResult = result.Results.FirstOrDefault();

            if (providerResult == null)
            {
                return new StageCheck
                {
                    Stage = stage,
                    Status = failureStatus,
                    Provider = providerName,
                    Message = "Provider validation failed",
                    Hint = "Check provider configuration"
                };
            }

            if (providerResult.Ok)
            {
                return new StageCheck
                {
                    Stage = stage,
                    Status = CheckStatus.Pass,
                    Provider = providerName,
                    Message = providerResult.Details,
                    Hint = null
                };
            }

            return new StageCheck
            {
                Stage = stage,
                Status = failureStatus,
                Provider = providerName,
                Message = providerResult.Details,
                Hint = GetHintForProvider(providerName, providerResult.Details),
                Suggestions = GetSuggestionsForProvider(providerName, providerResult.Details)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating provider {Provider}", providerName);
            return new StageCheck
            {
                Stage = stage,
                Status = failureStatus,
                Provider = providerName,
                Message = $"Validation error: {ex.Message}",
                Hint = "Check logs for details"
            };
        }
    }

    private string? GetHintForProvider(string providerName, string details)
    {
        return providerName switch
        {
            "OpenAI" when details.Contains("API key") => "Configure your OpenAI API key in Settings",
            "OpenAI" => "Check OpenAI service status and API key",
            "ElevenLabs" when details.Contains("API key") => "Configure your ElevenLabs API key in Settings",
            "ElevenLabs" => "Check ElevenLabs service status and API key",
            "Ollama" => "Start Ollama service: ollama serve",
            "StableDiffusion" when details.Contains("--api") => "Start SD WebUI with --api flag. Requires NVIDIA GPU with 6GB+ VRAM",
            "StableDiffusion" when details.Contains("not running") => "Start SD WebUI at http://127.0.0.1:7860. Install from Downloads page if needed",
            "StableDiffusion" => "Ensure SD WebUI is running with --api flag. Requires NVIDIA GPU with 6GB+ VRAM",
            "Piper" => "Install Piper TTS from Downloads page or configure path in Settings",
            "Mimic3" => "Start Mimic3 server from Downloads page or ensure it's running on port 59125",
            _ => null
        };
    }

    private string[]? GetSuggestionsForProvider(string providerName, string details)
    {
        return providerName switch
        {
            "OpenAI" when details.Contains("API key") => new[] 
            { 
                "Get API key from https://platform.openai.com/api-keys",
                "Add key in Settings → API Keys → OpenAI"
            },
            "ElevenLabs" when details.Contains("API key") => new[] 
            { 
                "Get API key from https://elevenlabs.io",
                "Add key in Settings → API Keys → ElevenLabs"
            },
            "Ollama" when details.Contains("not running") => new[] 
            { 
                "Install Ollama from https://ollama.ai",
                "Run 'ollama serve' in terminal",
                "Ensure Ollama is listening on http://127.0.0.1:11434"
            },
            "Ollama" => new[] 
            { 
                "Run 'ollama pull llama2' to download a model",
                "Check if Ollama service is running: curl http://127.0.0.1:11434"
            },
            "StableDiffusion" when details.Contains("VRAM") => new[] 
            { 
                "GPU detected has insufficient VRAM (need 6GB+)",
                "Consider using SD 1.5 models which require less VRAM",
                "Or use cloud providers like Stability AI or Runway"
            },
            "StableDiffusion" when details.Contains("not running") => new[] 
            { 
                "Download SD WebUI from https://github.com/AUTOMATIC1111/stable-diffusion-webui",
                "Launch with: webui.bat --api (Windows) or ./webui.sh --api (Linux)",
                "Default URL: http://127.0.0.1:7860"
            },
            "StableDiffusion" => new[] 
            { 
                "Ensure SD WebUI is started with --api flag",
                "Check if accessible: curl http://127.0.0.1:7860/sdapi/v1/sd-models"
            },
            "Piper" => new[] 
            { 
                "Download Piper from https://github.com/rhasspy/piper",
                "Extract to a folder and configure path in Settings",
                "Download a voice model (e.g., en_US-lessac-medium)"
            },
            "Mimic3" => new[] 
            { 
                "Install Mimic3 from Downloads page",
                "Start server: mimic3-server",
                "Default port: 59125"
            },
            "Stability" or "Runway" when details.Contains("API key") => new[] 
            { 
                $"Sign up for {providerName} API at their website",
                $"Add API key in Settings → API Keys → {providerName}"
            },
            _ => null
        };
    }

    private ProviderProfile? GetProfileByName(string name)
    {
        return name switch
        {
            "Free-Only" => ProviderProfile.FreeOnly,
            "Balanced Mix" => ProviderProfile.BalancedMix,
            "Pro-Max" => ProviderProfile.ProMax,
            _ => null
        };
    }
}

/// <summary>
/// Status of a preflight check
/// </summary>
public enum CheckStatus
{
    /// <summary>Check passed successfully</summary>
    Pass,
    /// <summary>Check failed but can proceed with fallback</summary>
    Warn,
    /// <summary>Check failed and cannot proceed</summary>
    Fail
}

/// <summary>
/// Result of checking a single stage
/// </summary>
public record StageCheck
{
    /// <summary>Stage name (Script, TTS, Visuals)</summary>
    public string Stage { get; init; } = string.Empty;
    
    /// <summary>Check status</summary>
    public CheckStatus Status { get; init; }
    
    /// <summary>Provider being used</summary>
    public string Provider { get; init; } = string.Empty;
    
    /// <summary>Status message</summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>Hint for fixing issues (null if no issues)</summary>
    public string? Hint { get; init; }
    
    /// <summary>Actionable suggestions for improving setup (null if none)</summary>
    public string[]? Suggestions { get; init; }
}

/// <summary>
/// Complete preflight check report
/// </summary>
public record PreflightReport
{
    /// <summary>Whether all critical checks passed</summary>
    public bool Ok { get; init; }
    
    /// <summary>Individual stage checks</summary>
    public StageCheck[] Stages { get; init; } = Array.Empty<StageCheck>();
}

/// <summary>
/// Full health matrix for all providers
/// </summary>
public record HealthMatrix
{
    /// <summary>LLM/Script provider health</summary>
    public ProviderHealth[] LlmProviders { get; init; } = Array.Empty<ProviderHealth>();
    
    /// <summary>TTS provider health</summary>
    public ProviderHealth[] TtsProviders { get; init; } = Array.Empty<ProviderHealth>();
    
    /// <summary>Visual provider health</summary>
    public ProviderHealth[] VisualProviders { get; init; } = Array.Empty<ProviderHealth>();
    
    /// <summary>Timestamp when matrix was generated</summary>
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Health status of a single provider
/// </summary>
public record ProviderHealth
{
    /// <summary>Provider name</summary>
    public string Name { get; init; } = string.Empty;
    
    /// <summary>Provider category (Script, TTS, Visuals)</summary>
    public string Category { get; init; } = string.Empty;
    
    /// <summary>Current status</summary>
    public ProviderStatus Status { get; init; }
    
    /// <summary>Whether this is a local/offline provider</summary>
    public bool IsLocal { get; init; }
    
    /// <summary>Status message/details</summary>
    public string Message { get; init; } = string.Empty;
    
    /// <summary>Last time health was checked</summary>
    public DateTimeOffset LastChecked { get; init; }
}

/// <summary>
/// Provider status enum
/// </summary>
public enum ProviderStatus
{
    /// <summary>Provider is available and ready</summary>
    Available,
    
    /// <summary>Provider is not available (not running, not configured)</summary>
    Unavailable,
    
    /// <summary>Provider is installed but not running</summary>
    Installed,
    
    /// <summary>Provider update is available</summary>
    UpdateAvailable,
    
    /// <summary>Provider is unreachable (network/timeout issue)</summary>
    Unreachable,
    
    /// <summary>Provider is not supported on this platform/hardware</summary>
    Unsupported,
    
    /// <summary>Error checking provider status</summary>
    Error
}

