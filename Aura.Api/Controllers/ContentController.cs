using Aura.Core.Models;
using Aura.Core.Providers;
using Aura.Core.Services.Content;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Aura.Api.Controllers;

/// <summary>
/// Controller for AI-powered content analysis and enhancement
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly ContentAnalyzer _contentAnalyzer;
    private readonly ScriptEnhancer _scriptEnhancer;
    private readonly VisualAssetSuggester _visualAssetSuggester;
    private readonly PacingOptimizer _pacingOptimizer;
    private readonly DocumentImportService? _documentImportService;
    private readonly ScriptConverter? _scriptConverter;

    public ContentController(
        ContentAnalyzer contentAnalyzer,
        ScriptEnhancer scriptEnhancer,
        VisualAssetSuggester visualAssetSuggester,
        PacingOptimizer pacingOptimizer,
        DocumentImportService? documentImportService = null,
        ScriptConverter? scriptConverter = null)
    {
        _contentAnalyzer = contentAnalyzer;
        _scriptEnhancer = scriptEnhancer;
        _visualAssetSuggester = visualAssetSuggester;
        _pacingOptimizer = pacingOptimizer;
        _documentImportService = documentImportService;
        _scriptConverter = scriptConverter;
    }

    /// <summary>
    /// Analyzes a script for quality metrics
    /// </summary>
    [HttpPost("analyze-script")]
    public async Task<IActionResult> AnalyzeScript(
        [FromBody] AnalyzeScriptRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Analyzing script", correlationId);

            var analysis = await _contentAnalyzer.AnalyzeScriptAsync(request.Script, ct).ConfigureAwait(false);

            return Ok(new
            {
                coherenceScore = analysis.CoherenceScore,
                pacingScore = analysis.PacingScore,
                engagementScore = analysis.EngagementScore,
                readabilityScore = analysis.ReadabilityScore,
                overallQualityScore = analysis.OverallQualityScore,
                issues = analysis.Issues,
                suggestions = analysis.Suggestions,
                statistics = new
                {
                    totalWordCount = analysis.Statistics.TotalWordCount,
                    averageWordsPerScene = analysis.Statistics.AverageWordsPerScene,
                    estimatedReadingTime = analysis.Statistics.EstimatedReadingTime.ToString(),
                    complexityScore = analysis.Statistics.ComplexityScore
                },
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error analyzing script", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#content-analysis",
                title = "Script Analysis Failed",
                status = 500,
                detail = $"Failed to analyze script: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Enhances a script based on provided options
    /// </summary>
    [HttpPost("enhance-script")]
    public async Task<IActionResult> EnhanceScript(
        [FromBody] EnhanceScriptRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Enhancing script", correlationId);

            var options = new EnhancementOptions(
                FixCoherence: request.FixCoherence,
                IncreaseEngagement: request.IncreaseEngagement,
                ImproveClarity: request.ImproveClarity,
                AddDetails: request.AddDetails
            );

            var enhanced = await _scriptEnhancer.EnhanceScriptAsync(request.Script, options, ct).ConfigureAwait(false);

            return Ok(new
            {
                newScript = enhanced.NewScript,
                changes = enhanced.Changes.Select(c => new
                {
                    type = c.Type,
                    lineNumber = c.LineNumber,
                    originalText = c.OriginalText,
                    newText = c.NewText
                }),
                improvementSummary = enhanced.ImprovementSummary,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error enhancing script", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#content-enhancement",
                title = "Script Enhancement Failed",
                status = 500,
                detail = $"Failed to enhance script: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Suggests visual assets for a scene
    /// </summary>
    [HttpPost("suggest-assets")]
    public async Task<IActionResult> SuggestAssets(
        [FromBody] SuggestAssetsRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Suggesting assets for scene: {Heading}", correlationId, request.SceneHeading);

            var suggestions = await _visualAssetSuggester.SuggestAssetsForSceneAsync(
                request.SceneHeading, 
                request.SceneScript, 
                ct
            ).ConfigureAwait(false);

            return Ok(new
            {
                suggestions = suggestions.Select(s => new
                {
                    keyword = s.Keyword,
                    description = s.Description,
                    matches = s.Matches.Select(m => new
                    {
                        filePath = m.FilePath,
                        url = m.Url,
                        relevanceScore = m.RelevanceScore,
                        thumbnailUrl = m.ThumbnailUrl
                    })
                }),
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error suggesting assets", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#asset-suggestion",
                title = "Asset Suggestion Failed",
                status = 500,
                detail = $"Failed to suggest assets: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Optimizes pacing for a timeline
    /// </summary>
    [HttpPost("optimize-pacing")]
    public async Task<IActionResult> OptimizePacing(
        [FromBody] OptimizePacingRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Optimizing pacing for timeline", correlationId);

            // Convert request to Timeline
            var scenes = request.Scenes.Select(s => new Scene(
                Index: s.Index,
                Heading: s.Heading,
                Script: s.Script,
                Start: TimeSpan.Parse(s.Start),
                Duration: TimeSpan.Parse(s.Duration)
            )).ToList();

            var timeline = new Aura.Core.Providers.Timeline(
                Scenes: scenes,
                SceneAssets: new Dictionary<int, IReadOnlyList<Asset>>(),
                NarrationPath: request.NarrationPath ?? "",
                MusicPath: request.MusicPath ?? "",
                SubtitlesPath: null
            );

            var optimization = await _pacingOptimizer.OptimizeTimingAsync(timeline, ct).ConfigureAwait(false);

            return Ok(new
            {
                suggestions = optimization.Suggestions.Select(s => new
                {
                    sceneIndex = s.SceneIndex,
                    currentDuration = s.CurrentDuration.ToString(),
                    suggestedDuration = s.SuggestedDuration.ToString(),
                    reasoning = s.Reasoning,
                    priority = s.Priority.ToString()
                }),
                overallAssessment = optimization.OverallAssessment,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error optimizing pacing", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#pacing-optimization",
                title = "Pacing Optimization Failed",
                status = 500,
                detail = $"Failed to optimize pacing: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Imports a document and extracts structure and metadata
    /// </summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ImportDocument(
        IFormFile file,
        CancellationToken ct = default)
    {
        try
        {
            if (_documentImportService == null)
            {
                return StatusCode(503, new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#service-unavailable",
                    title = "Document Import Service Unavailable",
                    status = 503,
                    detail = "Document import service is not configured",
                    correlationId = HttpContext.TraceIdentifier
                });
            }

            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Importing document: {FileName}", correlationId, file.FileName);

            if (file.Length == 0)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#invalid-file",
                    title = "Invalid File",
                    status = 400,
                    detail = "File is empty",
                    correlationId
                });
            }

            using var stream = file.OpenReadStream();
            var result = await _documentImportService.ImportDocumentAsync(stream, file.FileName, ct).ConfigureAwait(false);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#import-failed",
                    title = "Document Import Failed",
                    status = 400,
                    detail = result.ErrorMessage,
                    correlationId
                });
            }

            return Ok(new
            {
                success = result.Success,
                metadata = new
                {
                    originalFileName = result.Metadata.OriginalFileName,
                    format = result.Metadata.Format.ToString(),
                    fileSizeBytes = result.Metadata.FileSizeBytes,
                    importedAt = result.Metadata.ImportedAt,
                    wordCount = result.Metadata.WordCount,
                    characterCount = result.Metadata.CharacterCount,
                    detectedLanguage = result.Metadata.DetectedLanguage,
                    title = result.Metadata.Title,
                    author = result.Metadata.Author
                },
                structure = new
                {
                    sections = result.Structure.Sections.Select(s => new
                    {
                        level = s.Level,
                        heading = s.Heading,
                        content = s.Content.Length > 200 ? s.Content[..200] + "..." : s.Content,
                        wordCount = s.WordCount,
                        estimatedSpeechDurationSeconds = s.EstimatedSpeechDuration.TotalSeconds
                    }),
                    headingLevels = result.Structure.HeadingLevels,
                    keyConcepts = result.Structure.KeyConcepts,
                    complexity = new
                    {
                        readingLevel = result.Structure.Complexity.ReadingLevel,
                        technicalDensity = result.Structure.Complexity.TechnicalDensity,
                        abstractionLevel = result.Structure.Complexity.AbstractionLevel,
                        averageSentenceLength = result.Structure.Complexity.AverageSentenceLength,
                        complexWordCount = result.Structure.Complexity.ComplexWordCount,
                        complexityDescription = result.Structure.Complexity.ComplexityDescription
                    },
                    tone = new
                    {
                        primaryTone = result.Structure.Tone.PrimaryTone,
                        formalityLevel = result.Structure.Tone.FormalityLevel,
                        writingStyle = result.Structure.Tone.WritingStyle
                    }
                },
                inferredAudience = result.InferredAudience != null ? new
                {
                    educationLevel = result.InferredAudience.EducationLevel,
                    expertiseLevel = result.InferredAudience.ExpertiseLevel,
                    possibleProfessions = result.InferredAudience.PossibleProfessions,
                    ageRange = result.InferredAudience.AgeRange,
                    confidenceScore = result.InferredAudience.ConfidenceScore,
                    reasoning = result.InferredAudience.Reasoning
                } : null,
                warnings = result.Warnings,
                processingTimeSeconds = result.ProcessingTime.TotalSeconds,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error importing document", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#document-import",
                title = "Document Import Failed",
                status = 500,
                detail = $"Failed to import document: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Converts an imported document to a video script
    /// </summary>
    [HttpPost("convert")]
    public async Task<IActionResult> ConvertDocument(
        [FromBody] ConvertDocumentRequestDto request,
        CancellationToken ct = default)
    {
        try
        {
            if (_scriptConverter == null)
            {
                return StatusCode(503, new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#service-unavailable",
                    title = "Script Converter Service Unavailable",
                    status = 503,
                    detail = "Script converter service is not configured",
                    correlationId = HttpContext.TraceIdentifier
                });
            }

            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Converting document to script using preset: {Preset}", 
                correlationId, request.Preset);

            var documentResult = MapToDocumentImportResult(request);
            var config = MapToConversionConfig(request);

            var result = await _scriptConverter.ConvertToScriptAsync(documentResult, config, ct).ConfigureAwait(false);

            if (!result.Success)
            {
                return BadRequest(new
                {
                    type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#conversion-failed",
                    title = "Document Conversion Failed",
                    status = 400,
                    detail = result.ErrorMessage,
                    correlationId
                });
            }

            return Ok(new
            {
                success = result.Success,
                scenes = result.Scenes.Select(s => new
                {
                    index = s.Index,
                    heading = s.Heading,
                    script = s.Script,
                    startSeconds = s.Start.TotalSeconds,
                    durationSeconds = s.Duration.TotalSeconds
                }),
                suggestedBrief = new
                {
                    topic = result.SuggestedBrief.Topic,
                    audience = result.SuggestedBrief.Audience,
                    goal = result.SuggestedBrief.Goal,
                    tone = result.SuggestedBrief.Tone,
                    language = result.SuggestedBrief.Language,
                    aspect = result.SuggestedBrief.Aspect.ToString()
                },
                changes = result.Changes.Select(c => new
                {
                    category = c.Category,
                    description = c.Description,
                    justification = c.Justification,
                    sectionIndex = c.SectionIndex,
                    impactLevel = c.ImpactLevel
                }),
                metrics = new
                {
                    originalWordCount = result.Metrics.OriginalWordCount,
                    convertedWordCount = result.Metrics.ConvertedWordCount,
                    compressionRatio = result.Metrics.CompressionRatio,
                    sectionsCreated = result.Metrics.SectionsCreated,
                    transitionsAdded = result.Metrics.TransitionsAdded,
                    visualSuggestionsGenerated = result.Metrics.VisualSuggestionsGenerated,
                    overallConfidenceScore = result.Metrics.OverallConfidenceScore
                },
                sectionConversions = result.SectionConversions.Select(sc => new
                {
                    sectionIndex = sc.SectionIndex,
                    originalHeading = sc.OriginalHeading,
                    convertedHeading = sc.ConvertedHeading,
                    originalContent = sc.OriginalContent,
                    convertedContent = sc.ConvertedContent,
                    confidenceScore = sc.ConfidenceScore,
                    requiresManualReview = sc.RequiresManualReview,
                    changeHighlights = sc.ChangeHighlights,
                    reasoning = sc.Reasoning
                }),
                processingTimeSeconds = result.ProcessingTime.TotalSeconds,
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error converting document", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#document-conversion",
                title = "Document Conversion Failed",
                status = 500,
                detail = $"Failed to convert document: {ex.Message}",
                correlationId
            });
        }
    }

    /// <summary>
    /// Gets available conversion presets
    /// </summary>
    [HttpGet("presets")]
    public IActionResult GetConversionPresets()
    {
        try
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Information("[{CorrelationId}] Getting conversion presets", correlationId);

            var presets = ConversionPresets.GetAllPresets();

            return Ok(new
            {
                presets = presets.Select(p => new
                {
                    type = p.Type.ToString(),
                    name = p.Name,
                    description = p.Description,
                    defaultConfig = new
                    {
                        preset = p.DefaultConfig.Preset.ToString(),
                        targetDurationMinutes = p.DefaultConfig.TargetDuration.TotalMinutes,
                        wordsPerMinute = p.DefaultConfig.WordsPerMinute,
                        enableAudienceRetargeting = p.DefaultConfig.EnableAudienceRetargeting,
                        enableVisualSuggestions = p.DefaultConfig.EnableVisualSuggestions,
                        preserveOriginalStructure = p.DefaultConfig.PreserveOriginalStructure,
                        addTransitions = p.DefaultConfig.AddTransitions,
                        aggressivenessLevel = p.DefaultConfig.AggressivenessLevel
                    },
                    bestForFormats = p.BestForFormats,
                    restructuringStrategy = p.RestructuringStrategy
                }),
                correlationId
            });
        }
        catch (Exception ex)
        {
            var correlationId = HttpContext.TraceIdentifier;
            Log.Error(ex, "[{CorrelationId}] Error getting presets", correlationId);
            
            return StatusCode(500, new
            {
                type = "https://github.com/Coffee285/aura-video-studio/blob/main/docs/errors/README.md#presets",
                title = "Failed to Get Presets",
                status = 500,
                detail = $"Failed to get conversion presets: {ex.Message}",
                correlationId
            });
        }
    }

    private Aura.Core.Models.Content.DocumentImportResult MapToDocumentImportResult(ConvertDocumentRequestDto request)
    {
        var sections = request.Sections.Select(s => new Aura.Core.Models.Content.DocumentSection
        {
            Level = s.Level,
            Heading = s.Heading,
            Content = s.Content,
            WordCount = s.WordCount,
            EstimatedSpeechDuration = TimeSpan.FromSeconds(s.EstimatedSpeechDurationSeconds),
            Subsections = new List<Aura.Core.Models.Content.DocumentSection>(),
            Examples = new List<string>(),
            VisualOpportunities = new List<string>()
        }).ToList();

        return new Aura.Core.Models.Content.DocumentImportResult
        {
            Success = true,
            Metadata = new Aura.Core.Models.Content.DocumentMetadata
            {
                OriginalFileName = request.OriginalFileName,
                Format = Enum.Parse<Aura.Core.Models.Content.DocumentFormat>(request.Format),
                FileSizeBytes = request.FileSizeBytes,
                ImportedAt = request.ImportedAt,
                WordCount = request.WordCount,
                CharacterCount = request.CharacterCount,
                DetectedLanguage = request.DetectedLanguage,
                Title = request.Title,
                Author = request.Author
            },
            Structure = new Aura.Core.Models.Content.DocumentStructure
            {
                Sections = sections,
                HeadingLevels = sections.Any() ? sections.Max(s => s.Level) : 0,
                KeyConcepts = request.KeyConcepts ?? new List<string>(),
                Complexity = new Aura.Core.Models.Content.DocumentComplexity
                {
                    ReadingLevel = request.ReadingLevel,
                    TechnicalDensity = request.TechnicalDensity,
                    ComplexityDescription = request.ComplexityDescription ?? "Unknown"
                },
                Tone = new Aura.Core.Models.Content.DocumentTone
                {
                    PrimaryTone = request.PrimaryTone ?? "Professional",
                    FormalityLevel = request.FormalityLevel,
                    WritingStyle = request.WritingStyle ?? "Standard"
                }
            },
            RawContent = string.Join("\n\n", sections.Select(s => s.Content)),
            ProcessingTime = TimeSpan.Zero
        };
    }

    private Aura.Core.Models.Content.ConversionConfig MapToConversionConfig(ConvertDocumentRequestDto request)
    {
        return new Aura.Core.Models.Content.ConversionConfig
        {
            Preset = Enum.Parse<Aura.Core.Models.Content.ConversionPreset>(request.Preset),
            TargetDuration = TimeSpan.FromMinutes(request.TargetDurationMinutes),
            WordsPerMinute = request.WordsPerMinute,
            EnableAudienceRetargeting = request.EnableAudienceRetargeting,
            EnableVisualSuggestions = request.EnableVisualSuggestions,
            PreserveOriginalStructure = request.PreserveOriginalStructure,
            AddTransitions = request.AddTransitions,
            AggressivenessLevel = request.AggressivenessLevel,
            TargetAudienceProfileId = request.TargetAudienceProfileId
        };
    }
}

// Request models
public record AnalyzeScriptRequest(string Script);

public record EnhanceScriptRequest(
    string Script,
    bool FixCoherence = false,
    bool IncreaseEngagement = false,
    bool ImproveClarity = false,
    bool AddDetails = false);

public record SuggestAssetsRequest(string SceneHeading, string SceneScript);

public record OptimizePacingRequest(
    List<SceneDto> Scenes,
    string? NarrationPath = null,
    string? MusicPath = null);

public record SceneDto(
    int Index,
    string Heading,
    string Script,
    string Start,
    string Duration);

public record ConvertDocumentRequestDto(
    string OriginalFileName,
    string Format,
    long FileSizeBytes,
    DateTime ImportedAt,
    int WordCount,
    int CharacterCount,
    string? DetectedLanguage,
    string? Title,
    string? Author,
    List<DocumentSectionRequestDto> Sections,
    List<string>? KeyConcepts,
    double ReadingLevel,
    double TechnicalDensity,
    string? ComplexityDescription,
    string? PrimaryTone,
    double FormalityLevel,
    string? WritingStyle,
    string Preset,
    double TargetDurationMinutes,
    int WordsPerMinute,
    bool EnableAudienceRetargeting,
    bool EnableVisualSuggestions,
    bool PreserveOriginalStructure,
    bool AddTransitions,
    double AggressivenessLevel,
    string? TargetAudienceProfileId);

public record DocumentSectionRequestDto(
    int Level,
    string Heading,
    string Content,
    int WordCount,
    double EstimatedSpeechDurationSeconds);
