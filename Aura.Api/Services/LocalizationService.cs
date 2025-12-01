using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Errors;
using Aura.Core.Models.Localization;
using Aura.Core.Providers;
using Aura.Core.Services.Localization;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Aura.Api.Services;

/// <summary>
/// Localization service with retry logic and circuit breaker pattern
/// Wraps the core TranslationService with resilience policies
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly TranslationService _translationService;
    private readonly ILlmProvider _llmProvider;
    private readonly ResiliencePipeline<TranslationResult> _translationPipeline;
    private readonly ResiliencePipeline<CulturalAnalysisResult> _analysisPipeline;

    public LocalizationService(
        ILogger<LocalizationService> logger,
        ILlmProvider llmProvider,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        
        // Create the translation service once for reuse across all methods
        _translationService = new TranslationService(
            loggerFactory.CreateLogger<TranslationService>(),
            llmProvider);

        // Build resilience pipeline for translation operations
        _translationPipeline = new ResiliencePipelineBuilder<TranslationResult>()
            .AddRetry(new RetryStrategyOptions<TranslationResult>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<TranslationResult>()
                    .Handle<ProviderException>(ex => ex.IsTransient)
                    .Handle<TimeoutException>()
                    .Handle<OperationCanceledException>(ex => false) // Don't retry cancellations
                    .Handle<Exception>(ex => IsTransientException(ex)),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Translation retry attempt {Attempt} after {Delay}ms. Exception: {ExceptionType}",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds,
                        args.Outcome.Exception?.GetType().Name ?? "None");
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<TranslationResult>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromMinutes(1),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<TranslationResult>()
                    .Handle<ProviderException>()
                    .Handle<TimeoutException>(),
                OnOpened = args =>
                {
                    _logger.LogError(
                        "Translation circuit breaker opened for {Duration}s due to repeated failures",
                        args.BreakDuration.TotalSeconds);
                    return ValueTask.CompletedTask;
                },
                OnClosed = _ =>
                {
                    _logger.LogInformation("Translation circuit breaker closed - service recovered");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = _ =>
                {
                    _logger.LogInformation("Translation circuit breaker half-open - testing service");
                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        // Build resilience pipeline for cultural analysis operations
        _analysisPipeline = new ResiliencePipelineBuilder<CulturalAnalysisResult>()
            .AddRetry(new RetryStrategyOptions<CulturalAnalysisResult>
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<CulturalAnalysisResult>()
                    .Handle<ProviderException>(ex => ex.IsTransient)
                    .Handle<TimeoutException>()
                    .Handle<OperationCanceledException>(ex => false)
                    .Handle<Exception>(ex => IsTransientException(ex)),
                OnRetry = args =>
                {
                    _logger.LogWarning(
                        "Cultural analysis retry attempt {Attempt} after {Delay}ms",
                        args.AttemptNumber,
                        args.RetryDelay.TotalMilliseconds);
                    return ValueTask.CompletedTask;
                }
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions<CulturalAnalysisResult>
            {
                FailureRatio = 0.5,
                MinimumThroughput = 5,
                SamplingDuration = TimeSpan.FromMinutes(1),
                BreakDuration = TimeSpan.FromSeconds(30),
                ShouldHandle = new PredicateBuilder<CulturalAnalysisResult>()
                    .Handle<ProviderException>()
                    .Handle<TimeoutException>()
            })
            .Build();
    }

    /// <inheritdoc />
    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting resilient translation: {Source} -> {Target}",
            request.SourceLanguage,
            request.TargetLanguage);

        return await _translationPipeline.ExecuteAsync(
            async ct => await _translationService.TranslateAsync(request, ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CulturalAnalysisResult> AnalyzeCulturalContentAsync(
        CulturalAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting resilient cultural analysis: {Language}/{Region}",
            request.TargetLanguage,
            request.TargetRegion);

        return await _analysisPipeline.ExecuteAsync(
            async ct => await _translationService.AnalyzeCulturalContentAsync(request, ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<BatchTranslationResult> BatchTranslateAsync(
        BatchTranslationRequest request,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting batch translation to {Count} languages",
            request.TargetLanguages.Count);

        return await _translationService.BatchTranslateAsync(request, progress, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public LanguageValidationResult ValidateLanguageCode(string languageCode)
    {
        // Accept ANY non-empty string for language description
        // The LLM can intelligently interpret any language input, including:
        // - ISO 639-1 codes (e.g., "en", "es", "fr")
        // - Full language names (e.g., "English", "Spanish")
        // - Regional variants (e.g., "English (US)", "Spanish (Mexico)")
        // - Historical/creative variants (e.g., "Medieval English", "Texan English in 1891")
        // - Fictional languages (e.g., "Pirate Speak", "Formal Victorian English")
        
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return new LanguageValidationResult(
                false,
                "Language description is required. You can use language codes (en, es), names (English, Spanish), or descriptive phrases (Medieval English, Formal Japanese).",
                ErrorCode: "INVALID_LANGUAGE_EMPTY");
        }

        // Accept any non-empty string - the LLM will interpret it
        return new LanguageValidationResult(
            true, 
            "Valid language description");
    }

    /// <inheritdoc />
    public async Task<bool> IsLlmProviderAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Try a simple completion to check if the provider is responding
            var testResponse = await _llmProvider.CompleteAsync(
                "Test connection. Reply with 'OK'.",
                cancellationToken).ConfigureAwait(false);

            return !string.IsNullOrWhiteSpace(testResponse);
        }
        catch (NotSupportedException)
        {
            // RuleBased provider doesn't support translation
            _logger.LogWarning("LLM provider does not support completion (likely RuleBased fallback)");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM provider availability check failed");
            return false;
        }
    }

    /// <summary>
    /// Determines if an exception is transient and should trigger a retry
    /// </summary>
    private static bool IsTransientException(Exception ex)
    {
        // Check for common transient exception patterns
        if (ex is TimeoutException)
            return true;

        if (ex is HttpRequestException)
        {
            // Network-related errors are typically transient
            return true;
        }

        // Check for timeout-related messages in OperationCanceledException
        // Note: User-initiated cancellations should NOT be retried, but timeout-induced ones may be
        if (ex is OperationCanceledException oce && 
            oce.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("too many requests", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (ex.Message.Contains("temporarily unavailable", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("service unavailable", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for Ollama-specific transient errors
        if (ex.Message.Contains("model is loading", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("busy", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}

/// <summary>
/// Interface for the localization service with retry support
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Translate content with automatic retry on transient failures
    /// </summary>
    Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyze cultural content with automatic retry on transient failures
    /// </summary>
    Task<CulturalAnalysisResult> AnalyzeCulturalContentAsync(
        CulturalAnalysisRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch translate to multiple languages
    /// </summary>
    Task<BatchTranslationResult> BatchTranslateAsync(
        BatchTranslationRequest request,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate a language code format and check if it's supported
    /// </summary>
    LanguageValidationResult ValidateLanguageCode(string languageCode);

    /// <summary>
    /// Check if the LLM provider is available for translation
    /// </summary>
    Task<bool> IsLlmProviderAvailableAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of language code validation
/// </summary>
public record LanguageValidationResult(
    bool IsValid,
    string Message,
    bool IsWarning = false,
    string? ErrorCode = null);
