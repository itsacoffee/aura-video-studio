using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Aura.Core.Services.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Orchestrator;

/// <summary>
/// Orchestrates script generation with provider routing and fallback logic
/// Now delegates to LlmStageAdapter for unified orchestration
/// </summary>
public class ScriptOrchestrator
{
    private readonly ILogger<ScriptOrchestrator> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ProviderMixer _providerMixer;
    private readonly Func<Dictionary<string, ILlmProvider>>? _providerFactory;
    private Dictionary<string, ILlmProvider>? _providers;
    private readonly object _lock = new object();
    private volatile LlmStageAdapter? _stageAdapter;
    private readonly OllamaDetectionService? _ollamaDetectionService;
    private readonly ProviderSettings? _providerSettings;

    /// <summary>
    /// Constructor with pre-created providers (for backward compatibility)
    /// </summary>
    public ScriptOrchestrator(
        ILogger<ScriptOrchestrator> logger,
        ILoggerFactory loggerFactory,
        ProviderMixer providerMixer,
        Dictionary<string, ILlmProvider> providers,
        OllamaDetectionService? ollamaDetectionService = null,
        ProviderSettings? providerSettings = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _providerMixer = providerMixer;
        _providers = providers;
        _providerFactory = null;
        _ollamaDetectionService = ollamaDetectionService;
        _providerSettings = providerSettings;
    }

    /// <summary>
    /// Constructor with factory delegate for lazy provider initialization (recommended)
    /// </summary>
    public ScriptOrchestrator(
        ILogger<ScriptOrchestrator> logger,
        ILoggerFactory loggerFactory,
        ProviderMixer providerMixer,
        Func<Dictionary<string, ILlmProvider>> providerFactory,
        OllamaDetectionService? ollamaDetectionService = null,
        ProviderSettings? providerSettings = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _providerMixer = providerMixer;
        _providerFactory = providerFactory;
        _providers = null;
        _ollamaDetectionService = ollamaDetectionService;
        _providerSettings = providerSettings;
    }

    private Dictionary<string, ILlmProvider> GetProviders()
    {
        // If using factory delegate pattern, always call it to get fresh providers
        // This enables dynamic provider refresh (e.g., when Ollama becomes available)
        if (_providerFactory != null)
        {
            _logger.LogDebug("Refreshing LLM providers from factory (supports dynamic Ollama detection)");
            return _providerFactory();
        }

        // If using pre-created providers pattern (backward compatibility), return cached value
        if (_providers != null)
        {
            return _providers;
        }

        throw new InvalidOperationException("No providers or provider factory configured");
    }

    /// <summary>
    /// Ensures Ollama detection has completed before script generation.
    /// Waits up to 10 seconds for detection, then proceeds with available providers.
    /// </summary>
    private async Task EnsureOllamaDetectionCompleteAsync(CancellationToken ct)
    {
        if (_ollamaDetectionService == null)
        {
            _logger.LogDebug("OllamaDetectionService not available, skipping detection wait");
            return;
        }

        if (_ollamaDetectionService.IsDetectionComplete)
        {
            _logger.LogDebug("Ollama detection already complete");
            return;
        }

        _logger.LogInformation("Waiting for Ollama detection to complete...");
        var detectionCompleted = await _ollamaDetectionService.WaitForInitialDetectionAsync(
            TimeSpan.FromSeconds(30), ct).ConfigureAwait(false); // Lenient for slow system initialization

        if (detectionCompleted)
        {
            _logger.LogInformation("Ollama detection completed successfully");
        }
        else
        {
            _logger.LogWarning("Ollama detection did not complete within 30s timeout (lenient for slow systems), proceeding with available providers");
        }
    }

