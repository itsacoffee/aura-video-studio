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
            result.Metrics = CalculateMetrics(
                result.SourceText,
                result.TranslatedText,
                result.TranslationTimeSeconds);

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
        var metrics = new TranslationMetrics
        {
            CharacterCount = translatedText.Length,
            WordCount = translatedText.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length,
            LengthRatio = (double)translatedText.Length / Math.Max(1, sourceText.Length),
            TranslationTimeSeconds = translationTimeSeconds
        };

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
}
