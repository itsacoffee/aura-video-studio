using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Content;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content.DocumentParsers;

/// <summary>
/// Parser for Microsoft Word documents (.docx)
/// Note: Full implementation requires DocumentFormat.OpenXml package
/// This is a basic stub implementation
/// </summary>
public class WordParser : IDocumentParser
{
    private readonly ILogger<WordParser> _logger;

    public DocumentFormat SupportedFormat => DocumentFormat.Word;
    public string[] SupportedExtensions => new[] { ".docx", ".doc" };

    public WordParser(ILogger<WordParser> logger)
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
        
        try
        {
            _logger.LogWarning("Word document parsing not fully implemented yet: {FileName}. Basic extraction attempted.", fileName);

            var content = "Word document detected. Full parsing requires DocumentFormat.OpenXml library. " +
                         "Please convert to .txt or .md format for full support.";

            var metadata = new DocumentMetadata
            {
                OriginalFileName = fileName,
                Format = DocumentFormat.Word,
                FileSizeBytes = stream.Length,
                ImportedAt = DateTime.UtcNow,
                WordCount = 0,
                CharacterCount = content.Length,
                DetectedLanguage = "en"
            };

            var section = new DocumentSection
            {
                Level = 1,
                Heading = "Document Content",
                Content = content,
                WordCount = 0,
                EstimatedSpeechDuration = TimeSpan.Zero
            };

            stopwatch.Stop();

            return new DocumentImportResult
            {
                Success = true,
                Metadata = metadata,
                Structure = new DocumentStructure
                {
                    Sections = new() { section },
                    HeadingLevels = 1,
                    KeyConcepts = new(),
                    Complexity = new DocumentComplexity
                    {
                        ReadingLevel = 10.0,
                        ComplexityDescription = "Unknown - requires full parsing"
                    },
                    Tone = new DocumentTone
                    {
                        PrimaryTone = "Professional",
                        FormalityLevel = 0.6,
                        WritingStyle = "Document"
                    }
                },
                RawContent = content,
                Warnings = new() { "Word document parsing requires additional libraries. Consider converting to plain text or Markdown." },
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Word document: {FileName}", fileName);
            stopwatch.Stop();
            
            return new DocumentImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to parse Word document: {ex.Message}. Try converting to .txt or .md format.",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }
}
