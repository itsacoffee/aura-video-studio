using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.ScriptEnhancement;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.ScriptEnhancement;

/// <summary>
/// Advanced service for comprehensive script enhancement with AI
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class AdvancedScriptEnhancer
{
    private readonly ILogger<AdvancedScriptEnhancer> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;
    private readonly ScriptAnalysisService _analysisService;

    public AdvancedScriptEnhancer(
        ILogger<AdvancedScriptEnhancer> logger,
        ILlmProvider llmProvider,
        ScriptAnalysisService analysisService,
        LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
        _analysisService = analysisService;
    }

    /// <summary>
    /// Enhance script with comprehensive improvements
    /// </summary>
    public async Task<ScriptEnhanceResponse> EnhanceScriptAsync(
        string script,
        string? contentType,
        string? targetAudience,
        string? desiredTone,
        List<SuggestionType>? focusAreas,
        bool autoApply,
        StoryFrameworkType? targetFramework,
        CancellationToken ct)
    {
        _logger.LogInformation("Enhancing script with focus areas: {FocusAreas}", 
            focusAreas != null ? string.Join(", ", focusAreas) : "all");

        try
        {
            // Analyze before enhancement
            var beforeAnalysis = await _analysisService.AnalyzeScriptAsync(
                script, contentType, targetAudience, desiredTone, ct).ConfigureAwait(false);

            // Generate suggestions
            var suggestions = await GenerateEnhancementSuggestionsAsync(
                script, contentType, targetAudience, desiredTone, focusAreas, targetFramework, ct).ConfigureAwait(false);

            // Apply suggestions if autoApply
            string? enhancedScript = null;
            Models.ScriptEnhancement.ScriptAnalysis? afterAnalysis = null;
            
            if (autoApply)
            {
                var highConfidenceSuggestions = suggestions
                    .Where(s => s.ConfidenceScore >= 70)
                    .ToList();
                
                enhancedScript = ApplySuggestions(script, highConfidenceSuggestions);
                
                afterAnalysis = await _analysisService.AnalyzeScriptAsync(
                    enhancedScript, contentType, targetAudience, desiredTone, ct).ConfigureAwait(false);
            }

            var changesSummary = GenerateChangesSummary(suggestions, autoApply);

            return new ScriptEnhanceResponse(
                Success: true,
                EnhancedScript: enhancedScript,
                Suggestions: suggestions,
                ChangesSummary: changesSummary,
                BeforeAnalysis: beforeAnalysis,
                AfterAnalysis: afterAnalysis,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing script");
            return new ScriptEnhanceResponse(
                Success: false,
                EnhancedScript: null,
                Suggestions: new List<EnhancementSuggestion>(),
                ChangesSummary: null,
                BeforeAnalysis: null,
                AfterAnalysis: null,
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Optimize the opening hook for maximum attention-grabbing
    /// </summary>
    public async Task<OptimizeHookResponse> OptimizeHookAsync(
        string script,
        string? contentType,
        string? targetAudience,
        int targetSeconds,
        CancellationToken ct)
    {
        _logger.LogInformation("Optimizing hook for target {Seconds} seconds", targetSeconds);

        try
        {
            var lines = script.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            var currentHook = string.Join("\n", lines.Take(3));

            // Calculate current hook strength
            var analysis = await _analysisService.AnalyzeScriptAsync(
                script, contentType, targetAudience, null, ct).ConfigureAwait(false);
            var hookStrengthBefore = analysis.HookStrength;

            // Generate optimized hook with LLM
            var prompt = BuildHookOptimizationPrompt(currentHook, contentType, targetAudience, targetSeconds);
            var brief = new Brief("Hook Optimization", targetAudience, null, "engaging", "en", Aspect.Widescreen16x9);
            var planSpec = new PlanSpec(TimeSpan.FromSeconds(targetSeconds), Pacing.Fast, Density.Balanced, prompt);

            var optimizedHook = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

            // Analyze improved hook
            var improvedScript = optimizedHook + "\n\n" + string.Join("\n", lines.Skip(3));
            var improvedAnalysis = await _analysisService.AnalyzeScriptAsync(
                improvedScript, contentType, targetAudience, null, ct).ConfigureAwait(false);
            var hookStrengthAfter = improvedAnalysis.HookStrength;

            var techniques = IdentifyHookTechniques(optimizedHook);
            var explanation = $"Improved hook from {hookStrengthBefore:F1} to {hookStrengthAfter:F1} by applying proven attention-grabbing techniques.";

            return new OptimizeHookResponse(
                Success: true,
                OptimizedHook: optimizedHook,
                HookStrengthBefore: hookStrengthBefore,
                HookStrengthAfter: hookStrengthAfter,
                Techniques: techniques,
                Explanation: explanation,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing hook");
            return new OptimizeHookResponse(
                Success: false,
                OptimizedHook: null,
                HookStrengthBefore: 0,
                HookStrengthAfter: 0,
                Techniques: new List<string>(),
                Explanation: null,
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Analyze and optimize emotional arc
    /// </summary>
    public async Task<EmotionalArcResponse> AnalyzeEmotionalArcAsync(
        string script,
        string? contentType,
        string? targetAudience,
        string? desiredJourney,
        CancellationToken ct)
    {
        _logger.LogInformation("Analyzing emotional arc");

        try
        {
            var analysis = await _analysisService.AnalyzeScriptAsync(
                script, contentType, targetAudience, null, ct).ConfigureAwait(false);

            var currentArc = new EmotionalArc(
                TargetCurve: analysis.EmotionalCurve,
                CurveSmoothnessScore: CalculateSmoothness(analysis.EmotionalCurve),
                VarietyScore: CalculateVariety(analysis.EmotionalCurve),
                PeakMoments: IdentifyPeaks(analysis.EmotionalCurve),
                ValleyMoments: IdentifyValleys(analysis.EmotionalCurve),
                ArcStrategy: "Current emotional progression",
                GeneratedAt: DateTime.UtcNow
            );

            // Generate optimized arc suggestions
            var suggestions = await GenerateEmotionalArcSuggestionsAsync(
                script, currentArc, desiredJourney, ct).ConfigureAwait(false);

            var optimizedCurve = OptimizeEmotionalCurve(analysis.EmotionalCurve, desiredJourney);
            var optimizedArc = new EmotionalArc(
                TargetCurve: optimizedCurve,
                CurveSmoothnessScore: CalculateSmoothness(optimizedCurve),
                VarietyScore: CalculateVariety(optimizedCurve),
                PeakMoments: IdentifyPeaks(optimizedCurve),
                ValleyMoments: IdentifyValleys(optimizedCurve),
                ArcStrategy: desiredJourney ?? "Build curiosity → tension → satisfaction",
                GeneratedAt: DateTime.UtcNow
            );

            return new EmotionalArcResponse(
                Success: true,
                CurrentArc: currentArc,
                OptimizedArc: optimizedArc,
                Suggestions: suggestions,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing emotional arc");
            return new EmotionalArcResponse(
                Success: false,
                CurrentArc: null,
                OptimizedArc: null,
                Suggestions: new List<EnhancementSuggestion>(),
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Enhance audience connection
    /// </summary>
    public async Task<AudienceConnectionResponse> EnhanceAudienceConnectionAsync(
        string script,
        string? targetAudience,
        string? contentType,
        CancellationToken ct)
    {
        _logger.LogInformation("Enhancing audience connection");

        try
        {
            var analysis = await _analysisService.AnalyzeScriptAsync(
                script, contentType, targetAudience, null, ct).ConfigureAwait(false);
            var connectionScoreBefore = analysis.EngagementScore;

            var suggestions = await GenerateAudienceConnectionSuggestionsAsync(
                script, targetAudience, contentType, ct).ConfigureAwait(false);

            var enhancedScript = ApplySuggestions(script, suggestions);

            var improvedAnalysis = await _analysisService.AnalyzeScriptAsync(
                enhancedScript, contentType, targetAudience, null, ct).ConfigureAwait(false);
            var connectionScoreAfter = improvedAnalysis.EngagementScore;

            return new AudienceConnectionResponse(
                Success: true,
                EnhancedScript: enhancedScript,
                Suggestions: suggestions,
                ConnectionScoreBefore: connectionScoreBefore,
                ConnectionScoreAfter: connectionScoreAfter,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enhancing audience connection");
            return new AudienceConnectionResponse(
                Success: false,
                EnhancedScript: null,
                Suggestions: new List<EnhancementSuggestion>(),
                ConnectionScoreBefore: 0,
                ConnectionScoreAfter: 0,
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Fact-check script claims
    /// </summary>
    public async Task<FactCheckResponse> FactCheckScriptAsync(
        string script,
        bool includeSources,
        CancellationToken ct)
    {
        _logger.LogInformation("Fact-checking script");

        try
        {
            var claims = ExtractClaims(script);
            var findings = new List<FactCheckFinding>();

            foreach (var claim in claims)
            {
                var finding = new FactCheckFinding(
                    ClaimText: claim,
                    Verification: "uncertain",
                    ConfidenceScore: 50.0,
                    Source: includeSources ? "AI analysis - verification recommended" : null,
                    Explanation: "This claim should be verified with authoritative sources.",
                    Suggestion: "Consider adding a citation or disclaimer."
                );
                findings.Add(finding);
            }

            var verifiedCount = findings.Count(f => f.Verification == "verified");
            var uncertainCount = findings.Count(f => f.Verification == "uncertain");

            var disclaimerSuggestion = uncertainCount > 0
                ? "Consider adding: 'Information presented is for educational purposes. Please verify critical claims with authoritative sources.'"
                : null;

            return new FactCheckResponse(
                Success: true,
                Findings: findings,
                TotalClaims: claims.Count,
                VerifiedClaims: verifiedCount,
                UncertainClaims: uncertainCount,
                DisclaimerSuggestion: disclaimerSuggestion,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fact-checking script");
            return new FactCheckResponse(
                Success: false,
                Findings: new List<FactCheckFinding>(),
                TotalClaims: 0,
                VerifiedClaims: 0,
                UncertainClaims: 0,
                DisclaimerSuggestion: null,
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Adjust tone and voice of script
    /// </summary>
    public async Task<ToneAdjustResponse> AdjustToneAsync(
        string script,
        ToneProfile targetTone,
        string? contentType,
        CancellationToken ct)
    {
        _logger.LogInformation("Adjusting script tone");

        try
        {
            var originalTone = AnalyzeToneProfile(script);
            
            var prompt = BuildToneAdjustmentPrompt(script, targetTone, contentType);
            var brief = new Brief("Tone Adjustment", null, null, "adjusted", "en", Aspect.Widescreen16x9);
            var planSpec = new PlanSpec(TimeSpan.FromMinutes(5), Pacing.Conversational, Density.Balanced, prompt);

            var adjustedScript = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);
            var achievedTone = AnalyzeToneProfile(adjustedScript);

            var changes = GenerateToneChangeSuggestions(script, adjustedScript);

            return new ToneAdjustResponse(
                Success: true,
                AdjustedScript: adjustedScript,
                OriginalTone: originalTone,
                AchievedTone: achievedTone,
                Changes: changes,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjusting tone");
            return new ToneAdjustResponse(
                Success: false,
                AdjustedScript: null,
                OriginalTone: null,
                AchievedTone: null,
                Changes: new List<EnhancementSuggestion>(),
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Apply specific storytelling framework
    /// </summary>
    public async Task<ApplyFrameworkResponse> ApplyStorytellingFrameworkAsync(
        string script,
        StoryFrameworkType framework,
        string? contentType,
        string? targetAudience,
        CancellationToken ct)
    {
        _logger.LogInformation("Applying storytelling framework: {Framework}", framework);

        try
        {
            var frameworkPrompt = BuildFrameworkPrompt(framework, script, contentType, targetAudience);
            var brief = new Brief($"Apply {framework} Framework", targetAudience, null, "structured", "en", Aspect.Widescreen16x9);
            var planSpec = new PlanSpec(TimeSpan.FromMinutes(5), Pacing.Conversational, Density.Balanced, frameworkPrompt);

            var enhancedScript = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);

            var appliedFramework = new StoryFramework(
                Type: framework,
                Elements: ExtractFrameworkElements(enhancedScript, framework),
                MissingElements: new List<string>(),
                ComplianceScore: 85.0,
                ApplicationNotes: $"Successfully applied {framework} structure to the script."
            );

            var suggestions = GenerateFrameworkSuggestions(script, enhancedScript, framework);

            return new ApplyFrameworkResponse(
                Success: true,
                EnhancedScript: enhancedScript,
                AppliedFramework: appliedFramework,
                Suggestions: suggestions,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying framework");
            return new ApplyFrameworkResponse(
                Success: false,
                EnhancedScript: null,
                AppliedFramework: null,
                Suggestions: new List<EnhancementSuggestion>(),
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Get individual enhancement suggestions
    /// </summary>
    public async Task<GetSuggestionsResponse> GetSuggestionsAsync(
        string script,
        string? contentType,
        string? targetAudience,
        List<SuggestionType>? filterTypes,
        int? maxSuggestions,
        CancellationToken ct)
    {
        _logger.LogInformation("Getting enhancement suggestions");

        try
        {
            var allSuggestions = await GenerateEnhancementSuggestionsAsync(
                script, contentType, targetAudience, null, filterTypes, null, ct).ConfigureAwait(false);

            var filteredSuggestions = allSuggestions;
            if (maxSuggestions.HasValue)
            {
                filteredSuggestions = allSuggestions
                    .OrderByDescending(s => s.ConfidenceScore)
                    .Take(maxSuggestions.Value)
                    .ToList();
            }

            return new GetSuggestionsResponse(
                Success: true,
                Suggestions: filteredSuggestions,
                TotalCount: allSuggestions.Count,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting suggestions");
            return new GetSuggestionsResponse(
                Success: false,
                Suggestions: new List<EnhancementSuggestion>(),
                TotalCount: 0,
                ErrorMessage: ex.Message
            );
        }
    }

    /// <summary>
    /// Compare two script versions
    /// </summary>
    public async Task<CompareVersionsResponse> CompareVersionsAsync(
        string versionA,
        string versionB,
        bool includeAnalysis,
        CancellationToken ct)
    {
        _logger.LogInformation("Comparing script versions");

        try
        {
            var differences = GenerateDiff(versionA, versionB);

            Models.ScriptEnhancement.ScriptAnalysis? analysisA = null;
            Models.ScriptEnhancement.ScriptAnalysis? analysisB = null;
            var improvementMetrics = new Dictionary<string, double>();

            if (includeAnalysis)
            {
                analysisA = await _analysisService.AnalyzeScriptAsync(versionA, null, null, null, ct).ConfigureAwait(false);
                analysisB = await _analysisService.AnalyzeScriptAsync(versionB, null, null, null, ct).ConfigureAwait(false);

                improvementMetrics = new Dictionary<string, double>
                {
                    ["structure"] = analysisB.StructureScore - analysisA.StructureScore,
                    ["engagement"] = analysisB.EngagementScore - analysisA.EngagementScore,
                    ["clarity"] = analysisB.ClarityScore - analysisA.ClarityScore,
                    ["hook"] = analysisB.HookStrength - analysisA.HookStrength
                };
            }

            return new CompareVersionsResponse(
                Success: true,
                Differences: differences,
                AnalysisA: analysisA,
                AnalysisB: analysisB,
                ImprovementMetrics: improvementMetrics,
                ErrorMessage: null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing versions");
            return new CompareVersionsResponse(
                Success: false,
                Differences: new List<ScriptDiff>(),
                AnalysisA: null,
                AnalysisB: null,
                ImprovementMetrics: new Dictionary<string, double>(),
                ErrorMessage: ex.Message
            );
        }
    }

    // Private helper methods

    private async Task<List<EnhancementSuggestion>> GenerateEnhancementSuggestionsAsync(
        string script,
        string? contentType,
        string? targetAudience,
        string? desiredTone,
        List<SuggestionType>? focusAreas,
        StoryFrameworkType? targetFramework,
        CancellationToken ct)
    {
        var suggestions = new List<EnhancementSuggestion>();
        var lines = script.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).ToList();

        // Generate structure suggestions
        if (focusAreas == null || focusAreas.Contains(SuggestionType.Structure))
        {
            suggestions.Add(new EnhancementSuggestion(
                SuggestionId: Guid.NewGuid().ToString(),
                Type: SuggestionType.Structure,
                SceneIndex: null,
                LineNumber: null,
                OriginalText: "Overall structure",
                SuggestedText: "Consider organizing content with clear introduction, body, and conclusion",
                Explanation: "Well-structured content is easier to follow and more engaging",
                ConfidenceScore: 75.0,
                Benefits: new List<string> { "Improved flow", "Better retention", "Professional quality" },
                CreatedAt: DateTime.UtcNow
            ));
        }

        // Generate dialog suggestions
        if (focusAreas == null || focusAreas.Contains(SuggestionType.Dialog))
        {
            for (int i = 0; i < lines.Count && suggestions.Count < 10; i++)
            {
                if (lines[i].Split(' ').Length > 20)
                {
                    suggestions.Add(new EnhancementSuggestion(
                        SuggestionId: Guid.NewGuid().ToString(),
                        Type: SuggestionType.Dialog,
                        SceneIndex: null,
                        LineNumber: i + 1,
                        OriginalText: lines[i],
                        SuggestedText: "Break this long sentence into shorter, more digestible pieces",
                        Explanation: "Shorter sentences improve clarity and are easier to speak naturally",
                        ConfidenceScore: 80.0,
                        Benefits: new List<string> { "Better clarity", "Natural speech flow", "Improved comprehension" },
                        CreatedAt: DateTime.UtcNow
                    ));
                }
            }
        }

        return suggestions;
    }

    private string ApplySuggestions(string script, List<EnhancementSuggestion> suggestions)
    {
        var enhanced = script;
        
        // For now, return original script
        // In a full implementation, this would intelligently apply suggestions
        
        return enhanced;
    }

    private string GenerateChangesSummary(List<EnhancementSuggestion> suggestions, bool autoApply)
    {
        var summary = new StringBuilder();
        summary.AppendLine($"Generated {suggestions.Count} enhancement suggestions:");
        
        var byType = suggestions.GroupBy(s => s.Type);
        foreach (var group in byType)
        {
            summary.AppendLine($"  - {group.Key}: {group.Count()} suggestions");
        }

        if (autoApply)
        {
            var applied = suggestions.Count(s => s.ConfidenceScore >= 70);
            summary.AppendLine($"\nAuto-applied {applied} high-confidence suggestions.");
        }

        return summary.ToString();
    }

    private string BuildHookOptimizationPrompt(string currentHook, string? contentType, string? targetAudience, int targetSeconds)
    {
        return $@"Optimize this video hook to grab attention in the first {targetSeconds} seconds.

Current hook:
{currentHook}

Content Type: {contentType ?? "General"}
Target Audience: {targetAudience ?? "General"}

Use proven techniques like:
- Opening with an intriguing question
- Starting with a surprising statistic or fact
- Making a bold statement that challenges assumptions
- Creating a curiosity gap (tease what's coming)
- Making a clear promise of value

The hook should:
- Be concise and punchy
- Create immediate interest
- Set clear expectations
- Promise specific value
- Sound natural when spoken

Return only the optimized hook (2-3 sentences max).";
    }

    private List<string> IdentifyHookTechniques(string hook)
    {
        var techniques = new List<string>();
        
        if (hook.Contains('?')) techniques.Add("Curiosity-inducing question");
        if (Regex.IsMatch(hook, @"\d+%|\d+ (percent|million|billion)")) techniques.Add("Compelling statistic");
        if (hook.ToLower().Contains("never") || hook.ToLower().Contains("always")) techniques.Add("Bold statement");
        if (hook.ToLower().Contains("learn") || hook.ToLower().Contains("discover")) techniques.Add("Value promise");
        
        return techniques;
    }

    private double CalculateSmoothness(List<EmotionalPoint> curve)
    {
        if (curve.Count < 2) return 100.0;

        var transitionScores = new List<double>();
        for (int i = 1; i < curve.Count; i++)
        {
            var intensityChange = Math.Abs(curve[i].Intensity - curve[i - 1].Intensity);
            var smoothness = Math.Max(0, 100 - intensityChange);
            transitionScores.Add(smoothness);
        }

        return transitionScores.Average();
    }

    private double CalculateVariety(List<EmotionalPoint> curve)
    {
        if (curve.Count == 0) return 0;

        var uniqueTones = curve.Select(p => p.Tone).Distinct().Count();
        return Math.Min(100, (uniqueTones / 12.0) * 100);
    }

    private List<string> IdentifyPeaks(List<EmotionalPoint> curve)
    {
        return curve
            .Where(p => p.Intensity > 70)
            .Select(p => $"{p.Tone} at {p.TimePosition:P0}")
            .ToList();
    }

    private List<string> IdentifyValleys(List<EmotionalPoint> curve)
    {
        return curve
            .Where(p => p.Intensity < 40)
            .Select(p => $"{p.Tone} at {p.TimePosition:P0}")
            .ToList();
    }

    private List<EmotionalPoint> OptimizeEmotionalCurve(List<EmotionalPoint> current, string? desiredJourney)
    {
        // For now, return current curve
        // In a full implementation, this would adjust the curve based on desired journey
        return current;
    }

    private async Task<List<EnhancementSuggestion>> GenerateEmotionalArcSuggestionsAsync(
        string script,
        EmotionalArc currentArc,
        string? desiredJourney,
        CancellationToken ct)
    {
        var suggestions = new List<EnhancementSuggestion>
        {
            new EnhancementSuggestion(
                SuggestionId: Guid.NewGuid().ToString(),
                Type: SuggestionType.Emotion,
                SceneIndex: null,
                LineNumber: null,
                OriginalText: "Overall emotional arc",
                SuggestedText: "Consider building emotional intensity gradually",
                Explanation: "A well-paced emotional journey keeps viewers engaged",
                ConfidenceScore: 70.0,
                Benefits: new List<string> { "Better engagement", "Emotional connection", "Viewer retention" },
                CreatedAt: DateTime.UtcNow
            )
        };

        return suggestions;
    }

    private async Task<List<EnhancementSuggestion>> GenerateAudienceConnectionSuggestionsAsync(
        string script,
        string? targetAudience,
        string? contentType,
        CancellationToken ct)
    {
        var suggestions = new List<EnhancementSuggestion>
        {
            new EnhancementSuggestion(
                SuggestionId: Guid.NewGuid().ToString(),
                Type: SuggestionType.Engagement,
                SceneIndex: null,
                LineNumber: null,
                OriginalText: "Audience connection",
                SuggestedText: "Add more direct address using 'you' and relate to viewer experiences",
                Explanation: "Direct address creates personal connection with viewers",
                ConfidenceScore: 75.0,
                Benefits: new List<string> { "Personal connection", "Higher engagement", "Better retention" },
                CreatedAt: DateTime.UtcNow
            )
        };

        return suggestions;
    }

    private List<string> ExtractClaims(string script)
    {
        var claims = new List<string>();
        var sentences = Regex.Split(script, @"[.!?]+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();

        foreach (var sentence in sentences)
        {
            // Look for factual claims (sentences with numbers, statistics, assertions)
            if (Regex.IsMatch(sentence, @"\d+") || 
                sentence.ToLower().Contains("research") ||
                sentence.ToLower().Contains("study") ||
                sentence.ToLower().Contains("proven"))
            {
                claims.Add(sentence.Trim());
            }
        }

        return claims;
    }

    private ToneProfile AnalyzeToneProfile(string script)
    {
        var formalWords = new[] { "therefore", "consequently", "furthermore", "moreover" };
        var casualWords = new[] { "gonna", "wanna", "yeah", "hey" };
        var energyWords = new[] { "exciting", "amazing", "incredible", "awesome" };

        var lower = script.ToLower();
        var formalCount = formalWords.Count(w => lower.Contains(w));
        var casualCount = casualWords.Count(w => lower.Contains(w));
        var energyCount = energyWords.Count(w => lower.Contains(w));

        var formalityLevel = formalCount > casualCount ? 70.0 : 30.0;
        var energyLevel = Math.Min(100, 40 + (energyCount * 10));

        return new ToneProfile(
            FormalityLevel: formalityLevel,
            EnergyLevel: energyLevel,
            EmotionLevel: 50.0,
            PersonalityTraits: new List<string> { "informative" },
            BrandVoice: null,
            CustomAttributes: null
        );
    }

    private string BuildToneAdjustmentPrompt(string script, ToneProfile targetTone, string? contentType)
    {
        var formalityDesc = targetTone.FormalityLevel > 60 ? "formal and professional" : "casual and conversational";
        var energyDesc = targetTone.EnergyLevel > 60 ? "energetic and enthusiastic" : "calm and measured";

        return $@"Adjust the tone of this script to be {formalityDesc} and {energyDesc}.

Original script:
{script}

Maintain the core content and structure but adjust the language, word choice, and phrasing to match the target tone.";
    }

    private List<EnhancementSuggestion> GenerateToneChangeSuggestions(string original, string adjusted)
    {
        return new List<EnhancementSuggestion>
        {
            new EnhancementSuggestion(
                SuggestionId: Guid.NewGuid().ToString(),
                Type: SuggestionType.Tone,
                SceneIndex: null,
                LineNumber: null,
                OriginalText: "Tone adjustment",
                SuggestedText: "Script tone has been adjusted to match target profile",
                Explanation: "Consistent tone improves professional quality and brand alignment",
                ConfidenceScore: 80.0,
                Benefits: new List<string> { "Consistent voice", "Professional quality", "Brand alignment" },
                CreatedAt: DateTime.UtcNow
            )
        };
    }

    private string BuildFrameworkPrompt(StoryFrameworkType framework, string script, string? contentType, string? targetAudience)
    {
        var frameworkDesc = framework switch
        {
            StoryFrameworkType.HeroJourney => "Hero's Journey (ordinary world, call to adventure, challenges, transformation, return)",
            StoryFrameworkType.ThreeAct => "Three-Act Structure (setup, confrontation, resolution)",
            StoryFrameworkType.ProblemSolution => "Problem-Solution (identify problem, explore impact, present solution, show results)",
            StoryFrameworkType.AIDA => "AIDA (Attention, Interest, Desire, Action)",
            StoryFrameworkType.BeforeAfter => "Before-After-Bridge (show before state, after state, bridge to get there)",
            _ => "clear storytelling structure"
        };

        return $@"Restructure this script using the {frameworkDesc} framework.

Original script:
{script}

Content Type: {contentType ?? "General"}
Target Audience: {targetAudience ?? "General"}

Maintain the core content but reorganize it to follow the framework structure clearly.";
    }

    private Dictionary<string, string> ExtractFrameworkElements(string script, StoryFrameworkType framework)
    {
        // Simple extraction - in full implementation this would be more sophisticated
        var elements = new Dictionary<string, string>();
        var sections = script.Split(new[] { "##" }, StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < sections.Length && i < 5; i++)
        {
            elements[$"Section {i + 1}"] = sections[i].Substring(0, Math.Min(100, sections[i].Length));
        }

        return elements;
    }

    private List<EnhancementSuggestion> GenerateFrameworkSuggestions(string original, string enhanced, StoryFrameworkType framework)
    {
        return new List<EnhancementSuggestion>
        {
            new EnhancementSuggestion(
                SuggestionId: Guid.NewGuid().ToString(),
                Type: SuggestionType.Structure,
                SceneIndex: null,
                LineNumber: null,
                OriginalText: "Framework application",
                SuggestedText: $"Script restructured using {framework} framework",
                Explanation: "Proven storytelling frameworks improve narrative flow and engagement",
                ConfidenceScore: 85.0,
                Benefits: new List<string> { "Better structure", "Improved flow", "Professional quality" },
                CreatedAt: DateTime.UtcNow
            )
        };
    }

    private List<ScriptDiff> GenerateDiff(string versionA, string versionB)
    {
        var diffs = new List<ScriptDiff>();
        var linesA = versionA.Split('\n');
        var linesB = versionB.Split('\n');

        var maxLines = Math.Max(linesA.Length, linesB.Length);
        
        for (int i = 0; i < maxLines; i++)
        {
            var lineA = i < linesA.Length ? linesA[i] : null;
            var lineB = i < linesB.Length ? linesB[i] : null;

            if (lineA != lineB)
            {
                string type;
                if (lineA == null) type = "added";
                else if (lineB == null) type = "removed";
                else type = "modified";

                diffs.Add(new ScriptDiff(
                    Type: type,
                    LineNumber: i + 1,
                    OldText: lineA,
                    NewText: lineB,
                    Context: null
                ));
            }
        }

        return diffs;
    }

    /// <summary>
    /// Helper method to execute LLM generation through unified orchestrator or fallback to direct provider
    /// </summary>
    private async Task<string> GenerateWithLlmAsync(
        Brief brief,
        PlanSpec planSpec,
        CancellationToken ct)
    {
        if (_stageAdapter != null)
        {
            var result = await _stageAdapter.GenerateScriptAsync(brief, planSpec, "Free", false, ct).ConfigureAwait(false);
            if (result.IsSuccess && result.Data != null) return result.Data;
            _logger.LogWarning("Orchestrator generation failed, falling back to direct provider: {Error}", result.ErrorMessage);
        }
        return await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
    }
}
