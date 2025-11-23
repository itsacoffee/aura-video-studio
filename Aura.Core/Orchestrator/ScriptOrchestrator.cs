using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

    /// <summary>
    /// Constructor with pre-created providers (for backward compatibility)
    /// </summary>
    public ScriptOrchestrator(
        ILogger<ScriptOrchestrator> logger,
        ILoggerFactory loggerFactory,
        ProviderMixer providerMixer,
        Dictionary<string, ILlmProvider> providers,
        OllamaDetectionService? ollamaDetectionService = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _providerMixer = providerMixer;
        _providers = providers;
        _providerFactory = null;
        _ollamaDetectionService = ollamaDetectionService;
    }

    /// <summary>
    /// Constructor with factory delegate for lazy provider initialization (recommended)
    /// </summary>
    public ScriptOrchestrator(
        ILogger<ScriptOrchestrator> logger,
        ILoggerFactory loggerFactory,
        ProviderMixer providerMixer,
        Func<Dictionary<string, ILlmProvider>> providerFactory,
        OllamaDetectionService? ollamaDetectionService = null)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _providerMixer = providerMixer;
        _providerFactory = providerFactory;
        _providers = null;
        _ollamaDetectionService = ollamaDetectionService;
    }

    private Dictionary<string, ILlmProvider> GetProviders()
    {
        if (_providers != null)
        {
            return _providers;
        }

        if (_providerFactory == null)
        {
            throw new InvalidOperationException("No providers or provider factory configured");
        }

        lock (_lock)
        {
            if (_providers != null)
            {
                return _providers;
            }

            _logger.LogDebug("Lazily initializing LLM providers on first use");
            _providers = _providerFactory();
            return _providers;
        }
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
            TimeSpan.FromSeconds(10), ct).ConfigureAwait(false);

        if (detectionCompleted)
        {
            _logger.LogInformation("Ollama detection completed successfully");
        }
        else
        {
            _logger.LogWarning("Ollama detection did not complete within timeout, proceeding with available providers");
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

        // Use the new deterministic ResolveLlm method
        var decision = _providerMixer.ResolveLlm(GetProviders(), preferredTier, offlineOnly);
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

        // All providers failed
        return new ScriptResult
        {
            Success = false,
            ErrorCode = "E300",
            ErrorMessage = "All LLM providers failed to generate script",
            Script = null,
            ProviderUsed = null,
            IsFallback = false
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

        // Select provider
        var selection = _providerMixer.SelectLlmProvider(GetProviders(), preferredTier);
        _providerMixer.LogSelection(selection);

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

        // If primary provider failed, try fallback chain (always enabled for now)
        {
            var primaryFailureReason = result.ErrorMessage ?? "Provider failed";
            _logger.LogWarning("Primary provider {Provider} failed, attempting fallback: {Reason}", 
                selection.SelectedProvider, primaryFailureReason);
            
            // Try Ollama if not already tried
            if (selection.SelectedProvider != "Ollama" && GetProviders().ContainsKey("Ollama"))
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

        // All providers failed
        return new ScriptResult
        {
            Success = false,
            ErrorCode = "E300",
            ErrorMessage = "All LLM providers failed to generate script",
            Script = null,
            ProviderUsed = null,
            IsFallback = false
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

        try
        {
            _logger.LogInformation("=== Attempting script generation with provider: {Provider} ===", providerName);
            _logger.LogInformation("Provider type: {Type}, Brief topic: {Topic}", provider.GetType().Name, brief.Topic);
            var script = await provider.DraftScriptAsync(brief, spec, ct).ConfigureAwait(false);
            _logger.LogInformation("=== Provider {Provider} completed script generation ===", providerName);

            if (string.IsNullOrWhiteSpace(script))
            {
                _logger.LogWarning("Provider {Provider} returned empty script", providerName);
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

            _logger.LogInformation("Successfully generated script with {Provider} ({Length} chars)", 
                providerName, script.Length);

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
            _logger.LogWarning(ex, "Provider {Provider} failed: {Message}", providerName, ex.Message);
            return new ScriptResult
            {
                Success = false,
                ErrorCode = "E300",
                ErrorMessage = $"Provider {providerName} failed: {ex.Message}",
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
            _stageAdapter = new LlmStageAdapter(logger, providers, _providerMixer);
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
