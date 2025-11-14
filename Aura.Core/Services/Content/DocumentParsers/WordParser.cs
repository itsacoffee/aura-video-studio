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
using Microsoft.Extensions.Logging;
using OpenXmlPackaging = DocumentFormat.OpenXml.Packaging;
using OpenXmlWordprocessing = DocumentFormat.OpenXml.Wordprocessing;

namespace Aura.Core.Services.Content.DocumentParsers;

/// <summary>
/// Parser for Microsoft Word documents (.docx) using DocumentFormat.OpenXml
/// Extracts text content, headings, metadata, and document structure
/// Note: Only supports .docx format (OpenXML), not legacy .doc format
/// </summary>
public class WordParser : IDocumentParser
{
    private readonly ILogger<WordParser> _logger;
    private const int MaxFileSize = 50 * 1024 * 1024; // 50MB limit
    private const int AverageReadingSpeed = 200; // words per minute

    public DocFormat SupportedFormat => DocFormat.Word;
    public string[] SupportedExtensions => new[] { ".docx" };

    public WordParser(ILogger<WordParser> logger)
    {
        _logger = logger;
    }

    public bool CanParse(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension == ".docx";
    }

    public async Task<DocumentImportResult> ParseAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();
        
        try
        {
            _logger.LogInformation("Starting Word document parsing for: {FileName}", fileName);

            if (stream.Length > MaxFileSize)
            {
                return CreateErrorResult($"Word file size ({stream.Length / (1024 * 1024)}MB) exceeds maximum allowed size (50MB)", stopwatch);
            }

            if (Path.GetExtension(fileName).ToLowerInvariant() == ".doc")
            {
                return CreateErrorResult("Legacy .doc format is not supported. Please convert to .docx format.", stopwatch);
            }

            ct.ThrowIfCancellationRequested();

            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream, ct).ConfigureAwait(false);
            memoryStream.Position = 0;

            using var wordDocument = OpenXmlPackaging.WordprocessingDocument.Open(memoryStream, false);
            
            if (wordDocument.MainDocumentPart == null)
            {
                return CreateErrorResult("Word document has no main document part", stopwatch);
            }

            var body = wordDocument.MainDocumentPart.Document.Body;
            if (body == null)
            {
                return CreateErrorResult("Word document has no body content", stopwatch);
            }

            var sections = new List<DocumentSection>();
            var fullText = new StringBuilder();
            var currentSection = new StringBuilder();
            string? currentHeading = null;
            int currentLevel = 1;

            foreach (var element in body.Elements())
            {
                ct.ThrowIfCancellationRequested();

                if (element is OpenXmlWordprocessing.Paragraph para)
                {
                    var text = GetParagraphText(para);
                    if (string.IsNullOrWhiteSpace(text))
                        continue;

                    var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
                    var isHeading = IsHeadingStyle(styleId);

                    if (isHeading)
                    {
                        if (currentSection.Length > 0 && currentHeading != null)
                        {
                            AddSection(sections, currentHeading, currentLevel, currentSection.ToString());
                            currentSection.Clear();
                        }

                        currentHeading = text;
                        currentLevel = GetHeadingLevel(styleId);
                        fullText.AppendLine(text);
                    }
                    else
                    {
                        currentSection.AppendLine(text);
                        fullText.AppendLine(text);
                    }
                }
                else if (element is OpenXmlWordprocessing.Table table)
                {
                    var tableText = ExtractTableText(table);
                    if (!string.IsNullOrWhiteSpace(tableText))
                    {
                        currentSection.AppendLine(tableText);
                        fullText.AppendLine(tableText);
                    }
                }
            }

            if (currentSection.Length > 0)
            {
                AddSection(sections, currentHeading ?? "Content", currentLevel, currentSection.ToString());
            }

            if (sections.Count == 0)
            {
                var content = fullText.ToString();
                if (string.IsNullOrWhiteSpace(content))
                {
                    warnings.Add("No text content could be extracted from the Word document.");
                    content = "[Empty Word document]";
                }

                sections.Add(new DocumentSection
                {
                    Level = 1,
                    Heading = "Document Content",
                    Content = content,
                    WordCount = CountWords(content),
                    EstimatedSpeechDuration = EstimateSpeechDuration(content)
                });
            }

