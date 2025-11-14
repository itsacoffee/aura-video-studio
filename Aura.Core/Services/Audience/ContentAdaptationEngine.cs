using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audience;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Intelligent content adaptation engine that uses audience profile to automatically adjust
/// script complexity, vocabulary, examples, pacing, and tone to match target audience
/// </summary>
public class ContentAdaptationEngine
{
    private readonly ILogger<ContentAdaptationEngine> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly VocabularyLevelAdjuster _vocabularyAdjuster;
    private readonly ExamplePersonalizer _examplePersonalizer;
    private readonly PacingAdapter _pacingAdapter;
    private readonly ToneOptimizer _toneOptimizer;
    private readonly CognitiveLoadBalancer _cognitiveLoadBalancer;

    public ContentAdaptationEngine(
        ILogger<ContentAdaptationEngine> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _vocabularyAdjuster = new VocabularyLevelAdjuster(logger, llmProvider);
        _examplePersonalizer = new ExamplePersonalizer(logger, llmProvider);
        _pacingAdapter = new PacingAdapter(logger, llmProvider);
        _toneOptimizer = new ToneOptimizer(logger, llmProvider);
        _cognitiveLoadBalancer = new CognitiveLoadBalancer(logger);
    }

    /// <summary>
    /// Adapt content based on audience profile
    /// </summary>
    public async Task<ContentAdaptationResult> AdaptContentAsync(
        string originalContent,
        AudienceProfile audienceProfile,
        ContentAdaptationConfig config,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting content adaptation for audience: {AudienceName}", audienceProfile.Name);

        var result = new ContentAdaptationResult
        {
            OriginalContent = originalContent,
            AdaptedContent = originalContent
        };

        try
        {
            // Calculate original metrics
            result.OriginalMetrics = CalculateReadabilityMetrics(originalContent);
            _logger.LogInformation("Original content metrics - Grade Level: {GradeLevel:F1}, SMOG: {Smog:F1}",
                result.OriginalMetrics.FleschKincaidGradeLevel,
                result.OriginalMetrics.SmogScore);

            // Build adaptation context from audience profile
            var context = BuildAdaptationContext(audienceProfile);

            string adaptedContent = originalContent;

            // Phase 1: Vocabulary adjustment
            if (config.EnableVocabularyAdjustment)
            {
                _logger.LogInformation("Adjusting vocabulary level to target: {TargetLevel:F1}", context.TargetReadingLevel);
                var vocabResult = await _vocabularyAdjuster.AdjustVocabularyAsync(
                    adaptedContent, 
                    context, 
                    config.AggressivenessLevel,
                    cancellationToken).ConfigureAwait(false);
                adaptedContent = vocabResult.AdaptedText;
                result.Changes.AddRange(vocabResult.Changes);
            }

            // Phase 2: Example personalization
            if (config.EnableExamplePersonalization)
            {
                _logger.LogInformation("Personalizing examples for audience profile");
                var exampleResult = await _examplePersonalizer.PersonalizeExamplesAsync(
                    adaptedContent,
                    context,
                    config.ExamplesPerConcept,
                    cancellationToken).ConfigureAwait(false);
                adaptedContent = exampleResult.AdaptedText;
                result.Changes.AddRange(exampleResult.Changes);
                result.OverallRelevanceScore = exampleResult.AverageRelevanceScore;
            }

            // Phase 3: Pacing adaptation
            if (config.EnablePacingAdaptation)
            {
                _logger.LogInformation("Adapting pacing for expertise level: {ExpertiseLevel}", 
                    audienceProfile.ExpertiseLevel);
                var pacingResult = await _pacingAdapter.AdaptPacingAsync(
                    adaptedContent,
                    context,
                    cancellationToken).ConfigureAwait(false);
                adaptedContent = pacingResult.AdaptedText;
                result.Changes.AddRange(pacingResult.Changes);
            }

            // Phase 4: Tone and formality optimization
            if (config.EnableToneOptimization)
            {
                _logger.LogInformation("Optimizing tone and formality");
                var toneResult = await _toneOptimizer.OptimizeToneAsync(
                    adaptedContent,
                    context,
                    cancellationToken).ConfigureAwait(false);
                adaptedContent = toneResult.AdaptedText;
                result.Changes.AddRange(toneResult.Changes);
            }

            // Phase 5: Cognitive load balancing
            if (config.EnableCognitiveLoadBalancing)
            {
                _logger.LogInformation("Balancing cognitive load");
                var loadResult = await _cognitiveLoadBalancer.BalanceLoadAsync(
                    adaptedContent,
                    context,
                    config.CognitiveLoadThreshold,
                    cancellationToken).ConfigureAwait(false);
                adaptedContent = loadResult.AdaptedText;
                result.Changes.AddRange(loadResult.Changes);
            }

            result.AdaptedContent = adaptedContent;
            result.AdaptedMetrics = CalculateReadabilityMetrics(adaptedContent);
            result.ProcessingTime = stopwatch.Elapsed;

            _logger.LogInformation(
                "Content adaptation complete in {ElapsedSeconds:F2}s. Changes: {ChangeCount}, " +
                "Grade Level: {OriginalGL:F1} → {AdaptedGL:F1}",
                stopwatch.Elapsed.TotalSeconds,
                result.Changes.Count,
                result.OriginalMetrics.FleschKincaidGradeLevel,
                result.AdaptedMetrics.FleschKincaidGradeLevel);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during content adaptation");
            result.ProcessingTime = stopwatch.Elapsed;
            return result;
        }
    }

