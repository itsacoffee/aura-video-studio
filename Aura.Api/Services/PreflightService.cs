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
                // Require cloud provider (not implemented yet, so we'll check SD as fallback)
                return new StageCheck
                {
                    Stage = "Visuals",
                    Status = CheckStatus.Warn,
                    Provider = "Cloud (not implemented)",
                    Message = "Pro visual providers not yet implemented",
                    Hint = "Using local Stable Diffusion or stock images as fallback"
                };

            case "StockOrLocal":
                // Try Stable Diffusion, fallback to stock
                var sdCheck = await ValidateProviderAsync("Visuals", "StableDiffusion", CheckStatus.Warn, ct);
                if (sdCheck.Status == CheckStatus.Pass)
                {
                    return sdCheck;
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
                Hint = GetHintForProvider(providerName, providerResult.Details)
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
            "StableDiffusion" when details.Contains("--api") => "Start SD WebUI with --api flag",
            "StableDiffusion" when details.Contains("not running") => "Start SD WebUI at http://127.0.0.1:7860",
            "StableDiffusion" => "Ensure SD WebUI is running with --api flag",
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
