using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Captions;
using Aura.Core.Models;
using Aura.Core.Models.Audio;
using Aura.Core.Models.Localization;
using Aura.Core.Models.Voice;
using Aura.Core.Services.Audio;
using Aura.Core.Services.TTS;
using Aura.Core.Services.Voice;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Localization;

/// <summary>
/// Integration service that connects translation with TTS/SSML pipeline
/// Handles translation → SSML → TTS → subtitle generation workflow
/// </summary>
public class TranslationIntegrationService
{
    private readonly ILogger<TranslationIntegrationService> _logger;
    private readonly TranslationService _translationService;
    private readonly SSMLPlannerService _ssmlPlannerService;
    private readonly CaptionBuilder _captionBuilder;
    private readonly SSMLSubtitleSynchronizer? _subtitleSynchronizer;
    private readonly VoiceProviderRegistry? _voiceRegistry;

    public TranslationIntegrationService(
        ILogger<TranslationIntegrationService> logger,
        TranslationService translationService,
        SSMLPlannerService ssmlPlannerService,
        CaptionBuilder captionBuilder,
        SSMLSubtitleSynchronizer? subtitleSynchronizer = null,
        VoiceProviderRegistry? voiceRegistry = null)
    {
        _logger = logger;
        _translationService = translationService;
        _ssmlPlannerService = ssmlPlannerService;
        _captionBuilder = captionBuilder;
        _subtitleSynchronizer = subtitleSynchronizer;
        _voiceRegistry = voiceRegistry;
    }

    /// <summary>
    /// Translate script and generate SSML with timing alignment
    /// </summary>
    public async Task<TranslatedSSMLResult> TranslateAndPlanSSMLAsync(
        TranslateAndPlanSSMLRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting translation and SSML planning: {Source} → {Target}, Provider: {Provider}",
            request.SourceLanguage, request.TargetLanguage, request.TargetProvider);

        if (_voiceRegistry != null)
        {
            var voiceValidation = _voiceRegistry.ValidateVoice(
                request.TargetProvider.ToString(),
                request.TargetLanguage,
                request.VoiceSpec.VoiceName);

            if (!voiceValidation.IsValid)
            {
                _logger.LogWarning(
                    "Voice validation failed: {Error}. Using fallback.",
                    voiceValidation.ErrorMessage);
                
                if (voiceValidation.FallbackSuggestion != null)
                {
                    _logger.LogInformation(
                        "Fallback voice suggested: {Voice}",
                        voiceValidation.FallbackSuggestion.Name);
                }
            }
            else
            {
                _logger.LogInformation(
                    "Voice validated: {Voice} for {Language}/{Provider}",
                    voiceValidation.MatchedVoice?.Name, request.TargetLanguage, request.TargetProvider);
            }
        }

        var translationRequest = new TranslationRequest
        {
            SourceLanguage = request.SourceLanguage,
            TargetLanguage = request.TargetLanguage,
            ScriptLines = request.ScriptLines.ToList(),
            CulturalContext = request.CulturalContext,
            Options = request.TranslationOptions,
            Glossary = request.Glossary,
            AudienceProfileId = request.AudienceProfileId
        };

        var translationResult = await _translationService.TranslateAsync(
            translationRequest, cancellationToken);

        var translatedScriptLines = translationResult.TranslatedLines
            .Select(line => new ScriptLine(
                line.SceneIndex,
                line.TranslatedText,
                TimeSpan.FromSeconds(line.AdjustedStartSeconds),
                TimeSpan.FromSeconds(line.AdjustedDurationSeconds)))
            .ToList();

        var targetDurations = translationResult.TranslatedLines
            .ToDictionary(
                line => line.SceneIndex,
                line => line.AdjustedDurationSeconds);

        var ssmlRequest = new SSMLPlanningRequest
        {
            ScriptLines = translatedScriptLines,
            TargetProvider = request.TargetProvider,
            VoiceSpec = request.VoiceSpec,
            TargetDurations = targetDurations,
            DurationTolerance = request.DurationTolerance,
            MaxFittingIterations = request.MaxFittingIterations,
            EnableAggressiveAdjustments = request.EnableAggressiveAdjustments
        };

        var ssmlResult = await _ssmlPlannerService.PlanSSMLAsync(
            ssmlRequest, cancellationToken);

        var finalScriptLines = translatedScriptLines;
        SubtitleTimingSyncResult? timingSyncResult = null;