            var finalText = fullText.ToString();
            var wordCount = CountWords(finalText);
            var coreProps = wordDocument.PackageProperties;

            var customMetadata = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(coreProps.Subject))
                customMetadata["Subject"] = coreProps.Subject;
            if (!string.IsNullOrWhiteSpace(coreProps.Keywords))
                customMetadata["Keywords"] = coreProps.Keywords;
            if (coreProps.Modified.HasValue)
                customMetadata["ModifiedDate"] = coreProps.Modified.Value.ToString("O");

            var metadata = new DocumentMetadata
            {
                OriginalFileName = fileName,
                Format = DocFormat.Word,
                FileSizeBytes = stream.Length,
                ImportedAt = DateTime.UtcNow,
                WordCount = wordCount,
                CharacterCount = finalText.Length,
                DetectedLanguage = DetectLanguage(finalText),
                Author = coreProps.Creator,
                Title = coreProps.Title,
                CreatedDate = coreProps.Created,
                CustomMetadata = customMetadata
            };

            var structure = new DocumentStructure
            {
                Sections = sections,
                HeadingLevels = sections.Count != 0 ? sections.Max(s => s.Level) : 1,
                KeyConcepts = ExtractKeyPhrases(finalText),
                Complexity = AnalyzeComplexity(finalText, wordCount),
                Tone = AnalyzeTone(finalText)
            };

            stopwatch.Stop();

            _logger.LogInformation(
                "Successfully parsed Word document: {FileName}, Words: {Words}, Sections: {Sections}, Duration: {Duration}ms",
                fileName, wordCount, sections.Count, stopwatch.ElapsedMilliseconds);

            return new DocumentImportResult
            {
                Success = true,
                Metadata = metadata,
                Structure = structure,
                RawContent = finalText,
                Warnings = warnings,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Word document parsing cancelled for: {FileName}", fileName);
            stopwatch.Stop();
            return CreateErrorResult("Word document parsing was cancelled", stopwatch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Word document: {FileName}", fileName);
            stopwatch.Stop();
            return CreateErrorResult($"Failed to parse Word document: {ex.Message}", stopwatch);
        }
    }

    private string GetParagraphText(OpenXmlWordprocessing.Paragraph paragraph)
    {
        var textBuilder = new StringBuilder();
        
        foreach (var run in paragraph.Elements<OpenXmlWordprocessing.Run>())
        {
            foreach (var text in run.Elements<OpenXmlWordprocessing.Text>())
            {
                textBuilder.Append(text.Text);
            }
        }
        
        return textBuilder.ToString();
    }

    private string ExtractTableText(OpenXmlWordprocessing.Table table)
    {
        var tableText = new StringBuilder();
        
        foreach (var row in table.Elements<OpenXmlWordprocessing.TableRow>())
        {
            var rowText = new StringBuilder();
            foreach (var cell in row.Elements<OpenXmlWordprocessing.TableCell>())
            {
                foreach (var para in cell.Elements<OpenXmlWordprocessing.Paragraph>())
                {
                    var cellText = GetParagraphText(para);
                    if (!string.IsNullOrWhiteSpace(cellText))
                    {
                        rowText.Append(cellText);
                        rowText.Append(" | ");
                    }
                }
            }
            
            if (rowText.Length > 0)
            {
                tableText.AppendLine(rowText.ToString().TrimEnd(' ', '|'));
            }
        }
        
        return tableText.ToString();
    }

    private bool IsHeadingStyle(string? styleId)
    {
        if (string.IsNullOrWhiteSpace(styleId))
            return false;

        return styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase) ||
               styleId.StartsWith("Title", StringComparison.OrdinalIgnoreCase);
    }

    private int GetHeadingLevel(string? styleId)
    {
        if (string.IsNullOrWhiteSpace(styleId))
            return 1;

        var match = Regex.Match(styleId, @"\d+");
        if (match.Success && int.TryParse(match.Value, out var level))
        {
            return Math.Max(1, Math.Min(6, level));
        }

        return 1;
    }

    private void AddSection(List<DocumentSection> sections, string heading, int level, string content)
    {
        var wordCount = CountWords(content);
        sections.Add(new DocumentSection
        {
            Level = level,
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
