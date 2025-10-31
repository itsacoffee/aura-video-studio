using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audience;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Orchestrates intelligent content adaptation based on audience profiles.
/// Coordinates vocabulary adjustment, example personalization, pacing optimization,
/// tone matching, and cognitive load balancing.
/// </summary>
public class ContentAdaptationEngine
{
    private readonly ILogger<ContentAdaptationEngine> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly VocabularyLevelAdjuster _vocabularyAdjuster;
    private readonly ExamplePersonalizationService _examplePersonalizer;
    private readonly PacingAdaptationService _pacingAdapter;
    private readonly ToneAndFormalityOptimizer _toneOptimizer;
    private readonly CognitiveLoadBalancer _loadBalancer;

    public ContentAdaptationEngine(
        ILogger<ContentAdaptationEngine> logger,
        ILlmProvider llmProvider,
        VocabularyLevelAdjuster vocabularyAdjuster,
        ExamplePersonalizationService examplePersonalizer,
        PacingAdaptationService pacingAdapter,
        ToneAndFormalityOptimizer toneOptimizer,
        CognitiveLoadBalancer loadBalancer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _vocabularyAdjuster = vocabularyAdjuster ?? throw new ArgumentNullException(nameof(vocabularyAdjuster));
        _examplePersonalizer = examplePersonalizer ?? throw new ArgumentNullException(nameof(examplePersonalizer));
        _pacingAdapter = pacingAdapter ?? throw new ArgumentNullException(nameof(pacingAdapter));
        _toneOptimizer = toneOptimizer ?? throw new ArgumentNullException(nameof(toneOptimizer));
        _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
    }

    /// <summary>
    /// Adapts content to match target audience profile across multiple dimensions
    /// </summary>
    public async Task<AdaptationResult> AdaptContentAsync(
        AdaptationRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Starting content adaptation for audience: education={Education}, expertise={Expertise}, age={Age}",
            request.Config.AudienceProfile.EducationLevel,
            request.Config.AudienceProfile.ExpertiseLevel,
            request.Config.AudienceProfile.AgeRange);

        try
        {
            var result = new AdaptationResult
            {
                OriginalContent = request.Content,
                AdaptedContent = request.Content,
                OriginalMetrics = await AnalyzeReadabilityAsync(request.Content, ct).ConfigureAwait(false)
            };

            var workingContent = request.Content;

            if (request.Config.AdjustTone)
            {
                _logger.LogDebug("Adjusting tone and formality");
                var toneResult = await _toneOptimizer.OptimizeToneAsync(
                    workingContent,
                    request.Config.AudienceProfile,
                    request.Config.Aggressiveness,
                    ct).ConfigureAwait(false);
                workingContent = toneResult.AdaptedText;
                result = result with { Changes = new(result.Changes) { toneResult.Change } };
            }

            if (request.Config.PersonalizeExamples)
            {
                _logger.LogDebug("Personalizing examples and analogies");
                var exampleResults = await _examplePersonalizer.PersonalizeExamplesAsync(
                    workingContent,
                    request.Config.AudienceProfile,
                    request.Config.MinExamplesPerConcept,
                    request.Config.MaxExamplesPerConcept,
                    ct).ConfigureAwait(false);
                workingContent = exampleResults.AdaptedText;
                result = result with { Changes = new(result.Changes) { exampleResults.Change } };
            }

            if (true)
            {
                _logger.LogDebug("Adjusting vocabulary level");
                var vocabResult = await _vocabularyAdjuster.AdjustVocabularyAsync(
                    workingContent,
                    request.Config.AudienceProfile,
                    request.Config.AddDefinitions,
                    request.Config.Aggressiveness,
                    ct).ConfigureAwait(false);
                workingContent = vocabResult.AdaptedText;
                result = result with { Changes = new(result.Changes) { vocabResult.Change } };
            }

            if (request.Config.AdjustPacing)
            {
                _logger.LogDebug("Adapting pacing and information density");
                var pacingResult = await _pacingAdapter.AdaptPacingAsync(
                    workingContent,
                    request.Config.AudienceProfile,
                    ct).ConfigureAwait(false);
                workingContent = pacingResult.AdaptedText;
                result = result with { Changes = new(result.Changes) { pacingResult.Change } };
            }

            if (request.Config.BalanceCognitiveLoad)
            {
                _logger.LogDebug("Balancing cognitive load");
                var loadResult = await _loadBalancer.BalanceLoadAsync(
                    workingContent,
                    request.Config.AudienceProfile,
                    ct).ConfigureAwait(false);
                workingContent = loadResult.AdaptedText;
                result = result with { Changes = new(result.Changes) { loadResult.Change } };
            }

            var finalMetrics = await AnalyzeReadabilityAsync(workingContent, ct).ConfigureAwait(false);
            var qualityScore = CalculateQualityScore(result.OriginalMetrics, finalMetrics, request.Config.AudienceProfile);

            stopwatch.Stop();

            _logger.LogInformation(
                "Content adaptation completed in {ElapsedMs}ms. Quality score: {QualityScore:F2}",
                stopwatch.ElapsedMilliseconds,
                qualityScore);

            return result with
            {
                AdaptedContent = workingContent,
                AdaptedMetrics = finalMetrics,
                QualityScore = qualityScore,
                AdaptationTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Content adaptation failed");
            throw;
        }
    }

