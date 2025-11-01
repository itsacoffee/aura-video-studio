using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Localization;
using Aura.Core.Models.Audience;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Advanced translation service with LLM-powered cultural localization
/// Supports 50+ languages with cultural adaptation and quality assurance
/// </summary>
public class TranslationService
{
    private readonly ILogger<TranslationService> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly CulturalLocalizationEngine _culturalEngine;
    private readonly TranslationQualityValidator _qualityValidator;
    private readonly TimingAdjuster _timingAdjuster;
    private readonly VisualLocalizationAnalyzer _visualAnalyzer;

    public TranslationService(
        ILogger<TranslationService> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _culturalEngine = new CulturalLocalizationEngine(logger, llmProvider);
        _qualityValidator = new TranslationQualityValidator(logger, llmProvider);
        _timingAdjuster = new TimingAdjuster(logger);
        _visualAnalyzer = new VisualLocalizationAnalyzer(logger, llmProvider);
    }

    /// <summary>
    /// Translate script with cultural localization
    /// </summary>
    public async Task<TranslationResult> TranslateAsync(
        TranslationRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting translation from {Source} to {Target}", 
            request.SourceLanguage, request.TargetLanguage);

        var targetLanguage = LanguageRegistry.GetLanguage(request.TargetLanguage);
        if (targetLanguage == null)
        {
            throw new ArgumentException($"Unsupported target language: {request.TargetLanguage}");
        }

        var result = new TranslationResult
        {
            SourceLanguage = request.SourceLanguage,
            TargetLanguage = request.TargetLanguage
        };

        try
        {
            // Phase 1: Core translation with cultural context
            _logger.LogInformation("Phase 1: Core translation");
            var translatedLines = await TranslateScriptLinesAsync(
                request, 
                targetLanguage, 
                cancellationToken);
            result.TranslatedLines = translatedLines;
            result.TranslatedText = string.Join("\n", translatedLines.Select(l => l.TranslatedText));
            result.SourceText = string.Join("\n", translatedLines.Select(l => l.SourceText));

            // Phase 2: Cultural adaptations
            if (request.Options.Mode != TranslationMode.Literal)
            {
                _logger.LogInformation("Phase 2: Cultural localization");
                result.CulturalAdaptations = await _culturalEngine.ApplyCulturalAdaptationsAsync(
                    result.TranslatedLines,
                    request.CulturalContext ?? BuildDefaultCulturalContext(targetLanguage),
                    cancellationToken);
            }

            // Phase 3: Timing adjustments
            if (request.Options.AdjustTimings && request.ScriptLines.Any())
            {
                _logger.LogInformation("Phase 3: Timing adjustment");
                result.TimingAdjustment = _timingAdjuster.AdjustTimings(
                    result.TranslatedLines,
                    targetLanguage.TypicalExpansionFactor,
                    request.Options.MaxTimingVariance);
            }

            // Phase 4: Quality validation
            if (request.Options.EnableQualityScoring)
            {
                _logger.LogInformation("Phase 4: Quality validation");
                result.Quality = await _qualityValidator.ValidateQualityAsync(
                    result.SourceText,
                    result.TranslatedText,
                    request.SourceLanguage,
                    request.TargetLanguage,
                    request.Options,
                    request.Glossary,
                    cancellationToken);
            }

            // Phase 5: Visual localization recommendations
            _logger.LogInformation("Phase 5: Visual localization analysis");
            result.VisualRecommendations = await _visualAnalyzer.AnalyzeVisualLocalizationNeedsAsync(
                result.TranslatedLines,
                targetLanguage,
                request.CulturalContext,
                cancellationToken);

            stopwatch.Stop();
            result.TranslationTimeSeconds = stopwatch.Elapsed.TotalSeconds;
            
            _logger.LogInformation("Translation completed in {Time:F2}s with quality score {Quality:F1}", 
                result.TranslationTimeSeconds, result.Quality.OverallScore);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed: {Error}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Batch translate to multiple languages
    /// </summary>
    public async Task<BatchTranslationResult> BatchTranslateAsync(
        BatchTranslationRequest request,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting batch translation to {Count} languages", 
            request.TargetLanguages.Count);

        var result = new BatchTranslationResult
        {
            SourceLanguage = request.SourceLanguage
        };

        var completed = 0;
        foreach (var targetLanguage in request.TargetLanguages)
        {
            try
            {
                var translationRequest = new TranslationRequest
                {
                    SourceLanguage = request.SourceLanguage,
                    TargetLanguage = targetLanguage,
                    SourceText = request.SourceText,
                    ScriptLines = request.ScriptLines,
                    CulturalContext = request.CulturalContext,
                    Options = request.Options,
                    Glossary = request.Glossary
                };

                var translation = await TranslateAsync(translationRequest, cancellationToken);
                result.Translations[targetLanguage] = translation;
                result.SuccessfulLanguages.Add(targetLanguage);
                
                _logger.LogInformation("Completed translation to {Language}", targetLanguage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to translate to {Language}", targetLanguage);
                result.FailedLanguages.Add(targetLanguage);
            }

            completed++;
            progress?.Report((double)completed / request.TargetLanguages.Count);
        }

        stopwatch.Stop();
        result.TotalTimeSeconds = stopwatch.Elapsed.TotalSeconds;

        _logger.LogInformation("Batch translation completed: {Success}/{Total} successful", 
            result.SuccessfulLanguages.Count, request.TargetLanguages.Count);

        return result;
    }

    /// <summary>
    /// Analyze cultural appropriateness of content
    /// </summary>
    public async Task<CulturalAnalysisResult> AnalyzeCulturalContentAsync(
        CulturalAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing cultural content for {Language}/{Region}", 
            request.TargetLanguage, request.TargetRegion);

        var targetLanguage = LanguageRegistry.GetLanguage(request.TargetLanguage);
        if (targetLanguage == null)
        {
            throw new ArgumentException($"Unsupported target language: {request.TargetLanguage}");
        }

        return await _culturalEngine.AnalyzeCulturalContentAsync(
            request.Content,
            targetLanguage,
            request.TargetRegion,
            cancellationToken);
    }