    /// <summary>
    /// Build adaptation context from audience profile
    /// </summary>
    private AudienceAdaptationContext BuildAdaptationContext(AudienceProfile profile)
    {
        var context = new AudienceAdaptationContext
        {
            Profile = profile
        };

        // Calculate target reading level based on education
        context.TargetReadingLevel = profile.EducationLevel switch
        {
            EducationLevel.HighSchool => 9.0,
            EducationLevel.SomeCollege => 11.0,
            EducationLevel.AssociateDegree => 12.0,
            EducationLevel.BachelorDegree => 13.0,
            EducationLevel.MasterDegree => 14.0,
            EducationLevel.Doctorate => 16.0,
            EducationLevel.Vocational => 10.0,
            EducationLevel.SelfTaught => 11.0,
            EducationLevel.InProgress => 10.0,
            _ => 12.0
        };

        // Adjust for expertise level
        if (profile.ExpertiseLevel.HasValue)
        {
            context.TargetReadingLevel += profile.ExpertiseLevel.Value switch
            {
                ExpertiseLevel.CompleteBeginner => -2.0,
                ExpertiseLevel.Novice => -1.0,
                ExpertiseLevel.Intermediate => 0.0,
                ExpertiseLevel.Advanced => 1.0,
                ExpertiseLevel.Expert => 2.0,
                ExpertiseLevel.Professional => 3.0,
                _ => 0.0
            };
        }

        // Determine preferred analogies based on profession and interests
        context.PreferredAnalogies = DeterminePreferredAnalogies(profile);

        // Determine cultural references based on region
        context.CulturalReferences = DetermineCulturalReferences(profile);

        // Calculate pacing multiplier based on expertise and attention span
        context.PacingMultiplier = CalculatePacingMultiplier(profile);

        // Determine formality level
        context.FormalityLevel = DetermineFormalityLevel(profile);

        // Calculate cognitive capacity
        context.CognitiveCapacity = CalculateCognitiveCapacity(profile);

        // Determine communication style
        context.CommunicationStyle = DetermineCommunicationStyle(profile);

        return context;
    }

