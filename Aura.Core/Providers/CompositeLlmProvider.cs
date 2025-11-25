using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Configuration;
using Aura.Core.Models;
using Aura.Core.Models.Narrative;
using Aura.Core.Models.Streaming;
using Aura.Core.Models.Visual;
using Aura.Core.Orchestrator;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Providers;

/// <summary>
/// Composite LLM provider that orchestrates across all available providers (OpenAI, Azure, Gemini, Ollama, RuleBased)
/// with automatic fallback based on ProviderMixer decisions and secure key availability.
/// </summary>
public class CompositeLlmProvider : ILlmProvider
{
    private const string DefaultPreferredTier = "ProIfAvailable";

    private readonly ILogger<CompositeLlmProvider> _logger;
    private readonly LlmProviderFactory _providerFactory;
    private readonly ProviderMixer _providerMixer;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IKeyStore _keyStore;
    private readonly ProviderSettings _providerSettings;

    private readonly object _sync = new();
    private Dictionary<string, ILlmProvider>? _cachedProviders;
    private DateTime _lastProviderRefresh = DateTime.MinValue;
    private readonly TimeSpan _providerCacheDuration = TimeSpan.FromMinutes(5);

    public CompositeLlmProvider(
        ILogger<CompositeLlmProvider> logger,
        LlmProviderFactory providerFactory,
        ProviderMixer providerMixer,
        ILoggerFactory loggerFactory,
        IKeyStore keyStore,
        ProviderSettings providerSettings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _providerMixer = providerMixer ?? throw new ArgumentNullException(nameof(providerMixer));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _keyStore = keyStore ?? throw new ArgumentNullException(nameof(keyStore));
        _providerSettings = providerSettings ?? throw new ArgumentNullException(nameof(providerSettings));
    }