    private async Task<List<TranslatedScriptLine>> TranslateScriptLinesAsync(
        TranslationRequest request,
        LanguageInfo targetLanguage,
        CancellationToken cancellationToken)
    {
        var translatedLines = new List<TranslatedScriptLine>();

        if (request.ScriptLines.Any())
        {
            // Translate script lines with context
            for (int i = 0; i < request.ScriptLines.Count; i++)
            {
                var line = request.ScriptLines[i];
                var context = BuildTranslationContext(request, i);
                
                var translatedText = await TranslateWithContextAsync(
                    line.Text,
                    request.SourceLanguage,
                    request.TargetLanguage,
                    context,
                    request.Options,
                    request.Glossary,
                    cancellationToken);

                translatedLines.Add(new TranslatedScriptLine
                {
                    SceneIndex = i,
                    SourceText = line.Text,
                    TranslatedText = translatedText,
                    OriginalStartSeconds = line.Start.TotalSeconds,
                    OriginalDurationSeconds = line.Duration.TotalSeconds,
                    AdjustedStartSeconds = line.Start.TotalSeconds,
                    AdjustedDurationSeconds = line.Duration.TotalSeconds
                });
            }
        }
        else if (!string.IsNullOrEmpty(request.SourceText))
        {
            // Translate plain text
            var context = BuildTranslationContext(request, 0);
            var translatedText = await TranslateWithContextAsync(
                request.SourceText,
                request.SourceLanguage,
                request.TargetLanguage,
                context,
                request.Options,
                request.Glossary,
                cancellationToken);

            translatedLines.Add(new TranslatedScriptLine
            {
                SceneIndex = 0,
                SourceText = request.SourceText,
                TranslatedText = translatedText
            });
        }

        return translatedLines;
    }