    /// <summary>
    /// Generate a script using the appropriate provider based on tier and availability.
    /// Uses the deterministic ResolveLlm method for provider selection.
    /// </summary>
    public async Task<ScriptResult> GenerateScriptDeterministicAsync(
        Brief brief,
        PlanSpec spec,
        string preferredTier,
        bool offlineOnly,
        CancellationToken ct)
    {
        _logger.LogInformation("Starting deterministic script generation for topic: {Topic}, preferredTier: {Tier}, offlineOnly: {OfflineOnly}",
            brief.Topic, preferredTier, offlineOnly);

        // Wait for Ollama detection to complete before selecting providers
        await EnsureOllamaDetectionCompleteAsync(ct).ConfigureAwait(false);

        // Get preferred provider from settings if available
        var preferredProvider = _providerSettings?.GetPreferredLlmProvider();

        // Use the new deterministic ResolveLlm method with preferred provider
        var decision = _providerMixer.ResolveLlm(GetProviders(), preferredTier, offlineOnly, preferredProvider);
        _providerMixer.LogDecision(decision);

        // If Pro is blocked in offline mode, return error immediately
        if (decision.ProviderName == "None")
        {
            return new ScriptResult
            {
                Success = false,
                ErrorCode = "E307",
                ErrorMessage = "Pro LLM providers require internet connection but system is in OfflineOnly mode. Please disable OfflineOnly mode or use Free providers.",
                Script = null,
                ProviderUsed = null,
                IsFallback = false
            };
        }

        var requestedProvider = decision.DowngradeChain.Length > 0 ? decision.DowngradeChain[0] : decision.ProviderName;

        // Try the selected provider
        var result = await TryGenerateWithProviderAsync(
            brief,
            spec,
            decision.ProviderName,
            decision.IsFallback,
            requestedProvider,
            decision.FallbackFrom,
            ct).ConfigureAwait(false);

        if (result.Success)
        {
            return result;
        }

        // If primary provider failed, try remaining providers in the downgrade chain
        var currentIndex = Array.IndexOf(decision.DowngradeChain, decision.ProviderName);
        if (currentIndex >= 0 && currentIndex < decision.DowngradeChain.Length - 1)
        {
            var primaryFailureReason = result.ErrorMessage ?? "Provider failed";
            _logger.LogWarning("Primary provider {Provider} failed, attempting fallback chain: {Reason}",
                decision.ProviderName, primaryFailureReason);

            // Try remaining providers in chain
            for (int i = currentIndex + 1; i < decision.DowngradeChain.Length; i++)
            {
                var fallbackProvider = decision.DowngradeChain[i];
                _logger.LogInformation("Falling back to {Provider}", fallbackProvider);

                result = await TryGenerateWithProviderAsync(
                    brief, spec, fallbackProvider, true, requestedProvider,
                    $"Primary provider {decision.ProviderName} failed: {primaryFailureReason}",
                    ct).ConfigureAwait(false);

                if (result.Success)
                {
                    _logger.LogWarning("Successfully downgraded from {Requested} to {Actual}",
                        requestedProvider, result.ProviderUsed);
                    return result;
                }
            }
        }

        // If we're here and RuleBased hasn't been tried, try it as guaranteed fallback
        if (!decision.DowngradeChain.Contains("RuleBased") && decision.ProviderName != "RuleBased")
        {
            _logger.LogInformation("Falling back to RuleBased provider (final guaranteed fallback)");
            result = await TryGenerateWithProviderAsync(
                brief, spec, "RuleBased", true, requestedProvider,
                "All providers in chain failed, final fallback to RuleBased",
                ct).ConfigureAwait(false);
            if (result.Success)
            {
                _logger.LogWarning("Successfully downgraded from {Requested} to {Actual} (final fallback)",
                    requestedProvider, result.ProviderUsed);
                return result;
            }
            else
            {
                _logger.LogError("CRITICAL: Even RuleBased fallback failed - no providers available");
            }
        }

        // All providers failed - provide detailed error message
        var providers = GetProviders();
        var errorMessage = "All LLM providers failed to generate script. ";
        if (providers.Count == 0)
        {
            errorMessage += "No LLM providers are available. Please configure at least one provider (Ollama, OpenAI, Gemini, or RuleBased).";
        }
        else
        {
            var availableProviders = string.Join(", ", providers.Keys);
            errorMessage += $"Available providers ({providers.Count}): {availableProviders}. All attempts failed.";
        }

        return new ScriptResult
        {
            Success = false,
            ErrorCode = "E300",
            ErrorMessage = errorMessage,
            Script = null,
            ProviderUsed = null,
            IsFallback = false,
            RequestedProvider = requestedProvider
        };
    }

