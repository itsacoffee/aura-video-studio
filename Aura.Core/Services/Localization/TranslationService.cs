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
                cancellationToken).ConfigureAwait(false);
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
                    cancellationToken).ConfigureAwait(false);
            }

            // Phase 3: Timing adjustments
            if (request.Options.AdjustTimings && request.ScriptLines.Count != 0)
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
                    cancellationToken).ConfigureAwait(false);
            }

            // Phase 5: Visual localization recommendations
            _logger.LogInformation("Phase 5: Visual localization analysis");
            result.VisualRecommendations = await _visualAnalyzer.AnalyzeVisualLocalizationNeedsAsync(
                result.TranslatedLines,
                targetLanguage,
                request.CulturalContext,
                cancellationToken).ConfigureAwait(false);

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

                var translation = await TranslateAsync(translationRequest, cancellationToken).ConfigureAwait(false);
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
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<List<TranslatedScriptLine>> TranslateScriptLinesAsync(
        TranslationRequest request,
        LanguageInfo targetLanguage,
        CancellationToken cancellationToken)
    {
        var translatedLines = new List<TranslatedScriptLine>();

        if (request.ScriptLines.Count != 0)
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
                    cancellationToken).ConfigureAwait(false);

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
                cancellationToken).ConfigureAwait(false);

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

        try
        {
            // Use CompleteAsync for direct prompt completion - this is the correct approach
            // for translation tasks that require sending a specific prompt to the LLM
            var response = await _llmProvider.CompleteAsync(prompt, cancellationToken).ConfigureAwait(false);
            var translation = ExtractTranslation(response);
            
            _logger.LogDebug(
                "Translation completed: {SourceLang} -> {TargetLang}, Input: {InputLength} chars, Output: {OutputLength} chars",
                sourceLanguage, targetLanguage, text.Length, translation.Length);
            
            return translation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed for {SourceLang} -> {TargetLang}: {Error}", 
                sourceLanguage, targetLanguage, ex.Message);
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
        
        // Enhanced system context for high-quality translation
        sb.AppendLine($@"You are an expert professional translator specializing in {sourceLanguage} to {targetLanguage} translation.

TRANSLATION TASK:
Translate the following text from {sourceLanguage} to {targetLanguage}.");
        sb.AppendLine();

        // Mode-specific instructions with detailed guidance
        if (options.Mode == TranslationMode.Localized)
        {
            sb.AppendLine(@"LOCALIZATION MODE - Apply these principles:
1. CULTURAL ADAPTATION: Adapt idioms, expressions, and cultural references to resonate with the target audience
2. NATURAL FLOW: Ensure the translation reads naturally to native speakers, not as a translation
3. CULTURAL EQUIVALENCE: Replace culturally-specific references with local equivalents when appropriate
4. HUMOR AND TONE: Adjust humor, sarcasm, and tone for cultural appropriateness
5. EXAMPLES: Modify examples to be culturally relevant and relatable
6. FORMALITY: Match the formality level expected in the target culture");
        }
        else if (options.Mode == TranslationMode.Transcreation)
        {
            sb.AppendLine(@"TRANSCREATION MODE - Apply creative adaptation:
1. MESSAGE PRESERVATION: Preserve the core message and emotional impact above literal accuracy
2. CREATIVE FREEDOM: Adapt freely to maximize resonance with target culture
3. BRAND VOICE: Maintain consistent brand voice and personality
4. EMOTIONAL IMPACT: Ensure the translation evokes the same emotional response
5. MARKETING EFFECTIVENESS: Optimize for persuasive impact in the target market
6. CULTURAL APPEAL: Make the content feel native, not translated");
        }
        else // Literal mode
        {
            sb.AppendLine(@"LITERAL TRANSLATION MODE - Apply these principles:
1. ACCURACY: Preserve exact meaning with high fidelity
2. TERMINOLOGY: Maintain consistent terminology throughout
3. STRUCTURE: Preserve sentence structure where grammatically appropriate
4. COMPLETENESS: Include all information from the source text");
        }

        // Glossary terms with emphasis
        if (glossary.Count != 0)
        {
            sb.AppendLine();
            sb.AppendLine("MANDATORY TERMINOLOGY (use these exact translations):");
            foreach (var entry in glossary)
            {
                sb.AppendLine($"  • \"{entry.Key}\" → \"{entry.Value}\"");
            }
            sb.AppendLine();
            sb.AppendLine("IMPORTANT: These glossary terms MUST be used exactly as specified for consistency.");
        }

        // Additional constraints
        var constraints = new List<string>();
        
        if (options.AdaptMeasurements)
        {
            constraints.Add("Convert measurements to local units (imperial ↔ metric as appropriate for target region)");
        }

        if (options.PreserveNames)
        {
            constraints.Add("Preserve proper names in their original form unless standard localization exists (e.g., country names)");
        }

        if (options.PreserveBrands)
        {
            constraints.Add("Keep brand names, trademarks, and product names unchanged");
        }

        if (constraints.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("ADDITIONAL CONSTRAINTS:");
            foreach (var constraint in constraints)
            {
                sb.AppendLine($"  • {constraint}");
            }
        }

        // Context information
        if (!string.IsNullOrWhiteSpace(context))
        {
            sb.AppendLine();
            sb.AppendLine($"CONTEXT FOR TRANSLATION:");
            sb.AppendLine(context);
        }

        // Source text with clear demarcation
        sb.AppendLine();
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine("SOURCE TEXT TO TRANSLATE:");
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine(text);
        sb.AppendLine("═══════════════════════════════════════════════════════════════");
        sb.AppendLine();
        sb.AppendLine("OUTPUT INSTRUCTIONS:");
        sb.AppendLine("Provide ONLY the translated text. Do not include explanations, notes, or commentary.");
        sb.AppendLine();
        sb.AppendLine($"TRANSLATION ({targetLanguage}):");

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

        if (request.ScriptLines.Count != 0 && lineIndex > 0)
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
