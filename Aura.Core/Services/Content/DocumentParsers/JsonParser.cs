using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Aura.Core.Models.Content;
using Microsoft.Extensions.Logging;

namespace Aura.Core.Services.Content.DocumentParsers;

/// <summary>
/// Parser for JSON structured content (.json)
/// Supports both Aura script format and generic JSON content
/// </summary>
public class JsonParser : IDocumentParser
{
    private readonly ILogger<JsonParser> _logger;

    public DocFormat SupportedFormat => DocFormat.Json;
    public string[] SupportedExtensions => new[] { ".json" };

    public JsonParser(ILogger<JsonParser> logger)
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
            _logger.LogInformation("Parsing JSON document: {FileName}", fileName);

            using var reader = new StreamReader(stream, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

            var jsonDoc = JsonDocument.Parse(jsonContent);
            
            if (IsAuraScriptFormat(jsonDoc))
            {
                return ParseAuraScript(jsonDoc, fileName, stopwatch);
            }
            
            return ParseGenericJson(jsonDoc, fileName, jsonContent, stopwatch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON document: {FileName}", fileName);
            stopwatch.Stop();
            
            return new DocumentImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to parse JSON: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    private bool IsAuraScriptFormat(JsonDocument doc)
    {
        return doc.RootElement.TryGetProperty("scenes", out _) ||
               doc.RootElement.TryGetProperty("script", out _);
    }

    private DocumentImportResult ParseAuraScript(JsonDocument doc, string fileName, Stopwatch stopwatch)
    {
        var sections = new List<DocumentSection>();
        var content = new StringBuilder();

        if (doc.RootElement.TryGetProperty("scenes", out var scenesElement) && 
            scenesElement.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var scene in scenesElement.EnumerateArray())
            {
                var heading = scene.TryGetProperty("heading", out var h) ? h.GetString() ?? $"Scene {index + 1}" : $"Scene {index + 1}";
                var script = scene.TryGetProperty("script", out var s) ? s.GetString() ?? "" : "";
                
                if (!string.IsNullOrWhiteSpace(script))
                {
                    var wordCount = CountWords(script);
                    sections.Add(new DocumentSection
                    {
                        Level = 1,
                        Heading = heading,
                        Content = script,
                        WordCount = wordCount,
                        EstimatedSpeechDuration = TimeSpan.FromSeconds(wordCount / 2.5)
                    });
                    
                    content.AppendLine($"{heading}: {script}");
                }
                index++;
            }
        }

        var plainText = content.ToString();
        var metadata = new DocumentMetadata
        {
            OriginalFileName = fileName,
            Format = DocFormat.AuraScript,
            FileSizeBytes = 0,
            ImportedAt = DateTime.UtcNow,
            WordCount = CountWords(plainText),
            CharacterCount = plainText.Length,
            DetectedLanguage = "en"
        };

        stopwatch.Stop();

        return new DocumentImportResult
        {
            Success = true,
            Metadata = metadata,
            Structure = new DocumentStructure
            {
                Sections = sections,
                HeadingLevels = 1,
                KeyConcepts = ExtractKeyConcepts(plainText),
                Complexity = AnalyzeComplexity(plainText),
                Tone = new DocumentTone
                {
                    PrimaryTone = "Professional",
                    FormalityLevel = 0.6,
                    WritingStyle = "Script"
                }
            },
            RawContent = plainText,
            ProcessingTime = stopwatch.Elapsed
        };
    }

    private DocumentImportResult ParseGenericJson(JsonDocument doc, string fileName, string jsonContent, Stopwatch stopwatch)
    {
        var plainText = ExtractTextFromJson(doc.RootElement);
        
        var metadata = new DocumentMetadata
        {
            OriginalFileName = fileName,
            Format = DocFormat.Json,
            FileSizeBytes = jsonContent.Length,
            ImportedAt = DateTime.UtcNow,
            WordCount = CountWords(plainText),
            CharacterCount = plainText.Length,
            DetectedLanguage = "en"
        };

        var sections = new List<DocumentSection>
        {
            new DocumentSection
            {
                Level = 1,
                Heading = "Content",
                Content = plainText,
                WordCount = CountWords(plainText),
                EstimatedSpeechDuration = TimeSpan.FromSeconds(CountWords(plainText) / 2.5)
            }
        };

        stopwatch.Stop();

        return new DocumentImportResult
        {
            Success = true,
            Metadata = metadata,
            Structure = new DocumentStructure
            {
                Sections = sections,
                HeadingLevels = 1,
                KeyConcepts = ExtractKeyConcepts(plainText),
                Complexity = AnalyzeComplexity(plainText),
                Tone = new DocumentTone
                {
                    PrimaryTone = "Professional",
                    FormalityLevel = 0.5,
                    WritingStyle = "Structured Data"
                }
            },
            RawContent = plainText,
            ProcessingTime = stopwatch.Elapsed
        };
    }

    private string ExtractTextFromJson(JsonElement element)
    {
        var sb = new StringBuilder();

        void ExtractRecursive(JsonElement el, int depth = 0)
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.String:
                    var str = el.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                    {
                        sb.AppendLine(str);
                    }
                    break;
                
                case JsonValueKind.Array:
                    foreach (var item in el.EnumerateArray())
                    {
                        ExtractRecursive(item, depth + 1);
                    }
                    break;
                
                case JsonValueKind.Object:
                    foreach (var prop in el.EnumerateObject())
                    {
                        if (depth < 5)
                        {
                            ExtractRecursive(prop.Value, depth + 1);
                        }
                    }
                    break;
            }
        }

        ExtractRecursive(element);
        return sb.ToString();
    }

    private DocumentComplexity AnalyzeComplexity(string content)
    {
        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        return new DocumentComplexity
        {
            ReadingLevel = 10.0,
            TechnicalDensity = 0.5,
            AbstractionLevel = 0.5,
            AverageSentenceLength = 15,
            ComplexWordCount = 0,
            ComplexityDescription = "High School"
        };
    }

    private List<string> ExtractKeyConcepts(string content)
    {
        var words = content.Split(new[] { ' ', '\n', '\r', '\t', '.', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var wordFrequency = new Dictionary<string, int>();

        foreach (var word in words)
        {
            var cleaned = word.ToLowerInvariant().Trim();
            if (cleaned.Length > 4 && !IsCommonWord(cleaned))
            {
                wordFrequency[cleaned] = wordFrequency.GetValueOrDefault(cleaned) + 1;
            }
        }

        return wordFrequency
            .OrderByDescending(kvp => kvp.Value)
            .Take(10)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    private int CountWords(string text)
    {
        return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private bool IsCommonWord(string word)
    {
        var commonWords = new HashSet<string> { "the", "and", "that", "this", "with", "from", "have", "been", "were", "will", "would", "could", "should" };
        return commonWords.Contains(word);
    }
}
