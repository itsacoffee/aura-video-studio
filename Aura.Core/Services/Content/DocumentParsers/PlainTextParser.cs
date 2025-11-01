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

namespace Aura.Core.Services.Content.DocumentParsers;

/// <summary>
/// Parser for plain text documents (.txt)
/// </summary>
public class PlainTextParser : IDocumentParser
{
    private readonly ILogger<PlainTextParser> _logger;
    private const double DefaultWordsPerSecond = 2.5; // 150 words per minute

    public DocumentFormat SupportedFormat => DocumentFormat.PlainText;
    public string[] SupportedExtensions => new[] { ".txt" };

    public PlainTextParser(ILogger<PlainTextParser> logger)
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
            _logger.LogInformation("Parsing plain text document: {FileName}", fileName);

            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

            var metadata = ExtractMetadata(content, fileName, stream.Length);
            var structure = AnalyzeStructure(content);
            
            stopwatch.Stop();

            return new DocumentImportResult
            {
                Success = true,
                Metadata = metadata,
                Structure = structure,
                RawContent = content,
                ProcessingTime = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing plain text document: {FileName}", fileName);
            stopwatch.Stop();
            
            return new DocumentImportResult
            {
                Success = false,
                ErrorMessage = $"Failed to parse plain text: {ex.Message}",
                ProcessingTime = stopwatch.Elapsed
            };
        }
    }

    private DocumentMetadata ExtractMetadata(string content, string fileName, long fileSize)
    {
        var wordCount = CountWords(content);
        
        return new DocumentMetadata
        {
            OriginalFileName = fileName,
            Format = DocumentFormat.PlainText,
            FileSizeBytes = fileSize,
            ImportedAt = DateTime.UtcNow,
            WordCount = wordCount,
            CharacterCount = content.Length,
            DetectedLanguage = "en"
        };
    }

    private DocumentStructure AnalyzeStructure(string content)
    {
        var sections = new List<DocumentSection>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var currentSection = new StringBuilder();
        var currentHeading = "Introduction";
        var sectionIndex = 0;

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }

            if (IsLikelyHeading(trimmedLine))
            {
                if (currentSection.Length > 0)
                {
                    sections.Add(CreateSection(sectionIndex++, currentHeading, currentSection.ToString()));
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
            sections.Add(CreateSection(sectionIndex, currentHeading, currentSection.ToString()));
        }

        var complexity = AnalyzeComplexity(content);
        var tone = AnalyzeTone(content);
        var keyConcepts = ExtractKeyConcepts(content);

        return new DocumentStructure
        {
            Sections = sections,
            HeadingLevels = 1,
            KeyConcepts = keyConcepts,
            Complexity = complexity,
            Tone = tone
        };
    }

    private bool IsLikelyHeading(string line)
    {
        if (line.Length > 100) return false;
        
        if (line.EndsWith(':') || line.EndsWith('.'))
        {
            return false;
        }

        if (line.All(char.IsUpper) && line.Length < 60)
        {
            return true;
        }

        var words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length >= 2 && words.Length <= 10 && words.All(w => w.Length > 0 && char.IsUpper(w[0])))
        {
            return true;
        }

        return false;
    }

    private DocumentSection CreateSection(int index, string heading, string content)
    {
        var wordCount = CountWords(content);
        var duration = TimeSpan.FromSeconds(wordCount / DefaultWordsPerSecond);

        return new DocumentSection
        {
            Level = 1,
            Heading = heading,
            Content = content,
            WordCount = wordCount,
            EstimatedSpeechDuration = duration,
            Examples = ExtractExamples(content),
            VisualOpportunities = ExtractVisualOpportunities(content)
        };
    }

    private DocumentComplexity AnalyzeComplexity(string content)
    {
        var sentences = Regex.Split(content, @"[.!?]+").Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        var words = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        var avgSentenceLength = sentences.Count > 0 ? words.Length / sentences.Count : 0;
        var complexWords = words.Count(w => CountSyllables(w) >= 3);
        var readingLevel = CalculateFleschKincaid(words.Length, sentences.Count, CountTotalSyllables(words));

        return new DocumentComplexity
        {
            ReadingLevel = readingLevel,
            TechnicalDensity = CalculateTechnicalDensity(content),
            AbstractionLevel = 0.5,
            AverageSentenceLength = avgSentenceLength,
            ComplexWordCount = complexWords,
            ComplexityDescription = GetComplexityDescription(readingLevel)
        };
    }

    private DocumentTone AnalyzeTone(string content)
    {
        var formalityScore = CalculateFormalityScore(content);
        
        return new DocumentTone
        {
            PrimaryTone = DeterminePrimaryTone(content),
            FormalityLevel = formalityScore,
            WritingStyle = formalityScore > 0.6 ? "Formal" : "Conversational",
            ToneIndicators = new List<string>()
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

    private List<string> ExtractExamples(string content)
    {
        var examples = new List<string>();
        var patterns = new[] { "for example", "e.g.", "such as", "for instance" };
        
        foreach (var pattern in patterns)
        {
            var index = content.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var start = Math.Max(0, index - 20);
                var length = Math.Min(100, content.Length - start);
                examples.Add(content.Substring(start, length).Trim());
            }
        }
        
        return examples.Take(5).ToList();
    }

    private List<string> ExtractVisualOpportunities(string content)
    {
        var opportunities = new List<string>();
        var visualKeywords = new[] { "diagram", "chart", "graph", "illustration", "image", "photo", "picture", "visualization" };
        
        foreach (var keyword in visualKeywords)
        {
            if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                opportunities.Add($"Consider adding {keyword} visual element");
            }
        }
        
        return opportunities;
    }

    private int CountWords(string text)
    {
        return text.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private int CountSyllables(string word)
    {
        word = word.ToLowerInvariant();
        var vowels = new[] { 'a', 'e', 'i', 'o', 'u', 'y' };
        var syllableCount = 0;
        var previousWasVowel = false;

        foreach (var c in word)
        {
            var isVowel = vowels.Contains(c);
            if (isVowel && !previousWasVowel)
            {
                syllableCount++;
            }
            previousWasVowel = isVowel;
        }

        if (word.EndsWith("e"))
        {
            syllableCount--;
        }

        return Math.Max(1, syllableCount);
    }

    private int CountTotalSyllables(string[] words)
    {
        return words.Sum(CountSyllables);
    }

    private double CalculateFleschKincaid(int wordCount, int sentenceCount, int syllableCount)
    {
        if (sentenceCount == 0 || wordCount == 0) return 0;
        
        var asl = (double)wordCount / sentenceCount;
        var asw = (double)syllableCount / wordCount;
        
        return 0.39 * asl + 11.8 * asw - 15.59;
    }

    private double CalculateTechnicalDensity(string content)
    {
        var technicalTerms = new[] { "algorithm", "system", "process", "method", "analysis", "implementation", "framework" };
        var foundCount = technicalTerms.Count(term => content.Contains(term, StringComparison.OrdinalIgnoreCase));
        return foundCount / (double)technicalTerms.Length;
    }

    private double CalculateFormalityScore(string content)
    {
        var formalMarkers = new[] { "therefore", "however", "furthermore", "consequently", "moreover" };
        var informalMarkers = new[] { "gonna", "wanna", "yeah", "stuff", "things", "cool" };
        
        var formalCount = formalMarkers.Count(m => content.Contains(m, StringComparison.OrdinalIgnoreCase));
        var informalCount = informalMarkers.Count(m => content.Contains(m, StringComparison.OrdinalIgnoreCase));
        
        var total = formalCount + informalCount;
        return total == 0 ? 0.5 : formalCount / (double)total;
    }

    private string DeterminePrimaryTone(string content)
    {
        var lowerContent = content.ToLowerInvariant();
        
        if (lowerContent.Contains("research") || lowerContent.Contains("study"))
            return "Academic";
        if (lowerContent.Contains("you") || lowerContent.Contains("your"))
            return "Conversational";
        
        return "Professional";
    }

    private string GetComplexityDescription(double gradeLevel)
    {
        return gradeLevel switch
        {
            < 6 => "Elementary",
            < 9 => "Middle School",
            < 13 => "High School",
            < 16 => "College",
            _ => "Graduate"
        };
    }

    private bool IsCommonWord(string word)
    {
        var commonWords = new HashSet<string> { "the", "and", "that", "this", "with", "from", "have", "been", "were", "will", "would", "could", "should" };
        return commonWords.Contains(word);
    }
}
