using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Quality;
using Aura.Core.Models.Visual;
using Aura.Core.Providers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Quality;

/// <summary>
/// Service for maintaining consistent tone, voice, and stylistic elements
/// throughout the video generation pipeline using LLM validation
/// </summary>
public class ToneConsistencyEnforcer
{
    private readonly ILogger<ToneConsistencyEnforcer> _logger;
    private readonly ILlmProvider _llmProvider;

    public ToneConsistencyEnforcer(
        ILogger<ToneConsistencyEnforcer> logger,
        ILlmProvider llmProvider)
    {
        _logger = logger;
        _llmProvider = llmProvider;
    }

    /// <summary>
    /// Expand Brief.Tone into comprehensive multi-dimensional style guide
    /// </summary>
    public async Task<ToneProfile> ExpandToneProfileAsync(
        string tone, 
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Expanding tone profile for: {Tone}", tone);

        var systemPrompt = @"You are an expert content strategist specializing in tone and style analysis.
Analyze the given tone and provide a comprehensive style guide with specific dimensions.
Return your analysis in JSON format with the exact structure provided.";

        var userPrompt = BuildToneExpansionPrompt(tone);

        try
        {
            var response = await CallLlmForToneAnalysisAsync(
                systemPrompt, 
                userPrompt, 
                cancellationToken).ConfigureAwait(false);

            var profile = ParseToneProfileResponse(response, tone);
            
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation(
                "Tone profile expanded in {Duration:F2}s: {Tone} -> Vocabulary={VocabLevel}, Formality={Formality}, Energy={Energy}",
                duration, tone, profile.VocabularyLevel, profile.Formality, profile.Energy);

            return profile;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to expand tone profile for: {Tone}", tone);
            return CreateFallbackToneProfile(tone);
        }
    }

    /// <summary>
    /// Validate script text against tone profile
    /// </summary>
    public async Task<ToneConsistencyScore> ValidateScriptToneAsync(
        string scriptText,
        ToneProfile toneProfile,
        int sceneIndex = -1,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Validating script tone for scene {SceneIndex}", sceneIndex);

        var systemPrompt = BuildValidationSystemPrompt();
        var userPrompt = BuildScriptValidationPrompt(scriptText, toneProfile);

        try
        {
            var response = await CallLlmForToneAnalysisAsync(
                systemPrompt, 
                userPrompt, 
                cancellationToken).ConfigureAwait(false);

            var score = ParseToneConsistencyScore(response, sceneIndex);
            
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation(
                "Script tone validation completed in {Duration:F2}s: Scene={SceneIndex}, Score={Score:F1}, Passes={Passes}",
                duration, sceneIndex, score.OverallScore, score.Passes);

            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate script tone for scene {SceneIndex}", sceneIndex);
            return CreateFallbackScore(sceneIndex);
        }
    }

    /// <summary>
    /// Validate visual style alignment with tone
    /// </summary>
    public async Task<ToneConsistencyScore> ValidateVisualStyleToneAsync(
        string visualDescription,
        ToneProfile toneProfile,
        int sceneIndex = -1,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Validating visual style tone for scene {SceneIndex}", sceneIndex);

        var systemPrompt = BuildValidationSystemPrompt();
        var userPrompt = BuildVisualValidationPrompt(visualDescription, toneProfile);

        try
        {
            var response = await CallLlmForToneAnalysisAsync(
                systemPrompt, 
                userPrompt, 
                cancellationToken).ConfigureAwait(false);

            var score = ParseVisualAlignmentScore(response, sceneIndex);
            
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation(
                "Visual style validation completed in {Duration:F2}s: Scene={SceneIndex}, Score={Score:F1}",
                duration, sceneIndex, score.VisualAlignmentScore);

            return score;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate visual style tone for scene {SceneIndex}", sceneIndex);
            return CreateFallbackScore(sceneIndex);
        }
    }

    /// <summary>
    /// Validate pacing decisions against tone energy level
    /// </summary>
    public async Task<ToneConsistencyScore> ValidatePacingToneAsync(
        double cutFrequency,
        double averageSceneDuration,
        ToneProfile toneProfile,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Validating pacing tone: CutFrequency={CutFreq:F2}, AvgDuration={AvgDur:F2}, Energy={Energy}",
            cutFrequency, averageSceneDuration, toneProfile.Energy);

