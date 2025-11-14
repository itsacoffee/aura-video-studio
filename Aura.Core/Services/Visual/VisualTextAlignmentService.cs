using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.PacingModels;
using Aura.Core.Models.Visual;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Visual;

/// <summary>
/// Service for analyzing and ensuring tight synchronization between narration and visual content.
/// Uses LLM analysis to generate scene-specific visual descriptions that align perfectly with spoken words,
/// maximizing viewer comprehension and retention through coordinated multi-modal delivery.
/// </summary>
public class VisualTextAlignmentService
{
    private readonly ILogger<VisualTextAlignmentService> _logger;
    private readonly TimeSpan _llmTimeout = TimeSpan.FromSeconds(30);
    // private readonly int _maxRetries = 2; // Currently unused - reserved for future retry logic
    private readonly double _targetComplexityCorrelation = -0.7;

    public VisualTextAlignmentService(ILogger<VisualTextAlignmentService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Analyzes synchronization between narration and visuals for all scenes.
    /// Performance target: less than 3 seconds per scene
    /// </summary>
    public async Task<VisualTextSyncResult> AnalyzeSynchronizationAsync(
        IReadOnlyList<Scene> scenes,
        Brief brief,
        ILlmProvider? llmProvider = null,
        IReadOnlyList<SceneTimingSuggestion>? pacingData = null,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Starting visual-text synchronization analysis for {SceneCount} scenes", scenes.Count);

        var segments = new List<NarrationSegment>();
        var warnings = new List<string>();
        var usedLlm = llmProvider != null;

        for (int i = 0; i < scenes.Count; i++)
        {
            var sceneStartTime = DateTime.UtcNow;
            var scene = scenes[i];
            var pacing = pacingData?.FirstOrDefault(p => p.SceneIndex == i);

            var sceneSegments = await AnalyzeSceneSegmentsAsync(
                scene,
                brief,
                pacing,
                llmProvider,
                ct).ConfigureAwait(false);

            segments.AddRange(sceneSegments);

            var sceneTime = (DateTime.UtcNow - sceneStartTime).TotalSeconds;
            if (sceneTime > 3.0)
            {
                _logger.LogWarning("Scene {SceneIndex} analysis took {Duration:F2}s (target: <3s)", 
                    i, sceneTime);
                warnings.Add($"Scene {i} analysis exceeded 3-second target ({sceneTime:F2}s)");
            }

            _logger.LogDebug("Scene {SceneIndex} analyzed in {Duration:F2}s with {SegmentCount} segments",
                i, sceneTime, sceneSegments.Count);
        }

        var cognitiveLoad = CalculateOverallCognitiveLoad(segments);
        var complexityCorrelation = CalculateComplexityCorrelation(segments);
        var transitionAlignment = CalculateTransitionAlignmentRate(segments);

        if (cognitiveLoad > CognitiveLoadMetrics.RecommendedThreshold)
        {
            warnings.Add($"Overall cognitive load ({cognitiveLoad:F1}) exceeds threshold ({CognitiveLoadMetrics.RecommendedThreshold})");
        }

        if (Math.Abs(complexityCorrelation) < Math.Abs(_targetComplexityCorrelation))
        {
            warnings.Add($"Complexity correlation ({complexityCorrelation:F2}) below target ({_targetComplexityCorrelation})");
        }

        if (transitionAlignment < 90.0)
        {
            warnings.Add($"Transition alignment rate ({transitionAlignment:F1}%) below 90% target");
        }

        var totalTime = (DateTime.UtcNow - startTime).TotalSeconds;
        _logger.LogInformation(
            "Visual-text synchronization complete in {Duration:F2}s. Load: {Load:F1}, Correlation: {Corr:F2}, Alignment: {Align:F1}%",
            totalTime, cognitiveLoad, complexityCorrelation, transitionAlignment);

        return new VisualTextSyncResult
        {
            Segments = segments,
            OverallCognitiveLoad = cognitiveLoad,
            ComplexityCorrelation = complexityCorrelation,
            TransitionAlignmentRate = transitionAlignment,
            AnalyzedAt = DateTime.UtcNow,
            Warnings = warnings,
            UsedLlmAnalysis = usedLlm,
            RecommendationSummary = GenerateRecommendationSummary(cognitiveLoad, complexityCorrelation, transitionAlignment)
        };
    }

    /// <summary>
    /// Analyzes a single scene and breaks it into synchronized narration segments
    /// </summary>
    private async Task<List<NarrationSegment>> AnalyzeSceneSegmentsAsync(
        Scene scene,
        Brief brief,
        SceneTimingSuggestion? pacing,
        ILlmProvider? llmProvider,
        CancellationToken ct)
    {
        var sentences = SplitIntoSentences(scene.Script);
        var segments = new List<NarrationSegment>();
        var sceneDuration = scene.Duration.TotalSeconds;
        var currentTime = TimeSpan.Zero;

        for (int i = 0; i < sentences.Count; i++)
        {
            var sentence = sentences[i];
            var sentenceDuration = EstimateSentenceDuration(sentence, pacing);
            
            var narrationComplexity = CalculateNarrationComplexity(sentence);
            var keyConcepts = await IdentifyKeyConceptsAsync(sentence, brief.Tone, llmProvider, ct).ConfigureAwait(false);
            var visualRecommendations = await GenerateVisualRecommendationsAsync(
                sentence, 
                narrationComplexity,
                keyConcepts,
                brief.Tone,
                llmProvider,
                ct).ConfigureAwait(false);

            var cognitiveLoad = CalculateSegmentCognitiveLoad(
                narrationComplexity,
                visualRecommendations,
                keyConcepts.Count);

            var isTransition = DetectTransitionPoint(sentence, i == sentences.Count - 1);
            var narrationRate = CalculateNarrationRate(sentence, sentenceDuration);
            var informationDensity = DetermineInformationDensity(keyConcepts.Count, sentence.Length);

            segments.Add(new NarrationSegment
            {
                SceneIndex = scene.Index,
                Text = sentence,
                StartTime = currentTime,
                Duration = sentenceDuration,
                NarrationComplexity = narrationComplexity,
                KeyConcepts = keyConcepts,
                VisualRecommendations = visualRecommendations,
                CognitiveLoadScore = cognitiveLoad,
                IsTransitionPoint = isTransition,
                NarrationRate = narrationRate,
                InformationDensity = informationDensity
            });

            currentTime += sentenceDuration;
        }

        return segments;
    }

    /// <summary>
    /// Identifies key concepts in narration that require visual support
    /// </summary>
    public async Task<IReadOnlyList<KeyConcept>> IdentifyKeyConceptsAsync(
        string text,
        string tone,
        ILlmProvider? llmProvider,
        CancellationToken ct = default)
    {
        var concepts = new List<KeyConcept>();

        var nouns = ExtractNouns(text);
        foreach (var noun in nouns)
        {
            concepts.Add(new KeyConcept
            {
                Text = noun,
                Type = ConceptType.Noun,
                Importance = 60.0,
                TimeOffset = EstimateWordOffset(text, noun),
                SuggestedVisualization = $"Visual representation of {noun}",
                RequiresMetaphor = IsAbstractNoun(noun),
                SuggestsMotion = false
            });
        }

        var numbers = ExtractDataPoints(text);
        foreach (var number in numbers)
        {
            concepts.Add(new KeyConcept
            {
                Text = number,
                Type = ConceptType.DataPoint,
                Importance = 80.0,
                TimeOffset = EstimateWordOffset(text, number),
                SuggestedVisualization = "Chart or graph visualization",
                RequiresMetaphor = false,
                SuggestsMotion = false
            });
        }

        var actionVerbs = ExtractActionVerbs(text);
        foreach (var verb in actionVerbs)
        {
            concepts.Add(new KeyConcept
            {
                Text = verb,
                Type = ConceptType.ActionVerb,
                Importance = 70.0,
                TimeOffset = EstimateWordOffset(text, verb),
                SuggestedVisualization = $"Motion or animation showing {verb}",
                RequiresMetaphor = false,
                SuggestsMotion = true
            });
        }

        return concepts;
    }

    /// <summary>
    /// Generates visual recommendations with timing and complexity balancing
    /// </summary>
    public async Task<IReadOnlyList<VisualRecommendation>> GenerateVisualRecommendationsAsync(
        string text,
        double narrationComplexity,
        IReadOnlyList<KeyConcept> keyConcepts,
        string tone,
        ILlmProvider? llmProvider,
        CancellationToken ct = default)
    {
        var recommendations = new List<VisualRecommendation>();
        var visualComplexity = CalculateInverseComplexity(narrationComplexity);

        if (keyConcepts.Count > 0)
        {
            var primaryConcept = keyConcepts.OrderByDescending(k => k.Importance).First();
            
            var contentType = DetermineVisualContentType(primaryConcept);
            var metadata = GenerateVisualMetadata(primaryConcept, tone, visualComplexity);
            var brollKeywords = GenerateSpecificBRollKeywords(primaryConcept, text);

            recommendations.Add(new VisualRecommendation
            {
                ContentType = contentType,
                Description = $"{contentType} for {primaryConcept.Text}: {primaryConcept.SuggestedVisualization}",
                StartTime = primaryConcept.TimeOffset,
                Duration = TimeSpan.FromSeconds(3.0),
                VisualComplexity = visualComplexity,
                BRollKeywords = brollKeywords,
                Metadata = metadata,
                Priority = primaryConcept.Importance,
                RequiresDynamicContent = primaryConcept.SuggestsMotion,
                Reasoning = $"Primary concept '{primaryConcept.Text}' requires visual emphasis. " +
                           $"Visual complexity ({visualComplexity:F0}) inversely balanced with narration complexity ({narrationComplexity:F0})"
            });
        }

        if (narrationComplexity > 70.0)
        {
            recommendations.Add(new VisualRecommendation
            {
                ContentType = VisualContentType.Illustration,
                Description = "Clean, simple visual due to high narration complexity",
                StartTime = TimeSpan.Zero,
                Duration = TimeSpan.FromSeconds(5.0),
                VisualComplexity = 20.0,
                BRollKeywords = new[] { "minimalist", "clean background", "simple composition" },
                Metadata = new VisualMetadataTags
                {
                    CompositionRule = CompositionRule.Centered,
                    ColorScheme = new[] { "#ecf0f1", "#3498db" },
                    EmotionalTones = new[] { "calm", "focused" },
                    DepthOfField = "shallow",
                    LightingMood = "soft"
                },
                Priority = 85.0,
                RequiresDynamicContent = false,
                Reasoning = "High narration complexity requires simple, static visuals to reduce cognitive load"
            });
        }

        return recommendations;
    }

    /// <summary>
    /// Validates that visual elements don't contradict narration content
    /// Ensures no mismatches like showing cats while saying "dogs"
    /// </summary>
    public async Task<VisualConsistencyValidation> ValidateVisualConsistencyAsync(
        NarrationSegment segment,
        IReadOnlyList<VisualRecommendation> proposedVisuals,
        ILlmProvider? llmProvider,
        CancellationToken ct = default)
    {
        var contradictions = new List<Contradiction>();
        var supportingElements = new List<string>();

        foreach (var visual in proposedVisuals)
        {
            var narrationWords = segment.Text.ToLowerInvariant().Split(' ');
            var visualWords = visual.Description.ToLowerInvariant().Split(' ');

            var potentialContradiction = DetectContradiction(
                segment.Text,
                visual.Description,
                visual.BRollKeywords);

            if (potentialContradiction != null)
            {
                contradictions.Add(potentialContradiction);
            }
            else
            {
                supportingElements.Add(visual.Description);
            }
        }

        var consistencyScore = contradictions.Count == 0 ? 100.0 :
            Math.Max(0, 100.0 - (contradictions.Count * 20.0));

        return new VisualConsistencyValidation
        {
            IsConsistent = contradictions.Count == 0,
            ConsistencyScore = consistencyScore,
            Contradictions = contradictions,
            SupportingElements = supportingElements,
            AlignmentQuality = consistencyScore > 80 ? "Excellent" :
                              consistencyScore > 60 ? "Good" :
                              consistencyScore > 40 ? "Fair" : "Poor"
        };
    }

    /// <summary>
    /// Balances cognitive load by ensuring visual complexity inversely correlates with narration complexity
    /// Target correlation coefficient > 0.7
    /// </summary>
    public CognitiveLoadMetrics BalanceCognitiveLoadAsync(
        NarrationSegment segment,
        IReadOnlyList<VisualRecommendation> visuals)
    {
        var narrationLoad = segment.NarrationComplexity;
        var visualLoad = visuals.Count > 0 ? visuals.Average(v => v.VisualComplexity) : 30.0;

        var multiModalLoad = CalculateMultiModalLoad(narrationLoad, visualLoad);
        var overallLoad = (narrationLoad + visualLoad + multiModalLoad) / 3.0;

        var processingRate = segment.KeyConcepts.Count / segment.Duration.TotalSeconds;

        var recommendations = new List<string>();
        if (overallLoad > CognitiveLoadMetrics.RecommendedThreshold)
        {
            recommendations.Add($"Reduce cognitive load (current: {overallLoad:F1}, threshold: {CognitiveLoadMetrics.RecommendedThreshold})");
            
            if (narrationLoad > 70.0)
            {
                recommendations.Add("Simplify narration or split into multiple segments");
            }
            
            if (visualLoad > 70.0)
            {
                recommendations.Add("Use simpler, cleaner visuals with less detail");
            }
        }

        var loadBreakdown = $"Narration: {narrationLoad:F1}, Visual: {visualLoad:F1}, Multi-modal: {multiModalLoad:F1}";

        return new CognitiveLoadMetrics
        {
            OverallLoad = overallLoad,
            NarrationLoad = narrationLoad,
            VisualLoad = visualLoad,
            MultiModalLoad = multiModalLoad,
            ProcessingRate = processingRate,
            LoadBreakdown = loadBreakdown,
            Recommendations = recommendations
        };
    }

    /// <summary>
    /// Generates visual metadata tags with camera angles, composition rules, and color schemes
    /// </summary>
    public VisualMetadataTags GenerateVisualMetadata(
        KeyConcept concept,
        string tone,
        double visualComplexity)
    {
        var cameraAngle = DetermineCameraAngle(concept, tone);
        var compositionRule = DetermineCompositionRule(concept, visualComplexity);
        var colorScheme = DetermineColorScheme(tone, concept.Type);
        var emotionalTones = DetermineEmotionalTones(tone);
        var shotType = DetermineShotType(concept, visualComplexity);
        var attentionCues = GenerateAttentionCues(concept);

        return new VisualMetadataTags
        {
            CameraAngle = cameraAngle,
            CompositionRule = compositionRule,
            FocusPoint = concept.Text,
            ColorScheme = colorScheme,
            EmotionalTones = emotionalTones,
            ShotType = shotType,
            DepthOfField = visualComplexity < 40 ? "shallow" : "medium",
            LightingMood = DetermineLightingMood(tone),
            UseMotionBlur = concept.SuggestsMotion,
            AttentionCues = attentionCues
        };
    }

    /// <summary>
    /// Calculates pacing recommendations where visual transitions align with narration pauses
    /// </summary>
    public VisualPacingRecommendation CalculatePacingRecommendationsAsync(
        Scene scene,
        IReadOnlyList<NarrationSegment> segments)
    {
        var avgRate = segments.Average(s => s.NarrationRate);
        
        var recommendedChanges = avgRate > 180 ? 2 :
                                avgRate > 150 ? 3 :
                                avgRate > 120 ? 4 : 5;

        var transitionPoints = segments
            .Where(s => s.IsTransitionPoint)
            .Select(s => s.StartTime)
            .ToList();

        var hasNaturalPauses = segments.Any(s => s.IsTransitionPoint);

        var transitionDuration = avgRate > 180 ? TimeSpan.FromMilliseconds(150) :
                                avgRate > 150 ? TimeSpan.FromMilliseconds(300) :
                                TimeSpan.FromMilliseconds(500);

        return new VisualPacingRecommendation
        {
            SceneIndex = scene.Index,
            NarrationRate = avgRate,
            RecommendedVisualChanges = recommendedChanges,
            TransitionDuration = transitionDuration,
            TransitionPoints = transitionPoints,
            HasNaturalPauses = hasNaturalPauses,
            Reasoning = $"Narration rate of {avgRate:F0} WPM suggests {recommendedChanges} visual changes. " +
                       (avgRate > 180 ? "Fast pace requires fewer visual changes to avoid overload." :
                        avgRate < 120 ? "Slow pace allows for more visual variety." :
                        "Moderate pace balanced with visual transitions.")
        };
    }

    private List<string> SplitIntoSentences(string text)
    {
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
        
        return sentences.Count > 0 ? sentences : new List<string> { text };
    }

    private TimeSpan EstimateSentenceDuration(string sentence, SceneTimingSuggestion? pacing)
    {
        var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var wpm = 157.0;
        
        if (pacing != null)
        {
            wpm = pacing.InformationDensity == InformationDensity.High ? 130.0 :
                  pacing.InformationDensity == InformationDensity.Low ? 180.0 : 157.0;
        }
        
        var seconds = (words / wpm) * 60.0;
        return TimeSpan.FromSeconds(Math.Max(1.0, seconds));
    }

    private double CalculateNarrationComplexity(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var avgWordLength = words.Average(w => w.Length);
        var sentenceLength = words.Length;
        
        var lengthComplexity = Math.Min(100, avgWordLength * 10);
        var densityComplexity = Math.Min(100, sentenceLength * 2);
        
        return (lengthComplexity + densityComplexity) / 2.0;
    }

    private double CalculateInverseComplexity(double narrationComplexity)
    {
        return Math.Max(10.0, 100.0 - narrationComplexity);
    }

    private double CalculateSegmentCognitiveLoad(
        double narrationComplexity,
        IReadOnlyList<VisualRecommendation> visuals,
        int conceptCount)
    {
        var visualComplexity = visuals.Count > 0 ? visuals.Average(v => v.VisualComplexity) : 30.0;
        var conceptDensity = Math.Min(100, conceptCount * 15);
        
        return (narrationComplexity + visualComplexity + conceptDensity) / 3.0;
    }

    private bool DetectTransitionPoint(string sentence, bool isLast)
    {
        if (isLast) return true;
        
        var transitionWords = new[] { "however", "therefore", "meanwhile", "next", "finally", "now", "then" };
        var lowerSentence = sentence.ToLowerInvariant();
        
        return transitionWords.Any(tw => lowerSentence.Contains(tw));
    }

    private double CalculateNarrationRate(string sentence, TimeSpan duration)
    {
        var words = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var minutes = duration.TotalMinutes;
        return minutes > 0 ? words / minutes : 157.0;
    }

    private InformationDensity DetermineInformationDensity(int conceptCount, int textLength)
    {
        var density = (double)conceptCount / textLength * 100;
        
        return density > 15 ? InformationDensity.High :
               density > 8 ? InformationDensity.Medium :
               InformationDensity.Low;
    }

    private List<string> ExtractNouns(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var nouns = new List<string>();
        
        foreach (var word in words)
        {
            var cleaned = Regex.Replace(word, @"[^\w]", "");
            if (cleaned.Length > 3 && char.IsUpper(cleaned[0]))
            {
                nouns.Add(cleaned);
            }
        }
        
        return nouns.Take(3).ToList();
    }

    private List<string> ExtractDataPoints(string text)
    {
        var numbers = Regex.Matches(text, @"\b\d+(\.\d+)?%?\b")
            .Select(m => m.Value)
            .ToList();
        
        return numbers.Take(2).ToList();
    }

    private List<string> ExtractActionVerbs(string text)
    {
        var commonVerbs = new[] { "run", "jump", "fly", "move", "grow", "create", "build", "develop", "transform", "change" };
        var found = new List<string>();
        
        var lowerText = text.ToLowerInvariant();
        foreach (var verb in commonVerbs)
        {
            if (lowerText.Contains(verb))
            {
                found.Add(verb);
            }
        }
        
        return found.Take(2).ToList();
    }

    private bool IsAbstractNoun(string noun)
    {
        var abstractTerms = new[] { "concept", "idea", "theory", "principle", "strategy", "approach", "method" };
        return abstractTerms.Any(term => noun.ToLowerInvariant().Contains(term));
    }

    private TimeSpan EstimateWordOffset(string text, string word)
    {
        var index = text.IndexOf(word, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return TimeSpan.Zero;
        
        var precedingText = text.Substring(0, index);
        var precedingWords = precedingText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var seconds = (precedingWords / 157.0) * 60.0;
        
        return TimeSpan.FromSeconds(seconds);
    }

    private VisualContentType DetermineVisualContentType(KeyConcept concept)
    {
        return concept.Type switch
        {
            ConceptType.DataPoint => VisualContentType.Chart,
            ConceptType.ActionVerb => VisualContentType.Animation,
            ConceptType.AbstractConcept => VisualContentType.Metaphor,
            ConceptType.TechnicalTerm => VisualContentType.Illustration,
            _ => VisualContentType.BRoll
        };
    }

    private List<string> GenerateSpecificBRollKeywords(KeyConcept concept, string context)
    {
        var keywords = new List<string> { concept.Text };
        
        var contextWords = context.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length > 4)
            .Take(3);
        
        keywords.AddRange(contextWords);
        
        if (concept.Type == ConceptType.ActionVerb)
        {
            keywords.Add($"{concept.Text} in action");
            keywords.Add($"dynamic {concept.Text}");
        }
        
        return keywords.Distinct().ToList();
    }

    private CameraAngle DetermineCameraAngle(KeyConcept concept, string tone)
    {
        if (concept.Type == ConceptType.DataPoint) return CameraAngle.EyeLevel;
        if (concept.SuggestsMotion) return CameraAngle.LowAngle;
        
        return tone.ToLowerInvariant() switch
        {
            "dramatic" => CameraAngle.LowAngle,
            "professional" => CameraAngle.EyeLevel,
            _ => CameraAngle.EyeLevel
        };
    }

    private CompositionRule DetermineCompositionRule(KeyConcept concept, double visualComplexity)
    {
        if (visualComplexity < 30) return CompositionRule.Centered;
        if (concept.Type == ConceptType.DataPoint) return CompositionRule.SymmetricalBalance;
        
        return CompositionRule.RuleOfThirds;
    }

    private List<string> DetermineColorScheme(string tone, ConceptType conceptType)
    {
        if (conceptType == ConceptType.DataPoint)
        {
            return new List<string> { "#3498db", "#2ecc71", "#e74c3c", "#f39c12" };
        }
        
        return tone.ToLowerInvariant() switch
        {
            "professional" => new List<string> { "#2c3e50", "#ecf0f1", "#3498db" },
            "dramatic" => new List<string> { "#1a1a1a", "#8b0000", "#ffd700" },
            _ => new List<string> { "#34495e", "#ecf0f1", "#3498db" }
        };
    }

    private List<string> DetermineEmotionalTones(string tone)
    {
        return tone.ToLowerInvariant() switch
        {
            "professional" => new List<string> { "confident", "authoritative", "focused" },
            "dramatic" => new List<string> { "intense", "powerful", "compelling" },
            "conversational" => new List<string> { "friendly", "approachable", "warm" },
            _ => new List<string> { "neutral", "balanced", "clear" }
        };
    }

    private ShotType DetermineShotType(KeyConcept concept, double visualComplexity)
    {
        if (concept.Type == ConceptType.DataPoint) return ShotType.MediumShot;
        if (visualComplexity < 30) return ShotType.CloseUp;
        
        return ShotType.MediumShot;
    }

    private string DetermineLightingMood(string tone)
    {
        return tone.ToLowerInvariant() switch
        {
            "dramatic" => "high contrast",
            "professional" => "balanced",
            _ => "soft"
        };
    }

    private List<string> GenerateAttentionCues(KeyConcept concept)
    {
        return new List<string> { concept.Text, $"Focus on {concept.Text}" };
    }

    private Contradiction? DetectContradiction(string narration, string visualDesc, IReadOnlyList<string> keywords)
    {
        var contradictionPairs = new Dictionary<string, string[]>
        {
            { "cat", new[] { "dog", "canine" } },
            { "dog", new[] { "cat", "feline" } },
            { "up", new[] { "down", "descending" } },
            { "down", new[] { "up", "ascending" } },
            { "hot", new[] { "cold", "freezing" } },
            { "cold", new[] { "hot", "burning" } }
        };

        var narrationLower = narration.ToLowerInvariant();
        var visualLower = visualDesc.ToLowerInvariant();

        foreach (var pair in contradictionPairs)
        {
            if (narrationLower.Contains(pair.Key))
            {
                foreach (var opposite in pair.Value)
                {
                    if (visualLower.Contains(opposite) || keywords.Any(k => k.ToLowerInvariant().Contains(opposite)))
                    {
                        return new Contradiction
                        {
                            NarrationContent = $"Narration mentions '{pair.Key}'",
                            VisualContent = $"Visual shows '{opposite}'",
                            TimeOffset = TimeSpan.Zero,
                            Severity = 80.0,
                            SuggestedCorrection = $"Replace '{opposite}' with '{pair.Key}' in visual content"
                        };
                    }
                }
            }
        }

        return null;
    }

    private double CalculateMultiModalLoad(double narrationLoad, double visualLoad)
    {
        return Math.Min(100, (narrationLoad + visualLoad) / 2.0 * 1.2);
    }

    private double CalculateOverallCognitiveLoad(IReadOnlyList<NarrationSegment> segments)
    {
        return segments.Count > 0 ? segments.Average(s => s.CognitiveLoadScore) : 0.0;
    }

    private double CalculateComplexityCorrelation(IReadOnlyList<NarrationSegment> segments)
    {
        if (segments.Count < 2) return 0.0;

        var narrationComplexities = segments.Select(s => s.NarrationComplexity).ToList();
        var visualComplexities = segments
            .Select(s => s.VisualRecommendations.Count > 0 ? 
                s.VisualRecommendations.Average(v => v.VisualComplexity) : 50.0)
            .ToList();

        return CalculatePearsonCorrelation(narrationComplexities, visualComplexities);
    }

    private double CalculatePearsonCorrelation(List<double> x, List<double> y)
    {
        if (x.Count != y.Count || x.Count == 0) return 0.0;

        var avgX = x.Average();
        var avgY = y.Average();

        var sumProduct = x.Zip(y, (a, b) => (a - avgX) * (b - avgY)).Sum();
        var sumXSquared = x.Sum(a => Math.Pow(a - avgX, 2));
        var sumYSquared = y.Sum(b => Math.Pow(b - avgY, 2));

        if (sumXSquared == 0 || sumYSquared == 0) return 0.0;

        return sumProduct / Math.Sqrt(sumXSquared * sumYSquared);
    }

    private double CalculateTransitionAlignmentRate(IReadOnlyList<NarrationSegment> segments)
    {
        var transitionPoints = segments.Count(s => s.IsTransitionPoint);
        var visualTransitions = segments.Count(s => s.VisualRecommendations.Any());
        
        if (transitionPoints == 0) return 100.0;
        
        var aligned = segments.Count(s => s.IsTransitionPoint && s.VisualRecommendations.Any());
        return (double)aligned / transitionPoints * 100.0;
    }

    private string GenerateRecommendationSummary(double cognitiveLoad, double correlation, double alignment)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"Cognitive Load: {cognitiveLoad:F1}/100 ({(cognitiveLoad < 75 ? "PASS" : "REVIEW")})");
        sb.AppendLine($"Complexity Correlation: {correlation:F2} ({(Math.Abs(correlation) >= 0.7 ? "PASS" : "REVIEW")})");
        sb.AppendLine($"Transition Alignment: {alignment:F1}% ({(alignment >= 90 ? "PASS" : "REVIEW")})");
        
        return sb.ToString();
    }


}
