using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    private readonly ILlmProvider _llmProvider;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ResiliencePipeline<TranslationResult> _translationPipeline;
    private readonly ResiliencePipeline<CulturalAnalysisResult> _analysisPipeline;

    /// <summary>
    /// Supported ISO 639-1 language codes for validation
    /// </summary>
    private static readonly HashSet<string> SupportedLanguageCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "en", "es", "fr", "de", "it", "pt", "zh", "ja", "ko", "ar", "ru", "hi", "bn", "pa",
        "nl", "sv", "no", "da", "fi", "pl", "tr", "el", "cs", "sk", "hu", "ro", "uk", "vi",
        "th", "id", "ms", "tl", "sw", "he", "fa", "ur", "ta", "te", "ml", "gu", "kn", "mr"
    };

    /// <summary>
    /// ISO 639-1 language code pattern (2-3 letters, optionally with region)
    /// </summary>
    private static readonly Regex LanguageCodePattern = new(
        @"^[a-zA-Z]{2,3}(-[a-zA-Z]{2,4})?$",
        RegexOptions.Compiled);

    public LocalizationService(
        ILogger<LocalizationService> logger,
        ILlmProvider llmProvider,
        ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _loggerFactory = loggerFactory;

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

        var translationService = new TranslationService(
            _loggerFactory.CreateLogger<TranslationService>(),
            _llmProvider);

        return await _translationPipeline.ExecuteAsync(
            async ct => await translationService.TranslateAsync(request, ct).ConfigureAwait(false),
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

        var translationService = new TranslationService(
            _loggerFactory.CreateLogger<TranslationService>(),
            _llmProvider);

        return await _analysisPipeline.ExecuteAsync(
            async ct => await translationService.AnalyzeCulturalContentAsync(request, ct).ConfigureAwait(false),
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

        var translationService = new TranslationService(
            _loggerFactory.CreateLogger<TranslationService>(),
            _llmProvider);

        return await translationService.BatchTranslateAsync(request, progress, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public LanguageValidationResult ValidateLanguageCode(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return new LanguageValidationResult(
                false,
                "Language code is required",
                ErrorCode: "INVALID_LANGUAGE_EMPTY");
        }

        // Check if it matches the ISO 639-1 pattern
        if (!LanguageCodePattern.IsMatch(languageCode))
        {
            return new LanguageValidationResult(
                false,
                $"Invalid language code format: '{languageCode}'. Expected ISO 639-1 format (e.g., 'en', 'es-MX')",
                ErrorCode: "INVALID_LANGUAGE_FORMAT");
        }

        // Extract base language code (without region)
        var baseCode = languageCode.Split('-')[0].ToLowerInvariant();

        // Check if it's a known language code
        if (!SupportedLanguageCodes.Contains(baseCode))
        {
            // Allow custom languages but warn that they may have limited support
            return new LanguageValidationResult(
                true,
                $"Language code '{languageCode}' is not in the standard list but will be processed by the LLM",
                IsWarning: true,
                ErrorCode: "LANGUAGE_NOT_IN_STANDARD_LIST");
        }

        return new LanguageValidationResult(true, "Valid language code");
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

        if (ex is HttpRequestException httpEx)
        {
            // Network-related errors are typically transient
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
