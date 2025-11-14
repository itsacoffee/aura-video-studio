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
using Aura.Core.Models.Content;
using Aura.Core.Orchestration;
using Aura.Core.Providers;
using Aura.Core.Services.Audience;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content;

/// <summary>
/// Converts imported documents into video-optimized scripts with scene structure
/// Handles content restructuring, written-to-spoken conversion, and audience adaptation
/// Now uses unified orchestration via LlmStageAdapter
/// </summary>
public class ScriptConverter
{
    private readonly ILogger<ScriptConverter> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly LlmStageAdapter? _stageAdapter;
    private readonly ContentAdaptationEngine? _adaptationEngine;
    private readonly AudienceProfileStore? _audienceProfileStore;

    private const string VisualTagPattern = "[VISUAL:";

    public ScriptConverter(
        ILogger<ScriptConverter> logger,
        ILlmProvider llmProvider,
        ContentAdaptationEngine? adaptationEngine = null,
        AudienceProfileStore? audienceProfileStore = null,
        LlmStageAdapter? stageAdapter = null)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _stageAdapter = stageAdapter;
        _adaptationEngine = adaptationEngine;
        _audienceProfileStore = audienceProfileStore;
    }

    /// <summary>
    /// Converts a document into a video script with scenes
    /// </summary>
    public async Task<ConversionResult> ConvertToScriptAsync(
        DocumentImportResult document,
        ConversionConfig config,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting document to script conversion using preset: {Preset}", config.Preset);

            var scenes = await ConvertSectionsToScenesAsync(document, config, ct).ConfigureAwait(false);
            
            if (scenes.Count == 0)
            {
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to generate scenes from document",
                    ProcessingTime = stopwatch.Elapsed
                };
            }

            var originalWordCount = document.Metadata.WordCount;
            var convertedWordCount = scenes.Sum(s => CountWords(s.Script));
            
            var sectionConversions = BuildSectionConversions(document.Structure.Sections, scenes);
            var changes = AnalyzeChanges(document, scenes, config);
            
            var suggestedBrief = BuildSuggestedBrief(document, config);

            var result = new ConversionResult
            {
                Success = true,
                Scenes = scenes,
                SuggestedBrief = suggestedBrief,
                Changes = changes,
                Metrics = new ConversionMetrics
                {
                    OriginalWordCount = originalWordCount,
                    ConvertedWordCount = convertedWordCount,
                    CompressionRatio = originalWordCount > 0 ? (double)convertedWordCount / originalWordCount : 0,
                    SectionsCreated = scenes.Count,
                    TransitionsAdded = config.AddTransitions ? scenes.Count - 1 : 0,
                    VisualSuggestionsGenerated = scenes.Sum(s => s.Script.Contains(VisualTagPattern, StringComparison.OrdinalIgnoreCase) ? 1 : 0),
                    OverallConfidenceScore = CalculateOverallConfidence(sectionConversions)
                },
                SectionConversions = sectionConversions,
                ProcessingTime = stopwatch.Elapsed
            };

            if (config.EnableAudienceRetargeting && !string.IsNullOrEmpty(config.TargetAudienceProfileId))
            {
                result = await ApplyAudienceRetargetingAsync(result, config.TargetAudienceProfileId, ct).ConfigureAwait(false);
            }

            stopwatch.Stop();
            _logger.LogInformation("Conversion completed in {ElapsedMs}ms. Created {SceneCount} scenes.", 
                stopwatch.ElapsedMilliseconds, scenes.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting document to script");
            stopwatch.Stop();
            
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = $"Conversion failed: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Converts document sections into video scenes using LLM
    /// </summary>
    private async Task<List<Scene>> ConvertSectionsToScenesAsync(
        DocumentImportResult document,
        ConversionConfig config,
        CancellationToken ct)
    {
        var preset = ConversionPresets.GetPreset(config.Preset);
        var sections = document.Structure.Sections;
        
        if (sections.Count == 0)
        {
            sections = new List<DocumentSection>
            {
                new DocumentSection
                {
                    Level = 1,
                    Heading = "Content",
                    Content = document.RawContent,
                    WordCount = document.Metadata.WordCount
                }
            };
        }

        var targetWordCount = (int)(config.TargetDuration.TotalMinutes * config.WordsPerMinute);
        var compressionRatio = targetWordCount / (double)document.Metadata.WordCount;

        var prompt = BuildConversionPrompt(document, sections, config, preset, compressionRatio);

        _logger.LogInformation("Converting {SectionCount} sections to video script. Target: {TargetWords} words", 
            sections.Count, targetWordCount);

        var brief = new Brief(
            Topic: document.Metadata.Title ?? "Document Content",
            Audience: document.InferredAudience?.EducationLevel,
            Goal: "Inform and engage",
            Tone: document.Structure.Tone.PrimaryTone,
            Language: "en",
            Aspect: Aspect.Widescreen16x9
        );

        var planSpec = new PlanSpec(
            TargetDuration: config.TargetDuration,
            Pacing: Pacing.Conversational,
            Density: Density.Balanced,
            Style: prompt
        );

        var response = await GenerateWithLlmAsync(brief, planSpec, ct).ConfigureAwait(false);
        
        return ParseScriptToScenes(response, config);
    }

    /// <summary>
    /// Builds the LLM prompt for converting document to script
    /// </summary>
    private string BuildConversionPrompt(
        DocumentImportResult document,
        List<DocumentSection> sections,
        ConversionConfig config,
        PresetDefinition preset,
        double compressionRatio)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine($"Convert this document into an engaging video script using the '{preset.Name}' approach.");
        sb.AppendLine($"Strategy: {preset.RestructuringStrategy}");
        sb.AppendLine();
        sb.AppendLine($"Target video duration: {config.TargetDuration.TotalMinutes:F1} minutes");
        sb.AppendLine($"Speech rate: {config.WordsPerMinute} words per minute");
        sb.AppendLine($"Target word count: ~{(int)(config.TargetDuration.TotalMinutes * config.WordsPerMinute)} words");
        sb.AppendLine();

        if (!config.PreserveOriginalStructure)
        {
            sb.AppendLine("Restructure the content for video format:");
            sb.AppendLine("1. Start with a strong hook (5-10 seconds)");
            sb.AppendLine("2. Present main content in logical flow");
            sb.AppendLine("3. End with key takeaways and conclusion");
        }

        sb.AppendLine();
        sb.AppendLine("Conversion guidelines:");
        sb.AppendLine("- Convert written text to natural spoken language");
        sb.AppendLine("- Break long sentences into shorter, clearer ones");
        sb.AppendLine("- Remove footnotes and citations (adapt as needed)");
        sb.AppendLine("- Expand abbreviations on first use");
        if (config.AddTransitions)
        {
            sb.AppendLine("- Add smooth transitions between sections");
        }
        if (config.EnableVisualSuggestions)
        {
            sb.AppendLine("- Note opportunities for visuals with [VISUAL: description]");
        }

        sb.AppendLine();
        sb.AppendLine("Original document sections:");
        sb.AppendLine();

        var maxContentLength = 300;
        foreach (var section in sections.Take(10))
        {
            sb.AppendLine($"## {section.Heading}");
            var content = section.Content.Length > maxContentLength 
                ? section.Content[..maxContentLength] + "..." 
                : section.Content;
            sb.AppendLine(content);
            sb.AppendLine();
        }

        sb.AppendLine("Generate a complete video script with scene headings and narration.");
        sb.AppendLine("Format each scene as:");
        sb.AppendLine("SCENE: [Scene title]");
        sb.AppendLine("[Narration text]");
        sb.AppendLine();

        return sb.ToString();
    }

    /// <summary>
    /// Parses LLM response into Scene objects
    /// </summary>
    private List<Scene> ParseScriptToScenes(string scriptText, ConversionConfig config)
    {
        var scenes = new List<Scene>();
        var scenePattern = @"SCENE:\s*(.+?)\n((?:(?!SCENE:).)+)";
        var matches = Regex.Matches(scriptText, scenePattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        if (matches.Count == 0)
        {
            var paragraphs = scriptText.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
            
            for (var i = 0; i < paragraphs.Length; i++)
            {
                var paragraph = paragraphs[i].Trim();
                if (string.IsNullOrWhiteSpace(paragraph) || paragraph.Length < 50)
                    continue;

                var heading = $"Scene {i + 1}";
                var duration = CalculateSceneDuration(paragraph, config.WordsPerMinute);

                scenes.Add(new Scene(
                    Index: i,
                    Heading: heading,
                    Script: paragraph,
                    Start: TimeSpan.Zero,
                    Duration: duration
                ));
            }
        }
        else
        {
            for (var i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                var heading = match.Groups[1].Value.Trim();
                var script = match.Groups[2].Value.Trim();

                var duration = CalculateSceneDuration(script, config.WordsPerMinute);

                scenes.Add(new Scene(
                    Index: i,
                    Heading: heading,
                    Script: script,
                    Start: TimeSpan.Zero,
                    Duration: duration
                ));
            }
        }

        AdjustSceneTimings(scenes);
        
        return scenes;
    }

    /// <summary>
    /// Adjusts scene start times based on durations
    /// </summary>
    private void AdjustSceneTimings(List<Scene> scenes)
    {
        var currentTime = TimeSpan.Zero;
        
        for (var i = 0; i < scenes.Count; i++)
        {
            scenes[i] = scenes[i] with { Start = currentTime };
            currentTime += scenes[i].Duration;
        }
    }

    /// <summary>
    /// Calculates scene duration based on word count and speech rate
    /// </summary>
    private TimeSpan CalculateSceneDuration(string text, int wordsPerMinute)
    {
        var wordCount = CountWords(text);
        var minutes = wordCount / (double)wordsPerMinute;
        return TimeSpan.FromMinutes(Math.Max(0.1, minutes));
    }

    /// <summary>
    /// Applies audience retargeting using ContentAdaptationEngine
    /// </summary>
    private async Task<ConversionResult> ApplyAudienceRetargetingAsync(
        ConversionResult result,
        string audienceProfileId,
        CancellationToken ct)
    {
        if (_adaptationEngine == null || _audienceProfileStore == null)
        {
            _logger.LogWarning("Audience retargeting requested but adaptation engine not available");
            return result;
        }

        try
        {
            _logger.LogInformation("Applying audience retargeting to profile: {ProfileId}", audienceProfileId);

            var profile = await _audienceProfileStore.GetByIdAsync(audienceProfileId, ct).ConfigureAwait(false);
            if (profile == null)
            {
                _logger.LogWarning("Audience profile not found: {ProfileId}", audienceProfileId);
                return result;
            }

            var adaptedScenes = new List<Scene>();
            var additionalChanges = new List<ConversionChange>();

            foreach (var scene in result.Scenes)
            {
                var adaptationResult = await _adaptationEngine.AdaptContentAsync(
                    scene.Script,
                    profile,
                    new Models.Audience.ContentAdaptationConfig { AggressivenessLevel = 0.6 },
                    ct
                ).ConfigureAwait(false);

                adaptedScenes.Add(scene with { Script = adaptationResult.AdaptedContent });

                foreach (var change in adaptationResult.Changes)
                {
                    additionalChanges.Add(new ConversionChange
                    {
                        Category = "Audience Retargeting",
                        Description = change.Description,
                        Justification = change.Reasoning,
                        SectionIndex = scene.Index,
                        ImpactLevel = 0.7
                    });
                }
            }

            return result with
            {
                Scenes = adaptedScenes,
                Changes = result.Changes.Concat(additionalChanges).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying audience retargeting");
            return result;
        }
    }

    /// <summary>
    /// Builds section conversion details
    /// </summary>
    private List<SectionConversion> BuildSectionConversions(
        List<DocumentSection> originalSections,
        List<Scene> convertedScenes)
    {
        var conversions = new List<SectionConversion>();
        
        for (var i = 0; i < Math.Min(originalSections.Count, convertedScenes.Count); i++)
        {
            var original = originalSections[i];
            var converted = convertedScenes[i];
            
            conversions.Add(new SectionConversion
            {
                SectionIndex = i,
                OriginalHeading = original.Heading,
                ConvertedHeading = converted.Heading,
                OriginalContent = original.Content.Length > 200 ? original.Content[..200] + "..." : original.Content,
                ConvertedContent = converted.Script,
                ConfidenceScore = 0.85,
                RequiresManualReview = original.WordCount > 1000,
                ChangeHighlights = new List<string> { "Converted to spoken language", "Restructured for video flow" },
                Reasoning = "Optimized for video format"
            });
        }

        return conversions;
    }

    /// <summary>
    /// Analyzes changes made during conversion
    /// </summary>
    private List<ConversionChange> AnalyzeChanges(
        DocumentImportResult document,
        List<Scene> scenes,
        ConversionConfig config)
    {
        var changes = new List<ConversionChange>();

        changes.Add(new ConversionChange
        {
            Category = "Structure",
            Description = $"Converted {document.Structure.Sections.Count} sections into {scenes.Count} video scenes",
            Justification = "Optimized for video pacing and viewer engagement",
            SectionIndex = -1,
            ImpactLevel = 0.8
        });

        if (!config.PreserveOriginalStructure)
        {
            changes.Add(new ConversionChange
            {
                Category = "Flow",
                Description = "Restructured content with hook-body-conclusion format",
                Justification = "Standard video structure for maximum engagement",
                SectionIndex = -1,
                ImpactLevel = 0.7
            });
        }

        changes.Add(new ConversionChange
        {
            Category = "Language",
            Description = "Converted written text to natural spoken language",
            Justification = "Written and spoken language have different conventions",
            SectionIndex = -1,
            ImpactLevel = 0.9
        });

        return changes;
    }

    /// <summary>
    /// Builds suggested Brief from document
    /// </summary>
    private Brief BuildSuggestedBrief(DocumentImportResult document, ConversionConfig config)
    {
        var topic = document.Metadata.Title ?? "Video from Document";
        var audience = document.InferredAudience?.EducationLevel ?? "General";
        var tone = document.Structure.Tone.PrimaryTone.ToLowerInvariant();

        return new Brief(
            Topic: topic,
            Audience: audience,
            Goal: "Inform and engage",
            Tone: tone,
            Language: document.Metadata.DetectedLanguage ?? "en",
            Aspect: Aspect.Widescreen16x9
        );
    }

    /// <summary>
    /// Calculates overall confidence score
    /// </summary>
    private double CalculateOverallConfidence(List<SectionConversion> conversions)
    {
        if (conversions.Count == 0) return 0.75;
        return conversions.Average(c => c.ConfidenceScore);
    }

    /// <summary>
    /// Counts words in text
    /// </summary>
    private int CountWords(string text)
    {
        return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
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
