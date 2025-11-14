using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Aura.Core.Models;

namespace Aura.Core.Services;

/// <summary>
/// Loads and manages provider validation policies from configuration
/// </summary>
public class ProviderValidationPolicyLoader
{
    private readonly ILogger<ProviderValidationPolicyLoader> _logger;
    private readonly string _policyFilePath;
    private ProviderValidationPolicySet? _cachedPolicySet;
    private DateTime _lastLoadTime;

    public ProviderValidationPolicyLoader(ILogger<ProviderValidationPolicyLoader> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Look for providerTimeoutProfiles.json in the application directory
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _policyFilePath = Path.Combine(appDirectory, "providerTimeoutProfiles.json");
        
        // Fallback: Check parent directories for the configuration file
        if (!File.Exists(_policyFilePath))
        {
            var parentDir = Directory.GetParent(appDirectory);
            if (parentDir != null)
            {
                var candidatePath = Path.Combine(parentDir.FullName, "providerTimeoutProfiles.json");
                if (File.Exists(candidatePath))
                {
                    _policyFilePath = candidatePath;
                }
            }
        }

        _lastLoadTime = DateTime.MinValue;
    }

    /// <summary>
    /// Loads validation policies from configuration file
    /// </summary>
    public async Task<ProviderValidationPolicySet> LoadPoliciesAsync(CancellationToken ct = default)
    {
        try
        {
            // Check if we need to reload (file changed or not loaded yet)
            if (_cachedPolicySet != null && File.Exists(_policyFilePath))
            {
                var fileLastWrite = File.GetLastWriteTimeUtc(_policyFilePath);
                if (fileLastWrite <= _lastLoadTime)
                {
                    _logger.LogDebug("Using cached validation policies");
                    return _cachedPolicySet;
                }
            }

            if (!File.Exists(_policyFilePath))
            {
                _logger.LogWarning(
                    "Provider validation policy file not found at {Path}, using defaults",
                    _policyFilePath);
                return CreateDefaultPolicySet();
            }

            _logger.LogInformation("Loading provider validation policies from {Path}", _policyFilePath);

            var json = await File.ReadAllTextAsync(_policyFilePath, ct).ConfigureAwait(false);
            var config = JsonSerializer.Deserialize<TimeoutProfilesConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config == null)
            {
                _logger.LogWarning("Failed to parse validation policy config, using defaults");
                return CreateDefaultPolicySet();
            }

            var policySet = new ProviderValidationPolicySet
            {
                ProviderCategoryMapping = config.ProviderMapping ?? new Dictionary<string, string>()
            };

            // Convert timeout profiles to validation policies
            if (config.Profiles != null)
            {
                foreach (var kvp in config.Profiles)
                {
                    var profile = kvp.Value;
                    var policy = new ProviderValidationPolicy
                    {
                        Category = kvp.Key,
                        NormalTimeoutMs = profile.NormalThresholdMs,
                        ExtendedTimeoutMs = profile.ExtendedThresholdMs,
                        MaxTimeoutMs = profile.DeepWaitThresholdMs,
                        RetryIntervalMs = profile.HeartbeatIntervalMs,
                        MaxRetries = 3,
                        ShowProgressDuringExtendedWait = true,
                        Description = profile.Description ?? string.Empty
                    };

                    policySet.Policies[kvp.Key] = policy;
                }
            }

            _cachedPolicySet = policySet;
            _lastLoadTime = DateTime.UtcNow;

            _logger.LogInformation(
                "Loaded {Count} validation policies from configuration",
                policySet.Policies.Count);

            return policySet;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading validation policies, using defaults");
            return CreateDefaultPolicySet();
        }
    }

    /// <summary>
    /// Creates default policy set when configuration is unavailable
    /// </summary>
    private ProviderValidationPolicySet CreateDefaultPolicySet()
    {
        return new ProviderValidationPolicySet
        {
            Policies = new Dictionary<string, ProviderValidationPolicy>
            {
                ["local_llm"] = new()
                {
                    Category = "local_llm",
                    NormalTimeoutMs = 30000,
                    ExtendedTimeoutMs = 180000,
                    MaxTimeoutMs = 300000,
                    RetryIntervalMs = 15000,
                    MaxRetries = 2,
                    Description = "Local LLM models (Ollama) - expect extended processing times"
                },
                ["cloud_llm"] = new()
                {
                    Category = "cloud_llm",
                    NormalTimeoutMs = 15000,
                    ExtendedTimeoutMs = 60000,
                    MaxTimeoutMs = 120000,
                    RetryIntervalMs = 5000,
                    MaxRetries = 3,
                    Description = "Cloud LLM APIs - generally faster with better infrastructure"
                },
                ["tts"] = new()
                {
                    Category = "tts",
                    NormalTimeoutMs = 20000,
                    ExtendedTimeoutMs = 60000,
                    MaxTimeoutMs = 120000,
                    RetryIntervalMs = 10000,
                    MaxRetries = 3,
                    Description = "TTS synthesis services"
                }
            },
            ProviderCategoryMapping = new Dictionary<string, string>
            {
                ["Ollama"] = "local_llm",
                ["OpenAI"] = "cloud_llm",
                ["Anthropic"] = "cloud_llm",
                ["Gemini"] = "cloud_llm",
                ["ElevenLabs"] = "tts",
                ["PlayHT"] = "tts"
            }
        };
    }
}

/// <summary>
/// Configuration structure matching providerTimeoutProfiles.json
/// </summary>
internal class TimeoutProfilesConfig
{
    public Dictionary<string, TimeoutProfile>? Profiles { get; set; }
    public Dictionary<string, string>? ProviderMapping { get; set; }
}

internal class TimeoutProfile
{
    public int NormalThresholdMs { get; set; }
    public int ExtendedThresholdMs { get; set; }
    public int DeepWaitThresholdMs { get; set; }
    public int HeartbeatIntervalMs { get; set; }
    public int StallSuspicionMultiplier { get; set; }
    public string? Description { get; set; }
}