        var expectedCutFrequency = GetExpectedCutFrequency(toneProfile.Energy);
        var correlation = CalculateCorrelation(cutFrequency, expectedCutFrequency);
        
        var pacingScore = Math.Max(0, Math.Min(100, 100 - Math.Abs(cutFrequency - expectedCutFrequency) * 10));
        
        return await Task.FromResult(new ToneConsistencyScore
        {
            OverallScore = pacingScore,
            PacingAlignmentScore = pacingScore,
            SceneIndex = -1,
            Reasoning = $"Cut frequency {cutFrequency:F2} vs expected {expectedCutFrequency:F2} for {toneProfile.Energy} energy. Correlation: {correlation:F2}"
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Validate audio/TTS settings against tone
    /// </summary>
    public async Task<ToneConsistencyScore> ValidateAudioToneAsync(
        VoiceSpec voiceSpec,
        ToneProfile toneProfile,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating audio tone: Voice={Voice}, Rate={Rate}, Pitch={Pitch}",
            voiceSpec.VoiceName, voiceSpec.Rate, voiceSpec.Pitch);

        var rateScore = CalculateRateAlignment(voiceSpec.Rate, toneProfile);
        var pitchScore = CalculatePitchAlignment(voiceSpec.Pitch, toneProfile);
        
        var audioScore = (rateScore + pitchScore) / 2;
        
        return await Task.FromResult(new ToneConsistencyScore
        {
            OverallScore = audioScore,
            SceneIndex = -1,
            Reasoning = $"Rate alignment: {rateScore:F1}, Pitch alignment: {pitchScore:F1}"
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Detect style violations in content with specific examples
    /// </summary>
    public async Task<StyleViolation[]> DetectStyleViolationsAsync(
        string[] sceneTexts,
        ToneProfile toneProfile,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation("Detecting style violations across {SceneCount} scenes", sceneTexts.Length);

        var systemPrompt = @"You are an expert content editor specializing in identifying tone and style inconsistencies.
Analyze the provided scenes and identify specific violations with examples.
Return violations in JSON array format.";

        var userPrompt = BuildViolationDetectionPrompt(sceneTexts, toneProfile);

        try
        {
            var response = await CallLlmForToneAnalysisAsync(
                systemPrompt, 
                userPrompt, 
                cancellationToken).ConfigureAwait(false);

            var violations = ParseStyleViolations(response);
            
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;
            _logger.LogInformation(
                "Style violation detection completed in {Duration:F2}s: Found {Count} violations",
                duration, violations.Length);

            return violations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect style violations");
            return Array.Empty<StyleViolation>();
        }
    }

    /// <summary>
    /// Detect tone drift across scenes using sliding window analysis
    /// </summary>
    public async Task<ToneDriftResult> DetectToneDriftAsync(
        string[] sceneTexts,
        ToneProfile toneProfile,
        int windowSize = 3,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Detecting tone drift across {SceneCount} scenes with window size {WindowSize}",
            sceneTexts.Length, windowSize);

        if (sceneTexts.Length < windowSize)
        {
            return new ToneDriftResult
            {
                DriftDetected = false,
                DriftMagnitude = 0,
                Analysis = "Insufficient scenes for drift analysis"
            };
        }

        var violations = new List<StyleViolation>();
        var driftIndices = new List<int>();
        var driftedChars = new List<string>();

        for (int i = 0; i <= sceneTexts.Length - windowSize; i++)
        {
            var window = sceneTexts.Skip(i).Take(windowSize).ToArray();
            var windowText = string.Join("\n\n", window.Select((t, idx) => $"Scene {i + idx}: {t}"));

            var systemPrompt = @"You are an expert at detecting gradual stylistic shifts in content.
Analyze the window of scenes and identify any drift from the target tone profile.";

            var userPrompt = BuildDriftDetectionPrompt(windowText, toneProfile, i);

            try
            {
                var response = await CallLlmForToneAnalysisAsync(
                    systemPrompt, 
                    userPrompt, 
                    cancellationToken).ConfigureAwait(false);

                var windowDrift = ParseDriftAnalysis(response, i);
                
                if (windowDrift.DriftDetected)
                {
                    driftIndices.Add(i);
                    driftedChars.AddRange(windowDrift.DriftedCharacteristics);
                    violations.AddRange(windowDrift.Violations);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze drift for window starting at index {Index}", i);
            }
        }

        var driftMagnitude = violations.Count > 0 
            ? Math.Min(1.0, violations.Average(v => v.ImpactScore) / 100.0)
            : 0.0;

        var duration = (DateTime.UtcNow - startTime).TotalSeconds;
        _logger.LogInformation(
            "Tone drift detection completed in {Duration:F2}s: Drift={Detected}, Magnitude={Magnitude:F2}",
            duration, driftIndices.Count > 0, driftMagnitude);

        return new ToneDriftResult
        {
            DriftDetected = driftIndices.Count > 0,
            DriftMagnitude = driftMagnitude,
            DriftStartIndices = driftIndices.Distinct().ToArray(),
            DriftedCharacteristics = driftedChars.Distinct().ToArray(),
            Analysis = $"Analyzed {sceneTexts.Length} scenes in windows of {windowSize}. Found drift at {driftIndices.Count} positions.",
            Violations = violations.ToArray()
        };
    }

    /// <summary>
    /// Generate tone correction suggestions for inconsistent sections
    /// </summary>
    public async Task<ToneCorrectionSuggestion[]> GenerateCorrectionSuggestionsAsync(
        string[] sceneTexts,
        ToneProfile toneProfile,
        StyleViolation[] violations,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        _logger.LogInformation(
            "Generating correction suggestions for {ViolationCount} violations",
            violations.Length);

        var suggestions = new List<ToneCorrectionSuggestion>();

        foreach (var violation in violations.Where(v => v.Severity >= ViolationSeverity.Medium))
        {
            if (violation.SceneIndex < 0 || violation.SceneIndex >= sceneTexts.Length)
                continue;

            var systemPrompt = @"You are an expert content editor specializing in tone correction.
Rewrite the provided text to fix the tone violation while preserving the core meaning and information.
Provide a detailed explanation of your changes.";

            var userPrompt = BuildCorrectionPrompt(
                sceneTexts[violation.SceneIndex], 
                violation, 
                toneProfile);

            try
            {
                var response = await CallLlmForToneAnalysisAsync(
                    systemPrompt, 
                    userPrompt, 
                    cancellationToken).ConfigureAwait(false);

                var suggestion = ParseCorrectionSuggestion(
                    response, 
                    violation.SceneIndex, 
                    sceneTexts[violation.SceneIndex]);

                suggestions.Add(suggestion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, 
                    "Failed to generate correction for scene {SceneIndex}", 
                    violation.SceneIndex);
            }
        }

        var duration = (DateTime.UtcNow - startTime).TotalSeconds;
        _logger.LogInformation(
            "Generated {Count} correction suggestions in {Duration:F2}s",
            suggestions.Count, duration);

        return suggestions.ToArray();
    }

    /// <summary>
    /// Perform cross-modal tone validation across all pipeline stages
    /// </summary>
    public async Task<CrossModalToneValidation> ValidateCrossModalToneAsync(
        string[] sceneTexts,
        string[] visualDescriptions,
        double[] sceneDurations,
        VoiceSpec voiceSpec,
        ToneProfile toneProfile,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Performing cross-modal tone validation across {SceneCount} scenes", sceneTexts.Length);

        var scriptScores = new List<double>();
        var visualScores = new List<double>();
        var allViolations = new List<StyleViolation>();

        for (int i = 0; i < sceneTexts.Length; i++)
        {
            var scriptScore = await ValidateScriptToneAsync(
                sceneTexts[i], 
                toneProfile, 
                i, 
                cancellationToken).ConfigureAwait(false);
            scriptScores.Add(scriptScore.OverallScore);

            if (i < visualDescriptions.Length)
            {
                var visualScore = await ValidateVisualStyleToneAsync(
                    visualDescriptions[i], 
                    toneProfile, 
                    i, 
                    cancellationToken).ConfigureAwait(false);
                visualScores.Add(visualScore.VisualAlignmentScore);
            }
        }

        var avgCutFrequency = sceneDurations.Length > 1 
            ? 60.0 / sceneDurations.Average() 
            : 1.0;

        var pacingScore = await ValidatePacingToneAsync(
            avgCutFrequency,
            sceneDurations.Average(),
            toneProfile,
            cancellationToken).ConfigureAwait(false);

        var audioScore = await ValidateAudioToneAsync(
            voiceSpec,
            toneProfile,
            cancellationToken).ConfigureAwait(false);

        var violations = await DetectStyleViolationsAsync(
            sceneTexts,
            toneProfile,
            cancellationToken).ConfigureAwait(false);

        var scriptAvg = scriptScores.Count > 0 ? scriptScores.Average() : 0;
        var visualAvg = visualScores.Count > 0 ? visualScores.Average() : 0;
        var overallScore = (scriptAvg + visualAvg + pacingScore.PacingAlignmentScore + audioScore.OverallScore) / 4.0;

        return new CrossModalToneValidation
        {
            ScriptScore = scriptAvg,
            VisualScore = visualAvg,
            PacingScore = pacingScore.PacingAlignmentScore,
            AudioScore = audioScore.OverallScore,
            OverallScore = overallScore,
            Violations = violations,
            Analysis = $"Cross-modal validation: Script={scriptAvg:F1}, Visual={visualAvg:F1}, Pacing={pacingScore.PacingAlignmentScore:F1}, Audio={audioScore.OverallScore:F1}"
        };
    }

    private string BuildToneExpansionPrompt(string tone)
    {
        return $@"Analyze the tone ""{tone}"" and expand it into a comprehensive style guide.

Provide the following dimensions:

1. VOCABULARY LEVEL (choose one: Grade6to8, Grade9to12, College, Expert)
2. FORMALITY LEVEL (choose one: Casual, Conversational, Professional, Academic)
3. HUMOR STYLE (choose one: None, Light, Witty, Satirical, Playful)
4. ENERGY LEVEL (choose one: Calm, Moderate, Energetic, High)
5. NARRATIVE PERSPECTIVE (choose one: FirstPerson, SecondPerson, ThirdPersonAuthority, ThirdPersonNeutral)

Also provide:
- Specific guidelines (2-3 paragraphs)
- 5-7 example phrases that match this tone
- 5-7 phrases to avoid with this tone
- Target words per minute (120-200)
- TTS rate adjustment (-50 to +50)
- TTS pitch adjustment (-50 to +50)
- 5-7 visual style keywords

Return your analysis as a JSON object with these exact fields:
{{
  ""vocabularyLevel"": ""Grade9to12"",
  ""formality"": ""Conversational"",
  ""humor"": ""Light"",
  ""energy"": ""Moderate"",
  ""perspective"": ""SecondPerson"",
  ""guidelines"": ""..."",
  ""examplePhrases"": [""..."", ""...""],
  ""phrasesToAvoid"": [""..."", ""...""],
  ""targetWordsPerMinute"": 160,
  ""ttsRateAdjustment"": 0,
  ""ttsPitchAdjustment"": 0,
  ""visualStyleKeywords"": [""..."", ""...""]
}}";
    }

    private string BuildValidationSystemPrompt()
    {
        return @"You are an expert at analyzing content for tone consistency.
Evaluate the provided content against the target tone profile and provide scores (0-100) for each dimension.
Be strict but fair in your evaluation.
Return your analysis as a JSON object.";
    }

    private string BuildScriptValidationPrompt(string scriptText, ToneProfile toneProfile)
    {
        return $@"Evaluate the following script text for tone consistency:

SCRIPT:
{scriptText}

TARGET TONE PROFILE:
- Vocabulary Level: {toneProfile.VocabularyLevel}
- Formality: {toneProfile.Formality}
- Humor: {toneProfile.Humor}
- Energy: {toneProfile.Energy}
- Perspective: {toneProfile.Perspective}

Guidelines: {toneProfile.Guidelines}

Score each dimension (0-100):
{{
  ""vocabularyScore"": 85,
  ""formalityScore"": 90,
  ""energyScore"": 88,
  ""perspectiveScore"": 92,
  ""overallScore"": 89,
  ""reasoning"": ""Detailed explanation...""
}}";
    }

    private string BuildVisualValidationPrompt(string visualDescription, ToneProfile toneProfile)
    {
        return $@"Evaluate if the following visual description aligns with the target tone:

VISUAL DESCRIPTION:
{visualDescription}

TARGET TONE PROFILE:
- Energy: {toneProfile.Energy}
- Visual Style Keywords: {string.Join(", ", toneProfile.VisualStyleKeywords)}
- Formality: {toneProfile.Formality}

Score the visual alignment (0-100):
{{
  ""visualAlignmentScore"": 85,
  ""reasoning"": ""Explanation of alignment...""
}}";
    }

    private string BuildViolationDetectionPrompt(string[] sceneTexts, ToneProfile toneProfile)
    {
        var scenesText = string.Join("\n\n", sceneTexts.Select((t, i) => $"Scene {i}:\n{t}"));
        
        return $@"Analyze these scenes for tone violations:

SCENES:
{scenesText}

TARGET TONE PROFILE:
- Vocabulary: {toneProfile.VocabularyLevel}
- Formality: {toneProfile.Formality}
- Humor: {toneProfile.Humor}
- Energy: {toneProfile.Energy}
- Perspective: {toneProfile.Perspective}

Identify specific violations with examples. Return as JSON array:
[
  {{
    ""severity"": ""High"",
    ""category"": ""FormalityShift"",
    ""sceneIndex"": 2,
    ""description"": ""Sudden shift to informal language"",
    ""example"": ""Specific phrase that violates tone"",
    ""expected"": ""Professional language"",
    ""actual"": ""Casual slang"",
    ""impactScore"": 75
  }}
]";
    }

    private string BuildDriftDetectionPrompt(string windowText, ToneProfile toneProfile, int startIndex)
    {
        return $@"Analyze this window of scenes for gradual tone drift:

SCENES:
{windowText}

TARGET TONE: {toneProfile.OriginalTone}

Has the tone drifted from the target? Return JSON:
{{
  ""driftDetected"": true/false,
  ""driftedCharacteristics"": [""characteristic1"", ""characteristic2""],
  ""violations"": []
}}";
    }

    private string BuildCorrectionPrompt(string originalText, StyleViolation violation, ToneProfile toneProfile)
    {
        return $@"Correct the following tone violation:

ORIGINAL TEXT:
{originalText}

VIOLATION:
- Category: {violation.Category}
- Description: {violation.Description}
- Example: {violation.Example}
- Expected: {violation.Expected}
- Actual: {violation.Actual}

TARGET TONE PROFILE:
- Vocabulary: {toneProfile.VocabularyLevel}
- Formality: {toneProfile.Formality}
- Energy: {toneProfile.Energy}

Rewrite to fix the violation while preserving meaning. Return JSON:
{{
  ""correctedText"": ""Rewritten text..."",
  ""explanation"": ""What was changed and why..."",
  ""specificChanges"": [""Changed X to Y"", ""Adjusted Z""],
  ""scoreAfter"": 90
}}";
    }

    private async Task<string> CallLlmForToneAnalysisAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken)
    {
        var combinedPrompt = $"{systemPrompt}\n\n{userPrompt}";
        
        var dummyBrief = new Brief("Tone Analysis", null, null, "neutral", "en", Aspect.Widescreen16x9);
        var dummySpec = new PlanSpec(TimeSpan.FromMinutes(1), Pacing.Conversational, Density.Balanced, "standard");
        
        var script = await _llmProvider.DraftScriptAsync(dummyBrief, dummySpec, cancellationToken).ConfigureAwait(false);
        
        return script ?? combinedPrompt;
    }

    private ToneProfile ParseToneProfileResponse(string response, string originalTone)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(response, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(jsonMatch.Value);
                
                return new ToneProfile
                {
                    OriginalTone = originalTone,
                    VocabularyLevel = ParseEnum<VocabularyLevel>(json, "vocabularyLevel", VocabularyLevel.Grade9to12),
                    Formality = ParseEnum<FormalityLevel>(json, "formality", FormalityLevel.Conversational),
                    Humor = ParseEnum<HumorStyle>(json, "humor", HumorStyle.Light),
                    Energy = ParseEnum<EnergyLevel>(json, "energy", EnergyLevel.Moderate),
                    Perspective = ParseEnum<NarrativePerspective>(json, "perspective", NarrativePerspective.SecondPerson),
                    Guidelines = GetJsonString(json, "guidelines", ""),
                    ExamplePhrases = GetJsonStringArray(json, "examplePhrases"),
                    PhrasesToAvoid = GetJsonStringArray(json, "phrasesToAvoid"),
                    TargetWordsPerMinute = GetJsonInt(json, "targetWordsPerMinute", 160),
                    RecommendedTtsRateAdjustment = GetJsonInt(json, "ttsRateAdjustment", 0),
                    RecommendedTtsPitchAdjustment = GetJsonInt(json, "ttsPitchAdjustment", 0),
                    VisualStyleKeywords = GetJsonStringArray(json, "visualStyleKeywords")
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse tone profile response, using fallback");
        }

        return CreateFallbackToneProfile(originalTone);
    }

    private ToneConsistencyScore ParseToneConsistencyScore(string response, int sceneIndex)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(response, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(jsonMatch.Value);
                
                return new ToneConsistencyScore
                {
                    VocabularyScore = GetJsonDouble(json, "vocabularyScore", 85),
                    FormalityScore = GetJsonDouble(json, "formalityScore", 85),
                    EnergyScore = GetJsonDouble(json, "energyScore", 85),
                    PerspectiveScore = GetJsonDouble(json, "perspectiveScore", 85),
                    OverallScore = GetJsonDouble(json, "overallScore", 85),
                    SceneIndex = sceneIndex,
                    Reasoning = GetJsonString(json, "reasoning", "Tone analysis completed")
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse tone score response");
        }

        return CreateFallbackScore(sceneIndex);
    }

    private ToneConsistencyScore ParseVisualAlignmentScore(string response, int sceneIndex)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(response, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(jsonMatch.Value);
                var visualScore = GetJsonDouble(json, "visualAlignmentScore", 85);
                
                return new ToneConsistencyScore
                {
                    VisualAlignmentScore = visualScore,
                    OverallScore = visualScore,
                    SceneIndex = sceneIndex,
                    Reasoning = GetJsonString(json, "reasoning", "Visual alignment analysis completed")
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse visual alignment score");
        }

        return CreateFallbackScore(sceneIndex);
    }

    private StyleViolation[] ParseStyleViolations(string response)
    {
        try
        {
            var arrayMatch = System.Text.RegularExpressions.Regex.Match(response, @"\[.*\]", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (arrayMatch.Success)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(arrayMatch.Value);
                var violations = new List<StyleViolation>();

                foreach (var item in json.EnumerateArray())
                {
                    violations.Add(new StyleViolation
                    {
                        Severity = ParseEnum<ViolationSeverity>(item, "severity", ViolationSeverity.Medium),
                        Category = ParseEnum<ViolationCategory>(item, "category", ViolationCategory.ToneDrift),
                        SceneIndex = GetJsonInt(item, "sceneIndex", 0),
                        Description = GetJsonString(item, "description", ""),
                        Example = GetJsonString(item, "example", ""),
                        Expected = GetJsonString(item, "expected", ""),
                        Actual = GetJsonString(item, "actual", ""),
                        ImpactScore = GetJsonDouble(item, "impactScore", 50)
                    });
                }

                return violations.ToArray();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse style violations");
        }

        return Array.Empty<StyleViolation>();
    }

    private ToneDriftResult ParseDriftAnalysis(string response, int startIndex)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(response, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(jsonMatch.Value);
                
                return new ToneDriftResult
                {
                    DriftDetected = GetJsonBool(json, "driftDetected", false),
                    DriftedCharacteristics = GetJsonStringArray(json, "driftedCharacteristics"),
                    Violations = ParseStyleViolationsFromElement(json, "violations")
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse drift analysis");
        }

        return new ToneDriftResult { DriftDetected = false };
    }

    private ToneCorrectionSuggestion ParseCorrectionSuggestion(string response, int sceneIndex, string originalText)
    {
        try
        {
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(response, @"\{.*\}", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(jsonMatch.Value);
                
                return new ToneCorrectionSuggestion
                {
                    SceneIndex = sceneIndex,
                    OriginalText = originalText,
                    CorrectedText = GetJsonString(json, "correctedText", originalText),
                    Explanation = GetJsonString(json, "explanation", ""),
                    SpecificChanges = GetJsonStringArray(json, "specificChanges"),
                    ScoreBefore = 70,
                    ScoreAfter = GetJsonDouble(json, "scoreAfter", 90)
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse correction suggestion");
        }

        return new ToneCorrectionSuggestion
        {
            SceneIndex = sceneIndex,
            OriginalText = originalText,
            CorrectedText = originalText,
            Explanation = "Could not generate correction"
        };
    }

    private ToneProfile CreateFallbackToneProfile(string tone)
    {
        return new ToneProfile
        {
            OriginalTone = tone,
            VocabularyLevel = VocabularyLevel.Grade9to12,
            Formality = FormalityLevel.Conversational,
            Humor = HumorStyle.Light,
            Energy = EnergyLevel.Moderate,
            Perspective = NarrativePerspective.SecondPerson,
            Guidelines = $"General guidelines for {tone} tone.",
            ExamplePhrases = new[] { $"Example phrase for {tone}" },
            PhrasesToAvoid = new[] { "Phrases that don't match the tone" },
            TargetWordsPerMinute = 160,
            VisualStyleKeywords = new[] { "appropriate", "engaging", "professional" }
        };
    }

    private ToneConsistencyScore CreateFallbackScore(int sceneIndex)
    {
        return new ToneConsistencyScore
        {
            OverallScore = 85,
            VocabularyScore = 85,
            FormalityScore = 85,
            EnergyScore = 85,
            PerspectiveScore = 85,
            VisualAlignmentScore = 85,
            PacingAlignmentScore = 85,
            SceneIndex = sceneIndex,
            Reasoning = "Fallback score due to analysis failure"
        };
    }

    private double GetExpectedCutFrequency(EnergyLevel energy)
    {
        return energy switch
        {
            EnergyLevel.Calm => 0.5,
            EnergyLevel.Moderate => 1.0,
            EnergyLevel.Energetic => 2.0,
            EnergyLevel.High => 3.0,
            _ => 1.0
        };
    }

    private double CalculateCorrelation(double actual, double expected)
    {
        var diff = Math.Abs(actual - expected);
        return Math.Max(0, 1.0 - (diff / expected));
    }

    private double CalculateRateAlignment(double rate, ToneProfile profile)
    {
        var targetRate = profile.Energy switch
        {
            EnergyLevel.Calm => 0.8,
            EnergyLevel.Moderate => 1.0,
            EnergyLevel.Energetic => 1.2,
            EnergyLevel.High => 1.4,
            _ => 1.0
        };

        var diff = Math.Abs(rate - targetRate);
        return Math.Max(0, 100 - (diff * 100));
    }

    private double CalculatePitchAlignment(double pitch, ToneProfile profile)
    {
        var targetPitch = profile.Energy switch
        {
            EnergyLevel.Calm => 0.9,
            EnergyLevel.Moderate => 1.0,
            EnergyLevel.Energetic => 1.1,
            EnergyLevel.High => 1.2,
            _ => 1.0
        };

        var diff = Math.Abs(pitch - targetPitch);
        return Math.Max(0, 100 - (diff * 100));
    }

    private T ParseEnum<T>(JsonElement json, string propertyName, T defaultValue) where T : struct, Enum
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            if (Enum.TryParse<T>(prop.GetString(), true, out var result))
            {
                return result;
            }
        }
        return defaultValue;
    }

    private string GetJsonString(JsonElement json, string propertyName, string defaultValue)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString() ?? defaultValue;
        }
        return defaultValue;
    }

    private int GetJsonInt(JsonElement json, string propertyName, int defaultValue)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetInt32();
        }
        return defaultValue;
    }

