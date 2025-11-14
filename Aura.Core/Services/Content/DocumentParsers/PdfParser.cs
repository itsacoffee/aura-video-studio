using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Content;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content.DocumentParsers;

/// <summary>
/// Parser for PDF documents (.pdf) using iText library
/// Extracts text content, metadata, and basic document structure
/// </summary>
public class PdfParser : IDocumentParser
{
    private readonly ILogger<PdfParser> _logger;
    private const int MaxFileSize = 50 * 1024 * 1024; // 50MB limit
    private const int AverageReadingSpeed = 200; // words per minute

    public DocFormat SupportedFormat => DocFormat.Pdf;
    public string[] SupportedExtensions => new[] { ".pdf" };

    public PdfParser(ILogger<PdfParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return SupportedExtensions.Contains(extension);
    }

    public async Task<DocumentImportResult> ParseAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();
        
        try
        {
            _logger.LogInformation("Starting PDF parsing for: {FileName}", fileName);

            if (stream.Length > MaxFileSize)
            {
                return CreateErrorResult($"PDF file size ({stream.Length / (1024 * 1024)}MB) exceeds maximum allowed size (50MB)", stopwatch);
            }

            ct.ThrowIfCancellationRequested();

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, ct).ConfigureAwait(false);
            memoryStream.Position = 0;

            using var pdfReader = new PdfReader(memoryStream);
            using var pdfDocument = new PdfDocument(pdfReader);
            
            var numberOfPages = pdfDocument.GetNumberOfPages();
            _logger.LogDebug("PDF has {PageCount} pages", numberOfPages);

            if (numberOfPages == 0)
            {
                return CreateErrorResult("PDF document is empty (0 pages)", stopwatch);
            }

            var extractedText = new StringBuilder();
            var pageTexts = new List<string>();