    /// <summary>
    /// Generate a script using the appropriate provider based on tier and availability
    /// </summary>
    public async Task<ScriptResult> GenerateScriptAsync(
        Brief brief,
        PlanSpec spec,
        string preferredTier,
        bool offlineOnly,
        CancellationToken ct)
    {
        _logger.LogInformation("Starting script generation for topic: {Topic}, preferredTier: {Tier}, offlineOnly: {OfflineOnly}",
            brief.Topic, preferredTier, offlineOnly);

        // Wait for Ollama detection to complete before selecting providers
        await EnsureOllamaDetectionCompleteAsync(ct).ConfigureAwait(false);

        // Block Pro providers if offline-only mode
        if (offlineOnly && (preferredTier == "Pro" || preferredTier == "ProIfAvailable"))
        {
            _logger.LogWarning("Pro providers requested but system is in OfflineOnly mode (E307)");

            if (preferredTier == "Pro")
            {
                return new ScriptResult
                {
                    Success = false,
                    ErrorCode = "E307",
                    ErrorMessage = "Pro LLM providers require internet connection but system is in OfflineOnly mode. Please disable OfflineOnly mode or use Free providers.",
                    Script = null,
                    ProviderUsed = null,
                    IsFallback = false
                };
            }

            // ProIfAvailable downgrades gracefully
            _logger.LogInformation("ProIfAvailable downgrading to Free providers due to OfflineOnly mode");
            preferredTier = "Free";
        }

        // Get fresh provider list (factory delegate ensures we get latest providers)
        var providers = GetProviders();

        // Log available providers for debugging
        _logger.LogInformation("Available LLM providers: {Providers}", string.Join(", ", providers.Keys));

        // If Ollama was specifically requested, verify it's in the dictionary
        if (preferredTier == "Ollama" && !providers.ContainsKey("Ollama"))
        {
            _logger.LogError("Ollama was requested but is not in the providers dictionary. Available: {Providers}",
                string.Join(", ", providers.Keys));
            return new ScriptResult
            {
                Success = false,
                ErrorCode = "E308",
                ErrorMessage = "Ollama provider is not registered. Please ensure Ollama is properly configured.",
                Script = null,
                ProviderUsed = null,
                IsFallback = false,
                RequestedProvider = preferredTier
            };
        }

        // Select provider
        var selection = _providerMixer.SelectLlmProvider(providers, preferredTier);
        _providerMixer.LogSelection(selection);

        // If provider selection returned "None", it means the requested provider isn't available
        if (selection.SelectedProvider == "None")
        {
            _logger.LogError("Requested provider '{Provider}' is not available. Reason: {Reason}",
                preferredTier, selection.Reason);
            return new ScriptResult
            {
                Success = false,
                ErrorCode = "E308",
                ErrorMessage = selection.Reason ?? $"Requested provider '{preferredTier}' is not available. Please ensure the provider is properly configured and running.",
                Script = null,
                ProviderUsed = null,
                IsFallback = false,
                RequestedProvider = preferredTier
            };
        }

        var requestedProvider = selection.SelectedProvider;

        // Try primary provider
        var result = await TryGenerateWithProviderAsync(
            brief,
            spec,
            selection.SelectedProvider,
            selection.IsFallback,
            requestedProvider,
            null,
            ct).ConfigureAwait(false);

        if (result.Success)
        {
            return result;
        }

        // If a specific provider was explicitly requested (not a tier), don't fall back
        // Return the error so user knows their requested provider isn't available
        var isSpecificProviderRequest = preferredTier != "Pro" &&
                                       preferredTier != "ProIfAvailable" &&
                                       preferredTier != "Free" &&
                                       !string.IsNullOrWhiteSpace(preferredTier);

        if (isSpecificProviderRequest)
        {
            _logger.LogError("Requested provider '{Provider}' failed and will not fall back. Error: {Error}",
                preferredTier, result.ErrorMessage);
            return result; // Return the error instead of falling back
        }

        // If primary provider failed, try fallback chain (only for tier-based requests)
        {
            var primaryFailureReason = result.ErrorMessage ?? "Provider failed";
            _logger.LogWarning("Primary provider {Provider} failed, attempting fallback: {Reason}",
                selection.SelectedProvider, primaryFailureReason);

            // Refresh providers in case Ollama became available
            providers = GetProviders();

            // Try Ollama if not already tried
            if (selection.SelectedProvider != "Ollama" && providers.ContainsKey("Ollama"))
            {
                _logger.LogInformation("Falling back to Ollama");
                result = await TryGenerateWithProviderAsync(
                    brief, spec, "Ollama", true, requestedProvider,
                    $"Primary provider {requestedProvider} failed: {primaryFailureReason}",
                    ct).ConfigureAwait(false);
                if (result.Success)
                {
                    _logger.LogWarning("Successfully downgraded from {Requested} to {Actual}",
                        requestedProvider, result.ProviderUsed);
                    return result;
                }
            }

            // Finally, fall back to RuleBased (always available - guaranteed fallback)
            // Try this even if RuleBased is not in the providers dictionary
            if (selection.SelectedProvider != "RuleBased")
            {
                _logger.LogInformation("Falling back to RuleBased provider (final guaranteed fallback)");
                result = await TryGenerateWithProviderAsync(
                    brief, spec, "RuleBased", true, requestedProvider,
                    $"All higher-tier providers failed, final fallback to RuleBased",
                    ct).ConfigureAwait(false);
                if (result.Success)
                {
                    _logger.LogWarning("Successfully downgraded from {Requested} to {Actual} (final fallback)",
                        requestedProvider, result.ProviderUsed);
                    return result;
                }
                else
                {
                    _logger.LogError("CRITICAL: Even RuleBased fallback failed - no providers available");
                }
            }
        }

        // All providers failed - log comprehensive error summary
        _logger.LogError(
            "All LLM providers failed to generate script for topic: {Topic}. Requested tier: {Tier}, Available providers: {Providers}",
            brief.Topic, preferredTier, string.Join(", ", providers.Keys));

        return new ScriptResult
        {
            Success = false,
            ErrorCode = "E300",
            ErrorMessage = $"All LLM providers failed to generate script. Tried: {requestedProvider}. Please check provider logs for details.",
            Script = null,
            ProviderUsed = null,
            IsFallback = false,
            RequestedProvider = requestedProvider
        };
    }