        if (_subtitleSynchronizer != null)
        {
            timingSyncResult = _subtitleSynchronizer.SynchronizeWithSSML(
                ssmlResult.Segments,
                translatedScriptLines,
                request.DurationTolerance);

            if (!timingSyncResult.IsValid)
            {
                _logger.LogWarning(
                    "Subtitle timing synchronization deviation {Deviation:P2} exceeds tolerance {Tolerance:P2}",
                    timingSyncResult.OverallDeviation, request.DurationTolerance);
            }
            else
            {
                _logger.LogInformation(
                    "Subtitle timing synchronized: {WithinTolerance}/{Total} segments within tolerance",
                    timingSyncResult.WithinToleranceCount, timingSyncResult.Adjustments.Count);
            }

            finalScriptLines = timingSyncResult.SynchronizedLines;
        }

        var subtitles = GenerateSubtitles(finalScriptLines, request.SubtitleFormat);

        return new TranslatedSSMLResult
        {
            Translation = translationResult,
            SSMLPlanning = ssmlResult,
            TranslatedScriptLines = finalScriptLines,
            Subtitles = subtitles,
            SubtitleFormat = request.SubtitleFormat,
            TimingSyncResult = timingSyncResult
        };
    }

    /// <summary>
    /// Generate subtitles from translated script lines
    /// </summary>
    private SubtitleOutput GenerateSubtitles(
        IEnumerable<ScriptLine> lines,
        SubtitleFormat format)
    {
        _logger.LogInformation("Generating {Format} subtitles", format);

        if (!_captionBuilder.ValidateTimecodes(lines, out var validationMessage))
        {
            _logger.LogWarning("Subtitle validation warning: {Message}", validationMessage);
        }

        var content = format switch
        {
            SubtitleFormat.SRT => _captionBuilder.GenerateSrt(lines),
            SubtitleFormat.VTT => _captionBuilder.GenerateVtt(lines),
            _ => throw new ArgumentException($"Unsupported subtitle format: {format}")
        };

        return new SubtitleOutput
        {
            Format = format,
            Content = content,
            LineCount = lines.Count()
        };
    }

    /// <summary>
    /// Get recommended voice for target language and provider
    /// </summary>
    public VoiceRecommendation GetRecommendedVoice(
        string targetLanguage,
        VoiceProvider provider,
        string? preferredGender = null,
        string? preferredStyle = null)
    {
        var language = LanguageRegistry.GetLanguage(targetLanguage);
        if (language == null)
        {
            throw new ArgumentException($"Unsupported language: {targetLanguage}");
        }

        _logger.LogInformation(
            "Getting voice recommendation for {Language} with {Provider}",
            targetLanguage, provider);

        var recommendedVoices = provider switch
        {
            VoiceProvider.ElevenLabs => GetElevenLabsVoices(targetLanguage, preferredGender, preferredStyle),
            VoiceProvider.PlayHT => GetPlayHTVoices(targetLanguage, preferredGender, preferredStyle),
            VoiceProvider.WindowsSAPI => GetWindowsSAPIVoices(targetLanguage),
            VoiceProvider.Piper => GetPiperVoices(targetLanguage),
            _ => new List<RecommendedVoice>()
        };

        return new VoiceRecommendation
        {
            TargetLanguage = targetLanguage,
            Provider = provider,
            IsRTL = language.IsRightToLeft,
            RecommendedVoices = recommendedVoices
        };
    }

    private List<RecommendedVoice> GetElevenLabsVoices(
        string language,
        string? gender,
        string? style)
    {
        var voices = new List<RecommendedVoice>();

        var baseLanguage = ExtractBaseLanguageCode(language);
        
        var languageMap = new Dictionary<string, List<(string Name, string Gender, string Style)>>
        {
            ["es"] = new()
            {
                ("Diego", "Male", "Professional"),
                ("Sofia", "Female", "Warm"),
                ("Matias", "Male", "Conversational")
            },
            ["fr"] = new()
            {
                ("Antoine", "Male", "Professional"),
                ("Charlotte", "Female", "Elegant"),
                ("Thomas", "Male", "Friendly")
            },
            ["de"] = new()
            {
                ("Hans", "Male", "Authoritative"),
                ("Greta", "Female", "Professional"),
                ("Klaus", "Male", "Conversational")
            },
            ["ja"] = new()
            {
                ("Akira", "Male", "Professional"),
                ("Sakura", "Female", "Gentle"),
                ("Takeshi", "Male", "Dynamic")
            },
            ["zh"] = new()
            {
                ("Li Wei", "Male", "Professional"),
                ("Mei Lin", "Female", "Warm"),
                ("Zhang Ming", "Male", "Authoritative")
            },
            ["ar"] = new()
            {
                ("Ahmed", "Male", "Professional"),
                ("Fatima", "Female", "Warm"),
                ("Omar", "Male", "Authoritative")
            }
        };

        if (languageMap.TryGetValue(baseLanguage, out var languageVoices))
        {
            voices.AddRange(languageVoices.Select(v => new RecommendedVoice
            {
                VoiceName = v.Name,
                Gender = v.Gender,
                Style = v.Style,
                Quality = "Premium"
            }));
        }

        return voices;
    }

    private List<RecommendedVoice> GetPlayHTVoices(
        string language,
        string? gender,
        string? style)
    {
        return new List<RecommendedVoice>
        {
            new() { VoiceName = "PlayHT Auto", Gender = "Neutral", Style = "Adaptive", Quality = "Premium" }
        };
    }

    private List<RecommendedVoice> GetWindowsSAPIVoices(string language)
    {
        return new List<RecommendedVoice>
        {
            new() { VoiceName = "Microsoft Voice", Gender = "Neutral", Style = "Standard", Quality = "Free" }
        };
    }

    private List<RecommendedVoice> GetPiperVoices(string language)
    {
        return new List<RecommendedVoice>
        {
            new() { VoiceName = "Piper Default", Gender = "Neutral", Style = "Neural", Quality = "Free" }
        };
    }

    private static string ExtractBaseLanguageCode(string languageCode)
    {
        if (string.IsNullOrEmpty(languageCode))
        {
            return "en";
        }

        var hyphenIndex = languageCode.IndexOf('-');
        return hyphenIndex >= 0 ? languageCode.Substring(0, hyphenIndex) : languageCode;
    }
}

