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

        // Try to get language from registry, but if not found, create a default LanguageInfo
        // This allows LLM to handle any language intelligently, not just hardcoded ones
        var targetLanguage = LanguageRegistry.GetLanguage(request.TargetLanguage);
        if (targetLanguage == null)
        {
            _logger.LogInformation("Language {Language} not in registry, using LLM intelligence for custom language", 
                request.TargetLanguage);
            // Create a default LanguageInfo for custom languages - LLM will handle it intelligently
            targetLanguage = new LanguageInfo
            {
                Code = request.TargetLanguage,
                Name = request.TargetLanguage, // LLM will interpret this
                NativeName = request.TargetLanguage,
                Region = "Global",
                TypicalExpansionFactor = 1.15, // Safe default
                DefaultFormality = FormalityLevel.Neutral
            };
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

        // Allow custom languages - LLM will handle them intelligently
        var targetLanguage = LanguageRegistry.GetLanguage(request.TargetLanguage);
        if (targetLanguage == null)
        {
            _logger.LogInformation("Language {Language} not in registry, using LLM intelligence for cultural analysis", 
                request.TargetLanguage);
            targetLanguage = new LanguageInfo
            {
                Code = request.TargetLanguage,
                Name = request.TargetLanguage,
                NativeName = request.TargetLanguage,
                Region = request.TargetRegion ?? "Global",
                TypicalExpansionFactor = 1.15,
                DefaultFormality = FormalityLevel.Neutral
            };
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
        // Build system and user prompts for chat completion (more consistent with ideation pattern)
        var (systemPrompt, userPrompt) = BuildTranslationChatPrompts(
            text, 
            sourceLanguage, 
            targetLanguage, 
            context, 
            options, 
            glossary);

        try
        {
            // Use GenerateChatCompletionAsync for translation - this is consistent with how ideation works
            // and ensures proper fallback behavior through CompositeLlmProvider
            _logger.LogInformation(
                "Starting translation: {SourceLang} -> {TargetLang}, Mode: {Mode}, Transcreation: {HasTranscreation}",
                sourceLanguage, targetLanguage, options.Mode, 
                !string.IsNullOrWhiteSpace(options.TranscreationContext));
            
            var response = await _llmProvider.GenerateChatCompletionAsync(
                systemPrompt,
                userPrompt,
                null, // Use default LLM parameters
                cancellationToken).ConfigureAwait(false);
            
            var translation = ExtractTranslation(response);
            
            _logger.LogDebug(
                "Translation completed: {SourceLang} -> {TargetLang}, Input: {InputLength} chars, Output: {OutputLength} chars",
                sourceLanguage, targetLanguage, text.Length, translation.Length);
            
            return translation;
        }
        catch (NotSupportedException)
        {
            // RuleBased provider doesn't support translation - provide helpful message
            _logger.LogWarning(
                "Translation not available - no AI provider (Ollama, OpenAI, etc.) is running. " +
                "Please start Ollama or configure another AI provider.");
            return $"[Translation requires an AI provider. Please ensure Ollama is running.]";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Translation failed for {SourceLang} -> {TargetLang}: {Error}", 
                sourceLanguage, targetLanguage, ex.Message);
            return $"[Translation unavailable: {ex.Message}]";
        }
    }

    /// <summary>
    /// Build system and user prompts for translation chat completion.
    /// This separates the translator instructions (system) from the content to translate (user).
    /// </summary>
    private (string SystemPrompt, string UserPrompt) BuildTranslationChatPrompts(
        string text,
        string sourceLanguage,
        string targetLanguage,
        string context,
        TranslationOptions options,
        Dictionary<string, string> glossary)
    {
        var systemBuilder = new StringBuilder();
        var userBuilder = new StringBuilder();
        
        // System prompt: Translator persona and instructions
        systemBuilder.AppendLine($@"You are an expert professional translator with deep knowledge of languages and dialects worldwide, including constructed languages, fictional languages, historical variants, and regional dialects.

CRITICAL LANGUAGE HANDLING:
- Source language: '{sourceLanguage}'
- Target language: '{targetLanguage}'

These can be ANY of the following - interpret them intelligently:
- Standard language codes (e.g., 'en', 'es', 'fr')
- Full language names (e.g., 'English', 'Spanish', 'French')
- Language names in their native form (e.g., 'Español', 'Français')
- Regional variants with full descriptions (e.g., 'English (US)', 'English (UK)', 'Spanish (Mexico)')
- Dialects and less common languages (e.g., 'Swiss German', 'Cockney English')
- Historical variants (e.g., 'Medieval English', 'Old French')
- Constructed/fictional languages (e.g., 'Klingon', 'Elvish', 'Esperanto')
- Descriptive language variants (e.g., 'Formal Spanish', 'Slang English', 'Technical German')

Intelligently interpret the source and target languages. For fictional or constructed languages, translate to the best of your ability.");

        // Mode-specific instructions
        if (options.Mode == TranslationMode.Localized)
        {
            systemBuilder.AppendLine(@"

LOCALIZATION MODE:
1. CULTURAL ADAPTATION: Adapt idioms, expressions, and cultural references
2. NATURAL FLOW: Ensure the translation reads naturally to native speakers
3. CULTURAL EQUIVALENCE: Replace culturally-specific references with local equivalents
4. HUMOR AND TONE: Adjust humor, sarcasm, and tone for cultural appropriateness
5. FORMALITY: Match the formality level expected in the target culture");
        }
        else if (options.Mode == TranslationMode.Transcreation)
        {
            systemBuilder.AppendLine(@"

TRANSCREATION MODE:
1. MESSAGE PRESERVATION: Preserve the core message and emotional impact above literal accuracy
2. CREATIVE FREEDOM: Adapt freely to maximize resonance with target culture or specified style
3. EMOTIONAL IMPACT: Ensure the translation evokes the same emotional response
4. CULTURAL APPEAL: Make the content feel native, not translated");
            
            if (!string.IsNullOrWhiteSpace(options.TranscreationContext))
            {
                systemBuilder.AppendLine($@"

TRANSCREATION CONTEXT - Apply these specific instructions:
{options.TranscreationContext}

Transform the content to match the specified style, format, or era. Even if source and target languages are the same, apply the style transformation described above.");
            }
        }
        else // Literal mode
        {
            systemBuilder.AppendLine(@"

LITERAL TRANSLATION MODE:
1. ACCURACY: Preserve exact meaning with high fidelity
2. TERMINOLOGY: Maintain consistent terminology throughout
3. STRUCTURE: Preserve sentence structure where grammatically appropriate
4. COMPLETENESS: Include all information from the source text");
        }

        // Glossary terms
        if (glossary.Count != 0)
        {
            systemBuilder.AppendLine(@"

MANDATORY TERMINOLOGY (use these exact translations):");
            foreach (var entry in glossary)
            {
                systemBuilder.AppendLine($"  • \"{entry.Key}\" → \"{entry.Value}\"");
            }
        }

        // Additional constraints
        var constraints = new List<string>();
        if (options.AdaptMeasurements)
            constraints.Add("Convert measurements to local units (imperial ↔ metric as appropriate)");
        if (options.PreserveNames)
            constraints.Add("Preserve proper names in their original form unless standard localization exists");
        if (options.PreserveBrands)
            constraints.Add("Keep brand names, trademarks, and product names unchanged");

        if (constraints.Count > 0)
        {
            systemBuilder.AppendLine(@"

ADDITIONAL CONSTRAINTS:");
            foreach (var constraint in constraints)
            {
                systemBuilder.AppendLine($"  • {constraint}");
            }
        }

        systemBuilder.AppendLine(@"

OUTPUT INSTRUCTIONS:
Provide ONLY the translated/transformed text. Do not include explanations, notes, or commentary.");

        // User prompt: The actual content to translate
        userBuilder.AppendLine($"Translate the following text from {sourceLanguage} to {targetLanguage}:");
        userBuilder.AppendLine();
        
        if (!string.IsNullOrWhiteSpace(context))
        {
            userBuilder.AppendLine($"Context: {context}");
            userBuilder.AppendLine();
        }
        
        userBuilder.AppendLine("TEXT TO TRANSLATE:");
        userBuilder.AppendLine(text);

        return (systemBuilder.ToString(), userBuilder.ToString());
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