    public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        return ExecuteWithFallbackAsync(
            provider => provider.DraftScriptAsync(brief, spec, ct),
            "script generation",
            ct,
            DefaultPreferredTier,
            result => string.IsNullOrWhiteSpace(result));
    }

    public Task<string> CompleteAsync(string prompt, CancellationToken ct)
    {
        return ExecuteWithFallbackAsync(
            provider => provider.CompleteAsync(prompt, ct),
            "prompt completion",
            ct,
            DefaultPreferredTier,
            result => string.IsNullOrWhiteSpace(result) || result.Trim() == "{}" || result.Trim() == "[]" || result.Trim().Length < 10);
    }

    public Task<string> GenerateChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        LlmParameters? parameters = null,
        CancellationToken ct = default)
    {
        return ExecuteWithFallbackAsync(
            provider => provider.GenerateChatCompletionAsync(systemPrompt, userPrompt, parameters, ct),
            "chat completion",
            ct,
            DefaultPreferredTier,
            result => string.IsNullOrWhiteSpace(result));
    }

    public Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(
        string sceneText,
        string? previousSceneText,
        string videoGoal,
        CancellationToken ct)
    {
        return ExecuteWithFallbackAsync(
            provider => provider.AnalyzeSceneImportanceAsync(sceneText, previousSceneText, videoGoal, ct),
            "scene importance analysis",
            ct,
            DefaultPreferredTier,
            result => result == null,
            allowDefaultResultOnFailure: true);
    }

    public Task<VisualPromptResult?> GenerateVisualPromptAsync(
        string sceneText,
        string? previousSceneText,
        string videoTone,
        VisualStyle targetStyle,
        CancellationToken ct)
    {
        return ExecuteWithFallbackAsync(
            provider => provider.GenerateVisualPromptAsync(sceneText, previousSceneText, videoTone, targetStyle, ct),
            "visual prompt generation",
            ct,
            DefaultPreferredTier,
            result => result == null,
            allowDefaultResultOnFailure: true);
    }

    public Task<ContentComplexityAnalysisResult?> AnalyzeContentComplexityAsync(
        string sceneText,
        string? previousSceneText,
        string videoGoal,
        CancellationToken ct)
    {
        return ExecuteWithFallbackAsync(
            provider => provider.AnalyzeContentComplexityAsync(sceneText, previousSceneText, videoGoal, ct),
            "content complexity analysis",
            ct,
            DefaultPreferredTier,
            result => result == null,
            allowDefaultResultOnFailure: true);
    }

    public Task<SceneCoherenceResult?> AnalyzeSceneCoherenceAsync(
        string fromSceneText,
        string toSceneText,
        string videoGoal,
        CancellationToken ct)
    {
        return ExecuteWithFallbackAsync(
            provider => provider.AnalyzeSceneCoherenceAsync(fromSceneText, toSceneText, videoGoal, ct),
            "scene coherence analysis",
            ct,
            DefaultPreferredTier,
            result => result == null,
            allowDefaultResultOnFailure: true);
    }

    public Task<NarrativeArcResult?> ValidateNarrativeArcAsync(
        IReadOnlyList<string> sceneTexts,
        string videoGoal,
        string videoType,
        CancellationToken ct)
    {
        return ExecuteWithFallbackAsync(
            provider => provider.ValidateNarrativeArcAsync(sceneTexts, videoGoal, videoType, ct),
            "narrative arc validation",
            ct,
            DefaultPreferredTier,
            result => result == null,
            allowDefaultResultOnFailure: true);
    }

    public Task<string?> GenerateTransitionTextAsync(
        string fromSceneText,
        string toSceneText,
        string videoGoal,
        CancellationToken ct)
    {
        return ExecuteWithFallbackAsync(
            provider => provider.GenerateTransitionTextAsync(fromSceneText, toSceneText, videoGoal, ct),
            "transition text generation",
            ct,
            DefaultPreferredTier,
            result => string.IsNullOrWhiteSpace(result),
            allowDefaultResultOnFailure: true);
    }

    /// <summary>
    /// Whether this composite provider supports streaming (delegates to underlying providers)
    /// </summary>
    public bool SupportsStreaming => true;

    /// <summary>
    /// Get characteristics from the first available streaming provider
    /// </summary>
    public LlmProviderCharacteristics GetCharacteristics()
    {
        var providers = GetProviders();
        var chain = BuildProviderChain(providers, DefaultPreferredTier, "get characteristics");

        foreach (var providerName in chain)
        {
            if (providers.TryGetValue(providerName, out var provider) && provider != null)
            {
                if (provider.SupportsStreaming)
                {
                    return provider.GetCharacteristics();
                }
            }
        }

        return new LlmProviderCharacteristics
        {
            IsLocal = false,
            ExpectedFirstTokenMs = 1000,
            ExpectedTokensPerSec = 15,
            SupportsStreaming = true,
            ProviderTier = "Unknown"
        };
    }

    /// <summary>
    /// Stream script generation using the first available streaming provider
    /// </summary>
    public async IAsyncEnumerable<LlmStreamChunk> DraftScriptStreamAsync(
        Brief brief,
        PlanSpec spec,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var providers = GetProviders();
        var chain = BuildProviderChain(providers, DefaultPreferredTier, "streaming script generation");

        Exception? lastException = null;

        foreach (var providerName in chain)
        {
            if (!providers.TryGetValue(providerName, out var provider) || provider == null)
            {
                continue;
            }

            if (!provider.SupportsStreaming)
            {
                _logger.LogDebug("Provider {Provider} does not support streaming, skipping", providerName);
                continue;
            }

            _logger.LogInformation("Starting streaming script generation with provider {Provider}", providerName);

            var hasYieldedChunks = false;
            var streamEnumerator = provider.DraftScriptStreamAsync(brief, spec, ct).ConfigureAwait(false).GetAsyncEnumerator();
            LlmStreamChunk? errorChunk = null;

            try
            {
                while (true)
                {
                    LlmStreamChunk chunk;
                    try
                    {
                        if (!await streamEnumerator.MoveNextAsync())
                        {
                            break;
                        }
                        chunk = streamEnumerator.Current;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        lastException = ex;
                        _logger.LogWarning(ex, "Streaming script generation failed with provider {Provider}, trying next", providerName);

                        if (hasYieldedChunks)
                        {
                            errorChunk = new LlmStreamChunk
                            {
                                ProviderName = providerName,
                                Content = string.Empty,
                                TokenIndex = 0,
                                IsFinal = true,
                                ErrorMessage = $"Stream interrupted: {ex.Message}"
                            };
                        }
                        break;
                    }

                    hasYieldedChunks = true;
                    yield return chunk;

                    if (chunk.IsFinal)
                    {
                        if (!string.IsNullOrWhiteSpace(chunk.ErrorMessage))
                        {
                            _logger.LogWarning(
                                "Provider {Provider} completed with error: {Error}",
                                providerName,
                                chunk.ErrorMessage);
                            break;
                        }

                        yield break;
                    }
                }

                if (errorChunk != null)
                {
                    yield return errorChunk;
                    yield break;
                }

                if (hasYieldedChunks)
                {
                    yield break;
                }
            }
            finally
            {
                await streamEnumerator.DisposeAsync();
            }
        }

        yield return new LlmStreamChunk
        {
            ProviderName = "Composite",
            Content = string.Empty,
            TokenIndex = 0,
            IsFinal = true,
            ErrorMessage = lastException != null
                ? $"All streaming providers failed: {lastException.Message}"
                : "No streaming providers available"
        };
    }

    private Dictionary<string, ILlmProvider> GetProviders(bool forceRefresh = false)
    {
        lock (_sync)
        {
            if (forceRefresh ||
                _cachedProviders == null ||
                DateTime.UtcNow - _lastProviderRefresh > _providerCacheDuration)
            {
                _logger.LogInformation("Refreshing LLM provider registry (force: {Force})", forceRefresh);
                _cachedProviders = _providerFactory.CreateAvailableProviders(_loggerFactory);
                _lastProviderRefresh = DateTime.UtcNow;
            }

            return _cachedProviders;
        }
    }

    private List<string> BuildProviderChain(
        Dictionary<string, ILlmProvider> providers,
        string preferredTier,
        string operationName)
    {
        var offlineOnly = _keyStore.IsOfflineOnly();

        // CRITICAL: Check for user-configured preferred provider first - it takes precedence over everything
        var configuredPreferredProvider = _providerSettings.GetPreferredLlmProvider();
        if (!string.IsNullOrWhiteSpace(configuredPreferredProvider))
        {
            var normalizedPreferred = NormalizeProviderName(configuredPreferredProvider);
            _logger.LogInformation(
                "User-configured preferred LLM provider detected: {Preferred} (normalized: {Normalized}) for {Operation}",
                configuredPreferredProvider, normalizedPreferred, operationName);

            if (providers.ContainsKey(normalizedPreferred))
            {
                _logger.LogInformation(
                    "Using configured preferred provider {Provider} as primary for {Operation}",
                    normalizedPreferred, operationName);

                var chain = new List<string> { normalizedPreferred };

                // Add fallback chain after preferred provider
                // Build a fallback chain that excludes the preferred provider
                var decision = _providerMixer.ResolveLlm(providers, preferredTier, offlineOnly, normalizedPreferred);

                if (decision.DowngradeChain?.Length > 0)
                {
                    foreach (var providerName in decision.DowngradeChain)
                    {
                        if (providerName != normalizedPreferred && !chain.Contains(providerName))
                        {
                            chain.Add(providerName);
                        }
                    }
                }

                // Always add RuleBased as final fallback if not already present
                if (!chain.Contains("RuleBased") && providers.ContainsKey("RuleBased"))
                {
                    chain.Add("RuleBased");
                }

                _logger.LogInformation(
                    "Provider chain for {Operation} (preferred provider first): {Chain}",
                    operationName,
                    string.Join(" → ", chain));

                return chain;
            }
            else
            {
                _logger.LogWarning(
                    "Configured preferred provider {Provider} (normalized: {Normalized}) is not available. " +
                    "Available providers: {Available}. Falling back to tier-based selection.",
                    configuredPreferredProvider, normalizedPreferred, string.Join(", ", providers.Keys));
            }
        }

        var decision = _providerMixer.ResolveLlm(providers, preferredTier, offlineOnly);

        var chain = new List<string>();

        void AddIfAvailable(string? providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                return;
            }

            if (!providers.ContainsKey(providerName))
            {
                _logger.LogDebug("Provider {Provider} not registered for {Operation}, skipping", providerName, operationName);
                return;
            }

            if (!chain.Contains(providerName))
            {
                chain.Add(providerName);
            }
        }

        if (decision.ProviderName == "None")
        {
            _logger.LogWarning(
                "Preferred tier {Tier} unavailable for {Operation} (offlineOnly: {Offline}). Falling back to local providers.",
                preferredTier,
                operationName,
                offlineOnly);

            AddIfAvailable("Ollama");
            AddIfAvailable("RuleBased");
        }
        else
        {
            // For ProIfAvailable tier, prioritize Ollama for completion/translation/localization operations
            // This avoids trying misconfigured cloud providers first for localization tasks
            if (preferredTier == "ProIfAvailable" &&
                (operationName.Contains("completion", StringComparison.OrdinalIgnoreCase) ||
                 operationName.Contains("translation", StringComparison.OrdinalIgnoreCase) ||
                 operationName.Contains("localization", StringComparison.OrdinalIgnoreCase) ||
                 operationName.Contains("chat", StringComparison.OrdinalIgnoreCase) ||
                 operationName.Contains("ideation", StringComparison.OrdinalIgnoreCase)) &&
                providers.ContainsKey("Ollama"))
            {
                _logger.LogInformation("ProIfAvailable tier with Ollama available - prioritizing Ollama for {Operation}", operationName);
                AddIfAvailable("Ollama");
            }

            AddIfAvailable(decision.ProviderName);

            if (decision.DowngradeChain?.Length > 0)
            {
                foreach (var providerName in decision.DowngradeChain)
                {
                    // Skip cloud providers if Ollama is already in chain and we're prioritizing it
                    if (chain.Contains("Ollama") &&
                        (providerName == "OpenAI" || providerName == "Azure" || providerName == "Gemini" || providerName == "Anthropic"))
                    {
                        _logger.LogDebug("Skipping cloud provider {Provider} since Ollama is already prioritized", providerName);
                        continue;
                    }
                    AddIfAvailable(providerName);
                }
            }

            if (!chain.Contains("RuleBased"))
            {
                AddIfAvailable("RuleBased");
            }
        }

        if (chain.Count == 0 && providers.ContainsKey("RuleBased"))
        {
            chain.Add("RuleBased");
        }

        _logger.LogInformation(
            "Provider chain for {Operation}: {Chain}",
            operationName,
            chain.Count > 0 ? string.Join(" → ", chain) : "[none]");

        return chain;
    }

    /// <summary>
    /// Normalizes provider names to match DI registration keys
    /// </summary>
    private static string NormalizeProviderName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name ?? string.Empty;
        }

        // Strip model information from provider names (e.g., "Ollama (qwen3:4b)" -> "Ollama")
        var trimmedName = name.Trim();
        var parenIndex = trimmedName.IndexOf('(');
        if (parenIndex > 0)
        {
            trimmedName = trimmedName.Substring(0, parenIndex).Trim();
        }

        return trimmedName switch
        {
            "RuleBased" or "rulebased" or "Rule-Based" or "rule-based" => "RuleBased",
            "Ollama" or "ollama" => "Ollama",
            "OpenAI" or "openai" or "OpenAi" => "OpenAI",
            "AzureOpenAI" or "Azure" or "azure" or "AzureOpenAi" or "azureopenai" => "Azure",
            "Gemini" or "gemini" => "Gemini",
            "Anthropic" or "anthropic" or "Claude" or "claude" => "Anthropic",
            _ => trimmedName
        };
    }

    private async Task<T> ExecuteWithFallbackAsync<T>(
        Func<ILlmProvider, Task<T>> operation,
        string operationName,
        CancellationToken ct,
        string preferredTier,
        Func<T, bool>? shouldRetry,
        bool allowDefaultResultOnFailure = false)
    {
        var exceptions = new List<Exception>();

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var forceRefresh = attempt > 0;
            var providers = GetProviders(forceRefresh);
            if (providers == null || providers.Count == 0)
            {
                _logger.LogWarning("No LLM providers available in registry for {Operation} (attempt {Attempt})", operationName, attempt + 1);
                continue;
            }

            var chain = BuildProviderChain(providers, preferredTier, operationName);
            if (chain.Count == 0)
            {
                _logger.LogWarning("Provider chain empty for {Operation} (attempt {Attempt})", operationName, attempt + 1);
                continue;
            }

            foreach (var providerName in chain)
            {
                if (!providers.TryGetValue(providerName, out var provider) || provider == null)
                {
                    _logger.LogDebug("Resolved provider {Provider} not instantiated for {Operation}", providerName, operationName);
                    continue;
                }

                try
                {
                    ct.ThrowIfCancellationRequested();

                    _logger.LogInformation("Executing {Operation} with provider {Provider}", operationName, providerName);
                    var result = await operation(provider).ConfigureAwait(false);

                    if (shouldRetry != null && shouldRetry(result))
                    {
                        _logger.LogWarning(
                            "{Operation} via {Provider} returned insufficient result, attempting fallback",
                            operationName,
                            providerName);
                        continue;
                    }

                    return result;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (NotSupportedException ex)
                {
                    // NotSupportedException is expected for providers that don't support certain operations
                    // (e.g., RuleBased doesn't support chat completion)
                    // Skip it silently to avoid polluting error messages
                    _logger.LogDebug(
                        "Provider {Provider} does not support {Operation}, skipping (this is expected for some providers)",
                        providerName,
                        operationName);
                    // Don't add to exceptions list - this is expected behavior
                }
                catch (InvalidOperationException ex) when (ex.Message.Contains("404") || ex.Message.Contains("Not Found") || ex.Message.Contains("not found"))
                {
                    // 404 errors typically indicate misconfiguration (wrong endpoint, missing deployment, etc.)
                    // Skip these providers immediately - don't retry, don't add to error list
                    // This allows faster fallback to working providers like Ollama
                    _logger.LogInformation(
                        "Provider {Provider} returned 404/Not Found for {Operation}, skipping (likely misconfigured): {Message}",
                        providerName,
                        operationName,
                        ex.Message);
                    // Don't add to exceptions list - this is a configuration issue, not a transient error
                    continue; // Skip to next provider immediately
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("404") || ex.Message.Contains("Not Found"))
                {
                    // HTTP 404 errors indicate the endpoint doesn't exist
                    // Skip these providers immediately - don't retry, don't add to error list
                    _logger.LogInformation(
                        "Provider {Provider} returned HTTP 404 for {Operation}, skipping (endpoint not found): {Message}",
                        providerName,
                        operationName,
                        ex.Message);
                    // Don't add to exceptions list - this is a configuration issue
                    continue; // Skip to next provider immediately
                }
                catch (Exception ex)
                {
                    // Extract more details from HTTP exceptions for better diagnostics
                    var errorDetails = ex.Message;
                    if (ex is HttpRequestException httpEx)
                    {
                        errorDetails = $"HTTP error: {httpEx.Message}";
                        if (httpEx.Data.Contains("StatusCode"))
                        {
                            errorDetails += $" (Status: {httpEx.Data["StatusCode"]})";
                        }
                    }

                    _logger.LogWarning(
                        ex,
                        "{Operation} failed using provider {Provider} (attempt {Attempt}): {ErrorDetails}",
                        operationName,
                        providerName,
                        attempt + 1,
                        errorDetails);
                    exceptions.Add(ex);
                }
            }
        }

        if (allowDefaultResultOnFailure && exceptions.Count == 0)
        {
            _logger.LogInformation("{Operation} could not produce a result from available providers; returning default value", operationName);
            return default!;
        }

        // Build a more helpful error message that shows which providers were tried
        var providerNames = string.Join(", ", exceptions.Select((ex, idx) =>
        {
            // Try to extract provider name from exception message or stack trace
            var exMsg = ex.Message;
            if (exMsg.Contains("Ollama")) return "Ollama";
            if (exMsg.Contains("OpenAI")) return "OpenAI";
            if (exMsg.Contains("Azure")) return "Azure";
            if (exMsg.Contains("Gemini")) return "Gemini";
            if (exMsg.Contains("Anthropic")) return "Anthropic";
            return $"Provider{idx + 1}";
        }));

        var errorMessage = $"All LLM providers failed for {operationName}";
        if (exceptions.Count > 0)
        {
            errorMessage += $". Tried providers: {providerNames}";
            // Include the first exception's message for more context
            var firstEx = exceptions[0];
            if (firstEx is HttpRequestException httpEx)
            {
                errorMessage += $". First error: {httpEx.Message}";
            }
            else if (!string.IsNullOrWhiteSpace(firstEx.Message))
            {
                errorMessage += $". First error: {firstEx.Message}";
            }
        }

        throw new AggregateException(errorMessage, exceptions);
    }
}
