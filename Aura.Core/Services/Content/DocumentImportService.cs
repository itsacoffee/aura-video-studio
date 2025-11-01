using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models;
using Aura.Core.Models.Audience;
using Aura.Core.Models.Content;
using Aura.Core.Providers;
using Aura.Core.Services.Content.DocumentParsers;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content;

/// <summary>
/// Service for importing documents in various formats and extracting structure and metadata
/// Supports: Plain Text, Markdown, HTML, JSON, Word, PDF, and more
/// </summary>
public class DocumentImportService
{
    private readonly ILogger<DocumentImportService> _logger;
    private readonly ILlmProvider _llmProvider;
    private readonly List<IDocumentParser> _parsers;
    private readonly ILoggerFactory _loggerFactory;
    
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const int MaxWordCount = 50000;

    public DocumentImportService(ILogger<DocumentImportService> logger, ILlmProvider llmProvider, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _llmProvider = llmProvider;
        _loggerFactory = loggerFactory;
        
        _parsers = new List<IDocumentParser>
        {
            new PlainTextParser(_loggerFactory.CreateLogger<PlainTextParser>()),
            new MarkdownParser(_loggerFactory.CreateLogger<MarkdownParser>()),
            new HtmlParser(_loggerFactory.CreateLogger<HtmlParser>()),
            new JsonParser(_loggerFactory.CreateLogger<JsonParser>()),
            new WordParser(_loggerFactory.CreateLogger<WordParser>()),
            new PdfParser(_loggerFactory.CreateLogger<PdfParser>())
        };
    }

    /// <summary>
    /// Imports a document from a stream and extracts structure and metadata
    /// </summary>
    public async Task<DocumentImportResult> ImportDocumentAsync(
        Stream stream, 
        string fileName, 
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting document import: {FileName}", fileName);

            if (stream.Length > MaxFileSizeBytes)
            {
                return new DocumentImportResult
                {
                    Success = false,
                    ErrorMessage = $"File size exceeds maximum limit of {MaxFileSizeBytes / 1024 / 1024}MB",
                    ProcessingTime = stopwatch.Elapsed
                };
            }

            var parser = FindParser(fileName);
            if (parser == null)
            {
                return new DocumentImportResult
                {
                    Success = false,
                    ErrorMessage = $"Unsupported file format: {Path.GetExtension(fileName)}",
                    ProcessingTime = stopwatch.Elapsed
                };
            }

            _logger.LogInformation("Using {ParserType} for {FileName}", parser.GetType().Name, fileName);

            var result = await parser.ParseAsync(stream, fileName, ct).ConfigureAwait(false);

            if (!result.Success)
            {
                return result;
            }

            if (result.Metadata.WordCount > MaxWordCount)
            {
                return result with
                {
                    Warnings = result.Warnings.Concat(new[]
                    {
                        $"Document exceeds recommended word count of {MaxWordCount}. Consider splitting into multiple documents."
                    }).ToList()
                };
            }

            if (string.IsNullOrWhiteSpace(result.InferredAudience?.EducationLevel))
            {
                result = await EnhanceWithLlmAnalysisAsync(result, ct).ConfigureAwait(false);
            }

            stopwatch.Stop();
            _logger.LogInformation("Document import completed in {ElapsedMs}ms: {FileName}", 
                stopwatch.ElapsedMilliseconds, fileName);

            return result with { ProcessingTime = stopwatch.Elapsed };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing document: {FileName}", fileName);
            stopwatch.Stop();
            
            return new DocumentImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to import document: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Enhances document analysis with LLM-powered audience inference and deeper analysis
    /// </summary>
    private async Task<DocumentImportResult> EnhanceWithLlmAnalysisAsync(
        DocumentImportResult result, 
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Enhancing document analysis with LLM");

            var contentSample = GetContentSample(result.RawContent, maxWords: 500);
            
            var prompt = $@"Analyze this document excerpt and infer the target audience characteristics.

Document excerpt:
{contentSample}

Provide analysis in this format:
EDUCATION_LEVEL: [Elementary/Middle School/High School/College/Graduate/Professional]
EXPERTISE_LEVEL: [Beginner/Intermediate/Advanced/Expert]
PROFESSIONS: [comma-separated list of likely professions]
AGE_RANGE: [e.g., 18-25, 25-40, 40-60, 60+]
REASONING: [brief explanation of your analysis]";

            var brief = new Brief(
                Topic: "Document Analysis",
                Audience: null,
                Goal: null,
                Tone: "analytical",
                Language: "en",
                Aspect: Aspect.Widescreen16x9
            );

            var planSpec = new PlanSpec(
                TargetDuration: TimeSpan.FromMinutes(1),
                Pacing: Pacing.Conversational,
                Density: Density.Balanced,
                Style: prompt
            );

            var response = await _llmProvider.DraftScriptAsync(brief, planSpec, ct).ConfigureAwait(false);
            
            var inferredAudience = ParseAudienceInference(response);
            
            return result with { InferredAudience = inferredAudience };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enhance document analysis with LLM, continuing with basic analysis");
            return result;
        }
    }