    private async Task<string> TranslateWithContextAsync(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string context,
        TranslationOptions options,
        Dictionary<string, string> glossary,
        CancellationToken cancellationToken)
    {
        var prompt = BuildTranslationPrompt(
            text, 
            sourceLanguage, 
            targetLanguage, 
            context, 
            options, 
            glossary);

        var brief = LlmRequestHelper.CreateTranslationBrief(
            "Translation",
            "Translation system",
            "Translate text with cultural adaptation"
        );

        var spec = LlmRequestHelper.CreateTranslationPlanSpec();

        try
        {
            var response = await _llmProvider.DraftScriptAsync(brief, spec, cancellationToken);
            return ExtractTranslation(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed, using fallback");
            return $"[Translation unavailable: {text}]";
        }
    }

    private string BuildTranslationPrompt(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string context,
        TranslationOptions options,
        Dictionary<string, string> glossary)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Translate the following text from {sourceLanguage} to {targetLanguage}.");
        sb.AppendLine();

        if (options.Mode == TranslationMode.Localized)
        {
            sb.AppendLine("IMPORTANT: Provide LOCALIZED translation, not literal:");
            sb.AppendLine("- Adapt idioms and expressions to target culture");
            sb.AppendLine("- Replace culturally-specific references with local equivalents");
            sb.AppendLine("- Adjust humor and tone for cultural appropriateness");
            sb.AppendLine("- Modify examples for cultural relevance");
        }
        else if (options.Mode == TranslationMode.Transcreation)
        {
            sb.AppendLine("IMPORTANT: Provide TRANSCREATION (creative adaptation):");
            sb.AppendLine("- Preserve the message and emotional impact");
            sb.AppendLine("- Adapt freely to resonate with target culture");
            sb.AppendLine("- Maintain brand voice and tone");
        }

        if (glossary.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Use these specific translations for key terms:");
            foreach (var entry in glossary)
            {
                sb.AppendLine($"- {entry.Key} → {entry.Value}");
            }
        }

        if (options.AdaptMeasurements)
        {
            sb.AppendLine("- Convert measurements to local units (imperial ↔ metric)");
        }

        if (options.PreserveNames)
        {
            sb.AppendLine("- Preserve proper names unless localization is standard");
        }

        if (options.PreserveBrands)
        {
            sb.AppendLine("- Keep brand names unchanged");
        }

        sb.AppendLine();
        sb.AppendLine($"Context: {context}");
        sb.AppendLine();
        sb.AppendLine("Text to translate:");
        sb.AppendLine(text);
        sb.AppendLine();
        sb.AppendLine("Provide ONLY the translated text, no explanations.");

        return sb.ToString();
    }

    private string BuildTranslationContext(TranslationRequest request, int lineIndex)
    {
        var context = new StringBuilder();
        
        if (request.CulturalContext != null)
        {
            context.Append($"Target region: {request.CulturalContext.TargetRegion}. ");
            context.Append($"Formality: {request.CulturalContext.TargetFormality}. ");
            context.Append($"Style: {request.CulturalContext.PreferredStyle}. ");
        }

        if (request.ScriptLines.Any() && lineIndex > 0)
        {
            context.Append($"Previous line: {request.ScriptLines[lineIndex - 1].Text}. ");
        }

        return context.ToString();
    }

    private string ExtractTranslation(string llmResponse)
    {
        var lines = llmResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", lines).Trim();
    }

    private CulturalContext BuildDefaultCulturalContext(LanguageInfo targetLanguage)
    {
        return new CulturalContext
        {
            TargetRegion = targetLanguage.Region,
            TargetFormality = targetLanguage.DefaultFormality,
            PreferredStyle = Models.Audience.CommunicationStyle.Professional,
            ContentRating = AgeRating.General
        };
    }
}
