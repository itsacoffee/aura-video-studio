using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Audience;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Audience;

/// <summary>
/// Balances cognitive load to match audience capabilities
/// Ensures no scene exceeds audience threshold and inserts breather moments
/// </summary>
public class CognitiveLoadBalancer
{
    private readonly ILogger _logger;

    public CognitiveLoadBalancer(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Balance cognitive load across content
    /// </summary>
    public Task<CognitiveLoadBalancingResult> BalanceLoadAsync(
        string text,
        AudienceAdaptationContext context,
        double threshold,
        CancellationToken cancellationToken)
    {
        var result = new CognitiveLoadBalancingResult
        {
            AdaptedText = text
        };

        try
        {
            _logger.LogInformation("Balancing cognitive load with threshold: {Threshold}", threshold);

            var scenes = SplitIntoScenes(text);
            var loadAnalyses = new List<CognitiveLoadAnalysis>();

            for (int i = 0; i < scenes.Count; i++)
            {
                var analysis = AnalyzeSceneLoad(scenes[i], i, context);
                loadAnalyses.Add(analysis);

                if (analysis.LoadScore > threshold)
                {
                    _logger.LogWarning("Scene {SceneIndex} exceeds threshold: {LoadScore:F1} > {Threshold}",
                        i, analysis.LoadScore, threshold);
                    analysis.ExceedsThreshold = true;
                }
            }

            var adaptedScenes = ApplyCognitiveLoadBalancing(scenes, loadAnalyses, threshold, context);
            result.AdaptedText = string.Join("\n\n", adaptedScenes);
            result.LoadAnalyses = loadAnalyses;
            result.Changes = GenerateLoadChanges(scenes, adaptedScenes);

            _logger.LogInformation("Cognitive load balancing complete. Scenes analyzed: {SceneCount}", scenes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error balancing cognitive load");
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// Split text into scenes
    /// </summary>
    private List<string> SplitIntoScenes(string text)
    {
        return Regex.Split(text, @"\n\n+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    /// <summary>
    /// Analyze cognitive load for a scene
    /// </summary>
    private CognitiveLoadAnalysis AnalyzeSceneLoad(string scene, int index, AudienceAdaptationContext context)
    {
        var analysis = new CognitiveLoadAnalysis
        {
            SceneIndex = index
        };

        analysis.ConceptualComplexity = CalculateConceptualComplexity(scene);
        analysis.VerbalComplexity = CalculateVerbalComplexity(scene);
        analysis.VisualComplexity = 50.0;

        analysis.LoadScore = 
            (analysis.ConceptualComplexity * 0.5) +
            (analysis.VerbalComplexity * 0.3) +
            (analysis.VisualComplexity * 0.2);

        var targetLoad = context.CognitiveCapacity;
        if (analysis.LoadScore > targetLoad)
        {
            analysis.ExceedsThreshold = true;
            analysis.RequiresBreather = true;
            analysis.Recommendations.Add("Insert breather moment after this scene");
            analysis.Recommendations.Add($"Reduce complexity by {analysis.LoadScore - targetLoad:F0} points");
        }

        if (index > 0 && analysis.ConceptualComplexity > 70)
        {
            analysis.Recommendations.Add("Consider adding transitional content before this scene");
        }

        return analysis;
    }

    /// <summary>
    /// Calculate conceptual complexity of scene
    /// </summary>
    private double CalculateConceptualComplexity(string scene)
    {
        double complexity = 50.0;

        var abstractWords = new[] { "concept", "theory", "principle", "abstract", "notion", "paradigm", "framework" };
        var technicalWords = new[] { "algorithm", "process", "system", "mechanism", "protocol", "methodology" };

        var sceneLower = scene.ToLowerInvariant();
        
        foreach (var word in abstractWords)
        {
            if (sceneLower.Contains(word))
            {
                complexity += 5;
            }
        }

        foreach (var word in technicalWords)
        {
            if (sceneLower.Contains(word))
            {
                complexity += 3;
            }
        }

        var sentenceCount = scene.Split('.').Length;
        var wordCount = scene.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var wordsPerSentence = sentenceCount > 0 ? (double)wordCount / sentenceCount : 0;

        if (wordsPerSentence > 25)
        {
            complexity += 10;
        }
        else if (wordsPerSentence > 20)
        {
            complexity += 5;
        }

        return Math.Clamp(complexity, 0, 100);
    }

    /// <summary>
    /// Calculate verbal complexity
    /// </summary>
    private double CalculateVerbalComplexity(string scene)
    {
        var words = scene.Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 0)
        {
            return 0;
        }

        int complexWords = 0;
        int totalSyllables = 0;

        foreach (var word in words)
        {
            int syllables = CountSyllables(word);
            totalSyllables += syllables;
            if (syllables >= 3)
            {
                complexWords++;
            }
        }

        double avgSyllables = (double)totalSyllables / words.Length;
        double complexWordRatio = (double)complexWords / words.Length;

        double complexity = (avgSyllables * 20) + (complexWordRatio * 80);

        return Math.Clamp(complexity, 0, 100);
    }

    /// <summary>
    /// Count syllables in a word (simplified)
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
    /// Apply cognitive load balancing to scenes
    /// </summary>
    private List<string> ApplyCognitiveLoadBalancing(
        List<string> scenes,
        List<CognitiveLoadAnalysis> analyses,
        double threshold,
        AudienceAdaptationContext context)
    {
        var balancedScenes = new List<string>();

        for (int i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            var analysis = analyses[i];

            if (analysis.ExceedsThreshold && analysis.LoadScore > threshold * 1.2)
            {
                scene = SimplifyComplexScene(scene, analysis, context);
            }

            balancedScenes.Add(scene);

            if (analysis.RequiresBreather && i < scenes.Count - 1)
            {
                var nextAnalysis = analyses[i + 1];
                if (nextAnalysis.ConceptualComplexity > 60)
                {
                    var breather = GenerateBreatherMoment(scene, context);
                    if (!string.IsNullOrEmpty(breather))
                    {
                        balancedScenes.Add(breather);
                    }
                }
            }
        }

        return balancedScenes;
    }

    /// <summary>
    /// Simplify a complex scene
    /// </summary>
    private string SimplifyComplexScene(string scene, CognitiveLoadAnalysis analysis, AudienceAdaptationContext context)
    {
        var sentences = scene.Split('.').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        if (sentences.Count > 3)
        {
            var simplified = new List<string>();
            for (int i = 0; i < sentences.Count; i++)
            {
                simplified.Add(sentences[i].Trim() + ".");
                
                if (i > 0 && (i + 1) % 2 == 0 && i < sentences.Count - 1)
                {
                    simplified.Add("Let's break that down.");
                }
            }
            return string.Join(" ", simplified);
        }

        return scene;
    }

    /// <summary>
    /// Generate a breather moment
    /// </summary>
    private string GenerateBreatherMoment(string previousScene, AudienceAdaptationContext context)
    {
        if (context.Profile.ExpertiseLevel == ExpertiseLevel.CompleteBeginner ||
            context.Profile.ExpertiseLevel == ExpertiseLevel.Novice)
        {
            return "Take a moment to let that sink in.";
        }

        return string.Empty;
    }

    /// <summary>
    /// Generate changes from load balancing
    /// </summary>
    private List<AdaptationChange> GenerateLoadChanges(List<string> original, List<string> adapted)
    {
        var changes = new List<AdaptationChange>();

        if (adapted.Count > original.Count)
        {
            changes.Add(new AdaptationChange
            {
                Category = "CognitiveLoad",
                Description = $"Added {adapted.Count - original.Count} breather moment(s)",
                OriginalText = $"{original.Count} scenes",
                AdaptedText = $"{adapted.Count} scenes",
                Reasoning = "Balanced cognitive load to prevent audience overwhelm",
                Position = 0
            });
        }

        return changes;
    }
}

/// <summary>
/// Result of cognitive load balancing
/// </summary>
public class CognitiveLoadBalancingResult
{
    public string AdaptedText { get; set; } = string.Empty;
    public List<AdaptationChange> Changes { get; set; } = new();
    public List<CognitiveLoadAnalysis> LoadAnalyses { get; set; } = new();
}