    /// <summary>
    /// Analyzes document to suggest a Brief configuration
    /// </summary>
    public Brief SuggestBriefFromDocument(DocumentImportResult importResult)
    {
        var metadata = importResult.Metadata;
        var structure = importResult.Structure;
        
        var topic = metadata.Title ?? 
                   (structure.Sections.Any() ? structure.Sections[0].Heading : "Video from Document");

        var audience = importResult.InferredAudience?.EducationLevel ?? "General";
        
        var tone = structure.Tone.PrimaryTone.ToLowerInvariant();
        
        return new Brief(
            Topic: topic,
            Audience: audience,
            Goal: "Inform and engage",
            Tone: tone,
            Language: metadata.DetectedLanguage ?? "en",
            Aspect: Aspect.Widescreen16x9
        );
    }

    /// <summary>
    /// Estimates target video duration based on document word count
    /// Uses 150 words per minute speech rate by default
    /// </summary>
    public TimeSpan EstimateVideoDuration(DocumentImportResult importResult, int wordsPerMinute = 150)
    {
        var wordCount = importResult.Metadata.WordCount;
        var minutes = Math.Max(1, wordCount / wordsPerMinute);
        
        return TimeSpan.FromMinutes(Math.Min(minutes, 15));
    }

    /// <summary>
    /// Finds the appropriate parser for a given file
    /// </summary>
    private IDocumentParser? FindParser(string fileName)
    {
        return _parsers.FirstOrDefault(p => p.CanParse(fileName));
    }

    /// <summary>
    /// Gets a sample of content for LLM analysis (limits word count to avoid token limits)
    /// </summary>
    private string GetContentSample(string content, int maxWords)
    {
        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length <= maxWords)
        {
            return content;
        }

        var sample = string.Join(" ", words.Take(maxWords));
        return sample + "\n\n[...content truncated...]";
    }

    /// <summary>
    /// Parses LLM response to extract audience inference
    /// </summary>
    private InferredAudience ParseAudienceInference(string response)
    {
        var educationMatch = Regex.Match(response, @"EDUCATION_LEVEL:\s*(.+)", RegexOptions.IgnoreCase);
        var expertiseMatch = Regex.Match(response, @"EXPERTISE_LEVEL:\s*(.+)", RegexOptions.IgnoreCase);
        var professionsMatch = Regex.Match(response, @"PROFESSIONS:\s*(.+)", RegexOptions.IgnoreCase);
        var ageRangeMatch = Regex.Match(response, @"AGE_RANGE:\s*(.+)", RegexOptions.IgnoreCase);
        var reasoningMatch = Regex.Match(response, @"REASONING:\s*(.+)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        var professions = new List<string>();
        if (professionsMatch.Success)
        {
            professions = professionsMatch.Groups[1].Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();
        }

        return new InferredAudience
        {
            EducationLevel = educationMatch.Success ? educationMatch.Groups[1].Value.Trim() : "High School",
            ExpertiseLevel = expertiseMatch.Success ? expertiseMatch.Groups[1].Value.Trim() : "Intermediate",
            PossibleProfessions = professions,
            AgeRange = ageRangeMatch.Success ? ageRangeMatch.Groups[1].Value.Trim() : "25-40",
            ConfidenceScore = 0.75,
            Reasoning = reasoningMatch.Success ? reasoningMatch.Groups[1].Value.Trim() : "Based on document analysis"
        };
    }
}