/// <summary>
/// Request for translation and SSML planning
/// </summary>
public record TranslateAndPlanSSMLRequest
{
    public required string SourceLanguage { get; init; }
    public required string TargetLanguage { get; init; }
    public required IReadOnlyList<ScriptLine> ScriptLines { get; init; }
    public required VoiceProvider TargetProvider { get; init; }
    public required VoiceSpec VoiceSpec { get; init; }
    public CulturalContext? CulturalContext { get; init; }
    public TranslationOptions TranslationOptions { get; init; } = new();
    public Dictionary<string, string> Glossary { get; init; } = new();
    public string? AudienceProfileId { get; init; }
    public double DurationTolerance { get; init; } = 0.02;
    public int MaxFittingIterations { get; init; } = 10;
    public bool EnableAggressiveAdjustments { get; init; } = false;
    public SubtitleFormat SubtitleFormat { get; init; } = SubtitleFormat.SRT;
}

/// <summary>
/// Result of translation and SSML planning
/// </summary>
public record TranslatedSSMLResult
{
    public required TranslationResult Translation { get; init; }
    public required SSMLPlanningResult SSMLPlanning { get; init; }
    public required IReadOnlyList<ScriptLine> TranslatedScriptLines { get; init; }
    public required SubtitleOutput Subtitles { get; init; }
    public SubtitleFormat SubtitleFormat { get; init; }
    public SubtitleTimingSyncResult? TimingSyncResult { get; init; }
}

/// <summary>
/// Subtitle format
/// </summary>
public enum SubtitleFormat
{
    SRT,
    VTT
}

/// <summary>
/// Subtitle output
/// </summary>
public record SubtitleOutput
{
    public required SubtitleFormat Format { get; init; }
    public required string Content { get; init; }
    public int LineCount { get; init; }
}

/// <summary>
/// Voice recommendation for target language
/// </summary>
public record VoiceRecommendation
{
    public required string TargetLanguage { get; init; }
    public required VoiceProvider Provider { get; init; }
    public bool IsRTL { get; init; }
    public List<RecommendedVoice> RecommendedVoices { get; init; } = new();
}

/// <summary>
/// Recommended voice option
/// </summary>
public record RecommendedVoice
{
    public required string VoiceName { get; init; }
    public required string Gender { get; init; }
    public required string Style { get; init; }
    public required string Quality { get; init; }
}