    /// <summary>
    /// Determine preferred analogies based on audience characteristics
    /// </summary>
    private List<string> DeterminePreferredAnalogies(AudienceProfile profile)
    {
        var analogies = new List<string>();

        if (!string.IsNullOrEmpty(profile.Profession))
        {
            var professionLower = profile.Profession.ToLowerInvariant();
            if (professionLower.Contains("tech") || professionLower.Contains("developer") || professionLower.Contains("engineer"))
            {
                analogies.Add("programming");
                analogies.Add("software");
                analogies.Add("algorithm");
            }
            else if (professionLower.Contains("teacher") || professionLower.Contains("educator"))
            {
                analogies.Add("academic");
                analogies.Add("classroom");
                analogies.Add("learning");
            }
            else if (professionLower.Contains("medical") || professionLower.Contains("healthcare"))
            {
                analogies.Add("medical");
                analogies.Add("health");
                analogies.Add("diagnosis");
            }
        }

        if (profile.Interests.Count > 0)
        {
            foreach (var interest in profile.Interests.Take(3))
            {
                analogies.Add(interest.ToLowerInvariant());
            }
        }

        if (profile.AgeRange != null)
        {
            if (profile.AgeRange.MinAge < 18)
            {
                analogies.Add("school");
                analogies.Add("gaming");
                analogies.Add("social-media");
            }
            else if (profile.AgeRange.MinAge >= 25 && profile.AgeRange.MaxAge <= 45)
            {
                analogies.Add("parenting");
                analogies.Add("career");
                analogies.Add("home");
            }
        }

        return analogies.Distinct().ToList();
    }

    /// <summary>
    /// Determine culturally relevant references
    /// </summary>
    private List<string> DetermineCulturalReferences(AudienceProfile profile)
    {
        var references = new List<string>();

        if (profile.GeographicRegion.HasValue)
        {
            references.Add(profile.GeographicRegion.Value.ToString());

            switch (profile.GeographicRegion.Value)
            {
                case GeographicRegion.NorthAmerica:
                    references.Add("football");
                    references.Add("thanksgiving");
                    break;
                case GeographicRegion.Europe:
                    references.Add("football-soccer");
                    references.Add("euro");
                    break;
                case GeographicRegion.Asia:
                    references.Add("lunar-new-year");
                    references.Add("cricket");
                    break;
            }
        }

        return references;
    }

    /// <summary>
    /// Calculate pacing multiplier based on audience characteristics
    /// </summary>
    private double CalculatePacingMultiplier(AudienceProfile profile)
    {
        double multiplier = 1.0;

        if (profile.ExpertiseLevel.HasValue)
        {
            multiplier = profile.ExpertiseLevel.Value switch
            {
                ExpertiseLevel.CompleteBeginner => 1.3,
                ExpertiseLevel.Novice => 1.2,
                ExpertiseLevel.Intermediate => 1.0,
                ExpertiseLevel.Advanced => 0.9,
                ExpertiseLevel.Expert => 0.8,
                ExpertiseLevel.Professional => 0.75,
                _ => 1.0
            };
        }

        if (profile.AttentionSpan != null)
        {
            if (profile.AttentionSpan.PreferredDuration < TimeSpan.FromMinutes(3))
            {
                multiplier *= 0.9;
            }
            else if (profile.AttentionSpan.PreferredDuration > TimeSpan.FromMinutes(10))
            {
                multiplier *= 1.1;
            }
        }

        return multiplier;
    }

    /// <summary>
    /// Determine formality level based on audience
    /// </summary>
    private string DetermineFormalityLevel(AudienceProfile profile)
    {
        if (profile.AgeRange != null && profile.AgeRange.MinAge < 25)
        {
            return "casual";
        }

        if (!string.IsNullOrEmpty(profile.Profession))
        {
            var professionLower = profile.Profession.ToLowerInvariant();
            if (professionLower.Contains("executive") || 
                professionLower.Contains("professional") ||
                professionLower.Contains("business"))
            {
                return "professional";
            }
        }

        if (profile.EducationLevel.HasValue)
        {
            if (profile.EducationLevel.Value >= EducationLevel.MasterDegree)
            {
                return "academic";
            }
        }

        return "conversational";
    }

    /// <summary>
    /// Calculate cognitive capacity based on profile
    /// </summary>
    private double CalculateCognitiveCapacity(AudienceProfile profile)
    {
        double capacity = 75.0;

        if (profile.ExpertiseLevel.HasValue)
        {
            capacity += profile.ExpertiseLevel.Value switch
            {
                ExpertiseLevel.CompleteBeginner => -15,
                ExpertiseLevel.Novice => -10,
                ExpertiseLevel.Intermediate => 0,
                ExpertiseLevel.Advanced => 10,
                ExpertiseLevel.Expert => 15,
                ExpertiseLevel.Professional => 20,
                _ => 0
            };
        }

        if (profile.EducationLevel.HasValue)
        {
            capacity += profile.EducationLevel.Value switch
            {
                EducationLevel.HighSchool => -5,
                EducationLevel.BachelorDegree => 5,
                EducationLevel.MasterDegree => 10,
                EducationLevel.Doctorate => 15,
                _ => 0
            };
        }

        return Math.Clamp(capacity, 50.0, 100.0);
    }