            for (int i = 1; i <= numberOfPages; i++)
            {
                ct.ThrowIfCancellationRequested();
                
                var page = pdfDocument.GetPage(i);
                var strategy = new LocationTextExtractionStrategy();
                var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    pageTexts.Add(pageText);
                    extractedText.AppendLine(pageText);
                    extractedText.AppendLine();
                }
            }

            var fullText = extractedText.ToString();
            
            if (string.IsNullOrWhiteSpace(fullText))
            {
                warnings.Add("No text content could be extracted from the PDF. It may contain only images or be encrypted.");
                fullText = $"[PDF document with {numberOfPages} page(s) - no extractable text found]";
            }

            var pdfInfo = pdfDocument.GetDocumentInfo();
            var sections = ParseSections(fullText);
            var words = CountWords(fullText);
            var characters = fullText.Length;

            var customMetadata = new Dictionary<string, string>();
            var subject = pdfInfo.GetSubject();
            if (!string.IsNullOrWhiteSpace(subject))
                customMetadata["Subject"] = subject;
            
            var keywords = pdfInfo.GetKeywords();
            if (!string.IsNullOrWhiteSpace(keywords))
                customMetadata["Keywords"] = keywords;
            
            var modifiedDate = TryParsePdfDate(pdfInfo.GetMoreInfo("ModDate"));
            if (modifiedDate.HasValue)
                customMetadata["ModifiedDate"] = modifiedDate.Value.ToString("O");

            var metadata = new DocumentMetadata
            {
                OriginalFileName = fileName,
                Format = DocFormat.Pdf,
                FileSizeBytes = stream.Length,
                ImportedAt = DateTime.UtcNow,
                WordCount = words,
                CharacterCount = characters,
                DetectedLanguage = DetectLanguage(fullText),
                Author = pdfInfo.GetAuthor(),
                Title = pdfInfo.GetTitle(),
                CreatedDate = TryParsePdfDate(pdfInfo.GetMoreInfo("CreationDate")),
                CustomMetadata = customMetadata
            };

            var structure = new DocumentStructure
            {
                Sections = sections,
                HeadingLevels = sections.Count != 0 ? sections.Max(s => s.Level) : 1,
                KeyConcepts = ExtractKeyPhrases(fullText),
                Complexity = AnalyzeComplexity(fullText, words),
                Tone = AnalyzeTone(fullText)
            };

            stopwatch.Stop();

            _logger.LogInformation(
                "Successfully parsed PDF: {FileName}, Pages: {Pages}, Words: {Words}, Sections: {Sections}, Duration: {Duration}ms",
                fileName, numberOfPages, words, sections.Count, stopwatch.ElapsedMilliseconds);

            return new DocumentImportResult
            {
                Success = true,
                Metadata = metadata,
                Structure = structure,
                RawContent = fullText,
                Warnings = warnings,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PDF parsing cancelled for: {FileName}", fileName);
            stopwatch.Stop();
            return CreateErrorResult("PDF parsing was cancelled", stopwatch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing PDF document: {FileName}", fileName);
            stopwatch.Stop();
            return CreateErrorResult($"Failed to parse PDF document: {ex.Message}", stopwatch);
        }
    }

    private List<DocumentSection> ParseSections(string text)
    {
        var sections = new List<DocumentSection>();
        var lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        
        var currentSection = new StringBuilder();
        string? currentHeading = null;
        var headingPattern = new Regex(@"^([A-Z][A-Za-z\s]{2,50})$", RegexOptions.Compiled);
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            if (headingPattern.IsMatch(trimmedLine) && trimmedLine.Length < 100)
            {
                if (currentSection.Length > 0 && currentHeading != null)
                {
                    AddSection(sections, currentHeading, currentSection.ToString());
                    currentSection.Clear();
                }
                currentHeading = trimmedLine;
            }
            else
            {
                currentSection.AppendLine(trimmedLine);
            }
        }

        if (currentSection.Length > 0)
        {
            AddSection(sections, currentHeading ?? "Content", currentSection.ToString());
        }

        if (sections.Count == 0)
        {
            sections.Add(new DocumentSection
            {
                Level = 1,
                Heading = "Document Content",
                Content = text,
                WordCount = CountWords(text),
                EstimatedSpeechDuration = EstimateSpeechDuration(text)
            });
        }

        return sections;
    }

    private void AddSection(List<DocumentSection> sections, string heading, string content)
    {
        var wordCount = CountWords(content);
        sections.Add(new DocumentSection
        {
            Level = 1,
            Heading = heading,
            Content = content,
            WordCount = wordCount,
            EstimatedSpeechDuration = EstimateSpeechDuration(content)
        });
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private TimeSpan EstimateSpeechDuration(string text)
    {
        var words = CountWords(text);
        var minutes = (double)words / AverageReadingSpeed;
        return TimeSpan.FromMinutes(minutes);
    }

    private string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "unknown";

        var sample = text.Length > 1000 ? text.Substring(0, 1000) : text;
        
        var englishWords = new[] { "the", "and", "is", "in", "to", "of", "a", "for", "on", "with" };
        var matchCount = englishWords.Count(word => 
            sample.Contains(word, StringComparison.OrdinalIgnoreCase));

        return matchCount >= 3 ? "en" : "unknown";
    }

    private List<string> ExtractKeyPhrases(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var words = text.Split(new[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '!', '?' }, 
            StringSplitOptions.RemoveEmptyEntries);
        
        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "the", "and", "is", "in", "to", "of", "a", "for", "on", "with", "as", "by", "at", "from"
        };

        var keyPhrases = words
            .Where(w => w.Length > 4 && !stopWords.Contains(w))
            .GroupBy(w => w.ToLowerInvariant())
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => g.Key)
            .ToList();

        return keyPhrases;
    }

    private DocumentComplexity AnalyzeComplexity(string text, int wordCount)
    {
        if (wordCount == 0)
        {
            return new DocumentComplexity
            {
                ReadingLevel = 0,
                ComplexityDescription = "Empty document"
            };
        }

        var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Length;
        var avgWordsPerSentence = sentences > 0 ? (double)wordCount / sentences : wordCount;
        
        var longWords = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Count(w => w.Length > 6);
        var complexityScore = (avgWordsPerSentence * 0.39) + ((double)longWords / wordCount * 100 * 11.8);

        string description;
        if (complexityScore < 60)
            description = "Easy to read - suitable for general audience";
        else if (complexityScore < 80)
            description = "Moderate complexity - suitable for educated audience";
        else
            description = "Complex - requires specialized knowledge";

        return new DocumentComplexity
        {
            ReadingLevel = Math.Round(complexityScore / 10, 1),
            ComplexityDescription = description
        };
    }

    private DocumentTone AnalyzeTone(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new DocumentTone
            {
                PrimaryTone = "Neutral",
                FormalityLevel = 0.5,
                WritingStyle = "Unknown"
            };
        }

        var formalIndicators = new[] { "therefore", "furthermore", "consequently", "nevertheless", "accordingly" };
        var informalIndicators = new[] { "really", "very", "quite", "pretty", "actually" };

        var lowerText = text.ToLowerInvariant();
        var formalCount = formalIndicators.Count(indicator => lowerText.Contains(indicator));
        var informalCount = informalIndicators.Count(indicator => lowerText.Contains(indicator));

        var totalIndicators = formalCount + informalCount;
        var formalityLevel = totalIndicators > 0 ? (double)formalCount / totalIndicators : 0.5;

        return new DocumentTone
        {
            PrimaryTone = "Professional",
            FormalityLevel = formalityLevel,
            WritingStyle = formalityLevel > 0.6 ? "Academic" : formalityLevel > 0.4 ? "Professional" : "Conversational"
        };
    }

    private DateTime? TryParsePdfDate(string? dateString)
    {
        if (string.IsNullOrWhiteSpace(dateString))
            return null;

        try
        {
            if (dateString.StartsWith("D:"))
            {
                dateString = dateString.Substring(2);
            }

            if (dateString.Length >= 14)
            {
                var year = int.Parse(dateString.Substring(0, 4));
                var month = int.Parse(dateString.Substring(4, 2));
                var day = int.Parse(dateString.Substring(6, 2));
                var hour = int.Parse(dateString.Substring(8, 2));
                var minute = int.Parse(dateString.Substring(10, 2));
                var second = int.Parse(dateString.Substring(12, 2));

                return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private DocumentImportResult CreateErrorResult(string errorMessage, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        return new DocumentImportResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ProcessingTime = stopwatch.Elapsed
        };
    }
}
