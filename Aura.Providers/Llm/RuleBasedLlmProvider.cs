using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.AI;
using Microsoft.Extensions.Logging;

namespace Aura.Providers.Llm;

/// <summary>
/// Rule-based LLM provider for offline script generation.
/// Supports prompt customization for additional instructions.
/// </summary>
public class RuleBasedLlmProvider : ILlmProvider
{
    private readonly ILogger<RuleBasedLlmProvider> _logger;
    private readonly Random _random;
    private readonly PromptCustomizationService _promptCustomizationService;

    public RuleBasedLlmProvider(
        ILogger<RuleBasedLlmProvider> logger,
        PromptCustomizationService? promptCustomizationService = null)
    {
        _logger = logger;
        _random = new Random(42); // Fixed seed for deterministic output
        
        // Create PromptCustomizationService if not provided (using logger factory pattern)
        if (promptCustomizationService == null)
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Warning));
            var customizationLogger = loggerFactory.CreateLogger<PromptCustomizationService>();
            _promptCustomizationService = new PromptCustomizationService(customizationLogger);
        }
        else
        {
            _promptCustomizationService = promptCustomizationService;
        }
    }

    public Task<string> DraftScriptAsync(Brief brief, PlanSpec spec, CancellationToken ct)
    {
        _logger.LogInformation("Generating rule-based script for topic: {Topic}", brief.Topic);
        
        // Log if user has provided custom instructions
        if (brief.PromptModifiers?.AdditionalInstructions != null)
        {
            _logger.LogInformation("User provided additional instructions for script generation");
        }
        
        // Calculate the target word count based on duration, pacing, and density
        int targetWordCount = CalculateWordCount(spec.TargetDuration, spec.Pacing, spec.Density);
        
        // Determine the number of scenes based on the total duration
        int sceneCount = DetermineSceneCount(spec.TargetDuration);
        
        // Approximate words per scene
        int wordsPerScene = targetWordCount / sceneCount;
        
        _logger.LogDebug("Target word count: {WordCount}, Scenes: {SceneCount}, Words per scene: {WordsPerScene}",
            targetWordCount, sceneCount, wordsPerScene);

        // Generate the script (RuleBased provider uses templates, prompt modifiers logged but not directly applied)
        string script = GenerateScript(brief, spec, sceneCount, wordsPerScene);
        
        return Task.FromResult(script);
    }

    private int CalculateWordCount(TimeSpan duration, Pacing pacing, Density density)
    {
        // Base words per minute based on pacing
        int wpm = pacing switch
        {
            Pacing.Chill => 130,
            Pacing.Conversational => 160,
            Pacing.Fast => 190,
            _ => 160
        };

        // Apply density factor
        double densityFactor = density switch
        {
            Density.Sparse => 0.8,
            Density.Balanced => 1.0,
            Density.Dense => 1.2,
            _ => 1.0
        };

        // Calculate target word count
        return (int)(duration.TotalMinutes * wpm * densityFactor);
    }

    private int DetermineSceneCount(TimeSpan duration)
    {
        // Approximate 1 scene per 30 seconds, with some bounds
        int baseCount = (int)Math.Ceiling(duration.TotalSeconds / 30);
        
        // Ensure a minimum of 3 scenes and maximum of 20 scenes
        return Math.Clamp(baseCount, 3, 20);
    }

    private string GenerateScript(Brief brief, PlanSpec spec, int sceneCount, int wordsPerScene)
    {
        var script = new List<string>();
        
        // Add title
        script.Add($"# {brief.Topic}");
        script.Add("");
        
        // Introduction
        script.Add("## Introduction");
        script.Add(GenerateIntroduction(brief));
        script.Add("");
        
        // Body sections
        for (int i = 1; i <= sceneCount - 2; i++)
        {
            string heading = $"## Section {i}";
            string content = GenerateSection(brief, i, wordsPerScene);
            
            script.Add(heading);
            script.Add(content);
            script.Add("");
        }
        
        // Conclusion
        script.Add("## Conclusion");
        script.Add(GenerateConclusion(brief));
        
        return string.Join("\n", script);
    }

    private string GenerateIntroduction(Brief brief)
    {
        List<string> hooks = new()
        {
            "Have you ever wondered about {0}? It's a fascinating topic that deserves exploration.",
            "In today's video, we're diving deep into {0}. This is something that affects many people.",
            "{0} has become increasingly important in recent years. Let's explore why.",
            "Welcome back! Today we're looking at {0}, a subject that's been requested by many viewers."
        };

        List<string> promises = new()
        {
            "By the end of this video, you'll understand the key aspects of this topic and how it can benefit you.",
            "We'll cover everything you need to know, from the basics to more advanced concepts.",
            "I'm going to break this down into simple, actionable steps that anyone can follow.",
            "We'll explore the what, why, and how so you'll have a complete understanding."
        };

        string hook = hooks[_random.Next(hooks.Count)];
        string promise = promises[_random.Next(promises.Count)];
        
        return string.Format(hook, brief.Topic) + " " + promise;
    }

    private string GenerateSection(Brief brief, int sectionNumber, int targetWords)
    {
        List<string> templates = new()
        {
            "One important aspect of {0} is worth highlighting. This relates to how it functions in everyday scenarios.",
            "When we look closely at {0}, we can see several key factors at play. These elements work together to create the overall effect.",
            "Let's examine {0} from a different perspective. This approach reveals insights that might otherwise be missed.",
            "The history of {0} provides valuable context for our discussion. Understanding its development helps explain its current state."
        };

        List<string> midSentences = new()
        {
            "This is particularly relevant when considering the broader implications.",
            "Many experts in the field have noted this pattern over time.",
            "Research has consistently shown this to be the case across different contexts.",
            "It's worth taking a moment to appreciate the complexity here."
        };

        List<string> closingSentences = new()
        {
            "This points us toward the next important consideration.",
            "With that understanding, we can now move to the next topic.",
            "These insights form a foundation for what comes next.",
            "Keeping these points in mind will help as we continue our exploration."
        };

        string template = templates[_random.Next(templates.Count)];
        string midSentence = midSentences[_random.Next(midSentences.Count)];
        string closingSentence = closingSentences[_random.Next(closingSentences.Count)];
        
        string baseContent = string.Format(template, brief.Topic) + " " + midSentence + " " + closingSentence;
        
        // Expand content to reach target word count if needed
        while (CountWords(baseContent) < targetWords)
        {
            string filler = midSentences[_random.Next(midSentences.Count)];
            baseContent += " " + filler;
        }
        
        return baseContent;
    }

    private string GenerateConclusion(Brief brief)
    {
        List<string> conclusions = new()
        {
            "In conclusion, {0} represents an important area that continues to evolve. The concepts we've covered today should give you a solid foundation.",
            "To summarize what we've learned about {0}: it's a multifaceted topic with several key considerations that we've explored together.",
            "As we wrap up our discussion on {0}, remember the main points we've covered and consider how they might apply to your situation.",
            "That brings us to the end of our exploration of {0}. I hope you've gained some valuable insights that you can apply going forward."
        };

        List<string> callsToAction = new()
        {
            "If you found this video helpful, please like and subscribe for more content like this. Let me know in the comments what topics you'd like to see next!",
            "Don't forget to subscribe and hit the notification bell so you never miss an upload. Share this video with someone who might find it useful!",
            "Thanks for watching! If you have questions, leave them in the comments below, and I'll do my best to answer them in a future video.",
            "Remember to like, share, and subscribe for more content. Your support helps the channel grow and allows me to create more videos like this one."
        };

        string conclusion = conclusions[_random.Next(conclusions.Count)];
        string cta = callsToAction[_random.Next(callsToAction.Count)];
        
        return string.Format(conclusion, brief.Topic) + " " + cta;
    }

    private int CountWords(string text)
    {
        return text.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    public Task<SceneAnalysisResult?> AnalyzeSceneImportanceAsync(
        string sceneText,
        string? previousSceneText,
        string videoGoal,
        CancellationToken ct)
    {
        _logger.LogInformation("Analyzing scene with rule-based heuristics");
        
        // Implement deterministic analysis based on heuristics
        var wordCount = sceneText.Split(new[] { ' ', '\t', '\n', '\r' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;

        // Heuristic complexity based on word count
        double complexity = wordCount switch
        {
            < 30 => 30.0,
            < 70 => 50.0,
            < 120 => 70.0,
            _ => 85.0
        };

        // Heuristic importance - default moderate importance
        // Can be enhanced with keyword detection
        double importance = 50.0;
        var importantKeywords = new[] { "introduction", "conclusion", "summary", "key", "important", "critical" };
        var importantCount = importantKeywords.Count(keyword => 
            sceneText.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        importance += Math.Min(importantCount * 10, 30);

        // Heuristic emotional intensity
        double emotionalIntensity = 50.0;
        var emotionalWords = new[] { "amazing", "incredible", "important", "critical", "exciting", 
                                      "fantastic", "wonderful", "terrible", "devastating", "shocking" };
        var emotionalCount = emotionalWords.Count(word => 
            sceneText.Contains(word, StringComparison.OrdinalIgnoreCase));
        emotionalIntensity += Math.Min(emotionalCount * 10, 30);

        // Information density based on word count
        string informationDensity = wordCount switch
        {
            < 50 => "low",
            < 100 => "medium",
            _ => "high"
        };

        // Optimal duration: ~2.5 words per second (conversational pace)
        double optimalDuration = Math.Max(5.0, wordCount / 2.5);

        // Default transition type based on content
        string transitionType = "cut";
        if (sceneText.Contains("meanwhile", StringComparison.OrdinalIgnoreCase) ||
            sceneText.Contains("later", StringComparison.OrdinalIgnoreCase))
        {
            transitionType = "fade";
        }
        else if (sceneText.Contains("gradually", StringComparison.OrdinalIgnoreCase) ||
                 sceneText.Contains("slowly", StringComparison.OrdinalIgnoreCase))
        {
            transitionType = "dissolve";
        }

        var result = new SceneAnalysisResult(
            Importance: Math.Clamp(importance, 0, 100),
            Complexity: Math.Clamp(complexity, 0, 100),
            EmotionalIntensity: Math.Clamp(emotionalIntensity, 0, 100),
            InformationDensity: informationDensity,
            OptimalDurationSeconds: optimalDuration,
            TransitionType: transitionType,
            Reasoning: "Rule-based heuristic analysis using word count, keyword detection, and content patterns"
        );

        _logger.LogDebug("Scene analyzed: Importance={Importance}, Complexity={Complexity}, Duration={Duration}s",
            result.Importance, result.Complexity, result.OptimalDurationSeconds);

        return Task.FromResult<SceneAnalysisResult?>(result);
    }
}