    /// <summary>
    /// Analyzes readability metrics for content
    /// </summary>
    private async Task<ReadabilityMetrics> AnalyzeReadabilityAsync(string content, CancellationToken ct)
    {
        await Task.Delay(10, ct).ConfigureAwait(false);

        var sentences = content.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        var avgSentenceLength = sentences.Length > 0 ? (double)words.Length / sentences.Length : 0;
        var complexWords = 0;
        
        foreach (var word in words)
        {
            if (EstimateSyllables(word) >= 3)
            {
                complexWords++;
            }
        }

        var complexWordPercentage = words.Length > 0 ? (double)complexWords / words.Length * 100 : 0;

        var fleschKincaid = CalculateFleschKincaidGrade(avgSentenceLength, complexWordPercentage);
        var smog = CalculateSmogIndex(sentences.Length, complexWords);
        var cognitiveLoad = EstimateCognitiveLoad(fleschKincaid, complexWordPercentage);
        var vocabularyComplexity = EstimateVocabularyComplexity(complexWordPercentage);

        return new ReadabilityMetrics
        {
            FleschKincaidGrade = fleschKincaid,
            SmogIndex = smog,
            AverageSentenceLength = avgSentenceLength,
            ComplexWordPercentage = complexWordPercentage,
            CognitiveLoad = cognitiveLoad,
            VocabularyComplexity = vocabularyComplexity
        };
    }

    private static double CalculateFleschKincaidGrade(double avgSentenceLength, double complexWordPercentage)
    {
        return 0.39 * avgSentenceLength + 11.8 * (complexWordPercentage / 100) - 15.59;
    }

    private static double CalculateSmogIndex(int sentenceCount, int complexWords)
    {
        if (sentenceCount == 0) return 0;
        return 1.0430 * Math.Sqrt(complexWords * 30.0 / sentenceCount) + 3.1291;
    }

    private static int EstimateCognitiveLoad(double fleschKincaid, double complexWordPercentage)
    {
        var gradeLoad = Math.Min(100, Math.Max(0, fleschKincaid * 5));
        var vocabLoad = complexWordPercentage;
        return (int)Math.Round((gradeLoad + vocabLoad) / 2);
    }

    private static int EstimateVocabularyComplexity(double complexWordPercentage)
    {
        return (int)Math.Round(Math.Min(100, complexWordPercentage * 2));
    }

    private static int EstimateSyllables(string word)
    {
        if (string.IsNullOrWhiteSpace(word)) return 0;
        
        word = word.ToLowerInvariant();
        var vowels = "aeiouy";
        var syllableCount = 0;
        var previousWasVowel = false;

        foreach (var c in word)
        {
            var isVowel = vowels.Contains(c);
            if (isVowel && !previousWasVowel)
            {
                syllableCount++;
            }
            previousWasVowel = isVowel;
        }

        if (word.EndsWith("e", StringComparison.OrdinalIgnoreCase))
        {
            syllableCount--;
        }

        return Math.Max(1, syllableCount);
    }

    private static double CalculateQualityScore(
        ReadabilityMetrics? original,
        ReadabilityMetrics? adapted,
        AudienceProfile profile)
    {
        if (original == null || adapted == null) return 75.0;

        var targetGrade = profile.EducationLevel switch
        {
            EducationLevel.HighSchool => 10.0,
            EducationLevel.Undergraduate => 14.0,
            EducationLevel.Graduate => 16.0,
            EducationLevel.Expert => 18.0,
            _ => 12.0
        };

        var gradeDistance = Math.Abs(adapted.FleschKincaidGrade - targetGrade);
        var gradeScore = Math.Max(0, 100 - (gradeDistance * 10));

        var loadFit = Math.Max(0, 100 - Math.Abs(adapted.CognitiveLoad - profile.CognitiveLoadCapacity));

        return (gradeScore + loadFit) / 2;
    }
}