    /// <summary>
    /// Determine communication style preference
    /// </summary>
    private string DetermineCommunicationStyle(AudienceProfile profile)
    {
        if (profile.CulturalBackground?.PreferredCommunicationStyle != null)
        {
            return profile.CulturalBackground.PreferredCommunicationStyle switch
            {
                CommunicationStyle.Direct => "direct",
                CommunicationStyle.Indirect => "indirect",
                CommunicationStyle.Formal => "formal",
                CommunicationStyle.Casual => "casual",
                CommunicationStyle.Humorous => "humorous",
                CommunicationStyle.Professional => "professional",
                _ => "conversational"
            };
        }

        return "conversational";
    }

    /// <summary>
    /// Calculate readability metrics for text
    /// </summary>
    private ReadabilityMetrics CalculateReadabilityMetrics(string text)
    {
        var metrics = new ReadabilityMetrics();

        if (string.IsNullOrWhiteSpace(text))
        {
            return metrics;
        }

        var sentences = Regex.Split(text, @"[.!?]+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var words = Regex.Split(text, @"\s+").Where(w => !string.IsNullOrWhiteSpace(w)).ToList();

        if (sentences.Count == 0 || words.Count == 0)
        {
            return metrics;
        }

        int totalSyllables = 0;
        int complexWords = 0;

        foreach (var word in words)
        {
            int syllables = CountSyllables(word);
            totalSyllables += syllables;
            if (syllables >= 3)
            {
                complexWords++;
            }
        }

        metrics.AverageWordsPerSentence = (double)words.Count / sentences.Count;
        metrics.AverageSyllablesPerWord = (double)totalSyllables / words.Count;
        metrics.ComplexWordPercentage = (double)complexWords / words.Count * 100;

        // Flesch-Kincaid Grade Level
        metrics.FleschKincaidGradeLevel = 
            0.39 * metrics.AverageWordsPerSentence + 
            11.8 * metrics.AverageSyllablesPerWord - 
            15.59;

        // SMOG Score (Simple Measure of Gobbledygook)
        metrics.SmogScore = 1.0430 * Math.Sqrt(complexWords * (30.0 / sentences.Count)) + 3.1291;

        // Technical term density (simplified heuristic)
        metrics.TechnicalTermDensity = metrics.ComplexWordPercentage * 0.8;

        // Overall complexity
        metrics.OverallComplexity = (metrics.FleschKincaidGradeLevel * 4 + metrics.SmogScore * 3 + metrics.ComplexWordPercentage) / 8;

        return metrics;
    }

    /// <summary>
    /// Count syllables in a word (simplified algorithm)
    /// </summary>
    private int CountSyllables(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return 0;
        }

        word = word.ToLowerInvariant();
        word = Regex.Replace(word, @"[^a-z]", "");

        if (word.Length <= 3)
        {
            return 1;
        }

        word = Regex.Replace(word, @"(?:[^laeiouy]es|ed|[^laeiouy]e)$", "");
        word = Regex.Replace(word, @"^y", "");

        var matches = Regex.Matches(word, @"[aeiouy]+");
        int syllables = matches.Count;

        return syllables == 0 ? 1 : syllables;
    }

    /// <summary>
    /// Get target reading level description for audience
    /// </summary>
    public string GetTargetReadingLevelDescription(AudienceProfile profile)
    {
        var context = BuildAdaptationContext(profile);
        return context.TargetReadingLevel switch
        {
            < 9 => "Elementary (6th-8th grade)",
            < 10 => "High School Freshman (9th grade)",
            < 12 => "High School (10th-11th grade)",
            < 13 => "High School Senior / College Freshman",
            < 15 => "College (Undergraduate)",
            < 17 => "College (Advanced) / Graduate",
            _ => "Professional / Academic"
        };
    }
}
