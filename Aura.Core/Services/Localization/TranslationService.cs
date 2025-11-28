using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    /// Validate that the current LLM provider supports translation
    /// </summary>
    private void ValidateProviderCapabilities()
    {
        var capabilities = _llmProvider.GetCapabilities();

        if (!capabilities.SupportsTranslation)
        {
            throw new InvalidOperationException(
                $"The current LLM provider ({capabilities.ProviderName}) does not support translation. " +
                $"Please configure an AI provider that supports translation capabilities.");
        }

        if (capabilities.IsLocalModel)
        {
            _logger.LogInformation(
                "Using local model provider: {Provider}. Known limitations: {Limitations}",
                capabilities.ProviderName,
                string.Join(", ", capabilities.KnownLimitations));
        }
    }

    /// <summary>
    /// Determines if the current LLM provider is a local model (Ollama, Local, or RuleBased).
    /// Local models may require stronger prompt constraints to produce clean translation output.
    /// </summary>
    private bool IsLocalModel()
    {
        var providerTypeName = _llmProvider.GetType().Name;
        return providerTypeName.Contains("Ollama", StringComparison.OrdinalIgnoreCase) ||
               providerTypeName.Contains("Local", StringComparison.OrdinalIgnoreCase) ||
               providerTypeName.Contains("RuleBased", StringComparison.OrdinalIgnoreCase);
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

        // Validate provider capabilities before attempting translation
        ValidateProviderCapabilities();

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

            // Phase 6: Calculate translation metrics for monitoring
            _logger.LogInformation("Phase 6: Calculating translation metrics");

            // Only calculate metrics if we have valid source and translated text
            if (!string.IsNullOrWhiteSpace(result.SourceText) && !string.IsNullOrWhiteSpace(result.TranslatedText))
            {
                result.Metrics = CalculateMetrics(
                    result.SourceText,
                    result.TranslatedText,
                    result.TranslationTimeSeconds);
            }
            else
            {
                _logger.LogWarning(
                    "Cannot calculate translation metrics - source or translated text is empty. " +
                    "Source length: {SourceLength}, Translated length: {TranslatedLength}",
                    result.SourceText?.Length ?? 0, result.TranslatedText?.Length ?? 0);

                // Create metrics with error indication
                result.Metrics = new TranslationMetrics
                {
                    CharacterCount = 0,
                    WordCount = 0,
                    LengthRatio = 0.0,
                    TranslationTimeSeconds = result.TranslationTimeSeconds,
                    ProviderUsed = "Unknown",
                    Grade = TranslationQualityGrade.Poor,
                    QualityIssues = new List<string> { "Translation failed or returned empty result" }
                };

                // Try to get provider name even if translation failed
                try
                {
                    var capabilities = _llmProvider.GetCapabilities();
                    result.Metrics.ProviderUsed = capabilities.ProviderName;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not retrieve provider capabilities for metrics");
                }
            }

            // Log quality issues if any detected
            if (result.Metrics.QualityIssues.Count > 0)
            {
                _logger.LogWarning(
                    "Translation quality issues detected ({Grade}): {Issues}",
                    result.Metrics.Grade,
                    string.Join("; ", result.Metrics.QualityIssues));
            }

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
            var providerType = _llmProvider.GetType();
            var providerTypeName = providerType.Name;
            var isComposite = providerTypeName == "CompositeLlmProvider";

            _logger.LogInformation(
                "Starting translation: {SourceLang} -> {TargetLang}, Mode: {Mode}, Transcreation: {HasTranscreation}, Provider: {Provider}",
                sourceLanguage, targetLanguage, options.Mode,
                !string.IsNullOrWhiteSpace(options.TranscreationContext), providerTypeName);

            // CRITICAL: Verify we're not using RuleBased or mock providers
            if (providerTypeName.Contains("RuleBased", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "CRITICAL: Translation is using RuleBased provider instead of real LLM (Ollama). " +
                    "This will produce poor quality translations. Check Ollama is running and configured. " +
                    "Please ensure Ollama is running: 'ollama serve' and check 'ollama list' to verify models are installed.");
                throw new InvalidOperationException(
                    "Translation requires a real LLM provider (Ollama). RuleBased provider cannot produce quality translations. " +
                    "Please ensure Ollama is running and configured. Start Ollama with: 'ollama serve'");
            }
            else if (providerTypeName.Contains("Mock", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "CRITICAL: Translation is using Mock provider. This should never happen in production. " +
                    "Check LLM provider configuration.");
                throw new InvalidOperationException("Translation cannot use Mock provider. Check LLM provider configuration.");
            }
            else if (isComposite)
            {
                _logger.LogInformation(
                    "Using CompositeLlmProvider - it will select the best available provider (Ollama if available)");
            }

            // Validate prompts before sending
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                _logger.LogError("System prompt is empty for translation {SourceLang} -> {TargetLang}",
                    sourceLanguage, targetLanguage);
                throw new InvalidOperationException("System prompt is empty - cannot perform translation");
            }

            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                _logger.LogError("User prompt (text to translate) is empty for translation {SourceLang} -> {TargetLang}",
                    sourceLanguage, targetLanguage);
                throw new InvalidOperationException("Text to translate is empty");
            }

            _logger.LogDebug(
                "Translation request: Source={SourceLang}, Target={TargetLang}, TextLength={Length}, SystemPromptLength={SysLength}, UserPromptLength={UserLength}",
                sourceLanguage, targetLanguage, text.Length, systemPrompt.Length, userPrompt.Length);

            var translationStartTime = DateTime.UtcNow;
            string? response = null;
            try
            {
                // Use CompositeLlmProvider for translation - it will automatically select the best available provider
                // and handle fallback logic. This is more reliable than direct Ollama calls.
                response = await _llmProvider.GenerateChatCompletionAsync(
                    systemPrompt,
                    userPrompt,
                    null, // Use default LLM parameters - do NOT use format="json" for translation
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Translation was cancelled by user");
                throw;
            }
            catch (InvalidOperationException ioe) when (ioe.Message.Contains("empty response") || ioe.Message.Contains("empty content"))
            {
                // Ollama returned empty response - provide detailed diagnostics
                _logger.LogError(ioe,
                    "Ollama returned empty response for translation {SourceLang} -> {TargetLang}. " +
                    "This usually means: (1) The model is not responding correctly, " +
                    "(2) The prompt was too restrictive, or (3) The model needs to be reloaded. " +
                    "Source text length: {SourceLength}, Provider: {Provider}",
                    sourceLanguage, targetLanguage, text.Length, providerTypeName);

                throw new InvalidOperationException(
                    $"Translation failed: Ollama returned an empty response. " +
                    $"This may indicate the model is not working correctly. " +
                    $"Try: (1) Test the model directly with 'ollama run <model>', " +
                    $"(2) Reload the model with 'ollama run <model>', " +
                    $"(3) Try a different model, or (4) Check Ollama logs for errors.", ioe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM call failed for translation {SourceLang} -> {TargetLang}: {Error}",
                    sourceLanguage, targetLanguage, ex.Message);
                throw;
            }

            var translationDuration = DateTime.UtcNow - translationStartTime;

            if (string.IsNullOrWhiteSpace(response))
            {
                _logger.LogError(
                    "LLM returned empty response for translation {SourceLang} -> {TargetLang}. " +
                    "Provider: {Provider}, Duration: {Duration}ms. " +
                    "If using Ollama, ensure it's running and the model is loaded.",
                    sourceLanguage, targetLanguage, providerTypeName, translationDuration.TotalMilliseconds);
                throw new InvalidOperationException(
                    $"LLM returned empty response. If using Ollama, ensure it's running and the model is available.");
            }

            _logger.LogInformation(
                "Translation LLM call completed: Provider={Provider}, Duration={Duration}ms, ResponseLength={Length} chars. " +
                "If Ollama is running, you should see CPU/GPU utilization in system monitor.",
                providerTypeName, translationDuration.TotalMilliseconds, response.Length);

            var translation = ExtractTranslation(response);

            if (string.IsNullOrWhiteSpace(translation))
            {
                _logger.LogError(
                    "Extracted translation is empty after processing LLM response. " +
                    "Response length: {Length}, Response preview: {Preview}",
                    response.Length, response.Substring(0, Math.Min(200, response.Length)));
                throw new InvalidOperationException(
                    "Translation extraction failed - LLM response could not be processed. Please try again.");
            }

            // Validate translation quality - check for structured metadata artifacts
            // Use the helper method to ensure we're checking for JSON property patterns
            if (ContainsStructuredArtifactKeys(translation))
            {
                _logger.LogError(
                    "Translation output contains structured metadata. This indicates prompt engineering failure. " +
                    "Source: {Source}, Target: {Target}, ResponseLength: {Length}",
                    sourceLanguage, targetLanguage, response.Length);

                // Attempt aggressive cleanup
                translation = StripStructuredArtifacts(translation);
            }

            // Ensure translation is not suspiciously long compared to source
            if (translation.Length > text.Length * 5)
            {
                _logger.LogWarning(
                    "Translation is {Ratio:F1}x longer than source text. This may indicate verbose LLM output. " +
                    "Consider adjusting prompt or model parameters.",
                    (double)translation.Length / text.Length);
            }

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

        // Add provider-specific reinforcement for local models
        if (IsLocalModel())
        {
            systemBuilder.AppendLine(@"

IMPORTANT FOR LOCAL MODELS:
You are a translation tool, not a tutorial generator. Your ONLY job is to translate text.
Do NOT generate structured content, tutorials, or explanations.
Return the translated text EXACTLY as it should appear in the target language - nothing more.");
        }

        systemBuilder.AppendLine(@"

CRITICAL OUTPUT REQUIREMENTS:
1. Return ONLY the translated text itself - nothing else
2. DO NOT wrap the output in JSON, XML, or any structured format
3. DO NOT include metadata fields like 'title', 'description', 'steps', etc.
4. DO NOT include explanations, notes, commentary, or reasoning
5. DO NOT include the word 'translation' or 'translated text' in your response
6. DO NOT add introductory phrases like 'Here is the translation:'
7. If the input is a simple sentence, output should be a simple translated sentence
8. Preserve the original formatting (line breaks, paragraphs) but nothing more

WRONG OUTPUT EXAMPLE:
{""translation"": ""Prueba nuestro nuevo sabor de pasta de dientes hoy"", ""title"": ""Tutorial...""}

CORRECT OUTPUT EXAMPLE:
Prueba nuestro nuevo sabor de pasta de dientes hoy -- Spearmint. Larga duración refrescante! - Atom Toothpaste Company

Your response must contain ONLY the translated text, exactly as shown in the correct example.");

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

    /// <summary>
    /// Extracts the actual translation from an LLM response, handling various malformed output formats.
    /// Supports extraction from JSON structures and removal of common unwanted prefixes.
    /// </summary>
    private string ExtractTranslation(string llmResponse)
    {
        if (string.IsNullOrWhiteSpace(llmResponse))
        {
            _logger.LogWarning("LLM returned empty translation response");
            return string.Empty;
        }

        var trimmedResponse = llmResponse.Trim();

        // Check if response looks like JSON (starts with { and ends with })
        // Use TryParse to reliably detect valid JSON before attempting extraction
        if (trimmedResponse.StartsWith("{") && trimmedResponse.EndsWith("}"))
        {
            try
            {
                // Attempt to parse as JSON
                using var doc = JsonDocument.Parse(trimmedResponse);
                var root = doc.RootElement;

                // Only proceed with extraction if this is a JSON object (not an array or primitive)
                if (root.ValueKind == JsonValueKind.Object)
                {
                    _logger.LogWarning("Detected structured JSON response instead of plain text translation. Attempting to extract translation field.");

                    // Try common field names in order of preference
                    if (root.TryGetProperty("translation", out var translationField) &&
                        translationField.ValueKind == JsonValueKind.String)
                    {
                        var extracted = translationField.GetString();
                        if (!string.IsNullOrWhiteSpace(extracted))
                            return extracted;
                    }
                    if (root.TryGetProperty("translatedText", out var translatedTextField) &&
                        translatedTextField.ValueKind == JsonValueKind.String)
                    {
                        var extracted = translatedTextField.GetString();
                        if (!string.IsNullOrWhiteSpace(extracted))
                            return extracted;
                    }
                    if (root.TryGetProperty("text", out var textField) &&
                        textField.ValueKind == JsonValueKind.String)
                    {
                        var extracted = textField.GetString();
                        if (!string.IsNullOrWhiteSpace(extracted))
                            return extracted;
                    }
                    if (root.TryGetProperty("content", out var contentField) &&
                        contentField.ValueKind == JsonValueKind.String)
                    {
                        var extracted = contentField.GetString();
                        if (!string.IsNullOrWhiteSpace(extracted))
                            return extracted;
                    }

                    _logger.LogWarning("Could not extract translation from JSON structure, returning raw response");
                }
            }
            catch (JsonException)
            {
                // Not valid JSON, continue with text processing
            }
        }

        // Remove common unwanted prefixes that models sometimes add
        var prefixesToRemove = new[]
        {
            "Translation:",
            "Translated text:",
            "Here is the translation:",
            "The translation is:",
            "Output:",
            "Result:"
        };

        foreach (var prefix in prefixesToRemove)
        {
            if (trimmedResponse.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                trimmedResponse = trimmedResponse.Substring(prefix.Length).TrimStart();
                break;
            }
        }

        // Normalize line breaks while preserving paragraph structure
        // Replace multiple consecutive newlines with a double newline (paragraph break)
        // This preserves intentional formatting while cleaning up excessive whitespace
        var normalizedText = Regex.Replace(trimmedResponse, @"\n\s*\n\s*\n+", "\n\n");

        // Trim whitespace from each line but preserve line structure
        var lines = normalizedText.Split('\n');
        var cleanedLines = lines.Select(l => l.Trim());

        return string.Join("\n", cleanedLines).Trim();
    }

    /// <summary>
    /// Aggressively strips structured artifacts (JSON-like structures) from translation output.
    /// Used as a fallback when the LLM returns metadata instead of plain translation text.
    /// </summary>
    private string StripStructuredArtifacts(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Try to parse embedded JSON objects and remove them
        // This approach is more robust than regex for handling nested structures
        var result = new StringBuilder();
        var i = 0;
        var braceDepth = 0;
        var inJsonObject = false;
        var jsonStart = -1;

        while (i < text.Length)
        {
            var c = text[i];

            if (c == '{' && !inJsonObject)
            {
                // Potential start of JSON object
                jsonStart = i;
                inJsonObject = true;
                braceDepth = 1;
            }
            else if (c == '{' && inJsonObject)
            {
                braceDepth++;
            }
            else if (c == '}' && inJsonObject)
            {
                braceDepth--;
                if (braceDepth == 0)
                {
                    // End of JSON object - check if it contains known artifact keys
                    var potentialJson = text.Substring(jsonStart, i - jsonStart + 1);
                    if (!ContainsStructuredArtifactKeys(potentialJson))
                    {
                        // Not a structured artifact, keep it
                        result.Append(potentialJson);
                    }
                    // Otherwise, we strip it by not appending
                    inJsonObject = false;
                    jsonStart = -1;
                }
            }
            else if (!inJsonObject)
            {
                result.Append(c);
            }

            i++;
        }

        // If we ended mid-JSON, append the remainder
        if (inJsonObject && jsonStart >= 0)
        {
            result.Append(text.Substring(jsonStart));
        }

        return result.ToString().Trim();
    }

    /// <summary>
    /// Checks if a JSON-like string contains known structured artifact keys.
    /// </summary>
    private static bool ContainsStructuredArtifactKeys(string text)
    {
        // Check for common artifact patterns as JSON property names (with quotes)
        return text.Contains("\"title\"", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("\"description\"", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("\"tutorial\"", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("\"steps\"", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("\"metadata\"", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Calculates translation quality metrics based on source and translated text.
    /// Detects common LLM output issues like structured artifacts, unwanted prefixes,
    /// and unusual length ratios.
    /// </summary>
    private TranslationMetrics CalculateMetrics(
        string sourceText,
        string translatedText,
        double translationTimeSeconds)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            _logger.LogWarning("CalculateMetrics called with empty sourceText");
            sourceText = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(translatedText))
        {
            _logger.LogWarning("CalculateMetrics called with empty translatedText");
            translatedText = string.Empty;
        }

        var sourceLength = sourceText.Length;
        var translatedLength = translatedText.Length;
        var sourceWordCount = sourceText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var translatedWordCount = translatedText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

        var metrics = new TranslationMetrics
        {
            CharacterCount = translatedLength,
            WordCount = translatedWordCount,
            LengthRatio = sourceLength > 0 ? (double)translatedLength / sourceLength : 0.0,
            TranslationTimeSeconds = translationTimeSeconds
        };

        _logger.LogDebug(
            "Translation metrics calculated: Source={SourceLen} chars ({SourceWords} words), " +
            "Translated={TranslatedLen} chars ({TranslatedWords} words), Ratio={Ratio:F2}x",
            sourceLength, sourceWordCount, translatedLength, translatedWordCount, metrics.LengthRatio);

        // Try to get provider name
        try
        {
            var capabilities = _llmProvider.GetCapabilities();
            metrics.ProviderUsed = capabilities.ProviderName;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not retrieve provider capabilities for metrics");
            metrics.ProviderUsed = "Unknown";
        }

        // Detect quality issues
        var issues = new List<string>();

        // Check for structured artifacts (JSON-like patterns in translated text)
        if (ContainsStructuredArtifactKeys(translatedText))
        {
            metrics.HasStructuredArtifacts = true;
            issues.Add("Response contained structured JSON artifacts");
        }

        // Check for common unwanted prefixes
        var unwantedPrefixes = new[] { "Translation:", "Translated text:", "Here is the translation:", "Output:", "Result:" };
        if (unwantedPrefixes.Any(prefix => translatedText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            metrics.HasUnwantedPrefixes = true;
            issues.Add("Response contained unwanted prefixes");
        }

        // Check for unusual length ratios
        if (metrics.LengthRatio > 3.0)
        {
            issues.Add($"Translation unusually long ({metrics.LengthRatio:F1}x source length)");
        }

        if (metrics.LengthRatio < 0.3 && sourceText.Length > 10)
        {
            issues.Add($"Translation unusually short ({metrics.LengthRatio:F1}x source length)");
        }

        metrics.QualityIssues = issues;

        // Calculate grade based on issues
        if (issues.Count == 0 && metrics.LengthRatio > 0.5 && metrics.LengthRatio < 2.5)
        {
            metrics.Grade = TranslationQualityGrade.Excellent;
        }
        else if (issues.Count <= 1)
        {
            metrics.Grade = TranslationQualityGrade.Good;
        }
        else if (issues.Count <= 2)
        {
            metrics.Grade = TranslationQualityGrade.Fair;
        }
        else
        {
            metrics.Grade = TranslationQualityGrade.Poor;
        }

        return metrics;
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

    /// <summary>
    /// Generate using Ollama API directly with /api/chat endpoint (similar to script generation and ideation approach)
    /// This bypasses CompositeLlmProvider fallback logic and ensures we use Ollama when available
    /// Uses /api/chat WITHOUT format=json for translation (plain text output)
    /// </summary>
    private async Task<string> GenerateWithOllamaDirectAsync(
        string systemPrompt,
        string userPrompt,
        string sourceLanguage,
        string targetLanguage,
        CancellationToken ct)
    {
        // Use reflection to access Ollama provider's internal HttpClient and configuration
        var providerType = _llmProvider.GetType();
        System.Net.Http.HttpClient? httpClient = null;
        string baseUrl = "http://127.0.0.1:11434";
        string defaultModel = "llama3.1:8b-q4_k_m";
        TimeSpan timeout = TimeSpan.FromSeconds(900);
        int maxRetries = 3;
        ILlmProvider? ollamaProvider = null;

        // Try to get Ollama provider - handle both direct OllamaLlmProvider and CompositeLlmProvider
        if (providerType.Name == "OllamaLlmProvider")
        {
            ollamaProvider = _llmProvider;
            var httpClientField = providerType.GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var baseUrlField = providerType.GetField("_baseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var modelField = providerType.GetField("_model", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var timeoutField = providerType.GetField("_timeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var maxRetriesField = providerType.GetField("_maxRetries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (httpClientField != null && baseUrlField != null && modelField != null)
            {
                httpClient = (System.Net.Http.HttpClient?)httpClientField.GetValue(_llmProvider);
                baseUrl = (string?)baseUrlField.GetValue(_llmProvider) ?? baseUrl;
                defaultModel = (string?)modelField.GetValue(_llmProvider) ?? defaultModel;
                timeout = timeoutField?.GetValue(_llmProvider) as TimeSpan? ?? timeout;
                maxRetries = (int)(maxRetriesField?.GetValue(_llmProvider) ?? maxRetries);
            }
        }
        else if (providerType.Name == "CompositeLlmProvider")
        {
            // Composite provider - try to get Ollama provider from its internal providers
            try
            {
                var getProvidersMethod = providerType.GetMethod("GetProviders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Collections.Generic.Dictionary<string, ILlmProvider>? providers = null;

                if (getProvidersMethod != null)
                {
                    var providersResult = getProvidersMethod.Invoke(_llmProvider, new object[] { false });
                    providers = providersResult as System.Collections.Generic.Dictionary<string, ILlmProvider>;
                }

                if (providers == null)
                {
                    var providersField = providerType.GetField("_cachedProviders", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (providersField != null)
                    {
                        providers = providersField.GetValue(_llmProvider) as System.Collections.Generic.Dictionary<string, ILlmProvider>;
                    }
                }

                if (providers != null && providers.TryGetValue("Ollama", out ollamaProvider) && ollamaProvider != null)
                {
                    var ollamaProviderType = ollamaProvider.GetType();
                    var httpClientField = ollamaProviderType.GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var baseUrlField = ollamaProviderType.GetField("_baseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var modelField = ollamaProviderType.GetField("_model", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var timeoutField = ollamaProviderType.GetField("_timeout", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var maxRetriesField = ollamaProviderType.GetField("_maxRetries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (httpClientField != null && baseUrlField != null && modelField != null)
                    {
                        httpClient = (System.Net.Http.HttpClient?)httpClientField.GetValue(ollamaProvider);
                        baseUrl = (string?)baseUrlField.GetValue(ollamaProvider) ?? baseUrl;
                        defaultModel = (string?)modelField.GetValue(ollamaProvider) ?? defaultModel;
                        timeout = timeoutField?.GetValue(ollamaProvider) as TimeSpan? ?? timeout;
                        maxRetries = (int)(maxRetriesField?.GetValue(ollamaProvider) ?? maxRetries);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract Ollama provider from CompositeLlmProvider via reflection");
            }
        }

        // Check availability first (like script generation does)
        if (ollamaProvider != null)
        {
            try
            {
                var availabilityMethod = ollamaProvider.GetType().GetMethod("IsServiceAvailableAsync", 
                    new[] { typeof(CancellationToken), typeof(bool) });
                if (availabilityMethod != null)
                {
                    using var availabilityCts = new System.Threading.CancellationTokenSource();
                    availabilityCts.CancelAfter(TimeSpan.FromSeconds(5));
                    var availabilityTask = (Task<bool>)availabilityMethod.Invoke(ollamaProvider, 
                        new object[] { availabilityCts.Token, false })!;
                    var isAvailable = await availabilityTask.ConfigureAwait(false);
                    
                    if (!isAvailable)
                    {
                        _logger.LogError("Ollama is not available for translation. Please ensure Ollama is running: 'ollama serve'");
                        throw new InvalidOperationException(
                            "Ollama is required for translation but is not available. " +
                            "Please ensure Ollama is running: 'ollama serve' and verify models are installed: 'ollama list'");
                    }
                    _logger.LogInformation("Ollama availability check passed for translation");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not check Ollama availability, proceeding with request attempt");
            }
        }

        // Track if we created the HttpClient so we can dispose it properly
        bool createdHttpClient = false;
        if (httpClient == null)
        {
            _logger.LogInformation("Creating new HttpClient for direct Ollama API call (baseUrl: {BaseUrl})", baseUrl);
            httpClient = new System.Net.Http.HttpClient
            {
                Timeout = timeout.Add(TimeSpan.FromMinutes(5))
            };
            createdHttpClient = true;
        }

        _logger.LogInformation(
            "Calling Ollama API directly for translation {SourceLang} -> {TargetLang} (Model: {Model})",
            sourceLanguage, targetLanguage, defaultModel);

        // Build combined prompt (like script generation does) - combine system and user prompts
        // Script generation uses a single prompt, not messages array
        var combinedPrompt = string.IsNullOrWhiteSpace(systemPrompt)
            ? userPrompt
            : $"{systemPrompt}\n\n{userPrompt}";

        // Build Ollama API request (using /api/generate endpoint like script generation)
        var options = new Dictionary<string, object>
        {
            { "temperature", 0.7 },
            { "top_p", 0.9 },
            { "num_predict", 2000 }
        };

        var requestBody = new
        {
            model = defaultModel,
            prompt = combinedPrompt,
            stream = false,
            options = options
        };
        // Note: NOT adding format="json" for translation - we want plain text output

        var json = System.Text.Json.JsonSerializer.Serialize(requestBodyDict);
        var content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");

        Exception? lastException = null;
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    var backoffDelay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogInformation("Retrying Ollama translation (attempt {Attempt}/{MaxRetries}) after {Delay}s",
                        attempt + 1, maxRetries + 1, backoffDelay.TotalSeconds);
                    await Task.Delay(backoffDelay, ct).ConfigureAwait(false);
                }

                using var cts = new System.Threading.CancellationTokenSource();
                cts.CancelAfter(timeout);

                if (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException("Translation was cancelled by user", ct);
                }

                _logger.LogInformation("Sending translation request to Ollama (attempt {Attempt}/{MaxRetries}, timeout: {Timeout:F1} minutes)",
                    attempt + 1, maxRetries + 1, timeout.TotalMinutes);

                // Use /api/generate endpoint (like script generation) - this is the correct endpoint for Ollama
                var response = await httpClient.PostAsync($"{baseUrl}/api/generate", content, cts.Token).ConfigureAwait(false);
                
                // Check for model not found error
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);
                    if (errorContent.Contains("model") && errorContent.Contains("not found"))
                    {
                        throw new InvalidOperationException(
                            $"Model '{defaultModel}' not found. Please pull the model first using: ollama pull {defaultModel}");
                    }
                    response.EnsureSuccessStatusCode();
                }

                // CRITICAL: Use cts.Token instead of ct for ReadAsStringAsync (like script generation)
                // This prevents upstream components from cancelling our long-running operation
                var responseJson = await response.Content.ReadAsStringAsync(cts.Token).ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(responseJson))
                {
                    _logger.LogError("Ollama returned empty JSON response for translation");
                    throw new InvalidOperationException("Ollama returned an empty JSON response");
                }

                // Parse and validate response structure (like script generation)
                System.Text.Json.JsonDocument? responseDoc = null;
                try
                {
                    responseDoc = System.Text.Json.JsonDocument.Parse(responseJson);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse Ollama JSON response: {Response}", 
                        responseJson.Substring(0, Math.Min(500, responseJson.Length)));
                    throw new InvalidOperationException("Ollama returned invalid JSON response", ex);
                }

                // Check for errors in response (like script generation)
                if (responseDoc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    var errorMessage = errorElement.GetString() ?? "Unknown error";
                    _logger.LogError("Ollama API error: {Error}", errorMessage);
                    throw new InvalidOperationException($"Ollama API error: {errorMessage}");
                }

                // /api/generate returns response in 'response' field (like script generation)
                if (responseDoc.RootElement.TryGetProperty("response", out var responseText))
                {
                    var result = responseText.GetString() ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(result))
                    {
                        _logger.LogError("Ollama returned an empty response. Response JSON: {Response}",
                            responseJson.Substring(0, Math.Min(1000, responseJson.Length)));
                        throw new InvalidOperationException(
                            "Ollama returned an empty response. " +
                            "This may indicate: (1) The model is not responding correctly, " +
                            "(2) The prompt was too restrictive, or (3) The model needs to be reloaded. " +
                            "Try: 'ollama run <model>' to test the model directly.");
                    }

                    _logger.LogInformation("Ollama translation succeeded with {Length} characters", result.Length);
                    return result;
                }

                var availableFields = string.Join(", ", responseDoc.RootElement.EnumerateObject().Select(p => p.Name));
                _logger.LogError(
                    "Ollama response did not contain expected 'response' field. Available fields: {Fields}. Response: {Response}",
                    availableFields,
                    responseJson.Substring(0, Math.Min(500, responseJson.Length)));
                throw new InvalidOperationException($"Invalid response structure from Ollama. Expected 'response' field but got: {responseJson.Substring(0, Math.Min(200, responseJson.Length))}");
            }
            catch (TaskCanceledException ex)
            {
                lastException = ex;
                if (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Ollama translation timed out (attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
                else
                {
                    _logger.LogError(ex, "Ollama translation timed out on final attempt ({Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                lastException = ex;
                if (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Ollama translation connection failed (attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
                else
                {
                    _logger.LogError(ex, "Ollama translation connection failed on final attempt ({Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
            }
            catch (InvalidOperationException)
            {
                // Re-throw availability/configuration errors immediately (don't retry these)
                // These are configuration/availability issues that won't be fixed by retrying
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                if (attempt < maxRetries)
                {
                    _logger.LogWarning(ex, "Ollama translation failed (attempt {Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
                else
                {
                    _logger.LogError(ex, "Ollama translation failed on final attempt ({Attempt}/{MaxRetries})", attempt + 1, maxRetries + 1);
                }
            }
        }

        throw lastException ?? new InvalidOperationException(
            $"Ollama translation failed after {maxRetries + 1} attempts");
    }
}