    private async Task<ScriptResult> TryGenerateWithProviderAsync(
        Brief brief,
        PlanSpec spec,
        string providerName,
        bool isFallback,
        string? requestedProvider,
        string? downgradeReason,
        CancellationToken ct)
    {
        var providers = GetProviders();
        if (!providers.TryGetValue(providerName, out var provider))
        {
            // Special case: if RuleBased is requested but not in dictionary, instantiate it as last resort
            if (providerName == "RuleBased")
            {
                _logger.LogWarning("RuleBased provider not in registry, instantiating as guaranteed fallback");
                try
                {
                    // Instantiate RuleBased provider dynamically with correct logger type
                    var type = Type.GetType("Aura.Providers.Llm.RuleBasedLlmProvider, Aura.Providers");
                    if (type != null)
                    {
                        // Create a typed logger using reflection
                        var createLoggerMethod = typeof(LoggerFactoryExtensions)
                            .GetMethods()
                            .FirstOrDefault(m => m.Name == "CreateLogger" && m.IsGenericMethod && m.GetParameters().Length == 1);

                        if (createLoggerMethod != null)
                        {
                            var genericMethod = createLoggerMethod.MakeGenericMethod(type);
                            var logger = genericMethod.Invoke(null, new object[] { _loggerFactory });
                            provider = (ILlmProvider)Activator.CreateInstance(type, logger)!;
                            providers[providerName] = provider; // Cache it for future use
                        }
                        else
                        {
                            _logger.LogError("CreateLogger<T> method not found");
                            return new ScriptResult
                            {
                                Success = false,
                                ErrorCode = "E305",
                                ErrorMessage = "CreateLogger<T> method not found for RuleBased provider",
                                Script = null,
                                ProviderUsed = providerName,
                                IsFallback = isFallback,
                                RequestedProvider = requestedProvider,
                                DowngradeReason = downgradeReason
                            };
                        }
                    }
                    else
                    {
                        _logger.LogError("RuleBasedLlmProvider type not found via reflection");
                        return new ScriptResult
                        {
                            Success = false,
                            ErrorCode = "E305",
                            ErrorMessage = "RuleBased provider type not found",
                            Script = null,
                            ProviderUsed = providerName,
                            IsFallback = isFallback,
                            RequestedProvider = requestedProvider,
                            DowngradeReason = downgradeReason
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to instantiate RuleBased provider");
                    return new ScriptResult
                    {
                        Success = false,
                        ErrorCode = "E305",
                        ErrorMessage = $"Failed to instantiate RuleBased provider: {ex.Message}",
                        Script = null,
                        ProviderUsed = providerName,
                        IsFallback = isFallback,
                        RequestedProvider = requestedProvider,
                        DowngradeReason = downgradeReason
                    };
                }
            }
            else
            {
                _logger.LogWarning("Provider {Provider} not available", providerName);
                return new ScriptResult
                {
                    Success = false,
                    ErrorCode = "E305",
                    ErrorMessage = $"Provider {providerName} not available",
                    Script = null,
                    ProviderUsed = providerName,
                    IsFallback = isFallback,
                    RequestedProvider = requestedProvider,
                    DowngradeReason = downgradeReason
                };
            }
        }

        // Ensure provider is not null before proceeding
        if (provider == null)
        {
            _logger.LogError("Provider {Provider} is null after initialization", providerName);
            return new ScriptResult
            {
                Success = false,
                ErrorCode = "E305",
                ErrorMessage = $"Provider {providerName} failed to initialize",
                Script = null,
                ProviderUsed = providerName,
                IsFallback = isFallback,
                RequestedProvider = requestedProvider,
                DowngradeReason = downgradeReason
            };
        }

        // For Ollama, check availability before attempting generation
        // This prevents wasting time on a provider that's not actually running
        if (providerName == "Ollama")
        {
            try
            {
                var providerType = provider.GetType();
                var healthCheckMethod = providerType.GetMethod("IsServiceAvailableAsync");
                if (healthCheckMethod != null)
                {
                    var task = (Task<bool>)healthCheckMethod.Invoke(provider, new object[] { ct })!;
                    var isAvailable = await task.ConfigureAwait(false);
                if (!isAvailable)
                {
                    _logger.LogWarning("Ollama provider is not available (service not running)");
                    return new ScriptResult
                    {
                        Success = false,
                        ErrorCode = "E306",
                        ErrorMessage = "Ollama service is not running. Please start Ollama or use another provider.",
                        Script = null,
                        ProviderUsed = providerName,
                        IsFallback = isFallback,
                        RequestedProvider = requestedProvider,
                        DowngradeReason = downgradeReason
                    };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check Ollama availability, proceeding anyway");
                // Continue - the provider's DraftScriptAsync will handle the error
            }
        }

        try
        {
            var providerType = provider.GetType();
            var providerTypeName = providerType.Name;

            _logger.LogInformation("=== Attempting script generation with provider: {Provider} (Type: {Type}) ===",
                providerName, providerTypeName);
            _logger.LogInformation("Provider type: {Type}, Brief topic: {Topic}, Model: {Model}",
                providerTypeName, brief.Topic, brief.LlmParameters?.ModelOverride ?? "default");

            // CRITICAL: Verify we're not using RuleBased when Ollama should be available
            if (providerTypeName.Contains("RuleBased", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "CRITICAL: Script generation is using RuleBased provider instead of real LLM (Ollama). " +
                    "This will produce low-quality template scripts. Check Ollama is running and configured. " +
                    "Available providers should include Ollama if it's running.");
            }
            else if (providerTypeName.Contains("Mock", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "CRITICAL: Script generation is using Mock provider. This should never happen in production. " +
                    "Check LLM provider configuration.");
            }
            else
            {
                _logger.LogInformation(
                    "Using real LLM provider: {Provider} - this should be Ollama or another real LLM. " +
                    "If Ollama is running, you should see CPU/GPU utilization during script generation.",
                    providerTypeName);
            }

            string? script = null;
            try
            {
                var scriptStartTime = DateTime.UtcNow;
                script = await provider.DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);
                var scriptDuration = DateTime.UtcNow - scriptStartTime;

                _logger.LogInformation(
                    "=== Provider {Provider} completed script generation in {Duration}ms. " +
                    "Response length: {Length} chars. If this was Ollama, system utilization should show activity. ===",
                    providerName, scriptDuration.TotalMilliseconds, script?.Length ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Provider {Provider} threw exception during DraftScriptAsync. Exception type: {ExceptionType}",
                    providerName, ex.GetType().Name);
                throw; // Re-throw to be caught by outer catch block
            }

            if (script == null)
            {
                _logger.LogError("Provider {Provider} returned null script (not empty string, but null)", providerName);
                return new ScriptResult
                {
                    Success = false,
                    ErrorCode = "E302",
                    ErrorMessage = $"Provider {providerName} returned null script",
                    Script = null,
                    ProviderUsed = providerName,
                    IsFallback = isFallback,
                    RequestedProvider = requestedProvider,
                    DowngradeReason = downgradeReason
                };
            }

            if (string.IsNullOrWhiteSpace(script))
            {
                _logger.LogWarning("Provider {Provider} returned empty or whitespace-only script (length: {Length})",
                    providerName, script.Length);
                return new ScriptResult
                {
                    Success = false,
                    ErrorCode = "E302",
                    ErrorMessage = $"Provider {providerName} returned empty script",
                    Script = null,
                    ProviderUsed = providerName,
                    IsFallback = isFallback,
                    RequestedProvider = requestedProvider,
                    DowngradeReason = downgradeReason
                };
            }

            // Log preview of generated script for debugging
            var preview = script.Substring(0, Math.Min(300, script.Length));
            _logger.LogInformation("Successfully generated script with {Provider} ({Length} chars). Preview: {Preview}...",
                providerName, script.Length, preview.Replace('\n', ' ').Replace('\r', ' '));

            return new ScriptResult
            {
                Success = true,
                ErrorCode = null,
                ErrorMessage = null,
                Script = script,
                ProviderUsed = providerName,
                IsFallback = isFallback,
                RequestedProvider = requestedProvider,
                DowngradeReason = downgradeReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Provider {Provider} failed with exception. Type: {ExceptionType}, Message: {Message}, InnerException: {InnerException}",
                providerName, ex.GetType().Name, ex.Message, ex.InnerException?.Message ?? "none");

            // Extract more detailed error message based on exception type
            var errorMessage = $"Provider {providerName} failed";
            if (ex is InvalidOperationException)
            {
                errorMessage = ex.Message;
            }
            else if (ex is HttpRequestException httpEx)
            {
                errorMessage = $"HTTP error with {providerName}: {httpEx.Message}";
            }
            else if (ex is TaskCanceledException)
            {
                errorMessage = $"Provider {providerName} request timed out or was canceled";
            }
            else if (!string.IsNullOrWhiteSpace(ex.Message))
            {
                errorMessage = $"{providerName} error: {ex.Message}";
            }

            return new ScriptResult
            {
                Success = false,
                ErrorCode = "E300",
                ErrorMessage = errorMessage,
                Script = null,
                ProviderUsed = providerName,
                IsFallback = isFallback,
                RequestedProvider = requestedProvider,
                DowngradeReason = downgradeReason
            };
        }
    }

    /// <summary>
    /// Generate script using unified orchestrator (recommended - uses LlmStageAdapter)
    /// </summary>
    public async Task<ScriptResult> GenerateScriptUnifiedAsync(
        Brief brief,
        PlanSpec spec,
        string preferredTier,
        bool offlineOnly,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting unified script generation for topic: {Topic}, preferredTier: {Tier}, offlineOnly: {OfflineOnly}",
            brief.Topic, preferredTier, offlineOnly);

        var adapter = GetOrCreateStageAdapter();

        var result = await adapter.GenerateScriptAsync(brief, spec, preferredTier, offlineOnly, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return new ScriptResult
            {
                Success = false,
                ErrorCode = "E300",
                ErrorMessage = result.ErrorMessage ?? "Script generation failed",
                Script = null,
                ProviderUsed = result.ProviderUsed,
                IsFallback = false
            };
        }

        return new ScriptResult
        {
            Success = true,
            ErrorCode = null,
            ErrorMessage = null,
            Script = result.Data,
            ProviderUsed = result.ProviderUsed,
            IsFallback = false
        };
    }

    private LlmStageAdapter GetOrCreateStageAdapter()
    {
        if (_stageAdapter != null)
        {
            return _stageAdapter;
        }

        lock (_lock)
        {
            if (_stageAdapter != null)
            {
                return _stageAdapter;
            }

            var providers = GetProviders();
            var logger = _loggerFactory.CreateLogger<LlmStageAdapter>();
            _stageAdapter = new LlmStageAdapter(logger, providers, _providerMixer, _providerSettings);
            return _stageAdapter;
        }
    }
}

/// <summary>
/// Result of script generation attempt
///
/// Error Codes:
/// - E300: General script provider failure
/// - E301: Request timeout or cancellation
/// - E302: Provider returned empty/invalid script
/// - E303: Invalid enum value or input validation failure
/// - E304: Invalid plan parameters (duration, etc.)
/// - E305: Provider not available/not registered
/// - E307: Offline mode restriction (Pro providers blocked)
/// </summary>
public record ScriptResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Script { get; init; }
    public string? ProviderUsed { get; init; }
    public bool IsFallback { get; init; }

    /// <summary>
    /// The provider that was originally requested (if different from ProviderUsed due to fallback)
    /// </summary>
    public string? RequestedProvider { get; init; }

    /// <summary>
    /// Reason for downgrade/fallback if one occurred
    /// </summary>
    public string? DowngradeReason { get; init; }
}