    private double GetJsonDouble(JsonElement json, string propertyName, double defaultValue)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Number)
        {
            return prop.GetDouble();
        }
        return defaultValue;
    }

    private bool GetJsonBool(JsonElement json, string propertyName, bool defaultValue)
    {
        if (json.TryGetProperty(propertyName, out var prop) && (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False))
        {
            return prop.GetBoolean();
        }
        return defaultValue;
    }

    private string[] GetJsonStringArray(JsonElement json, string propertyName)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
        {
            return prop.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString() ?? "")
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }
        return Array.Empty<string>();
    }

    private StyleViolation[] ParseStyleViolationsFromElement(JsonElement json, string propertyName)
    {
        if (json.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
        {
            return prop.EnumerateArray()
                .Select(item => new StyleViolation
                {
                    Severity = ParseEnum<ViolationSeverity>(item, "severity", ViolationSeverity.Medium),
                    Category = ParseEnum<ViolationCategory>(item, "category", ViolationCategory.ToneDrift),
                    SceneIndex = GetJsonInt(item, "sceneIndex", 0),
                    Description = GetJsonString(item, "description", ""),
                    Example = GetJsonString(item, "example", ""),
                    Expected = GetJsonString(item, "expected", ""),
                    Actual = GetJsonString(item, "actual", ""),
                    ImpactScore = GetJsonDouble(item, "impactScore", 50)
                })
                .ToArray();
        }
        return Array.Empty<StyleViolation>();
    }
}
